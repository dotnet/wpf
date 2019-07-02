// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification
{
    #region Namespaces.

    using System;
    using System.Drawing;
    using System.Drawing.Imaging;

    #endregion Namespaces.

    /// <summary>
    /// This class provides utility methods to work with colors.
    /// </summary>
    public class ColorUtils
    {
        /// <summary>
        /// Calculates the properly-weighted sum of red, green and blue
        /// linear-light components for contemporary video cameras.
        /// </summary>
        /// <param name="red">Red component.</param>
        /// <param name="green">Green component.</param>
        /// <param name="blue">Blue component.</param>
        /// <returns>Luminance value for color.</returns>
        public static float GetLuminance(int red, int green, int blue)
        {
            return 
                (float)red * 0.2126f +
                (float)green * 0.7152f +
                (float)blue * 0.0722f;
        }

        /// <summary>
        /// Calculates the properly-weighted sum of red, green and blue
        /// linear-light components for contemporary video cameras.
        /// </summary>
        /// <param name="red">Red component.</param>
        /// <param name="green">Green component.</param>
        /// <param name="blue">Blue component.</param>
        /// <returns>Luminance value for color.</returns>
        /// <remarks>As a piece of anecdotal information, the coefficients
        /// currently used as .2126, .7152 and .0722 for red, green and
        /// blue, respectively. In 1953, for NTSC television, these coefficients
        /// were .299, .587 and .114. These are still appropriate for computing
        /// video luma.</remarks>
        public static float GetLuminance(float red, float green, float blue)
        {
            return red * 0.2126f + green * 0.7152f + blue * 0.0722f;
        }

        /// <summary>
        /// Computes the perceptual (non-linear) response to luminance.
        /// </summary>
        /// <param name="luminance">Luminance value, normalized to 1 for
        /// reference white.</param>
        /// <returns>The lightness value for the given luminance.</returns>
        public static float GetLightness(float luminance)
        {
            return 116 * (float) Math.Pow(luminance, 0.3333);
        }
/*
        /// <summary>
        /// Gets the square of the distance in linear-light space of two 
        /// colors.
        /// </summary>        
        /// <remarks>This method is used to speed up comparisons with
        /// a distance threshold. Instead of calculating the root for every
        /// distance value, this step can be avoided by squaring the
        /// threshold.</remarks>
        /// <param name="red">Red component of first color.</param>
        /// <param name="green">Green component of first color.</param>
        /// <param name="blue">Blue component of first color.</param>
        /// <param name="red2">Red component of second color.</param>
        /// <param name="green2">Green component of second color.</param>
        /// <param name="blue2">Blue component of second color.</param>
        public static float GetSquareLinearDistance(
            int red, int green, int blue,
            int red2, int green2, int blue2)
        {
            int deltaR = red2-red;
            int deltaG = green2-green;
            int deltaB = blue2-blue;
            return deltaR*deltaR + deltaG*deltaG + deltaB*deltaB;
        }

        internal unsafe static float GetSquareLinearDistance(
            PixelData* pixel, PixelData* otherPixel)
        {
            float deltaR = pixel->red - otherPixel->red;
            float deltaG = pixel->green - otherPixel->green;
            float deltaB = pixel->blue - otherPixel->blue;
            deltaR *= deltaR; deltaG *= deltaG; deltaB *= deltaB;
            return deltaR + deltaG + deltaB;
        }
*/
    }
}
