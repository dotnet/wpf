// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class Int64AnimationUsingKeyFramesFactory : TimelineFactory<Int64AnimationUsingKeyFrames>
    {
        #region Public Members

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public Int64KeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        public override Int64AnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            Int64AnimationUsingKeyFrames int64AnimationUsingKeyFrames = new Int64AnimationUsingKeyFrames();
            int64AnimationUsingKeyFrames.IsAdditive = IsAdditive;
            int64AnimationUsingKeyFrames.IsCumulative = IsCumulative;
            int64AnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(int64AnimationUsingKeyFrames, random);

            return int64AnimationUsingKeyFrames;
        }

        #endregion
    }
}
