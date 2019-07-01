// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class EasingByteKeyFrameFactory : ByteKeyFrameFactory<EasingByteKeyFrame>
    {
        #region Public Members

        public EasingFunctionBase EasingFunction { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new EasingByteKeyFrame</returns>
        public override EasingByteKeyFrame Create(DeterministicRandom random)
        {
            EasingByteKeyFrame easingByteKeyFrame = new EasingByteKeyFrame();
            easingByteKeyFrame.EasingFunction = EasingFunction;
            ApplyByteKeyFrameProperties(easingByteKeyFrame, random);

            return easingByteKeyFrame;
        }

        #endregion
    }
}
