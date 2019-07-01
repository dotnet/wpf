// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(DiscreteStringKeyFrameFactory))]
    internal class DiscreteStringKeyFrameFactory : DiscoverableFactory<DiscreteStringKeyFrame>
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public string String { get; set; }

        public KeyTime KeyTime { get; set; }

        #endregion

        #region Override Members

        public override DiscreteStringKeyFrame Create(DeterministicRandom random)
        {
            DiscreteStringKeyFrame discreteStringKeyFrame = new DiscreteStringKeyFrame();
            discreteStringKeyFrame.Value = String;
            discreteStringKeyFrame.KeyTime = KeyTime;

            return discreteStringKeyFrame;
        }

        #endregion
    }
}
