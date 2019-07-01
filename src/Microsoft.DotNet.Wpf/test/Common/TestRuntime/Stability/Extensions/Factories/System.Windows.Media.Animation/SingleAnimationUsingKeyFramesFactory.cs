// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class SingleAnimationUsingKeyFramesFactory : TimelineFactory<SingleAnimationUsingKeyFrames>
    {
        #region Public Members

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public SingleKeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        public override SingleAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            SingleAnimationUsingKeyFrames singleAnimationUsingKeyFrames = new SingleAnimationUsingKeyFrames();
            singleAnimationUsingKeyFrames.IsAdditive = IsAdditive;
            singleAnimationUsingKeyFrames.IsCumulative = IsCumulative;
            singleAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(singleAnimationUsingKeyFrames, random);

            return singleAnimationUsingKeyFrames;
        }

        #endregion
    }
}
