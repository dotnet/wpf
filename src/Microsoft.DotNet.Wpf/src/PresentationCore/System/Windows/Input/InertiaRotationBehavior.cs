// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input.Manipulations;

namespace System.Windows.Input
{
    /// <summary>
    ///     Provides information about the inertia behavior.
    /// </summary>
    public class InertiaRotationBehavior
    {
        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        public InertiaRotationBehavior()
        {
        }

        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        internal InertiaRotationBehavior(double initialVelocity)
        {
            _initialVelocity = initialVelocity;
        }

        /// <summary>
        ///     The initial rate of angular change of the element at the start of the inertia phase in degrees/ms.
        /// </summary>
        public double InitialVelocity
        {
            get { return _initialVelocity; }
            set
            {
                _isInitialVelocitySet = true;
                _initialVelocity = value;
            }
        }

        /// <summary>
        ///     The desired rate of change of velocity in degrees/ms^2.
        /// </summary>
        public double DesiredDeceleration
        {
            get { return _desiredDeceleration; }
            set
            {
                if (Double.IsInfinity(value) || Double.IsNaN(value))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _isDesiredDecelerationSet = true;
                _desiredDeceleration = value;
                _isDesiredRotationSet = false;
                _desiredRotation = double.NaN;
            }
        }

        /// <summary>
        ///     The desired total change in angle in degrees.
        /// </summary>
        public double DesiredRotation
        {
            get { return _desiredRotation; }
            set
            {
                if (Double.IsInfinity(value) || Double.IsNaN(value))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _isDesiredRotationSet = true;
                _desiredRotation = value;
                _isDesiredDecelerationSet = false;
                _desiredDeceleration = double.NaN;
            }
        }

        internal bool CanUseForInertia()
        {
            return _isInitialVelocitySet || _isDesiredDecelerationSet || _isDesiredRotationSet;
        }

        internal static void ApplyParameters(InertiaRotationBehavior behavior, InertiaProcessor2D processor, double initialVelocity)
        {
            if (behavior != null && behavior.CanUseForInertia())
            {
                InertiaRotationBehavior2D behavior2D = new InertiaRotationBehavior2D();

                if (behavior._isInitialVelocitySet)
                {
                    behavior2D.InitialVelocity = (float)AngleUtil.DegreesToRadians(behavior._initialVelocity);
                }
                else
                {
                    behavior2D.InitialVelocity = (float)AngleUtil.DegreesToRadians(initialVelocity);
                }
                if (behavior._isDesiredDecelerationSet)
                {
                    behavior2D.DesiredDeceleration = (float)AngleUtil.DegreesToRadians(behavior._desiredDeceleration);
                }
                if (behavior._isDesiredRotationSet)
                {
                    behavior2D.DesiredRotation = (float)AngleUtil.DegreesToRadians(behavior._desiredRotation);
                }

                processor.RotationBehavior = behavior2D;
            }
        }

        private bool _isInitialVelocitySet;
        private double _initialVelocity = double.NaN;
        private bool _isDesiredDecelerationSet;
        private double _desiredDeceleration = double.NaN;
        private bool _isDesiredRotationSet;
        private double _desiredRotation = double.NaN;
    }
}
