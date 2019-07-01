// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class EasingSizeKeyFrameFactory : SizeKeyFrameFactory<EasingSizeKeyFrame>
    {
        public EasingFunctionBase EasingFunction { get; set; }

        public override EasingSizeKeyFrame Create(DeterministicRandom random)
        {
            EasingSizeKeyFrame easingSizeKeyFrame = new EasingSizeKeyFrame();
            easingSizeKeyFrame.EasingFunction = EasingFunction;
            ApplySizeKeyFrameProperties(easingSizeKeyFrame, random);

            return easingSizeKeyFrame;
        }
    }
}
