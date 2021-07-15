// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Input
{
    /// <summary>
    ///     The MouseButtonState enumeration describes the possible states
    ///     of the buttons available on the Mouse input device.
    /// </summary>
    /// <ExternalAPI Inherit="true"/>
    public enum MouseButtonState
    {
        /// <summary>
        ///    The button is released.
        /// </summary>
        Released,
        
        /// <summary>
        ///    The button is pressed.
        /// </summary>
        Pressed,

        // Update the IsValid helper in RawMouseState.cs if this enum changes.
    }
}
