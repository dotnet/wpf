// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;

namespace Microsoft.Test.Graphics
{    
    /// <summary>
    /// Helper methods to scale on different Dpi. 
    /// </summary>
    public class DpiScalingHelper
    {
        /// <summary>
        /// Change the size of a Window and its content, such 
        /// that they would be rendered the same way as under 
        /// certain Dpi. After call this function, the content 
        /// of the window can using same master images for 
        /// different Dpis. 
        /// </summary>
        /// <param name="window">Window to change</param>
        /// <param name="targetDpi">Dpi this function is targeting</param>
        public static void ScaleWindowToFixedDpi(Window window, double targetDpiX, double targetDpiY)
        {
            double scaleX = targetDpiX / Microsoft.Test.Display.Monitor.Dpi.x;
            double scaleY = targetDpiY / Microsoft.Test.Display.Monitor.Dpi.y;

            //Do nothing if the system Dpi is the same as the target Dpi. 
            if (scaleX == 1.0 && scaleY == 1.0)
            {
                return;
            }

            //Change the window size
            window.Width = window.Width * scaleX;
            window.Height = window.Height * scaleY;

            FrameworkElement content = window.Content as FrameworkElement;

            if (content == null)
            {
                throw new InvalidOperationException("Content of window cannot be null.");
            }

            //Apply a ScaleTransform so the content would be rendered
            //as if with targetDpi. 
            Matrix A = content.LayoutTransform.Value;
            A.Scale(scaleX, scaleY);
            content.LayoutTransform = new MatrixTransform(A);
        }

    }
}
