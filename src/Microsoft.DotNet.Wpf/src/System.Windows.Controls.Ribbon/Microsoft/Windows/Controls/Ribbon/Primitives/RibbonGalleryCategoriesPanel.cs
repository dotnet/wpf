// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon.Primitives
#else
namespace Microsoft.Windows.Controls.Ribbon.Primitives
#endif
{

    #region Using declarations

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Diagnostics;
    using System.Windows.Media;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif
    using MS.Internal;

    #endregion

    public class RibbonGalleryCategoriesPanel : Panel, IProvideStarLayoutInfoBase, IContainsStarLayoutManager, IScrollInfo
    {
        #region Constructors

        public RibbonGalleryCategoriesPanel()
        {
            Unloaded += new RoutedEventHandler(OnRibbonGalleryCategoriesPanelUnloaded);
            Loaded += new RoutedEventHandler(OnRibbonGalleryCategoriesPanelLoaded);
        }

        #endregion

        #region Private Methods and Properties

        private void OnRibbonGalleryCategoriesPanelUnloaded(object sender, RoutedEventArgs e)
        {
            IContainsStarLayoutManager iContainsStarLayoutManager = (IContainsStarLayoutManager)this;
            if (iContainsStarLayoutManager.StarLayoutManager != null)
            {
                iContainsStarLayoutManager.StarLayoutManager.UnregisterStarLayoutProvider(this);
                iContainsStarLayoutManager.StarLayoutManager = null;
            }
        }

        private void OnRibbonGalleryCategoriesPanelLoaded(object sender, RoutedEventArgs e)
        {
            RibbonGallery gallery = this.Gallery;
            if (gallery != null)
            {
#if IN_RIBBON_GALLERY
                if (gallery.IsInInRibbonGalleryMode())
                {
                    return;
                }
#endif
                RibbonHelper.InitializeStarLayoutManager(this);
            }
        }

#if IN_RIBBON_GALLERY
        // PreComputing MaxItemHeight and MaxItemWidth as the Measure algorithms of parent panels of Items will need it already
        // If it is in InRibbonGalleryMode.
        private void PreComputeMaxRibbonGalleryItemWidthAndHeight()
        {
            UIElementCollection children = InternalChildren;
            Size childConstraint = new Size(double.PositiveInfinity, double.PositiveInfinity);
            double maxItemHeight = 0;
            int childrenCount = children.Count;
            double maxColumnWidth = 0;

            for (int i = 0; i < childrenCount; i++)
            {
                RibbonGalleryCategory child = children[i] as RibbonGalleryCategory;
                RibbonGalleryItemsPanel itemPanel = child.ItemsHostSite as RibbonGalleryItemsPanel;
                if (itemPanel != null)
                {
                    int itemPanelChildrenCount = itemPanel.Children.Count;
                    for (int j = 0; j < itemPanelChildrenCount; j++)
                    {
                        RibbonGalleryItem item = (RibbonGalleryItem)itemPanel.Children[j];
                        item.Measure(childConstraint);
                        Size itemSize = item.DesiredSize;
                        maxColumnWidth = Math.Max(maxColumnWidth, itemSize.Width);
                        maxItemHeight = Math.Max(maxItemHeight, itemSize.Height);
                    }
                }
            }

            RibbonGallery gallery = this.Gallery;
            if (gallery != null)
            {
                gallery.MaxItemHeight = maxItemHeight;
                gallery.MaxColumnWidth = maxColumnWidth;
            }
        }

        internal bool InRibbonModeCanLineUp()
        {
            return !DoubleUtil.AreClose(VerticalOffset, 0.0);
        }

        internal bool InRibbonModeCanLineDown()
        {
            return DoubleUtil.GreaterThan(_scrollData._extent.Height, VerticalOffset + ViewportHeight);
        }
#endif

        #region Scrolling Helper

        private void EnsureScrollData()
        {
            if (_scrollData == null) { _scrollData = new ScrollData(); }
        }

        private static void ResetScrolling(RibbonGalleryCategoriesPanel element)
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

        private void VerifyScrollingData(Size viewport, Size extent, Vector offset)
        {
            bool fValid = true;

            Debug.Assert(IsScrolling);

            fValid &= DoubleUtil.AreClose(viewport, _scrollData._viewport);
            fValid &= DoubleUtil.AreClose(extent, _scrollData._extent);
            fValid &= DoubleUtil.AreClose(offset, _scrollData._offset);
            _scrollData._offset = offset;

            if (!fValid)
            {
                _scrollData._viewport = viewport;
                _scrollData._extent = extent;
            }
            OnScrollChange();
        }

        static private double ComputeScrollOffsetWithMinimalScroll(
            double topView,
            double bottomView,
            double topChild,
            double bottomChild)
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
               || (fBelow && fLarger))
            {
                return topChild;
            }

            // Handle Cases: 2 & 3 above
            else if (fAbove || fBelow)
            {
                return (bottomChild - (bottomView - topView));
            }

            // Handle cases: 5 & 6 above.
            return topView;
        }

        // Returns an offset coerced into the [0, Extent - Viewport] range.
        static private double CoerceOffset(double offset, double extent, double viewport)
        {
            if (offset > extent - viewport) { offset = extent - viewport; }
            if (offset < 0) { offset = 0; }
            return offset;
        }

        private bool IsScrolling
        {
            get { return (_scrollData != null) && (_scrollData._scrollOwner != null); }
        }

        private bool CanMouseWheelVerticallyScroll
        {
            get { return (SystemParameters.WheelScrollLines > 0); }
        }

        #endregion Scrolling Helper

        #endregion

        #region Protected Methods

        /// <summary>
        /// In normal(Star) pass this panel behaves like a StackPanel but during Auto(non Star) pass
        /// It returns minimum Width and Height required to represent the children. There is another
        /// mode wherein it provides laying out mechanism for InRibbonGallery in INRibbon mode.
        /// </summary>
        /// <param name="availableSize"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            RibbonGallery gallery = this.Gallery;
#if IN_RIBBON_GALLERY
            InRibbonGallery parentInRibbonGallery = gallery != null ? gallery.ParentInRibbonGallery : null;
            bool isInInRibbonMode = parentInRibbonGallery != null ? parentInRibbonGallery.IsInInRibbonMode : false;

            // For an InRibbonGallery rendering with IsDropDownOpen==true, we force gallery.ItemsPresenter's
            // MinWidth to be at least the value of IRG.ContentPresenter.ActualWidth.  This way, the IRG's popup
            // totally eclipses the IRG, which is required by the Office Fluent UI guidelines.
            if (gallery != null &&
                gallery.ItemsPresenter != null &&
                parentInRibbonGallery != null)
            {
                if (isInInRibbonMode && _irgIsConstrainingWidth)
                {
                    gallery.ItemsPresenter.MinWidth = _originalGalleryItemsPresenterMinWidth;
                    _irgIsConstrainingWidth = false;
                }
                else if (parentInRibbonGallery.IsDropDownOpen && !_irgIsConstrainingWidth)
                {
                    _originalGalleryItemsPresenterMinWidth = gallery.ItemsPresenter.MinWidth;
                    double minWidthFromParent = parentInRibbonGallery.CalculateGalleryItemsPresenterMinWidth();
                    gallery.ItemsPresenter.MinWidth = Math.Max(minWidthFromParent, _originalGalleryItemsPresenterMinWidth);
                    _irgIsConstrainingWidth = true;
                }
            }

            if (!isInInRibbonMode)
            {
#endif
                RibbonHelper.InitializeStarLayoutManager(this);
#if IN_RIBBON_GALLERY
            }
#endif

            IContainsStarLayoutManager iContainsStarLayoutManager = (IContainsStarLayoutManager)this;
            bool isStarLayoutPass = (iContainsStarLayoutManager.StarLayoutManager == null ? true : iContainsStarLayoutManager.StarLayoutManager.IsStarLayoutPass);

#if IN_RIBBON_GALLERY
            if (isInInRibbonMode)
            {
                PreComputeMaxRibbonGalleryItemWidthAndHeight();
                return InRibbonGalleryModeMeasureOverride(availableSize);
            }
            else
            {
#endif
                if (isStarLayoutPass)
                {
                    return RealMeasureOverride(availableSize);
                }
                else
                {
                    return AutoPassMeasureOverride();
                }
#if IN_RIBBON_GALLERY
            }
#endif
        }

#if IN_RIBBON_GALLERY
        // InRibbonGalleryMode where the gallery is shown within Ribbon via InRibbonGallery and Measure becomes responsibility
        // of InRibbonGalleryModeMeasureOverride.
        private Size InRibbonGalleryModeMeasureOverride(Size availableSize)
        {
            Size desiredSize = new Size();
            Size childConstraint = availableSize;
            childConstraint.Height = Double.PositiveInfinity;
            UIElementCollection children = InternalChildren;
            int childrenCount = children.Count;
            int galleryItemCount = 0;
            double galleryItemCumulativeHeight = 0.0;
            double maxChildWidth = 0.0;
            double maxChildHeight = 0.0;
            int rowOffset = 0;
            int colOffset = 0;

            // Measure the child and also sets start and end offsets of the categories to be used their ItemsPanel
            // (RibbonGalleryItemsPanel) in their Measure and Arrange algorithms when in InRibbonMode.
            for (int i = 0; i < childrenCount; i++)
            {
                RibbonGalleryCategory child = children[i] as RibbonGalleryCategory;

                if (child == null ||
                    child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                child.RowOffset = rowOffset;
                child.ColumnOffset = colOffset;
                child.Measure(childConstraint);
                Size childSize = child.DesiredSize;
                maxChildWidth = Math.Max(maxChildWidth, childSize.Width);
                maxChildHeight = Math.Max(maxChildHeight, childSize.Height);
                galleryItemCount += child.averageItemHeightInfo.Count;
                galleryItemCumulativeHeight += child.averageItemHeightInfo.CumulativeHeight;

                int childFullRows = Math.Max(0, child.RowCount - 1);
                if (child.RowCount > 0 && child.ColumnEndOffSet == 0)
                    childFullRows++;

                rowOffset += childFullRows;
                colOffset = child.ColumnEndOffSet;
            }

            if (galleryItemCount == 0 || galleryItemCumulativeHeight == 0.0)
            {
                internalScrollDelta = 16.0;
            }
            else
            {
                internalScrollDelta = galleryItemCumulativeHeight/galleryItemCount;
            }
            desiredSize.Width = maxChildWidth;
            desiredSize.Height = maxChildHeight;

            if (IsScrolling)
            {
                UpdateScrollingData(availableSize, desiredSize);
            }

            return desiredSize;
        }
#endif

        private Size AutoPassMeasureOverride()
        {
            Size desiredSize = new Size();
            Size childConstraint = new Size(double.PositiveInfinity, double.PositiveInfinity);
            IContainsStarLayoutManager iContainsStarLayoutManager = (IContainsStarLayoutManager)this;

            // This is Auto(non Star) pass
            UIElementCollection children = InternalChildren;
            int childrenCount, endCountForUpdate = children.Count;
            int galleryItemCount = 0;
            double galleryItemCumulativeHeight = 0.0;
            double maxChildWidth = 0.0;
            double maxChildHeight = 0.0;
            double maxColumnWidth = 0.0;

            // Since we support SharedColumnSizes across different categories, we need
            // to repeatedly measure them until their MaxColumnWidth has been synchronized.
            // The inner loop iterates over all of those categories that need an update.
            // Of course in the beginning all of them do. During that pass we detect if the
            // MaxColumnWidth has increased such that a previously measured category needs
            // an update. If so we make another pass over those categories once more. We
            // repeat this strategy until none of the categories need an update.

            while (endCountForUpdate > 0)
            {
                childrenCount = endCountForUpdate;
                endCountForUpdate = 0;
                for (int i = 0; i < childrenCount; i++)
                {
                    UIElement child = children[i] as UIElement;
                    child.Measure(childConstraint);
                    Size childSize = child.DesiredSize;
                    maxChildWidth = Math.Max(maxChildWidth, childSize.Width);
                    maxChildHeight = Math.Max(maxChildHeight, childSize.Height);

                    RibbonGalleryCategory category = child as RibbonGalleryCategory;
                    if (category != null)
                    {
                        galleryItemCount += category.averageItemHeightInfo.Count;
                        galleryItemCumulativeHeight += category.averageItemHeightInfo.CumulativeHeight;

                        // If the category is a ColumnSizeScope in itself it does not need
                        // to be synchronized with the gallery's scope and hence can be ignored.

                        if (!category.IsSharedColumnSizeScope && DoubleUtil.GreaterThan(category.MaxColumnWidth, maxColumnWidth))
                        {
                            maxColumnWidth = category.MaxColumnWidth;
                            endCountForUpdate = i;
                        }
                    }
                }
            }

            if (galleryItemCount == 0 || galleryItemCumulativeHeight == 0.0)
            {
                internalScrollDelta = 16.0;
            }
            else
            {
                internalScrollDelta = galleryItemCumulativeHeight/galleryItemCount;
            }

            desiredSize.Width = maxChildWidth;
            desiredSize.Height = maxChildHeight;

            return desiredSize;
        }

        /// <summary>
        /// General RibbonGalleryCategoriesPanel layout behavior is to grow unbounded in the "vertical" direction (Size To Content).
        /// Children in this dimension are encouraged to be as large as they like.  In the other dimension,
        /// RibbonGalleryCategoriesPanel will assume the maximum size of its children.
        /// </summary>
        /// <remarks>
        /// When scrolling, RibbonGalleryCategoriesPanel will not grow in layout size but effectively add the children on a z-plane which
        /// will probably be clipped by some parent (typically a ScrollContentPresenter) to Stack's size.
        /// </remarks>
        /// <param name="constraint">Constraint</param>
        /// <returns>Desired size</returns>

        private Size RealMeasureOverride(Size constraint)
        {
            UIElementCollection children = InternalChildren;
            Size stackDesiredSize = new Size();
            Size layoutSlotSize = constraint;

            //
            // Initialize child sizing and iterator data
            // Allow children as much size as they want along the stack.
            //
            layoutSlotSize.Height = Double.PositiveInfinity;

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

                stackDesiredSize.Width = Math.Max(stackDesiredSize.Width, childDesiredSize.Width);
                stackDesiredSize.Height += childDesiredSize.Height;
            }
            if (IsScrolling)
            {
                UpdateScrollingData(constraint, stackDesiredSize);
            }

            // Since we can offset and clip our content, we never need to be larger than the parent suggestion.
            // If we returned the full size of the content, we would always be so big we didn't need to scroll.  :)
            stackDesiredSize.Width = Math.Min(stackDesiredSize.Width, constraint.Width);
            stackDesiredSize.Height = Math.Min(stackDesiredSize.Height, constraint.Height);

            return stackDesiredSize;
        }

        // Compute Scrolling stuff while in Measure.
        private void UpdateScrollingData(Size constraint, Size stackDesiredSize)
        {
            double viewportOffsetY = (IsScrolling) ? _scrollData._offset.Y : 0;
            double logicalVisibleSpace = constraint.Height;

            // Compute viewport and extent.
            Size viewport = constraint;
            Size extent = stackDesiredSize;
            Vector offset = _scrollData._offset;

            // If we or children have resized, it's possible that we can now display more content.
            // This is true if we started at a nonzero offeset and still have space remaining.
            // In this case, we move up as much as remaining space.
            logicalVisibleSpace = DoubleUtil.LessThanOrClose((viewportOffsetY + logicalVisibleSpace) - stackDesiredSize.Height, 0.0) ? 0.0 : Math.Min((viewportOffsetY + logicalVisibleSpace) - stackDesiredSize.Height, constraint.Height);
            viewportOffsetY = Math.Max(viewportOffsetY - logicalVisibleSpace, 0.0);

            offset.Y = viewportOffsetY;
            offset.X = Math.Max(0, Math.Min(offset.X, extent.Width - viewport.Width));

            // Verify Scroll Info, invalidate ScrollOwner if necessary.
            VerifyScrollingData(viewport, extent, offset);
        }

        /// <summary>
        /// Content arrangement.
        /// </summary>
        /// <param name="finalSize">Arrange size</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            RibbonGallery gallery = this.Gallery;
#if IN_RIBBON_GALLERY
            if (gallery != null &&
                gallery.IsInInRibbonGalleryMode())
            {
                return InRibbonGalleryModeArrangeOverride(finalSize);
            }
            else
            {
#endif
                return RealArrangeOverride(finalSize);
#if IN_RIBBON_GALLERY
            }
#endif
        }

#if IN_RIBBON_GALLERY
        // InRibbonGalleryMode where the gallery is shown within Ribbon via InRibbonGallery and Arrange
        // becomes responsibility of InRibbonGalleryModeArrangeOverride.
        private Size InRibbonGalleryModeArrangeOverride(Size finalSize)
        {
            UIElementCollection children = this.Children;
            Rect rcChild = new Rect(finalSize);

            //
            // Seed scroll offset into rcChild.
            //
            if (IsScrolling)
            {
                rcChild.X = -1.0 * _scrollData._offset.X;
                rcChild.Y = -1.0 * _scrollData._offset.Y;
            }

            // Arrange and Position Children. over each other as the items being arranged in child
            // in a way to respect the offset of where previous category children are ending.
            // It's merged wrapping.
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                RibbonGalleryCategory child = children[i] as RibbonGalleryCategory;

                if (child == null ||
                    child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                rcChild.Width = Math.Max(finalSize.Width, child.DesiredSize.Width);
                child.Arrange(rcChild);
            }

            // Refresh the IsEnabled state of the InRibbonGallery's LineUp & LineDown buttons.
            RibbonGallery gallery = Gallery;
            if (gallery != null)
            {
                InRibbonGallery irg = gallery.ParentInRibbonGallery;
                if (irg != null &&
                    irg.IsInInRibbonMode)
                {
                    irg.CoerceValue(InRibbonGallery.CanLineUpProperty);
                    irg.CoerceValue(InRibbonGallery.CanLineDownProperty);
                }
            }

            return DesiredSize;
        }
#endif

        private Size RealArrangeOverride(Size finalSize)
        {
            UIElementCollection children = this.Children;
            Rect rcChild = new Rect(finalSize);
            double previousChildSize = 0.0;

            //
            // Compute scroll offset and seed it into rcChild.
            //
            if (IsScrolling)
            {
                rcChild.X = -1.0 * _scrollData._offset.X;
                rcChild.Y = -1.0 * _scrollData._offset.Y;
            }

            //
            // Arrange and Position Children.
            //
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                UIElement child = (UIElement)children[i];

                if (child == null) { continue; }

                rcChild.Y += previousChildSize;
                previousChildSize = child.DesiredSize.Height;
                rcChild.Height = previousChildSize;
                rcChild.Width = Math.Max(finalSize.Width, child.DesiredSize.Width);

                child.Arrange(rcChild);
            }

            return finalSize;
        }


        #endregion Protected Methods

        #region IProvideStarLayoutInfoBase Members

        public void OnInitializeLayout()
        {
            IContainsStarLayoutManager iContainsStarLayoutManager = (IContainsStarLayoutManager)this;
            if (iContainsStarLayoutManager.StarLayoutManager != null)
            {
                TreeHelper.InvalidateMeasureForVisualAncestorPath(this, RibbonHelper.IsISupportStarLayout);
                RibbonGallery gallery = this.Gallery;
                if (gallery != null)
                {
                    gallery.InvalidateMeasureOnAllCategoriesPanel();
                }
            }
        }

        public UIElement TargetElement
        {
            get
            {
                return this.Gallery;
            }
        }

        #endregion

        #region IContainsStarLayoutManager Members

        ISupportStarLayout IContainsStarLayoutManager.StarLayoutManager
        {
            get;
            set;
        }

        #endregion

        #region IScrollInfo Methods

        /// <summary>
        /// Scroll content by one line to the top.
        /// </summary>
        public void LineUp()
        {
            SetVerticalOffset(VerticalOffset - internalScrollDelta);
        }

        /// <summary>
        /// Scroll content by one line to the bottom.
        /// </summary>
        public void LineDown()
        {
            SetVerticalOffset(VerticalOffset + internalScrollDelta);
        }

        /// <summary>
        /// Scroll content by one line to the left.
        /// </summary>
        public void LineLeft()
        {
            SetHorizontalOffset(HorizontalOffset - _scrollLineDelta);
        }

        /// <summary>
        /// Scroll content by one line to the right.
        /// </summary>
        public void LineRight()
        {
            SetHorizontalOffset(HorizontalOffset + _scrollLineDelta);
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
        /// Scroll content by SystemParameters.WheelScrollLines lines to the top.
        /// </summary>
        public void MouseWheelUp()
        {
            if (CanMouseWheelVerticallyScroll)
            {
                SetVerticalOffset(VerticalOffset - SystemParameters.WheelScrollLines * internalScrollDelta);
            }
            else
            {
                PageUp();
            }
        }

        /// <summary>
        /// Scroll content by SystemParameters.WheelScrollLines lines to the bottom.
        /// </summary>
        public void MouseWheelDown()
        {
            if (CanMouseWheelVerticallyScroll)
            {
                SetVerticalOffset(VerticalOffset + SystemParameters.WheelScrollLines * internalScrollDelta);
            }
            else
            {
                PageDown();
            }
        }

        /// <summary>
        /// Scroll content a little to the left.
        /// </summary>
        public void MouseWheelLeft()
        {
            SetHorizontalOffset(HorizontalOffset - 3.0 * _scrollLineDelta);
        }

        /// <summary>
        /// Scroll content a little to the right.
        /// </summary>
        public void MouseWheelRight()
        {
            SetHorizontalOffset(HorizontalOffset + 3.0 * _scrollLineDelta);
        }

        /// <summary>
        /// Set the HorizontalOffset to the passed value.
        /// </summary>
        public void SetHorizontalOffset(double offset)
        {
            EnsureScrollData();
            double scrollX = CoerceOffset(offset, _scrollData._extent.Width, _scrollData._viewport.Width);
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
            double scrollY = CoerceOffset(offset, _scrollData._extent.Height, _scrollData._viewport.Height);
            if (!DoubleUtil.AreClose(scrollY, _scrollData._offset.Y))
            {
                _scrollData._offset.Y = scrollY;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// RibbonGalleryCategoriesPanel implementation of <seealso cref="IScrollInfo.MakeVisible" />.
        /// </summary>
        /// <param name="visual">The Visual that should become visible</param>
        /// <param name="rectangle">A rectangle representing in the visual's coordinate space to make visible.</param>
        /// <returns>
        /// A rectangle in the IScrollInfo's coordinate space that has been made visible.
        /// Other ancestors to in turn make this new rectangle visible.
        /// The rectangle should generally be a transformed version of the input rectangle.  In some cases, like
        /// when the input rectangle cannot entirely fit in the viewport, the return value might be smaller.
        /// </returns>
        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            // We can only work on visuals that are us or children.
            // An empty rect has no size or position.  We can't meaningfully use it.
            if (rectangle.IsEmpty
                || visual == null
                || visual == (Visual)this
                || !this.IsAncestorOf(visual))
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

            if (!rectangle.IsEmpty)
            {
                rectangle.X -= viewport.X;
                rectangle.Y -= viewport.Y;
            }

            // Return the rectangle
            return rectangle;
        }

        #endregion

        #region IScrollInfo Properties

        /// <summary>
        /// For RibbonGalleryCategoriesPanel it is always false.
        /// </summary>
        [DefaultValue(false)]
        public bool CanHorizontallyScroll
        {
            get
            {
                return false;
            }
            set
            {
                EnsureScrollData();
                if (_scrollData._allowHorizontal != value)
                {
                    _scrollData._allowHorizontal = false;
                    InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// For RibbonGalleryCategoriesPanel it is always true.
        /// </summary>
        [DefaultValue(true)]
        public bool CanVerticallyScroll
        {
            get
            {
                return true;
            }
            set
            {
                EnsureScrollData();
                if (_scrollData._allowVertical != value)
                {
                    _scrollData._allowVertical = true;
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
                return _scrollData._offset.X;
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
                return _scrollData._offset.Y;
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

        #region Data

        private RibbonGallery Gallery
        {
            get
            {
                return (RibbonGallery)ItemsControl.GetItemsOwner(this);
            }
        }

        // Logical scrolling.
        private ScrollData _scrollData;

        internal double internalScrollDelta;
        internal const double _scrollLineDelta = 16.0;   // Default physical amount to scroll with one Up/Down/Left/Right key

#if IN_RIBBON_GALLERY
        // Flag indicating whether we are constraining IRG.FirstGallery.ItemPresenter.MinWidth to be
        // approximately the ActualWidth of the IRG while in in-Ribbon mode.
        private bool _irgIsConstrainingWidth;
        private double _originalGalleryItemsPresenterMinWidth;
#endif

        #endregion Data

        #region Private Structures Classes

        //-----------------------------------------------------------
        // ScrollData class
        //-----------------------------------------------------------
        #region ScrollData

        // Helper class to hold scrolling data.
        // This class exists to reduce working set when StackPanel is used outside a scrolling situation.
        // Standard "extra pointer always for less data sometimes" cache savings model:
        //      !Scroll [1xReference]
        //      Scroll  [1xReference] + [4xDouble + 1xReference]
        private class ScrollData
        {
            // Clears layout generated data.
            // Does not clear scrollOwner, because unless resetting due to a scrollOwner change, we won't get reattached.
            internal void ClearLayout()
            {
                _offset = new Vector();
                _viewport = _extent = new Size();
                _allowHorizontal = false;
                _allowVertical = true;
            }

            // For Stack/Flow, the two dimensions of properties are in different units:
            // 1. The "logically scrolling" dimension uses items as units.
            // 2. The other dimension physically scrolls.  Units are in Avalon pixels (1/96").
            internal bool _allowHorizontal;
            internal bool _allowVertical;
            internal Vector _offset;            // Scroll offset of content.  Positive corresponds to a visually upward offset.
            internal Size _viewport;            // ViewportSize is in {pixels x items} (or vice-versa).
            internal Size _extent;              // Extent is the total number of children (logical dimension) or physical size
            internal ScrollViewer _scrollOwner; // ScrollViewer to which we're attached.
        }

        #endregion ScrollData

        #endregion Private Structures Classes
    }
}
