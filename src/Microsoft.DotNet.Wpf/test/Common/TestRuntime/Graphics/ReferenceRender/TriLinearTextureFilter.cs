// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics.ReferenceRender
{
    /// <summary>
    ///  Tri-Linear texturing lookup
    /// </summary>
    internal class TrilinearTextureFilter : BilinearTextureFilter
    {
        // nested class extends Bilinear to store more information about the image size
        private class MipMapLevel : BilinearTextureFilter
        {
            public MipMapLevel(BitmapSource texture, double uvPixelSize)
                : base(texture)
            {
                this.uvPixelSize = uvPixelSize;
            }

            double uvPixelSize;
            public double PixelSize
            {
                get { return uvPixelSize; }
            }
        }

        public TrilinearTextureFilter(BitmapSource texture)
            : base(texture)
        {
            ImageBrush brush = new ImageBrush(texture);

            // Create MipMap levels - force power of two texture size
            int currentHeight = MathEx.RoundUpToNextPowerOfTwo(height);
            int currentWidth = MathEx.RoundUpToNextPowerOfTwo(width);

            levels = new List<MipMapLevel>();
            while (currentHeight > 0 && currentWidth > 0)
            {
                // cannonical uv length of 1 pixel
                double pixelLength = 1.0 / Math.Max(currentHeight, currentWidth);

                BitmapSource nextLevelTexture = TextureGenerator.RenderBrushToImageData(
                        brush, currentWidth, currentHeight);

                // create mip map level
                MipMapLevel level = new MipMapLevel(nextLevelTexture, pixelLength);
                level.HasErrorEstimation = true;
                levels.Add(level);

                // reuse this texture brush so that filtering is recursive
                brush = new ImageBrush(nextLevelTexture);

                // reduce size for next level
                currentHeight /= 2;
                currentWidth /= 2;
            }
            mipmapFactor = 0;
        }

        public override Color FilteredTextureLookup(Point uv)
        {
            int lowerIndex = 0;
            int upperIndex = 0;

            // look for appropriate levels
            while (upperIndex < levels.Count && mipmapFactor > levels[upperIndex].PixelSize)
            {
                upperIndex++;
            }
            upperIndex = Math.Min(upperIndex, levels.Count - 1);
            lowerIndex = Math.Max(upperIndex - 1, 0);

            // Now perform texture lookup on the mipmap levels
            if (lowerIndex == upperIndex)
            {
                // no need to blend, clear case of too near (pure bilinear mag)
                // or too far (1 pixel last level)
                return levels[lowerIndex].FilteredTextureLookup(uv);
            }
            else
            {
                // Find the colors of the adjacent levels
                Color lowerLevel = levels[lowerIndex].FilteredTextureLookup(uv); ;
                Color upperLevel = levels[upperIndex].FilteredTextureLookup(uv); ;

                // Find a suitable blend factor
                double lowerPixelSize = levels[lowerIndex].PixelSize;
                double upperPixelSize = levels[upperIndex].PixelSize;
                // upper pixel is 2x larger than lower pixel
                double blendFactor = (mipmapFactor / lowerPixelSize) - 1;

                // return the mix
                return ColorOperations.Blend(upperLevel, lowerLevel, blendFactor);
            }
        }

        public override Color FilteredErrorLookup(Point uvLow, Point uvHigh, Color computedColor)
        {
            int lowerIndex = 0;
            int upperIndex = 0;

            Color computedError;

            // look for appropriate levels
            while (upperIndex < levels.Count && mipmapFactor > levels[upperIndex].PixelSize)
            {
                upperIndex++;
            }
            upperIndex = Math.Min(upperIndex, levels.Count - 1);
            lowerIndex = Math.Max(upperIndex - 1, 0);

            // Now perform error lookup on the mipmap levels
            if (lowerIndex == upperIndex)
            {
                // no need to blend, clear case of too near (pure bilinear mag)
                // or too far (1 pixel last level)
                computedError = levels[lowerIndex].FilteredErrorLookup(uvLow, uvHigh, computedColor);
            }
            else
            {
                // Find the colors of the adjacent levels
                Color lowerLevel = levels[lowerIndex].FilteredErrorLookup(uvLow, uvHigh, computedColor);
                Color upperLevel = levels[upperIndex].FilteredErrorLookup(uvLow, uvHigh, computedColor);

                // Find a suitable blend factor
                double lowerPixelSize = levels[lowerIndex].PixelSize;
                double upperPixelSize = levels[upperIndex].PixelSize;
                // upper pixel is 2x larger than lower pixel
                double blendFactor = (mipmapFactor / lowerPixelSize) - 1;

                // return the mix
                computedError = ColorOperations.Blend(upperLevel, lowerLevel, blendFactor);
            }

            // Compare against bilinear error
            Color baseError = base.FilteredErrorLookup(uvLow, uvHigh, computedColor);
            computedError = ColorOperations.Max(computedError, baseError);

            return computedError;
        }

        public double MipMapFactor
        {
            get { return mipmapFactor; }
            set { mipmapFactor = value; }
        }

        private List<MipMapLevel> levels;
        private double mipmapFactor;
    }
}
