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

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     Encapsulates a perspective projection camera.
    /// </summary>
    public partial class PerspectiveCamera : ProjectionCamera
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        ///<summary />
        public PerspectiveCamera() {}

        ///<summary />
        public PerspectiveCamera(Point3D position, Vector3D lookDirection, Vector3D upDirection, double fieldOfView)
        {
            Position = position;
            LookDirection = lookDirection;
            UpDirection = upDirection;
            FieldOfView = fieldOfView;
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
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal Matrix3D GetProjectionMatrix(double aspectRatio, double zn, double zf)
        {
            double fov = M3DUtil.DegreesToRadians(FieldOfView);

            // Note: h and w are 1/2 of the inverse of the width/height ratios:
            //
            //  h = 1/(heightDepthRatio) * (1/2)
            //  w = 1/(widthDepthRatio) * (1/2)
            //
            // Computation for h is a bit different than what you will find in
            // D3DXMatrixPerspectiveFovRH because we have a horizontal rather
            // than vertical FoV.

            double halfWidthDepthRatio = Math.Tan(fov/2);
            double h = aspectRatio/halfWidthDepthRatio;
            double w = 1/halfWidthDepthRatio;

            double m22 = zf != Double.PositiveInfinity ? zf/(zn-zf) : -1;
            double m32 = zn*m22;

            return new Matrix3D(
                w,  0,  0,     0,
                0,  h,  0,     0,
                0,  0,  m22,  -1,
                0,  0,  m32,   0);
        }

        internal override Matrix3D GetProjectionMatrix(double aspectRatio)
        {
            return GetProjectionMatrix(aspectRatio, NearPlaneDistance, FarPlaneDistance);
        }

        internal override RayHitTestParameters RayFromViewportPoint(Point p, Size viewSize, Rect3D boundingRect, out double distanceAdjustment)
        {
            // The camera may be animating.  Take a snapshot of the current value
            // and get the property values we need. (Window OS #992662)
            Point3D position = Position;
            Vector3D lookDirection = LookDirection;
            Vector3D upDirection = UpDirection;
            Transform3D transform = Transform;
            double zn = NearPlaneDistance;
            double zf = FarPlaneDistance;
            double fov = M3DUtil.DegreesToRadians(FieldOfView);

            //
            //  Compute rayParameters
            //
            
            // Find the point on the projection plane in post-projective space where
            // the viewport maps to a 2x2 square from (-1,1)-(1,-1).
            Point np = M3DUtil.GetNormalizedPoint(p, viewSize);

            // Note: h and w are 1/2 of the inverse of the width/height ratios:
            //
            //  h = 1/(heightDepthRatio) * (1/2)
            //  w = 1/(widthDepthRatio) * (1/2)
            //
            // Computation for h is a bit different than what you will find in
            // D3DXMatrixPerspectiveFovRH because we have a horizontal rather
            // than vertical FoV.
            double aspectRatio = M3DUtil.GetAspectRatio(viewSize);
            double halfWidthDepthRatio = Math.Tan(fov/2);
            double h = aspectRatio/halfWidthDepthRatio;
            double w = 1/halfWidthDepthRatio;

            // To get from projective space to camera space we apply the
            // width/height ratios to find our normalized point at 1 unit
            // in front of the camera.  (1 is convenient, but has no other
            // special significance.) See note above about the construction
            // of w and h.
            Vector3D rayDirection = new Vector3D(np.X/w, np.Y/h, -1);

            // Apply the inverse of the view matrix to our rayDirection vector
            // to convert it from camera to world space.
            //
            // NOTE: Because our construction of the ray assumes that the
            //       viewMatrix translates the position to the origin we pass
            //       null for the Camera.Transform below and account for it
            //       later.

            Matrix3D viewMatrix = CreateViewMatrix(/* trasform = */ null, ref position, ref lookDirection, ref upDirection);
            Matrix3D invView = viewMatrix;
            invView.Invert();
            invView.MultiplyVector(ref rayDirection);

            // The we have the ray direction, now we need the origin.  The camera's
            // position would work except that we would intersect geometry between
            // the camera plane and the near plane so instead we must find the
            // point on the project plane where the ray (position, rayDirection)
            // intersect (Windows OS #1005064):
            //
            //                     | _.>       p = camera position
            //                rd  _+"          ld = camera look direction
            //                 .-" |ro         pp = projection plane
            //             _.-"    |           rd = ray direction
            //         p +"--------+--->       ro = desired ray origin on pp
            //                ld   |
            //                     pp
            //
            // Above we constructed the direction such that it's length projects to
            // 1 unit on the lookDirection vector.
            //
            //
            //                rd  _.>
            //                 .-"        rd = unnormalized rayDirection
            //             _.-"           ld = normalized lookDirection (length = 1)
            //           -"--------->
            //                 ld   
            //
            // So to find the desired rayOrigin on the projection plane we simply do:            
            Point3D rayOrigin = position + zn*rayDirection;
            rayDirection.Normalize();
            
            // Account for the Camera.Transform we ignored during ray construction above.
            if (transform != null && transform != Transform3D.Identity)
            {
                Matrix3D m = transform.Value;
                m.MultiplyPoint(ref rayOrigin);
                m.MultiplyVector(ref rayDirection);

                PrependInverseTransform(m, ref viewMatrix);
            }

            RayHitTestParameters rayParameters = new RayHitTestParameters(rayOrigin, rayDirection);

            //
            //  Compute HitTestProjectionMatrix
            //

            Matrix3D projectionMatrix = GetProjectionMatrix(aspectRatio, zn, zf);
            
            // The projectionMatrix takes camera-space 3D points into normalized clip
            // space.

            // The viewportMatrix will take normalized clip space into
            // viewport coordinates, with an additional 2D translation
            // to put the ray at the rayOrigin.
            Matrix3D viewportMatrix = new Matrix3D();
            viewportMatrix.TranslatePrepend(new Vector3D(-p.X, viewSize.Height - p.Y, 0));
            viewportMatrix.ScalePrepend(new Vector3D(viewSize.Width/2, -viewSize.Height/2, 1));
            viewportMatrix.TranslatePrepend(new Vector3D(1, 1, 0));
            
            // First, world-to-camera, then camera's projection, then normalized clip space to viewport.
            rayParameters.HitTestProjectionMatrix = 
                viewMatrix *
                projectionMatrix *
                viewportMatrix;

            // 
            // Perspective camera doesn't allow negative NearPlanes, so there's
            // not much point in adjusting the ray origin. Hence, the
            // distanceAdjustment remains 0.
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
