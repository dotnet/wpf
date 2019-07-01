// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
#if TESTBUILD_CLR40
    /// <summary>
    /// EasingFunction Factory. Creates a new or null Easing Function. Null Easing Function means, regular animation. 
    /// </summary>
    [TargetTypeAttribute(typeof(EasingFunctionBase))]
    class EasingFunctionFactory : DiscoverableFactory<EasingFunctionBase>
    {
        public override EasingFunctionBase Create(DeterministicRandom random)
        {
            EasingFunctionBase easingFunction;
            EasingMode easingMode;            
            int easingSwitch = random.Next(10);
            double amplitude1 = (double)random.NextDouble() * 10;
            double amplitude2 = (double)random.NextDouble() * 10;            

            easingMode = random.NextEnum<EasingMode>();

            switch (easingSwitch)
            {
                case 0:
                    easingFunction = new BackEase() { EasingMode = easingMode, Amplitude = amplitude1 };
                    break;
                case 1:
                    easingFunction = new BounceEase() { EasingMode = easingMode, Bounces = (int)amplitude1, Bounciness = amplitude2 };
                    break;
                case 2:
                    easingFunction = new CircleEase() { EasingMode = easingMode };
                    break;
                case 3:
                    easingFunction = new CubicEase() { EasingMode = easingMode };
                    break;
                case 4:
                    easingFunction = new ElasticEase() { EasingMode = easingMode, Oscillations = (int)amplitude1, Springiness = amplitude2 };
                    break;
                case 5:
                    easingFunction = new ExponentialEase() { EasingMode = easingMode, Exponent = amplitude1 };
                    break;
                case 6:
                    easingFunction = new PowerEase() { EasingMode = easingMode, Power = amplitude1 };
                    break;
                case 7:
                    easingFunction = new QuarticEase() { EasingMode = easingMode };
                    break;
                case 8:
                    easingFunction = new SineEase() { EasingMode = easingMode };
                    break;
                default:
                    return null;
            }
            return easingFunction;
        }
    }
#endif
}
