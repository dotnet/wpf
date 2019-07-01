// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary/>
    internal abstract class TimelineFactory<TimelineType> : DiscoverableFactory<TimelineType> where TimelineType : Timeline
    {
        #region Public Members

        public RepeatBehavior RepeatBehavior { get; set; }

        #endregion

        #region Protected Members

        protected void ApplyTimelineProperties(Timeline timeline, DeterministicRandom random)
        {
            //The sum of AccelerationRatio and DecelerationRatio must be less than or equal to 1.
            double accelerationRatio = random.NextDouble();
            double decelerationRatio = (1 - accelerationRatio) * random.NextDouble();

            timeline.AccelerationRatio = accelerationRatio;
            timeline.DecelerationRatio = decelerationRatio;
            timeline.AutoReverse = random.NextBool();
            timeline.Duration = TimeSpan.FromSeconds(random.Next(60));
            //Change value to (0,10];
            timeline.SpeedRatio = 10 * random.NextDouble();
            timeline.RepeatBehavior = RepeatBehavior;
        }

        #endregion
    }
}
