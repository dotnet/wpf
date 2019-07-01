// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class DecimalAnimationFactory : TimelineFactory<DecimalAnimation>
    {
        #region Public Members

        public decimal FromValue { get; set; }

        public decimal ByValue { get; set; }

        public decimal ToValue { get; set; }

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        public EasingFunctionBase EasingFunction { get; set; }

        #endregion

        #region Override Members

        public override DecimalAnimation Create(DeterministicRandom random)
        {
            DecimalAnimation decimalAnimation = new DecimalAnimation();
            if (random.NextBool())
            {
                decimalAnimation.From = FromValue;
            }

            if (random.NextBool())
            {
                decimalAnimation.To = ToValue;
            }

            if (random.NextBool())
            {
                decimalAnimation.By = ByValue;
            }

            decimalAnimation.IsAdditive = IsAdditive;
            decimalAnimation.IsCumulative = IsCumulative;
            decimalAnimation.EasingFunction = EasingFunction;
            ApplyTimelineProperties(decimalAnimation, random);

            return decimalAnimation;
        }

        #endregion
    }
}
