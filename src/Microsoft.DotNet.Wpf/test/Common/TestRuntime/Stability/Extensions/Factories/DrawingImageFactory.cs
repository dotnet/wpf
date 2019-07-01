// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class DrawingImageFactory : DiscoverableFactory<DrawingImage>
    {
        [InputAttribute(ContentInputSource.CreateFromFactory)]
        public Drawing DrawingSource { get; set; }

        public override DrawingImage Create(DeterministicRandom random)
        {
            DrawingImage drawingImage = new DrawingImage();
            drawingImage.Drawing = DrawingSource;
            return drawingImage;
        }
    }
}
