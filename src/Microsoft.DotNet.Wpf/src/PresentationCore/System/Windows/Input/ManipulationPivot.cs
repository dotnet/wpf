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
    ///     Data regarding a pivot associated with a manipulation.
    /// </summary>
    public class ManipulationPivot
    {
        /// <summary>
        ///     Initializes a new instance of this object.
        /// </summary>
        public ManipulationPivot()
        {
        }

        /// <summary>
        ///     Initializes a new instance of this object.
        /// </summary>
        public ManipulationPivot(Point center, double radius)
        {
            Center = center;
            Radius = radius;
        }

        /// <summary>
        ///     Location of a pivot.
        /// </summary>
        public Point Center
        {
            get;
            set;
        }

        /// <summary>
        ///     The area that is considered "close" to the pivot.
        ///     Movement within this area will dampen the effect of rotation.
        /// </summary>
        public double Radius
        {
            get;
            set;
        }
    }
}
