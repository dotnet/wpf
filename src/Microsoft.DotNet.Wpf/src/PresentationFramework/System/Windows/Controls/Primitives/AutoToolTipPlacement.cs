// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Controls.Primitives
{
        /// <summary>
        /// Placement options for Slider's AutoToolTip
        /// </summary>
        public enum AutoToolTipPlacement
        {
            /// <summary>
            /// No AutoToolTip
            /// </summary>
            None,
            /// <summary>
            /// Show AutoToolTip at top edge of Thumb (for HorizontalSlider), or at left edge of Thumb (for VerticalSlider)
            /// </summary>
            TopLeft,
            /// <summary>
            /// Show AutoToolTip at bottom edge of Thumb (for HorizontalSlider), or at right edge of Thumb (for VerticalSlider)
            /// </summary>
            BottomRight,

            // NOTE: if you add or remove any values in this enum, be sure to update Slider.IsValidAutoToolTipPlacement()
        };
}
