// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: The implementation of HitTestWithPointDrawingContextWalker,
//              used to perform hit tests with a point on renderdata.
//
//

using MS.Internal;
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace System.Windows.Media
{
    /// <summary>
    /// HitTestDrawingContextWalker - a DrawingContextWalker which performs a hit test with a point
    /// </summary>
    internal class HitTestWithPointDrawingContextWalker: HitTestDrawingContextWalker
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="point"> Point - the point to hit test, in local coordinates. </param>
        internal HitTestWithPointDrawingContextWalker(Point point)
        {
            _point = point;
        }

        /// <summary>
        /// IsHit Property - Returns whether the point hit the drawing instructions.
        /// </summary>
        internal override bool IsHit
        {
            get
            {
                return _contains;
            }
        }

        internal override IntersectionDetail IntersectionDetail
        {
            get
            {
                return _contains ? IntersectionDetail.FullyInside : IntersectionDetail.Empty;
            }
        }

        #region Static Drawing Context Methods

        /// <summary>
        ///     DrawGeometry -
        ///     Draw a Geometry with the provided Brush and/or Pen.
        ///     If both the Brush and Pen are null this call is a no-op.
        /// </summary>
        /// <param name="brush">
        ///     The Brush with which to fill the Geometry.
        ///     This is optional, and can be null, in which case no fill is performed.
        /// </param>
        /// <param name="pen">
        ///     The Pen with which to stroke the Geometry.
        ///     This is optional, and can be null, in which case no stroke is performed.
        /// </param>
        /// <param name="geometry"> The Geometry to fill and/or stroke. </param>
        public override void DrawGeometry(
            Brush brush,
            Pen pen,
            Geometry geometry)
        {
            if (IsCurrentLayerNoOp ||(geometry == null) || geometry.IsEmpty())
            {
                return;
            }

            if (brush != null)
            {
                _contains |= geometry.FillContains(_point);
            }

            // If we have a pen and we haven't yet hit, try the widened geometry.
            if ((pen != null) && !_contains)
            {
                _contains |= geometry.StrokeContains(pen, _point);
            }

            // If we've hit, stop walking.
            if (_contains)
            {
                StopWalking();
            }
        }

        /// <summary>
        /// Draw a GlyphRun.
        /// </summary>
        /// <param name="foregroundBrush">Foreground brush to draw GlyphRun with. </param>
        /// <param name="glyphRun"> The GlyphRun to draw. </param>
        /// <exception cref="ObjectDisposedException">
        /// This call is illegal if this object has already been closed or disposed.
        /// </exception>
        public override void DrawGlyphRun(Brush foregroundBrush, GlyphRun glyphRun)
        {
            if (!IsCurrentLayerNoOp && (glyphRun != null))
            {
                // The InkBoundingBox + the Origin produce the true InkBoundingBox.
                Rect rectangle = glyphRun.ComputeInkBoundingBox();

                if (!rectangle.IsEmpty)
                {
                    rectangle.Offset((Vector)glyphRun.BaselineOrigin);

                    _contains |= rectangle.Contains(_point);

                    // If we've hit, stop walking.
                    if (_contains)
                    {
                        StopWalking();
                    }
                }
            }
        }

        /// <summary>
        ///     PushClip -
        ///     Push a clip region, which will apply to all drawing primitives until the
        ///     corresponding Pop call.
        /// </summary>
        /// <param name="clipGeometry"> The Geometry to which we will clip. </param>
        public override void PushClip(
            Geometry clipGeometry)
        {
            if (!IsPushNoOp())
            {
                PushPointStack(_point);

                // If the clip being pushed doesn't contain the hit test point,
                // then we don't need to consider any of the subsequent Drawing
                // operations in this layer.
                if ((clipGeometry != null) && !clipGeometry.FillContains(_point))
                {
                    IsCurrentLayerNoOp = true;
                }
            }
        }

        /// <summary>
        ///     PushOpacityMask -
        ///     Push an opacity mask
        /// </summary>
        /// <param name="brush">
        ///     The opacity mask brush
        /// </param>
        public override void PushOpacityMask(Brush brush)
        {
            if (!IsPushNoOp())
            {
                // This Push doesn't affect the hit test, so just push the current point
                PushPointStack(_point);
            }
        }

        /// <summary>
        ///     PushOpacity -
        ///     Push an opacity which will blend the composite of all drawing primitives added
        ///     until the corresponding Pop call.
        /// </summary>
        /// <param name="opacity">
        ///     The opacity with which to blend - 0 is transparent, 1 is opaque.
        /// </param>
        public override void PushOpacity(
            Double opacity)
        {
            if (!IsPushNoOp())
            {
                // This Push doesn't affect the hit test, so just push the current point
                PushPointStack(_point);
            }
        }

        /// <summary>
        ///     PushTransform -
        ///     Push a Transform which will apply to all drawing operations until the corresponding
        ///     Pop.
        /// </summary>
        /// <param name="transform"> The Transform to push. </param>
        public override void PushTransform(
            Transform transform)
        {
            if (!IsPushNoOp())
            {
                if (transform == null || transform.IsIdentity)
                {
                    PushPointStack(_point);
                }
                else
                {
                    Matrix matrix = transform.Value;

                    if (matrix.HasInverse)
                    {
                        // Invert the transform.  The inverse will be applied to the point
                        // so that hit testing is done in the original geometry's coordinates

                        matrix.Invert();

                        // Push the transformed point on the stack.  This also updates _point.
                        PushPointStack(_point * matrix);
                    }
                    else
                    {
                        // If this transform doesn't have an inverse, then we don't need to consider any
                        // of the subsequent Drawing operations in this layer.
                        IsCurrentLayerNoOp = true;
                    }
                }
            }
        }

        /// <summary>
        ///     PushGuidelineSet - 
        ///     Push a set of guidelines which should be applied
        ///     to all drawing operations until the 
        ///     corresponding Pop.
        /// </summary>
        /// <param name="guidelines"> The GuidelineSet to push. </param>
        public override void PushGuidelineSet(
            GuidelineSet guidelines)
        {
            if (!IsPushNoOp())
            {
                // This Push doesn't affect the hit test, so just push the current point
                PushPointStack(_point);
            }
        }

        /// <summary>
        ///     PushGuidelineY1 - 
        ///     Explicitly push one horizontal guideline.
        /// </summary>
        /// <param name="coordinate"> The coordinate of leading guideline. </param>
        internal override void PushGuidelineY1(
            Double coordinate)
        {
            if (!IsPushNoOp())
            {
                // This Push doesn't affect the hit test, so just push the current point
                PushPointStack(_point);
            }
        }


        /// <summary>
        ///     PushGuidelineY2 - 
        ///     Explicitly push a pair of horizontal guidelines.
        /// </summary>
        /// <param name="leadingCoordinate">
        ///     The coordinate of leading guideline.
        /// </param>
        /// <param name="offsetToDrivenCoordinate">
        ///     The offset from leading guideline to driven guideline.
        /// </param>
        internal override void PushGuidelineY2(
            Double leadingCoordinate,
            Double offsetToDrivenCoordinate)
        {
            if (!IsPushNoOp())
            {
                // This Push doesn't affect the hit test, so just push the current point
                PushPointStack(_point);
            }
        }


        /// <summary>
        ///     PushEffect - 
        ///     Push a BitmapEffect which will apply to all drawing operations until the 
        ///     corresponding Pop.
        /// </summary>
        /// <param name="effect"> The BitmapEffect to push. </param>
        /// <param name="effectInput"> The BitmapEffectInput. </param>
        [Obsolete(MS.Internal.Media.VisualTreeUtils.BitmapEffectObsoleteMessage)]
        public override void PushEffect(
            BitmapEffect effect,
            BitmapEffectInput effectInput)
        {
            if (!IsPushNoOp())
            {
                // This API has been deprecated, so any BitmapEffect is ignored.
                PushPointStack(_point);
            }               
        }

        /// <summary>
        /// Pop
        /// </summary>
        public override void Pop(
            )
        {
            if (!IsPopNoOp())
            {
                PopPointStack();
            }
        }

        #endregion Static Drawing Context Methods

        #region Private Methods

        /// <summary>
        /// PushPointStack - push a point onto the stack and update _point with it.
        /// </summary>
        /// <param name="point"> The new Point to push. </param>
        private void PushPointStack(Point point)
        {
            if (_pointStack == null)
            {
                _pointStack = new Stack<Point>(2);
            }

            // Push the old point.
            _pointStack.Push(_point);

            // update current point
            _point = point;
        }

        /// <summary>
        /// PopPointStack - pop a point off of the point stack and update _point.
        /// </summary>
        private void PopPointStack()
        {
            // We must have a point stack and it must not be empty.
            Debug.Assert(_pointStack != null);
            Debug.Assert(_pointStack.Count > 0);

            // Retrieve the previous point from the stack.
            _point = _pointStack.Pop();
        }

        /// <summary>
        /// Called by every Push operation, this method returns whether or not
        /// the operation should be a no-op.  If the current subgraph layer
        /// is being no-op'd, it also increments the no-op depth.
        /// </summary>        
        private bool IsPushNoOp()
        {
            if (IsCurrentLayerNoOp)
            {
                // Increment the depth so that the no-op status isn't reset
                // when this layer's cooresponding Pop is called.
                _noOpLayerDepth++;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Called by Pop, this method returns whether or not Pop should be
        /// a no-op'd.  If the current subgraph layer is being no-op'd, it also
        /// decrements the no-op depth, then reset's the no-op status if this
        /// is the last Pop in the no-op layer.
        /// </summary>        
        private bool IsPopNoOp()
        {
            if (IsCurrentLayerNoOp)
            {
                Debug.Assert(_noOpLayerDepth >= 1);
                
                _noOpLayerDepth--;

                // If this Pop cooresponds to the Push that created
                // the no-op layer, then reset the no-op status.
                if (_noOpLayerDepth == 0)
                {
                    IsCurrentLayerNoOp = false;
                }
                
                return true;
            }
            else
            {
                return false;
            }            
        }

        /// <summary>
        /// Set/resets and gets whether or not the current subgraph layer is a no-op.
        /// Currently, all subsequent instructions are skipped (no-op'd) when a non-invertible
        /// transform is pushed (because we have to invert the matrix to perform
        /// a hit-test), or during a point hit-test when a clip is pushed that 
        /// doesn't contain the point.
        /// </summary>   
        private bool IsCurrentLayerNoOp
        {
            set
            {
                if (value == true)
                {
                    // Guard that we aren't already in a no-op layer
                    //
                    // Instructions that can cause the layer to be no-op'd should be
                    // no-op'd themselves, and thus can't call this method,  if we 
                    // are already in a no-op layer
                    Debug.Assert(!_currentLayerIsNoOp);
                    Debug.Assert(_noOpLayerDepth == 0);

                    // Set the no-op status & initial depth
                    _currentLayerIsNoOp = true;
                    _noOpLayerDepth++;
                }
                else
                {
                    // Guard that we are in a no-op layer, and that the correct corresponding 
                    // Pop has been called.
                    Debug.Assert(_currentLayerIsNoOp);
                    Debug.Assert(_noOpLayerDepth == 0);   

                    // Reset the no-op status
                    _currentLayerIsNoOp = false;                    
                }
            }
            
            get
            {
                return _currentLayerIsNoOp;
            }
        }

        #endregion Private Methods


        #region Private Fields

        // If _isPointHitTest is true, this _point is the hit test point.
        private Point _point;

        // The hit test point transformed to target geometry's original coordinates
        private Stack<Point> _pointStack;

        // When true, all instructions should be perform no logic until the
        // layer is exited via a Pop()
        private bool _currentLayerIsNoOp;

        // Number of Pop() calls until _currentLayerIsNoOp should be reset.
        private int _noOpLayerDepth;

        #endregion Private Fields
    }
}

