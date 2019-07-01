// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class SplineQuaternionKeyFrameFactory : QuaternionKeyFrameFactory<SplineQuaternionKeyFrame>
    {
        #region

        public KeySpline KeySpline { get; set; }

        public bool UseShortestPath { get; set; }

        #endregion

        #region Override Members

        public override SplineQuaternionKeyFrame Create(DeterministicRandom random)
        {
            SplineQuaternionKeyFrame splineQuaternionKeyFrame = new SplineQuaternionKeyFrame();
            splineQuaternionKeyFrame.KeySpline = KeySpline;
            splineQuaternionKeyFrame.UseShortestPath = UseShortestPath;
            ApplyQuaternionKeyFrameProperties(splineQuaternionKeyFrame, random);

            return splineQuaternionKeyFrame;
        }

        #endregion
    }
}
