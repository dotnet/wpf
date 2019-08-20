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
    public class InertiaTranslationBehavior
    {
        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        public InertiaTranslationBehavior()
        {
        }

        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        internal InertiaTranslationBehavior(Vector initialVelocity)
        {
            _initialVelocity = initialVelocity;
        }

        /// <summary>
        ///     The initial rate of change of position of the element at the start of the inertia phase.
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
                _isDesiredDisplacementSet = false;
                _desiredDisplacement = double.NaN;
            }
        }

        /// <summary>
        ///     The desired total change in position.
        /// </summary>
        public double DesiredDisplacement
        {
            get { return _desiredDisplacement; }
            set
            {
                if (Double.IsInfinity(value) || Double.IsNaN(value))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _isDesiredDisplacementSet = true;
                _desiredDisplacement = value;
                _isDesiredDecelerationSet = false;
                _desiredDeceleration = double.NaN;
            }
        }

        internal bool CanUseForInertia()
        {
            return _isInitialVelocitySet || _isDesiredDecelerationSet || _isDesiredDisplacementSet;
        }

        internal static void ApplyParameters(InertiaTranslationBehavior behavior, InertiaProcessor2D processor, Vector initialVelocity)
        {
            if (behavior != null && behavior.CanUseForInertia())
            {
                InertiaTranslationBehavior2D behavior2D = new InertiaTranslationBehavior2D();
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
                if (behavior._isDesiredDisplacementSet)
                {
                    behavior2D.DesiredDisplacement = (float)behavior._desiredDisplacement;
                }

                processor.TranslationBehavior = behavior2D;
            }
        }

        private bool _isInitialVelocitySet;
        private Vector _initialVelocity = new Vector(double.NaN, double.NaN);
        private bool _isDesiredDecelerationSet;
        private double _desiredDeceleration = double.NaN;
        private bool _isDesiredDisplacementSet;
        private double _desiredDisplacement = double.NaN;
    }
}
