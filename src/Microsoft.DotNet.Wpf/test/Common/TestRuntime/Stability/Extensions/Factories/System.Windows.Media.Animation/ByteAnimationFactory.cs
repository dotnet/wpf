// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class ByteAnimationFactory : TimelineFactory<ByteAnimation>
    {
        #region Public Members

        public byte FromValue { get; set; }

        public byte ToValue { get; set; }

        public byte ByValue { get; set; }

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        public EasingFunctionBase EasingFunction { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new ByteAnimation</returns>
        public override ByteAnimation Create(DeterministicRandom random)
        {
            ByteAnimation byteAnimation = new ByteAnimation();
            byteAnimation.IsAdditive = IsAdditive;
            byteAnimation.IsCumulative = IsCumulative;
            byteAnimation.EasingFunction = EasingFunction;
            if (random.NextBool())
            {
                byteAnimation.From = FromValue;
            }

            if (random.NextBool())
            {
                byteAnimation.To = ToValue;
            }

            if (random.NextBool())
            {
                byteAnimation.By = ByValue;
            }

            ApplyTimelineProperties(byteAnimation, random);

            return byteAnimation;
        }

        #endregion
    }
}
