// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;


namespace Microsoft.Test.Graphics.Factories
{
    /// <summary>
    /// This summary has not been prepared yet. NOSUMMARY - pantal07
    /// </summary>
    public class DrawingFactory
    {

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Drawing MakeDrawing(string drawing)
        {
            string[] parsedDrawing = drawing.Split(' ');

            switch (parsedDrawing[0])
            {
                case "SimpleGeometryDrawing":
                    return new GeometryDrawing(
                            Brushes.Black,
                            new Pen(Brushes.Yellow, 3.0),
                            new RectangleGeometry(new Rect(10, 10, 50, 102))
                            );

                // TO DO:  Create for general Drawing objects.

                default:
                    throw new ArgumentException("Specified Drawing (" + drawing + ") cannot be created");

            }
        }
    }
}