// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class ImageFactory : DiscoverableFactory<Image>
    {
        [InputAttribute(ContentInputSource.CreateFromFactory)]
        public ImageSource SourceImage { get; set; }
        public BitmapScalingMode BitmapScalingMode { get; set; }
        public CachingHint CachingHint { get; set; }


        public override Image Create(DeterministicRandom random)
        {
            Image image = new Image();
            image.Source = SourceImage;
            image.Stretch = random.NextEnum<Stretch>();
            image.StretchDirection = random.NextEnum<StretchDirection>();
            image.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode);
            image.SetValue(RenderOptions.CachingHintProperty, CachingHint);
            return image;
        }
    }
}
