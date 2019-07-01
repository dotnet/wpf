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
    /// Nearest neighbour texturing lookup
    /// </summary>
    internal class NearestNeighbourTextureFilter : TextureFilter
    {
        public NearestNeighbourTextureFilter(BitmapSource texture)
            : base(texture)
        {
        }

        public override Color FilteredTextureLookup(Point uv)
        {
            // Scale x and y to size of texture
            double x = uv.X * width;
            double y = uv.Y * height;

            return GetColor((int)Math.Round(x), (int)Math.Round(y));
        }
    };
}
