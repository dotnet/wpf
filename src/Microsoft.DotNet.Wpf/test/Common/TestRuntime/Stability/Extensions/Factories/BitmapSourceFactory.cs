// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(BitmapSource))]
    class BitmapSourceFactory : DiscoverableFactory<BitmapSource>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public PixelFormat PixelFormat { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 PixelWidth { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 PixelHeight { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double DpiX { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double DpiY { get; set; }
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent=true)]
        public BitmapPalette BitmapPalette { get; set; }

        public override BitmapSource Create(DeterministicRandom random)
        {
            int rawStride = (PixelWidth * PixelFormat.BitsPerPixel + 7) / 8;
            byte[] rawImage = new byte[rawStride * PixelHeight];

            for (int i = 0; i <= rawStride * PixelHeight - 1; i++)
            {
                rawImage[i] = (byte)random.Next(256);
            }

            BitmapSource bitmapSource = BitmapSource.Create(PixelWidth, PixelHeight,
                DpiX, DpiY, PixelFormat, BitmapPalette,
                rawImage, rawStride);

            bitmapSource.Freeze();

            return bitmapSource;
        }
    }
}
