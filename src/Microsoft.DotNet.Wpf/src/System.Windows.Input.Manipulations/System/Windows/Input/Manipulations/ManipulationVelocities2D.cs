// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Represents a set of velocities calculated by the manipulation and inertia processors.
    /// </summary>
    /// <remarks>
    /// A <strong>ManipulationVelocities2D</strong> object is used in the event arguments of
    /// <strong><see cref="T:System.Windows.Input.Manipulations.Manipulation2DDeltaEventArgs"/></strong>
    /// and
    /// <strong><see cref="T:System.Windows.Input.Manipulations.Manipulation2DCompletedEventArgs"/></strong>
    /// to inform the event handler of the velocities involved in the manipulation.
    /// </remarks>
    public class ManipulationVelocities2D
    {
        private readonly Lazy<float> linearVelocityX;
        private readonly Lazy<float> linearVelocityY;
        private readonly Lazy<float> angularVelocity;
        private readonly Lazy<float> expansionVelocity;

        /// <summary>
        /// Gets a ManipulationVelocities2D with all velocities set to zero.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification="ManipulationVelocities2D is immutable.")]
        public static readonly ManipulationVelocities2D Zero = new ManipulationVelocities2D(0, 0, 0, 0);

        /// <summary>
        /// Gets the velocity along the x-axis, in coordinate units per millisecond.
        /// </summary>
        public float LinearVelocityX
        {
            get { return this.linearVelocityX.Value; }
        }

        /// <summary>
        /// Gets the velocity along the y-axis, in coordinate units per millisecond.
        /// </summary>
        public float LinearVelocityY
        {
            get { return this.linearVelocityY.Value; }
        }

        /// <summary>
        /// Gets the angular velocity, in radians per millisecond.
        /// </summary>
        public float AngularVelocity
        {
            get { return this.angularVelocity.Value; }
        }

        /// <summary>
        /// Gets the expansion velocity along the x-axis, in coordinate
        /// units per millisecond.
        /// </summary>
        public float ExpansionVelocityX
        {
            get { return this.expansionVelocity.Value; }
        }

        /// <summary>
        /// Gets the expansion velocity along the y-axis, in coordinate
        /// units per millisecond.
        /// </summary>
        public float ExpansionVelocityY
        {
            get { return this.expansionVelocity.Value; }
        }

        /// <summary>
        /// Internal constructor that takes explicit (non-lazily-calculated) values.
        /// </summary>
        /// <param name="linearVelocityX"></param>
        /// <param name="linearVelocityY"></param>
        /// <param name="angularVelocity"></param>
        /// <param name="expansionVelocity"></param>
        internal ManipulationVelocities2D(
            float linearVelocityX,
            float linearVelocityY,
            float angularVelocity,
            float expansionVelocity)
        {
            Debug.Assert(Validations.IsFinite(linearVelocityX));
            Debug.Assert(Validations.IsFinite(linearVelocityY));
            Debug.Assert(Validations.IsFinite(angularVelocity));
            Debug.Assert(Validations.IsFinite(expansionVelocity));

            this.linearVelocityX = new Lazy<float>(linearVelocityX);
            this.linearVelocityY = new Lazy<float>(linearVelocityY);
            this.angularVelocity = new Lazy<float>(angularVelocity);
            this.expansionVelocity = new Lazy<float>(expansionVelocity);
        }

        /// <summary>
        /// Internal constructor that allows for lazy evaluation of velocities.
        /// </summary>
        /// <param name="getLinearVelocityX"></param>
        /// <param name="getLinearVelocityY"></param>
        /// <param name="getAngularVelocity"></param>
        /// <param name="getExpansionVelocity"></param>
        internal ManipulationVelocities2D(
            Func<float> getLinearVelocityX,
            Func<float> getLinearVelocityY,
            Func<float> getAngularVelocity,
            Func<float> getExpansionVelocity)
        {
            Debug.Assert(getLinearVelocityX != null, "getLinearVelocityX should not be null");
            Debug.Assert(getLinearVelocityY != null, "getLinearVelocityX should not be null");
            Debug.Assert(getAngularVelocity != null, "getLinearVelocityX should not be null");
            Debug.Assert(getExpansionVelocity != null, "getLinearVelocityX should not be null");

            this.linearVelocityX = new Lazy<float>(getLinearVelocityX);
            this.linearVelocityY = new Lazy<float>(getLinearVelocityY);
            this.angularVelocity = new Lazy<float>(getAngularVelocity);
            this.expansionVelocity = new Lazy<float>(getExpansionVelocity);
        }
}
}
