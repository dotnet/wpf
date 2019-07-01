// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class Int16AnimationFactory : TimelineFactory<Int16Animation>
    {
        #region Public Members

        public short FromValue { get; set; }

        public short ByValue { get; set; }

        public short ToValue { get; set; }

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        public EasingFunctionBase EasingFunction { get; set; }

        #endregion

        #region Override Members

        public override Int16Animation Create(DeterministicRandom random)
        {
            Int16Animation int16Animation = new Int16Animation();
            if (random.NextBool())
            {
                int16Animation.From = FromValue;
            }

            if (random.NextBool())
            {
                int16Animation.To = ToValue;
            }

            if (random.NextBool())
            {
                int16Animation.By = ByValue;
            }

            int16Animation.IsAdditive = IsAdditive;
            int16Animation.IsCumulative = IsCumulative;
            int16Animation.EasingFunction = EasingFunction;
            ApplyTimelineProperties(int16Animation, random);

            return int16Animation;
        }

        #endregion
    }
}
