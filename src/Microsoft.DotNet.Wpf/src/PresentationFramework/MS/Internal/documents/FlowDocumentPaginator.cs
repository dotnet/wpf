// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: DynamicDocumentPaginator associated with FlowDocument.
//

using System;                       // Object
using System.Collections.Generic;   // List<T>
using System.ComponentModel;        // AsyncCompletedEventArgs
using System.Windows;               // Size
using System.Windows.Documents;     // DocumentPaginator
using System.Windows.Media;         // Visual
using System.Windows.Threading;     // DispatcherOperationCallback
using MS.Internal.PtsHost;          // BreakRecordTable, FlowDocumentPage
using MS.Internal.Text;             // DynamicPropertyReader

namespace MS.Internal.Documents
{
    /// <summary>
    /// Delegate indicating when entire break record has been invalidated.
    /// </summary>
    internal delegate void BreakRecordTableInvalidatedEventHandler(object sender, EventArgs e);

    /// <summary>
    /// DynamicDocumentPaginator associated with FlowDocument.
    /// </summary>
    internal class FlowDocumentPaginator : DynamicDocumentPaginator, IServiceProvider, IFlowDocumentFormatter
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        internal FlowDocumentPaginator(FlowDocument document)
        {
            _pageSize = _defaultPageSize;
            _document = document;
            _brt = new BreakRecordTable(this);
            _dispatcherObject = new CustomDispatcherObject();

            // Background pagination by default is enabled.
            _backgroundPagination = true;
            InitiateNextAsyncOperation();
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods


        /// <summary>
        /// Async version of <see cref="DocumentPaginator.GetPage"/>
        /// </summary>
        /// <param name="pageNumber">Page number.</param>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Throws ArgumentOutOfRangeException if PageNumber is negative.
        /// </exception>
        public override void GetPageAsync(int pageNumber, object userState)
        {
            // Page number cannot be negative.
            if (pageNumber < 0)
            {
                throw new ArgumentOutOfRangeException("pageNumber", SR.Get(SRID.IDPNegativePageNumber));
            }

            // Reentrancy check.
            if (_document.StructuralCache.IsFormattingInProgress)
            {
                throw new InvalidOperationException(SR.Get(SRID.FlowDocumentFormattingReentrancy));
            }
            if (_document.StructuralCache.IsContentChangeInProgress)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextContainerChangingReentrancyInvalid));
            }

            DocumentPage page = null;

            if (!_backgroundPagination)
            {
                page = GetPage(pageNumber);
            }
            else
            {
                // If entire content has been already pre-paginated (BreakRecordTable is clean)
                // and requesting non-existing page number, return DocumentPage.Missing.
                if (_brt.IsClean && !_brt.HasPageBreakRecord(pageNumber))
                {
                    page = DocumentPage.Missing;
                }

                if (_brt.HasPageBreakRecord(pageNumber))
                {
                    page = GetPage(pageNumber);
                }

                if (page == null)
                {
                    _asyncRequests.Add(new GetPageAsyncRequest(pageNumber, userState, this));
                    InitiateNextAsyncOperation();
                }
            }

            if (page != null)
            {
                OnGetPageCompleted(new GetPageCompletedEventArgs(page, pageNumber, null, false, userState));
            }
        }

        /// <summary>
        /// Retrieves the DocumentPage for the given page number. PageNumber
        /// is zero-based.
        /// </summary>
        /// <param name="pageNumber">Page number.</param>
        /// <returns>
        /// Returns DocumentPage.Missing if the given page does not exist.
        /// </returns>
        /// <remarks>
        /// Multiple requests for the same page number may return the same
        /// object (this is implementation specific).
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Throws ArgumentOutOfRangeException if PageNumber is negative.
        /// </exception>
        public override DocumentPage GetPage(int pageNumber)
        {
            DocumentPage page;

            // Ensure usage from just one Dispatcher object.
            // FlowDocumentPaginator runs its own layout, hence there is a need
            // to protect it from random access from other threads.
            _dispatcherObject.VerifyAccess();

            // Page number cannot be negative.
            if (pageNumber < 0)
            {
                throw new ArgumentOutOfRangeException("pageNumber", SR.Get(SRID.IDPNegativePageNumber));
            }

            // Reentrancy check.
            if (_document.StructuralCache.IsFormattingInProgress)
            {
                throw new InvalidOperationException(SR.Get(SRID.FlowDocumentFormattingReentrancy));
            }
            if (_document.StructuralCache.IsContentChangeInProgress)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextContainerChangingReentrancyInvalid));
            }

            // Disable processing of the queue during blocking operations to prevent unrelated reentrancy.
            using (_document.Dispatcher.DisableProcessing())
            {
                _document.StructuralCache.IsFormattingInProgress = true; // Set reentrancy flag.
                try
                {
                    // If entire content has been already pre-paginated (BreakRecordTable is clean)
                    // and requesting non-existing page number, return DocumentPage.Missing.
                    if (_brt.IsClean && !_brt.HasPageBreakRecord(pageNumber))
                    {
                        page = DocumentPage.Missing;
                    }
                    else
                    {
                        // If the DocumentPage is cached in the BreakRecordTable, use it.
                        page = _brt.GetCachedDocumentPage(pageNumber);
                        if (page == null)
                        {
                            // If requested page number does not have pre-calculated BreakRecord,
                            // do synchronous pagination up to the requested page number.
                            // [Synchronous pagination is done here, because GetPage is a sync operation.]
                            if (!_brt.HasPageBreakRecord(pageNumber))
                            {
                                page = FormatPagesTill(pageNumber);
                            }
                            // If requested page number does have pre-calculated BreakRecord,
                            // format a single page.
                            else
                            {
                                page = FormatPage(pageNumber);
                            }
                        }
                    }
                }
                finally
                {
                    _document.StructuralCache.IsFormattingInProgress = false; // Clear reentrancy flag.
                }
            }

            return page;
        }


        /// <summary>
        /// Async version of <see cref="DynamicDocumentPaginator.GetPageNumber"/>
        /// </summary>
        /// <param name="contentPosition">Content position.</param>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        /// <exception cref="ArgumentException">
        /// Throws ArgumentException if the ContentPosition does not exist within
        /// this element’s tree.
        /// </exception>
        public override void GetPageNumberAsync(ContentPosition contentPosition, object userState)
        {
            // Content position cannot be null.
            if (contentPosition == null)
            {
                throw new ArgumentNullException("contentPosition");
            }
            // Content position cannot be Missing.
            if (contentPosition == ContentPosition.Missing)
            {
                throw new ArgumentException(SR.Get(SRID.IDPInvalidContentPosition), "contentPosition");
            }

            // ContentPosition must be of appropriate type and must be part of
            // the content.
            TextPointer flowContentPosition = contentPosition as TextPointer;
            if (flowContentPosition == null)
            {
                throw new ArgumentException(SR.Get(SRID.IDPInvalidContentPosition), "contentPosition");
            }
            if (flowContentPosition.TextContainer != _document.StructuralCache.TextContainer)
            {
                throw new ArgumentException(SR.Get(SRID.IDPInvalidContentPosition), "contentPosition");
            }

            int pageNumber = 0;

            if (!_backgroundPagination)
            {
                pageNumber = GetPageNumber(contentPosition);
                OnGetPageNumberCompleted(new GetPageNumberCompletedEventArgs(contentPosition, pageNumber, null, false, userState));
            }
            else
            {
                if (_brt.GetPageNumberForContentPosition(flowContentPosition, ref pageNumber))
                {
                    OnGetPageNumberCompleted(new GetPageNumberCompletedEventArgs(contentPosition, pageNumber, null, false, userState));
                }
                else
                {
                    _asyncRequests.Add(new GetPageNumberAsyncRequest(flowContentPosition, userState, this));
                    InitiateNextAsyncOperation();
                }
            }
        }


        /// <summary>
        /// Returns the page number on which the ContentPosition appears.
        /// </summary>
        /// <param name="contentPosition">Content position.</param>
        /// <returns>
        /// Returns the page number on which the ContentPosition appears.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Throws ArgumentException if the ContentPosition does not exist within
        /// this element's tree.
        /// </exception>
        public override int GetPageNumber(ContentPosition contentPosition)
        {
            TextPointer flowContentPosition;
            int pageNumber;

            // Ensure usage from just one Dispatcher object.
            // FlowDocumentPaginator runs its own layout, hence there is a need
            // to protect it from random access from other threads.
            _dispatcherObject.VerifyAccess();

            // ContentPosition cannot be null.
            if (contentPosition == null)
            {
                throw new ArgumentNullException("contentPosition");
            }
            // ContentPosition must be of appropriate type and must be part of
            // the content.
            flowContentPosition = contentPosition as TextPointer;
            if (flowContentPosition == null)
            {
                throw new ArgumentException(SR.Get(SRID.IDPInvalidContentPosition), "contentPosition");
            }
            if (flowContentPosition.TextContainer != _document.StructuralCache.TextContainer)
            {
                throw new ArgumentException(SR.Get(SRID.IDPInvalidContentPosition), "contentPosition");
            }

            // We are about to perform synchronous pagination, so need to check for
            // reentrancy.
            if (_document.StructuralCache.IsFormattingInProgress)
            {
                throw new InvalidOperationException(SR.Get(SRID.FlowDocumentFormattingReentrancy));
            }
            if (_document.StructuralCache.IsContentChangeInProgress)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextContainerChangingReentrancyInvalid));
            }

            // Disable processing of the queue during blocking operations to prevent unrelated reentrancy.
            using (_document.Dispatcher.DisableProcessing())
            {
                _document.StructuralCache.IsFormattingInProgress = true; // Set reentrancy flag.
                pageNumber = 0;
                try
                {
                    while (!_brt.GetPageNumberForContentPosition(flowContentPosition, ref pageNumber))
                    {
                        // If failed to get PageNumber and BreakRecordTable is clean,
                        // the input ContentPosition does not belong to the content.
                        // But according to check above, it does belong to the content.
                        // Break and return -1 in this case
                        if (_brt.IsClean)
                        {
                            pageNumber = -1;
                            break;
                        }

                        // Do synchronous pagination for the next missing page number.
                        FormatPage(pageNumber);
                    }
                }
                finally
                {
                    _document.StructuralCache.IsFormattingInProgress = false; // Clear reentrancy flag.
                }
            }

            return pageNumber;
        }

        /// <summary>
        /// Returns the ContentPosition for the given page.
        /// </summary>
        /// <param name="page">Document page.</param>
        /// <returns>Returns the ContentPosition for the given page.</returns>
        /// <exception cref="ArgumentException">
        /// Throws ArgumentException if the page is not valid.
        /// </exception>
        public override ContentPosition GetPagePosition(DocumentPage page)
        {
            FlowDocumentPage flowDocumentPage;
            ITextView textView;
            ITextPointer position;
            Point point, newPoint;
            MatrixTransform transform;

            // Ensure usage from just one Dispatcher object.
            // FlowDocumentPaginator runs its own layout, hence there is a need
            // to protect it from random access from other threads.
            _dispatcherObject.VerifyAccess();

            // ContentPosition cannot be null.
            if (page == null)
            {
                throw new ArgumentNullException("page");
            }
            // DocumentPage must be of appropriate type.
            flowDocumentPage = page as FlowDocumentPage;
            if (flowDocumentPage == null || flowDocumentPage.IsDisposed)
            {
                return ContentPosition.Missing;
            }

            // DocumentPage.Visual for printing scenarions needs to be always returned
            // in LeftToRight FlowDirection. Hence, if the document is RightToLeft,
            // mirroring transform need to be applied to the content of DocumentPage.Visual.
            point = new Point(0, 0);
            if (_document.FlowDirection == FlowDirection.RightToLeft)
            {
                transform = new MatrixTransform(-1.0, 0.0, 0.0, 1.0, flowDocumentPage.Size.Width, 0.0);
                transform.TryTransform(point, out newPoint);
                point = newPoint;
            }

            // Get TextView for DocumentPage. Position of the page is calculated through hittesting
            // the top-left of the page. If position cannot be found, the start position of
            // the first range for TextView is treated as ContentPosition for the page.
            textView = (ITextView)((IServiceProvider)flowDocumentPage).GetService(typeof(ITextView));
            Invariant.Assert(textView != null, "Cannot access ITextView for FlowDocumentPage.");

            //Invariant.Assert(textView.TextSegments.Count > 0, "Page cannot be empty.");
            // It is not necessarily WPF's fault if there are no TextSegments.  
            // We have seen examples where PTS aborts the formatting because the content
            // exceeds its capacity.  Rather than crashing in this case, limp along
            // as if the position couldn't be determined.
            if (textView.TextSegments.Count == 0)
            {
                return ContentPosition.Missing;
            }

            position = textView.GetTextPositionFromPoint(point, true);
            if (position == null)
            {
                position = textView.TextSegments[0].Start;
            }
            return (position is TextPointer) ? (ContentPosition)position : ContentPosition.Missing;
        }

        /// <summary>
        /// Returns the ContentPosition for an object within the content.
        /// </summary>
        /// <param name="o">Object within this element's tree.</param>
        /// <returns>Returns the ContentPosition for an object within the content.</returns>
        /// <exception cref="ArgumentException">
        /// Throws ArgumentException if the object does not exist within this element's tree.
        /// </exception>
        public override ContentPosition GetObjectPosition(Object o)
        {
            // Ensure usage from just one Dispatcher object.
            // FlowDocumentPaginator runs its own layout, hence there is a need
            // to protect it from random access from other threads.
            _dispatcherObject.VerifyAccess();

            // Object cannot be null.
            if (o == null)
            {
                throw new ArgumentNullException("o");
            }
            return _document.GetObjectPosition(o);
        }

        /// <summary>
        /// Cancels all asynchronous calls made with the given userState.
        /// If userState is null, all asynchronous calls are cancelled.
        /// </summary>
        /// <param name="userState">Unique identifier for the asynchronous task.</param>
        public override void CancelAsync(object userState)
        {
            if (userState == null)
            {
                CancelAllAsyncOperations();
            }
            else
            {
                for (int index = 0; index < _asyncRequests.Count; index++)
                {
                    AsyncRequest asyncRequest = _asyncRequests[index];

                    if (asyncRequest.UserState == userState)
                    {
                        asyncRequest.Cancel();
                        _asyncRequests.RemoveAt(index);
                        index--;
                    }
                }
            }
        }


        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Whether PageCount is currently valid. If False, then the value of
        /// PageCount is the number of pages that have currently been formatted.
        /// </summary>
        /// <remarks>
        /// This value may revert to False after being True, in cases where
        /// PageSize or content changes, forcing a repagination.
        /// </remarks>
        public override bool IsPageCountValid
        {
            get
            {
                // Ensure usage from just one Dispatcher object.
                // FlowDocumentPaginator runs its own layout, hence there is a need
                // to protect it from random access from other threads.
                _dispatcherObject.VerifyAccess();

                return _brt.IsClean;
            }
        }

        /// <summary>
        /// If IsPageCountValid is True, this value is the number of pages
        /// of content. If False, this is the number of pages that have
        /// currently been formatted.
        /// </summary>
        /// <remarks>
        /// Value may change depending upon changes in PageSize or content changes.
        /// </remarks>
        public override int PageCount
        {
            get
            {
                // Ensure usage from just one Dispatcher object.
                // FlowDocumentPaginator runs its own layout, hence there is a need
                // to protect it from random access from other threads.
                _dispatcherObject.VerifyAccess();

                return _brt.Count;
            }
        }

        /// <summary>
        /// The suggested size for formatting pages.
        /// </summary>
        /// <remarks>
        /// Note that the paginator may override the specified page size. Users
        /// should check DocumentPage.Size.
        /// </remarks>
        public override Size PageSize
        {
            get
            {
                return _pageSize;
            }
            set
            {
                // Ensure usage from just one Dispatcher object.
                // FlowDocumentPaginator runs its own layout, hence there is a need
                // to protect it from random access from other threads.
                _dispatcherObject.VerifyAccess();

                Size newPageSize = value;
                if (DoubleUtil.IsNaN(newPageSize.Width))
                {
                    newPageSize.Width = _defaultPageSize.Width;
                }
                if (DoubleUtil.IsNaN(newPageSize.Height))
                {
                    newPageSize.Height = _defaultPageSize.Height;
                }
                Size oldActualSize = ComputePageSize();
                _pageSize = newPageSize;
                Size newActualSize = ComputePageSize();
                if (!DoubleUtil.AreClose(oldActualSize, newActualSize))
                {
                    // Detect invalid content change operations.
                    if (_document.StructuralCache.IsFormattingInProgress)
                    {
                        _document.StructuralCache.OnInvalidOperationDetected();
                        throw new InvalidOperationException(SR.Get(SRID.FlowDocumentInvalidContnetChange));
                    }

                    // Any change of page metrics invalidates entire break record table.
                    // Hence page metrics change is treated in the same way as ContentChanged
                    // spanning entire content.
                    // NOTE: May execute external code, so it is possible to get
                    //       an exception here.
                    InvalidateBRT();
                }
            }
        }

        /// <summary>
        /// Whether content is paginated in the background.
        /// When True, the Paginator will paginate its content in the background,
        /// firing the PaginationCompleted and PaginationProgress events as appropriate.
        /// Background pagination begins immediately when set to True. If the
        /// PageSize is modified and this property is set to True, then all pages
        /// will be repaginated and existing pages may be destroyed.
        /// The default value is False.
        /// </summary>
        public override bool IsBackgroundPaginationEnabled
        {
            get
            {
                return _backgroundPagination;
            }
            set
            {
                // Ensure usage from just one Dispatcher object.
                // FlowDocumentPaginator runs its own layout, hence there is a need
                // to protect it from random access from other threads.
                _dispatcherObject.VerifyAccess();

                if (value != _backgroundPagination)
                {
                    _backgroundPagination = value;
                    InitiateNextAsyncOperation();
                }

                if (!_backgroundPagination)
                {
                    CancelAllAsyncOperations();
                }
            }
        }

        /// <summary>
        /// <see cref="DocumentPaginator.Source"/>
        /// </summary>
        public override IDocumentPaginatorSource Source
        {
            get { return _document; }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Initiate the next async operation.
        /// </summary>
        internal void InitiateNextAsyncOperation()
        {
            // Do background pagination if it is enabled and BreakRecordTable is not clean or async requests are pending
            if (_backgroundPagination && _backgroundPaginationOperation == null && (!_brt.IsClean || _asyncRequests.Count > 0))
            {
                _backgroundPaginationOperation = _document.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(OnBackgroundPagination), this);
            }
        }

        /// <summary>
        /// Cancels all pending async operations.
        /// </summary>
        internal void CancelAllAsyncOperations()
        {
            for (int index = 0; index < _asyncRequests.Count; index++)
            {
                _asyncRequests[index].Cancel();
            }

            _asyncRequests.Clear();
        }

        /// <summary>
        /// Raise PagesChanged event.
        /// </summary>
        internal void OnPagesChanged(int pageStart, int pageCount)
        {
            OnPagesChanged(new PagesChangedEventArgs(pageStart, pageCount));
        }

        /// <summary>
        /// Asynchronously raise PaginationProgress event.
        /// </summary>
        /// <param name="pageStart"></param>
        /// <param name="pageCount"></param>
        internal void OnPaginationProgress(int pageStart, int pageCount)
        {
            OnPaginationProgress(new PaginationProgressEventArgs(pageStart, pageCount));
        }

        /// <summary>
        /// Asynchronously raise PaginationCompleted event.
        /// </summary>
        internal void OnPaginationCompleted()
        {
            OnPaginationCompleted(EventArgs.Empty);
        }

        #endregion Internal Methods

        #region Internal Events

        /// <summary>
        /// Fired when all break records in the BreakRecordTable are invalidated.
        /// </summary>
        internal event BreakRecordTableInvalidatedEventHandler BreakRecordTableInvalidated;

        #endregion Internal Events

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods


        /// <summary>
        /// Invalidates the content of the entire break record table.
        /// </summary>
        private void InvalidateBRT()
        {
            if (BreakRecordTableInvalidated != null)
            {
                BreakRecordTableInvalidated(this, EventArgs.Empty);
            }

            _brt.OnInvalidateLayout();
        }

        /// <summary>
        /// Invalidates a subset of the break record table, no event is fired
        /// </summary>
        private void InvalidateBRTLayout(ITextPointer start, ITextPointer end)
        {
            _brt.OnInvalidateLayout(start, end);
        }


        /// <summary>
        /// Format pages up to a page identified by the pageNumber parameter.
        /// </summary>
        private DocumentPage FormatPagesTill(int pageNumber)
        {
            // Pre-calculate all BreakRecords up to the point where the input
            // BreakRecord for specified pageNumber is available.
            // Stop when required BreakRecord is available or entire BreakRecordTable is clean.
            while (!_brt.HasPageBreakRecord(pageNumber) && !_brt.IsClean)
            {
                // Get the first invalid entry in the BreakRecordTable, calculate
                // BreakRecord for it and update BreakRecordTable with the calculated
                // value.
                FormatPage(_brt.Count);
            }

            // If entire BreakRecordTable is clean, the page is not available.
            if (_brt.IsClean)
            {
                return DocumentPage.Missing;
            }

            // The input BreakRecord for the specified page number is already available.
            // Format the requested page.
            return FormatPage(pageNumber);
        }

        /// <summary>
        /// Format the page identified by the pageNumber parameter.
        /// </summary>
        private DocumentPage FormatPage(int pageNumber)
        {
            FlowDocumentPage page;
            PageBreakRecord breakRecordIn, breakRecordOut;
            Thickness pageMargin;
            Size pageSize;

            Invariant.Assert(_brt.HasPageBreakRecord(pageNumber), "BreakRecord for specified page number does not exist.");

            breakRecordIn = _brt.GetPageBreakRecord(pageNumber);
            page = new FlowDocumentPage(_document.StructuralCache);
            pageSize = ComputePageSize();
            pageMargin = _document.ComputePageMargin();

            breakRecordOut = page.FormatFinite(pageSize, pageMargin, breakRecordIn);
            page.Arrange(pageSize);

            // NOTE: May execute external code, so it is possible to get
            //       an exception here.
            _brt.UpdateEntry(pageNumber, page, breakRecordOut, page.DependentMax);
            return page;
        }

        /// <summary>
        /// Partially fill out BreakRecordTable by pre-calculating BreakRecords.
        /// This callback is invoked when background pagination is enabled and
        /// BreakRecordTable is not completely updated yet.
        /// </summary>
        private object OnBackgroundPagination(object arg)
        {
            DateTime dtStart = DateTime.Now;
            DateTime dtStop;

            _backgroundPaginationOperation = null; // Clear out pending request.

            // Ensure usage from just one Dispatcher object.
            // FlowDocumentPaginator runs its own layout, hence there is a need
            // to protect it from random access from other threads.
            _dispatcherObject.VerifyAccess();

            // Detect reentrancy.
            if (_document.StructuralCache.IsFormattingInProgress)
            {
                throw new InvalidOperationException(SR.Get(SRID.FlowDocumentFormattingReentrancy));
            }

            // Ignore this formatting request, if the element was already disposed.
            if (_document.StructuralCache.PtsContext.Disposed)
            {
                return null;
            }


            // Disable processing of the queue during blocking operations to prevent unrelated reentrancy.
            using (_document.Dispatcher.DisableProcessing())
            {
                _document.StructuralCache.IsFormattingInProgress = true; // Set reentrancy flag
                try
                {
                    for (int index = 0; index < _asyncRequests.Count; index++)
                    {
                        AsyncRequest asyncRequest = _asyncRequests[index];

                        if (asyncRequest.Process())
                        {
                            _asyncRequests.RemoveAt(index);
                            index--; // Offset the index add
                        }
                    }

                    dtStop = DateTime.Now;

                    if (_backgroundPagination && !_brt.IsClean)
                    {
                        // Calculate BreakRecords until entire content is calculated or
                        // specific time span has been exceeded (_paginationTimeout).
                        while (!_brt.IsClean)
                        {
                            // Get the first invalid entry in the BreakRecordTable, calculate
                            // BreakRecord for it and update BreakRecordTable with the calculated
                            // value.
                            FormatPage(_brt.Count);

                            // Update time span.
                            dtStop = DateTime.Now;
                            long timeSpan = (dtStop.Ticks - dtStart.Ticks) / TimeSpan.TicksPerMillisecond;

                            if (timeSpan > _paginationTimeout)
                            {
                                break;
                            }
                        }

                        // Initiate the next async operation.
                        InitiateNextAsyncOperation();
                    }
                }
                finally
                {
                    _document.StructuralCache.IsFormattingInProgress = false;    // Clear reentrancy flag.
                }
            }

            return null;
        }

        /// <summary>
        /// Compute size for the page.
        /// </summary>
        private Size ComputePageSize()
        {
            double max, min;
            Size pageSize = new Size(_document.PageWidth, _document.PageHeight);
            if (DoubleUtil.IsNaN(pageSize.Width))
            {
                pageSize.Width = _pageSize.Width;
                max = _document.MaxPageWidth;
                if (pageSize.Width > max)
                {
                    pageSize.Width = max;
                }
                min = _document.MinPageWidth;
                if (pageSize.Width < min)
                {
                    pageSize.Width = min;
                }
            }
            if (DoubleUtil.IsNaN(pageSize.Height))
            {
                pageSize.Height = _pageSize.Height;
                max = _document.MaxPageHeight;
                if (pageSize.Height > max)
                {
                    pageSize.Height = max;
                }
                min = _document.MinPageHeight;
                if (pageSize.Height < min)
                {
                    pageSize.Height = min;
                }
            }
            return pageSize;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// FlowDocument associated with the paginator.
        /// </summary>
        private readonly FlowDocument _document;

        /// <summary>
        /// Provides mechanism to ensure usage from just one Dispatcher.
        /// </summary>
        private readonly CustomDispatcherObject _dispatcherObject;

        /// <summary>
        /// BreakRecordTable
        /// </summary>
        private readonly BreakRecordTable _brt;

        /// <summary>
        /// Page size.
        /// </summary>
        private Size _pageSize;

        /// <summary>
        /// Whether content is paginated in the background.
        /// </summary>
        private bool _backgroundPagination;


        /// <summary>
        /// Pagination timeout time.
        /// </summary>
        private const int _paginationTimeout = 30;

        /// <summary>
        /// Default page size if none is specified.
        /// </summary>
        private static Size _defaultPageSize = new Size(8.5d * 96d, 11.0d * 96d);

        /// <summary>
        /// Async request list
        /// </summary>
        List<AsyncRequest> _asyncRequests = new List<AsyncRequest>(0);

        /// <summary>
        /// Background pagination dispatcher operation.
        /// </summary>
        DispatcherOperation _backgroundPaginationOperation;

        #endregion Private Fields


        #region Private Classes

        /// <summary>
        /// Base class for all async requests.
        /// </summary>
        private abstract class AsyncRequest
        {
            internal AsyncRequest(object userState, FlowDocumentPaginator paginator)
            {
                UserState = userState;
                Paginator = paginator;
            }

            /// <summary>
            /// Cancels this async request, responsible for firing appropriate events.
            /// </summary>
            internal abstract void Cancel();

            /// <summary>
            /// Processes this request. Returns true if processing completed. Responsible for firing appropriate events.
            /// </summary>
            internal abstract bool Process();


            /// <summary>
            /// User state - Needs to be internal for cancel.
            /// </summary>
            internal readonly object UserState;
            protected readonly FlowDocumentPaginator Paginator;
        }


        /// <summary>
        /// GetPage async request.
        /// </summary>
        private class GetPageAsyncRequest : AsyncRequest
        {
            internal GetPageAsyncRequest(int pageNumber, object userState, FlowDocumentPaginator paginator) : base(userState, paginator)
            {
                PageNumber = pageNumber;
            }

            /// <summary>
            /// <see cref="AsyncRequest.Cancel"/>
            /// </summary>
            internal override void Cancel()
            {
                Paginator.OnGetPageCompleted(new GetPageCompletedEventArgs(null, PageNumber, null, true, UserState));
            }

            /// <summary>
            /// <see cref="AsyncRequest.Process"/>
            /// </summary>
            internal override bool Process()
            {
                if (!Paginator._brt.HasPageBreakRecord(PageNumber))
                {
                    return false;
                }

                DocumentPage page = Paginator.FormatPage(PageNumber);

                Paginator.OnGetPageCompleted(new GetPageCompletedEventArgs(page, PageNumber, null, false, UserState));

                return true;
            }

            internal readonly int PageNumber;
        }

        /// <summary>
        /// GetPageNumber async request.
        /// </summary>
        private class GetPageNumberAsyncRequest : AsyncRequest
        {
            internal GetPageNumberAsyncRequest(TextPointer textPointer, object userState, FlowDocumentPaginator paginator) : base(userState, paginator)
            {
                TextPointer = textPointer;
            }

            /// <summary>
            /// <see cref="AsyncRequest.Cancel"/>
            /// </summary>
            internal override void Cancel()
            {
                Paginator.OnGetPageNumberCompleted(new GetPageNumberCompletedEventArgs(TextPointer, -1, null, true, UserState));
            }

            /// <summary>
            /// <see cref="AsyncRequest.Process"/>
            /// </summary>
            internal override bool Process()
            {
                int pageNumber = 0;

                if (!Paginator._brt.GetPageNumberForContentPosition(TextPointer, ref pageNumber))
                {
                    return false;
                }

                Paginator.OnGetPageNumberCompleted(new GetPageNumberCompletedEventArgs(TextPointer, pageNumber, null, false, UserState));

                return true;
            }

            internal readonly TextPointer TextPointer;
        }

        #endregion Private Classes

        //-------------------------------------------------------------------
        //
        //  Private Types
        //
        //-------------------------------------------------------------------

        #region Private Types

        /// <summary>
        /// Provides mechanism to ensure usage from just one Dispatcher.
        /// </summary>
        private class CustomDispatcherObject : DispatcherObject { }

        #endregion Private Types

        //-------------------------------------------------------------------
        //
        //  IServiceProvider Members
        //
        //-------------------------------------------------------------------

        #region IServiceProvider Members

        /// <summary>
        /// Returns service objects associated with this control.
        /// </summary>
        /// <param name="serviceType">Specifies the type of service object to get.</param>
        object IServiceProvider.GetService(Type serviceType)
        {
            return ((IServiceProvider)_document).GetService(serviceType);
        }

        #endregion IServiceProvider Members

        //-------------------------------------------------------------------
        //
        //  IFlowDocumentFormatter Members
        //
        //-------------------------------------------------------------------

        #region IFlowDocumentFormatter Members

        /// <summary>
        /// Responds to change affecting entire content of associated FlowDocument.
        /// </summary>
        /// <param name="affectsLayout">Whether change affects layout.</param>
        void IFlowDocumentFormatter.OnContentInvalidated(bool affectsLayout)
        {
            if (affectsLayout)
            {
                InvalidateBRT();
            }
            else
            {
                _brt.OnInvalidateRender();
            }
        }

        /// <summary>
        /// Responds to change affecting entire content of associated FlowDocument.
        /// </summary>
        /// <param name="affectsLayout">Whether change affects layout.</param>
        /// <param name="start">Start of the affected content range.</param>
        /// <param name="end">End of the affected content range.</param>
        void IFlowDocumentFormatter.OnContentInvalidated(bool affectsLayout, ITextPointer start, ITextPointer end)
        {
            if (affectsLayout)
            {
                InvalidateBRTLayout(start, end);
            }
            else
            {
                _brt.OnInvalidateRender(start, end);
            }
        }

        /// <summary>
        /// Suspend formatting.
        /// </summary>
        void IFlowDocumentFormatter.Suspend()
        {
            IsBackgroundPaginationEnabled = false;
            InvalidateBRT();
        }

        /// <summary>
        /// Is layout data in a valid state.
        /// </summary>
        bool IFlowDocumentFormatter.IsLayoutDataValid
        {
            get
            {
                return !_document.StructuralCache.IsContentChangeInProgress;
            }
        }

        #endregion IFlowDocumentFormatter Members
    }
}
