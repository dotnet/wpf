// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using MS.Internal.Commands;
using MS.Internal.Documents;
using MS.Internal.KnownBoxes;
using MS.Internal.PresentationFramework;
using MS.Internal.Telemetry.PresentationFramework;
using MS.Utility;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Shapes;

namespace System.Windows.Controls
{
    #region ScrollBarVisibility enum

    /// <summary>
    /// ScrollBarVisibilty defines the visibility behavior of a scrollbar.
    /// </summary>
    public enum ScrollBarVisibility
    {
        /// <summary>
        /// No scrollbars and no scrolling in this dimension.
        /// </summary>
        Disabled = 0,
        /// <summary>
        /// The scrollbar should be visible only if there is more content than fits in the viewport.
        /// </summary>
        Auto,
        /// <summary>
        /// The scrollbar should never be visible.  No space should ever be reserved for the scrollbar.
        /// </summary>
        Hidden,
        /// <summary>
        /// The scrollbar should always be visible.  Space should always be reserved for the scrollbar.
        /// </summary>
        Visible,

        // NOTE: if you add or remove any values in this enum, be sure to update ScrollViewer.IsValidScrollBarVisibility()
    }

    #endregion

    /// <summary>
    /// A ScrollViewer accepts content and provides the logic that allows it to scroll.
    /// </summary>
    [DefaultEvent("ScrollChangedEvent")]
    [Localizability(LocalizationCategory.Ignore)]
    [TemplatePart(Name = "PART_HorizontalScrollBar", Type = typeof(ScrollBar))]
    [TemplatePart(Name = "PART_VerticalScrollBar", Type = typeof(ScrollBar))]
    [TemplatePart(Name = "PART_ScrollContentPresenter", Type = typeof(ScrollContentPresenter))]
    public class ScrollViewer : ContentControl
    {
        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Scroll content by one line to the top.
        /// </summary>
        public void LineUp() { EnqueueCommand(Commands.LineUp, 0, null); }
        /// <summary>
        /// Scroll content by one line to the bottom.
        /// </summary>
        public void LineDown() { EnqueueCommand(Commands.LineDown, 0, null); }
        /// <summary>
        /// Scroll content by one line to the left.
        /// </summary>
        public void LineLeft() { EnqueueCommand(Commands.LineLeft, 0, null); }
        /// <summary>
        /// Scroll content by one line to the right.
        /// </summary>
        public void LineRight() { EnqueueCommand(Commands.LineRight, 0, null); }

        /// <summary>
        /// Scroll content by one page to the top.
        /// </summary>
        public void PageUp() { EnqueueCommand(Commands.PageUp, 0, null); }
        /// <summary>
        /// Scroll content by one page to the bottom.
        /// </summary>
        public void PageDown() { EnqueueCommand(Commands.PageDown, 0, null); }
        /// <summary>
        /// Scroll content by one page to the left.
        /// </summary>
        public void PageLeft() { EnqueueCommand(Commands.PageLeft, 0, null); }
        /// <summary>
        /// Scroll content by one page to the right.
        /// </summary>
        public void PageRight() { EnqueueCommand(Commands.PageRight, 0, null); }

        /// <summary>
        /// Horizontally scroll to the beginning of the content.
        /// </summary>
        public void ScrollToLeftEnd() { EnqueueCommand(Commands.SetHorizontalOffset, Double.NegativeInfinity, null); }
        /// <summary>
        /// Horizontally scroll to the end of the content.
        /// </summary>
        public void ScrollToRightEnd() { EnqueueCommand(Commands.SetHorizontalOffset, Double.PositiveInfinity, null); }

        /// <summary>
        /// Scroll to Top-Left of the content.
        /// </summary>
        public void ScrollToHome()
        {
            EnqueueCommand(Commands.SetHorizontalOffset, Double.NegativeInfinity, null);
            EnqueueCommand(Commands.SetVerticalOffset, Double.NegativeInfinity, null);
        }
        /// <summary>
        /// Scroll to Bottom-Left of the content.
        /// </summary>
        public void ScrollToEnd()
        {
            EnqueueCommand(Commands.SetHorizontalOffset, Double.NegativeInfinity, null);
            EnqueueCommand(Commands.SetVerticalOffset, Double.PositiveInfinity, null);
        }

        /// <summary>
        /// Vertically scroll to the beginning of the content.
        /// </summary>
        public void ScrollToTop() { EnqueueCommand(Commands.SetVerticalOffset, Double.NegativeInfinity, null); }
        /// <summary>
        /// Vertically scroll to the end of the content.
        /// </summary>
        public void ScrollToBottom() { EnqueueCommand(Commands.SetVerticalOffset, Double.PositiveInfinity, null); }

        /// <summary>
        /// Scroll horizontally to specified offset. Not guaranteed to end up at the specified offset though.
        /// </summary>
        public void ScrollToHorizontalOffset(double offset)
        {
            double validatedOffset = ScrollContentPresenter.ValidateInputOffset(offset, "offset");

            // Queue up the scroll command, which tells the content to scroll.
            // Will lead to an update of all offsets (both live and deferred).
            EnqueueCommand(Commands.SetHorizontalOffset, validatedOffset, null);
        }

        /// <summary>
        /// Scroll vertically to specified offset. Not guaranteed to end up at the specified offset though.
        /// </summary>
        public void ScrollToVerticalOffset(double offset)
        {
            double validatedOffset = ScrollContentPresenter.ValidateInputOffset(offset, "offset");

            // Queue up the scroll command, which tells the content to scroll.
            // Will lead to an update of all offsets (both live and deferred).
            EnqueueCommand(Commands.SetVerticalOffset, validatedOffset, null);
        }

        private void DeferScrollToHorizontalOffset(double offset)
        {
            double validatedOffset = ScrollContentPresenter.ValidateInputOffset(offset, "offset");

            // Update the offset property but not the deferred (content offset)
            // property, which will be updated when the drag operation is complete.
            HorizontalOffset = validatedOffset;
        }

        private void DeferScrollToVerticalOffset(double offset)
        {
            double validatedOffset = ScrollContentPresenter.ValidateInputOffset(offset, "offset");

            // Update the offset property but not the deferred (content offset)
            // property, which will be updated when the drag operation is complete.
            VerticalOffset = validatedOffset;
        }

        internal void MakeVisible(Visual child, Rect rect)
        {
            MakeVisibleParams p = new MakeVisibleParams(child, rect);
            EnqueueCommand(Commands.MakeVisible, 0, p);
        }

        private void EnsureLayoutUpdatedHandler()
        {
            if (_layoutUpdatedHandler == null)
            {
                _layoutUpdatedHandler = new EventHandler(OnLayoutUpdated);
                LayoutUpdated += _layoutUpdatedHandler;
            }
            InvalidateArrange(); //can be that there is no outstanding need to do layout - make sure it is.
        }

        private void ClearLayoutUpdatedHandler()
        {
            // If queue is not empty - then we still need that handler to make sure queue is being processed.
            if ((_layoutUpdatedHandler != null) && (_queue.IsEmpty()))
            {
                LayoutUpdated -= _layoutUpdatedHandler;
                _layoutUpdatedHandler = null;
            }
        }

        /// <summary>
        /// This function is called by an IScrollInfo attached to this ScrollViewer when any values
        /// of scrolling properties (Offset, Extent, and ViewportSize) change.  The function schedules
        /// invalidation of other elements like ScrollBars that are dependant on these properties.
        /// </summary>
        public void InvalidateScrollInfo()
        {
            IScrollInfo isi = this.ScrollInfo;

            //STRESS 1627654: anybody can call this method even if we don't have ISI...
            if(isi == null)
                return;

            // This is a public API, and is expected to be called by the
            // IScrollInfo implementation when any of the scrolling properties
            // change.  Sometimes this is done independently (not as a result
            // of laying out this ScrollViewer) and that means we should re-run
            // the logic of determining visibility of autoscrollbars, if any.
            //
            // However, invalidating measure during arrange is dangerous
            // because it could lead to layout never settling down.  This has
            // been observed with the layout rounding feature and non-standard
            // DPIs causing ScrollViewer to never settle on the visibility of
            // autoscrollbars.
            //
            // To guard against this condition, we only allow measure to be
            // invalidated from arrange once.
            //
            // We also don't invalidate measure if we are in the middle of the
            // measure pass, as the ScrollViewer will already be updating the
            // visibility of the autoscrollbars.
            if(!MeasureInProgress && 
               (!ArrangeInProgress || !InvalidatedMeasureFromArrange))
            {
                //
                // Check if we should remove/add scrollbars.
                //
                double extent = ScrollInfo.ExtentWidth;
                double viewport = ScrollInfo.ViewportWidth;

                if (    HorizontalScrollBarVisibility == ScrollBarVisibility.Auto
                    && (    (   _scrollVisibilityX == Visibility.Collapsed
                            &&  DoubleUtil.GreaterThan(extent, viewport))
                        || (    _scrollVisibilityX == Visibility.Visible
                            &&  DoubleUtil.LessThanOrClose(extent, viewport))))
                {
                    InvalidateMeasure();
                }
                else
                {
                    extent = ScrollInfo.ExtentHeight;
                    viewport = ScrollInfo.ViewportHeight;

                    if (VerticalScrollBarVisibility == ScrollBarVisibility.Auto
                        && ((_scrollVisibilityY == Visibility.Collapsed
                                && DoubleUtil.GreaterThan(extent, viewport))
                            || (_scrollVisibilityY == Visibility.Visible
                                && DoubleUtil.LessThanOrClose(extent, viewport))))
                    {
                        InvalidateMeasure();
                    }
                }
            }


            // If any scrolling properties have actually changed, fire public events post-layout
            if (        !DoubleUtil.AreClose(HorizontalOffset, ScrollInfo.HorizontalOffset)
                    ||  !DoubleUtil.AreClose(VerticalOffset, ScrollInfo.VerticalOffset)
                    ||  !DoubleUtil.AreClose(ViewportWidth, ScrollInfo.ViewportWidth)
                    ||  !DoubleUtil.AreClose(ViewportHeight, ScrollInfo.ViewportHeight)
                    ||  !DoubleUtil.AreClose(ExtentWidth, ScrollInfo.ExtentWidth)
                    ||  !DoubleUtil.AreClose(ExtentHeight, ScrollInfo.ExtentHeight))
            {
                EnsureLayoutUpdatedHandler();
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// This property indicates whether the Content should handle scrolling if it can.
        /// A true value indicates Content should be allowed to scroll if it supports IScrollInfo.
        /// A false value will always use the default physically scrolling handler.
        /// </summary>
        public bool CanContentScroll
        {
            get { return (bool)GetValue(CanContentScrollProperty); }
            set { SetValue(CanContentScrollProperty, value); }
        }

        /// <summary>
        /// HorizonalScollbarVisibility is a <see cref="System.Windows.Controls.ScrollBarVisibility" /> that
        /// determines if a horizontal scrollbar is shown.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return (ScrollBarVisibility) GetValue(HorizontalScrollBarVisibilityProperty); }
            set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
        }

        /// <summary>
        /// VerticalScrollBarVisibility is a <see cref="System.Windows.Controls.ScrollBarVisibility" /> that
        /// determines if a vertical scrollbar is shown.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return (ScrollBarVisibility) GetValue(VerticalScrollBarVisibilityProperty); }
            set { SetValue(VerticalScrollBarVisibilityProperty, value); }
        }

        /// <summary>
        /// ComputedHorizontalScrollBarVisibility contains the ScrollViewer's current calculation as to
        /// whether or not scrollbars should be displayed.
        /// </summary>
        public Visibility ComputedHorizontalScrollBarVisibility
        {
            get { return _scrollVisibilityX; }
        }
        /// <summary>
        /// ComputedVerticalScrollBarVisibility contains the ScrollViewer's current calculation as to
        /// whether or not scrollbars should be displayed.
        /// </summary>
        public Visibility ComputedVerticalScrollBarVisibility
        {
            get { return _scrollVisibilityY; }
        }

        /// <summary>
        /// Actual HorizontalOffset contains the ScrollViewer's current horizontal offset.
        /// This is a computed value, derived from viewport/content size and previous scroll commands
        /// </summary>
        public double HorizontalOffset
        {
            // _xPositionISI is a local cache of GetValue(HorizontalOffsetProperty)
            // In the future, it could be replaced with the GetValue call.
            get { return _xPositionISI; }
            private set { SetValue(HorizontalOffsetPropertyKey, value); }
        }

        /// <summary>
        /// Actual VerticalOffset contains the ScrollViewer's current Vertical offset.
        /// This is a computed value, derived from viewport/content size and previous scroll commands
        /// </summary>
        public double VerticalOffset
        {
            // _yPositionISI is a local cache of GetValue(VerticalOffsetProperty)
            // In the future, it could be replaced with the GetValue call.
            get { return _yPositionISI; }
            private set { SetValue(VerticalOffsetPropertyKey, value); }
        }

        /// <summary>
        /// ExtentWidth contains the horizontal size of the scrolled content element.
        /// </summary>
        /// <remarks>
        /// ExtentWidth is only an output property; it can effectively be set by specifying
        /// <see cref="System.Windows.FrameworkElement.Width" /> on the content element.
        /// </remarks>
        [Category("Layout")]
        public double ExtentWidth
        {
            get { return _xExtent; }
        }
        /// <summary>
        /// ExtentHeight contains the vertical size of the scrolled content element.
        /// </summary>
        /// <remarks>
        /// ExtentHeight is only an output property; it can effectively be set by specifying
        /// <see cref="System.Windows.FrameworkElement.Height" /> on the content element.
        /// </remarks>
        [Category("Layout")]
        public double ExtentHeight
        {
            get { return _yExtent; }
        }

        /// <summary>
        /// ScrollableWidth contains the horizontal size of the content element that can be scrolled.
        /// </summary>
        public double ScrollableWidth
        {
            get { return Math.Max(0.0, ExtentWidth - ViewportWidth); }
        }

        /// <summary>
        /// ScrollableHeight contains the vertical size of the content element that can be scrolled.
        /// </summary>
        public double ScrollableHeight
        {
            get { return Math.Max(0.0, ExtentHeight - ViewportHeight); }
        }

        /// <summary>
        /// ViewportWidth contains the horizontal size of the scrolling viewport.
        /// </summary>
        /// <remarks>
        /// ExtentWidth is only an output property; it can effectively be set by specifying
        /// <see cref="System.Windows.FrameworkElement.Width" /> on this element.
        /// </remarks>
        [Category("Layout")]
        public double ViewportWidth
        {
            get { return _xSize; }
        }
        /// <summary>
        /// ViewportHeight contains the vertical size of the scrolling viewport.
        /// </summary>
        /// <remarks>
        /// ViewportHeight is only an output property; it can effectively be set by specifying
        /// <see cref="System.Windows.FrameworkElement.Height" /> on this element.
        /// </remarks>
        [Category("Layout")]
        public double ViewportHeight
        {
            get { return _ySize; }
        }

        /// <summary>
        /// DependencyProperty for <see cref="CanContentScroll" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty CanContentScrollProperty =
                DependencyProperty.RegisterAttached(
                        "CanContentScroll",
                        typeof(bool),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// Helper for setting CanContentScroll property.
        /// </summary>
        public static void SetCanContentScroll(DependencyObject element, bool canContentScroll)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(CanContentScrollProperty, canContentScroll);
        }

        /// <summary>
        /// Helper for reading CanContentScroll property.
        /// </summary>
        public static bool GetCanContentScroll(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((bool)element.GetValue(CanContentScrollProperty));
        }

        /// <summary>
        /// DependencyProperty for <see cref="HorizontalScrollBarVisibility" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
                DependencyProperty.RegisterAttached(
                        "HorizontalScrollBarVisibility",
                        typeof(ScrollBarVisibility),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(
                                ScrollBarVisibility.Disabled,
                                FrameworkPropertyMetadataOptions.AffectsMeasure),
                        new ValidateValueCallback(IsValidScrollBarVisibility));

        /// <summary>
        /// Helper for setting HorizontalScrollBarVisibility property.
        /// </summary>
        public static void SetHorizontalScrollBarVisibility(DependencyObject element, ScrollBarVisibility horizontalScrollBarVisibility)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(HorizontalScrollBarVisibilityProperty, horizontalScrollBarVisibility);
        }

        /// <summary>
        /// Helper for reading HorizontalScrollBarVisibility property.
        /// </summary>
        public static ScrollBarVisibility GetHorizontalScrollBarVisibility(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((ScrollBarVisibility)element.GetValue(HorizontalScrollBarVisibilityProperty));
        }

        /// <summary>
        /// DependencyProperty for <see cref="VerticalScrollBarVisibility" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
                DependencyProperty.RegisterAttached(
                        "VerticalScrollBarVisibility",
                        typeof(ScrollBarVisibility),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(
                                ScrollBarVisibility.Visible,
                                FrameworkPropertyMetadataOptions.AffectsMeasure),
                        new ValidateValueCallback(IsValidScrollBarVisibility));

        /// <summary>
        /// Helper for setting VerticalScrollBarVisibility property.
        /// </summary>
        public static void SetVerticalScrollBarVisibility(DependencyObject element, ScrollBarVisibility verticalScrollBarVisibility)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(VerticalScrollBarVisibilityProperty, verticalScrollBarVisibility);
        }

        /// <summary>
        /// Helper for reading VerticalScrollBarVisibility property.
        /// </summary>
        public static ScrollBarVisibility GetVerticalScrollBarVisibility(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((ScrollBarVisibility)element.GetValue(VerticalScrollBarVisibilityProperty));
        }

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey ComputedHorizontalScrollBarVisibilityPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "ComputedHorizontalScrollBarVisibility",
                        typeof(Visibility),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(Visibility.Visible));

        /// <summary>
        /// Dependency property that indicates whether horizontal scrollbars should display.  The
        /// value of this property is computed by ScrollViewer; it can be controlled via the
        /// <see cref="HorizontalScrollBarVisibilityProperty" />
        /// </summary>
        public static readonly DependencyProperty ComputedHorizontalScrollBarVisibilityProperty =
                ComputedHorizontalScrollBarVisibilityPropertyKey.DependencyProperty;

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey ComputedVerticalScrollBarVisibilityPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "ComputedVerticalScrollBarVisibility",
                        typeof(Visibility),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(Visibility.Visible));

        /// <summary>
        /// Dependency property that indicates whether vertical scrollbars should display.  The
        /// value of this property is computed by ScrollViewer; it can be controlled via the
        /// <see cref="VerticalScrollBarVisibilityProperty" />
        /// </summary>
        public static readonly DependencyProperty ComputedVerticalScrollBarVisibilityProperty =
                ComputedVerticalScrollBarVisibilityPropertyKey.DependencyProperty;


        /// <summary>
        ///     Actual VerticalOffset.
        /// </summary>
        private static readonly DependencyPropertyKey VerticalOffsetPropertyKey =
            DependencyProperty.RegisterReadOnly(
                        "VerticalOffset",
                        typeof(double),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(0d));

        /// <summary>
        /// DependencyProperty for <see cref="VerticalOffset" /> property.
        /// </summary>
        public static readonly DependencyProperty VerticalOffsetProperty =
            VerticalOffsetPropertyKey.DependencyProperty;


        /// <summary>
        ///     HorizontalOffset.
        /// </summary>
        private static readonly DependencyPropertyKey HorizontalOffsetPropertyKey =
            DependencyProperty.RegisterReadOnly(
                        "HorizontalOffset",
                        typeof(double),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(0d));

        /// <summary>
        /// DependencyProperty for <see cref="HorizontalOffset" /> property.
        /// </summary>
        public static readonly DependencyProperty HorizontalOffsetProperty =
            HorizontalOffsetPropertyKey.DependencyProperty;

        /// <summary>
        ///     When not doing live scrolling, this is the offset value where the
        ///     content is visually located.
        /// </summary>
        private static readonly DependencyPropertyKey ContentVerticalOffsetPropertyKey =
            DependencyProperty.RegisterReadOnly(
                        "ContentVerticalOffset",
                        typeof(double),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(0d));

        /// <summary>
        ///     DependencyProperty for <see cref="ContentVerticalOffset" /> property.
        /// </summary>
        public static readonly DependencyProperty ContentVerticalOffsetProperty =
            ContentVerticalOffsetPropertyKey.DependencyProperty;

        /// <summary>
        ///     When not doing live scrolling, this is the offset value where the
        ///     content is visually located.
        /// </summary>
        public double ContentVerticalOffset
        {
            get
            {
                return (double)GetValue(ContentVerticalOffsetProperty);
            }

            private set
            {
                SetValue(ContentVerticalOffsetPropertyKey, value);
            }
        }

        /// <summary>
        ///     When not doing live scrolling, this is the offset value where the
        ///     content is visually located.
        /// </summary>
        private static readonly DependencyPropertyKey ContentHorizontalOffsetPropertyKey =
            DependencyProperty.RegisterReadOnly(
                        "ContentHorizontalOffset",
                        typeof(double),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(0d));

        /// <summary>
        ///     DependencyProperty for <see cref="ContentHorizontalOffset" /> property.
        /// </summary>
        public static readonly DependencyProperty ContentHorizontalOffsetProperty =
            ContentHorizontalOffsetPropertyKey.DependencyProperty;

        /// <summary>
        ///     When not doing live scrolling, this is the offset value where the
        ///     content is visually located.
        /// </summary>
        public double ContentHorizontalOffset
        {
            get
            {
                return (double)GetValue(ContentHorizontalOffsetProperty);
            }

            private set
            {
                SetValue(ContentHorizontalOffsetPropertyKey, value);
            }
        }

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey ExtentWidthPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "ExtentWidth",
                        typeof(double),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(0d));

        /// <summary>
        /// DependencyProperty for <see cref="ExtentWidth" /> property.
        /// </summary>
        public static readonly DependencyProperty ExtentWidthProperty =
            ExtentWidthPropertyKey.DependencyProperty;

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey ExtentHeightPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "ExtentHeight",
                        typeof(double),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(0d));

        /// <summary>
        /// DependencyProperty for <see cref="ExtentHeight" /> property.
        /// </summary>
        public static readonly DependencyProperty ExtentHeightProperty =
            ExtentHeightPropertyKey.DependencyProperty;

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey ScrollableWidthPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "ScrollableWidth",
                        typeof(double),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(0d));

        /// <summary>
        /// DependencyProperty for <see cref="ScrollableWidth" /> property.
        /// </summary>
        public static readonly DependencyProperty ScrollableWidthProperty =
            ScrollableWidthPropertyKey.DependencyProperty;

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey ScrollableHeightPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "ScrollableHeight",
                        typeof(double),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(0d));

        /// <summary>
        /// DependencyProperty for <see cref="ScrollableHeight" /> property.
        /// </summary>
        public static readonly DependencyProperty ScrollableHeightProperty =
            ScrollableHeightPropertyKey.DependencyProperty;

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey ViewportWidthPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "ViewportWidth",
                        typeof(double),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(0d));

        /// <summary>
        /// DependencyProperty for <see cref="ViewportWidth" /> property.
        /// </summary>
        public static readonly DependencyProperty ViewportWidthProperty =
            ViewportWidthPropertyKey.DependencyProperty;

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        internal static readonly DependencyPropertyKey ViewportHeightPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "ViewportHeight",
                        typeof(double),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(0d));


        /// <summary>
        /// DependencyProperty for <see cref="ViewportHeight" /> property.
        /// </summary>
        public static readonly DependencyProperty ViewportHeightProperty =
            ViewportHeightPropertyKey.DependencyProperty;

        /// <summary>
        ///     DependencyProperty that indicates whether the ScrollViewer should
        ///     scroll contents immediately during a thumb drag or defer until
        ///     a drag completes.
        /// </summary>
        public static readonly DependencyProperty IsDeferredScrollingEnabledProperty = DependencyProperty.RegisterAttached("IsDeferredScrollingEnabled", typeof(bool), typeof(ScrollViewer), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        ///     Gets the value of IsDeferredScrollingEnabled.
        /// </summary>
        /// <param name="element">The element on which to query the property.</param>
        /// <returns>The value of the property.</returns>
        public static bool GetIsDeferredScrollingEnabled(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (bool)element.GetValue(IsDeferredScrollingEnabledProperty);
        }

        /// <summary>
        ///     Sets the value of IsDeferredScrollingEnabled.
        /// </summary>
        /// <param name="element">The element on which to set the property.</param>
        /// <param name="value">The new value of the property.</param>
        public static void SetIsDeferredScrollingEnabled(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(IsDeferredScrollingEnabledProperty, BooleanBoxes.Box(value));
        }

        /// <summary>
        ///     Indicates whether the ScrollViewer should scroll contents
        ///     immediately during a thumb drag or defer until a drag completes.
        /// </summary>
        public bool IsDeferredScrollingEnabled
        {
            get
            {
                return (bool)GetValue(IsDeferredScrollingEnabledProperty);
            }

            set
            {
                SetValue(IsDeferredScrollingEnabledProperty, BooleanBoxes.Box(value));
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Events (CLR + Avalon)
        //
        //-------------------------------------------------------------------

        #region Public Events

        /// <summary>
        /// Event ID that corresponds to a change in scrolling state.
        /// See ScrollChangeEvent for the corresponding event handler.
        /// </summary>
        public static readonly RoutedEvent ScrollChangedEvent = EventManager.RegisterRoutedEvent(
            "ScrollChanged",
            RoutingStrategy.Bubble,
            typeof(ScrollChangedEventHandler),
            typeof(ScrollViewer));

        /// <summary>
        /// Event handler registration for the event fired when scrolling state changes.
        /// </summary>
        [Category("Action")]
        public event ScrollChangedEventHandler ScrollChanged
        {
            add { AddHandler(ScrollChangedEvent, value); }
            remove { RemoveHandler(ScrollChangedEvent, value); }
        }

        #endregion


        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        protected override void OnStylusSystemGesture(StylusSystemGestureEventArgs e)
        {
            // DevDiv:1139804
            // Keep track of seeing a tap gesture so that we can use this information to 
            // make decisions about panning.
            _seenTapGesture = e.SystemGesture == SystemGesture.Tap;
        }

        /// <summary>
        /// OnScrollChanged is an override called whenever scrolling state changes on this ScrollViewer.
        /// </summary>
        /// <remarks>
        /// OnScrollChanged fires the ScrollChangedEvent.  Overriders of this method should call
        /// base.OnScrollChanged(args) if they want the event to be fired.
        /// </remarks>
        /// <param name="e">ScrollChangedEventArgs containing information about the change in scrolling state.</param>
        protected virtual void OnScrollChanged(ScrollChangedEventArgs e)
        {
            // Fire the event.
            RaiseEvent(e);
        }

        /// <summary>
        /// ScrollViewer always wants to be hit even when transparent so that it gets input such as MouseWheel.
        /// </summary>
        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            // Assumptions:
            // 1. Input comes after layout, so Actual* are valid at this point
            // 2. The clipping part of scrolling is on the SCP, not SV.  Thus, Actual* not taking clipping into
            //    account is okay here, barring psychotic styles.
            Rect rc = new Rect(0, 0, this.ActualWidth, this.ActualHeight);
            if (rc.Contains(hitTestParameters.HitPoint))
            {
                return new PointHitTestResult(this, hitTestParameters.HitPoint);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        ///     If control has a scrollviewer in its style and has a custom keyboard scrolling behavior when HandlesScrolling should return true.
        /// Then ScrollViewer will not handle keyboard input and leave it up to the control.
        /// </summary>
        protected internal override bool HandlesScrolling
        {
            get { return true; }
        }

        /// <summary>
        /// ScrollArea handles keyboard scrolling events.
        /// ScrollArea handles:  Left, Right, Up, Down, PageUp, PageDown, Home, End
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Handled)
                return;

            Control templatedParentControl = TemplatedParent as Control;
            if (templatedParentControl != null && templatedParentControl.HandlesScrolling)
                return;

            // If the ScrollViewer has focus or other that arrow key is pressed
            // then it only scrolls
            if (e.OriginalSource == this)
            {
                ScrollInDirection(e);
            }
            // Focus is on the element within the ScrollViewer
            else
            {
                // If arrow key is pressed
                if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down)
                {
                    ScrollContentPresenter viewPort = GetTemplateChild(ScrollContentPresenterTemplateName) as ScrollContentPresenter;
                    // If style changes and ConentSite cannot be found - just scroll and exit
                    if (viewPort == null)
                    {
                        ScrollInDirection(e);
                        return;
                    }

                    FocusNavigationDirection direction = KeyboardNavigation.KeyToTraversalDirection(e.Key);
                    DependencyObject predictedFocus = null;
                    DependencyObject focusedElement = Keyboard.FocusedElement as DependencyObject;
                    bool isFocusWithinViewport = IsInViewport(viewPort, focusedElement);

                    if (isFocusWithinViewport)
                    {
                        // Navigate from current focused element
                        UIElement currentFocusUIElement = focusedElement as UIElement;
                        if (currentFocusUIElement != null)
                        {
                            predictedFocus = currentFocusUIElement.PredictFocus(direction);
                        }
                        else
                        {
                            ContentElement currentFocusContentElement = focusedElement as ContentElement;
                            if (currentFocusContentElement != null)
                            {
                                predictedFocus = currentFocusContentElement.PredictFocus(direction);
                            }
                            else
                            {
                                UIElement3D currentFocusUIElement3D = focusedElement as UIElement3D;
                                if (currentFocusUIElement3D != null)
                                {
                                    predictedFocus = currentFocusUIElement3D.PredictFocus(direction);
                                }
                            }
                        }
                    }
                    else
                    { // Navigate from current viewport
                        predictedFocus = viewPort.PredictFocus(direction);
                    }

                    if (predictedFocus == null)
                    {
                        // predictedFocus is null - just scroll
                        ScrollInDirection(e);
                    }
                    else
                    {
                        // Case 1: predictedFocus is entirely in current view port
                        // Action: Set focus to predictedFocus, handle the event and exit
                        if (IsInViewport(viewPort, predictedFocus))
                        {
                            ((IInputElement)predictedFocus).Focus();
                            e.Handled = true;
                        }
                        // Case 2: else - predictedFocus is not entirely in the viewport
                        // Scroll in the direction
                        // If predictedFocus is in the new viewport - set focus
                        // handle the event and exit
                        else
                        {
                            ScrollInDirection(e);
                            UpdateLayout();
                            if (IsInViewport(viewPort, predictedFocus))
                            {
                                ((IInputElement)predictedFocus).Focus();
                            }
                        }
                    }
                }
                else // If other than arrow Key is down
                {
                    ScrollInDirection(e);
                }
            }
        }


        // Returns true only if element is partly visible in the current viewport
        private bool IsInViewport(ScrollContentPresenter scp, DependencyObject element)
        {
            Visual baseRoot = KeyboardNavigation.GetVisualRoot(scp);
            Visual elementRoot = KeyboardNavigation.GetVisualRoot(element);

            // If scp and element are not under the same root, find the
            // parent of root of element and try with it instead and so on.
            while (baseRoot != elementRoot)
            {
                if (elementRoot == null)
                {
                    return false;
                }

                FrameworkElement fe = elementRoot as FrameworkElement;
                if (fe == null)
                {
                    return false;
                }

                element = fe.Parent;
                if (element == null)
                {
                    return false;
                }

                elementRoot = KeyboardNavigation.GetVisualRoot(element);
            }

            Rect viewPortRect = KeyboardNavigation.GetRectangle(scp);
            Rect elementRect = KeyboardNavigation.GetRectangle(element);
            return viewPortRect.IntersectsWith(elementRect);
        }

        internal void ScrollInDirection(KeyEventArgs e)
        {
            bool fControlDown = ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0);
            bool fAltDown = ((e.KeyboardDevice.Modifiers & ModifierKeys.Alt) != 0);

            // We don't handle Alt + Key
            if (!fAltDown)
            {
                bool fInvertForRTL = (FlowDirection == FlowDirection.RightToLeft);
                switch (e.Key)
                {
                    case Key.Left:
                        if (fInvertForRTL) LineRight(); else LineLeft();
                        e.Handled = true;
                        break;
                    case Key.Right:
                        if (fInvertForRTL) LineLeft(); else LineRight();
                        e.Handled = true;
                        break;
                    case Key.Up:
                        LineUp();
                        e.Handled = true;
                        break;
                    case Key.Down:
                        LineDown();
                        e.Handled = true;
                        break;
                    case Key.PageUp:
                        PageUp();
                        e.Handled = true;
                        break;
                    case Key.PageDown:
                        PageDown();
                        e.Handled = true;
                        break;
                    case Key.Home:
                        if (fControlDown) ScrollToTop(); else ScrollToLeftEnd();
                        e.Handled = true;
                        break;
                    case Key.End:
                        if (fControlDown) ScrollToBottom(); else ScrollToRightEnd();
                        e.Handled = true;
                        break;
                }
            }
        }

        /// <summary>
        /// This is the method that responds to the MouseWheel event.
        /// </summary>
        /// <param name="e">Event Arguments</param>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (e.Handled) { return; }

            if (!HandlesMouseWheelScrolling)
            {
                return;
            }

            if (ScrollInfo != null)
            {
                if (e.Delta < 0) { ScrollInfo.MouseWheelDown(); }
                else { ScrollInfo.MouseWheelUp(); }
            }

            e.Handled = true;
        }

        /// <summary>
        /// This is the method that responds to the MouseButtonEvent event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (Focus())
                e.Handled = true;
            base.OnMouseLeftButtonDown(e);
        }

        /// <summary>
        /// Updates DesiredSize of the ScrollViewer.  Called by parent UIElement.  This is the first pass of layout.
        /// </summary>
        /// <param name="constraint">Constraint size is an "upper limit" that the return value should not exceed.</param>
        /// <returns>The ScrollViewer's desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            InChildInvalidateMeasure = false;
            IScrollInfo isi = this.ScrollInfo;
            int count = this.VisualChildrenCount;

            UIElement child = (count > 0) ? this.GetVisualChild(0) as UIElement : null;
            ScrollBarVisibility vsbv = VerticalScrollBarVisibility;
            ScrollBarVisibility hsbv = HorizontalScrollBarVisibility;
            Size desiredSize = new Size();
            
            if (child != null)
            {
                bool etwTracingEnabled = EventTrace.IsEnabled(EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info);
                if (etwTracingEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringBegin, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, "SCROLLVIEWER:MeasureOverride");
                }

                try
                {
                    bool vsbAuto = (vsbv == ScrollBarVisibility.Auto);
                    bool hsbAuto = (hsbv == ScrollBarVisibility.Auto);
                    bool vDisableScroll = (vsbv == ScrollBarVisibility.Disabled);
                    bool hDisableScroll = (hsbv == ScrollBarVisibility.Disabled);
                    Visibility vv = (vsbv == ScrollBarVisibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
                    Visibility hv = (hsbv == ScrollBarVisibility.Visible) ? Visibility.Visible : Visibility.Collapsed;

                    if (_scrollVisibilityY != vv)
                    {
                        _scrollVisibilityY = vv;
                        SetValue(ComputedVerticalScrollBarVisibilityPropertyKey, _scrollVisibilityY);
                    }
                    if (_scrollVisibilityX != hv)
                    {
                        _scrollVisibilityX = hv;
                        SetValue(ComputedHorizontalScrollBarVisibilityPropertyKey, _scrollVisibilityX);
                    }

                    if (isi != null)
                    {
                        isi.CanHorizontallyScroll = !hDisableScroll;
                        isi.CanVerticallyScroll = !vDisableScroll;
                    }

                    try
                    {
                        // Measure our visual tree.
                        InChildMeasurePass1 = true;
                        child.Measure(constraint);
                    }
                    finally
                    {
                        InChildMeasurePass1 = false;
                    }

                    //it could now be here as a result of visual template expansion that happens during Measure
                    isi = this.ScrollInfo;

                    if (isi != null && (hsbAuto || vsbAuto))
                    {
                        bool makeHorizontalBarVisible = hsbAuto && DoubleUtil.GreaterThan(isi.ExtentWidth, isi.ViewportWidth);
                        bool makeVerticalBarVisible = vsbAuto && DoubleUtil.GreaterThan(isi.ExtentHeight, isi.ViewportHeight);

                        if (makeHorizontalBarVisible)
                        {
                            if (_scrollVisibilityX != Visibility.Visible)
                            {
                                _scrollVisibilityX = Visibility.Visible;
                                SetValue(ComputedHorizontalScrollBarVisibilityPropertyKey, _scrollVisibilityX);
                            }
                        }

                        if (makeVerticalBarVisible)
                        {
                            if (_scrollVisibilityY != Visibility.Visible)
                            {
                                _scrollVisibilityY = Visibility.Visible;
                                SetValue(ComputedVerticalScrollBarVisibilityPropertyKey, _scrollVisibilityY);
                            }
                        }

                        if (makeHorizontalBarVisible || makeVerticalBarVisible)
                        {
                            // Remeasure our visual tree.
                            // Requires this extra invalidation because we need to remeasure Grid which is not neccessarily dirty now
                            // since we only invlaidated scrollbars but we don't have LayoutUpdate loop at our disposal here
                            InChildInvalidateMeasure = true;
                            child.InvalidateMeasure();

                            try
                            {
                                InChildMeasurePass2 = true;
                                child.Measure(constraint);
                            }
                            finally
                            {
                                InChildMeasurePass2 = false;
                            }
                        }

                        //if both are Auto, then appearance of one scrollbar may causes appearance of another.
                        //If we don't re-check here, we get some part of content covered by auto scrollbar and can never reach to it since
                        //another scrollbar may not appear (in cases when viewport==extent) - bug 1199443
                        if(hsbAuto && vsbAuto && (makeHorizontalBarVisible != makeVerticalBarVisible))
                        {
                            bool makeHorizontalBarVisible2 = !makeHorizontalBarVisible && DoubleUtil.GreaterThan(isi.ExtentWidth, isi.ViewportWidth);
                            bool makeVerticalBarVisible2 = !makeVerticalBarVisible && DoubleUtil.GreaterThan(isi.ExtentHeight, isi.ViewportHeight);

                            if(makeHorizontalBarVisible2)
                            {
                                if (_scrollVisibilityX != Visibility.Visible)
                                {
                                    _scrollVisibilityX = Visibility.Visible;
                                    SetValue(ComputedHorizontalScrollBarVisibilityPropertyKey, _scrollVisibilityX);
                                }
                            }
                            else if (makeVerticalBarVisible2) //only one can be true
                            {
                                if (_scrollVisibilityY != Visibility.Visible)
                                {
                                    _scrollVisibilityY = Visibility.Visible;
                                    SetValue(ComputedVerticalScrollBarVisibilityPropertyKey, _scrollVisibilityY);
                                }
                            }

                            if (makeHorizontalBarVisible2 || makeVerticalBarVisible2)
                            {
                                // Remeasure our visual tree.
                                // Requires this extra invalidation because we need to remeasure Grid which is not neccessarily dirty now
                                // since we only invlaidated scrollbars but we don't have LayoutUpdate loop at our disposal here
                                InChildInvalidateMeasure = true;
                                child.InvalidateMeasure();

                                try
                                {
                                    InChildMeasurePass3 = true;
                                    child.Measure(constraint);
                                }
                                finally
                                {
                                    InChildMeasurePass3 = false;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    if (etwTracingEnabled)
                    {
                        EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringEnd, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, "SCROLLVIEWER:MeasureOverride");
                    }
                }


                desiredSize = child.DesiredSize;
            }


            if(!ArrangeDirty && InvalidatedMeasureFromArrange)
            {
                // If we invalidated measure from a previous arrange pass, but
                // if after the following measure pass we are not dirty for
                // arrange, then ArrangeOverride will not get called, and we
                // need to clean up our state here.
                InvalidatedMeasureFromArrange = false;
            }

            return desiredSize;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            bool previouslyInvalidatedMeasureFromArrange = InvalidatedMeasureFromArrange;

            Size size = base.ArrangeOverride(arrangeSize);

            if(previouslyInvalidatedMeasureFromArrange)
            {
                // If we invalidated measure from a previous arrange pass,
                // then we are not supposed to invalidate measure this time.
                Debug.Assert(!MeasureDirty);
                InvalidatedMeasureFromArrange = false;
            }
            else
            {
                InvalidatedMeasureFromArrange = MeasureDirty;
            }

            return size;
        }

        private void BindToTemplatedParent(DependencyProperty property)
        {
            if (!HasNonDefaultValue(property))
            {
                Binding binding = new Binding();
                binding.RelativeSource = RelativeSource.TemplatedParent;
                binding.Path = new PropertyPath(property);
                SetBinding(property, binding);
            }
        }

        /// <summary>
        /// ScrollViewer binds to the TemplatedParent's attached properties
        /// if they are not set directly on the ScrollViewer
        /// </summary>
        internal override void OnPreApplyTemplate()
        {
            base.OnPreApplyTemplate();

            if (TemplatedParent != null)
            {
                BindToTemplatedParent(HorizontalScrollBarVisibilityProperty);
                BindToTemplatedParent(VerticalScrollBarVisibilityProperty);
                BindToTemplatedParent(CanContentScrollProperty);
                BindToTemplatedParent(IsDeferredScrollingEnabledProperty);
                BindToTemplatedParent(PanningModeProperty);
            }
        }

        /// <summary>
        /// Called when the Template's tree has been generated
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ScrollBar scrollBar = GetTemplateChild(HorizontalScrollBarTemplateName) as ScrollBar;

            if (scrollBar != null)
                scrollBar.IsStandalone = false;

            scrollBar = GetTemplateChild(VerticalScrollBarTemplateName) as ScrollBar;

            if (scrollBar != null)
                scrollBar.IsStandalone = false;

            OnPanningModeChanged();
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Protected Propeties
        //
        //-------------------------------------------------------------------

        #region Protected Properties


        /// <summary>
        /// The ScrollInfo is the source of scrolling properties (Extent, Offset, and ViewportSize)
        /// for this ScrollViewer and any of its components like scrollbars.
        /// </summary>
        protected internal IScrollInfo ScrollInfo
        {
            get { return _scrollInfo; }
            set
            {
                _scrollInfo = value;
                if (_scrollInfo != null)
                {
                    _scrollInfo.CanHorizontallyScroll = (HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled);
                    _scrollInfo.CanVerticallyScroll = (VerticalScrollBarVisibility != ScrollBarVisibility.Disabled);
                    EnsureQueueProcessing();
                }
            }
        }

        #endregion

        #region Scroll Manipulations

        /// <summary>
        ///     The mode of manipulation based panning
        /// </summary>
        public PanningMode PanningMode
        {
            get { return (PanningMode)GetValue(PanningModeProperty); }
            set { SetValue(PanningModeProperty, value); }
        }

        /// <summary>
        ///     Dependency property for PanningMode property
        /// </summary>
        public static readonly DependencyProperty PanningModeProperty =
                DependencyProperty.RegisterAttached(
                        "PanningMode",
                        typeof(PanningMode),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(PanningMode.None, new PropertyChangedCallback(OnPanningModeChanged)));

        /// <summary>
        ///     Set method for PanningMode
        /// </summary>
        public static void SetPanningMode(DependencyObject element, PanningMode panningMode)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(PanningModeProperty, panningMode);
        }

        /// <summary>
        ///     Get method for PanningMode
        /// </summary>
        public static PanningMode GetPanningMode(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((PanningMode)element.GetValue(PanningModeProperty));
        }

        /// <summary>
        ///     Property changed callback for PanningMode.
        /// </summary>
        private static void OnPanningModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScrollViewer sv = d as ScrollViewer;
            if (sv != null)
            {
                sv.OnPanningModeChanged();
            }
        }

        /// <summary>
        ///     Method which sets IsManipulationEnabled
        ///     property based on the PanningMode
        /// </summary>
        private void OnPanningModeChanged()
        {
            if (!HasTemplateGeneratedSubTree)
            {
                return;
            }
            PanningMode mode = PanningMode;

            // Call InvalidateProperty for IsManipulationEnabledProperty
            // to reset previous SetCurrentValueInternal if any. 
            // Then call SetCurrentValueInternal to
            // set the value of these properties if needed.
            InvalidateProperty(IsManipulationEnabledProperty);

            if (mode != PanningMode.None)
            {
                SetCurrentValueInternal(IsManipulationEnabledProperty, BooleanBoxes.TrueBox);
            }
        }

        /// <summary>
        ///     The inertial linear deceleration of manipulation based scrolling
        /// </summary>
        public double PanningDeceleration
        {
            get { return (double)GetValue(PanningDecelerationProperty); }
            set { SetValue(PanningDecelerationProperty, value); }
        }

        /// <summary>
        ///     Dependency property for PanningDeceleration
        /// </summary>
        public static readonly DependencyProperty PanningDecelerationProperty =
                DependencyProperty.RegisterAttached(
                        "PanningDeceleration",
                        typeof(double),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(0.001),
                        new ValidateValueCallback(CheckFiniteNonNegative));

        /// <summary>
        ///     Set method for PanningDeceleration property
        /// </summary>
        public static void SetPanningDeceleration(DependencyObject element, double value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(PanningDecelerationProperty, value);
        }

        /// <summary>
        ///     Get method for PanningDeceleration property.
        /// </summary>
        public static double GetPanningDeceleration(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((double)element.GetValue(PanningDecelerationProperty));
        }

        /// <summary>
        ///     The Scroll pixels to panning pixels.
        /// </summary>
        public double PanningRatio
        {
            get { return (double)GetValue(PanningRatioProperty); }
            set { SetValue(PanningRatioProperty, value); }
        }

        /// <summary>
        ///      Dependency property for PanningRatio.
        /// </summary>
        public static readonly DependencyProperty PanningRatioProperty =
                DependencyProperty.RegisterAttached(
                        "PanningRatio",
                        typeof(double),
                        typeof(ScrollViewer),
                        new FrameworkPropertyMetadata(1d),
                        new ValidateValueCallback(CheckFiniteNonNegative));

        /// <summary>
        ///     Set method for PanningRatio property.
        /// </summary>
        public static void SetPanningRatio(DependencyObject element, double value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(PanningRatioProperty, value);
        }

        /// <summary>
        ///     Get method for PanningRatio property
        /// </summary>
        public static double GetPanningRatio(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return ((double)element.GetValue(PanningRatioProperty));
        }

        private static bool CheckFiniteNonNegative(object value)
        {
            double doubleValue = (double)value;
            return (DoubleUtil.GreaterThanOrClose(doubleValue, 0) &&
                !double.IsInfinity(doubleValue));
        }

        protected override void OnManipulationStarting(ManipulationStartingEventArgs e)
        {
            _panningInfo = null;

            // DevDiv:1139804
            // When starting a new manipulation, clear out that we saw a tap
            _seenTapGesture = false;

            PanningMode panningMode = PanningMode;
            if (panningMode != PanningMode.None)
            {
                CompleteScrollManipulation = false;
                ScrollContentPresenter viewport = GetTemplateChild(ScrollContentPresenterTemplateName) as ScrollContentPresenter;

                if (ShouldManipulateScroll(e, viewport))
                {
                    // Set Manipulation mode and container
                    if (panningMode == PanningMode.HorizontalOnly)
                    {
                        e.Mode = ManipulationModes.TranslateX;
                    }
                    else if (panningMode == PanningMode.VerticalOnly)
                    {
                        e.Mode = ManipulationModes.TranslateY;
                    }
                    else
                    {
                        e.Mode = ManipulationModes.Translate;
                    }
                    e.ManipulationContainer = this;

                    // initialize _panningInfo
                    _panningInfo = new PanningInfo()
                    {
                        OriginalHorizontalOffset = HorizontalOffset,
                        OriginalVerticalOffset = VerticalOffset,
                        PanningMode = panningMode
                    };

                    // Determine pixels per offset value. This is useful when performing non-pixel scrolling.

                    double viewportWidth = ViewportWidth + 1d; // Using +1 to account for last partially visible item in viewport
                    double viewportHeight = ViewportHeight + 1d; // Using +1 to account for last partially visible item in viewport
                    if (viewport != null)
                    {
                        _panningInfo.DeltaPerHorizontalOffet = (DoubleUtil.AreClose(viewportWidth, 0) ? 0 : viewport.ActualWidth / viewportWidth);
                        _panningInfo.DeltaPerVerticalOffset = (DoubleUtil.AreClose(viewportHeight, 0) ? 0 : viewport.ActualHeight / viewportHeight);
                    }
                    else
                    {
                        _panningInfo.DeltaPerHorizontalOffet = (DoubleUtil.AreClose(viewportWidth, 0) ? 0 : ActualWidth / viewportWidth);
                        _panningInfo.DeltaPerVerticalOffset = (DoubleUtil.AreClose(viewportHeight, 0) ? 0 : ActualHeight / viewportHeight);
                    }

                    // Template bind other Scroll Manipulation properties if needed.
                    if (!ManipulationBindingsInitialized)
                    {
                        BindToTemplatedParent(PanningDecelerationProperty);
                        BindToTemplatedParent(PanningRatioProperty);
                        ManipulationBindingsInitialized = true;
                    }
                }
                else
                {
                    e.Cancel();
                    ForceNextManipulationComplete = false;
                }
                e.Handled = true;
            }
        }

        private bool ShouldManipulateScroll(ManipulationStartingEventArgs e, ScrollContentPresenter viewport)
        {
            // If the original source is not from the same PresentationSource as of ScrollViewer,
            // then do not start the manipulation.
            if (!PresentationSource.UnderSamePresentationSource(e.OriginalSource as DependencyObject, this))
            {
                return false;
            }

            if (viewport == null)
            {
                // If there is no ScrollContentPresenter, then always start Manipulation
                return true;
            }

            // Dont start the manipulation if any of the manipulator positions
            // does not lie inside the viewport.
            GeneralTransform viewportTransform = TransformToDescendant(viewport);
            double viewportWidth = viewport.ActualWidth;
            double viewportHeight = viewport.ActualHeight;
            foreach (IManipulator manipulator in e.Manipulators)
            {
                Point manipulatorPosition = viewportTransform.Transform(manipulator.GetPosition(this));
                if (DoubleUtil.LessThan(manipulatorPosition.X, 0) ||
                    DoubleUtil.LessThan(manipulatorPosition.Y, 0) ||
                    DoubleUtil.GreaterThan(manipulatorPosition.X, viewportWidth) ||
                    DoubleUtil.GreaterThan(manipulatorPosition.Y, viewportHeight))
                {
                    return false;
                }
            }
            return true;
        }

        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            if (_panningInfo != null)
            {
                if (e.IsInertial && CompleteScrollManipulation)
                {
                    e.Complete();
                }
                else
                {
                    bool cancelManipulation = false;

                    // DevDiv:1139804
                    // High precision touch devices can trigger a panning manipulation
                    // due to the low threshold we set for pan initiation.  This may be
                    // undesirable since we may enter pan for what the system considers a
                    // tap.  Panning should be contingent on a drag gesture as that is the 
                    // most consistent with the system at large.  So if we have seen a tap
                    // on our main input, we should cancel any panning.
                    if (_seenTapGesture)
                    {
                        e.Cancel();
                        _panningInfo = null;
                    }
                    else if (_panningInfo.IsPanning)
                    {
                        // Do the scrolling if we already started it.
                        ManipulateScroll(e);
                    }
                    else if (CanStartScrollManipulation(e.CumulativeManipulation.Translation, out cancelManipulation))
                    {
                        // Check if we can start the scrolling and do accordingly
                        _panningInfo.IsPanning = true;
                        ManipulateScroll(e);
                    }
                    else if (cancelManipulation)
                    {
                        e.Cancel();
                        _panningInfo = null;
                    }
                }

                e.Handled = true;
            }
        }

        private void ManipulateScroll(ManipulationDeltaEventArgs e)
        {
            Debug.Assert(_panningInfo != null);
            PanningMode panningMode = _panningInfo.PanningMode;
            if (panningMode != PanningMode.VerticalOnly)
            {
                // Scroll horizontally unless the mode is VerticalOnly
                ManipulateScroll(e.DeltaManipulation.Translation.X, e.CumulativeManipulation.Translation.X, true);
            }

            if (panningMode != PanningMode.HorizontalOnly)
            {
                // Scroll vertically unless the mode is HorizontalOnly
                ManipulateScroll(e.DeltaManipulation.Translation.Y, e.CumulativeManipulation.Translation.Y, false);
            }

            if (e.IsInertial && IsPastInertialLimit())
            {
                e.Complete();
            }
            else
            {
                double unusedX = _panningInfo.UnusedTranslation.X;
                if (!_panningInfo.InHorizontalFeedback &&
                    DoubleUtil.LessThan(Math.Abs(unusedX), PanningInfo.PreFeedbackTranslationX))
                {
                    unusedX = 0;
                }
                _panningInfo.InHorizontalFeedback = (!DoubleUtil.AreClose(unusedX, 0));

                double unusedY = _panningInfo.UnusedTranslation.Y;
                if (!_panningInfo.InVerticalFeedback &&
                    DoubleUtil.LessThan(Math.Abs(unusedY), PanningInfo.PreFeedbackTranslationY))
                {
                    unusedY = 0;
                }
                _panningInfo.InVerticalFeedback = (!DoubleUtil.AreClose(unusedY, 0));

                if (_panningInfo.InHorizontalFeedback || _panningInfo.InVerticalFeedback)
                {
                    // Report boundary feedback if needed
                    e.ReportBoundaryFeedback(new ManipulationDelta(new Vector(unusedX, unusedY), 0.0, new Vector(1.0, 1.0), new Vector()));

                    if (e.IsInertial && _panningInfo.InertiaBoundaryBeginTimestamp == 0)
                    {
                        _panningInfo.InertiaBoundaryBeginTimestamp = Environment.TickCount;
                    }
                }
            }
        }

        private void ManipulateScroll(double delta, double cumulativeTranslation, bool isHorizontal)
        {
            double unused = (isHorizontal ? _panningInfo.UnusedTranslation.X : _panningInfo.UnusedTranslation.Y);
            double offset = (isHorizontal ? HorizontalOffset : VerticalOffset);
            double scrollableLength = (isHorizontal ? ScrollableWidth : ScrollableHeight);

            if (DoubleUtil.AreClose(scrollableLength, 0))
            {
                // If the Scrollable length in this direction is 0, 
                // then we should neither scroll nor report the boundary feedback
                unused = 0;
                delta = 0;
            }
            else if ((DoubleUtil.GreaterThan(delta, 0) && DoubleUtil.AreClose(offset, 0)) ||
                (DoubleUtil.LessThan(delta, 0) && DoubleUtil.AreClose(offset, scrollableLength)))
            {
                // If we are past the boundary and the delta is in the same direction,
                // then add the delta to the unused vector
                unused += delta;
                delta = 0;
            }
            else if (DoubleUtil.LessThan(delta, 0) && DoubleUtil.GreaterThan(unused, 0))
            {
                // If we are past the boundary in positive direction 
                // and the delta is in negative direction,
                // then compensate the delta from unused vector.
                double newUnused = Math.Max(unused + delta, 0);
                delta += unused - newUnused;
                unused = newUnused;
            }
            else if (DoubleUtil.GreaterThan(delta, 0) && DoubleUtil.LessThan(unused, 0))
            {
                // If we are past the boundary in negative direction 
                // and the delta is in positive direction,
                // then compensate the delta from unused vector.
                double newUnused = Math.Min(unused + delta, 0);
                delta += unused - newUnused;
                unused = newUnused;
            }

            if (isHorizontal)
            {
                if (!DoubleUtil.AreClose(delta, 0))
                {
                    // if there is any delta left, then re-evalute the horizontal offset
                    ScrollToHorizontalOffset(_panningInfo.OriginalHorizontalOffset -
                        Math.Round(PanningRatio * cumulativeTranslation / _panningInfo.DeltaPerHorizontalOffet));
                }
                _panningInfo.UnusedTranslation = new Vector(unused, _panningInfo.UnusedTranslation.Y);
            }
            else
            {
                if (!DoubleUtil.AreClose(delta, 0))
                {
                    // if there is any delta left, then re-evalute the vertical offset
                    ScrollToVerticalOffset(_panningInfo.OriginalVerticalOffset - 
                        Math.Round(PanningRatio * cumulativeTranslation / _panningInfo.DeltaPerVerticalOffset));
                }
                _panningInfo.UnusedTranslation = new Vector(_panningInfo.UnusedTranslation.X, unused);
            }
        }

        /// <summary>
        ///     Translation due to intertia past the boundary is restricted to a certain limit.
        ///     This method checks if the unused vector falls beyound that limit
        /// </summary>
        /// <returns></returns>
        private bool IsPastInertialLimit()
        {
            if (Math.Abs(Environment.TickCount - _panningInfo.InertiaBoundaryBeginTimestamp) < PanningInfo.InertiaBoundryMinimumTicks)
            {
                return false;
            }

            return (DoubleUtil.GreaterThanOrClose(Math.Abs(_panningInfo.UnusedTranslation.X), PanningInfo.MaxInertiaBoundaryTranslation) ||
                DoubleUtil.GreaterThanOrClose(Math.Abs(_panningInfo.UnusedTranslation.Y), PanningInfo.MaxInertiaBoundaryTranslation));
        }

        /// <summary>
        ///     Scrolling due to manipulation can start only if there is a considerable delta
        ///     in the direction based on the mode. This method makes sure that the delta is 
        ///     considerable.
        /// </summary>
        private bool CanStartScrollManipulation(Vector translation, out bool cancelManipulation)
        {
            Debug.Assert(_panningInfo != null);
            cancelManipulation = false;
            PanningMode panningMode = _panningInfo.PanningMode;
            if (panningMode == PanningMode.None)
            {
                cancelManipulation = true;
                return false;
            }

            bool validX = (DoubleUtil.GreaterThan(Math.Abs(translation.X), PanningInfo.PrePanTranslation));
            bool validY = (DoubleUtil.GreaterThan(Math.Abs(translation.Y), PanningInfo.PrePanTranslation));

            if (((panningMode == PanningMode.Both) && (validX || validY)) ||
                (panningMode == PanningMode.HorizontalOnly && validX) ||
                (panningMode == PanningMode.VerticalOnly && validY))
            {
                return true;
            }
            else if (panningMode == PanningMode.HorizontalFirst)
            {
                bool biggerX = (DoubleUtil.GreaterThanOrClose(Math.Abs(translation.X), Math.Abs(translation.Y)));
                if (validX && biggerX)
                {
                    return true;
                }
                else if (validY)
                {
                    cancelManipulation = true;
                    return false;
                }
            }
            else if (panningMode == PanningMode.VerticalFirst)
            {
                bool biggerY = (DoubleUtil.GreaterThanOrClose(Math.Abs(translation.Y), Math.Abs(translation.X)));
                if (validY && biggerY)
                {
                    return true;
                }
                else if (validX)
                {
                    cancelManipulation = true;
                    return false;
                }
            }

            return false;
        }

        protected override void OnManipulationInertiaStarting(ManipulationInertiaStartingEventArgs e)
        {
            if (_panningInfo != null)
            {
                if (!_panningInfo.IsPanning && !ForceNextManipulationComplete)
                {
                    // If the inertia starts and we are not scrolling yet, then cancel the manipulation.
                    e.Cancel();
                    _panningInfo = null;
                }
                else
                {
                    e.TranslationBehavior.DesiredDeceleration = PanningDeceleration;
                }
                e.Handled = true;
            }
        }

        protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            if (_panningInfo != null)
            {
                if (!(e.IsInertial && CompleteScrollManipulation))
                {
                    if (e.IsInertial &&
                        !DoubleUtil.AreClose(e.FinalVelocities.LinearVelocity, new Vector()) &&
                        !IsPastInertialLimit())
                    {
                        // if an inertial manipualtion gets completed without its LinearVelocity reaching 0,
                        // then most probably it was forced to complete by other manipulation.
                        // In such case we dont want the next manipulation to ever cancel.
                        ForceNextManipulationComplete = true;
                    }
                    else
                    {
                        if (!e.IsInertial && !_panningInfo.IsPanning && !ForceNextManipulationComplete)
                        {
                            // If we are not scrolling yet and the manipulation gets completed, then cancel the manipulation.
                            e.Cancel();
                        }
                        ForceNextManipulationComplete = false;
                    }
                }
                _panningInfo = null;
                CompleteScrollManipulation = false;
                e.Handled = true;
            }
        }

        private class PanningInfo
        {
            public PanningMode PanningMode
            {
                get;
                set;
            }

            public double OriginalHorizontalOffset
            {
                get;
                set;
            }

            public double OriginalVerticalOffset
            {
                get;
                set;
            }

            public double DeltaPerHorizontalOffet
            {
                get;
                set;
            }

            public double DeltaPerVerticalOffset
            {
                get;
                set;
            }

            public bool IsPanning
            {
                get;
                set;
            }

            public Vector UnusedTranslation
            {
                get;
                set;
            }

            public bool InHorizontalFeedback
            {
                get;
                set;
            }

            public bool InVerticalFeedback
            {
                get;
                set;
            }

            public int InertiaBoundaryBeginTimestamp
            {
                get;
                set;
            }

            public const double PrePanTranslation = 3d;
            public const double MaxInertiaBoundaryTranslation = 50d;
            public const double PreFeedbackTranslationX = 8d;
            public const double PreFeedbackTranslationY = 5d;
            public const int InertiaBoundryMinimumTicks = 100;
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Internal Propeties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Whether or not the ScrollViewer should handle mouse wheel events.  This property was
        /// specifically introduced for TextBoxBase, to prevent mouse wheel scrolling from "breaking"
        /// if the mouse pointer happens to land on a TextBoxBase with no more content in the direction
        /// of the scroll, as with a single-line TextBox.  In that scenario, ScrollViewer would
        /// try to scroll the TextBoxBase and not allow the scroll event to bubble up to an outer
        /// control even though the TextBoxBase doesn't scroll.
        ///
        /// This property defaults to true.  TextBoxBase sets it to false.
        /// </summary>
        internal bool HandlesMouseWheelScrolling
        {
            get
            {
                return ((_flags & Flags.HandlesMouseWheelScrolling) == Flags.HandlesMouseWheelScrolling);
            }
            set
            {
                SetFlagValue(Flags.HandlesMouseWheelScrolling, value);
            }
        }

        internal bool InChildInvalidateMeasure
        {
            get
            {
                return ((_flags & Flags.InChildInvalidateMeasure) == Flags.InChildInvalidateMeasure);
            }
            set
            {
                SetFlagValue(Flags.InChildInvalidateMeasure, value);
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private enum Commands
        {
            Invalid,
            LineUp,
            LineDown,
            LineLeft,
            LineRight,
            PageUp,
            PageDown,
            PageLeft,
            PageRight,
            SetHorizontalOffset,
            SetVerticalOffset,
            MakeVisible,
        }

        private struct Command
        {
            internal Command(Commands code, double param, MakeVisibleParams mvp)
            {
                Code = code;
                Param = param;
                MakeVisibleParam = mvp;
            }

            internal Commands Code;
            internal double Param;
            internal MakeVisibleParams MakeVisibleParam;
        }

        private class MakeVisibleParams
        {
            internal MakeVisibleParams(Visual child, Rect targetRect)
            {
                Child = child;
                TargetRect = targetRect;
            }
            internal Visual Child;
            internal Rect TargetRect;
        }

        // implements ring buffer of commands
        private struct CommandQueue
        {
            private const int _capacity = 32;

            //returns false if capacity is used up and entry ignored
            internal void Enqueue(Command command)
            {
                if(_lastWritePosition == _lastReadPosition) //buffer is empty
                {
                    _array = new Command[_capacity];
                    _lastWritePosition = _lastReadPosition = 0;
                }

                if(!OptimizeCommand(command)) //regular insertion, if optimization didn't happen
                {
                    _lastWritePosition = (_lastWritePosition + 1) % _capacity;

                    if(_lastWritePosition == _lastReadPosition) //buffer is full
                    {
                        // throw away the oldest entry and continue to accumulate fresh input
                        _lastReadPosition = (_lastReadPosition + 1) % _capacity;
                    }

                    _array[_lastWritePosition] = command;
                }
            }

            // this tries to "merge" the incoming command with the accumulated queue
            // for example, if we get SetHorizontalOffset incoming, all "horizontal"
            // commands in the queue get removed and replaced with incoming one,
            // since horizontal position is going to end up at the specified offset anyways.
            private bool OptimizeCommand(Command command)
            {
                if(_lastWritePosition != _lastReadPosition) //buffer has something
                {
                    if(   (   command.Code == Commands.SetHorizontalOffset
                           && _array[_lastWritePosition].Code == Commands.SetHorizontalOffset)
                       || (   command.Code == Commands.SetVerticalOffset
                           && _array[_lastWritePosition].Code == Commands.SetVerticalOffset)
                       || (command.Code == Commands.MakeVisible
                           && _array[_lastWritePosition].Code == Commands.MakeVisible))
                    {
                        //if the last command was "set offset" or "make visible", simply replace it and
                        //don't insert new command
                        _array[_lastWritePosition].Param = command.Param;
                        _array[_lastWritePosition].MakeVisibleParam = command.MakeVisibleParam;
                        return true;
                    }
                }
                return false;
            }

            // returns Invalid command if there is no more commands
            internal Command Fetch()
            {
                if(_lastWritePosition == _lastReadPosition) //buffer is empty
                {
                    return new Command(Commands.Invalid, 0, null);
                }
                _lastReadPosition = (_lastReadPosition + 1) % _capacity;

                //array exists always if writePos != readPos
                Command command = _array[_lastReadPosition];
                _array[_lastReadPosition].MakeVisibleParam = null; //to release the allocated object

                if(_lastWritePosition == _lastReadPosition) //it was the last command
                {
                    _array = null; // make GC work. Hopefully the whole queue is processed in Gen0
                }
                return command;
            }

            internal bool IsEmpty()
            {
                return (_lastWritePosition == _lastReadPosition);
            }

            private int _lastWritePosition;
            private int _lastReadPosition;
            private Command[] _array;
        }

        //returns true if there was a command sent to ISI
        private bool ExecuteNextCommand()
        {
            IScrollInfo isi = ScrollInfo;
            if(isi == null) return false;

            Command cmd = _queue.Fetch();
            switch(cmd.Code)
            {
                case Commands.LineUp:    isi.LineUp();    break;
                case Commands.LineDown:  isi.LineDown();  break;
                case Commands.LineLeft:  isi.LineLeft();  break;
                case Commands.LineRight: isi.LineRight(); break;

                case Commands.PageUp:    isi.PageUp();    break;
                case Commands.PageDown:  isi.PageDown();  break;
                case Commands.PageLeft:  isi.PageLeft();  break;
                case Commands.PageRight: isi.PageRight(); break;

                case Commands.SetHorizontalOffset: isi.SetHorizontalOffset(cmd.Param); break;
                case Commands.SetVerticalOffset:   isi.SetVerticalOffset(cmd.Param);   break;

                case Commands.MakeVisible:
                {
                    Visual child = cmd.MakeVisibleParam.Child;
                    Visual visi = isi as Visual;

                    if (    child != null
                        &&  visi != null
                        &&  (visi == child || visi.IsAncestorOf(child))
                        //  bug 1616807. ISI could be removed from visual tree,
                        //  but ScrollViewer.ScrollInfo may not reflect this yet.
                        &&  this.IsAncestorOf(visi) )
                    {
                        Rect targetRect = cmd.MakeVisibleParam.TargetRect;
                        if(targetRect.IsEmpty)
                        {
                            UIElement uie = child as UIElement;
                            if(uie != null)
                                targetRect = new Rect(uie.RenderSize);
                            else
                                targetRect = new Rect(); //not a good idea to invoke ISI with Empty rect
                        }

                        // (ScrollContentPresenter.MakeVisible can cause an exception when encountering an empty rectangle)
                        // Workaround:
                        // The method throws InvalidOperationException in some scenarios where it should return Rect.Empty.
                        // Do not workaround for derived classes.
                        Rect rcNew;
                        if(isi.GetType() == typeof(System.Windows.Controls.ScrollContentPresenter))
                        {
                            rcNew = ((System.Windows.Controls.ScrollContentPresenter)isi).MakeVisible(child, targetRect, false);
                        }
                        else
                        {
                            rcNew = isi.MakeVisible(child, targetRect);
                        }

                        if (!rcNew.IsEmpty)
                        {
                            GeneralTransform t = visi.TransformToAncestor(this);
                            rcNew = t.TransformBounds(rcNew);
                        }

                        BringIntoView(rcNew);
                    }
                }
                break;

                case Commands.Invalid: return false;
            }
            return true;
        }

        private void EnqueueCommand(Commands code, double param, MakeVisibleParams mvp)
        {
            _queue.Enqueue(new Command(code, param, mvp));
            EnsureQueueProcessing();
        }

        private void EnsureQueueProcessing()
        {
            if(!_queue.IsEmpty())
            {
                EnsureLayoutUpdatedHandler();
            }
        }

        // LayoutUpdated event handler.
        // 1. executes next queued command, if any
        // 2. If no commands to execute, updates properties and fires events
        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            // if there was a command, execute it and leave the handler for the next pass
            if(ExecuteNextCommand())
            {
                InvalidateArrange();
                return;
            }

            double oldActualHorizontalOffset = HorizontalOffset;
            double oldActualVerticalOffset = VerticalOffset;

            double oldViewportWidth = ViewportWidth;
            double oldViewportHeight = ViewportHeight;

            double oldExtentWidth = ExtentWidth;
            double oldExtentHeight = ExtentHeight;

            double oldScrollableWidth = ScrollableWidth;
            double oldScrollableHeight = ScrollableHeight;

            bool changed = false;

            //
            // Go through scrolling properties updating values.
            //
            if (ScrollInfo != null && !DoubleUtil.AreClose(oldActualHorizontalOffset, ScrollInfo.HorizontalOffset))
            {
                _xPositionISI = ScrollInfo.HorizontalOffset;
                HorizontalOffset = _xPositionISI;
                ContentHorizontalOffset = _xPositionISI;
                changed = true;
            }

            if (ScrollInfo != null && !DoubleUtil.AreClose(oldActualVerticalOffset, ScrollInfo.VerticalOffset))
            {
                _yPositionISI = ScrollInfo.VerticalOffset;
                VerticalOffset = _yPositionISI;
                ContentVerticalOffset = _yPositionISI;
                changed = true;
            }

            if (ScrollInfo != null && !DoubleUtil.AreClose(oldViewportWidth, ScrollInfo.ViewportWidth))
            {
                _xSize = ScrollInfo.ViewportWidth;
                SetValue(ViewportWidthPropertyKey, _xSize);
                changed = true;
            }

            if (ScrollInfo != null && !DoubleUtil.AreClose(oldViewportHeight, ScrollInfo.ViewportHeight))
            {
                _ySize = ScrollInfo.ViewportHeight;
                SetValue(ViewportHeightPropertyKey, _ySize);
                changed = true;
            }

            if (ScrollInfo != null && !DoubleUtil.AreClose(oldExtentWidth, ScrollInfo.ExtentWidth))
            {
                _xExtent = ScrollInfo.ExtentWidth;
                SetValue(ExtentWidthPropertyKey, _xExtent);
                changed = true;
            }

            if (ScrollInfo != null && !DoubleUtil.AreClose(oldExtentHeight, ScrollInfo.ExtentHeight))
            {
                _yExtent = ScrollInfo.ExtentHeight;
                SetValue(ExtentHeightPropertyKey, _yExtent);
                changed = true;
            }

            // ScrollableWidth/Height are dependant on Viewport and Extent set above.  This check must be done after those.
            double scrollableWidth = ScrollableWidth;
            if (!DoubleUtil.AreClose(oldScrollableWidth, ScrollableWidth))
            {
                SetValue(ScrollableWidthPropertyKey, scrollableWidth);
                changed = true;
            }

            double scrollableHeight = ScrollableHeight;
            if (!DoubleUtil.AreClose(oldScrollableHeight, ScrollableHeight))
            {
                SetValue(ScrollableHeightPropertyKey, scrollableHeight);
                changed = true;
            }

            Debug.Assert(DoubleUtil.GreaterThanOrClose(_xSize, 0.0) && DoubleUtil.GreaterThanOrClose(_ySize, 0.0), "Negative size for scrolling viewport.  Bad IScrollInfo implementation.");


            //
            // Fire scrolling events.
            //
            if(changed)
            {
                // Fire ScrollChange event
                ScrollChangedEventArgs args = new ScrollChangedEventArgs(
                    new Vector(HorizontalOffset, VerticalOffset),
                    new Vector(HorizontalOffset - oldActualHorizontalOffset, VerticalOffset - oldActualVerticalOffset),
                    new Size(ExtentWidth, ExtentHeight),
                    new Vector(ExtentWidth - oldExtentWidth, ExtentHeight - oldExtentHeight),
                    new Size(ViewportWidth, ViewportHeight),
                    new Vector(ViewportWidth - oldViewportWidth, ViewportHeight - oldViewportHeight));
                args.RoutedEvent = ScrollChangedEvent;
                args.Source = this;

                try
                {
                    OnScrollChanged(args);

                    // Fire automation events if automation is active.
                    ScrollViewerAutomationPeer peer = UIElementAutomationPeer.FromElement(this) as ScrollViewerAutomationPeer;
                    if(peer != null)
                    {
                        peer.RaiseAutomationEvents(oldExtentWidth,
                                                   oldExtentHeight,
                                                   oldViewportWidth,
                                                   oldViewportHeight,
                                                   oldActualHorizontalOffset,
                                                   oldActualVerticalOffset);
                    }
                }
                finally
                {
                    //
                    // Disconnect the layout listener.
                    //
                    ClearLayoutUpdatedHandler();
                }
            }

            ClearLayoutUpdatedHandler();
        }


        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ScrollViewerAutomationPeer(this);
        }

        /// <summary>
        /// OnRequestBringIntoView is called from the event handler ScrollViewer registers for the event.
        /// The default implementation checks to make sure the visual is a child of the IScrollInfo, and then
        /// delegates to a method there
        /// </summary>
        /// <param name="sender">The instance handling the event.</param>
        /// <param name="e">RequestBringIntoViewEventArgs indicates the element and region to scroll into view.</param>
        private static void OnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            ScrollViewer sv = sender as ScrollViewer;
            Visual child = e.TargetObject as Visual;

            if (child != null)
            {
                //the event starts from the elemetn itself, so if it is an SV.BringINtoView we would
                //get an SV trying to bring into view itself  - this does not work obviously
                //so don't handle if the request is about ourselves, the event will bubble
                if (child != sv && child.IsDescendantOf(sv))
                {
                    e.Handled = true;
                    sv.MakeVisible(child, e.TargetRect);
                }
            }
            else
            {
                ContentElement contentElement = e.TargetObject as ContentElement;
                if (contentElement != null)
                {
                    // We need to find the containing Visual and the bounding box for this element.
                    IContentHost contentHost = ContentHostHelper.FindContentHost(contentElement);
                    child = contentHost as Visual;

                    if (child != null && child.IsDescendantOf(sv))
                    {
                        ReadOnlyCollection<Rect> rects = contentHost.GetRectangles(contentElement);
                        if (rects.Count > 0)
                        {
                            e.Handled = true;
                            sv.MakeVisible(child, rects[0]);
                        }
                    }
                }
            }
        }

        private static void OnScrollCommand(object target, ExecutedRoutedEventArgs args)
        {
            if (args.Command == ScrollBar.DeferScrollToHorizontalOffsetCommand)
            {
                if (args.Parameter is double) { ((ScrollViewer)target).DeferScrollToHorizontalOffset((double)args.Parameter); }
            }
            else if (args.Command == ScrollBar.DeferScrollToVerticalOffsetCommand)
            {
                if (args.Parameter is double) { ((ScrollViewer)target).DeferScrollToVerticalOffset((double)args.Parameter); }
            }
            else if (args.Command == ScrollBar.LineLeftCommand)
            {
                ((ScrollViewer)target).LineLeft();
            }
            else if (args.Command == ScrollBar.LineRightCommand)
            {
                ((ScrollViewer)target).LineRight();
            }
            else if (args.Command == ScrollBar.PageLeftCommand)
            {
                ((ScrollViewer)target).PageLeft();
            }
            else if (args.Command == ScrollBar.PageRightCommand)
            {
                ((ScrollViewer)target).PageRight();
            }
            else if (args.Command == ScrollBar.LineUpCommand)
            {
                ((ScrollViewer)target).LineUp();
            }
            else if (args.Command == ScrollBar.LineDownCommand)
            {
                ((ScrollViewer)target).LineDown();
            }
            else if (   args.Command == ScrollBar.PageUpCommand
                    ||  args.Command == ComponentCommands.ScrollPageUp  )
            {
                ((ScrollViewer)target).PageUp();
            }
            else if (   args.Command == ScrollBar.PageDownCommand
                    ||  args.Command == ComponentCommands.ScrollPageDown    )
            {
                ((ScrollViewer)target).PageDown();
            }
            else if (args.Command == ScrollBar.ScrollToEndCommand)
            {
                ((ScrollViewer)target).ScrollToEnd();
            }
            else if (args.Command == ScrollBar.ScrollToHomeCommand)
            {
                ((ScrollViewer)target).ScrollToHome();
            }
            else if (args.Command == ScrollBar.ScrollToLeftEndCommand)
            {
                ((ScrollViewer)target).ScrollToLeftEnd();
            }
            else if (args.Command == ScrollBar.ScrollToRightEndCommand)
            {
                ((ScrollViewer)target).ScrollToRightEnd();
            }
            else if (args.Command == ScrollBar.ScrollToTopCommand)
            {
                ((ScrollViewer)target).ScrollToTop();
            }
            else if (args.Command == ScrollBar.ScrollToBottomCommand)
            {
                ((ScrollViewer)target).ScrollToBottom();
            }
            else if (args.Command == ScrollBar.ScrollToHorizontalOffsetCommand)
            {
                if (args.Parameter is double) { ((ScrollViewer)target).ScrollToHorizontalOffset((double)args.Parameter); }
            }
            else if (args.Command == ScrollBar.ScrollToVerticalOffsetCommand)
            {
                if (args.Parameter is double) { ((ScrollViewer)target).ScrollToVerticalOffset((double)args.Parameter); }
            }

            ScrollViewer sv = target as ScrollViewer;
            if (sv != null)
            {
                // If any of the ScrollBar scroll commands are raised while 
                // scroll manipulation is in its inertia, then the manipualtion
                // should be completed.
                sv.CompleteScrollManipulation = true;
            }
        }

        private static void OnQueryScrollCommand(object target, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;

            //  ScrollViewer is capable of execution of the majority of commands.
            //  The only special case is the component commands below.
            //  When scroll viewer is a primitive / part of another control
            //  capable to handle scrolling - scroll viewer leaves it up
            //  to the control to deal with component commands...
            if (    args.Command == ComponentCommands.ScrollPageUp
                ||  args.Command == ComponentCommands.ScrollPageDown    )
            {
                ScrollViewer scrollViewer = target as ScrollViewer;
                Control templatedParentControl = scrollViewer != null ? scrollViewer.TemplatedParent as Control : null;

                if (    templatedParentControl != null
                    &&  templatedParentControl.HandlesScrolling )
                {
                    args.CanExecute = false;
                    args.ContinueRouting = true;
                    
                    // It is important to handle this event to prevent any 
                    // other ScrollViewers in the ancestry from claiming it.
                    args.Handled = true;
                }
            }
            else if ((args.Command == ScrollBar.DeferScrollToHorizontalOffsetCommand) ||
                     (args.Command == ScrollBar.DeferScrollToVerticalOffsetCommand))
            {
                // The scroll bar has indicated that a drag operation is in progress.
                // If deferred scrolling is disabled, then mark the command as
                // not executable so that the scroll bar will fire the regular scroll
                // command, and the scroll viewer will do live scrolling.
                ScrollViewer scrollViewer = target as ScrollViewer;
                if ((scrollViewer != null) && !scrollViewer.IsDeferredScrollingEnabled)
                {
                    args.CanExecute = false;

                    // It is important to handle this event to prevent any 
                    // other ScrollViewers in the ancestry from claiming it.
                    args.Handled = true;
                }
            }
        }

        private static void InitializeCommands()
        {
            ExecutedRoutedEventHandler executeScrollCommandEventHandler = new ExecutedRoutedEventHandler(OnScrollCommand);
            CanExecuteRoutedEventHandler canExecuteScrollCommandEventHandler = new CanExecuteRoutedEventHandler(OnQueryScrollCommand);

            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.LineLeftCommand,          executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.LineRightCommand,         executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.PageLeftCommand,          executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.PageRightCommand,         executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.LineUpCommand,            executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.LineDownCommand,          executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.PageUpCommand,            executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.PageDownCommand,          executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.ScrollToLeftEndCommand,   executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.ScrollToRightEndCommand,  executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.ScrollToEndCommand,       executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.ScrollToHomeCommand,      executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.ScrollToTopCommand,       executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.ScrollToBottomCommand,    executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.ScrollToHorizontalOffsetCommand,  executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.ScrollToVerticalOffsetCommand,    executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.DeferScrollToHorizontalOffsetCommand, executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ScrollBar.DeferScrollToVerticalOffsetCommand,   executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);

            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ComponentCommands.ScrollPageUp,     executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
            CommandHelpers.RegisterCommandHandler(typeof(ScrollViewer), ComponentCommands.ScrollPageDown,   executeScrollCommandEventHandler, canExecuteScrollCommandEventHandler);
        }

        // Creates the default control template for ScrollViewer.
        private static ControlTemplate CreateDefaultControlTemplate()
        {
            ControlTemplate template = null;

            // Our default style is a 2x2 grid:
            // <Grid Columns="*,Auto" Rows="*,Auto">        // Grid
            //   <ColumnDefinition Width="*" />
            //   <ColumnDefinition Width="Auto" />
            //   <RowDefinition Height="*" />
            //   <RowDefinition Height="Auto" />
            //   <Border>                                   // Cell 1-2, 1-2
            //     <ScrollContentPresenter />
            //   </Border>
            //   <VerticalScrollBar  />                     // Cell 1, 2
            //   <HorizontalScrollBar />                    // Cell 2, 1
            // </Grid>
            FrameworkElementFactory grid = new FrameworkElementFactory(typeof(Grid), "Grid");
            FrameworkElementFactory gridColumn1 = new FrameworkElementFactory(typeof(ColumnDefinition), "ColumnDefinitionOne");
            FrameworkElementFactory gridColumn2 = new FrameworkElementFactory(typeof(ColumnDefinition), "ColumnDefinitionTwo");
            FrameworkElementFactory gridRow1 = new FrameworkElementFactory(typeof(RowDefinition), "RowDefinitionOne");
            FrameworkElementFactory gridRow2 = new FrameworkElementFactory(typeof(RowDefinition), "RowDefinitionTwo");
            FrameworkElementFactory vsb = new FrameworkElementFactory(typeof(ScrollBar), VerticalScrollBarTemplateName);
            FrameworkElementFactory hsb = new FrameworkElementFactory(typeof(ScrollBar), HorizontalScrollBarTemplateName);
            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ScrollContentPresenter), ScrollContentPresenterTemplateName);
            FrameworkElementFactory corner = new FrameworkElementFactory(typeof(Rectangle), "Corner");

            // Bind Actual HorizontalOffset to HorizontalScrollBar.Value
            // Bind Actual VerticalOffset to VerticalScrollbar.Value
            Binding bindingHorizontalOffset = new Binding("HorizontalOffset");
            bindingHorizontalOffset.Mode = BindingMode.OneWay;
            bindingHorizontalOffset.RelativeSource = RelativeSource.TemplatedParent;
            Binding bindingVerticalOffset = new Binding("VerticalOffset");
            bindingVerticalOffset.Mode = BindingMode.OneWay;
            bindingVerticalOffset.RelativeSource = RelativeSource.TemplatedParent;

            grid.SetValue(Grid.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
            grid.AppendChild(gridColumn1);
            grid.AppendChild(gridColumn2);
            grid.AppendChild(gridRow1);
            grid.AppendChild(gridRow2);
            grid.AppendChild(corner);
            grid.AppendChild(content);
            grid.AppendChild(vsb);
            grid.AppendChild(hsb);

            gridColumn1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1.0, GridUnitType.Star));
            gridColumn2.SetValue(ColumnDefinition.WidthProperty, new GridLength(1.0, GridUnitType.Auto));
            gridRow1.SetValue(RowDefinition.HeightProperty, new GridLength(1.0, GridUnitType.Star));
            gridRow2.SetValue(RowDefinition.HeightProperty, new GridLength(1.0, GridUnitType.Auto));

            content.SetValue(Grid.ColumnProperty, 0);
            content.SetValue(Grid.RowProperty, 0);
            content.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(PaddingProperty));
            content.SetValue(ContentProperty, new TemplateBindingExtension(ContentProperty));
            content.SetValue(ContentTemplateProperty, new TemplateBindingExtension(ContentTemplateProperty));
            content.SetValue(CanContentScrollProperty, new TemplateBindingExtension(CanContentScrollProperty));

            hsb.SetValue(ScrollBar.OrientationProperty, Orientation.Horizontal);
            hsb.SetValue(Grid.ColumnProperty, 0);
            hsb.SetValue(Grid.RowProperty, 1);
            hsb.SetValue(RangeBase.MinimumProperty, 0.0);
            hsb.SetValue(RangeBase.MaximumProperty, new TemplateBindingExtension(ScrollableWidthProperty));
            hsb.SetValue(ScrollBar.ViewportSizeProperty, new TemplateBindingExtension(ViewportWidthProperty));
            hsb.SetBinding(RangeBase.ValueProperty, bindingHorizontalOffset);
            hsb.SetValue(UIElement.VisibilityProperty, new TemplateBindingExtension(ComputedHorizontalScrollBarVisibilityProperty));
            hsb.SetValue(FrameworkElement.CursorProperty, Cursors.Arrow);
            hsb.SetValue(AutomationProperties.AutomationIdProperty, "HorizontalScrollBar");

            vsb.SetValue(Grid.ColumnProperty, 1);
            vsb.SetValue(Grid.RowProperty, 0);
            vsb.SetValue(RangeBase.MinimumProperty, 0.0);
            vsb.SetValue(RangeBase.MaximumProperty, new TemplateBindingExtension(ScrollableHeightProperty));
            vsb.SetValue(ScrollBar.ViewportSizeProperty, new TemplateBindingExtension(ViewportHeightProperty));
            vsb.SetBinding(RangeBase.ValueProperty, bindingVerticalOffset);
            vsb.SetValue(UIElement.VisibilityProperty, new TemplateBindingExtension(ComputedVerticalScrollBarVisibilityProperty));
            vsb.SetValue(FrameworkElement.CursorProperty, Cursors.Arrow);
            vsb.SetValue(AutomationProperties.AutomationIdProperty, "VerticalScrollBar");

            corner.SetValue(Grid.ColumnProperty, 1);
            corner.SetValue(Grid.RowProperty, 1);
            corner.SetResourceReference(Rectangle.FillProperty, SystemColors.ControlBrushKey);

            template = new ControlTemplate(typeof(ScrollViewer));
            template.VisualTree = grid;
            template.Seal();

            return (template);
        }

        [Flags]
        private enum Flags
        {
            None                            = 0x0000,
            InvalidatedMeasureFromArrange   = 0x0001,
            InChildInvalidateMeasure        = 0x0002,
            HandlesMouseWheelScrolling      = 0x0004,
            ForceNextManipulationComplete   = 0x0008,
            ManipulationBindingsInitialized = 0x0010,
            CompleteScrollManipulation      = 0x0020,
            InChildMeasurePass1             = 0x0040,
            InChildMeasurePass2             = 0x0080,
            InChildMeasurePass3             = 0x00C0,
        }

        private void SetFlagValue(Flags flag, bool value)
        {
            if (value)
            {
                _flags |= flag;
            }
            else
            {
                _flags &= ~flag;
            }
        }

        private bool InvalidatedMeasureFromArrange
        {
            get
            {
                return ((_flags & Flags.InvalidatedMeasureFromArrange) == Flags.InvalidatedMeasureFromArrange);
            }
            set
            {
                SetFlagValue(Flags.InvalidatedMeasureFromArrange, value);
            }
        }

        private bool ForceNextManipulationComplete
        {
            get
            {
                return ((_flags & Flags.ForceNextManipulationComplete) == Flags.ForceNextManipulationComplete);
            }
            set
            {
                SetFlagValue(Flags.ForceNextManipulationComplete, value);
            }
        }

        private bool ManipulationBindingsInitialized
        {
            get
            {
                return ((_flags & Flags.ManipulationBindingsInitialized) == Flags.ManipulationBindingsInitialized);
            }
            set
            {
                SetFlagValue(Flags.ManipulationBindingsInitialized, value);
            }
        }

        private bool CompleteScrollManipulation
        {
            get
            {
                return ((_flags & Flags.CompleteScrollManipulation) == Flags.CompleteScrollManipulation);
            }
            set
            {
                SetFlagValue(Flags.CompleteScrollManipulation, value);
            }
        }

        internal bool InChildMeasurePass1
        {
            get
            {
                return ((_flags & Flags.InChildMeasurePass1) == Flags.InChildMeasurePass1);
            }
            set
            {
                SetFlagValue(Flags.InChildMeasurePass1, value);
            }
        }

        internal bool InChildMeasurePass2
        {
            get
            {
                return ((_flags & Flags.InChildMeasurePass2) == Flags.InChildMeasurePass2);
            }
            set
            {
                SetFlagValue(Flags.InChildMeasurePass2, value);
            }
        }

        internal bool InChildMeasurePass3
        {
            get
            {
                return ((_flags & Flags.InChildMeasurePass3) == Flags.InChildMeasurePass3);
            }
            set
            {
                SetFlagValue(Flags.InChildMeasurePass3, value);
            }
        }

        #endregion


        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        // DevDiv:1139804
        // Tracks if the current main input to the ScrollViewer result in a Tap gesture
        private bool _seenTapGesture = false;

        // Scrolling physical "line" metrics.
        internal const double _scrollLineDelta = 16.0;   // Default physical amount to scroll with one Up/Down/Left/Right key
        internal const double _mouseWheelDelta = 48.0;   // Default physical amount to scroll with one MouseWheel.

        private const string HorizontalScrollBarTemplateName = "PART_HorizontalScrollBar";
        private const string VerticalScrollBarTemplateName = "PART_VerticalScrollBar";
        internal const string ScrollContentPresenterTemplateName = "PART_ScrollContentPresenter";

        // Property caching
        private Visibility _scrollVisibilityX;
        private Visibility _scrollVisibilityY;

        // Scroll property values - cache of what was computed by ISI
        private double _xPositionISI;
        private double _yPositionISI;
        private double _xExtent;
        private double _yExtent;
        private double _xSize;
        private double _ySize;

        // Event/infrastructure
        private EventHandler _layoutUpdatedHandler;
        private IScrollInfo _scrollInfo;

        private CommandQueue _queue;

        private PanningInfo _panningInfo = null;
        private Flags _flags = Flags.HandlesMouseWheelScrolling;
        
        #endregion

        //-------------------------------------------------------------------
        //
        //  Static Constructors & Delegates
        //
        //-------------------------------------------------------------------

        #region Static Constructors & Delegates

        static ScrollViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ScrollViewer), new FrameworkPropertyMetadata(typeof(ScrollViewer)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(ScrollViewer));

            InitializeCommands();

            ControlTemplate template = CreateDefaultControlTemplate();
            Control.TemplateProperty.OverrideMetadata(typeof(ScrollViewer), new FrameworkPropertyMetadata(template));
            IsTabStopProperty.OverrideMetadata(typeof(ScrollViewer), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(ScrollViewer), new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));

            EventManager.RegisterClassHandler(typeof(ScrollViewer), RequestBringIntoViewEvent, new RequestBringIntoViewEventHandler(OnRequestBringIntoView));

            ControlsTraceLogger.AddControl(TelemetryControls.ScrollViewer);
        }

        private static bool IsValidScrollBarVisibility(object o)
        {
            ScrollBarVisibility value = (ScrollBarVisibility)o;
            return (value == ScrollBarVisibility.Disabled
                || value == ScrollBarVisibility.Auto
                || value == ScrollBarVisibility.Hidden
                || value == ScrollBarVisibility.Visible);
        }

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 28; }
        }

        #endregion

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey
    }
}

