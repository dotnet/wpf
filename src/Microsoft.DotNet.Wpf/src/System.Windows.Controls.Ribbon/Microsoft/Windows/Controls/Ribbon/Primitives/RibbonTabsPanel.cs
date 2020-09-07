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
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;
    using MS.Internal;
#if RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Controls;
#endif

    #endregion

    public class RibbonTabsPanel : Panel, IScrollInfo
    {
        #region Protected Methods

        /// <summary>
        ///     Measures the children
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            UIElementCollection children = InternalChildren;
            int childCount = children.Count;
            Size returnSize = new Size();

            for (int i = 0; i < childCount; i++)
            {
                children[i].Measure(availableSize);
                Size childSize = children[i].DesiredSize;
                returnSize = new Size(Math.Max(returnSize.Width, childSize.Width), Math.Max(returnSize.Height, childSize.Height));
            }
            return returnSize;
        }

        /// <summary>
        ///     Arranges the children
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            UIElementCollection children = InternalChildren;
            int childCount = children.Count;

            for (int i = 0; i < childCount; i++)
            {
                children[i].Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            }
            return finalSize;
        }

        #endregion

        #region Private Members
        
        private Ribbon Ribbon
        {
            get
            {
                if (_ribbon == null)
                {
                    _ribbon = TreeHelper.FindTemplatedAncestor<Ribbon>(this);
                }
                return _ribbon;
            }
        }

        private Ribbon _ribbon;

        #endregion

        #region IScrollInfo Members

        public ScrollViewer ScrollOwner
        {
            get { return ScrollData._scrollOwner; }
            set { ScrollData._scrollOwner = value; }
        }

        public void SetHorizontalOffset(double offset)
        {
            double newValue = ValidateInputOffset(offset, "HorizontalOffset");
            if (!DoubleUtil.AreClose(ScrollData._offsetX, newValue))
            {
                _scrollData._offsetX = newValue;
                InvalidateMeasure();
            }
        }

        public double ExtentWidth
        {
            get { return ScrollData._extentWidth; }
        }

        public double HorizontalOffset
        {
            get { return ScrollData._offsetX; }
        }

        public double ViewportWidth
        {
            get { return ScrollData._viewportWidth; }
        }

        public void LineLeft()
        {
            SetHorizontalOffset(HorizontalOffset - 16.0);
        }

        public void LineRight()
        {
            SetHorizontalOffset(HorizontalOffset + 16.0);
        }

        // This is optimized for horizontal scrolling only
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

            // Compute the child's rect relative to (0,0) in our coordinate space.
            GeneralTransform childTransform = visual.TransformToAncestor(this);

            rectangle = childTransform.TransformBounds(rectangle);

            // Initialize the viewport
            Rect viewport = new Rect(HorizontalOffset, rectangle.Top, ViewportWidth, rectangle.Height);
            rectangle.X += viewport.X;

            // Compute the offsets required to minimally scroll the child maximally into view.
            double minX = ComputeScrollOffsetWithMinimalScroll(viewport.Left, viewport.Right, rectangle.Left, rectangle.Right);

            // We have computed the scrolling offsets; scroll to them.
            double originalOffset = ScrollData._offsetX;
            SetHorizontalOffset(minX);

            if (!DoubleUtil.AreClose(originalOffset, ScrollData._offsetX))
            {
                OnScrollChange();
            }

            // Compute the visible rectangle of the child relative to the viewport.
            viewport.X = minX;
            rectangle.Intersect(viewport);

            rectangle.X -= viewport.X;

            // Return the rectangle
            return rectangle;
        }

        private void OnScrollChange()
        {
            if (ScrollOwner != null) { ScrollOwner.InvalidateScrollInfo(); }
        }

        internal static double ComputeScrollOffsetWithMinimalScroll(
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
            // These child thus may overlap with the viewport, but will scroll the same direction
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
                return bottomChild - (bottomView - topView);
            }

            // Handle cases: 5 & 6 above.
            return topView;
        }

        // Does not support other scrolling than LineLeft/LineRight
        public void MouseWheelDown() 
        { 
        }
        
        public void MouseWheelLeft() 
        { 
        }
        
        public void MouseWheelRight() 
        { 
        }
        
        public void MouseWheelUp() 
        { 
        }
        
        public void LineDown() 
        { 
        }
        
        public void LineUp() 
        { 
        }
        
        public void PageDown() 
        { 
        }
        
        public void PageLeft() 
        {
        }
        
        public void PageRight() 
        { 
        }
        
        public void PageUp() 
        {
        }

        public void SetVerticalOffset(double offset) 
        { 
        }

        public bool CanVerticallyScroll
        {
            get { return false; }
            set { }
        }

        public bool CanHorizontallyScroll
        {
            get { return true; }
            set { }
        }

        public double ExtentHeight
        {
            get { return 0.0; }
        }

        public double VerticalOffset
        {
            get { return 0.0; }
        }

        public double ViewportHeight
        {
            get { return 0.0; }
        }

        private ScrollData ScrollData
        {
            get
            {
                return _scrollData ?? (_scrollData = new ScrollData());
            }
        }

        private ScrollData _scrollData;

        internal static double ValidateInputOffset(double offset, string parameterName)
        {
            if (double.IsNaN(offset))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }

            return Math.Max(0.0, offset);
        }

        #endregion
    }

    //-----------------------------------------------------------
    // ScrollData class
    //-----------------------------------------------------------
    #region ScrollData

    // Helper class to hold scrolling data.
    // This class exists to reduce working set when SCP is delegating to another implementation of ISI.
    // Standard "extra pointer always for less data sometimes" cache savings model:
    internal class ScrollData
    {
        internal ScrollViewer _scrollOwner;

        internal double _offsetX;

        internal double _viewportWidth; // ViewportSize is computed from our FinalSize, but may be in different units.
        internal double _extentWidth; // Extent is the total size of our content.
    }

    #endregion ScrollData
}
