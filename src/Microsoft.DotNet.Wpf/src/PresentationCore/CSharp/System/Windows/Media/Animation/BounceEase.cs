// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//

using MS.Internal;

namespace System.Windows.Media.Animation
{
    /// <summary>
    ///     This class implements an easing function that can be used to simulate bouncing
    /// </summary>
    public class BounceEase : EasingFunctionBase
    {
        public BounceEase()
        {
}

        /// <summary>
        /// Bounces Property
        /// </summary>
        public static readonly DependencyProperty BouncesProperty =
            DependencyProperty.Register(
                    "Bounces",
                    typeof(int),
                    typeof(BounceEase),
                    new PropertyMetadata(3));

        /// <summary>
        /// Specifies the number of bounces.  This does not include the final half bounce.
        /// </summary>
        public int Bounces
        {
            get
            {
                return (int)GetValue(BouncesProperty);
            }
            set
            {
                SetValueInternal(BouncesProperty, value);
            }
        }

        /// <summary>
        /// Bounciness Property
        /// </summary>
        public static readonly DependencyProperty BouncinessProperty =
            DependencyProperty.Register(
                    "Bounciness",
                    typeof(double),
                    typeof(BounceEase),
                    new PropertyMetadata(2.0));

        /// <summary>
        ///     Specifies the amount of bounciness.  This corresponds to the scale difference between a bounce and the next bounce.  
        ///     For example, Bounciness = 2.0 correspondes to the next bounce being twices as high and taking twice as long.
        /// </summary>
        public double Bounciness
        {
            get
            {
                return (double)GetValue(BouncinessProperty);
            }
            set
            {
                SetValueInternal(BouncinessProperty, value);
            }
        }

        protected override double EaseInCore(double normalizedTime)
        {
            // The math below is complicated because we have a few requirements to get the correct look for bounce:
            //  1) The bounces should be symetrical
            //  2) Bounciness should control both the amplitude and the period of the bounces
            //  3) Bounces should control the number of bounces without including the final half bounce to get you back to 1.0
            //
            //  Note: Simply modulating a expo or power curve with a abs(sin(...)) wont work because it violates 1) above.
            //

            // Constants
            double bounces = Math.Max(0.0, (double)Bounces);
            double bounciness = Bounciness;

            // Clamp the bounciness so we dont hit a divide by zero
            if (bounciness < 1.0 || DoubleUtil.IsOne(bounciness))
            {
                // Make it just over one.  In practice, this will look like 1.0 but avoid divide by zeros.
                bounciness = 1.001;
            }

            double pow = Math.Pow(bounciness, bounces);
            double oneMinusBounciness = 1.0 - bounciness;
            
            // 'unit' space calculations.
            // Our bounces grow in the x axis exponentially.  we define the first bounce as having a 'unit' width of 1.0 and compute
            // the total number of 'units' using a geometric series.
            // We then compute which 'unit' the current time is in.
            double sumOfUnits = (1.0 - pow) / oneMinusBounciness + pow * 0.5; // geometric series with only half the last sum
            double unitAtT = normalizedTime * sumOfUnits;

            // 'bounce' space calculations.
            // Now that we know which 'unit' the current time is in, we can determine which bounce we're in by solving the geometric equation:
            // unitAtT = (1 - bounciness^bounce) / (1 - bounciness), for bounce.
            double bounceAtT = Math.Log(-unitAtT * (1.0-bounciness) + 1.0, bounciness);
            double start = Math.Floor(bounceAtT);
            double end = start + 1.0;

            // 'time' space calculations.
            // We then project the start and end of the bounce into 'time' space
            double startTime = (1.0 - Math.Pow(bounciness, start)) / (oneMinusBounciness * sumOfUnits);
            double endTime = (1.0 - Math.Pow(bounciness, end)) / (oneMinusBounciness * sumOfUnits);

            // Curve fitting for bounce.
            double midTime = (startTime + endTime) * 0.5;
            double timeRelativeToPeak = normalizedTime - midTime;
            double radius = midTime - startTime;
            double amplitude = Math.Pow(1.0 / bounciness, (bounces - start));

            // Evaluate a quadratic that hits (startTime,0), (endTime, 0), and peaks at amplitude.
            return (-amplitude / (radius * radius)) * (timeRelativeToPeak - radius) * (timeRelativeToPeak + radius);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new BounceEase();
        }
    }
}
