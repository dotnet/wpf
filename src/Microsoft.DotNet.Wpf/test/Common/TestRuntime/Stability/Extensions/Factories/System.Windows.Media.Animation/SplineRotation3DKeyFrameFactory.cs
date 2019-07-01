// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class SplineRotation3DKeyFrameFactory : Rotation3DKeyFrameFactory<SplineRotation3DKeyFrame>
    {
        public KeySpline KeySpline { get; set; }

        public override SplineRotation3DKeyFrame Create(DeterministicRandom random)
        {
            SplineRotation3DKeyFrame splineRotation3DKeyFrame = new SplineRotation3DKeyFrame();
            splineRotation3DKeyFrame.KeySpline = KeySpline;
            ApplyRotation3DKeyFrameProperties(splineRotation3DKeyFrame, random);

            return splineRotation3DKeyFrame;
        }
    }
}
