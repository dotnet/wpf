// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     Describes how a popup should animate open or closed.
    /// </summary>
    public enum PopupAnimation
    {
        /// <summary>
        ///     No animation is to be used.
        /// </summary>
        None,

        /// <summary>
        ///     Animates the opacity of the popup.
        /// </summary>
        Fade,

        /// <summary>
        ///     Animates the width and height of the popup at the same time, 
        ///     using the upper-left corner as the origin (or lower left when flipped).
        /// </summary>
        Slide,

        /// <summary>
        ///     Animates the only the height of the popup.
        /// </summary>
        Scroll,

        // NOTE: If you add or remove any values in this enum, be sure to update Popup.IsValidPopupAnimation()    
    }
}
