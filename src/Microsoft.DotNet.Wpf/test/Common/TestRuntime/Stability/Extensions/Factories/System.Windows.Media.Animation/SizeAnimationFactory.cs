// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class SizeAnimationFactory : TimelineFactory<SizeAnimation>
    {
        #region Public Members

        public Size FromValue { get; set; }

        public Size ByValue { get; set; }

        public Size ToValue { get; set; }

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        public EasingFunctionBase EasingFunction { get; set; }

        #endregion

        #region Override Members

        public override SizeAnimation Create(DeterministicRandom random)
        {
            SizeAnimation sizeAnimation = new SizeAnimation();
            if (random.NextBool())
            {
                sizeAnimation.From = FromValue;
            }

            if (random.NextBool())
            {
                sizeAnimation.To = ToValue;
            }

            if (random.NextBool())
            {
                sizeAnimation.By = ByValue;
            }

            sizeAnimation.IsAdditive = IsAdditive;
            sizeAnimation.EasingFunction = EasingFunction;
            if (FromValue.Height > ToValue.Height || FromValue.Width > ToValue.Width)
            {
                sizeAnimation.IsCumulative = false;
            }
            else
            {
                sizeAnimation.IsCumulative = IsCumulative;
            }

            ApplyTimelineProperties(sizeAnimation, random);
            return sizeAnimation;
        }

        #endregion
    }
}
