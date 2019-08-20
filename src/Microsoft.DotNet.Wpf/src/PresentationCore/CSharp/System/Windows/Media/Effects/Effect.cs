// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Security;
using SecurityHelper=MS.Internal.SecurityHelper; 

namespace System.Windows.Media.Effects
{
    /// <summary>
    /// Effect
    /// </summary>
    public abstract partial class Effect
    {
        static Effect()
        {
            // Let the ImplicitInput be a very obscure brush, and treat it as
            // the implicit input if the exact color matches.  Note that this
            // color will be matched in native code that recognized
            // ImplicitInput, so don't change.
            
            // This specific brush is used as a placeholder for the implicit input 
            // texture. This is a little hacky, but *way* easier than adding a new 
            // Brush subclass for the implicit input and having it work properly 
            // as a real brush.
            
            ImplicitInput = new ImplicitInputBrush();
            ImplicitInput.Freeze();
        }

        /// Represents the Sampler-destined shader input that comes from
        /// context.  Like the representation of the UIElement when a
        /// UIElement.Effect is applied.  Intended to be set into an input of a
        /// ShaderEffect.
        [System.ComponentModel.BrowsableAttribute(false)]
        public static Brush ImplicitInput
        {
            get;
            private set;
        }


        /// <summary>
        /// Default constructor for an Effect.
        /// </summary>
        protected Effect()
        {
        }


        /// <summary>
        /// Takes in content bounds, and returns the bounds of the rendered
        /// output of that content after the Effect is applied.
        /// </summary>
        internal abstract Rect GetRenderBounds(Rect contentBounds);


        /// <summary>
        /// Input will get transformed through the inverse of this transform.
        /// TransformToAncestor/Descendant will also go through this.
        /// Override to modify from the identity transform.  Should expect
        /// incoming coordinates to be in the [0-1] range, and outgoing points
        /// should map to [0-1] as well.  The Inverse property should return a
        /// GeneralTransform that does the inverse mapping.  
        /// The inverse maps a point from after the effect was applied to the
        /// point that it came from before the effect.  The non-inverse maps
        /// where a point before the effect is applied goes after the effect is
        /// applied. 
        /// </summary>
        internal protected virtual GeneralTransform EffectMapping
        {
            get
            {
                return Transform.Identity;
            }
        }


        // The GeneralTransform returned by Effect is in unit space.  The
        // invocation of GeneralTransform is in world space.  This method
        // returns a GeneralTransform that maps between the two.
        internal GeneralTransform CoerceToUnitSpaceGeneralTransform(GeneralTransform gt,
                                                                    Rect worldBounds)
        {
            GeneralTransform result;
            
            // First, if the gt is identity, just return it straight away.
            if (gt == Transform.Identity)
            {
                result = Transform.Identity;
            }
            else 
            {
                // Maintain an MRU cache of GeneralTransforms with exactly one in
                // it.  May want to extend in the future.  Note that this will
                // thrash if the effect is used on multiple elements with different
                // sizes, but that's not our sweetspot.
                if (_mruWorldBounds != worldBounds || _mruInnerGeneralTransform != gt)
                {
                    _mruWorldBounds = worldBounds;
                    _mruInnerGeneralTransform = gt;
                    _mruWorldSpaceGeneralTransform = new UnitSpaceCoercingGeneralTransform(worldBounds, gt);
                }

                result = _mruWorldSpaceGeneralTransform;
            }

            return result;
        }

        // If worldBounds is Rect.Empty, will return (NaN, NaN) which tends to propagate silently and eventually show up as corruption much later
        private static Point UnitToWorldUnsafe(Point unitPoint, Rect worldBounds)
        {
            return new Point(
                worldBounds.Left + unitPoint.X * worldBounds.Width,
                worldBounds.Top  + unitPoint.Y * worldBounds.Height);
        }
        
        internal static Point? UnitToWorld(Point unitPoint, Rect worldBounds)
        {
            return worldBounds.IsEmpty ? null : new Nullable<Point>(UnitToWorldUnsafe(unitPoint, worldBounds));
        }

        internal static Point? WorldToUnit(Point worldPoint, Rect worldBounds)
        {
            if (worldBounds.Width == 0 || worldBounds.Height == 0)
            {
                return null;
            }

            return new Point(
                (worldPoint.X - worldBounds.Left) / worldBounds.Width,
                (worldPoint.Y - worldBounds.Top)  / worldBounds.Height);
        }

        internal static Rect UnitToWorld(Rect unitRect, Rect worldBounds)
        {
            return worldBounds.IsEmpty 
                ? Rect.Empty
                : new Rect(UnitToWorldUnsafe(unitRect.TopLeft, worldBounds),
                                UnitToWorldUnsafe(unitRect.BottomRight, worldBounds));
        }

        internal static Rect? WorldToUnit(Rect worldRect, Rect worldBounds)
        {
            Point? tl = WorldToUnit(worldRect.TopLeft, worldBounds);
            Point? br = WorldToUnit(worldRect.BottomRight, worldBounds);
            
            if (tl == null || br == null)
            {
                return null;
            }

            return new Rect(tl.Value, br.Value);
        }

        
        // Private GeneralTransform subclass that's all about transforming from
        // and to the unit square.
        private class UnitSpaceCoercingGeneralTransform : GeneralTransform
        {
            public UnitSpaceCoercingGeneralTransform(Rect worldBounds, GeneralTransform innerTransform)
            {
                _worldBounds = worldBounds;
                _innerTransform = innerTransform;
                _isInverse = false;
            }

            public override GeneralTransform Inverse
            {
                get 
                {
                    if (_inverseTransform == null)
                    {
                        // We can cache the clone because the _worldBounds and
                        // inner transform won't change.
                        _inverseTransform = (UnitSpaceCoercingGeneralTransform)this.Clone();
                        _inverseTransform._isInverse = !_isInverse;
                    }
                    return _inverseTransform;
                }
            }

            public override Rect TransformBounds(Rect rect)
            {
                // Since this doesn't rotate or skew, we can just pass each
                // point to the point transformer.
                Point topLeftResult = new Point();
                Point bottomRightResult = new Point();
                
                bool ok = TryTransform(rect.TopLeft, out topLeftResult)
                       && TryTransform(rect.BottomRight, out bottomRightResult);
                
                if (!ok)
                {
                    return Rect.Empty;
                }

                return new Rect(topLeftResult, bottomRightResult);
            }

            public override bool TryTransform(Point inPoint, out Point result)
            {
                bool ok = false;
                result = new Point();

                // Both the normal and the inverse require the point to first be
                // translated from world space to unit space.
                Point? unitSpace = Effect.WorldToUnit(inPoint, _worldBounds);
                if (unitSpace != null)
                {
                    // Now just run through the normal or the inverse version of the
                    // inner effect.
                    GeneralTransform innerToUse = GetCorrectInnerTransform();

                    Point unitResult;
                    if (innerToUse.TryTransform(unitSpace.Value, out unitResult))
                    {
                        // Both the normal and the inverse require the unit-space result
                        // to be converted back to world space
                        Point? worldSpace = Effect.UnitToWorld(unitResult, _worldBounds);
                        if (worldSpace != null)
                        {
                            result = worldSpace.Value;
                            ok = true;
                        }
                    }
                }

                return ok;
            }

            protected override Freezable CreateInstanceCore()
            {
                return new UnitSpaceCoercingGeneralTransform(_worldBounds, _innerTransform) { _isInverse = _isInverse };
            }

            private GeneralTransform GetCorrectInnerTransform()
            {
                GeneralTransform result;
                if (_isInverse)
                {
                    if (_innerTransformInverse == null)
                    {
                        // Cache the inverse so it doesn't get new'd up all the
                        // time. 
                        _innerTransformInverse = _innerTransform.Inverse;
                    }
                    result = _innerTransformInverse;
                }
                else
                {
                    result = _innerTransform;
                }
                
                return result;
            }
            
            private readonly Rect _worldBounds;
            private readonly GeneralTransform _innerTransform;
            private GeneralTransform _innerTransformInverse = null;
            private bool _isInverse;
            private UnitSpaceCoercingGeneralTransform _inverseTransform = null;
        }
        

        // Stores the "cache" of bounds x inner transform -> world space transform.
        private Rect _mruWorldBounds = Rect.Empty;
        private GeneralTransform _mruInnerGeneralTransform = null;
        private GeneralTransform _mruWorldSpaceGeneralTransform;
    }
}

