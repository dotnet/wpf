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
    /// <summary>
    /// BitmapCacheBrush inherit Brush, but not all Brush properties can be applied. For example, Opacity cannot apply 
    /// on a BitmapCacheBrush. So BitmapCacheBrushFactory doesn't inherit BrushFactory. 
    /// </summary>
    class BitmapCacheBrushFactory : DiscoverableFactory<BitmapCacheBrush>
    {
        public Visual Visual { get; set; }
        public bool AutoLayoutContent { get; set; }
        public BitmapCache BitmapCache { get; set; }
        
        public override BitmapCacheBrush Create(DeterministicRandom random)
        {
            BitmapCacheBrush brush = new BitmapCacheBrush(Visual);

            brush.AutoLayoutContent = AutoLayoutContent;
            brush.BitmapCache = BitmapCache;

            return brush;
        }
    }
#endif
}
