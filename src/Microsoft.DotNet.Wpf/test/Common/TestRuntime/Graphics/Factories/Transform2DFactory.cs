// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;

namespace Microsoft.Test.Graphics.Factories
{
    /// <summary>
    /// 2D Transform factory
    /// </summary>
    public class Transform2DFactory
    {

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Transform MakeTransform2D(string transform)
        {
            string[] parsedTransform = transform.Split(' ');

            switch (parsedTransform[0])
            {
                case "Translate":
                    return new TranslateTransform(
                        StringConverter.ToDouble(parsedTransform[1]),
                        StringConverter.ToDouble(parsedTransform[2]));

                case "Rotate":
                    return new RotateTransform(
                        StringConverter.ToDouble(parsedTransform[1]),
                        StringConverter.ToPoint(parsedTransform[2]).X,
                        StringConverter.ToPoint(parsedTransform[2]).Y);

                case "Scale":
                    return new ScaleTransform(
                        StringConverter.ToDouble(parsedTransform[1]),
                        StringConverter.ToDouble(parsedTransform[2]),
                        StringConverter.ToPoint(parsedTransform[3]).X,
                        StringConverter.ToPoint(parsedTransform[3]).Y);

                case "Identity":
                    return new MatrixTransform(1.0, 0.0, 0.0, 1.0, 0.0, 0.0);

                case "Null":
                    return null;

                // TO DO:  Add more Transform types here

                default:
                    throw new ArgumentException("Specified transform (" + transform + ") cannot be created");
            }

        }
    }
}