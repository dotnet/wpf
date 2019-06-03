// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Contains the ScrollContentPresenter class.
//

using MS.Internal;
using MS.Utility;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Markup;

namespace System.Windows.Controls
{
    /// <summary>
    /// </summary>
    sealed public class ScrollContentPresenter : ContentPresenter, IScrollInfo
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Default DependencyObject constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public ScrollContentPresenter() : base()
        {
            _adornerLayer = new AdornerLayer();
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Scroll content by one line to the top.
        /// </summary>
        public void LineUp()
        {
            if (IsScrollClient) { SetVerticalOffset(VerticalOffset - ScrollViewer._scrollLineDelta); }
        }
        /// <summary>
        /// Scroll content by one line to the bottom.
        /// </summary>
        public void LineDown()
        {
            if (IsScrollClient) { SetVerticalOffset(VerticalOffset + ScrollViewer._scrollLineDelta); }
        }
        /// <summary>
        /// Scroll content by one line to the left.
        /// </summary>
        public void LineLeft()
        {
            if (IsScrollClient) { SetHorizontalOffset(HorizontalOffset - ScrollViewer._scrollLineDelta); }
        }
        /// <summary>
        /// Scroll content by one line to the right.
        /// </summary>
        public void LineRight()
        {
            if (IsScrollClient) { SetHorizontalOffset(HorizontalOffset + ScrollViewer._scrollLineDelta); }
        }

        /// <summary>
        /// Scroll content by one page to the top.
        /// </summary>
        public void PageUp()
        {
            if (IsScrollClient) { SetVerticalOffset(VerticalOffset - ViewportHeight); }
        }
        /// <summary>
        /// Scroll content by one page to the bottom.
        /// </summary>
        public void PageDown()
        {
            if (IsScrollClient) { SetVerticalOffset(VerticalOffset + ViewportHeight); }
        }
        /// <summary>
        /// Scroll content by one page to the left.
        /// </summary>
        public void PageLeft()
        {
            if (IsScrollClient) { SetHorizontalOffset(HorizontalOffset - ViewportWidth); }
        }
        /// <summary>
        /// Scroll content by one page to the right.
        /// </summary>
        public void PageRight()
        {
            if (IsScrollClient) { SetHorizontalOffset(HorizontalOffset + ViewportWidth); }
        }

        /// <summary>
        /// Scroll content by one line to the top.
        /// </summary>
        public void MouseWheelUp()
        {
            if (IsScrollClient) { SetVerticalOffset(VerticalOffset - ScrollViewer._mouseWheelDelta); }
        }
        /// <summary>
        /// Scroll content by one line to the bottom.
        /// </summary>
        public void MouseWheelDown()
        {
            if (IsScrollClient) { SetVerticalOffset(VerticalOffset + ScrollViewer._mouseWheelDelta); }
        }
        /// <summary>
        /// Scroll content by one page to the top.
        /// </summary>
        public void MouseWheelLeft()
        {
            if (IsScrollClient) { SetHorizontalOffset(HorizontalOffset - ScrollViewer._mouseWheelDelta); }
        }
        /// <summary>
        /// Scroll content by one page to the bottom.
        /// </summary>
        public void MouseWheelRight()
        {
            if (IsScrollClient) { SetHorizontalOffset(HorizontalOffset + ScrollViewer._mouseWheelDelta); }
        }

        /// <summary>
        /// Set the HorizontalOffset to the passed value.
        /// </summary>
        public void SetHorizontalOffset(double offset)
        {
            if (IsScrollClient)
            {
                double newValue = ValidateInputOffset(offset, "HorizontalOffset");
                if (!DoubleUtil.AreClose(EnsureScrollData()._offset.X, newValue))
                {
                    _scrollData._offset.X = newValue;
                    InvalidateArrange();
                }
            }
        }

        /// <summary>
        /// Set the VerticalOffset to the passed value.
        /// </summary>
        public void SetVerticalOffset(double offset)
        {
            if (IsScrollClient)
            {
                double newValue = ValidateInputOffset(offset, "VerticalOffset");
                if (!DoubleUtil.AreClose(EnsureScrollData()._offset.Y, newValue))
                {
                    _scrollData._offset.Y = newValue;
                    InvalidateArrange();
                }
            }
        }

        /// <summary>
        /// ScrollContentPresenter implementation of <seealso cref="IScrollInfo.MakeVisible" />.
        /// </summary>
        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return MakeVisible(visual, rectangle, true);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties (CLR + Avalon)
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// AdornerLayer on which adorners are rendered.
        /// Adorners are rendered under the ScrollContentPresenter's clip region.
        /// </summary>
        public AdornerLayer AdornerLayer
        {
            get { return _adornerLayer; }
        }

        /// <summary>
        /// This property indicates whether the ScrollContentPresenter should try to allow the Content
        /// to scroll or not.  A true value indicates Content should be allowed to scroll if it supports
        /// IScrollInfo.  A false value will cause ScrollContentPresenter to always act as the scrolling
        /// client.
        /// </summary>
        public bool CanContentScroll
        {
            get { return (bool) GetValue(CanContentScrollProperty); }
            set { SetValue(CanContentScrollProperty, value); }
        }

        /// <summary>
        /// ScrollContentPresenter reacts to this property by changing it's child measurement algorithm.
        /// If scrolling in a dimension, infinite space is allowed the child; otherwise, available size is preserved.
        /// </summary>
        public bool CanHorizontallyScroll
        {
            get { return (IsScrollClient) ? EnsureScrollData()._canHorizontallyScroll : false;  }
            set
            {
                if (IsScrollClient && (EnsureScrollData()._canHorizontallyScroll != value))
                {
                    _scrollData._canHorizontallyScroll = value;
                    InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// ScrollContentPresenter reacts to this property by changing it's child measurement algorithm.
        /// If scrolling in a dimension, infinite space is allowed the child; otherwise, available size is preserved.
        /// </summary>
        public bool CanVerticallyScroll
        {
            get { return (IsScrollClient) ? EnsureScrollData()._canVerticallyScroll : false; }
            set
            {
                if (IsScrollClient && (EnsureScrollData()._canVerticallyScroll != value))
                {
                    _scrollData._canVerticallyScroll = value;
                    InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// ExtentWidth contains the horizontal size of the scrolled content element in 1/96"
        /// </summary>
        public double ExtentWidth
        {
            get  { return (IsScrollClient) ? EnsureScrollData()._extent.Width : 0.0; }
        }
        /// <summary>
        /// ExtentHeight contains the vertical size of the scrolled content element in 1/96"
        /// </summary>
        public double ExtentHeight
        {
            get  { return (IsScrollClient) ? EnsureScrollData()._extent.Height : 0.0; }
        }
        /// <summary>
        /// ViewportWidth contains the horizontal size of content's visible range in 1/96"
        /// </summary>
        public double ViewportWidth
        {
            get { return (IsScrollClient) ? EnsureScrollData()._viewport.Width : 0.0; }
        }
        /// <summary>
        /// ViewportHeight contains the vertical size of content's visible range in 1/96"
        /// </summary>
        public double ViewportHeight
        {
            get { return (IsScrollClient) ? EnsureScrollData()._viewport.Height : 0.0; }
        }

        /// <summary>
        /// HorizontalOffset is the horizontal offset of the scrolled content in 1/96".
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double HorizontalOffset
        {
            get { return (IsScrollClient) ? EnsureScrollData()._computedOffset.X : 0.0; }
        }
        /// <summary>
        /// VerticalOffset is the vertical offset of the scrolled content in 1/96".
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double VerticalOffset
        {
            get { return (IsScrollClient) ? EnsureScrollData()._computedOffset.Y : 0.0; }
        }

        /// <summary>
        /// ScrollOwner is the container that controls any scrollbars, headers, etc... that are dependant
        /// on this ScrollArea's properties.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ScrollViewer ScrollOwner
        {
            get { return (IsScrollClient) ? _scrollData._scrollOwner: null; }
            set { if (IsScrollClient) { _scrollData._scrollOwner = value; } }
        }

        /// <summary>
        /// DependencyProperty for <see cref="CanContentScroll" /> property.
        /// </summary>
        public static readonly DependencyProperty CanContentScrollProperty =
                ScrollViewer.CanContentScrollProperty.AddOwner(
                        typeof(ScrollContentPresenter),
                        new FrameworkPropertyMetadata(new PropertyChangedCallback(OnCanContentScrollChanged)));

        #endregion

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods
        /// <summary>
        /// Returns the Visual children count.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                // Four states make sense:
                // 0 Children.  No Content or AdornerLayer.  Valid - do nothing.
                // 2 Children.  Content is first child, AdornerLayer

                // One for the base.TemplateChild and one for the _adornerlayer.
                return (base.TemplateChild == null) ? 0 : 2;
            }
        }

        /// <summary>
        /// Returns the child at the specified index.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            //check if there is a TemplateChild on FrameworkElement
            if (base.TemplateChild == null)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }
            else
            {
                switch (index)
                {
                    case 0:
                        return base.TemplateChild;

                    case 1:
                        return _adornerLayer;

                    default:
                        throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
                }
            }
         }

        /// <summary>
        /// Gets or sets the template child of the FrameworkElement.
        /// </summary>
        override internal UIElement TemplateChild
        {
            get
            {
                return base.TemplateChild;
            }
            set
            {
                UIElement oldTemplate = base.TemplateChild;
                if (value != oldTemplate)
                {
                    if (oldTemplate != null && value == null)
                    {
                        // If we used to have a template child and we don't have a
                        // new template child disconnect the adorner layer.
                        this.RemoveVisualChild(_adornerLayer);
                    }

                    base.TemplateChild = value;

                    if(oldTemplate == null && value != null)
                    {
                        // If we did not use to have a template child, but we have one
                        // now, attach the adorner layer.
                        this.AddVisualChild(_adornerLayer);
                    }
                }
            }
        }


        /// <summary>
        /// </summary>
        protected override Size MeasureOverride(Size constraint)
        {
            Size desiredSize = new Size();
            bool etwTracingEnabled = IsScrollClient && EventTrace.IsEnabled(EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info);
            if (etwTracingEnabled)
            {
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringBegin, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, "SCROLLCONTENTPRESENTER:MeasureOverride");
            }
            try
            {
                int count = this.VisualChildrenCount;


                if (count > 0)
                {
                    // The AdornerLayer is always the size of our surface, and does not contribute to our own size.
                    _adornerLayer.Measure(constraint);

                    if (!IsScrollClient)
                    {
                        desiredSize = base.MeasureOverride(constraint);
                    }
                    else
                    {
                        Size childConstraint = constraint;

                        if (_scrollData._canHorizontallyScroll) { childConstraint.Width = Double.PositiveInfinity; }
                        if (_scrollData._canVerticallyScroll) { childConstraint.Height = Double.PositiveInfinity; }

                        desiredSize = base.MeasureOverride(childConstraint);
                    }
                }

                // If we're handling scrolling (as the physical scrolling client, validate properties.
                if (IsScrollClient)
                {
                    VerifyScrollData(constraint, desiredSize);
                }

                desiredSize.Width = Math.Min(constraint.Width, desiredSize.Width);
                desiredSize.Height = Math.Min(constraint.Height, desiredSize.Height);
            }
            finally
            {
                if (etwTracingEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringEnd, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, "SCROLLCONTENTPRESENTER:MeasureOverride");
                }
            }
            return desiredSize;
        }

        /// <summary>
        /// </summary>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            bool etwTracingEnabled = IsScrollClient && EventTrace.IsEnabled(EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info);
            if (etwTracingEnabled)
            {
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringBegin, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, "SCROLLCONTENTPRESENTER:ArrangeOverride");
            }
            try
            {
                int count = this.VisualChildrenCount;


                // Verifies IScrollInfo properties & invalidates ScrollViewer if necessary.
                if (IsScrollClient)
                {
                    VerifyScrollData(arrangeSize, _scrollData._extent);
                }

                if (count > 0)
                {
                    _adornerLayer.Arrange(new Rect(arrangeSize));

                    UIElement child = this.GetVisualChild(0) as UIElement;
                    if (child != null)
                    {
                        Rect childRect = new Rect(child.DesiredSize);

                        if (IsScrollClient)
                        {
                            childRect.X = -HorizontalOffset;
                            childRect.Y = -VerticalOffset;
                        }

                        //this is needed to stretch the child to arrange space,
                        childRect.Width = Math.Max(childRect.Width, arrangeSize.Width);
                        childRect.Height = Math.Max(childRect.Height, arrangeSize.Height);

                        child.Arrange(childRect);
                    }
                }
            }
            finally
            {
                if (etwTracingEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringEnd, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, "SCROLLCONTENTPRESENTER:ArrangeOverride");
                }
            }
            return (arrangeSize);
        }

        /// <summary>
        /// Override of <seealso cref="UIElement.GetLayoutClip"/>.
        /// </summary>
        /// <returns>Viewport geometry</returns>
        protected override Geometry GetLayoutClip(Size layoutSlotSize)
        {
            return new RectangleGeometry(new Rect(RenderSize));
        }

        /// <summary>
        /// Called when the Template's tree has been generated
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();


            // Add the AdornerLayer to our visual tree.
            // Iff we have content, we need an adorner layer.
            // It has Content(eg. Button, TextBlock) as its first child and AdornerLayer as its second child

            // Get our scrolling owner and content talking.
            HookupScrollingComponents();
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// ScrollContentPresenter implementation of <seealso cref="IScrollInfo.MakeVisible" />.
        /// </summary>
        /// <param name="visual">The Visual that should become visible</param>
        /// <param name="rectangle">A rectangle representing in the visual's coordinate space to make visible.</param>
        /// <param name="throwOnError">If true the method throws an exception when an error is encountered, otherwise the method returns Rect.Empty when an error is encountered</param>
        /// <returns>
        /// A rectangle in the IScrollInfo's coordinate space that has been made visible.
        /// Other ancestors to in turn make this new rectangle visible.
        /// The rectangle should generally be a transformed version of the input rectangle.  In some cases, like
        /// when the input rectangle cannot entirely fit in the viewport, the return value might be smaller.
        /// </returns>
        internal Rect MakeVisible(Visual visual, Rect rectangle, bool throwOnError)
        {
            // (ScrollContentPresenter.MakeVisible can cause an exception when encountering an empty rectangle)
            // This method exists to keep ScrollContentPresenter.MakeVisible v1 behavior
            // while allowing callers of IScrollInfo.MakeVisible in the platform work around a bug
            // in the v1 behavior.
            // If this bug is fixed look for callers of IScrollInfo.MakeVisible with workarounds.
            // They should be updated remove the workarounds.

            //
            // Note: This code presently assumes we/children are layout clean.  See work item 22269 for more detail.
            //

            // We can only work on visuals that are us or children.
            // An empty rect has no size or position.  We can't meaningfully use it.
            if (rectangle.IsEmpty
                || visual == null
                || visual == (Visual)this
                || !this.IsAncestorOf(visual))
            {
                return Rect.Empty;
            }

            // This is a false positive by PreSharp. visual cannot be null because of the 'if' check above
#pragma warning disable 1634, 1691
#pragma warning disable 56506
            // Compute the child's rect relative to (0,0) in our coordinate space.
            GeneralTransform childTransform = visual.TransformToAncestor(this);
#pragma warning restore 56506
#pragma warning restore 1634, 1691

            rectangle = childTransform.TransformBounds(rectangle);

            if (!IsScrollClient || (!throwOnError && rectangle.IsEmpty))
            {
                return rectangle;
            }

            // Initialize the viewport
            Rect viewport = new Rect(HorizontalOffset, VerticalOffset, ViewportWidth, ViewportHeight);
            rectangle.X += viewport.X;
            rectangle.Y += viewport.Y;

            // Compute the offsets required to minimally scroll the child maximally into view.
            double minX = ComputeScrollOffsetWithMinimalScroll(viewport.Left, viewport.Right, rectangle.Left, rectangle.Right);
            double minY = ComputeScrollOffsetWithMinimalScroll(viewport.Top, viewport.Bottom, rectangle.Top, rectangle.Bottom);

            // We have computed the scrolling offsets; scroll to them.
            SetHorizontalOffset(minX);
            SetVerticalOffset(minY);

            // Compute the visible rectangle of the child relative to the viewport.
            viewport.X = minX;
            viewport.Y = minY;
            rectangle.Intersect(viewport);

            if (throwOnError)
            {
                // (ScrollContentPresenter.MakeVisible can cause an exception when encountering an empty rectangle)
                // Old behavior for app compat
                rectangle.X -= viewport.X;
                rectangle.Y -= viewport.Y;
            }
            else
            {
                // (ScrollContentPresenter.MakeVisible can cause an exception when encountering an empty rectangle)
                // New correct behavior
                if (!rectangle.IsEmpty)
                {
                    rectangle.X -= viewport.X;
                    rectangle.Y -= viewport.Y;
                }
            }

            // Return the rectangle
            return rectangle;
        }

        internal static double ComputeScrollOffsetWithMinimalScroll(
            double topView,
            double bottomView,
            double topChild,
            double bottomChild)
        {
            bool alignTop = false;
            bool alignBottom = false;
            return ComputeScrollOffsetWithMinimalScroll(topView, bottomView, topChild, bottomChild, ref alignTop, ref alignBottom);
        }

        internal static double ComputeScrollOffsetWithMinimalScroll(
            double topView,
            double bottomView,
            double topChild,
            double bottomChild,
            ref bool alignTop,
            ref bool alignBottom)
        {
            // # CHILD POSITION       CHILD SIZE      SCROLL      REMEDY
            // 1 Above viewport       <= viewport     Down        Align top edge of child & viewport
            // 2 Above viewport       > viewport      Down        Align bottom edge of child & viewport
            // 3 Below viewport       <= viewport     Up          Align bottom edge of child & viewport
            // 4 Below viewport       > viewport      Up          Align top edge of child & viewport
            // 5 Entirely within viewport             NA          No scroll.
            // 6 Spanning viewport                    NA          No scroll.
            //
            // Note: "Above viewport" = childTop above viewportTop, childBottom above viewportBottom
            //       "Below viewport" = childTop below viewportTop, childBottom below viewportBottom
            // These child thus may overlap with the viewport, but will scroll the same direction/

            bool fAbove = DoubleUtil.LessThan(topChild, topView) && DoubleUtil.LessThan(bottomChild, bottomView);
            bool fBelow = DoubleUtil.GreaterThan(bottomChild, bottomView) && DoubleUtil.GreaterThan(topChild, topView);
            bool fLarger = (bottomChild - topChild) > (bottomView - topView);

            // Handle Cases:  1 & 4 above
            if ((fAbove && !fLarger)
               || (fBelow && fLarger)
               || alignTop)
            {
                alignTop = true;
                return topChild;
            }

            // Handle Cases: 2 & 3 above
            else if (fAbove || fBelow || alignBottom)
            {
                alignBottom = true;
                return (bottomChild - (bottomView - topView));
            }

            // Handle cases: 5 & 6 above.
            return topView;
        }

        static internal double ValidateInputOffset(double offset, string parameterName)
        {
            if (DoubleUtil.IsNaN(offset))
            {
                throw new ArgumentOutOfRangeException(parameterName, SR.Get(SRID.ScrollViewer_CannotBeNaN, parameterName));
            }
            return Math.Max(0.0, offset);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private ScrollData EnsureScrollData()
        {
            if (_scrollData == null) { _scrollData = new ScrollData(); }
            return _scrollData;
        }


        // Helper method to get our ScrollViewer owner and its scrolling content talking.
        // Method introduces the current owner/content, and clears a from any previous content.
        internal void HookupScrollingComponents()
        {
            // We need to introduce our IScrollInfo to our ScrollViewer (and break any previous links).
            ScrollViewer scrollContainer = TemplatedParent as ScrollViewer;

            // If our content is not an IScrollInfo, we should have selected a style that contains one.
            // This (readonly) style contains an AdornerDecorator with a ScrollArea child.
            if (scrollContainer != null)
            {
                IScrollInfo si = null;

                if (CanContentScroll)
                {
                    // We need to get an IScrollInfo to introduce to the ScrollViewer.
                    // 1. Try our content...
                    si = Content as IScrollInfo;

                    if (si == null)
                    {
                        Visual child = Content as Visual;
                        if (child != null)
                        {
                            // 2. Our child might be an ItemsPresenter.  In this case check its child for being an IScrollInfo
                            ItemsPresenter itemsPresenter = child as ItemsPresenter;
                            if (itemsPresenter == null)
                            {
                                // 3. With the change in templates for ClearTypeHint the ItemsPresenter is not guranteed to be the 
                                // immediate child. We now look for a named element instead of naively walking the descendents.
                                FrameworkElement templatedParent = scrollContainer.TemplatedParent as FrameworkElement;
                                if (templatedParent != null)
                                {
                                    itemsPresenter = templatedParent.GetTemplateChild("ItemsPresenter") as ItemsPresenter;
                                }
                            }

                            if (itemsPresenter != null)
                            {
                                itemsPresenter.ApplyTemplate();

                                int count = VisualTreeHelper.GetChildrenCount(itemsPresenter);
                                if(count > 0)
                                    si = VisualTreeHelper.GetChild(itemsPresenter, 0) as IScrollInfo;
                            }
                        }
                    }
                }

                // 4. As a final fallback, we use ourself.
                if (si == null)
                {
                    si = (IScrollInfo)this;
                    EnsureScrollData();
                }

                // Detach any differing previous IScrollInfo from ScrollViewer
                if (si != _scrollInfo && _scrollInfo != null)
                {
                    if (IsScrollClient) { _scrollData = null; }
                    else _scrollInfo.ScrollOwner = null;
                }

                // Introduce our ScrollViewer and IScrollInfo to each other.
                if (si != null)
                {
                    _scrollInfo = si;                   // At this point, we pass IsScrollClient if si == this.
                    si.ScrollOwner = scrollContainer;
                    scrollContainer.ScrollInfo = si;
                }
            }

            // We're not really in a valid scrolling scenario.  Break any previous references, and get us
            // back into a totally unlinked state.
            else if (_scrollInfo != null)
            {
                if (_scrollInfo.ScrollOwner != null) { _scrollInfo.ScrollOwner.ScrollInfo = null; }
                _scrollInfo.ScrollOwner = null;
                _scrollInfo = null;
                _scrollData = null;
            }
        }

        // Verifies scrolling data using the passed viewport and extent as newly computed values.
        // Checks the X/Y offset and coerces them into the range [0, Extent - ViewportSize]
        // If extent, viewport, or the newly coerced offsets are different than the existing offset,
        //   cachces are updated and InvalidateScrollInfo() is called.
        private void VerifyScrollData(Size viewport, Size extent)
        {
            Debug.Assert(IsScrollClient);

            bool fValid = true;

            // These two lines of code are questionable, but they are needed right now as VSB may return
            //  Infinity size from measure, which is a regression from the old scrolling model.
            // They also have the incidental affect of probably avoiding reinvalidation at Arrange
            //   when inside a parent that measures you to Infinity.
            if (Double.IsInfinity(viewport.Width)) viewport.Width = extent.Width;
            if (Double.IsInfinity(viewport.Height)) viewport.Height = extent.Height;

            fValid &= DoubleUtil.AreClose(viewport, _scrollData._viewport);
            fValid &= DoubleUtil.AreClose(extent, _scrollData._extent);
            _scrollData._viewport = viewport;
            _scrollData._extent = extent;

            fValid &= CoerceOffsets();

            if (!fValid)
            {
                ScrollOwner.InvalidateScrollInfo();
            }
        }

        // Returns an offset coerced into the [0, Extent - Viewport] range.
        // Internal because it is also used by other Avalon ISI implementations (just to avoid code duplication).
        static internal double CoerceOffset(double offset, double extent, double viewport)
        {
            if (offset > extent - viewport) { offset = extent - viewport; }
            if (offset < 0) { offset = 0; }
            return offset;
        }

        private bool CoerceOffsets()
        {
            Debug.Assert(IsScrollClient);
            Vector computedOffset = new Vector(
                CoerceOffset(_scrollData._offset.X, _scrollData._extent.Width, _scrollData._viewport.Width),
                CoerceOffset(_scrollData._offset.Y, _scrollData._extent.Height, _scrollData._viewport.Height));

            bool fValid = DoubleUtil.AreClose(_scrollData._computedOffset, computedOffset);
            _scrollData._computedOffset = computedOffset;

            return fValid;
        }

        // This property is structurally important; we can't do layout without it set right.
        // So, we synchronously make changes.
        static private void OnCanContentScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScrollContentPresenter scp = (ScrollContentPresenter)d;
            if (scp._scrollInfo == null)
            {
                return;
            }

// the code that was here appeared to care about the first time this property was ever set -- verify if this replacement is okay
            scp.HookupScrollingComponents();
            scp.InvalidateMeasure();
        }
        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Properties
        //
        //-------------------------------------------------------------------

        #region Private Properties

        private bool IsScrollClient
        {
            get { return (_scrollInfo == this); }
        }

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 42; }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        // Only one of the following will be used.
        // The _scrollInfo holds a content IScrollInfo implementation that is given to the ScrollViewer.
        // _scrollData holds values for the scrolling properties we use if we are handling IScrollInfo for the ScrollViewer ourself.
        // ScrollData could implement IScrollInfo, but then the v-table would hurt in the common case as much as we save
        //   in the less common case.
        private IScrollInfo _scrollInfo;
        private ScrollData _scrollData;
        // To hold adorners (caret, &c...) under the clipping region of the scroller.
        private readonly AdornerLayer _adornerLayer;

        #endregion

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
        // This class exists to reduce working set when SCP is delegating to another implementation of ISI.
        // Standard "extra pointer always for less data sometimes" cache savings model:
        // Consider using the Stack internal helper.  It's more or less the same.
        private class ScrollData
        {
            internal ScrollViewer _scrollOwner;

            internal bool _canHorizontallyScroll;
            internal bool _canVerticallyScroll;

            internal Vector _offset;            // Set scroll offset of content.  Positive corresponds to a visually upward offset.
            internal Vector _computedOffset;    // Actual (computed) scroll offset of content. ""  ""

            internal Size _viewport;    // ViewportSize is computed from our FinalSize, but may be in different units.
            internal Size _extent;      // Extent is the total size of our content.
        }

        #endregion ScrollData

        #endregion Private Structures Classes
    }
}




