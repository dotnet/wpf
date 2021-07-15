// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: IScrollInfo implementation helper for FlowDocumentView, TextBoxView.
//

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MS.Internal;
using System.Windows.Controls.Primitives; // for doc comments

namespace MS.Internal.Documents
{
    // IScrollInfo implementation helper for FlowDocumentView, TextBoxView.
    internal class ScrollData
    {
        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// <see cref="IScrollInfo.LineUp"/>
        /// </summary>
        internal void LineUp(UIElement owner)
        {
            SetVerticalOffset(owner, _offset.Y - ScrollViewer._scrollLineDelta);
        }

        /// <summary>
        /// <see cref="IScrollInfo.LineDown"/>
        /// </summary>
        internal void LineDown(UIElement owner)
        {
            SetVerticalOffset(owner, _offset.Y + ScrollViewer._scrollLineDelta);
        }

        /// <summary>
        /// <see cref="IScrollInfo.LineLeft"/>
        /// </summary>
        internal void LineLeft(UIElement owner)
        {
            SetHorizontalOffset(owner, _offset.X - ScrollViewer._scrollLineDelta);
        }

        /// <summary>
        /// <see cref="IScrollInfo.LineRight"/>
        /// </summary>
        internal void LineRight(UIElement owner)
        {
            SetHorizontalOffset(owner, _offset.X + ScrollViewer._scrollLineDelta);
        }

        /// <summary>
        /// <see cref="IScrollInfo.PageUp"/>
        /// </summary>
        internal void PageUp(UIElement owner)
        {
            SetVerticalOffset(owner, _offset.Y - _viewport.Height);
        }

        /// <summary>
        /// <see cref="IScrollInfo.PageDown"/>
        /// </summary>
        internal void PageDown(UIElement owner)
        {
            SetVerticalOffset(owner, _offset.Y + _viewport.Height);
        }

        /// <summary>
        /// <see cref="IScrollInfo.PageLeft"/>
        /// </summary>
        internal void PageLeft(UIElement owner)
        {
            SetHorizontalOffset(owner, _offset.X - _viewport.Width);
        }

        /// <summary>
        /// <see cref="IScrollInfo.PageRight"/>
        /// </summary>
        internal void PageRight(UIElement owner)
        {
            SetHorizontalOffset(owner, _offset.X + _viewport.Width);
        }

        /// <summary>
        /// <see cref="IScrollInfo.MouseWheelUp"/>
        /// </summary>
        internal void MouseWheelUp(UIElement owner)
        {
            SetVerticalOffset(owner, _offset.Y - ScrollViewer._mouseWheelDelta);
        }

        /// <summary>
        /// <see cref="IScrollInfo.MouseWheelDown"/>
        /// </summary>
        internal void MouseWheelDown(UIElement owner)
        {
            SetVerticalOffset(owner, _offset.Y + ScrollViewer._mouseWheelDelta);
        }

        /// <summary>
        /// <see cref="IScrollInfo.MouseWheelLeft"/>
        /// </summary>
        internal void MouseWheelLeft(UIElement owner)
        {
            SetHorizontalOffset(owner, _offset.X - ScrollViewer._mouseWheelDelta);
        }

        /// <summary>
        /// <see cref="IScrollInfo.MouseWheelRight"/>
        /// </summary>
        internal void MouseWheelRight(UIElement owner)
        {
            SetHorizontalOffset(owner, _offset.X + ScrollViewer._mouseWheelDelta);
        }

        /// <summary>
        /// <see cref="IScrollInfo.SetHorizontalOffset"/>
        /// </summary>
        internal void SetHorizontalOffset(UIElement owner, double offset)
        {
            if (!this.CanHorizontallyScroll)
            {
                return;
            }

            offset = Math.Max(0, Math.Min(_extent.Width - _viewport.Width, offset));
            if (!DoubleUtil.AreClose(offset, _offset.X))
            {
                _offset.X = offset;
                owner.InvalidateArrange();
                if (_scrollOwner != null)
                {
                    _scrollOwner.InvalidateScrollInfo();
                }
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.SetVerticalOffset"/>
        /// </summary>
        internal void SetVerticalOffset(UIElement owner, double offset)
        {
            if (!this.CanVerticallyScroll)
            {
                return;
            }

            offset = Math.Max(0, Math.Min(_extent.Height - _viewport.Height, offset));
            if (!DoubleUtil.AreClose(offset, _offset.Y))
            {
                _offset.Y = offset;
                owner.InvalidateArrange();
                if (_scrollOwner != null)
                {
                    _scrollOwner.InvalidateScrollInfo();
                }
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.MakeVisible"/>
        /// </summary>
        internal Rect MakeVisible(UIElement owner, Visual visual, Rect rectangle)
        {
            // We can only work on visuals that are us or children.
            // An empty rect has no size or position.  We can't meaningfully use it.
            if (rectangle.IsEmpty ||
                visual == null ||
                (visual != owner && !owner.IsAncestorOf(visual)))
            {
                return Rect.Empty;
            }

            // Compute the child's rect relative to (0,0) in our coordinate space.
            GeneralTransform childTransform = visual.TransformToAncestor(owner);
            rectangle = childTransform.TransformBounds(rectangle);

            // Initialize the viewport
            Rect viewport = new Rect(_offset.X, _offset.Y, _viewport.Width, _viewport.Height);
            rectangle.X += viewport.X;
            rectangle.Y += viewport.Y;

            // Compute the offsets required to minimally scroll the child maximally into view.
            double minX = ComputeScrollOffset(viewport.Left, viewport.Right, rectangle.Left, rectangle.Right);
            double minY = ComputeScrollOffset(viewport.Top, viewport.Bottom, rectangle.Top, rectangle.Bottom);

            // We have computed the scrolling offsets; scroll to them.
            SetHorizontalOffset(owner, minX);
            SetVerticalOffset(owner, minY);

            // Compute the visible rectangle of the child relative to the viewport.
            if (this.CanHorizontallyScroll)
            {
                viewport.X = minX;
            }
            else
            {
                // munge the intersection
                rectangle.X = viewport.X;
            }
            if (this.CanVerticallyScroll)
            {
                viewport.Y = minY;
            }
            else
            {
                // munge the intersection
                rectangle.Y = viewport.Y;
            }
            rectangle.Intersect(viewport);
            if (!rectangle.IsEmpty)
            {
                rectangle.X -= viewport.X;
                rectangle.Y -= viewport.Y;
            }

            // Return the rectangle
            return rectangle;
        }

        /// <summary>
        /// <see cref="IScrollInfo.ScrollOwner"/>
        /// </summary>
        internal void SetScrollOwner(UIElement owner, ScrollViewer value)
        {
            if (value != _scrollOwner)
            {
                // Reset cached scroll info.
                _disableHorizonalScroll = false;
                _disableVerticalScroll = false;
                _offset = new Vector();
                _viewport = new Size();
                _extent = new Size();

                _scrollOwner = value;
                owner.InvalidateArrange();
            }
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// <see cref="IScrollInfo.CanVerticallyScroll"/>
        /// </summary>
        internal bool CanVerticallyScroll
        {
            get
            {
                return !_disableVerticalScroll;
            }
            set
            {
                _disableVerticalScroll = !value;
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.CanHorizontallyScroll"/>
        /// </summary>
        internal bool CanHorizontallyScroll
        {
            get
            {
                return !_disableHorizonalScroll;
            }
            set
            {
                _disableHorizonalScroll = !value;
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.ExtentWidth"/>
        /// </summary>
        internal double ExtentWidth
        {
            get
            {
                return _extent.Width;
            }

            set
            {
                _extent.Width = value;
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.ExtentHeight"/>
        /// </summary>
        internal double ExtentHeight
        {
            get
            {
                return _extent.Height;
            }

            set
            {
                _extent.Height = value;
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.ViewportWidth"/>
        /// </summary>
        internal double ViewportWidth
        {
            get
            {
                return _viewport.Width;
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.ViewportHeight"/>
        /// </summary>
        internal double ViewportHeight
        {
            get
            {
                return _viewport.Height;
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.HorizontalOffset"/>
        /// </summary>
        internal double HorizontalOffset
        {
            get
            {
                return _offset.X;
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.VerticalOffset"/>
        /// </summary>
        internal double VerticalOffset
        {
            get
            {
                return _offset.Y;
            }
        }

        /// <summary>
        /// <see cref="IScrollInfo.ScrollOwner"/>
        /// </summary>
        internal ScrollViewer ScrollOwner
        {
            get
            {
                return _scrollOwner;
            }
        }

        // HorizontalOffset/VerticalOffset as a Vector.
        internal Vector Offset
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

        // ExtenttWidth/ExtentHeight as a Size.
        internal Size Extent
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

        // ViewportWidth/ViewportHeight as a Size.
        internal Size Viewport
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

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Compute scroll offset for child rectangle.
        /// </summary>
        private double ComputeScrollOffset(double topView, double bottomView, double topChild, double bottomChild)
        {
            // # CHILD POSITION             REMEDY
            // 1 Above viewport             Align top edge of child & viewport
            // 2 Below viewport             Align top edge of child & viewport
            // 3 Entirely within viewport   No scroll
            // 4 Spanning viewport          Align top edge of child & viewport
            //
            // Note: "Above viewport" = childTop above viewportTop, childBottom above viewportBottom
            //       "Below viewport" = childTop below viewportTop, childBottom below viewportBottom
            // These child thus may overlap with the viewport, but will scroll the same direction/

            bool topInView = DoubleUtil.GreaterThanOrClose(topChild, topView) && DoubleUtil.LessThan(topChild, bottomView);
            bool bottomInView = DoubleUtil.LessThanOrClose(bottomChild, bottomView) && DoubleUtil.GreaterThan(bottomChild, topView);

            double position;
            if (topInView && bottomInView)
            {
                position = topView;
            }
            else
            {
                position = topChild;
            }
            return position;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private bool _disableHorizonalScroll;
        private bool _disableVerticalScroll;
        private Vector _offset;
        private Size _viewport;
        private Size _extent;
        private ScrollViewer _scrollOwner;

        #endregion Private Fields
    }
}
