// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Utilities
{
    /// <summary>
    /// A float array wrapper for writing into a writeable bitmap
    /// </summary>
    class FloatWriteableBitmapWriter : WriteableBitmapWriter
    {
        private float[] pixels;

        public FloatWriteableBitmapWriter(int height, int strideInBytes, PixelFormat pixelFormat)
        {
            this.height = height;
            this.strideInBytes = strideInBytes;
            this.pixelFormat = pixelFormat;

            int numFloats = (height * strideInBytes) / sizeof(float);
            pixels = new float[numFloats];
        }

        public override void SetWriteableBitmapPixels(WriteableBitmap wbmp, Int32Rect rect, Color color)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                switch (i%4)
                {
                    case 0:
                        pixels[i] = color.ScR;
                        break;
                    case 1:
                        pixels[i] = color.ScG;
                        break;
                    case 2:
                        pixels[i] = color.ScB;
                        break;
                    case 3:
                        pixels[i] = color.ScA;
                        break;
                    default:
                        break;
                }
            }

            wbmp.WritePixels(rect, pixels, strideInBytes, rect.X, rect.Y);
        }

        public override void SetWriteableBitmapPixels(WriteableBitmap wbmp, Int32Rect rect, DeterministicRandom random)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                // scRGB ~= (sRGB / 255)^2.2
                pixels[i] = (float)Math.Pow(random.NextDouble(), 2.2);
            }

            wbmp.WritePixels(rect, pixels, strideInBytes, rect.X, rect.Y);
        }
    }
}
