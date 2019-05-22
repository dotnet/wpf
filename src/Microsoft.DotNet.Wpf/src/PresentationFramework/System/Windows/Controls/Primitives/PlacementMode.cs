// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     Describes where a popup should be placed on screen.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public enum PlacementMode
    {
        /// <summary>
        ///     Uses HorizontalOffset and VerticalOffset properties to position the Popup relative to 
        ///     the upper left corner of the screen on which the parent window is located.
        /// </summary>
        Absolute,

        /// <summary>
        ///     Uses HorizontalOffset and VerticalOffset properties to position the Popup relative to 
        ///     the upper left corner of the parent element.
        /// </summary>
        Relative,

        /// <summary>
        ///     Positions the popup below the parent element and aligns the left edges of each.
        ///     If the popup crosses the lower edge of the screen, the position will flip to position
        ///     the popup above the parent. If that causes the popup to cross the upper edge of the screen,
        ///     then the popup will be positioned with its lower edge at the bottom of the screen. If it still
        ///     crosses the upper edge of the screen, then the upper edge of the popup will be positioned at
        ///     at the top of the screen.
        /// </summary>
        Bottom,

        /// <summary>
        ///     Centers the popup over the parent element.
        ///     If the popup crosses any edge of the screen, it will be repositioned such that the majority
        ///     of the popup is onscreen. It will favor the top and left edges of the popup in keeping the
        ///     popup onscreen.
        /// </summary>
        Center,

        /// <summary>
        ///     Positions the popup to the right side of the parent element and aligns the top edges of each.
        ///     If the popup crosses the lower edge of the screen, the popup and the parent will be
        ///     realigned with their bottom edges. If the popup crosses the upper edge, then the popup will be 
        ///     nudged onscreen, favoring keeping the top and left edges on screen.
        ///     If the popup crosses the right edge of the screen, the popup will flip to the left side of the
        ///     parent element unless that causes it to cross the left side of the screen.
        /// </summary>
        Right,

        /// <summary>
        ///     Uses HorizontalOffset and VerticalOffset properties to position the Popup relative to
        ///     the upper left corner of the screen on which the parent window is located.
        ///     If the popup extends beyond the edge of the screen, the popup will flip to
        ///     the other side of the point.
        /// </summary>
        AbsolutePoint,

        /// <summary>
        ///     Uses HorizontalOffset and VerticalOffset properties to position the Popup relative to
        ///     the upper left corner of the parent element.
        ///     If the popup extends beyond the edge of the screen, the popup will flip to
        ///     the other side of the point.
        /// </summary>
        RelativePoint,

        /// <summary>
        /// This setting has the same effect as Bottom except that the bounding box (the 
        /// box which would normally bound the control) is the bounding box of the mouse cursor.  
        /// This has the effect of displaying the popup below the area occupied by the mouse 
        /// cursor.  Edge behaviors are the same as those defined by bottom.
        /// </summary>
        Mouse,

        /// <summary>
        /// This setting has the same effect as RelativePoint except that the reference point
        /// is the tip of the mouse cursor.  Edge behaviors are the same as those defined 
        /// by RelativePoint.
        /// </summary>
        MousePoint,

        /// <summary>
        ///     This mode is just like Right except it favors the left side instead of the right.
        /// </summary>
        Left,

        /// <summary>
        ///     This mode is just like Bottom except it favors the top side instead of the bottom.
        /// </summary>
        Top,

        /// <summary>
        ///     Use custom code provided by CustomPopupPlacementCallback.
        /// </summary>
        Custom,

        // NOTE: if you add or remove any values in this enum, be sure to update Popup.IsValidPlacementMode()    
    }
}
