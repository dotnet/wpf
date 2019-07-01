// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics.Factories
{
    /// <summary>
    /// Make cameras that are easy to verify
    /// </summary>
    public class CameraFactory
    {
        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Camera MakeCamera(string camera)
        {
            switch (camera)
            {
                case "OrthographicDefault": return OrthographicDefault;
                case "OrthographicDefaultWidth4": return OrthographicDefaultWidth4;
                case "PerspectiveDefault": return PerspectiveDefault;
                case "PerspectiveRotate45": return PerspectiveRotate45;
                case "MatrixDefaultOrthographic": return MatrixDefaultOrthographic;
                case "MatrixDefaultPerspective": return MatrixDefaultPerspective;
                case "MatrixNonInvertible": return MatrixNonInvertible;
                case "AutoOrthographic": return AutoOrthographic;
                case "AutoPerspective": return AutoPerspective;
            }
            throw new ArgumentException("Specified camera (" + camera + ") cannot be created");
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static OrthographicCamera OrthographicDefault
        {
            get
            {
                OrthographicCamera oc = new OrthographicCamera(
                                                new Point3D(0, 0, 5),     // Position
                                                new Vector3D(0, 0, -1),   // LookDirection
                                                new Vector3D(0, 1, 0),    // UpDirection
                                                2                           // Width: -1 and 1 (world coords) are the edges of the viewport
                                                );
                oc.NearPlaneDistance = 1;
                oc.FarPlaneDistance = 20;

                return oc;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static OrthographicCamera OrthographicDefaultWidth4
        {
            get
            {
                OrthographicCamera oc = new OrthographicCamera(
                                                new Point3D(0, 0, 5),     // Position
                                                new Vector3D(0, 0, -1),   // LookDirection
                                                new Vector3D(0, 1, 0),    // UpDirection
                                                4                           // Width: -2 and 2 (world coords) are the edges of the viewport
                                                );
                oc.NearPlaneDistance = 1;
                oc.FarPlaneDistance = 20;

                return oc;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static PerspectiveCamera PerspectiveDefault
        {
            get
            {
                PerspectiveCamera pc = new PerspectiveCamera(
                                                new Point3D(0, 0, 5),     // Position
                                                new Vector3D(0, 0, -1),   // LookDirection
                                                new Vector3D(0, 1, 0),    // UpDirection
                                                45                          // FieldOfView
                                                );
                pc.NearPlaneDistance = 1;
                pc.FarPlaneDistance = 20;

                return pc;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static PerspectiveCamera PerspectiveRotate45
        {
            get
            {
                PerspectiveCamera pc = new PerspectiveCamera();
                pc.Position = new Point3D(-3, 0, 3);
                pc.LookDirection = new Vector3D(1, 0, -1);
                pc.UpDirection = new Vector3D(0, 1, 0);
                pc.FieldOfView = 45;
                pc.NearPlaneDistance = 1;
                pc.FarPlaneDistance = 20;

                return pc;
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MatrixCamera MatrixDefaultOrthographic
        {
            get
            {
                Matrix3D viewMatrix = MatrixUtils.MakeViewMatrix(new Point3D(0, 0, 5), new Vector3D(0, 0, -1), Const.yAxis);
                Matrix3D projMatrix = MatrixUtils.MakeOrthographicProjection(1, 20, 2, 2);
                return new MatrixCamera(viewMatrix, projMatrix);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MatrixCamera MatrixDefaultPerspective
        {
            get
            {
                Matrix3D viewMatrix = MatrixUtils.MakeViewMatrix(new Point3D(0, 0, 5), new Vector3D(0, 0, -1), Const.yAxis);
                Matrix3D projMatrix = MatrixUtils.MakePerspectiveProjection(1, 20, 45, 45);
                return new MatrixCamera(viewMatrix, projMatrix);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static MatrixCamera MatrixNonInvertible
        {
            get
            {
                // Looking straight down -z axis and flattening the scene in Z.
                Matrix3D viewMatrix = new Matrix3D(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, -5, 1);
                Matrix3D projMatrix = new Matrix3D(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0);
                return new MatrixCamera(viewMatrix, projMatrix);
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static OrthographicCamera AutoOrthographic
        {
            get
            {
                return new OrthographicCamera(
                                new Point3D(0, 0, 5),     // Position
                                new Vector3D(0, 0, -1),   // LookDirection
                                new Vector3D(0, 1, 0),    // UpDirection
                                2                           // Width: -1 and 1 (world coords) are the edges of the viewport
                                );
            }
        }

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static PerspectiveCamera AutoPerspective
        {
            get
            {
                return new PerspectiveCamera(
                                new Point3D(0, 0, 5),     // Position
                                new Vector3D(0, 0, -1),   // LookDirection
                                new Vector3D(0, 1, 0),    // UpDirection
                                45                          // FieldOfView
                                );
            }
        }
    }
}
