// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class EasingDecimalKeyFrameFactory : DecimalKeyFrameFactory<EasingDecimalKeyFrame>
    {
        #region Public Members

        public EasingFunctionBase EasingFunction { get; set; }

        #endregion

        #region Override Members

        public override EasingDecimalKeyFrame Create(DeterministicRandom random)
        {
            EasingDecimalKeyFrame easingDecimalKeyFrame = new EasingDecimalKeyFrame();
            easingDecimalKeyFrame.EasingFunction = EasingFunction;
            ApplyDecimalKeyFrameProperties(easingDecimalKeyFrame, random);

            return easingDecimalKeyFrame;
        }

        #endregion
    }
}
