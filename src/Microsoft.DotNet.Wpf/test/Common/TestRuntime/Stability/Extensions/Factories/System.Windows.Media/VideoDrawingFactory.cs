// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(VideoDrawing))]
    class VideoDrawingFactory : DiscoverableFactory<VideoDrawing>
    {
        public MediaPlayer Player { get; set; }

        public override VideoDrawing Create(DeterministicRandom random)
        {
            VideoDrawing drawing = new VideoDrawing();
            drawing.Player = Player;
            drawing.Freeze();

            return drawing;
        }
    }
}
