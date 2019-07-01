// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class DoubleAnimationUsingKeyFramesFactory : TimelineFactory<DoubleAnimationUsingKeyFrames>
    {
        #region Public Members

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public DoubleKeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        public override DoubleAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            DoubleAnimationUsingKeyFrames doubleAnimationUsingKeyFrames = new DoubleAnimationUsingKeyFrames();
            doubleAnimationUsingKeyFrames.IsAdditive = IsAdditive;
            doubleAnimationUsingKeyFrames.IsCumulative = IsCumulative;
            doubleAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(doubleAnimationUsingKeyFrames, random);

            return doubleAnimationUsingKeyFrames;
        }

        #endregion
    }
}
