// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media.Imaging;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class FormatConvertedBitmapFactory : DiscoverableFactory<FormatConvertedBitmap>
    {
        public BitmapSource BitmapSource { get; set; }

        public override FormatConvertedBitmap Create(DeterministicRandom random)
        {
            FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap();
            formatConvertedBitmap.BeginInit();
            formatConvertedBitmap.Source = BitmapSource;
            formatConvertedBitmap.EndInit();
            formatConvertedBitmap.Freeze();
            return formatConvertedBitmap;
        }
    }
}
