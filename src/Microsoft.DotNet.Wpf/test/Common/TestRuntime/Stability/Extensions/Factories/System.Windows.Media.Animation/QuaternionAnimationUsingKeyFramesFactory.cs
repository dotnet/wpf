// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class QuaternionAnimationUsingKeyFramesFactory : TimelineFactory<QuaternionAnimationUsingKeyFrames>
    {
        #region Public Members

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public QuaternionKeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        public override QuaternionAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            QuaternionAnimationUsingKeyFrames quaternionAnimationUsingKeyFrames = new QuaternionAnimationUsingKeyFrames();
            quaternionAnimationUsingKeyFrames.IsAdditive = IsAdditive;
            quaternionAnimationUsingKeyFrames.IsCumulative = IsCumulative;
            quaternionAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(quaternionAnimationUsingKeyFrames, random);

            return quaternionAnimationUsingKeyFrames;
        }

        #endregion
    }
}
