// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(TransformedBitmap))]
    class TransformedBitmapFactory : DiscoverableFactory<TransformedBitmap>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Transform Transform { set; get; }

        public BitmapSource BitmapSource { set; get; }

        public override TransformedBitmap Create(DeterministicRandom random)
        {
            if (BitmapSource != null)
            {
                TransformedBitmap transformed = new TransformedBitmap(BitmapSource, Transform);
                transformed.Freeze();
                return transformed;
            }
            else
            {
                return null;
            }
        }
    }
}
