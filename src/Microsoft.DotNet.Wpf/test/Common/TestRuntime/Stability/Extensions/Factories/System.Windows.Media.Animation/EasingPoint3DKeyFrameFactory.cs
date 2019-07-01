// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class EasingPoint3DKeyFrameFactory : Point3DKeyFrameFactory<EasingPoint3DKeyFrame>
    {
        public EasingFunctionBase EasingFunction { get; set; }

        public override EasingPoint3DKeyFrame Create(DeterministicRandom random)
        {
            EasingPoint3DKeyFrame easingPoint3DKeyFrame = new EasingPoint3DKeyFrame();
            easingPoint3DKeyFrame.EasingFunction = EasingFunction;
            ApplyPoint3DKeyFrameProperties(easingPoint3DKeyFrame, random);

            return easingPoint3DKeyFrame;
        }
    }
}
