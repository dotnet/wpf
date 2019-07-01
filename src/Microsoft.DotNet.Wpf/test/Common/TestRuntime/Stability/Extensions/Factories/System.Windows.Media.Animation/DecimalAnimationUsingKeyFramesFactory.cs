// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class DecimalAnimationUsingKeyFramesFactory : TimelineFactory<DecimalAnimationUsingKeyFrames>
    {
        #region Public Members

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public DecimalKeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        public override DecimalAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            DecimalAnimationUsingKeyFrames decimalAnimationUsingKeyFrames = new DecimalAnimationUsingKeyFrames();
            decimalAnimationUsingKeyFrames.IsAdditive = IsAdditive;
            decimalAnimationUsingKeyFrames.IsCumulative = IsCumulative;
            decimalAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(decimalAnimationUsingKeyFrames, random);

            return decimalAnimationUsingKeyFrames;
        }

        #endregion
    }
}
