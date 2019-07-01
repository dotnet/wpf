// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class QuaternionAnimationFactory : TimelineFactory<QuaternionAnimation>
    {
        #region Public Members

        public Quaternion FromValue { get; set; }

        public Quaternion ByValue { get; set; }

        public Quaternion ToValue { get; set; }

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        public bool UseShortestPath { get; set; }

        public EasingFunctionBase EasingFunction { get; set; }

        #endregion

        #region Override Members

        public override QuaternionAnimation Create(DeterministicRandom random)
        {
            QuaternionAnimation quaternionAnimation = new QuaternionAnimation();
            if (random.NextBool())
            {
                quaternionAnimation.From = FromValue;
            }

            if (random.NextBool())
            {
                quaternionAnimation.To = ToValue;
            }

            if (random.NextBool())
            {
                quaternionAnimation.By = ByValue;
            }

            quaternionAnimation.IsAdditive = IsAdditive;
            quaternionAnimation.IsCumulative = IsCumulative;
            quaternionAnimation.EasingFunction = EasingFunction;
            quaternionAnimation.UseShortestPath = UseShortestPath;
            ApplyTimelineProperties(quaternionAnimation, random);

            return quaternionAnimation;
        }

        #endregion
    }
}
