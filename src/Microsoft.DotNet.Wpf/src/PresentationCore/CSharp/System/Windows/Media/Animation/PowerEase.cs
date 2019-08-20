// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//

namespace System.Windows.Media.Animation
{
    /// <summary>
    ///     This class implements an easing function that gives a polynomial curve of arbitrary degree.
    ///     If the curve you desire is cubic, quadratic, quartic, or quintic it is better to use the 
    ///     specialized easing functions.
    /// </summary>
    public class PowerEase : EasingFunctionBase
    {
        public PowerEase()
        {
}

        /// <summary>
        /// Power Property
        /// </summary>
        public static readonly DependencyProperty PowerProperty =
            DependencyProperty.Register(
                    "Power",
                    typeof(double),
                    typeof(PowerEase),
                    new PropertyMetadata(2.0));

        /// <summary>
        /// Specifies the power for the polynomial equation.
        /// </summary>
        public double Power
        {
            get
            {
                return (double)GetValue(PowerProperty);
            }
            set
            {
                SetValueInternal(PowerProperty, value);
            }
        }

        protected override double EaseInCore(double normalizedTime)
        {
            double power = Math.Max(0.0, Power);
            return Math.Pow(normalizedTime, power);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new PowerEase();
        }
    }
}
