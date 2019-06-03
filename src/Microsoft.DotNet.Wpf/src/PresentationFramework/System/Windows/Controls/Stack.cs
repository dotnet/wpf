// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Implementation of StackPanel class.
//              Spec at http://avalon/layout/Specs/StackPanel.doc
//

//#define Profiling

using MS.Internal;
using MS.Internal.Telemetry.PresentationFramework;
using MS.Utility;

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Internal interface for elements which needs stack like measure
    /// </summary>
    internal interface IStackMeasure
    {
        bool IsScrolling { get; }
        UIElementCollection InternalChildren { get; }
        Orientation Orientation { get; }
        bool CanVerticallyScroll { get; }
        bool CanHorizontallyScroll { get; }
        void OnScrollChange();
    }

    /// <summary>
    ///     Internal interface for scrolling information of elements which
    ///     need stack like measure.
    /// </summary>
    internal interface IStackMeasureScrollData
    {
        Vector Offset { get; set; }
        Size Viewport { get; set; }
        Size Extent { get; set; }
        Vector ComputedOffset { get; set; }
        void SetPhysicalViewport(double value);
    }

    /// <summary>
    /// StackPanel is used to arrange children into single line.
    /// </summary>
    public class StackPanel : Panel, IScrollInfo, IStackMeasure
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        static StackPanel()
        {
            ControlsTraceLogger.AddControl(TelemetryControls.StackPanel);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public StackPanel() : base()
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        //-----------------------------------------------------------
        //  IScrollInfo Methods
        //-----------------------------------------------------------
        #region IScrollInfo Methods

        /// <summary>
        /// Scroll content by one line to the top.
        /// </summary>
        public void LineUp()
        {
            SetVerticalOffset(VerticalOffset - ((Orientation == Orientation.Vertical) ? 1.0 : ScrollViewer._scrollLineDelta));
        }

        /// <summary>
        /// Scroll content by one line to the bottom.
        /// </summary>
        public void LineDown()
        {
            SetVerticalOffset(VerticalOffset + ((Orientation == Orientation.Vertical) ? 1.0 : ScrollViewer._scrollLineDelta));
        }

        /// <summary>
        /// Scroll content by one line to the left.
        /// </summary>
        public void LineLeft()
        {
            SetHorizontalOffset(HorizontalOffset - ((Orientation == Orientation.Horizontal) ? 1.0 : ScrollViewer._scrollLineDelta));
        }

        /// <summary>
        /// Scroll content by one line to the right.
        /// </summary>
        public void LineRight()
        {
            SetHorizontalOffset(HorizontalOffset + ((Orientation == Orientation.Horizontal) ? 1.0 : ScrollViewer._scrollLineDelta));
        }

        /// <summary>
        /// Scroll content by one page to the top.
        /// </summary>
        public void PageUp()
        {
            SetVerticalOffset(VerticalOffset - ViewportHeight);
        }

        /// <summary>
        /// Scroll content by one page to the bottom.
        /// </summary>
        public void PageDown()
        {
            SetVerticalOffset(VerticalOffset + ViewportHeight);
        }

        /// <summary>
        /// Scroll content by one page to the left.
        /// </summary>
        public void PageLeft()
        {
            SetHorizontalOffset(HorizontalOffset - ViewportWidth);
        }

        /// <summary>
        /// Scroll content by one page to the right.
        /// </summary>
        public void PageRight()
        {
            SetHorizontalOffset(HorizontalOffset + ViewportWidth);
        }

        /// <summary>
        /// Scroll content by one page to the top.
        /// </summary>
        public void MouseWheelUp()
        {
            if (CanMouseWheelVerticallyScroll)
            {
                SetVerticalOffset(VerticalOffset - SystemParameters.WheelScrollLines * ((Orientation == Orientation.Vertical) ? 1.0 : ScrollViewer._scrollLineDelta));
            }
            else
            {
                PageUp();
            }
        }

        /// <summary>
        /// Scroll content by one page to the bottom.
        /// </summary>
        public void MouseWheelDown()
        {
            if (CanMouseWheelVerticallyScroll)
            {
                SetVerticalOffset(VerticalOffset + SystemParameters.WheelScrollLines * ((Orientation == Orientation.Vertical) ? 1.0 : ScrollViewer._scrollLineDelta));
            }
            else
            {
                PageDown();
            }
        }

        /// <summary>
        /// Scroll content by one page to the left.
        /// </summary>
        public void MouseWheelLeft()
        {
            SetHorizontalOffset(HorizontalOffset - 3.0 * ((Orientation == Orientation.Horizontal) ? 1.0 : ScrollViewer._scrollLineDelta));
        }

        /// <summary>
        /// Scroll content by one page to the right.
        /// </summary>
        public void MouseWheelRight()
        {
            SetHorizontalOffset(HorizontalOffset + 3.0 * ((Orientation == Orientation.Horizontal) ? 1.0 : ScrollViewer._scrollLineDelta));
        }

        /// <summary>
        /// Set the HorizontalOffset to the passed value.
        /// </summary>
        public void SetHorizontalOffset(double offset)
        {
            EnsureScrollData();
            double scrollX = ScrollContentPresenter.ValidateInputOffset(offset, "HorizontalOffset");
            if (!DoubleUtil.AreClose(scrollX, _scrollData._offset.X))
            {
                _scrollData._offset.X = scrollX;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Set the VerticalOffset to the passed value.
        /// </summary>
        public void SetVerticalOffset(double offset)
        {
            EnsureScrollData();
            double scrollY = ScrollContentPresenter.ValidateInputOffset(offset, "VerticalOffset");
            if (!DoubleUtil.AreClose(scrollY, _scrollData._offset.Y))
            {
                _scrollData._offset.Y = scrollY;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// StackPanel implementation of <seealso cref="IScrollInfo.MakeVisible" />.
        /// </summary>
        // The goal is to change offsets to bring the child into view, and return a rectangle in our space to make visible.
        // The rectangle we return is in the physical dimension the input target rect transformed into our pace.
        // In the logical dimension, it is our immediate child's rect.
        // Note: This code presently assumes we/children are layout clean.  See work item 22269 for more detail.
        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            Vector newOffset = new Vector();
            Rect newRect = new Rect();

            // We can only work on visuals that are us or children.
            // An empty rect has no size or position.  We can't meaningfully use it.
            if (    rectangle.IsEmpty
                ||  visual == null
                ||  visual == (Visual)this
                ||  !this.IsAncestorOf(visual))
            {
                return Rect.Empty;
            }
#pragma warning disable 1634, 1691
#pragma warning disable 56506
            // Compute the child's rect relative to (0,0) in our coordinate space.
            // This is a false positive by PreSharp. visual cannot be null because of the 'if' check above
            GeneralTransform childTransform = visual.TransformToAncestor(this);
#pragma warning restore 56506
#pragma warning restore 1634, 1691
            rectangle = childTransform.TransformBounds(rectangle);

            // We can't do any work unless we're scrolling.
            if (!IsScrolling)
            {
                return rectangle;
            }

            // Bring the target rect into view in the physical dimension.
            MakeVisiblePhysicalHelper(rectangle, ref newOffset, ref newRect);

            // Bring our child containing the visual into view.
            int childIndex = FindChildIndexThatParentsVisual(visual);
            MakeVisibleLogicalHelper(childIndex, ref newOffset, ref newRect);

            // We have computed the scrolling offsets; validate and scroll to them.
            newOffset.X = ScrollContentPresenter.CoerceOffset(newOffset.X, _scrollData._extent.Width, _scrollData._viewport.Width);
            newOffset.Y = ScrollContentPresenter.CoerceOffset(newOffset.Y, _scrollData._extent.Height, _scrollData._viewport.Height);
            if (!DoubleUtil.AreClose(newOffset, _scrollData._offset))
            {
                _scrollData._offset = newOffset;
                InvalidateMeasure();
                OnScrollChange();
            }

            // Return the rectangle
            return newRect;
        }

        #endregion

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Specifies dimension of children stacking.
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation) GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Orientation" /> property.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
                DependencyProperty.Register(
                        "Orientation",
                        typeof(Orientation),
                        typeof(StackPanel),
                        new FrameworkPropertyMetadata(
                                Orientation.Vertical,
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnOrientationChanged)),
                        new ValidateValueCallback(ScrollBar.IsValidOrientation));

        /// <summary>
        /// This property is always true because this panel has vertical or horizontal orientation
        /// </summary>
        protected internal override bool HasLogicalOrientation
        {
            get { return true; }
        }

        /// <summary>
        ///     Orientation of the panel if its layout is in one dimension.
        /// Otherwise HasLogicalOrientation is false and LogicalOrientation should be ignored
        /// </summary>
        protected internal override Orientation LogicalOrientation
        {
            get { return this.Orientation; }
        }

        //-----------------------------------------------------------
        //  IScrollInfo Properties
        //-----------------------------------------------------------
        #region IScrollInfo Properties

        /// <summary>
        /// StackPanel reacts to this property by changing it's child measurement algorithm.
        /// If scrolling in a dimension, infinite space is allowed the child; otherwise, available size is preserved.
        /// </summary>
        [DefaultValue(false)]
        public bool CanHorizontallyScroll
        {
            get
            {
                if (_scrollData == null) { return false; }
                return _scrollData._allowHorizontal;
            }
            set
            {
                EnsureScrollData();
                if (_scrollData._allowHorizontal != value)
                {
                    _scrollData._allowHorizontal = value;
                    InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// StackPanel reacts to this property by changing it's child measurement algorithm.
        /// If scrolling in a dimension, infinite space is allowed the child; otherwise, available size is preserved.
        /// </summary>
        [DefaultValue(false)]
        public bool CanVerticallyScroll
        {
            get
            {
                if (_scrollData == null) { return false; }
                return _scrollData._allowVertical;
            }
            set
            {
                EnsureScrollData();
                if (_scrollData._allowVertical != value)
                {
                    _scrollData._allowVertical = value;
                    InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// ExtentWidth contains the horizontal size of the scrolled content element in 1/96"
        /// </summary>
        public double ExtentWidth
        {
            get
            {
                if (_scrollData == null) { return 0.0; }
                return _scrollData._extent.Width;
            }
        }

        /// <summary>
        /// ExtentHeight contains the vertical size of the scrolled content element in 1/96"
        /// </summary>
        public double ExtentHeight
        {
            get
            {
                if (_scrollData == null) { return 0.0; }
                return _scrollData._extent.Height;
            }
        }

        /// <summary>
        /// ViewportWidth contains the horizontal size of content's visible range in 1/96"
        /// </summary>
        public double ViewportWidth
        {
            get
            {
                if (_scrollData == null) { return 0.0; }
                return _scrollData._viewport.Width;
            }
        }

        /// <summary>
        /// ViewportHeight contains the vertical size of content's visible range in 1/96"
        /// </summary>
        public double ViewportHeight
        {
            get
            {
                if (_scrollData == null) { return 0.0; }
                return _scrollData._viewport.Height;
            }
        }

        /// <summary>
        /// HorizontalOffset is the horizontal offset of the scrolled content in 1/96".
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double HorizontalOffset
        {
            get
            {
                if (_scrollData == null) { return 0.0; }
                return _scrollData._computedOffset.X;
            }
        }

        /// <summary>
        /// VerticalOffset is the vertical offset of the scrolled content in 1/96".
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double VerticalOffset
        {
            get
            {
                if (_scrollData == null) { return 0.0; }
                return _scrollData._computedOffset.Y;
            }
        }

        /// <summary>
        /// ScrollOwner is the container that controls any scrollbars, headers, etc... that are dependant
        /// on this IScrollInfo's properties.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ScrollViewer ScrollOwner
        {
            get
            {
                EnsureScrollData();
                return _scrollData._scrollOwner;
            }
            set
            {
                EnsureScrollData();
                if (value != _scrollData._scrollOwner)
                {
                    ResetScrolling(this);
                    _scrollData._scrollOwner = value;
                }
            }
        }

        #endregion IScrollInfo Properties

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// General StackPanel layout behavior is to grow unbounded in the "stacking" direction (Size To Content).
        /// Children in this dimension are encouraged to be as large as they like.  In the other dimension,
        /// StackPanel will assume the maximum size of its children.
        /// </summary>
        /// <remarks>
        /// When scrolling, StackPanel will not grow in layout size but effectively add the children on a z-plane which
        /// will probably be clipped by some parent (typically a ScrollContentPresenter) to Stack's size.
        /// </remarks>
        /// <param name="constraint">Constraint</param>
        /// <returns>Desired size</returns>
        protected override Size MeasureOverride(Size constraint)
        {
#if Profiling
            if (Panel.IsAboutToGenerateContent(this))
                return MeasureOverrideProfileStub(constraint);
            else
                return RealMeasureOverride(constraint);
        }

        // this is a handy place to start/stop profiling
        private Size MeasureOverrideProfileStub(Size constraint)
        {
            return RealMeasureOverride(constraint);
        }

        private Size RealMeasureOverride(Size constraint)
        {
#endif
            Size stackDesiredSize = new Size();
            bool etwTracingEnabled = IsScrolling && EventTrace.IsEnabled(EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info);
            if (etwTracingEnabled)
            {
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringBegin, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, "STACK:MeasureOverride");
            }
            try
            {
                // Call the measure helper.
                stackDesiredSize = StackMeasureHelper(this, _scrollData, constraint);
            }
            finally
            {
                if (etwTracingEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringEnd, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, "STACK:MeasureOverride");
                }
            }

            return stackDesiredSize;
        }

        /// <summary>
        ///     Helper method which implements the stack like measure.
        /// </summary>
        internal static Size StackMeasureHelper(IStackMeasure measureElement, IStackMeasureScrollData scrollData, Size constraint)
        {
            Size stackDesiredSize = new Size();
            UIElementCollection children = measureElement.InternalChildren;
            Size layoutSlotSize = constraint;
            bool fHorizontal = (measureElement.Orientation == Orientation.Horizontal);
            int firstViewport;          // First child index in the viewport.
            int lastViewport = -1;      // Last child index in the viewport.  -1 indicates we have not yet iterated through the last child.

            double logicalVisibleSpace, childLogicalSize;


            //
            // Initialize child sizing and iterator data
            // Allow children as much size as they want along the stack.
            //
            if (fHorizontal)
            {
                layoutSlotSize.Width = Double.PositiveInfinity;
                if (measureElement.IsScrolling && measureElement.CanVerticallyScroll) { layoutSlotSize.Height = Double.PositiveInfinity; }
                firstViewport = (measureElement.IsScrolling) ? CoerceOffsetToInteger(scrollData.Offset.X, children.Count) : 0;
                logicalVisibleSpace = constraint.Width;
            }
            else
            {
                layoutSlotSize.Height = Double.PositiveInfinity;
                if (measureElement.IsScrolling && measureElement.CanHorizontallyScroll) { layoutSlotSize.Width = Double.PositiveInfinity; }
                firstViewport = (measureElement.IsScrolling) ? CoerceOffsetToInteger(scrollData.Offset.Y, children.Count) : 0;
                logicalVisibleSpace = constraint.Height;
            }

            //
            //  Iterate through children.
            //  While we still supported virtualization, this was hidden in a child iterator (see source history).
            //
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                // Get next child.
                UIElement child = children[i];

                if (child == null) { continue; }

                // Measure the child.
                child.Measure(layoutSlotSize);
                Size childDesiredSize = child.DesiredSize;

                // Accumulate child size.
                if (fHorizontal)
                {
                    stackDesiredSize.Width += childDesiredSize.Width;
                    stackDesiredSize.Height = Math.Max(stackDesiredSize.Height, childDesiredSize.Height);
                    childLogicalSize = childDesiredSize.Width;
                }
                else
                {
                    stackDesiredSize.Width = Math.Max(stackDesiredSize.Width, childDesiredSize.Width);
                    stackDesiredSize.Height += childDesiredSize.Height;
                    childLogicalSize = childDesiredSize.Height;
                }

                // Adjust remaining viewport space if we are scrolling and within the viewport region.
                // While scrolling (not virtualizing), we always measure children before and after the viewport.
                if (measureElement.IsScrolling && lastViewport == -1 && i >= firstViewport)
                {
                    logicalVisibleSpace -= childLogicalSize;
                    if (DoubleUtil.LessThanOrClose(logicalVisibleSpace, 0.0))
                    {
                        lastViewport = i;
                    }
                }
            }

            //
            // Compute Scrolling stuff.
            //
            if (measureElement.IsScrolling)
            {
                // Compute viewport and extent.
                Size viewport = constraint;
                Size extent = stackDesiredSize;
                Vector offset = scrollData.Offset;

                // If we have not yet set the last child in the viewport, set it to the last child.
                if (lastViewport == -1) { lastViewport = children.Count - 1; }

                // If we or children have resized, it's possible that we can now display more content.
                // This is true if we started at a nonzero offeset and still have space remaining.
                // In this case, we loop back through previous children until we run out of space.
                while (firstViewport > 0)
                {
                    double projectedLogicalVisibleSpace = logicalVisibleSpace;
                    if (fHorizontal) { projectedLogicalVisibleSpace -= children[firstViewport - 1].DesiredSize.Width; }
                    else { projectedLogicalVisibleSpace -= children[firstViewport - 1].DesiredSize.Height; }

                    // If we have run out of room, break.
                    if (DoubleUtil.LessThan(projectedLogicalVisibleSpace, 0.0)) { break; }

                    // Adjust viewport
                    firstViewport--;
                    logicalVisibleSpace = projectedLogicalVisibleSpace;
                }

                int logicalExtent = children.Count;
                int logicalViewport = lastViewport - firstViewport;

                // We are conservative when estimating a viewport, not including the last element in case it is only partially visible.
                // We want to count it if it is fully visible (>= 0 space remaining) or the only element in the viewport.
                if (logicalViewport == 0 || DoubleUtil.GreaterThanOrClose(logicalVisibleSpace, 0.0)) { logicalViewport++; }

                if (fHorizontal)
                {
                    scrollData.SetPhysicalViewport(viewport.Width);
                    viewport.Width = logicalViewport;
                    extent.Width = logicalExtent;
                    offset.X = firstViewport;
                    offset.Y = Math.Max(0, Math.Min(offset.Y, extent.Height - viewport.Height));
                }
                else
                {
                    scrollData.SetPhysicalViewport(viewport.Height);
                    viewport.Height = logicalViewport;
                    extent.Height = logicalExtent;
                    offset.Y = firstViewport;
                    offset.X = Math.Max(0, Math.Min(offset.X, extent.Width - viewport.Width));
                }

                // Since we can offset and clip our content, we never need to be larger than the parent suggestion.
                // If we returned the full size of the content, we would always be so big we didn't need to scroll.  :)
                stackDesiredSize.Width = Math.Min(stackDesiredSize.Width, constraint.Width);
                stackDesiredSize.Height = Math.Min(stackDesiredSize.Height, constraint.Height);

                // Verify Scroll Info, invalidate ScrollOwner if necessary.
                VerifyScrollingData(measureElement, scrollData, viewport, extent, offset);
            }

            return stackDesiredSize;
        }

        /// <summary>
        /// Content arrangement.
        /// </summary>
        /// <param name="arrangeSize">Arrange size</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            bool etwTracingEnabled = IsScrolling && EventTrace.IsEnabled(EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info);
            if (etwTracingEnabled)
            {
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringBegin, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, "STACK:ArrangeOverride");
            }
            try
            {
                // Call the arrange helper.
                StackArrangeHelper(this, _scrollData, arrangeSize);
            }
            finally
            {
                if (etwTracingEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringEnd, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, "STACK:ArrangeOverride");
                }
            }

            return arrangeSize;
        }

        /// <summary>
        ///     Helper method which implements the stack like arrange.
        /// </summary>
        internal static Size StackArrangeHelper(IStackMeasure arrangeElement, IStackMeasureScrollData scrollData, Size arrangeSize)
        {
            UIElementCollection children = arrangeElement.InternalChildren;
            bool fHorizontal = (arrangeElement.Orientation == Orientation.Horizontal);
            Rect rcChild = new Rect(arrangeSize);
            double previousChildSize = 0.0;


            //
            // Compute scroll offset and seed it into rcChild.
            //
            if (arrangeElement.IsScrolling)
            {
                if (fHorizontal)
                {
                    rcChild.X = ComputePhysicalFromLogicalOffset(arrangeElement, scrollData.ComputedOffset.X, true);
                    rcChild.Y = -1.0 * scrollData.ComputedOffset.Y;
                }
                else
                {
                    rcChild.X = -1.0 * scrollData.ComputedOffset.X;
                    rcChild.Y = ComputePhysicalFromLogicalOffset(arrangeElement, scrollData.ComputedOffset.Y, false);
                }
            }

            //
            // Arrange and Position Children.
            //
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                UIElement child = (UIElement)children[i];

                if (child == null) { continue; }

                if (fHorizontal)
                {
                    rcChild.X += previousChildSize;
                    previousChildSize = child.DesiredSize.Width;
                    rcChild.Width = previousChildSize;
                    rcChild.Height = Math.Max(arrangeSize.Height, child.DesiredSize.Height);
                }
                else
                {
                    rcChild.Y += previousChildSize;
                    previousChildSize = child.DesiredSize.Height;
                    rcChild.Height = previousChildSize;
                    rcChild.Width = Math.Max(arrangeSize.Width, child.DesiredSize.Width);
                }

                child.Arrange(rcChild);
            }
            return arrangeSize;
        }


        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void EnsureScrollData()
        {
            if (_scrollData == null) { _scrollData = new ScrollData(); }
        }

        private static void ResetScrolling(StackPanel element)
        {
            element.InvalidateMeasure();

            // Clear scrolling data.  Because of thrash (being disconnected & reconnected, &c...), we may
            if (element.IsScrolling)
            {
                element._scrollData.ClearLayout();
            }
        }

        // OnScrollChange is an override called whenever the IScrollInfo exposed scrolling state changes on this element.
        // At the time this method is called, scrolling state is in its new, valid state.
        private void OnScrollChange()
        {
            if (ScrollOwner != null) { ScrollOwner.InvalidateScrollInfo(); }
        }

        private static void VerifyScrollingData(IStackMeasure measureElement, IStackMeasureScrollData scrollData, Size viewport, Size extent, Vector offset)
        {
            bool fValid = true;

            Debug.Assert(measureElement.IsScrolling);

            fValid &= DoubleUtil.AreClose(viewport, scrollData.Viewport);
            fValid &= DoubleUtil.AreClose(extent, scrollData.Extent);
            fValid &= DoubleUtil.AreClose(offset, scrollData.ComputedOffset);
            scrollData.Offset = offset;

            if (!fValid)
            {
                scrollData.Viewport = viewport;
                scrollData.Extent = extent;
                scrollData.ComputedOffset = offset;
                measureElement.OnScrollChange();
            }
        }

        // Translates a logical (child index) offset to a physical (1/96") when scrolling.
        // If virtualizing, it makes the assumption that the logicalOffset is always the first in the visual collection
        //   and thus returns 0.
        // If not virtualizing, it assumes that children are Measure clean; should only be called after running Measure.
        private static double ComputePhysicalFromLogicalOffset(IStackMeasure arrangeElement, double logicalOffset, bool fHorizontal)
        {
            double physicalOffset = 0.0;

            UIElementCollection children = arrangeElement.InternalChildren;
            Debug.Assert(logicalOffset == 0 || (logicalOffset > 0 && logicalOffset < children.Count));

            for (int i = 0; i < logicalOffset; i++)
            {
                physicalOffset -= (fHorizontal)
                    ? ((UIElement)children[i]).DesiredSize.Width
                    : ((UIElement)children[i]).DesiredSize.Height;
            }

            return physicalOffset;
        }

        private int FindChildIndexThatParentsVisual(Visual child)
        {
            DependencyObject dependencyObjectChild = child;

            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != this)
            {
                dependencyObjectChild = parent;
                parent = VisualTreeHelper.GetParent(dependencyObjectChild);
                if (parent == null)
                {
                    throw new ArgumentException(SR.Get(SRID.Stack_VisualInDifferentSubTree),"child");
                }
            }

            UIElementCollection children = this.Children;
            //The Downcast is ok because StackPanel's
            //child has to be a UIElement to be in this.Children collection
            return (children.IndexOf((UIElement)dependencyObjectChild));
        }

        private void MakeVisiblePhysicalHelper(Rect r, ref Vector newOffset, ref Rect newRect)
        {
            double viewportOffset;
            double viewportSize;
            double targetRectOffset;
            double targetRectSize;
            double minPhysicalOffset;

            bool fHorizontal = (Orientation == Orientation.Horizontal);
            if (fHorizontal)
            {
                viewportOffset = _scrollData._computedOffset.Y;
                viewportSize = ViewportHeight;
                targetRectOffset = r.Y;
                targetRectSize = r.Height;
            }
            else
            {
                viewportOffset = _scrollData._computedOffset.X;
                viewportSize = ViewportWidth;
                targetRectOffset = r.X;
                targetRectSize = r.Width;
            }

            targetRectOffset += viewportOffset;
            minPhysicalOffset = ScrollContentPresenter.ComputeScrollOffsetWithMinimalScroll(
                viewportOffset, viewportOffset + viewportSize, targetRectOffset, targetRectOffset + targetRectSize);

            // Compute the visible rectangle of the child relative to the viewport.
            double left = Math.Max(targetRectOffset, minPhysicalOffset);
            targetRectSize = Math.Max(Math.Min(targetRectSize + targetRectOffset, minPhysicalOffset + viewportSize) - left, 0);
            targetRectOffset = left;
            targetRectOffset -= viewportOffset;

            if (fHorizontal)
            {
                newOffset.Y = minPhysicalOffset;
                newRect.Y = targetRectOffset;
                newRect.Height = targetRectSize;
            }
            else
            {
                newOffset.X = minPhysicalOffset;
                newRect.X = targetRectOffset;
                newRect.Width = targetRectSize;
            }
        }

        private void MakeVisibleLogicalHelper(int childIndex, ref Vector newOffset, ref Rect newRect)
        {
            bool fHorizontal = (Orientation == Orientation.Horizontal);
            int firstChildInView;
            int newFirstChild;
            int viewportSize;
            double childOffsetWithinViewport = 0;

            if (fHorizontal)
            {
                firstChildInView = (int)_scrollData._computedOffset.X;
                viewportSize = (int)_scrollData._viewport.Width;
            }
            else
            {
                firstChildInView = (int)_scrollData._computedOffset.Y;
                viewportSize = (int)_scrollData._viewport.Height;
            }

            newFirstChild = firstChildInView;

            // If the target child is before the current viewport, move the viewport to put the child at the top.
            if (childIndex < firstChildInView)
            {
                newFirstChild = childIndex;
            }
            // If the target child is after the current viewport, move the viewport to put the child at the bottom.
            else if (childIndex > firstChildInView + viewportSize - 1)
            {
                Size childDesiredSize = InternalChildren[childIndex].DesiredSize;
                double nextChildSize = ((fHorizontal) ? childDesiredSize.Width : childDesiredSize.Height);
                double viewportSpace = _scrollData._physicalViewport - nextChildSize;
                int i = childIndex;
                while (i > 0 && DoubleUtil.GreaterThanOrClose(viewportSpace, 0.0))
                {
                    i--;
                    childDesiredSize = InternalChildren[i].DesiredSize;
                    nextChildSize = ((fHorizontal) ? childDesiredSize.Width : childDesiredSize.Height);
                    childOffsetWithinViewport += nextChildSize;
                    viewportSpace -= nextChildSize;
                }

                if (i != childIndex && DoubleUtil.LessThan(viewportSpace, 0.0))
                {
                    childOffsetWithinViewport -= nextChildSize;
                    i++;
                }

                newFirstChild = i;
            }

            if (fHorizontal)
            {
                newOffset.X = newFirstChild;
                newRect.X = childOffsetWithinViewport;
                newRect.Width = InternalChildren[childIndex].DesiredSize.Width;
            }
            else
            {
                newOffset.Y = newFirstChild;
                newRect.Y = childOffsetWithinViewport;
                newRect.Height = InternalChildren[childIndex].DesiredSize.Height;
            }
        }

        static private int CoerceOffsetToInteger(double offset, int numberOfItems)
        {
            int iNewOffset;

            if (Double.IsNegativeInfinity(offset))
            {
                iNewOffset = 0;
            }
            else if (Double.IsPositiveInfinity(offset))
            {
                iNewOffset = numberOfItems - 1;
            }
            else
            {
                iNewOffset = (int)offset;
                iNewOffset = Math.Max(Math.Min(numberOfItems - 1, iNewOffset), 0);
            }

            return iNewOffset;
        }

        //-----------------------------------------------------------
        // Avalon Property Callbacks/Overrides
        //-----------------------------------------------------------
        #region Avalon Property Callbacks/Overrides

        /// <summary>
        /// <see cref="PropertyMetadata.PropertyChangedCallback"/>
        /// </summary>
        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Since Orientation is so essential to logical scrolling/virtualization, we synchronously check if
            // the new value is different and clear all scrolling data if so.
            ResetScrolling(d as StackPanel);
        }

        #endregion

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        private bool IsScrolling
        {
            get { return (_scrollData != null) && (_scrollData._scrollOwner != null); }
        }

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 9; }
        }

        bool IStackMeasure.IsScrolling
        {
            get { return IsScrolling; }
        }

        UIElementCollection IStackMeasure.InternalChildren
        {
            get { return InternalChildren; }
        }

        void IStackMeasure.OnScrollChange()
        {
            OnScrollChange();
        }

        private bool CanMouseWheelVerticallyScroll
        {
            get { return (SystemParameters.WheelScrollLines > 0); }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Logical scrolling and virtualization data.
        private ScrollData _scrollData;

        #endregion Private Fields


        //------------------------------------------------------
        //
        //  Private Structures / Classes
        //
        //------------------------------------------------------

        #region Private Structures Classes

        //-----------------------------------------------------------
        // ScrollData class
        //-----------------------------------------------------------
        #region ScrollData

        // Helper class to hold scrolling data.
        // This class exists to reduce working set when StackPanel is used outside a scrolling situation.
        // Standard "extra pointer always for less data sometimes" cache savings model:
        //      !Scroll [1xReference]
        //      Scroll  [1xReference] + [6xDouble + 1xReference]
        private class ScrollData: IStackMeasureScrollData
        {
            // Clears layout generated data.
            // Does not clear scrollOwner, because unless resetting due to a scrollOwner change, we won't get reattached.
            internal void ClearLayout()
            {
                _offset = new Vector();
                _viewport = _extent = new Size();
                _physicalViewport = 0;
            }

            // For Stack/Flow, the two dimensions of properties are in different units:
            // 1. The "logically scrolling" dimension uses items as units.
            // 2. The other dimension physically scrolls.  Units are in Avalon pixels (1/96").
            internal bool _allowHorizontal;
            internal bool _allowVertical;
            internal Vector _offset;            // Scroll offset of content.  Positive corresponds to a visually upward offset.
            internal Vector _computedOffset = new Vector(0,0);
            internal Size _viewport;            // ViewportSize is in {pixels x items} (or vice-versa).
            internal Size _extent;              // Extent is the total number of children (logical dimension) or physical size
            internal double _physicalViewport;  // The physical size of the viewport for the items dimension above.
            internal ScrollViewer _scrollOwner; // ScrollViewer to which we're attached.

            public Vector Offset
            {
                get
                {
                    return _offset;
                }
                set
                {
                    _offset = value;
                }
            }

            public Size Viewport
            {
                get
                {
                    return _viewport;
                }
                set
                {
                    _viewport = value;
                }
            }

            public Size Extent
            {
                get
                {
                    return _extent;
                }
                set
                {
                    _extent = value;
                }
            }

            public Vector ComputedOffset
            {
                get
                {
                    return _computedOffset;
                }
                set
                {
                    _computedOffset = value;
                }
            }

            public void SetPhysicalViewport(double value)
            {
                _physicalViewport = value;
            }
        }

        #endregion ScrollData

        #endregion Private Structures Classes
    }
}

