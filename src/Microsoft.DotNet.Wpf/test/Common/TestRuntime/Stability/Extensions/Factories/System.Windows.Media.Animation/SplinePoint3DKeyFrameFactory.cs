// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class SplinePoint3DKeyFrameFactory : Point3DKeyFrameFactory<SplinePoint3DKeyFrame>
    {
        public KeySpline KeySpline { get; set; }

        public override SplinePoint3DKeyFrame Create(DeterministicRandom random)
        {
            SplinePoint3DKeyFrame splinePoint3DKeyFrame = new SplinePoint3DKeyFrame();
            splinePoint3DKeyFrame.KeySpline = KeySpline;
            ApplyPoint3DKeyFrameProperties(splinePoint3DKeyFrame, random);

            return splinePoint3DKeyFrame;
        }
    }
}
