// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class SplineInt64KeyFrameFactory : Int64KeyFrameFactory<SplineInt64KeyFrame>
    {
        public KeySpline KeySpline { get; set; }

        public override SplineInt64KeyFrame Create(DeterministicRandom random)
        {
            SplineInt64KeyFrame splineInt64KeyFrame = new SplineInt64KeyFrame();
            splineInt64KeyFrame.KeySpline = KeySpline;
            ApplyInt64KeyFrameProperties(splineInt64KeyFrame, random);
            return splineInt64KeyFrame;
        }
    }
}
