// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Removes the ink that is within the bounds of the specified area.
    /// </summary>
    public class EraseInkAction : SimpleDiscoverableAction
    {
        public int PointNum { get; set; }

        public InkCanvas InkCanvas { get; set; }

        public double RandomWidth { get; set; }

        public double RandomHeight { get; set; }

        public override void Perform()
        {
            // Create a point array which length is larger than zero.
            Point[] points = new Point[PointNum % 100 + 1];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Point(InkCanvas.ActualWidth * RandomWidth, InkCanvas.ActualHeight * RandomHeight);
            }

            InkCanvas.Strokes.Erase(points);
        }
    }
}
