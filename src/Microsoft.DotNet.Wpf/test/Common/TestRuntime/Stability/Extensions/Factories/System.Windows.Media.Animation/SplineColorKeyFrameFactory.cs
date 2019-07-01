// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class SplineColorKeyFrameFactory : ColorKeyFrameFactory<SplineColorKeyFrame>
    {
        #region Public Members

        public KeySpline KeySpline { get; set; }

        #endregion

        #region Override Members

        public override SplineColorKeyFrame Create(DeterministicRandom random)
        {
            SplineColorKeyFrame splineColorKeyFrame = new SplineColorKeyFrame();
            splineColorKeyFrame.KeySpline = KeySpline;
            ApplyColorKeyFrameProperties(splineColorKeyFrame, random);
            return splineColorKeyFrame;
        }

        #endregion
    }
}
