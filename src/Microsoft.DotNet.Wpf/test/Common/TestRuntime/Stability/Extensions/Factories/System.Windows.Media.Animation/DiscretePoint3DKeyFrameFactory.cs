// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class DiscretePoint3DKeyFrameFactory : Point3DKeyFrameFactory<DiscretePoint3DKeyFrame>
    {
        public override DiscretePoint3DKeyFrame Create(DeterministicRandom random)
        {
            DiscretePoint3DKeyFrame discretePoint3DKeyFrame = new DiscretePoint3DKeyFrame();
            ApplyPoint3DKeyFrameProperties(discretePoint3DKeyFrame, random);

            return discretePoint3DKeyFrame;
        }
    }
}
