// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class EasingRotation3DKeyFrameFactory : Rotation3DKeyFrameFactory<EasingRotation3DKeyFrame>
    {
        public EasingFunctionBase EasingFunction { get; set; }

        public override EasingRotation3DKeyFrame Create(DeterministicRandom random)
        {
            EasingRotation3DKeyFrame easingRotation3DKeyFrame = new EasingRotation3DKeyFrame();
            easingRotation3DKeyFrame.EasingFunction = EasingFunction;
            ApplyRotation3DKeyFrameProperties(easingRotation3DKeyFrame, random);

            return easingRotation3DKeyFrame;
        }
    }
}
