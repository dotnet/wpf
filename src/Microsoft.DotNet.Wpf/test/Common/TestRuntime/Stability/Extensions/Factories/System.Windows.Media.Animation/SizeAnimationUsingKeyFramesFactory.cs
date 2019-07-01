// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class SizeAnimationUsingKeyFramesFactory : TimelineFactory<SizeAnimationUsingKeyFrames>
    {
        #region Public Members

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public SizeKeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        public override SizeAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            SizeAnimationUsingKeyFrames sizeAnimationUsingKeyFrames = new SizeAnimationUsingKeyFrames();
            sizeAnimationUsingKeyFrames.IsAdditive = IsAdditive;
            sizeAnimationUsingKeyFrames.IsCumulative = IsCumulative;
            sizeAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(sizeAnimationUsingKeyFrames, random);

            return sizeAnimationUsingKeyFrames;
        }

        #endregion
    }
}
