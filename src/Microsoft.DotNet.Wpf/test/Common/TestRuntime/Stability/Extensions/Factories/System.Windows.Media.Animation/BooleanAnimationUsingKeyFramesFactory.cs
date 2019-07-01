// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class BooleanAnimationUsingKeyFramesFactory : TimelineFactory<BooleanAnimationUsingKeyFrames>
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public BooleanKeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new BooleanAnimationUsingKeyFrames</returns>
        public override BooleanAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            BooleanAnimationUsingKeyFrames booleanAnimationUsingKeyFrames = new BooleanAnimationUsingKeyFrames();
            booleanAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(booleanAnimationUsingKeyFrames, random);
            return booleanAnimationUsingKeyFrames;
        }

        #endregion
    }
}
