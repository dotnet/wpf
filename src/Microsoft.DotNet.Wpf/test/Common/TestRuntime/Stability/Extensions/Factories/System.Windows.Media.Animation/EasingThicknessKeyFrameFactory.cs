// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class EasingThicknessKeyFrameFactory : ThicknessKeyFrameFactory<EasingThicknessKeyFrame>
    {
        public EasingFunctionBase EasingFunction { get; set; }

        public override EasingThicknessKeyFrame Create(DeterministicRandom random)
        {
            EasingThicknessKeyFrame easingThicknessKeyFrame = new EasingThicknessKeyFrame();
            easingThicknessKeyFrame.EasingFunction = EasingFunction;
            ApplyThicknessKeyFrameProperties(easingThicknessKeyFrame, random);

            return easingThicknessKeyFrame;
        }
    }
}
