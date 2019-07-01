// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class EasingInt16KeyFrameFactory : Int16KeyFrameFactory<EasingInt16KeyFrame>
    {
        #region Public Members

        public EasingFunctionBase EasingFunction { get; set; }

        #endregion

        #region Override Members

        public override EasingInt16KeyFrame Create(DeterministicRandom random)
        {
            EasingInt16KeyFrame easingInt16KeyFrame = new EasingInt16KeyFrame();
            easingInt16KeyFrame.EasingFunction = EasingFunction;
            ApplyInt16KeyFrameProperties(easingInt16KeyFrame, random);
            return easingInt16KeyFrame;
        }

        #endregion
    }
}
