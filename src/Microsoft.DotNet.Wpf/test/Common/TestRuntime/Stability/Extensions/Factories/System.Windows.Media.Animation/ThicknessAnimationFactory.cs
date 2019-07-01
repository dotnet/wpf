// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class ThicknessAnimationFactory : TimelineFactory<ThicknessAnimation>
    {
        #region Public Members

        public Thickness FromValue { get; set; }

        public Thickness ByValue { get; set; }

        public Thickness ToValue { get; set; }

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        public EasingFunctionBase EasingFunction { get; set; }

        #endregion

        #region Override Members

        public override ThicknessAnimation Create(DeterministicRandom random)
        {
            ThicknessAnimation thicknessAnimation = new ThicknessAnimation();
            if (random.NextBool())
            {
                thicknessAnimation.From = FromValue;
            }

            if (random.NextBool())
            {
                thicknessAnimation.To = ToValue;
            }

            if (random.NextBool())
            {
                thicknessAnimation.By = ByValue;
            }

            thicknessAnimation.IsAdditive = IsAdditive;
            thicknessAnimation.IsCumulative = IsCumulative;
            thicknessAnimation.EasingFunction = EasingFunction;
            ApplyTimelineProperties(thicknessAnimation, random);

            return thicknessAnimation;
        }

        #endregion
    }
}
