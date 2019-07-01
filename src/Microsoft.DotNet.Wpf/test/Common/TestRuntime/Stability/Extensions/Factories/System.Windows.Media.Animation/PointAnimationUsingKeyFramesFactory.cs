// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class PointAnimationUsingKeyFramesFactory : TimelineFactory<PointAnimationUsingKeyFrames>
    {
        #region Public Members

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public PointKeyFrameCollection KeyFrames { get; set; }

        #endregion


        #region Override Members

        public override PointAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            PointAnimationUsingKeyFrames pointAnimationUsingKeyFrames = new PointAnimationUsingKeyFrames();
            pointAnimationUsingKeyFrames.IsAdditive = IsAdditive;
            pointAnimationUsingKeyFrames.IsCumulative = IsCumulative;
            pointAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(pointAnimationUsingKeyFrames, random);

            return pointAnimationUsingKeyFrames;
        }

        #endregion
    }
}
