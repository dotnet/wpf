// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Media.Imaging;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(BitmapFrame))]
    class BitmapFrameFactory : DiscoverableFactory<BitmapFrame>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Uri Uri { get; set; }

        public override BitmapFrame Create(DeterministicRandom random)
        {
            BitmapFrame bitmapFrame = BitmapFrame.Create(Uri);
            bitmapFrame.Freeze();
            return bitmapFrame;
        }
    }
}
