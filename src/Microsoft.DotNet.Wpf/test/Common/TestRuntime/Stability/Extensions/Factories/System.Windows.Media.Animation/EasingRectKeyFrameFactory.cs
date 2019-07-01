// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class EasingRectKeyFrameFactory : RectKeyFrameFactory<EasingRectKeyFrame>
    {
        public EasingFunctionBase EasingFunction { get; set; }

        public override EasingRectKeyFrame Create(DeterministicRandom random)
        {
            EasingRectKeyFrame easingRectKeyFrame = new EasingRectKeyFrame();
            easingRectKeyFrame.EasingFunction = EasingFunction;
            ApplyRectKeyFrameProperties(easingRectKeyFrame, random);

            return easingRectKeyFrame;
        }
    }
}
