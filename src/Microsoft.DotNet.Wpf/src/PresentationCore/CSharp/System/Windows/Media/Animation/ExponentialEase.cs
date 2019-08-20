// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//

using MS.Internal;

namespace System.Windows.Media.Animation
{
    /// <summary>
    ///     This class implements an easing function that gives an exponential curve
    /// </summary>
    public class ExponentialEase : EasingFunctionBase
    {
        public ExponentialEase()
        {
}

        /// <summary>
        /// Factor Property
        /// </summary>
        public static readonly DependencyProperty ExponentProperty =
            DependencyProperty.Register(
                    "Exponent",
                    typeof(double),
                    typeof(ExponentialEase),
                    new PropertyMetadata(2.0));

        /// <summary>
        /// Specifies the factor which controls the shape of easing.
        /// </summary>
        public double Exponent
        {
            get
            {
                return (double)GetValue(ExponentProperty);
            }
            set
            {
                SetValueInternal(ExponentProperty, value);
            }
        }

        protected override double EaseInCore(double normalizedTime)
        {
            double factor = Exponent;
            if (DoubleUtil.IsZero(factor))
            {
                return normalizedTime;
            }
            else
            {
                return (Math.Exp(factor * normalizedTime) - 1.0) / (Math.Exp(factor) - 1.0);
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new ExponentialEase();
        }
    }
}
