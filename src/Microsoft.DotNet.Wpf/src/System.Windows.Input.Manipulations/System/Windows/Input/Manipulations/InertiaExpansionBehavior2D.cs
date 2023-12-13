// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Describes desired expansion behavior of an inertia processor.
    /// </summary>
    public sealed class InertiaExpansionBehavior2D : InertiaParameters2D
    {
        private const string initialRadiusName = "InitialRadius";
        private const string desiredDecelerationName = "DesiredDeceleration";
        private const string desiredExpansionXName = "DesiredExpansionX";
        private const string desiredExpansionYName = "DesiredExpansionY";
        private const string initialVelocityXName = "InitialVelocityX";
        private const string initialVelocityYName = "InitialVelocityY";

        private float initialRadius = 1;
        private float desiredDeceleration = float.NaN;
        private float desiredExpansionX = float.NaN;
        private float desiredExpansionY = float.NaN;
        private float initialVelocityX = float.NaN;
        private float initialVelocityY = float.NaN;

        /// <summary>
        /// Gets or sets the initial average radius, in coordinate units.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is used by the inertia processor to calculate the scale factor for an element 
        /// that is expanding.
        /// </para>
        /// <para>
        /// For instance, if the 
        /// <strong><see cref="InitialVelocityX"/></strong> and  <strong><see cref="InitialVelocityY"/></strong>
        /// properties are set to 3.75, this informs the inertia processor that the starting expansion velocity is 3.75 
        /// coordinate units per millisecond along both axes (expansion must be proportional). 
        /// </para>
        /// <para>
        /// The scale factor associated with this rate of expansion depends upon the size of the element 
        /// that is expanding. For a small element, this expansion velocity represents a larger scale factor 
        /// than for a large element. You inform the inertia processor of the element size by setting the 
        /// <strong>InitialRadius</strong> property.
        /// </para>
        /// <para>The default value for the <strong>InitialRadius</strong> property is 1.0.</para>
        /// <para>Valid values are any finite number greater than or equal to 1.0.</para>
        /// </remarks>
        public float InitialRadius
        {
            get
            {
                return this.initialRadius;
            }
            set
            {
                CheckRadius(value, initialRadiusName);
                ProtectedChangeProperty(
                    () => value == this.initialRadius,
                    () => this.initialRadius = value,
                    initialRadiusName);
            }
        }

        /// <summary>
        /// Gets or sets the desired expansion deceleration, in coordinate units
        /// per millisecond squared.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property value is mutually exclusive with the
        /// <strong><see cref="DesiredExpansionX"/></strong>
        /// and 
        /// <strong><see cref="DesiredExpansionY"/></strong> properties;
        /// setting this property will set 
        /// <strong>DesiredExpansionX</strong>
        /// and 
        /// <strong>DesiredExpansionY</strong> to <strong>NaN</strong>.
        /// </para>
        /// <para>
        /// The default value for this property is <strong>NaN</strong>.
        /// </para>
        /// <para>
        /// This property cannot be set while the inertia processor is running; 
        /// otherwise, an exception is thrown.
        /// </para>
        /// </remarks>
        public float DesiredDeceleration
        {
            get { return this.desiredDeceleration; }
            set
            {
                Validations.CheckFiniteNonNegative(value, desiredDecelerationName);
                ProtectedChangeProperty(
                    () => value == this.desiredDeceleration,
                    () => { this.desiredDeceleration = value; this.desiredExpansionX = this.desiredExpansionY = float.NaN; },
                    desiredDecelerationName);
            }
        }

        /// <summary>
        /// Gets or sets the desired expansion along the x-axis, in coordinate units.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Expansion must be proportional. The value of this property must equal the value of the
        /// <strong><see cref="DesiredExpansionY"/></strong> property when the inertia processor starts;
        /// otherwise an exception is thrown.
        /// </para>
        /// <para>
        /// This property value is mutually exclusive with the
        /// <strong><see cref="DesiredDeceleration"/></strong> property;
        /// setting this property will set 
        /// <strong>DesiredDeceleration</strong> to <strong>NaN</strong>.
        /// </para>
        /// <para>
        /// The default value for this property is <strong>NaN</strong>.
        /// </para>
        /// <para>
        /// <strong>DesiredExpansionX</strong> must be a finite, non-negative number. 
        /// The rate of expansion is determined by the
        /// <strong><see cref="InitialVelocityX"/></strong> property.
        /// </para>
        /// <para>
        /// This property cannot be set while the inertia processor is running; 
        /// otherwise, an exception is thrown.
        /// </para>
        /// </remarks>
        public float DesiredExpansionX
        {
            get { return this.desiredExpansionX; }
            set
            {
                Validations.CheckFiniteNonNegative(value, desiredExpansionXName);
                ProtectedChangeProperty(
                    () => (value == this.desiredExpansionX),
                    () => { this.desiredExpansionX = value; this.desiredDeceleration = float.NaN; },
                    desiredExpansionXName);
            }
        }

        /// <summary>
        /// Gets or sets the desired expansion along the y-axis, in coordinate units.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Expansion must be proportional. The value of this property must equal the value of the
        /// <strong><see cref="DesiredExpansionX"/></strong> property when the inertia processor starts; 
        /// otherwise an exception is thrown.
        /// </para>
        /// <para>
        /// This property value is mutually exclusive with the
        /// <strong><see cref="DesiredDeceleration"/></strong> property;
        /// setting this property will set 
        /// <strong>DesiredDeceleration</strong> to <strong>NaN</strong>.
        /// </para>
        /// <para>
        /// The default value for this property is <strong>NaN</strong>.
        /// </para>
        /// <para>
        /// <strong>DesiredExpansionY</strong> must be a finite, non-negative number. 
        /// The rate of expansion is determined by the
        /// <strong><see cref="InitialVelocityY"/></strong> property.
        /// </para>
        /// <para>
        /// This property cannot be set while the inertia processor is running; 
        /// otherwise, an exception is thrown.
        /// </para>
        /// </remarks>
        public float DesiredExpansionY
        {
            get { return this.desiredExpansionY; }
            set
            {
                Validations.CheckFiniteNonNegative(value, desiredExpansionYName);
                ProtectedChangeProperty(
                    () => (value == this.desiredExpansionY),
                    () => { this.desiredExpansionY = value; this.desiredDeceleration = float.NaN; },
                    desiredExpansionYName);
            }
        }

        /// <summary>
        /// Gets or sets the initial expansion velocity along the x-axis, in coordinate
        /// units per millisecond.
        /// </summary>
        /// <remarks>  
        /// <para>
        /// Expansion must be proportional. The value of this property must equal the value of the
        /// <strong><see cref="InitialVelocityY"/></strong> property when the inertia processor starts;
        /// otherwise an exception is thrown.
        /// </para>
        /// <para>
        /// The default value for this property is <strong>NaN</strong>. 
        /// Leaving this property and <strong>InitialVelocityY</strong> unchanged from the default or 
        /// setting this property and <strong>InitialVelocityY</strong> to zero (0) 
        /// will disable expansion inertia.
        /// </para>
        /// <para>
        /// This property cannot be set while the inertia processor is running; 
        /// otherwise, an exception is thrown.
        /// </para>
        /// </remarks>
        public float InitialVelocityX
        {
            get { return this.initialVelocityX; }
            set
            {
                Validations.CheckFinite(value, initialVelocityXName);
                ProtectedChangeProperty(
                    () => value == this.initialVelocityX,
                    () => this.initialVelocityX = value,
                    initialVelocityXName);
            }
        }

        /// <summary>
        /// Gets or sets the initial expansion velocity along the y-axis, in coordinate
        /// units per millisecond.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Expansion must be proportional. The value of this property must equal the value of the
        /// <strong><see cref="InitialVelocityX"/></strong> property when the inertia processor starts;
        /// otherwise an exception is thrown.
        /// </para>
        /// <para>
        /// The default value for this property is <strong>NaN</strong>. 
        /// Leaving this property and <strong>InitialVelocityX</strong> unchanged from the default or 
        /// setting this property and <strong>InitialVelocityX</strong> to zero (0) 
        /// will disable expansion inertia.
        /// </para>
        /// <para>
        /// This property cannot be set while the inertia processor is running; 
        /// otherwise, an exception is thrown.
        /// </para>
        /// </remarks>
        public float InitialVelocityY
        {
            get { return this.initialVelocityY; }
            set
            {
                Validations.CheckFinite(value, initialVelocityYName);
                ProtectedChangeProperty(
                    () => value == this.initialVelocityY,
                    () => this.initialVelocityY = value,
                    initialVelocityYName);
            }
        }

        /// <summary>
        /// This is called when the inertia processor is about to start processing.
        /// It checks to make sure that the behavior is in a valid state. There are
        /// two possible valid states:
        /// 
        /// 1. Initial velocity is NaN, in which case expansion inertia will not
        ///    occur. Desired deceleration and expansion values are ignored.
        /// 
        /// 2. Initial velocity is set to something other than NaN, which means
        ///    that rotation inertia will occur. In this case, either the
        ///    desired deceleration or rotation values must be set. Also,
        ///    the desired X and Y expansion components (velocities, and
        ///    desired expansions if set) must equal each other.
        /// </summary>
        internal void CheckValid()
        {
            if (float.IsNaN(this.initialVelocityX) && float.IsNaN(this.initialVelocityY))
            {
                // Velocity is unspecified, so no inertia will occur. This is
                // a valid state.
                return;
            }

            // X and Y components must match.
            if (this.initialVelocityX != this.initialVelocityY)
            {
                throw Exceptions.OnlyProportionalExpansionSupported(
                    SubpropertyName(initialVelocityXName),
                    SubpropertyName(initialVelocityYName));
            }
            if (!float.IsNaN(this.desiredExpansionX)
                && !float.IsNaN(this.desiredExpansionY)
                && (this.desiredExpansionX != this.desiredExpansionY))
            {
                throw Exceptions.OnlyProportionalExpansionSupported(
                    SubpropertyName(desiredExpansionXName),
                    SubpropertyName(desiredExpansionYName));
            }

            // Must specify either deceleration or desired expansion.
            if (float.IsNaN(this.desiredDeceleration)
                && (float.IsNaN(this.desiredExpansionX) || float.IsNaN(this.desiredExpansionY)))
            {
                throw Exceptions.InertiaParametersUnspecified1and2(
                    SubpropertyName(desiredDecelerationName),
                    SubpropertyName(desiredExpansionXName),
                    SubpropertyName(desiredExpansionYName));
            }
        }

        private static string SubpropertyName(string paramName)
        {
            return "ExpansionBehavior." + paramName;
        }

        /// <summary>
        /// Checks if the given value is valid radius.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="paramName"></param>
        private static void CheckRadius(float value, string paramName)
        {
            if (value < 1 || double.IsInfinity(value) || double.IsNaN(value))
            {
                throw Exceptions.IllegialInertiaRadius(paramName, value);
            }
        }

    }
}
