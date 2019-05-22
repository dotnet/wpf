// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: DocumentGrid displays DocumentPaginator content in a grid-like
//              arrangement and is used by DocumentViewer to display documents.
//


using MS.Internal;
using MS.Internal.Media;
using MS.Utility;
using MS.Win32;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MS.Internal.Documents
{
    /// <summary>
    /// DocumentGrid is an internal Avalon FrameworkElement that executes all the
    /// "heavy lifting" involved in loading and displaying an DocumentPaginator-based
    /// document inside of a DocumentViewer control.
    /// </summary>
    /// <speclink>http://d2/DRX/default.aspx</speclink>
    internal class DocumentGrid : FrameworkElement, IDocumentScrollInfo
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        #region Constructors

        /// <summary>
        /// Static constructor
        /// </summary>
        static DocumentGrid()
        {
            //Register for the RequestBringIntoView event so we can get BIV events for
            //TextEditor IP movements.
            EventManager.RegisterClassHandler(typeof(DocumentGrid),
                RequestBringIntoViewEvent,
                new RequestBringIntoViewEventHandler(OnRequestBringIntoView));
            //Register the default ContextMenu
            DocumentGridContextMenu.RegisterClassHandler();
        }

        /// <summary>
        /// The constructor
        /// </summary>
        public DocumentGrid() : base()
        {
            Initialize();
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        #region Internal Methods
        /// <summary>
        /// Hit-Test on the multi-page UI scope to return a DocumentPage
        /// that contains this point
        /// </summary>
        /// <param name="point">Point in pixel unit, relative to the UI Scope's coordinates</param>
        /// <returns>A DocumentPage that is hit or null if no page is hit</returns>
        internal DocumentPage GetDocumentPageFromPoint(Point point)
        {
            DocumentPageView dp = GetDocumentPageViewFromPoint(point);

            // if we hit a DocumentPageView we can return its DocumentPage.
            if (dp != null)
            {
                return dp.DocumentPage;
            }

            //Nothing hit, return null.
            return null;
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Public Interfaces
        //
        //------------------------------------------------------
        #region Interface Implementations


        #region IDocumentScrollInfo
        //------------------------------------------------------
        //
        //  IDocumentScrollInfo Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Scroll content by one line to the top.
        /// </summary>
        public void LineUp()
        {
            if (_canVerticallyScroll)
            {
                SetVerticalOffsetInternal(VerticalOffset - _verticalLineScrollAmount);
            }
        }

        /// <summary>
        /// Scroll content by one line to the bottom.
        /// </summary>
        public void LineDown()
        {
            if (_canVerticallyScroll)
            {
                SetVerticalOffsetInternal(VerticalOffset + _verticalLineScrollAmount);

                //Perf Tracing - Mark LineDown Start
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXLineDown);
            }
        }

        /// <summary>
        /// Scroll content by one line to the left.
        /// </summary>
        public void LineLeft()
        {
            if (_canHorizontallyScroll)
            {
                SetHorizontalOffsetInternal(HorizontalOffset - _horizontalLineScrollAmount);
            }
        }

        /// <summary>
        /// Scroll content by one line to the right.
        /// </summary>
        public void LineRight()
        {
            if (_canHorizontallyScroll)
            {
                SetHorizontalOffsetInternal(HorizontalOffset + _horizontalLineScrollAmount);
            }
        }

        /// <summary>
        /// Scroll content by one viewport to the top.
        /// </summary>
        public void PageUp()
        {
            SetVerticalOffsetInternal(VerticalOffset - ViewportHeight);
        }

        /// <summary>
        /// Scroll content by one viewport to the bottom.
        /// </summary>
        public void PageDown()
        {
            SetVerticalOffsetInternal(VerticalOffset + ViewportHeight);

            //Perf Tracing - Mark PageDown Start
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXPageDown, (int)VerticalOffset);
        }

        /// <summary>
        /// Scroll content by one viewport to the left.
        /// </summary>
        public void PageLeft()
        {
            SetHorizontalOffsetInternal(HorizontalOffset - ViewportWidth);
        }

        /// <summary>
        /// Scroll content by one viewport to the right.
        /// </summary>
        public void PageRight()
        {
            SetHorizontalOffsetInternal(HorizontalOffset + ViewportWidth);
        }

        /// <summary>
        /// Scroll content up via the mousewheel.
        /// </summary>
        public void MouseWheelUp()
        {
            if (CanMouseWheelVerticallyScroll)
            {
                SetVerticalOffsetInternal(VerticalOffset - MouseWheelVerticalScrollAmount);
            }
            else
            {
                PageUp();
            }
        }

        /// <summary>
        /// Scroll content down via the mousewheel.
        /// </summary>
        public void MouseWheelDown()
        {
            if (CanMouseWheelVerticallyScroll)
            {
                SetVerticalOffsetInternal(VerticalOffset + MouseWheelVerticalScrollAmount);
            }
            else
            {
                PageDown();
            }
        }

        /// <summary>
        /// Scroll content left via the mousewheel.
        /// </summary>
        public void MouseWheelLeft()
        {
            if (CanMouseWheelHorizontallyScroll)
            {
                SetHorizontalOffsetInternal(HorizontalOffset - MouseWheelHorizontalScrollAmount);
            }
            else
            {
                PageLeft();
            }
        }

        /// <summary>
        /// Scroll content right via the mousewheel.
        /// </summary>
        public void MouseWheelRight()
        {
            if (CanMouseWheelHorizontallyScroll)
            {
                SetHorizontalOffsetInternal(HorizontalOffset + MouseWheelHorizontalScrollAmount);
            }
            else
            {
                PageRight();
            }
        }

        /// <summary>
        /// Ensures that the specified visual is made visible.
        /// </summary>
        /// <returns>
        /// A rectangle in the IScrollInfo's coordinate space that has been made visible.
        /// Other ancestors to in turn make this new rectangle visible.
        /// The rectangle should generally be a transformed version of the input rectangle.  In some cases, like
        /// when the input rectangle cannot entirely fit in the viewport, the return value might be smaller.
        /// </returns>
        public Rect MakeVisible(Visual v, Rect r)
        {
            if (Content != null && v != null)
            {
                ContentPosition cp = Content.GetObjectPosition(v);
                MakeContentPositionVisibleAsync(new MakeVisibleData(v, cp, r));
            }

            return r;
        }

        /// <summary>
        /// Ensures that the specified object is made visible, given that the page it lives on is already known.
        /// </summary>
        /// <returns>
        /// A rectangle in the IScrollInfo's coordinate space that has been made visible.
        /// Other ancestors to in turn make this new rectangle visible.
        /// The rectangle should generally be a transformed version of the input rectangle.  In some cases, like
        /// when the input rectangle cannot entirely fit in the viewport, the return value might be smaller.
        /// </returns>
        public Rect MakeVisible(object o, Rect r, int pageNumber)
        {
            ContentPosition cp = Content.GetObjectPosition(o);
            MakeVisibleAsync(new MakeVisibleData(o as Visual, cp, r), pageNumber);
            return r;
        }

        /// <summary>
        /// Scrolls the current selection into view.  Requests for empty or
        /// invalid selections will do nothing.
        /// </summary>
        public void MakeSelectionVisible()
        {
            //We can only continue if we have a TextEditor attached...
            if (TextEditor != null && TextEditor.Selection != null)
            {
                //Get the TextPointer for the start of our selection.
                ITextPointer tp = TextEditor.Selection.Start;

                //Ensure that the TextPointer we use has gravity set to forwards (or into
                //the selection) so that the selected text is always displayed.
                tp = tp.CreatePointer(LogicalDirection.Forward);

                //If the TextPointer is also a ContentPosition, we can
                //make that ContentPosition visible.
                ContentPosition cp = tp as ContentPosition;
                MakeContentPositionVisibleAsync(new MakeVisibleData(null, cp, Rect.Empty));
            }
        }

        /// <summary>
        /// Scrolls the requested page into view.
        /// </summary>
        /// <param name="pageNumber">The page to make visible.</param>
        public void MakePageVisible(int pageNumber)
        {
            //If we're moving more than one page then this is a "page jump"
            //and we should log the perf event.
            if (Math.Abs(pageNumber - _firstVisiblePageNumber) > 1)
            {
                //Perf Tracing - Mark Page Jump Start
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXPageJump, _firstVisiblePageNumber, pageNumber);
            }

            //Clip the offset into range for out-of-range page numbers
            if (pageNumber < 0)
            {
                //Clip to the top-left of the document
                SetVerticalOffsetInternal(0.0d);
                SetHorizontalOffsetInternal(0.0d);
            }
            else if (pageNumber >= _pageCache.PageCount || _rowCache.RowCount == 0)
            {
                //If the doc is done loading, then this page is out of range.
                if (_pageCache.IsPaginationCompleted && _rowCache.HasValidLayout)
                {
                    //Clip to the bottom-right of the document
                    SetVerticalOffsetInternal(ExtentHeight);
                    SetHorizontalOffsetInternal(ExtentWidth);
                }
                else
                {
                    //The doc is not done loading.
                    //Wait for the page to be laid out and try again.
                    _pageJumpAfterLayout = true;
                    _pageJumpAfterLayoutPageNumber = pageNumber;
                }
            }
            else
            {
                //This page is valid, so scroll to it now.
                RowInfo scrolledRow = _rowCache.GetRowForPageNumber(pageNumber);
                SetVerticalOffsetInternal(scrolledRow.VerticalOffset);

                //Calculate the Horizontal offset of the page we're bringing into view:
                double horizontalOffset = GetHorizontalOffsetForPage(scrolledRow, pageNumber);
                SetHorizontalOffsetInternal(horizontalOffset);
            }
        }

        /// <summary>
        /// Scrolls the next row of pages into view.  This differs from
        /// IScrollInfo?s "PageDown" in that PageDown pages by Viewports
        /// which may not coincide with page dimensions, whereas
        /// ScrollToNextRow takes these dimensions into account so that
        /// precisely the next row of pages is displayed.
        /// </summary>
        public void ScrollToNextRow()
        {
            //We change our vertical offset to be the offset of the next row (if there is one).
            //If there isn't, we do nothing.
            int nextRow = _firstVisibleRow + 1;

            if (nextRow < _rowCache.RowCount)
            {
                //Get the next row.
                RowInfo row = _rowCache.GetRow(nextRow);
                SetVerticalOffsetInternal(row.VerticalOffset);
            }
        }

        /// Scrolls the previous row of pages into view.  This differs from
        /// IScrollInfo?s "PageUp" in that PageUp pages by Viewports
        /// which may not coincide with page dimensions, whereas
        /// ScrollToPreviousRow takes these dimensions into account so that
        /// precisely the previously row of pages is displayed.
        public void ScrollToPreviousRow()
        {
            //We change our vertical offset to be the offset of the previous row (if there is one).
            int previousRow = _firstVisibleRow - 1;

            if (previousRow >= 0 && previousRow < _rowCache.RowCount)
            {
                //Get the previous row.
                RowInfo row = _rowCache.GetRow(previousRow);
                SetVerticalOffsetInternal(row.VerticalOffset);
            }
        }

        /// <summary>
        /// Scrolls to the top of the document.
        /// </summary>
        public void ScrollToHome()
        {
            //We just set the VerticalOffset to 0.
            SetVerticalOffsetInternal(0);
        }

        /// <summary>
        /// Scrolls to the bottom of the document.
        /// </summary>
        public void ScrollToEnd()
        {
            //We just set the VerticalOffset to our document's extent.
            SetVerticalOffsetInternal(ExtentHeight);
        }

        /// <summary>
        /// Sets the scale factor applied to pages in the document, while
        /// keeping the "Active Focus" centered.
        /// </summary>
        /// <param name="scale"></param>
        public void SetScale(double scale)
        {
            if (!DoubleUtil.AreClose(scale, Scale))
            {
                if (scale <= 0.0)
                {
                    throw new ArgumentOutOfRangeException("scale");
                }

                if (!Helper.IsDoubleValid(scale))
                {
                    throw new ArgumentOutOfRangeException("scale");
                }

                QueueSetScale(scale);
            }
        }

        /// <summary>
        /// Changes the view to the specified number of columns.
        /// </summary>
        /// <param name="columns"></param>
        public void SetColumns(int columns)
        {
            if (columns < 1)
            {
                throw new ArgumentOutOfRangeException("columns");
            }

            //Perf Tracing - Mark Layout Change Start
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXLayoutBegin);

            QueueUpdateDocumentLayout(new DocumentLayout(columns, ViewMode.SetColumns));
        }

        /// <summary>
        /// Changes the view to the specified number of columns.
        /// </summary>
        /// <param name="columns"></param>
        public void FitColumns(int columns)
        {
            if (columns < 1)
            {
                throw new ArgumentOutOfRangeException("columns");
            }

            //Perf Tracing - Mark Layout Change Start
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXLayoutBegin);

            QueueUpdateDocumentLayout(new DocumentLayout(columns, ViewMode.FitColumns));
        }

        /// <summary>
        /// Changes the view to a single page, scaled such that it is as wide as the Viewport.
        /// </summary>
        public void FitToPageWidth()
        {
            //Perf Tracing - Mark Layout Change Start
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXLayoutBegin);

            QueueUpdateDocumentLayout(
                new DocumentLayout(1 /* one column */, ViewMode.PageWidth));
        }

        /// <summary>
        /// Changes the view to a single page, scaled such that it is as tall as the Viewport.
        /// </summary>
        public void FitToPageHeight()
        {
            //Perf Tracing - Mark Layout Change Start
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXLayoutBegin);

            QueueUpdateDocumentLayout(
                new DocumentLayout(1 /* one column */, ViewMode.PageHeight));
        }

        /// <summary>
        /// Changes the view to ?thumbnail view? which will scale the document
        /// such that as many pages are visible at once as is possible.
        /// </summary>
        public void ViewThumbnails()
        {
            //Perf Tracing - Mark Layout Change Start
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXLayoutBegin);

            QueueUpdateDocumentLayout(
                new DocumentLayout(1 /* one column, arbitrary */, ViewMode.Thumbnails));
        }

        //------------------------------------------------------
        //
        //  IDocumentScrollInfo Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// DocumentGrid always scrolls in both dimensions.
        /// </summary>
        public bool CanHorizontallyScroll
        {
            get { return _canHorizontallyScroll; }
            set { _canHorizontallyScroll = value; }
        }

        /// <summary>
        /// DocumentGrid always scrolls in both dimensions.
        /// </summary>
        public bool CanVerticallyScroll
        {
            get { return _canVerticallyScroll; }
            set { _canVerticallyScroll = value; }
        }

        /// <summary>
        /// ExtentWidth contains the full horizontal range of the scrolled content.
        /// </summary>
        public double ExtentWidth
        {
            get
            {
                return _rowCache.ExtentWidth;
            }
        }

        /// <summary>
        /// ExtentHeight contains the full vertical range of the scrolled content.
        /// </summary>
        public double ExtentHeight
        {
            get
            {
                return _rowCache.ExtentHeight;
            }
        }

        /// <summary>
        /// ViewportWidth contains the currently visible horizontal range of the scrolled content.
        /// </summary>
        public double ViewportWidth
        {
            get
            {
                return _viewportWidth;
            }
        }

        /// <summary>
        /// ViewportHeight contains the currently visible vertical range of the scrolled content.
        /// </summary>
        public double ViewportHeight
        {
            get
            {
                return _viewportHeight;
            }
        }

        /// <summary>
        /// HorizontalOffset is the horizontal offset into the scrolled content that represents the first unit visible.
        /// Valid values are inclusively between 0 and <see cref="ExtentWidth" /> less <see cref="ViewportWidth" />.
        /// </summary>
        public double HorizontalOffset
        {
            get
            {
                //Clip HorizontalOffset into range.
                double clippedHorizontalOffset = Math.Min(_horizontalOffset, ExtentWidth - ViewportWidth);
                clippedHorizontalOffset = Math.Max(clippedHorizontalOffset, 0.0);

                return clippedHorizontalOffset;
            }
        }

        /// <summary>
        /// Set the HorizontalOffset.  If there are pending layout delegates, then
        /// this will be processed by a delegate.
        /// </summary>
        /// <param name="offset"></param>
        public void SetHorizontalOffset(double offset)
        {
            if (!DoubleUtil.AreClose(_horizontalOffset, offset))
            {
                if (Double.IsNaN(offset))
                {
                    throw new ArgumentOutOfRangeException("offset");
                }

                // If there aren't any pending document layout delegates, then change
                // the HorizontalOffset immediately, otherwise schedule a delegate for it.
                if (_documentLayoutsPending == 0)
                {
                    SetHorizontalOffsetInternal(offset);
                }
                else
                {
                    QueueUpdateDocumentLayout(new DocumentLayout(offset, ViewMode.SetHorizontalOffset));
                }
            }
        }

        /// <summary>
        /// VerticalOffset is the vertical offset into the scrolled content that represents the first unit visible.
        /// Valid values are inclusively between 0 and <see cref="ExtentHeight" /> less <see cref="ViewportHeight" />.
        /// </summary>
        public double VerticalOffset
        {
            get
            {
                //Clip VerticalOffset into range.
                double clippedVerticalOffset = Math.Min(_verticalOffset, ExtentHeight - ViewportHeight);
                clippedVerticalOffset = Math.Max(clippedVerticalOffset, 0.0);

                return clippedVerticalOffset;
            }
        }

        /// <summary>
        /// Set the VerticalOffset.  If there are pending layout delegates, then
        /// this will be processed by a delegate.
        /// </summary>
        /// <param name="offset"></param>
        public void SetVerticalOffset(double offset)
        {
            if (!DoubleUtil.AreClose(_verticalOffset, offset))
            {
                if (Double.IsNaN(offset))
                {
                    throw new ArgumentOutOfRangeException("offset");
                }

                // If there aren't any pending document layout delegates, then change
                // the VerticalOffset immediately, otherwise schedule a delegate for it.
                if (_documentLayoutsPending == 0)
                {
                    SetVerticalOffsetInternal(offset);
                }
                else
                {
                    QueueUpdateDocumentLayout(new DocumentLayout(offset, ViewMode.SetVerticalOffset));
                }
            }
        }

        /// <summary>
        /// Provides the IDocumentScrollInfo implementer with a content
        /// tree to be paginated.  Developers are free to modify this
        /// Content at any time (remove, add, modify pages, etc?)
        /// and the IDocumentScrollInfo implementer is responsible for
        /// noting the changes and updating as necessary.
        /// </summary>
        /// <value>The DocumentPaginator to be assigned as the content</value>
        public DynamicDocumentPaginator Content
        {
            get
            {
                //_pageCache is guaranteed to be non-null as it's created in the
                //Constructor.
                return _pageCache.Content;
            }
            set
            {
                //_pageCache is guaranteed to be non-null as it's created in the
                //Constructor.
                if (value != _pageCache.Content)
                {
                    //Null out our TextContainer.  It will be created as needed.
                    _textContainer = null;

                    //Remove our old events from the content
                    if (_pageCache.Content != null)
                    {
                        _pageCache.Content.GetPageNumberCompleted -= new GetPageNumberCompletedEventHandler(OnGetPageNumberCompleted);
                    }

                    //Remove our ScrollChanged events from our ScrollViewer
                    if (ScrollOwner != null)
                    {
                        ScrollOwner.ScrollChanged -= new ScrollChangedEventHandler(OnScrollChanged);
                        _scrollChangedEventAttached = false;
                    }

                    //Assign the new content
                    _pageCache.Content = value;

                    if (_pageCache.Content != null)
                    {
                        //Add our new events to the content
                        _pageCache.Content.GetPageNumberCompleted += new GetPageNumberCompletedEventHandler(OnGetPageNumberCompleted);
                    }

                    //Clear out our visual collection so that the old pages (pointing to old content)
                    //will be replaced with new ones on the next Measure/Arrange pass.
                    ResetVisualTree(false /*pruneOnly*/);
                    ResetPageViewCollection();

                    //Reset our visible pages.
                    _firstVisiblePageNumber = 0;
                    _lastVisiblePageNumber = 0;

                    // Perf Tracing - PageVisible Changed
                    EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXPageVisible, _firstVisiblePageNumber, _lastVisiblePageNumber);

                    _lastRowChangeExtentWidth = 0.0;
                    _lastRowChangeVerticalOffset = 0.0;

                    //Cause the new content to be laid out in the same fashion as
                    //the previous content.
                    //If the view is Thumbnails we'll change it to SetColumns
                    //so that the Column count will be maintained.  This is done
                    //because we're getting new content which initially has 0
                    //pages and a Thumbnail view only gives decent results after
                    //the entire content has been loaded; since we don't want to provide a
                    //"jarring" situation (where layout suddenly changes after the content's loaded)
                    //we use SetColumns to maintain the same exact layout as the old content.
                    //(This is consistent with DocumentViewer's overall behavior -- any view setting is
                    //a "one time thing" and isn't recomputed if the content changes.)
                    if (_documentLayout.ViewMode == ViewMode.Thumbnails)
                    {
                        _documentLayout.ViewMode = ViewMode.SetColumns;
                    }
                    QueueUpdateDocumentLayout(_documentLayout);

                    //Invalidate Measure and our IDSI so that properties changed
                    //by the content assignment will be properly updated.
                    InvalidateMeasure();
                    InvalidateDocumentScrollInfo();
                }
            }
        }

        /// <summary>
        /// Indicates the number of pages currently in the document.
        /// </summary>
        /// <value></value>
        public int PageCount
        {
            get
            {
                return _pageCache.PageCount;
            }
        }

        /// <summary>
        /// When queried, FirstVisiblePageNumber returns the first page visible onscreen.
        /// </summary>
        /// <value></value>
        public int FirstVisiblePageNumber
        {
            get
            {
                return _firstVisiblePageNumber;
            }
        }

        /// <summary>
        /// Returns the current Scale factor applied to the pages given the current settings.
        /// </summary>
        /// <value></value>
        public double Scale
        {
            get
            {
                return _rowCache.Scale;
            }
        }

        /// <summary>
        /// Returns the current number of Columns of pages displayed given the current settings.
        /// </summary>
        /// <value></value>
        public int MaxPagesAcross
        {
            get
            {
                return _maxPagesAcross;
            }
        }

        /// <summary>
        /// Specifies the vertical gap between Pages when laid out, in pixel (1/96?) units.
        /// </summary>
        /// <value></value>
        public double VerticalPageSpacing
        {
            get
            {
                return _rowCache.VerticalPageSpacing;
            }

            set
            {
                if (!Helper.IsDoubleValid(value))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _rowCache.VerticalPageSpacing = value;
            }
        }

        /// <summary>
        /// Specifies the horizontal gap between Pages when laid out, in pixel (1/96?) units.
        /// </summary>
        /// <value></value>
        public double HorizontalPageSpacing
        {
            get
            {
                return _rowCache.HorizontalPageSpacing;
            }

            set
            {
                if (!Helper.IsDoubleValid(value))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _rowCache.HorizontalPageSpacing = value;
            }
        }

        /// <summary>
        /// Specifies whether each displayed page should be adorned with a ?Drop Shadow? border or not.
        /// </summary>
        /// <value></value>
        public bool ShowPageBorders
        {
            get
            {
                return _showPageBorders;
            }

            set
            {
                if (_showPageBorders != value)
                {
                    _showPageBorders = value;

                    //Update our pages' ShowPageBorder properties.
                    //Get the current Visual Collection, which contains our pages.
                    int count = _childrenCollection.Count;
                    for (int i = 0; i < count; i++)
                    {
                        DocumentGridPage dp = _childrenCollection[i] as DocumentGridPage;

                        if (dp != null)
                        {
                            dp.ShowPageBorders = _showPageBorders;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Specifies whether the last "view mode" related property change should be locked
        /// for resizing.
        /// </summary>
        /// <value></value>
        public bool LockViewModes
        {
            get
            {
                return _lockViewModes;
            }

            set
            {
                _lockViewModes = value;
            }
        }

        /// <summary>
        /// Returns a TextContainer for current content
        /// </summary>
        /// <returns>The content's TextContainer, or null if there is none.</returns>
        public ITextContainer TextContainer
        {
            get
            {
                if (_textContainer == null)
                {
                    if (Content != null)
                    {
                        IServiceProvider isp = Content as IServiceProvider;
                        if (isp != null)
                        {
                            _textContainer = (ITextContainer)isp.GetService(typeof(ITextContainer));
                        }
                    }
                }

                return _textContainer;
            }
        }

        /// <summary>
        /// Returns the MultiPageTextView for the current content.
        /// </summary>
        /// <value></value>
        public ITextView TextView
        {
            get
            {
                if (TextEditor != null)
                {
                    return TextEditor.TextView;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The collection of currently-visible DocumentPageViews.
        /// </summary>
        public ReadOnlyCollection<DocumentPageView> PageViews
        {
            get
            {
                return _pageViews;
            }
        }

        /// <summary>
        /// ScrollOwner is the container that controls any scrollbars, headers, etc... that are dependent
        /// on this IScrollInfo's properties.  Implementers of IScrollInfo should call InvalidateScrollInfo()
        /// on this object when related properties change.
        /// </summary>
        public ScrollViewer ScrollOwner
        {
            get
            {
                return _scrollOwner;
            }

            set
            {
                _scrollOwner = value;
                InvalidateDocumentScrollInfo();
            }
        }

        /// <summary>
        /// DocumentViewerOwner is the DocumentViewer Control and UI that hosts the IDocumentScrollInfo object.
        /// This control is dependent on this IDSI?s properties, so implementers of IDSI should call
        /// InvalidateDocumentScrollInfo() on this object when related properties change so that
        /// DocumentViewer?s UI will be kept in sync.  This property is analogous to IScrollInfo?s ScrollOwner
        /// property.
        /// </summary>
        /// <value></value>
        public DocumentViewer DocumentViewerOwner
        {
            get
            {
                return _documentViewerOwner;
            }

            set
            {
                _documentViewerOwner = value;
            }
        }

        #endregion IDocumentScrollInfo

        #endregion Interface Implementations

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
        #region Protected Methods

        /// <summary>
        ///   Derived class must implement to support Visual children. The method must return
        ///    the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark:
        ///       During this virtual call it is not valid to modify the Visual tree.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (_childrenCollection == null || index < 0 || index >= _childrenCollection.Count)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }

            return _childrenCollection[index];
        }

        /// <summary>
        ///  Derived classes override this property to enable the Visual code to enumerate
        ///  the Visual children. Derived classes need to return the number of children
        ///  from this method.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark: During this virtual method the Visual tree must not be modified.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                // _childrenCollection cannot be null since its initialized in the constructor
                return _childrenCollection.Count;
            }
        }

        /// <summary>
        /// MeasureOverride is repsonsible for measuring any visible pages to their correct sizes.
        /// </summary>
        /// <param name="constraint">The upper bound for child sizes</param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size constraint)
        {
            // If layoutSize is infinity, we need to return our absolute smallest size.
            // This might happen if we are inside an element which sizes-to-content.
            // For DocumentGrid, we use a hard coded constraint.
            if (double.IsInfinity(constraint.Width) || double.IsInfinity(constraint.Height))
            {
                constraint = _defaultConstraint;
            }

            //Determine which pages are visible at the current offset given the current constraint.
            RecalculateVisualPages(VerticalOffset, constraint);

            //Get our visual children count...
            int count = _childrenCollection.Count;

            //Now go through our child collection and measure all the pages to their sizes.
            for (int i = 0; i < count; i++)
            {
                //This should be our background.
                if (i == _backgroundVisualIndex)
                {
                    Border background = _childrenCollection[i] as Border;

                    if (background == null)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.DocumentGridVisualTreeContainsNonBorderAsFirstElement));
                    }

                    //We measure this to the size of our constraint.
                    background.Measure(constraint);
                }
                //Otherwise it's a page.
                else
                {
                    //Ensure that this is actually a DocumentGridPage.  If it is not,
                    //Then someone's been mucking with our VisualTree, so we'll throw.
                    DocumentGridPage page = _childrenCollection[i] as DocumentGridPage;

                    if (page == null)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.DocumentGridVisualTreeContainsNonDocumentGridPage));
                    }

                    //Get the cached size of this page and scale it to our current scale factor.
                    Size pageSize = _pageCache.GetPageSize(page.PageNumber);
                    pageSize.Width *= Scale;
                    pageSize.Height *= Scale;

                    //Measure the page if necessary.
                    if (!page.IsMeasureValid)
                    {
                        page.Measure(pageSize);

                        //See if the cached size has changed since we Measured.
                        //This can happen if in the course of Measuring the page
                        //a GetPageAsync() calls back immediately with the real page size.
                        //If this happens we need to re-measure the page before we finish here,
                        //otherwise we'll end up with a page that's Measured to one size
                        //and Arranged to another, which looks bad.
                        Size newPageSize = _pageCache.GetPageSize(page.PageNumber);
                        if (newPageSize != Size.Empty)
                        {
                            newPageSize.Width *= Scale;
                            newPageSize.Height *= Scale;
                            if (newPageSize.Width != pageSize.Width ||
                            newPageSize.Height != pageSize.Height)
                            {
                                //Measure again.
                                page.Measure(newPageSize);
                            }
                        }
                    }
                }
            }

            return constraint;
        }

        /// <summary>
        /// ArrangeOverride is responsible for arranging the previously measured pages in the right order.
        /// </summary>
        /// <param name="arrangeSize">The final constraint, inside of which everything must live.</param>
        /// <returns></returns>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if (_viewportHeight != arrangeSize.Height ||
                _viewportWidth != arrangeSize.Width)
            {
                //Update our Viewport sizes
                _viewportWidth = arrangeSize.Width;
                _viewportHeight = arrangeSize.Height;

                if (LockViewModes && IsViewLoaded())
                {
                    if (_firstVisiblePageNumber < _pageCache.PageCount && _rowCache.HasValidLayout)
                    {
                        //If we're locking the view modes and we have loaded content, then we need to re-apply
                        //the last mode setting since our constraint has changed
                        ApplyViewParameters(_rowCache.GetRowForPageNumber(_firstVisiblePageNumber));
                        MeasureOverride(arrangeSize);
                    }
                }

                UpdateTextView();
            }

            //If we have a non-zero viewport size, we should execute any
            //requests for layout that may have been made but were unable
            //to complete due to a zero viewport size.
            if (IsViewportNonzero)
            {
                if (ExecutePendingLayoutRequests())
                {
                    //We need to re-do layout (RowCache has changed), so call measure here
                    //to ensure everything's updated accordingly.
                    MeasureOverride(arrangeSize);
                }
            }

            //If our constraint size has changed then we need to
            //alert our parents so they can update their ViewportWidth/Height properties.
            if (_previousConstraint != arrangeSize)
            {
                _previousConstraint = arrangeSize;
                InvalidateDocumentScrollInfo();
            }

            //Now we go through the visible rows and arrange the pages within them.
            //Get our visual collection count
            int count = _childrenCollection.Count;

            //If we have no visual children, there's nothing to arrange
            //so quit now.
            if (count == 0)
            {
                return arrangeSize;
            }

            //Arrange the background first.  This is always child 0.
            //The background takes up the entire constraint.
            UIElement background = _childrenCollection[_backgroundVisualIndex] as UIElement;
            background.Arrange(new Rect(new Point(0, 0), arrangeSize));

            //The offsets for the current page being arranged.
            double xOffset;
            double yOffset;

            //The current visual child (aka DocumentGridPage) we're arranging.
            //The first child in our tree is always the background so we start at
            //1 which is our first page.
            int visualChild = _firstPageVisualIndex;

            //Now walk through the visible rows and arrange the pages therein.
            for (int row = _firstVisibleRow; row < _firstVisibleRow + _visibleRowCount; row++)
            {
                //Calculate the position for this row.
                CalculateRowOffsets(row, out xOffset, out yOffset);

                //Get the current row.
                RowInfo currentRow = _rowCache.GetRow(row);

                //Now we can lay out this row.
                for (int page = currentRow.FirstPage; page < currentRow.FirstPage + currentRow.PageCount; page++)
                {
                    //This should never, ever happen so we'll throw if it does.
                    if (visualChild > _childrenCollection.Count - 1)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.DocumentGridVisualTreeOutOfSync));
                    }

                    //Get the cached size of this page.
                    Size pageSize = _pageCache.GetPageSize(page);

                    //Scale it by our scale factor
                    pageSize.Width *= Scale;
                    pageSize.Height *= Scale;

                    //Arrange the page if necessary
                    UIElement uiPage = _childrenCollection[visualChild] as UIElement;
                    if (uiPage != null)
                    {
                        Point pageOffset;
                        //Move the page to the right place based on the FlowDirection of the content.
                        if (_pageCache.IsContentRightToLeft)
                        {
                            pageOffset = new Point(Math.Max(ViewportWidth, ExtentWidth) - (xOffset + pageSize.Width), yOffset);
                        }
                        else
                        {
                            pageOffset = new Point(xOffset, yOffset);
                        }
                        uiPage.Arrange(new Rect(pageOffset, pageSize));
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Get(SRID.DocumentGridVisualTreeContainsNonUIElement));
                    }

                    //Increment our horizontal offset to point to where the next page should go.
                    xOffset += (pageSize.Width + HorizontalPageSpacing);

                    //Move to the next page.
                    visualChild++;
                }
            }

            // As we scroll we need to keep the AdornerLayer up-to-date.
            // This ensures that annotation components scroll with the content.
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(this);
            if (layer != null && layer.GetAdorners(this) != null)
                layer.Update(this);

            return arrangeSize;
        }

        /// <summary>
        /// Override the OnPreviewMouseLeftButtonDown method so that we can trap the
        /// keyboard+mouse events needed for Rubberband selection.
        /// Clicking the Left mouse button while holding Alt will enable the Rubberband
        /// selection "mode" until the Left mouse button is again pressed without the
        /// Alt key held.
        /// </summary>
        /// <param name="e">The MouseButtonEventArgs associated with this mouse event.</param>
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            //Determine whether either Alt key is being held at this moment.
            bool altKeyDown = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);

            //If the Alt key is held and we aren't currently in RubberBandSelection mode,
            //we can create and attach our RubberBandSelector now.
            //We'll stay in this mode until the mouse is clicked without the Alt key held.
            if (altKeyDown && _rubberBandSelector == null)
            {
                //See if our content implements IServiceProvider.
                IServiceProvider serviceProvider = Content as IServiceProvider;
                if (serviceProvider != null)
                {
                    //See if our content supports rubber band selection.
                    _rubberBandSelector = serviceProvider.GetService(typeof(RubberbandSelector)) as RubberbandSelector;

                    if (_rubberBandSelector != null)
                    {
                        DocumentViewerOwner.Focus(); // text editor needs to be focused when cleared
                        ITextRange textRange = TextEditor.Selection;
                        textRange.Select(textRange.Start, textRange.Start); //clear selection
                        DocumentViewerOwner.IsSelectionEnabled = false;

                        _rubberBandSelector.AttachRubberbandSelector((FrameworkElement)this); //attach the Rubber band selector.
                    }
                }
            }
            //We got a mouse-down event and the Alt key is not being held, so we revert back
            //to normal selection mode now.
            else if (!altKeyDown && _rubberBandSelector != null)
            {
                //Detach the Rubberband Selector
                if (_rubberBandSelector != null)
                {
                    _rubberBandSelector.DetachRubberbandSelector();
                    _rubberBandSelector = null;
                }

                DocumentViewerOwner.IsSelectionEnabled = true;
            }
        }

        /// <summary>
        ///
        /// Reset the entire visual tree when the visual parent changes in order to ensure that
        /// the HighlightVisuals associated with the FixedPages in the document are re-created
        /// and added to the new AdornerLayer.
        /// </summary>
        /// <param name="oldParent">The old visual parent (not used)</param>
        protected internal override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);

            // No need for a reset if we don't have a parent since there is no AdornerLayer
            // to add HighlightVisuals back to.
            if (VisualTreeHelper.GetParent(this) != null)
            {
                // Do a full reset, we want to ensure even visible pages are reset.
                ResetVisualTree(pruneOnly: false);
            }
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        #region Private Methods

        /// <summary>
        /// Recalculates the set of pages that are currently visible and updates
        /// DocumentGrid's VisualCollection so it contains them.
        /// </summary>
        /// <param name="constraint">The viewport to search for visible pages in.</param>
        /// <param name="offset">The offset in the document to start the search.</param>
        private void RecalculateVisualPages(double offset, Size constraint)
        {
            //Do we actually have any rows in the cache?
            //If not, we can just clear our visual collection and return.
            if (_rowCache.RowCount == 0)
            {
                ResetVisualTree(false /*pruneOnly*/);
                ResetPageViewCollection();
                _firstVisibleRow = 0;
                _visibleRowCount = 0;
                _firstVisiblePageNumber = 0;
                _lastVisiblePageNumber = 0;

                // Perf Tracing - PageVisible Changed
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXPageVisible, _firstVisiblePageNumber, _lastVisiblePageNumber);

                return;
            }

            int newFirstVisibleRow = 0;
            int newVisibleRowCount = 0;

            //Ask the RowCache for the currently visible rows.
            _rowCache.GetVisibleRowIndices(offset,
                                            offset + constraint.Height,
                                            out newFirstVisibleRow,
                                            out newVisibleRowCount);

            //Do we have no visible rows at all?  Then clear the Visual collection and return.
            if (newVisibleRowCount == 0)
            {
                ResetVisualTree(false /*pruneOnly*/);
                ResetPageViewCollection();
                _firstVisibleRow = 0;
                _visibleRowCount = 0;
                _firstVisiblePageNumber = 0;
                _lastVisiblePageNumber = 0;

                // Perf Tracing - PageVisible Changed
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXPageVisible, _firstVisiblePageNumber, _lastVisiblePageNumber);

                return;
            }

            //Now walk through each visible row and compare the pages therein
            //with the current set of pages in our Visual Collection.
            //New pages are inserted into the collection, unused pages are removed.

            //Get the current first and last pages visible.
            int firstPage = -1;
            int lastPage = -1;

            //If we have more visuals than just the background (element 0)
            //then we have pages, so get the page numbers from them.
            if (_childrenCollection.Count > _firstPageVisualIndex)
            {
                DocumentGridPage firstDp = _childrenCollection[1] as DocumentGridPage;
                firstPage = firstDp != null ? firstDp.PageNumber : -1;

                DocumentGridPage lastDp = _childrenCollection[_childrenCollection.Count - 1] as DocumentGridPage;
                lastPage = lastDp != null ? lastDp.PageNumber : -1;
            }


            //Update our First & LastVisiblePage properties
            RowInfo firstRow = _rowCache.GetRow(newFirstVisibleRow);
            _firstVisiblePageNumber = firstRow.FirstPage;
            RowInfo lastRow = _rowCache.GetRow(newFirstVisibleRow + newVisibleRowCount - 1);
            _lastVisiblePageNumber = lastRow.FirstPage + lastRow.PageCount - 1;

            // Perf Tracing - PageVisible Changed
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXPageVisible, _firstVisiblePageNumber, _lastVisiblePageNumber);

            //Update our cached visible row info (used by Measure/Arrange)
            _firstVisibleRow = newFirstVisibleRow;
            _visibleRowCount = newVisibleRowCount;


            //If IDSI properties have changed (namely the First/LastVisiblePage properties) we invalidate them now.
            if (_firstVisiblePageNumber != firstPage ||
                _lastVisiblePageNumber != lastPage)
            {
                //Create our temporary VisualCollection, which will hold the new list of
                //visible pages.
                ArrayList visiblePages = new ArrayList();

                //Now walk through the visible rows and add the pages to our temporary list.
                for (int i = _firstVisibleRow; i < _firstVisibleRow + _visibleRowCount; i++)
                {
                    //Get the row
                    RowInfo currentRow = _rowCache.GetRow(i);

                    //Walk through the row
                    for (int j = currentRow.FirstPage; j < currentRow.FirstPage + currentRow.PageCount; j++)
                    {
                        //Is this page new?
                        if (j < firstPage || j > lastPage || _childrenCollection.Count <= _firstPageVisualIndex)
                        {
                            //Create a new page and add it to our temporary visual collection.
                            DocumentGridPage dp = new DocumentGridPage(Content);
                            dp.ShowPageBorders = ShowPageBorders;
                            dp.PageNumber = j;

                            //Attach the Loaded event handler
                            dp.PageLoaded += new EventHandler(OnPageLoaded);
                            visiblePages.Add(dp);
                        }
                        else
                        {
                            //This page already exists in our visual collection, so we copy that entry over
                            //from the visual collection instead of creating a new page.
                            //(We start at 1 to skip over the background visual.)
                            visiblePages.Add(_childrenCollection[_firstPageVisualIndex + j - Math.Max(0, firstPage)]);
                        }
                    }
                }

                //Copy our new visible page collection over to the VisualCollection and update
                //the MultiPageTextView's list of visible DocumentPageViews.
                //First, prune our visual tree so it only contains the set of pages that are visible
                //before and after the layout change.
                ResetVisualTree(true /*pruneOnly*/);

                Collection<DocumentPageView> documentPageViews =
                    new Collection<DocumentPageView>();

                //State machine for updating visual collection without removing still-visible pages
                //from the collection:
                //We walk through the set of visible pages that we computed above.
                //We insert new pages before existing pages, and add pages after existing pages.
                //
                //We take advantage of the fact that both the current set of pages in the Visual Collection
                //and the set of to-be-made visible pages in visiblePages are in strictly increasing order
                //with no gaps between pages.
                //To better understand how the below works, refer to Fig. A below:
                //
                //         +-----------------------------+
                //  +------+                             +------+
                //  | new  |  Pruned Visual Collection   |  new |
                //  +------+   (Existing Page Visuals)   +------+
                //         +-----------------------------+
                //  ^      ^                             ^
                //  |      |                             |
                //  A      B                             C
                //
                //                                 [Fig. A: Diagram of states]
                //
                //- The routine starts off in state A (BeforeExisting).
                //  At this point we insert any new pages in the visiblePages collection until we
                //  find a page in the visiblePages collection that is also in the Pruned Visual Collection.
                //  This indicates that the set of common unchanged pages has been reached.
                //  The state machine then transitions to state B (DuringExisting).
                //- The routine stays in B merely iterating through visiblePages until it finds a page
                //  in visiblePages that does not correspond to a page in the Visual Tree.  This indicates
                //  that the end of the set of common unchanged pages has been reached; at this point we
                //  add the new page and transition to state C (AfterExisting).
                //- State C ends when no more pages are left in visiblePages.
                VisualTreeModificationState state = VisualTreeModificationState.BeforeExisting;

                //The index pointing to the first common page still in the visual tree after the above pruning.
                int vcIndex = _firstPageVisualIndex;

                for (int i = 0; i < visiblePages.Count; i++)
                {
                    Visual current = (Visual)visiblePages[i];

                    switch (state)
                    {
                        case VisualTreeModificationState.BeforeExisting:
                            //Keep inserting until we find a page that already exists
                            if (vcIndex < _childrenCollection.Count && _childrenCollection[vcIndex] == current)
                            {
                                //Move to "During" state
                                state = VisualTreeModificationState.DuringExisting;
                            }
                            else
                            {
                                //Insert this page at the current index.
                                _childrenCollection.Insert(vcIndex, current);
                            }
                            //Increment the index into the Visual collection to ensure that it continues
                            //to point to the first common page.
                            vcIndex++;
                            break;

                        case VisualTreeModificationState.DuringExisting:
                            //Leave the visual collection alone until we find a page that isn't in the collection
                            //or run out of pages in the collection.
                            if (vcIndex >= _childrenCollection.Count || _childrenCollection[vcIndex] != current)
                            {
                                //Move to "After" state
                                state = VisualTreeModificationState.AfterExisting;
                                //Append this page to the end.
                                _childrenCollection.Add(current);
                            }
                            //Keep moving through the Visual collection...
                            vcIndex++;
                            break;

                        case VisualTreeModificationState.AfterExisting:
                            //Keep going until the end.
                            _childrenCollection.Add(current);
                            break;
                    }

                    //Add this to the collection of PageViews.
                    documentPageViews.Add(((DocumentGridPage)visiblePages[i]).DocumentPageView);
                }

                //Update our collection of PageViews with the current set.
                _pageViews = new ReadOnlyCollection<DocumentPageView>(documentPageViews);

                //Tell our parent DocumentViewer that we've updated our PageView collection.
                InvalidatePageViews();
                InvalidateDocumentScrollInfo();
            }
        }

        /// <summary>
        /// Handles the PageLoaded event for a given page, and kicks off
        /// BringIntoView actions where necessary.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnPageLoaded(object sender, EventArgs args)
        {
            DocumentGridPage page = sender as DocumentGridPage;
            Invariant.Assert(page != null, "Invalid sender for OnPageLoaded event.");

            //Detach the event handler, we don't need this event any longer.
            page.PageLoaded -= new EventHandler(OnPageLoaded);

            //Is there a MakeVisible operation waiting for this page to be loaded?
            //If so, invoke its dispatcher in the background.
            if (_makeVisiblePageNeeded == page.PageNumber)
            {
                _makeVisiblePageNeeded = -1;
                _makeVisibleDispatcher.Priority = DispatcherPriority.Background;
            }

            // Perf Tracing - PageLoaded
            if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordXPS, EventTrace.Level.Info))
            {
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientDRXPageLoaded, EventTrace.Keyword.KeywordXPS, EventTrace.Level.Info, page.PageNumber);
            }
        }


        /// <summary>
        /// Calculates the X and Y offsets of the given row based on the current
        /// Viewport dimensions.
        /// </summary>
        /// <param name="row">The row to calculate the offsets of</param>
        /// <param name="xOffset">The X offset of the row</param>
        /// <param name="yOffset">The Y offset of the row</param>
        private void CalculateRowOffsets(int row, out double xOffset, out double yOffset)
        {
            xOffset = 0.0;
            yOffset = 0.0;

            //Get the current row.
            RowInfo currentRow = _rowCache.GetRow(row);

            //Figure out the width we'll use to center the pages.
            //If the ViewportWidth is wider than the document, we use that.  Otherwise we center
            //the content based on the width of the document.
            double centerWidth = Math.Max(ViewportWidth, ExtentWidth);

            //Figure out the offset of the upper left corner of this row.

            //X Coordinate:
            //If this is the last row in the document and we're viewing
            //uniformly-sized pages then this row is
            //always left-aligned (not centered).
            if (row == _rowCache.RowCount - 1 && !_pageCache.DynamicPageSizes)
            {
                //This is the last row, so we arrange it such that the left edge of the
                //page is flush with the left edge of the document.
                xOffset = (centerWidth - ExtentWidth) / 2.0 +
                    (HorizontalPageSpacing / 2.0) - HorizontalOffset;
            }
            else
            {
                //Otherwise we center this page inside the document.
                xOffset = (centerWidth - currentRow.RowSize.Width) / 2.0 +
                    (HorizontalPageSpacing / 2.0) - HorizontalOffset;
            }

            //Y Coordinate:
            if (ExtentHeight > ViewportHeight)
            {
                //The document is taller than the viewport, so we just display
                //the content at the current offset.
                yOffset = currentRow.VerticalOffset +
                    (VerticalPageSpacing / 2.0) - VerticalOffset;
            }
            else
            {
                //If the document is shorter than the Viewport we're showing it in,
                //we center it vertically within the viewport.  We do not need to factor in
                //VerticalOffset as it is always 0.0 in this scenario.
                yOffset = currentRow.VerticalOffset +
                    (ViewportHeight - ExtentHeight) / 2.0 + (VerticalPageSpacing / 2.0);
            }
        }

        /// <summary>
        /// Resets DocumentGrid's visual tree to its initial state or prunes non-visible pages.
        /// This is empty except for a border which acts as a background.
        /// </summary>
        /// <param name="pruneOnly">Whether to clear all pages, or only those that are not visible.</param>
        private void ResetVisualTree(bool pruneOnly)
        {
            //We need to dispose and remove any pages that will no longer be in the visual tree.
            for (int i = _childrenCollection.Count - 1; i >= _firstPageVisualIndex; i--)
            {
                DocumentGridPage dp = _childrenCollection[i] as DocumentGridPage;
                if (dp != null &&
                        (!pruneOnly ||
                        _rowCache.RowCount == 0 ||
                        dp.PageNumber < _firstVisiblePageNumber ||
                        dp.PageNumber > _lastVisiblePageNumber))
                {
                    //This page will not be visible any longer, so get rid of it.
                    //Remove this page from the Visual tree.
                    _childrenCollection.Remove(dp);

                    //Remove any PageLoaded event handlers
                    dp.PageLoaded -= new EventHandler(OnPageLoaded);

                    //Dispose of the page.
                    ((IDisposable)dp).Dispose();
                }
            }

            //Create the background if it does not exist.
            if (_documentGridBackground == null)
            {
                //We create a Border with a transparent background so that it can
                //participate in Hit-Testing (which allows click events like those
                //for our Context Menu to work).
                _documentGridBackground = new Border();
                _documentGridBackground.Background = Brushes.Transparent;

                //Add the background in.
                _childrenCollection.Add(_documentGridBackground);
            }
        }

        /// <summary>
        /// Nulls out the PageViews collection and notifies DocumentViewer of the change.
        /// </summary>
        private void ResetPageViewCollection()
        {
            //Null out our collection of PageViews.
            _pageViews = null;

            //Tell our parent DocumentViewer that we've updated our PageView collection.
            InvalidatePageViews();
        }

        #region MakeVisible Helpers

        /// <summary>
        /// Handles the GetPageNumberCompleted event fired as a result of a MakeContentVisibleAsync
        /// call.  At this point we know the page number corresponding to the ContentPosition we need
        /// to make visible, so we invoke MakeVisibleAsync() to bring it into view.
        /// </summary>
        /// <param name="sender">The sender of this event</param>
        /// <param name="e">The args associated with this event.
        /// We expect e.UserState to be a MakeVisibleData.</param>
        private void OnGetPageNumberCompleted(object sender, GetPageNumberCompletedEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }
            //Ensure that the UserState passed with this event contains an
            //MakeVisibleData object. If not, we ignore it as this event
            //could have originated from someone else calling GetPageNumberAsync.
            if (e.UserState is MakeVisibleData)
            {
                MakeVisibleData data = (MakeVisibleData)e.UserState;
                MakeVisibleAsync(data, e.PageNumber);
            }
        }


        /// <summary>
        /// Makes the specified object on the specified page visible, which may be an
        /// asynchronous operation if the page is not already in view.
        /// </summary>
        /// <param name="data">Data corresponding to the object to be made visible.</param>
        /// <param name="pageNumber">The page number the object is on.</param>
        private void MakeVisibleAsync(MakeVisibleData data, int pageNumber)
        {
            //This page may not be currently visible.
            //First we need to make the page visible, if necessary.
            //This will be done at background priority to allow currently-loading pages time to
            //finish.  If we do not do this, then in the corner case of:
            // 1) Document has just been loaded (for example, just after a hyperlink navigation to the doc)
            // 2) Document has non 8.5x11-sized pages at the beginning of the document
            // 3) Navigation is to a page past the initially visible pages.
            //In this case, the order of operations is something like this:
            // 1) Initially visible pages start loading
            // 2) MakeVisible is invoked, and MakePageVisible is called, which uses cached
            //    page info to decide where to scroll to.  (8.5x11 is assumed until a page is loaded)
            // 3) Document is scrolled to position computed in #2
            // 4) Pages loading in #1 finish loading, GetPageCompleted is called, PageCache is updated,
            //    and the document layout changes.  This will shift the page we actually want to see up or down,
            //    potentially by a substantial amount.
            // 5) User is now looking at the wrong page, and if the page that the hyperlink target is on isn't
            //    visible at this point then the MakeVisible operation fails in MakeVisibleImpl since the target
            //    isn't in the visual tree.
            // Everyone got that?  So, doing the initial "MakePageVisible" operation at Background priority
            // allows step #4 above to finish _before_ we do steps 2 and 3, so the right page will be visible
            // in 5 and the MakeVisible operation will succeed.
            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                   new BringPageIntoViewCallback(BringPageIntoViewDelegate), data, pageNumber);
        }

        /// <summary>
        /// Delegate method used to bring a specified page into view.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="pageNumber"></param>
        private void BringPageIntoViewDelegate(MakeVisibleData data, int pageNumber)
        {
            //Make the page visible if necessary:
            // - If our layout is not yet valid
            // - If the visual being made visible is a FixedPage and
            //   the bring-into-view rect is the entire page
            //   (in which case we always want to move it as close to the top of the
            //   viewport as possible even if it is already partially visible)
            // - If the page isn't currently visible.
            if (!_rowCache.HasValidLayout ||
                (data.Visual is FixedPage &&
                 data.Visual.VisualContentBounds == data.Rect) ||
                pageNumber < _firstVisiblePageNumber ||
                pageNumber > _lastVisiblePageNumber)
            {
                MakePageVisible(pageNumber);
            }

            //The page's contents have already been loaded, we can bring the object into view immediately.
            if (IsPageLoaded(pageNumber))
            {
                MakeVisibleImpl(data);
            }
            else
            {
                //Now we have to wait for the page to be loaded so that we can
                //ensure that the object itself is visible.
                //As pages are loaded, this page will be checked for, and the dispatcher below
                //executed as appropriate.
                _makeVisiblePageNeeded = pageNumber;
                _makeVisibleDispatcher = Dispatcher.BeginInvoke(DispatcherPriority.Inactive,
                    (DispatcherOperationCallback)delegate (object arg)
                    {
                        MakeVisibleImpl((MakeVisibleData)arg);
                        return null;
                    }, data);
            }
        }

        /// <summary>
        /// Implementation of MakeVisible logic, the final step in a MakeVisible operation.
        /// </summary>
        /// <param name="data"></param>
        private void MakeVisibleImpl(MakeVisibleData data)
        {
            if (data.Visual != null)
            {
                //Ensure that the passed-in visual is a descendant of DocumentGrid.
                if (((Visual)this).IsAncestorOf(data.Visual))
                {
                    //Now we can determine where this visual is relative to the upper left
                    //corner of the DocumentGrid and thus make it visible.
                    GeneralTransform transform = data.Visual.TransformToAncestor(this);
                    Rect boundingRect = (data.Rect != Rect.Empty) ? data.Rect : data.Visual.VisualContentBounds;

                    Rect offsetRect = transform.TransformBounds(boundingRect);
                    MakeRectVisible(offsetRect, false /* alwaysCenter */);
                }
            }
            else if (data.ContentPosition != null)
            {
                ITextPointer tp = data.ContentPosition as ITextPointer;

                //If we have a valid TextView and the TextPointer is in that TextView
                //we can make the TextPointer's Rect visible...
                if (TextViewContains(tp))
                {
                    MakeRectVisible(TextView.GetRectangleFromTextPosition(tp), false /* alwaysCenter */);
                }
            }
            else
            {
                Invariant.Assert(false, "Invalid object brought into view.");
            }
        }


        /// <summary>
        /// Moves the specified rectangle into view, if it isn't already visible.
        /// </summary>
        /// <param name="r">A rectangle relative to the upper-left corner of the Viewport</param>
        /// <param name="alwaysCenter">Whether to center the rect at all times or only when necessary.</param>
        private void MakeRectVisible(Rect r, bool alwaysCenter)
        {
            if (r != Rect.Empty)
            {
                //Calculate the real position of the rectangle in the document.
                Rect translatedRect = new Rect(HorizontalOffset + r.X, VerticalOffset + r.Y,
                                               r.Width, r.Height);

                Rect viewportRect = new Rect(HorizontalOffset, VerticalOffset,
                                             ViewportWidth,
                                             ViewportHeight);

                //Unless the alwaysCenter flag is set, if the new position is already
                //visible we don't need to shift the viewport. Otherwise we shift
                //the offsets so the rect is visible, centering if possible.
                if (alwaysCenter || !translatedRect.IntersectsWith(viewportRect))
                {
                    SetHorizontalOffsetInternal(translatedRect.X - (ViewportWidth / 2.0));
                    SetVerticalOffsetInternal(translatedRect.Y - (ViewportHeight / 2.0));
                }
            }
        }

        /// <summary>
        /// Moves the specified IP into view, if it isn't already visible.
        /// </summary>
        /// <param name="r">A rectangle relative to the upper-left corner of the Viewport which represents
        /// an IP (Insertion Point)</param>
        private void MakeIPVisible(Rect r)
        {
            if (r != Rect.Empty && TextEditor != null)
            {
                Rect viewportRect = new Rect(HorizontalOffset, VerticalOffset,
                                             ViewportWidth,
                                             ViewportHeight);

                //If the new position is already fully visible, we don't need to shift the viewport,
                //otherwise we shift the offsets so the rect is visible, moving as minimally as possible.
                if (!viewportRect.Contains(r))
                {
                    //Scroll left/right if the IP is off the screen Horizontally.
                    if (r.X < HorizontalOffset)
                    {
                        SetHorizontalOffsetInternal(HorizontalOffset - (HorizontalOffset - r.X));
                    }
                    else if (r.X > HorizontalOffset + ViewportWidth)
                    {
                        SetHorizontalOffsetInternal(HorizontalOffset + (r.X - (HorizontalOffset + ViewportWidth)));
                    }

                    //Scroll up/down if part of the IP is off the screen Vertically.
                    if (r.Y < VerticalOffset)
                    {
                        SetVerticalOffsetInternal(VerticalOffset - (VerticalOffset - r.Y));
                    }
                    else if (r.Y + r.Height > VerticalOffset + ViewportHeight)
                    {
                        SetVerticalOffsetInternal(VerticalOffset + ((r.Y + r.Height) - (VerticalOffset + ViewportHeight)));
                    }
                }
            }
        }

        /// <summary>
        /// Invokes GetPageNumberAsync on the passed in ContentPosition.
        /// The handler for GetPageNumberAsync will bring that ContentPosition into view.
        /// </summary>
        /// <param name="data">The MakeVisibleData to be made visible</param>
        private void MakeContentPositionVisibleAsync(MakeVisibleData data)
        {
            //If the ContentPosition is valid, we can make it visible now.
            if (data.ContentPosition != null && data.ContentPosition != ContentPosition.Missing)
            {
                Content.GetPageNumberAsync(data.ContentPosition, data);
            }
        }

        #endregion MakeVisible Helpers

        /// <summary>
        /// Places a delegate for an SetScale call on the queue.  We do this for
        /// performance reasons, as changing the document scale takes significant time.
        /// </summary>
        /// <param name="scale"></param>
        private void QueueSetScale(double scale)
        {
            //If there's a SetScale operation in the Pending state, then we'll
            //abort it (we only care that the last operation invoked completes.)
            if (_setScaleOperation != null &&
                _setScaleOperation.Status == DispatcherOperationStatus.Pending)
            {
                _setScaleOperation.Abort();
            }
            _setScaleOperation = Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Input,
                   new DispatcherOperationCallback(SetScaleDelegate),
                   scale);
        }

        private object SetScaleDelegate(object scale)
        {
            if (!(scale is double))
            {
                return null;
            }

            double newScale = (double)scale;
            _documentLayout.ViewMode = ViewMode.Zoom;

            //Get the current visible selection, if any.
            //The results of this will determine how we handle the
            //zoom operation.
            ITextPointer selection = GetVisibleSelection();

            if (selection != null)
            {
                //The visible-IP case:
                //First, we find out what page the IP is on:
                int selectionPage = GetPageNumberForVisibleSelection(selection);

                //Then we scale the document:
                UpdateLayoutScale(newScale);

                //Now we ensure that the selection page is still
                //visible:
                MakePageVisible(selectionPage);

                //The rest of this process is done asynchronously --
                //We wait for LayoutUpdated (which happens after layout
                //but before rendering) and then make the IP visible.
                //This will cause the IP to be centered without any
                //visible flicker.

                //Attach a LayoutUpdated handler.
                LayoutUpdated += new EventHandler(OnZoomLayoutUpdated);
            }
            else
            {
                //The non-visible-IP case:
                //This is considerably easier.  The expected behavior is that
                //we zoom in on the upper-left corner of the currently-visible
                //content.  This is accomplished by scaling the Vertical and
                //Horizontal offsets in tandem with the document scale which will
                //put us approximately where we were before.

                //Scale the document:
                UpdateLayoutScale(newScale);
            }

            return null;
        }

        /// <summary>
        /// Updates the Scale applied to our RowCache.
        /// </summary>
        /// <param name="scale"></param>
        private void UpdateLayoutScale(double scale)
        {
            if (!DoubleUtil.AreClose(scale, Scale))
            {
                double oldExtentHeight = ExtentHeight;
                double oldExtentWidth = ExtentWidth;

                //Tell our RowCache to rescale the layout.
                _rowCache.Scale = scale;

                //Rescale our offsets
                //Divide the old extents by the new to determine the amount to
                //scale the offsets
                double verticalScale = oldExtentHeight == 0.0 ? 1.0 : ExtentHeight / oldExtentHeight;
                double horizontalScale = oldExtentWidth == 0.0 ? 1.0 : ExtentWidth / oldExtentWidth;

                //Now we scale the offsets.
                SetVerticalOffsetInternal(_verticalOffset * verticalScale);
                SetHorizontalOffsetInternal(_horizontalOffset * horizontalScale);

                InvalidateMeasure();

                //Invalidate the measure of our visual children so that they can
                //be resized.
                InvalidateChildMeasure();

                //Invalidate our parents' properties
                InvalidateDocumentScrollInfo();
            }
        }

        /// <summary>
        /// Places a delegate for an UpdateDocumentLayout call on the queue.  We do this for
        /// performance reasons, as changing the document layout takes significant time.
        /// </summary>
        /// <param name="layout"></param>
        private void QueueUpdateDocumentLayout(DocumentLayout layout)
        {
            // Increase the count of pending DocumentLayout delegates
            _documentLayoutsPending++;
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Input,
                   new DispatcherOperationCallback(UpdateDocumentLayoutDelegate),
                   layout);
        }

        /// <summary>
        /// Asynchronously invokes UpdateDocumentLayout.
        /// </summary>
        /// <param name="layout"></param>
        /// <returns></returns>
        private object UpdateDocumentLayoutDelegate(object layout)
        {
            if (layout is DocumentLayout)
            {
                UpdateDocumentLayout((DocumentLayout)layout);
            }
            // Decrease the count of pending DocumentLayout delegates
            _documentLayoutsPending--;

            return null;
        }

        /// <summary>
        /// Updates the current layout of our RowCache to the specified number of
        /// columns.
        /// </summary>
        /// <param name="layout"></param>
        private void UpdateDocumentLayout(DocumentLayout layout)
        {
            // Check if the layout is for a Vertical or Horizontal offset update,
            // in which case the value can be changed immediately.
            if (layout.ViewMode == ViewMode.SetHorizontalOffset)
            {
                SetHorizontalOffsetInternal(layout.Offset);
                return;
            }
            else if (layout.ViewMode == ViewMode.SetVerticalOffset)
            {
                SetVerticalOffsetInternal(layout.Offset);
                return;
            }

            //Store off the layout in case we need it later.
            //(For example, if our viewport is (0,0).
            _documentLayout = layout;

            //Update MaxPagesAcross
            _maxPagesAcross = _documentLayout.Columns;

            //If we have a non (0,0) Viewport then we can calculate a new layout.
            //Otherwise we set our "Layout Requested" flag so that when we get
            //a non-zero Viewport size we'll compute the requested layout.
            if (IsViewportNonzero)
            {
                //If this is a Thumbnails layout, we need to calculate how many
                //columns we should fit on the pivotRow.
                if (_documentLayout.ViewMode == ViewMode.Thumbnails)
                {
                    _maxPagesAcross = _documentLayout.Columns = CalculateThumbnailColumns();
                }

                //We need to determine the page that has the active focus so we know what page
                //to keep visible in the new layout.
                int pivotPage = GetActiveFocusPage();

                //Ask the RowCache to recalculate the layout based
                //on the specified pivot page and the number of columns
                //requested.
                //The RowCache will call us back with a
                //RowLayoutCompleted event when the layout is complete
                //and we'll update the scale and invalidate our layout there.
                _rowCache.RecalcRows(pivotPage, _documentLayout.Columns);

                _isLayoutRequested = false;
            }
            else
            {
                _isLayoutRequested = true;
            }
        }

        /// <summary>
        /// Calls UpdateLayout with the saved column and view mode parameters,
        /// if there's a previously requested layout to perform.
        /// Used when we get a non-zero Viewport size and we've previously requested
        /// a new Row layout.
        /// </summary>
        /// <returns>A bool indicating whether a new layout was calculated.</returns>
        private bool ExecutePendingLayoutRequests()
        {
            if (_isLayoutRequested)
            {
                UpdateDocumentLayout(_documentLayout);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Set the HorizontalOffset to the value provided, the value will be set immediately.
        /// </summary>
        /// <param name="offset"></param>
        private void SetHorizontalOffsetInternal(double offset)
        {
            if (!DoubleUtil.AreClose(_horizontalOffset, offset))
            {
                if (Double.IsNaN(offset))
                {
                    throw new ArgumentOutOfRangeException("offset");
                }

                _horizontalOffset = offset;
                InvalidateMeasure();
                InvalidateDocumentScrollInfo();
                UpdateTextView();
            }
        }

        /// <summary>
        /// Set the VerticalOffset to the value provided, the value will be set immediately.
        /// </summary>
        /// <param name="offset"></param>
        private void SetVerticalOffsetInternal(double offset)
        {
            if (!DoubleUtil.AreClose(_verticalOffset, offset))
            {
                if (Double.IsNaN(offset))
                {
                    throw new ArgumentOutOfRangeException("offset");
                }

                _verticalOffset = offset;
                InvalidateMeasure();
                InvalidateDocumentScrollInfo();
                UpdateTextView();
            }
        }

        /// <summary>
        /// Updates the TextView so that it knows about size and position changes.
        /// </summary>
        private void UpdateTextView()
        {
            MultiPageTextView tv = TextView as MultiPageTextView;
            if (tv != null)
            {
                tv.OnPageLayoutChanged();
            }
        }

        /// <summary>
        /// Calculates the number of columns to fit on one row so that the resultant
        /// view will approximate a "thumbnail" view.
        /// The basic idea is that we attempt to fit (and scale) a number of pages on the row
        /// such that the resultant view given the current Viewport will show as many
        /// pages as possible, with a lower bound of a 5% zoom.
        /// We attempt to optimize to minimize wasted space, where possible.  This may mean
        /// that not all pages will be completely displayed, but we favor a better looking
        /// layout (less dead space) over being able to see every page.
        /// </summary>
        /// <returns>The number of pages to show on the first row.</returns>
        private int CalculateThumbnailColumns()
        {
            //If our current Viewport size is zero, we'll just return 1.
            //(because there always needs to be at least 1 page on a row regardless.)
            if (!IsViewportNonzero)
            {
                return 1;
            }

            //If we have no pages, we'll just return 1
            //(because there always needs to be at least 1 page on a row regardless.)
            if (_pageCache.PageCount == 0)
            {
                return 1;
            }

            //We use the first page of the document as our basis for our calculations.
            //This means that documents with varying page sizes can potentially have
            //sub-optimal thumbnail views.
            Size pageSize = _pageCache.GetPageSize(0);

            //Calculate the viewport's aspect ratio.
            double viewportAspect = ViewportWidth / ViewportHeight;

            //Calculate the maximum number of columns we can lay out on a single row
            //without needing to scale below our floor of 12.5%.
            int maxColumns =
                (int)Math.Floor(ViewportWidth /
                    (CurrentMinimumScale * pageSize.Width + HorizontalPageSpacing));

            //Ensure this value isn't greater than the number of pages in the document,
            //since we can't possibly lay out a row with more than that number of pages in it.
            maxColumns = Math.Min(maxColumns, _pageCache.PageCount);
            maxColumns = Math.Min(maxColumns, DocumentViewerConstants.MaximumMaxPagesAcross);

            //Now we do the following:
            //We iterate through the possible permutations of row and column
            //combinations and choose the arrangement of columns that best fits the current
            //viewport's aspect ratio.
            int minAspectColumns = 1;   //The current optimal number of columns found.
            double minAspectDiff = Double.MaxValue; //The current optimal aspect ratio match
            for (int columns = 1; columns <= maxColumns; columns++)
            {
                //Calculate the number of rows for this arrangment given the current column count
                int rows = (int)Math.Floor((double)(_pageCache.PageCount / columns));

                //Calculate the approximate dimensions that this layout would
                //have.
                double width = pageSize.Width * columns;
                double height = pageSize.Height * rows;

                //Determine the aspect ratio of this layout.
                double layoutAspect = width / height;

                //See if the aspect ratio of this layout is a closer match for our Viewport
                //than previous attempts.
                double aspectDiff = Math.Abs(layoutAspect - viewportAspect);
                if (aspectDiff < minAspectDiff)
                {
                    //It is, so save it.
                    minAspectDiff = aspectDiff;
                    minAspectColumns = columns;
                }
            }

            return minAspectColumns;
        }

        /// <summary>
        /// Calls InvalidateMeasure() on our visual children in order to force them
        /// to be re-measured and arranged on the next layout pass.  This is called
        /// whenever the Scale is changed, as it is only then when re-measuring is
        /// required.  We do this to avoid unnecessary layout/measure passes on our
        /// pages.
        /// </summary>
        private void InvalidateChildMeasure()
        {
            //Get the current Visual Collection, which contains our pages.
            int count = _childrenCollection.Count;

            for (int i = 0; i < count; i++)
            {
                UIElement page = _childrenCollection[i] as UIElement;

                if (page != null)
                {
                    page.InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// Helper function that indicates whether all the pages on the specified
        /// row point to clean cache entries.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private bool RowIsClean(RowInfo row)
        {
            bool clean = true;

            for (int i = row.FirstPage; i < row.FirstPage + row.PageCount; i++)
            {
                if (_pageCache.IsPageDirty(i))
                {
                    clean = false;
                    break;
                }
            }

            return clean;
        }

        /// <summary>
        /// Checks that the current scale factor is optimal for the passed in row.
        /// </summary>
        /// <param name="pivotRow">The Row to pass to the Delegate</param>
        private void EnsureFit(RowInfo pivotRow)
        {
            //Get the scale factor necessary to fit this row into view.
            //If the result is not 1.0 (within a certain margin of error)
            //then we need to re-layout, alas.
            double neededScaleFactor = CalculateScaleFactor(pivotRow);
            double newScale = neededScaleFactor * _rowCache.Scale;

            //If the neededScaleFactor would require DocumentGrid scale the pages
            //below the minimum allowed zoom, or above the maximum, then we won't
            //do anything here.
            if (newScale < CurrentMinimumScale ||
                newScale > DocumentViewerConstants.MaximumScale)
            {
                return;
            }

            if (!DoubleUtil.AreClose(1.0, neededScaleFactor))
            {
                //Rescale the row.
                ApplyViewParameters(pivotRow);

                //Make the row visible again -- the offsets may have
                //changed due to the above rescaling.
                SetVerticalOffsetInternal(pivotRow.VerticalOffset);
            }
        }

        /// <summary>
        /// Given a pivot row and a previously set ViewMode, the scale is adjusted so as
        /// to cause the pivot row to be fit based on the specified ViewMode.
        /// </summary>
        /// <param name="pivotRow"></param>
        private void ApplyViewParameters(RowInfo pivotRow)
        {
            //Update our MaxPagesAcross property to the number of rows on the pivot row
            //if page sizes vary.  (If page sizes are uniform, this value will not change as a result of
            //a layout change)
            if (_pageCache.DynamicPageSizes)
            {
                _maxPagesAcross = pivotRow.PageCount;
            }

            //Get the scale factor necessary to fit the given row into the Viewport.
            double scaleFactor = CalculateScaleFactor(pivotRow);

            //Calculate the new scale. We multiply our scale factor by the old scale factor to cancel out any
            //previously applied scale.
            double newScale = scaleFactor * _rowCache.Scale;

            //Clip the value into the acceptable range
            newScale = Math.Max(newScale, CurrentMinimumScale);
            newScale = Math.Min(newScale, DocumentViewerConstants.MaximumScale);

            //Update the Row Layout's scale.
            UpdateLayoutScale(newScale);
        }

        private double CalculateScaleFactor(RowInfo pivotRow)
        {
            //Determine the dimensions of this row minus any spacing between the pages.
            //We use this as the baseline for our scale factor as page spacing does not scale.
            double rowWidth;

            //If the page sizes vary, we use the width of the pivot row,
            //otherwise we use the overall width of the document (ExtentWidth).
            //(For uniform page sizes, we always use the width of the document, even
            //for the last row which may not have the same width as the rest of the document).
            if (_pageCache.DynamicPageSizes)
            {
                rowWidth = pivotRow.RowSize.Width - pivotRow.PageCount * HorizontalPageSpacing;
            }
            else
            {
                rowWidth = ExtentWidth - MaxPagesAcross * HorizontalPageSpacing;
            }

            double rowHeight = pivotRow.RowSize.Height - VerticalPageSpacing;

            //If we have row dimensions of zero or less, there's no reason to scale anything.
            //So just return 1.0 to indicate no change.
            if (rowWidth <= 0.0 || rowHeight <= 0.0)
            {
                return 1.0;
            }

            //The dimensions of our Viewport minus any spacing.  We use this as the baseline for our
            //scale factor as page spacing does not scale.
            double compensatedViewportWidth;

            if (_pageCache.DynamicPageSizes)
            {
                compensatedViewportWidth = ViewportWidth - pivotRow.PageCount * HorizontalPageSpacing;
            }
            else
            {
                compensatedViewportWidth = ViewportWidth - MaxPagesAcross * HorizontalPageSpacing;
            }

            double compensatedViewportHeight = ViewportHeight - VerticalPageSpacing;

            //If we have no space to display pages, there's nothing to scale.
            //So just return 1.0 to indicate no change.
            if (compensatedViewportWidth <= 0.0 ||
                compensatedViewportHeight <= 0.0)
            {
                return 1.0;
            }

            double scaleFactor = 1.0;

            //Based on the previously determined ViewMode (set in SetColumns(), FitToWidth(), etc..
            //scale the pages appropriately.
            switch (_documentLayout.ViewMode)
            {
                case ViewMode.SetColumns:
                    //We leave the scale factor as is -- this is not a "page-fit" mode.
                    break;

                case ViewMode.FitColumns:
                    //Update the scale factor so that the pivot row is completely visible.
                    scaleFactor = Math.Min(compensatedViewportWidth / rowWidth, compensatedViewportHeight / rowHeight);
                    break;

                case ViewMode.PageWidth:
                    //Update the scale factor so that the pivot row is as wide as the viewport.
                    scaleFactor = compensatedViewportWidth / rowWidth;
                    break;

                case ViewMode.PageHeight:
                    //Update the scale factor so that the pivot row is as tall as the viewport.
                    scaleFactor = compensatedViewportHeight / rowHeight;
                    break;

                case ViewMode.Thumbnails:
                    //Update the scale factor so that the _entire layout_ is completely visible.  As in previous
                    //cases we must compensate for the fact that the spacing between pages does not scale.
                    //However, unlike in previous cases, we must exclude the space between all rows rather
                    //merely one space, so we must recalculate the compensated values.  Furthermore we must
                    //also compensate for the ExtentHeight as well since it includes the spaces.
                    double thumbnailCompensatedExtentHeight = ExtentHeight - VerticalPageSpacing * _rowCache.RowCount;
                    double thumbnailCompensatedViewportHeight = ViewportHeight - VerticalPageSpacing * _rowCache.RowCount;
                    //If we have no space to display pages, there's nothing to scale.
                    //So just return 1.0 to indicate no change.
                    if (thumbnailCompensatedViewportHeight <= 0.0)
                    {
                        scaleFactor = 1.0;
                    }
                    else
                    {
                        scaleFactor = Math.Min(compensatedViewportWidth / rowWidth,
                            thumbnailCompensatedViewportHeight / thumbnailCompensatedExtentHeight);
                    }
                    break;

                case ViewMode.Zoom:
                    //We will not change the scale here, as this is not a "page-fit" mode.
                    break;

                default:
                    throw new InvalidOperationException(SR.Get(SRID.DocumentGridInvalidViewMode));
            }

            return scaleFactor;
        }

        /// <summary>
        /// Creates the caches used by DocumentGrid, and sets default property values.
        /// </summary>
        private void Initialize()
        {
            //Create our caches
            _pageCache = new PageCache();
            _childrenCollection = new VisualCollection(this);

            _rowCache = new RowCache();
            _rowCache.PageCache = _pageCache;
            _rowCache.RowCacheChanged += new RowCacheChangedEventHandler(OnRowCacheChanged);
            _rowCache.RowLayoutCompleted += new RowLayoutCompletedEventHandler(OnRowLayoutCompleted);
        }

        /// <summary>
        /// Updates Scrolling-related properties and calls
        /// InvalidateScrollInfo and InvalidateDocumentScrollInfo on the
        /// ScrollOwner and DocumentScrollOwner, respectively.
        /// </summary>
        private void InvalidateDocumentScrollInfo()
        {
            if (ScrollOwner != null)
            {
                ScrollOwner.InvalidateScrollInfo();
            }

            if (DocumentViewerOwner != null)
            {
                DocumentViewerOwner.InvalidateDocumentScrollInfo();
            }
        }

        /// <summary>
        /// Calls InvalidatePageViews and ApplyTemplate on our DocumentViewer owner
        /// so that the base implementation can keep its collection up to date.
        /// </summary>
        private void InvalidatePageViews()
        {
            Invariant.Assert(DocumentViewerOwner != null, "DocumentViewerOwner cannot be null.");

            if (DocumentViewerOwner != null)
            {
                DocumentViewerOwner.InvalidatePageViewsInternal();
                DocumentViewerOwner.ApplyTemplate();
            }

            //Perf Tracing - InvalidatePageViews
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXInvalidateView);
        }

        /// <summary>
        /// Returns an ITextPointer to the current visible selection, if there is one.
        /// </summary>
        /// <returns>An ITextPointer to the current selection, or null if none exists.</returns>
        private ITextPointer GetVisibleSelection()
        {
            ITextPointer selection = null;

            if (HasSelection())
            {
                ITextPointer tp = TextEditor.Selection.Start;

                //If the TextView contains the selection
                //then the selection is on a visible page.
                if (TextViewContains(tp))
                {
                    selection = tp;
                }
            }

            return selection;
        }

        /// <summary>
        /// Indicates whether a selection (visible or not) has been made.
        /// </summary>
        /// <returns>true if a selection has been made, false otherwise.</returns>
        private bool HasSelection()
        {
            return (TextEditor != null && TextEditor.Selection != null);
        }

        /// <summary>
        /// Gets the page number that the specified ITextPointer to a visible selection
        /// is on.
        /// </summary>
        /// <param name="selection">The TextPointer to find the page number for.</param>
        /// <returns></returns>
        private int GetPageNumberForVisibleSelection(ITextPointer selection)
        {
            Invariant.Assert(TextViewContains(selection));

            //Walk through the current DocumentPageViews and see which one contains the selection.
            foreach (DocumentPageView pageView in _pageViews)
            {
                //Get the TextView for this page.
                DocumentPageTextView textView =
                    ((IServiceProvider)pageView).GetService(typeof(ITextView)) as DocumentPageTextView;

                //If this TextView contains the selection, return the page's number.
                if (textView != null &&
                    textView.IsValid &&
                    textView.Contains(selection))
                {
                    return pageView.PageNumber;
                }
            }

            Invariant.Assert(false, "Selection was in TextView, but not found in any visible page!");
            return 0;
        }

        /// <summary>
        /// Finds the "Active Focus" point:
        /// Either the page that has a Selection/Insertion Point on it,
        /// or lacking that, the center of the viewport.
        /// </summary>
        /// <returns></returns>
        private Point GetActiveFocusPoint()
        {
            ITextPointer tp = GetVisibleSelection();

            if (tp != null && tp.HasValidLayout)
            {
                Rect selectionRect = TextView.GetRectangleFromTextPosition(tp);

                //If the selection rectangle is not empty, then we have a selection or an IP
                if (selectionRect != Rect.Empty)
                {
                    //Return the upper-left corner of the selection.
                    return new Point(selectionRect.Left, selectionRect.Top);
                }
            }

            //No selection, so we default to the upper-left of the viewport.
            return new Point(0.0, 0.0);
        }

        /// <summary>
        /// Returns the page that has "Active Focus," or
        /// the first visible page if there is none.
        /// </summary>
        /// <returns></returns>
        private int GetActiveFocusPage()
        {
            DocumentPageView dp = GetDocumentPageViewFromPoint(GetActiveFocusPoint());

            if (dp != null)
            {
                return dp.PageNumber;
            }

            //No selection, we default to the first visible page.
            return _firstVisiblePageNumber;
        }

        /// <summary>
        /// Given a point onscreen, returns a DocumentPageView that occupies that point,
        /// if any.
        /// </summary>
        /// <param name="point">The point at which to search for a DocumentPageView.</param>
        /// <returns></returns>
        private DocumentPageView GetDocumentPageViewFromPoint(Point point)
        {
            //Hit test to find the DocumentPageView
            HitTestResult result = VisualTreeHelper.HitTest(this, point);
            DependencyObject currentVisual = (result != null) ? result.VisualHit : null;

            DocumentPageView page = null;

            // Traverse the visual parent chain until we encounter a DocumentPageView.
            while (currentVisual != null)
            {
                page = currentVisual as DocumentPageView;
                if (page != null)
                {
                    //We found the DocumentPageView.
                    return page;
                }
                currentVisual = VisualTreeHelper.GetParent(currentVisual);
            }

            //Didn't find one at this point.
            return null;
        }

        /// <summary>
        /// Helper function to safely verify that the TextView contains a given TextPointer.
        /// </summary>
        /// <param name="tp">The TextPointer to check</param>
        /// <returns></returns>
        private bool TextViewContains(ITextPointer tp)
        {
            return (TextView != null &&
                TextView.IsValid &&
                TextView.Contains(tp));
        }

        /// <summary>
        /// Helper function that calculates the Horizontal offset of the given page.
        /// </summary>
        /// <param name="row">The row which the desired page lives on</param>
        /// <param name="pageNumber">The page to find the offset for</param>
        /// <returns>The Horizontal offset of the page in the document.</returns>
        private double GetHorizontalOffsetForPage(RowInfo row, int pageNumber)
        {
            if (row == null)
            {
                throw new ArgumentNullException("row");
            }

            if (pageNumber < row.FirstPage ||
                pageNumber > row.FirstPage + row.PageCount)
            {
                throw new ArgumentOutOfRangeException("pageNumber");
            }

            //Rows are centered if the content has varying page sizes,
            //Left-aligned otherwise.
            double horizontalOffset = _pageCache.DynamicPageSizes ?
                Math.Max(0.0, (ExtentWidth - row.RowSize.Width) / 2.0) : 0.0;

            //Add the widths of the pages (and spacing) prior to this one on the row
            for (int i = row.FirstPage; i < pageNumber; i++)
            {
                Size pageSize = _pageCache.GetPageSize(i);
                horizontalOffset += pageSize.Width * Scale + HorizontalPageSpacing;
            }

            return horizontalOffset;
        }

        /// <summary>
        /// Helper method that determines whether a given RowCacheChange will have
        /// an impact on currently-visible rows.
        /// </summary>
        /// <param name="change"></param>
        /// <returns></returns>
        private bool RowCacheChangeIsVisible(RowCacheChange change)
        {
            int firstVisibleRow = _firstVisibleRow;
            int lastVisibleRow = _firstVisibleRow + _visibleRowCount;

            int firstChangedRow = change.Start;
            int lastChangedRow = change.Start + change.Count;

            //If the first changed row (and hence following changes) are visible OR
            //The last changed row (and hence prior changes) are visible OR
            //if the changes are a super-set of the visible range, then the change is visible.
            if ((firstChangedRow >= firstVisibleRow && firstChangedRow <= lastVisibleRow) ||
                (lastChangedRow >= firstVisibleRow && lastChangedRow <= lastVisibleRow) ||
                (firstChangedRow < firstVisibleRow && lastChangedRow > lastVisibleRow))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the given page's contents have been loaded into the visual tree.
        /// </summary>
        /// <param name="pageNumber">The number of the page to check</param>
        /// <returns></returns>
        private bool IsPageLoaded(int pageNumber)
        {
            DocumentGridPage page = GetDocumentGridPageForPageNumber(pageNumber);

            if (page != null)
            {
                return page.IsPageLoaded;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether every page currently visible has been loaded into the visual tree.
        /// </summary>
        /// <returns></returns>
        private bool IsViewLoaded()
        {
            bool viewIsLoaded = true;

            for (int i = _firstPageVisualIndex; i < _childrenCollection.Count; i++)
            {
                DocumentGridPage dp = _childrenCollection[i] as DocumentGridPage;

                // Check that this page has been loaded; break if not.
                if (dp != null && !dp.IsPageLoaded)
                {
                    viewIsLoaded = false;
                    break;
                }
            }

            return viewIsLoaded;
        }

        /// <summary>
        /// Retrieves a DocumentGridPage from our Visual Tree that has the given page number (if one exists).
        /// </summary>
        /// <param name="pageNumber">The number of the page to get</param>
        /// <returns></returns>
        private DocumentGridPage GetDocumentGridPageForPageNumber(int pageNumber)
        {
            for (int i = _firstPageVisualIndex; i < _childrenCollection.Count; i++)
            {
                DocumentGridPage dp = _childrenCollection[i] as DocumentGridPage;

                if (dp != null && dp.PageNumber == pageNumber)
                {
                    return dp;
                }
            }

            return null;
        }

        #region Event Handlers

        /// <summary>
        /// Handles the RequestBringIntoView routed event in the case where the element to be
        /// brought into view is DocumentGrid itself, as is the case when the TextEditor's IP moves.
        /// In this case we use the incoming target rectangle
        /// and ensure that rect is made visible inside of DocumentGrid.
        /// </summary>
        /// <param name="sender">The sender of this routed event, expected to be a DocumentGrid.</param>
        /// <param name="args">The RequestBringIntoView event args associated with this event.</param>
        private static void OnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs args)
        {
            //We only handle this here if the sender and the target of this event are both the same
            //DocumentGrid.
            DocumentGrid senderGrid = sender as DocumentGrid;
            DocumentGrid targetGrid = args.TargetObject as DocumentGrid;
            if (senderGrid != null && targetGrid != null && senderGrid == targetGrid)
            {
                //Bring the IP into view and mark the event as handled.
                args.Handled = true;
                targetGrid.MakeIPVisible(args.TargetRect);
            }
            else
            {
                args.Handled = false;
            }
        }

        /// <summary>
        /// This event is fired when our parent ScrollViewer's layout has changed(before render).
        /// If we need to ensure that a given row has been properly fit -- if ScrollBars have been hidden/
        /// made visible due to this change then we may need to resize.  We call EnsureFit from here
        /// to make sure that's done.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnScrollChanged(object sender, EventArgs args)
        {
            //Remove our handler.
            if (ScrollOwner != null)
            {
                _scrollChangedEventAttached = false;
                ScrollOwner.ScrollChanged -= new ScrollChangedEventHandler(OnScrollChanged);
            }

            //Ensure that our fit is good for the currently displayed row if we have any.
            if (_rowCache.HasValidLayout)
            {
                EnsureFit(_rowCache.GetRowForPageNumber(FirstVisiblePageNumber));
            }
        }

        /// <summary>
        /// This event is fired after a Zoom change when Layout has completed (but before render).
        /// We make sure the current visible selection is centered onscreen.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnZoomLayoutUpdated(object sender, EventArgs args)
        {
            //Remove the event handler so we don't get called again.
            LayoutUpdated -= new EventHandler(OnZoomLayoutUpdated);

            ITextPointer selection = GetVisibleSelection();

            if (selection != null)
            {
                //Now we make the selection visible.
                MakeRectVisible(TextView.GetRectangleFromTextPosition(
                    selection), true /* alwaysCenter */);
            }
        }

        /// <summary>
        /// When the RowCache is changed for any reason (due to a new layout, or if pages change, etc...)
        /// then we need to invalidate our Measure (so we can pick up any changes to visible pages)
        /// and invalidate our IDocumentScrollInfo parents so they know that something's changed.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private void OnRowCacheChanged(object source, RowCacheChangedEventArgs args)
        {
            //If:
            //1) We have a saved pivot row from a previous RowCacheCompleted event,
            //and
            //2) We've been told to do a "Page-Fit" operation (that is, a non-zoom viewing preference)
            //and
            //3) The pivot row is now "clean" (that is, we know the actual dimensions of all the pages
            //   on the row and we aren't just guessing)
            //Then we can now officially calculate the scale needed in order to fit the given row in the manner
            //chosen.
            if (_savedPivotRow != null &&
                RowIsClean(_savedPivotRow))
            {
                if (_documentLayout.ViewMode != ViewMode.Zoom &&
                    _documentLayout.ViewMode != ViewMode.SetColumns
                    )
                {
                    if (_savedPivotRow.FirstPage < _rowCache.RowCount)
                    {
                        RowInfo newRow = _rowCache.GetRowForPageNumber(_savedPivotRow.FirstPage);

                        //If the new row's dimensions differ, then we need to rescale, otherwise we do nothing.
                        if (newRow.RowSize.Width != _savedPivotRow.RowSize.Width ||
                            newRow.RowSize.Height != _savedPivotRow.RowSize.Height)
                        {
                            //Rescale.
                            ApplyViewParameters(newRow);
                        }

                        //Null out the saved Pivot Row -- we've scaled this row properly now
                        //so we don't need to be concerned with it any longer.
                        _savedPivotRow = null;
                    }
                }
                else
                {
                    // The view is already correct; null out the saved Pivot Row.
                    _savedPivotRow = null;
                }
            }

            //If we're viewing a document with varying page size, we've scrolled since the last layout change
            //and the Width of the document has increased
            //then this means that new, wider pages have just been scrolled into view.
            //If we do nothing here, then the content prior to these new pages will appear to "jump"
            //to the right (because we center the pages within the width of the document.)
            //This jump is jarring and not a good user experience.
            //To prevent this, we adjust the HorizontalOffset such that the content that was previously visible
            //appears at the same position when it is rendered.
            if (_pageCache.DynamicPageSizes &&
                _lastRowChangeVerticalOffset != VerticalOffset &&
                _lastRowChangeExtentWidth < ExtentWidth)
            {
                if (_lastRowChangeExtentWidth != 0.0)
                {
                    //Tweak the HorizontalOffset so that the content does not appear to move.
                    SetHorizontalOffsetInternal(HorizontalOffset + (ExtentWidth - _lastRowChangeExtentWidth) / 2.0);
                }

                _lastRowChangeExtentWidth = ExtentWidth;
            }

            _lastRowChangeVerticalOffset = VerticalOffset;

            //The row cache has been changed.
            //If we're displaying rows that were affected,
            //we need to invalidate our measure so they'll be
            //redrawn.
            for (int i = 0; i < args.Changes.Count; i++)
            {
                RowCacheChange change = args.Changes[i];

                if (RowCacheChangeIsVisible(change))
                {
                    InvalidateMeasure();
                    InvalidateChildMeasure();
                }
            }

            InvalidateDocumentScrollInfo();
        }

        /// <summary>
        /// When a new RowLayout has finished being computed we scale the layout such that it
        /// fits within our window.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private void OnRowLayoutCompleted(object source, RowLayoutCompletedEventArgs args)
        {
            if (args == null)
            {
                return;
            }
            if (args.PivotRowIndex >= _rowCache.RowCount)
            {
                throw new ArgumentOutOfRangeException("args");
            }

            //Get the pivot row
            RowInfo pivotRow = _rowCache.GetRow(args.PivotRowIndex);

            //If this row is not clean, and we're not applying a
            //Zoom to the content then we need to rescale the layout when the
            //pages on this row are retrieved.
            if (!RowIsClean(pivotRow) && _documentLayout.ViewMode != ViewMode.Zoom)
            {
                //Save off this row in case we need to rescale due to
                //dirty cache entries becoming clean (i.e. page sizes changing
                //due to the cached size being an inaccurate guess.)
                //OnRowCacheChanged will check this row to ensure that it gets scaled
                //properly when all the pages on the row become available.
                _savedPivotRow = pivotRow;
            }
            else
            {
                _savedPivotRow = null;
            }

            //Now rescale.  We do this after checking the cleanliness of the row
            //so that _savedPivotRow is properly set before we apply our view parameters.
            //Otherwise the code that relies on it in OnRowCacheChanged (which may be called
            //as a result of calling ApplyViewParameters) may use the wrong row.
            ApplyViewParameters(pivotRow);

            //Now that we've recalculated the row layout, it's time to make the previously-visible
            //content visible again.
            //We do not do this the first time the content is assigned, for two reasons,
            //(similar to the ones described in DocumentViewer.OnDocumentChanged()):
            //  1) If this is the first assignment, then we're already there by default.
            //  2) The user may have specified vertical or horizontal offsets in markup or
            //     otherwise (<DocumentViewer VerticalOffset="1000">) and we need to honor
            //     those settings.
            if (!_firstRowLayout && !_pageJumpAfterLayout)
            {
                MakePageVisible(pivotRow.FirstPage);
            }
            else if (_pageJumpAfterLayout)
            {
                MakePageVisible(_pageJumpAfterLayoutPageNumber);
                _pageJumpAfterLayout = false;
            }

            _firstRowLayout = false;

            //If our view was of a "Fit" type, we need to ensure that the fit is
            //correct -- if the status of Vertical/Horizontal Scrollbars has changed
            //as a result of our view selection then the Viewport size may have changed.
            //If so, our current fit is probably wrong.  We'll attach a ScrollChanged handler
            //to our ScrollOwner and when the event is invoked (after layout, but before rendering)
            //we'll check.
            //This is "Step 2" of the two-pass layout necessary to do fit properly inside of a
            //ScrollViewer.
            if (!_scrollChangedEventAttached &&
                ScrollOwner != null &&
                _documentLayout.ViewMode != ViewMode.Zoom &&
                _documentLayout.ViewMode != ViewMode.SetColumns)
            {
                _scrollChangedEventAttached = true;
                ScrollOwner.ScrollChanged += new ScrollChangedEventHandler(OnScrollChanged);
            }
        }

        #endregion Event Handlers

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------
        #region Private Properties

        /// <summary>
        /// Indicates that our Viewport is or is not exactly (0,0).
        /// </summary>
        /// <value></value>
        private bool IsViewportNonzero
        {
            get
            {
                return (ViewportWidth != 0.0 && ViewportHeight != 0.0);
            }
        }

        /// <summary>
        /// Provides access to DocumentViewer's TextEditor.
        /// </summary>
        private TextEditor TextEditor
        {
            get
            {
                if (DocumentViewerOwner != null)
                {
                    return DocumentViewerOwner.TextEditor;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Represents the number of pixels to scroll by when using the
        /// Mouse Wheel; based on System.Parameters.WheelScrollLines.
        /// </summary>
        private double MouseWheelVerticalScrollAmount
        {
            get
            {
                //SystemParameters.WheelScrollLines indicates the number of lines to
                //scroll when the wheel is moved one "click," we multiply this by
                //our scroll amount to get the number of pixels to move.
                return _verticalLineScrollAmount * SystemParameters.WheelScrollLines;
            }
        }

        private bool CanMouseWheelVerticallyScroll
        {
            get { return _canVerticallyScroll && SystemParameters.WheelScrollLines > 0; }
        }

        /// <summary>
        /// Represents the number of pixels to scroll by when using the
        /// Mouse Wheel; based on System.Parameters.WheelScrollLines.
        /// </summary>
        private double MouseWheelHorizontalScrollAmount
        {
            get
            {
                //SystemParameters.WheelScrollLines indicates the number of lines to
                //scroll when the wheel is moved one "click," we multiply this by
                //our scroll amount to get the number of pixels to move.
                return _horizontalLineScrollAmount * SystemParameters.WheelScrollLines;
            }
        }

        private bool CanMouseWheelHorizontallyScroll
        {
            get { return _canHorizontallyScroll && SystemParameters.WheelScrollLines > 0; }
        }

        /// <summary>
        /// Returns the minimum allowed scale based on the current view mode.
        /// Thumbnails mode has a higher minimum than other views.
        /// </summary>
        private double CurrentMinimumScale
        {
            get
            {
                return _documentLayout.ViewMode == ViewMode.Thumbnails ?
                  DocumentViewerConstants.MinimumThumbnailsScale :
                  DocumentViewerConstants.MinimumScale;
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Our Caches
        private PageCache _pageCache;
        private RowCache _rowCache;

        // Our collection of currently-displayed pages.
        private ReadOnlyCollection<DocumentPageView> _pageViews;

        // Data for Properties
        private bool _canHorizontallyScroll;
        private bool _canVerticallyScroll;
        private double _verticalOffset;
        private double _horizontalOffset;
        private double _viewportHeight;
        private double _viewportWidth;
        private int _firstVisibleRow;
        private int _visibleRowCount;
        private int _firstVisiblePageNumber;
        private int _lastVisiblePageNumber;
        private ScrollViewer _scrollOwner;
        private DocumentViewer _documentViewerOwner;
        private bool _showPageBorders = true;
        private bool _lockViewModes;
        private int _maxPagesAcross = 1;

        // The previous constraint passed to ArrangeCore.
        private Size _previousConstraint;

        // The viewing mode (columns, fit, etc...) last used to request a layout.
        private DocumentLayout _documentLayout =
            new DocumentLayout(1, ViewMode.SetColumns);
        private int _documentLayoutsPending;

        // The pivot rows last used to form the basis of a layout.
        private RowInfo _savedPivotRow;

        // The last ExtentWidth and VerticalOffsets encountered in
        // OnRowCacheChanged, used to determine whether to tweak the HorizontalOffset.
        private double _lastRowChangeExtentWidth;
        private double _lastRowChangeVerticalOffset;

        // Editing
        private ITextContainer _textContainer;

        // RubberBand selector used for rubberband selection.
        private RubberbandSelector _rubberBandSelector;

        // Flags
        private bool _isLayoutRequested;  //Whether we have requested a layout from the RowCache.
        private bool _pageJumpAfterLayout;   //Whether we need to bring a page into view after layout
        private int _pageJumpAfterLayoutPageNumber; //The page to jump to after layout
        private bool _firstRowLayout = true;
        private bool _scrollChangedEventAttached; //Whether or not we've attached a ScrollChanged event to our ScrollViewer.

        // We create a Border with a transparent background so that it can
        // participate in Hit-Testing (which allows click events like those
        // for our Context Menu to work).  This border is displayed behind
        // the displayed pages so that "dead space" surrounding the pages can
        // be clicked on.
        private Border _documentGridBackground;
        private const int _backgroundVisualIndex = 0;
        private const int _firstPageVisualIndex = 1;

        //Constants for MeasureCore constraints
        //We use this size if we're placed inside a "Size-To-Parent" container like
        //ScrollViewer or StackPanel and are given Infinite constraints.
        private readonly Size _defaultConstraint = new Size(250.0, 250.0);

        //Store all our visual children (pages) here
        private VisualCollection _childrenCollection;

        //Information for MakeVisible operations involving pages that are not
        //yet visible.
        private int _makeVisiblePageNeeded = -1;
        private DispatcherOperation _makeVisibleDispatcher;

        //DispatcherOperations used for executing time-consuming property changes in the background.
        private DispatcherOperation _setScaleOperation;

        //Delegate used for BringPageIntoView.
        private delegate void BringPageIntoViewCallback(MakeVisibleData data, int pageNumber);

        /// <summary>
        /// Represents a state in the Visual tree merging state machine
        /// used in RecalcVisiblePages.
        /// </summary>
        private enum VisualTreeModificationState
        {
            /// <summary>
            /// Inserting pages before existing
            /// </summary>
            BeforeExisting = 0,

            /// <summary>
            /// Scanning through existing pages
            /// </summary>
            DuringExisting,

            /// <summary>
            /// Adding pages after existing
            /// </summary>
            AfterExisting
        }

        /// <summary>
        /// Represents a layout mode specified by SetColumns,
        /// FitToPage, FitToWidth, etc...
        /// </summary>
        private enum ViewMode
        {
            /// <summary>
            /// A request to lay out a specified number of columns was made.
            /// </summary>
            SetColumns = 0,

            /// <summary>
            /// A request to make the specified number of columns visible was made.
            /// </summary>
            FitColumns,

            /// <summary>
            /// A request for a fit-to-page-width view was made.
            /// </summary>
            PageWidth,

            /// <summary>
            /// A request for a fit-to-page-height view was made.
            /// </summary>
            PageHeight,

            /// <summary>
            /// A request for a thumbnail view was made.
            /// </summary>
            Thumbnails,

            /// <summary>
            /// A request for a non "page-fit" view was made.
            /// </summary>
            Zoom,

            /// <summary>
            /// A request for the HorizontalOffset to be updated.
            /// </summary>
            SetHorizontalOffset,

            /// <summary>
            /// A request for the VerticalOffset to be updated.
            /// </summary>
            SetVerticalOffset
        }

        /// <summary>
        /// Represents a particular document layout --
        /// includes the number of Columns to view and the
        /// mode to view them in.
        /// </summary>
        private class DocumentLayout
        {
            public DocumentLayout(int columns, ViewMode viewMode)
                : this(columns, 0.0 /* default */, viewMode) { }

            public DocumentLayout(double offset, ViewMode viewMode)
                : this(1 /* default */, offset, viewMode) { }

            public DocumentLayout(int columns, double offset, ViewMode viewMode)
            {
                _columns = columns;
                _offset = offset;
                _viewMode = viewMode;
            }

            /// <summary>
            /// The ViewMode to apply to the layout.
            /// </summary>
            public ViewMode ViewMode
            {
                set { _viewMode = value; }
                get { return _viewMode; }
            }

            /// <summary>
            /// The number of columns for the layout.
            /// </summary>
            public int Columns
            {
                set { _columns = value; }
                get { return _columns; }
            }

            /// <summary>
            /// The offset (horizontal of vertical) for the layout.
            /// </summary>
            public double Offset
            {
                // Set not currently used.
                // set { _offset = value; }
                get { return _offset; }
            }

            private ViewMode _viewMode;
            private int _columns;
            private double _offset;
        }

        /// <summary>
        /// An MakeVisibleData object contains data and operation information
        /// related to asynchronous MakeVisible operations.
        /// </summary>
        private struct MakeVisibleData
        {
            /// <summary>
            /// Constructs a new MakeVisibleData object
            /// </summary>
            /// <param name="visual">A visual to be made visible.</param>
            /// <param name="contentPosition">A ContentPosition to be made visible.</param>
            /// <param name="rect">Any bounding rect to be made visible.</param>
            public MakeVisibleData(Visual visual, ContentPosition contentPosition, Rect rect)
            {
                _visual = visual;
                _contentPosition = contentPosition;
                _rect = rect;
            }

            /// <summary>
            /// The Visual to be made visible
            /// </summary>
            public Visual Visual
            {
                get { return _visual; }
            }

            /// <summary>
            /// The ContentPosition to be made Visible
            /// </summary>
            public ContentPosition ContentPosition
            {
                get { return _contentPosition; }
            }

            /// <summary>
            /// The bounding rectangle to be made visible
            /// </summary>
            public Rect Rect
            {
                get { return _rect; }
            }

            private Visual _visual;
            private ContentPosition _contentPosition;
            private Rect _rect;
        }

        //Constants for line scrolling amounts
        private const double _verticalLineScrollAmount = 16.0;
        private const double _horizontalLineScrollAmount = 16.0;

        #endregion Private Fields
    }
}

