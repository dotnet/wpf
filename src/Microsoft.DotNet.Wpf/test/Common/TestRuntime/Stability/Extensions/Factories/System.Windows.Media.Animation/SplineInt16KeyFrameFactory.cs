// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class SplineInt16KeyFrameFactory : Int16KeyFrameFactory<SplineInt16KeyFrame>
    {
        #region Public Members

        public KeySpline KeySpline { get; set; }

        #endregion

        #region Override Members

        public override SplineInt16KeyFrame Create(DeterministicRandom random)
        {
            SplineInt16KeyFrame splineInt16KeyFrame = new SplineInt16KeyFrame();
            splineInt16KeyFrame.KeySpline = KeySpline;
            ApplyInt16KeyFrameProperties(splineInt16KeyFrame, random);
            return splineInt16KeyFrame;
        }

        #endregion
    }
}
