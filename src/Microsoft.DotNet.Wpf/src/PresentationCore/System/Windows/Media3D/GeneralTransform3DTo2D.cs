// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Declaration of the GeneralTransform3DTo2D class.
//

using System.Windows.Media;
using System.Windows.Media.Animation;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// GeneralTransform3DTo2D class provides services to transform points and rects in 3D to 2D
    /// </summary>
    public class GeneralTransform3DTo2D : Freezable
    {
        internal GeneralTransform3DTo2D()
        {
            _transformBetween2D = null;        }
        
        internal GeneralTransform3DTo2D(Matrix3D projectionTransform, GeneralTransform transformBetween2D)
        {
            _projectionTransform = projectionTransform;
            _transformBetween2D = (GeneralTransform)transformBetween2D.GetAsFrozen();
        }
        
        /// <summary>
        /// Transform a point
        /// </summary>
        /// <param name="inPoint">Input point</param>
        /// <param name="result">Output point</param>
        /// <returns>True if the point was transformed successfuly, false otherwise</returns>
        public bool TryTransform(Point3D inPoint, out Point result)
        {
            bool success = false;
            result = new Point();
            
            // project the point
            if (_projectionTransform != null)
            {                Point3D projectedPoint = _projectionTransform.Transform(inPoint);

                if (_transformBetween2D != null)
                {
                    result = _transformBetween2D.Transform(new Point(projectedPoint.X, projectedPoint.Y));
                    success = true;
                }
            }            

            return success;
        }

        /// <summary>
        /// Transform a point from 3D in to 2D
        /// 
        /// If the transformation does not succeed, this will throw an InvalidOperationException.
        /// If you don't want to try/catch, call TryTransform instead and check the boolean it
        /// returns.
        ///
        /// </summary>
        /// <param name="point">Input point</param>
        /// <returns>The transformed point</returns>
        public Point Transform(Point3D point)
        {
            Point transformedPoint;

            if (!TryTransform(point, out transformedPoint))
            {
                throw new InvalidOperationException(SR.Get(SRID.GeneralTransform_TransformFailed, null));
            }

            return transformedPoint;
        }

        /// <summary>
        /// Transform a Rect3D to a Rect.  If this transformation cannot be completed Rect.Empty is returned.
        /// </summary>
        /// <param name="rect3D">Input 3D bounding box</param>
        /// <returns>The 2D bounding box of the projection of these points</returns>
        public Rect TransformBounds(Rect3D rect3D)
        {
            if (_transformBetween2D != null)
            {
                return _transformBetween2D.TransformBounds(MILUtilities.ProjectBounds(ref _projectionTransform, ref rect3D));            
            }
            else
            {
                return Rect.Empty;
            }
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new GeneralTransform3DTo2D();
        }
        

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCore(Freezable)">Freezable.CloneCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            GeneralTransform3DTo2D transform = (GeneralTransform3DTo2D)sourceFreezable;
            base.CloneCore(sourceFreezable);
            CopyCommon(transform);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCurrentValueCore(Freezable)">Freezable.CloneCurrentValueCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            GeneralTransform3DTo2D transform = (GeneralTransform3DTo2D)sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);
            CopyCommon(transform);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetAsFrozenCore(Freezable)">Freezable.GetAsFrozenCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            GeneralTransform3DTo2D transform = (GeneralTransform3DTo2D)sourceFreezable;
            base.GetAsFrozenCore(sourceFreezable);
            CopyCommon(transform);
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetCurrentValueAsFrozenCore(Freezable)">Freezable.GetCurrentValueAsFrozenCore</see>.
        /// </summary>
        /// <param name="sourceFreezable"></param>
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            GeneralTransform3DTo2D transform = (GeneralTransform3DTo2D)sourceFreezable;
            base.GetCurrentValueAsFrozenCore(sourceFreezable);
            CopyCommon(transform);
        }

        /// <summary>
        /// Clones values that do not have corresponding DPs
        /// </summary>
        /// <param name="transform"></param>
        private void CopyCommon(GeneralTransform3DTo2D transform)
        {
            _projectionTransform = transform._projectionTransform;
            _transformBetween2D = transform._transformBetween2D;
        }
        

        private Matrix3D _projectionTransform;
        private GeneralTransform _transformBetween2D;
} 
}
