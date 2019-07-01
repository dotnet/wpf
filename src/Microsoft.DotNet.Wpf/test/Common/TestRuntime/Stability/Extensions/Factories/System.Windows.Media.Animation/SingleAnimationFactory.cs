// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class SingleAnimationFactory : TimelineFactory<SingleAnimation>
    {
        #region Public Members

        public float FromValue { get; set; }

        public float ByValue { get; set; }

        public float ToValue { get; set; }

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        public EasingFunctionBase EasingFunction { get; set; }

        #endregion

        #region Override Members

        public override SingleAnimation Create(DeterministicRandom random)
        {
            SingleAnimation singleAnimation = new SingleAnimation();
            if (random.NextBool())
            {
                singleAnimation.From = FromValue;
            }

            if (random.NextBool())
            {
                singleAnimation.To = ToValue;
            }

            if (random.NextBool())
            {
                singleAnimation.By = ByValue;
            }

            singleAnimation.IsAdditive = IsAdditive;
            singleAnimation.IsCumulative = IsCumulative;
            singleAnimation.EasingFunction = EasingFunction;
            ApplyTimelineProperties(singleAnimation, random);

            return singleAnimation;
        }

        #endregion
    }
}
