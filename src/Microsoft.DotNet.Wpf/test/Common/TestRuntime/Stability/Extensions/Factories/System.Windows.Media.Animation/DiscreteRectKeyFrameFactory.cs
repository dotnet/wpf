// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class DiscreteRectKeyFrameFactory : RectKeyFrameFactory<DiscreteRectKeyFrame>
    {
        public override DiscreteRectKeyFrame Create(DeterministicRandom random)
        {
            DiscreteRectKeyFrame discreteRectKeyFrame = new DiscreteRectKeyFrame();
            ApplyRectKeyFrameProperties(discreteRectKeyFrame, random);

            return discreteRectKeyFrame;
        }
    }
}
