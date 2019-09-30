// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Diagnostics;
using System.Windows;
using MS.Internal.Media3D;
using System.ComponentModel.Design.Serialization;
using System.Windows.Markup;
using CultureInfo = System.Globalization.CultureInfo;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     The MatrixCamera subclass of Camera provides a means for directly
    ///     specifying a Matrix as the projection transformation.  This is
    ///     useful for apps that have their own projection matrix calculation
    ///     mechanisms.
    /// </summary>
    public partial class MatrixCamera : Camera
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Default constructor
        /// </summary>
        public MatrixCamera() {}

        /// <summary>
        ///     Construct a MatrixCamera from view and projection matrices
        /// </summary>
        public MatrixCamera(Matrix3D viewMatrix, Matrix3D projectionMatrix)
        {
            ViewMatrix = viewMatrix;
            ProjectionMatrix = projectionMatrix;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // NOTE:  We consider Camera.Transform to be part of the view matrix
        //        here, where as the read/write ViewMatrix property does not
        //        include the camera's Transform.
        internal override Matrix3D GetViewMatrix()
        {
            Matrix3D viewMatrix = ViewMatrix;
            PrependInverseTransform(Transform, ref viewMatrix);
            return viewMatrix;
        }
        
        internal override Matrix3D GetProjectionMatrix(double aspectRatio) { return ProjectionMatrix; }

        internal override RayHitTestParameters RayFromViewportPoint(Point p, Size viewSize, Rect3D boundingRect, out double distanceAdjustment)
        {
            //
            //  Compute rayParameters
            //
            
            // Find the point on the projection plane in post-projective space where
            // the viewport maps to a 2x2 square from (-1,1)-(1,-1).
            Point np = M3DUtil.GetNormalizedPoint(p, viewSize);

            // This ray is consistent with a left-handed camera
            // MatrixCamera should be right-handed and should be updated to be so.
            
            // So (conceptually) the user clicked on the point (np.X,
            // np.Y, 0) in post-projection clipping space and the ray
            // extends in the direction (0, 0, 1) because our ray
            // after projection looks down the positive z axis.  We
            // need to convert this ray and direction back to world
            // space.

            Matrix3D worldToCamera = GetViewMatrix() * ProjectionMatrix;
            Matrix3D cameraToWorld = worldToCamera;

            if (!cameraToWorld.HasInverse)
            {
                // When the following issue is addressed we should 
                //   investigate if the custom code paths in Orthographic and PerspectiveCamera
                //   are worth keeping.  They may not be buying us anything aside from handling
                //   singular matrices.

                // Need to handle singular matrix cameras
                throw new NotSupportedException(SR.Get(SRID.HitTest_Singular));
            }
            
            cameraToWorld.Invert();

            Point4D origin4D = new Point4D(np.X,np.Y,0,1) * cameraToWorld;
            Point3D origin = new Point3D( origin4D.X/origin4D.W,
                                          origin4D.Y/origin4D.W,
                                          origin4D.Z/origin4D.W );

            // To transform the direction we use the Jacobian of
            // cameraToWorld at the point np.X,np.Y,0 that we just
            // transformed.
            //
            // The Jacobian of the homogeneous matrix M is a 3x3 matrix.
            //
            // Let x be the point we are computing the Jacobian at, and y be the
            // result of transforming x by M, i.e.
            // (wy w) = (x 1) M
            // Where (wy w) is the homogeneous point representing y with w as its homogeneous coordinate
            // And (x 1) is the homogeneous point representing x with 1 as its homogeneous coordinate
            //
            // Then the i,j component of the Jacobian (at x) is
            // (M_ij - M_i4 y_j) / w
            //
            // Since we're only concerned with the direction of the
            // transformed vector and not its magnitude, we can scale
            // this matrix by a POSITIVE factor.  The computation
            // below computes the Jacobian scaled by 1/w and then
            // after we normalize the final vector we flip it around
            // if w is negative.
            //
            // To transform a vector we just right multiply it by this Jacobian matrix.
            //
            // Compute the Jacobian at np.X,np.Y,0 ignoring the constant factor of w.
            // Here's the pattern
            //
            // double Jij = cameraToWorld.Mij - cameraToWorld.Mi4 * origin.j
            //
            // but we only need J31,J32,&J33 because we're only
            // transforming the vector 0,0,1

            double J31 = cameraToWorld.M31 - cameraToWorld.M34 * origin.X;
            double J32 = cameraToWorld.M32 - cameraToWorld.M34 * origin.Y;
            double J33 = cameraToWorld.M33 - cameraToWorld.M34 * origin.Z;

            // Then multiply that matrix by (0, 0, 1) which is
            // the direction of the ray in post-projection space.
            Vector3D direction = new Vector3D( J31, J32, J33 );
            direction.Normalize();

            // We multiplied by the Jacobian times W, so we need to
            // account for whether that flipped our result or not.
            if (origin4D.W < 0)
            {
                direction = -direction;
            }
            
            RayHitTestParameters rayParameters = new RayHitTestParameters(origin, direction);

            //
            //  Compute HitTestProjectionMatrix
            //

            // The viewportMatrix will take normalized clip space into
            // viewport coordinates, with an additional 2D translation
            // to put the ray at the origin.
            Matrix3D viewportMatrix = new Matrix3D();
            viewportMatrix.TranslatePrepend(new Vector3D(-p.X,viewSize.Height-p.Y,0));
            viewportMatrix.ScalePrepend(new Vector3D(viewSize.Width/2,-viewSize.Height/2,1));
            viewportMatrix.TranslatePrepend(new Vector3D(1,1,0));
            
            // First, world-to-camera, then camera's projection, then normalized clip space to viewport.
            rayParameters.HitTestProjectionMatrix = 
                worldToCamera *
                viewportMatrix;

            // 
            // MatrixCamera does not allow for Near/Far plane adjustment, so
            // the distanceAdjustment remains 0.
            //
            distanceAdjustment = 0.0;

            return rayParameters;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
    }
}
