// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class EasingVector3DKeyFrameFactory : Vector3DKeyFrameFactory<EasingVector3DKeyFrame>
    {
        public EasingFunctionBase EasingFunction { get; set; }

        public override EasingVector3DKeyFrame Create(DeterministicRandom random)
        {
            EasingVector3DKeyFrame easingVector3DKeyFrame = new EasingVector3DKeyFrame();
            easingVector3DKeyFrame.EasingFunction = EasingFunction;
            ApplyVector3DKeyFrameProperties(easingVector3DKeyFrame, random);

            return easingVector3DKeyFrame;
        }
    }
}
