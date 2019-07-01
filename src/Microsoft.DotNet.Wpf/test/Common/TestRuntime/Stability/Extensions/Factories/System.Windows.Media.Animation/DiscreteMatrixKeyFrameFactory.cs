// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class DiscreteMatrixKeyFrameFactory : DiscoverableFactory<DiscreteMatrixKeyFrame>
    {
        #region Public Members

        public Matrix Matrix { get; set; }

        public KeyTime KeyTime { get; set; }

        #endregion

        #region Override Members

        public override DiscreteMatrixKeyFrame Create(DeterministicRandom random)
        {
            DiscreteMatrixKeyFrame discreteMatrixKeyFrame = new DiscreteMatrixKeyFrame();
            discreteMatrixKeyFrame.Value = Matrix;
            discreteMatrixKeyFrame.KeyTime = KeyTime;

            return discreteMatrixKeyFrame;
        }

        #endregion
    }
}
