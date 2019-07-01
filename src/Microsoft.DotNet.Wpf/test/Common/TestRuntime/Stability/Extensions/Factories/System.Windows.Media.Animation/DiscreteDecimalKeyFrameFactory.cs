// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class DiscreteDecimalKeyFrameFactory : DecimalKeyFrameFactory<DiscreteDecimalKeyFrame>
    {
        #region Public Members

        public override DiscreteDecimalKeyFrame Create(DeterministicRandom random)
        {
            DiscreteDecimalKeyFrame discreteDecimalKeyFrame = new DiscreteDecimalKeyFrame();
            ApplyDecimalKeyFrameProperties(discreteDecimalKeyFrame, random);

            return discreteDecimalKeyFrame;
        }

        #endregion
    }
}
