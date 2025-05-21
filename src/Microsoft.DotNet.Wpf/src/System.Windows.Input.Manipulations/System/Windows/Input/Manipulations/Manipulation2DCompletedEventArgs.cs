// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Diagnostics;

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Represents data that is sent with a 
    /// <strong><see cref="System.Windows.Input.Manipulations.ManipulationProcessor2D.Completed">ManipulationProcessor2D.Completed</see></strong> 
    /// event or an
    /// <strong><see cref="E:System.Windows.Input.Manipulations.InertiaProcessor2D.Completed">InertiaProcessor2D.Completed</see></strong> 
    /// event.
    /// </summary>
    public class Manipulation2DCompletedEventArgs: EventArgs
    {
        private readonly float originX;
        private readonly float originY;
        private readonly ManipulationVelocities2D velocities;
        private readonly ManipulationDelta2D total;

        /// <summary>
        /// Creates a new Manipulation2DCompletedEventArgs object with
        /// the specified properties.
        /// </summary>
        /// <param name="originX">the x coordinate of the composite position of the manipulation</param>
        /// <param name="originY">the y coordinate of the composite position of the manipulation</param>
        /// <param name="velocities">velocities</param>
        /// <param name="total">total change since operation began</param>
        internal Manipulation2DCompletedEventArgs(
            float originX,
            float originY,
            ManipulationVelocities2D velocities,
            ManipulationDelta2D total)
        {
            Debug.Assert(Validations.IsFinite(originX), "originX should be finite");
            Debug.Assert(Validations.IsFinite(originY), "originY should be finite");
            Debug.Assert(velocities != null, "velocities should not be null");
            Debug.Assert(total != null, "total should not be null");
            this.originX = originX;
            this.originY = originY;
            this.velocities = velocities;
            this.total = total;
        }

        /// <summary>
        /// Gets the new x-coordinate of the composite position of the manipulation.
        /// </summary>
        /// <remarks>
        /// The origin point represented by the <strong>OriginX</strong> and
        /// <strong><see cref="OriginY"/></strong>
        /// properties is the average position of all manipulators associated with an element. 
        /// </remarks>
        public float OriginX
        {
            get
            {
                return this.originX;
            }
        }

        /// <summary>
        /// Gets the new y-coordinate of the composite position of the manipulation.
        /// </summary>
        /// <remarks>
        /// The origin point represented by the <strong><see cref="OriginX"/></strong> and
        /// <strong>OriginY</strong>
        /// properties is the average position of all manipulators associated with an element. 
        /// </remarks>
        public float OriginY
        {
            get
            {
                return this.originY;
            }
        }

        /// <summary>
        /// Gets the current velocities of the manipulation.
        /// </summary>
        public ManipulationVelocities2D Velocities
        {
            get
            {
                return this.velocities;
            }
        }

        /// <summary>
        /// Gets the total amount of change since the manipulation started.
        /// </summary>
        public ManipulationDelta2D Total
        {
            get
            {
                return this.total;
            }
        }
    }
}
