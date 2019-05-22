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
    public class InertiaExpansionBehavior
    {
        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        public InertiaExpansionBehavior()
        {
        }

        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        internal InertiaExpansionBehavior(Vector initialVelocity)
        {
            _initialVelocity = initialVelocity;
        }

        /// <summary>
        ///     The initial rate of change of size of the element at the start of the inertia phase.
        /// </summary>
        public Vector InitialVelocity
        {
            get { return _initialVelocity; }
            set
            {
                _isInitialVelocitySet = true;
                _initialVelocity = value;
            }
        }

        /// <summary>
        ///     The desired rate of change of velocity.
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
                _isDesiredExpansionSet = false;
                _desiredExpansion = new Vector(double.NaN, double.NaN);
            }
        }

        /// <summary>
        ///     The desired total change in size.
        /// </summary>
        public Vector DesiredExpansion
        {
            get { return _desiredExpansion; }
            set
            {
                _isDesiredExpansionSet = true;
                _desiredExpansion = value;
                _isDesiredDecelerationSet = false;
                _desiredDeceleration = double.NaN;
            }
        }

        public double InitialRadius
        {
            get { return _initialRadius; }
            set
            {
                _isInitialRadiusSet = true;
                _initialRadius = value;
            }
        }

        internal bool CanUseForInertia()
        {
            return _isInitialVelocitySet || _isInitialRadiusSet || _isDesiredDecelerationSet || _isDesiredExpansionSet;
        }

        internal static void ApplyParameters(InertiaExpansionBehavior behavior, InertiaProcessor2D processor, Vector initialVelocity)
        {
            if (behavior != null && behavior.CanUseForInertia())
            {
                InertiaExpansionBehavior2D behavior2D = new InertiaExpansionBehavior2D();
                if (behavior._isInitialVelocitySet)
                {
                    behavior2D.InitialVelocityX = (float)behavior._initialVelocity.X;
                    behavior2D.InitialVelocityY = (float)behavior._initialVelocity.Y;
                }
                else
                {
                    behavior2D.InitialVelocityX = (float)initialVelocity.X;
                    behavior2D.InitialVelocityY = (float)initialVelocity.Y;
                }
                if (behavior._isDesiredDecelerationSet)
                {
                    behavior2D.DesiredDeceleration = (float)behavior._desiredDeceleration;
                }
                if (behavior._isDesiredExpansionSet)
                {
                    behavior2D.DesiredExpansionX = (float)behavior._desiredExpansion.X;
                    behavior2D.DesiredExpansionY = (float)behavior._desiredExpansion.Y;
                }
                if (behavior._isInitialRadiusSet)
                {
                    behavior2D.InitialRadius = (float)behavior._initialRadius;
                }

                processor.ExpansionBehavior = behavior2D;
            }
        }

        private bool _isInitialVelocitySet;
        private Vector _initialVelocity = new Vector(double.NaN, double.NaN);
        private bool _isDesiredDecelerationSet;
        private double _desiredDeceleration = double.NaN;
        private bool _isDesiredExpansionSet;
        private Vector _desiredExpansion = new Vector(double.NaN, double.NaN);
        private bool _isInitialRadiusSet;
        private double _initialRadius = 1.0;
    }
}
