// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(ColorConvertedBitmap))]
    class ColorConvertedBitmapFactory : DiscoverableFactory<ColorConvertedBitmap>
    {
        public BitmapFrame BitmapFrame { get; set; }
        public ColorContext ColorContext { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public PixelFormat PixelFormat { get; set; }

        public override ColorConvertedBitmap Create(DeterministicRandom random)
        {
            BitmapSource bitmapSource = (BitmapSource)BitmapFrame;
            ColorContext sourceColorContext = BitmapFrame.ColorContexts[0];
            ColorConvertedBitmap cloreConverted = new ColorConvertedBitmap(bitmapSource, sourceColorContext, ColorContext, PixelFormat);
            cloreConverted.Freeze();
            return cloreConverted;
        }
    }
}
