// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
#if TESTBUILD_CLR40
    class BitmapCacheFactory : DiscoverableFactory<BitmapCache>
    {
        public bool EnableClearType { get; set; }
        public double RenderAtScale { get; set; }
        public bool SnapsToDevicePixels { get; set; }
        
        public override BitmapCache Create(DeterministicRandom random)
        {
            BitmapCache bitmapCache = new BitmapCache();
            bitmapCache.EnableClearType = EnableClearType;

            bitmapCache.RenderAtScale = RenderAtScale;

            return bitmapCache;
        }
    }
#endif
}
