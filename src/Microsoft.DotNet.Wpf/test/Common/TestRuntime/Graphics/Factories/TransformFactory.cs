// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics.Factories
{
    /// <summary>
    /// This summary has not been prepared yet. NOSUMMARY - pantal07
    /// </summary>
    public class TransformFactory
    {
        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Transform3D MakeTransform(string transform)
        {
            if (transform == null)
            {
                return Transform3D.Identity;
            }

            string[] parsedTransform = transform.Split(' ');

            switch (parsedTransform[0])
            {
                case "Translate":
                    return new TranslateTransform3D(StringConverter.ToVector3D(parsedTransform[1]));

                case "Rotate":
                    return new RotateTransform3D(
                                new AxisAngleRotation3D(
                                    StringConverter.ToVector3D(parsedTransform[1]),
                                    StringConverter.ToDouble(parsedTransform[2])));

                case "RotateCenter":
                    return new RotateTransform3D(
                                new AxisAngleRotation3D(
                                    StringConverter.ToVector3D(parsedTransform[1]),
                                    StringConverter.ToDouble(parsedTransform[2])),
                                StringConverter.ToPoint3D(parsedTransform[3]));

                case "Scale":
                    return new ScaleTransform3D(StringConverter.ToVector3D(parsedTransform[1]));

                case "ScaleCenter":
                    return new ScaleTransform3D(
                                    StringConverter.ToVector3D(parsedTransform[1]),
                                    StringConverter.ToPoint3D(parsedTransform[2]));

                case "Matrix":
                    return new MatrixTransform3D(StringConverter.ToMatrix3D(parsedTransform[1]));

                case "ViewMatrix":
                    return new MatrixTransform3D(
                                    MatrixUtils.MakeViewMatrix(
                                            StringConverter.ToPoint3D(parsedTransform[1]),
                                            StringConverter.ToVector3D(parsedTransform[2]),
                                            StringConverter.ToVector3D(parsedTransform[3])));

                case "OrthographicProjection":
                    return new MatrixTransform3D(
                                    MatrixUtils.MakeOrthographicProjection(
                                            StringConverter.ToDouble(parsedTransform[1]),
                                            StringConverter.ToDouble(parsedTransform[2]),
                                            StringConverter.ToDouble(parsedTransform[3]),
                                            StringConverter.ToDouble(parsedTransform[4])));

                case "PerspectiveProjection":
                    return new MatrixTransform3D(
                                    MatrixUtils.MakePerspectiveProjection(
                                            StringConverter.ToDouble(parsedTransform[1]),
                                            StringConverter.ToDouble(parsedTransform[2]),
                                            StringConverter.ToDouble(parsedTransform[3]),
                                            StringConverter.ToDouble(parsedTransform[4])));

                case "Identity":
                    return Transform3D.Identity;

                case "Null":
                    return null;

                default:
                    throw new ArgumentException("Specified transform (" + transform + ") cannot be created");
            }
        }
    }
}
