// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     The Camera is the mechanism by which a 3D model is projected onto
    ///     a 2D visual.  The Camera itself is an abstract base class.
    /// </summary>
    public abstract partial class Camera : Animatable
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        // Prevent 3rd parties from extending this abstract base class.
        internal Camera() {}

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

        // Creates a ray by projecting the given point on the viewport into the scene.
        // Used for bridging 2D -> 3D hit testing.
        //
        // The latter two parameters in this method are used to deal with the
        // case where the camera's near plane is far away from the viewport
        // contents. In these cases, we can sometimes construct a new, closer,
        // near plane and start the ray on that plane. To do this, we need an
        // axis-aligned bounding box of the viewport's contents (boundingRect).
        // We also need to return the distance between the original an new near
        // planes (distanceAdjustment), so we can correct the hit-test
        // distances before handing them back to the user.

        internal abstract RayHitTestParameters RayFromViewportPoint(Point point, Size viewSize, Rect3D boundingRect, out double distanceAdjustment);
        internal abstract Matrix3D GetViewMatrix();
        internal abstract Matrix3D GetProjectionMatrix(double aspectRatio);

        internal static void PrependInverseTransform(Transform3D transform, ref Matrix3D viewMatrix)
        {
            if (transform != null && transform != Transform3D.Identity)
            {
                PrependInverseTransform(transform.Value, ref viewMatrix);
            }
        }

        // Helper method to prepend the inverse of Camera.Transform to the
        // the given viewMatrix.  This is used by the various GetViewMatrix()
        // and RayFromViewportPoint implementations.
        // 
        // Transforming the camera is equivalent to applying the inverse
        // transform to the scene.  We invert the transform and prepend it to
        // the result of viewMatrix:
        //
        //                                  -1
        //     viewMatrix = Camera.Transform   x viewMatrix
        //
        // If the matrix is non-invertable we set the viewMatrix to NaNs which
        // will result in nothing being rendered.  This is the correct behavior
        // since the near and far planes will have collapsed onto each other.
        internal static void PrependInverseTransform(Matrix3D matrix, ref Matrix3D viewMatrix)
        {
            if (!matrix.InvertCore())
            {
                // If the matrix is non-invertable we return a NaN matrix.
                viewMatrix = new Matrix3D(
                    double.NaN, double.NaN, double.NaN, double.NaN,
                    double.NaN, double.NaN, double.NaN, double.NaN,
                    double.NaN, double.NaN, double.NaN, double.NaN,
                    double.NaN, double.NaN, double.NaN, double.NaN);
            }
            else
            {
                viewMatrix.Prepend(matrix);
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        #endregion Private Fields
    }
}
