// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

using MS.Internal;
using MS.Win32;

namespace System.Windows.Automation.Peers
{
    /// 
    public class ScrollViewerAutomationPeer : FrameworkElementAutomationPeer, IScrollProvider
    {
        ///
        public ScrollViewerAutomationPeer(ScrollViewer owner): base(owner)
        {}
    
        ///
        override protected string GetClassNameCore()
        {
            return "ScrollViewer";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Pane;
        }

        ///
        override protected bool IsControlElementCore()
        {
            // Return false if ScrollViewer is part of a ControlTemplate, otherwise return the base method
            ScrollViewer sv = (ScrollViewer)Owner;
            DependencyObject templatedParent = sv.TemplatedParent;

            // If the templatedParent is a ContentPresenter, this ScrollViewer is generated from a DataTemplate
            if (templatedParent == null || templatedParent is ContentPresenter)
            {
                return base.IsControlElementCore();
            }

            return false;
        }

        /// 
        override public object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Scroll)
            {
                return this;
            }
            else
            {
                return base.GetPattern(patternInterface);
            }
        }


        //-------------------------------------------------------------------
        //
        //  IScrollProvider
        //
        //-------------------------------------------------------------------

        /// <summary>
        /// Request to scroll horizontally and vertically by the specified amount.
        /// The ability to call this method and simultaneously scroll horizontally
        /// and vertically provides simple panning support.
        /// </summary>
        /// <see cref="IScrollProvider.Scroll"/>
        void IScrollProvider.Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
        {
            if(!IsEnabled())
                throw new ElementNotEnabledException();

            bool scrollHorizontally = (horizontalAmount != ScrollAmount.NoAmount);
            bool scrollVertically = (verticalAmount != ScrollAmount.NoAmount);

            ScrollViewer owner = (ScrollViewer)Owner;

            if (scrollHorizontally && !HorizontallyScrollable || scrollVertically && !VerticallyScrollable)
            {
                throw new InvalidOperationException(SR.Get(SRID.UIA_OperationCannotBePerformed));
            }

            switch (horizontalAmount)
            {
                case ScrollAmount.LargeDecrement:
                    owner.PageLeft();
                    break;
                case ScrollAmount.SmallDecrement:
                    owner.LineLeft();
                    break;
                case ScrollAmount.SmallIncrement:
                    owner.LineRight();
                    break;
                case ScrollAmount.LargeIncrement:
                    owner.PageRight();
                    break;
                case ScrollAmount.NoAmount:
                    break;
                default:
                    throw new InvalidOperationException(SR.Get(SRID.UIA_OperationCannotBePerformed));
            }

            switch (verticalAmount)
            {
                case ScrollAmount.LargeDecrement:
                    owner.PageUp();
                    break;
                case ScrollAmount.SmallDecrement:
                    owner.LineUp();
                    break;
                case ScrollAmount.SmallIncrement:
                    owner.LineDown();
                    break;
                case ScrollAmount.LargeIncrement:
                    owner.PageDown();
                    break;
                case ScrollAmount.NoAmount:
                    break;
                default:
                    throw new InvalidOperationException(SR.Get(SRID.UIA_OperationCannotBePerformed));
            }
        }

        /// <summary>
        /// Request to set the current horizontal and Vertical scroll position by percent (0-100).
        /// Passing in the value of "-1", represented by the constant "NoScroll", will indicate that scrolling
        /// in that direction should be ignored.
        /// The ability to call this method and simultaneously scroll horizontally and vertically provides simple panning support.
        /// </summary>
        /// <see cref="IScrollProvider.SetScrollPercent"/>
        void IScrollProvider.SetScrollPercent(double horizontalPercent, double verticalPercent)
        {
            if(!IsEnabled())
                throw new ElementNotEnabledException();

            bool scrollHorizontally = (horizontalPercent != (double)ScrollPatternIdentifiers.NoScroll);
            bool scrollVertically = (verticalPercent != (double)ScrollPatternIdentifiers.NoScroll);

            ScrollViewer owner = (ScrollViewer)Owner;

            if (scrollHorizontally && !HorizontallyScrollable || scrollVertically && !VerticallyScrollable)
            {
                throw new InvalidOperationException(SR.Get(SRID.UIA_OperationCannotBePerformed));
            }

            if (scrollHorizontally && (horizontalPercent < 0.0) || (horizontalPercent > 100.0))
            {
                throw new ArgumentOutOfRangeException("horizontalPercent", SR.Get(SRID.ScrollViewer_OutOfRange, "horizontalPercent", horizontalPercent.ToString(CultureInfo.InvariantCulture), "0", "100"));
            }
            if (scrollVertically && (verticalPercent < 0.0) || (verticalPercent > 100.0))
            {
                throw new ArgumentOutOfRangeException("verticalPercent", SR.Get(SRID.ScrollViewer_OutOfRange, "verticalPercent", verticalPercent.ToString(CultureInfo.InvariantCulture), "0", "100"));
            }

            if (scrollHorizontally)
            {
                owner.ScrollToHorizontalOffset((owner.ExtentWidth - owner.ViewportWidth) * (double)horizontalPercent * 0.01);
            }
            if (scrollVertically)
            {
                owner.ScrollToVerticalOffset((owner.ExtentHeight - owner.ViewportHeight) * (double)verticalPercent * 0.01);
            }
        }

        /// <summary>
        /// Get the current horizontal scroll position
        /// </summary>
        /// <see cref="IScrollProvider.HorizontalScrollPercent"/>
        double IScrollProvider.HorizontalScrollPercent
        {
            get
            {
                if (!HorizontallyScrollable) { return ScrollPatternIdentifiers.NoScroll; }
                ScrollViewer owner = (ScrollViewer)Owner;
                return (double)(owner.HorizontalOffset * 100.0 / (owner.ExtentWidth - owner.ViewportWidth));
            }
        }

        /// <summary>
        /// Get the current vertical scroll position
        /// </summary>
        /// <see cref="IScrollProvider.VerticalScrollPercent"/>
        double IScrollProvider.VerticalScrollPercent
        {
            get
            {
                if (!VerticallyScrollable) { return ScrollPatternIdentifiers.NoScroll; }
                ScrollViewer owner = (ScrollViewer)Owner;
                return (double)(owner.VerticalOffset * 100.0 / (owner.ExtentHeight - owner.ViewportHeight));
            }
        }

        /// <summary>
        /// Equal to the horizontal percentage of the entire control that is currently viewable.
        /// </summary>
        /// <see cref="IScrollProvider.HorizontalViewSize"/>
        double IScrollProvider.HorizontalViewSize
        {
            get
            {
                ScrollViewer owner = (ScrollViewer)Owner;
                if (owner.ScrollInfo == null || DoubleUtil.IsZero(owner.ExtentWidth)) { return 100.0; }
                return Math.Min(100.0, (double)(owner.ViewportWidth * 100.0 / owner.ExtentWidth));
            }
        }

        /// <summary>
        /// Equal to the vertical percentage of the entire control that is currently viewable.
        /// </summary>
        /// <see cref="IScrollProvider.VerticalViewSize"/>
        double IScrollProvider.VerticalViewSize
        {
            get
            {
                ScrollViewer owner = (ScrollViewer)Owner;
                if (owner.ScrollInfo == null || DoubleUtil.IsZero(owner.ExtentHeight)) { return 100f; }
                return Math.Min(100.0, (double)(owner.ViewportHeight * 100.0 / owner.ExtentHeight));
            }
        }

        /// <summary>
        /// True if control can scroll horizontally
        /// </summary>
        /// <see cref="IScrollProvider.HorizontallyScrollable"/>
        bool IScrollProvider.HorizontallyScrollable
        {
            get { return HorizontallyScrollable; }
        }

        /// <summary>
        /// True if control can scroll vertically
        /// </summary>
        /// <see cref="IScrollProvider.VerticallyScrollable"/>
        bool IScrollProvider.VerticallyScrollable
        {
            get { return VerticallyScrollable; }
        }

        private static bool AutomationIsScrollable(double extent, double viewport)
        {
            return DoubleUtil.GreaterThan(extent, viewport); 
        }

        private static double AutomationGetScrollPercent(double extent, double viewport, double actualOffset)
        {
            if (!AutomationIsScrollable(extent, viewport)) { return ScrollPatternIdentifiers.NoScroll; }
            return (double)(actualOffset * 100.0 / (extent - viewport));
        }

        private static double AutomationGetViewSize(double extent, double viewport)
        {
            if (DoubleUtil.IsZero(extent)) { return 100.0; }
            return Math.Min(100.0, (double)(viewport * 100.0 / extent));
        }

        // Private *Scrollable properties used to determine scrollability for IScrollProvider implemenation.
        private bool HorizontallyScrollable
        {
            get 
            { 
                ScrollViewer owner = (ScrollViewer)Owner;
                return owner.ScrollInfo != null && DoubleUtil.GreaterThan(owner.ExtentWidth, owner.ViewportWidth); 
            }
        }
        private bool VerticallyScrollable
        {
            get 
            { 
                ScrollViewer owner = (ScrollViewer)Owner;
                return owner.ScrollInfo != null && DoubleUtil.GreaterThan(owner.ExtentHeight, owner.ViewportHeight); 
            }
        }

        // This helper synchronously fires automation PropertyChange events for every IScrollProvider property that has changed.
        // BUG 1555137: Never inline, as we don't want to unnecessarily link the automation DLL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseAutomationEvents(double extentX,
                                          double extentY,
                                          double viewportX,
                                          double viewportY,
                                          double offsetX,
                                          double offsetY)
        {
            IScrollProvider isp = (IScrollProvider)this;
            
            if (AutomationIsScrollable(extentX, viewportX) != isp.HorizontallyScrollable)
            {
                RaisePropertyChangedEvent(
                    ScrollPatternIdentifiers.HorizontallyScrollableProperty, 
                    AutomationIsScrollable(extentX, viewportX), 
                    isp.HorizontallyScrollable);
            }
            if (AutomationIsScrollable(extentY, viewportY) != isp.VerticallyScrollable)
            {
                RaisePropertyChangedEvent(
                    ScrollPatternIdentifiers.VerticallyScrollableProperty, 
                    AutomationIsScrollable(extentY, viewportY), 
                    isp.VerticallyScrollable);
            }
            if (AutomationGetViewSize(extentX, viewportX) != isp.HorizontalViewSize)
            {
                RaisePropertyChangedEvent(
                    ScrollPatternIdentifiers.HorizontalViewSizeProperty, 
                    AutomationGetViewSize(extentX, viewportX), 
                    isp.HorizontalViewSize);
            }
            if (AutomationGetViewSize(extentY, viewportY) != isp.VerticalViewSize)
            {
                RaisePropertyChangedEvent(
                    ScrollPatternIdentifiers.VerticalViewSizeProperty, 
                    AutomationGetViewSize(extentY, viewportY), 
                    isp.VerticalViewSize);
            }
            if (AutomationGetScrollPercent(extentX, viewportX, offsetX) != isp.HorizontalScrollPercent)
            {
                RaisePropertyChangedEvent(
                    ScrollPatternIdentifiers.HorizontalScrollPercentProperty, 
                    AutomationGetScrollPercent(extentX, viewportX, offsetX), 
                    isp.HorizontalScrollPercent);
            }
            if (AutomationGetScrollPercent(extentY, viewportY, offsetY) != isp.VerticalScrollPercent)
            {
                RaisePropertyChangedEvent(
                    ScrollPatternIdentifiers.VerticalScrollPercentProperty, 
                    AutomationGetScrollPercent(extentY, viewportY, offsetY), 
                    isp.VerticalScrollPercent);
            }
        }
    }
}


