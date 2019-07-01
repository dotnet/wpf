// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class SplineByteKeyFrameFactory : ByteKeyFrameFactory<SplineByteKeyFrame>
    {
        #region Public Members

        public KeySpline KeySpline { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new SplineByteKeyFrame</returns>
        public override SplineByteKeyFrame Create(DeterministicRandom random)
        {
            SplineByteKeyFrame splineByteKeyFrame = new SplineByteKeyFrame();
            splineByteKeyFrame.KeySpline = KeySpline;
            ApplyByteKeyFrameProperties(splineByteKeyFrame, random);

            return splineByteKeyFrame;
        }

        #endregion
    }
}
