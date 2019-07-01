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
    [TargetTypeAttribute(typeof(RenderTargetBitmap))]
    class RenderTargetBitmapFactory : DiscoverableFactory<RenderTargetBitmap>
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
        public Visual Visual { get; set; }

        public override RenderTargetBitmap Create(DeterministicRandom random)
        {
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(PixelWidth, PixelHeight, DpiX, DpiY, PixelFormat);
            if (Visual != null)
            {
                renderTargetBitmap.Render(Visual);
            }
            renderTargetBitmap.Freeze();
            return renderTargetBitmap;
        }
    }
}
