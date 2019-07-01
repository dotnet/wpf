// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class EasingInt32KeyFrameFactory : Int32KeyFrameFactory<EasingInt32KeyFrame>
    {
        #region Public Members

        public EasingFunctionBase EasingFunction { get; set; }

        #endregion

        #region Override Members

        public override EasingInt32KeyFrame Create(DeterministicRandom random)
        {
            EasingInt32KeyFrame easingInt32KeyFrame = new EasingInt32KeyFrame();
            easingInt32KeyFrame.EasingFunction = EasingFunction;
            ApplyInt32KeyFrameProperties(easingInt32KeyFrame, random);
            return easingInt32KeyFrame;
        }

        #endregion
    }
}
