// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Utilities
{
    /// <summary>
    /// A byte array wrapper for writing into a writeable bitmap
    /// </summary>
    class ByteWriteableBitmapWriter : WriteableBitmapWriter
    {
        private byte[] pixels;

        public ByteWriteableBitmapWriter(int height, int strideInBytes, PixelFormat pixelFormat)
        {
            this.height = height;
            this.strideInBytes = strideInBytes;
            this.pixelFormat = pixelFormat;
            pixels = new byte[height * strideInBytes];
        }

        public override void SetWriteableBitmapPixels(WriteableBitmap wbmp, Int32Rect rect, Color color)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                switch (i % 4)
                {
                    case 0:
                        pixels[i] = color.R;
                        break;
                    case 1:
                        pixels[i] = color.G;
                        break;
                    case 2:
                        pixels[i] = color.B;
                        break;
                    case 3:
                        pixels[i] = color.A;
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
                pixels[i] = unchecked((byte)random.Next());
            }

            wbmp.WritePixels(rect, pixels, strideInBytes, rect.X, rect.Y);
        }
    }
}
