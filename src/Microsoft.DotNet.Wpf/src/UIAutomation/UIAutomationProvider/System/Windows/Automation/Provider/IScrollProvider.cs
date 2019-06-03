// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Scroll pattern provider interface

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// The Scroll pattern exposes a control's ability to change the portion of its visible region
    /// that is visible to the user by scrolling its content.
    ///
    /// Examples:
    ///
    ///     Listboxes
    ///     TreeViews
    ///     other containers that maintain a content area larger than the control's visible region
    ///
    /// Note that scrollbars themselves should not support the Scrollable pattern; they support the
    /// RangeValue pattern. 
    ///
    /// Servers must normalize scrolling (0 to 100).
    ///
    /// This public interface represents UI elements that scroll their content.
    /// </summary>
    [ComVisible(true)]
    [Guid("b38b8077-1fc3-42a5-8cae-d40c2215055a")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IScrollProvider
#else
    public interface IScrollProvider
#endif
    {
        /// <summary>
        /// Request to scroll horizontally and vertically by the specified amount.
        /// The ability to call this method and simultaneously scroll horizontally 
        /// and vertically provides simple panning support.
        /// </summary>
        /// <param name="horizontalAmount">horizontal amount to scroll by</param>
        /// <param name="verticalAmount">vertical amount to scroll by</param>
        void Scroll( ScrollAmount horizontalAmount, ScrollAmount verticalAmount );

        /// <summary>
        /// Request to set the current horizontal and Vertical scroll position 
        /// by percent (0-100).  Passing in the value of "-1", represented by the 
        /// constant "NoScroll", will indicate that scrolling in that direction 
        /// should be ignored.
        /// The ability to call this method and simultaneously scroll horizontally and 
        /// vertically provides simple panning support.
        /// </summary>
        /// <param name="horizontalPercent">horizontal position to scroll to</param>
        /// <param name="verticalPercent">vertical position to scroll to</param>
        void SetScrollPercent( double horizontalPercent, double verticalPercent );
        
        /// <summary>
        /// Get the current horizontal scroll position
        /// </summary>
        double HorizontalScrollPercent
        {
            get;
        }

        /// <summary>
        /// Get the current vertical scroll position
        /// </summary>
        double VerticalScrollPercent
        {
            get;
        }

        /// <summary>
        /// Equal to the horizontal percentage of the entire control that is currently viewable.
        /// </summary>
        double HorizontalViewSize
        {
            get;
        }

        /// <summary>
        /// Equal to the horizontal percentage of the entire control that is currently viewable.
        /// </summary>
        double VerticalViewSize
        {
            get;
        }
        
        /// <summary>
        /// True if control can scroll horizontally
        /// </summary>
        bool HorizontallyScrollable
        {
            [return: MarshalAs(UnmanagedType.Bool)] // Without this, only lower SHORT of BOOL*pRetVal param is updated.
            get;
        }
        
        /// <summary>
        /// True if control can scroll vertically
        /// </summary>
        bool VerticallyScrollable
        {
            [return: MarshalAs(UnmanagedType.Bool)] // Without this, only lower SHORT of BOOL*pRetVal param is updated.
            get;
        }
    }
}
