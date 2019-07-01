// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class Int64AnimationFactory : TimelineFactory<Int64Animation>
    {
        #region Public Members

        public long FromValue { get; set; }

        public long ByValue { get; set; }

        public long ToValue { get; set; }

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        public EasingFunctionBase EasingFunction { get; set; }

        #endregion

        #region Override Members

        public override Int64Animation Create(DeterministicRandom random)
        {
            Int64Animation int64Animation = new Int64Animation();
            if (random.NextBool())
            {
                int64Animation.From = FromValue;
            }

            if (random.NextBool())
            {
                int64Animation.To = ToValue;
            }

            if (random.NextBool())
            {
                int64Animation.By = ByValue;
            }

            int64Animation.IsAdditive = IsAdditive;
            int64Animation.IsCumulative = IsCumulative;
            int64Animation.EasingFunction = EasingFunction;
            ApplyTimelineProperties(int64Animation, random);

            return int64Animation;
        }

        #endregion
    }
}
