// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//

namespace System.Windows.Media.Animation
{
    /// <summary>
    ///     This class implements an easing function that backs up before going to the destination.
    /// </summary>
    public class BackEase : EasingFunctionBase
    {
        public BackEase()
        {
        }

        /// <summary>
        /// Amplitude Property
        /// </summary>
        public static readonly DependencyProperty AmplitudeProperty =
            DependencyProperty.Register(
                    "Amplitude",
                    typeof(double),
                    typeof(BackEase),
                    new PropertyMetadata(1.0));

        /// <summary>
        /// Specifies how much the function will pull back
        /// </summary>
        public double Amplitude
        {
            get
            {
                return (double)GetValue(AmplitudeProperty);
            }
            set
            {
                SetValueInternal(AmplitudeProperty, value);
            }
       }

        protected override double EaseInCore(double normalizedTime)
        {
            double amp = Math.Max(0.0, Amplitude);
            return Math.Pow(normalizedTime, 3.0) - normalizedTime * amp * Math.Sin(Math.PI * normalizedTime);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new BackEase();
        }
    }
}
