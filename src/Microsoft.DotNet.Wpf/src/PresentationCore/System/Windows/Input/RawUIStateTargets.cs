// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Input
{
    /// <summary>
    ///    The targets being reported from WM_*UISTATE* messages.
    /// </summary>
    /// <remarks>
    ///     Note that multiple targets can be reported at once.
    /// </remarks>
    [Flags]
    internal enum RawUIStateTargets
    {
        /// <summary>
        ///     No targets.
        /// </summary>
        None = 0x0,

        /// <summary>
        ///     Show/hide focus adorners
        /// </summary>
        HideFocus = 0x1,        // == UISF_HIDEFOCUS

        /// <summary>
        ///     Show/hide accelerator keys
        /// </summary>
        HideAccelerators = 0x2, // == UISF_HIDEACCEL

        /// <summary>
        ///     Active component
        /// </summary>
        Active = 0x4,           // == UISF_ACTIVE
    }
}
