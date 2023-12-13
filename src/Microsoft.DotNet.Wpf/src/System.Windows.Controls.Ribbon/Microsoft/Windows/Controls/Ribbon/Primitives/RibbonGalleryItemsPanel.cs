// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon.Primitives
#else
namespace Microsoft.Windows.Controls.Ribbon.Primitives
#endif
{

    #region Using Declarations

    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Diagnostics;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif
    using MS.Internal;

    #endregion

    /// <summary>
    ///   The default Items panel for the RibbonGalleryCategory class.
    /// </summary>
    public class RibbonGalleryItemsPanel : Panel
    {

        #region Private Methods

        // It just determines if any ancestor supports StarLayout and is not in StarLayoutPass mode.
        private bool IsAutoLayoutPass()
        {
            RibbonGallery gallery = Gallery;
            if (gallery != null)
            {
                RibbonGalleryCategoriesPanel categoriesPanel = gallery.ItemsHostSite as RibbonGalleryCategoriesPanel;
                if (categoriesPanel != null)
                {
                    IContainsStarLayoutManager iContainsStarLayoutManager = (IContainsStarLayoutManager)categoriesPanel;
                    if (iContainsStarLayoutManager.StarLayoutManager != null)
                        return !iContainsStarLayoutManager.StarLayoutManager.IsStarLayoutPass;
                }
            }

            return false;
        }

        private void AddScrollDeltaInfo(double sumOfHeight, int childrenCount)
        {
            RibbonGalleryCategory category = Category;
            if (category != null)
            {
                // Adding virtual count of items and cumulative height to RGC for the purpose of calcualting 
                // avg height as scrolling delta in RibbonGalleryCategoriesPanel.
                category.averageItemHeightInfo.Count = childrenCount;
                category.averageItemHeightInfo.CumulativeHeight = sumOfHeight;
            }
        }

        // Minimum number of Col must be shown is determined by this method
        private int GetMinColumnCount()
        {
            RibbonGalleryCategory category = Category;
            if (category != null)
            {
                return (int)category.MinColumnCount;
            }
            return 1;
        }

        // Maximum number of Col must be shown is determined by this method
        private int GetMaxColumnCount()
        {
            RibbonGalleryCategory category = Category;
            if (category != null)
            {
                return (int)category.MaxColumnCount;
            }

            return int.MaxValue;
        }

        // Gets the MaxColumnWidth if the shared scope is Gallery
        // then MaxColumnWidth is used which is defined on Gallery to lay out all Items as Uniform width columns in scope of Gallery
        // otherwise they layout as Uniform width column within the scope of current Category only.
        private double GetMaxColumnWidth()
        {
            RibbonGallery gallery = Gallery;
            if (gallery != null && SharedColumnSizeScopeAtGalleryLevel)
            {
                return gallery.MaxColumnWidth;
            }

            return _maxColumnWidth;
        }

#if IN_RIBBON_GALLERY
        // MaxItemHeight is the maximum height of an Item among all the Items of all categories in the gallery this panel is part of.
        // MaximumItemHeight is used by InRibbonGallery to find the Item Box for uniformity.
        private double GetMaxItemHeight()
        {
            RibbonGallery gallery = Gallery;
            if (gallery != null)
            {
                return gallery.MaxItemHeight;
            }

            return _maxItemHeight;
        }
#endif

        // Sets the MaxColumnWidth on Gallery if the shared scope is Gallery
        private void SetMaxColumnWidth(double value)
        {
            MaxColumnWidth = value;

            RibbonGallery gallery = Gallery;
            if (gallery != null)
            {
                if (SharedColumnSizeScopeAtGalleryLevel)
                {
                    if (gallery.MaxColumnWidth < value || !gallery.IsMaxColumnWidthValid)
                    {
                        gallery.MaxColumnWidth = value;
                        gallery.IsMaxColumnWidthValid = true;
                    }
                }
            }
        }

#if IN_RIBBON_GALLERY
        private void SetMaxColumnWidthAndHeight(double maxWidth, double maxHeight)
        {
            SetMaxColumnWidth(maxWidth);

            RibbonGallery gallery = Gallery;
            if (gallery != null)
            {            
                if (gallery.MaxItemHeight < maxHeight)
                {
                    gallery.MaxItemHeight = maxHeight;
                }
            }
        }
#endif

        private double GetArrangeWidth()
        {
            RibbonGallery gallery = Gallery;
            if (gallery != null && SharedColumnSizeScopeAtGalleryLevel)
            {
                return gallery.ArrangeWidth;
            }

            return _arrangeWidth;
        }

        // Sets the ArrangeWidth on Gallery if the shared scope is Gallery
        private void SetArrangeWidth(double value)
        {
            _arrangeWidth = value;

            RibbonGallery gallery = Gallery;
            if (gallery != null && SharedColumnSizeScopeAtGalleryLevel)
            {
                gallery.ArrangeWidth = value;
                gallery.IsArrangeWidthValid = true;
            }
        }

        #endregion Private Methods

        #region Protected Overrides
        
        // There are three different scenarios:
        // 1. In InRibbonGalleryMode where the gallery is shown within Ribbon via InRibbonGallery and Measure becomes responsibility 
        //    of InRibbonGalleryModeMeasureOverride.
        // 2. Popup mode : it's the common and RealMeasureOverride which is the common case of RibbonGallery hosted normally in Popup
        //    Weather it belongs to InRibbonGallery or any other control containing Gallery like RibbonComboBox/RibbonMenuButton etc.
        // 3. Popup mode also utilizes AutoPass to be understood by hosting Control like RibbonComboBox/RibbonMenuButton etc.
        protected override Size MeasureOverride(Size availableSize)
        {
            RibbonGallery gallery = Gallery;
#if IN_RIBBON_GALLERY
            if (gallery != null && gallery.IsInInRibbonGalleryMode())
            {
                return InRibbonGalleryModeMeasureOverride(availableSize);
            }
            else
            {
#endif
                return RealMeasureOverride(availableSize);
#if IN_RIBBON_GALLERY
            }
#endif
        }

#if IN_RIBBON_GALLERY
        // InRibbonGalleryMode where the gallery is shown within Ribbon via InRibbonGallery and Measure becomes responsibility 
        // of InRibbonGalleryModeMeasureOverride.
        private Size InRibbonGalleryModeMeasureOverride(Size availableSize)
        {
            UIElementCollection children = InternalChildren;
            Size panelSize = new Size();
            Size childConstraint = new Size(double.PositiveInfinity, double.PositiveInfinity);
            double maxItemHeight = 0;
            double maxColumnWidth = 0;
            double sumItemHeight = 0;
            int columnCount = 0;
            int childrenCount = children.Count;
            int minColumnCount = GetMinColumnCount();
            int maxColumnCount = GetMaxColumnCount();

            Debug.Assert(maxColumnCount >= minColumnCount);

            // Determine the maximum column width so that all items 
            // can be hosted in equispaced columns. row height is auto
            // and depends on the maximum height of the items in that row
            for (int i = 0; i < childrenCount; i++)
            {
                UIElement child = children[i] as UIElement;

                // It has been already measure once directly in RibbonCategoriesPanel and hence will get short circuit already.
                // The call still needed as to Measure in case if the Parent panel is being changed from 
                // RibbonGalleryCatgoriesPanel to something else.
                child.Measure(childConstraint);

                Size childSize = child.DesiredSize;
                maxColumnWidth = Math.Max(maxColumnWidth, childSize.Width);
                maxItemHeight = Math.Max(maxItemHeight, childSize.Height);
                sumItemHeight += childSize.Height;
            }

            // It has been already calculated once directly in RibbonCategoriesPanel and hence will get short circuit already.
            // The call still needs to be made in case if the Parent panel is being changed from RibbonGalleryCatgoriesPanel 
            // to something else.
            SetMaxColumnWidthAndHeight(maxColumnWidth, maxItemHeight);

            // Gets the final MaxColumnWidth for this panel.
            maxColumnWidth = GetMaxColumnWidth();
            maxItemHeight = GetMaxItemHeight();

            AddScrollDeltaInfo(sumItemHeight, childrenCount);
            
            if (!double.IsInfinity(availableSize.Width) && maxColumnWidth != 0)
            {
                columnCount = (int)(availableSize.Width / maxColumnWidth);
            }
            else
            {
                columnCount = childrenCount;
            }

            // Initialize Width and Height, specially in case of height it's more off leaving the empty space in panel where Items in other categories
            // are to be rendered.
            panelSize.Width = columnCount * maxColumnWidth;

            RibbonGalleryCategory category = Category;
            if (category != null)
            {
                panelSize.Height = category.RowOffset * maxItemHeight;

                if (columnCount != 0)
                {
                    // There could be space left in the last row of the previous category to render some of the items in this category. 
                    Debug.Assert(category.ColumnOffset < columnCount);
                    int spotsLeftInPreviousRow = category.ColumnOffset == 0 ? 0 : columnCount - category.ColumnOffset;

                    if (childrenCount > spotsLeftInPreviousRow)
                    {
                        int remainingChildren = childrenCount - spotsLeftInPreviousRow;
                        category.ColumnEndOffSet = remainingChildren % columnCount;

                        category.RowCount = remainingChildren / columnCount;  // Initialize RowCount as the number of full rows.

                        // Increment RowCount if we filled the previous row.
                        if (spotsLeftInPreviousRow > 0)
                            category.RowCount++;

                        // Increment RowCount if we started a new row.
                        if (category.ColumnEndOffSet > 0)
                            category.RowCount++;
                    }
                    else
                    {
                        // All children can fit in the previous row.

                        category.ColumnEndOffSet = (category.ColumnOffset + childrenCount) % columnCount;
                        category.RowCount = 1;
                    }
                }
                else
                {
                    category.ColumnEndOffSet = 0;
                    category.RowCount = 0;
                }

                panelSize.Height += category.RowCount * maxItemHeight;
            }

            return panelSize;
        }
#endif

        private Size RealMeasureOverride(Size availableSize)
        {
            // Iterate through all of the children. For each row first measure # of children
            // to infinity and gather their Max of DesiredWidths. Also, calculate the 
            // columnCount. Then space permitting measure as many more 
            // children that will fit into that row till columnCount is acheived and get 
            // MaxHeight for that Row. Cumulative height of such Rows gives you DesiredHeight
            // for Panel.
            // Return desired size for this Panel as the 
            // new Size(ColumnCount * MaxColumnWidth, cumulative RowHeights).

            UIElementCollection children = InternalChildren;
            Size panelSize = new Size();
            Size childConstraint = new Size(double.PositiveInfinity, double.PositiveInfinity);
            double maxRowHeight = 0;
            double maxItemHeight = 0;
            double sumItemHeight = 0;
            int columnCount = 0;
            int childrenCount = children.Count;
            RibbonGallery parentGallery = Gallery;
            double maxColumnWidth = (parentGallery != null && parentGallery.IsMaxColumnWidthValid) ? GetMaxColumnWidth() : 0.0;
            int minColumnCount = GetMinColumnCount();
            int maxColumnCount = GetMaxColumnCount();

            Debug.Assert(maxColumnCount >= minColumnCount);
            
            // Determine the maximum column width so that all items 
            // can be hosted in equispaced columns. row height is auto
            // and depends on the maximum height of the items in that row
            for (int i = 0; i < childrenCount; i++)
            {
                UIElement child = children[i] as UIElement;
                child.Measure(childConstraint);
                Size childSize = child.DesiredSize;
                maxColumnWidth = Math.Max(maxColumnWidth, childSize.Width);
                maxItemHeight = Math.Max(maxItemHeight, childSize.Height);
                sumItemHeight += childSize.Height;
            }

            // if none of the children has substantial width, panelsize would be equivalent to zero.
            if (maxColumnWidth == 0.0)
            {
                return panelSize;
            }

            // Updates the MaxColumnWidth of this Category as well as the of the parent Gallery if suffices all conditions.
            SetMaxColumnWidth(maxColumnWidth);

            // Gets the final MaxColumnWidth for this panel.
            maxColumnWidth = GetMaxColumnWidth();
            AddScrollDeltaInfo(sumItemHeight, childrenCount);

            if (!IsAutoLayoutPass())
            {
                if (!double.IsInfinity(availableSize.Width))
                {
                    columnCount = Math.Min(Math.Max(minColumnCount, Math.Min((int)(availableSize.Width / maxColumnWidth), childrenCount)), maxColumnCount);
                    
                    RibbonGalleryCategory category = Category;
                    if (parentGallery != null && category != null)
                    {
                        if (SharedColumnSizeScopeAtGalleryLevel && parentGallery.ColumnsStretchToFill)
                        {
                            // Since Gallery is a SharedColumnScope, store ArrangeWidth to be shared by the entire Gallery
                            double arrangeWidth = GetArrangeWidth();
                            if (!parentGallery.IsArrangeWidthValid)
                            {
                                // Calculate ArrangeWidth such that columnCount no. of columns occupy all of availableSize.Width
                                columnCount = Math.Min(Math.Max(minColumnCount, Math.Min((int)(availableSize.Width / maxColumnWidth), childrenCount)), maxColumnCount);
                                arrangeWidth = Math.Max(availableSize.Width / columnCount, maxColumnWidth);
                                SetArrangeWidth(arrangeWidth);
                            }
                            else
                            {
                                // Once a valid arrangeWidth has been computed, we use arrangeWidth to determine number of columns. 
                                columnCount = Math.Min(Math.Max(minColumnCount, Math.Min((int)(availableSize.Width / arrangeWidth), childrenCount)), maxColumnCount);
                            }
                        }
                        else if (!SharedColumnSizeScopeAtGalleryLevel && category.ColumnsStretchToFill)
                        {
                            // Since category is a sharedColumnScope. 
                            // Calculate and store _arrangeWidth locally for just this category
                            if (!_isArrangeWidthValid)
                            {
                                columnCount = Math.Min(Math.Max(minColumnCount, Math.Min((int)(availableSize.Width / maxColumnWidth), childrenCount)), maxColumnCount);
                                _arrangeWidth = Math.Max(availableSize.Width / columnCount, maxColumnWidth);
                                _isArrangeWidthValid = true;
                            }
                            else
                            {
                                // Once a valid arrangeWidth has been computed, we use arrangeWidth to determine number of columns. 
                                columnCount = Math.Min(Math.Max(minColumnCount, Math.Min((int)(availableSize.Width / _arrangeWidth), childrenCount)), maxColumnCount);
                            }
                        }
                    }
                }
                else
                {
                    columnCount = Math.Max(minColumnCount, Math.Min(childrenCount, maxColumnCount));
                }

                // Finds row Items once ColumnWidth is determined to fetch MaxHeight of a particular row.
                // Also adds these height to acheive cumulative height which is desired height of the panel.
                for (int i = 0; i < childrenCount; i++)
                {
                    UIElement child = children[i] as UIElement;
                    Size childSize = child.DesiredSize;
                    maxRowHeight = Math.Max(maxRowHeight, childSize.Height);
                    if ((i + 1) % columnCount == 0 || i == childrenCount - 1)
                    {
                        panelSize.Height += maxRowHeight;

                        // Save the maxRowHeight so it can be used for PageDown operations
                        _maxRowHeight = maxRowHeight;

                        maxRowHeight = 0;
                    }
                }
            }
            else
            {
                columnCount = minColumnCount;
                panelSize.Height = maxItemHeight;
            }

            panelSize.Width = columnCount * maxColumnWidth;
            return panelSize;
        }

        // There are two different scenarios:
        // 1. In InRibbonGalleryMode where the gallery is shown within Ribbon via InRibbonGallery and Arrange becomes responsibility 
        //    of InRibbonGalleryModeMeasureOverride.
        // 2. Popup mode : it's the common and RealArrangeOverride which is the common case of RibbonGallery hosted normally in Popup
        //    weather it belongs to InRibbonGallery or any other control containing Gallery like RibbonComboBox/RibbonMenuButton etc.
        protected override Size ArrangeOverride(Size finalSize)
        {
            RibbonGallery gallery = Gallery;
#if IN_RIBBON_GALLERY
            if (gallery != null && gallery.IsInInRibbonGalleryMode())
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
        private Size InRibbonGalleryModeArrangeOverride(Size finalSize)
        {
            // Get final coumn count by finalsizw.width , MaxColumnWidth
            // Iterate through children one row at a time. 
            // Arrange the first row at offset 0 and the next 
            // row just below that and so on. Besure to arrange 
            // the children within each row uniformly based on 
            // the MaxColumnWidth that was computed during the 
            // Measure pass.

            UIElementCollection children = InternalChildren;
            double rowStartHeight = 0;
            double rowStartWidth = 0;
            int finalColumnCount = 0;
            int childrenCount = children.Count;
            double maxColumnWidth = GetMaxColumnWidth();
            double maxItemHeight = GetMaxItemHeight();

            //Calculate the available column count based on final space
            //keeping the same column width. 
            if (maxColumnWidth == 0.0)
                return finalSize;

            if (!double.IsInfinity(finalSize.Width) && maxColumnWidth != 0)
            {
                finalColumnCount = (int)(finalSize.Width / maxColumnWidth);
            }
            else
            {
                finalColumnCount = childrenCount;
            }
            
            if (finalColumnCount == 0)
            {
                return finalSize;
            }

            RibbonGalleryCategory category = Category;

            // Calculate the starting offsets in pixels from actual Row and Coulmn offsets.
            if (category != null)
            {
                rowStartHeight = category.RowOffset * maxItemHeight;
                rowStartWidth = category.ColumnOffset * maxColumnWidth;
            }

            for (int i = 0; i < childrenCount; i++)
            {
                children[i].Arrange(new Rect(rowStartWidth, rowStartHeight, maxColumnWidth, maxItemHeight));
                rowStartWidth += maxColumnWidth;
                if ((i + category.ColumnOffset + 1) % finalColumnCount == 0)
                {
                    rowStartHeight += maxItemHeight;
                    rowStartWidth = 0;
                }
            }

            return finalSize;
        }
#endif

        private Size RealArrangeOverride(Size finalSize)
        {
            // Get final coumn count by finalsizw.width , MaxColumnWidth
            // Iterate through children one row at a time. 
            // Arrange the first row at offset 0 and the next 
            // row just below that and so on. Besure to arrange 
            // the children within each row uniformly based on 
            // the MaxColumnWidth that was computed during the 
            // Measure pass.

            UIElementCollection children = InternalChildren;
            double rowStartHeight = 0;
            double rowStartWidth = 0; 
            double maxRowHeight = 0.0;
            int finalColumnCount = 0;
            int rowStartIndex = 0;
            int childrenCount = children.Count;
            int minColumnCount = GetMinColumnCount();
            int maxColumnCount = GetMaxColumnCount();
            RibbonGallery parentGallery = Gallery;
            RibbonGalleryCategory category = Category;
            double arrangeWidth = 0.0;

            if (parentGallery != null && category != null)
            {
                if (SharedColumnSizeScopeAtGalleryLevel && parentGallery.ColumnsStretchToFill)
                {
                    // If sharedScope is Gallery, fetch global ArrangeWidth
                    arrangeWidth = GetArrangeWidth();
                }
                else if (category.IsSharedColumnSizeScope && category.ColumnsStretchToFill)
                {
                    // SharedScope is Category, use local arrangeWidth.
                    arrangeWidth = _arrangeWidth;
                }
                else
                {
                    // ColumnStretchToFill is false. 
                    arrangeWidth = GetMaxColumnWidth();
                }
            }

            //Calculate the available column count based on final space
            //keeping the same column width. 
            if (arrangeWidth == 0.0)
                return finalSize;

            finalColumnCount = Math.Max(minColumnCount, Math.Min((int)(finalSize.Width / arrangeWidth), maxColumnCount));

            if (finalColumnCount == 0)
            {
                return finalSize;
            }

            for (int i = 0; i < childrenCount; i++)
            {
                maxRowHeight = Math.Max(maxRowHeight, children[i].DesiredSize.Height);
                if ((i + 1) % finalColumnCount == 0 || i == childrenCount-1)
                {
                    //Arrange the row
                    for (int j = rowStartIndex; j <= i; j++)
                    {
                        children[j].Arrange(new Rect(rowStartWidth, rowStartHeight, arrangeWidth, maxRowHeight));
                        rowStartWidth += arrangeWidth;
                    }
                    rowStartHeight += maxRowHeight;
                    maxRowHeight = 0;
                    rowStartIndex = i + 1;
                    rowStartWidth = 0;
                }
            }

            return finalSize;
        }

        #endregion Protected Overrides

        #region Data

        private RibbonGalleryCategory Category
        {
            get
            {
                return (RibbonGalleryCategory)ItemsControl.GetItemsOwner(this);
            }
        }
		
        private RibbonGallery Gallery
        {
            get
            {
                RibbonGalleryCategory category = this.Category;
                if (category != null)
                {
                    return category.RibbonGallery;
                }
                return null;
            }
        }

        // Shared column size scope can be at either the gallery or category level.
        // Gallery level is the default.
        private bool SharedColumnSizeScopeAtGalleryLevel
        {
            get
            {
                RibbonGalleryCategory category = Category;
                if (category != null &&
                    category.IsSharedColumnSizeScope)
                {
                    return false;
                }

                return true;
            }
        }

        internal double MaxColumnWidth
        {
            get { return _maxColumnWidth; }
            private set
            {
                if (_maxColumnWidth != value)
                {
                    _maxColumnWidth = value;
                    _isArrangeWidthValid = false;
                }
            }
        }

        internal double MaxRowHeight
        {
            get { return _maxRowHeight; }
        }

        // this is local value of maxColumnWidth per category
        private double _maxColumnWidth = 0, _arrangeWidth = 0.0;
        private double _maxRowHeight = 0;
#if IN_RIBBON_GALLERY
        private double _maxItemHeight = 0;
#endif
        private bool _isArrangeWidthValid = false;

        #endregion
    }
}

