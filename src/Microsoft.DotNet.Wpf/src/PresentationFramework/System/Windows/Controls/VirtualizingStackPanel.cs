// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//#define Profiling

using MS.Internal;
using MS.Internal.Controls;
using MS.Utility;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Data;
using MS.Internal.Data;

namespace System.Windows.Controls
{
    /// <summary>
    /// VirtualizingStackPanel is used to arrange children into single line.
    /// </summary>
    public class VirtualizingStackPanel : VirtualizingPanel, IScrollInfo, IStackMeasure
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public VirtualizingStackPanel()
        {
            this.IsVisibleChanged += new DependencyPropertyChangedEventHandler(OnIsVisibleChanged);
        }

        //
        // DependencyProperty used by ItemValueStorage to store the PixelSize or LogicalSize of a
        // UIElement when it is a virtualized container. Used by TreeView and TreeViewItem
        // and ItemsControl to remember the size of TreeViewItems and GroupItems when
        // they get virtualized away.
        //
        private static readonly DependencyProperty ContainerSizeProperty = DependencyProperty.Register("ContainerSize", typeof(Size), typeof(VirtualizingStackPanel));

        //
        // DependencyProperty used by ItemValueStorage to store both the PixelSize and LogicalSize of a
        // UIElement when it is a virtualized container. For item-scrolling we need both.
        //
        private static readonly DependencyProperty ContainerSizeDualProperty = DependencyProperty.Register("ContainerSizeDual", typeof(ContainerSizeDual), typeof(VirtualizingStackPanel));

        //
        // DependencyProperty used by ItemValueStorage to store the flag that says if the containers
        // of this panel are all uniformly sized.
        //
        private static readonly DependencyProperty AreContainersUniformlySizedProperty = DependencyProperty.Register("AreContainersUniformlySized", typeof(bool), typeof(VirtualizingStackPanel));

        //
        // DependencyProperty used by ItemValueStorage to store the uniform size of the containers
        // of this panel if they are indeed uniformly sized. If they aren't uniformly sized then
        // this index is used to store the average of the realized container sizes in this panel
        // as a way of approximating the sizes of other containers of this panel that haven't
        // been realized yet.
        //
        private static readonly DependencyProperty UniformOrAverageContainerSizeProperty = DependencyProperty.Register("UniformOrAverageContainerSize", typeof(double), typeof(VirtualizingStackPanel));

        //
        // DependencyProperty used by ItemValueStorage to store the uniform size of the containers
        // of this panel if they are indeed uniformly sized. If they aren't uniformly sized then
        // this index is used to store the average of the realized container sizes in this panel
        // as a way of approximating the sizes of other containers of this panel that haven't
        // been realized yet.  For item-scrolling we need this quantity expressed in both
        // pixels and in items.
        //
        private static readonly DependencyProperty UniformOrAverageContainerSizeDualProperty = DependencyProperty.Register("UniformOrAverageContainerSizeDual", typeof(UniformOrAverageContainerSizeDual), typeof(VirtualizingStackPanel));

        // DependencyProperty used by ItemValueStorage to store the inset of an
        // inner ItemsHost - the distance from the ItemsHost to the outer boundary
        // of its owner, measured in the coordinate system of the ItemsHost.  This
        // property is only used when the owner is an IHierarchicalVirtualizationAndScrollInfo
        // (i.e. a TreeViewItem or a GroupItem).
        internal static readonly DependencyProperty ItemsHostInsetProperty = DependencyProperty.Register("ItemsHostInset", typeof(Thickness), typeof(VirtualizingStackPanel));

        // Implement the "uniform or average" optimization for ItemsHostInset,
        // if perf data warrants.   It probably needs to be independent of the
        // ItemExtent optimization;  the situation where insets are uniform but
        // extents are not seems pretty likely.

        static VirtualizingStackPanel()
        {
            lock (DependencyProperty.Synchronized)
            {
                _indicesStoredInItemValueStorage = new int[]
                    {   ContainerSizeProperty.GlobalIndex,
                        ContainerSizeDualProperty.GlobalIndex,
                        AreContainersUniformlySizedProperty.GlobalIndex,
                        UniformOrAverageContainerSizeProperty.GlobalIndex,
                        UniformOrAverageContainerSizeDualProperty.GlobalIndex,
                        ItemsHostInsetProperty.GlobalIndex
                    };
            }
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
        ///     Scroll content by one line to the top.
        ///     Subclases can override this method and call SetVerticalOffset to change
        ///     the behavior of what "line" means.
        /// </summary>
        public virtual void LineUp()
        {
            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.LineUp);
            }

            bool isHorizontal = (Orientation == Orientation.Horizontal);
            double newOffset = (IsPixelBased || isHorizontal)
                ? VerticalOffset - ScrollViewer._scrollLineDelta
                : NewItemOffset(isHorizontal, -1.0, fromFirst:true);

            SetVerticalOffsetImpl(newOffset, setAnchorInformation:true);
        }

        /// <summary>
        ///     Scroll content by one line to the bottom.
        ///     Subclases can override this method and call SetVerticalOffset to change
        ///     the behavior of what "line" means.
        /// </summary>
        public virtual void LineDown()
        {
            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.LineDown);
            }

            bool isHorizontal = (Orientation == Orientation.Horizontal);
            double newOffset = (IsPixelBased || isHorizontal)
                ? VerticalOffset + ScrollViewer._scrollLineDelta
                : NewItemOffset(isHorizontal, 1.0, fromFirst:false);

            SetVerticalOffsetImpl(newOffset, setAnchorInformation:true);
        }

        /// <summary>
        ///     Scroll content by one line to the left.
        ///     Subclases can override this method and call SetHorizontalOffset to change
        ///     the behavior of what "line" means.
        /// </summary>
        public virtual void LineLeft()
        {
            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.LineLeft);
            }

            bool isHorizontal = (Orientation == Orientation.Horizontal);
            double newOffset = (IsPixelBased || !isHorizontal)
                ? HorizontalOffset - ScrollViewer._scrollLineDelta
                : NewItemOffset(isHorizontal, -1.0, fromFirst:true);

            SetHorizontalOffsetImpl(newOffset, setAnchorInformation:true);
        }

        /// <summary>
        ///     Scroll content by one line to the right.
        ///     Subclases can override this method and call SetHorizontalOffset to change
        ///     the behavior of what "line" means.
        /// </summary>
        public virtual void LineRight()
        {
            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.LineRight);
            }

            bool isHorizontal = (Orientation == Orientation.Horizontal);
            double newOffset = (IsPixelBased || !isHorizontal)
                ? HorizontalOffset + ScrollViewer._scrollLineDelta
                : NewItemOffset(isHorizontal, 1.0, fromFirst:false);

            SetHorizontalOffsetImpl(newOffset, setAnchorInformation:true);
        }

        /// <summary>
        ///     Scroll content by one page to the top.
        ///     Subclases can override this method and call SetVerticalOffset to change
        ///     the behavior of what "page" means.
        /// </summary>
        public virtual void PageUp()
        {
            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.PageUp);
            }

            bool isHorizontal = (Orientation == Orientation.Horizontal);
            double newOffset = (IsPixelBased || isHorizontal)
                ? VerticalOffset - ViewportHeight
                : NewItemOffset(isHorizontal, -ViewportHeight, fromFirst:true);

            SetVerticalOffsetImpl(newOffset, setAnchorInformation:true);
        }

        /// <summary>
        ///     Scroll content by one page to the bottom.
        ///     Subclases can override this method and call SetVerticalOffset to change
        ///     the behavior of what "page" means.
        /// </summary>
        public virtual void PageDown()
        {
            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.PageDown);
            }

            bool isHorizontal = (Orientation == Orientation.Horizontal);
            double newOffset = (IsPixelBased || isHorizontal)
                ? VerticalOffset + ViewportHeight
                : NewItemOffset(isHorizontal, ViewportHeight, fromFirst:true);

            SetVerticalOffsetImpl(newOffset, setAnchorInformation:true);
        }

        /// <summary>
        ///     Scroll content by one page to the left.
        ///     Subclases can override this method and call SetHorizontalOffset to change
        ///     the behavior of what "page" means.
        /// </summary>
        public virtual void PageLeft()
        {
            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.PageLeft);
            }

            bool isHorizontal = (Orientation == Orientation.Horizontal);
            double newOffset = (IsPixelBased || !isHorizontal)
                ? HorizontalOffset - ViewportWidth
                : NewItemOffset(isHorizontal, -ViewportWidth, fromFirst:true);

            SetHorizontalOffsetImpl(newOffset, setAnchorInformation:true);
        }

        /// <summary>
        ///     Scroll content by one page to the right.
        ///     Subclases can override this method and call SetHorizontalOffset to change
        ///     the behavior of what "page" means.
        /// </summary>
        public virtual void PageRight()
        {
            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.PageRight);
            }

            bool isHorizontal = (Orientation == Orientation.Horizontal);
            double newOffset = (IsPixelBased || !isHorizontal)
                ? HorizontalOffset + ViewportWidth
                : NewItemOffset(isHorizontal, ViewportWidth, fromFirst:true);

            SetHorizontalOffsetImpl(newOffset, setAnchorInformation:true);
        }

        /// <summary>
        ///     Scroll content by one page to the top.
        ///     Subclases can override this method and call SetVerticalOffset to change
        ///     the behavior of the mouse wheel increment.
        /// </summary>
        public virtual void MouseWheelUp()
        {
            if (CanMouseWheelVerticallyScroll)
            {
                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                {
                    ScrollTracer.Trace(this, ScrollTraceOp.MouseWheelUp);
                }

                int lines = SystemParameters.WheelScrollLines;
                bool isHorizontal = (Orientation == Orientation.Horizontal);
                double newOffset = (IsPixelBased || isHorizontal)
                    ? VerticalOffset - lines * ScrollViewer._scrollLineDelta
                    : NewItemOffset(isHorizontal, (double)-lines, fromFirst:true);

                SetVerticalOffsetImpl(newOffset, setAnchorInformation:true);
            }
            else
            {
                PageUp();
            }
        }

        /// <summary>
        ///     Scroll content by one page to the bottom.
        ///     Subclases can override this method and call SetVerticalOffset to change
        ///     the behavior of the mouse wheel increment.
        /// </summary>
        public virtual void MouseWheelDown()
        {
            if (CanMouseWheelVerticallyScroll)
            {
                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                {
                    ScrollTracer.Trace(this, ScrollTraceOp.MouseWheelDown);
                }

                int lines = SystemParameters.WheelScrollLines;
                bool isHorizontal = (Orientation == Orientation.Horizontal);
                double newOffset = (IsPixelBased || isHorizontal)
                    ? VerticalOffset + lines * ScrollViewer._scrollLineDelta
                    : NewItemOffset(isHorizontal, (double)lines, fromFirst:false);

                SetVerticalOffsetImpl(newOffset, setAnchorInformation:true);
            }
            else
            {
                PageDown();
            }
        }

        /// <summary>
        ///     Scroll content by one page to the left.
        ///     Subclases can override this method and call SetHorizontalOffset to change
        ///     the behavior of the mouse wheel increment.
        /// </summary>
        public virtual void MouseWheelLeft()
        {
            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.MouseWheelLeft);
            }

            bool isHorizontal = (Orientation == Orientation.Horizontal);
            double newOffset = (IsPixelBased || !isHorizontal)
                ? HorizontalOffset - 3.0 * ScrollViewer._scrollLineDelta
                : NewItemOffset(isHorizontal, -3.0, fromFirst:true);

            SetHorizontalOffsetImpl(newOffset, setAnchorInformation:true);
        }

        /// <summary>
        ///     Scroll content by one page to the right.
        ///     Subclases can override this method and call SetHorizontalOffset to change
        ///     the behavior of the mouse wheel increment.
        /// </summary>
        public virtual void MouseWheelRight()
        {
            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.MouseWheelRight);
            }

            bool isHorizontal = (Orientation == Orientation.Horizontal);
            double newOffset = (IsPixelBased || !isHorizontal)
                ? HorizontalOffset + 3.0 * ScrollViewer._scrollLineDelta
                : NewItemOffset(isHorizontal, 3.0, fromFirst:false);

            SetHorizontalOffsetImpl(newOffset, setAnchorInformation:true);
        }

        /// <summary>
        ///     Return the new offset when item-scrolling along the principal axis.
        ///     This method handles the case where more than one container lies
        ///     at the top of the viewport - we want to compute the new offset
        ///     relative to the first or last of the "top" containers, not relative
        ///     to the current offset.
        ///
        ///     This can arise if an app declares a custom layout for TreeViewItem
        ///     (or GroupItem) that puts the ItemsHost for the children at the top
        ///     of the layout, e.g. a "side-by-side" layout.  See DevDiv2 1126786.
        ///     It can also arise if there are invisible containers at the top
        ///     of the viewport.
        /// </summary>
        /// <param name="isHorizontal">direction of the principal axis</param>
        /// <param name="delta">desired amount to change the offset</param>
        /// <param name="fromFirst">whether the delta should be added to the
        ///     offset of the first (shallowest) or last (deepest) of the "top" containers
        /// </param>
        private double NewItemOffset(bool isHorizontal, double delta, bool fromFirst)
        {
            Debug.Assert(!IsPixelBased && IsScrolling &&
                            (isHorizontal==(Orientation == Orientation.Horizontal)),
                "this method is only for use when item-scrolling along the principal axis");

            if (DoubleUtil.IsZero(delta))
            {
                delta = 1.0;        // scroll by at least one item
            }

            if (IsVSP45Compat)
            {
                // 4.5 computed the answer the simple way
                return (isHorizontal ? HorizontalOffset : VerticalOffset) + delta;
            }

            // find the top container(s)
            double firstContainerOffsetFromViewport;
            FrameworkElement deepestTopContainer = ComputeFirstContainerInViewport(
                this,   /* viewportElement */
                isHorizontal ? FocusNavigationDirection.Right : FocusNavigationDirection.Down,
                this,   /* itemsHost */
                null,   /* action callback */
                true,   /* findTopContainer */
                out firstContainerOffsetFromViewport);

            // there are two cases where we can still use the simple approach:
            //  a. first container can't be found - fallback to using current offset
            //  b. there's only one top container - no ambiguity, so no need to work hard
            if (deepestTopContainer == null || DoubleUtil.IsZero(firstContainerOffsetFromViewport))
            {
                return (isHorizontal ? HorizontalOffset : VerticalOffset) + delta;
            }

            // get the scroll offset of the deepest top container
            double startingOffset = FindScrollOffset(deepestTopContainer);

            // the shallowest top container's offset differs from the deepest's by
            // the offset-from-viewport.  This only works for item-scrolling,
            // where the top of the viewport is always at an item boundary.
            if (fromFirst)
            {
                startingOffset -= firstContainerOffsetFromViewport;
            }

            // reset the computed offset to agree with the new starting offset,
            // so as not to confuse anchoring calculations.
            if (isHorizontal)
            {
                _scrollData._computedOffset.X = startingOffset;
            }
            else
            {
                _scrollData._computedOffset.Y = startingOffset;
            }

            // return the desired offset
            return (startingOffset + delta);
        }

        /// <summary>
        /// Set the HorizontalOffset to the passed value.
        /// </summary>
        public void SetHorizontalOffset(double offset)
        {
            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.SetHorizontalOffset,
                    offset,
                    "delta:", offset - HorizontalOffset);
            }

            ClearAnchorInformation(true /*shouldAbort*/);
            SetHorizontalOffsetImpl(offset, setAnchorInformation:false);
        }

        private void SetHorizontalOffsetImpl(double offset, bool setAnchorInformation)
        {
            Debug.Assert(IsScrolling, "setting offset on non-scrolling panel");
            if (!IsScrolling)
                return;

            double scrollX = ScrollContentPresenter.ValidateInputOffset(offset, "HorizontalOffset");
            if (!DoubleUtil.AreClose(scrollX, _scrollData._offset.X))
            {
                Vector oldViewportOffset = _scrollData._offset;

                // Store the new offset
                _scrollData._offset.X = scrollX;

                // Report the change in offset
                OnViewportOffsetChanged(oldViewportOffset, _scrollData._offset);

                if (IsVirtualizing)
                {
                    IsScrollActive = true;
                    _scrollData.SetHorizontalScrollType(oldViewportOffset.X, scrollX);
                    InvalidateMeasure();

                    // if the scroll is small (at most one screenful), make it an
                    // anchored scroll if it's not already.  Otherwise problems
                    // can arise when the scroll devirtualizes new content that
                    // changes the average container size, thus changing the
                    // effective scroll offset of every item and the total extent;
                    // SetAndVerifyScrollData would try to maintain the ratio
                    // offset/extent, which ends up scrolling to a "random" place.
                    if (!IsVSP45Compat && Orientation == Orientation.Horizontal)
                    {
                        IncrementScrollGeneration();

                        double delta = Math.Abs(scrollX - oldViewportOffset.X);
                        if (DoubleUtil.LessThanOrClose(delta, ViewportWidth))
                        {
                            // When item-scrolling, the scroll offset is effectively
                            // an integer.  But certain operations (scroll-to-here,
                            // drag the thumb) can put fractional values into
                            // _scrollData.  Adding anchoring to such an operation
                            // produces an infinite loop:  the expected distance
                            // between viewports is fractional, but the actual distance
                            // is always integral.  To fix this, remove the fractions
                            // here, before setting the anchor.
                            if (!IsPixelBased)
                            {
                                _scrollData._offset.X = Math.Floor(_scrollData._offset.X);
                                _scrollData._computedOffset.X = Math.Floor(_scrollData._computedOffset.X);
                            }
                            else if (UseLayoutRounding)
                            {
                                DpiScale dpi = GetDpi();
                                // similarly, when layout rounding is enabled, round
                                // the offsets to pixel boundaries to avoid an infinite
                                // loop attempting to converge to a fractional offset
                                _scrollData._offset.X = UIElement.RoundLayoutValue(_scrollData._offset.X, dpi.DpiScaleX);
                                _scrollData._computedOffset.X = UIElement.RoundLayoutValue(_scrollData._computedOffset.X, dpi.DpiScaleX);
                            }

                            // resolve any ambiguity due to multiple top containers
                            // (this has already been done in NewItemOffset for
                            // most anchored scrolls, only small item-scrolls remain)
                            if (!setAnchorInformation && !IsPixelBased)
                            {
                                double topContainerOffset;
                                FrameworkElement deepestTopContainer = ComputeFirstContainerInViewport(
                                    this,   /* viewportElement */
                                    FocusNavigationDirection.Right,
                                    this,   /* itemsHost */
                                    null,   /* action callback */
                                    true,   /* findTopContainer */
                                    out topContainerOffset);

                                if (topContainerOffset > 0.0)
                                {
                                    // if there are multiple top containers, reset
                                    // the current offset to agree with the last one
                                    double startingOffset = FindScrollOffset(deepestTopContainer);
                                    _scrollData._computedOffset.X = startingOffset;
                                }
                            }

                            setAnchorInformation = true;
                        }
                    }
                }
                else if (!IsPixelBased)
                {
                    InvalidateMeasure();
                }
                else
                {
                    _scrollData._offset.X  = ScrollContentPresenter.CoerceOffset(scrollX, _scrollData._extent.Width, _scrollData._viewport.Width);
                    _scrollData._computedOffset.X = _scrollData._offset.X;
                    InvalidateArrange();
                    OnScrollChange();
                }

                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                {
                    ScrollTracer.Trace(this, ScrollTraceOp.SetHOff,
                        _scrollData._offset, _scrollData._extent, _scrollData._computedOffset);
                }
            }

            if (setAnchorInformation)
            {
                SetAnchorInformation(isHorizontalOffset:true);
            }
        }

        /// <summary>
        /// Set the VerticalOffset to the passed value.
        /// </summary>
        public void SetVerticalOffset(double offset)
        {
            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.SetVerticalOffset,
                    offset,
                    "delta:", offset - VerticalOffset);
            }

            ClearAnchorInformation(true /*shouldAbort*/);
            SetVerticalOffsetImpl(offset, setAnchorInformation:false);
        }

        private void SetVerticalOffsetImpl(double offset, bool setAnchorInformation)
        {
            Debug.Assert(IsScrolling, "setting offset on non-scrolling panel");
            if (!IsScrolling)
                return;

            double scrollY = ScrollContentPresenter.ValidateInputOffset(offset, "VerticalOffset");
            if (!DoubleUtil.AreClose(scrollY, _scrollData._offset.Y))
            {
                Vector oldViewportOffset = _scrollData._offset;

                // Store the new offset
                _scrollData._offset.Y = scrollY;

                // Report the change in offset
                OnViewportOffsetChanged(oldViewportOffset, _scrollData._offset);

                if (IsVirtualizing)
                {
                    InvalidateMeasure();
                    IsScrollActive = true;
                    _scrollData.SetVerticalScrollType(oldViewportOffset.Y, scrollY);

                    // if the scroll is small (at most one screenful), make it an
                    // anchored scroll if it's not already.  Otherwise problems
                    // can arise when the scroll devirtualizes new content that
                    // changes the average container size, thus changing the
                    // effective scroll offset of every item and the total extent;
                    // SetAndVerifyScrollData would try to maintain the ratio
                    // offset/extent, which ends up scrolling to a "random" place.
                    if (!IsVSP45Compat && Orientation == Orientation.Vertical)
                    {
                        IncrementScrollGeneration();

                        double delta = Math.Abs(scrollY - oldViewportOffset.Y);
                        if (DoubleUtil.LessThanOrClose(delta, ViewportHeight))
                        {
                            // When item-scrolling, the scroll offset is effectively
                            // an integer.  But certain operations (scroll-to-here,
                            // drag the thumb) can put fractional values into
                            // _scrollData.  Adding anchoring to such an operation
                            // produces an infinite loop:  the expected distance
                            // between viewports is fractional, but the actual distance
                            // is always integral.  To fix this, remove the fractions
                            // here, before setting the anchor.
                            if (!IsPixelBased)
                            {
                                _scrollData._offset.Y = Math.Floor(_scrollData._offset.Y);
                                _scrollData._computedOffset.Y = Math.Floor(_scrollData._computedOffset.Y);
                            }
                            else if (UseLayoutRounding)
                            {
                                DpiScale dpi = GetDpi();
                                // similarly, when layout rounding is enabled, round
                                // the offsets to pixel boundaries to avoid an infinite
                                // loop attempting to converge to a fractional offset
                                _scrollData._offset.Y = UIElement.RoundLayoutValue(_scrollData._offset.Y, dpi.DpiScaleY);
                                _scrollData._computedOffset.Y = UIElement.RoundLayoutValue(_scrollData._computedOffset.Y, dpi.DpiScaleY);
                            }

                            // resolve any ambiguity due to multiple top containers
                            // (this has already been done in NewItemOffset for
                            // most anchored scrolls, only small item-scrolls remain)
                            if (!setAnchorInformation && !IsPixelBased)
                            {
                                double topContainerOffset;
                                FrameworkElement deepestTopContainer = ComputeFirstContainerInViewport(
                                    this,   /* viewportElement */
                                    FocusNavigationDirection.Down,
                                    this,   /* itemsHost */
                                    null,   /* action callback */
                                    true,   /* findTopContainer */
                                    out topContainerOffset);

                                if (topContainerOffset > 0.0)
                                {
                                    // if there are multiple top containers, reset
                                    // the current offset to agree with the last one
                                    double startingOffset = FindScrollOffset(deepestTopContainer);
                                    _scrollData._computedOffset.Y = startingOffset;
                                }
                            }

                            setAnchorInformation = true;
                        }
                    }
                }
                else if (!IsPixelBased)
                {
                    InvalidateMeasure();
                }
                else
                {
                    _scrollData._offset.Y  = ScrollContentPresenter.CoerceOffset(scrollY, _scrollData._extent.Height, _scrollData._viewport.Height);
                    _scrollData._computedOffset.Y = _scrollData._offset.Y;
                    InvalidateArrange();
                    OnScrollChange();
                }
            }

            if (setAnchorInformation)
            {
                SetAnchorInformation(isHorizontalOffset:false);
            }

            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.SetVOff,
                    _scrollData._offset, _scrollData._extent, _scrollData._computedOffset);
            }
        }

        private void SetAnchorInformation(bool isHorizontalOffset)
        {
            if (IsScrolling)
            {
                //
                // Anchoring is a technique used to overcome the shortcoming that when virtualizing
                // the extent size is an estimation and could fluctuate as we measure more containers.
                // So we only care to employ this technique when virtualizing.
                //
                if (IsVirtualizing)
                {
                    //
                    // We only care about anchoring along the stacking direction because
                    // that is the direction along which we virtualize
                    //
                    bool isHorizontal = (Orientation == Orientation.Horizontal);
                    if (isHorizontal == isHorizontalOffset)
                    {
                        bool areContainersUniformlySized = GetAreContainersUniformlySized(null, this);

                        //
                        // If the containers in this panel aren't uniformly sized or if this is a hierarchical scenario
                        // involving grouping or TreeView then the chances that we err in our extent estimations
                        // are larger and hence we need to store the anchor information.
                        //
                        if (!areContainersUniformlySized || HasVirtualizingChildren)
                        {
                            ItemsControl itemsControl;
                            ItemsControl.GetItemsOwnerInternal(this, out itemsControl);

                            if (itemsControl != null)
                            {
                                bool isTracing = ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this);

                                double expectedDistanceBetweenViewports = (isHorizontal ? _scrollData._offset.X - _scrollData._computedOffset.X : _scrollData._offset.Y - _scrollData._computedOffset.Y);

                                if (isTracing)
                                {
                                    ScrollTracer.Trace(this, ScrollTraceOp.BSetAnchor,
                                        expectedDistanceBetweenViewports);
                                }

                                if (_scrollData._firstContainerInViewport != null)
                                {
                                    //
                                    // Retry the pending AnchorOperation
                                    //
                                    OnAnchorOperation(true /*isAnchorOperationPending*/);

                                    //
                                    // Adjust offsets
                                    //
                                    if (isHorizontal)
                                    {
                                        _scrollData._offset.X += expectedDistanceBetweenViewports;
                                    }
                                    else
                                    {
                                        _scrollData._offset.Y += expectedDistanceBetweenViewports;
                                    }
                                }

                                if (_scrollData._firstContainerInViewport == null)
                                {
                                    _scrollData._firstContainerInViewport = ComputeFirstContainerInViewport(
                                        itemsControl.GetViewportElement(),
                                        isHorizontal ? FocusNavigationDirection.Right : FocusNavigationDirection.Down,
                                        this,
                                        delegate(DependencyObject d)
                                        {
                                            // Mark this container non-virtualizable because it is along the path leading to
                                            // the leaf container that will serve as an anchor for the current scroll operation.
                                            d.SetCurrentValue(VirtualizingPanel.IsContainerVirtualizableProperty, false);
                                        },
                                        false,  /* findTopContainer */
                                        out _scrollData._firstContainerOffsetFromViewport);

                                    if (_scrollData._firstContainerInViewport != null)
                                    {
                                        _scrollData._expectedDistanceBetweenViewports = expectedDistanceBetweenViewports;

                                        Debug.Assert(AnchorOperationField.GetValue(this) == null, "There is already a pending AnchorOperation.");
                                        DispatcherOperation anchorOperation = Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)OnAnchorOperation);
                                        AnchorOperationField.SetValue(this, anchorOperation);
                                    }
                                }
                                else
                                {
                                    _scrollData._expectedDistanceBetweenViewports += expectedDistanceBetweenViewports;
                                }

                                if (isTracing)
                                {
                                    ScrollTracer.Trace(this, ScrollTraceOp.ESetAnchor,
                                        _scrollData._expectedDistanceBetweenViewports,
                                        _scrollData._firstContainerInViewport,
                                        _scrollData._firstContainerOffsetFromViewport);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnAnchorOperation()
        {
            bool isAnchorOperationPending = false;
            OnAnchorOperation(isAnchorOperationPending);
        }

        private void OnAnchorOperation(bool isAnchorOperationPending)
        {
            Debug.Assert(_scrollData._firstContainerInViewport != null, "Must have an anchor element");

            ItemsControl itemsControl;
            ItemsControl.GetItemsOwnerInternal(this, out itemsControl);
            if (itemsControl == null || !VisualTreeHelper.IsAncestorOf(this, _scrollData._firstContainerInViewport))
            {
                ClearAnchorInformation(isAnchorOperationPending /*shouldAbort*/);
                return;
            }

            bool isTracing = ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this);
            if (isTracing)
            {
                ScrollTracer.Trace(this, ScrollTraceOp.BOnAnchor,
                    isAnchorOperationPending,
                    _scrollData._expectedDistanceBetweenViewports,
                    _scrollData._firstContainerInViewport);
            }

            // when called asynchronously (isAnchorOperationPending=false),
            // this method depends on having valid layout state, which is normally
            // true at this point.  But it's not true if the layout manager switched
            // to background layout.  In that case, try again at Background priority.
            bool isVSP45Compat = IsVSP45Compat;
            if (!isVSP45Compat && !isAnchorOperationPending && (MeasureDirty || ArrangeDirty))
            {
                Debug.Assert(AnchorOperationField.GetValue(this) != null, "anchor state is inconsistent");

                if (isTracing)
                {
                    ScrollTracer.Trace(this, ScrollTraceOp.ROnAnchor);
                }

                CancelPendingAnchoredInvalidateMeasure();

                // schedule a new anchor operation, and remember it.
                // (We can't merely change the priority of the old operation, since
                // we need to run after the background UpdateLayout task.)
                DispatcherOperation anchorOperation = Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)OnAnchorOperation);
                AnchorOperationField.SetValue(this, anchorOperation);
                return;
            }

            bool isHorizontal = (Orientation == Orientation.Horizontal);

            FrameworkElement prevFirstContainerInViewport = _scrollData._firstContainerInViewport;
            double prevFirstContainerOffsetFromViewport = _scrollData._firstContainerOffsetFromViewport;
            double prevFirstContainerOffset = FindScrollOffset(_scrollData._firstContainerInViewport);

            double currFirstContainerOffsetFromViewport;
            FrameworkElement currFirstContainerInViewport = ComputeFirstContainerInViewport(
                itemsControl.GetViewportElement(),
                isHorizontal ? FocusNavigationDirection.Right : FocusNavigationDirection.Down,
                this,
                null,
                false,   /* findTopContainer */
                out currFirstContainerOffsetFromViewport);
            Debug.Assert(currFirstContainerInViewport != null, "Cannot find container in viewport");
            double currFirstContainerOffset = FindScrollOffset(currFirstContainerInViewport);

            double actualDistanceBetweenViewports = (currFirstContainerOffset - currFirstContainerOffsetFromViewport) -
                                                    (prevFirstContainerOffset - prevFirstContainerOffsetFromViewport);

            bool success = (LayoutDoubleUtil.AreClose(_scrollData._expectedDistanceBetweenViewports, actualDistanceBetweenViewports));

            // if the simple test for success fails, check some more complex cases
            if (!success && !isVSP45Compat)
            {
                if (!IsPixelBased)
                {
                    // if item-scrolling to a position with more than one "top container",
                    // success is when any of the top containers match the expected distance
                    double topContainerOffset;
                    FrameworkElement deepestTopContainer = ComputeFirstContainerInViewport(
                        this,
                        isHorizontal ? FocusNavigationDirection.Right : FocusNavigationDirection.Down,
                        this,
                        null,
                        true,   /* findTopContainer*/
                        out topContainerOffset);
                    double diff = actualDistanceBetweenViewports - _scrollData._expectedDistanceBetweenViewports;
                    success = (!LayoutDoubleUtil.LessThan(diff, 0.0) &&
                                !LayoutDoubleUtil.LessThan(topContainerOffset, diff));

                    if (success)
                    {
                        // adjust the offset from viewport by the top-container size,
                        // so that we reset the computed offset (below) to agree with
                        // the most recent measure pass
                        currFirstContainerOffsetFromViewport += topContainerOffset;
                    }

                    // item-scrolling to the last page should be treated as a success,
                    // regardless of the distance between viewports (there's 4.5rtm code
                    // farther down that tries to check this, but it use the previous
                    // offset and the current viewport size, which doesn't work in
                    // situations where the viewport size has changed)
                    if (!success)
                    {
                        if (isHorizontal)
                        {
                            success = DoubleUtil.GreaterThanOrClose(_scrollData._computedOffset.X,
                                                                _scrollData._extent.Width - _scrollData._viewport.Width);
                        }
                        else
                        {
                            success = DoubleUtil.GreaterThanOrClose(_scrollData._computedOffset.Y,
                                                                _scrollData._extent.Height - _scrollData._viewport.Height);
                        }
                    }
                }
            }

            if (success)
            {
                if (isHorizontal)
                {
                    _scrollData._computedOffset.X = currFirstContainerOffset - currFirstContainerOffsetFromViewport;
                    _scrollData._offset.X = _scrollData._computedOffset.X;
                }
                else
                {
                    _scrollData._computedOffset.Y = currFirstContainerOffset - currFirstContainerOffsetFromViewport;
                    _scrollData._offset.Y = _scrollData._computedOffset.Y;
                }

                //
                // If we are at the right position with respect to the anchor then
                // we dont need the anchor element any more. So clear it.
                //
                ClearAnchorInformation(isAnchorOperationPending /*shouldAbort*/);

                if (isTracing)
                {
                    ScrollTracer.Trace(this, ScrollTraceOp.SOnAnchor,
                        _scrollData._offset);
                }
            }
            else
            {
                bool remeasure = false;
                double actualOffset, expectedOffset;

                if (isHorizontal)
                {
                    _scrollData._computedOffset.X = prevFirstContainerOffset - prevFirstContainerOffsetFromViewport;

                    actualOffset = _scrollData._computedOffset.X + actualDistanceBetweenViewports;
                    expectedOffset = _scrollData._computedOffset.X + _scrollData._expectedDistanceBetweenViewports;

                    if (DoubleUtil.LessThan(expectedOffset, 0) || DoubleUtil.GreaterThan(expectedOffset, _scrollData._extent.Width - _scrollData._viewport.Width))
                    {
                        // the condition can fail due to estimated sizes in subtrees that contribute
                        // to FindScrollOffset(_scrollData._firstContainerInViewport) but not to
                        // _scrollData._extent.  If that happens, remeasure.
                        if (DoubleUtil.AreClose(actualOffset, 0) || DoubleUtil.AreClose(actualOffset, _scrollData._extent.Width - _scrollData._viewport.Width))
                        {
                            _scrollData._computedOffset.X = actualOffset;
                            _scrollData._offset.X = actualOffset;
                        }
                        else
                        {
                            remeasure = true;
                            _scrollData._offset.X = expectedOffset;
                        }
                    }
                    else
                    {
                        remeasure = true;
                        _scrollData._offset.X = expectedOffset;
                    }
                }
                else
                {
                    _scrollData._computedOffset.Y = prevFirstContainerOffset - prevFirstContainerOffsetFromViewport;

                    actualOffset = _scrollData._computedOffset.Y + actualDistanceBetweenViewports;
                    expectedOffset = _scrollData._computedOffset.Y + _scrollData._expectedDistanceBetweenViewports;

                    if (DoubleUtil.LessThan(expectedOffset, 0) || DoubleUtil.GreaterThan(expectedOffset, _scrollData._extent.Height - _scrollData._viewport.Height))
                    {
                        // the condition can fail due to estimated sizes in subtrees that contribute
                        // to FindScrollOffset(_scrollData._firstContainerInViewport) but not to
                        // _scrollData._extent.  If that happens, remeasure.
                        if (DoubleUtil.AreClose(actualOffset, 0) || DoubleUtil.AreClose(actualOffset, _scrollData._extent.Height - _scrollData._viewport.Height))
                        {
                            _scrollData._computedOffset.Y = actualOffset;
                            _scrollData._offset.Y = actualOffset;
                        }
                        else
                        {
                            remeasure = true;
                            _scrollData._offset.Y = expectedOffset;
                        }
                    }
                    else
                    {
                        remeasure = true;
                        _scrollData._offset.Y = expectedOffset;
                    }
                }

                if (isTracing)
                {
                    ScrollTracer.Trace(this, ScrollTraceOp.EOnAnchor,
                        remeasure, expectedOffset, actualOffset, _scrollData._offset, _scrollData._computedOffset);
                }

                if (remeasure)
                {
                    //
                    // We have adjusted the offset and need to remeasure
                    //
                    OnScrollChange();
                    InvalidateMeasure();

                    if (!isVSP45Compat)
                    {
                        CancelPendingAnchoredInvalidateMeasure();

                        // remeasure from the root should use fresh effective offsets
                        IncrementScrollGeneration();
                    }

                    if (!isAnchorOperationPending)
                    {
                        DispatcherOperation anchorOperation = Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)OnAnchorOperation);
                        AnchorOperationField.SetValue(this, anchorOperation);
                    }

                    if (!isVSP45Compat && IsScrollActive)
                    {
                        // abort the pending task to end the scrolling operation;  we're not done yet
                        DispatcherOperation clearIsScrollActiveOperation =
                                    ClearIsScrollActiveOperationField.GetValue(this);
                        if (clearIsScrollActiveOperation != null)
                        {
                            clearIsScrollActiveOperation.Abort();
                            ClearIsScrollActiveOperationField.SetValue(this, null);
                        }
                    }
                }
                else
                {
                    ClearAnchorInformation(isAnchorOperationPending /*shouldAbort*/);
                }
            }
        }

        private void ClearAnchorInformation(bool shouldAbort)
        {
            if (_scrollData == null)
                return;

            if (_scrollData._firstContainerInViewport != null)
            {
                DependencyObject element = _scrollData._firstContainerInViewport;
                do
                {
                    DependencyObject parent = VisualTreeHelper.GetParent(element);
                    Panel parentItemsHost = parent as Panel;
                    if (parentItemsHost != null && parentItemsHost.IsItemsHost)
                    {
                        // This to clear the current value that we previously set
                        element.InvalidateProperty(VirtualizingPanel.IsContainerVirtualizableProperty);
                    }
                    element = parent;
                }
                while (element != null && element != this);

                _scrollData._firstContainerInViewport = null;
                _scrollData._firstContainerOffsetFromViewport = 0;
                _scrollData._expectedDistanceBetweenViewports = 0;

                if (shouldAbort)
                {
                    DispatcherOperation anchorOperation = AnchorOperationField.GetValue(this);
                    anchorOperation.Abort();
                }

                AnchorOperationField.ClearValue(this);
            }
        }

        private FrameworkElement ComputeFirstContainerInViewport(
            FrameworkElement viewportElement,
            FocusNavigationDirection direction,
            Panel itemsHost,
            Action<DependencyObject> action,
            bool findTopContainer,
            out double firstContainerOffsetFromViewport)
        {
            bool foundTopContainer;
            return ComputeFirstContainerInViewport(
                viewportElement, direction, itemsHost, action, findTopContainer,
                out firstContainerOffsetFromViewport, out foundTopContainer);
        }

        private FrameworkElement ComputeFirstContainerInViewport(
            FrameworkElement viewportElement,
            FocusNavigationDirection direction,
            Panel itemsHost,
            Action<DependencyObject> action,
            bool findTopContainer,
            out double firstContainerOffsetFromViewport,
            out bool foundTopContainer)
        {
            Debug.Assert(!IsPixelBased || !findTopContainer, "find 'top' container only makes sense when item-scrolling");
            firstContainerOffsetFromViewport = 0;
            foundTopContainer = false;

            if (itemsHost == null)
            {
                return null;
            }

            bool isVSP45Compat = IsVSP45Compat;
            if (!isVSP45Compat)
            {
                // 4.5 used itemsControl.GetViewportElement() as the viewportElement,
                // but should have used this (the top-level VSP).   This matters
                // when the panel (this) is offset from the viewportElement (typically
                // a ScrollContentPresenter), say by a margin or border on some
                // element between the viewportElement and the panel.   For example,
                // Freeze occurs when using VirtualizingPanel.IsVirtualizingWhenGrouping
                // - and in this example the ItemPresenter immediately above the panel
                // has a margin.
                //
                // In this case, the method returns an offset describing the distance
                // from the first container to the viewportElement, whereas other
                // methods (like FindScrollOffset) describe the distance from the
                // container to the top-level panel.  This difference leads to
                // incorrect decisions in OnAnchorOperation, causing infinite loops.
                viewportElement = this;
            }

            FrameworkElement result = null;
            UIElementCollection children = itemsHost.Children;
            if (children != null)
            {
                int count = children.Count;
                int invisibleContainers = 0;
                int i = (itemsHost is VirtualizingStackPanel ? ((VirtualizingStackPanel)itemsHost)._firstItemInExtendedViewportChildIndex : 0);
                for (; i<count; i++)
                {
                    FrameworkElement fe = children[i] as FrameworkElement;
                    if (fe == null)
                        continue;

                    if (fe.IsVisible)
                    {
                        Rect elementRect;

                        // get the vp-position of the element, ignoring the secondary axis
                        // (DevDiv2 1136036, 1203626 show two different cases why we
                        // ignore the secondary axis - both involving searches that
                        // miss the desired container merely because it's off-screen
                        // horizontally, in a vertically scrolling panel)
                        ElementViewportPosition elementPosition = ItemsControl.GetElementViewportPosition(
                            viewportElement,
                            fe,
                            direction,
                            false /*fullyVisible*/,
                            !isVSP45Compat /*ignorePerpendicularAxis*/,
                            out elementRect);

                        if (elementPosition == ElementViewportPosition.PartiallyInViewport ||
                            elementPosition == ElementViewportPosition.CompletelyInViewport)
                        {
                            bool isTopContainer = false;

                            if (!IsPixelBased)
                            {
                                double startPosition = (direction == FocusNavigationDirection.Down)
                                    ? elementRect.Y : elementRect.X;
                                if (findTopContainer)
                                {
                                    // when looking for a "top" container, break as soon
                                    // as we find a child that's positioned after the start
                                    // of the viewport

                                    if (DoubleUtil.GreaterThan(startPosition, 0.0))
                                    {
                                        break;
                                    }
                                }

                                // determine if this is a top container
                                isTopContainer = DoubleUtil.IsZero(startPosition);
                            }

                            if (action != null)
                            {
                                action(fe);
                            }

                            if (isVSP45Compat)
                            {
                                ItemsControl itemsControl = fe as ItemsControl;
                                if (itemsControl != null)
                                {
                                    if (itemsControl.ItemsHost != null && itemsControl.ItemsHost.IsVisible)
                                    {
                                        result = ComputeFirstContainerInViewport(viewportElement, direction, itemsControl.ItemsHost, action, findTopContainer, out firstContainerOffsetFromViewport);
                                    }
                                }
                                else
                                {
                                    GroupItem groupItem = fe as GroupItem;
                                    if (groupItem != null && groupItem.ItemsHost != null && groupItem.ItemsHost.IsVisible)
                                    {
                                        result = ComputeFirstContainerInViewport(viewportElement, direction, groupItem.ItemsHost, action, findTopContainer, out firstContainerOffsetFromViewport);
                                    }
                                }
                            }
                            else
                            {
                                Panel innerPanel = null;
                                ItemsControl itemsControl;
                                GroupItem groupItem;

                                if ((itemsControl = fe as ItemsControl) != null)
                                {
                                    innerPanel = itemsControl.ItemsHost;
                                }
                                else if ((groupItem = fe as GroupItem) != null)
                                {
                                    innerPanel = groupItem.ItemsHost;
                                }

                                // don't delve into non-VSP panels - the result is
                                // inconsistent with FindScrollOffset, and causes
                                // infinite loops
                                innerPanel = innerPanel as VirtualizingStackPanel;

                                if (innerPanel != null && innerPanel.IsVisible)
                                {
                                    result = ComputeFirstContainerInViewport(viewportElement, direction, innerPanel, action, findTopContainer, out firstContainerOffsetFromViewport, out foundTopContainer);
                                }
                            }

                            if (result == null)
                            {
                                result = fe;
                                foundTopContainer = isTopContainer;

                                if (IsPixelBased)
                                {
                                    if (direction == FocusNavigationDirection.Down)
                                    {
                                        firstContainerOffsetFromViewport = elementRect.Y;
                                        if (!isVSP45Compat)
                                        {
                                            firstContainerOffsetFromViewport -= fe.Margin.Top;
                                        }
                                    }
                                    else // (direction == FocusNavigationDirection.Right)
                                    {
                                        firstContainerOffsetFromViewport = elementRect.X;
                                        if (!isVSP45Compat)
                                        {
                                            firstContainerOffsetFromViewport -= fe.Margin.Left;
                                        }
                                    }
                                }
                                else if (findTopContainer && isTopContainer)
                                {
                                    // when looking for a top-container, invisible
                                    // containers contribute to the offset,
                                    firstContainerOffsetFromViewport += invisibleContainers;
                                }
                            }
                            else if (!IsPixelBased)
                            {
                                IHierarchicalVirtualizationAndScrollInfo virtualizingElement = fe as IHierarchicalVirtualizationAndScrollInfo;
                                if (virtualizingElement != null)
                                {
                                    if (isVSP45Compat)
                                    {
                                        if (direction == FocusNavigationDirection.Down)
                                        {
                                            if (DoubleUtil.GreaterThanOrClose(elementRect.Y, 0))
                                            {
                                                firstContainerOffsetFromViewport += virtualizingElement.HeaderDesiredSizes.LogicalSize.Height;
                                            }
                                        }
                                        else // (direction == FocusNavigationDirection.Right)
                                        {
                                            if (DoubleUtil.GreaterThanOrClose(elementRect.X, 0))
                                            {
                                                firstContainerOffsetFromViewport += virtualizingElement.HeaderDesiredSizes.LogicalSize.Width;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // If the current container's item is considered
                                        // to be displayed before the subitems,
                                        // it contributes 1 to the offset.
                                        // Exception - If findTopContainer is false,
                                        // ignore the contribution of "top" containers
                                        // when 'result' is also a top container - so
                                        // that its offset is always reported as 0;
                                        // this simplifies the anchoring logic.
                                        Thickness inset = GetItemsHostInsetForChild(virtualizingElement);
                                        if (direction == FocusNavigationDirection.Down)
                                        {
                                            if (IsHeaderBeforeItems(false, fe, ref inset) &&
                                                DoubleUtil.GreaterThanOrClose(elementRect.Y, 0))
                                            {
                                                if (findTopContainer ||
                                                    !foundTopContainer ||                       // already found a non-top container
                                                    DoubleUtil.GreaterThan(elementRect.Y, 0))   // this container is non-top
                                                {
                                                    firstContainerOffsetFromViewport += 1;
                                                }
                                            }
                                        }
                                        else // (direction == FocusNavigationDirection.Right)
                                        {
                                            if (IsHeaderBeforeItems(true, fe, ref inset) &&
                                                DoubleUtil.GreaterThanOrClose(elementRect.X, 0))
                                            {
                                                if (findTopContainer ||
                                                    !foundTopContainer ||                       // already found a non-top container
                                                    DoubleUtil.GreaterThan(elementRect.X, 0))   // this container is non-top
                                                {
                                                    firstContainerOffsetFromViewport += 1;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            break;
                        }
                        else if (elementPosition == ElementViewportPosition.AfterViewport)
                        {
                            // We've gone too far
                            break;
                        }

                        invisibleContainers = 0;
                    }
                    else
                    {
                        // accumulate the size of the region of invisible containers
                        ++invisibleContainers;
                    }
                }
            }

            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.CFCIV,
                    ContainerPath(result), firstContainerOffsetFromViewport);
            }

            return result;
        }

        internal void AnchoredInvalidateMeasure()
        {
            WasLastMeasurePassAnchored = (FirstContainerInViewport != null) || (BringIntoViewLeafContainer != null);

            DispatcherOperation anchoredInvalidateMeasureOperation = AnchoredInvalidateMeasureOperationField.GetValue(this);
            if (anchoredInvalidateMeasureOperation == null)
            {
                anchoredInvalidateMeasureOperation = Dispatcher.BeginInvoke(DispatcherPriority.Render,
                    (Action)delegate()
                {
                    if (IsVSP45Compat)
                    {
                        AnchoredInvalidateMeasureOperationField.ClearValue(this);

                        if (WasLastMeasurePassAnchored)
                        {
                            SetAnchorInformation(Orientation == Orientation.Horizontal);
                        }

                        InvalidateMeasure();
                    }
                    else
                    {
                        // call InvalidateMeasure before calling SetAnchorInformation,
                        // so that the measure will happen before the OnAnchor callback
                        // (both are posted at Render priority)
                        InvalidateMeasure();

                        AnchoredInvalidateMeasureOperationField.ClearValue(this);

                        if (WasLastMeasurePassAnchored)
                        {
                            SetAnchorInformation(Orientation == Orientation.Horizontal);
                        }
                    }
                });

                AnchoredInvalidateMeasureOperationField.SetValue(this, anchoredInvalidateMeasureOperation);
            }
        }

        // cancel a pending callback to AnchoredInvalidateMeasure, if one exists.
        // This is called when the callback's work (InvalidateMeasure and SetAnchor)
        // is no longer needed because it's being done explicitly.  The pending
        // callback would arrive earlier than the new measure pass, and end up
        // adjusting the scroll offset twice.
        private void CancelPendingAnchoredInvalidateMeasure()
        {
            DispatcherOperation anchoredInvalidateMeasureOperation = AnchoredInvalidateMeasureOperationField.GetValue(this);
            if (anchoredInvalidateMeasureOperation != null)
            {
                anchoredInvalidateMeasureOperation.Abort();
                AnchoredInvalidateMeasureOperationField.ClearValue(this);
            }
        }

        /// <summary>
        /// VirtualizingStackPanel implementation of <seealso cref="IScrollInfo.MakeVisible" />.
        /// </summary>
        // The goal is to change offsets to bring the child into view, and return a rectangle in our space to make visible.
        // The rectangle we return is in the physical dimension the input target rect transformed into our pace.
        // In the logical dimension, it is our immediate child's rect.
        // Note: This code presently assumes we/children are layout clean.  See work item 22269 for more detail.
        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            ClearAnchorInformation(true /*shouldAbort*/);

            Vector newOffset = new Vector();
            Rect newRect = new Rect();
            Rect originalRect = rectangle;
            bool isHorizontal = (Orientation == Orientation.Horizontal);

            // We can only work on visuals that are us or children.
            // An empty rect has no size or position.  We can't meaningfully use it.
            if (    rectangle.IsEmpty
                || visual == null
                || visual == (Visual)this
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

            bool isVSP45Compat = IsVSP45Compat;
            bool alignTop = false;
            bool alignBottom = false;

            // Make ourselves visible in the non-stacking direction
            MakeVisiblePhysicalHelper(rectangle, ref newOffset, ref newRect, !isHorizontal, ref alignTop, ref alignBottom);

            alignTop = (_scrollData._bringIntoViewLeafContainer == visual && AlignTopOfBringIntoViewContainer);
            alignBottom = (_scrollData._bringIntoViewLeafContainer == visual &&
                (isVSP45Compat ? !AlignTopOfBringIntoViewContainer : AlignBottomOfBringIntoViewContainer));

            if (IsPixelBased)
            {
                MakeVisiblePhysicalHelper(rectangle, ref newOffset, ref newRect, isHorizontal, ref alignTop, ref alignBottom);
            }
            else
            {
                // Bring our child containing the visual into view.
                // For non-pixel based scrolling the offset is in logical units in the stacking direction
                // and physical units in the other. Hence the logical helper call here.
                int childIndex = (int)FindScrollOffset(visual);
                MakeVisibleLogicalHelper(childIndex, rectangle, ref newOffset, ref newRect, ref alignTop, ref alignBottom);
            }

            // We have computed the scrolling offsets; validate and scroll to them.
            newOffset.X = ScrollContentPresenter.CoerceOffset(newOffset.X, _scrollData._extent.Width, _scrollData._viewport.Width);
            newOffset.Y = ScrollContentPresenter.CoerceOffset(newOffset.Y, _scrollData._extent.Height, _scrollData._viewport.Height);

            if (!LayoutDoubleUtil.AreClose(newOffset.X, _scrollData._offset.X) ||
                !LayoutDoubleUtil.AreClose(newOffset.Y, _scrollData._offset.Y))
            {
                // We are about to make this container visible
                if (visual != _scrollData._bringIntoViewLeafContainer)
                {
                    _scrollData._bringIntoViewLeafContainer = visual;
                    AlignTopOfBringIntoViewContainer = alignTop;
                    AlignBottomOfBringIntoViewContainer = alignBottom;
                }

                Vector oldOffset = _scrollData._offset;
                _scrollData._offset = newOffset;

                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                {
                    ScrollTracer.Trace(this, ScrollTraceOp.MakeVisible,
                        _scrollData._offset,
                        rectangle,
                        _scrollData._bringIntoViewLeafContainer);
                }

                OnViewportOffsetChanged(oldOffset, newOffset);

                if (IsVirtualizing)
                {
                    IsScrollActive = true;
                    _scrollData.SetHorizontalScrollType(oldOffset.X, newOffset.X);
                    _scrollData.SetVerticalScrollType(oldOffset.Y, newOffset.Y);
                    InvalidateMeasure();
                }
                else if (!IsPixelBased)
                {
                    InvalidateMeasure();
                }
                else
                {
                    _scrollData._computedOffset = newOffset;
                    InvalidateArrange();
                }

                OnScrollChange();
                if (ScrollOwner != null)
                {
                    // When layout gets updated it may happen that visual is obscured by a ScrollBar
                    // We call MakeVisible again to make sure element is visible in this case
                    ScrollOwner.MakeVisible(visual, originalRect);
                }
            }
            else
            {
                // We have successfully made the container visible

                if (isVSP45Compat)
                {
                    _scrollData._bringIntoViewLeafContainer = null;
                }
                else
                {
                    // wait until IsScrollActive=false before clearing _bringIntoViewLeafContainer.
                    // This has the effect of keeping the scroll anchored, so that
                    // the target container isn't scrolled off the screen if the
                    // measure-cache pass (or any later measure pass) refines the estimates
                    // of container sizes, total extent, etc.
                }

                AlignTopOfBringIntoViewContainer = false;
                AlignBottomOfBringIntoViewContainer = false;
            }

            // Return the rectangle
            return newRect;
        }

        /// <summary>
        /// Generates the item at the specified index and calls BringIntoView on it.
        /// </summary>
        /// <param name="index">Specify the item index that should become visible. This is the index into ItemsControl.Items collection</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if index is out of range
        /// </exception>
        protected internal override void BringIndexIntoView(int index)
        {
            ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);

            // If panel is hosting a flat list of containers,
            // this panel can directly generate a container for it
            // and call bring the container into view.
            if( !itemsControl.IsGrouping )
            {
                BringContainerIntoView(itemsControl, index);
            }
            else
            {
                // When grouping the item could be any number of levels deep into hierarchy of CollectionViewGroups.
                EnsureGenerator();
                ItemContainerGenerator generator = (ItemContainerGenerator)Generator;
                IList items = generator.ItemsInternal;

                for (int i = 0; i < items.Count; i++)
                {
                    CollectionViewGroup cvg = items[i] as CollectionViewGroup;
                    if (cvg != null)
                    {
                        if (index >= cvg.ItemCount)
                        {
                            index -= cvg.ItemCount;
                        }
                        else
                        {
                            // Item was found somewhere within this CVG hierarchy
                            // Get the GroupItem hosting this CVG.
                            GroupItem groupItem = generator.ContainerFromItem(cvg) as GroupItem;
                            if (groupItem == null)
                            {
                                // Devirtualize container and try 2nd time.
                                BringContainerIntoView(itemsControl, i);
                                groupItem = generator.ContainerFromItem(cvg) as GroupItem;
                            }

                            if (groupItem != null)
                            {
                                // flush out layout queue so that ItemsHost gets hooked up.
                                // GroupItem also would inherit updated viewport data from parent VSP.
                                groupItem.UpdateLayout();

                                VirtualizingPanel itemsHost = groupItem.ItemsHost as VirtualizingPanel;
                                if (itemsHost != null)
                                {
                                    // Recursively call child panels until item is found.
                                    itemsHost.BringIndexIntoViewPublic(index);
                                }
                            }
                            break;
                        }
                    }
                    else if (i == index)
                    {
                        // This is the leaf level panel
                        // Compare
                        BringContainerIntoView(itemsControl, i);
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="itemIndex">index into the children of this panel</param>
        private void BringContainerIntoView(ItemsControl itemsControl, int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= ItemCount)
                throw new ArgumentOutOfRangeException("itemIndex");

            UIElement child;
            IItemContainerGenerator generator = Generator;
            int childIndex;
            bool visualOrderChanged = false;
            GeneratorPosition position = IndexToGeneratorPositionForStart(itemIndex, out childIndex);
            using (generator.StartAt(position, GeneratorDirection.Forward, true))
            {
                bool newlyRealized;
                child = generator.GenerateNext(out newlyRealized) as UIElement;
                if (child != null)
                {
                    visualOrderChanged = AddContainerFromGenerator(childIndex, child, newlyRealized, false /*isBeforeViewport*/);

                    if (visualOrderChanged)
                    {
                        Debug.Assert(IsVirtualizing && InRecyclingMode, "We should only modify the visual order when in recycling mode");
                        InvalidateZState();
                    }
                }
            }

            if (child != null)
            {
                FrameworkElement childFE = child as FrameworkElement;
                if (childFE != null)
                {
                    _bringIntoViewContainer = childFE;

                    Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)delegate()
                        {
                            //
                            // Carefully remove the _bringIntoViewContainer after the storm of layouts to bring it into view has subsided
                            //
                            _bringIntoViewContainer = null;
                        });

                    if (!itemsControl.IsGrouping && VirtualizingPanel.GetScrollUnit(itemsControl) == ScrollUnit.Item)
                    {
                        childFE.BringIntoView();
                    }
                    else if (!(childFE is GroupItem))
                    {
                        UpdateLayout();
                        childFE.BringIntoView();
                    }
                }
            }
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
        ///     Attached property for use on the ItemsControl that is the host for the items being
        ///     presented by this panel. Use this property to turn virtualization on/off.
        /// </summary>
        public new static readonly DependencyProperty IsVirtualizingProperty =
            VirtualizingPanel.IsVirtualizingProperty;

        /// <summary>
        ///     Attached property for use on the ItemsControl that is the host for the items being
        ///     presented by this panel. Use this property to modify the virtualization mode.
        ///
        ///     Note that this property can only be set before the panel has been initialized
        /// </summary>
        public new static readonly DependencyProperty VirtualizationModeProperty =
            VirtualizingPanel.VirtualizationModeProperty;

        /// <summary>
        /// Specifies dimension of children stacking.
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation) GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

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

        /// <summary>
        /// DependencyProperty for <see cref="Orientation" /> property.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(VirtualizingStackPanel),
                new FrameworkPropertyMetadata(Orientation.Vertical,
                        FrameworkPropertyMetadataOptions.AffectsMeasure,
                        new PropertyChangedCallback(OnOrientationChanged)),
                new ValidateValueCallback(ScrollBar.IsValidOrientation));

        //-----------------------------------------------------------
        //  IScrollInfo Properties
        //-----------------------------------------------------------
        #region IScrollInfo Properties

        /// <summary>
        /// VirtualizingStackPanel reacts to this property by changing its child measurement algorithm.
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
        /// VirtualizingStackPanel reacts to this property by changing its child measurement algorithm.
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
                if (_scrollData == null) return null;
                return _scrollData._scrollOwner;
            }
            set
            {
                if (_scrollData == null) EnsureScrollData();
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
        //  Public Events
        //
        //-------------------------------------------------------------------


        #region Public Events

        /// <summary>
        ///     Called on the ItemsControl that owns this panel when an item is being re-virtualized.
        /// </summary>
        public static readonly RoutedEvent CleanUpVirtualizedItemEvent = EventManager.RegisterRoutedEvent("CleanUpVirtualizedItemEvent", RoutingStrategy.Direct, typeof(CleanUpVirtualizedItemEventHandler), typeof(VirtualizingStackPanel));


        /// <summary>
        ///     Adds a handler for the CleanUpVirtualizedItem attached event
        /// </summary>
        /// <param name="element">DependencyObject that listens to this event</param>
        /// <param name="handler">Event Handler to be added</param>
        public static void AddCleanUpVirtualizedItemHandler(DependencyObject element, CleanUpVirtualizedItemEventHandler handler)
        {
            FrameworkElement.AddHandler(element, CleanUpVirtualizedItemEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the CleanUpVirtualizedItem attached event
        /// </summary>
        /// <param name="element">DependencyObject that listens to this event</param>
        /// <param name="handler">Event Handler to be removed</param>
        public static void RemoveCleanUpVirtualizedItemHandler(DependencyObject element, CleanUpVirtualizedItemEventHandler handler)
        {
            FrameworkElement.RemoveHandler(element, CleanUpVirtualizedItemEvent, handler);
        }

        /// <summary>
        ///     Called when an item is being re-virtualized.
        /// </summary>
        protected virtual void OnCleanUpVirtualizedItem(CleanUpVirtualizedItemEventArgs e)
        {
            ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);

            if (itemsControl != null)
            {
                itemsControl.RaiseEvent(e);
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        protected override bool CanHierarchicallyScrollAndVirtualizeCore
        {
            get { return true; }
        }

        /// <summary>
        /// General VirtualizingStackPanel layout behavior is to grow unbounded in the "stacking" direction (Size To Content).
        /// Children in this dimension are encouraged to be as large as they like.  In the other dimension,
        /// VirtualizingStackPanel will assume the maximum size of its children.
        /// </summary>
        /// <remarks>
        /// When scrolling, VirtualizingStackPanel will not grow in layout size but effectively add the children on a z-plane which
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
            List<double> previouslyMeasuredOffsets = null;
            double? lastPageSafeOffset = null;
            double? lastPagePixelSize = null;

            if (IsVSP45Compat)
            {
                return MeasureOverrideImpl(constraint,
                    ref lastPageSafeOffset,
                    ref previouslyMeasuredOffsets,
                    ref lastPagePixelSize,
                    remeasure:false);
            }
            else
            {
                // When scrolling to the last page, the information about previously
                // measured offsets needs to be preserved across calls to Measure
                // until the scroll is finished (IsScrollActive=false).  Otherwise
                // a second call to Measure (typically caused by a child reporting
                // a new size) would do a second search for an acceptable offset,
                // and de-virtualize a second set of children, one of which can
                // report a new size and cause a third Measure.   This can
                // continue indefinitely - a soft loop .

                // Recover the offset information from a previous Measure
                OffsetInformation info = OffsetInformationField.GetValue(this);
                if (info != null)
                {
                    previouslyMeasuredOffsets = info.previouslyMeasuredOffsets;
                    lastPageSafeOffset = info.lastPageSafeOffset;
                    lastPagePixelSize = info.lastPagePixelSize;
                }

                // Measure
                Size result = MeasureOverrideImpl(constraint,
                    ref lastPageSafeOffset,
                    ref previouslyMeasuredOffsets,
                    ref lastPagePixelSize,
                    remeasure:false);

                // Save the offset information for the next Measure.  It's cleared
                // when IsScrollActive is set to false (asynchronously).
                if (IsScrollActive)
                {
                    info = new OffsetInformation();
                    info.previouslyMeasuredOffsets = previouslyMeasuredOffsets;
                    info.lastPageSafeOffset = lastPageSafeOffset;
                    info.lastPagePixelSize = lastPagePixelSize;
                    OffsetInformationField.SetValue(this, info);
                }

                return result;
            }
        }

        private Size MeasureOverrideImpl(Size constraint,
            ref double? lastPageSafeOffset,
            ref List<double> previouslyMeasuredOffsets,
            ref double? lastPagePixelSize,
            bool remeasure)
        {
            bool etwTracingEnabled = IsScrolling && EventTrace.IsEnabled(EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info);
            if (etwTracingEnabled)
            {
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringBegin, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, "VirtualizingStackPanel :MeasureOverride");
            }

            //
            //  Initialize the sizes to be computed in this routine.
            //
            Size stackPixelSize = new Size();
            Size stackLogicalSize = new Size();
            Size stackPixelSizeInViewport = new Size();
            Size stackLogicalSizeInViewport = new Size();
            Size stackPixelSizeInCacheBeforeViewport = new Size();
            Size stackLogicalSizeInCacheBeforeViewport = new Size();
            Size stackPixelSizeInCacheAfterViewport = new Size();
            Size stackLogicalSizeInCacheAfterViewport = new Size();

            bool hasVirtualizingChildren = false;
            ItemsChangedDuringMeasure = false;

            try
            {
                if (!IsItemsHost)
                {
                    stackPixelSize = MeasureNonItemsHost(constraint);
                }
                else
                {
                    bool isVSP45Compat = IsVSP45Compat;

                    // ===================================================================================
                    // ===================================================================================
                    // Fetch owners
                    // ===================================================================================
                    // ===================================================================================

                    ItemsControl itemsControl = null;
                    GroupItem groupItem = null;

                    //
                    // This is an interface implemented by the owner of this panel in order to facilitate between a parent
                    // panel and this one when virtualizing in a hierarchy. (Eg. TreeView or grouping ItemsControl.) This
                    // interface is currently implemented by TreeViewItem and GroupItem.
                    //
                    IHierarchicalVirtualizationAndScrollInfo virtualizationInfoProvider = null;

                    //
                    // This is a service provided by the owner of this panel to store and retrieve information on a per item
                    // basis. Specifically this panel uses this service to remember DesiredSize of items when virtualizing.
                    // This interface is currently implemented by ItemsControl and GroupItem.
                    //
                    IContainItemStorage itemStorageProvider = null;

                    //
                    // This is the item representing the owner for this panel. (Eg. The CollectionViewGroup for the owner GroupItem)
                    //
                    object parentItem = null;
                    IContainItemStorage parentItemStorageProvider;

                    //
                    // Is horizontally stacking
                    //
                    bool isHorizontal = (Orientation == Orientation.Horizontal);

                    //
                    // Compute if this panel is different in orientation that either its parent or descendents
                    //
                    bool mustDisableVirtualization = false;

                    //
                    // Fetch the owner for this panel. That could either be an ItemsControl or a GroupItem.
                    //
                    GetOwners(true /*shouldSetVirtualizationState*/, isHorizontal, out itemsControl, out groupItem, out itemStorageProvider, out virtualizationInfoProvider, out parentItem, out parentItemStorageProvider, out mustDisableVirtualization);

                    // ===================================================================================
                    // ===================================================================================
                    // Initialize viewport
                    // ===================================================================================
                    // ===================================================================================

                    //
                    // The viewport constraint used by this panel.
                    //
                    Rect viewport = Rect.Empty, extendedViewport = Rect.Empty;
                    long scrollGeneration;

                    //
                    // Sizes of cache before/after viewport
                    //
                    VirtualizationCacheLength cacheSize = new VirtualizationCacheLength(0.0);
                    VirtualizationCacheLengthUnit cacheUnit = VirtualizationCacheLengthUnit.Pixel;

                    //
                    // Initialize the viewport for this panel.
                    //
                    InitializeViewport(parentItem, parentItemStorageProvider, virtualizationInfoProvider, isHorizontal, constraint, ref viewport, ref cacheSize, ref cacheUnit, out extendedViewport, out scrollGeneration);

                    // ===================================================================================
                    // ===================================================================================
                    // Compute first item in viewport
                    // ===================================================================================
                    // ===================================================================================

                    //
                    // Index of first item in the viewport.
                    //
                    int firstItemInViewportIndex = Int32.MinValue, lastItemInViewportIndex = Int32.MaxValue, firstItemInViewportChildIndex = Int32.MinValue, firstItemInExtendedViewportIndex = Int32.MinValue;
                    UIElement firstContainerInViewport = null;

                    //
                    // Offset and span of the first item that extends beyond the top of the viewport.
                    //
                    double firstItemInViewportOffset = 0.0, firstItemInExtendedViewportOffset = 0.0;
                    double firstItemInViewportContainerSpan, firstItemInExtendedViewportContainerSpan;

                    //
                    // Says if the first and last items in the viewport has been encountered this far
                    //
                    bool foundFirstItemInViewport = false, foundLastItemInViewport = false, foundFirstItemInExtendedViewport = false;

                    //
                    // Get set to enumerate the items of the owner
                    //
                    EnsureGenerator();
                    IList children = RealizedChildren;  // yes, this is weird, but this property ensures the Generator is properly initialized.
                    IItemContainerGenerator generator = Generator;
                    IList items = ((ItemContainerGenerator)generator).ItemsInternal;
                    int itemCount = items.Count;

                    //
                    // Locally cache the values of this flag and size until the end of this measure pass.
                    // This is important because switching the areContainersUniformlySized flag in the
                    // middle of a measure leads to skewed results in the computation of the extensions
                    // to the stackSize.
                    //
                    IContainItemStorage uniformSizeItemStorageProvider = isVSP45Compat ? itemStorageProvider : parentItemStorageProvider;
                    bool areContainersUniformlySized = GetAreContainersUniformlySized(uniformSizeItemStorageProvider, parentItem);
                    bool computedAreContainersUniformlySized = areContainersUniformlySized;
                    bool hasUniformOrAverageContainerSizeBeenSet;
                    double uniformOrAverageContainerSize, uniformOrAverageContainerPixelSize;
                    GetUniformOrAverageContainerSize(uniformSizeItemStorageProvider, parentItem,
                        IsPixelBased || isVSP45Compat,
                        out uniformOrAverageContainerSize,
                        out uniformOrAverageContainerPixelSize,
                        out hasUniformOrAverageContainerSizeBeenSet);
                    double computedUniformOrAverageContainerSize = uniformOrAverageContainerSize;
                    double computedUniformOrAverageContainerPixelSize = uniformOrAverageContainerPixelSize;

                    if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                    {
                        ScrollTracer.Trace(this, ScrollTraceOp.BeginMeasure,
                            constraint,
                            "MC:", MeasureCaches,
                            "reM:", remeasure,
                            "acs:", uniformOrAverageContainerSize, areContainersUniformlySized, hasUniformOrAverageContainerSizeBeenSet);
                    }

                    //
                    // Compute index and offset of first item in the viewport
                    //
                    ComputeFirstItemInViewportIndexAndOffset(items, itemCount, itemStorageProvider, viewport, cacheSize,
                        isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize,
                        out firstItemInViewportOffset,
                        out firstItemInViewportContainerSpan,
                        out firstItemInViewportIndex,
                        out foundFirstItemInViewport);

                    //
                    // Compute index and offset of first item in the extendedViewport
                    //
                    ComputeFirstItemInViewportIndexAndOffset(items, itemCount, itemStorageProvider, extendedViewport, new VirtualizationCacheLength(0.0),
                        isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize,
                        out firstItemInExtendedViewportOffset,
                        out firstItemInExtendedViewportContainerSpan,
                        out firstItemInExtendedViewportIndex,
                        out foundFirstItemInExtendedViewport);

                    if (IsVirtualizing)
                    {
                        // ===================================================================================
                        // ===================================================================================
                        // Recycle containers
                        // ===================================================================================
                        // ===================================================================================

                        //
                        // If we arrive here through a remeasure this assertion wont be true
                        // because we have postponed the cleanup until after this remeasure
                        // is complete.
                        //
                        // debug_AssertRealizedChildrenEqualVisualChildren();

                        //
                        // If recycling clean up before generating children so that recycled
                        // containers are available for the current measure pass.
                        // But if this is a remeasure pass we do not want to recycle the
                        // containers because there could be several iterations of these and
                        // we should wait for the storm to subside to reclaim containers.
                        //
                        if (!remeasure && InRecyclingMode)
                        {
                            int excludeCount = _itemsInExtendedViewportCount;

                            if (!isVSP45Compat)
                            {
                                // in some cases (e.g. PageUp from a region with large
                                // items to a region with small items), the range
                                // excluded from recycling doesn't span containers
                                // that will belong to the normal viewport.  It's
                                // wasteful to re-virtualize those items, only to
                                // de-virtualize them right away;  it's even worse
                                // when the containers take time to converge on their
                                // final size (e.g. when content arrives via bindings
                                // that don't activate until after layout), as it
                                // causes a lot of extra measure passes, where "a lot"
                                // can be as bad as "infinite".
                                //
                                // To avoid this waste, exclude a proportional part
                                // of the normal viewport as well.
                                double factor = Math.Min(1.0, isHorizontal ? viewport.Width / extendedViewport.Width : viewport.Height / extendedViewport.Height);
                                int calcItemsInViewportCount = (int)Math.Ceiling(factor * _itemsInExtendedViewportCount);
                                excludeCount = Math.Max(excludeCount,
                                                firstItemInViewportIndex + calcItemsInViewportCount - firstItemInExtendedViewportIndex);
                            }

                            CleanupContainers(firstItemInExtendedViewportIndex, excludeCount, itemsControl);
                            debug_VerifyRealizedChildren();
                        }
                    }

                    // ===================================================================================
                    // ===================================================================================
                    // Initialize child constraint
                    // ===================================================================================
                    // ===================================================================================

                    //
                    // Initialize child constraint. Allow children as much size as they want along the stack.
                    //
                    Size childConstraint = constraint;
                    if (isHorizontal)
                    {
                        childConstraint.Width = Double.PositiveInfinity;
                        if (IsScrolling && CanVerticallyScroll) { childConstraint.Height = Double.PositiveInfinity; }
                    }
                    else
                    {
                        childConstraint.Height = Double.PositiveInfinity;
                        if (IsScrolling && CanHorizontallyScroll) { childConstraint.Width = Double.PositiveInfinity; }
                    }

                    remeasure = false;
                    _actualItemsInExtendedViewportCount = 0;
                    _firstItemInExtendedViewportIndex = 0;
                    _firstItemInExtendedViewportOffset = 0.0;
                    _firstItemInExtendedViewportChildIndex = 0;

                    bool visualOrderChanged = false;
                    int childIndex = 0;
                    GeneratorPosition startPos;
                    bool hasBringIntoViewContainerBeenMeasured = false;
                    bool hasAverageContainerSizeChanged = false;
                    bool hasAnyContainerSpanChanged = false;

                    if (itemCount > 0)
                    {
                        // We will generate containers in several batches - one for
                        // the first visible item and the before-cache, another for
                        // the remaining visible items and the after-cache, and possibly
                        // more if the first attempts don't work out.   We want to keep
                        // the generator's status at "GeneratingContainers" throughout the
                        // entire process.  GenerateBatches does exactly what we want.
                        using (((ItemContainerGenerator)generator).GenerateBatches())
                        {
                            // ===================================================================================
                            // ===================================================================================
                            // Generate and measure children cached before the viewport.
                            // ===================================================================================
                            // ===================================================================================

                            if (!foundFirstItemInViewport ||
                                !IsEndOfCache(isHorizontal, cacheSize.CacheBeforeViewport, cacheUnit, stackPixelSizeInCacheBeforeViewport, stackLogicalSizeInCacheBeforeViewport) ||
                                !IsEndOfViewport(isHorizontal, viewport, stackPixelSizeInViewport))
                            {
                                bool adjustToChangeInFirstItem = false;

                                do
                                {
                                    Debug.Assert(!adjustToChangeInFirstItem || foundFirstItemInViewport, "This loop should only happen twice at most");

                                    adjustToChangeInFirstItem = false;

                                    //
                                    // Figure out the generator position
                                    //
                                    int startIndex;
                                    bool isAlwaysBeforeFirstItem = false;
                                    bool isAlwaysAfterFirstItem = false;
                                    bool isAlwaysAfterLastItem = false;
                                    if (IsViewportEmpty(isHorizontal, viewport) && DoubleUtil.GreaterThan(cacheSize.CacheBeforeViewport, 0.0))
                                    {
                                        isAlwaysBeforeFirstItem = true;
                                    }

                                    startIndex = firstItemInViewportIndex;
                                    startPos = IndexToGeneratorPositionForStart(firstItemInViewportIndex, out childIndex);
                                    firstItemInViewportChildIndex = childIndex;
                                    _firstItemInExtendedViewportIndex = firstItemInViewportIndex;
                                    _firstItemInExtendedViewportOffset = firstItemInViewportOffset;
                                    _firstItemInExtendedViewportChildIndex = childIndex;

                                    using (generator.StartAt(startPos, GeneratorDirection.Backward, true))
                                    {
                                        for (int i = startIndex; i >= 0; i--)
                                        {
                                            object item = items[i];

                                            MeasureChild(
                                                ref generator,
                                                ref itemStorageProvider,
                                                ref parentItemStorageProvider,
                                                ref parentItem,
                                                ref hasUniformOrAverageContainerSizeBeenSet,
                                                ref computedUniformOrAverageContainerSize,
                                                ref computedUniformOrAverageContainerPixelSize,
                                                ref computedAreContainersUniformlySized,
                                                ref hasAnyContainerSpanChanged,
                                                ref items,
                                                ref item,
                                                ref children,
                                                ref _firstItemInExtendedViewportChildIndex,
                                                ref visualOrderChanged,
                                                ref isHorizontal,
                                                ref childConstraint,
                                                ref viewport,
                                                ref cacheSize,
                                                ref cacheUnit,
                                                ref scrollGeneration,
                                                ref foundFirstItemInViewport,
                                                ref firstItemInViewportOffset,
                                                ref stackPixelSize,
                                                ref stackPixelSizeInViewport,
                                                ref stackPixelSizeInCacheBeforeViewport,
                                                ref stackPixelSizeInCacheAfterViewport,
                                                ref stackLogicalSize,
                                                ref stackLogicalSizeInViewport,
                                                ref stackLogicalSizeInCacheBeforeViewport,
                                                ref stackLogicalSizeInCacheAfterViewport,
                                                ref mustDisableVirtualization,
                                                (i < firstItemInViewportIndex) || isAlwaysBeforeFirstItem,
                                                isAlwaysAfterFirstItem,
                                                isAlwaysAfterLastItem,
                                                false /*skipActualMeasure*/,
                                                false /*skipGeneration*/,
                                                ref hasBringIntoViewContainerBeenMeasured,
                                                ref hasVirtualizingChildren);

                                            if (ItemsChangedDuringMeasure)
                                            {
                                                // if the Items collection changed, our state is now invalid.  Start over.
                                                remeasure = true;
                                                goto EscapeMeasure;
                                            }

                                            _actualItemsInExtendedViewportCount++;

                                            if (!foundFirstItemInViewport)
                                            {
                                                //
                                                // Re-compute index and offset of first item in the viewport
                                                //
                                                if (isVSP45Compat)
                                                {
                                                    SyncUniformSizeFlags(parentItem,
                                                        parentItemStorageProvider,
                                                        children,
                                                        items,
                                                        itemStorageProvider,
                                                        itemCount,
                                                        computedAreContainersUniformlySized,
                                                        computedUniformOrAverageContainerSize,
                                                        ref areContainersUniformlySized,
                                                        ref uniformOrAverageContainerSize,
                                                        ref hasAverageContainerSizeChanged,
                                                        isHorizontal,
                                                        false /* evaluateAreContainersUniformlySized */);
                                                }
                                                else
                                                {
                                                    SyncUniformSizeFlags(parentItem,
                                                        parentItemStorageProvider,
                                                        children,
                                                        items,
                                                        itemStorageProvider,
                                                        itemCount,
                                                        computedAreContainersUniformlySized,
                                                        computedUniformOrAverageContainerSize,
                                                        computedUniformOrAverageContainerPixelSize,
                                                        ref areContainersUniformlySized,
                                                        ref uniformOrAverageContainerSize,
                                                        ref uniformOrAverageContainerPixelSize,
                                                        ref hasAverageContainerSizeChanged,
                                                        isHorizontal,
                                                        false /* evaluateAreContainersUniformlySized */);
                                                }

                                                ComputeFirstItemInViewportIndexAndOffset(items, itemCount, itemStorageProvider, viewport, cacheSize,
                                                    isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize,
                                                    out firstItemInViewportOffset,
                                                    out firstItemInViewportContainerSpan,
                                                    out firstItemInViewportIndex,
                                                    out foundFirstItemInViewport);

                                                if (foundFirstItemInViewport)
                                                {
                                                    if (i == firstItemInViewportIndex)
                                                    {
                                                        MeasureChild(
                                                            ref generator,
                                                            ref itemStorageProvider,
                                                            ref parentItemStorageProvider,
                                                            ref parentItem,
                                                            ref hasUniformOrAverageContainerSizeBeenSet,
                                                            ref computedUniformOrAverageContainerSize,
                                                            ref computedUniformOrAverageContainerPixelSize,
                                                            ref computedAreContainersUniformlySized,
                                                            ref hasAnyContainerSpanChanged,
                                                            ref items,
                                                            ref item,
                                                            ref children,
                                                            ref _firstItemInExtendedViewportChildIndex,
                                                            ref visualOrderChanged,
                                                            ref isHorizontal,
                                                            ref childConstraint,
                                                            ref viewport,
                                                            ref cacheSize,
                                                            ref cacheUnit,
                                                            ref scrollGeneration,
                                                            ref foundFirstItemInViewport,
                                                            ref firstItemInViewportOffset,
                                                            ref stackPixelSize,
                                                            ref stackPixelSizeInViewport,
                                                            ref stackPixelSizeInCacheBeforeViewport,
                                                            ref stackPixelSizeInCacheAfterViewport,
                                                            ref stackLogicalSize,
                                                            ref stackLogicalSizeInViewport,
                                                            ref stackLogicalSizeInCacheBeforeViewport,
                                                            ref stackLogicalSizeInCacheAfterViewport,
                                                            ref mustDisableVirtualization,
                                                            false /*isBeforeFirstItem*/,
                                                            false /*isAfterFirstItem*/,
                                                            false /*isAfterLastItem*/,
                                                            true /*skipActualMeasure*/,
                                                            true /*skipGeneration*/,
                                                            ref hasBringIntoViewContainerBeenMeasured,
                                                            ref hasVirtualizingChildren);

                                                        if (ItemsChangedDuringMeasure)
                                                        {
                                                            // if the Items collection changed, our state is now invalid.  Start over.
                                                            remeasure = true;
                                                            goto EscapeMeasure;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        stackPixelSize = new Size();
                                                        stackLogicalSize = new Size();
                                                        _actualItemsInExtendedViewportCount--;
                                                        adjustToChangeInFirstItem = true;
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }

                                            // remember the first container in the viewport - we'll need it later
                                            // to set the scrolling information.
                                            // We have to do this as soon as the container is generated,
                                            // while firstItemInViewportChildIndex is still valid.
                                            // (When the before-cache size increases, the enclosing for-loop
                                            // may insert containers at the beginning of the children
                                            // collection, which invalidates firstItemInViewportChildIndex.)
                                            if (!isVSP45Compat &&
                                                (firstContainerInViewport == null) &&
                                                foundFirstItemInViewport &&
                                                (i == startIndex))
                                            {
                                                if (0 <= firstItemInViewportChildIndex &&
                                                    firstItemInViewportChildIndex < children.Count)
                                                {
                                                    firstContainerInViewport = children[firstItemInViewportChildIndex] as UIElement;

                                                    // avoid problems with size changes during an anchored scroll
                                                    if (IsScrolling && _scrollData._firstContainerInViewport != null && !areContainersUniformlySized)
                                                    {
                                                        // when the firstItemInViewport was found, it was true that either
                                                        //   a. the viewport is at the beginning, or
                                                        //   b. firstItemInViewportOffset + containerSize(item) > viewport.origin
                                                        // In case (b), this may no longer be true if the containerSize decreased
                                                        // during MeasureChild - because its size changed while the
                                                        // item was virtualized.  During an anchored scroll this can
                                                        // be a problem:  all the children can get arranged before
                                                        // the viewport, and OnAnchor crashes
                                                        // So if the inequality is no longer true, remeasure and try again.
                                                        Size newContainerSize;
                                                        GetContainerSizeForItem(itemStorageProvider,
                                                            item,
                                                            isHorizontal,
                                                            areContainersUniformlySized, uniformOrAverageContainerSize,
                                                            out newContainerSize);

                                                        double spanBeforeViewport = Math.Max(isHorizontal ? viewport.X : viewport.Y, 0.0);
                                                        double newContainerSpan = isHorizontal ? newContainerSize.Width : newContainerSize.Height;
                                                        bool endsAfterViewport = DoubleUtil.AreClose(spanBeforeViewport, 0) ||
                                                            LayoutDoubleUtil.LessThan(spanBeforeViewport, firstItemInViewportOffset + newContainerSpan);

                                                        if (!endsAfterViewport)
                                                        {
                                                            // adjust the offset by the same amount that the container size changed,
                                                            // to get an equivalent measure using the new size
                                                            double delta = newContainerSpan - firstItemInViewportContainerSpan;

                                                            // A previous change exposed a case where this logic requested a remeasure
                                                            // but didn't actually change the offset, leading to infinite recursion
                                                            // (stack overflow).  That can only happen when the container sizes are
                                                            // uniform (among other conditions).  We don't need this logic at all
                                                            // in that case - the size change we're worried about puts us in
                                                            // non-uniform mode, ipso facto - so we skip it if areContainersUniformlySized=true.
                                                            // But just in case some other case can get here with a no-op remeasure
                                                            // request, check for that now.
                                                            if (!LayoutDoubleUtil.AreClose(delta, 0.0))
                                                            {
                                                                if (isHorizontal)
                                                                {
                                                                    _scrollData._offset.X += delta;
                                                                }
                                                                else
                                                                {
                                                                    _scrollData._offset.Y += delta;
                                                                }

                                                                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                                                                {
                                                                    ScrollTracer.Trace(this, ScrollTraceOp.SizeChangeDuringAnchorScroll,
                                                                        "fivOffset:", firstItemInViewportOffset,
                                                                        "vpSpan:", spanBeforeViewport,
                                                                        "oldCSpan:", firstItemInViewportContainerSpan,
                                                                        "newCSpan:", newContainerSpan,
                                                                        "delta:", delta,
                                                                        "newVpOff:", _scrollData._offset);
                                                                }

                                                                remeasure = true;
                                                                goto EscapeMeasure;
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            //
                                            // If this is the end of the cache before the viewport break out of the loop.
                                            //
                                            if (IsEndOfCache(isHorizontal, cacheSize.CacheBeforeViewport, cacheUnit, stackPixelSizeInCacheBeforeViewport, stackLogicalSizeInCacheBeforeViewport))
                                            {
                                                break;
                                            }

                                            _firstItemInExtendedViewportIndex = Math.Max(_firstItemInExtendedViewportIndex - 1, 0);
                                            IndexToGeneratorPositionForStart(_firstItemInExtendedViewportIndex, out _firstItemInExtendedViewportChildIndex);
                                            _firstItemInExtendedViewportChildIndex = Math.Max(_firstItemInExtendedViewportChildIndex, 0);
                                        }
                                    }
                                }
                                while (adjustToChangeInFirstItem);

                                ComputeDistance(items, itemStorageProvider, isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize, 0, _firstItemInExtendedViewportIndex, out _firstItemInExtendedViewportOffset);
                            }

                            if (foundFirstItemInViewport &&
                                (!IsEndOfCache(isHorizontal, cacheSize.CacheAfterViewport, cacheUnit, stackPixelSizeInCacheAfterViewport, stackLogicalSizeInCacheAfterViewport) ||
                                 !IsEndOfViewport(isHorizontal, viewport, stackPixelSizeInViewport)))
                            {
                                //
                                // Figure out the generator position
                                //
                                int startIndex;
                                bool isAlwaysBeforeFirstItem = false;
                                bool isAlwaysAfterFirstItem = false;
                                bool isAlwaysAfterLastItem = false;

                                if (IsViewportEmpty(isHorizontal, viewport))
                                {
                                    startIndex = 0;
                                    isAlwaysAfterFirstItem = true;
                                    isAlwaysAfterLastItem = true;
                                }
                                else
                                {
                                    startIndex = firstItemInViewportIndex + 1;
                                    isAlwaysAfterFirstItem = true;
                                }

                                startPos = IndexToGeneratorPositionForStart(startIndex, out childIndex);

                                // ===================================================================================
                                // ===================================================================================
                                // Generate and measure children in the viewport and cached after the viewport.
                                // ===================================================================================
                                // ===================================================================================
                                using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
                                {
                                    for (int i = startIndex; i < itemCount; i++, childIndex++)
                                    {
                                        object item = items[i];

                                        MeasureChild(
                                            ref generator,
                                            ref itemStorageProvider,
                                            ref parentItemStorageProvider,
                                            ref parentItem,
                                            ref hasUniformOrAverageContainerSizeBeenSet,
                                            ref computedUniformOrAverageContainerSize,
                                            ref computedUniformOrAverageContainerPixelSize,
                                            ref computedAreContainersUniformlySized,
                                            ref hasAnyContainerSpanChanged,
                                            ref items,
                                            ref item,
                                            ref children,
                                            ref childIndex,
                                            ref visualOrderChanged,
                                            ref isHorizontal,
                                            ref childConstraint,
                                            ref viewport,
                                            ref cacheSize,
                                            ref cacheUnit,
                                            ref scrollGeneration,
                                            ref foundFirstItemInViewport,
                                            ref firstItemInViewportOffset,
                                            ref stackPixelSize,
                                            ref stackPixelSizeInViewport,
                                            ref stackPixelSizeInCacheBeforeViewport,
                                            ref stackPixelSizeInCacheAfterViewport,
                                            ref stackLogicalSize,
                                            ref stackLogicalSizeInViewport,
                                            ref stackLogicalSizeInCacheBeforeViewport,
                                            ref stackLogicalSizeInCacheAfterViewport,
                                            ref mustDisableVirtualization,
                                            isAlwaysBeforeFirstItem,
                                            (i > firstItemInViewportIndex) || isAlwaysAfterFirstItem,
                                            (i > lastItemInViewportIndex) || isAlwaysAfterLastItem,
                                            false /*skipActualMeasure*/,
                                            false /*skipGeneration*/,
                                            ref hasBringIntoViewContainerBeenMeasured,
                                            ref hasVirtualizingChildren);

                                            if (ItemsChangedDuringMeasure)
                                            {
                                                // if the Items collection changed, our state is now invalid.  Start over.
                                                remeasure = true;
                                                goto EscapeMeasure;
                                            }

                                        _actualItemsInExtendedViewportCount++;

                                        if (IsEndOfViewport(isHorizontal, viewport, stackPixelSizeInViewport))
                                        {
                                            //
                                            // If this is the last item in the original viewport make a record of it.
                                            //
                                            if (!foundLastItemInViewport)
                                            {
                                                foundLastItemInViewport = true;
                                                lastItemInViewportIndex = i;
                                            }

                                            //
                                            // If this is the end of the cache after the viewport break out of the loop.
                                            //
                                            if (IsEndOfCache(isHorizontal, cacheSize.CacheAfterViewport, cacheUnit, stackPixelSizeInCacheAfterViewport, stackLogicalSizeInCacheAfterViewport))
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (IsVirtualizing &&
                        !IsPixelBased &&
                        (hasVirtualizingChildren || virtualizationInfoProvider != null) &&
                        (MeasureCaches || (DoubleUtil.AreClose(cacheSize.CacheBeforeViewport, 0) && DoubleUtil.AreClose(cacheSize.CacheAfterViewport, 0))))
                    {
                        //
                        // All of the descendent panels in hierarchical item scrolling scenarios that are after the extended
                        // viewport need to be measured so that they do not arrange any of their children above their own
                        // bounds and hence show through in the viewport.
                        //
                        int startIndex = _firstItemInExtendedViewportChildIndex+_actualItemsInExtendedViewportCount;
                        int childrenCount = children.Count;
                        for (int i=startIndex; i<childrenCount; i++)
                        {
                            MeasureExistingChildBeyondExtendedViewport(
                                ref generator,
                                ref itemStorageProvider,
                                ref parentItemStorageProvider,
                                ref parentItem,
                                ref hasUniformOrAverageContainerSizeBeenSet,
                                ref computedUniformOrAverageContainerSize,
                                ref computedUniformOrAverageContainerPixelSize,
                                ref computedAreContainersUniformlySized,
                                ref hasAnyContainerSpanChanged,
                                ref items,
                                ref children,
                                ref i,
                                ref visualOrderChanged,
                                ref isHorizontal,
                                ref childConstraint,
                                ref foundFirstItemInViewport,
                                ref firstItemInViewportOffset,
                                ref mustDisableVirtualization,
                                ref hasVirtualizingChildren,
                                ref hasBringIntoViewContainerBeenMeasured,
                                ref scrollGeneration);

                            if (ItemsChangedDuringMeasure)
                                {
                                    // if the Items collection changed, our state is now invalid.  Start over.
                                    remeasure = true;
                                    goto EscapeMeasure;
                                }
                        }
                    }

                    if (_bringIntoViewContainer != null && !hasBringIntoViewContainerBeenMeasured)
                    {
                        //
                        // Measure the container meant to be brought into view in preparation for the next MakeVisible operation
                        //
                        childIndex = children.IndexOf(_bringIntoViewContainer);
                        if (childIndex < 0)
                        {
                            //
                            // If there were a collection changed between the BringIndexIntoView call
                            // and the current Measure then it is possible that the item for the
                            // _bringIntoViewContainer has been removed from the collection and so
                            // has the container. We need to guard against this scenario.
                            _bringIntoViewContainer = null;
                        }
                        else
                        {
                            MeasureExistingChildBeyondExtendedViewport(
                                ref generator,
                                ref itemStorageProvider,
                                ref parentItemStorageProvider,
                                ref parentItem,
                                ref hasUniformOrAverageContainerSizeBeenSet,
                                ref computedUniformOrAverageContainerSize,
                                ref computedUniformOrAverageContainerPixelSize,
                                ref computedAreContainersUniformlySized,
                                ref hasAnyContainerSpanChanged,
                                ref items,
                                ref children,
                                ref childIndex,
                                ref visualOrderChanged,
                                ref isHorizontal,
                                ref childConstraint,
                                ref foundFirstItemInViewport,
                                ref firstItemInViewportOffset,
                                ref mustDisableVirtualization,
                                ref hasVirtualizingChildren,
                                ref hasBringIntoViewContainerBeenMeasured,
                                ref scrollGeneration);

                            if (ItemsChangedDuringMeasure)
                                {
                                    // if the Items collection changed, our state is now invalid.  Start over.
                                    remeasure = true;
                                    goto EscapeMeasure;
                                }
                        }
                    }

                    if (isVSP45Compat)
                    {
                        SyncUniformSizeFlags(parentItem,
                            parentItemStorageProvider,
                            children,
                            items,
                            itemStorageProvider,
                            itemCount,
                            computedAreContainersUniformlySized,
                            computedUniformOrAverageContainerSize,
                            ref areContainersUniformlySized,
                            ref uniformOrAverageContainerSize,
                            ref hasAverageContainerSizeChanged,
                            isHorizontal,
                            false /* evaluateAreContainersUniformlySized */);
                    }
                    else
                    {
                        SyncUniformSizeFlags(parentItem,
                            parentItemStorageProvider,
                            children,
                            items,
                            itemStorageProvider,
                            itemCount,
                            computedAreContainersUniformlySized,
                            computedUniformOrAverageContainerSize,
                            computedUniformOrAverageContainerPixelSize,
                            ref areContainersUniformlySized,
                            ref uniformOrAverageContainerSize,
                            ref uniformOrAverageContainerPixelSize,
                            ref hasAverageContainerSizeChanged,
                            isHorizontal,
                            false /* evaluateAreContainersUniformlySized */);
                    }

                    if (IsVirtualizing)
                    {
#if DEBUG
                        if (InRecyclingMode)
                        {
                            debug_VerifyRealizedChildren();
                        }
#endif

                        // ===================================================================================
                        // ===================================================================================
                        // Acount for the size of items before and after the viewport that won't be generated
                        // ===================================================================================
                        // ===================================================================================
                        ExtendPixelAndLogicalSizes(
                            children,
                            items,
                            itemCount,
                            itemStorageProvider,
                            areContainersUniformlySized,
                            uniformOrAverageContainerSize,
                            uniformOrAverageContainerPixelSize,
                            ref stackPixelSize,
                            ref stackLogicalSize,
                            isHorizontal,
                            _firstItemInExtendedViewportIndex,
                            _firstItemInExtendedViewportChildIndex,
                            firstItemInViewportIndex,
                            true /*before */);

                        ExtendPixelAndLogicalSizes(
                            children,
                            items,
                            itemCount,
                            itemStorageProvider,
                            areContainersUniformlySized,
                            uniformOrAverageContainerSize,
                            uniformOrAverageContainerPixelSize,
                            ref stackPixelSize,
                            ref stackLogicalSize,
                            isHorizontal,
                            _firstItemInExtendedViewportIndex + _actualItemsInExtendedViewportCount,
                            _firstItemInExtendedViewportChildIndex + _actualItemsInExtendedViewportCount,
                            -1,                     // firstItemInViewportIndex - ignored in 'after' call
                            false /*before */);
                    }

                    // ===================================================================================
                    // ===================================================================================
                    // Sync members that may be required during Arrange or in later Measure passes
                    // ===================================================================================
                    // ===================================================================================
                    _previousStackPixelSizeInViewport = stackPixelSizeInViewport;
                    _previousStackLogicalSizeInViewport = stackLogicalSizeInViewport;
                    _previousStackPixelSizeInCacheBeforeViewport = stackPixelSizeInCacheBeforeViewport;

                    // For item-scrolling, we need the pixel distance to the viewport, in
                    // order to arrange children correctly.  This is the sum of three terms:
                    //      distance to the first container (computed in ExtendPixelAndLogicalSizes)
                    //   +  front inset from container to its items host
                    //   +  items host's distance to the viewport
                    // The latter two terms are only needed when the viewport starts
                    // after the first container.
                    if (!IsPixelBased &&
                        DoubleUtil.GreaterThan((isHorizontal ? viewport.Left : viewport.Top), firstItemInViewportOffset))
                    {
                        IHierarchicalVirtualizationAndScrollInfo firstContainer = GetVirtualizingChild(firstContainerInViewport);
                        if (firstContainer != null)
                        {
                            Thickness inset = GetItemsHostInsetForChild(firstContainer);
                            _pixelDistanceToViewport += (isHorizontal ? inset.Left : inset.Top);

                            VirtualizingStackPanel childPanel = firstContainer.ItemsHost as VirtualizingStackPanel;
                            if (childPanel != null)
                            {
                                _pixelDistanceToViewport += childPanel._pixelDistanceToViewport;
                            }
                        }
                    }

                    // Coerce infinite viewport dimensions to stackPixelSize
                    if (double.IsInfinity(viewport.Width))
                    {
                        viewport.Width = stackPixelSize.Width;
                    }
                    if (double.IsInfinity(viewport.Height))
                    {
                        viewport.Height = stackPixelSize.Height;
                    }

                    _extendedViewport = ExtendViewport(
                                            virtualizationInfoProvider,
                                            isHorizontal,
                                            viewport,
                                            cacheSize,
                                            cacheUnit,
                                            stackPixelSizeInCacheBeforeViewport,
                                            stackLogicalSizeInCacheBeforeViewport,
                                            stackPixelSizeInCacheAfterViewport,
                                            stackLogicalSizeInCacheAfterViewport,
                                            stackPixelSize,
                                            stackLogicalSize,
                                            ref _itemsInExtendedViewportCount);

                    // It is important that this be set after the call to ExtendedViewport because that method uses the previous value of _viewport
                    _viewport = viewport;

                    // ===================================================================================
                    // ===================================================================================
                    // Store the sizes that have been computed on the parent
                    // ===================================================================================
                    // ===================================================================================
                    if (virtualizationInfoProvider != null && IsVisible)
                    {
                        //
                        // Note that it is possible to receive a Measure request even if the panel is
                        // actually not visible. This has been observed in a recycling TreeView where
                        // recycled TreeViewItems often switch IsExpanded states when representing
                        // different pieces of data. The IsVisible check above is to account for this scenario.
                        //
                        virtualizationInfoProvider.ItemDesiredSizes = new HierarchicalVirtualizationItemDesiredSizes(
                            stackLogicalSize,
                            stackLogicalSizeInViewport,
                            stackLogicalSizeInCacheBeforeViewport,
                            stackLogicalSizeInCacheAfterViewport,
                            stackPixelSize,
                            stackPixelSizeInViewport,
                            stackPixelSizeInCacheBeforeViewport,
                            stackPixelSizeInCacheAfterViewport);
                        virtualizationInfoProvider.MustDisableVirtualization = mustDisableVirtualization;
                    }

                    if (MustDisableVirtualization != mustDisableVirtualization)
                    {
                        MustDisableVirtualization = mustDisableVirtualization;
                        remeasure |= IsScrolling;
                    }

                    // ===================================================================================
                    // ===================================================================================
                    // Adjust the scroll offset, extent, etc.
                    // ===================================================================================
                    // ===================================================================================
                    double effectiveOffset = 0.0;
                    if (!isVSP45Compat)
                    {
                        if (hasAverageContainerSizeChanged || hasAnyContainerSpanChanged)
                        {
                            // revise the offset used for the viewport origin, for use
                            // in future calls to InitializeViewport (part of Measure)
                            effectiveOffset = ComputeEffectiveOffset(
                                ref viewport,
                                firstContainerInViewport,
                                firstItemInViewportIndex,
                                firstItemInViewportOffset,
                                items,
                                itemStorageProvider,
                                virtualizationInfoProvider,
                                isHorizontal,
                                areContainersUniformlySized,
                                uniformOrAverageContainerSize,
                                scrollGeneration);

                            // also revise the offset of the first container, for use in Arrange
                            if (firstContainerInViewport != null)
                            {
                                double newOffset;
                                ComputeDistance(items, itemStorageProvider, isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize, 0, _firstItemInExtendedViewportIndex, out newOffset);

                                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                                {
                                    ScrollTracer.Trace(this, ScrollTraceOp.ReviseArrangeOffset,
                                        _firstItemInExtendedViewportOffset, newOffset);
                                }
                                _firstItemInExtendedViewportOffset = newOffset;
                            }

                            // make sure the parent panel reacts to this panel's effective
                            // offset change, even if this panel doesn't change size
                            if (!IsScrolling)
                            {
                                DependencyObject itemsOwner = itemStorageProvider as DependencyObject;
                                Panel parentPanel = (itemsOwner != null) ? VisualTreeHelper.GetParent(itemsOwner) as Panel : null;
                                if (parentPanel != null)
                                {
                                    parentPanel.InvalidateMeasure();
                                }
                            }
                        }

                        // (DevDiv2 1174102) if items are added/removed in a descendant panel,
                        // we may need to recompute the effective offset of this panel
                        // (see UpdateExtent).  The information necessary to do this won't
                        // be directly available at that time, so we store it now
                        // just in case.
                        if (HasVirtualizingChildren)
                        {
                            FirstContainerInformation info =
                                new FirstContainerInformation(
                                        ref viewport,
                                        firstContainerInViewport,
                                        firstItemInViewportIndex,
                                        firstItemInViewportOffset,
                                        scrollGeneration);
                            FirstContainerInformationField.SetValue(this, info);
                        }
                    }

                    if (IsScrolling)
                    {
                        if (isVSP45Compat)
                        {
                            SetAndVerifyScrollingData(
                                isHorizontal,
                                viewport,
                                constraint,
                                ref stackPixelSize,
                                ref stackLogicalSize,
                                ref stackPixelSizeInViewport,
                                ref stackLogicalSizeInViewport,
                                ref stackPixelSizeInCacheBeforeViewport,
                                ref stackLogicalSizeInCacheBeforeViewport,
                                ref remeasure,
                                ref lastPageSafeOffset,
                                ref previouslyMeasuredOffsets);
                        }
                        else
                        {
                            SetAndVerifyScrollingData(
                                isHorizontal,
                                viewport,
                                constraint,
                                firstContainerInViewport,
                                firstItemInViewportOffset,
                                hasAverageContainerSizeChanged,
                                effectiveOffset,
                                ref stackPixelSize,
                                ref stackLogicalSize,
                                ref stackPixelSizeInViewport,
                                ref stackLogicalSizeInViewport,
                                ref stackPixelSizeInCacheBeforeViewport,
                                ref stackLogicalSizeInCacheBeforeViewport,
                                ref remeasure,
                                ref lastPageSafeOffset,
                                ref lastPagePixelSize,
                                ref previouslyMeasuredOffsets);
                        }
                    }

                    EscapeMeasure:
                    // ===================================================================================
                    // ===================================================================================
                    // Cleanup items no longer in the viewport
                    // ===================================================================================
                    // ===================================================================================
                    if (!remeasure)
                    {
                        if (IsVirtualizing)
                        {
                            if (InRecyclingMode)
                            {
                                DisconnectRecycledContainers();

                                if (visualOrderChanged)
                                {
                                    //
                                    // We moved some containers in the visual tree without firing
                                    // changed events.  ZOrder is now invalid.
                                    //
                                    InvalidateZState();
                                }
                            }
                            else
                            {
                                EnsureCleanupOperation(false /*delay*/);
                            }
                        }
                        HasVirtualizingChildren = hasVirtualizingChildren;

                        debug_AssertRealizedChildrenEqualVisualChildren();
                    }

                    if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                    {
                        // save information needed by Snapshot
                        DependencyObject offsetHost = virtualizationInfoProvider as DependencyObject;
                        EffectiveOffsetInformation effectiveOffsetInfo = (offsetHost != null) ? EffectiveOffsetInformationField.GetValue(offsetHost) : null;
                        SnapshotData data = new SnapshotData {
                            UniformOrAverageContainerSize = uniformOrAverageContainerPixelSize,
                            UniformOrAverageContainerPixelSize = uniformOrAverageContainerPixelSize,
                            EffectiveOffsets = (effectiveOffsetInfo != null) ? effectiveOffsetInfo.OffsetList : null
                        };
                        SnapshotDataField.SetValue(this, data);
                    }
                }
            }
            finally
            {
                if (etwTracingEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringEnd, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, "VirtualizingStackPanel :MeasureOverride");
                }
            }

            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.EndMeasure,
                    stackPixelSize, remeasure);
            }

            if (remeasure)
            {
                if (!IsVSP45Compat && IsScrolling)
                {
                    // remeasure from the root should use fresh effective offsets
                    IncrementScrollGeneration();
                }

                //
                // Make another pass of MeasureOverride if remeasure is true.
                //
                return MeasureOverrideImpl(constraint,
                                ref lastPageSafeOffset,
                                ref previouslyMeasuredOffsets,
                                ref lastPagePixelSize,
                                remeasure);
            }
            else
            {
                return stackPixelSize;
            }
        }

        private Size MeasureNonItemsHost(Size constraint)
        {
            return StackPanel.StackMeasureHelper(this, _scrollData, constraint);
        }

        private Size ArrangeNonItemsHost(Size arrangeSize)
        {
            return StackPanel.StackArrangeHelper(this, _scrollData, arrangeSize);
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
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringBegin, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, "VirtualizingStackPanel :ArrangeOverride");
            }
            try
            {
                if (!IsItemsHost)
                {
                    ArrangeNonItemsHost(arrangeSize);
                }
                else
                {
                    // ===================================================================================
                    // ===================================================================================
                    // Fetch owners
                    // ===================================================================================
                    // ===================================================================================

                    ItemsControl itemsControl = null;
                    GroupItem groupItem = null;

                    //
                    // This is an interface implemented by the owner of this panel in order to facilitate between a parent
                    // panel and this one when virtualizing in a hierarchy. (Eg. TreeView or grouping ItemsControl.) This
                    // interface is currently implemented by TreeViewItem and GroupItem.
                    //
                    IHierarchicalVirtualizationAndScrollInfo virtualizationInfoProvider = null;

                    //
                    // This is a service provided by the owner of this panel to store and retrieve information on a per item
                    // basis. Specifically this panel uses this service to remember DesiredSize of items when virtualizing.
                    // This interface is currently implemented by ItemsControl and GroupItem.
                    //
                    IContainItemStorage itemStorageProvider = null;

                    //
                    // This is the item representing the owner for this panel. (Eg. The CollectionViewGroup for the owner GroupItem)
                    //
                    object parentItem = null;
                    IContainItemStorage parentItemStorageProvider;

                    //
                    // Is horizontally stacking
                    //
                    bool isHorizontal = (Orientation == Orientation.Horizontal);

                    //
                    // Compute if this panel is different in orientation that either its parent or descendents
                    //
                    bool mustDisableVirtualization = false;

                    //
                    // Fetch the owner for this panel. That could either be an ItemsControl or a GroupItem.
                    //
                    GetOwners(false /*shouldSetVirtualizationState*/, isHorizontal, out itemsControl, out groupItem, out itemStorageProvider, out virtualizationInfoProvider, out parentItem, out parentItemStorageProvider, out mustDisableVirtualization);

                    if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                    {
                        ScrollTracer.Trace(this, ScrollTraceOp.BeginArrange,
                            arrangeSize,
                            "ptv:", _pixelDistanceToViewport,
                            "ptfc:", _pixelDistanceToFirstContainerInExtendedViewport);
                    }

                    // ===================================================================================
                    // ===================================================================================
                    // Get set to enumerate the items of the owner
                    // ===================================================================================
                    // ===================================================================================

                    EnsureGenerator();
                    IList children = RealizedChildren;  // yes, this is weird, but this property ensures the Generator is properly initialized.
                    IItemContainerGenerator generator = Generator;
                    IList items = ((ItemContainerGenerator)generator).ItemsInternal;
                    int itemCount = items.Count;

                    //
                    // Locally cache the values of this flag and size for better performance.
                    //
                    IContainItemStorage uniformSizeItemStorageProvider = IsVSP45Compat ? itemStorageProvider : parentItemStorageProvider;
                    bool areContainersUniformlySized = GetAreContainersUniformlySized(uniformSizeItemStorageProvider, parentItem);
                    double uniformOrAverageContainerSize, uniformOrAverageContainerPixelSize;
                    GetUniformOrAverageContainerSize(uniformSizeItemStorageProvider, parentItem,
                        IsPixelBased || IsVSP45Compat,
                        out uniformOrAverageContainerSize,
                        out uniformOrAverageContainerPixelSize);

                    ScrollViewer scrollOwner = ScrollOwner;
                    double arrangeLength = 0;
                    if (scrollOwner != null && scrollOwner.CanContentScroll)
                    {
                        // If scollowner's CanContentScroll is true,
                        // loop through all the children and find the
                        // maximum desired size and arrange all the chilren
                        // with it.
                        arrangeLength = GetMaxChildArrangeLength(children, isHorizontal);
                    }

                    arrangeLength = Math.Max(isHorizontal ? arrangeSize.Height : arrangeSize.Width, arrangeLength);

                    // ===================================================================================
                    // ===================================================================================
                    // Arrange the children of this panel starting with the first item in the extended viewport
                    // ===================================================================================
                    // ===================================================================================

                    UIElement child = null;
                    Size childDesiredSize = Size.Empty;
                    Rect rcChild = new Rect(arrangeSize);

                    Size previousChildSize = new Size();
                    int previousChildItemIndex = -1;
                    Point previousChildOffset = new Point();

                    bool isVSP45Compat = IsVSP45Compat;

                    for (int i = _firstItemInExtendedViewportChildIndex; i < children.Count; ++i)
                    {
                        child = (UIElement)children[i];
                        childDesiredSize = child.DesiredSize;

                        if (i >= _firstItemInExtendedViewportChildIndex && i < _firstItemInExtendedViewportChildIndex + _actualItemsInExtendedViewportCount)
                        {
                            // ===================================================================================
                            // ===================================================================================
                            // Arrange the first item in the extended viewport
                            // ===================================================================================
                            // ===================================================================================

                            if (i == _firstItemInExtendedViewportChildIndex)
                            {
                                ArrangeFirstItemInExtendedViewport(
                                    isHorizontal,
                                    child,
                                    childDesiredSize,
                                    arrangeLength,
                                    ref rcChild,
                                    ref previousChildSize,
                                    ref previousChildOffset,
                                    ref previousChildItemIndex);

                                // ===================================================================================
                                // ===================================================================================
                                // Arrange the items before the extended viewport
                                // ===================================================================================
                                // ===================================================================================

                                UIElement containerBeforeViewport = null;
                                Size childSizeBeforeViewport = Size.Empty;
                                Rect rcChildBeforeViewport = rcChild;
                                Size previousChildSizeBeforeViewport = child.DesiredSize;
                                int previousChildItemIndexBeforeViewport = previousChildItemIndex;
                                Point previousChildOffsetBeforeViewport = previousChildOffset;

                                for (int j = _firstItemInExtendedViewportChildIndex - 1; j >= 0; j--)
                                {
                                    containerBeforeViewport = (UIElement)children[j];
                                    childSizeBeforeViewport = containerBeforeViewport.DesiredSize;

                                    ArrangeItemsBeyondTheExtendedViewport(
                                        isHorizontal,
                                        containerBeforeViewport,
                                        childSizeBeforeViewport,
                                        arrangeLength,
                                        items,
                                        generator,
                                        itemStorageProvider,
                                        areContainersUniformlySized,
                                        uniformOrAverageContainerSize,
                                        true /*beforeExtendedViewport*/,
                                        ref rcChildBeforeViewport,
                                        ref previousChildSizeBeforeViewport,
                                        ref previousChildOffsetBeforeViewport,
                                        ref previousChildItemIndexBeforeViewport);

                                    if (!isVSP45Compat)
                                    {
                                        SetItemsHostInsetForChild(j, containerBeforeViewport, itemStorageProvider, isHorizontal);
                                    }
                                }
                            }
                            else
                            {
                                // ===================================================================================
                                // ===================================================================================
                                // Arrange the other items within the extended viewport after the first
                                // ===================================================================================
                                // ===================================================================================

                                ArrangeOtherItemsInExtendedViewport(
                                    isHorizontal,
                                    child,
                                    childDesiredSize,
                                    arrangeLength,
                                    i,
                                    ref rcChild,
                                    ref previousChildSize,
                                    ref previousChildOffset,
                                    ref previousChildItemIndex);
                            }
                        }
                        else
                        {
                            // ===================================================================================
                            // ===================================================================================
                            // Arrange the items after the extended viewport
                            // ===================================================================================
                            // ===================================================================================

                            ArrangeItemsBeyondTheExtendedViewport(
                                isHorizontal,
                                child,
                                childDesiredSize,
                                arrangeLength,
                                items,
                                generator,
                                itemStorageProvider,
                                areContainersUniformlySized,
                                uniformOrAverageContainerSize,
                                false /*beforeExtendedViewport*/,
                                ref rcChild,
                                ref previousChildSize,
                                ref previousChildOffset,
                                ref previousChildItemIndex);
                        }

                        if (!isVSP45Compat)
                        {
                            SetItemsHostInsetForChild(i, child, itemStorageProvider, isHorizontal);
                        }
                    }

                    if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                    {
                        // save information needed by Snapshot
                        DependencyObject offsetHost = virtualizationInfoProvider as DependencyObject;
                        EffectiveOffsetInformation effectiveOffsetInfo = (offsetHost != null) ? EffectiveOffsetInformationField.GetValue(offsetHost) : null;
                        SnapshotData data = new SnapshotData {
                            UniformOrAverageContainerSize = uniformOrAverageContainerPixelSize,
                            UniformOrAverageContainerPixelSize = uniformOrAverageContainerPixelSize,
                            EffectiveOffsets = (effectiveOffsetInfo != null) ? effectiveOffsetInfo.OffsetList : null
                        };
                        SnapshotDataField.SetValue(this, data);

                        ScrollTracer.Trace(this, ScrollTraceOp.EndArrange,
                            arrangeSize, _firstItemInExtendedViewportIndex, _firstItemInExtendedViewportOffset);
                    }
                }
            }
            finally
            {
                if (etwTracingEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientStringEnd, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, "VirtualizingStackPanel :ArrangeOverride");
                }
            }

            return arrangeSize;
        }

        /// <summary>
        ///     Called when the Items collection associated with the containing ItemsControl changes.
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="args">Event arguments</param>
        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.ItemsChanged,
                    args.Action,
                    "pos:", args.OldPosition, args.Position,
                    "count:", args.ItemCount, args.ItemUICount,
                    MeasureInProgress ? "MeasureInProgress" : String.Empty);
            }

            if (MeasureInProgress)
            {
                // If the Items collection changes during Measure, the measure state
                // local to MeasureOverrideImpl is invalid and we need to start over.
                // This is an unusual situation, but it can happen if the process
                // of linking the container to its item has a side-effect of adding
                // or removing items to the underlying collection.

                // This occurs in VS SolutionNavigator, when
                // VirtualizingTreeView.PrepareContainerForItemOverride binds a
                // PivotTreeViewItem to the HasItems property of a VirtualizingTreeView+TreeNode.
                // The property-getter for HasItems can invoke an inline task that
                // adds/removes items.

                ItemsChangedDuringMeasure = true;
            }

            base.OnItemsChanged(sender, args);

            bool resetMaximumDesiredSize = false;

            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                    OnItemsRemove(args);
                    resetMaximumDesiredSize = true;
                    break;

                case NotifyCollectionChangedAction.Replace:
                    OnItemsReplace(args);
                    resetMaximumDesiredSize = true;
                    break;

                case NotifyCollectionChangedAction.Move:
                    OnItemsMove(args);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    resetMaximumDesiredSize = true;

                    IContainItemStorage itemStorageProvider = GetItemStorageProvider(this);
                    itemStorageProvider.Clear();

                    ClearAsyncOperations();

                    break;
            }

            if (resetMaximumDesiredSize && IsScrolling)
            {
                ResetMaximumDesiredSize();
            }
        }

        internal void ResetMaximumDesiredSize()
        {
            if (IsScrolling)
            {
                // The items changed such that the maximum size may no longer be valid.
                // The next layout pass will update this value.
                _scrollData._maxDesiredSize = new Size();
            }
        }

        /// <summary>
        ///     Returns whether an Items collection change affects layout for this panel.
        /// </summary>
        /// <param name="args">Event arguments</param>
        /// <param name="areItemChangesLocal">Says if this notification represents a direct change to this Panel's collection</param>
        protected override bool ShouldItemsChangeAffectLayoutCore(bool areItemChangesLocal, ItemsChangedEventArgs args)
        {
            bool shouldItemsChangeAffectLayout = true;

            if (IsVirtualizing)
            {
                if (areItemChangesLocal)
                {
                    //
                    // Check if the indices being mutated lie beyond the currently generated indices.
                    // Please note that mutations prior to the currently generated viewport affect the
                    // start element within the viewport and hence necessitates a layout update. This
                    // is the reason we only consider mutations after the generated viewport for this
                    // optimization.
                    //

                    switch (args.Action)
                    {
                        case NotifyCollectionChangedAction.Remove:
                            {
                                int startOldIndex = Generator.IndexFromGeneratorPosition(args.OldPosition);

                                shouldItemsChangeAffectLayout = args.ItemUICount > 0 ||
                                    (startOldIndex < _firstItemInExtendedViewportIndex + _itemsInExtendedViewportCount);
                            }
                            break;

                        case NotifyCollectionChangedAction.Replace:
                            {
                                shouldItemsChangeAffectLayout = args.ItemUICount > 0;
                            }
                            break;

                        case NotifyCollectionChangedAction.Add:
                            {
                                int startIndex = Generator.IndexFromGeneratorPosition(args.Position);

                                shouldItemsChangeAffectLayout =
                                    (startIndex < _firstItemInExtendedViewportIndex + _itemsInExtendedViewportCount);
                            }
                            break;

                        case NotifyCollectionChangedAction.Move:
                            {
                                int startIndex = Generator.IndexFromGeneratorPosition(args.Position);
                                int startOldIndex = Generator.IndexFromGeneratorPosition(args.OldPosition);

                                shouldItemsChangeAffectLayout =
                                    ((startIndex < _firstItemInExtendedViewportIndex + _itemsInExtendedViewportCount) ||
                                     (startOldIndex < _firstItemInExtendedViewportIndex + _itemsInExtendedViewportCount));
                            }
                            break;
                    }
                }
                else
                {
                    //
                    // Given that this isnt the collection being directly manipulated, we check to see if the
                    // index affected is the last one generated. Consider the following example.
                    //
                    // Grp1
                    //  1
                    //  2
                    //  3
                    // Grp2
                    //  4
                    //  5
                    //  6
                    //
                    // Now if item 7 gets added to Grp1, even though 7 is beyond the currently generated items
                    // within Grp1, Grp1 is not the last entity within the viewport. Hence we need a layout update
                    // here. Conversely if item 7 were added to Grp2, then 7 is both beyond the currently generated
                    // range for Grp2 and also beyond the overall viewport because Grp2 happens to be last
                    // generated container within its parent panel.
                    //

                    Debug.Assert(args.Action == NotifyCollectionChangedAction.Reset && args.ItemCount == 1);

                    int startIndex = Generator.IndexFromGeneratorPosition(args.Position);

                    shouldItemsChangeAffectLayout =
                        (startIndex != _firstItemInExtendedViewportIndex + _itemsInExtendedViewportCount - 1);
                }

                if (!shouldItemsChangeAffectLayout)
                {
                    if (IsScrolling)
                    {
                        //
                        // If this is the scrolling panel we finally need to ensure that the viewport is currently
                        // fully occupied to sanction is optimization. Because if it isnt then any collection mutations
                        // show in the viewport by default and thus need a layout update.
                        //

                        shouldItemsChangeAffectLayout = !IsExtendedViewportFull();

                        if (!shouldItemsChangeAffectLayout)
                        {
                            //
                            // If we've passed the earlier check we attempt a surgical update to the scroll extent
                            //

                            UpdateExtent(areItemChangesLocal);
                        }
                    }
                    else
                    {
                        //
                        // If this isnt the scrolling panel then we need to recursively check parent panels
                        //

                        DependencyObject itemsOwner = ItemsControl.GetItemsOwnerInternal(this);
                        VirtualizingPanel vp = VisualTreeHelper.GetParent(itemsOwner) as VirtualizingPanel;
                        if (vp != null)
                        {
                            //
                            // In hierarchical scenarios we must update the extent at each descendent level before we recurse
                            // to the level higher so that the level higher gets to synchronize its uniform size flags based upon
                            // this update.
                            //

                            UpdateExtent(areItemChangesLocal);

                            IItemContainerGenerator generator = vp.ItemContainerGenerator;
                            int index = ((ItemContainerGenerator)generator).IndexFromContainer(itemsOwner, true /*returnLocalIndex*/);
                            ItemsChangedEventArgs newArgs = new ItemsChangedEventArgs(NotifyCollectionChangedAction.Reset,
                                generator.GeneratorPositionFromIndex(index), 1, 1);

                            shouldItemsChangeAffectLayout = vp.ShouldItemsChangeAffectLayout(false /*areItemChangesLocal*/, newArgs);
                        }
                        else
                        {
                            //
                            // If we arent able to find VirtualizingPanels to check, then we must default to updating layout
                            //

                            shouldItemsChangeAffectLayout = true;
                        }
                    }
                }
            }

            return shouldItemsChangeAffectLayout;
        }

        private void UpdateExtent(bool areItemChangesLocal)
        {
            bool isHorizontal = (Orientation == Orientation.Horizontal);
            bool isVSP45Compat = IsVSP45Compat;

            ItemsControl itemsControl;
            GroupItem groupItem;
            IContainItemStorage itemStorageProvider;
            IHierarchicalVirtualizationAndScrollInfo virtualizationInfoProvider;
            object parentItem;
            IContainItemStorage parentItemStorageProvider;
            bool mustDisableVirtualization;

            GetOwners(false /*shouldSetVirtualizationState*/, isHorizontal,
                out itemsControl, out groupItem, out itemStorageProvider,
                out virtualizationInfoProvider, out parentItem,
                out parentItemStorageProvider, out mustDisableVirtualization);

            IContainItemStorage uniformSizeItemStorageProvider = isVSP45Compat ? itemStorageProvider : parentItemStorageProvider;
            bool areContainersUniformlySized = GetAreContainersUniformlySized(uniformSizeItemStorageProvider, parentItem);
            double uniformOrAverageContainerSize, uniformOrAverageContainerPixelSize;
            GetUniformOrAverageContainerSize(uniformSizeItemStorageProvider, parentItem,
                isVSP45Compat || IsPixelBased,
                out uniformOrAverageContainerSize, out uniformOrAverageContainerPixelSize);

            IList children = RealizedChildren;
            IItemContainerGenerator generator = Generator;
            IList items = ((ItemContainerGenerator)generator).ItemsInternal;
            int itemCount = items.Count;

            if (!areItemChangesLocal)
            {
                //
                // If the actual item changes arent local to this panel then we need to sync
                // the flags for this panel to make sure we gather size updates from
                // descendent panel that actually contained the collection changes.
                //
                double computedUniformOrAverageContainerSize = uniformOrAverageContainerSize;
                double computedUniformOrAverageContainerPixelSize = uniformOrAverageContainerPixelSize;
                bool computedAreContainersUniformlySized = areContainersUniformlySized;
                bool hasAverageContainerSizeChanged = false;

                if (isVSP45Compat)
                {
                    SyncUniformSizeFlags(
                        parentItem,
                        parentItemStorageProvider,
                        children,
                        items,
                        itemStorageProvider,
                        itemCount,
                        computedAreContainersUniformlySized,
                        computedUniformOrAverageContainerSize,
                        ref areContainersUniformlySized,
                        ref uniformOrAverageContainerSize,
                        ref hasAverageContainerSizeChanged,
                        isHorizontal,
                        true /* evaluateAreContainersUniformlySized */);
                }
                else
                {
                    SyncUniformSizeFlags(
                        parentItem,
                        parentItemStorageProvider,
                        children,
                        items,
                        itemStorageProvider,
                        itemCount,
                        computedAreContainersUniformlySized,
                        computedUniformOrAverageContainerSize,
                        computedUniformOrAverageContainerPixelSize,
                        ref areContainersUniformlySized,
                        ref uniformOrAverageContainerSize,
                        ref uniformOrAverageContainerPixelSize,
                        ref hasAverageContainerSizeChanged,
                        isHorizontal,
                        true /* evaluateAreContainersUniformlySized */);
                }

                if (hasAverageContainerSizeChanged && !IsVSP45Compat)
                {
                    // the extent change has altered the coordinate system, so
                    // store an effective offset (DevDiv2 1174102)
                    FirstContainerInformation info = FirstContainerInformationField.GetValue(this);
                    Debug.Assert(info != null, "Expected state from previous measure not found");
                    if (info != null)
                    {
                        ComputeEffectiveOffset(
                                ref info.Viewport,
                                info.FirstContainer,
                                info.FirstItemIndex,
                                info.FirstItemOffset,
                                items,
                                itemStorageProvider,
                                virtualizationInfoProvider,
                                isHorizontal,
                                areContainersUniformlySized,
                                uniformOrAverageContainerSize,
                                info.ScrollGeneration);
                    }
                }
            }

            double distance = 0;
            ComputeDistance(items, itemStorageProvider, isHorizontal,
                areContainersUniformlySized,
                uniformOrAverageContainerSize,
                0, items.Count, out distance);

            if (IsScrolling)
            {
                if (isHorizontal)
                {
                    _scrollData._extent.Width = distance;
                }
                else
                {
                    _scrollData._extent.Height = distance;
                }

                ScrollOwner.InvalidateScrollInfo();
            }
            else if (virtualizationInfoProvider != null)
            {
                HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes = virtualizationInfoProvider.ItemDesiredSizes;

                if (IsPixelBased)
                {
                    Size pixelSize = itemDesiredSizes.PixelSize;
                    if (isHorizontal)
                    {
                        pixelSize.Width = distance;
                    }
                    else
                    {
                        pixelSize.Height = distance;
                    }

                    itemDesiredSizes = new HierarchicalVirtualizationItemDesiredSizes(
                        itemDesiredSizes.LogicalSize,
                        itemDesiredSizes.LogicalSizeInViewport,
                        itemDesiredSizes.LogicalSizeBeforeViewport,
                        itemDesiredSizes.LogicalSizeAfterViewport,
                        pixelSize,
                        itemDesiredSizes.PixelSizeInViewport,
                        itemDesiredSizes.PixelSizeBeforeViewport,
                        itemDesiredSizes.PixelSizeAfterViewport);
                }
                else
                {
                    Size logicalSize = itemDesiredSizes.LogicalSize;
                    if (isHorizontal)
                    {
                        logicalSize.Width = distance;
                    }
                    else
                    {
                        logicalSize.Height = distance;
                    }

                    itemDesiredSizes = new HierarchicalVirtualizationItemDesiredSizes(
                        logicalSize,
                        itemDesiredSizes.LogicalSizeInViewport,
                        itemDesiredSizes.LogicalSizeBeforeViewport,
                        itemDesiredSizes.LogicalSizeAfterViewport,
                        itemDesiredSizes.PixelSize,
                        itemDesiredSizes.PixelSizeInViewport,
                        itemDesiredSizes.PixelSizeBeforeViewport,
                        itemDesiredSizes.PixelSizeAfterViewport);
                }

                virtualizationInfoProvider.ItemDesiredSizes = itemDesiredSizes;
            }
        }

        private bool IsExtendedViewportFull()
        {
            Debug.Assert(IsScrolling && IsVirtualizing, "Only check viewport on scrolling panel when virtualizing");

            bool isHorizontal = (Orientation == Orientation.Horizontal);

            bool isViewportFull =
                ((isHorizontal && DoubleUtil.GreaterThanOrClose(DesiredSize.Width, PreviousConstraint.Width)) ||
                 (!isHorizontal && DoubleUtil.GreaterThanOrClose(DesiredSize.Height, PreviousConstraint.Height)));

            if (isViewportFull)
            {
                IHierarchicalVirtualizationAndScrollInfo virtualizationInfoProvider = null;
                Rect viewport = _viewport;
                Rect currentExtendedViewport = _extendedViewport;
                Rect estimatedExtendedViewport = Rect.Empty;
                VirtualizationCacheLength cacheLength = VirtualizingPanel.GetCacheLength(this);
                VirtualizationCacheLengthUnit cacheUnit = VirtualizingPanel.GetCacheLengthUnit(this);
                int itemsInExtendedViewportCount = _itemsInExtendedViewportCount;

                NormalizeCacheLength(isHorizontal, viewport, ref cacheLength, ref cacheUnit);

                estimatedExtendedViewport = ExtendViewport(
                    virtualizationInfoProvider,
                    isHorizontal,
                    viewport,
                    cacheLength,
                    cacheUnit,
                    Size.Empty,
                    Size.Empty,
                    Size.Empty,
                    Size.Empty,
                    Size.Empty,
                    Size.Empty,
                    ref itemsInExtendedViewportCount);

                return ((isHorizontal && DoubleUtil.GreaterThanOrClose(currentExtendedViewport.Width, estimatedExtendedViewport.Width)) ||
                        (!isHorizontal && DoubleUtil.GreaterThanOrClose(currentExtendedViewport.Height, estimatedExtendedViewport.Height)));
            }

            return false;
        }

        /// <summary>
        ///     Called when the UI collection of children is cleared by the base Panel class.
        /// </summary>
        protected override void OnClearChildren()
        {
            base.OnClearChildren();

            if (IsVirtualizing && IsItemsHost)
            {
                ItemsControl itemsControl;
                ItemsControl.GetItemsOwnerInternal(this, out itemsControl);

                CleanupContainers(Int32.MaxValue, Int32.MaxValue, itemsControl);
            }

            if (_realizedChildren != null)
            {
                _realizedChildren.Clear();
            }

            InternalChildren.ClearInternal();
        }

        #endregion Protected Methods

        #region Internal Methods

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                IHierarchicalVirtualizationAndScrollInfo virtualizingProvider = GetVirtualizingProvider();
                if (virtualizingProvider != null)
                {
                    Helper.ClearVirtualizingElement(virtualizingProvider);
                }

                ClearAsyncOperations();
            }
            else
            {
                // We now depend upon the IsVisible state of the panel in a number of places.
                // So it is required that we relayout when the panel is made visible.
                InvalidateMeasure();
            }
        }

        // Tells the Generator to clear out all containers for this ItemsControl.  This is called by the ItemValueStorage
        // service when the ItemsControl this panel is a host for is about to be thrown away.  This allows the VSP to save
        // off any properties it is interested in and results in a call to ClearContainerForItem on the ItemsControl, allowing
        // the Item Container Storage to do so as well.

        // Note: A possible perf improvement may be to make 'fast' RemoveAll on the Generator that simply calls ClearContainerForItem
        // for us without walking through its data structures to actually clean out items.
        internal void ClearAllContainers()
        {
            IItemContainerGenerator generator = Generator;
            if (generator != null)
            {
                generator.RemoveAll();
            }
        }

        #endregion

        //
        // MeasureOverride Helpers
        //

        #region MeasureOverride Helpers

        private IHierarchicalVirtualizationAndScrollInfo GetVirtualizingProvider()
        {
            ItemsControl itemsControl = null;
            DependencyObject itemsOwner = ItemsControl.GetItemsOwnerInternal(this, out itemsControl);
            if (itemsOwner is GroupItem)
            {
                return GetVirtualizingProvider(itemsOwner);
            }
            return GetVirtualizingProvider(itemsControl);
        }

        private static IHierarchicalVirtualizationAndScrollInfo GetVirtualizingProvider(DependencyObject element)
        {
            IHierarchicalVirtualizationAndScrollInfo virtualizingProvider = element as IHierarchicalVirtualizationAndScrollInfo;
            if (virtualizingProvider != null)
            {
                VirtualizingPanel virtualizingPanel = VisualTreeHelper.GetParent(element) as VirtualizingPanel;
                if (virtualizingPanel == null || !virtualizingPanel.CanHierarchicallyScrollAndVirtualize)
                {
                    virtualizingProvider = null;
                }
            }

            return virtualizingProvider;
        }

        private static IHierarchicalVirtualizationAndScrollInfo GetVirtualizingChild(DependencyObject element)
        {
            bool isChildHorizontal = false;
            return GetVirtualizingChild(element, ref isChildHorizontal);
        }

        private static IHierarchicalVirtualizationAndScrollInfo GetVirtualizingChild(DependencyObject element, ref bool isChildHorizontal)
        {
            IHierarchicalVirtualizationAndScrollInfo virtualizingChild = element as IHierarchicalVirtualizationAndScrollInfo;
            if (virtualizingChild != null && virtualizingChild.ItemsHost != null)
            {
                isChildHorizontal = (virtualizingChild.ItemsHost.LogicalOrientationPublic == Orientation.Horizontal);

                VirtualizingPanel virtualizingPanel = virtualizingChild.ItemsHost as VirtualizingPanel;
                if (virtualizingPanel == null || !virtualizingPanel.CanHierarchicallyScrollAndVirtualize)
                {
                    virtualizingChild = null;
                }
            }

            return virtualizingChild;
        }


        /// <summary>
        /// Initializes the owner and interfaces for the virtualization services it supports.
        /// </summary>
        private static IContainItemStorage GetItemStorageProvider(Panel itemsHost)
        {
            ItemsControl itemsControl = null;
            GroupItem groupItem = null;

            DependencyObject itemsOwner = ItemsControl.GetItemsOwnerInternal(itemsHost, out itemsControl);
            if (itemsOwner != itemsControl)
            {
                groupItem = itemsOwner as GroupItem;
            }

            return itemsOwner as IContainItemStorage;
        }

        /// <summary>
        /// Initializes the owner and interfaces for the virtualization services it supports.
        /// </summary>
        private void GetOwners(
            bool shouldSetVirtualizationState,
            bool isHorizontal,
            out ItemsControl itemsControl,
            out GroupItem groupItem,
            out IContainItemStorage itemStorageProvider,
            out IHierarchicalVirtualizationAndScrollInfo virtualizationInfoProvider,
            out object parentItem,
            out IContainItemStorage parentItemStorageProvider,
            out bool mustDisableVirtualization)
        {
            groupItem = null;
            parentItem = null;
            parentItemStorageProvider = null;

            bool isScrolling = IsScrolling;

            mustDisableVirtualization = isScrolling ? MustDisableVirtualization : false;

            ItemsControl parentItemsControl = null;
            DependencyObject itemsOwner = ItemsControl.GetItemsOwnerInternal(this, out itemsControl);
            if (itemsOwner != itemsControl)
            {
                groupItem = itemsOwner as GroupItem;
                parentItem = itemsControl.ItemContainerGenerator.ItemFromContainer(groupItem);
            }
            else if (!isScrolling)
            {
                parentItemsControl = ItemsControl.GetItemsOwnerInternal(VisualTreeHelper.GetParent(itemsControl)) as ItemsControl;
                if (parentItemsControl != null)
                {
                    parentItem = parentItemsControl.ItemContainerGenerator.ItemFromContainer(itemsControl);
                }
                else
                {
                    parentItem = this;
                }
            }
            else
            {
                parentItem = this;
            }

            itemStorageProvider = itemsOwner as IContainItemStorage;
            virtualizationInfoProvider = null;
            parentItemStorageProvider = (IsVSP45Compat || isScrolling || itemsOwner == null) ? null :
                ItemsControl.GetItemsOwnerInternal(VisualTreeHelper.GetParent(itemsOwner)) as IContainItemStorage;

            if (groupItem != null)
            {
                virtualizationInfoProvider = GetVirtualizingProvider(groupItem);
                mustDisableVirtualization = virtualizationInfoProvider != null ? virtualizationInfoProvider.MustDisableVirtualization : false;
            }
            else if (!isScrolling)
            {
                virtualizationInfoProvider = GetVirtualizingProvider(itemsControl);
                mustDisableVirtualization = virtualizationInfoProvider != null ? virtualizationInfoProvider.MustDisableVirtualization : false;
            }

            if (shouldSetVirtualizationState)
            {
                // this is a good opportunity to set up tracing, if requested
                if (ScrollTracer.IsEnabled)
                {
                    ScrollTracer.ConfigureTracing(this, itemsOwner, parentItem, itemsControl);
                }

                //
                // Synchronize properties such as IsVirtualizing, IsRecycling & IsPixelBased
                //
                SetVirtualizationState(itemStorageProvider, itemsControl, mustDisableVirtualization);
            }
        }

        /// <summary>
        /// Sets up IsVirtualizing, VirtualizationMode, and IsPixelBased
        /// </summary>
        private void SetVirtualizationState(
            IContainItemStorage itemStorageProvider,
            ItemsControl itemsControl,
            bool mustDisableVirtualization)
        {
            if (itemsControl != null)
            {
                bool isVirtualizing = GetIsVirtualizing(itemsControl);
                bool isVirtualizingWhenGrouping = GetIsVirtualizingWhenGrouping(itemsControl);
                VirtualizationMode virtualizationMode = GetVirtualizationMode(itemsControl);
                bool isGrouping = itemsControl.IsGrouping;
                IsVirtualizing = !mustDisableVirtualization && ((!isGrouping && isVirtualizing) || (isGrouping && isVirtualizing && isVirtualizingWhenGrouping));

                ScrollUnit scrollUnit = GetScrollUnit(itemsControl);
                bool oldIsPixelBased = IsPixelBased;
                IsPixelBased = mustDisableVirtualization || (scrollUnit == ScrollUnit.Pixel);
                if (IsScrolling)
                {
                    if (!HasMeasured || oldIsPixelBased != IsPixelBased)
                    {
                        ClearItemValueStorageRecursive(itemStorageProvider, this);
                    }

                    SetCacheLength(this, GetCacheLength(itemsControl));
                    SetCacheLengthUnit(this, GetCacheLengthUnit(itemsControl));
                }

                //
                // Set up info on first measure
                //
                if (HasMeasured)
                {
                    VirtualizationMode oldVirtualizationMode = InRecyclingMode ? VirtualizationMode.Recycling : VirtualizationMode.Standard;
                    if (oldVirtualizationMode != virtualizationMode)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.CantSwitchVirtualizationModePostMeasure));
                    }
                }
                else
                {
                    HasMeasured = true;
                }

                InRecyclingMode = (virtualizationMode == VirtualizationMode.Recycling);
            }
        }

        private static void ClearItemValueStorageRecursive(IContainItemStorage itemStorageProvider, Panel itemsHost)
        {
            Helper.ClearItemValueStorage((DependencyObject)itemStorageProvider, _indicesStoredInItemValueStorage);

            UIElementCollection children = itemsHost.InternalChildren;
            int childrenCount = children.Count;
            for (int i=0; i<childrenCount; i++)
            {
                IHierarchicalVirtualizationAndScrollInfo virtualizingChild = children[i] as IHierarchicalVirtualizationAndScrollInfo;
                if (virtualizingChild != null)
                {
                    Panel childItemsHost = virtualizingChild.ItemsHost;
                    if (childItemsHost != null)
                    {
                        IContainItemStorage childItemStorageProvider = GetItemStorageProvider(childItemsHost);
                        if (childItemStorageProvider != null)
                        {
                            ClearItemValueStorageRecursive(childItemStorageProvider, childItemsHost);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the viewport for this panel.
        /// </summary>
        private void InitializeViewport(
            object parentItem,
            IContainItemStorage parentItemStorageProvider,
            IHierarchicalVirtualizationAndScrollInfo virtualizationInfoProvider,
            bool isHorizontal,
            Size constraint,
            ref Rect viewport,
            ref VirtualizationCacheLength cacheSize,
            ref VirtualizationCacheLengthUnit cacheUnit,
            out Rect extendedViewport,
            out long scrollGeneration)
        {
            Size extent = new Size();
            bool isVSP45Compat = IsVSP45Compat;

            if (IsScrolling)
            {
                //
                // We're the top level scrolling panel. Fetch the offset from the _scrollData.
                //

                Size size;
                double offsetX, offsetY;
                Size viewportSize;

                size = constraint;
                offsetX = _scrollData._offset.X;
                offsetY = _scrollData._offset.Y;
                extent = _scrollData._extent;
                viewportSize = _scrollData._viewport;
                scrollGeneration = _scrollData._scrollGeneration;

                if (!IsScrollActive || IgnoreMaxDesiredSize)
                {
                    _scrollData._maxDesiredSize = new Size();
                }

                if (IsPixelBased)
                {
                    viewport = new Rect(offsetX, offsetY, size.Width, size.Height);
                    CoerceScrollingViewportOffset(ref viewport, extent, isHorizontal);
                }
                else
                {
                    viewport = new Rect(offsetX, offsetY, viewportSize.Width, viewportSize.Height);
                    CoerceScrollingViewportOffset(ref viewport, extent, isHorizontal);
                    viewport.Size = size;
                }

                if (IsVirtualizing)
                {
                    cacheSize = VirtualizingStackPanel.GetCacheLength(this);
                    cacheUnit = VirtualizingStackPanel.GetCacheLengthUnit(this);

                    if (DoubleUtil.GreaterThan(cacheSize.CacheBeforeViewport, 0) ||
                        DoubleUtil.GreaterThan(cacheSize.CacheAfterViewport, 0))
                    {
                        if (!MeasureCaches)
                        {
                            WasLastMeasurePassAnchored = (_scrollData._firstContainerInViewport != null) || (_scrollData._bringIntoViewLeafContainer != null);

                            DispatcherOperation measureCachesOperation = MeasureCachesOperationField.GetValue(this);
                            if (measureCachesOperation == null)
                            {
                                Action measureCachesAction = null;
                                int retryCount = 3;
                                measureCachesAction = (Action)delegate()
                                    {
                                        Debug.Assert(retryCount >=0, "retry MeasureCaches too often");
                                        bool isLayoutDirty = (0 < retryCount--) && (MeasureDirty || ArrangeDirty);
                                        try
                                        {
                                            if (isVSP45Compat || !isLayoutDirty)
                                            {
                                                MeasureCachesOperationField.ClearValue(this);

                                                MeasureCaches = true;

                                                if (WasLastMeasurePassAnchored)
                                                {
                                                    SetAnchorInformation(isHorizontal);
                                                }

                                                InvalidateMeasure();
                                                UpdateLayout();
                                            }
                                        }
                                        finally
                                        {
                                            // check whether UpdateLayout finished the job
                                            isLayoutDirty = isLayoutDirty ||
                                                    ((0 < retryCount) && (MeasureDirty || ArrangeDirty));
                                            if (!isVSP45Compat && isLayoutDirty)
                                            {
                                                // try the measure-cache pass again later.
                                                // Note that we only do this when:
                                                // 1. this VSP's layout is dirty, either because
                                                //    a. it was dirty to begin with so we
                                                //       skipped UpdateLayout, or
                                                //    b. UpdateLayout ran, but left this VSP's
                                                //       layout dirty.
                                                // 2. we haven't run out of retries
                                                // 3. we're not in 4.5-compat mode
                                                //
                                                // (1) can happen if layout times out and moves to
                                                //     background.
                                                // (2) protects against loops when an app calls
                                                //     VSP.Measure directly, outside of the normal
                                                //     layout system (real appps don't do this,
                                                //     but test code does - it happens in the DrtXaml test).
                                                // (3) preserves compat with 4.5RTM, where the
                                                //     "move to background" situation led to an
                                                //     infinite loop.
                                                MeasureCachesOperationField.SetValue(this,
                                                    Dispatcher.BeginInvoke(DispatcherPriority.Background, measureCachesAction));
                                            }

                                            MeasureCaches = false;

                                            // If there is a pending anchor operation that got registered in
                                            // the current pass, or if layout didn't finish, we don't want to
                                            // clear the IsScrollActive flag.
                                            // We should allow that measure pass to also settle and then clear
                                            // the flag.

                                            DispatcherOperation anchoredInvalidateMeasureOperation = AnchoredInvalidateMeasureOperationField.GetValue(this);
                                            if (anchoredInvalidateMeasureOperation == null && (isVSP45Compat || !isLayoutDirty))
                                            {
                                                if (isVSP45Compat)
                                                {
                                                    IsScrollActive = false;
                                                }
                                                else if (IsScrollActive)
                                                {
                                                    // keep IsScrollActive set until the
                                                    // anchored measure has occurred.  It may
                                                    // need to remeasure, which should count
                                                    // as part of the scroll operation
                                                    DispatcherOperation clearIsScrollActiveOperation = ClearIsScrollActiveOperationField.GetValue(this);
                                                    if (clearIsScrollActiveOperation != null)
                                                    {
                                                        clearIsScrollActiveOperation.Abort();
                                                    }
                                                    clearIsScrollActiveOperation = Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                                        (Action)ClearIsScrollActive);

                                                    ClearIsScrollActiveOperationField.SetValue(this, clearIsScrollActiveOperation);
                                                }
                                            }
                                        }
                                    };
                                measureCachesOperation = Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                                                                measureCachesAction);
                                MeasureCachesOperationField.SetValue(this, measureCachesOperation);
                            }
                        }
                    }
                    else if (IsScrollActive)
                    {
                        DispatcherOperation clearIsScrollActiveOperation = ClearIsScrollActiveOperationField.GetValue(this);
                        if (clearIsScrollActiveOperation == null)
                        {
                            clearIsScrollActiveOperation = Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                (Action)ClearIsScrollActive);

                            ClearIsScrollActiveOperationField.SetValue(this, clearIsScrollActiveOperation);
                        }
                    }

                    NormalizeCacheLength(isHorizontal, viewport, ref cacheSize, ref cacheUnit);
                }
                else
                {
                    cacheSize = new VirtualizationCacheLength(
                        Double.PositiveInfinity,
                        IsViewportEmpty(isHorizontal, viewport) ?
                        0.0 :
                        Double.PositiveInfinity);
                    cacheUnit = VirtualizationCacheLengthUnit.Pixel;

                    ClearAsyncOperations();
                }
            }
            else if (virtualizationInfoProvider != null)
            {
                //
                // Adjust the viewport offset for a non scrolling panel to account for the HeaderSize
                // when virtualizing.
                //
                HierarchicalVirtualizationConstraints virtualizationConstraints = virtualizationInfoProvider.Constraints;
                viewport = virtualizationConstraints.Viewport;
                cacheSize = virtualizationConstraints.CacheLength;
                cacheUnit = virtualizationConstraints.CacheLengthUnit;
                scrollGeneration = virtualizationConstraints.ScrollGeneration;
                MeasureCaches = virtualizationInfoProvider.InBackgroundLayout;

                if (isVSP45Compat)
                {
                    AdjustNonScrollingViewportForHeader(virtualizationInfoProvider, ref viewport, ref cacheSize, ref cacheUnit);
                }
                else
                {
                    AdjustNonScrollingViewportForInset(isHorizontal, parentItem, parentItemStorageProvider, virtualizationInfoProvider, ref viewport, ref cacheSize, ref cacheUnit);

                    // The viewport position may be expressed in an old coordinate system
                    // relying on an old average container size.  Using that position would
                    // produce bad results;  for example the first step in Measure computes
                    // the first item that intersects the viewport - it uses the latest
                    // average container size, and hence would choose the wrong item
                    // (from user's point of view, the panel scrolls to a random place).
                    //      To work around this, ComputeEffectiveOffset stores a list
                    // of substitute offsets when the ave container size changes;
                    // this instructs this method to replace an old offset with a new
                    // one (appearing last) that's the equivalent in the current coordinate
                    // system.
                    //      This replacement stays in effect until the parent panel gives us
                    // an offset from a more recent coordinate change, after which older
                    // offsets won't appear again.   Or until a new scroll motion has started,
                    // as indicated by a scroll generation that exceeds the one in effect
                    // when the list was created.
                    DependencyObject container = virtualizationInfoProvider as DependencyObject;
                    EffectiveOffsetInformation effectiveOffsetInfo = EffectiveOffsetInformationField.GetValue(container);
                    if (effectiveOffsetInfo != null)
                    {
                        List<double> offsetList = effectiveOffsetInfo.OffsetList;
                        int index = -1;

                        // effective offsets only apply when the scroll generation matches
                        Debug.Assert(effectiveOffsetInfo.ScrollGeneration <= scrollGeneration,
                            "stored scroll generation exceeds current - this can't happen");
                        if (effectiveOffsetInfo.ScrollGeneration >= scrollGeneration)
                        {
                            // find the given offset on the list
                            double offset = isHorizontal ? viewport.X : viewport.Y;
                            for (int i = 0, n = offsetList.Count; i < n; ++i)
                            {
                                if (LayoutDoubleUtil.AreClose(offset, offsetList[i]))
                                {
                                    index = i;
                                    break;
                                }
                            }
                        }

                        if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                        {
                            object[] args = new object[offsetList.Count + 7];
                            args[0] = "gen";
                            args[1] = effectiveOffsetInfo.ScrollGeneration;
                            args[2] = virtualizationConstraints.ScrollGeneration;
                            args[3] = viewport.Location;
                            args[4] = "at";
                            args[5] = index;
                            args[6] = "in";
                            for (int i=0; i<offsetList.Count; ++i)
                            {
                                args[i+7] = offsetList[i];
                            }
                            ScrollTracer.Trace(this, ScrollTraceOp.UseSubstOffset,
                                args);
                        }

                        // if it appears, susbstitue the last offset
                        if (index >= 0)
                        {
                            if (isHorizontal)
                            {
                                viewport.X = offsetList[offsetList.Count-1];
                            }
                            else
                            {
                                viewport.Y = offsetList[offsetList.Count-1];
                            }

                            // and remove offsets before the matching one -
                            // they'll never be needed again
                            offsetList.RemoveRange(0, index);
                        }

                        // if the list is no longer needed, discard it
                        if (index < 0 || offsetList.Count <= 1)
                        {
                            EffectiveOffsetInformationField.ClearValue(container);
                        }
                    }
                }
            }
            else
            {
                scrollGeneration = 0;
                viewport = new Rect(0, 0, constraint.Width, constraint.Height);

                if (isHorizontal)
                {
                    viewport.Width = Double.PositiveInfinity;
                }
                else
                {
                    viewport.Height = Double.PositiveInfinity;
                }
            }

            // Adjust extendedViewport

            extendedViewport = _extendedViewport;

            if (isHorizontal)
            {
                extendedViewport.X += viewport.X - _viewport.X;
            }
            else
            {
                extendedViewport.Y += viewport.Y - _viewport.Y;
            }

            // Some work needs to wait for a MeasureCache pass.  Set the flag now.
            if (IsVirtualizing)
            {
                if (MeasureCaches)
                {
                    IsMeasureCachesPending = false;
                }
                else if (DoubleUtil.GreaterThan(cacheSize.CacheBeforeViewport, 0) ||
                        DoubleUtil.GreaterThan(cacheSize.CacheAfterViewport, 0))
                {
                    IsMeasureCachesPending = true;
                }
            }
        }

        private void ClearMeasureCachesState()
        {
            // discard a pending MeasureCaches operation
            DispatcherOperation measureCachesOperation = MeasureCachesOperationField.GetValue(this);
            if (measureCachesOperation != null)
            {
                measureCachesOperation.Abort();
                MeasureCachesOperationField.ClearValue(this);
            }

            // MeasureCaches is no longer pending
            IsMeasureCachesPending = false;

            // cancel any async cleanup (which depends on MeasureCaches)
            if (_cleanupOperation != null)
            {
                if (_cleanupOperation.Abort())
                {
                    _cleanupOperation = null;
                }
            }

            if (_cleanupDelay != null)
            {
                _cleanupDelay.Stop();
                _cleanupDelay = null;
            }
        }

        private void ClearIsScrollActive()
        {
            ClearIsScrollActiveOperationField.ClearValue(this);
            OffsetInformationField.ClearValue(this);
            _scrollData._bringIntoViewLeafContainer = null;
            IsScrollActive = false;

            if (!IsVSP45Compat)
            {
                // when the scroll is truly complete, record the real scroll offset
                // (this matters after a scroll-to-end, when the _offset
                // is infinite)
                _scrollData._offset = _scrollData._computedOffset;
            }
        }

        private void NormalizeCacheLength(
            bool isHorizontal,
            Rect viewport,
            ref VirtualizationCacheLength cacheLength,
            ref VirtualizationCacheLengthUnit cacheUnit)
        {
            if (cacheUnit == VirtualizationCacheLengthUnit.Page)
            {
                double factor = isHorizontal ? viewport.Width : viewport.Height;

                if (Double.IsPositiveInfinity(factor))
                {
                    cacheLength = new VirtualizationCacheLength(
                        0,
                        0);
                }
                else
                {
                    cacheLength = new VirtualizationCacheLength(
                        cacheLength.CacheBeforeViewport * factor,
                        cacheLength.CacheAfterViewport * factor);
                }

                cacheUnit = VirtualizationCacheLengthUnit.Pixel;
            }

            // if the viewport is empty in the scrolling direction, force the
            // cache to be empty also.   This avoids an infinite loop re- and
            // de-virtualizing the last item
            if (IsViewportEmpty(isHorizontal, viewport))
            {
                cacheLength = new VirtualizationCacheLength(0, 0);
            }
        }

        /// <summary>
        /// Extends the viewport to include the cacheSizeBeforeViewport and cacheSizeAfterViewport.
        /// </summary>
        private Rect ExtendViewport(
            IHierarchicalVirtualizationAndScrollInfo virtualizationInfoProvider,
            bool isHorizontal,
            Rect viewport,
            VirtualizationCacheLength cacheLength,
            VirtualizationCacheLengthUnit cacheUnit,
            Size stackPixelSizeInCacheBeforeViewport,
            Size stackLogicalSizeInCacheBeforeViewport,
            Size stackPixelSizeInCacheAfterViewport,
            Size stackLogicalSizeInCacheAfterViewport,
            Size stackPixelSize,
            Size stackLogicalSize,
            ref int itemsInExtendedViewportCount)
        {
            Debug.Assert(cacheUnit != VirtualizationCacheLengthUnit.Page, "Page cacheUnit is not expected here.");

            double pixelSize, pixelSizeBeforeViewport, pixelSizeAfterViewport;
            double logicalSize, logicalSizeBeforeViewport, logicalSizeAfterViewport;
            Rect extendedViewport = viewport;

            if (isHorizontal)
            {
                double approxSizeOfLogicalUnit = (DoubleUtil.GreaterThan(_previousStackPixelSizeInViewport.Width, 0.0) && DoubleUtil.GreaterThan(_previousStackLogicalSizeInViewport.Width, 0.0)) ?
                    _previousStackPixelSizeInViewport.Width / _previousStackLogicalSizeInViewport.Width : ScrollViewer._scrollLineDelta;

                pixelSize = stackPixelSize.Width;
                logicalSize = stackLogicalSize.Width;

                if (MeasureCaches)
                {
                    pixelSizeBeforeViewport = stackPixelSizeInCacheBeforeViewport.Width;
                    pixelSizeAfterViewport = stackPixelSizeInCacheAfterViewport.Width;
                    logicalSizeBeforeViewport = stackLogicalSizeInCacheBeforeViewport.Width;
                    logicalSizeAfterViewport = stackLogicalSizeInCacheAfterViewport.Width;
                }
                else
                {
                    pixelSizeBeforeViewport = (cacheUnit == VirtualizationCacheLengthUnit.Item) ? cacheLength.CacheBeforeViewport * approxSizeOfLogicalUnit : cacheLength.CacheBeforeViewport;
                    pixelSizeAfterViewport = (cacheUnit == VirtualizationCacheLengthUnit.Item) ? cacheLength.CacheAfterViewport * approxSizeOfLogicalUnit : cacheLength.CacheAfterViewport;
                    logicalSizeBeforeViewport = (cacheUnit == VirtualizationCacheLengthUnit.Item) ? cacheLength.CacheBeforeViewport : cacheLength.CacheBeforeViewport / approxSizeOfLogicalUnit;
                    logicalSizeAfterViewport = (cacheUnit == VirtualizationCacheLengthUnit.Item) ? cacheLength.CacheAfterViewport : cacheLength.CacheAfterViewport / approxSizeOfLogicalUnit;

                    if (IsPixelBased)
                    {
                        pixelSizeBeforeViewport = Math.Max(pixelSizeBeforeViewport, Math.Abs(_viewport.X - _extendedViewport.X));
                    }
                    else
                    {
                        logicalSizeBeforeViewport = Math.Max(logicalSizeBeforeViewport, Math.Abs(_viewport.X - _extendedViewport.X));
                    }
                }

                if (IsPixelBased)
                {
                    if (!IsScrolling && virtualizationInfoProvider != null &&
                        IsViewportEmpty(isHorizontal, extendedViewport) &&
                        DoubleUtil.GreaterThan(pixelSizeBeforeViewport, 0))
                    {
                        //
                        // If this is a GroupItem or a TreeViewItem that is completely above the viewport,
                        // then the CacheBeforeViewport allways designates the distance of the bottom of
                        // this panel from the top of the extendedViewport. Hence this computation for the offset.
                        //

                        extendedViewport.X = pixelSize - pixelSizeBeforeViewport;
                    }
                    else
                    {
                        extendedViewport.X -= pixelSizeBeforeViewport;
                    }

                    extendedViewport.Width += pixelSizeBeforeViewport + pixelSizeAfterViewport;

                    //
                    // Once again coerce the extended viewport dimensions to be within valid range.
                    //
                    if (IsScrolling)
                    {
                        if (DoubleUtil.LessThan(extendedViewport.X, 0.0))
                        {
                            extendedViewport.Width = Math.Max(extendedViewport.Width + extendedViewport.X, 0.0);
                            extendedViewport.X = 0.0;
                        }

                        if (DoubleUtil.GreaterThan(extendedViewport.X + extendedViewport.Width, _scrollData._extent.Width))
                        {
                            extendedViewport.Width = _scrollData._extent.Width - extendedViewport.X;
                        }
                    }
                }
                else
                {
                    if (!IsScrolling && virtualizationInfoProvider != null &&
                        IsViewportEmpty(isHorizontal, extendedViewport) &&
                        DoubleUtil.GreaterThan(pixelSizeBeforeViewport, 0))
                    {
                        //
                        // If this is a GroupItem or a TreeViewItem that is completely above the viewport,
                        // then the CacheBeforeViewport allways designates the distance of the bottom of
                        // this panel from the top of the extendedViewport. Hence this computation for the offset.
                        //

                        extendedViewport.X = logicalSize - logicalSizeBeforeViewport;
                    }
                    else
                    {
                        extendedViewport.X -= logicalSizeBeforeViewport;
                    }

                    extendedViewport.Width += pixelSizeBeforeViewport + pixelSizeAfterViewport;

                    if (IsScrolling)
                    {
                        if (DoubleUtil.LessThan(extendedViewport.X, 0.0))
                        {
                            extendedViewport.Width = Math.Max(extendedViewport.Width / approxSizeOfLogicalUnit + extendedViewport.X, 0.0) * approxSizeOfLogicalUnit;
                            extendedViewport.X = 0.0;
                        }

                        if (DoubleUtil.GreaterThan(extendedViewport.X + extendedViewport.Width / approxSizeOfLogicalUnit, _scrollData._extent.Width))
                        {
                            extendedViewport.Width = (_scrollData._extent.Width - extendedViewport.X) * approxSizeOfLogicalUnit;
                        }
                    }
                }
            }
            else
            {
                double approxSizeOfLogicalUnit = (DoubleUtil.GreaterThan(_previousStackPixelSizeInViewport.Height, 0.0) && DoubleUtil.GreaterThan(_previousStackLogicalSizeInViewport.Height, 0.0)) ?
                    _previousStackPixelSizeInViewport.Height / _previousStackLogicalSizeInViewport.Height : ScrollViewer._scrollLineDelta;

                pixelSize = stackPixelSize.Height;
                logicalSize = stackLogicalSize.Height;

                if (MeasureCaches)
                {
                    pixelSizeBeforeViewport = stackPixelSizeInCacheBeforeViewport.Height;
                    pixelSizeAfterViewport = stackPixelSizeInCacheAfterViewport.Height;
                    logicalSizeBeforeViewport = stackLogicalSizeInCacheBeforeViewport.Height;
                    logicalSizeAfterViewport = stackLogicalSizeInCacheAfterViewport.Height;
                }
                else
                {
                    pixelSizeBeforeViewport = (cacheUnit == VirtualizationCacheLengthUnit.Item) ? cacheLength.CacheBeforeViewport * approxSizeOfLogicalUnit : cacheLength.CacheBeforeViewport;
                    pixelSizeAfterViewport = (cacheUnit == VirtualizationCacheLengthUnit.Item) ? cacheLength.CacheAfterViewport * approxSizeOfLogicalUnit : cacheLength.CacheAfterViewport;
                    logicalSizeBeforeViewport = (cacheUnit == VirtualizationCacheLengthUnit.Item) ? cacheLength.CacheBeforeViewport : cacheLength.CacheBeforeViewport / approxSizeOfLogicalUnit;
                    logicalSizeAfterViewport = (cacheUnit == VirtualizationCacheLengthUnit.Item) ? cacheLength.CacheAfterViewport : cacheLength.CacheAfterViewport / approxSizeOfLogicalUnit;

                    if (IsPixelBased)
                    {
                        pixelSizeBeforeViewport = Math.Max(pixelSizeBeforeViewport, Math.Abs(_viewport.Y - _extendedViewport.Y));
                    }
                    else
                    {
                        logicalSizeBeforeViewport = Math.Max(logicalSizeBeforeViewport, Math.Abs(_viewport.Y - _extendedViewport.Y));
                    }
                }

                if (IsPixelBased)
                {
                    if (!IsScrolling && virtualizationInfoProvider != null &&
                        IsViewportEmpty(isHorizontal, extendedViewport) &&
                        DoubleUtil.GreaterThan(pixelSizeBeforeViewport, 0))
                    {
                        //
                        // If this is a GroupItem or a TreeViewItem that is completely above the viewport,
                        // then the CacheBeforeViewport allways designates the distance of the bottom of
                        // this panel from the top of the extendedViewport. Hence this computation for the offset.
                        //

                        extendedViewport.Y = pixelSize - pixelSizeBeforeViewport;
                    }
                    else
                    {
                        extendedViewport.Y -= pixelSizeBeforeViewport;
                    }

                    extendedViewport.Height += pixelSizeBeforeViewport + pixelSizeAfterViewport;

                    //
                    // Once again coerce the extended viewport dimensions to be within valid range.
                    //
                    if (IsScrolling)
                    {
                        if (DoubleUtil.LessThan(extendedViewport.Y, 0.0))
                        {
                            extendedViewport.Height = Math.Max(extendedViewport.Height + extendedViewport.Y, 0.0);
                            extendedViewport.Y = 0.0;
                        }

                        if (DoubleUtil.GreaterThan(extendedViewport.Y + extendedViewport.Height, _scrollData._extent.Height))
                        {
                            extendedViewport.Height = _scrollData._extent.Height - extendedViewport.Y;
                        }
                    }
                }
                else
                {
                    if (!IsScrolling && virtualizationInfoProvider != null &&
                        IsViewportEmpty(isHorizontal, extendedViewport) &&
                        DoubleUtil.GreaterThan(pixelSizeBeforeViewport, 0))
                    {
                        //
                        // If this is a GroupItem or a TreeViewItem that is completely above the viewport,
                        // then the CacheBeforeViewport allways designates the distance of the bottom of
                        // this panel from the top of the extendedViewport. Hence this computation for the offset.
                        //

                        extendedViewport.Y = logicalSize - logicalSizeBeforeViewport;
                    }
                    else
                    {
                        extendedViewport.Y -= logicalSizeBeforeViewport;
                    }

                    extendedViewport.Height += pixelSizeBeforeViewport + pixelSizeAfterViewport;

                    if (IsScrolling)
                    {
                        if (DoubleUtil.LessThan(extendedViewport.Y, 0.0))
                        {
                            extendedViewport.Height = Math.Max(extendedViewport.Height / approxSizeOfLogicalUnit + extendedViewport.Y, 0.0) * approxSizeOfLogicalUnit;
                            extendedViewport.Y = 0.0;
                        }

                        if (DoubleUtil.GreaterThan(extendedViewport.Y + extendedViewport.Height / approxSizeOfLogicalUnit, _scrollData._extent.Height))
                        {
                            extendedViewport.Height = (_scrollData._extent.Height - extendedViewport.Y) * approxSizeOfLogicalUnit;
                        }
                    }
                }
            }

            if (MeasureCaches)
            {
                itemsInExtendedViewportCount = _actualItemsInExtendedViewportCount;
            }
            else
            {
                double factor = Math.Max(1.0, isHorizontal ? extendedViewport.Width / viewport.Width : extendedViewport.Height / viewport.Height);
                int calcItemsInExtendedViewportCount = (int)Math.Ceiling(factor * _actualItemsInExtendedViewportCount);
                itemsInExtendedViewportCount = Math.Max(calcItemsInExtendedViewportCount, itemsInExtendedViewportCount);
            }

            return extendedViewport;
        }


        private void CoerceScrollingViewportOffset(ref Rect viewport, Size extent, bool isHorizontal)
        {
            Debug.Assert(IsScrolling, "The scrolling panel is the only one that should extend the viewport");

            if (!_scrollData.IsEmpty)
            {
                viewport.X = ScrollContentPresenter.CoerceOffset(viewport.X, extent.Width, viewport.Width);
                if (!IsPixelBased && isHorizontal && DoubleUtil.IsZero(viewport.Width) && DoubleUtil.AreClose(viewport.X, extent.Width))
                {
                    viewport.X = ScrollContentPresenter.CoerceOffset(viewport.X - 1, extent.Width, viewport.Width);
                }
            }

            if (!_scrollData.IsEmpty)
            {
                viewport.Y = ScrollContentPresenter.CoerceOffset(viewport.Y, extent.Height, viewport.Height);
                if (!IsPixelBased && !isHorizontal && DoubleUtil.IsZero(viewport.Height) && DoubleUtil.AreClose(viewport.Y, extent.Height))
                {
                    viewport.Y = ScrollContentPresenter.CoerceOffset(viewport.Y - 1, extent.Height, viewport.Height);
                }
            }
        }

        // *** DEAD CODE   Only called in VSP45 compat mode ***
        /// <summary>
        /// Adjusts viewport to accomodate the header.
        /// </summary>
        private void AdjustNonScrollingViewportForHeader(IHierarchicalVirtualizationAndScrollInfo virtualizationInfoProvider,
                                                            ref Rect viewport,
                                                            ref VirtualizationCacheLength cacheLength,
                                                            ref VirtualizationCacheLengthUnit cacheLengthUnit)
        {
            bool forHeader = true;
            AdjustNonScrollingViewport(virtualizationInfoProvider, ref viewport, ref cacheLength, ref cacheLengthUnit, forHeader);
        } // *** END DEAD CODE ***

        // *** DEAD CODE   Only call is from dead code in GetSizesForChild ***
        /// <summary>
        /// Adjusts viewport to accomodate the items.
        /// </summary>
        private void AdjustNonScrollingViewportForItems(IHierarchicalVirtualizationAndScrollInfo virtualizationInfoProvider,
                                                            ref Rect viewport,
                                                            ref VirtualizationCacheLength cacheLength,
                                                            ref VirtualizationCacheLengthUnit cacheLengthUnit)
        {
            bool forHeader = false;
            AdjustNonScrollingViewport(virtualizationInfoProvider, ref viewport, ref cacheLength, ref cacheLengthUnit, forHeader);
        }   // *** END DEAD CODE ***

        // *** DEAD CODE  Only called in VSP45 compat mode ***
        /// <summary>
        /// Adjusts viewport to accomodate the either the Header or ItemsPanel.
        /// </summary>
        private void AdjustNonScrollingViewport(
            IHierarchicalVirtualizationAndScrollInfo virtualizationInfoProvider,
            ref Rect viewport,
            ref VirtualizationCacheLength cacheLength,
            ref VirtualizationCacheLengthUnit cacheUnit,
            bool forHeader)     // *** forHeader is always true;  only call with false is from dead code in AdjustNonScrollingViewportForItems ***
        {
            Debug.Assert(virtualizationInfoProvider != null, "This method should only be invoked for a virtualizing owner");
            Debug.Assert(cacheUnit != VirtualizationCacheLengthUnit.Page, "Page after cache size is not expected here.");

            Rect parentViewport = viewport;
            double sizeAfterStartViewportEdge = 0;
            double sizeBeforeStartViewportEdge = 0;
            double sizeAfterEndViewportEdge = 0;
            double sizeBeforeEndViewportEdge = 0;
            double cacheBeforeSize = cacheLength.CacheBeforeViewport;
            double cacheAfterSize = cacheLength.CacheAfterViewport;

            HierarchicalVirtualizationHeaderDesiredSizes headerDesiredSizes = virtualizationInfoProvider.HeaderDesiredSizes;
            HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes = virtualizationInfoProvider.ItemDesiredSizes;

            Size pixelSize = forHeader ? headerDesiredSizes.PixelSize : itemDesiredSizes.PixelSize;
            Size logicalSize = forHeader ? headerDesiredSizes.LogicalSize : itemDesiredSizes.LogicalSize;

            RelativeHeaderPosition headerPosition = RelativeHeaderPosition.Top; // virtualizationInfoProvider.RelativeHeaderPosition;

            if ((forHeader && headerPosition == RelativeHeaderPosition.Left) ||
                (!forHeader && headerPosition == RelativeHeaderPosition.Right))
            {
                // ***DEAD CODE***   headerPosition is always Top
                //
                // Adjust the offset
                //

                viewport.X -= IsPixelBased ? pixelSize.Width : logicalSize.Width;

                if (DoubleUtil.GreaterThan(parentViewport.X, 0))
                {
                    //
                    // Viewport is after the start of this panel
                    //

                    if (IsPixelBased && DoubleUtil.GreaterThan(pixelSize.Width, parentViewport.X))
                    {
                        //
                        // Header straddles the start edge of the viewport
                        //

                        sizeAfterStartViewportEdge = pixelSize.Width - parentViewport.X;
                        sizeBeforeStartViewportEdge = pixelSize.Width - sizeAfterStartViewportEdge;

                        viewport.Width = Math.Max(viewport.Width - sizeAfterStartViewportEdge, 0);

                        if (cacheUnit == VirtualizationCacheLengthUnit.Pixel)
                        {
                            cacheBeforeSize = Math.Max(cacheBeforeSize - sizeBeforeStartViewportEdge, 0);
                        }
                        else
                        {
                            cacheBeforeSize = Math.Max(cacheBeforeSize - Math.Floor(logicalSize.Width * sizeBeforeStartViewportEdge / pixelSize.Width), 0);
                        }
                    }
                    else
                    {
                        //
                        // Header is completely before the start edge of the viewport. We do not need to
                        // adjust the cacheBefore size in this case because the cacheBefore is populated
                        // bottom up and we cant really be certain that the header will infact lie within the
                        // cacheBefore region.
                        //
                    }
                }
                else
                {
                    //
                    // Viewport is at or before this panel
                    //

                    if (DoubleUtil.GreaterThan(parentViewport.Width, 0))
                    {
                        if (DoubleUtil.GreaterThanOrClose(parentViewport.Width, pixelSize.Width))
                        {
                            //
                            // Header is completely within the viewport
                            //

                            viewport.Width = Math.Max(0, parentViewport.Width - pixelSize.Width);
                        }
                        else
                        {
                            //
                            // Header straddles the end edge of the viewport
                            //

                            sizeBeforeEndViewportEdge = parentViewport.Width;
                            sizeAfterEndViewportEdge = pixelSize.Width - sizeBeforeEndViewportEdge;

                            viewport.Width = 0;

                            if (cacheUnit == VirtualizationCacheLengthUnit.Pixel)
                            {
                                cacheAfterSize = Math.Max(cacheAfterSize - sizeAfterEndViewportEdge, 0);
                            }
                            else
                            {
                                cacheAfterSize = Math.Max(cacheAfterSize - Math.Floor(logicalSize.Width * sizeAfterEndViewportEdge / pixelSize.Width), 0);
                            }
                        }
                    }
                    else
                    {
                        //
                        // Header is completely after the end edge of the viewport
                        //

                        if (cacheUnit == VirtualizationCacheLengthUnit.Pixel)
                        {
                            cacheAfterSize = Math.Max(cacheAfterSize - pixelSize.Width, 0);
                        }
                        else
                        {
                            cacheAfterSize = Math.Max(cacheAfterSize - logicalSize.Width, 0);
                        }
                    }
                }
            }// *** End DEAD CODE ***
            else if ((forHeader && headerPosition == RelativeHeaderPosition.Top) ||
                    (!forHeader && headerPosition == RelativeHeaderPosition.Bottom))
            {   // *** This branch is always taken:  always forHeader==true, headerPosition==Top
                //
                // Adjust the offset
                //

                viewport.Y -= IsPixelBased ? pixelSize.Height : logicalSize.Height;

                if (DoubleUtil.GreaterThan(parentViewport.Y, 0))
                {
                    //
                    // Viewport is after the start of this panel
                    //

                    if (IsPixelBased && DoubleUtil.GreaterThan(pixelSize.Height, parentViewport.Y))
                    {
                        //
                        // Header straddles the start edge of the viewport
                        //

                        sizeAfterStartViewportEdge = pixelSize.Height - parentViewport.Y;
                        sizeBeforeStartViewportEdge = pixelSize.Height - sizeAfterStartViewportEdge;

                        viewport.Height = Math.Max(viewport.Height - sizeAfterStartViewportEdge, 0);

                        if (cacheUnit == VirtualizationCacheLengthUnit.Pixel)
                        {
                            cacheBeforeSize = Math.Max(cacheBeforeSize - sizeBeforeStartViewportEdge, 0);
                        }
                        else
                        {
                            cacheBeforeSize = Math.Max(cacheBeforeSize - Math.Floor(logicalSize.Height * sizeBeforeStartViewportEdge / pixelSize.Height), 0);
                        }
                    }
                    else
                    {
                        //
                        // Header is completely before the start edge of the viewport. We do not need to
                        // adjust the cacheBefore size in this case because the cacheBefore is populated
                        // bottom up and we cant really be certain that the header will infact lie within the
                        // cacheBefore region.
                        //
                    }
                }
                else
                {
                    //
                    // Viewport is at or before the start of this panel
                    //

                    if (DoubleUtil.GreaterThan(parentViewport.Height, 0))
                    {
                        if (DoubleUtil.GreaterThanOrClose(parentViewport.Height, pixelSize.Height))
                        {
                            //
                            // Header is completely within the viewport
                            //

                            viewport.Height = Math.Max(0, parentViewport.Height - pixelSize.Height);
                        }
                        else
                        {
                            //
                            // Header straddles the end edge of the viewport
                            //

                            sizeBeforeEndViewportEdge = parentViewport.Height;
                            sizeAfterEndViewportEdge = pixelSize.Height - sizeBeforeEndViewportEdge;

                            viewport.Height = 0;

                            if (cacheUnit == VirtualizationCacheLengthUnit.Pixel)
                            {
                                cacheAfterSize = Math.Max(cacheAfterSize - sizeAfterEndViewportEdge, 0);
                            }
                            else
                            {
                                cacheAfterSize = Math.Max(cacheAfterSize - Math.Floor(logicalSize.Height * sizeAfterEndViewportEdge / pixelSize.Height), 0);
                            }
                        }
                    }
                    else
                    {
                        //
                        // Header is completely after the end edge of the viewport
                        //

                        if (cacheUnit == VirtualizationCacheLengthUnit.Pixel)
                        {
                            cacheAfterSize = Math.Max(cacheAfterSize - pixelSize.Height, 0);
                        }
                        else
                        {
                            cacheAfterSize = Math.Max(cacheAfterSize - logicalSize.Height, 0);
                        }
                    }
                }
            }

            cacheLength = new VirtualizationCacheLength(cacheBeforeSize, cacheAfterSize);
        }// *** END DEAD CODE ***

        /// <summary>
        /// Adjusts viewport to accommodate the inset.
        /// </summary>
        private void AdjustNonScrollingViewportForInset(
            bool isHorizontal,
            object parentItem,
            IContainItemStorage parentItemStorageProvider,
            IHierarchicalVirtualizationAndScrollInfo virtualizationInfoProvider,
            ref Rect viewport,
            ref VirtualizationCacheLength cacheLength,
            ref VirtualizationCacheLengthUnit cacheUnit)
        {
            // Recall that a viewport-rect has location (X,Y) expressed in scroll units
            // (pixels or items), but size (Width,Height) expressed in pixels and
            // describing the available size - previous contributions have been
            // deducted.
            Rect parentViewport = viewport;
            FrameworkElement container = virtualizationInfoProvider as FrameworkElement;
            Thickness inset = GetItemsHostInsetForChild(virtualizationInfoProvider, parentItemStorageProvider, parentItem);
            bool isHeaderBeforeItems = IsHeaderBeforeItems(isHorizontal, container, ref inset);
            double cacheBeforeSize = cacheLength.CacheBeforeViewport;
            double cacheAfterSize = cacheLength.CacheAfterViewport;

            // offset the viewport by the inset, along the scrolling axis
            if (isHorizontal)
            {
                viewport.X -= IsPixelBased ? inset.Left : isHeaderBeforeItems ? 1 : 0;
            }
            else
            {
                viewport.Y -= IsPixelBased ? inset.Top : isHeaderBeforeItems ? 1 : 0;
            }

            if (isHorizontal)
            {
                if (DoubleUtil.GreaterThan(parentViewport.X, 0))
                {
                    // Viewport is after start of this container

                    if (DoubleUtil.GreaterThan(viewport.Width, 0))
                    {
                        // Viewport is not yet full - we're delving for the first
                        // container in the viewport.  We're moving forward, so
                        // do not contribute to cache-after (we won't know whether
                        // this container needs to contribute to cache-after until
                        // after measuring this panel).

                        if (IsPixelBased && DoubleUtil.GreaterThan(0, viewport.X))
                        {
                            // Viewport starts within the leading inset

                            // The inset is split in two pieces by the viewport leading edge.
                            // The first piece contributes to the cache-before;
                            // its width is parentViewport.X
                            if (cacheUnit == VirtualizationCacheLengthUnit.Pixel)
                            {
                                cacheBeforeSize = Math.Max(0, cacheBeforeSize - parentViewport.X);
                            }

                            // The second piece contributes to the viewport itself;
                            // its width is (inset.Left - parentViewport.X) = -viewport.X
                            viewport.Width = Math.Max(0, viewport.Width + viewport.X);
                        }
                        else
                        {
                            // Viewport starts after the leading inset.

                            // The contributions due to this container cannot be
                            // determined yet.  These are (a) leading inset to
                            // cache-before, (b) trailing inset to viewport and/or
                            // cache-after.
                        }
                    }
                    else
                    {
                        // viewport is full (and starts after this container).
                        // We're filling the cache-before back-to-front.

                        // The trailing inset contributes to cache-before
                        if (cacheUnit == VirtualizationCacheLengthUnit.Pixel)
                        {
                            cacheBeforeSize = Math.Max(0, cacheBeforeSize - inset.Right);
                        }
                        else if (!isHeaderBeforeItems)
                        {
                            cacheBeforeSize = Math.Max(0, cacheBeforeSize - 1);
                        }
                    }
                }

                else if (DoubleUtil.GreaterThan(viewport.Width, 0))
                {
                    // Viewport has available space (and starts before this container)
                    // We are filling the viewport front-to-back.

                    if (DoubleUtil.GreaterThanOrClose(viewport.Width, inset.Left))
                    {
                        // Viewport has room for the entire leading inset.

                        // Leading inset contributes to viewport
                        viewport.Width = Math.Max(0, viewport.Width - inset.Left);
                    }
                    else
                    {
                        // Leading inset exhausts the remaining available space.

                        // The inset is split into two pieces (by the viewport trailing edge).
                        // The second piece contributes to the cache-after;
                        // its width is (inset.Left - viewport.Width)
                        if (cacheUnit == VirtualizationCacheLengthUnit.Pixel)
                        {
                            cacheAfterSize = Math.Max(0, cacheAfterSize - (inset.Left - viewport.Width));
                        }

                        // The first piece contributes to the viewport itself;
                        // its width is viewport.Width (enough to decrease available width to zero)
                        viewport.Width = 0;
                    }
                }

                else
                {
                    // Viewport has no available space (and starts before this container).
                    // We are filling the cache-after front-to-back.

                    // The leading inset contributes to cache-after
                    if (cacheUnit == VirtualizationCacheLengthUnit.Pixel)
                    {
                        cacheAfterSize = Math.Max(0, cacheAfterSize - inset.Left);
                    }
                    else if (isHeaderBeforeItems)
                    {
                        cacheAfterSize = Math.Max(0, cacheAfterSize - 1);
                    }
                }
            }
            else    // scroll axis is vertical
            {
                if (DoubleUtil.GreaterThan(parentViewport.Y, 0))
                {
                    // Viewport is after start of this container

                    if (DoubleUtil.GreaterThan(viewport.Height, 0))
                    {
                        // Viewport is not yet full - we're delving for the first
                        // container in the viewport.  We're moving forward, so
                        // do not contribute to cache-after (we won't know whether
                        // this container needs to contribute to cache-after until
                        // after measuring this panel).

                        if (IsPixelBased && DoubleUtil.GreaterThan(0, viewport.Y))
                        {
                            // Viewport starts within the leading inset

                            // The inset is split in two pieces by the viewport leading edge.
                            // The first piece contributes to the cache-before;
                            // its height is parentViewport.Y
                            if (cacheUnit == VirtualizationCacheLengthUnit.Pixel)
                            {
                                cacheBeforeSize = Math.Max(0, cacheBeforeSize - parentViewport.Y);
                            }

                            // The second piece contributes to the viewport itself;
                            // its height is (inset.Top - parentViewport.Y) = -viewport.Y
                            viewport.Height = Math.Max(0, viewport.Height + viewport.Y);
                        }
                        else
                        {
                            // Viewport starts after the leading inset.

                            // The contributions due to this container cannot be
                            // determined yet.  These are (a) leading inset to
                            // cache-before, (b) trailing inset to viewport and/or
                            // cache-after.
                        }
                    }
                    else
                    {
                        // viewport is full (and starts after this container).
                        // We're filling the cache-before back-to-front.

                        // The trailing inset contributes to cache-before
                        if (cacheUnit == VirtualizationCacheLengthUnit.Pixel)
                        {
                            cacheBeforeSize = Math.Max(0, cacheBeforeSize - inset.Bottom);
                        }
                        else if (!isHeaderBeforeItems)
                        {
                            cacheBeforeSize = Math.Max(0, cacheBeforeSize - 1);
                        }
                    }
                }

                else if (DoubleUtil.GreaterThan(viewport.Height, 0))
                {
                    // Viewport has available space (and starts before this container)
                    // We are filling the viewport front-to-back.

                    if (DoubleUtil.GreaterThanOrClose(viewport.Height, inset.Top))
                    {
                        // Viewport has room for the entire leading inset.

                        // Leading inset contributes to viewport
                        viewport.Height = Math.Max(0, viewport.Height - inset.Top);
                    }
                    else
                    {
                        // Leading inset exhausts the remaining available space.

                        // The inset is split into two pieces (by the viewport trailing edge).
                        // The second piece contributes to the cache-after;
                        // its height is (inset.Top - viewport.Height)
                        if (cacheUnit == VirtualizationCacheLengthUnit.Pixel)
                        {
                            cacheAfterSize = Math.Max(0, cacheAfterSize - (inset.Top - viewport.Height));
                        }

                        // The first piece contributes to the viewport itself;
                        // its height is viewport.Height (enough to decrease available height to zero)
                        viewport.Height = 0;
                    }
                }

                else
                {
                    // Viewport has no available space (and starts before this container).
                    // We are filling the cache-after front-to-back.

                    // The leading inset contributes to cache-after
                    if (cacheUnit == VirtualizationCacheLengthUnit.Pixel)
                    {
                        cacheAfterSize = Math.Max(0, cacheAfterSize - inset.Top);
                    }
                    else if (isHeaderBeforeItems)
                    {
                        cacheAfterSize = Math.Max(0, cacheAfterSize - 1);
                    }
                }
            }

            // apply the cache adjustment
            cacheLength = new VirtualizationCacheLength(cacheBeforeSize, cacheAfterSize);
        }

        /// <summary>
        /// Returns the index of the first item visible (even partially) in the viewport.
        /// </summary>
        private void ComputeFirstItemInViewportIndexAndOffset(
            IList items,
            int itemCount,
            IContainItemStorage itemStorageProvider,
            Rect viewport,
            VirtualizationCacheLength cacheSize,
            bool isHorizontal,
            bool areContainersUniformlySized,
            double uniformOrAverageContainerSize,
            out double firstItemInViewportOffset,
            out double firstItemInViewportContainerSpan,
            out int firstItemInViewportIndex,
            out bool foundFirstItemInViewport)
        {
            firstItemInViewportOffset = 0.0;
            firstItemInViewportContainerSpan = 0.0;
            firstItemInViewportIndex = 0;
            foundFirstItemInViewport = false;

            if (IsViewportEmpty(isHorizontal, viewport))
            {
                if (DoubleUtil.GreaterThan(cacheSize.CacheBeforeViewport, 0.0))
                {
                    firstItemInViewportIndex = itemCount-1;
                    ComputeDistance(items, itemStorageProvider, isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize, 0, itemCount-1, out firstItemInViewportOffset);
                    foundFirstItemInViewport = true;
                }
                else
                {
                    //
                    // If the cacheSizeAfterViewport is also empty then we are merely
                    // here scouting to get a better measurement of this item.
                    //
                    firstItemInViewportIndex = 0;
                    firstItemInViewportOffset = 0;
                    foundFirstItemInViewport = DoubleUtil.GreaterThan(cacheSize.CacheAfterViewport, 0.0);
                }
            }
            else
            {
                //
                // Compute the span of this panel above the viewport. Note that if
                // the panel is below the viewport then this span is 0.0.
                //
                double spanBeforeViewport = Math.Max(isHorizontal ? viewport.X : viewport.Y, 0.0);

                if (areContainersUniformlySized)
                {
                    //
                    // This is an optimization for the case that all the children are of
                    // uniform dimension along the stacking axis. In this case the index
                    // and offset for the first item in the viewport is computed in constant time.
                    //
                    double childSize = uniformOrAverageContainerSize;
                    if (DoubleUtil.GreaterThan(childSize, 0))
                    {
                        firstItemInViewportIndex = (int)Math.Floor(spanBeforeViewport / childSize);
                        firstItemInViewportOffset = firstItemInViewportIndex * childSize;
                    }

                    firstItemInViewportContainerSpan = uniformOrAverageContainerSize;
                    foundFirstItemInViewport = (firstItemInViewportIndex < itemCount);
                    if (!foundFirstItemInViewport)
                    {
                        firstItemInViewportOffset = 0.0;
                        firstItemInViewportIndex = 0;
                    }
                }
                else
                {
                    if (DoubleUtil.AreClose(spanBeforeViewport, 0))
                    {
                        foundFirstItemInViewport = true;
                        firstItemInViewportOffset = 0.0;
                        firstItemInViewportIndex = 0;
                    }
                    else
                    {
                        Size containerSize;
                        double totalSpan = 0.0;      // total height or width in the stacking direction
                        double containerSpan = 0.0;
                        bool isVSP45Compat = IsVSP45Compat;

                        for (int i = 0; i < itemCount; i++)
                        {
                            object item = items[i];

                            GetContainerSizeForItem(itemStorageProvider, item, isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize, out containerSize);
                            containerSpan = isHorizontal ? containerSize.Width : containerSize.Height;
                            totalSpan += containerSpan;

                            // rounding errors while accumulating totalSpan can
                            // cause this loop to terminate one iteration too early,
                            // which leads to an infinite loop in recycling mode.
                            // Use LayoutDoubleUtil here (as in other calculations
                            // related to the viewport);  it is more tolerant of rounding error.
                            bool endsAfterViewport =
                                isVSP45Compat ? DoubleUtil.GreaterThan(totalSpan, spanBeforeViewport)
                                              : LayoutDoubleUtil.LessThan(spanBeforeViewport, totalSpan);

                            if (endsAfterViewport)
                            {
                                //
                                // This is the first item that starts before the  viewport but ends after it.
                                // It is thus the the first item in the viewport.
                                //
                                firstItemInViewportIndex = i;
                                firstItemInViewportOffset = totalSpan - containerSpan;
                                firstItemInViewportContainerSpan = containerSpan;
                                break;
                            }
                        }

                        foundFirstItemInViewport =
                                isVSP45Compat ? DoubleUtil.GreaterThan(totalSpan, spanBeforeViewport)
                                              : LayoutDoubleUtil.LessThan(spanBeforeViewport, totalSpan);
                        if (!foundFirstItemInViewport)
                        {
                            firstItemInViewportOffset = 0.0;
                            firstItemInViewportIndex = 0;
                        }
                    }
                }
            }

            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.CFIVIO,
                    viewport, foundFirstItemInViewport, firstItemInViewportIndex, firstItemInViewportOffset);
            }
        }


        /// <summary>
        /// After a measure pass, compute the effective offset - taking into account
        /// changes to the average container size discovered during measure.  This
        /// is the inverse of the previous method - ComputeFirstItemInViewportIndexAndOffset -
        /// in the sense that it produces an offset that, if fed back into that method (with
        /// the revised average container sizes), will yield the same result obtained
        /// in the current measure pass.  That is, the same item will be selected
        /// as the first item, and likewise for its sub-items.
        /// </summary>
        private double ComputeEffectiveOffset(
            ref Rect viewport,
            DependencyObject firstContainer,
            int itemIndex,
            double firstItemOffset,
            IList items,
            IContainItemStorage itemStorageProvider,
            IHierarchicalVirtualizationAndScrollInfo virtualizationInfoProvider,
            bool isHorizontal,
            bool areContainersUniformlySized,
            double uniformOrAverageContainerSize,
            long scrollGeneration)
        {
            if (firstContainer == null || IsViewportEmpty(isHorizontal, viewport))
            {
                return -1.0;    // undefined if no children in view
            }

            Debug.Assert(itemIndex < items.Count, "index out of range");
            double oldOffset = isHorizontal ? viewport.X : viewport.Y;
            double newOffset;

            // start with the effective offset of the first item
            ComputeDistance(items, itemStorageProvider, isHorizontal,
                areContainersUniformlySized, uniformOrAverageContainerSize,
                0, itemIndex, out newOffset);

            // add the offset within the first item
            newOffset += (oldOffset - firstItemOffset);

            // if the item's container has recorded a substitute offset,
            // adjust newOffset by the same amount.   This has the effect of
            // giving the child panel the offset it wants the next time this
            // panel measures the child.
            EffectiveOffsetInformation effectiveOffsetInformation = EffectiveOffsetInformationField.GetValue(firstContainer);
            List<Double> childOffsetList = (effectiveOffsetInformation != null) ? effectiveOffsetInformation.OffsetList : null;
            if (childOffsetList != null)
            {
                int count = childOffsetList.Count;
                newOffset += (childOffsetList[count-1] - childOffsetList[0]);
            }

            // record the answer (when not a top-level panel), for use by InitializeViewport.
            DependencyObject container = virtualizationInfoProvider as DependencyObject;
            if (container != null && !LayoutDoubleUtil.AreClose(oldOffset, newOffset))
            {
                // preserve the existing old offsets, if any, in case there are
                // multiple calls to measure this panel before the parent
                // adjusts to the change in our coordinate system, or calls from
                // a parent who set its own offset using an older offset from here
                effectiveOffsetInformation = EffectiveOffsetInformationField.GetValue(container);
                if (effectiveOffsetInformation == null || effectiveOffsetInformation.ScrollGeneration != scrollGeneration)
                {
                    effectiveOffsetInformation = new EffectiveOffsetInformation(scrollGeneration);
                    effectiveOffsetInformation.OffsetList.Add(oldOffset);
                }

                effectiveOffsetInformation.OffsetList.Add(newOffset);

                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                {
                    List<double> offsetList = effectiveOffsetInformation.OffsetList;
                    object[] args = new object[offsetList.Count + 2];
                    args[0] = scrollGeneration;
                    args[1] = ":";
                    for (int i = 0; i < offsetList.Count; ++i)
                    {
                        args[i + 2] = offsetList[i];
                    }
                    ScrollTracer.Trace(this, ScrollTraceOp.StoreSubstOffset,
                        args);
                }

                EffectiveOffsetInformationField.SetValue(container, effectiveOffsetInformation);
            }

            return newOffset;
        }

        /// <summary>
        /// To distinguish effective offsets set during one scrolling operation
        /// from those set in a different, each scrolling operation in the
        /// virtualizing direction increments the "scroll generation" counter.
        /// This counter is saved along with the effective offsets (see
        /// ComputeEffectiveOffsets), and compared with the current counter
        /// before applying the effective offset (see InitializeViewport).
        /// </summary>
        private void IncrementScrollGeneration()
        {
            // This will break if the counter ever rolls over the maximum.
            // If you do 1000 scroll operations per second, that will
            // happen in about 280 million years.
            ++_scrollData._scrollGeneration;
        }


        /// <summary>
        /// DesiredSize is normally computed by summing up the size of all items we've generated.  Pixel-based virtualization uses a 'full' desired size.
        /// This extends the given desired size beyond the visible items.  It will extend it by the items before or after the set of generated items.
        /// The given pivotIndex is the index of either the first or last item generated.
        /// </summary>
        private void ExtendPixelAndLogicalSizes(
            IList children,
            IList items,
            int itemCount,
            IContainItemStorage itemStorageProvider,
            bool areContainersUniformlySized,
            double uniformOrAverageContainerSize,
            double uniformOrAverageContainerPixelSize,
            ref Size stackPixelSize,
            ref Size stackLogicalSize,
            bool isHorizontal,
            int pivotIndex,
            int pivotChildIndex,
            int firstContainerInViewportIndex,
            bool before)
        {
            bool isVSP45Compat = IsVSP45Compat;
            Debug.Assert(IsVirtualizing, "We should only need to extend the viewport beyond the generated items when virtualizing");

            //
            // If we're virtualizing the sum of all generated containers is not the true desired size since not all containers were generated.
            // In the old items-based mode it didn't matter because only the scrolling panel could virtualize and scrollviewer doesn't *really*
            // care about desired size.
            //
            // In pixel-based mode we need to compute the same desired size as if we weren't virtualizing.
            //

            double distance, pixelDistance=0.0;
            if (before)
            {
                if (isVSP45Compat)
                {
                    ComputeDistance(items, itemStorageProvider, isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize, 0, pivotIndex, out distance);
                }
                else
                {
                    ComputeDistance(items, itemStorageProvider, isHorizontal, areContainersUniformlySized,
                        uniformOrAverageContainerSize,
                        uniformOrAverageContainerPixelSize,
                        0, pivotIndex, out distance, out pixelDistance);

                    // in item-scrolling mode, we need the pixel distance to the first container in the viewport
                    if (!IsPixelBased)
                    {
                        double unused, pixelDistanceToFirstContainer;
                        ComputeDistance(items, itemStorageProvider, isHorizontal, areContainersUniformlySized,
                            uniformOrAverageContainerSize,
                            uniformOrAverageContainerPixelSize,
                            pivotIndex, firstContainerInViewportIndex - pivotIndex,
                            out unused, out pixelDistanceToFirstContainer);
                        _pixelDistanceToViewport = pixelDistance + pixelDistanceToFirstContainer;
                        _pixelDistanceToFirstContainerInExtendedViewport = pixelDistance;
                    }
                }
            }
            else
            {
                if (isVSP45Compat)
                {
                    ComputeDistance(items, itemStorageProvider, isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize, pivotIndex, itemCount - pivotIndex, out distance);
                }
                else
                {
                    ComputeDistance(items, itemStorageProvider, isHorizontal, areContainersUniformlySized,
                        uniformOrAverageContainerSize,
                        uniformOrAverageContainerPixelSize,
                        pivotIndex, itemCount - pivotIndex, out distance, out pixelDistance);
                }
            }

            if (IsPixelBased)
            {
                if (isHorizontal)
                {
                    stackPixelSize.Width += distance;
                }
                else
                {
                    stackPixelSize.Height += distance;
                }
            }
            else
            {
                if (isHorizontal)
                {
                    stackLogicalSize.Width += distance;
                }
                else
                {
                    stackLogicalSize.Height += distance;
                }

                //
                // If there are containers beyond the extended
                // viewport then their sizes need to be added to
                // the stackPixelSize. This is only required in the
                // hierarchical cases to be able to arrange containers
                // beyond the extended viewport accurately.
                //
                if (isVSP45Compat)
                {
                    if (!IsScrolling)
                    {
                        int startIndex, count;

                        if (before)
                        {
                            startIndex = 0;
                            count = pivotChildIndex;
                        }
                        else
                        {
                            startIndex = pivotChildIndex;
                            count = children.Count;
                        }

                        for (int i=startIndex; i<count; i++)
                        {
                            Size childDesiredSize = ((UIElement)children[i]).DesiredSize;

                            if (isHorizontal)
                            {
                                stackPixelSize.Width += childDesiredSize.Width;
                            }
                            else
                            {
                                stackPixelSize.Height += childDesiredSize.Height;
                            }
                        }
                    }
                }
                else
                {
                    // 4.5 only accounted for realized items beyond the extended
                    // viewport.  The actual stack pixel size should depend on
                    // all items, otherwise containers can get arranged in the
                    // wrong place.
                    if (!IsScrolling)
                    {
                        if (isHorizontal)
                        {
                            stackPixelSize.Width += pixelDistance;
                        }
                        else
                        {
                            stackPixelSize.Height += pixelDistance;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This method is called upon to compute the pixel and logical
        /// distances for the itemCount beginning at the the start index.
        /// </summary>
        private void ComputeDistance(
            IList items,
            IContainItemStorage itemStorageProvider,
            bool isHorizontal,
            bool areContainersUniformlySized,
            double uniformOrAverageContainerSize,
            int startIndex,
            int itemCount,
            out double distance)
        {
            if (!(IsPixelBased || IsVSP45Compat))
            {
                double pixelDistance;
                ComputeDistance(items, itemStorageProvider, isHorizontal, areContainersUniformlySized,
                    uniformOrAverageContainerSize,
                    1.0 /*uniformOrAverageContainerPixelSize*/, // dummy - pixelDistance not used
                    startIndex, itemCount, out distance, out pixelDistance);
                return;
            }

            distance = 0.0;

            if (areContainersUniformlySized)
            {
                //
                // Performance optimization for the most general case where
                // all the children are of uniform size along the stacking direction.
                // Note that the computation of the range size is performed in constant time.
                //
                double childSize = uniformOrAverageContainerSize;
                if (isHorizontal)
                {
                    distance += childSize * itemCount;
                }
                else
                {
                    distance += childSize * itemCount;
                }
            }
            else
            {
                for (int i = startIndex; i < startIndex + itemCount; i++)
                {
                    object item = items[i];

                    Size containerSize;
                    GetContainerSizeForItem(itemStorageProvider, item, isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize,
                        out containerSize);

                    if (isHorizontal)
                    {
                        distance += containerSize.Width;
                    }
                    else
                    {
                        distance += containerSize.Height;
                    }
                }
            }
        }

        /// <summary>
        /// This method is called upon to compute the pixel and logical
        /// distances for the itemCount beginning at the the start index.
        /// </summary>
        private void ComputeDistance(
            IList items,
            IContainItemStorage itemStorageProvider,
            bool isHorizontal,
            bool areContainersUniformlySized,
            double uniformOrAverageContainerSize,
            double uniformOrAverageContainerPixelSize,
            int startIndex,
            int itemCount,
            out double distance,
            out double pixelDistance)
        {
            distance = 0.0;
            pixelDistance = 0.0;

            if (areContainersUniformlySized)
            {
                //
                // Performance optimization for the most general case where
                // all the children are of uniform size along the stacking direction.
                // Note that the computation of the range size is performed in constant time.
                //
                distance += uniformOrAverageContainerSize * itemCount;
                pixelDistance += uniformOrAverageContainerPixelSize * itemCount;
            }
            else
            {
                for (int i = startIndex; i < startIndex + itemCount; i++)
                {
                    object item = items[i];

                    Size containerSize;
                    Size containerPixelSize;
                    GetContainerSizeForItem(itemStorageProvider, item, isHorizontal,
                        areContainersUniformlySized,
                        uniformOrAverageContainerSize,
                        uniformOrAverageContainerPixelSize,
                        out containerSize,
                        out containerPixelSize);

                    if (isHorizontal)
                    {
                        distance += containerSize.Width;
                        pixelDistance += containerPixelSize.Width;
                    }
                    else
                    {
                        distance += containerSize.Height;
                        pixelDistance += containerPixelSize.Height;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the size of the container for a given item.  The size can come from the container or a lookup in the ItemStorage
        /// </summary>
        private void GetContainerSizeForItem(
            IContainItemStorage itemStorageProvider,
            object item,
            bool isHorizontal,
            bool areContainersUniformlySized,
            double uniformOrAverageContainerSize,
            out Size containerSize)
        {
            if (!IsVSP45Compat)
            {
                Size containerPixelSize;
                GetContainerSizeForItem(itemStorageProvider, item, isHorizontal,
                    areContainersUniformlySized,
                    uniformOrAverageContainerSize,
                    1.0 /*uniformOrAverageContainerPixelSize*/, // dummy - pixelSize not used
                    out containerSize,
                    out containerPixelSize);
                return;
            }

            containerSize = Size.Empty;

            if (areContainersUniformlySized)
            {
                //
                // This is a performance optimization for the case that the containers are unformly sized.
                //
                containerSize = new Size();
                double uniformSize = uniformOrAverageContainerSize;

                if (isHorizontal)
                {
                    containerSize.Width = uniformSize;
                    containerSize.Height = IsPixelBased ? DesiredSize.Height : 1;
                }
                else
                {
                    containerSize.Height = uniformSize;
                    containerSize.Width = IsPixelBased ? DesiredSize.Width : 1;
                }
            }
            else
            {
                //
                // We fetch the size of a container from the ItemStorage.
                // The size is cached if this item were previously realized.
                //
                object value = itemStorageProvider.ReadItemValue(item, ContainerSizeProperty);
                if (value != null)
                {
                    containerSize = (Size)value;
                }
                else
                {
                    //
                    // This item has never been realized previously. So use the average size.
                    //
                    containerSize = new Size();
                    double averageSize = uniformOrAverageContainerSize;

                    if (isHorizontal)
                    {
                        containerSize.Width = averageSize;
                        containerSize.Height = IsPixelBased ? DesiredSize.Height : 1;
                    }
                    else
                    {
                        containerSize.Height = averageSize;
                        containerSize.Width = IsPixelBased ? DesiredSize.Width : 1;
                    }
                }
            }

            Debug.Assert(!containerSize.IsEmpty, "We can't estimate an empty size");
        }

        /// <summary>
        /// Returns the size of the container for a given item.  The size can come from the container or a lookup in the ItemStorage
        /// </summary>
        private void GetContainerSizeForItem(
            IContainItemStorage itemStorageProvider,
            object item,
            bool isHorizontal,
            bool areContainersUniformlySized,
            double uniformOrAverageContainerSize,
            double uniformOrAverageContainerPixelSize,
            out Size containerSize,
            out Size containerPixelSize)
        {
            Debug.Assert(!IsVSP45Compat, "this method should not be called in VSP45Compat mode");
            containerSize = new Size();
            containerPixelSize = new Size();

            bool useAverageSize = areContainersUniformlySized;

            if (!areContainersUniformlySized)
            {
                //
                // We fetch the size of a container from the ItemStorage.
                // The size is cached if this item were previously realized.
                //
                if (IsPixelBased)
                {
                    object value = itemStorageProvider.ReadItemValue(item, ContainerSizeProperty);
                    if (value != null)
                    {
                        containerSize = (Size)value;
                        containerPixelSize = containerSize;
                    }
                    else
                    {
                        useAverageSize = true;
                    }
                }
                else
                {
                    object value = itemStorageProvider.ReadItemValue(item, ContainerSizeDualProperty);
                    if (value != null)
                    {
                        ContainerSizeDual cds = (ContainerSizeDual)value;
                        containerSize = cds.ItemSize;
                        containerPixelSize = cds.PixelSize;
                    }
                    else
                    {
                        useAverageSize = true;
                    }
                }
            }

            if (useAverageSize)
            {
                //
                // This is a performance optimization for the case that the containers are unformly sized.
                //
                if (isHorizontal)
                {
                    double pixelHeight = DesiredSize.Height;

                    containerSize.Width = uniformOrAverageContainerSize;
                    containerSize.Height = IsPixelBased ? pixelHeight : 1;

                    containerPixelSize.Width = uniformOrAverageContainerPixelSize;
                    containerPixelSize.Height = pixelHeight;
                }
                else
                {
                    double pixelWidth = DesiredSize.Width;

                    containerSize.Height = uniformOrAverageContainerSize;
                    containerSize.Width = IsPixelBased ? pixelWidth : 1;

                    containerPixelSize.Height = uniformOrAverageContainerPixelSize;
                    containerPixelSize.Width = pixelWidth;
                }
            }

            Debug.Assert(!containerSize.IsEmpty, "We can't estimate an empty size");
        }

        /// <summary>
        /// Sets the size of the container for a given item. If the items aren't uniformly sized store it in the ItemStorage.
        /// </summary>
        // *** DEAD CODE This method is only called in VSP45-compat mode ***
        private void SetContainerSizeForItem(
            IContainItemStorage itemStorageProvider,
            IContainItemStorage parentItemStorageProvider,
            object parentItem,
            object item,
            Size containerSize,
            bool isHorizontal,
            ref bool hasUniformOrAverageContainerSizeBeenSet,
            ref double uniformOrAverageContainerSize,
            ref bool areContainersUniformlySized)
        {
            if (!hasUniformOrAverageContainerSizeBeenSet)
            {
                // 4.5 used the wrong ItemStorageProvider for the UniformSize information
                if (IsVSP45Compat)
                {
                    parentItemStorageProvider = itemStorageProvider;
                }

                hasUniformOrAverageContainerSizeBeenSet = true;
                uniformOrAverageContainerSize = isHorizontal ? containerSize.Width : containerSize.Height;
                SetUniformOrAverageContainerSize(parentItemStorageProvider, parentItem, uniformOrAverageContainerSize, 1.0);
            }
            else if (areContainersUniformlySized)
            {
                //
                // if we come across a child whose DesiredSize is different from _uniformOrAverageContainerSize
                // Once AreContainersUniformlySized becomes false, dont set it back ever.
                //
                if (isHorizontal)
                {
                    areContainersUniformlySized = DoubleUtil.AreClose(containerSize.Width, uniformOrAverageContainerSize);
                }
                else
                {
                    areContainersUniformlySized = DoubleUtil.AreClose(containerSize.Height, uniformOrAverageContainerSize);
                }
            }

            //
            // Save off the child's desired size for later. The stored size is useful in hierarchical virtualization
            // scenarios (Eg. TreeView, Grouping) to compute the index of the first visible item in the viewport
            // and to Arrange children in their proper locations.
            //
            if (!areContainersUniformlySized)
            {
                itemStorageProvider.StoreItemValue(item, ContainerSizeProperty, containerSize);
            }
        }

        /// <summary>
        /// Sets the size of the container for a given item. If the items aren't uniformly sized store it in the ItemStorage.
        /// </summary>
        private void SetContainerSizeForItem(
            IContainItemStorage itemStorageProvider,
            IContainItemStorage parentItemStorageProvider,
            object parentItem,
            object item,
            Size containerSize,
            Size containerPixelSize,
            bool isHorizontal,
            bool hasVirtualizingChildren,
            ref bool hasUniformOrAverageContainerSizeBeenSet,
            ref double uniformOrAverageContainerSize,
            ref double uniformOrAverageContainerPixelSize,
            ref bool areContainersUniformlySized,
            ref bool hasAnyContainerSpanChanged)
        {
            if (!hasUniformOrAverageContainerSizeBeenSet)
            {
                hasUniformOrAverageContainerSizeBeenSet = true;
                uniformOrAverageContainerSize = isHorizontal ? containerSize.Width : containerSize.Height;
                uniformOrAverageContainerPixelSize = isHorizontal ? containerPixelSize.Width : containerPixelSize.Height;
                SetUniformOrAverageContainerSize(parentItemStorageProvider, parentItem, uniformOrAverageContainerSize, uniformOrAverageContainerPixelSize);
            }
            else if (areContainersUniformlySized)
            {
                //
                // if we come across a child whose DesiredSize is different from _uniformOrAverageContainerSize,
                // set AreContainersUniformlySized to false.
                // Once AreContainersUniformlySized becomes false, don't ever set it back to true.
                //

                // ignore the container pixel size if (a) pixel-scrolling or (b) no hierarchy.
                // (a) because the two sizes are the same.
                // (b) because the second size isn't used.   There's no need to go
                // into non-uniform mode just because the pixel heights are different,
                // and a good reason to avoid it:  it requires looping through all
                // containers to compute an average size, which breaks third-party's
                // attempts to do data virtualization
                bool ignoreContainerPixelSize = IsPixelBased || (IsScrolling && !hasVirtualizingChildren);

                if (isHorizontal)
                {
                    areContainersUniformlySized = DoubleUtil.AreClose(containerSize.Width, uniformOrAverageContainerSize)
                        && (ignoreContainerPixelSize || DoubleUtil.AreClose(containerPixelSize.Width, uniformOrAverageContainerPixelSize));
                }
                else
                {
                    areContainersUniformlySized = DoubleUtil.AreClose(containerSize.Height, uniformOrAverageContainerSize)
                        && (ignoreContainerPixelSize || DoubleUtil.AreClose(containerPixelSize.Height, uniformOrAverageContainerPixelSize));
                }
            }

            //
            // Save off the child's desired size for later. The stored size is useful in hierarchical virtualization
            // scenarios (Eg. TreeView, Grouping) to compute the index of the first visible item in the viewport
            // and to Arrange children in their proper locations.
            //
            if (!areContainersUniformlySized)
            {
                double oldSpan=0, newSpan=0;
                bool isTracing = (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this));

                if (IsPixelBased)
                {
                    object oldValue = itemStorageProvider.ReadItemValue(item, ContainerSizeProperty);
                    Size oldSize = (oldValue != null) ? (Size)oldValue : Size.Empty;

                    if (oldValue == null || containerSize != oldSize)
                    {
                        if (isTracing)
                        {
                            ItemContainerGenerator generator = (ItemContainerGenerator)Generator;
                            ScrollTracer.Trace(this, ScrollTraceOp.SetContainerSize,
                                generator.IndexFromContainer(generator.ContainerFromItem(item)),
                                oldSize, containerSize);
                        }

                        if (isHorizontal)
                        {
                            oldSpan = (oldValue != null) ? oldSize.Width : uniformOrAverageContainerSize;
                            newSpan = containerSize.Width;
                        }
                        else
                        {
                            oldSpan = (oldValue != null) ? oldSize.Height : uniformOrAverageContainerSize;
                            newSpan = containerSize.Height;
                        }
                    }

                    // for pixel-scrolling the two values are the same - store only one
                    itemStorageProvider.StoreItemValue(item, ContainerSizeProperty, containerSize);
                }
                else
                {
                    object oldValue = itemStorageProvider.ReadItemValue(item, ContainerSizeDualProperty);
                    ContainerSizeDual oldCSD = (oldValue != null) ? (ContainerSizeDual)oldValue
                        : new ContainerSizeDual(Size.Empty, Size.Empty);

                    if (oldValue == null || containerSize != oldCSD.ItemSize || containerPixelSize != oldCSD.PixelSize)
                    {
                        if (isTracing)
                        {
                            ItemContainerGenerator generator = (ItemContainerGenerator)Generator;
                            ScrollTracer.Trace(this, ScrollTraceOp.SetContainerSize,
                                generator.IndexFromContainer(generator.ContainerFromItem(item)),
                                oldCSD.ItemSize, containerSize,
                                oldCSD.PixelSize, containerPixelSize);
                        }

                        if (isHorizontal)
                        {
                            oldSpan = (oldValue != null) ? oldCSD.ItemSize.Width : uniformOrAverageContainerSize;;
                            newSpan = containerSize.Width;
                        }
                        else
                        {
                            oldSpan = (oldValue != null) ? oldCSD.ItemSize.Height : uniformOrAverageContainerSize;;
                            newSpan = containerSize.Height;
                        }
                    }

                    // for item-scrolling, store both values
                    ContainerSizeDual value =
                            new ContainerSizeDual(containerPixelSize, containerSize);
                    itemStorageProvider.StoreItemValue(item, ContainerSizeDualProperty, value);
                }

                // if the size changes (along the scrolling axis) during
                // measure, we will have to recompute offsets
                if (!LayoutDoubleUtil.AreClose(oldSpan, newSpan))
                {
                    hasAnyContainerSpanChanged = true;
                }
            }
        }

        private Thickness GetItemsHostInsetForChild(IHierarchicalVirtualizationAndScrollInfo virtualizationInfoProvider, IContainItemStorage parentItemStorageProvider=null, object parentItem=null)
        {
            // This method is called in two ways:
            // 1) Before this panel has been measured.
            //      Args:  parentItemStorageProvider is non-null.
            //      This is called from AdjustNonScrollingViewportForInset, to
            //      get the viewport into the panel's coordinates.  We don't yet
            //      know the real inset, but the parentItemStorageProvider may
            //      have a last-known estimate.
            // 2) After this panel has been measured.
            //      Args: parentItemStorageProvider is null.
            //      This is called while measuring or arranging an ancestor panel,
            //      who needs to know the inset for this panel.  In this case,
            //      the inset is already stored on the container, either by an
            //      earlier query of type 1, or during this panel's arrange.

            FrameworkElement container = virtualizationInfoProvider as FrameworkElement;
            Debug.Assert(parentItemStorageProvider != null || container != null,
                "Caller of GetItemsHostInsetForChild must provide either an ItemsStorageProvider or a container");

            // type 2 - get the value directly from the container
            if (parentItemStorageProvider == null)
            {
                return (Thickness)container.GetValue(ItemsHostInsetProperty);
            }

            // type 1 - get the last-known inset
            Thickness inset = new Thickness();
            object box = parentItemStorageProvider.ReadItemValue(parentItem, ItemsHostInsetProperty);
            if (box != null)
            {
                inset = (Thickness)box;
            }
            else if ((box = container.ReadLocalValue(ItemsHostInsetProperty)) != DependencyProperty.UnsetValue)
            {
                // recycled container - use the recycled value
                inset = (Thickness)box;
            }
            else
            {
                // first-time, use header desired size as a guess.  This is correct
                // for the default container templates.   Even better guess - include
                // the container's margin.
                HierarchicalVirtualizationHeaderDesiredSizes headerDesiredSizes = virtualizationInfoProvider.HeaderDesiredSizes;
                Thickness margin = container.Margin;
                inset.Top = headerDesiredSizes.PixelSize.Height + margin.Top;
                inset.Left = headerDesiredSizes.PixelSize.Width + margin.Left;

                // store the value, for use by later queries of type 1
                parentItemStorageProvider.StoreItemValue(parentItem, ItemsHostInsetProperty, inset);
            }

            // store the value, for use by later queries of type 2
            container.SetValue(ItemsHostInsetProperty, inset);

            return inset;
        }

        private void SetItemsHostInsetForChild(int index, UIElement child, IContainItemStorage itemStorageProvider, bool isHorizontal)
        {
            Debug.Assert(!IsVSP45Compat, "SetItemsHostInset should not be called in VSP45-compat mode");

            // this only applies to a hierarchical element with a visible ItemsHost
            bool isChildHorizontal = isHorizontal;
            IHierarchicalVirtualizationAndScrollInfo virtualizingChild = GetVirtualizingChild(child, ref isChildHorizontal);
            Panel itemsHost = (virtualizingChild == null) ? null : virtualizingChild.ItemsHost;
            if (itemsHost == null || !itemsHost.IsVisible)
                return;

            // get the transformation from child coords to itemsHost coords
            GeneralTransform transform = child.TransformToDescendant(itemsHost);
            if (transform == null)
                return;     // when transform is undefined, ItemsHost is effectively invisible

            // build a rect (in child coords) describing the child's extended frame
            FrameworkElement fe = virtualizingChild as FrameworkElement;
            Thickness margin = (fe == null) ? new Thickness() : fe.Margin;
            Rect childRect = new Rect(new Point(), child.DesiredSize);
            childRect.Offset(-margin.Left, -margin.Top);

            // transform to itemsHost coords
            Rect itemsRect = transform.TransformBounds(childRect);

            // compute the desired inset, avoiding catastrophic cancellation errors
            Size itemsSize = itemsHost.DesiredSize;
            double left = DoubleUtil.AreClose(0, itemsRect.Left) ? 0 : -itemsRect.Left;
            double top = DoubleUtil.AreClose(0, itemsRect.Top) ? 0 : -itemsRect.Top;
            double right = DoubleUtil.AreClose(itemsSize.Width, itemsRect.Right) ? 0 : itemsRect.Right-itemsSize.Width;
            double bottom = DoubleUtil.AreClose(itemsSize.Height, itemsRect.Bottom) ? 0 : itemsRect.Bottom-itemsSize.Height;
            Thickness inset = new Thickness(left, top, right, bottom);


            // get the item to use as the key into items storage
            object item = GetItemFromContainer(child);
            if (item == DependencyProperty.UnsetValue)
            {
                Debug.Assert(false, "SetInset should only be called for a container");
                return;
            }

            // see whether inset is changing
            object box = itemStorageProvider.ReadItemValue(item, ItemsHostInsetProperty);
            bool changed = (box == null);
            bool remeasure = changed;
            if (!changed)
            {
                Thickness oldInset = (Thickness)box;
                changed = !(    DoubleUtil.AreClose(oldInset.Left, inset.Left) &&
                                DoubleUtil.AreClose(oldInset.Top, inset.Top ) &&
                                DoubleUtil.AreClose(oldInset.Right, inset.Right) &&
                                DoubleUtil.AreClose(oldInset.Bottom, inset.Bottom) );

                // only changes along the scrolling axis require a remeasure.
                // use a less stringent "AreClose" test;  experiments show that the
                // trailing inset can change due to roundoff error by an amount
                // that is larger than the tolerance in DoubleUtil, but not large
                // enough to warrant an expensive remeasure.
                remeasure = changed &&
                    (   (isHorizontal  && !(AreInsetsClose(oldInset.Left, inset.Left) &&
                                            AreInsetsClose(oldInset.Right, inset.Right)))
                     || (!isHorizontal && !(AreInsetsClose(oldInset.Top, inset.Top) &&
                                            AreInsetsClose(oldInset.Bottom, inset.Bottom))) );
            }

            if (changed)
            {
                // store the new inset
                itemStorageProvider.StoreItemValue(item, ItemsHostInsetProperty, inset);
                child.SetValue(ItemsHostInsetProperty, inset);
            }

            if (remeasure)
            {
                // re-measure the scrolling panel
                ItemsControl scrollingItemsControl = GetScrollingItemsControl(child);
                Panel scrollingPanel = (scrollingItemsControl == null) ? null : scrollingItemsControl.ItemsHost;
                if (scrollingPanel != null)
                {
                    VirtualizingStackPanel vsp = scrollingPanel as VirtualizingStackPanel;
                    if (vsp != null)
                    {
                        vsp.AnchoredInvalidateMeasure();
                    }
                    else
                    {
                        scrollingPanel.InvalidateMeasure();
                    }
                }
            }
        }

        private static bool AreInsetsClose(double value1, double value2)
        {
            const double Tolerance = .001;  // relative error less than this is considered "close"

            //in case they are Infinities (then epsilon check does not work)
            if (value1 == value2) return true;

            // This computes (|value1-value2| / (|value1| + |value2|) <= Tolerance
            double eps = (Math.Abs(value1) + Math.Abs(value2)) * Tolerance;
            double delta = value1 - value2;
            return (-eps <= delta) && (eps >= delta);
        }

        // find the top-level ItemsControl (in a presentation of hierarchical data)
        // for the given mid-level container.
        // NOTE: This assumes that the only container types are TreeViewItem and
        // GroupItem.   This is true in 4.5.  If hierarchical virtualization
        // is ever extended to other container types, this method will need to change.
        private ItemsControl GetScrollingItemsControl(UIElement container)
        {
            if (container is TreeViewItem)
            {
                ItemsControl parent = ItemsControl.ItemsControlFromItemContainer(container);
                while (parent != null)
                {
                    TreeView tv = parent as TreeView;
                    if (tv != null)
                    {
                        return tv;
                    }

                    parent = ItemsControl.ItemsControlFromItemContainer(parent);
                }
            }
            else if (container is GroupItem)
            {
                DependencyObject parent = container;
                do
                {
                    parent = VisualTreeHelper.GetParent(parent);
                    ItemsControl parentItemsControl = parent as ItemsControl;
                    if (parentItemsControl != null)
                    {
                        return parentItemsControl;
                    }
                } while (parent != null);
            }
            else
            {
                string name = (container == null) ? "null" : container.GetType().Name;
                Debug.Assert(false, "Unexpected container type: " + name);
            }

            return null;
        }

        private object GetItemFromContainer(DependencyObject container)
        {
            return container.ReadLocalValue(System.Windows.Controls.ItemContainerGenerator.ItemForItemContainerProperty);
        }

        // true if "header" occurs before the ItemsHost panel in the stacking direction
        private bool IsHeaderBeforeItems(bool isHorizontal, FrameworkElement container, ref Thickness inset)
        {
            // don't depend on finding an element identified as the "header".  Instead
            // see whether there is more space before the ItemsHost than after, and
            // deem the "header" to belong to the larger area.   Don't count the
            // container margins.
            Thickness margin = (container == null) ? new Thickness() : container.Margin;
            if (isHorizontal)
            {
                return DoubleUtil.GreaterThanOrClose(inset.Left - margin.Left, inset.Right - margin.Right);
            }
            else
            {
                return DoubleUtil.GreaterThanOrClose(inset.Top - margin.Top, inset.Bottom - margin.Bottom);
            }
        }

        private bool IsEndOfCache(
            bool isHorizontal,
            double cacheSize,
            VirtualizationCacheLengthUnit cacheUnit,
            Size stackPixelSizeInCache,
            Size stackLogicalSizeInCache)
        {
            if (!MeasureCaches)
            {
                return true;
            }

            Debug.Assert(cacheUnit != VirtualizationCacheLengthUnit.Page, "Page cacheUnit is not expected here.");

            if (cacheUnit == VirtualizationCacheLengthUnit.Item)
            {
                if (isHorizontal)
                {
                    return DoubleUtil.GreaterThanOrClose(stackLogicalSizeInCache.Width, cacheSize);
                }
                else
                {
                    return DoubleUtil.GreaterThanOrClose(stackLogicalSizeInCache.Height, cacheSize);
                }
            }
            else if (cacheUnit == VirtualizationCacheLengthUnit.Pixel)
            {
                if (isHorizontal)
                {
                    return DoubleUtil.GreaterThanOrClose(stackPixelSizeInCache.Width, cacheSize);
                }
                else
                {
                    return DoubleUtil.GreaterThanOrClose(stackPixelSizeInCache.Height, cacheSize);
                }
            }
            return false;
        }

        private bool IsEndOfViewport(bool isHorizontal, Rect viewport, Size stackPixelSizeInViewport)
        {
            if (isHorizontal)
            {
                return DoubleUtil.GreaterThanOrClose(stackPixelSizeInViewport.Width, viewport.Width);
            }
            else
            {
                return DoubleUtil.GreaterThanOrClose(stackPixelSizeInViewport.Height, viewport.Height);
            }
        }

        private bool IsViewportEmpty(bool isHorizontal, Rect viewport)
        {
            if (isHorizontal)
            {
                return DoubleUtil.AreClose(viewport.Width, 0.0);
            }
            else
            {
                return DoubleUtil.AreClose(viewport.Height, 0.0);
            }
        }

        /// <summary>
        /// Called when to set a new viewport on the child when it is about to be measured.
        /// </summary>
        private void SetViewportForChild(
            bool isHorizontal,
            IContainItemStorage itemStorageProvider,
            bool areContainersUniformlySized,
            double uniformOrAverageContainerSize,
            bool mustDisableVirtualization,
            UIElement child,
            IHierarchicalVirtualizationAndScrollInfo virtualizingChild,
            object item,
            bool isBeforeFirstItem,
            bool isAfterFirstItem,
            double firstItemInViewportOffset,
            Rect parentViewport,
            VirtualizationCacheLength parentCacheSize,
            VirtualizationCacheLengthUnit parentCacheUnit,
            long scrollGeneration,
            Size stackPixelSize,
            Size stackPixelSizeInViewport,
            Size stackPixelSizeInCacheBeforeViewport,
            Size stackPixelSizeInCacheAfterViewport,
            Size stackLogicalSize,
            Size stackLogicalSizeInViewport,
            Size stackLogicalSizeInCacheBeforeViewport,
            Size stackLogicalSizeInCacheAfterViewport,
            out Rect childViewport,
            ref VirtualizationCacheLength childCacheSize,
            ref VirtualizationCacheLengthUnit childCacheUnit)
        {
            childViewport = parentViewport;

            //
            // Adjust viewport offset for the child by deducting
            // the dimensions of the previous siblings.
            //
            if (isHorizontal)
            {
                if (isBeforeFirstItem)
                {
                    Size containerSize;
                    GetContainerSizeForItem(itemStorageProvider, item, isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize, out containerSize);
                    childViewport.X = (IsPixelBased ? stackPixelSizeInCacheBeforeViewport.Width : stackLogicalSizeInCacheBeforeViewport.Width) + containerSize.Width;
                    childViewport.Width = 0.0;
                }
                else if (isAfterFirstItem)
                {
                    childViewport.X = Math.Min(childViewport.X, 0) -
                                      (IsPixelBased ? stackPixelSizeInViewport.Width + stackPixelSizeInCacheAfterViewport.Width :
                                                       stackLogicalSizeInViewport.Width + stackLogicalSizeInCacheAfterViewport.Width);
                    childViewport.Width = Math.Max(childViewport.Width - stackPixelSizeInViewport.Width, 0.0);
                }
                else
                {
                    childViewport.X -= firstItemInViewportOffset;
                    childViewport.Width = Math.Max(childViewport.Width - stackPixelSizeInViewport.Width, 0.0);
                }

                if (parentCacheUnit == VirtualizationCacheLengthUnit.Item)
                {
                    childCacheSize = new VirtualizationCacheLength(
                        isAfterFirstItem || DoubleUtil.LessThanOrClose(childViewport.X, 0.0) ?
                            0.0 :
                            Math.Max(parentCacheSize.CacheBeforeViewport - stackLogicalSizeInCacheBeforeViewport.Width, 0.0),
                        isBeforeFirstItem ?
                            0.0 :
                            Math.Max(parentCacheSize.CacheAfterViewport - stackLogicalSizeInCacheAfterViewport.Width, 0.0));
                    childCacheUnit = VirtualizationCacheLengthUnit.Item;
                }
                else if (parentCacheUnit == VirtualizationCacheLengthUnit.Pixel)
                {
                    childCacheSize = new VirtualizationCacheLength(
                        isAfterFirstItem || DoubleUtil.LessThanOrClose(childViewport.X, 0.0) ?
                            0.0 :
                            Math.Max(parentCacheSize.CacheBeforeViewport - stackPixelSizeInCacheBeforeViewport.Width, 0.0),
                        isBeforeFirstItem ?
                            0.0 :
                            Math.Max(parentCacheSize.CacheAfterViewport - stackPixelSizeInCacheAfterViewport.Width, 0.0));
                    childCacheUnit = VirtualizationCacheLengthUnit.Pixel;
                }
            }
            else
            {
                if (isBeforeFirstItem)
                {
                    Size containerSize;
                    GetContainerSizeForItem(itemStorageProvider, item, isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize, out containerSize);
                    childViewport.Y = (IsPixelBased ? stackPixelSizeInCacheBeforeViewport.Height : stackLogicalSizeInCacheBeforeViewport.Height) + containerSize.Height;
                    childViewport.Height = 0.0;
                }
                else if (isAfterFirstItem)
                {
                    childViewport.Y = Math.Min(childViewport.Y, 0) -
                                      (IsPixelBased ? stackPixelSizeInViewport.Height + stackPixelSizeInCacheAfterViewport.Height :
                                                       stackLogicalSizeInViewport.Height + stackLogicalSizeInCacheAfterViewport.Height);

                    childViewport.Height = Math.Max(childViewport.Height - stackPixelSizeInViewport.Height, 0.0);
                }
                else
                {
                    childViewport.Y -= firstItemInViewportOffset;
                    childViewport.Height = Math.Max(childViewport.Height - stackPixelSizeInViewport.Height, 0.0);
                }

                if (parentCacheUnit == VirtualizationCacheLengthUnit.Item)
                {
                    childCacheSize = new VirtualizationCacheLength(
                        isAfterFirstItem || DoubleUtil.LessThanOrClose(childViewport.Y, 0.0) ?
                            0.0 :
                            Math.Max(parentCacheSize.CacheBeforeViewport - stackLogicalSizeInCacheBeforeViewport.Height, 0.0),
                        isBeforeFirstItem ?
                            0.0 :
                            Math.Max(parentCacheSize.CacheAfterViewport - stackLogicalSizeInCacheAfterViewport.Height, 0.0));
                    childCacheUnit = VirtualizationCacheLengthUnit.Item;
                }
                else if (parentCacheUnit == VirtualizationCacheLengthUnit.Pixel)
                {
                    childCacheSize = new VirtualizationCacheLength(
                        isAfterFirstItem || DoubleUtil.LessThanOrClose(childViewport.Y, 0.0) ?
                            0.0 :
                            Math.Max(parentCacheSize.CacheBeforeViewport - stackPixelSizeInCacheBeforeViewport.Height, 0.0),
                        isBeforeFirstItem ?
                            0.0 :
                            Math.Max(parentCacheSize.CacheAfterViewport - stackPixelSizeInCacheAfterViewport.Height, 0.0));
                    childCacheUnit = VirtualizationCacheLengthUnit.Pixel;
                }
            }

            if (virtualizingChild != null)
            {
                HierarchicalVirtualizationConstraints constraints = new HierarchicalVirtualizationConstraints(
                    childCacheSize,
                    childCacheUnit,
                    childViewport);
                constraints.ScrollGeneration = scrollGeneration;
                virtualizingChild.Constraints = constraints;
                virtualizingChild.InBackgroundLayout = MeasureCaches;
                virtualizingChild.MustDisableVirtualization = mustDisableVirtualization;
            }

            if (child is IHierarchicalVirtualizationAndScrollInfo)
            {
                //
                // Ensure that measure is invalid through the items panel
                // of the child, so it can react to the new viewport.
                //
                InvalidateMeasureOnItemsHost((IHierarchicalVirtualizationAndScrollInfo)child);
            }
        }

        /// <summary>
        /// Called when a new viewport is set on the child and it is about to be measured.
        /// Invalidates Measure on all elements between child.ItemsHost and this panel.
        /// </summary>
        private void InvalidateMeasureOnItemsHost(IHierarchicalVirtualizationAndScrollInfo virtualizingChild)
        {
            Debug.Assert(virtualizingChild != null, "This method should only be invoked for a virtualizing child");

            Panel childItemsHost = virtualizingChild.ItemsHost;
            if (childItemsHost != null)
            {
                Helper.InvalidateMeasureOnPath(childItemsHost, this, true /*duringMeasure*/);

                if (!(childItemsHost is VirtualizingStackPanel))
                {
                    //
                    // For non-VSPs recurse a level deeper
                    //
                    IList children =  childItemsHost.InternalChildren;
                    for (int i=0; i<children.Count; i++)
                    {
                        IHierarchicalVirtualizationAndScrollInfo virtualizingGrandChild = children[i] as IHierarchicalVirtualizationAndScrollInfo;
                        if (virtualizingGrandChild != null)
                        {
                            InvalidateMeasureOnItemsHost(virtualizingGrandChild);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the size of the child in pixel and logical units and also identifies the part of the child visible in the viewport.
        /// </summary>
        // *** DEAD CODE This method is only called in VSP45-compat mode ***
        private void GetSizesForChild(
            bool isHorizontal,
            bool isChildHorizontal,
            bool isBeforeFirstItem,
            bool isAfterLastItem,
            IHierarchicalVirtualizationAndScrollInfo virtualizingChild,
            Size childDesiredSize,
            Rect childViewport,
            VirtualizationCacheLength childCacheSize,
            VirtualizationCacheLengthUnit childCacheUnit,
            out Size childPixelSize,
            out Size childPixelSizeInViewport,
            out Size childPixelSizeInCacheBeforeViewport,
            out Size childPixelSizeInCacheAfterViewport,
            out Size childLogicalSize,
            out Size childLogicalSizeInViewport,
            out Size childLogicalSizeInCacheBeforeViewport,
            out Size childLogicalSizeInCacheAfterViewport)
        {
            childPixelSize = new Size();
            childPixelSizeInViewport = new Size();
            childPixelSizeInCacheBeforeViewport = new Size();
            childPixelSizeInCacheAfterViewport = new Size();

            childLogicalSize = new Size();
            childLogicalSizeInViewport = new Size();
            childLogicalSizeInCacheBeforeViewport = new Size();
            childLogicalSizeInCacheAfterViewport = new Size();

            if (virtualizingChild != null)
            {
                RelativeHeaderPosition headerPosition = RelativeHeaderPosition.Top; // virtualizingChild.RelativeHeaderPosition;
                HierarchicalVirtualizationHeaderDesiredSizes headerDesiredSizes = virtualizingChild.HeaderDesiredSizes;
                HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes = virtualizingChild.ItemDesiredSizes;

                Size pixelHeaderSize = headerDesiredSizes.PixelSize;
                Size logicalHeaderSize = headerDesiredSizes.LogicalSize;

                childPixelSize = childDesiredSize;

                if (headerPosition == RelativeHeaderPosition.Top || headerPosition == RelativeHeaderPosition.Bottom)
                {
                    childLogicalSize.Height = itemDesiredSizes.LogicalSize.Height + logicalHeaderSize.Height;
                    childLogicalSize.Width = Math.Max(itemDesiredSizes.LogicalSize.Width, logicalHeaderSize.Width);
                }
                else // if (headerPosition == RelativeHeaderPosition.Left || headerPosition == RelativeHeaderPosition.Right)
                {   // *** DEAD CODE *** headerPosition is always Top
                    childLogicalSize.Width = itemDesiredSizes.LogicalSize.Width + logicalHeaderSize.Width;
                    childLogicalSize.Height = Math.Max(itemDesiredSizes.LogicalSize.Height, logicalHeaderSize.Height);
                }   // *** END DEAD CODE ***

                if (IsPixelBased &&
                    ((isHorizontal && DoubleUtil.AreClose(itemDesiredSizes.PixelSize.Width, itemDesiredSizes.PixelSizeInViewport.Width)) ||
                    (!isHorizontal && DoubleUtil.AreClose(itemDesiredSizes.PixelSize.Height, itemDesiredSizes.PixelSizeInViewport.Height))))
                {
                    Rect childItemsViewport = childViewport;

                    if (headerPosition == RelativeHeaderPosition.Top || headerPosition == RelativeHeaderPosition.Left)
                    {
                        VirtualizationCacheLength childItemsCacheSize = childCacheSize;
                        VirtualizationCacheLengthUnit childItemsCacheUnit = childCacheUnit;

                        AdjustNonScrollingViewportForHeader(virtualizingChild, ref childItemsViewport, ref childItemsCacheSize, ref childItemsCacheUnit);
                    }

                    GetSizesForChildIntersectingTheViewport(
                        isHorizontal,
                        isChildHorizontal,
                        itemDesiredSizes.PixelSizeInViewport,
                        itemDesiredSizes.LogicalSizeInViewport,
                        childItemsViewport,
                        ref childPixelSizeInViewport,
                        ref childLogicalSizeInViewport,
                        ref childPixelSizeInCacheBeforeViewport,
                        ref childLogicalSizeInCacheBeforeViewport,
                        ref childPixelSizeInCacheAfterViewport,
                        ref childLogicalSizeInCacheAfterViewport);
                }
                else
                {
                    StackSizes(isHorizontal, ref childPixelSizeInViewport, itemDesiredSizes.PixelSizeInViewport);
                    StackSizes(isHorizontal, ref childLogicalSizeInViewport, itemDesiredSizes.LogicalSizeInViewport);
                }

                if (isChildHorizontal == isHorizontal)
                {
                    StackSizes(isHorizontal, ref childPixelSizeInCacheBeforeViewport, itemDesiredSizes.PixelSizeBeforeViewport);
                    StackSizes(isHorizontal, ref childLogicalSizeInCacheBeforeViewport, itemDesiredSizes.LogicalSizeBeforeViewport);
                    StackSizes(isHorizontal, ref childPixelSizeInCacheAfterViewport, itemDesiredSizes.PixelSizeAfterViewport);
                    StackSizes(isHorizontal, ref childLogicalSizeInCacheAfterViewport, itemDesiredSizes.LogicalSizeAfterViewport);
                }

                Rect childHeaderViewport = childViewport;
                Size childHeaderPixelSizeInViewport = new Size();
                Size childHeaderLogicalSizeInViewport = new Size();
                Size childHeaderPixelSizeInCacheBeforeViewport = new Size();
                Size childHeaderLogicalSizeInCacheBeforeViewport = new Size();
                Size childHeaderPixelSizeInCacheAfterViewport = new Size();
                Size childHeaderLogicalSizeInCacheAfterViewport = new Size();
                bool isChildHeaderHorizontal = (headerPosition == RelativeHeaderPosition.Left || headerPosition == RelativeHeaderPosition.Right);

                if (headerPosition == RelativeHeaderPosition.Bottom || headerPosition == RelativeHeaderPosition.Right)
                {   // *** DEAD CODE *** headerPosition is always Top
                    VirtualizationCacheLength childHeaderCacheSize = childCacheSize;
                    VirtualizationCacheLengthUnit childHeaderCacheUnit = childCacheUnit;

                    AdjustNonScrollingViewportForItems(virtualizingChild, ref childHeaderViewport, ref childHeaderCacheSize, ref childHeaderCacheUnit);
                }   // *** END DEAD CODE ***

                if (isBeforeFirstItem)
                {
                    childHeaderPixelSizeInCacheBeforeViewport = pixelHeaderSize;
                    childHeaderLogicalSizeInCacheBeforeViewport = logicalHeaderSize;
                }
                else if (isAfterLastItem)
                {
                    childHeaderPixelSizeInCacheAfterViewport = pixelHeaderSize;
                    childHeaderLogicalSizeInCacheAfterViewport = logicalHeaderSize;
                }
                else
                {
                    GetSizesForChildIntersectingTheViewport(
                        isHorizontal,
                        isChildHorizontal,
                        pixelHeaderSize,
                        logicalHeaderSize,
                        childHeaderViewport,
                        ref childHeaderPixelSizeInViewport,
                        ref childHeaderLogicalSizeInViewport,
                        ref childHeaderPixelSizeInCacheBeforeViewport,
                        ref childHeaderLogicalSizeInCacheBeforeViewport,
                        ref childHeaderPixelSizeInCacheAfterViewport,
                        ref childHeaderLogicalSizeInCacheAfterViewport);
                }

                // *** isChildHeaderHorizontal is always false *** //
                StackSizes(isChildHeaderHorizontal, ref childPixelSizeInViewport, childHeaderPixelSizeInViewport);
                StackSizes(isChildHeaderHorizontal, ref childLogicalSizeInViewport, childHeaderLogicalSizeInViewport);
                StackSizes(isChildHeaderHorizontal, ref childPixelSizeInCacheBeforeViewport, childHeaderPixelSizeInCacheBeforeViewport);
                StackSizes(isChildHeaderHorizontal, ref childLogicalSizeInCacheBeforeViewport, childHeaderLogicalSizeInCacheBeforeViewport);
                StackSizes(isChildHeaderHorizontal, ref childPixelSizeInCacheAfterViewport, childHeaderPixelSizeInCacheAfterViewport);
                StackSizes(isChildHeaderHorizontal, ref childLogicalSizeInCacheAfterViewport, childHeaderLogicalSizeInCacheAfterViewport);
            }
            else
            {
                childPixelSize = childDesiredSize;
                childLogicalSize = new Size(DoubleUtil.GreaterThan(childPixelSize.Width, 0) ? 1 : 0,
                                            DoubleUtil.GreaterThan(childPixelSize.Height, 0) ? 1 : 0);

                if (isBeforeFirstItem)
                {
                    childPixelSizeInCacheBeforeViewport = childDesiredSize;
                    childLogicalSizeInCacheBeforeViewport = new Size(DoubleUtil.GreaterThan(childPixelSizeInCacheBeforeViewport.Width, 0) ? 1 : 0,
                                                                     DoubleUtil.GreaterThan(childPixelSizeInCacheBeforeViewport.Height, 0) ? 1 : 0);
                }
                else if (isAfterLastItem)
                {
                    childPixelSizeInCacheAfterViewport = childDesiredSize;
                    childLogicalSizeInCacheAfterViewport = new Size(DoubleUtil.GreaterThan(childPixelSizeInCacheAfterViewport.Width, 0) ? 1 : 0,
                                                                    DoubleUtil.GreaterThan(childPixelSizeInCacheAfterViewport.Height, 0) ? 1 : 0);
                }
                else
                {
                    GetSizesForChildIntersectingTheViewport(
                        isHorizontal,
                        isHorizontal,
                        childPixelSize,
                        childLogicalSize,
                        childViewport,
                        ref childPixelSizeInViewport,
                        ref childLogicalSizeInViewport,
                        ref childPixelSizeInCacheBeforeViewport,
                        ref childLogicalSizeInCacheBeforeViewport,
                        ref childPixelSizeInCacheAfterViewport,
                        ref childLogicalSizeInCacheAfterViewport);
                }
            }
        }// *** End DEAD CODE ***

        private void GetSizesForChildWithInset(
            bool isHorizontal,
            bool isChildHorizontal,
            bool isBeforeFirstItem,
            bool isAfterLastItem,
            IHierarchicalVirtualizationAndScrollInfo virtualizingChild,
            Size childDesiredSize,
            Rect childViewport,
            VirtualizationCacheLength childCacheSize,
            VirtualizationCacheLengthUnit childCacheUnit,
            out Size childPixelSize,
            out Size childPixelSizeInViewport,
            out Size childPixelSizeInCacheBeforeViewport,
            out Size childPixelSizeInCacheAfterViewport,
            out Size childLogicalSize,
            out Size childLogicalSizeInViewport,
            out Size childLogicalSizeInCacheBeforeViewport,
            out Size childLogicalSizeInCacheAfterViewport)
        {
            // set childPixelSize to childDesiredSize directly.   Ideally this should
            // be the same as adding the contributions from the items panel and
            // the front and back insets, as indicated by commented-out lines below,
            // but this isn't true when the inset is merely an estimate (i.e.
            // before the child has been arranged) - contributions from margins,
            // border thickness, etc. between the items panel and the virtualizingChild
            // are not yet accounted for.  The childDesiredSize is more accurate,
            // so we use that.
            childPixelSize = childDesiredSize;
            childPixelSizeInViewport = new Size();
            childPixelSizeInCacheBeforeViewport = new Size();
            childPixelSizeInCacheAfterViewport = new Size();

            childLogicalSize = new Size();
            childLogicalSizeInViewport = new Size();
            childLogicalSizeInCacheBeforeViewport = new Size();
            childLogicalSizeInCacheAfterViewport = new Size();

            HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes =
                (virtualizingChild != null) ? virtualizingChild.ItemDesiredSizes
                                            : new HierarchicalVirtualizationItemDesiredSizes();

            // the interesting case is when there is nested virtualization.  It's not
            // enough to test whether virtualizingChild is non-null;  the child
            // may not have done any virtualization (eg. because its ItemsHost
            // is not a VSP).  Instead, test whether its ItemsHost recorded a
            // nonzero extent.
            if ((!isHorizontal &&  (itemDesiredSizes.PixelSize.Height > 0 ||
                                    itemDesiredSizes.LogicalSize.Height > 0)) ||
                ( isHorizontal &&  (itemDesiredSizes.PixelSize.Width > 0 ||
                                    itemDesiredSizes.LogicalSize.Width > 0)))
            {
                // itemDesiredSizes gives the contribution from the nested panel.
              //StackSizes(isHorizontal, ref childPixelSize, itemDesiredSizes.PixelSize); (omitted - see comment at initialization)
                StackSizes(isHorizontal, ref childPixelSizeInCacheBeforeViewport, itemDesiredSizes.PixelSizeBeforeViewport);
                StackSizes(isHorizontal, ref childPixelSizeInViewport, itemDesiredSizes.PixelSizeInViewport);
                StackSizes(isHorizontal, ref childPixelSizeInCacheAfterViewport, itemDesiredSizes.PixelSizeAfterViewport);

                StackSizes(isHorizontal, ref childLogicalSize, itemDesiredSizes.LogicalSize);
                StackSizes(isHorizontal, ref childLogicalSizeInCacheBeforeViewport, itemDesiredSizes.LogicalSizeBeforeViewport);
                StackSizes(isHorizontal, ref childLogicalSizeInViewport, itemDesiredSizes.LogicalSizeInViewport);
                StackSizes(isHorizontal, ref childLogicalSizeInCacheAfterViewport, itemDesiredSizes.LogicalSizeAfterViewport);

                // Now add the contributions from the inset.  First decide whether
                // the child's item belongs to the front or back inset
                Thickness inset = GetItemsHostInsetForChild(virtualizingChild);
                bool isHeaderBeforeItems = IsHeaderBeforeItems(isHorizontal, virtualizingChild as FrameworkElement, ref inset);

                // Add contributions from the front inset.
                Size frontPixelSize = isHorizontal ? new Size(Math.Max(inset.Left,0), childDesiredSize.Height)
                                                   : new Size(childDesiredSize.Width, Math.Max(inset.Top, 0));
                Size frontLogicalSize = isHeaderBeforeItems ? new Size(1,1) : new Size(0,0);

              //StackSizes(isHorizontal, ref childPixelSize, frontPixelSize); (omitted - see comment at initialization)
                StackSizes(isHorizontal, ref childLogicalSize, frontLogicalSize);
                GetSizesForChildIntersectingTheViewport(isHorizontal, isChildHorizontal,
                    frontPixelSize, frontLogicalSize,
                    childViewport,
                    ref childPixelSizeInViewport, ref childLogicalSizeInViewport,
                    ref childPixelSizeInCacheBeforeViewport, ref childLogicalSizeInCacheBeforeViewport,
                    ref childPixelSizeInCacheAfterViewport, ref childLogicalSizeInCacheAfterViewport);

                // Add contributions from the back inset.
                Size backPixelSize = isHorizontal ? new Size(Math.Max(inset.Right,0), childDesiredSize.Height)
                                                   : new Size(childDesiredSize.Width, Math.Max(inset.Bottom,0));
                Size backLogicalSize = isHeaderBeforeItems ? new Size(0,0) : new Size(1,1);

              //StackSizes(isHorizontal, ref childPixelSize, backPixelSize); (omitted - see comment at initialization)
                StackSizes(isHorizontal, ref childLogicalSize, backLogicalSize);

                // before calling the helper, adjust the viewport to account for
                // the contributions we've just made
                Rect adjustedChildViewport = childViewport;
                if (isHorizontal)
                {
                    adjustedChildViewport.X -= IsPixelBased ? frontPixelSize.Width + itemDesiredSizes.PixelSize.Width
                                                            : frontLogicalSize.Width + itemDesiredSizes.LogicalSize.Width;
                    adjustedChildViewport.Width = Math.Max(0, adjustedChildViewport.Width - childPixelSizeInViewport.Width);
                }
                else
                {
                    adjustedChildViewport.Y -= IsPixelBased ? frontPixelSize.Height + itemDesiredSizes.PixelSize.Height
                                                            : frontLogicalSize.Height + itemDesiredSizes.LogicalSize.Height;
                    adjustedChildViewport.Height = Math.Max(0, adjustedChildViewport.Height - childPixelSizeInViewport.Height);
                }
                GetSizesForChildIntersectingTheViewport(isHorizontal, isChildHorizontal,
                    backPixelSize, backLogicalSize,
                    adjustedChildViewport,
                    ref childPixelSizeInViewport, ref childLogicalSizeInViewport,
                    ref childPixelSizeInCacheBeforeViewport, ref childLogicalSizeInCacheBeforeViewport,
                    ref childPixelSizeInCacheAfterViewport, ref childLogicalSizeInCacheAfterViewport);
            }
            else
            {
                // there was no nested virtualization.  The only contributions come
                // directly from the given child.

              //childPixelSize = childDesiredSize; (already done at initialization)
                childLogicalSize = new Size(1, 1);

                if (isBeforeFirstItem)
                {
                    childPixelSizeInCacheBeforeViewport = childDesiredSize;
                    childLogicalSizeInCacheBeforeViewport = new Size(DoubleUtil.GreaterThan(childPixelSizeInCacheBeforeViewport.Width, 0) ? 1 : 0,
                                                                     DoubleUtil.GreaterThan(childPixelSizeInCacheBeforeViewport.Height, 0) ? 1 : 0);
                }
                else if (isAfterLastItem)
                {
                    childPixelSizeInCacheAfterViewport = childDesiredSize;
                    childLogicalSizeInCacheAfterViewport = new Size(DoubleUtil.GreaterThan(childPixelSizeInCacheAfterViewport.Width, 0) ? 1 : 0,
                                                                    DoubleUtil.GreaterThan(childPixelSizeInCacheAfterViewport.Height, 0) ? 1 : 0);
                }
                else
                {
                    GetSizesForChildIntersectingTheViewport(
                        isHorizontal,
                        isChildHorizontal,
                        childPixelSize,
                        childLogicalSize,
                        childViewport,
                        ref childPixelSizeInViewport,
                        ref childLogicalSizeInViewport,
                        ref childPixelSizeInCacheBeforeViewport,
                        ref childLogicalSizeInCacheBeforeViewport,
                        ref childPixelSizeInCacheAfterViewport,
                        ref childLogicalSizeInCacheAfterViewport);
                }
            }
        }

        private void GetSizesForChildIntersectingTheViewport(
            bool isHorizontal,
            bool childIsHorizontal,
            Size childPixelSize,
            Size childLogicalSize,
            Rect childViewport,
            ref Size childPixelSizeInViewport,
            ref Size childLogicalSizeInViewport,
            ref Size childPixelSizeInCacheBeforeViewport,
            ref Size childLogicalSizeInCacheBeforeViewport,
            ref Size childPixelSizeInCacheAfterViewport,
            ref Size childLogicalSizeInCacheAfterViewport)
        {
            bool isVSP45Compat = IsVSP45Compat;
            double pixelSizeInViewport = 0.0, logicalSizeInViewport = 0.0;
            double pixelSizeBeforeViewport = 0.0, logicalSizeBeforeViewport = 0.0;
            double pixelSizeAfterViewport = 0.0, logicalSizeAfterViewport = 0.0;

            if (isHorizontal)
            {
                //
                // Split the child's sizes into portions before, after and within the viewport
                //

                if (IsPixelBased)
                {
                    if (childIsHorizontal != isHorizontal)
                    {
                        //
                        // If the child is beyond the viewport in the opposite orientation to this panel then we musn't count that child in.
                        //
                        if (DoubleUtil.GreaterThanOrClose(childViewport.Y, childPixelSize.Height) ||
                            DoubleUtil.AreClose(childViewport.Height, 0.0))
                        {
                            return;
                        }
                    }

                    pixelSizeBeforeViewport = DoubleUtil.LessThan(childViewport.X, childPixelSize.Width) ? Math.Max(childViewport.X, 0.0) : childPixelSize.Width;
                    pixelSizeInViewport = Math.Min(childViewport.Width, childPixelSize.Width - pixelSizeBeforeViewport);
                    pixelSizeAfterViewport = Math.Max(childPixelSize.Width - pixelSizeInViewport - pixelSizeBeforeViewport, 0.0); // Please note that due to rounding errors this subtraction can lead to negative values. Hence the Math.Max call
                }
                else
                {
                    if (childIsHorizontal != isHorizontal)
                    {
                        //
                        // If the child is beyond the viewport in the opposite orientation to this panel then we musn't count that child in.
                        //
                        if (DoubleUtil.GreaterThanOrClose(childViewport.Y, childLogicalSize.Height) ||
                            DoubleUtil.AreClose(childViewport.Height, 0.0))
                        {
                            return;
                        }
                    }

                    if (DoubleUtil.GreaterThanOrClose(childViewport.X, childLogicalSize.Width))
                    {
                        pixelSizeBeforeViewport = childPixelSize.Width;
                        if (!isVSP45Compat)
                        {
                            logicalSizeBeforeViewport = childLogicalSize.Width;
                        }
                    }
                    else
                    {
                        if (DoubleUtil.GreaterThan(childViewport.Width, 0.0))
                        {
                            pixelSizeInViewport = childPixelSize.Width;
                        }
                        else
                        {
                            pixelSizeAfterViewport = childPixelSize.Width;
                            if (!isVSP45Compat)
                            {
                                logicalSizeAfterViewport = childLogicalSize.Width;
                            }
                        }
                    }
                }

                Debug.Assert(DoubleUtil.AreClose(pixelSizeInViewport + pixelSizeBeforeViewport + pixelSizeAfterViewport, childPixelSize.Width), "The computed sizes within and outside the viewport should add up to the childPixelSize.");

                if (DoubleUtil.GreaterThan(childPixelSize.Width, 0.0))
                {
                    logicalSizeBeforeViewport = Math.Floor(childLogicalSize.Width * pixelSizeBeforeViewport / childPixelSize.Width);
                    logicalSizeAfterViewport = Math.Floor(childLogicalSize.Width * pixelSizeAfterViewport / childPixelSize.Width);
                    logicalSizeInViewport = childLogicalSize.Width - logicalSizeBeforeViewport - logicalSizeAfterViewport;
                }
                else if (!isVSP45Compat)
                {
                    logicalSizeInViewport = childLogicalSize.Width - logicalSizeBeforeViewport - logicalSizeAfterViewport;
                }

                double childPixelHeightInViewport = Math.Min(childViewport.Height, childPixelSize.Height - Math.Max(childViewport.Y, 0.0));

                childPixelSizeInViewport.Width += pixelSizeInViewport;
                childPixelSizeInViewport.Height = Math.Max(childPixelSizeInViewport.Height, childPixelHeightInViewport);
                childPixelSizeInCacheBeforeViewport.Width += pixelSizeBeforeViewport;
                childPixelSizeInCacheBeforeViewport.Height = Math.Max(childPixelSizeInCacheBeforeViewport.Height, childPixelHeightInViewport);
                childPixelSizeInCacheAfterViewport.Width += pixelSizeAfterViewport;
                childPixelSizeInCacheAfterViewport.Height = Math.Max(childPixelSizeInCacheAfterViewport.Height, childPixelHeightInViewport);

                childLogicalSizeInViewport.Width += logicalSizeInViewport;
                childLogicalSizeInViewport.Height = Math.Max(childLogicalSizeInViewport.Height, childLogicalSize.Height);
                childLogicalSizeInCacheBeforeViewport.Width += logicalSizeBeforeViewport;
                childLogicalSizeInCacheBeforeViewport.Height = Math.Max(childLogicalSizeInCacheBeforeViewport.Height, childLogicalSize.Height);
                childLogicalSizeInCacheAfterViewport.Width += logicalSizeAfterViewport;
                childLogicalSizeInCacheAfterViewport.Height = Math.Max(childLogicalSizeInCacheAfterViewport.Height, childLogicalSize.Height);
            }
            else
            {
                //
                // Split the child's sizes into portions before, after and within the viewport
                //

                if (IsPixelBased)
                {
                    if (childIsHorizontal != isHorizontal)
                    {
                        //
                        // If the child is beyond the viewport in the opposite orientation to this panel then we musn't count that child in.
                        //
                        if (DoubleUtil.GreaterThanOrClose(childViewport.X, childPixelSize.Width) ||
                            DoubleUtil.AreClose(childViewport.Width, 0.0))
                        {
                            return;
                        }
                    }

                    pixelSizeBeforeViewport = DoubleUtil.LessThan(childViewport.Y, childPixelSize.Height) ? Math.Max(childViewport.Y, 0.0) : childPixelSize.Height;
                    pixelSizeInViewport = Math.Min(childViewport.Height, childPixelSize.Height - pixelSizeBeforeViewport);
                    pixelSizeAfterViewport = Math.Max(childPixelSize.Height - pixelSizeInViewport - pixelSizeBeforeViewport, 0.0); // Please note that due to rounding errors this subtraction can lead to negative values. Hence the Math.Max call
                }
                else
                {
                    if (childIsHorizontal != isHorizontal)
                    {
                        //
                        // If the child is beyond the viewport in the opposite orientation to this panel then we musn't count that child in.
                        //
                        if (DoubleUtil.GreaterThanOrClose(childViewport.X, childLogicalSize.Width) ||
                            DoubleUtil.AreClose(childViewport.Width, 0.0))
                        {
                            return;
                        }
                    }

                    if (DoubleUtil.GreaterThanOrClose(childViewport.Y, childLogicalSize.Height))
                    {
                        pixelSizeBeforeViewport = childPixelSize.Height;
                        if (!isVSP45Compat)
                        {
                            logicalSizeBeforeViewport = childLogicalSize.Height;
                        }
                    }
                    else
                    {
                        if (DoubleUtil.GreaterThan(childViewport.Height, 0.0))
                        {
                            pixelSizeInViewport = childPixelSize.Height;
                        }
                        else
                        {
                            pixelSizeAfterViewport = childPixelSize.Height;
                            if (!isVSP45Compat)
                            {
                                logicalSizeAfterViewport = childLogicalSize.Height;
                            }
                        }
                    }
                }

                Debug.Assert(DoubleUtil.AreClose(pixelSizeInViewport + pixelSizeBeforeViewport + pixelSizeAfterViewport, childPixelSize.Height), "The computed sizes within and outside the viewport should add up to the childPixelSize.");

                if (DoubleUtil.GreaterThan(childPixelSize.Height, 0.0))
                {
                    logicalSizeBeforeViewport = Math.Floor(childLogicalSize.Height * pixelSizeBeforeViewport / childPixelSize.Height);
                    logicalSizeAfterViewport = Math.Floor(childLogicalSize.Height * pixelSizeAfterViewport / childPixelSize.Height);
                    logicalSizeInViewport = childLogicalSize.Height - logicalSizeBeforeViewport - logicalSizeAfterViewport;
                }
                else if (!IsVSP45Compat)
                {
                    logicalSizeInViewport = childLogicalSize.Height - logicalSizeBeforeViewport - logicalSizeAfterViewport;
                }

                double childPixelWidthInViewport = Math.Min(childViewport.Width, childPixelSize.Width - Math.Max(childViewport.X, 0.0));

                childPixelSizeInViewport.Height += pixelSizeInViewport;
                childPixelSizeInViewport.Width = Math.Max(childPixelSizeInViewport.Width, childPixelWidthInViewport);
                childPixelSizeInCacheBeforeViewport.Height += pixelSizeBeforeViewport;
                childPixelSizeInCacheBeforeViewport.Width = Math.Max(childPixelSizeInCacheBeforeViewport.Width, childPixelWidthInViewport);
                childPixelSizeInCacheAfterViewport.Height += pixelSizeAfterViewport;
                childPixelSizeInCacheAfterViewport.Width = Math.Max(childPixelSizeInCacheAfterViewport.Width, childPixelWidthInViewport);

                childLogicalSizeInViewport.Height += logicalSizeInViewport;
                childLogicalSizeInViewport.Width = Math.Max(childLogicalSizeInViewport.Width, childLogicalSize.Width);
                childLogicalSizeInCacheBeforeViewport.Height += logicalSizeBeforeViewport;
                childLogicalSizeInCacheBeforeViewport.Width = Math.Max(childLogicalSizeInCacheBeforeViewport.Width, childLogicalSize.Width);
                childLogicalSizeInCacheAfterViewport.Height += logicalSizeAfterViewport;
                childLogicalSizeInCacheAfterViewport.Width = Math.Max(childLogicalSizeInCacheAfterViewport.Width, childLogicalSize.Width);
            }
        }

        private void UpdateStackSizes(
            bool isHorizontal,
            bool foundFirstItemInViewport,
            Size childPixelSize,
            Size childPixelSizeInViewport,
            Size childPixelSizeInCacheBeforeViewport,
            Size childPixelSizeInCacheAfterViewport,
            Size childLogicalSize,
            Size childLogicalSizeInViewport,
            Size childLogicalSizeInCacheBeforeViewport,
            Size childLogicalSizeInCacheAfterViewport,
            ref Size stackPixelSize,
            ref Size stackPixelSizeInViewport,
            ref Size stackPixelSizeInCacheBeforeViewport,
            ref Size stackPixelSizeInCacheAfterViewport,
            ref Size stackLogicalSize,
            ref Size stackLogicalSizeInViewport,
            ref Size stackLogicalSizeInCacheBeforeViewport,
            ref Size stackLogicalSizeInCacheAfterViewport)
        {
            StackSizes(isHorizontal, ref stackPixelSize, childPixelSize);
            StackSizes(isHorizontal, ref stackLogicalSize, childLogicalSize);

            if (foundFirstItemInViewport)
            {
                StackSizes(isHorizontal, ref stackPixelSizeInViewport, childPixelSizeInViewport);
                StackSizes(isHorizontal, ref stackLogicalSizeInViewport, childLogicalSizeInViewport);
                StackSizes(isHorizontal, ref stackPixelSizeInCacheBeforeViewport, childPixelSizeInCacheBeforeViewport);
                StackSizes(isHorizontal, ref stackLogicalSizeInCacheBeforeViewport, childLogicalSizeInCacheBeforeViewport);
                StackSizes(isHorizontal, ref stackPixelSizeInCacheAfterViewport, childPixelSizeInCacheAfterViewport);
                StackSizes(isHorizontal, ref stackLogicalSizeInCacheAfterViewport, childLogicalSizeInCacheAfterViewport);
            }
        }

        private static void StackSizes(bool isHorizontal, ref Size sz1, Size sz2)
        {
            if (isHorizontal)
            {
                sz1.Width += sz2.Width;
                sz1.Height = Math.Max(sz1.Height, sz2.Height);
            }
            else
            {
                sz1.Height += sz2.Height;
                sz1.Width = Math.Max(sz1.Width, sz2.Width);
            }
        }

        // *** DEAD CODE This method is only called in VSP45-compat mode ***
        private void SyncUniformSizeFlags(
            object parentItem,
            IContainItemStorage parentItemStorageProvider,
            IList children,
            IList items,
            IContainItemStorage itemStorageProvider,
            int itemCount,
            bool computedAreContainersUniformlySized,
            double computedUniformOrAverageContainerSize,
            ref bool areContainersUniformlySized,
            ref double uniformOrAverageContainerSize,
            ref bool hasAverageContainerSizeChanged,
            bool isHorizontal,
            bool evaluateAreContainersUniformlySized)
        {
            Debug.Assert(IsVSP45Compat, "this method should only be called in VSP45Compat mode");
            // 4.5 used the wrong ItemStorageProvider for the AreUniformlySized flag
            parentItemStorageProvider = itemStorageProvider;

            if (evaluateAreContainersUniformlySized || areContainersUniformlySized != computedAreContainersUniformlySized)
            {
                Debug.Assert(evaluateAreContainersUniformlySized || !computedAreContainersUniformlySized, "AreContainersUniformlySized starts off true and can only be flipped to false.");

                if (!evaluateAreContainersUniformlySized)
                {
                    areContainersUniformlySized = computedAreContainersUniformlySized;
                    SetAreContainersUniformlySized(parentItemStorageProvider, parentItem, areContainersUniformlySized);
                }

                for (int i=0; i < children.Count; i++)
                {
                    UIElement child = children[i] as UIElement;
                    if (child != null && VirtualizingPanel.GetShouldCacheContainerSize(child))
                    {
                        IHierarchicalVirtualizationAndScrollInfo virtualizingChild  = GetVirtualizingChild(child);

                        Size childSize;

                        if (virtualizingChild != null)
                        {
                            HierarchicalVirtualizationHeaderDesiredSizes headerDesiredSizes = virtualizingChild.HeaderDesiredSizes;
                            HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes = virtualizingChild.ItemDesiredSizes;

                            if (IsPixelBased)
                            {
                                childSize = new Size(Math.Max(headerDesiredSizes.PixelSize.Width, itemDesiredSizes.PixelSize.Width),
                                                              headerDesiredSizes.PixelSize.Height + itemDesiredSizes.PixelSize.Height);
                            }
                            else
                            {
                                childSize = new Size(Math.Max(headerDesiredSizes.LogicalSize.Width, itemDesiredSizes.LogicalSize.Width),
                                                              headerDesiredSizes.LogicalSize.Height + itemDesiredSizes.LogicalSize.Height);
                            }
                        }
                        else
                        {
                            if (IsPixelBased)
                            {
                                childSize = child.DesiredSize;
                            }
                            else
                            {
                                childSize = new Size(DoubleUtil.GreaterThan(child.DesiredSize.Width, 0) ? 1 : 0,
                                                     DoubleUtil.GreaterThan(child.DesiredSize.Height, 0) ? 1 : 0);
                            }
                        }

                        if (evaluateAreContainersUniformlySized && computedAreContainersUniformlySized)
                        {
                            if (isHorizontal)
                            {
                                computedAreContainersUniformlySized = DoubleUtil.AreClose(childSize.Width, uniformOrAverageContainerSize);
                            }
                            else
                            {
                                computedAreContainersUniformlySized = DoubleUtil.AreClose(childSize.Height, uniformOrAverageContainerSize);
                            }

                            if (!computedAreContainersUniformlySized)
                            {
                                // We need to restart the loop and cache
                                // the sizes of all children prior to this one

                                i = -1;
                            }
                        }
                        else
                        {
                            itemStorageProvider.StoreItemValue(((ItemContainerGenerator)Generator).ItemFromContainer(child), ContainerSizeProperty, childSize);
                        }
                    }
                }

                if (evaluateAreContainersUniformlySized)
                {
                    areContainersUniformlySized = computedAreContainersUniformlySized;
                    SetAreContainersUniformlySized(parentItemStorageProvider, parentItem, areContainersUniformlySized);
                }
            }

            if (!computedAreContainersUniformlySized)
            {
                Size containerSize;
                double sumOfContainerSizes = 0;
                int numContainerSizes = 0;

                for (int i=0; i<itemCount; i++)
                {
                    object value = itemStorageProvider.ReadItemValue(items[i], ContainerSizeProperty);
                    if (value != null)
                    {
                        containerSize = (Size)value;

                        if (isHorizontal)
                        {
                            sumOfContainerSizes += containerSize.Width;
                            numContainerSizes++;
                        }
                        else
                        {
                            sumOfContainerSizes += containerSize.Height;
                            numContainerSizes++;
                        }
                    }
                }

                if (numContainerSizes > 0)
                {
                    if (IsPixelBased)
                    {
                        uniformOrAverageContainerSize = sumOfContainerSizes / numContainerSizes;
                    }
                    else
                    {
                        uniformOrAverageContainerSize = Math.Round(sumOfContainerSizes / numContainerSizes);
                    }
                }
            }
            else
            {
                uniformOrAverageContainerSize = computedUniformOrAverageContainerSize;
            }

            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.SyncAveSize,
                    uniformOrAverageContainerSize, areContainersUniformlySized, hasAverageContainerSizeChanged);
            }
        }

        private void SyncUniformSizeFlags(
            object parentItem,
            IContainItemStorage parentItemStorageProvider,
            IList children,
            IList items,
            IContainItemStorage itemStorageProvider,
            int itemCount,
            bool computedAreContainersUniformlySized,
            double computedUniformOrAverageContainerSize,
            double computedUniformOrAverageContainerPixelSize,
            ref bool areContainersUniformlySized,
            ref double uniformOrAverageContainerSize,
            ref double uniformOrAverageContainerPixelSize,
            ref bool hasAverageContainerSizeChanged,
            bool isHorizontal,
            bool evaluateAreContainersUniformlySized)
        {
            Debug.Assert(!IsVSP45Compat, "this method should not be called in VSP45Compat mode");
            bool isTracing = (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this));
            ItemContainerGenerator generator = (ItemContainerGenerator)Generator;

            if (evaluateAreContainersUniformlySized || areContainersUniformlySized != computedAreContainersUniformlySized)
            {
                Debug.Assert(evaluateAreContainersUniformlySized || !computedAreContainersUniformlySized, "AreContainersUniformlySized starts off true and can only be flipped to false.");

                if (!evaluateAreContainersUniformlySized)
                {
                    areContainersUniformlySized = computedAreContainersUniformlySized;
                    SetAreContainersUniformlySized(parentItemStorageProvider, parentItem, areContainersUniformlySized);
                }

                for (int i=0; i < children.Count; i++)
                {
                    UIElement child = children[i] as UIElement;
                    if (child != null && VirtualizingPanel.GetShouldCacheContainerSize(child))
                    {
                        IHierarchicalVirtualizationAndScrollInfo virtualizingChild  = GetVirtualizingChild(child);

                        Size childSize;
                        Size childPixelSize;

                        if (virtualizingChild != null)
                        {
                            HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes = virtualizingChild.ItemDesiredSizes;

                            object v = child.ReadLocalValue(ItemsHostInsetProperty);
                            if (v != DependencyProperty.UnsetValue)
                            {
                                // inset has been set - add it to the ItemsHost size
                                Thickness inset = (Thickness)v;
                                childPixelSize = new Size(inset.Left + itemDesiredSizes.PixelSize.Width + inset.Right,
                                                     inset.Top + itemDesiredSizes.PixelSize.Height + inset.Bottom);
                            }
                            else
                            {
                                // inset has not been set (typically because
                                // child doesn't have an ItemsHost).  Use
                                // the child's desired size
                                childPixelSize = child.DesiredSize;
                            }

                            if (IsPixelBased)
                            {
                                childSize = childPixelSize;
                            }
                            else
                            {
                                childSize = isHorizontal ? new Size(1 + itemDesiredSizes.LogicalSize.Width,
                                                                    Math.Max(1, itemDesiredSizes.LogicalSize.Height))
                                                         : new Size(Math.Max(1, itemDesiredSizes.LogicalSize.Width),
                                                                    1 + itemDesiredSizes.LogicalSize.Height);
                            }
                        }
                        else
                        {
                            childPixelSize = child.DesiredSize;

                            if (IsPixelBased)
                            {
                                childSize = childPixelSize;
                            }
                            else
                            {
                                childSize = new Size(DoubleUtil.GreaterThan(child.DesiredSize.Width, 0) ? 1 : 0,
                                                     DoubleUtil.GreaterThan(child.DesiredSize.Height, 0) ? 1 : 0);
                            }
                        }

                        if (evaluateAreContainersUniformlySized && computedAreContainersUniformlySized)
                        {
                            if (isHorizontal)
                            {
                                computedAreContainersUniformlySized = DoubleUtil.AreClose(childSize.Width, uniformOrAverageContainerSize)
                                    && (IsPixelBased || DoubleUtil.AreClose(childPixelSize.Width, uniformOrAverageContainerPixelSize));
                            }
                            else
                            {
                                computedAreContainersUniformlySized = DoubleUtil.AreClose(childSize.Height, uniformOrAverageContainerSize)
                                    && (IsPixelBased || DoubleUtil.AreClose(childPixelSize.Height, uniformOrAverageContainerPixelSize));
                            }

                            if (!computedAreContainersUniformlySized)
                            {
                                // We need to restart the loop and cache
                                // the sizes of all children prior to this one

                                i = -1;
                            }
                        }
                        else
                        {
                            if (IsPixelBased)
                            {
                                if (isTracing)
                                {
                                    ScrollTracer.Trace(this, ScrollTraceOp.SetContainerSize,
                                        generator.IndexFromContainer(child), childSize);
                                }

                                // for pixel-scrolling the two values are the same - store only one
                                itemStorageProvider.StoreItemValue(generator.ItemFromContainer(child), ContainerSizeProperty, childSize);
                            }
                            else
                            {
                                if (isTracing)
                                {
                                    ScrollTracer.Trace(this, ScrollTraceOp.SetContainerSize,
                                        generator.IndexFromContainer(child), childSize, childPixelSize);
                                }

                                // for item-scrolling, store both values
                                ContainerSizeDual value =
                                        new ContainerSizeDual(childPixelSize, childSize);
                                itemStorageProvider.StoreItemValue(generator.ItemFromContainer(child), ContainerSizeDualProperty, value);
                            }
                        }
                    }
                }

                if (evaluateAreContainersUniformlySized)
                {
                    areContainersUniformlySized = computedAreContainersUniformlySized;
                    SetAreContainersUniformlySized(parentItemStorageProvider, parentItem, areContainersUniformlySized);
                }
            }

            if (!computedAreContainersUniformlySized)
            {
                Size containerSize = new Size();
                Size containerPixelSize = new Size();
                double sumOfContainerSizes = 0;
                double sumOfContainerPixelSizes = 0;
                int numContainerSizes = 0;

                for (int i=0; i<itemCount; i++)
                {
                    object value = null;

                    if (IsPixelBased)
                    {
                        value = itemStorageProvider.ReadItemValue(items[i], ContainerSizeProperty);
                        if (value != null)
                        {
                            containerSize = (Size)value;
                            containerPixelSize = containerSize;
                        }
                    }
                    else
                    {
                        value = itemStorageProvider.ReadItemValue(items[i], ContainerSizeDualProperty);
                        if (value != null)
                        {
                            ContainerSizeDual csd = (ContainerSizeDual)value;
                            containerSize = csd.ItemSize;
                            containerPixelSize = csd.PixelSize;
                        }
                    }

                    if (value != null)
                    {
                        // we found an item that has been realized at some point.
                        // add its size to the accumulated total
                        if (isHorizontal)
                        {
                            sumOfContainerSizes += containerSize.Width;
                            sumOfContainerPixelSizes += containerPixelSize.Width;
                            numContainerSizes++;
                        }
                        else
                        {
                            sumOfContainerSizes += containerSize.Height;
                            sumOfContainerPixelSizes += containerPixelSize.Height;
                            numContainerSizes++;
                        }
                    }
                }

                if (numContainerSizes > 0)
                {
                    uniformOrAverageContainerPixelSize = sumOfContainerPixelSizes / numContainerSizes;

                    if (UseLayoutRounding)
                    {
                        // apply layout rounding to the average size, so that anchored
                        // scrolls use rounded sizes throughout.  Otherwise they can
                        // hang because of rounding done in layout that isn't accounted
                        // for in OnAnchor.
                        DpiScale dpi = GetDpi();
                        double dpiScale = isHorizontal ? dpi.DpiScaleX : dpi.DpiScaleY;
                        uniformOrAverageContainerPixelSize = RoundLayoutValue(
                                        Math.Max(uniformOrAverageContainerPixelSize, dpiScale), // don't round down to 0
                                        dpiScale);
                    }

                    if (IsPixelBased)
                    {
                        uniformOrAverageContainerSize = uniformOrAverageContainerPixelSize;
                    }
                    else
                    {
                        uniformOrAverageContainerSize = Math.Round(sumOfContainerSizes / numContainerSizes);
                    }

                    if (SetUniformOrAverageContainerSize(parentItemStorageProvider, parentItem, uniformOrAverageContainerSize, uniformOrAverageContainerPixelSize))
                    {
                        hasAverageContainerSizeChanged = true;
                    }
                }
            }
            else
            {
                uniformOrAverageContainerSize = computedUniformOrAverageContainerSize;
                uniformOrAverageContainerPixelSize = computedUniformOrAverageContainerPixelSize;
            }

            if (isTracing)
            {
                ScrollTracer.Trace(this, ScrollTraceOp.SyncAveSize,
                    uniformOrAverageContainerSize, uniformOrAverageContainerPixelSize, areContainersUniformlySized, hasAverageContainerSizeChanged);
            }
        }

        private void ClearAsyncOperations()
        {
            bool isVSP45Compat = IsVSP45Compat;

            if (isVSP45Compat)
            {
                DispatcherOperation measureCachesOperation = MeasureCachesOperationField.GetValue(this);
                if (measureCachesOperation != null)
                {
                    measureCachesOperation.Abort();
                    MeasureCachesOperationField.ClearValue(this);
                }
            }
            else
            {
                // this does what 4.5 did, and also cleans up state related
                // to MeasureCaches
                ClearMeasureCachesState();
            }

            DispatcherOperation anchorOperation = AnchorOperationField.GetValue(this);
            if (anchorOperation != null)
            {
                if (isVSP45Compat)
                {
                    anchorOperation.Abort();
                    AnchorOperationField.ClearValue(this);
                }
                else
                {
                    // this does what 4.5 did, and also cleans up state related
                    // to the anchor operation (_firstContainerInViewport,  IsContainerVirtualizable).
                    // 4.5 left things in an inconsistent state
                    ClearAnchorInformation(shouldAbort:true);
                }
            }

            DispatcherOperation anchoredInvalidateMeasureOperation = AnchoredInvalidateMeasureOperationField.GetValue(this);
            if (anchoredInvalidateMeasureOperation != null)
            {
                anchoredInvalidateMeasureOperation.Abort();
                AnchoredInvalidateMeasureOperationField.ClearValue(this);
            }

            DispatcherOperation clearIsScrollActiveOperation = ClearIsScrollActiveOperationField.GetValue(this);
            if (clearIsScrollActiveOperation != null)
            {
                if (isVSP45Compat)
                {
                    clearIsScrollActiveOperation.Abort();
                    ClearIsScrollActiveOperationField.ClearValue(this);
                }
                else
                {
                    // this does what 4.5 did, and also cleans up state related
                    // to IsScrollActive
                    clearIsScrollActiveOperation.Abort();
                    ClearIsScrollActive();
                }
            }
        }

        private bool GetAreContainersUniformlySized(IContainItemStorage itemStorageProvider, object item)
        {
            Debug.Assert(itemStorageProvider != null || item == this, "An item storage provider must be available.");
            Debug.Assert(item != null, "An item must be available.");

            if (item == this)
            {
                if (AreContainersUniformlySized.HasValue)
                {
                    // Return the cached value if for VSP and if present.
                    return (bool)AreContainersUniformlySized;
                }
            }
            else
            {
                object value = itemStorageProvider.ReadItemValue(item, AreContainersUniformlySizedProperty);
                if (value != null)
                {
                    return (bool)value;
                }
            }

            return true;
        }

        private void SetAreContainersUniformlySized(IContainItemStorage itemStorageProvider, object item, bool value)
        {
            Debug.Assert(itemStorageProvider != null || item == this, "An item storage provider must be available.");
            Debug.Assert(item != null, "An item must be available.");

            if (item == this)
            {
                // Set the cache if for VSP.
                AreContainersUniformlySized = value;
            }
            else
            {
                itemStorageProvider.StoreItemValue(item, AreContainersUniformlySizedProperty, value);
            }
        }

        private double GetUniformOrAverageContainerSize(IContainItemStorage itemStorageProvider, object item)
        {
            double uniformOrAverageContainerSize, uniformOrAverageContainerPixelSize;
            GetUniformOrAverageContainerSize(itemStorageProvider, item,
                IsPixelBased || IsVSP45Compat,
                out uniformOrAverageContainerSize,
                out uniformOrAverageContainerPixelSize);
            return uniformOrAverageContainerSize;
        }

        private void GetUniformOrAverageContainerSize(IContainItemStorage itemStorageProvider,
            object item,
            bool isSingleValue,
            out double uniformOrAverageContainerSize,
            out double uniformOrAverageContainerPixelSize)
        {
            bool hasUniformOrAverageContainerSizeBeenSet;
            GetUniformOrAverageContainerSize(itemStorageProvider, item, isSingleValue,
                out uniformOrAverageContainerSize,
                out uniformOrAverageContainerPixelSize,
                out hasUniformOrAverageContainerSizeBeenSet);
        }

        private void GetUniformOrAverageContainerSize(IContainItemStorage itemStorageProvider,
            object item,
            bool isSingleValue,
            out double uniformOrAverageContainerSize,
            out double uniformOrAverageContainerPixelSize,
            out bool hasUniformOrAverageContainerSizeBeenSet)
        {
            Debug.Assert(itemStorageProvider != null || item == this, "An item storage provider must be available.");
            Debug.Assert(item != null, "An item must be available.");

            if (item == this)
            {
                if (UniformOrAverageContainerSize.HasValue)
                {
                    // Return the cached value if for VSP and if present.
                    hasUniformOrAverageContainerSizeBeenSet = true;
                    uniformOrAverageContainerSize = (double)UniformOrAverageContainerSize;

                    if (isSingleValue)
                    {
                        uniformOrAverageContainerPixelSize = uniformOrAverageContainerSize;
                    }
                    else
                    {
                        uniformOrAverageContainerPixelSize = (double)UniformOrAverageContainerPixelSize;
                    }
                    return;
                }
            }
            else
            {
                if (isSingleValue)
                {
                    object value = itemStorageProvider.ReadItemValue(item, UniformOrAverageContainerSizeProperty);
                    if (value != null)
                    {
                        hasUniformOrAverageContainerSizeBeenSet = true;
                        uniformOrAverageContainerSize = (double)value;
                        uniformOrAverageContainerPixelSize = uniformOrAverageContainerSize;
                        return;
                    }
                }
                else
                {
                    object value = itemStorageProvider.ReadItemValue(item, UniformOrAverageContainerSizeDualProperty);
                    if (value != null)
                    {
                        UniformOrAverageContainerSizeDual d = (UniformOrAverageContainerSizeDual)value;
                        hasUniformOrAverageContainerSizeBeenSet = true;
                        uniformOrAverageContainerSize = d.ItemSize;
                        uniformOrAverageContainerPixelSize = d.PixelSize;
                        return;
                    }
                }
            }

            hasUniformOrAverageContainerSizeBeenSet = false;
            uniformOrAverageContainerPixelSize = ScrollViewer._scrollLineDelta;
            uniformOrAverageContainerSize = IsPixelBased ? uniformOrAverageContainerPixelSize : 1.0;
            return;
        }

        // returns true if the cached average size changed
        private bool SetUniformOrAverageContainerSize(IContainItemStorage itemStorageProvider, object item, double value, double pixelValue)
        {
            Debug.Assert(itemStorageProvider != null || item == this, "An item storage provider must be available.");
            Debug.Assert(item != null, "An item must be available.");

            bool result = false;

            //
            // This case was detected when entering a ListBoxItem into the ListBox through the XAML editor
            // in VS. In this case the ListBoxItem is empty and is of zero size. We do not want to record the
            // size of that ListBoxItem as the uniformOrAverageSize because on the next measure this will
            // lead to a divide by zero error when computing the firstItemInViewportIndex.
            //
            if (DoubleUtil.GreaterThan(value, 0))
            {
                if (item == this)
                {
                    // Set the cache if for VSP.
                    if (UniformOrAverageContainerSize != value)
                    {
                        UniformOrAverageContainerSize = value;
                        UniformOrAverageContainerPixelSize = pixelValue;
                        result = true;
                    }
                }
                else
                {
                    if (IsPixelBased || IsVSP45Compat)
                    {
                        object oldValue = itemStorageProvider.ReadItemValue(item, UniformOrAverageContainerSizeProperty);
                        itemStorageProvider.StoreItemValue(item, UniformOrAverageContainerSizeProperty, value);
                        result = !Object.Equals(oldValue, value);
                    }
                    else
                    {
                        UniformOrAverageContainerSizeDual oldValue = itemStorageProvider.ReadItemValue(item, UniformOrAverageContainerSizeDualProperty) as UniformOrAverageContainerSizeDual;
                        UniformOrAverageContainerSizeDual newValue = new UniformOrAverageContainerSizeDual(pixelValue, value);
                        itemStorageProvider.StoreItemValue(item, UniformOrAverageContainerSizeDualProperty, newValue);
                        result = (oldValue == null) || (oldValue.ItemSize != value);
                    }
                }
            }

            return result;
        }

        private void MeasureExistingChildBeyondExtendedViewport(
            ref IItemContainerGenerator generator,
            ref IContainItemStorage itemStorageProvider,
            ref IContainItemStorage parentItemStorageProvider,
            ref object parentItem,
            ref bool hasUniformOrAverageContainerSizeBeenSet,
            ref double computedUniformOrAverageContainerSize,
            ref double computedUniformOrAverageContainerPixelSize,
            ref bool computedAreContainersUniformlySized,
            ref bool hasAnyContainerSpanChanged,
            ref IList items,
            ref IList children,
            ref int childIndex,
            ref bool visualOrderChanged,
            ref bool isHorizontal,
            ref Size childConstraint,
            ref bool foundFirstItemInViewport,
            ref double firstItemInViewportOffset,
            ref bool mustDisableVirtualization,
            ref bool hasVirtualizingChildren,
            ref bool hasBringIntoViewContainerBeenMeasured,
            ref long scrollGeneration)
        {
            object item = ((ItemContainerGenerator)generator).ItemFromContainer((UIElement)children[childIndex]);
            Rect viewport = new Rect();
            VirtualizationCacheLength cacheSize = new VirtualizationCacheLength();
            VirtualizationCacheLengthUnit cacheUnit = VirtualizationCacheLengthUnit.Pixel;
            Size stackPixelSize = new Size();
            Size stackPixelSizeInViewport = new Size();
            Size stackPixelSizeInCacheBeforeViewport = new Size();
            Size stackPixelSizeInCacheAfterViewport = new Size();
            Size stackLogicalSize = new Size();
            Size stackLogicalSizeInViewport = new Size();
            Size stackLogicalSizeInCacheBeforeViewport = new Size();
            Size stackLogicalSizeInCacheAfterViewport = new Size();
            bool isBeforeFirstItem = childIndex < _firstItemInExtendedViewportChildIndex;
            bool isAfterFirstItem = childIndex > _firstItemInExtendedViewportChildIndex;
            bool isAfterLastItem = childIndex > _firstItemInExtendedViewportChildIndex + _actualItemsInExtendedViewportCount;
            bool skipActualMeasure = false;
            bool skipGeneration = true;

            MeasureChild(
                ref generator,
                ref itemStorageProvider,
                ref parentItemStorageProvider,
                ref parentItem,
                ref hasUniformOrAverageContainerSizeBeenSet,
                ref computedUniformOrAverageContainerSize,
                ref computedUniformOrAverageContainerPixelSize,
                ref computedAreContainersUniformlySized,
                ref hasAnyContainerSpanChanged,
                ref items,
                ref item,
                ref children,
                ref childIndex,
                ref visualOrderChanged,
                ref isHorizontal,
                ref childConstraint,
                ref viewport,
                ref cacheSize,
                ref cacheUnit,
                ref scrollGeneration,
                ref foundFirstItemInViewport,
                ref firstItemInViewportOffset,
                ref stackPixelSize,
                ref stackPixelSizeInViewport,
                ref stackPixelSizeInCacheBeforeViewport,
                ref stackPixelSizeInCacheAfterViewport,
                ref stackLogicalSize,
                ref stackLogicalSizeInViewport,
                ref stackLogicalSizeInCacheBeforeViewport,
                ref stackLogicalSizeInCacheAfterViewport,
                ref mustDisableVirtualization,
                isBeforeFirstItem,
                isAfterFirstItem,
                isAfterLastItem,
                skipActualMeasure,
                skipGeneration,
                ref hasBringIntoViewContainerBeenMeasured,
                ref hasVirtualizingChildren);
        }

        private void MeasureChild(
            ref IItemContainerGenerator generator,
            ref IContainItemStorage itemStorageProvider,
            ref IContainItemStorage parentItemStorageProvider,
            ref object parentItem,
            ref bool hasUniformOrAverageContainerSizeBeenSet,
            ref double computedUniformOrAverageContainerSize,
            ref double computedUniformOrAverageContainerPixelSize,
            ref bool computedAreContainersUniformlySized,
            ref bool hasAnyContainerSpanChanged,
            ref IList items,
            ref object item,
            ref IList children,
            ref int childIndex,
            ref bool visualOrderChanged,
            ref bool isHorizontal,
            ref Size childConstraint,
            ref Rect viewport,
            ref VirtualizationCacheLength cacheSize,
            ref VirtualizationCacheLengthUnit cacheUnit,
            ref long scrollGeneration,
            ref bool foundFirstItemInViewport,
            ref double firstItemInViewportOffset,
            ref Size stackPixelSize,
            ref Size stackPixelSizeInViewport,
            ref Size stackPixelSizeInCacheBeforeViewport,
            ref Size stackPixelSizeInCacheAfterViewport,
            ref Size stackLogicalSize,
            ref Size stackLogicalSizeInViewport,
            ref Size stackLogicalSizeInCacheBeforeViewport,
            ref Size stackLogicalSizeInCacheAfterViewport,
            ref bool mustDisableVirtualization,
            bool isBeforeFirstItem,
            bool isAfterFirstItem,
            bool isAfterLastItem,
            bool skipActualMeasure,
            bool skipGeneration,
            ref bool hasBringIntoViewContainerBeenMeasured,
            ref bool hasVirtualizingChildren)
        {
            UIElement child = null;
            IHierarchicalVirtualizationAndScrollInfo virtualizingChild = null;
            Rect childViewport = Rect.Empty;
            VirtualizationCacheLength childCacheSize = new VirtualizationCacheLength(0.0);
            VirtualizationCacheLengthUnit childCacheUnit = VirtualizationCacheLengthUnit.Pixel;
            Size childDesiredSize = new Size();

            //
            // Get and connect the next child.
            //
            if (!skipActualMeasure && !skipGeneration)
            {
                bool newlyRealized;
                child = generator.GenerateNext(out newlyRealized) as UIElement;

                ItemContainerGenerator icg;
                if (child == null && (icg = generator as ItemContainerGenerator) != null)
                {
                    icg.Verify();
                }

                visualOrderChanged |= AddContainerFromGenerator(childIndex, child, newlyRealized, isBeforeFirstItem);
            }
            else
            {
                child = (UIElement)children[childIndex];
            }

            hasBringIntoViewContainerBeenMeasured |= (child == _bringIntoViewContainer);

            //
            // Set viewport constraints
            //

            bool isChildHorizontal = isHorizontal;
            virtualizingChild = GetVirtualizingChild(child, ref isChildHorizontal);

            SetViewportForChild(
                isHorizontal,
                itemStorageProvider,
                computedAreContainersUniformlySized,
                computedUniformOrAverageContainerSize,
                mustDisableVirtualization,
                child,
                virtualizingChild,
                item,
                isBeforeFirstItem,
                isAfterFirstItem,
                firstItemInViewportOffset,
                viewport,
                cacheSize,
                cacheUnit,
                scrollGeneration,
                stackPixelSize,
                stackPixelSizeInViewport,
                stackPixelSizeInCacheBeforeViewport,
                stackPixelSizeInCacheAfterViewport,
                stackLogicalSize,
                stackLogicalSizeInViewport,
                stackLogicalSizeInCacheBeforeViewport,
                stackLogicalSizeInCacheAfterViewport,
                out childViewport,
                ref childCacheSize,
                ref childCacheUnit);

            //
            // Measure the child
            //

            if (!skipActualMeasure)
            {
                child.Measure(childConstraint);
            }

            childDesiredSize = child.DesiredSize;

            //
            // Accumulate child size.
            //

            if (virtualizingChild != null)
            {
                //
                // Update the virtualizingChild once more to really be sure
                // that we can trust the Desired values from it. Previously
                // we may have bypassed some checks because the ItemsHost
                // wasn't connected.
                //
                virtualizingChild = GetVirtualizingChild(child, ref isChildHorizontal);

                mustDisableVirtualization |=
                    (virtualizingChild != null && virtualizingChild.MustDisableVirtualization) ||
                    isChildHorizontal != isHorizontal;
            }

            Size childPixelSize, childPixelSizeInViewport, childPixelSizeInCacheBeforeViewport, childPixelSizeInCacheAfterViewport;
            Size childLogicalSize, childLogicalSizeInViewport, childLogicalSizeInCacheBeforeViewport, childLogicalSizeInCacheAfterViewport;

            if (IsVSP45Compat)
            {
                GetSizesForChild(
                    isHorizontal,
                    isChildHorizontal,
                    isBeforeFirstItem,
                    isAfterLastItem,
                    virtualizingChild,
                    childDesiredSize,
                    childViewport,
                    childCacheSize,
                    childCacheUnit,
                    out childPixelSize,
                    out childPixelSizeInViewport,
                    out childPixelSizeInCacheBeforeViewport,
                    out childPixelSizeInCacheAfterViewport,
                    out childLogicalSize,
                    out childLogicalSizeInViewport,
                    out childLogicalSizeInCacheBeforeViewport,
                    out childLogicalSizeInCacheAfterViewport);
            }
            else
            {
                GetSizesForChildWithInset(
                    isHorizontal,
                    isChildHorizontal,
                    isBeforeFirstItem,
                    isAfterLastItem,
                    virtualizingChild,
                    childDesiredSize,
                    childViewport,
                    childCacheSize,
                    childCacheUnit,
                    out childPixelSize,
                    out childPixelSizeInViewport,
                    out childPixelSizeInCacheBeforeViewport,
                    out childPixelSizeInCacheAfterViewport,
                    out childLogicalSize,
                    out childLogicalSizeInViewport,
                    out childLogicalSizeInCacheBeforeViewport,
                    out childLogicalSizeInCacheAfterViewport);
            }

            UpdateStackSizes(
                isHorizontal,
                foundFirstItemInViewport,
                childPixelSize,
                childPixelSizeInViewport,
                childPixelSizeInCacheBeforeViewport,
                childPixelSizeInCacheAfterViewport,
                childLogicalSize,
                childLogicalSizeInViewport,
                childLogicalSizeInCacheBeforeViewport,
                childLogicalSizeInCacheAfterViewport,
                ref stackPixelSize,
                ref stackPixelSizeInViewport,
                ref stackPixelSizeInCacheBeforeViewport,
                ref stackPixelSizeInCacheAfterViewport,
                ref stackLogicalSize,
                ref stackLogicalSizeInViewport,
                ref stackLogicalSizeInCacheBeforeViewport,
                ref stackLogicalSizeInCacheAfterViewport);

            if (virtualizingChild != null)
            {
                hasVirtualizingChildren = true;
            }

            //
            // Cache the container size.
            //

            if (VirtualizingPanel.GetShouldCacheContainerSize(child))
            {
                if (IsVSP45Compat)
                {
                    SetContainerSizeForItem(
                        itemStorageProvider,
                        parentItemStorageProvider,
                        parentItem,
                        item,
                        IsPixelBased ? childPixelSize : childLogicalSize,
                        isHorizontal,
                        ref hasUniformOrAverageContainerSizeBeenSet,
                        ref computedUniformOrAverageContainerSize,
                        ref computedAreContainersUniformlySized);
                }
                else
                {
                    SetContainerSizeForItem(
                        itemStorageProvider,
                        parentItemStorageProvider,
                        parentItem,
                        item,
                        IsPixelBased ? childPixelSize : childLogicalSize,
                        childPixelSize,
                        isHorizontal,
                        hasVirtualizingChildren,
                        ref hasUniformOrAverageContainerSizeBeenSet,
                        ref computedUniformOrAverageContainerSize,
                        ref computedUniformOrAverageContainerPixelSize,
                        ref computedAreContainersUniformlySized,
                        ref hasAnyContainerSpanChanged);
                }
            }
        }

        private void ArrangeFirstItemInExtendedViewport(
            bool isHorizontal,
            UIElement child,
            Size childDesiredSize,
            double arrangeLength,
            ref Rect rcChild,
            ref Size previousChildSize,
            ref Point previousChildOffset,
            ref int previousChildItemIndex)
        {
            //
            // This is the first child in the extendedViewport. Initialize the child rect.
            // This is required because there may have been other children
            // outside the viewport that were previously arranged and hence
            // mangled the child rect struct.
            //
            rcChild.X = 0.0;
            rcChild.Y = 0.0;

            if (IsScrolling)
            {
                //
                // This is the scrolling panel. Offset the arrange rect with
                // respect to the viewport.
                //
                if (!IsPixelBased)
                {
                    if (isHorizontal)
                    {
                        rcChild.X = -1.0 *
                            ((IsVSP45Compat || !IsVirtualizing || !HasVirtualizingChildren) ? _previousStackPixelSizeInCacheBeforeViewport.Width : _pixelDistanceToViewport);
                        rcChild.Y = -1.0 * _scrollData._computedOffset.Y;
                    }
                    else
                    {
                        rcChild.Y = -1.0 *
                            ((IsVSP45Compat || !IsVirtualizing || !HasVirtualizingChildren) ? _previousStackPixelSizeInCacheBeforeViewport.Height : _pixelDistanceToViewport);
                        rcChild.X = -1.0 * _scrollData._computedOffset.X;
                    }
                }
                else
                {
                    rcChild.X = -1.0 * _scrollData._computedOffset.X;
                    rcChild.Y = -1.0 * _scrollData._computedOffset.Y;
                }
            }

            if (IsVirtualizing)
            {
                if (IsPixelBased)
                {
                    if (isHorizontal)
                    {
                        rcChild.X += _firstItemInExtendedViewportOffset;
                    }
                    else
                    {
                        rcChild.Y += _firstItemInExtendedViewportOffset;
                    }
                }
                else if (!IsVSP45Compat && (!IsScrolling || HasVirtualizingChildren))
                {
                    if (isHorizontal)
                    {
                        rcChild.X += _pixelDistanceToFirstContainerInExtendedViewport;
                    }
                    else
                    {
                        rcChild.Y += _pixelDistanceToFirstContainerInExtendedViewport;
                    }
                }
            }

            bool isChildHorizontal = isHorizontal;
            IHierarchicalVirtualizationAndScrollInfo virtualizingChild = GetVirtualizingChild(child, ref isChildHorizontal);

            if (isHorizontal)
            {
                rcChild.Width = childDesiredSize.Width;
                rcChild.Height = Math.Max(arrangeLength, childDesiredSize.Height);
                previousChildSize = childDesiredSize;

                if (!IsPixelBased && virtualizingChild != null && IsVSP45Compat)
                {
                    //
                    // For a non leaf item we only want to account for the size in the extended viewport
                    //
                    HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes = virtualizingChild.ItemDesiredSizes;

                    previousChildSize.Width = itemDesiredSizes.PixelSizeInViewport.Width;

                    if (isChildHorizontal == isHorizontal)
                    {
                        previousChildSize.Width += itemDesiredSizes.PixelSizeBeforeViewport.Width + itemDesiredSizes.PixelSizeAfterViewport.Width;
                    }

                    RelativeHeaderPosition headerPosition = RelativeHeaderPosition.Top; // virtualizingChild.RelativeHeaderPosition;
                    Size pixelHeaderSize = virtualizingChild.HeaderDesiredSizes.PixelSize;
                    if (headerPosition == RelativeHeaderPosition.Left || headerPosition == RelativeHeaderPosition.Right)
                    {   // *** DEAD CODE  headerPosition is always Top ***
                        previousChildSize.Width += pixelHeaderSize.Width;
                    }   // *** END DEAD CODE ***
                    else
                    {
                        previousChildSize.Width = Math.Max(previousChildSize.Width, pixelHeaderSize.Width);
                    }
                }
            }
            else
            {
                rcChild.Height = childDesiredSize.Height;
                rcChild.Width = Math.Max(arrangeLength, childDesiredSize.Width);
                previousChildSize = childDesiredSize;

                if (!IsPixelBased && virtualizingChild != null && IsVSP45Compat)
                {
                    //
                    // For a non leaf item we only want to account for the size in the extended viewport
                    //
                    HierarchicalVirtualizationItemDesiredSizes itemDesiredSizes = virtualizingChild.ItemDesiredSizes;

                    previousChildSize.Height = itemDesiredSizes.PixelSizeInViewport.Height;

                    if (isChildHorizontal == isHorizontal)
                    {
                        previousChildSize.Height += itemDesiredSizes.PixelSizeBeforeViewport.Height + itemDesiredSizes.PixelSizeAfterViewport.Height;
                    }

                    RelativeHeaderPosition headerPosition = RelativeHeaderPosition.Top; // virtualizingChild.RelativeHeaderPosition;
                    Size pixelHeaderSize = virtualizingChild.HeaderDesiredSizes.PixelSize;
                    if (headerPosition == RelativeHeaderPosition.Top || headerPosition == RelativeHeaderPosition.Bottom)
                    {
                        previousChildSize.Height += pixelHeaderSize.Height;
                    }
                    else
                    {   // *** DEAD CODE  headerPosition is always Top ***
                        previousChildSize.Height = Math.Max(previousChildSize.Height, pixelHeaderSize.Height);
                    }   // *** END DEAD CODE ***
                }
            }

            previousChildItemIndex = _firstItemInExtendedViewportIndex;
            previousChildOffset = rcChild.Location;

            child.Arrange(rcChild);
        }

        private void ArrangeOtherItemsInExtendedViewport(
            bool isHorizontal,
            UIElement child,
            Size childDesiredSize,
            double arrangeLength,
            int index,
            ref Rect rcChild,
            ref Size previousChildSize,
            ref Point previousChildOffset,
            ref int previousChildItemIndex)
        {
            //
            // These are the items within the viewport beyond the first.
            // So they only need to be offset from the previous child.
            //
            if (isHorizontal)
            {
                rcChild.X += previousChildSize.Width;
                rcChild.Width = childDesiredSize.Width;
                rcChild.Height = Math.Max(arrangeLength, childDesiredSize.Height);
            }
            else
            {
                rcChild.Y += previousChildSize.Height;
                rcChild.Height = childDesiredSize.Height;
                rcChild.Width = Math.Max(arrangeLength, childDesiredSize.Width);
            }

            previousChildSize = childDesiredSize;
            previousChildItemIndex = _firstItemInExtendedViewportIndex + (index - _firstItemInExtendedViewportChildIndex);
            previousChildOffset = rcChild.Location;

            child.Arrange(rcChild);
        }

        private void ArrangeItemsBeyondTheExtendedViewport(
            bool isHorizontal,
            UIElement child,
            Size childDesiredSize,
            double arrangeLength,
            IList items,
            IItemContainerGenerator generator,
            IContainItemStorage itemStorageProvider,
            bool areContainersUniformlySized,
            double uniformOrAverageContainerSize,
            bool beforeExtendedViewport,
            ref Rect rcChild,
            ref Size previousChildSize,
            ref Point previousChildOffset,
            ref int previousChildItemIndex)
        {
            //
            // These are the items beyond the viewport. (Eg. Recyclable containers that
            // are waiting until next measure to be cleaned up, Elements that are kept
            // alive because they hold focus.) These element are arranged beyond the
            // visible viewport but at their right position. This is important because these
            // containers need to be brought into view when using keyboard navigation
            // and their arrange rect is what informs the scroillviewer of the right offset
            // to scroll to.
            //
            if (isHorizontal)
            {
                rcChild.Width = childDesiredSize.Width;
                rcChild.Height = Math.Max(arrangeLength, childDesiredSize.Height);

                if (IsPixelBased)
                {
                    int currChildItemIndex = ((ItemContainerGenerator)generator).IndexFromContainer(child, true /*returnLocalIndex*/);
                    double distance;

                    if (beforeExtendedViewport)
                    {
                        if (previousChildItemIndex == -1)
                        {
                            ComputeDistance(items, itemStorageProvider, isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize, 0, currChildItemIndex, out distance);
                        }
                        else
                        {
                            ComputeDistance(items, itemStorageProvider, isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize, currChildItemIndex, previousChildItemIndex-currChildItemIndex, out distance);
                        }

                        rcChild.X = previousChildOffset.X - distance;
                        rcChild.Y = previousChildOffset.Y;
                    }
                    else
                    {
                        if (previousChildItemIndex == -1)
                        {
                            ComputeDistance(items, itemStorageProvider, isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize, 0, currChildItemIndex, out distance);
                        }
                        else
                        {
                            ComputeDistance(items, itemStorageProvider, isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize, previousChildItemIndex, currChildItemIndex-previousChildItemIndex, out distance);
                        }

                        rcChild.X = previousChildOffset.X + distance;
                        rcChild.Y = previousChildOffset.Y;
                    }

                    previousChildItemIndex = currChildItemIndex;
                }
                else
                {
                    if (beforeExtendedViewport)
                    {
                        rcChild.X -= childDesiredSize.Width;
                    }
                    else
                    {
                        rcChild.X += previousChildSize.Width;
                    }
                }
            }
            else
            {
                rcChild.Height = childDesiredSize.Height;
                rcChild.Width = Math.Max(arrangeLength, childDesiredSize.Width);

                if (IsPixelBased)
                {
                    int currChildItemIndex = ((ItemContainerGenerator)generator).IndexFromContainer(child, true /*returnLocalIndex*/);
                    double distance;

                    if (beforeExtendedViewport)
                    {
                        if (previousChildItemIndex == -1)
                        {
                            ComputeDistance(items, itemStorageProvider, isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize, 0, currChildItemIndex, out distance);
                        }
                        else
                        {
                            ComputeDistance(items, itemStorageProvider, isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize, currChildItemIndex, previousChildItemIndex-currChildItemIndex, out distance);
                        }

                        rcChild.Y = previousChildOffset.Y - distance;
                        rcChild.X = previousChildOffset.X;
                    }
                    else
                    {
                        if (previousChildItemIndex == -1)
                        {
                            ComputeDistance(items, itemStorageProvider, isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize, 0, currChildItemIndex, out distance);
                        }
                        else
                        {
                            ComputeDistance(items, itemStorageProvider, isHorizontal, areContainersUniformlySized, uniformOrAverageContainerSize, previousChildItemIndex, currChildItemIndex-previousChildItemIndex, out distance);
                        }

                        rcChild.Y = previousChildOffset.Y + distance;
                        rcChild.X = previousChildOffset.X;
                    }

                    previousChildItemIndex = currChildItemIndex;
                }
                else
                {
                    if (beforeExtendedViewport)
                    {
                        rcChild.Y -= childDesiredSize.Height;
                    }
                    else
                    {
                        rcChild.Y += previousChildSize.Height;
                    }
                }
            }

            previousChildSize = childDesiredSize;
            previousChildOffset = rcChild.Location;

            child.Arrange(rcChild);
        }

        #endregion

        /// <summary>
        /// Inserts a new container in the visual tree
        /// </summary>
        /// <param name="childIndex"></param>
        /// <param name="container"></param>
        private void InsertNewContainer(int childIndex, UIElement container)
        {
            InsertContainer(childIndex, container, false);
        }

        /// <summary>
        /// Inserts a recycled container in the visual tree
        /// </summary>
        /// <param name="childIndex"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        private bool InsertRecycledContainer(int childIndex, UIElement container)
        {
            return InsertContainer(childIndex, container, true);
        }


        /// <summary>
        /// Inserts a container into the Children collection.  The container is either new or recycled.
        /// </summary>
        /// <param name="childIndex"></param>
        /// <param name="container"></param>
        /// <param name="isRecycled"></param>
        private bool InsertContainer(int childIndex, UIElement container, bool isRecycled)
        {
            Debug.Assert(container != null, "Null container was generated");

            bool visualOrderChanged = false;
            UIElementCollection children = InternalChildren;

            //
            // Find the index in the Children collection where we hope to insert the container.
            // This is done by looking up the index of the container BEFORE the one we hope to insert.
            //
            // We have to do it this way because there could be recycled containers between the container we're looking for and the one before it.
            // By finding the index before the place we want to insert and adding one, we ensure that we'll insert the new container in the
            // proper location.
            //
            // In recycling mode childIndex is the index in the _realizedChildren list, not the index in the
            // Children collection.  We have to convert the index; we'll call the index in the Children collection
            // the visualTreeIndex.
            //

            int visualTreeIndex = 0;

            if (childIndex > 0)
            {
                visualTreeIndex = ChildIndexFromRealizedIndex(childIndex - 1);
                visualTreeIndex++;
            }
            else
            {
                visualTreeIndex = ChildIndexFromRealizedIndex(childIndex);
            }


            if (isRecycled && visualTreeIndex < children.Count && children[visualTreeIndex] == container)
            {
                // Don't insert if a recycled container is in the proper place already
            }
            else
            {
                if (visualTreeIndex < children.Count)
                {
                    int insertIndex = visualTreeIndex;
                    if (isRecycled && container.InternalVisualParent != null)
                    {
                        // If the container is recycled we have to remove it from its place in the visual tree and
                        // insert it in the proper location.   For perf we'll use an internal Move API that moves
                        // the first parameter to right before the second one.
                        Debug.Assert(children[visualTreeIndex] != null, "MoveVisualChild interprets a null destination as 'move to end'");
                        children.MoveVisualChild(container, children[visualTreeIndex]);
                        visualOrderChanged = true;
                    }
                    else
                    {
                        VirtualizingPanel.InsertInternalChild(children, insertIndex, container);
                    }
                }
                else
                {
                    if (isRecycled && container.InternalVisualParent != null)
                    {
                        // Recycled container is still in the tree; move it to the end
                        children.MoveVisualChild(container, null);
                        visualOrderChanged = true;
                    }
                    else
                    {
                        VirtualizingPanel.AddInternalChild(children, container);
                    }
                }
            }

            //
            // Keep realizedChildren in sync w/ the visual tree.
            //
            if (IsVirtualizing && InRecyclingMode)
            {
                //  There is a Watson crash report/bucket that childIndex is out
                // of range.  We believe this to arise because VS's VirtualizingTreeView
                // control somehow is able to remove items from the Items collection
                // after childIndex is set (in MeasureOverrideImpl) but before adding
                // the new container (here).    If that should happen, the childIndex
                // is wrong, and the call to Insert will crash.   Instead, just
                // recompute the RealizedChildren collection
                if (ItemsChangedDuringMeasure)
                {
                    _realizedChildren = null;
                }

                if (_realizedChildren != null)
                {
                    _realizedChildren.Insert(childIndex, container);
                }
                else
                {
                    // Creates _realizedChildren list and syncs with InternalChildren.
                    EnsureRealizedChildren();
                }
            }

            Generator.PrepareItemContainer(container);

            return visualOrderChanged;
        }




        private void EnsureCleanupOperation(bool delay)
        {
            if (delay)
            {
                bool noPendingOperations = true;
                if (_cleanupOperation != null)
                {
                    noPendingOperations = _cleanupOperation.Abort();
                    if (noPendingOperations)
                    {
                        _cleanupOperation = null;
                    }
                }
                if (noPendingOperations && (_cleanupDelay == null))
                {
                    _cleanupDelay = new DispatcherTimer();
                    _cleanupDelay.Tick += new EventHandler(OnDelayCleanup);
                    _cleanupDelay.Interval = TimeSpan.FromMilliseconds(500.0);
                    _cleanupDelay.Start();
                }
            }
            else
            {
                if ((_cleanupOperation == null) && (_cleanupDelay == null))
                {
                    _cleanupOperation = Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(OnCleanUp), null);
                }
            }
        }

        private bool PreviousChildIsGenerated(int childIndex)
        {
            GeneratorPosition position = new GeneratorPosition(childIndex, 0);
            position = Generator.GeneratorPositionFromIndex(Generator.IndexFromGeneratorPosition(position) - 1);
            return (position.Offset == 0 && position.Index >= 0);
        }


        /// <summary>
        /// Takes a container returned from Generator.GenerateNext() and places it in the visual tree if necessary.
        /// Takes into account whether the container is new, recycled, or already realized.
        /// </summary>
        /// <param name="childIndex"></param>
        /// <param name="child"></param>
        /// <param name="newlyRealized"></param>
        private bool AddContainerFromGenerator(int childIndex, UIElement child, bool newlyRealized, bool isBeforeViewport)
        {
            bool visualOrderChanged = false;

            if (!newlyRealized)
            {
                //
                // Container is either realized or recycled.  If it's realized do nothing; it already exists in the visual
                // tree in the proper place.
                //

                if (InRecyclingMode)
                {
                    // Note there's no check for IsVirtualizing here.  If the user has just flipped off virtualization it's possible that
                    // the Generator will still return some recycled containers until its list runs out.

                    IList children = RealizedChildren;

                    if (childIndex < 0 || childIndex >= children.Count || !(children[childIndex] == child))
                    {
                        Debug.Assert(!children.Contains(child), "we incorrectly identified a recycled container");

                        //
                        // We have a recycled container (if it was a realized container it would have been returned in the
                        // proper location).  Note also that recycled containers are NOT in the _realizedChildren list.
                        //

                        visualOrderChanged = InsertRecycledContainer(childIndex, child);
                    }
                    else
                    {
                        // previously realized child.
                    }
                }
#if DEBUG
                else
                {
                    // The following Assert is not valid in the InRibbonGallery scenario, so we skip it.
                    if (this.GetType().Name != "RibbonMenuItemsPanel")
                    {
                        // Not recycling; realized container
                        Debug.Assert(child == InternalChildren[childIndex], "Wrong child was generated");
                    }
                }
#endif
            }
            else
            {
                InsertNewContainer(childIndex, child);
            }

            return visualOrderChanged;
        }

        private void OnItemsRemove(ItemsChangedEventArgs args)
        {
            RemoveChildRange(args.Position, args.ItemCount, args.ItemUICount);
        }

        private void OnItemsReplace(ItemsChangedEventArgs args)
        {
            if (args.ItemUICount > 0)
            {
                Debug.Assert(args.ItemUICount == args.ItemCount, "Both ItemUICount and ItemCount should be equal or ItemUICount should be 0.");
                UIElementCollection children = InternalChildren;

                using (Generator.StartAt(args.Position, GeneratorDirection.Forward, true))
                {
                    for (int i=0; i<args.ItemUICount; ++i)
                    {
                        int childIndex = args.Position.Index + i;
                        bool newlyRealized;
                        UIElement child = Generator.GenerateNext(out newlyRealized) as UIElement;
                        Debug.Assert(child != null, "Null child was generated");
                        Debug.Assert(!newlyRealized, "newlyRealized should be false after Replace");

                        children.SetInternal(childIndex, child);
                        Generator.PrepareItemContainer(child);
                    }
                }
            }
        }

        private void OnItemsMove(ItemsChangedEventArgs args)
        {
            RemoveChildRange(args.OldPosition, args.ItemCount, args.ItemUICount);
        }

        private void RemoveChildRange(GeneratorPosition position, int itemCount, int itemUICount)
        {
            if (IsItemsHost)
            {
                UIElementCollection children = InternalChildren;
                int pos = position.Index;
                if (position.Offset > 0)
                {
                    // An item is being removed after the one at the index
                    pos++;
                }

                if (pos < children.Count)
                {
                    int uiCount = itemUICount;
                    Debug.Assert((itemCount == itemUICount) || (itemUICount == 0), "Both ItemUICount and ItemCount should be equal or ItemUICount should be 0.");
                    if (uiCount > 0)
                    {
                        VirtualizingPanel.RemoveInternalChildRange(children, pos, uiCount);

                        if (IsVirtualizing && InRecyclingMode)
                        {
                            _realizedChildren.RemoveRange(pos, uiCount);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Immediately cleans up any containers that have gone offscreen.  Called by MeasureOverride.
        /// When recycling this runs before generating and measuring children; otherwise it
        /// runs after measuring the children.
        /// </summary>
        private void CleanupContainers(int firstItemInExtendedViewportIndex,
            int itemsInExtendedViewportCount,
            ItemsControl itemsControl)
        {
            CleanupContainers(firstItemInExtendedViewportIndex,
                itemsInExtendedViewportCount,
                itemsControl,
                false /*timeBound*/,
                0 /*startTickCount*/);
        }

        /// <summary>
        /// Immediately cleans up any containers that have gone offscreen.  Called by MeasureOverride.
        /// When recycling this runs before generating and measuring children; otherwise it
        /// runs after measuring the children.
        /// </summary>
        private bool CleanupContainers(int firstItemInExtendedViewportIndex,
            int itemsInExtendedViewportCount,
            ItemsControl itemsControl,
            bool timeBound,
            int startTickCount)
        {
            Debug.Assert(IsVirtualizing, "Can't clean up containers if not virtualizing");
            Debug.Assert(itemsControl != null, "We can't cleanup if we aren't the itemshost");

            //
            // When called it removes children both before and after the viewport.
            //
            // firstItemInViewportIndex is the index of first data item that will be in the viewport
            // at the end of Measure.
            //
            // _firstItemInExtendedViewportIndex and _actualItemsInExtendedViewportCount refer to values from the previous
            // measure pass when this method is called before measuring children.
            // We still use the _actualItemsInExtendedViewportCount as an approximation of the viewport
            // dimensions and prune the children that lay beyond. Occassionally
            // if the children are of varying sizes this may lead to a bit of excess work
            // disconnecting and reconnecting a container representing the same item.
            // But the tradeoff is valuable to speed up the more common case where
            // the children are all homogenous. When this method is called after
            // measuring the children these values are valid and can be used to
            // determine the containers that are beyond the viewport.
            //

            IList children = RealizedChildren;
            if (children.Count == 0)
            {
                return false; // nothing to do
            }

            int cleanupRangeStart = -1;
            int cleanupCount = 0;
            int itemIndex = -1, lastItemIndex = -1;

            bool performCleanup = false;
            UIElement child;
            object item;
            bool isVirtualizing = IsVirtualizing;
            bool needsMoreCleanup = false;

            //
            // Iterate over all realized children and recycle or remove the ones
            // that are eligible.  Items NOT eligible for recycling or removal have
            // one or more of the following properties
            //  - inside the viewport
            //  - the item is its own container
            //  - has keyboard focus
            //  - is about to be brought into view
            //  - the CleanupVirtualizedItem event was canceled
            //

            for (int childIndex = 0; childIndex < children.Count; childIndex++)
            {
                if (timeBound)
                {
                    // It is possible for TickCount to wrap around about every 30 days.
                    // If that were to occur, then this particular cleanup may not be interrupted.
                    // That is OK since the worst that can happen is that there is more of a stutter than normal.
                    int totalMilliseconds = Environment.TickCount - startTickCount;
                    if ((totalMilliseconds > 50) && (cleanupCount > 0))
                    {
                        // Cleanup has been working for 50ms already and the user might start
                        // noticing a lag. Stop cleaning up and release the thread for other work.
                        // Cleanup will continue later.
                        // Don't break out until after at least one item has been found to cleanup.
                        // Otherwise, we might end up in an infinite loop.
                        needsMoreCleanup = true;
                        break;
                    }
                }

                child = (UIElement)children[childIndex];
                lastItemIndex = itemIndex;
                itemIndex = GetGeneratedIndex(childIndex);

                //
                // itemsControl.Items can change without notifying VirtualizingStackPanel
                // (when ItemsSource is not an ObservableCollection or does not implement
                // INotifyCollectionChanged). Fetch the item from the container instead of
                // referencing from the Items collection.
                //
                item = itemsControl.ItemContainerGenerator.ItemFromContainer(child);

                if (itemIndex - lastItemIndex != 1)
                {
                    //
                    // There's a generated gap between the current item
                    // and the last.  Clean up the last range of items.
                    //
                    performCleanup = true;
                }

                if (performCleanup)
                {
                    if (cleanupRangeStart >= 0 && cleanupCount > 0)
                    {
                        //
                        // We've hit a non-virtualizable container or a non-contiguous section.
                        //
                        CleanupRange(children, Generator, cleanupRangeStart, cleanupCount);

                        //
                        // CleanupRange just modified the _realizedChildren list.
                        // Adjust the childIndex.
                        //
                        childIndex -= cleanupCount;

                        cleanupCount = 0;
                        cleanupRangeStart = -1;
                    }

                    performCleanup = false;
                }

                if (((itemIndex < firstItemInExtendedViewportIndex) || (itemIndex >= firstItemInExtendedViewportIndex + itemsInExtendedViewportCount)) &&
                    itemIndex >= 0 && // The container is not already disconnected.
                    !((IGeneratorHost)itemsControl).IsItemItsOwnContainer(item) &&
                    !child.IsKeyboardFocusWithin &&
                    child != _bringIntoViewContainer &&
                    NotifyCleanupItem(child, itemsControl) &&
                    VirtualizingPanel.GetIsContainerVirtualizable(child))
                {
                    //
                    // The container is eligible to be virtualized
                    //
                    if (cleanupRangeStart == -1)
                    {
                        cleanupRangeStart = childIndex;
                    }

                    cleanupCount++;
                }
                else
                {
                    // Non-recyclable container;
                    performCleanup = true;
                }
            }

            if (cleanupRangeStart >= 0 && cleanupCount > 0)
            {
                CleanupRange(children, Generator, cleanupRangeStart, cleanupCount);
            }

            return needsMoreCleanup;
        }

        private void EnsureRealizedChildren()
        {
            Debug.Assert(InRecyclingMode, "This method only applies to recycling mode");
            if (_realizedChildren == null)
            {
                UIElementCollection children = InternalChildren;

                _realizedChildren = new List<UIElement>(children.Count);

                for (int i = 0; i < children.Count; i++)
                {
                    _realizedChildren.Add(children[i]);
                }
            }
        }


        [Conditional("DEBUG")]
        private void debug_VerifyRealizedChildren()
        {
            // Debug method that ensures the _realizedChildren list matches the realized containers in the Generator.
            Debug.Assert(IsVirtualizing && InRecyclingMode, "Realized children only exist when recycling");
            Debug.Assert(_realizedChildren != null, "Realized children must exist to verify it");
            System.Windows.Controls.ItemContainerGenerator generator = Generator as System.Windows.Controls.ItemContainerGenerator;
            ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);

            if (generator != null && itemsControl != null && itemsControl.IsGrouping == false)
            {
                foreach (UIElement child in InternalChildren)
                {
                    int dataIndex = generator.IndexFromContainer(child);

                    if (dataIndex == -1)
                    {
                        // Child is not in the generator's realized container list (i.e. it's a recycled container): ensure it's NOT in _realizedChildren.
                        Debug.Assert(!_realizedChildren.Contains(child), "_realizedChildren should not contain recycled containers");
                    }
                    else
                    {
                        // Child is a realized container; ensure it's in _realizedChildren at the proper place.
                        GeneratorPosition position = Generator.GeneratorPositionFromIndex(dataIndex);
                        Debug.Assert(_realizedChildren[position.Index] == child, "_realizedChildren is corrupt!");
                    }
                }
            }
        }

        [Conditional("DEBUG")]
        private void debug_AssertRealizedChildrenEqualVisualChildren()
        {
            if (IsVirtualizing && InRecyclingMode)
            {
                UIElementCollection children = InternalChildren;
                Debug.Assert(_realizedChildren.Count == children.Count, "Realized and visual children must match");

                for (int i = 0; i < children.Count; i++)
                {
                    Debug.Assert(_realizedChildren[i] == children[i], "Realized and visual children must match");
                }
            }
        }

        /// <summary>
        /// Takes an index from the realized list and returns the corresponding index in the Children collection
        /// </summary>
        /// <param name="realizedChildIndex"></param>
        /// <returns></returns>
        private int ChildIndexFromRealizedIndex(int realizedChildIndex)
        {
            //
            // If we're not recycling containers then we're not using a realizedChild index and no translation is necessary
            //
            if (IsVirtualizing && InRecyclingMode)
            {
                if (realizedChildIndex < _realizedChildren.Count)
                {
                    UIElement child = _realizedChildren[realizedChildIndex];
                    UIElementCollection children = InternalChildren;

                    for (int i = realizedChildIndex; i < children.Count; i++)
                    {
                        if (children[i] == child)
                        {
                            return i;
                        }
                    }

                    Debug.Assert(false, "We should have found a child");
                }
            }

            return realizedChildIndex;
        }

        /// <summary>
        /// Recycled containers still in the Children collection at the end of Measure should be disconnected
        /// from the visual tree.  Otherwise they're still visible to things like Arrange, keyboard navigation, etc.
        /// </summary>
        private void DisconnectRecycledContainers()
        {
            int realizedIndex = 0;
            UIElement visualChild;
            UIElement realizedChild = _realizedChildren.Count > 0 ? _realizedChildren[0] : null;
            UIElementCollection children = InternalChildren;

            for (int i = 0; i < children.Count; i++)
            {
                visualChild = children[i];

                if (visualChild == realizedChild)
                {
                    realizedIndex++;

                    if (realizedIndex < _realizedChildren.Count)
                    {
                        realizedChild = _realizedChildren[realizedIndex];
                    }
                    else
                    {
                        realizedChild = null;
                    }
                }
                else
                {
                    // The visual child is a recycled container
                    children.RemoveNoVerify(visualChild);
                    i--;
                }
            }

            debug_VerifyRealizedChildren();
            debug_AssertRealizedChildrenEqualVisualChildren();
        }

        private GeneratorPosition IndexToGeneratorPositionForStart(int index, out int childIndex)
        {
            IItemContainerGenerator generator = Generator;
            GeneratorPosition position = (generator != null) ? generator.GeneratorPositionFromIndex(index) : new GeneratorPosition(-1, index + 1);

            // determine the position in the children collection for the first
            // generated container.  This assumes that generator.StartAt will be called
            // with direction=Forward and  allowStartAtRealizedItem=true.
            childIndex = (position.Offset == 0) ? position.Index : position.Index + 1;

            return position;
        }


        #region Delayed Cleanup Methods

        //
        // Delayed Cleanup is used when the VirtualizationMode is standard (not recycling) and the panel is scrolling and item-based
        // It chooses to defer virtualizing items until there are enough available.  It then cleans them using a background priority dispatcher
        // work item
        //

        private void OnDelayCleanup(object sender, EventArgs e)
        {
            Debug.Assert(_cleanupDelay != null);

            bool needsMoreCleanup = false;

            try
            {
                needsMoreCleanup = CleanUp();
            }
            finally
            {
                // Cleanup the timer if more cleanup is unnecessary
                if (!needsMoreCleanup)
                {
                    _cleanupDelay.Stop();
                    _cleanupDelay = null;
                }
            }
        }

        private object OnCleanUp(object args)
        {
            Debug.Assert(_cleanupOperation != null);

            bool needsMoreCleanup = false;

            try
            {
                needsMoreCleanup = CleanUp();
            }
            finally
            {
                // Keeping this non-null until here in case cleaning up causes re-entrancy
                _cleanupOperation = null;
            }

            if (needsMoreCleanup)
            {
                EnsureCleanupOperation(true /* delay */);
            }

            return null;
        }

        private bool CleanUp()
        {
            Debug.Assert(!InRecyclingMode, "This method only applies to standard virtualization");
            ItemsControl itemsControl = null;
            ItemsControl.GetItemsOwnerInternal(this, out itemsControl);

            if (itemsControl == null || !IsVirtualizing || !IsItemsHost)
            {
                // Virtualization is turned off or we aren't hosting children; no need to cleanup.
                return false;
            }

            // if a MeasureCache is pending, postpone the cleanup.  The values of
            // _firstItemInExtendedViewportIndex and _actualItemsInExtendedViewportCount
            // aren't valid until MeasureCache has run.
            if (!IsVSP45Compat && IsMeasureCachesPending)
            {
                return true;
            }

            int startMilliseconds = Environment.TickCount;
            bool needsMoreCleanup = false;
            UIElementCollection children = InternalChildren;
            int minDesiredGenerated = MinDesiredGenerated;
            int maxDesiredGenerated = MaxDesiredGenerated;
            int pageSize = maxDesiredGenerated - minDesiredGenerated;
            int extraChildren = children.Count - pageSize;

            if (HasVirtualizingChildren || (extraChildren > (pageSize * 2)))
            {
                if ((Mouse.LeftButton == MouseButtonState.Pressed) &&
                    (extraChildren < 1000))
                {
                    // An optimization for when we are dragging the mouse.
                    needsMoreCleanup = true;
                }
                else
                {
                    needsMoreCleanup = CleanupContainers(_firstItemInExtendedViewportIndex,
                        _actualItemsInExtendedViewportCount,
                        itemsControl,
                        true /*timeBound*/,
                        startMilliseconds);
                }
            }

            return needsMoreCleanup;
        }

        private bool NotifyCleanupItem(int childIndex, UIElementCollection children, ItemsControl itemsControl)
        {
            return NotifyCleanupItem(children[childIndex], itemsControl);
        }

        private bool NotifyCleanupItem(UIElement child, ItemsControl itemsControl)
        {
            CleanUpVirtualizedItemEventArgs e = new CleanUpVirtualizedItemEventArgs(itemsControl.ItemContainerGenerator.ItemFromContainer(child), child);
            e.Source = this;
            OnCleanUpVirtualizedItem(e);

            return !e.Cancel;
        }

        private void CleanupRange(IList children, IItemContainerGenerator generator, int startIndex, int count)
        {
            if (InRecyclingMode)
            {
                Debug.Assert(startIndex >= 0 && count > 0);
                Debug.Assert(children == _realizedChildren, "the given child list must be the _realizedChildren list when recycling");

                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                {
                    List<String> list = new List<String>(count);
                    for (int i=0; i<count; ++i)
                    {
                        list.Add(ContainerPath((DependencyObject)children[startIndex + i]));
                    }

                    ScrollTracer.Trace(this, ScrollTraceOp.RecycleChildren,
                        startIndex, count, list);
                }

                ((IRecyclingItemContainerGenerator)generator).Recycle(new GeneratorPosition(startIndex, 0), count);

                // The call to Recycle has caused the ItemContainerGenerator to remove some items
                // from its list of realized items; we adjust _realizedChildren to match.
                _realizedChildren.RemoveRange(startIndex, count);
            }
            else
            {
                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                {
                    List<String> list = new List<String>(count);
                    for (int i=0; i<count; ++i)
                    {
                        list.Add(ContainerPath((DependencyObject)children[startIndex + i]));
                    }

                    ScrollTracer.Trace(this, ScrollTraceOp.RemoveChildren,
                        startIndex, count, list);
                }

                // Remove the desired range of children
                VirtualizingPanel.RemoveInternalChildRange((UIElementCollection)children, startIndex, count);
                generator.Remove(new GeneratorPosition(startIndex, 0), count);

                // We only need to adjust the childIndex if the visual tree
                // is changing and this is the only case that that happens
                AdjustFirstVisibleChildIndex(startIndex, count);
            }
        }

        #endregion

        /// <summary>
        /// Called after 'count' items were removed or recycled from the Generator.  _firstItemInExtendedViewportChildIndex is the
        /// index of the first visible container.  This index isn't exactly the child position in the UIElement collection;
        /// it's actually the index of the realized container inside the generator.  Since we've just removed some realized
        /// containers from the generator (by calling Remove or Recycle), we have to adjust the first visible child index.
        /// </summary>
        /// <param name="startIndex">index of the first removed item</param>
        /// <param name="count">number of items removed</param>
        private void AdjustFirstVisibleChildIndex(int startIndex, int count)
        {
            //
            // Update the index of the first visible generated child
            //
            if (startIndex < _firstItemInExtendedViewportChildIndex)
            {
                int endIndex = startIndex + count - 1;
                if (endIndex < _firstItemInExtendedViewportChildIndex)
                {
                    // The first visible index is after the items that were removed
                    _firstItemInExtendedViewportChildIndex -= count;
                }
                else
                {
                    // The first visible index was within the items that were removed
                    _firstItemInExtendedViewportChildIndex = startIndex;
                }
            }
        }

        private int MinDesiredGenerated
        {
            get
            {
                return Math.Max(0, _firstItemInExtendedViewportIndex);
            }
        }

        private int MaxDesiredGenerated
        {
            get
            {
                return Math.Min(ItemCount, _firstItemInExtendedViewportIndex + _actualItemsInExtendedViewportCount);
            }
        }

        private int ItemCount
        {
            get
            {
                EnsureGenerator();
                return((ItemContainerGenerator)Generator).ItemsInternal.Count;
            }
        }

        private void EnsureScrollData()
        {
            if (_scrollData == null) { _scrollData = new ScrollData(); }
            else
            {
                Debug.Assert(_scrollData._scrollOwner != null, "Scrolling an unconnected VSP");
            }
        }

        private static void ResetScrolling(VirtualizingStackPanel element)
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

        /// <summary>
        ///     Sets the scolling Data
        /// </summary>
        // This is the 4.5.1+ version of the method.  Fixes many bugs.
        private void SetAndVerifyScrollingData(
            bool isHorizontal,
            Rect viewport,
            Size constraint,
            UIElement firstContainerInViewport,
            double firstContainerOffsetFromViewport,
            bool hasAverageContainerSizeChanged,
            double newOffset,
            ref Size stackPixelSize,
            ref Size stackLogicalSize,
            ref Size stackPixelSizeInViewport,
            ref Size stackLogicalSizeInViewport,
            ref Size stackPixelSizeInCacheBeforeViewport,
            ref Size stackLogicalSizeInCacheBeforeViewport,
            ref bool remeasure,
            ref double? lastPageSafeOffset,
            ref double? lastPagePixelSize,
            ref List<double> previouslyMeasuredOffsets)
        {
            Debug.Assert(IsScrolling, "ScrollData must only be set on a scrolling panel.");

            Vector computedViewportOffset, viewportOffset;
            Size viewportSize;
            Size extentSize;

            computedViewportOffset = new Vector(viewport.Location.X, viewport.Location.Y);

            Vector offsetForScrollViewerRemeasure = _scrollData._offset;

            if (IsPixelBased)
            {
                extentSize = stackPixelSize;
                viewportSize = viewport.Size;
            }
            else
            {
                extentSize = stackLogicalSize;
                viewportSize = stackLogicalSizeInViewport;

                if (isHorizontal)
                {
                    if (DoubleUtil.GreaterThan(stackPixelSizeInViewport.Width, constraint.Width) &&
                        viewportSize.Width > 1)
                    {
                        viewportSize.Width--;
                    }

                    viewportSize.Height = viewport.Height;
                }
                else
                {
                    if (DoubleUtil.GreaterThan(stackPixelSizeInViewport.Height, constraint.Height) &&
                        viewportSize.Height > 1)
                    {
                        viewportSize.Height--;
                    }

                    viewportSize.Width = viewport.Width;
                }
            }

            if (isHorizontal)
            {
                if (MeasureCaches && IsVirtualizing)
                {
                    //
                    // We do not want the cache measure pass to affect the visibility
                    // of the scrollbars because this makes bad user experience and
                    // is also the source of scrolling bugs.
                    //

                    stackPixelSize.Height = _scrollData._extent.Height;
                }

                // In order to avoid fluctuations in the minor axis scrollbar visibility
                // as we scroll items of varying dimensions in and out of the viewport,
                // we cache the _maxDesiredSize along that dimension and return that
                // instead.
                _scrollData._maxDesiredSize.Height = Math.Max(_scrollData._maxDesiredSize.Height, stackPixelSize.Height);
                stackPixelSize.Height = _scrollData._maxDesiredSize.Height;

                extentSize.Height = stackPixelSize.Height;

                if (Double.IsPositiveInfinity(constraint.Height))
                {
                    viewportSize.Height = stackPixelSize.Height;
                }
            }
            else
            {
                if (MeasureCaches && IsVirtualizing)
                {
                    //
                    // We do not want the cache measure pass to affect the visibility
                    // of the scrollbars because this makes bad user experience and
                    // is also the source of scrolling bugs.
                    //

                    stackPixelSize.Width = _scrollData._extent.Width;
                }

                // In order to avoid fluctuations in the minor axis scrollbar visibility
                // as we scroll items of varying dimensions in and out of the viewport,
                // we cache the _maxDesiredSize along that dimension and return that
                // instead.
                _scrollData._maxDesiredSize.Width = Math.Max(_scrollData._maxDesiredSize.Width, stackPixelSize.Width);
                stackPixelSize.Width = _scrollData._maxDesiredSize.Width;

                extentSize.Width = stackPixelSize.Width;

                if (Double.IsPositiveInfinity(constraint.Width))
                {
                    viewportSize.Width = stackPixelSize.Width;
                }
            }

            //
            // Since we can offset and clip our content, we never need to be larger than the parent suggestion.
            // If we returned the full size of the content, we would always be so big we didn't need to scroll.  :)
            //
            // Now consider item scrolling cases where we are scrolling to the very last page. In these cases
            // there may be a small region left which is not big enough to fit an entire item. So the sizes of the
            // items accrued within the viewport may be less than the constraint. But we still want to return the
            // constraint dimensions so that we do not unnecessarily cause the parents to be invalidated and
            // re-measured.
            //
            // However if the constraint is infinite, don't change the stack size. This avoids
            // returning an infinite desired size, which is forbidden.
            if (!Double.IsPositiveInfinity(constraint.Width))
            {
                stackPixelSize.Width = IsPixelBased || DoubleUtil.AreClose(computedViewportOffset.X, 0) ?
                    Math.Min(stackPixelSize.Width, constraint.Width) : constraint.Width;
            }
            if (!Double.IsPositiveInfinity(constraint.Height))
            {
                stackPixelSize.Height = IsPixelBased || DoubleUtil.AreClose(computedViewportOffset.Y, 0) ?
                    Math.Min(stackPixelSize.Height, constraint.Height) : constraint.Height;
            }

#if DEBUG
            if (!IsPixelBased)
            {
                // Verify that ViewportSize and ExtentSize are not fractional. Offset can be fractional since it is used to
                // to accumulate the fractional changes, but only the whole value is used for further computations.
                if (isHorizontal)
                {
                    Debug.Assert(DoubleUtil.AreClose(viewportSize.Width - Math.Floor(viewportSize.Width), 0.0), "The viewport size must not contain fractional values when in item scrolling mode.");
                    Debug.Assert(DoubleUtil.AreClose(extentSize.Width - Math.Floor(extentSize.Width), 0.0), "The extent size must not contain fractional values when in item scrolling mode.");
                }
                else
                {
                    Debug.Assert(DoubleUtil.AreClose(viewportSize.Height - Math.Floor(viewportSize.Height), 0.0), "The viewport size must not contain fractional values when in item scrolling mode.");
                    Debug.Assert(DoubleUtil.AreClose(extentSize.Height - Math.Floor(extentSize.Height), 0.0), "The extent size must not contain fractional values when in item scrolling mode.");
                }
            }
#endif

            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.SVSDBegin,
                    "isa:", IsScrollActive,
                    "mc:", MeasureCaches,
                    "o:", _scrollData._offset,
                    "co:", computedViewportOffset,
                    "ex:", extentSize,
                    "vs:", viewportSize,
                    "pxInV:", stackPixelSizeInViewport);

                if (hasAverageContainerSizeChanged)
                {
                    ScrollTracer.Trace(this, ScrollTraceOp.SVSDBegin,
                        "acs:", UniformOrAverageContainerSize, UniformOrAverageContainerPixelSize);
                }
            }

            // remember whether the viewport was moved from the end of the list
            // (do this before changing computedViewportOffset)
            bool wasViewportOffsetCoerced =
                isHorizontal ? (!DoubleUtil.AreClose(computedViewportOffset.X, _scrollData._offset.X) ||
                                (IsScrollActive && computedViewportOffset.X > 0.0 && DoubleUtil.GreaterThanOrClose(computedViewportOffset.X, _scrollData.Extent.Width-_scrollData.Viewport.Width)))
                             : (!DoubleUtil.AreClose(computedViewportOffset.Y, _scrollData._offset.Y) ||
                                (IsScrollActive && computedViewportOffset.Y > 0.0 && DoubleUtil.GreaterThanOrClose(computedViewportOffset.Y, _scrollData.Extent.Height-_scrollData.Viewport.Height)));
            bool wasPerpendicularOffsetCoerced =
                isHorizontal ? (!DoubleUtil.AreClose(computedViewportOffset.Y, _scrollData._offset.Y) ||
                                (IsScrollActive && computedViewportOffset.Y > 0.0 && DoubleUtil.GreaterThanOrClose(computedViewportOffset.Y, _scrollData.Extent.Height-_scrollData.Viewport.Height)))
                             : (!DoubleUtil.AreClose(computedViewportOffset.X, _scrollData._offset.X) ||
                                (IsScrollActive && computedViewportOffset.X > 0.0 && DoubleUtil.GreaterThanOrClose(computedViewportOffset.X, _scrollData.Extent.Width-_scrollData.Viewport.Width)));
            bool discardOffsets = false;

            // when the average container size changes during a measure pass, all
            // scroll offsets are potentially changed.  Treat this as if we had
            // actually measured using the new scroll offset, provided that there's
            // enough information available to compute it.
            if (hasAverageContainerSizeChanged && newOffset >= 0.0)
            {
                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                {
                    ScrollTracer.Trace(this, ScrollTraceOp.AdjustOffset,
                        newOffset, computedViewportOffset);
                }

                if (isHorizontal)
                {
                    if (!LayoutDoubleUtil.AreClose(computedViewportOffset.X, newOffset))
                    {
                        double delta = newOffset - computedViewportOffset.X;
                        computedViewportOffset.X = newOffset;
                        offsetForScrollViewerRemeasure.X = newOffset;

                        // adjust the persisted viewports too, in case the next use
                        // occurs before a measure, e.g. adding item to Items
                        _viewport.X = newOffset;
                        _extendedViewport.X += delta;

                        discardOffsets = true;

                        // if the new offset is close to the end, treat it like
                        // a scroll to the end
                        if (DoubleUtil.GreaterThan(newOffset + viewportSize.Width, extentSize.Width))
                        {
                            wasViewportOffsetCoerced = true;
                            IsScrollActive = true;
                            _scrollData.HorizontalScrollType = ScrollType.ToEnd;
                        }
                    }
                }
                else
                {
                    if (!LayoutDoubleUtil.AreClose(computedViewportOffset.Y, newOffset))
                    {
                        double delta = newOffset - computedViewportOffset.Y;
                        computedViewportOffset.Y = newOffset;
                        offsetForScrollViewerRemeasure.Y = newOffset;

                        // adjust the persisted viewports too, in case the next use
                        // occurs before a measure, e.g. adding item to Items
                        _viewport.Y = newOffset;
                        _extendedViewport.Y += delta;

                        // if the new offset is close to the end, treat it like
                        // a scroll to the end
                        if (DoubleUtil.GreaterThan(newOffset + viewportSize.Height, extentSize.Height))
                        {
                            wasViewportOffsetCoerced = true;
                            IsScrollActive = true;
                            _scrollData.VerticalScrollType = ScrollType.ToEnd;
                        }
                    }
                }
            }

            // similarly, if we're scrolling to the end and using the last safe offset,
            // and the pixel size in the viewport changes (because of a size change
            // within the children), discard the saved offsets and start looking for
            // a better candidate
            if (lastPagePixelSize.HasValue && !lastPageSafeOffset.HasValue &&
                !DoubleUtil.AreClose(isHorizontal ? stackPixelSizeInViewport.Width : stackPixelSizeInViewport.Height,
                                    (double)lastPagePixelSize))
            {
                discardOffsets = true;
                wasViewportOffsetCoerced = true;

                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                {
                    ScrollTracer.Trace(this, ScrollTraceOp.LastPageSizeChange,
                        computedViewportOffset,
                        stackPixelSizeInViewport, lastPagePixelSize);
                }
            }

            if (discardOffsets)
            {
                // All saved offsets are now meaningless.  Discard them.
                if (previouslyMeasuredOffsets != null)
                {
                    previouslyMeasuredOffsets.Clear();
                }
                lastPageSafeOffset = null;
                lastPagePixelSize = null;
            }

            // Detect changes to the viewportSize, extentSize, and computedViewportOffset
            bool viewportSizeChanged = !DoubleUtil.AreClose(viewportSize, _scrollData._viewport);
            bool extentSizeChanged = !DoubleUtil.AreClose(extentSize, _scrollData._extent);
            bool computedViewportOffsetChanged = !DoubleUtil.AreClose(computedViewportOffset, _scrollData._computedOffset);

            bool extentWidthChanged, extentHeightChanged;
            if (extentSizeChanged)
            {
                extentWidthChanged = !DoubleUtil.AreClose(extentSize.Width, _scrollData._extent.Width);
                extentHeightChanged = !DoubleUtil.AreClose(extentSize.Height, _scrollData._extent.Height);
            }
            else
            {
                extentWidthChanged = extentHeightChanged = false;
            }

            //
            // Check if we need to repeat the measure operation and adjust the
            // scroll offset and/or viewport size for this new iteration.
            //
            viewportOffset = computedViewportOffset;

            //
            // Check to see if we want to disregard this measure because the ScrollViewer is
            // about to remeasure this panel with changed ScrollBarVisibility. In this case we
            // should allow the ScrollViewer to retry the transition to the original offset.
            //

            bool scrollViewerWillRemeasure = false;

            ScrollViewer sv = ScrollOwner;
            if (sv.InChildMeasurePass1 || sv.InChildMeasurePass2)
            {
                ScrollBarVisibility vsbv = sv.VerticalScrollBarVisibility;
                bool vsbAuto = (vsbv == ScrollBarVisibility.Auto);

                if (vsbAuto)
                {
                    Visibility oldvv = sv.ComputedVerticalScrollBarVisibility;
                    Visibility newvv = DoubleUtil.LessThanOrClose(extentSize.Height, viewportSize.Height) ? Visibility.Collapsed : Visibility.Visible;
                    if (oldvv != newvv)
                    {
                        viewportOffset = offsetForScrollViewerRemeasure;
                        scrollViewerWillRemeasure = true;
                    }
                }

                if (!scrollViewerWillRemeasure)
                {
                    ScrollBarVisibility hsbv = sv.HorizontalScrollBarVisibility;
                    bool hsbAuto = (hsbv == ScrollBarVisibility.Auto);

                    if (hsbAuto)
                    {
                        Visibility oldhv = sv.ComputedHorizontalScrollBarVisibility;
                        Visibility newhv = DoubleUtil.LessThanOrClose(extentSize.Width, viewportSize.Width) ? Visibility.Collapsed : Visibility.Visible;
                        if (oldhv != newhv)
                        {
                            viewportOffset = offsetForScrollViewerRemeasure;
                            scrollViewerWillRemeasure = true;
                        }
                    }
                }

                if (scrollViewerWillRemeasure && ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                {
                    ScrollTracer.Trace(this, ScrollTraceOp.ScrollBarChangeVisibility,
                        viewportOffset);
                }
            }

            //
            // determine whether a remeasure is needed
            //

            if (isHorizontal)
            {
                // look for other reasons to remeasure
                if (scrollViewerWillRemeasure)
                {
                    // no further action is required if
                    // (a) ScrollViewer is already going to remeasure - let it proceed
                }
                else if (WasOffsetPreviouslyMeasured(previouslyMeasuredOffsets, computedViewportOffset.X))
                {
                    //
                    // If we've encountered a cycle in the list of offsets that we measured against
                    // when trying to navigate to the last page, we settle on the best available this far.
                    //
                    if (!IsPixelBased && lastPageSafeOffset.HasValue && !DoubleUtil.AreClose((double)lastPageSafeOffset, computedViewportOffset.X))
                    {
                        if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                        {
                            ScrollTracer.Trace(this, ScrollTraceOp.RemeasureCycle,
                                viewportOffset.X, lastPageSafeOffset);
                        }

                        viewportOffset.X = (double)lastPageSafeOffset;
                        lastPageSafeOffset = null;
                        remeasure = true;
                    }
                }
                else if (!remeasure)
                {
                    if (!IsPixelBased)
                    {
                        //
                        // If the viewportSize has the potential to increase and we are scrolling to the very end
                        // then we need to scoot back the offset to fit more in the viewport.
                        //
                        if (!remeasure && !IsEndOfViewport(isHorizontal, viewport, stackPixelSizeInViewport) &&
                            DoubleUtil.GreaterThan(stackLogicalSize.Width, stackLogicalSizeInViewport.Width))
                        {
                            //
                            // When navigating to the last page remember the smallest offset that
                            // was able to display the last item in the collection
                            //
                            if (!lastPageSafeOffset.HasValue || computedViewportOffset.X < (double)lastPageSafeOffset)
                            {
                                lastPageSafeOffset = computedViewportOffset.X;
                                lastPagePixelSize = stackPixelSizeInViewport.Width;
                            }

                            double approxSizeOfLogicalUnit = stackPixelSizeInViewport.Width / stackLogicalSizeInViewport.Width;
                            double proposedViewportSize = Math.Floor(viewport.Width / approxSizeOfLogicalUnit);
                            if (DoubleUtil.GreaterThan(proposedViewportSize, viewportSize.Width))
                            {
                                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                                {
                                    ScrollTracer.Trace(this, ScrollTraceOp.RemeasureEndExpandViewport,
                                        "off:", computedViewportOffset.X, lastPageSafeOffset,
                                        "pxSz:", stackPixelSizeInViewport.Width, viewport.Width,
                                        "itSz:", stackLogicalSizeInViewport.Width, viewportSize.Width,
                                        "newVpSz:", proposedViewportSize);
                                }

                                viewportOffset.X = Double.PositiveInfinity;
                                viewportSize.Width = proposedViewportSize;
                                remeasure = true;
                                StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.X);
                            }
                        }

                        //
                        // If the viewportSize has decreased and we are scrolling to the very end then we need
                        // to scoot the offset forward to infact be at the very end.
                        //
                        if (!remeasure && wasViewportOffsetCoerced && viewportSizeChanged &&
                            !DoubleUtil.AreClose(_scrollData._viewport.Width, viewportSize.Width))
                        {
                            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                            {
                                ScrollTracer.Trace(this, ScrollTraceOp.RemeasureEndChangeOffset,
                                    "off:", computedViewportOffset.X,
                                    "vpSz:", _scrollData._viewport.Width, viewportSize.Width,
                                    "newOff:", _scrollData._offset);
                            }

                            remeasure = true;
                            viewportOffset.X = Double.PositiveInfinity;
                            StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.X);

                            if (DoubleUtil.AreClose(viewportSize.Width, 0))
                            {
                                viewportSize.Width = _scrollData._viewport.Width;
                            }
                        }
                    }

                    if (!remeasure && extentWidthChanged)
                    {
                        //
                        // If the extentSize has changed in the scrolling direction, and
                        // we are scrolling to the very end, scoot the offset to fit
                        // more in the viewport.  We're scrolling to the end if either
                        // (a) the current attempt was scrolling to the end
                        // (b) the new extent and offset indicate scrolling to the end
                        //
                        if (_scrollData.HorizontalScrollType == ScrollType.ToEnd ||
                              ( DoubleUtil.GreaterThan(computedViewportOffset.X, 0.0) &&
                                DoubleUtil.GreaterThan(computedViewportOffset.X, extentSize.Width - viewportSize.Width)))
                        {
                            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                            {
                                ScrollTracer.Trace(this, ScrollTraceOp.RemeasureEndExtentChanged,
                                    "off:", computedViewportOffset.X,
                                    "ext:", _scrollData._extent.Width, extentSize.Width,
                                    "vpSz:", viewportSize.Width);
                            }

                            remeasure = true;
                            viewportOffset.X = Double.PositiveInfinity;
                            _scrollData.HorizontalScrollType = ScrollType.ToEnd;
                            StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.X);
                        }

                        //
                        // If the extentSize has changed and we are making an absolute
                        // move to an offset, we need to readjust the offset to be at the
                        // same percentage location with the respect to the new extent as
                        // was initially intended.
                        else if (_scrollData.HorizontalScrollType == ScrollType.Absolute)
                        {
                            if (!DoubleUtil.AreClose(_scrollData._extent.Width, 0) &&
                                !DoubleUtil.AreClose(extentSize.Width, 0))
                            {
                                if (IsPixelBased)
                                {
                                    if (!LayoutDoubleUtil.AreClose(computedViewportOffset.X/extentSize.Width, _scrollData._offset.X/_scrollData._extent.Width))
                                    {
                                        remeasure = true;
                                        viewportOffset.X = (extentSize.Width * _scrollData._offset.X) / _scrollData._extent.Width;
                                        StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.X);
                                    }
                                }
                                else
                                {
                                    if (!LayoutDoubleUtil.AreClose(Math.Floor(computedViewportOffset.X)/extentSize.Width, Math.Floor(_scrollData._offset.X)/_scrollData._extent.Width))
                                    {
                                        remeasure = true;
                                        viewportOffset.X = Math.Floor((extentSize.Width * Math.Floor(_scrollData._offset.X)) / _scrollData._extent.Width);
                                        StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.X);
                                    }
                                }

                                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this) && remeasure)
                                {
                                    ScrollTracer.Trace(this, ScrollTraceOp.RemeasureRatio,
                                        "expRat:", _scrollData._offset.X, _scrollData._extent.Width, (_scrollData._offset.X/_scrollData._extent.Width),
                                        "actRat:", computedViewportOffset.X, extentSize.Width, (computedViewportOffset.X/extentSize.Width),
                                        "newOff:", viewportOffset.X);
                                }
                            }
                        }
                    }

                    if (!remeasure && extentHeightChanged)
                    {
                        //
                        // If the extentSize has changed in the non-scrolling direction, and
                        // we are scrolling to the very end, scoot the offset to fit
                        // more in the viewport.  We're scrolling to the end if either
                        // (a) the current attempt was scrolling to the end
                        // (b) the new extent and offset indicate scrolling to the end
                        //
                        if (_scrollData.VerticalScrollType == ScrollType.ToEnd ||
                              ( DoubleUtil.GreaterThan(computedViewportOffset.Y, 0.0) &&
                                DoubleUtil.GreaterThan(computedViewportOffset.Y, extentSize.Height - viewportSize.Height)))
                        {
                            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                            {
                                ScrollTracer.Trace(this, ScrollTraceOp.RemeasureEndExtentChanged,
                                    "perp",
                                    "off:", computedViewportOffset.Y,
                                    "ext:", _scrollData._extent.Height, extentSize.Height,
                                    "vpSz:", viewportSize.Height);
                            }

                            remeasure = true;
                            viewportOffset.Y = Double.PositiveInfinity;
                            _scrollData.VerticalScrollType = ScrollType.ToEnd;
                        }

                        //
                        // If the extentSize has changed and we are making an absolute
                        // move to an offset, we need to readjust the offset to be at the
                        // same percentage location with the respect to the new extent as
                        // was initially intended.
                        else if (_scrollData.VerticalScrollType == ScrollType.Absolute)
                        {
                            if (!DoubleUtil.AreClose(_scrollData._extent.Height, 0) &&
                                !DoubleUtil.AreClose(extentSize.Height, 0))
                            {
                                if (!LayoutDoubleUtil.AreClose(computedViewportOffset.Y/extentSize.Height, _scrollData._offset.Y/_scrollData._extent.Height))
                                {
                                    remeasure = true;
                                    viewportOffset.Y = (extentSize.Height * _scrollData._offset.Y) / _scrollData._extent.Height;
                                }

                                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this) && remeasure)
                                {
                                    ScrollTracer.Trace(this, ScrollTraceOp.RemeasureRatio,
                                        "perp",
                                        "expRat:", _scrollData._offset.Y, _scrollData._extent.Height, (_scrollData._offset.Y/_scrollData._extent.Height),
                                        "actRat:", computedViewportOffset.Y, extentSize.Height, (computedViewportOffset.Y/extentSize.Height),
                                        "newOff:", viewportOffset.Y);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // look for other reasons to remeasure
                if (scrollViewerWillRemeasure)
                {
                    // no further action is required if
                    // (a) ScrollViewer is already going to remeasure - let it proceed
                }
                else if (WasOffsetPreviouslyMeasured(previouslyMeasuredOffsets, computedViewportOffset.Y))
                {
                    //
                    // If we've encountered a cycle in the list of offsets that we measured against
                    // when trying to navigate to the last page, we settle on the best available this far.
                    //
                    if (!IsPixelBased && lastPageSafeOffset.HasValue && !DoubleUtil.AreClose((double)lastPageSafeOffset, computedViewportOffset.Y))
                    {
                        if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                        {
                            ScrollTracer.Trace(this, ScrollTraceOp.RemeasureCycle,
                                viewportOffset.Y, lastPageSafeOffset);
                        }

                        viewportOffset.Y = (double)lastPageSafeOffset;
                        lastPageSafeOffset = null;
                        remeasure = true;
                    }
                }
                else if (!remeasure)
                {
                    if (!IsPixelBased)
                    {
                        //
                        // If the viewportSize has the potential to increase and we are scrolling to the very end
                        // then we need to scoot back the offset to fit more in the viewport.
                        //
                        if (!remeasure && !IsEndOfViewport(isHorizontal, viewport, stackPixelSizeInViewport) &&
                            DoubleUtil.GreaterThan(stackLogicalSize.Height, stackLogicalSizeInViewport.Height))
                        {
                            //
                            // When navigating to the last page remember the smallest offset that
                            // was able to display the last item in the collection
                            //
                            if (!lastPageSafeOffset.HasValue || computedViewportOffset.Y < (double)lastPageSafeOffset)
                            {
                                lastPageSafeOffset = computedViewportOffset.Y;
                                lastPagePixelSize = stackPixelSizeInViewport.Height;
                            }

                            double approxSizeOfLogicalUnit = stackPixelSizeInViewport.Height / stackLogicalSizeInViewport.Height;
                            double proposedViewportSize = Math.Floor(viewport.Height / approxSizeOfLogicalUnit);
                            if (DoubleUtil.GreaterThan(proposedViewportSize, viewportSize.Height))
                            {
                                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                                {
                                    ScrollTracer.Trace(this, ScrollTraceOp.RemeasureEndExpandViewport,
                                        "off:", computedViewportOffset.Y, lastPageSafeOffset,
                                        "pxSz:", stackPixelSizeInViewport.Height, viewport.Height,
                                        "itSz:", stackLogicalSizeInViewport.Height, viewportSize.Height,
                                        "newVpSz:", proposedViewportSize);
                                }

                                viewportOffset.Y = Double.PositiveInfinity;
                                viewportSize.Height = proposedViewportSize;
                                remeasure = true;
                                StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.Y);
                            }
                        }

                        //
                        // If the viewportSize has decreased and we are scrolling to the very end then we need
                        // to scoot the offset forward to infact be at the very end.
                        //
                        if (!remeasure && wasViewportOffsetCoerced && viewportSizeChanged &&
                            !DoubleUtil.AreClose(_scrollData._viewport.Height, viewportSize.Height))
                        {
                            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                            {
                                ScrollTracer.Trace(this, ScrollTraceOp.RemeasureEndChangeOffset,
                                    "off:", computedViewportOffset.Y,
                                    "vpSz:", _scrollData._viewport.Height, viewportSize.Height,
                                    "newOff:", _scrollData._offset);
                            }

                            remeasure = true;
                            viewportOffset.Y = Double.PositiveInfinity;
                            StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.Y);

                            if (DoubleUtil.AreClose(viewportSize.Height, 0))
                            {
                                viewportSize.Height = _scrollData._viewport.Height;
                            }
                        }
                    }

                    if (!remeasure && extentHeightChanged)
                    {
                        //
                        // If the extentSize has changed in the scrolling direction, and
                        // we are scrolling to the very end, scoot the offset to fit
                        // more in the viewport.  We're scrolling to the end if either
                        // (a) the current attempt was scrolling to the end
                        // (b) the new extent and offset indicate scrolling to the end
                        //
                        if (_scrollData.VerticalScrollType == ScrollType.ToEnd ||
                              ( DoubleUtil.GreaterThan(computedViewportOffset.Y, 0.0) &&
                                DoubleUtil.GreaterThan(computedViewportOffset.Y, extentSize.Height - viewportSize.Height)))
                        {
                            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                            {
                                ScrollTracer.Trace(this, ScrollTraceOp.RemeasureEndExtentChanged,
                                    "off:", computedViewportOffset.Y,
                                    "ext:", _scrollData._extent.Height, extentSize.Height,
                                    "vpSz:", viewportSize.Height);
                            }

                            remeasure = true;
                            viewportOffset.Y = Double.PositiveInfinity;
                            _scrollData.VerticalScrollType = ScrollType.ToEnd;
                            StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.Y);
                        }

                        //
                        // If the extentSize has changed and we are making an absolute
                        // move to an offset, we need to readjust the offset to be at the
                        // same percentage location with the respect to the new extent as
                        // was initially intended.
                        else if (_scrollData.VerticalScrollType == ScrollType.Absolute)
                        {
                            if (!DoubleUtil.AreClose(_scrollData._extent.Height, 0) &&
                                !DoubleUtil.AreClose(extentSize.Height, 0))
                            {
                                if (IsPixelBased)
                                {
                                    if (!LayoutDoubleUtil.AreClose(computedViewportOffset.Y/extentSize.Height, _scrollData._offset.Y/_scrollData._extent.Height))
                                    {
                                        remeasure = true;
                                        viewportOffset.Y = (extentSize.Height * _scrollData._offset.Y) / _scrollData._extent.Height;
                                        StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.Y);
                                    }
                                }
                                else
                                {
                                    if (!LayoutDoubleUtil.AreClose(Math.Floor(computedViewportOffset.Y)/extentSize.Height, Math.Floor(_scrollData._offset.Y)/_scrollData._extent.Height))
                                    {
                                        remeasure = true;
                                        viewportOffset.Y = Math.Floor((extentSize.Height * Math.Floor(_scrollData._offset.Y)) / _scrollData._extent.Height);
                                    }
                                }

                                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this) && remeasure)
                                {
                                    ScrollTracer.Trace(this, ScrollTraceOp.RemeasureRatio,
                                        "expRat:", _scrollData._offset.Y, _scrollData._extent.Height, (_scrollData._offset.Y/_scrollData._extent.Height),
                                        "actRat:", computedViewportOffset.Y, extentSize.Height, (computedViewportOffset.Y/extentSize.Height),
                                        "newOff:", viewportOffset.Y);
                                }
                            }
                        }
                    }

                    if (!remeasure && extentWidthChanged)
                    {
                        //
                        // If the extentSize has changed in the non-scrolling direction, and
                        // we are scrolling to the very end, scoot the offset to fit
                        // more in the viewport.  We're scrolling to the end if either
                        // (a) the current attempt was scrolling to the end
                        // (b) the new extent and offset indicate scrolling to the end
                        //
                        if (_scrollData.HorizontalScrollType == ScrollType.ToEnd ||
                              ( DoubleUtil.GreaterThan(computedViewportOffset.X, 0.0) &&
                                DoubleUtil.GreaterThan(computedViewportOffset.X, extentSize.Width - viewportSize.Width)))
                        {
                            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                            {
                                ScrollTracer.Trace(this, ScrollTraceOp.RemeasureEndExtentChanged,
                                    "perp",
                                    "off:", computedViewportOffset.X,
                                    "ext:", _scrollData._extent.Width, extentSize.Width,
                                    "vpSz:", viewportSize.Width);
                            }

                            remeasure = true;
                            viewportOffset.X = Double.PositiveInfinity;
                            _scrollData.HorizontalScrollType = ScrollType.ToEnd;
                        }

                        //
                        // If the extentSize has changed and we are making an absolute
                        // move to an offset, we need to readjust the offset to be at the
                        // same percentage location with the respect to the new extent as
                        // was initially intended.
                        else if (_scrollData.HorizontalScrollType == ScrollType.Absolute)
                        {
                            if (!DoubleUtil.AreClose(_scrollData._extent.Width, 0) &&
                                !DoubleUtil.AreClose(extentSize.Width, 0))
                            {
                                if (!LayoutDoubleUtil.AreClose(computedViewportOffset.X/extentSize.Width, _scrollData._offset.X/_scrollData._extent.Width))
                                {
                                    remeasure = true;
                                    viewportOffset.X = (extentSize.Width * _scrollData._offset.X) / _scrollData._extent.Width;
                                }

                                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this) && remeasure)
                                {
                                    ScrollTracer.Trace(this, ScrollTraceOp.RemeasureRatio,
                                        "perp",
                                        "expRat:", _scrollData._offset.X, _scrollData._extent.Width, (_scrollData._offset.X/_scrollData._extent.Width),
                                        "actRat:", computedViewportOffset.X, extentSize.Width, (computedViewportOffset.X/extentSize.Width),
                                        "newOff:", viewportOffset.X);
                                }
                            }
                        }
                    }
                }
            }

            // adding or removing content shouldn't scroll, unless the viewport is
            // at the end of the list and we need to fill it in.  If this happens,
            // set the state to "scrolling", so that the end-of-scroll actions
            // will happen.
            if (remeasure && (IsVirtualizing && !IsScrollActive))
            {
                if (isHorizontal && _scrollData.HorizontalScrollType == ScrollType.ToEnd)
                {
                    IsScrollActive = true;
                }
                if (!isHorizontal && _scrollData.VerticalScrollType == ScrollType.ToEnd)
                {
                    IsScrollActive = true;
                }
            }

            // non-virtualizing panels should invoke the end-of-scroll panel actions
            // now, unless a remeasure is needed. (Virtualizing panels invoke
            // end-of-scroll asyncrhonously)
            if (!IsVirtualizing && !remeasure)
            {
                ClearIsScrollActive();
            }

            viewportSizeChanged = !DoubleUtil.AreClose(viewportSize, _scrollData._viewport);

            if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
            {
                ScrollTracer.Trace(this, ScrollTraceOp.SVSDEnd,
                    "off:", _scrollData._offset, viewportOffset,
                    "ext:", _scrollData._extent, extentSize,
                    "co:", _scrollData._computedOffset, computedViewportOffset,
                    "vp:", _scrollData._viewport, viewportSize);
            }

            // Update data and fire scroll change notifications
            if (viewportSizeChanged || extentSizeChanged || computedViewportOffsetChanged)
            {
                Vector oldViewportOffset = _scrollData._computedOffset;
                Size oldViewportSize = _scrollData._viewport;

                _scrollData._viewport = viewportSize;
                _scrollData._extent = extentSize;
                _scrollData._computedOffset = computedViewportOffset;

                // Report changes to the viewportSize
                if (viewportSizeChanged)
                {
                    OnViewportSizeChanged(oldViewportSize, viewportSize);
                }

                // Report changes to the computedViewportOffset
                if (computedViewportOffsetChanged)
                {
                    OnViewportOffsetChanged(oldViewportOffset, computedViewportOffset);
                }

                OnScrollChange();
            }

            _scrollData._offset = viewportOffset;
        }

        // *** DEAD CODE This method is only called in VSP45-compat mode ***
        /// <summary>
        ///     Sets the scolling Data
        /// </summary>
        /// <returns>The return value indicates if the offset changed or not</returns>
        // This is the 4.5RTM version of the method - only called in compat mode.
        private void SetAndVerifyScrollingData(
            bool isHorizontal,
            Rect viewport,
            Size constraint,
            ref Size stackPixelSize,
            ref Size stackLogicalSize,
            ref Size stackPixelSizeInViewport,
            ref Size stackLogicalSizeInViewport,
            ref Size stackPixelSizeInCacheBeforeViewport,
            ref Size stackLogicalSizeInCacheBeforeViewport,
            ref bool remeasure,
            ref double? lastPageSafeOffset,
            ref List<double> previouslyMeasuredOffsets)
        {
            Debug.Assert(IsScrolling, "ScrollData must only be set on a scrolling panel.");

            Vector computedViewportOffset, viewportOffset;
            Size viewportSize;
            Size extentSize;

            computedViewportOffset = new Vector(viewport.Location.X, viewport.Location.Y);

            if (IsPixelBased)
            {
                extentSize = stackPixelSize;
                viewportSize = viewport.Size;
            }
            else
            {
                extentSize = stackLogicalSize;
                viewportSize = stackLogicalSizeInViewport;

                if (isHorizontal)
                {
                    if (DoubleUtil.GreaterThan(stackPixelSizeInViewport.Width, constraint.Width) &&
                        viewportSize.Width > 1)
                    {
                        viewportSize.Width--;
                    }

                    viewportSize.Height = viewport.Height;
                }
                else
                {
                    if (DoubleUtil.GreaterThan(stackPixelSizeInViewport.Height, constraint.Height) &&
                        viewportSize.Height > 1)
                    {
                        viewportSize.Height--;
                    }

                    viewportSize.Width = viewport.Width;
                }
            }

            if (isHorizontal)
            {
                if (MeasureCaches && IsVirtualizing)
                {
                    //
                    // We do not want the cache measure pass to affect the visibility
                    // of the scrollbars because this makes bad user experience and
                    // is also the source of scrolling bugs.
                    //

                    stackPixelSize.Height = _scrollData._extent.Height;
                }

                // In order to avoid fluctuations in the minor axis scrollbar visibility
                // as we scroll items of varying dimensions in and out of the viewport,
                // we cache the _maxDesiredSize along that dimension and return that
                // instead.
                _scrollData._maxDesiredSize.Height = Math.Max(_scrollData._maxDesiredSize.Height, stackPixelSize.Height);
                stackPixelSize.Height = _scrollData._maxDesiredSize.Height;

                extentSize.Height = stackPixelSize.Height;

                if (Double.IsPositiveInfinity(constraint.Height))
                {
                    viewportSize.Height = stackPixelSize.Height;
                }
            }
            else
            {
                if (MeasureCaches && IsVirtualizing)
                {
                    //
                    // We do not want the cache measure pass to affect the visibility
                    // of the scrollbars because this makes bad user experience and
                    // is also the source of scrolling bugs.
                    //

                    stackPixelSize.Width = _scrollData._extent.Width;
                }

                // In order to avoid fluctuations in the minor axis scrollbar visibility
                // as we scroll items of varying dimensions in and out of the viewport,
                // we cache the _maxDesiredSize along that dimension and return that
                // instead.
                _scrollData._maxDesiredSize.Width = Math.Max(_scrollData._maxDesiredSize.Width, stackPixelSize.Width);
                stackPixelSize.Width = _scrollData._maxDesiredSize.Width;

                extentSize.Width = stackPixelSize.Width;

                if (Double.IsPositiveInfinity(constraint.Width))
                {
                    viewportSize.Width = stackPixelSize.Width;
                }
            }

            //
            // Since we can offset and clip our content, we never need to be larger than the parent suggestion.
            // If we returned the full size of the content, we would always be so big we didn't need to scroll.  :)
            //
            // Now consider item scrolling cases where we are scrolling to the very last page. In these cases
            // there may be a small region left which is not big enough to fit an entire item. So the sizes of the
            // items accrued within the viewport may be less than the constraint. But we still want to return the
            // constraint dimensions so that we do not unnecessarily cause the parents to be invalidated and
            // re-measured.
            //
            // However if the constraint is infinite, don't change the stack size. This avoids
            // returning an infinite desired size, which is forbidden.
            if (!Double.IsPositiveInfinity(constraint.Width))
            {
                stackPixelSize.Width = IsPixelBased || DoubleUtil.AreClose(computedViewportOffset.X, 0) ?
                    Math.Min(stackPixelSize.Width, constraint.Width) : constraint.Width;
            }
            if (!Double.IsPositiveInfinity(constraint.Height))
            {
                stackPixelSize.Height = IsPixelBased || DoubleUtil.AreClose(computedViewportOffset.Y, 0) ?
                    Math.Min(stackPixelSize.Height, constraint.Height) : constraint.Height;
            }

#if DEBUG
            if (!IsPixelBased)
            {
                // Verify that ViewportSize and ExtentSize are not fractional. Offset can be fractional since it is used to
                // to accumulate the fractional changes, but only the whole value is used for further computations.
                if (isHorizontal)
                {
                    Debug.Assert(DoubleUtil.AreClose(viewportSize.Width - Math.Floor(viewportSize.Width), 0.0), "The viewport size must not contain fractional values when in item scrolling mode.");
                    Debug.Assert(DoubleUtil.AreClose(extentSize.Width - Math.Floor(extentSize.Width), 0.0), "The extent size must not contain fractional values when in item scrolling mode.");
                }
                else
                {
                    Debug.Assert(DoubleUtil.AreClose(viewportSize.Height - Math.Floor(viewportSize.Height), 0.0), "The viewport size must not contain fractional values when in item scrolling mode.");
                    Debug.Assert(DoubleUtil.AreClose(extentSize.Height - Math.Floor(extentSize.Height), 0.0), "The extent size must not contain fractional values when in item scrolling mode.");
                }
            }
#endif

            // Detect changes to the viewportSize, extentSize, and computedViewportOffset
            bool viewportSizeChanged = !DoubleUtil.AreClose(viewportSize, _scrollData._viewport);
            bool extentSizeChanged = !DoubleUtil.AreClose(extentSize, _scrollData._extent);
            bool computedViewportOffsetChanged = !DoubleUtil.AreClose(computedViewportOffset, _scrollData._computedOffset);

            //
            // Check if we need to repeat the measure operation and adjust the
            // scroll offset and/or viewport size for this new iteration.
            //
            viewportOffset = computedViewportOffset;

            //
            // Check to see if we want to disregard this measure because the ScrollViewer is
            // about to remeasure this panel with changed ScrollBarVisibility. In this case we
            // should allow the ScrollViewer to retry the transition to the original offset.
            //

            bool allowRemeasure = true;

            ScrollViewer sv = ScrollOwner;
            if (sv.InChildMeasurePass1 || sv.InChildMeasurePass2)
            {
                ScrollBarVisibility vsbv = sv.VerticalScrollBarVisibility;
                bool vsbAuto = (vsbv == ScrollBarVisibility.Auto);

                if (vsbAuto)
                {
                    Visibility oldvv = sv.ComputedVerticalScrollBarVisibility;
                    Visibility newvv = DoubleUtil.LessThanOrClose(extentSize.Height, viewportSize.Height) ? Visibility.Collapsed : Visibility.Visible;
                    if (oldvv != newvv)
                    {
                        viewportOffset = _scrollData._offset;
                        allowRemeasure = false;
                    }
                }

                if (allowRemeasure)
                {
                    ScrollBarVisibility hsbv = sv.HorizontalScrollBarVisibility;
                    bool hsbAuto = (hsbv == ScrollBarVisibility.Auto);

                    if (hsbAuto)
                    {
                        Visibility oldhv = sv.ComputedHorizontalScrollBarVisibility;
                        Visibility newhv = DoubleUtil.LessThanOrClose(extentSize.Width, viewportSize.Width) ? Visibility.Collapsed : Visibility.Visible;
                        if (oldhv != newhv)
                        {
                            viewportOffset = _scrollData._offset;
                            allowRemeasure = false;
                        }
                    }
                }
            }

            if (isHorizontal)
            {
                allowRemeasure = !WasOffsetPreviouslyMeasured(previouslyMeasuredOffsets, computedViewportOffset.X);

                if (allowRemeasure)
                {
                    bool wasViewportOffsetCoerced = !DoubleUtil.AreClose(computedViewportOffset.X, _scrollData._offset.X);

                    if (!IsPixelBased)
                    {
                        //
                        // If the viewportSize has the potential to increase and we are scrolling to the very end
                        // then we need to scoot back the offset to fit more in the viewport.
                        //
                        if (!IsEndOfViewport(isHorizontal, viewport, stackPixelSizeInViewport) &&
                            DoubleUtil.GreaterThan(stackLogicalSize.Width, stackLogicalSizeInViewport.Width))
                        {
                            //
                            // When navigating to the last page remember the smallest offset that
                            // was able to display the last item in the collection
                            //
                            lastPageSafeOffset = lastPageSafeOffset.HasValue ? Math.Min(computedViewportOffset.X, (double)lastPageSafeOffset) : computedViewportOffset.X;

                            double approxSizeOfLogicalUnit = stackPixelSizeInViewport.Width / stackLogicalSizeInViewport.Width;
                            double proposedViewportSize = Math.Floor(viewport.Width / approxSizeOfLogicalUnit);
                            if (DoubleUtil.GreaterThan(proposedViewportSize, viewportSize.Width))
                            {
                                viewportOffset.X = Double.PositiveInfinity;
                                viewportSize.Width = proposedViewportSize;
                                remeasure = true;
                                StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.X);
                            }
                        }

                        //
                        // If the viewportSize has decreased and we are scrolling to the very end then we need
                        // to scoot the offset forward to infact be at the very end.
                        //
                        if (!remeasure && wasViewportOffsetCoerced && viewportSizeChanged &&
                            !DoubleUtil.AreClose(_scrollData._viewport.Width, viewportSize.Width))
                        {
                            remeasure = true;
                            viewportOffset.X = _scrollData._offset.X;
                            StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.X);

                            if (DoubleUtil.AreClose(viewportSize.Width, 0))
                            {
                                viewportSize.Width = _scrollData._viewport.Width;
                            }
                        }
                    }

                    if (!remeasure && extentSizeChanged && !DoubleUtil.AreClose(_scrollData._extent.Width, extentSize.Width))
                    {
                        //
                        // If the extentSize has decreased and we are scrolling to the very end then again we
                        // need to scoot back the offset to fit more in the viewport.
                        //
                        if (DoubleUtil.GreaterThan(computedViewportOffset.X, 0.0) &&
                            DoubleUtil.GreaterThan(computedViewportOffset.X, extentSize.Width - viewportSize.Width))
                        {
                            remeasure = true;
                            viewportOffset.X = Double.PositiveInfinity;
                            StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.X);
                        }

                        //
                        // If the extentSize has increased and we are scrolling to the very end then again we need
                        // to scoot the offset forward to infact be at the very end.
                        //
                        if (!remeasure)
                        {
                            if (wasViewportOffsetCoerced)
                            {
                                remeasure = true;
                                viewportOffset.X = _scrollData._offset.X;
                                StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.X);
                            }
                        }

                        //
                        // If the extentSize has changed and we are making and absolute move to an offset (an
                        // active anchor suggests relative movement), we need to readjust the offset to be at the
                        // same percentage location with the respect to the new extent as was initially intended.
                        //
                        if (!remeasure)
                        {
                            bool isAbsoluteMove =
                                (MeasureCaches && !WasLastMeasurePassAnchored) ||
                                (_scrollData._firstContainerInViewport == null && computedViewportOffsetChanged && !LayoutDoubleUtil.AreClose(computedViewportOffset.X, _scrollData._computedOffset.X));

                            if (isAbsoluteMove &&
                                !DoubleUtil.AreClose(_scrollData._extent.Width, 0) &&
                                !DoubleUtil.AreClose(extentSize.Width, 0))
                            {
                                if (IsPixelBased)
                                {
                                    if (!LayoutDoubleUtil.AreClose(computedViewportOffset.X/extentSize.Width, _scrollData._offset.X/_scrollData._extent.Width))
                                    {
                                        remeasure = true;
                                        viewportOffset.X = (extentSize.Width * _scrollData._offset.X) / _scrollData._extent.Width;
                                        StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.X);
                                    }
                                }
                                else
                                {
                                    if (!LayoutDoubleUtil.AreClose(Math.Floor(computedViewportOffset.X)/extentSize.Width, Math.Floor(_scrollData._offset.X)/_scrollData._extent.Width))
                                    {
                                        remeasure = true;
                                        viewportOffset.X = Math.Floor((extentSize.Width * Math.Floor(_scrollData._offset.X)) / _scrollData._extent.Width);
                                        StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.X);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    //
                    // If we've encountered a cycle in the list of offsets that we measured against
                    // when trying to navigate to the last page, we settle on the best available this far.
                    //
                    if (!IsPixelBased && lastPageSafeOffset.HasValue && !DoubleUtil.AreClose((double)lastPageSafeOffset, computedViewportOffset.X))
                    {
                        viewportOffset.X = (double)lastPageSafeOffset;
                        lastPageSafeOffset = null;
                        remeasure = true;
                    }
                }
            }
            else
            {
                allowRemeasure = !WasOffsetPreviouslyMeasured(previouslyMeasuredOffsets, computedViewportOffset.Y);

                if (allowRemeasure)
                {
                    bool wasViewportOffsetCoerced = !DoubleUtil.AreClose(computedViewportOffset.Y, _scrollData._offset.Y);

                    if (!IsPixelBased)
                    {
                        //
                        // If the viewportSize has the potential to increase and we are scrolling to the very end
                        // then we need to scoot back the offset to fit more in the viewport.
                        //
                        if (!IsEndOfViewport(isHorizontal, viewport, stackPixelSizeInViewport) &&
                            DoubleUtil.GreaterThan(stackLogicalSize.Height, stackLogicalSizeInViewport.Height))
                        {
                            //
                            // When navigating to the last page remember the smallest offset that
                            // was able to display the last item in the collection
                            //
                            lastPageSafeOffset = lastPageSafeOffset.HasValue ? Math.Min(computedViewportOffset.Y, (double)lastPageSafeOffset) : computedViewportOffset.Y;

                            double approxSizeOfLogicalUnit = stackPixelSizeInViewport.Height / stackLogicalSizeInViewport.Height;
                            double proposedViewportSize = Math.Floor(viewport.Height / approxSizeOfLogicalUnit);
                            if (DoubleUtil.GreaterThan(proposedViewportSize, viewportSize.Height))
                            {
                                viewportOffset.Y = Double.PositiveInfinity;
                                viewportSize.Height = proposedViewportSize;
                                remeasure = true;
                                StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.Y);
                            }
                        }

                        //
                        // If the viewportSize has decreased and we are scrolling to the very end then we need
                        // to scoot the offset forward to infact be at the very end.
                        //
                        if (!remeasure && wasViewportOffsetCoerced && viewportSizeChanged &&
                            !DoubleUtil.AreClose(_scrollData._viewport.Height, viewportSize.Height))
                        {
                            remeasure = true;
                            viewportOffset.Y = _scrollData._offset.Y;
                            StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.Y);

                            if (DoubleUtil.AreClose(viewportSize.Height, 0))
                            {
                                viewportSize.Height = _scrollData._viewport.Height;
                            }
                        }
                    }

                    if (!remeasure && extentSizeChanged && !DoubleUtil.AreClose(_scrollData._extent.Height, extentSize.Height))
                    {
                        //
                        // If the extentSize has decreased and we are scrolling to the very end then again we
                        // need to scoot back the offset to fit more in the viewport.
                        //
                        if (DoubleUtil.GreaterThan(computedViewportOffset.Y, 0.0) &&
                            DoubleUtil.GreaterThan(computedViewportOffset.Y, extentSize.Height - viewportSize.Height))
                        {
                            remeasure = true;
                            viewportOffset.Y = Double.PositiveInfinity;
                            StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.Y);
                        }

                        //
                        // If the extentSize has increased and we are scrolling to the very end then again we need
                        // to scoot the offset forward to infact be at the very end.
                        //
                        if (!remeasure)
                        {
                            if (wasViewportOffsetCoerced)
                            {
                                remeasure = true;
                                viewportOffset.Y = _scrollData._offset.Y;
                                StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.Y);
                            }
                        }

                        //
                        // If the extentSize has changed and we are making and absolute move to an offset (an
                        // active anchor suggests relative movement), we need to readjust the offset to be at the
                        // same percentage location with the respect to the new extent as was initially intended.
                        //
                        if (!remeasure)
                        {
                            bool isAbsoluteMove =
                                (MeasureCaches && !WasLastMeasurePassAnchored) ||
                                (_scrollData._firstContainerInViewport == null && computedViewportOffsetChanged && !LayoutDoubleUtil.AreClose(computedViewportOffset.Y, _scrollData._computedOffset.Y));

                            if (isAbsoluteMove &&
                                !DoubleUtil.AreClose(_scrollData._extent.Height, 0) &&
                                !DoubleUtil.AreClose(extentSize.Height, 0))
                            {
                                if (IsPixelBased)
                                {
                                    if (!LayoutDoubleUtil.AreClose(computedViewportOffset.Y/extentSize.Height, _scrollData._offset.Y/_scrollData._extent.Height))
                                    {
                                        remeasure = true;
                                        viewportOffset.Y = (extentSize.Height * _scrollData._offset.Y) / _scrollData._extent.Height;
                                        StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.Y);
                                    }
                                }
                                else
                                {
                                    if (!LayoutDoubleUtil.AreClose(Math.Floor(computedViewportOffset.Y)/extentSize.Height, Math.Floor(_scrollData._offset.Y)/_scrollData._extent.Height))
                                    {
                                        remeasure = true;
                                        viewportOffset.Y = Math.Floor((extentSize.Height * Math.Floor(_scrollData._offset.Y)) / _scrollData._extent.Height);
                                        StorePreviouslyMeasuredOffset(ref previouslyMeasuredOffsets, computedViewportOffset.Y);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    //
                    // If we've encountered a cycle in the list of offsets that we measured against
                    // when trying to navigate to the last page, we settle on the best available this far.
                    //
                    if (!IsPixelBased && lastPageSafeOffset.HasValue && !DoubleUtil.AreClose((double)lastPageSafeOffset, computedViewportOffset.Y))
                    {
                        viewportOffset.Y = (double)lastPageSafeOffset;
                        lastPageSafeOffset = null;
                        remeasure = true;
                    }
                }
            }

            viewportSizeChanged = !DoubleUtil.AreClose(viewportSize, _scrollData._viewport);

            // Update data and fire scroll change notifications
            if (viewportSizeChanged || extentSizeChanged || computedViewportOffsetChanged)
            {
                Vector oldViewportOffset = _scrollData._computedOffset;
                Size oldViewportSize = _scrollData._viewport;

                _scrollData._viewport = viewportSize;
                _scrollData._extent = extentSize;
                _scrollData._computedOffset = computedViewportOffset;

                // Report changes to the viewportSize
                if (viewportSizeChanged)
                {
                    OnViewportSizeChanged(oldViewportSize, viewportSize);
                }

                // Report changes to the computedViewportOffset
                if (computedViewportOffsetChanged)
                {
                    OnViewportOffsetChanged(oldViewportOffset, computedViewportOffset);
                }

                OnScrollChange();
            }

            _scrollData._offset = viewportOffset;
        } // *** End DEAD CODE ***

        private void StorePreviouslyMeasuredOffset(
            ref List<double> previouslyMeasuredOffsets,
            double offset)
        {
            if (previouslyMeasuredOffsets == null)
            {
                previouslyMeasuredOffsets = new List<double>();
            }

            previouslyMeasuredOffsets.Add(offset);
        }

        private bool WasOffsetPreviouslyMeasured(
            List<double> previouslyMeasuredOffsets,
            double offset)
        {
            if (previouslyMeasuredOffsets != null)
            {
                for (int i=0; i<previouslyMeasuredOffsets.Count; i++)
                {
                    if (DoubleUtil.AreClose(previouslyMeasuredOffsets[i], offset))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Allows subclasses to be notified of changes to the viewport size data.
        /// </summary>
        /// <param name="oldViewportSize">The old value of the size.</param>
        /// <param name="newViewportSize">The new value of the size.</param>
        protected virtual void OnViewportSizeChanged(Size oldViewportSize, Size newViewportSize)
        {
        }

        /// <summary>
        ///     Allows subclasses to be notified of changes to the viewport offset data.
        /// </summary>
        /// <param name="oldViewportOffset">The old value of the offset.</param>
        /// <param name="newViewportOffset">The new value of the offset.</param>
        protected virtual void OnViewportOffsetChanged(Vector oldViewportOffset, Vector newViewportOffset)
        {
        }

        /// <summary>
        ///     Fetch the logical/item offset for this child with respect to the top of the
        ///     panel. This is similar to a TransformToAncestor operation. Just works
        ///     in logical units.
        /// </summary>
        protected override double GetItemOffsetCore(UIElement child)
        {
            if (child == null)
            {
                throw new ArgumentNullException("child");
            }

            bool isHorizontal = (Orientation == Orientation.Horizontal);
            ItemsControl itemsControl;
            GroupItem groupItem;
            IContainItemStorage itemStorageProvider;
            IHierarchicalVirtualizationAndScrollInfo virtualizationInfoProvider;
            object parentItem;
            IContainItemStorage parentItemStorageProvider;
            bool mustDisableVirtualization;

            GetOwners(false /*shouldSetVirtualizationState*/, isHorizontal, out itemsControl, out groupItem, out itemStorageProvider, out virtualizationInfoProvider,out parentItem, out parentItemStorageProvider, out mustDisableVirtualization);

            ItemContainerGenerator generator = (ItemContainerGenerator)Generator;
            IList items = generator.ItemsInternal;
            int itemIndex = generator.IndexFromContainer(child, true /*returnLocalIndex*/);

            double distance = 0;

            if (itemIndex >= 0)
            {
                IContainItemStorage uniformSizeItemStorageProvider = IsVSP45Compat ? itemStorageProvider : parentItemStorageProvider;
                ComputeDistance(items, itemStorageProvider, isHorizontal,
                    GetAreContainersUniformlySized(uniformSizeItemStorageProvider, parentItem),
                    GetUniformOrAverageContainerSize(uniformSizeItemStorageProvider, parentItem),
                    0, itemIndex, out distance);
            }

            return distance;
        }

        private double FindScrollOffset(Visual v)
        {
            ItemsControl scrollingItemsControl = ItemsControl.GetItemsOwner(this);

            DependencyObject child = v;
            DependencyObject element = VisualTreeHelper.GetParent(child);
            IHierarchicalVirtualizationAndScrollInfo virtualizingElement = null;
            Panel itemsHost = null;
            IList items = null;
            int childItemIndex = -1;

            double offset = 0.0;
            bool isHorizontal = (Orientation == Orientation.Horizontal);
            bool returnLocalIndex = true;

            //
            // Compute offset
            //
            while (true)
            {
                virtualizingElement = GetVirtualizingChild(element);
                if (virtualizingElement != null)
                {
                    //
                    // Find index for childItem in ItemsCollection
                    //
                    itemsHost = virtualizingElement.ItemsHost;
                    child = FindDirectDescendentOfItemsHost(itemsHost, child);
                    if (child != null)
                    {
                        VirtualizingPanel vp = itemsHost as VirtualizingPanel;
                        if (vp != null && vp.CanHierarchicallyScrollAndVirtualize)
                        {
                            double distance = vp.GetItemOffset((UIElement)child);
                            offset += distance;

                            if (IsPixelBased)
                            {
                                if (IsVSP45Compat)
                                {
                                    Size desiredPixelHeaderSize = virtualizingElement.HeaderDesiredSizes.PixelSize;
                                    offset += (isHorizontal ? desiredPixelHeaderSize.Width : desiredPixelHeaderSize.Height);
                                }
                                else
                                {
                                    Thickness inset = GetItemsHostInsetForChild(virtualizingElement);
                                    offset += (isHorizontal ? inset.Left : inset.Top);
                                }
                            }
                            else
                            {
                                if (IsVSP45Compat)
                                {
                                    Size desiredLogicalHeaderSize = virtualizingElement.HeaderDesiredSizes.LogicalSize;
                                    offset += (isHorizontal ? desiredLogicalHeaderSize.Width : desiredLogicalHeaderSize.Height);
                                }
                                else
                                {
                                    Thickness inset = GetItemsHostInsetForChild(virtualizingElement);
                                    bool isHeaderBeforeItems = IsHeaderBeforeItems(isHorizontal, virtualizingElement as FrameworkElement, ref inset);
                                    offset += isHeaderBeforeItems ? 1 : 0;
                                }
                            }
                        }
                    }

                    child = (DependencyObject)virtualizingElement;
                }
                else if (element == this) // We have walked as far as the scrolling panel
                {
                    //
                    // Find index for childItem in ItemsCollection
                    //
                    itemsHost = this;
                    child = FindDirectDescendentOfItemsHost(itemsHost, child);
                    if (child != null)
                    {
                        IContainItemStorage itemStorageProvider = GetItemStorageProvider(this);
                        IContainItemStorage parentItemStorageProvider = IsVSP45Compat ? itemStorageProvider :
                            ItemsControl.GetItemsOwnerInternal(VisualTreeHelper.GetParent((Visual)itemStorageProvider)) as IContainItemStorage;

                        items = ((ItemContainerGenerator)itemsHost.Generator).ItemsInternal;
                        childItemIndex = ((ItemContainerGenerator)itemsHost.Generator).IndexFromContainer(child, returnLocalIndex);

                        double distance;
                        ComputeDistance(items, itemStorageProvider, isHorizontal,
                            GetAreContainersUniformlySized(parentItemStorageProvider, this),
                            GetUniformOrAverageContainerSize(parentItemStorageProvider, this),
                            0, childItemIndex, out distance);

                        offset += distance;
                    }

                    break;
                }

                element = VisualTreeHelper.GetParent(element);
            }

            return offset;
        }

        private DependencyObject FindDirectDescendentOfItemsHost(Panel itemsHost, DependencyObject child)
        {
            if (itemsHost == null || !itemsHost.IsVisible)
            {
                return null;
            }

            //
            // Find the direct descendent of the ItemsHost encapsulating the given child
            //
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != itemsHost)
            {
                child = parent;
                if (child == null)
                {
                    break;
                }
                parent = VisualTreeHelper.GetParent(child);
            }

            return child;
        }

        // This is very similar to the work that ScrollContentPresenter does for MakeVisible.  Simply adjust by a
        // pixel offset.
        private void MakeVisiblePhysicalHelper(Rect r, ref Vector newOffset, ref Rect newRect, bool isHorizontal, ref bool alignTop, ref bool alignBottom)
        {
            double viewportOffset;
            double viewportSize;
            double targetRectOffset;
            double targetRectSize;
            double minPhysicalOffset;

            if (isHorizontal)
            {
                viewportOffset = _scrollData._computedOffset.X;
                viewportSize = ViewportWidth;
                targetRectOffset = r.X;
                targetRectSize = r.Width;
            }
            else
            {
                viewportOffset = _scrollData._computedOffset.Y;
                viewportSize = ViewportHeight;
                targetRectOffset = r.Y;
                targetRectSize = r.Height;
            }

            targetRectOffset += viewportOffset;
            minPhysicalOffset = ScrollContentPresenter.ComputeScrollOffsetWithMinimalScroll(
                viewportOffset, viewportOffset + viewportSize, targetRectOffset, targetRectOffset + targetRectSize, ref alignTop, ref alignBottom);

            // Compute the visible rectangle of the child relative to the viewport.

            if (alignTop)
            {
                targetRectOffset = viewportOffset;
            }
            else if (alignBottom)
            {
                targetRectOffset = viewportOffset + viewportSize - targetRectSize;
            }

            double left = Math.Max(targetRectOffset, minPhysicalOffset);
            targetRectSize = Math.Max(Math.Min(targetRectSize + targetRectOffset, minPhysicalOffset + viewportSize) - left, 0);
            targetRectOffset = left;
            targetRectOffset -= viewportOffset;

            if (isHorizontal)
            {
                newOffset.X = minPhysicalOffset;
                newRect.X = targetRectOffset;
                newRect.Width = targetRectSize;
            }
            else
            {
                newOffset.Y = minPhysicalOffset;
                newRect.Y = targetRectOffset;
                newRect.Height = targetRectSize;
            }
        }

        private void MakeVisibleLogicalHelper(int childIndex, Rect r, ref Vector newOffset, ref Rect newRect, ref bool alignTop, ref bool alignBottom)
        {
            bool fHorizontal = (Orientation == Orientation.Horizontal);
            int firstChildInView;
            int newFirstChild;
            int viewportSize;
            double childOffsetWithinViewport = r.Y;

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
                alignTop = true;
                childOffsetWithinViewport = 0;
                newFirstChild = childIndex;
            }
            // If the target child is after the current viewport, move the viewport to put the child at the bottom.
            else if (childIndex > firstChildInView + Math.Max(viewportSize - 1, 0))
            {
                alignBottom = true;
                newFirstChild = childIndex - viewportSize + 1;
                double pixelSize = fHorizontal ? ActualWidth : ActualHeight;
                childOffsetWithinViewport = pixelSize * (1.0 - (1.0 / viewportSize));
            }

            if (fHorizontal)
            {
                newOffset.X = newFirstChild;
                newRect.X = childOffsetWithinViewport;
                newRect.Width = r.Width;
            }
            else
            {
                newOffset.Y = newFirstChild;
                newRect.Y = childOffsetWithinViewport;
                newRect.Height = r.Height;
            }
        }

        private int GetGeneratedIndex(int childIndex)
        {
            return Generator.IndexFromGeneratorPosition(new GeneratorPosition(childIndex, 0));
        }

        /// <summary>
        ///     Helper method which loops through the children
        ///     and returns the max arrange height/width for
        ///     horizontal/vertical orientation
        /// </summary>
        private double GetMaxChildArrangeLength(IList children, bool isHorizontal)
        {
            double maxChildLength = 0;
            for (int i = 0, childCount = children.Count; i < childCount; i++)
            {
                UIElement container = null;
                Size childSize;

                // we are looping through the actual containers; the visual children of this panel.
                container = (UIElement)children[i];
                childSize = container.DesiredSize;

                if (isHorizontal)
                {
                    maxChildLength = Math.Max(maxChildLength, childSize.Height);
                }
                else
                {
                    maxChildLength = Math.Max(maxChildLength, childSize.Width);
                }
            }
            return maxChildLength;
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
            ResetScrolling(d as VirtualizingStackPanel);
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// True after the first MeasureOverride call. We can't use UIElement.NeverMeasured because it's set to true by the first call to MeasureOverride.
        /// Stored in a bool field on Panel.
        /// </summary>
        private bool HasMeasured
        {
            get
            {
                return VSP_HasMeasured;
            }
            set
            {
                VSP_HasMeasured = value;
            }
        }

        private bool InRecyclingMode
        {
            get
            {
                return VSP_InRecyclingMode;
            }
            set
            {
                VSP_InRecyclingMode = value;
            }
        }


        internal bool IsScrolling
        {
            get { return (_scrollData != null) && (_scrollData._scrollOwner != null); }
        }


        /// <summary>
        /// Specifies if this panel uses item-based or pixel-based computations in Measure and Arrange.
        ///
        /// Differences between the two:
        ///
        /// When pixel-based mode VSP behaves the same to the layout system virtualized as not; its desired size is the sum
        /// of all its children and it arranges children such that the ones in view appear in the right place.
        /// In this mode VSP is also able to make use of the viewport passed down to virtualize chidren.  When
        /// it's the scrolling panel it computes the offset and extent in pixels rather than logical units.
        ///
        /// When in item mode VSP's desired size grows and shrinks depending on which containers are virtualized and it arranges
        /// all children one on top the the other.
        /// In this mode VSP cannot use the viewport to virtualize; it can only virtualize if it is the scrolling panel
        /// (IsScrolling == true).  Thus its looseness with desired size isn't much of an issue since it owns the extent.
        /// </summary>
        /// <remarks>
        /// This should be private, except that one Debug.Assert in TreeView requires it.
        /// </remarks>
        internal bool IsPixelBased
        {
            get
            {
                return VSP_IsPixelBased;
            }
            set
            {
                VSP_IsPixelBased = value;
            }
        }


        internal bool MustDisableVirtualization
        {
            get
            {
                return VSP_MustDisableVirtualization;
            }
            set
            {
                VSP_MustDisableVirtualization = value;
            }
        }

        internal bool MeasureCaches
        {
            get
            {
                return VSP_MeasureCaches || !IsVirtualizing;
            }
            set
            {
                VSP_MeasureCaches = value;
            }
        }

        private bool IsVirtualizing
        {
            get
            {
                return VSP_IsVirtualizing;
            }
            set
            {
                // We must be the ItemsHost to turn on Virtualization.
                bool isVirtualizing = IsItemsHost && value;

                if (isVirtualizing == false)
                {
                    _realizedChildren = null;
                }

                VSP_IsVirtualizing = value;
            }
        }

        private bool HasVirtualizingChildren
        {
            get
            {
                return GetBoolField(BoolField.HasVirtualizingChildren);
            }

            set
            {
                SetBoolField(BoolField.HasVirtualizingChildren, value);
            }
        }

        private bool AlignTopOfBringIntoViewContainer
        {
            get
            {
                return GetBoolField(BoolField.AlignTopOfBringIntoViewContainer);
            }

            set
            {
                SetBoolField(BoolField.AlignTopOfBringIntoViewContainer, value);
            }
        }

        private bool AlignBottomOfBringIntoViewContainer
        {
            get
            {
                return GetBoolField(BoolField.AlignBottomOfBringIntoViewContainer);
            }

            set
            {
                SetBoolField(BoolField.AlignBottomOfBringIntoViewContainer, value);
            }
        }

        private bool WasLastMeasurePassAnchored
        {
            get
            {
                return GetBoolField(BoolField.WasLastMeasurePassAnchored);
            }

            set
            {
                SetBoolField(BoolField.WasLastMeasurePassAnchored, value);
            }
        }

        private bool ItemsChangedDuringMeasure
        {
            get
            {
                return GetBoolField(BoolField.ItemsChangedDuringMeasure);
            }

            set
            {
                SetBoolField(BoolField.ItemsChangedDuringMeasure, value);
            }
        }

        private bool IsScrollActive
        {
            get
            {
                return GetBoolField(BoolField.IsScrollActive);
            }

            set
            {
                if (ScrollTracer.IsEnabled && ScrollTracer.IsTracing(this))
                {
                    bool oldValue = GetBoolField(BoolField.IsScrollActive);
                    if (value != oldValue)
                    {
                        ScrollTracer.Trace(this, ScrollTraceOp.IsScrollActive,
                            value);
                    }
                }

                SetBoolField(BoolField.IsScrollActive, value);

                if (!value)
                {
                    _scrollData.HorizontalScrollType = ScrollType.None;
                    _scrollData.VerticalScrollType = ScrollType.None;
                }
            }
        }

        internal bool IgnoreMaxDesiredSize
        {
            get
            {
                return GetBoolField(BoolField.IgnoreMaxDesiredSize);
            }
            set
            {
                SetBoolField(BoolField.IgnoreMaxDesiredSize, value);
            }
        }

        private bool IsMeasureCachesPending
        {
            get
            {
                return GetBoolField(BoolField.IsMeasureCachesPending);
            }

            set
            {
                SetBoolField(BoolField.IsMeasureCachesPending, value);
            }
        }

        /// <summary>
        ///     Cache property for scrolling VSP to
        ///     avoid ItemStorageProvider calls
        /// </summary>
        private bool? AreContainersUniformlySized
        {
            get;
            set;
        }

        /// <summary>
        ///     Cache property for scrolling VSP to
        ///     avoid ItemStorageProvider calls
        /// </summary>
        private double? UniformOrAverageContainerSize
        {
            get;
            set;
        }

        /// <summary>
        ///     Cache property for scrolling VSP to
        ///     avoid ItemStorageProvider calls
        /// </summary>
        private double? UniformOrAverageContainerPixelSize
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the list of childen that have been realized by the Generator.
        /// We must use this method whenever we interact with the Generator's index.
        /// In recycling mode the Children collection also contains recycled containers and thus does
        /// not map to the Generator's list.
        /// </summary>
        private IList RealizedChildren
        {
            get
            {
                if (IsVirtualizing && InRecyclingMode)
                {
                    EnsureRealizedChildren();
                    return _realizedChildren;
                }
                else
                {
                    return InternalChildren;
                }
            }
        }

        internal static bool IsVSP45Compat
        {
            get { return FrameworkCompatibilityPreferences.GetVSP45Compat(); }
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

        private DependencyObject BringIntoViewLeafContainer
        {
            get { return _scrollData?._bringIntoViewLeafContainer ?? null; }
        }

        private FrameworkElement FirstContainerInViewport
        {
            get { return _scrollData?._firstContainerInViewport ?? null; }
        }

        private double FirstContainerOffsetFromViewport
        {
            get { return _scrollData?._firstContainerOffsetFromViewport ?? 0.0; }
        }

        private double ExpectedDistanceBetweenViewports
        {
            get { return _scrollData?._expectedDistanceBetweenViewports ?? 0.0; }
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

        private bool GetBoolField(BoolField field)
        {
            return (_boolFieldStore & field) != 0;
        }

        private void SetBoolField(BoolField field, bool value)
        {
            if (value)
            {
                 _boolFieldStore |= field;
            }
            else
            {
                 _boolFieldStore &= (~field);
            }
        }

        [System.Flags]
        private enum BoolField : byte
        {
            HasVirtualizingChildren                     = 0x01,
            AlignTopOfBringIntoViewContainer            = 0x02,
            WasLastMeasurePassAnchored                  = 0x04,
            ItemsChangedDuringMeasure                   = 0x08,
            IsScrollActive                              = 0x10,
            IgnoreMaxDesiredSize                        = 0x20,
            AlignBottomOfBringIntoViewContainer         = 0x40,
            IsMeasureCachesPending                      = 0x80,
        }

        private BoolField _boolFieldStore;

        // Scrolling and virtualization data.  Only used when this is the scrolling panel (IsScrolling is true).
        // When VSP is in pixel mode _scrollData is in units of pixels.  Otherwise the units are logical.
        private ScrollData _scrollData;

        // UIElement collection index of the first visible child container.  This is NOT the data item index. If the first visible container
        // is the 3rd child in the visual tree and contains data item 312, _firstItemInExtendedViewportChildIndex will be 2, while _firstItemInExtendedViewportIndex is 312.
        // This is useful because could be several live containers in the collection offscreen (maybe we cleaned up lazily, they couldn't be virtualized, etc).
        // This actually maps directly to realized containers inside the Generator.  It's the index of the first visible realized container.
        // Note that when RecyclingMode is active this is the index into the _realizedChildren collection, not the Children collection.
        private int _firstItemInExtendedViewportChildIndex;
        private int _firstItemInExtendedViewportIndex;              // index of the first data item in the extended viewport
        private double _firstItemInExtendedViewportOffset;          // offset of the first data item in the extended viewport

        private int _actualItemsInExtendedViewportCount;                  // count of the number of data items visible in the extended viewport
        private Rect _viewport;

        // If not MeasureCaches pass the _itemsInExtendedViewport refers to the number of items in the
        // actual viewport and not the extended viewport. Whereas _tailoredItemsInExtendedViewport
        // always refers to the real or approx item count in the extended viewport
        private int _itemsInExtendedViewportCount;
        private Rect _extendedViewport;

        private Size _previousStackPixelSizeInViewport;
        private Size _previousStackLogicalSizeInViewport;
        private Size _previousStackPixelSizeInCacheBeforeViewport;

        // two quantities needed when item-scrolling, to arrange children correctly
        private double _pixelDistanceToFirstContainerInExtendedViewport;
        private double _pixelDistanceToViewport;

        // Used by the Recycling mode to maintain the list of actual realized children (a realized child is one that the ItemContainerGenerator has
        // generated).  We need a mapping between children in the UIElementCollection and realized containers in the generator.  In standard virtualization
        // mode these lists are identical; in recycling mode they are not. When a container is recycled the Generator removes it from its realized list, but
        // for perf reasons the panel keeps these containers in its UIElement collection.  This list is the actual realized children -- i.e. the InternalChildren
        // list minus all recycled containers.
        private List<UIElement> _realizedChildren;

        // Cleanup
        private DispatcherOperation _cleanupOperation;
        private DispatcherTimer _cleanupDelay;
        private const int FocusTrail = 5; // The maximum number of items off the edge we will generate to get a focused item (so that keyboard navigation can work)
        private DependencyObject _bringIntoViewContainer;  // pointer to the container we're about to bring into view; it can't be recycled even if it's offscreen.

        private static int[] _indicesStoredInItemValueStorage;

        private static readonly UncommonField<DispatcherOperation> MeasureCachesOperationField = new UncommonField<DispatcherOperation>();
        private static readonly UncommonField<DispatcherOperation> AnchorOperationField = new UncommonField<DispatcherOperation>();
        private static readonly UncommonField<DispatcherOperation> AnchoredInvalidateMeasureOperationField = new UncommonField<DispatcherOperation>();
        private static readonly UncommonField<DispatcherOperation> ClearIsScrollActiveOperationField = new UncommonField<DispatcherOperation>();
        private static readonly UncommonField<OffsetInformation> OffsetInformationField = new UncommonField<OffsetInformation>();
        private static readonly UncommonField<EffectiveOffsetInformation> EffectiveOffsetInformationField = new UncommonField<EffectiveOffsetInformation>();
        private static readonly UncommonField<SnapshotData> SnapshotDataField = new UncommonField<SnapshotData>();

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

        private enum ScrollType { None, Relative, Absolute, ToEnd };

        // Helper class to hold scrolling data.
        // This class exists to reduce working set when VirtualizingStackPanel is used outside a scrolling situation.
        // Standard "extra pointer always for less data sometimes" cache savings model:
        //      !Scroll [1xReference]
        //      Scroll  [1xReference] + [6xDouble + 1xReference]
        private class ScrollData : IStackMeasureScrollData
        {
            // Clears layout generated data.
            // Does not clear scrollOwner, because unless resetting due to a scrollOwner change, we won't get reattached.
            internal void ClearLayout()
            {
                _offset = new Vector();
                _viewport = _extent = _maxDesiredSize = new Size();
            }

            internal bool IsEmpty
            {
                get
                {
                    return
                        _offset.X == 0.0 &&
                        _offset.Y == 0.0 &&
                        _viewport.Width == 0.0 &&
                        _viewport.Height == 0.0 &&
                        _extent.Width == 0.0 &&
                        _extent.Height == 0.0 &&
                        _maxDesiredSize.Width == 0.0 &&
                        _maxDesiredSize.Height == 0.0;
                }
            }

            // For Stack/Flow, the two dimensions of properties are in different units:
            // 1. The "logically scrolling" dimension uses items as units.
            // 2. The other dimension physically scrolls.  Units are in Avalon pixels (1/96").
            internal bool _allowHorizontal;
            internal bool _allowVertical;

            // Scroll offset of content.  Positive corresponds to a visually upward offset.  Set by methods like LineUp, PageDown, etc.
            internal Vector _offset;

            // Computed offset based on _offset set by the IScrollInfo methods.  Set at the end of a successful Measure pass.
            // This is the offset used by Arrange and exposed externally.  Thus an offset set by PageDown via IScrollInfo isn't
            // reflected publicly (e.g. via the VerticalOffset property) until a Measure pass.
            internal Vector _computedOffset = new Vector(0,0);
            //internal Vector _viewportOffset;    // ViewportOffset is in pixels
            internal Size _viewport;            // ViewportSize is in {pixels x items} (or vice-versa).
            internal Size _extent;              // Extent is the total number of children (logical dimension) or physical size
            internal ScrollViewer _scrollOwner; // ScrollViewer to which we're attached.

            internal Size _maxDesiredSize;      // Hold onto the maximum desired size to avoid re-laying out the parent ScrollViewer.

            // bring into view
            internal DependencyObject _bringIntoViewLeafContainer; // pointer to the container we are in the process of making visible. We remember it until the container has been successfully brought into view because this may require a few measure iterations.

            // anchor information
            internal FrameworkElement _firstContainerInViewport;
            internal double _firstContainerOffsetFromViewport;
            internal double _expectedDistanceBetweenViewports;

            // scroll generation - for effective offsets
            internal long _scrollGeneration;

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
            }

            public ScrollType HorizontalScrollType { get; set; }
            public ScrollType VerticalScrollType { get; set; }

            public void SetHorizontalScrollType(double oldOffset, double newOffset)
            {
                if (DoubleUtil.GreaterThanOrClose(newOffset, _extent.Width - _viewport.Width))
                {
                    HorizontalScrollType = ScrollType.ToEnd;
                }
                else if (DoubleUtil.GreaterThan(Math.Abs(newOffset - oldOffset), _viewport.Width))
                {
                    HorizontalScrollType = ScrollType.Absolute;
                }
                else if (HorizontalScrollType == ScrollType.None)
                {
                    HorizontalScrollType = ScrollType.Relative;
                }
            }

            public void SetVerticalScrollType(double oldOffset, double newOffset)
            {
                if (DoubleUtil.GreaterThanOrClose(newOffset, _extent.Height - _viewport.Height))
                {
                    VerticalScrollType = ScrollType.ToEnd;
                }
                else if (DoubleUtil.GreaterThan(Math.Abs(newOffset - oldOffset), _viewport.Height))
                {
                    VerticalScrollType = ScrollType.Absolute;
                }
                else if (VerticalScrollType == ScrollType.None)
                {
                    VerticalScrollType = ScrollType.Relative;
                }
            }
        }

        #endregion ScrollData

        #region Information caches

        // Information used to avoid loops when scrolling to the last page
        private class OffsetInformation
        {
            public List<double> previouslyMeasuredOffsets { get; set; }
            public double? lastPageSafeOffset { get; set; }
            public double? lastPagePixelSize { get; set; }
        }

        // Information used to handle extent changes due to added/removed items
        // This is part of the state of Measure - exactly what's needed to compute
        // the effective offset (see ComputeEffectiveOffset).   After an extent
        // change we need to recompute the effective offset using the new average
        // container size.  When this happens, there's no Measure going on;
        // instead we use the state saved at the end of the most recent Measure.
        private class FirstContainerInformation
        {
            public Rect             Viewport;           // in local coordinates
            public DependencyObject FirstContainer;     // first container visible in viewport
            public int              FirstItemIndex;     // index of corresponding item
            public double           FirstItemOffset;    // offset from top of viewport
            public long             ScrollGeneration;   // current scroll generation

            public FirstContainerInformation(ref Rect viewport, DependencyObject firstContainer, int firstItemIndex, double firstItemOffset, long scrollGeneration)
            {
                Viewport = viewport;
                FirstContainer = firstContainer;
                FirstItemIndex = firstItemIndex;
                FirstItemOffset = firstItemOffset;
                ScrollGeneration = scrollGeneration;
            }
        }

        private static UncommonField<FirstContainerInformation> FirstContainerInformationField =
            new UncommonField<FirstContainerInformation>();

        // For item-scrolling, record the size of each container in both
        // pixels and items
        private class ContainerSizeDual : Tuple<Size, Size>
        {
            public ContainerSizeDual(Size pixelSize, Size itemSize)
                : base(pixelSize, itemSize)
            {
            }

            public Size PixelSize
            {
                get { return Item1; }
            }

            public Size ItemSize
            {
                get { return Item2; }
            }
        }

        // For item-scrolling, record the average container size in both
        // pixels and items
        private class UniformOrAverageContainerSizeDual : Tuple<Double, Double>
        {
            public UniformOrAverageContainerSizeDual(Double pixelSize, Double itemSize)
                : base(pixelSize, itemSize)
            {
            }

            public Double PixelSize
            {
                get { return Item1; }
            }

            public Double ItemSize
            {
                get { return Item2; }
            }
        }

        // Info needed to support Effective Offsets
        private class EffectiveOffsetInformation
        {
            public long ScrollGeneration { get; private set; }
            public List<double> OffsetList { get; private set; }

            public EffectiveOffsetInformation(long scrollGeneration)
            {
                ScrollGeneration = scrollGeneration;
                OffsetList = new List<double>(2);
            }
        }

        #endregion Information caches

        #region ScrollTracer

        // NOTE The binary output is read by the StfViewer tool (wpf\src\tools\StfViewer).
        // Any changes that affect the binary output should have corresponding
        // changes in the tool.

        // a "black box" (flight data recorder) for diagnosing scrolling bugs
        private class ScrollTracer
        {
            #region static members

            const int s_StfFormatVersion = 2;   // Format of output file
            const int s_MaxTraceRecords = 30000;    // max length of in-memory _traceList
            const int s_MinTraceRecords = 5000;     // keep this many records after flushing
            const int s_DefaultLayoutUpdatedThreshold = 20; // see _luThreshold

            static string _targetName;
            static ScrollTracer()
            {
                _targetName = FrameworkCompatibilityPreferences.GetScrollingTraceTarget();
                _flushDepth = 0;
                _luThreshold = s_DefaultLayoutUpdatedThreshold;

                string s = FrameworkCompatibilityPreferences.GetScrollingTraceFile();
                if (!String.IsNullOrEmpty(s))
                {
                    string[] a = s.Split(';');
                    _fileName = a[0];

                    if (a.Length > 1)
                    {
                        int flushDepth;
                        if (Int32.TryParse(a[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out flushDepth))
                        {
                            _flushDepth = flushDepth;
                        }
                    }

                    if (a.Length > 2)
                    {
                        int luThreshold;
                        if (Int32.TryParse(a[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out luThreshold))
                        {
                            _luThreshold = (luThreshold <= 0) ? Int32.MaxValue : luThreshold;
                        }
                    }
                }

                if (_targetName != null)
                {
                    Enable();
                }
            }

            private static void Enable()
            {
                if (IsEnabled)
                    return;

                _isEnabled = true;

                Application app = Application.Current;
                if (app != null)
                {
                    app.Exit += OnApplicationExit;
                    app.DispatcherUnhandledException += OnUnhandledException;
                }
            }

            static bool _isEnabled;
            internal static bool IsEnabled { get { return _isEnabled; } }

            // for use from VS Immediate window
            internal static bool SetTarget(object o)
            {
                ItemsControl target = o as ItemsControl;
                if (target != null || o == null)
                {
                    lock (s_TargetToTraceListMap)
                    {
                        CloseAllTraceLists();

                        if (target != null)
                        {
                            Enable();
                            AddToMap(target);

                            // Change the null info's generation, to start tracing
                            // from scratch on the new target
                            ++_nullInfo.Generation;
                        }
                    }
                }
                return (target == o);
            }

            static string _fileName;
            static int _flushDepth;
            static int _luThreshold;    // go inactive after this many consecutive LayoutUpdated

            // for use from VS Immediate window
            internal static void SetFileAndDepth(string filename, int flushDepth)
            {
                // never really used this, and it's difficult to do with multiple
                // files.   So no longer supported - but keep method for compat.
                throw new NotSupportedException();
            }

            // for use from VS Immediate window
            static void Flush()
            {
                lock (s_TargetToTraceListMap)
                {
                    for (int i=0, n=s_TargetToTraceListMap.Count; i<n; ++i)
                    {
                        s_TargetToTraceListMap[i].Item2.Flush(-1);
                    }
                }
            }

            // for use from VS Immediate window
            static void Mark(params object[] args)
            {
                ScrollTraceRecord record = new ScrollTraceRecord(ScrollTraceOp.Mark, null, -1, 0, 0, BuildDetail(args));
                lock (s_TargetToTraceListMap)
                {
                    for (int i=0, n=s_TargetToTraceListMap.Count; i<n; ++i)
                    {
                        s_TargetToTraceListMap[i].Item2.Add(record);
                    }
                }
            }

            internal static bool IsConfigured(VirtualizingStackPanel vsp)
            {
                ScrollTracingInfo sti = ScrollTracingInfoField.GetValue(vsp);
                return (sti != null);
            }

            private static ScrollTracingInfo _nullInfo = new ScrollTracingInfo(null, 0, -1, null, null, null, -1);

            internal static void ConfigureTracing(VirtualizingStackPanel vsp,
                                            DependencyObject itemsOwner,
                                            object parentItem,
                                            ItemsControl itemsControl)
            {
                ScrollTracer tracer = null;
                ScrollTracingInfo sti = _nullInfo;  // default - do nothing
                ScrollTracingInfo oldsti = ScrollTracingInfoField.GetValue(vsp);

                // ignore (and replace) STI from older generation (created before most recent SetTarget)
                if (oldsti != null && oldsti.Generation < _nullInfo.Generation)
                {
                    oldsti = null;
                }

                if (parentItem == vsp)
                {
                    // top level VSP
                    if (oldsti == null)
                    {
                        // first time - create an STI for the VSP
                        if (itemsOwner == itemsControl)
                        {
                            TraceList traceList = TraceListForItemsControl(itemsControl);
                            if (traceList != null)
                            {
                                tracer = new ScrollTracer(itemsControl, vsp, traceList);
                            }
                        }

                        if (tracer != null)
                        {
                            sti = new ScrollTracingInfo(tracer, _nullInfo.Generation, 0, itemsOwner as FrameworkElement, null, null, 0);
                        }
                    }
                }
                else
                {
                    // inner VSP
                    VirtualizingStackPanel parent = VisualTreeHelper.GetParent(itemsOwner) as VirtualizingStackPanel;
                    if (parent != null)
                    {
                        ScrollTracingInfo parentInfo = ScrollTracingInfoField.GetValue(parent);
                        if (parentInfo != null)
                        {
                            tracer = parentInfo.ScrollTracer;
                            if (tracer != null)
                            {
                                ItemContainerGenerator generator = parent.ItemContainerGenerator as ItemContainerGenerator;
                                int itemIndex = (generator != null) ? generator.IndexFromContainer(itemsOwner, returnLocalIndex:true) : -1;

                                if (oldsti == null)
                                {
                                    // first time - create an STI for the VSP
                                    sti = new ScrollTracingInfo(tracer, _nullInfo.Generation, parentInfo.Depth + 1, itemsOwner as FrameworkElement, parent, parentItem, itemIndex);
                                }
                                else
                                {
                                    // already tracing the VSP - check for updates to item, item index
                                    if (Object.Equals(parentItem, oldsti.ParentItem))
                                    {
                                        if (itemIndex != oldsti.ItemIndex)
                                        {
                                            ScrollTracer.Trace(vsp, ScrollTraceOp.ID, "Index changed from ", oldsti.ItemIndex, " to ", itemIndex);
                                            oldsti.ChangeIndex(itemIndex);
                                        }
                                    }
                                    else
                                    {
                                        ScrollTracer.Trace(vsp, ScrollTraceOp.ID, "Container recyled from ", oldsti.ItemIndex, " to ", itemIndex);
                                        oldsti.ChangeItem(parentItem);
                                        oldsti.ChangeIndex(itemIndex);
                                    }
                                }
                            }
                        }
                    }
                }

                if (oldsti == null)
                {
                    // install the new STI
                    ScrollTracingInfoField.SetValue(vsp, sti);
                }
            }

            internal static bool IsTracing(VirtualizingStackPanel vsp)
            {
                ScrollTracingInfo sti = ScrollTracingInfoField.GetValue(vsp);
                return (sti != null && sti.ScrollTracer != null);
            }

            internal static void Trace(VirtualizingStackPanel vsp, ScrollTraceOp op, params object[] args)
            {
                ScrollTracingInfo sti = ScrollTracingInfoField.GetValue(vsp);
                ScrollTracer tracer = sti.ScrollTracer;
                Debug.Assert(tracer != null, "Trace called when not tracing");

                if (ShouldIgnore(op, sti))
                    return;

                tracer.AddTrace(vsp, op, sti, args);
            }

            private static bool ShouldIgnore(ScrollTraceOp op, ScrollTracingInfo sti)
            {
                return (op == ScrollTraceOp.NoOp);
            }

            private static string DisplayType(object o)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                bool needSeparator = false;
                bool isWPFControl = false;
                for (Type t = o.GetType(); !isWPFControl && t != null; t = t.BaseType)
                {
                    if (needSeparator)
                    {
                        sb.Append("/");
                    }

                    string name = t.ToString();
                    isWPFControl = name.StartsWith("System.Windows.Controls.");
                    if (isWPFControl)
                    {
                        name = name.Substring(24);  // 24 == length of "s.w.c."
                    }

                    sb.Append(name);
                    needSeparator = true;
                }

                return sb.ToString();
            }

            private static string BuildDetail(object[] args)
            {
                int length = (args != null) ? args.Length : 0;
                if (length == 0)
                    return String.Empty;
                else
                    return String.Format(CultureInfo.InvariantCulture, s_format[length], args);
            }

            private static string[] s_format = new string[] {
                "",
                "{0}",
                "{0} {1}",
                "{0} {1} {2}",
                "{0} {1} {2} {3}",
                "{0} {1} {2} {3} {4} ",
                "{0} {1} {2} {3} {4} {5}",
                "{0} {1} {2} {3} {4} {5} {6}",
                "{0} {1} {2} {3} {4} {5} {6} {7}",
                "{0} {1} {2} {3} {4} {5} {6} {7} {8}",
                "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9}",
                "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10}",
                "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}",
                "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12}",
                "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13}",
            };

            #endregion static members

            #region instance members

            private int _depth=0;       // depth of op stack
            private TraceList _traceList;
            private WeakReference<ItemsControl> _wrIC;
            private int _luCount = -1;  // count of LayoutUpdated events, or -1 if inactive

            private void Push()     { ++_depth; }
            private void Pop()      { --_depth; }
            private void Pop(ScrollTraceRecord record)  { --_depth; record.ChangeOpDepth(-1); }

            private ScrollTracer(ItemsControl itemsControl, VirtualizingStackPanel vsp, TraceList traceList)
            {
                _wrIC = new WeakReference<ItemsControl>(itemsControl);

                // set up file output
                _traceList = traceList;

                // write identifying information to the file
                IdentifyTrace(itemsControl, vsp);
            }

            // when app shuts down, flush pending info to the file
            static void OnApplicationExit(object sender, ExitEventArgs e)
            {
                Application app = sender as Application;
                if (app != null)
                {
                    app.Exit -= OnApplicationExit;   // avoid re-entrancy
                }

                CloseAllTraceLists();
            }

            // in case of unhandled exception, flush pending info to the file
            static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
            {
                Application app = sender as Application;
                if (app != null)
                {
                    app.DispatcherUnhandledException -= OnUnhandledException;   // avoid re-entrancy
                }

                CloseAllTraceLists();
            }

            private void IdentifyTrace(ItemsControl ic, VirtualizingStackPanel vsp)
            {
                AddTrace(null, ScrollTraceOp.ID, _nullInfo,
                    DisplayType(ic),
                    "Items:", ic.Items.Count,
                    "Panel:", DisplayType(vsp),
                    "Time:", DateTime.Now);

                AddTrace(null, ScrollTraceOp.ID, _nullInfo,
                    "IsVirt:", VirtualizingPanel.GetIsVirtualizing(ic),
                    "IsVirtWhenGroup:", VirtualizingPanel.GetIsVirtualizingWhenGrouping(ic),
                    "VirtMode:", VirtualizingPanel.GetVirtualizationMode(ic),
                    "ScrollUnit:", VirtualizingPanel.GetScrollUnit(ic),
                    "CacheLen:", VirtualizingPanel.GetCacheLength(ic), VirtualizingPanel.GetCacheLengthUnit(ic));

                AddTrace(null, ScrollTraceOp.ID, _nullInfo,
                    "CanContentScroll:", ScrollViewer.GetCanContentScroll(ic),
                    "IsDeferredScrolling:", ScrollViewer.GetIsDeferredScrollingEnabled(ic),
                    "PanningMode:", ScrollViewer.GetPanningMode(ic),
                    "HSBVisibility:", ScrollViewer.GetHorizontalScrollBarVisibility(ic),
                    "VSBVisibility:", ScrollViewer.GetVerticalScrollBarVisibility(ic));

                DataGrid dg = ic as DataGrid;
                if (dg != null)
                {
                    AddTrace(null, ScrollTraceOp.ID, _nullInfo,
                        "EnableRowVirt:", dg.EnableRowVirtualization,
                        "EnableColVirt:", dg.EnableColumnVirtualization,
                        "Columns:", dg.Columns.Count,
                        "FrozenCols:", dg.FrozenColumnCount);
                }
            }

            private void AddTrace(VirtualizingStackPanel vsp, ScrollTraceOp op, ScrollTracingInfo sti, params object[] args)
            {
                // the trace list contains references back into the VSP that can lead
                // to memory leaks if the app removes the VSP.  To avoid this, treat
                // a long sequence of LayoutUpdated events as a signal that the VSP might
                // have been removed, and release references.   Bring them back if
                // some other activity occurs.
                if (op == ScrollTraceOp.LayoutUpdated)
                {
                    if (++_luCount > _luThreshold)
                    {
                        AddTrace(null, ScrollTraceOp.ID, _nullInfo,
                            "Inactive at", DateTime.Now);
                        ItemsControl ic;
                        if (_wrIC.TryGetTarget(out ic))
                        {
                            ic.LayoutUpdated -= OnLayoutUpdated;
                        }
                        _traceList.FlushAndClear();
                        _luCount = -1;  // meaning "inactive"
                    }
                }
                else
                {
                    int luCount = _luCount;
                    _luCount = 0;

                    if (luCount < 0)
                    {
                        AddTrace(null, ScrollTraceOp.ID, _nullInfo,
                            "Reactivate at", DateTime.Now);
                        ItemsControl ic;
                        if (_wrIC.TryGetTarget(out ic))
                        {
                            ic.LayoutUpdated += OnLayoutUpdated;
                        }
                    }
                }

                ScrollTraceRecord record = new ScrollTraceRecord(op, vsp, sti.Depth, sti.ItemIndex, _depth, BuildDetail(args));
                _traceList.Add(record);

                switch (op)
                {
                    default:
                        break;

                    case ScrollTraceOp.BeginMeasure:
                        Push();
                        break;

                    case ScrollTraceOp.EndMeasure:
                        Pop(record);
                        record.Snapshot = vsp.TakeSnapshot();
                        _traceList.Flush(sti.Depth);
                        break;

                    case ScrollTraceOp.BeginArrange:
                        Push();
                        break;

                    case ScrollTraceOp.EndArrange:
                        Pop(record);
                        record.Snapshot = vsp.TakeSnapshot();
                        _traceList.Flush(sti.Depth);
                        break;

                    case ScrollTraceOp.BSetAnchor:
                        Push();
                        break;

                    case ScrollTraceOp.ESetAnchor:
                        Pop(record);
                        break;

                    case ScrollTraceOp.BOnAnchor:
                        Push();
                        break;

                    case ScrollTraceOp.ROnAnchor:
                        Pop(record);
                        break;

                    case ScrollTraceOp.SOnAnchor:
                        Pop();
                        break;

                    case ScrollTraceOp.EOnAnchor:
                        Pop(record);
                        break;

                    case ScrollTraceOp.RecycleChildren:
                    case ScrollTraceOp.RemoveChildren:
                        record.RevirtualizedChildren = args[2] as List<String>;
                        break;
                }

                if (_flushDepth < 0)
                {
                    _traceList.Flush(_flushDepth);
                }
            }

            private void OnLayoutUpdated(object sender, EventArgs e)
            {
                AddTrace(null, ScrollTraceOp.LayoutUpdated, _nullInfo, null);
            }

            #endregion instance members

            private static List<Tuple<WeakReference<ItemsControl>,TraceList>> s_TargetToTraceListMap
                = new List<Tuple<WeakReference<ItemsControl>,TraceList>>();
            private static int s_seqno;

            static TraceList TraceListForItemsControl(ItemsControl target)
            {
                TraceList traceList = null;

                lock (s_TargetToTraceListMap)
                {
                    // if target is already in the map, use its tracelist
                    for (int i=0, n=s_TargetToTraceListMap.Count; i<n; ++i)
                    {
                        WeakReference<ItemsControl> wr = s_TargetToTraceListMap[i].Item1;
                        ItemsControl itemsControl;
                        if (wr.TryGetTarget(out itemsControl) && itemsControl == target)
                        {
                            traceList = s_TargetToTraceListMap[i].Item2;
                            break;
                        }
                    }

                    // otherwise, if target's name matches, add a new entry to the map
                    if (traceList == null && target.Name == _targetName)
                    {
                        traceList = AddToMap(target);
                    }
                }

                return traceList;
            }

            private static TraceList AddToMap(ItemsControl target)
            {
                TraceList traceList = null;

                lock (s_TargetToTraceListMap)
                {
                    PurgeMap();
                    ++ s_seqno;

                    // get a name for the trace file
                    string filename = _fileName;
                    if (String.IsNullOrEmpty(filename) || filename == "default")
                    {
                        filename = "ScrollTrace.stf";
                    }
                    if (filename != "none" && s_seqno > 1)
                    {
                        int dotIndex = filename.LastIndexOf('.');
                        if (dotIndex < 0) dotIndex = filename.Length;
                        filename = filename.Substring(0, dotIndex) +
                            s_seqno.ToString() +
                            filename.Substring(dotIndex);
                    }

                    // create the TraceList
                    traceList = new TraceList(filename);

                    // add it to the map
                    s_TargetToTraceListMap.Add(
                        new Tuple<WeakReference<ItemsControl>,TraceList>(
                            new WeakReference<ItemsControl>(target),
                            traceList));
                }

                return traceList;
            }

            // Must be called under "lock (s_TargetToTraceListMap)"
            static void CloseAllTraceLists()
            {
                for (int i=0, n=s_TargetToTraceListMap.Count; i<n; ++i)
                {
                    TraceList traceList = s_TargetToTraceListMap[i].Item2;
                    traceList.FlushAndClose();
                }
                s_TargetToTraceListMap.Clear();
            }

            // remove entries whose targets are no longer active (closing their output files)
            // Must be called under "lock (s_TargetToTraceListMap)"
            private static void PurgeMap()
            {
                for (int i=0; i<s_TargetToTraceListMap.Count; ++i)
                {
                    WeakReference<ItemsControl> wr = s_TargetToTraceListMap[i].Item1;
                    ItemsControl unused;
                    if (!wr.TryGetTarget(out unused))
                    {
                        TraceList traceList = s_TargetToTraceListMap[i].Item2;
                        traceList.FlushAndClose();
                        s_TargetToTraceListMap.RemoveAt(i);
                        --i;
                    }
                }
            }

            #region TraceList

            private class TraceList
            {
                private List<ScrollTraceRecord> _traceList = new List<ScrollTraceRecord>();
                private BinaryWriter _writer;
                private int _flushIndex=0;  // where last flush ended

                internal TraceList(string filename)
                {
                    if (filename != "none")
                    {
                        _writer = new BinaryWriter(File.Open(filename, FileMode.Create));
                        _writer.Write((int)s_StfFormatVersion);
                    }
                }

                internal void Add(ScrollTraceRecord record)
                {
                    _traceList.Add(record);
                }

                internal void Flush(int depth)
                {
                    if (_writer != null && depth <= _flushDepth)
                    {
                        for (; _flushIndex < _traceList.Count; ++_flushIndex)
                        {
                            _traceList[_flushIndex].Write(_writer);
                        }

                        _writer.Flush();

                        // don't let _traceList exhaust memory
                        if (_flushIndex > s_MaxTraceRecords)
                        {
                            // but keep recent history in memory, for live debugging
                            int purgeCount = _flushIndex - s_MinTraceRecords;
                            _traceList.RemoveRange(0, purgeCount);
                            _flushIndex = _traceList.Count;
                        }
                    }
                }

                internal void FlushAndClose()
                {
                    if (_writer != null)
                    {
                        Flush(_flushDepth);
                        _writer.Close();
                        _writer = null;
                    }
                }

                internal void FlushAndClear()
                {
                    if (_writer != null)
                    {
                        Flush(_flushDepth);
                        _traceList.Clear();
                        _flushIndex = 0;
                    }
                }
            }

            #endregion TraceList
        }

        #endregion ScrollTracer

        #region ScrollTracingInfo

        // dynamic data associated with a VSP that's being traced
        private class ScrollTracingInfo
        {
            internal ScrollTracer   ScrollTracer    { get; private set; }
            internal int            Generation      { get; set; }
            internal int            Depth           { get; private set; }
            internal FrameworkElement Owner         { get; private set; }
            internal VirtualizingStackPanel Parent  { get; private set; }
            internal object         ParentItem      { get; private set; }
            internal int            ItemIndex       { get; private set; }

            internal ScrollTracingInfo(ScrollTracer tracer, int generation, int depth, FrameworkElement owner, VirtualizingStackPanel parent, object parentItem, int itemIndex)
            {
                ScrollTracer = tracer;
                Generation = generation;
                Depth = depth;
                Owner = owner;
                Parent = parent;
                ParentItem = parentItem;
                ItemIndex = itemIndex;
            }

            internal void ChangeItem(object newItem)
            {
                ParentItem = newItem;
            }

            internal void ChangeIndex(int newIndex)
            {
                ItemIndex = newIndex;
            }
        }

        static readonly UncommonField<ScrollTracingInfo>
            ScrollTracingInfoField = new UncommonField<ScrollTracingInfo>();

        #endregion ScrollTracingInfo

        #region ScrollTraceRecord and opcodes

        private enum ScrollTraceOp: ushort
        {
            NoOp,
            ID,
            Mark,

            LineUp,
            LineDown,
            LineLeft,
            LineRight,
            PageUp,
            PageDown,
            PageLeft,
            PageRight,
            MouseWheelUp,
            MouseWheelDown,
            MouseWheelLeft,
            MouseWheelRight,
            SetHorizontalOffset,
            SetVerticalOffset,
            SetHOff,
            SetVOff,
            MakeVisible,

            BeginMeasure,
            EndMeasure,
            BeginArrange,
            EndArrange,
            LayoutUpdated,

            BSetAnchor,
            ESetAnchor,
            BOnAnchor,
            ROnAnchor,              // Reschedule
            SOnAnchor,              // Success
            EOnAnchor,

            RecycleChildren,
            RemoveChildren,
            ItemsChanged,

            IsScrollActive,

            CFCIV,                  // ComputeFirstContainerInViewport
            CFIVIO,                 // ComputeFirstItemInViewportIndexAndOffset
            SyncAveSize,

            StoreSubstOffset,
            UseSubstOffset,
            ReviseArrangeOffset,

            SVSDBegin,              // SetAndVerifyScrollingData
            AdjustOffset,               // Adjust offset after ave. container size change
            ScrollBarChangeVisibility,  // Scroll bar visibility changed
            RemeasureCycle,             // cycle detected while scroll-to-end
            RemeasureEndExpandViewport, // expand viewport near end of list
            RemeasureEndChangeOffset,   // change offset to be at end of list
            RemeasureEndExtentChanged,  // extent changed, change offset at end of list
            RemeasureRatio,             // preserve offset/extent ratio
            RecomputeFirstOffset,       // recalculate _firstItemInExtendedViewportOffset
            LastPageSizeChange,         // pixel size for last page changed
            SVSDEnd,                // End - report new offset, extent, compoff, vp

            /****** Added in Version 1 ******/
            SetContainerSize,
            SizeChangeDuringAnchorScroll,
        }

        private class ScrollTraceRecord
        {
            internal ScrollTraceRecord(ScrollTraceOp op, VirtualizingStackPanel vsp,
                int vspDepth, int itemIndex, int opDepth, string detail)
            {
                Op = op;
                VSP = vsp;
                VDepth = vspDepth;
                ItemIndex = itemIndex;
                OpDepth = opDepth;
                Detail = detail;
            }

            internal ScrollTraceOp          Op          { get; private set; }
            internal int                    OpDepth     { get; private set; }
            internal VirtualizingStackPanel VSP         { get; private set; }
            internal int                    VDepth      { get; private set; }
            internal int                    ItemIndex   { get; private set; }
            internal string                 Detail      { get; set; }

            object _extraData;

            internal Snapshot Snapshot
            {
                get { return _extraData as Snapshot; }
                set { _extraData = value; }
            }

            internal List<String> RevirtualizedChildren
            {
                get { return _extraData as List<String>; }
                set { _extraData = value; }
            }

            internal void ChangeOpDepth(int delta)      { OpDepth += delta; }

            public override string ToString()
            {
                return String.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4}",
                    OpDepth, VDepth, ItemIndex, Op, Detail);
            }

            internal void Write(BinaryWriter writer)
            {
                writer.Write((ushort)Op);
                writer.Write(OpDepth);
                writer.Write(VDepth);
                writer.Write(ItemIndex);
                writer.Write(Detail);

                Snapshot snapshot;
                List<String> children;

                if ((snapshot = Snapshot) != null)
                {
                    writer.Write((byte)1);
                    Snapshot.Write(writer, VSP);
                }
                else if ((children = RevirtualizedChildren) != null)
                {
                    int n = children.Count;
                    writer.Write((byte)2);
                    writer.Write(n);
                    for (int i=0; i<n; ++i)
                    {
                        writer.Write(children[i]);
                    }
                }
                else
                {
                    writer.Write((byte)0);
                }
            }
        }

        #endregion ScrollTraceRecord and opcodes

        #region Snapshot

        // a snapshot of the state of the VSP
        private class Snapshot
        {
            internal ScrollData  _scrollData;
            internal BoolField   _boolFieldStore;
            internal bool?       _areContainersUniformlySized;
            internal double?     _uniformOrAverageContainerSize;
            internal double?     _uniformOrAverageContainerPixelSize;
            internal List<ChildInfo> _realizedChildren;
            internal int         _firstItemInExtendedViewportChildIndex;
            internal int         _firstItemInExtendedViewportIndex;
            internal double      _firstItemInExtendedViewportOffset;
            internal int         _actualItemsInExtendedViewportCount;
            internal Rect        _viewport;
            internal int         _itemsInExtendedViewportCount;
            internal Rect        _extendedViewport;
            internal Size        _previousStackPixelSizeInViewport;
            internal Size        _previousStackLogicalSizeInViewport;
            internal Size        _previousStackPixelSizeInCacheBeforeViewport;
            internal FrameworkElement _firstContainerInViewport;
            internal double      _firstContainerOffsetFromViewport;
            internal double      _expectedDistanceBetweenViewports;
            internal DependencyObject _bringIntoViewContainer;
            internal DependencyObject _bringIntoViewLeafContainer;
            internal List<Double> _effectiveOffsets;

            internal void Write(BinaryWriter writer, VirtualizingStackPanel vsp)
            {
                if (_scrollData == null)
                {
                    writer.Write(false);
                }
                else
                {
                    writer.Write(true);
                    WriteVector(writer, ref _scrollData._offset);
                    WriteSize(writer, ref _scrollData._extent);
                    WriteVector(writer, ref _scrollData._computedOffset);
                }

                writer.Write((byte)_boolFieldStore);
                writer.Write(!(_areContainersUniformlySized == false));
                writer.Write(_uniformOrAverageContainerSize.HasValue ? (double)_uniformOrAverageContainerSize : -1.0d);
                writer.Write(_uniformOrAverageContainerPixelSize.HasValue ? (double)_uniformOrAverageContainerPixelSize : -1.0d);
                writer.Write(_firstItemInExtendedViewportChildIndex);
                writer.Write(_firstItemInExtendedViewportIndex);
                writer.Write(_firstItemInExtendedViewportOffset);
                writer.Write(_actualItemsInExtendedViewportCount);
                WriteRect(writer, ref _viewport);
                writer.Write(_itemsInExtendedViewportCount);
                WriteRect(writer, ref _extendedViewport);
                WriteSize(writer, ref _previousStackPixelSizeInViewport);
                WriteSize(writer, ref _previousStackLogicalSizeInViewport);
                WriteSize(writer, ref _previousStackPixelSizeInCacheBeforeViewport);
                writer.Write(vsp.ContainerPath(_firstContainerInViewport));
                writer.Write(_firstContainerOffsetFromViewport);
                writer.Write(_expectedDistanceBetweenViewports);
                writer.Write(vsp.ContainerPath(_bringIntoViewContainer));
                writer.Write(vsp.ContainerPath(_bringIntoViewLeafContainer));

                writer.Write(_realizedChildren.Count);
                for (int i=0; i<_realizedChildren.Count; ++i)
                {
                    ChildInfo ci = _realizedChildren[i];
                    writer.Write(ci._itemIndex);
                    WriteSize(writer, ref ci._desiredSize);
                    WriteRect(writer, ref ci._arrangeRect);
                    WriteThickness(writer, ref ci._inset);
                }

                if (_effectiveOffsets != null)
                {
                    writer.Write(_effectiveOffsets.Count);
                    foreach (double offset in _effectiveOffsets)
                    {
                        writer.Write(offset);
                    }
                }
                else
                {
                    writer.Write((int)0);
                }
            }

            private static void WriteRect(BinaryWriter writer, ref Rect rect)
            {
                writer.Write(rect.Left);
                writer.Write(rect.Top);
                writer.Write(rect.Width);
                writer.Write(rect.Height);
            }

            private static void WriteSize(BinaryWriter writer, ref Size size)
            {
                writer.Write(size.Width);
                writer.Write(size.Height);
            }

            private static void WriteVector(BinaryWriter writer, ref Vector vector)
            {
                writer.Write(vector.X);
                writer.Write(vector.Y);
            }

            private static void WriteThickness(BinaryWriter writer, ref Thickness thickness)
            {
                writer.Write(thickness.Left);
                writer.Write(thickness.Top);
                writer.Write(thickness.Right);
                writer.Write(thickness.Bottom);
            }
        }

        private class ChildInfo
        {
            internal int         _itemIndex;
            internal Size        _desiredSize;
            internal Rect        _arrangeRect;
            internal Thickness   _inset;

            public override string ToString()
            {
                return String.Format(CultureInfo.InvariantCulture, "{0} ds: {1} ar: {2} in: {3}",
                    _itemIndex, _desiredSize, _arrangeRect, _inset);
            }
        }

        private Snapshot TakeSnapshot()
        {
            Snapshot s = new Snapshot();

            if (IsScrolling)
            {
                s._scrollData = new ScrollData();
                s._scrollData._offset = _scrollData._offset;
                s._scrollData._extent = _scrollData._extent;
                s._scrollData._computedOffset = _scrollData._computedOffset;
                s._scrollData._viewport = _scrollData._viewport;
            }

            s._boolFieldStore                               = _boolFieldStore;
            s._areContainersUniformlySized                  = AreContainersUniformlySized;
            s._firstItemInExtendedViewportChildIndex        = _firstItemInExtendedViewportChildIndex;
            s._firstItemInExtendedViewportIndex             = _firstItemInExtendedViewportIndex;
            s._firstItemInExtendedViewportOffset            = _firstItemInExtendedViewportOffset;
            s._actualItemsInExtendedViewportCount           = _actualItemsInExtendedViewportCount;
            s._viewport                                     = _viewport;
            s._itemsInExtendedViewportCount                 = _itemsInExtendedViewportCount;
            s._extendedViewport                             = _extendedViewport;
            s._previousStackPixelSizeInViewport             = _previousStackPixelSizeInViewport;
            s._previousStackLogicalSizeInViewport           = _previousStackLogicalSizeInViewport;
            s._previousStackPixelSizeInCacheBeforeViewport  = _previousStackPixelSizeInCacheBeforeViewport;
            s._firstContainerInViewport                     = FirstContainerInViewport;
            s._firstContainerOffsetFromViewport             = FirstContainerOffsetFromViewport;
            s._expectedDistanceBetweenViewports             = ExpectedDistanceBetweenViewports;
            s._bringIntoViewContainer                       = _bringIntoViewContainer;
            s._bringIntoViewLeafContainer                   = BringIntoViewLeafContainer;

            SnapshotData data = SnapshotDataField.GetValue(this);
            if (data != null)
            {
                s._uniformOrAverageContainerSize            = data.UniformOrAverageContainerSize;
                s._uniformOrAverageContainerPixelSize       = data.UniformOrAverageContainerPixelSize;
                s._effectiveOffsets                         = data.EffectiveOffsets;
                SnapshotDataField.ClearValue(this);
            }

            ItemContainerGenerator g = Generator as ItemContainerGenerator;
            List<ChildInfo> list = new List<ChildInfo>();
            foreach (UIElement child in RealizedChildren)
            {
                ChildInfo info = new ChildInfo();
                info._itemIndex = g.IndexFromContainer(child, returnLocalIndex:true);
                info._desiredSize = child.DesiredSize;
                info._arrangeRect = child.PreviousArrangeRect;
                info._inset = (Thickness)child.GetValue(ItemsHostInsetProperty);
                list.Add(info);
            }
            s._realizedChildren = list;

            return s;
        }

        private string ContainerPath(DependencyObject container)
        {
            if (container == null)
                return String.Empty;

            VirtualizingStackPanel vsp = VisualTreeHelper.GetParent(container) as VirtualizingStackPanel;
            if (vsp == null)
            {
                return "{Disconnected}";
            }
            else if (vsp == this)
            {
                ItemContainerGenerator g = Generator as ItemContainerGenerator;
                return String.Format(CultureInfo.InvariantCulture, "{0}", g.IndexFromContainer(container, returnLocalIndex:true));
            }
            else
            {
                ItemContainerGenerator g = vsp.Generator as ItemContainerGenerator;
                int localIndex = g.IndexFromContainer(container, returnLocalIndex:true);
                DependencyObject parentContainer = ItemsControl.ContainerFromElement(null, vsp);
                if (parentContainer == null)
                {
                    return String.Format(CultureInfo.InvariantCulture, "{0}", localIndex);
                }
                else
                {
                    return String.Format(CultureInfo.InvariantCulture, "{0}.{1}",
                        ContainerPath(parentContainer), localIndex);
                }
            }
        }

        // data that is included in a snapshot, but isn't directly available
        // from the VSP.  Created in Measure or Arrange (where the data is locally
        // available), and discarded in TakeSnapshot.
        private class SnapshotData
        {
            internal double UniformOrAverageContainerSize { get; set; }
            internal double UniformOrAverageContainerPixelSize { get; set; }
            internal List<double> EffectiveOffsets { get; set; }
        }

        #endregion Snapshot

        #endregion Private Structures Classes
    }
}
