// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Windows;

namespace System.Windows.Input
{
    public class ManipulationDelta
    {
        /// <summary>
        ///     Creates a new instance of this object.
        /// </summary>
        public ManipulationDelta(Vector translation, double rotation, Vector scale, Vector expansion)
        {
            Translation = translation;
            Rotation = rotation;
            Scale = scale;
            Expansion = expansion;
        }

        /// <summary>
        ///     Amount of change in position.
        ///     Unit: Device-independent pixels
        /// </summary>
        public Vector Translation
        {
            get;
            private set;
        }

        /// <summary>
        ///     Amount of change in orientation.
        ///     Unit: Angles (clockwise)
        /// </summary>
        public double Rotation
        {
            get;
            private set;
        }

        /// <summary>
        ///     Amount of change in size.
        ///     Unit: Factors in each dimension (1.0, 1.0 means no change)
        /// </summary>
        public Vector Scale
        {
            get;
            private set;
        }

        /// <summary>
        ///     Amount of change to the radius' size.
        ///     Unit: Device-independent pixels (0.0 means no change)
        /// </summary>
        public Vector Expansion
        {
            get;
            private set;
        }
    }
}
