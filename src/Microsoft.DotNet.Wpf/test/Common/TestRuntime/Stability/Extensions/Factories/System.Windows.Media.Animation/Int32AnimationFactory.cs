// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class Int32AnimationFactory : TimelineFactory<Int32Animation>
    {
        #region Public Members

        public int FromValue { get; set; }

        public int ByValue { get; set; }

        public int ToValue { get; set; }

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        public EasingFunctionBase EasingFunction { get; set; }

        #endregion

        #region Override Members

        public override Int32Animation Create(DeterministicRandom random)
        {
            Int32Animation int32Animation = new Int32Animation();
            if (random.NextBool())
            {
                int32Animation.From = FromValue;
            }

            if (random.NextBool())
            {
                int32Animation.To = ToValue;
            }

            if (random.NextBool())
            {
                int32Animation.By = ByValue;
            }

            int32Animation.IsAdditive = IsAdditive;
            int32Animation.IsCumulative = IsCumulative;
            int32Animation.EasingFunction = EasingFunction;
            ApplyTimelineProperties(int32Animation, random);

            return int32Animation;
        }

        #endregion
    }
}
