// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class ColorAnimationUsingKeyFramesFactory : TimelineFactory<ColorAnimationUsingKeyFrames>
    {
        #region Public Members

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public ColorKeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        public override ColorAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            ColorAnimationUsingKeyFrames colorAnimationUsingKeyFrames = new ColorAnimationUsingKeyFrames();
            colorAnimationUsingKeyFrames.IsAdditive = IsAdditive;
            colorAnimationUsingKeyFrames.IsCumulative = IsCumulative;
            colorAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(colorAnimationUsingKeyFrames, random);

            return colorAnimationUsingKeyFrames;
        }

        #endregion
    }
}
