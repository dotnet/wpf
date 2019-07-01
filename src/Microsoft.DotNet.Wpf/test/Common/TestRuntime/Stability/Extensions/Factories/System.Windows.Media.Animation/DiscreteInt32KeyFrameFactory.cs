// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class DiscreteInt32KeyFrameFactory : Int32KeyFrameFactory<DiscreteInt32KeyFrame>
    {
        #region Override Members

        public override DiscreteInt32KeyFrame Create(DeterministicRandom random)
        {
            DiscreteInt32KeyFrame discreteInt32KeyFrame = new DiscreteInt32KeyFrame();
            ApplyInt32KeyFrameProperties(discreteInt32KeyFrame, random);
            return discreteInt32KeyFrame;
        }

        #endregion
    }
}
