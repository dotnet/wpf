// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Describes desired translation behavior of an inertia processor.
    /// </summary>
    public sealed class InertiaTranslationBehavior2D : InertiaParameters2D
    {
        private const string desiredDecelerationName = "DesiredDeceleration";
        private const string desiredDisplacementName = "DesiredDisplacement";
        private const string initialVelocityXName = "InitialVelocityX";
        private const string initialVelocityYName = "InitialVelocityY";

        private float desiredDeceleration = float.NaN;
        private float desiredDisplacement = float.NaN;
        private float initialVelocityX = float.NaN;
        private float initialVelocityY = float.NaN;

        /// <summary>
        /// Gets or sets the desired deceleration, in coordinate units per millisecond squared.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property value is mutually exclusive with the
        /// <strong><see cref="DesiredDisplacement"/></strong> property;
        /// setting this property will set <strong>DesiredDisplacement</strong>
        /// to <strong>NaN</strong>. The default value for both this property and
        /// <strong>DesiredDisplacement</strong> is <strong>NaN</strong>.
        /// You must set one or the other property before inertia processing starts.
        /// </para>
        /// <para>
        /// <strong>DesiredDeceleration</strong> must be a finite, non-negative number.
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
                    () => { this.desiredDeceleration = value; this.desiredDisplacement = float.NaN; },
                    desiredDecelerationName);
            }
        }

        /// <summary>
        /// Gets or sets the absolute distance that the object needs to travel along the velocity vector, 
        /// in coordinate units. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property value is mutually exclusive with the
        /// <strong><see cref="DesiredDeceleration"/></strong> property;
        /// setting this property will set <strong>DesiredDeceleration</strong>
        /// to <strong>NaN</strong>. The default value for both this property and
        /// <strong>DesiredDeceleration</strong> property is <strong>NaN</strong>.
        /// You must set one or the other property before inertia processing starts.
        /// </para>
        /// <para>
        /// <strong>DesiredDisplacement</strong> must be a finite, non-negative number. 
        /// The direction of displacement is along the 
        /// <strong><see cref="InitialVelocityX"/></strong> and 
        /// <strong><see cref="InitialVelocityY"/></strong> vector.
        /// </para>
        /// <para>
        /// This property cannot be set while the inertia processor is running; 
        /// otherwise, an exception is thrown.
        /// </para>
        /// </remarks>
        public float DesiredDisplacement
        {
            get { return this.desiredDisplacement; }
            set
            {
                Validations.CheckFiniteNonNegative(value, desiredDisplacementName);
                ProtectedChangeProperty(
                    () => value == this.desiredDisplacement,
                    () => { this.desiredDisplacement = value; this.desiredDeceleration = float.NaN; },
                    desiredDisplacementName);
            }
        }

        /// <summary>
        /// Gets or sets the initial velocity along the x-axis, in coordinate units
        /// per millisecond.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default value for this property is <strong>NaN</strong>. 
        /// Leaving this property unchanged from the default or setting this property to zero (0) 
        /// will disable translational inertia along the x-axis.
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
        /// Gets or sets the initial velocity along the x-axis, in coordinate units
        /// per millisecond.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default value for this property is <strong>NaN</strong>. 
        /// Leaving this property unchanged from the default or setting this property to zero (0) 
        /// will disable translational inertia along the y-axis.
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
        /// 1. Initial velocity is NaN, in which case translation inertia will not
        ///    occur. Desired deceleration and displacement values are ignored.
        /// 
        /// 2. Initial velocity is set to something other than NaN, which means
        ///    that translation inertia will occur. In this case, either the
        ///    desired deceleration or displacement values must be set.
        /// </summary>
        internal void CheckValid()
        {
            if (float.IsNaN(this.initialVelocityX) && float.IsNaN(this.initialVelocityY))
            {
                // Velocity is unspecified, so no inertia will occur. This is
                // a valid state.
                return;
            }

            if (float.IsNaN(this.desiredDeceleration) && float.IsNaN(this.desiredDisplacement))
            {
                throw Exceptions.InertiaParametersUnspecified2(
                    SubpropertyName(desiredDecelerationName),
                    SubpropertyName(desiredDisplacementName));
            }
        }

        private static string SubpropertyName(string propertyName)
        {
            return "TranslationBehavior." + propertyName;
        }
    }
}
