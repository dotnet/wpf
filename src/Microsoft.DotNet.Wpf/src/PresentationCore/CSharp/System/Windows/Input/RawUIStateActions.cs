// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Input
{
    /// <summary>
    ///    The raw actions being reported from WM_*UISTATE* messages.
    /// </summary>
    internal enum RawUIStateActions
    {
        /// <summary>
        ///     The targets should be set.
        /// </summary>
        Set = 1,            // == UIS_SET

        /// <summary>
        ///     The targets should be cleared.
        /// </summary>
        Clear = 2,          // == UIS_CLEAR

        /// <summary>
        ///     The targets should be initialized.
        /// </summary>
        Initialize = 3,     // == UIS_INITIALIZE
    }
}
