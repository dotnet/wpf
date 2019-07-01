// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class VectorAnimationFactory : TimelineFactory<VectorAnimation>
    {
        #region Public Members

        public Vector FromValue { get; set; }

        public Vector ByValue { get; set; }

        public Vector ToValue { get; set; }

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        public EasingFunctionBase EasingFunction { get; set; }

        #endregion

        #region Override Members

        public override VectorAnimation Create(DeterministicRandom random)
        {
            VectorAnimation vectorAnimation = new VectorAnimation();
            if (random.NextBool())
            {
                vectorAnimation.From = FromValue;
            }

            if (random.NextBool())
            {
                vectorAnimation.To = ToValue;
            }

            if (random.NextBool())
            {
                vectorAnimation.By = ByValue;
            }

            vectorAnimation.IsAdditive = IsAdditive;
            vectorAnimation.IsCumulative = IsCumulative;
            vectorAnimation.EasingFunction = EasingFunction;
            ApplyTimelineProperties(vectorAnimation, random);

            return vectorAnimation;
        }

        #endregion
    }
}
