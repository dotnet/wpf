// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class Int32AnimationUsingKeyFramesFactory : TimelineFactory<Int32AnimationUsingKeyFrames>
    {
        #region Public Members

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public Int32KeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        public override Int32AnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            Int32AnimationUsingKeyFrames int32AnimationUsingKeyFrames = new Int32AnimationUsingKeyFrames();
            int32AnimationUsingKeyFrames.IsAdditive = IsAdditive;
            int32AnimationUsingKeyFrames.IsCumulative = IsCumulative;
            int32AnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(int32AnimationUsingKeyFrames, random);

            return int32AnimationUsingKeyFrames;
        }

        #endregion
    }
}
