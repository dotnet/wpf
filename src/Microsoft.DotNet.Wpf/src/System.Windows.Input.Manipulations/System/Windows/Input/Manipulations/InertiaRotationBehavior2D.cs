// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Describes desired rotation behavior of an inertia processor.
    /// </summary>
    public sealed class InertiaRotationBehavior2D : InertiaParameters2D
    {
        private const string desiredDecelerationName = "DesiredDeceleration";
        private const string desiredRotationName = "DesiredRotation";
        private const string initialVelocityName = "InitialVelocity";

        private float desiredDeceleration = float.NaN;
        private float desiredRotation = float.NaN;
        private float initialVelocity = float.NaN;

        /// <summary>
        /// Sets the desired angular deceleration, in radians per millisecond squared.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property represents the desired angular deceleration to be used during inertia operation.
        /// This property value is mutually exclusive with the 
        /// <strong><see cref="DesiredRotation"/></strong> property;
        /// setting this property will set <strong>DesiredRotation</strong> to <strong>NaN</strong>.
        /// The default value for both this property and
        /// <strong>DesiredRotation</strong> is <strong>NaN</strong>.
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
                    () => { this.desiredDeceleration = value; this.desiredRotation = float.NaN; },
                    desiredDecelerationName);
            }
        }

        /// <summary>
        /// Sets the desired rotation, in radians.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property represents the desired ending rotation of the inertia operation.
        /// This property value is mutually exclusive with the 
        /// <strong><see cref="DesiredDeceleration"/></strong> property;
        /// setting this property will set <strong>DesiredDeceleration</strong>
        /// to <strong>NaN</strong>.
        /// The default value for both this property and
        /// <strong>DesiredDeceleration</strong> is <strong>NaN</strong>.
        /// You must set one or the other property before inertia processing starts.
        /// </para>
        /// <para>
        /// <strong>DesiredRotation</strong> must be a finite, non-negative number.
        /// The direction of rotation is determined by the 
        /// <strong><see cref="InitialVelocity"/></strong> property.
        /// </para>
        /// <para>
        /// This property cannot be set while the inertia processor is running; 
        /// otherwise, an exception is thrown.
        /// </para>
        /// </remarks>
        /// <example>
        /// In the following example, the <strong>DesiredRotation</strong>
        /// property is set to enable inertia processing to rotate an object 
        /// three-and-one-half times from its starting orientation.
        /// <code source="MPIP\ManipulationAPI\ManipulationItem.xaml.cs" region="SetDesiredRotation"/>
        /// </example>
        public float DesiredRotation
        {
            get { return this.desiredRotation; }
            set
            {
                Validations.CheckFiniteNonNegative(value, desiredRotationName);
                ProtectedChangeProperty(
                    () => value == this.desiredRotation,
                    () => { this.desiredRotation = value; this.desiredDeceleration = float.NaN; },
                    desiredRotationName);
            }
        }

        /// <summary>
        /// Gets or sets the initial rotational velocity, in radians
        /// per millisecond.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default value for this property is <strong>NaN</strong>. 
        /// Leaving this property unchanged from the default or setting this property to zero (0) 
        /// will disable rotational inertia.
        /// </para>
        /// <para>
        /// This property cannot be set while the inertia processor is running; 
        /// otherwise, an exception is thrown.
        /// </para>
        /// </remarks>
        public float InitialVelocity
        {
            get { return this.initialVelocity; }
            set
            {
                Validations.CheckFinite(value, initialVelocityName);
                ProtectedChangeProperty(
                    () => value == this.initialVelocity,
                    () => this.initialVelocity = value,
                    initialVelocityName);
            }
        }

        /// <summary>
        /// This is called when the inertia processor is about to start processing.
        /// It checks to make sure that the behavior is in a valid state. There are
        /// two possible valid states:
        /// 
        /// 1. Initial velocity is NaN, in which case rotation inertia will not
        ///    occur. Desired deceleration and rotation values are ignored.
        /// 
        /// 2. Initial velocity is set to something other than NaN, which means
        ///    that rotation inertia will occur. In this case, either the
        ///    desired deceleration or rotation values must be set.
        /// </summary>
        internal void CheckValid()
        {
            if (float.IsNaN(this.initialVelocity))
            {
                // Velocity is unspecified, so no inertia will occur. This is
                // a valid state.
                return;
            }

            if (float.IsNaN(this.desiredDeceleration) && float.IsNaN(this.desiredRotation))
            {
                throw Exceptions.InertiaParametersUnspecified2(
                    SubpropertyName(desiredDecelerationName),
                    SubpropertyName(desiredRotationName));
            }
        }

        private static string SubpropertyName(string paramName)
        {
            return "RotationBehavior." + paramName;
        }
    }
}
