// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class DiscreteInt64KeyFrameFactory : Int64KeyFrameFactory<DiscreteInt64KeyFrame>
    {
        public override DiscreteInt64KeyFrame Create(DeterministicRandom random)
        {
            DiscreteInt64KeyFrame discreteInt64KeyFrame = new DiscreteInt64KeyFrame();
            ApplyInt64KeyFrameProperties(discreteInt64KeyFrame,random);
            return discreteInt64KeyFrame;
        }
    }
}
