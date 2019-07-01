// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class RectAnimationUsingKeyFramesFactory : TimelineFactory<RectAnimationUsingKeyFrames>
    {
        #region Public Members

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public RectKeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        public override RectAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            RectAnimationUsingKeyFrames rectAnimationUsingKeyFrames = new RectAnimationUsingKeyFrames();
            rectAnimationUsingKeyFrames.IsAdditive = IsAdditive;
            rectAnimationUsingKeyFrames.IsCumulative = IsCumulative;
            rectAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(rectAnimationUsingKeyFrames, random);

            return rectAnimationUsingKeyFrames;
        }

        #endregion
    }
}
