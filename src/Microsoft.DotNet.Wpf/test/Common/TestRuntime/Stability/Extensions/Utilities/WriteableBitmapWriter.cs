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
    /// The base class to abstract what type of array buffer is being used when writing to a writeable bitmap.
    /// </summary>
    abstract class WriteableBitmapWriter
    {
        protected int height;
        protected int strideInBytes;
        protected PixelFormat pixelFormat;

        public abstract void SetWriteableBitmapPixels(WriteableBitmap wbmp, Int32Rect rect, Color color);

        public abstract void SetWriteableBitmapPixels(WriteableBitmap wbmp, Int32Rect rect, DeterministicRandom random);

        public static WriteableBitmapWriter CreateWriter(int height, int strideInBytes, PixelFormat format)
        {
            WriteableBitmapWriter writer = null;

            if (PixelFormatHelper.IsScRGB(format))
            {
                writer = new FloatWriteableBitmapWriter(height, strideInBytes, format);
            }
            else
            {
                writer = new ByteWriteableBitmapWriter(height, strideInBytes, format);
            }

            return writer;
        }
    }
}
