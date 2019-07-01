// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class DiscreteVector3DKeyFrameFactory : Vector3DKeyFrameFactory<DiscreteVector3DKeyFrame>
    {
        public override DiscreteVector3DKeyFrame Create(DeterministicRandom random)
        {
            DiscreteVector3DKeyFrame discreteVector3DKeyFrame = new DiscreteVector3DKeyFrame();
            ApplyVector3DKeyFrameProperties(discreteVector3DKeyFrame, random);

            return discreteVector3DKeyFrame;
        }
    }
}
