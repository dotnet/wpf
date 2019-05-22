// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Controls.Primitives
{
        /// <summary>
        /// Placement options for Slider's Tickbar
        /// </summary>
        public enum TickPlacement
        {
            /// <summary>
            /// No TickMark
            /// </summary>
            None,
            /// <summary>
            /// Show TickMark above the Track (for HorizontalSlider), or left of the Track (for VerticalSlider)
            /// </summary>
            TopLeft,
            /// <summary>
            /// Show TickMark below the Track (for HorizontalSlider), or right of the Track (for VerticalSlider)
            /// </summary>
            BottomRight,
            /// <summary>
            /// Show TickMark on both side of the Track
            /// </summary>
            Both,

            // NOTE: if you add or remove any values in this enum, be sure to update Slider.IsValidTickPlacement()    
        };
}
