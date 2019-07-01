// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class CroppedBitmapFactory : DiscoverableFactory<CroppedBitmap>
    {
        public BitmapSource BitmapSource { get; set; }

        public override CroppedBitmap Create(DeterministicRandom random)
        {
            int maxX = BitmapSource.PixelWidth;
            int maxY = BitmapSource.PixelHeight;
            int width = random.Next(maxX) + 1;
            int height = random.Next(maxY) + 1;
            int x = maxX - width;
            int y = maxY - height;

            CroppedBitmap croppedBitmap = new CroppedBitmap();
            croppedBitmap.BeginInit();
            croppedBitmap.Source = BitmapSource;
            croppedBitmap.SourceRect = new Int32Rect(x, y, width, height);
            croppedBitmap.EndInit();
            croppedBitmap.Freeze();
            return croppedBitmap;
        }
    }
}
