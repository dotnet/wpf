// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class DiscreteRotation3DKeyFrameFactory : Rotation3DKeyFrameFactory<DiscreteRotation3DKeyFrame>
    {
        public override DiscreteRotation3DKeyFrame Create(DeterministicRandom random)
        {
            DiscreteRotation3DKeyFrame discreteRotation3DKeyFrame = new DiscreteRotation3DKeyFrame();
            ApplyRotation3DKeyFrameProperties(discreteRotation3DKeyFrame, random);

            return discreteRotation3DKeyFrame;
        }
    }
}
