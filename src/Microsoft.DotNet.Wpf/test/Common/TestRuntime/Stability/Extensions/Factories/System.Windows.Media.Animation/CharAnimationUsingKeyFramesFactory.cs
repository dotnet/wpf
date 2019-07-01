// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class CharAnimationUsingKeyFramesFactory : TimelineFactory<CharAnimationUsingKeyFrames>
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public CharKeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        public override CharAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            CharAnimationUsingKeyFrames charAnimationUsingKeyFrames = new CharAnimationUsingKeyFrames();
            charAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(charAnimationUsingKeyFrames, random);
            return charAnimationUsingKeyFrames;
        }

        #endregion
    }
}
