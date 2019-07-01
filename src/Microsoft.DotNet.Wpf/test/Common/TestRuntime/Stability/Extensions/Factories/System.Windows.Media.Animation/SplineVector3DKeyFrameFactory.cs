// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class SplineVector3DKeyFrameFactory : Vector3DKeyFrameFactory<SplineVector3DKeyFrame>
    {
        public KeySpline KeySpline { get; set; }

        public override SplineVector3DKeyFrame Create(DeterministicRandom random)
        {
            SplineVector3DKeyFrame splineVector3DKeyFrame = new SplineVector3DKeyFrame();
            splineVector3DKeyFrame.KeySpline = KeySpline;
            ApplyVector3DKeyFrameProperties(splineVector3DKeyFrame, random);

            return splineVector3DKeyFrame;
        }
    }
}
