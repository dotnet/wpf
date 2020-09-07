// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     The ProjectionCamera is an abstract base class from cameras
    ///     constructed from well-understand parameers such as Position,
    ///     LookAtPoint, and Up.
    /// </summary>
    public abstract partial class ProjectionCamera : Camera
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Prevent 3rd parties from extending this abstract base class.
        /// </summary>
        internal ProjectionCamera() 
        {
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
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal override Matrix3D GetViewMatrix()
        { 
            Point3D position = Position;
            Vector3D lookDirection = LookDirection;
            Vector3D upDirection = UpDirection;
            
            return CreateViewMatrix(Transform, ref position, ref lookDirection, ref upDirection);
        }

        // Transfrom that moves the world to a camera coordinate system
        // where the camera is at the origin looking down the negative z
        // axis and y is up.
        //
        // NOTE: We consider camera.Transform to be part of the view matrix.
        //
        internal static Matrix3D CreateViewMatrix(Transform3D transform, ref Point3D position, ref Vector3D lookDirection, ref Vector3D upDirection)
        {
            Vector3D zaxis = -lookDirection;
            zaxis.Normalize();

            Vector3D xaxis = Vector3D.CrossProduct(upDirection, zaxis);
            xaxis.Normalize();

            Vector3D yaxis = Vector3D.CrossProduct(zaxis, xaxis);

            Vector3D positionVec = (Vector3D) position;
            double cx = -Vector3D.DotProduct(xaxis, positionVec);
            double cy = -Vector3D.DotProduct(yaxis, positionVec);
            double cz = -Vector3D.DotProduct(zaxis, positionVec);
            
            Matrix3D viewMatrix = new Matrix3D(
                xaxis.X, yaxis.X, zaxis.X, 0,
                xaxis.Y, yaxis.Y, zaxis.Y, 0,
                xaxis.Z, yaxis.Z, zaxis.Z, 0,
                cx, cy, cz, 1);

            PrependInverseTransform(transform, ref viewMatrix);

            return viewMatrix;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
    }
}

