// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for Scroll Pattern

using System;
using MS.Internal.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation
{
    /// <summary>
    /// Used by ScrollPattern to indicate how much to scroll by
    /// </summary>
    [ComVisible(true)]
    [Guid("bd52d3c7-f990-4c52-9ae3-5c377e9eb772")]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal enum ScrollAmount
#else
    public enum ScrollAmount
#endif
    {
        /// <summary>
        /// Scroll back by a large value typically the amount equal to PageUp
        /// or invoking a scrollbar between the up arrow and the thumb.
        /// If PageUp is not a relevant amount for the control and no scrollbar
        /// exists, LargeValue represents an amount equal to the 
        /// current visible window.
        /// </summary>
        LargeDecrement,

        /// <summary>
        /// Scroll back by a small value typically the amount equal to the 
        /// Up or left arrow or invoking the arrow buttons on a scrollbar.
        /// </summary>
        SmallDecrement,

        /// <summary>
        /// used to allow for no movement is a given direction.
        /// </summary>
        NoAmount,

        /// <summary>
        /// Scroll forward by a large value typically the amount equal to PageDown
        /// or invoking a scrollbar between the down arrow and the thumb.
        /// If PageDown is not a relevant amount for the control and no scrollbar
        /// exists, LargeValue represents an amount equal to the 
        /// current visible window.
        /// </summary>
        LargeIncrement,

        /// <summary>
        /// Scroll forwards by a small value typically the amount equal to the
        /// Down or right arrow or invoking the arrow buttons on a scrollbar.
        /// </summary>
        SmallIncrement
    }


    /// <summary>
    /// Represents UI elements that are expressing a value
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class ScrollPatternIdentifiers
#else
    public static class ScrollPatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Value used by SetSCrollPercent to indicate that no scrolling should take place in the specified direction</summary>
        public const double NoScroll = -1.0;
        
        /// <summary>Scroll pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.Scroll, "ScrollPatternIdentifiers.Pattern");

        /// <summary>Property ID: HorizontalScrollPercent - Current horizontal scroll position</summary>
        public static readonly AutomationProperty HorizontalScrollPercentProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.ScrollHorizontalScrollPercent, "ScrollPatternIdentifiers.HorizontalScrollPercentProperty");
        
        /// <summary>Property ID: HorizontalViewSize - Minimum possible horizontal scroll position</summary>
        public static readonly AutomationProperty HorizontalViewSizeProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.ScrollHorizontalViewSize, "ScrollPatternIdentifiers.HorizontalViewSizeProperty");
        
        /// <summary>Property ID: VerticalScrollPercent - Current vertical scroll position</summary>
        public static readonly AutomationProperty VerticalScrollPercentProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.ScrollVerticalScrollPercent, "ScrollPatternIdentifiers.VerticalScrollPercentProperty");
        
        /// <summary>Property ID: VerticalViewSize </summary>
        public static readonly AutomationProperty VerticalViewSizeProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.ScrollVerticalViewSize, "ScrollPatternIdentifiers.VerticalViewSizeProperty");
        
        /// <summary>Property ID: HorizontallyScrollable</summary>
        public static readonly AutomationProperty HorizontallyScrollableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.ScrollHorizontallyScrollable, "ScrollPatternIdentifiers.HorizontallyScrollableProperty");
        
        /// <summary>Property ID: VerticallyScrollable</summary>
        public static readonly AutomationProperty VerticallyScrollableProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.ScrollVerticallyScrollable, "ScrollPatternIdentifiers.VerticallyScrollableProperty");

        #endregion Public Constants and Readonly Fields
    }
}
