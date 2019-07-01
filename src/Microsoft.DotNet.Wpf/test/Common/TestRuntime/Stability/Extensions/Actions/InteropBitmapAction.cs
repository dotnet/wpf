// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Test.Stability.Extensions.Factories;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    internal class InteropBitmapAction : DiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Image TargetImage { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory)]
        public Int32Rect DrawRect { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory)]
        public Color DrawColor { get; set; }

        public override bool CanPerform()
        {
            return TargetImage != null && (TargetImage.Source is InteropBitmap) && !DrawRect.IsEmpty;
        }

        public override void Perform()
        {
            InteropBitmap bitmap = (InteropBitmap)TargetImage.Source;

            // clamp to valid ranges
            DrawRect = Clamp(bitmap, DrawRect);
            SafeFileHandle sectionHandle = (SafeFileHandle)bitmap.GetValue(InteropBitmapFactory.HandleProperty);

            if (sectionHandle == null || sectionHandle.IsInvalid)
            {
                throw new InvalidOperationException("Handle property not set on the InteropBitmap");
            }

            int bytesPerPixel = (int)((bitmap.Format.BitsPerPixel + 7.0) / 8.0);
            uint bufferSizeInBytes = (uint)(bitmap.PixelWidth * bitmap.PixelHeight * bytesPerPixel);

            unsafe
            {
                IntPtr viewHandle = InteropBitmapFactory.MapViewOfFile(sectionHandle.DangerousGetHandle(), InteropBitmapFactory.FILE_MAP_ALL_ACCESS, 0, 0, bufferSizeInBytes);

                byte* pixels = (byte*)viewHandle.ToPointer();

                DrawToBitmap(pixels, bitmap);
                bitmap.Invalidate();

                InteropBitmapFactory.UnmapViewOfFile(viewHandle);
            }
        }

        private unsafe void DrawToBitmap(byte* pixels, BitmapSource bitmap)
        {
            int bytesPerPixel = (int)((bitmap.Format.BitsPerPixel + 7.0) / 8.0);
            int stride = (int)(bytesPerPixel * bitmap.PixelWidth);
            byte[] color = new byte[] { DrawColor.R, DrawColor.G, DrawColor.B, DrawColor.A };

            for (int y = 0; y < bitmap.PixelHeight; y++)
            {
                if (y >= DrawRect.Y && y < DrawRect.Y + DrawRect.Height)
                {
                    for (int x = 0; x < bitmap.PixelWidth; x++)
                    {
                        if (x >= DrawRect.X && x < DrawRect.X + DrawRect.Width)
                        {
                            for (int i = 0; i < bytesPerPixel; i++)
                            {
                                pixels[y * stride + x * bytesPerPixel + i] = color[i % color.Length];
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clamps the drawing rect to the bounds of the writeable bitmap
        /// </summary>
        /// <param name="bmp">The bitmap to constrain the rect to</param>
        /// <param name="drawRect">The rect to constrain</param>
        /// <returns></returns>
        private Int32Rect Clamp(BitmapSource bmp, Int32Rect drawRect)
        {
            Int32Rect result = drawRect;

            result.Width = Math.Min(result.Width, bmp.PixelWidth - 1);
            result.Height = Math.Min(result.Height, bmp.PixelHeight - 1);

            result.X = Math.Min(result.X, bmp.PixelWidth - result.Width - 1);
            result.Y = Math.Min(result.Y, bmp.PixelHeight - result.Height - 1);
            
            return result;
        }
    }
}
