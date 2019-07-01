// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Gets a value that indicates which part of the selection adorner intersects or surrounds the specified point.
    /// </summary>
    public class HitTestSelectAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas InkCanvas { get; set; }

        public override void Perform()
        {
            // calculate the smallest rectangle that contains the selected content
            StrokeCollection selectedStrokes = InkCanvas.GetSelectedStrokes();
            Rect rect = selectedStrokes.GetBounds();
            ReadOnlyCollection<UIElement> elements = InkCanvas.GetSelectedElements();
            foreach (UIElement element in elements)
            {
                //skip non-FrameworkElements
                FrameworkElement frameworkElement = element as FrameworkElement;
                if (frameworkElement != null)
                {
                    double left = InkCanvas.GetLeft(frameworkElement);
                    if (double.IsNaN(left))
                    {
                        double right = InkCanvas.GetRight(frameworkElement);
                        if (double.IsNaN(right))
                        {
                            left = 0d;
                        }
                        else
                        {
                            left = InkCanvas.ActualWidth - right - frameworkElement.ActualWidth;
                        }
                    }
                    double top = InkCanvas.GetTop(frameworkElement);
                    if (double.IsNaN(top))
                    {
                        double bottom = InkCanvas.GetBottom(frameworkElement);
                        if (double.IsNaN(bottom))
                        {
                            top = 0d;
                        }
                        else
                        {
                            top = InkCanvas.ActualHeight - bottom - frameworkElement.ActualHeight;
                        }
                    }
                    Rect frameworkElementRect = new Rect(left, top, frameworkElement.ActualWidth, frameworkElement.ActualHeight);
                    rect = Rect.Union(rect, frameworkElementRect);
                    rect.Inflate(30d, 30d);

                    double xStep = rect.Width / 10d;
                    double yStep = rect.Height / 10d;
                    for (double x = rect.Left; x < rect.Right; x += xStep)
                    {
                        for (double y = rect.Top; y < rect.Bottom; y += yStep)
                        {
                            InkCanvas.HitTestSelection(new Point(x, y));
                        }
                    }
                }
            }
        }
    }
}
