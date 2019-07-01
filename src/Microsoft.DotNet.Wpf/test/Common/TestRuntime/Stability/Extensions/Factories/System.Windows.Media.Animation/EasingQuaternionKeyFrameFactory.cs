// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class EasingQuaternionKeyFrameFactory : QuaternionKeyFrameFactory<EasingQuaternionKeyFrame>
    {
        #region Public Members

        public EasingFunctionBase EasingFunction { get; set; }

        public bool UseShortestPath { get; set; }

        #endregion

        #region Override Members

        public override EasingQuaternionKeyFrame Create(DeterministicRandom random)
        {
            EasingQuaternionKeyFrame easingQuaternionKeyFrame = new EasingQuaternionKeyFrame();
            easingQuaternionKeyFrame.EasingFunction = EasingFunction;
            easingQuaternionKeyFrame.UseShortestPath = UseShortestPath;
            ApplyQuaternionKeyFrameProperties(easingQuaternionKeyFrame, random);

            return easingQuaternionKeyFrame;
        }

        #endregion
    }
}
