// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class ColorAnimationFactory : TimelineFactory<ColorAnimation>
    {
        #region Public Members

        public Color ByValue { get; set; }

        public Color FromValue { get; set; }

        public Color ToValue { get; set; }

        #endregion

        #region Override Members

        public override ColorAnimation Create(DeterministicRandom random)
        {
            ColorAnimation colorAnimation = new ColorAnimation();

            /*
             *Randomly combinate From, To, By 
             */
            if (random.NextBool())
            {
                colorAnimation.By = ByValue;
            }

            if (random.NextBool())
            {
                colorAnimation.From = FromValue;
            }

            if (random.NextBool())
            {
                colorAnimation.To = ToValue;
            }

            colorAnimation.IsAdditive = random.NextBool();
            colorAnimation.IsCumulative = random.NextBool();

            ApplyTimelineProperties(colorAnimation, random);

            return colorAnimation;
        }

        #endregion
    }
}
