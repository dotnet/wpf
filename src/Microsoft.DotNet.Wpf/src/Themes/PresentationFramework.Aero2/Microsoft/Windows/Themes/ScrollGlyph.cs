// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    ///     Types and orientations of ScrollBar glyphs.
    /// </summary>
    public enum ScrollGlyph
    {
        /// <summary>
        ///     No glyph
        /// </summary>
        None,

        /// <summary>
        ///     Arrow pointing left.
        /// </summary>
        LeftArrow,

        /// <summary>
        ///     Arrow pointing right.
        /// </summary>
        RightArrow,

        /// <summary>
        ///     Arrow pointing up.
        /// </summary>
        UpArrow,

        /// <summary>
        ///     Arrow pointing down.
        /// </summary>
        DownArrow,

        /// <summary>
        ///     Vertical gripper.
        /// </summary>
        VerticalGripper,

        /// <summary>
        ///     Horizontal gripper.
        /// </summary>
        HorizontalGripper,

        // NOTE: if you add or remove any values in this enum, be sure to update ScrollChrome.IsValidScrollGlyph()    
    }
}
