// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class PointAnimationFactory : TimelineFactory<PointAnimation>
    {
        #region Public Members

        public Point FromValue { get; set; }

        public Point ByValue { get; set; }

        public Point ToValue { get; set; }

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        public EasingFunctionBase EasingFunction { get; set; }

        #endregion

        #region Override Members

        public override PointAnimation Create(DeterministicRandom random)
        {
            PointAnimation pointAnimation = new PointAnimation();
            if (random.NextBool())
            {
                pointAnimation.From = FromValue;
            }

            if (random.NextBool())
            {
                pointAnimation.To = ToValue;
            }

            if (random.NextBool())
            {
                pointAnimation.By = ByValue;
            }

            pointAnimation.IsAdditive = IsAdditive;
            pointAnimation.IsCumulative = IsCumulative;
            pointAnimation.EasingFunction = EasingFunction;
            ApplyTimelineProperties(pointAnimation, random);

            return pointAnimation;
        }

        #endregion
    }
}
