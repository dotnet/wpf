// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class DiscreteVectorKeyFrameFactory : VectorKeyFrameFactory<DiscreteVectorKeyFrame>
    {
        public override DiscreteVectorKeyFrame Create(DeterministicRandom random)
        {
            DiscreteVectorKeyFrame discreteVectorKeyFrame = new DiscreteVectorKeyFrame();
            ApplyVectorKeyFrameProperties(discreteVectorKeyFrame, random);

            return discreteVectorKeyFrame;
        }
    }
}
