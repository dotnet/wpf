// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class SplineInt32KeyFrameFactory : Int32KeyFrameFactory<SplineInt32KeyFrame>
    {
        #region Public Members

        public KeySpline KeySpline { get; set; }

        #endregion 

        #region Override Members

        public override SplineInt32KeyFrame Create(DeterministicRandom random)
        {
            SplineInt32KeyFrame splineInt32KeyFrame = new SplineInt32KeyFrame();
            splineInt32KeyFrame.KeySpline = KeySpline;
            ApplyInt32KeyFrameProperties(splineInt32KeyFrame,random);
            return splineInt32KeyFrame;
        }

        #endregion
    }
}
