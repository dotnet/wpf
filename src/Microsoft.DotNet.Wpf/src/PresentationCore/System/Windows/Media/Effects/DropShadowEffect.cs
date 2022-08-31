// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace System.Windows.Media.Effects
{
    /// <summary>
    /// DropShadowEffect
    /// </summary>
    public partial class DropShadowEffect
    {
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public DropShadowEffect()
        {
        }

        #endregion

        /// <summary>
        /// Takes in content bounds, and returns the bounds of the rendered
        /// output of that content after the Effect is applied.
        /// </summary>
        internal override Rect GetRenderBounds(Rect contentBounds)
        {
            Point topLeft = new Point();
            Point bottomRight = new Point();

            double radius = BlurRadius;
            topLeft.X = contentBounds.TopLeft.X - radius;
            topLeft.Y = contentBounds.TopLeft.Y - radius;
            bottomRight.X = contentBounds.BottomRight.X + radius;
            bottomRight.Y = contentBounds.BottomRight.Y + radius;

            double depth = ShadowDepth;
            double direction = Math.PI/180 * Direction;
            double offsetX = depth * Math.Cos(direction);
            double offsetY = depth * Math.Sin(direction);

            // If the shadow is horizontally aligned or to the right of the original element...
            if (offsetX >= 0.0f)
            {
                bottomRight.X += offsetX;
            }
            // If the shadow is to the left of the original element...
            else
            {
                topLeft.X += offsetX;
            }

            // If the shadow is above the original element...
            if (offsetY >= 0.0f)
            {
                topLeft.Y -= offsetY;
            }
            // If the shadow is below the original element...
            else 
            {
                bottomRight.Y -= offsetY;
            }

            return new Rect(topLeft, bottomRight);
        }
    }
}
