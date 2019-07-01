// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class EasingInt64KeyFrameFactory : Int64KeyFrameFactory<EasingInt64KeyFrame>
    {
        public EasingFunctionBase EasingFunction { get; set; }

        public override EasingInt64KeyFrame Create(DeterministicRandom random)
        {
            EasingInt64KeyFrame easingInt64KeyFrame = new EasingInt64KeyFrame();
            easingInt64KeyFrame.EasingFunction = EasingFunction;
            ApplyInt64KeyFrameProperties(easingInt64KeyFrame, random);
            return easingInt64KeyFrame;
        }
    }
}
