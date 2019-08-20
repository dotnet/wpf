// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Windows;

namespace System.Windows.Input
{
    /// <summary>
    ///     Data regarding a velocities associated with a manipulation for use with inertia.
    /// </summary>
    public class ManipulationVelocities
    {
        /// <summary>
        ///     Initializes a new instance of this object.
        /// </summary>
        public ManipulationVelocities(Vector linearVelocity, double angularVelocity, Vector expansionVelocity)
        {
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
            ExpansionVelocity = expansionVelocity;
        }

        /// <summary>
        ///     The rate of change of position.
        ///     Unit: Device-independent pixels per millisecond
        /// </summary>
        public Vector LinearVelocity
        {
            get;
            private set;
        }

        /// <summary>
        ///     The rate of change of orientation.
        ///     Unit: Degrees (clockwise) per millisecond
        /// </summary>
        public double AngularVelocity
        {
            get;
            private set;
        }

        /// <summary>
        ///     The rate of change of size.
        ///     Unit: Device-independent pixel per millisecond
        /// </summary>
        public Vector ExpansionVelocity
        {
            get;
            private set;
        }
    }
}
