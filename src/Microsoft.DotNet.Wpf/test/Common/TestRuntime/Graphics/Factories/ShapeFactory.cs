// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Microsoft.Test.Graphics.Factories
{   
    /// <summary>
    /// Shape Factory for creating 2D Shape objects
    /// </summary>
    public class ShapeFactory
    {

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Shape MakeShape(string shape)
        {
            string[] parsedShape = shape.Split(' ');

            switch (parsedShape[0])
            {
                case "SimpleRectangle":
                    System.Windows.Shapes.Rectangle rectangle = new System.Windows.Shapes.Rectangle();
                    rectangle.Width = 100;
                    rectangle.Height = 100;
                    rectangle.Stroke = Brushes.Black.Clone();
                    rectangle.StrokeThickness = 3;
                    rectangle.Fill = Brushes.Red.Clone();
                    rectangle.Stretch = Stretch.UniformToFill;
                    return rectangle;

                case "SimpleEllipse":
                    System.Windows.Shapes.Ellipse ellipse = new System.Windows.Shapes.Ellipse();
                    ellipse.Width = 100;
                    ellipse.Height = 60;
                    ellipse.Stroke = Brushes.Black.Clone();
                    ellipse.StrokeThickness = 3;
                    ellipse.Fill = Brushes.Green.Clone();
                    ellipse.Stretch = Stretch.Fill;
                    return ellipse;

                case "HorizontalLine":
                    System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                    line.Width = 100;
                    line.Height = 100;
                    line.X1 = 90;
                    line.Y1 = 80;
                    line.X2 = 0;
                    line.Y2 = 80;
                    line.Stroke = Brushes.Blue.Clone();
                    line.StrokeThickness = 5;
                    line.Stretch = Stretch.Uniform;
                    return line;

                // TO DO:  Create more shape objects.

                default:
                    throw new ArgumentException("Specified shape (" + parsedShape[0] + ") cannot be created");

            }
        }
    }
}
