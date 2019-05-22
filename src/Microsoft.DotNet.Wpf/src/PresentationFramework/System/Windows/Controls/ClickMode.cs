// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Controls
{
    /// <summary>
    /// ClickMode specifies when the Click event should fire
    /// </summary>
    public enum ClickMode
    {
        /// <summary>
        /// Used to specify that the Click event will fire on the
        /// normal down->up semantics of Button interaction.
        /// Escaping mechanisms work, too. Capture is taken by the
        /// Button while it is down and released after the
        /// Click is fired.
        /// </summary>
        Release,

        /// <summary>
        /// Used to specify that the Click event should fire on the
        /// down of the Button.  Basically, Click will fire as
        /// soon as the IsPressed property on Button becomes true.
        /// Even if the mouse is held down on the Button, capture
        /// is not taken.
        /// </summary>
        Press,

        /// <summary>
        /// Used to specify that the Click event should fire when the
        /// mouse hovers over a Button.
        /// </summary>
        Hover,

        // NOTE: if you add or remove any values in this enum, be sure to update ButtonBase.IsValidClickMode()
    }
}
