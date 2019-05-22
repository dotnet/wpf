// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: RowCache caches information about Row layouts used by DocumentGrid.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

namespace MS.Internal.Documents
{
    /// <summary>
    /// RowCache computes and caches layouts of an entire document's worth of pages.
    /// </summary>
    internal class RowCache
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        /// <summary>
        /// Default RowCache constructor
        /// </summary>
        public RowCache()
        {
            //Create the List which will hold our cached data.
            _rowCache = new List<RowInfo>(_defaultRowCacheSize);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The PageCache used by this RowCache to retrieve cached Page Size information.
        /// </summary>
        /// <value></value>
        public PageCache PageCache
        {
            set
            {
                //Clear our Cache.
                _rowCache.Clear();
                _isLayoutCompleted = false;
                _isLayoutRequested = false;

                //If the old PageCache is non-null, we need to
                //remove the old event handlers before we assign the new one.
                if (_pageCache != null)
                {
                    _pageCache.PageCacheChanged -= new PageCacheChangedEventHandler(OnPageCacheChanged);
                    _pageCache.PaginationCompleted -= new EventHandler(OnPaginationCompleted);
                }

                _pageCache = value;

                //Attach our event handlers if the new content is non-null.
                if (_pageCache != null)
                {
                    _pageCache.PageCacheChanged += new PageCacheChangedEventHandler(OnPageCacheChanged);
                    _pageCache.PaginationCompleted += new EventHandler(OnPaginationCompleted);
                }
            }

            get
            {
                return _pageCache;
            }
        }

        /// <summary>
        /// The number of Rows in the current layout.
        /// </summary>
        /// <value></value>
        public int RowCount
        {
            get
            {
                return _rowCache.Count;
            }
        }

        /// <summary>
        /// The amount of space (in pixel units) to put between pages in the layout, vertically.
        /// When this value is changed, the current Row Layout will be updated to accomodate this space.
        /// </summary>
        /// <value></value>        
        public double VerticalPageSpacing
        {
            set
            {
                if (value < 0)
                {
                    value = 0;
                }

                if (value != _verticalPageSpacing)
                {
                    _verticalPageSpacing = value;
                    RecalcLayoutForScaleOrSpacing();
                }
            }

            get
            {
                return _verticalPageSpacing;
            }
        }

        /// <summary>
        /// The amount of space (in pixel units) to put between pages in the layout, horizontally.
        /// When this value is changed, the current Row Layout will be updated to accomodate this space.
        /// </summary>
        /// <value></value>
        public double HorizontalPageSpacing
        {
            set
            {
                if (value < 0)
                {
                    value = 0;
                }

                if (value != _horizontalPageSpacing)
                {
                    _horizontalPageSpacing = value;
                    RecalcLayoutForScaleOrSpacing();
                }
            }

            get
            {
                return _horizontalPageSpacing;
            }
        }

        /// <summary>
        /// The scale factor to be applied to the row layout.  When this value is changed, the
        /// current Row Layout will be updated to reflect the new scale.
        /// </summary>
        /// <value></value>        
        public double Scale
        {
            set
            {
                if (_scale != value)
                {
                    _scale = value;
                    RecalcLayoutForScaleOrSpacing();
                }
            }

            get
            {
                return _scale;
            }
        }


        /// <summary>        
        /// The Height of the currently computed document layout.
        /// </summary>
        /// <value></value>
        public double ExtentHeight
        {
            get
            {
                return _extentHeight;
            }
        }

        /// <summary>
        /// The Width of the currently computed document layout.
        /// </summary>
        /// <value></value>
        public double ExtentWidth
        {
            get
            {
                return _extentWidth;
            }
        }

        public bool HasValidLayout
        {
            get
            {
                return _hasValidLayout;
            }
        }


        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        #region Public Events

        /// <summary>
        /// Fired when the RowCache has been changed for any reason.
        /// </summary>
        public event RowCacheChangedEventHandler RowCacheChanged;

        /// <summary>
        /// Fired when a new RowLayout has been calculated.
        /// </summary>
        public event RowLayoutCompletedEventHandler RowLayoutCompleted;

        #endregion Public Events

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Returns the row at the specified index.
        /// Throws an ArgumentOutOfRange exception if the specified index is out of range.
        /// </summary>
        /// <param name="index">The index of the row to return</param>
        /// <returns>The requested row.</returns>
        public RowInfo GetRow(int index)
        {
            if (index < 0 || index > _rowCache.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return _rowCache[index];
        }

        /// <summary>
        /// Returns the row that contains the given page.
        /// Throws an exception if the given page does not exist in the current layout.
        /// </summary>
        /// <param name="pageNumber">The page number to find the row for.</param>
        /// <returns>The requested row.</returns>
        public RowInfo GetRowForPageNumber(int pageNumber)
        {
            if (pageNumber < 0 || pageNumber > LastPageInCache)
            {
                throw new ArgumentOutOfRangeException("pageNumber");
            }

            return _rowCache[GetRowIndexForPageNumber(pageNumber)];
        }

        /// <summary>
        /// Returns the index of the row that contains the given page.
        /// Throws an exception if the given page does not exist in the current layout.
        /// </summary>
        /// <param name="pageNumber">The page number to find the row index for.</param>
        /// <returns>The requested row index</returns>
        public int GetRowIndexForPageNumber(int pageNumber)
        {
            if (pageNumber < 0 || pageNumber > LastPageInCache)
            {
                throw new ArgumentOutOfRangeException("pageNumber");
            }

            //Search our cache for the row that contains the page.
            //NOTE: Future perf item:
            //This search can be re-written as a binary search, which will be O(log(N))
            //instead of O(N).
            for (int i = 0; i < _rowCache.Count; i++)
            {
                RowInfo rowInfo = _rowCache[i];
                if (pageNumber >= rowInfo.FirstPage &&
                    pageNumber < rowInfo.FirstPage + rowInfo.PageCount)
                {
                    //We found the row, return the index.
                    return i;
                }
            }

            //We didn't find it.  Something is very likely wrong with our layout.
            //We'll throw, as this is an indicator that our layout cannot be trusted.
            throw new InvalidOperationException(SR.Get(SRID.RowCachePageNotFound));
        }

        /// <summary>
        /// Returns the row that lives at the specified vertical offset.
        /// </summary>
        /// <param name="offset">The vertical offset to find the corresponding row for</param>
        /// <returns>The index of the row that lives at the offset.</returns>
        public int GetRowIndexForVerticalOffset(double offset)
        {
            if (offset < 0 || offset > ExtentHeight)
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            //If we have no rows we'll return 0 (the top of the non-existent document)
            if (_rowCache.Count == 0)
            {
                return 0;
            }

            //We round the offsets and dimensions to the nearest 1/100th 
            //of a pixel to avoid decimal roundoff errors
            //that can result from scaling operations.  
            double roundedOffset = Math.Round(offset, _findOffsetPrecision);

            //Search our cache for the Row that occupies the specified offset.
            //NOTE: Future perf item:
            //This search can be re-written as a binary search, which will be O(log(N))
            //instead of O(N).
            for (int i = 0; i < _rowCache.Count; i++)
            {
                double rowOffset = Math.Round(_rowCache[i].VerticalOffset, _findOffsetPrecision);
                double rowHeight = Math.Round(_rowCache[i].RowSize.Height, _findOffsetPrecision);
                bool rowHasZeroHeight = false;

                //Check to see if this row has zero height (or if it appears that way due to
                //decimal precision errors with very large (1.0x10^19, for example) page heights).
                if (DoubleUtil.AreClose(rowOffset, rowOffset + rowHeight))
                {
                    rowHasZeroHeight = true;
                }

                //If the current row has Zero height (or decimal precision makes it seem that way)
                //then we'll only return this page if the offset is equal to the offset of this row.
                //Note that when decimial precision issues cause rowHasZeroHeight to be set then
                //offset and rowOffset will also be "equal" if the offset points to this row.
                if (rowHasZeroHeight && DoubleUtil.AreClose(roundedOffset, rowOffset))
                {
                    return i;
                }
                //Otherwise if this row contains the passed in offset, we'll return it.                   
                else if (roundedOffset >= rowOffset &&
                          roundedOffset < rowOffset + rowHeight)
                {
                    //We check that the bottom of the row would actually be visible -- if it won't be we use the next.
                    //If this is the last row, we use it anyway.
                    if (WithinVisibleDelta((rowOffset + rowHeight), roundedOffset) ||
                        i == _rowCache.Count - 1)
                    {
                        return i;
                    }
                    else
                    {
                        //The last row was just barely not visible, so we know the next one is the one we're looking for.                     
                        return i + 1;
                    }
                }
            }

            //If the offset is equal to the height of the document then we can just return the
            //last row in the document.
            //This handles the edge case caused by the fact that each row technically begins
            //"overlapping" the last -- a document with two rows of height 500.0 actually has 
            //the first row extend from 0.0 to 499.999999...9 and the second starts at 500.0 and
            //goes to 999.999999...9.  For all intents and purposes, the rows don't actually overlap,
            //but it does mean that if the offset passed in is exactly equal to ExtentHeight then
            //our algorithm won't find the row (the last row ends at ExtentHeight-0.000000...1, not ExtentHeight.)
            if (DoubleUtil.AreClose(offset, ExtentHeight))
            {
                return _rowCache.Count - 1;
            }


            //We didn't find it.  Something is very likely wrong here, but it is not fatal.
            //We will just return the last page in the document.
            return _rowCache.Count - 1;
        }

        /// <summary>
        /// Returns the starting index and the number of rows following that occupy the specified
        /// range of vertical offsets.
        /// </summary>
        /// <param name="startOffset">The starting offset</param>
        /// <param name="endOffset">The ending offset</param>
        /// <param name="startRowIndex">Returns the first visible row.</param>
        /// <param name="rowCount">Returns the number of visible rows.</param>
        public void GetVisibleRowIndices(double startOffset, double endOffset, out int startRowIndex, out int rowCount)
        {
            startRowIndex = 0;
            rowCount = 0;

            if (endOffset < startOffset)
            {
                throw new ArgumentOutOfRangeException("endOffset");
            }

            //If the offsets we're given are out of range we'll just return now
            //because we have no rows to find at this offset.
            if (startOffset < 0 || startOffset > ExtentHeight)
            {
                return;
            }

            //If we have no rows we'll return 0 for the start and count.
            if (_rowCache.Count == 0)
            {
                return;
            }

            //Get the first row that's visible.
            startRowIndex = GetRowIndexForVerticalOffset(startOffset);
            rowCount = 1;

            //We round the offsets and dimensions to the nearest 1/100th 
            //of a pixel to avoid decimal roundoff errors
            //that can result from scaling operations.
            startOffset = Math.Round(startOffset, _findOffsetPrecision);
            endOffset = Math.Round(endOffset, _findOffsetPrecision);

            //Now we continue downward until we either reach the end of the document
            //Or find a row that's outside the end offset (or close enough to it that it's not actually visible)         
            for (int i = startRowIndex + 1; i < _rowCache.Count; i++)
            {
                double rowOffset = Math.Round(_rowCache[i].VerticalOffset, _findOffsetPrecision);

                if (rowOffset >= endOffset ||
                     !WithinVisibleDelta(endOffset, rowOffset))
                {
                    //We've found a row that isn't visible so we're done.
                    break;
                }

                //This row is visible, add it to the count.
                rowCount++;
            }
        }

        /// <summary>
        /// Recalculates the current row layout by applying the current Scale
        /// and PageSpacing to the current row layout.  It does not change the
        /// contents of the rows.
        /// </summary>
        public void RecalcLayoutForScaleOrSpacing()
        {
            //Throw execption if we have no PageCache
            if (PageCache == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.RowCacheRecalcWithNoPageCache));
            }

            //Reset the extents
            _extentWidth = 0.0;
            _extentHeight = 0.0;

            //Walk through each row and based on the pages on the row,
            //recalculate the width and height of the row.
            double currentOffset = 0.0;
            for (int i = 0; i < _rowCache.Count; i++)
            {
                //Get this row and save off the page count.
                RowInfo currentRow = _rowCache[i];
                int pageCount = currentRow.PageCount;

                //Clear the pages so we can add the new, rescaled ones.
                currentRow.ClearPages();
                currentRow.VerticalOffset = currentOffset;

                //Add each page to the row.
                for (int j = currentRow.FirstPage; j < currentRow.FirstPage + pageCount; j++)
                {
                    Size pageSize = GetScaledPageSize(j);
                    currentRow.AddPage(pageSize);
                }

                //Adjust the extent width if necessary                
                _extentWidth = Math.Max(currentRow.RowSize.Width, _extentWidth);

                currentOffset += currentRow.RowSize.Height;
                _extentHeight += currentRow.RowSize.Height;
                _rowCache[i] = currentRow;
            }

            //Fire off our RowCacheChanged event indicating that all rows have changed.
            List<RowCacheChange> changes = new List<RowCacheChange>(1);
            changes.Add(new RowCacheChange(0, _rowCache.Count));
            RowCacheChangedEventArgs args = new RowCacheChangedEventArgs(changes);
            RowCacheChanged(this, args);
        }

        /// <summary>
        /// Recalculates the row layout given a starting page, and the number of pages to lay out
        /// on each row.
        /// In the event that there aren't currently enough pages to accomplish the layout, RowCache
        /// will wait until the pages are available and then fire off a RowLayoutCompleted event.
        /// </summary>
        /// <param name="pivotPage">The page to build the rows around</param>
        /// <param name="columns">The number of columns in the "pivot row"</param>      
        public void RecalcRows(int pivotPage, int columns)
        {
            //Throw execption if we have no PageCache
            if (PageCache == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.RowCacheRecalcWithNoPageCache));
            }

            //Throw exception for illegal values
            if (pivotPage < 0 || pivotPage > PageCache.PageCount)
            {
                throw new ArgumentOutOfRangeException("pivotPage");
            }

            //Can't lay out fewer than 1 column of pages.
            if (columns < 1)
            {
                throw new ArgumentOutOfRangeException("columns");
            }

            //Store off the requested layout parameters.
            _layoutColumns = columns;
            _layoutPivotPage = pivotPage;

            //We've started a new layout, reset the valid layout flag.
            _hasValidLayout = false;

            //We can't do anything here if we haven't gotten enough pages to create the first row yet.
            //But we'll save off the specified columns & pivot page (above)
            //And as soon as we get some pages (in OnPageCacheChanged) 
            //we'll start laying them out in the requested manner.            
            if (PageCache.PageCount < _layoutColumns)
            {
                //If pagination is still happening or if we have no pages in our content
                //we need to wait until later.
                if (!PageCache.IsPaginationCompleted || PageCache.PageCount == 0)
                {
                    //Reset our LayoutComputed flag, as we've been tasked to create a new one
                    //but can't do it yet.
                    _isLayoutRequested = true;
                    _isLayoutCompleted = false;
                    return;
                }
                //If pagination has completed, we trim down the column count and continue.
                else
                {
                    //We're done paginating, but we don't have enough
                    //pages to do the requested layout.  So we'll need to trim down the column count, with a
                    //lower bound of 1 column.
                    _layoutColumns = Math.Min(_layoutColumns, PageCache.PageCount);
                    _layoutColumns = Math.Max(1, _layoutColumns);

                    //The pivot page is always the first page in this instance                    
                    _layoutPivotPage = 0;
                }
            }

            //Reset the document extents, which will be recalculated by the RecalcRows methods.
            _extentHeight = 0.0;
            _extentWidth = 0.0;

            //Now call the specific RecalcRows... method for our document type.            
            if (PageCache.DynamicPageSizes)
            {
                _pivotRowIndex = RecalcRowsForDynamicPageSizes(_layoutPivotPage, _layoutColumns);
            }
            else
            {
                _pivotRowIndex = RecalcRowsForFixedPageSizes(_layoutPivotPage, _layoutColumns);
            }

            _isLayoutCompleted = true;
            _isLayoutRequested = false;

            //Set the valid layout flag now that we're done
            _hasValidLayout = true;

            //We've computed the layout, so we'll fire off our RowLayoutCompleted event.
            //We pass along the pivotRow's Index so the listener can keep the pivot row visible.
            RowLayoutCompletedEventArgs args = new RowLayoutCompletedEventArgs(_pivotRowIndex);
            RowLayoutCompleted(this, args);

            //Fire off our RowCacheChanged event indicating that all rows have changed.
            List<RowCacheChange> changes = new List<RowCacheChange>(1);
            changes.Add(new RowCacheChange(0, _rowCache.Count));
            RowCacheChangedEventArgs args2 = new RowCacheChangedEventArgs(changes);
            RowCacheChanged(this, args2);
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------
        #region Private Properties

        /// <summary>
        /// LastPageInCache returns the last page in the current Row layout.
        /// If there is no layout, returns -1.
        /// </summary>
        private int LastPageInCache
        {
            get
            {
                //If we have no rows, then we have no pages
                //at all in the cache.
                if (_rowCache.Count == 0)
                {
                    return -1;
                }
                else
                {
                    //Get the last row in the cache and return its
                    //last page.
                    RowInfo lastRow = _rowCache[_rowCache.Count - 1];
                    return lastRow.FirstPage + lastRow.PageCount - 1;
                }
            }
        }


        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Helper method to determine whether two offsets are visibly different
        /// (whether they're more than a certain fraction of a pixel apart).
        /// </summary>
        /// <param name="offset1"></param>
        /// <param name="offset2"></param>
        private bool WithinVisibleDelta(double offset1, double offset2)
        {
            return offset1 - offset2 > _visibleDelta;
        }

        /// <summary>
        /// Recalculates the row layout given the assumption that page sizes in the document may vary from
        /// page to page.
        /// </summary>
        /// <param name="pivotPage">The page which other rows are laid out around</param>
        /// <param name="columns">The number of columns to fit on the row containing the pivot page.</param>
        /// <returns>The index of the row that contains the pivot page.</returns>
        private int RecalcRowsForDynamicPageSizes(int pivotPage, int columns)
        {
            //Throw exception for illegal values
            if (pivotPage < 0 || pivotPage >= PageCache.PageCount)
            {
                throw new ArgumentOutOfRangeException("pivotPage");
            }

            //Can't lay out fewer than 1 column of pages.
            if (columns < 1)
            {
                throw new ArgumentOutOfRangeException("columns");
            }

            //Adjust the pivot page as necessary so that the to-be-computed layout has the specified number
            //of columns on the row.
            //(If the pivot page is the last page in the document, for example, we need to move it back
            //by the column specification.)
            if (pivotPage + columns > PageCache.PageCount)
            {
                pivotPage = Math.Max(0, PageCache.PageCount - columns);
            }

            //Clear our cache, since we're calculating a new layout.
            _rowCache.Clear();

            //Calculate this row so we can get the row width information
            //we need for the other rows.
            RowInfo pivotRow = CreateFixedRow(pivotPage, columns);
            double pivotRowWidth = pivotRow.RowSize.Width;
            int pivotRowIndex = 0;

            //We work our way back up to the top of the document from the pivot
            //and recalc the rows along the way.
            //This is necessary because page sizes vary and
            //we want to guarantee that the pages that have been fit
            //on the pivot row are the ones displayed in that row.  If we were to start
            //at the top and work our way down, we might not end up with the
            //same pages on this row.

            //Store off the rows we calculate here, we’ll need them later.            
            List<RowInfo> tempRows = new List<RowInfo>(pivotPage / columns);
            int currentPage = pivotPage;
            while (currentPage > 0)
            {
                //Create our row, specifying a backwards direction
                RowInfo newRow = CreateDynamicRow(currentPage - 1, pivotRowWidth, false /* backwards */);
                currentPage = newRow.FirstPage;
                tempRows.Add(newRow);
            }

            //We’ve made it to the top.
            //Now we can calculate the offsets of each row and add them to the Row cache.                       
            for (int i = tempRows.Count - 1; i >= 0; i--)
            {
                AddRow(tempRows[i]);
            }

            //Save off the index of this row.
            pivotRowIndex = _rowCache.Count;

            //Add the pivot row (calculated earlier)            
            AddRow(pivotRow);

            //Now we continue working down from after the pivot to the end of the 
            //document.
            currentPage = pivotPage + columns;
            while (currentPage < PageCache.PageCount)
            {
                //Create our row, specifying a forward direction
                RowInfo newRow = CreateDynamicRow(currentPage, pivotRowWidth, true /*forwards */);
                currentPage += newRow.PageCount;
                AddRow(newRow);
            }

            //And we’re done.  Whew.
            return pivotRowIndex;
        }

        /// <summary>
        /// Creates a "dynamic" row -- that is, given a maximum width for the row, it will fit as
        /// many pages on said row as possible.
        /// </summary>
        /// <param name="startPage">The first page to put on this row.</param>
        /// <param name="rowWidth">The requested width of this row.</param>
        /// <param name="createForward">Whether to create this row using the next N pages or the
        /// previous N.</param>
        /// <returns></returns>
        private RowInfo CreateDynamicRow(int startPage, double rowWidth, bool createForward)
        {
            if (startPage >= PageCache.PageCount)
            {
                throw new ArgumentOutOfRangeException("startPage");
            }

            //Given a starting page for this row, and the specified
            //width for each row, figure out how many pages will fit on this row
            //and return the resulting RowInfo object.

            //Populate the struct with initial data.
            RowInfo newRow = new RowInfo();

            //Each row is guaranteed to have at least one page, even if it’s wider
            //than the allotted size, so we add it here.        
            Size pageSize = GetScaledPageSize(startPage);
            newRow.AddPage(pageSize);

            //Keep adding pages until we either:
            // - run out of pages to add
            // - run out of space in the row
            for (; ; )
            {
                if (createForward)
                {
                    //Grab the next page.
                    pageSize = GetScaledPageSize(startPage + newRow.PageCount);

                    //We’re out of pages, or out of space.
                    if (startPage + newRow.PageCount >= PageCache.PageCount ||
                        newRow.RowSize.Width + pageSize.Width > rowWidth)
                    {
                        break;
                    }
                }
                else
                {
                    //Grab the previous page.
                    pageSize = GetScaledPageSize(startPage - newRow.PageCount);

                    //We’re out of pages, or out of space.
                    if (startPage - newRow.PageCount < 0 ||
                        newRow.RowSize.Width + pageSize.Width > rowWidth)
                    {
                        break;
                    }
                }
                newRow.AddPage(pageSize);

                //If we've hit the hard upper limit for pages on a row then we're done with this row.
                if (newRow.PageCount == DocumentViewerConstants.MaximumMaxPagesAcross)
                {
                    break;
                }
            }

            if (!createForward)
            {
                newRow.FirstPage = startPage - (newRow.PageCount - 1);
            }
            else
            {
                newRow.FirstPage = startPage;
            }

            return newRow;
        }

        /// <summary>
        /// Recalculates the row for content that does not have varying page sizes.
        /// </summary>
        /// <param name="startPage">The first page on this row</param>
        /// <param name="columns">The number of columns on this row</param>
        /// <returns>The index of the row that contains the starting page.</returns>
        private int RecalcRowsForFixedPageSizes(int startPage, int columns)
        {
            //Throw exception for illegal values
            if (startPage < 0 || startPage > PageCache.PageCount)
            {
                throw new ArgumentOutOfRangeException("startPage");
            }

            //Can't lay out fewer than 1 column of pages.
            if (columns < 1)
            {
                throw new ArgumentOutOfRangeException("columns");
            }

            //Since all pages in the document have been determined to be
            //the same size, we can use a simple algorithm to create our row layout.

            //We start at the top (no need to specify a pivot)
            //and calculate the rows from there.
            //Each row will have exactly "columns" pages on it.        
            _rowCache.Clear();

            for (int i = 0; i < PageCache.PageCount; i += columns)
            {
                RowInfo newRow = CreateFixedRow(i, columns);
                AddRow(newRow);
            }

            //Find the row the start page lives on and return its index.
            return GetRowIndexForPageNumber(startPage);
        }

        /// <summary>
        /// Creates a "fixed" row -- that is, a row with a specific number of columns on it.
        /// </summary>
        /// <param name="startPage">The first page to live on this row.</param>
        /// <param name="columns">The number of columns on this row.</param>
        /// <returns></returns>
        private RowInfo CreateFixedRow(int startPage, int columns)
        {
            if (startPage >= PageCache.PageCount)
            {
                throw new ArgumentOutOfRangeException("startPage");
            }

            if (columns < 1)
            {
                throw new ArgumentOutOfRangeException("columns");
            }

            //Given a starting page for this row and the number of columns in the row
            //calculate the width & height and return the resulting RowInfo struct

            //Populate the struct with initial data
            RowInfo newRow = new RowInfo();
            newRow.FirstPage = startPage;

            //Keep adding pages until we either:
            // - run out of pages to add
            // - add the appropriate number of pages            
            for (int i = startPage; i < startPage + columns; i++)
            {
                //We’re out of pages.
                if (i > PageCache.PageCount - 1)
                    break;

                //Get the size of the page
                Size pageSize = GetScaledPageSize(i);

                //Add this page to the row
                newRow.AddPage(pageSize);
            }

            return newRow;
        }

        /// <summary>
        /// Given a range of pages, adds the pages to the existing row cache,
        /// adding new rows where necessary.
        /// </summary>
        /// <param name="startPage">The first page to add to the layout</param>
        /// <param name="count">The number of pages to add.</param>
        private RowCacheChange AddPageRange(int startPage, int count)
        {
            if (!_isLayoutCompleted)
            {
                throw new InvalidOperationException(SR.Get(SRID.RowCacheCannotModifyNonExistentLayout));
            }

            int currentPage = startPage;
            int lastPage = startPage + count;

            int startRow = 0;
            int rowCount = 0;

            //First we check to see if startPage is such that we'd end up skipping
            //pages in the document -- that is, if the last page in our layout is currently
            //10 and start is 15, we need to fill in pages 11-14 as well.
            if (startPage > LastPageInCache + 1)
            {
                currentPage = LastPageInCache + 1;
            }

            //Get the last row in the layout
            RowInfo lastRow = _rowCache[_rowCache.Count - 1];

            //Now we need to check to see if we can add any pages to this row 
            //without exceeding the current layout's pivot row width.

            //Get the size of the page to add
            Size pageSize = GetScaledPageSize(currentPage);

            //Get the pivot row
            RowInfo pivotRow = GetRow(_pivotRowIndex);

            bool lastRowUpdated = false;

            //Add new pages to the last row until we run out of pages or space.
            while (currentPage < lastPage &&
                lastRow.RowSize.Width + pageSize.Width <= pivotRow.RowSize.Width)
            {
                //Add the current page
                lastRow.AddPage(pageSize);
                currentPage++;

                //Get the size of the next page.
                pageSize = GetScaledPageSize(currentPage);

                //Note that we updated this row so we'll update the cache when we're done here.
                lastRowUpdated = true;
            }

            //If we actually made a change to the last row, then we need to update the row cache.
            if (lastRowUpdated)
            {
                startRow = _rowCache.Count - 1;
                //Update the last row
                UpdateRow(startRow, lastRow);
            }
            else
            {
                startRow = _rowCache.Count;
            }

            //Now we add more rows to the layout, if we have any pages left.
            while (currentPage < lastPage)
            {
                //Build a new row.
                RowInfo newRow = new RowInfo();
                newRow.FirstPage = currentPage;

                //Add pages until we either run out of pages or need to start a new row.               
                do
                {
                    //Get the size of the next page
                    pageSize = GetScaledPageSize(currentPage);

                    //Add it.
                    newRow.AddPage(pageSize);
                    currentPage++;
                } while (newRow.RowSize.Width + pageSize.Width <= pivotRow.RowSize.Width
                    && currentPage < lastPage);

                //Add this new row to our cache
                AddRow(newRow);
                rowCount++;
            }

            return new RowCacheChange(startRow, rowCount);
        }

        /// <summary>
        /// Adds a new row onto the end of the layout and updates the current layout
        /// measurements.
        /// </summary>
        /// <param name="newRow">The new row to add to the layout</param>
        private void AddRow(RowInfo newRow)
        {
            //If this is the first row in the document we just put it at the beginning
            if (_rowCache.Count == 0)
            {
                newRow.VerticalOffset = 0.0;

                //The width of the document is just the width of the first row.
                _extentWidth = newRow.RowSize.Width;
            }
            else
            {
                //This is not the first row, so we put it at the end of the last row.
                RowInfo lastRow = _rowCache[_rowCache.Count - 1];

                //The new row needs to be positioned at the bottom of the last row
                newRow.VerticalOffset = lastRow.VerticalOffset + lastRow.RowSize.Height;

                //Update the document's width                
                _extentWidth = Math.Max(newRow.RowSize.Width, _extentWidth);
            }

            //Update the layout
            _extentHeight += newRow.RowSize.Height;

            //Add the row.
            _rowCache.Add(newRow);
        }

        /// <summary>
        /// Given a preexisting page range, updates the dimensions of the rows containing said 
        /// pages from the Page Cache.
        /// If any row's height changes as a result, all rows below have their offsets updated.
        /// </summary>
        /// <param name="startPage">The first page changed</param>
        /// <param name="count">The number of pages changed</param>
        private RowCacheChange UpdatePageRange(int startPage, int count)
        {
            if (!_isLayoutCompleted)
            {
                throw new InvalidOperationException(SR.Get(SRID.RowCacheCannotModifyNonExistentLayout));
            }

            //Get the row that contains the first page
            int startRowIndex = GetRowIndexForPageNumber(startPage);
            int rowIndex = startRowIndex;

            int currentPage = startPage;

            //Recalculate the rows affected by the changed pages.
            while (currentPage < startPage + count && rowIndex < _rowCache.Count)
            {
                //Get the current row
                RowInfo currentRow = _rowCache[rowIndex];
                //Create a new row and copy pertinent data
                //from the old one.
                RowInfo updatedRow = new RowInfo();
                updatedRow.VerticalOffset = currentRow.VerticalOffset;
                updatedRow.FirstPage = currentRow.FirstPage;

                //Now rebuild this row, thus recalculating the row's size
                //based on the new page sizes.
                for (int i = currentRow.FirstPage; i < currentRow.FirstPage + currentRow.PageCount; i++)
                {
                    //Get the updated page size and add it to our updated row
                    Size pageSize = GetScaledPageSize(i);
                    updatedRow.AddPage(pageSize);
                }

                //Update the row layout with this new row.
                UpdateRow(rowIndex, updatedRow);

                //Move to the next group of pages.
                currentPage = updatedRow.FirstPage + updatedRow.PageCount;

                //Move to the next row.
                rowIndex++;
            }

            return new RowCacheChange(startRowIndex, rowIndex - startRowIndex);
        }

        /// <summary>
        /// Updates the cache entry at the given index with the new RowInfo supplied.
        /// If the new row has a different height than the old one, we need to update
        /// the offsets of all the rows below this one and adjust the vertical extent appropriately.
        /// If it has a different width, we just need to update the horizontal extent appropriately.
        /// </summary>
        /// <param name="index">The index of the row to update</param>
        /// <param name="newRow">The new RowInfo to replace the old</param>
        private void UpdateRow(int index, RowInfo newRow)
        {
            if (!_isLayoutCompleted)
            {
                throw new InvalidOperationException(SR.Get(SRID.RowCacheCannotModifyNonExistentLayout));
            }

            //Check for invalid indices.  If it's out of range then we just return.
            if (index > _rowCache.Count)
            {
                Debug.Assert(false, "Requested to update a non-existent row.");
                return;
            }

            //Get the current entry.
            RowInfo oldRowInfo = _rowCache[index];

            //Replace the old with the new.
            _rowCache[index] = newRow;

            //Compare the heights -- if they differ we need to update the rows beneath it.
            if (oldRowInfo.RowSize.Height != newRow.RowSize.Height)
            {
                //The new row has a different height, so we add the delta
                //between the old and the new to every row below this one.
                double delta = newRow.RowSize.Height - oldRowInfo.RowSize.Height;
                for (int i = index + 1; i < _rowCache.Count; i++)
                {
                    RowInfo row = _rowCache[i];
                    row.VerticalOffset += delta;
                    _rowCache[i] = row;
                }

                //Add the delta for this row to our vertical extent.
                _extentHeight += delta;
            }

            //If the new row is wider than the current document's width, then we
            //can just update the document width now.
            if (newRow.RowSize.Width > _extentWidth)
            {
                _extentWidth = newRow.RowSize.Width;
            }
            //Otherwise, if the widths differ we need to recalculate the width 
            //of the document again.
            //(The logic is that this particular row could have defined the extent width, 
            //and now that it's changed size the extent may change as well.)
            else if (oldRowInfo.RowSize.Width != newRow.RowSize.Width)
            {
                //The width of this row has changed.
                //This means we need to recalculate the width of the entire document
                //by walking through each row.
                _extentWidth = 0;
                for (int i = 0; i < _rowCache.Count; i++)
                {
                    RowInfo row = _rowCache[i];
                    //Update the extent width.
                    _extentWidth = Math.Max(row.RowSize.Width, _extentWidth);
                }
            }
        }

        /// <summary>
        /// Trims any pages (and rows) from the end of the layout, starting with the specified page.
        /// </summary>
        /// <param name="startPage">The first page to remove.</param>
        private RowCacheChange TrimPageRange(int startPage)
        {
            //First, we find the row the last page is on.
            int rowIndex = GetRowIndexForPageNumber(startPage);

            //Now we replace this row with a new row that has the deleted pages
            //removed, if there are pages on this row that aren't going to be deleted.
            RowInfo oldRow = GetRow(rowIndex);

            if (oldRow.FirstPage < startPage)
            {
                RowInfo updatedRow = new RowInfo();
                updatedRow.VerticalOffset = oldRow.VerticalOffset;
                updatedRow.FirstPage = oldRow.FirstPage;

                for (int i = oldRow.FirstPage; i < startPage; i++)
                {
                    //Get the page we're interested in.
                    Size pageSize = GetScaledPageSize(i);
                    //Add it.
                    updatedRow.AddPage(pageSize);
                }

                UpdateRow(rowIndex, updatedRow);

                //Increment the rowIndex, since we're going to keep this row.
                rowIndex++;
            }

            int removeCount = _rowCache.Count - rowIndex;

            //Now remove all the rows below this one.
            if (rowIndex < _rowCache.Count)
            {
                _rowCache.RemoveRange(rowIndex, removeCount);
            }

            //Update our extents
            _extentHeight = oldRow.VerticalOffset;

            return new RowCacheChange(rowIndex, removeCount);
        }

        /// <summary>
        /// Helper function that returns the dimensions of a page 
        /// with the current Scale factor and Spacing applied.
        /// </summary>
        /// <param name="pageNumber">The page to retrieve page size info about.</param>
        /// <returns>The padded and scaled size of the given page.</returns>
        private Size GetScaledPageSize(int pageNumber)
        {
            //GetPageSize will return (0,0) for out-of-range pages.
            Size pageSize = PageCache.GetPageSize(pageNumber);

            if (pageSize.IsEmpty)
            {
                pageSize = new Size(0, 0);
            }

            pageSize.Width *= Scale;
            pageSize.Height *= Scale;
            pageSize.Width += HorizontalPageSpacing;
            pageSize.Height += VerticalPageSpacing;

            return pageSize;
        }

        /// <summary>
        /// Event Handler for the PageCacheChanged event.  Called whenever an entry in the
        /// PageCache is changed.  When this happens, the RowCache needs to Add, Remove, or
        /// Update row entries corresponding to the pages changed.
        /// </summary>
        /// <param name="sender">The object that sent this event.</param>
        /// <param name="args">The associated arguments.</param>
        private void OnPageCacheChanged(object sender, PageCacheChangedEventArgs args)
        {
            //If we have a computed layout then we'll add/remove/frob rows to conform
            //to that layout.            
            if (_isLayoutCompleted)
            {
                List<RowCacheChange> changes = new List<RowCacheChange>(args.Changes.Count);
                for (int i = 0; i < args.Changes.Count; i++)
                {
                    PageCacheChange pageChange = args.Changes[i];
                    switch (pageChange.Type)
                    {
                        case PageCacheChangeType.Add:
                        case PageCacheChangeType.Update:
                            if (pageChange.Start > LastPageInCache)
                            {
                                //Completely new pages, so we add new cache entries
                                RowCacheChange change = AddPageRange(pageChange.Start, pageChange.Count);
                                if (change != null)
                                {
                                    changes.Add(change);
                                }
                            }
                            else
                            {
                                if (pageChange.Start + pageChange.Count - 1 <= LastPageInCache)
                                {
                                    //All pre-existing pages, so we just update the current cache entries.
                                    RowCacheChange change = UpdatePageRange(pageChange.Start, pageChange.Count);
                                    if (change != null)
                                    {
                                        changes.Add(change);
                                    }
                                }
                                else
                                {
                                    //Some pre-existing pages, some new.                        
                                    RowCacheChange change;
                                    change = UpdatePageRange(pageChange.Start, LastPageInCache - pageChange.Start);
                                    if (change != null)
                                    {
                                        changes.Add(change);
                                    }
                                    change = AddPageRange(LastPageInCache + 1, pageChange.Count - (LastPageInCache - pageChange.Start));
                                    if (change != null)
                                    {
                                        changes.Add(change);
                                    }
                                }
                            }
                            break;

                        case PageCacheChangeType.Remove:
                            //If PageCount is now less than the size of our cache due to repagination
                            //we remove the corresponding entries from our row cache.
                            if (PageCache.PageCount - 1 < LastPageInCache)
                            {
                                //Remove pages starting at the first no-longer-existent page.
                                //(PageCache.PageCount now points to the first dead page)
                                RowCacheChange change = TrimPageRange(PageCache.PageCount);
                                if (change != null)
                                {
                                    changes.Add(change);
                                }
                            }

                            //If because of the above trimming we have fewer pages left 
                            //in the document than the columns that were initially requested
                            //We'll need to recalc our layout from scratch.
                            //First we check to see if we have one or fewer rows left.
                            if (_rowCache.Count <= 1)
                            {
                                //If we have either no rows left or the remaining row has
                                //less than _layoutColumns pages on it, we need to recalc from the first page.
                                if (_rowCache.Count == 0 || _rowCache[0].PageCount < _layoutColumns)
                                {
                                    RecalcRows(0, _layoutColumns);
                                }
                            }
                            break;

                        default:
                            throw new ArgumentOutOfRangeException("args");
                    }
                }

                RowCacheChangedEventArgs newArgs = new RowCacheChangedEventArgs(changes);
                RowCacheChanged(this, newArgs);
            }
            else if (_isLayoutRequested)
            {
                //We've had a request to create a layout previously, but didn't have enough pages to do so before.
                //Try it now.
                RecalcRows(_layoutPivotPage, _layoutColumns);
            }
        }

        /// <summary>
        /// Handler for the OnPaginationCompleted event.  If we still have an unfulfilled
        /// layout request, we'll call RecalcRows to ensure that we get a layout (though possibly
        /// with fewer columns than requested.)
        /// </summary>
        /// <param name="sender">The sender of this event</param>
        /// <param name="args">The arguments associated with this event</param>
        private void OnPaginationCompleted(object sender, EventArgs args)
        {
            if (_isLayoutRequested)
            {
                //We've had a request to create a layout previously, but we don't have enough
                //pages to do it.  We'll call RecalcRows here which will now properly trim down
                //the column count.                
                RecalcRows(_layoutPivotPage, _layoutColumns);
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        //The List<T> that contains our row cache.
        private List<RowInfo> _rowCache;

        //Default Row Layout parameters:                
        private int _layoutPivotPage;
        private int _layoutColumns;

        //The index of the pivot row
        private int _pivotRowIndex;

        //Reference to our PageCache
        private PageCache _pageCache;

        //Flag indicating if we've been asked for a row layout.
        private bool _isLayoutRequested;
        private bool _isLayoutCompleted;

        //Data for CLR properties.
        private double _verticalPageSpacing;
        private double _horizontalPageSpacing;
        private double _scale = 1.0;
        private double _extentHeight;
        private double _extentWidth;
        private bool _hasValidLayout;

        private readonly int _defaultRowCacheSize = 32;

        //This is the number of digits of precision we round to when searching for Rows given an offset.
        //We do this to avoid returning extraneous pages which are "visible" only by a few hundredths of
        //a pixel (i.e. not really visible).
        private readonly int _findOffsetPrecision = 2;

        //This is the fraction of a pixel of overlap required for a given offset to be considered "visible."
        private readonly double _visibleDelta = 0.5;
    }

    /// <summary>
    /// The RowInfo class represents a single row in the document
    /// layout and contains the necessary data to compute the location
    /// and render a given row of pages.
    /// </summary>
    internal class RowInfo
    {
        /// <summary>
        /// Constructor for a RowInfo object.
        /// </summary>
        public RowInfo()
        {
            //Create our RowSize, which is (0,0) by default.
            _rowSize = new Size(0, 0);
        }

        /// <summary>
        /// Adds a page to this Row.
        /// Increments the PageCount and updates the RowSize.
        /// </summary>
        /// <param name="pageSize"></param>
        public void AddPage(Size pageSize)
        {
            //Add the page to this row.
            _pageCount++;

            //Update the row's dimensions
            _rowSize.Width += pageSize.Width;
            _rowSize.Height = Math.Max(pageSize.Height, _rowSize.Height);
        }

        /// <summary>
        /// Removes all pages from this Row.
        /// </summary>
        public void ClearPages()
        {
            _pageCount = 0;
            _rowSize.Width = 0.0;
            _rowSize.Height = 0.0;
        }

        /// <summary>
        /// The dimensions of this row.
        /// </summary>
        public Size RowSize
        {
            get
            {
                return _rowSize;
            }
        }

        /// <summary>
        /// The offset at which this row appears in the document
        /// </summary>
        public double VerticalOffset
        {
            get
            {
                return _verticalOffset;
            }

            set
            {
                _verticalOffset = value;
            }
        }

        /// <summary>
        /// The first page on this row.
        /// </summary>
        public int FirstPage
        {
            get
            {
                return _firstPage;
            }

            set
            {
                _firstPage = value;
            }
        }

        /// <summary>
        /// The number of pages on this row.
        /// </summary>
        public int PageCount
        {
            get
            {
                return _pageCount;
            }
        }

        private Size _rowSize;
        private double _verticalOffset;
        private int _firstPage;
        private int _pageCount;
    }

    /// <summary>
    ///RowCacheChanged event handler.
    /// </summary>
    internal delegate void RowCacheChangedEventHandler(object sender, RowCacheChangedEventArgs e);

    /// <summary>
    ///RowLayoutCompleted event handler.
    /// </summary>
    internal delegate void RowLayoutCompletedEventHandler(object sender, RowLayoutCompletedEventArgs e);

    /// <summary>
    /// Event arguments for the RowLayoutCompleted event.
    /// </summary>
    internal class RowLayoutCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        ///<param name="pivotRowIndex">The index of the row to keep visible (the pivot row)</param>
        public RowLayoutCompletedEventArgs(int pivotRowIndex)
        {
            _pivotRowIndex = pivotRowIndex;
        }

        /// <summary>
        /// The index of the row to be kept visible by DocumentGrid.
        /// </summary>
        public int PivotRowIndex
        {
            get
            {
                return _pivotRowIndex;
            }
        }

        private readonly int _pivotRowIndex;
    }

    /// <summary>
    /// Event arguments for the RowCacheChanged event.
    /// </summary>
    internal class RowCacheChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="changes">The changes corresponding to this event</param>
        public RowCacheChangedEventArgs(List<RowCacheChange> changes)
        {
            _changes = changes;
        }

        /// <summary>
        /// The changes corresponding to this event.
        /// </summary>
        public List<RowCacheChange> Changes
        {
            get
            {
                return _changes;
            }
        }

        private readonly List<RowCacheChange> _changes;
    }

    /// <summary>
    /// Represents a single change to the RowCache
    /// </summary>
    internal class RowCacheChange
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="start">The first row changed</param>
        /// <param name="count">The number of rows changed</param>
        public RowCacheChange(int start, int count)
        {
            _start = start;
            _count = count;
        }

        /// <summary>
        /// Zero-based page number for this first row that has changed.
        /// </summary>
        public int Start
        {
            get
            {
                return _start;
            }
        }

        /// <summary>
        /// Number of continuous rows changed.
        /// </summary>
        public int Count
        {
            get
            {
                return _count;
            }
        }

        private readonly int _start;
        private readonly int _count;
    }
}
