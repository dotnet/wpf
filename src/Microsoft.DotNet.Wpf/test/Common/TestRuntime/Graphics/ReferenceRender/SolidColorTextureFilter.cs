// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Microsoft.Test.Graphics.ReferenceRender
{
    /// <summary>
    ///  Texturing lookup for solid color
    /// </summary>
    internal class SolidColorTextureFilter : TextureFilter
    {
        public SolidColorTextureFilter(Color color)
            : base()
        {
            this.color = color;
            // this type of filtering cannot have any error
            HasErrorEstimation = false;
        }

        public SolidColorTextureFilter(BitmapSource texture)
            : base(texture)
        {
            // texture is a 1x1 Solid Color of the first pixel in the image
            color = Color.FromArgb(
                pixels[3],  // A
                pixels[2],  // R
                pixels[1],  // G
                pixels[0]); // B

            // this type of filtering cannot have any error
            HasErrorEstimation = false;
        }

        public override Color FilteredTextureLookup(Point uv)
        {
            // ignore everything and return the cached color
            return color;
        }

        public override Color FilteredErrorLookup(Point uvLow, Point uvHigh, Color computedColor)
        {
            return Colors.Transparent;
        }

        private Color color;
    };
}
