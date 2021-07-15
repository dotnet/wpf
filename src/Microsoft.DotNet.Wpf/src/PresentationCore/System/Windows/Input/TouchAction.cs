// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;

namespace System.Windows.Input
{
    /// <summary>
    ///     Represents various actions that occur with touch devices.
    /// </summary>
    public enum TouchAction
    {
        /// <summary>
        ///     The act of putting a finger onto the screen.
        /// </summary>
        Down,

        /// <summary>
        ///     The act of dragging a finger across the screen.
        /// </summary>
        Move,

        /// <summary>
        ///     The act of lifting a finger off of the screen.
        /// </summary>
        Up,
    }
}
