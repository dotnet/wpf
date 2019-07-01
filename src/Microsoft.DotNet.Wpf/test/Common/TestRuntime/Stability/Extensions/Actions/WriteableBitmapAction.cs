// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Test.Stability.Extensions.Utilities;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    internal class WriteableBitmapAction : DiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Image TargetImage { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory)]
        public Int32Rect DrawRect { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory)]
        public Color DrawColor { get; set; }

        public override bool CanPerform()
        {
            return TargetImage != null && (TargetImage.Source is WriteableBitmap) && !DrawRect.IsEmpty;
        }

        public override void Perform()
        {
            WriteableBitmap wbmp = (WriteableBitmap)TargetImage.Source;

            // clamp to valid ranges
            Int32Rect drawRect = Clamp(wbmp, DrawRect);
            WriteableBitmapWriter writer = WriteableBitmapWriter.CreateWriter((int)wbmp.PixelHeight, wbmp.BackBufferStride, wbmp.Format);
            writer.SetWriteableBitmapPixels(wbmp, drawRect, DrawColor);
        }

        /// <summary>
        /// Clamps the drawing rect to the bounds of the writeable bitmap
        /// </summary>
        /// <param name="wbmp">The bitmap to constrain the rect to</param>
        /// <param name="drawRect">The rect to constrain</param>
        /// <returns></returns>
        private Int32Rect Clamp(WriteableBitmap wbmp, Int32Rect drawRect)
        {
            Int32Rect result = drawRect;

            result.Width = Math.Min(result.Width, wbmp.PixelWidth - 1);
            result.Height = Math.Min(result.Height, wbmp.PixelHeight - 1);

            result.X = Math.Min(result.X, wbmp.PixelWidth - result.Width - 1);
            result.Y = Math.Min(result.Y, wbmp.PixelHeight - result.Height - 1);
            
            return result;
        }
    }
}
