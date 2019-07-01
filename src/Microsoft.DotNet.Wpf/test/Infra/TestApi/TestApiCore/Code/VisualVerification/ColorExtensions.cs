// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;

namespace Microsoft.Test.VisualVerification
{
    /// <summary>
    /// Container for internal Extension methods on the Color type.
    /// </summary>
    internal static class ColorExtensions
    {
        #region Internal Static Members
        /// <summary>
        /// Compares colors by producing an absolute valued Color Difference object
        /// </summary>
        /// <param name="color1">The first color</param>
        /// <param name="color2">The second colar</param>
        /// <returns>The Color Difference of the two colors</returns>
        internal static ColorDifference Compare(this Color color1, Color color2)
        {
            ColorDifference diff = new ColorDifference();
            diff.A = (byte)Math.Abs(color1.A - color2.A);
            diff.R = (byte)Math.Abs(color1.R - color2.R);
            diff.G = (byte)Math.Abs(color1.G - color2.G);
            diff.B = (byte)Math.Abs(color1.B - color2.B);
            return diff;
        }

        /// <summary>
        /// Color differencing helper for snapshot comparisons.
        /// </summary>
        /// <param name="color1">The first color</param>
        /// <param name="color2">The second color</param>
        /// <param name="subtractAlpha">If set to false, the Alpha channel is overridden to full opacity, rather than the difference. 
        /// This is important for visualization, especially if both colors are fully opaque, as the difference produces a fully transparent difference.</param>
        /// <returns></returns>
        internal static Color Subtract(this Color color1, Color color2, bool subtractAlpha)
        {
            ColorDifference diff = Compare(color1, color2);
            if (!subtractAlpha)
            {
                diff.A = 255;
            }
            return Color.FromArgb(diff.A, diff.R, diff.G, diff.B);
        }

        /// <summary>
        /// Performs Bitwise OR of Color bits.
        /// </summary>
        /// <param name="color1">The first color.</param>
        /// <param name="color2">The second color.</param>
        /// <returns></returns>
        internal static Color Or(this Color color1, Color color2)
        {
            ColorDifference orValue = new ColorDifference();
            orValue.A = (byte)(color1.A | color2.A);
            orValue.R = (byte)(color1.R | color2.R);
            orValue.G = (byte)(color1.G | color2.G);
            orValue.B = (byte)(color1.B | color2.B);
            return Color.FromArgb(orValue.A, orValue.R, orValue.G, orValue.B);
        }

        /// <summary>
        /// Performs Bitwise AND of Color bits.
        /// </summary>
        /// <param name="color1">The first color.</param>
        /// <param name="color2">The second color.</param>
        /// <returns></returns>
        internal static Color And(this Color color1, Color color2)
        {
            ColorDifference andValue = new ColorDifference();
            andValue.A = (byte)(color1.A & color2.A);
            andValue.R = (byte)(color1.R & color2.R);
            andValue.G = (byte)(color1.G & color2.G);
            andValue.B = (byte)(color1.B & color2.B);
            return Color.FromArgb(andValue.A, andValue.R, andValue.G, andValue.B);
        }
        #endregion
    }
}
