// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System.Collections;
using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows;
#if RIBBON_IN_FRAMEWORK
using System.Windows.Controls.Ribbon.Primitives;
#else
using Microsoft.Windows.Controls.Ribbon.Primitives;
#endif
using MS.Internal;
using System;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    /// <summary>
    ///     A static class which defines various helper methods for DropDown Resizing in Ribbon controls.
    /// </summary>
    internal static class RibbonDropDownHelper
    {
        public static void OnPopupResizeStarted(FrameworkElement itemsPresenter)
        {
            if (itemsPresenter != null)
            {
                // We need Height and Width to be a valid double (not NaN or zero)
                // because we are going to add/subtract from these later
                itemsPresenter.Width = itemsPresenter.ActualWidth;
                itemsPresenter.Height = itemsPresenter.ActualHeight;
            }
       }

        public static bool ResizePopup(FrameworkElement itemsPresenter, 
            Size minDropDownSize,
            bool canUserResizeHorizontally, 
            bool canUserResizeVertically, 
            bool isDropDownPositionedLeft,
            bool isDropDownPositionedAbove,
            Rect screenBounds,
            UIElement popupRoot,
            double horizontalDelta, 
            double verticalDelta)
        {
            if (itemsPresenter != null)
            {
                verticalDelta = isDropDownPositionedAbove ? (-verticalDelta) : verticalDelta;
                horizontalDelta = isDropDownPositionedLeft? (-horizontalDelta): horizontalDelta;

                Rect rootScreenBounds = new Rect(popupRoot.PointToScreen(new Point()),
                              popupRoot.PointToScreen(new Point(popupRoot.RenderSize.Width, popupRoot.RenderSize.Height)));

                // There is a bug in .net3.5 in PopupRoot. When a Popup reaches a screen edge its size is restricted in both dimensions,
                // regardless of which dimension is at the screen edge. 
                // If Popup is at bottom screen edge and you try to increase width horizontally, 
                // the contents increase in size but get clipped by the Popup. This has been fixed in 4.0. 
                // To workaround this bug, we dont allow Popup to ever go beyond top/bottom screen edge via resizing
                // Note that Popup could still hit screen edge even before resizing, when Popup is opened for the first time. 
                // This will cause clipping but we are not going to workaround this case. 

                // Case 1: Popup is already at screen edge.
                // So we want to ignore the delta if trying to increase in direction of screen edge.
                bool isAtBottomScreenEdge = DoubleUtil.GreaterThanOrClose(rootScreenBounds.Bottom, screenBounds.Bottom);
                bool isAtTopScreenEdge = DoubleUtil.LessThanOrClose(rootScreenBounds.Top, screenBounds.Top + TopScreenEdgeBuffer);
                bool isAtRightScreenEdge = DoubleUtil.GreaterThanOrClose(rootScreenBounds.Right, screenBounds.Right);
                bool isAtLeftScreenEdge = DoubleUtil.LessThanOrClose(rootScreenBounds.Left, screenBounds.Left);

                // Case 2: If applying the deltas will make popup go over the screen edge.
                // Truncate delta to whatever is remaining until the screen edge.
                bool isAlmostAtBottomScreenEdge = DoubleUtil.GreaterThanOrClose(rootScreenBounds.Bottom + verticalDelta, screenBounds.Bottom);
                bool isAlmostAtTopScreenEdge = DoubleUtil.LessThanOrClose(rootScreenBounds.Top - verticalDelta, screenBounds.Top + TopScreenEdgeBuffer);
                bool isAlmostAtRightScreenEdge = DoubleUtil.GreaterThanOrClose(rootScreenBounds.Right + horizontalDelta, screenBounds.Right);
                bool isAlmostAtLeftScreenEdge = DoubleUtil.LessThanOrClose(rootScreenBounds.Left - horizontalDelta, screenBounds.Left);
                
                if (isDropDownPositionedAbove)
                {
                    if (isAtTopScreenEdge && DoubleUtil.GreaterThanOrClose(verticalDelta, 0))
                    {
                        verticalDelta = 0;
                    }
                    else if (isAlmostAtTopScreenEdge)
                    {
                        verticalDelta = rootScreenBounds.Top - TopScreenEdgeBuffer;
                    }
                }
                else if (!isDropDownPositionedAbove)
                {
                    if (isAtBottomScreenEdge && DoubleUtil.GreaterThanOrClose(verticalDelta, 0))
                    {
                        verticalDelta = 0;
                    }
                    else if (isAlmostAtBottomScreenEdge)
                    {
                        verticalDelta = screenBounds.Bottom - rootScreenBounds.Bottom;
                    }
                }

                // We need this check because WPF Popup's can render only on single monitor. 
                // (i.e. wont extend to second monitor in multi-mon setup).
                if (isAtRightScreenEdge && DoubleUtil.GreaterThanOrClose(horizontalDelta, 0))
                {
                    horizontalDelta = 0;
                }
                else if (isAlmostAtRightScreenEdge)
                {
                    horizontalDelta = screenBounds.Right - rootScreenBounds.Right;
                }

                if (isAtLeftScreenEdge && DoubleUtil.GreaterThanOrClose(horizontalDelta, 0))
                {
                    horizontalDelta = 0;
                }
                else if (isAlmostAtLeftScreenEdge)
                {
                    horizontalDelta = rootScreenBounds.Left - screenBounds.Left ;
                }

                double newWidth = itemsPresenter.Width + horizontalDelta;
                double newHeight = itemsPresenter.Height + verticalDelta;
                return ResizePopupActual(itemsPresenter, minDropDownSize, canUserResizeHorizontally, canUserResizeVertically, newWidth, newHeight);
            }

            return false;
        }

        private static bool ResizePopupActual(FrameworkElement itemsPresenter, Size minDropDownSize, bool canUserResizeHorizontally, bool canUserResizeVertically, double newWidth, double newHeight)
        {
            bool result = false;
            if (itemsPresenter != null)
            {
                // Only if the new Width and Height are adequate to display the items in their minimum variant.
                if (canUserResizeHorizontally && DoubleUtil.GreaterThanOrClose(newWidth, 0) &&
                    DoubleUtil.LessThanOrClose(minDropDownSize.Width, newWidth))
                {
                    itemsPresenter.Width = newWidth;
                    result = true;
                }

                if (canUserResizeVertically && DoubleUtil.GreaterThanOrClose(newHeight, 0)
                    && DoubleUtil.LessThanOrClose(minDropDownSize.Height, newHeight))
                {
                    itemsPresenter.Height = newHeight;
                    result = result & true;
                }
            }

            return result;
        }

        /// <summary>
        /// Cache the screen bounds of the monitor in which the targetElement is rendered.
        /// </summary>
        /// <param name="itemsPresenter"></param>
        /// <returns></returns>
        public static Rect GetScreenBounds(FrameworkElement targetElement, Popup popup)
        {
            if (targetElement != null)
            {
                Rect targetBoundingBox = new Rect(targetElement.PointToScreen(new Point()),
                                              targetElement.PointToScreen(new Point(targetElement.RenderSize.Width, targetElement.RenderSize.Height)));

                NativeMethods.RECT rect = new NativeMethods.RECT() { top = 0, bottom = 0, left = 0, right = 0 };
                NativeMethods.RECT nativeBounds = NativeMethods.FromRect(targetBoundingBox);

                IntPtr monitor = NativeMethods.MonitorFromRect(ref nativeBounds, NativeMethods.MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    NativeMethods.MONITORINFOEX monitorInfo = new NativeMethods.MONITORINFOEX();

                    monitorInfo.cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFOEX));
                    NativeMethods.GetMonitorInfo(new HandleRef(null, monitor), monitorInfo);

                    // WPF Popup special cases MenuItem to be restricted to work area
                    // Hence Ribbon applies the same rules as well. 
                    if (popup.TemplatedParent is RibbonMenuItem)
                    {
                        rect = monitorInfo.rcWork;
                    }
                    else if (popup.TemplatedParent is RibbonMenuButton)
                    {
                        rect = monitorInfo.rcMonitor;
                    }
                }

                return NativeMethods.ToRect(rect);
            }

            return Rect.Empty;
        }

        public static void ClearLocalValues(FrameworkElement itemsPresenter, Popup popup)
        {
            if (itemsPresenter != null)
            {
                itemsPresenter.ClearValue(FrameworkElement.HeightProperty);
                itemsPresenter.ClearValue(FrameworkElement.WidthProperty);
            }
            if (popup != null)
            {
                popup.ClearValue(Popup.PlacementProperty);
                popup.ClearValue(Popup.VerticalOffsetProperty);
                popup.ClearValue(Popup.HorizontalOffsetProperty);
            }
        }

        public static Size GetMinDropDownSize(RibbonMenuItemsPanel itemsHost, Popup popup, Thickness borderThickness)
        {
            Size minSize = new Size();
            if (itemsHost != null)
            {
                minSize = itemsHost.CachedAutoSize;
                if (popup != null)
                {
                    FrameworkElement popupChild = popup.Child as FrameworkElement;
                    if (popupChild != null && DoubleUtil.GreaterThan(popupChild.MinWidth, minSize.Width)) 
                    {
                        // MenuButton's BorderThickness around the ItemsPresenter
                        minSize.Width = popupChild.MinWidth - (borderThickness.Left + borderThickness.Right);
                    }
                }
            }
        
            return minSize;
        }

        // This const was deduced empirically. Popup leaves this much gap between the top of the screen edge and itself.
        private const double TopScreenEdgeBuffer = 5.0;
    }
}
