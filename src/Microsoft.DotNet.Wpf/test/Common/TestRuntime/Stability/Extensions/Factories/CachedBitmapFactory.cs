// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media.Imaging;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class CachedBitmapFactory : DiscoverableFactory<CachedBitmap>
    {
        public BitmapSource BitmapSource { get; set; }

        public override CachedBitmap Create(DeterministicRandom random)
        {
            CachedBitmap cached = new CachedBitmap(BitmapSource, random.NextEnum<BitmapCreateOptions>(), random.NextEnum<BitmapCacheOption>());
            cached.Freeze();

            return cached;
        }
    }
}
