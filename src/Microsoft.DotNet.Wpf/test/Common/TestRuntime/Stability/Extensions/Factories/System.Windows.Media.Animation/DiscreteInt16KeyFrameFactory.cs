// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class DiscreteInt16KeyFrameFactory : Int16KeyFrameFactory<DiscreteInt16KeyFrame>
    {
        #region Override Members

        public override DiscreteInt16KeyFrame Create(DeterministicRandom random)
        {
            DiscreteInt16KeyFrame discreteInt16KeyFrame = new DiscreteInt16KeyFrame();
            ApplyInt16KeyFrameProperties(discreteInt16KeyFrame, random);
            return discreteInt16KeyFrame;
        }

        #endregion
    }
}
