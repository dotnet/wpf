// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class SplineDecimalKeyFrameFactory : DecimalKeyFrameFactory<SplineDecimalKeyFrame>
    {
        #region Public Members

        public KeySpline KeySpline { get; set; }

        #endregion

        #region Override Members

        public override SplineDecimalKeyFrame Create(DeterministicRandom random)
        {
            SplineDecimalKeyFrame splineDecimalKeyFrame = new SplineDecimalKeyFrame();
            splineDecimalKeyFrame.KeySpline = KeySpline;
            ApplyDecimalKeyFrameProperties(splineDecimalKeyFrame, random);

            return splineDecimalKeyFrame;
        }

        #endregion
    }
}
