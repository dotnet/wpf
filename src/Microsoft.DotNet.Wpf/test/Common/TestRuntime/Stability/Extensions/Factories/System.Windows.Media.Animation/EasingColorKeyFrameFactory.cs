// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class EasingColorKeyFrameFactory : ColorKeyFrameFactory<EasingColorKeyFrame>
    {
        #region Public Members

        public EasingFunctionBase EasingFunction { get; set; }

        #endregion

        #region Override Members

        public override EasingColorKeyFrame Create(DeterministicRandom random)
        {
            EasingColorKeyFrame easingColorKeyFrame = new EasingColorKeyFrame();
            easingColorKeyFrame.EasingFunction = EasingFunction;
            ApplyColorKeyFrameProperties(easingColorKeyFrame, random);
            return easingColorKeyFrame;
        }

        #endregion
    }
}
