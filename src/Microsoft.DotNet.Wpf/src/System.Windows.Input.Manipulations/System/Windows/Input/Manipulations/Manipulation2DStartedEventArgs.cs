// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Diagnostics;

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Represents data that is sent with a
    /// <strong><see cref="System.Windows.Input.Manipulations.ManipulationProcessor2D.Started">ManipulationProcessor2D.Started</see></strong>
    /// event.
    /// </summary>
    /// <example>
    /// <para>
    /// In the following example, an event handler for the 
    /// <strong><see cref="System.Windows.Input.Manipulations.ManipulationProcessor2D.Started">ManipulationProcessor2D.Started</see></strong>
    /// event checks to see if inertia processing is running and if so, stops it.
    /// </para>
    /// <code lang="cs">
    ///  <code source="MPIP\ManipulationAPI\ManipulationItem.xaml.cs" region="OnManipulationStarted"/>
    ///  <code source="MPIP\ManipulationAPI\ManipulationItem.xaml.cs" region="Timestamp"/>
    /// </code>
    /// </example>
    public class Manipulation2DStartedEventArgs: EventArgs
    {
        private readonly float originX;
        private readonly float originY;

        /// <summary>
        /// Creates a new Manipulation2DStartedEventArgs object with
        /// the specified properties.
        /// </summary>
        /// <param name="originX">the x coordinate of the origin</param>
        /// <param name="originY">the y coordinate of the origin</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when either originX or
        /// originY are set to float.PositiveInfinity, float.NegativeInfinity, or float.NaN,
        /// which are all invalid values.</exception>
        internal Manipulation2DStartedEventArgs(float originX, float originY)
        {
            Debug.Assert(Validations.IsFinite(originX));
            Debug.Assert(Validations.IsFinite(originY));
            this.originX = originX;
            this.originY = originY;
        }

        /// <summary>
        /// Gets the x-coordinate of the origin.
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
        /// Gets the y-coordinate of the origin.
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
    }
}
