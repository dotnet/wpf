// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class Rotation3DAnimationUsingKeyFramesFactory : TimelineFactory<Rotation3DAnimationUsingKeyFrames>
    {
        #region Public Members

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public Rotation3DKeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        public override Rotation3DAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            Rotation3DAnimationUsingKeyFrames rotation3DAnimationUsingKeyFrames = new Rotation3DAnimationUsingKeyFrames();
            rotation3DAnimationUsingKeyFrames.IsAdditive = IsAdditive;
            rotation3DAnimationUsingKeyFrames.IsCumulative = IsCumulative;
            rotation3DAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(rotation3DAnimationUsingKeyFrames, random);

            return rotation3DAnimationUsingKeyFrames;
        }

        #endregion
    }
}
