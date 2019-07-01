// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class ByteAnimationUsingKeyFramesFactory : TimelineFactory<ByteAnimationUsingKeyFrames>
    {
        #region Public Members

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public ByteKeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new ByteAnimationUsingKeyFrames</returns>
        public override ByteAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            ByteAnimationUsingKeyFrames byteAnimationUsingKeyFrames = new ByteAnimationUsingKeyFrames();
            byteAnimationUsingKeyFrames.IsAdditive = IsAdditive;
            byteAnimationUsingKeyFrames.IsCumulative = IsCumulative;
            byteAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(byteAnimationUsingKeyFrames, random);

            return byteAnimationUsingKeyFrames;
        }

        #endregion
    }
}
