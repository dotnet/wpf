// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class Int16AnimationUsingKeyFramesFactory : TimelineFactory<Int16AnimationUsingKeyFrames>
    {
        #region Public Members

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public Int16KeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        public override Int16AnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            Int16AnimationUsingKeyFrames int16AnimationUsingKeyFrames = new Int16AnimationUsingKeyFrames();
            int16AnimationUsingKeyFrames.IsAdditive = IsAdditive;
            int16AnimationUsingKeyFrames.IsCumulative = IsCumulative;
            int16AnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(int16AnimationUsingKeyFrames, random);

            return int16AnimationUsingKeyFrames;
        }

        #endregion
    }
}
