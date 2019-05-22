// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Declaration of the GeneralTransform2DTo3D class.
//

using System.Diagnostics;

using System.Windows.Media;
using System.Windows.Media.Animation;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// TransformTo3D class provides services to transform points from 2D in to 3D
    /// </summary>
    public class GeneralTransform2DTo3D : Freezable
    {
        internal GeneralTransform2DTo3D()
        {
        }
        
        internal GeneralTransform2DTo3D(GeneralTransform transform2D, 
                                        Viewport2DVisual3D containingVisual3D, 
                                        GeneralTransform3D transform3D)
        {            
            Visual child = containingVisual3D.Visual;

            Debug.Assert(child != null, "Going from 2D to 3D containingVisual3D.Visual should not be null");
            
            _transform3D = (GeneralTransform3D)transform3D.GetCurrentValueAsFrozen();

            // we also need to go one more level up to handle a transform being placed
            // on the Viewport2DVisual3D's child
            GeneralTransformGroup transformGroup = new GeneralTransformGroup();
            transformGroup.Children.Add((GeneralTransform)transform2D.GetCurrentValueAsFrozen());
            transformGroup.Children.Add((GeneralTransform)child.TransformToOuterSpace().GetCurrentValueAsFrozen());
            transformGroup.Freeze();
            _transform2D = transformGroup;

            _positions = containingVisual3D.InternalPositionsCache;
            _textureCoords = containingVisual3D.InternalTextureCoordinatesCache;
            _triIndices = containingVisual3D.InternalTriangleIndicesCache;

            _childBounds = child.CalculateSubgraphRenderBoundsOuterSpace();
        }
        
        /// <summary>
        /// Transform a point
        /// </summary>
        /// <param name="inPoint">Input point</param>
        /// <param name="result">Output point</param>
        /// <returns>True if the point was transformed successfuly, false otherwise</returns>
        public bool TryTransform(Point inPoint, out Point3D result)
        {
            Point final2DPoint;

            // assign this now so that we can return false if needed
            result = new Point3D();
            
            if (!_transform2D.TryTransform(inPoint, out final2DPoint))
            {
                return false;
            }
                       
            Point texCoord = Viewport2DVisual3D.VisualCoordsToTextureCoords(final2DPoint, _childBounds);

            // need to walk the texture coordinates on the Viewport2DVisual3D
            // and look for where this point intersects one of them
            Point3D coordPoint;
            if (!Viewport2DVisual3D.Get3DPointFor2DCoordinate(texCoord, 
                                                            out coordPoint,
                                                            _positions,
                                                            _textureCoords,
                                                            _triIndices))
            {
                return false;}

            if (!_transform3D.TryTransform(coordPoint, out result))
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Transform a point from 2D in to 3D
        /// 
        /// If the transformation does not succeed, this will throw an InvalidOperationException.
        /// If you don't want to try/catch, call TryTransform instead and check the boolean it
        /// returns.
        ///
        /// </summary>
        /// <param name="point">Input point</param>
        /// <returns>The transformed point</returns>
        public Point3D Transform(Point point)
        {
            Point3D transformedPoint;

            if (!TryTransform(point, out transformedPoint))
            {
                throw new InvalidOperationException(SR.Get(SRID.GeneralTransform_TransformFailed, null));
            }

            return transformedPoint;
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new GeneralTransform2DTo3D();
        }
        

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCore(Freezable)">Freezable.CloneCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            GeneralTransform2DTo3D transform = (GeneralTransform2DTo3D)sourceFreezable;
            base.CloneCore(sourceFreezable);
            CopyCommon(transform);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCurrentValueCore(Freezable)">Freezable.CloneCurrentValueCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            GeneralTransform2DTo3D transform = (GeneralTransform2DTo3D)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);
            CopyCommon(transform);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetAsFrozenCore(Freezable)">Freezable.GetAsFrozenCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            GeneralTransform2DTo3D transform = (GeneralTransform2DTo3D)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);
            CopyCommon(transform);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetCurrentValueAsFrozenCore(Freezable)">Freezable.GetCurrentValueAsFrozenCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            GeneralTransform2DTo3D transform = (GeneralTransform2DTo3D)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            CopyCommon(transform);
        }

        /// <summary>
        /// Clones values that do not have corresponding DPs
        /// </summary>
        /// <param name="transform"></param>
        private void CopyCommon(GeneralTransform2DTo3D transform)
        {
            _transform2D = transform._transform2D;
            _transform3D = transform._transform3D;
            _positions = transform._positions;
            _textureCoords = transform._textureCoords;
            _triIndices = transform._triIndices;
            _childBounds = transform._childBounds;
        }

        private GeneralTransform _transform2D;
        private GeneralTransform3D _transform3D;

        private Point3DCollection _positions;
        private PointCollection _textureCoords;
        private Int32Collection _triIndices;

        private Rect _childBounds;
    } 
}

