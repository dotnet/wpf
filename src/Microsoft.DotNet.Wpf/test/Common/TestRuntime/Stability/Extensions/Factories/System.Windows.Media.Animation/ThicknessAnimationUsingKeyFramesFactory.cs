// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class ThicknessAnimationUsingKeyFramesFactory : TimelineFactory<ThicknessAnimationUsingKeyFrames>
    {
        #region Public Members

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public ThicknessKeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        public override ThicknessAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            ThicknessAnimationUsingKeyFrames thicknessAnimationUsingKeyFrames = new ThicknessAnimationUsingKeyFrames();
            thicknessAnimationUsingKeyFrames.IsAdditive = IsAdditive;
            thicknessAnimationUsingKeyFrames.IsCumulative = IsCumulative;
            thicknessAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(thicknessAnimationUsingKeyFrames, random);

            return thicknessAnimationUsingKeyFrames;
        }

        #endregion
    }
}
