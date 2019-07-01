// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class RectAnimationFactory : TimelineFactory<RectAnimation>
    {
        public Rect FromRect { get; set; }

        public Rect Rect { get; set; }

        public override RectAnimation Create(DeterministicRandom random)
        {
            RectAnimation rectAnimation = new RectAnimation();

            rectAnimation.From = FromRect;

            if (random.NextBool())
            {
                rectAnimation.To = Rect;
            }
            else
            {
                rectAnimation.By = Rect;
            }

            ApplyTimelineProperties(rectAnimation, random);

            return rectAnimation;
        }
    }
}
