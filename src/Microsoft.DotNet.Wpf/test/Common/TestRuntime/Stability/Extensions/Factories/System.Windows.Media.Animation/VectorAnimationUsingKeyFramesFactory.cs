// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class VectorAnimationUsingKeyFramesFactory : TimelineFactory<VectorAnimationUsingKeyFrames>
    {
        #region Public Members

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public VectorKeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        public override VectorAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            VectorAnimationUsingKeyFrames vectorAnimationUsingKeyFrames = new VectorAnimationUsingKeyFrames();
            vectorAnimationUsingKeyFrames.IsAdditive = IsAdditive;
            vectorAnimationUsingKeyFrames.IsCumulative = IsCumulative;
            vectorAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(vectorAnimationUsingKeyFrames, random);

            return vectorAnimationUsingKeyFrames;
        }

        #endregion
    }
}
