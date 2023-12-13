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
    ///     Encapsulates an orthagraphic projection camera.
    /// </summary>
    public partial class OrthographicCamera : ProjectionCamera
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        ///<summary />
        public OrthographicCamera() {}

        ///<summary />
        public OrthographicCamera(Point3D position, Vector3D lookDirection, Vector3D upDirection, double width)
        {
            Position = position;
            LookDirection = lookDirection;
            UpDirection = upDirection;
            Width = width;
        }

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

        internal Matrix3D GetProjectionMatrix(double aspectRatio, double zn, double zf)
        {
            double w = Width;
            double h = w/aspectRatio;

            double m22 = 1/(zn-zf);
            double m32 = zn*m22;

            return new Matrix3D(
                2/w,   0,    0,  0,
                0,   2/h,    0,  0,
                0,     0,  m22,  0,
                0,     0,  m32,  1);
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
            double zn = NearPlaneDistance;
            double zf = FarPlaneDistance;
            double width = Width;

            //
            //  Compute rayParameters
            //
            
            // Find the point on the projection plane in post-projective space where
            // the viewport maps to a 2x2 square from (-1,1)-(1,-1).
            Point np = M3DUtil.GetNormalizedPoint(p, viewSize);

            double aspectRatio = M3DUtil.GetAspectRatio(viewSize);
            double w = width;
            double h = w/aspectRatio;

            // Direction is always perpendicular to the viewing surface.
            Vector3D direction = new Vector3D(0, 0, -1);

            // Apply the inverse of the view matrix to our ray.
            Matrix3D viewMatrix = CreateViewMatrix(Transform, ref position, ref lookDirection, ref upDirection);
            Matrix3D invView = viewMatrix;
            invView.Invert();

            // We construct our ray such that the origin resides on the near
            // plane.  If our near plane is too far from our the bounding box
            // of our scene then the results will be inaccurate.  (e.g.,
            // OrthographicCameras permit negative near planes, so the near
            // plane could be at -Inf.)
            // 
            // However, it is permissable to move the near plane nearer to
            // the scene bounds without changing what the ray intersects.
            // If the near plane is sufficiently far from the scene bounds
            // we make this adjustment below to increase precision.
            
            Rect3D transformedBoundingBox =
                M3DUtil.ComputeTransformedAxisAlignedBoundingBox(
                    ref boundingRect,
                    ref viewMatrix);

            // DANGER:  The NearPlaneDistance property is specified as a
            //          distance from the camera position along the
            //          LookDirection with (Near < Far).  
            //
            //          However, when we transform our scene bounds so that
            //          the camera is aligned with the negative Z-axis the
            //          relationship inverts (Near > Far) as illustrated
            //          below:
            //
            //            NearPlane    Y                      FarPlane
            //                |        ^                          |
            //                |        |                          |
            //                |        | (rect.Z + rect.SizeZ)    |
            //                |        |           o____          |
            //                |        |           |    |         |
            //                |        |           |    |         |
            //                |        |            ____o         |
            //                |        |             (rect.Z)     |
            //                |     Camera ->                     |
            //          +Z  <----------+----------------------------> -Z
            //                |        0                          |
            //
            //          It is surprising, but its the "far" side of the
            //          transformed scene bounds that determines the near
            //          plane distance.

            double zn2 = - AddEpsilon(transformedBoundingBox.Z+transformedBoundingBox.SizeZ);

            if (zn2 > zn)
            {
                //
                // Our near plane is far from our children. Construct a new
                // near plane that's closer. Note that this will modify our
                // distance computations, so we have to be sure to adjust our
                // distances appropriately.
                //
                distanceAdjustment = zn2 - zn;

                zn = zn2;
            }
            else
            {
                //
                // Our near plane is either close to or in front of our
                // children, so let's keep it -- no distance adjustment needed.
                //
                distanceAdjustment = 0.0;
            }

            // Our origin is the point normalized to the front of our viewing volume.
            // To find our origin's x/y we just need to scale the normalize point by our
            // width/height.  In camera space we are looking down the negative Z axis
            // so we just set Z to be -zn which puts us on the projection plane
            // (Windows OS #1005064).
            Point3D origin = new Point3D(np.X*(w/2), np.Y*(h/2), -zn);

            invView.MultiplyPoint(ref origin);
            invView.MultiplyVector(ref direction);

            RayHitTestParameters rayParameters = new RayHitTestParameters(origin, direction);

            //
            //  Compute HitTestProjectionMatrix
            //

            Matrix3D projectionMatrix = GetProjectionMatrix(aspectRatio, zn, zf);

            // The projectionMatrix takes camera-space 3D points into normalized clip
            // space.

            // The viewportMatrix will take normalized clip space into
            // viewport coordinates, with an additional 2D translation
            // to put the ray at the origin.
            Matrix3D viewportMatrix = new Matrix3D();
            viewportMatrix.TranslatePrepend(new Vector3D(-p.X, viewSize.Height-p.Y, 0));
            viewportMatrix.ScalePrepend(new Vector3D(viewSize.Width/2, -viewSize.Height/2, 1));
            viewportMatrix.TranslatePrepend(new Vector3D(1, 1, 0));
            
            // First, world-to-camera, then camera's projection, then normalized clip space to viewport.
            rayParameters.HitTestProjectionMatrix = 
                viewMatrix *
                projectionMatrix *
                viewportMatrix;
                
            return rayParameters;
}

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        private double AddEpsilon(double x)
        {
            //
            // x is either close to 0 or not. If it's close to 0, then 1.0 is
            // sufficiently large to act as an epsilon.  If it's not, then
            // 0.1*Math.Abs(x) sufficiently large.
            //
            return x + 0.1*Math.Abs(x) + 1.0;
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
    }
}
