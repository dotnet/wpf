// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class ImageDrawingFactory : DiscoverableFactory<ImageDrawing>
    {
        [InputAttribute(ContentInputSource.CreateFromFactory)]
        public ImageSource Source { get; set; }

        public override ImageDrawing Create(DeterministicRandom random)
        {
            if (Source != null)
            {
                ImageDrawing imageDrawing = new ImageDrawing(Source, new Rect(0, 0, Source.Width, Source.Height));
                return imageDrawing;
            }
            else
            {
                return null;
            }
        }
    }
}
