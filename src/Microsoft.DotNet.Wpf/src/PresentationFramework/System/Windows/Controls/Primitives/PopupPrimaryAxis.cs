// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     Describes how a popup should be nudged on-screen.
    /// </summary>
    public enum PopupPrimaryAxis
    {
        /// <summary>
        ///     When a popup is nudged on-screen, it will be done using the
        ///     coordinate space of the screen.
        /// </summary>
        None,

        /// <summary>
        ///     When a popup is nudged on-screen, it will be done using the
        ///     horizontal axis of the popup coordinate space
        ///     before using the coordinate space of the screen.
        /// </summary>
        Horizontal,

        /// <summary>
        ///     When a popup is nudged on-screen, it will be done using the
        ///     vertical axis of the popup coordinate space
        ///     before using the coordinate space of the screen.
        /// </summary>
        Vertical,
}
}
