// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class DiscreteCharKeyFrameFactory : DiscoverableFactory<DiscreteCharKeyFrame>
    {
        #region Public Members

        public char Value { get; set; }

        public KeyTime KeyTime { get; set; }

        #endregion

        #region Override Members

        public override DiscreteCharKeyFrame Create(DeterministicRandom random)
        {
            DiscreteCharKeyFrame discreteCharKeyFrame = new DiscreteCharKeyFrame();
            discreteCharKeyFrame.Value = Value;
            discreteCharKeyFrame.KeyTime = KeyTime;
            return discreteCharKeyFrame;
        }

        #endregion
    }
}
