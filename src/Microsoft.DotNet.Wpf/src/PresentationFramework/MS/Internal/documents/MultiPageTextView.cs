// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: TextView implementation for collection of DocumentPageTextViews. 
//

using System;                               // InvalidOperationException, ...
using System.Collections.Generic;           // List<T>
using System.Collections.ObjectModel;       // ReadOnlyCollection
using System.Windows;                       // Point, Rect, ...
using System.Windows.Controls;              // FlowDocumentPageViewer, DocumentViewer
using System.Windows.Controls.Primitives;   // DocumentPageView, DocumentViewerBase
using System.Windows.Documents;             // ITextView, ITextContainer
using System.Windows.Media;                 // VisualTreeHelper
using System.Windows.Threading;             // DispatcherPriority, DispatherOperationCallback

namespace MS.Internal.Documents
{
    /// <summary>
    /// TextView implementation for collection of DocumentPageTextViews.
    /// </summary>
    internal class MultiPageTextView : TextViewBase
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="viewer">Viewer associated with TextView.</param>
        /// <param name="renderScope">Render scope - root of layout structure visualizing content.</param>
        /// <param name="textContainer">TextContainer representing content.</param>
        internal MultiPageTextView(DocumentViewerBase viewer, UIElement renderScope, ITextContainer textContainer)
        {
            _viewer = viewer;
            _renderScope = renderScope;
            _textContainer = textContainer;
            _pageTextViews = new List<DocumentPageTextView>();
            OnPagesUpdatedCore();
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Fires Updated event.
        /// </summary>
        /// <param name="e">Event arguments for the Updated event.</param>
        protected override void OnUpdated(EventArgs e)
        {
            // Forward the event.
            base.OnUpdated(e);

            // Update.
            if (this.IsValid)
            {
                OnUpdatedWorker(null);
            }
            else
            {
                _renderScope.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(OnUpdatedWorker), EventArgs.Empty);
            }
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// <see cref="ITextView.GetTextPositionFromPoint"/>
        /// </summary>
        internal override ITextPointer GetTextPositionFromPoint(Point point, bool snapToText)
        {
            ITextPointer position = null;
            DocumentPageTextView pageTextView;

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }

            pageTextView = GetTextViewFromPoint(point, false);
            if (pageTextView != null)
            {
                // Transform to DocumentPageView coordinates and query inner TextView
                point = TransformToDescendant(pageTextView.RenderScope, point);
                position = pageTextView.GetTextPositionFromPoint(point, snapToText);
            }
            return position;
        }

        /// <summary>
        /// <see cref="ITextView.GetRawRectangleFromTextPosition"/>
        /// </summary>
        internal override Rect GetRawRectangleFromTextPosition(ITextPointer position, out Transform transform)
        {
            Rect rect = Rect.Empty;
            DocumentPageTextView pageTextView;

            // Initialize transform to Identity
            transform = Transform.Identity;

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }

            pageTextView = GetTextViewFromPosition(position);
            if (pageTextView != null)
            {
                // Query nested TextView and transform from DocumentPageView coordinates.
                Transform pageTextViewTransform, ancestorTransform;
                rect = pageTextView.GetRawRectangleFromTextPosition(position, out pageTextViewTransform);
                ancestorTransform = GetTransformToAncestor(pageTextView.RenderScope);
                transform = GetAggregateTransform(pageTextViewTransform, ancestorTransform);
            }
            return rect;
        }

        /// <summary>
        /// <see cref="TextViewBase.GetTightBoundingGeometryFromTextPositions"/>
        /// </summary>
        internal override Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition)
        {
            //  verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }

            Geometry geometry = null;

            for (int i = 0, count = _pageTextViews.Count; i < count; ++i)
            {
                ReadOnlyCollection<TextSegment> textSegments = _pageTextViews[i].TextSegments;

                for (int segmentIndex = 0; segmentIndex < textSegments.Count; segmentIndex++)
                {
                    TextSegment textSegment = textSegments[segmentIndex];

                    ITextPointer startPositionInTextSegment = startPosition.CompareTo(textSegment.Start) > 0 ? startPosition : textSegment.Start;
                    ITextPointer endPositionInTextSegment = endPosition.CompareTo(textSegment.End) < 0 ? endPosition : textSegment.End;

                    if (startPositionInTextSegment.CompareTo(endPositionInTextSegment) >= 0)
                    {
                        continue;
                    }

                    Geometry pageGeometry = _pageTextViews[i].GetTightBoundingGeometryFromTextPositions(startPositionInTextSegment, endPositionInTextSegment);
                    if (pageGeometry != null)
                    {
                        Transform transform = _pageTextViews[i].RenderScope.TransformToAncestor(_renderScope).AffineTransform;
                        CaretElement.AddTransformToGeometry(pageGeometry, transform);

                        CaretElement.AddGeometry(ref geometry, pageGeometry);
                    }
                }
            }

            return (geometry);
        }

        /// <summary>
        /// <see cref="ITextView.GetPositionAtNextLine"/>
        /// </summary>
        internal override ITextPointer GetPositionAtNextLine(ITextPointer position, double suggestedX, int count, out double newSuggestedX, out int linesMoved)
        {
            int pageNumber;

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            return GetPositionAtNextLineCore(position, suggestedX, count, out newSuggestedX, out linesMoved, out pageNumber);
        }

        /// <summary>
        /// <see cref="ITextView.GetPositionAtNextPage"/>
        /// </summary>
        internal override ITextPointer GetPositionAtNextPage(ITextPointer position, Point suggestedOffset, int count, out Point newSuggestedOffset, out int pagesMoved)
        {
            int pageNumber;             // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            return GetPositionAtNextPageCore(position, suggestedOffset, count, out newSuggestedOffset, out pagesMoved, out pageNumber);
        }

        /// <summary>
        /// <see cref="ITextView.IsAtCaretUnitBoundary"/>
        /// </summary>
        internal override bool IsAtCaretUnitBoundary(ITextPointer position)
        {
            bool atCaretUnitBoundary = false;
            DocumentPageTextView pageTextView;

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }

            pageTextView = GetTextViewFromPosition(position);
            if (pageTextView != null)
            {
                atCaretUnitBoundary = pageTextView.IsAtCaretUnitBoundary(position);
            }
            return atCaretUnitBoundary;
        }

        /// <summary>
        /// <see cref="ITextView.GetNextCaretUnitPosition"/>
        /// </summary>
        internal override ITextPointer GetNextCaretUnitPosition(ITextPointer position, LogicalDirection direction)
        {
            ITextPointer positionOut = null;
            DocumentPageTextView pageTextView;

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }

            pageTextView = GetTextViewFromPosition(position);
            if (pageTextView != null)
            {
                positionOut = pageTextView.GetNextCaretUnitPosition(position, direction);
            }
            return positionOut;
        }

        /// <summary>
        /// <see cref="ITextView.GetBackspaceCaretUnitPosition"/>
        /// </summary>
        internal override ITextPointer GetBackspaceCaretUnitPosition(ITextPointer position)
        {
            ITextPointer positionOut = null;
            DocumentPageTextView pageTextView;

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }

            pageTextView = GetTextViewFromPosition(position);
            if (pageTextView != null)
            {
                positionOut = pageTextView.GetBackspaceCaretUnitPosition(position);
            }
            return positionOut;
        }

        /// <summary>
        /// <see cref="ITextView.GetBackspaceCaretUnitPosition"/>
        /// </summary>
        internal override TextSegment GetLineRange(ITextPointer position)
        {
            TextSegment textSegment = TextSegment.Null;
            DocumentPageTextView pageTextView;

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }

            pageTextView = GetTextViewFromPosition(position);
            if (pageTextView != null)
            {
                textSegment = pageTextView.GetLineRange(position);
            }
            return textSegment;
        }

        /// <summary>
        /// <see cref="ITextView.Contains"/>
        /// </summary>
        internal override bool Contains(ITextPointer position)
        {
            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            return (GetTextViewFromPosition(position) != null);
        }

        /// <summary>
        /// <see cref="ITextView.BringPositionIntoViewAsync"/>
        /// </summary>
        internal override void BringPositionIntoViewAsync(ITextPointer position, object userState)
        {
            DocumentPageTextView pageTextView;
            int pageNumber;
            BringPositionIntoViewRequest pendingRequest;

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            if (_pendingRequest != null)
            {
                // Ignore new request if the previous is not completed yet.
                OnBringPositionIntoViewCompleted(new BringPositionIntoViewCompletedEventArgs(
                    position, false, null, false, userState));
            }

            pendingRequest = new BringPositionIntoViewRequest(position, userState);
            _pendingRequest = pendingRequest;

            pageTextView = GetTextViewFromPosition(position);
            // If the position is currently in the view, do nothing.
            // Otherwise, let the viewer handle the request.
            if (pageTextView != null)
            {
                pendingRequest.Succeeded = true;
                OnBringPositionIntoViewCompleted(pendingRequest);
            }
            else
            {
                if (position is ContentPosition)
                {
                    DynamicDocumentPaginator documentPaginator = _viewer.Document.DocumentPaginator as DynamicDocumentPaginator;
                    if (documentPaginator != null)
                    {
                        pageNumber = documentPaginator.GetPageNumber((ContentPosition)position) + 1;
                        if (_viewer.CanGoToPage(pageNumber))
                        {
                            _viewer.GoToPage(pageNumber);
                        }
                        else
                        {
                            OnBringPositionIntoViewCompleted(pendingRequest);
                        }
                    }
                    else
                    {
                        OnBringPositionIntoViewCompleted(pendingRequest);
                    }
                }
                else
                {
                    OnBringPositionIntoViewCompleted(pendingRequest);
                }
            }
        }

        /// <summary>
        /// <see cref="ITextView.BringPointIntoViewAsync"/>
        /// </summary>
        internal override void BringPointIntoViewAsync(Point point, object userState)
        {
            DocumentPageTextView pageTextView;
            ITextPointer position;
            BringPointIntoViewRequest pendingRequest;
            bool bringIntoViewPending;

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            if (_pendingRequest != null)
            {
                // Ignore new request if the previous is not completed yet.
                OnBringPointIntoViewCompleted(new BringPointIntoViewCompletedEventArgs(
                    point, null, false, null, false, userState));
            }
            else
            {
                pendingRequest = new BringPointIntoViewRequest(point, userState);
                _pendingRequest = pendingRequest;

                pageTextView = GetTextViewFromPoint(point, false);
                // If the point is currently in the view, use existing TextView to retrieve the position.
                // Otherwise, let the viewer handle the request.
                if (pageTextView != null)
                {
                    // Transform to DocumentPageView coordinates and query inner TextView
                    point = TransformToDescendant(pageTextView.RenderScope, point);
                    position = pageTextView.GetTextPositionFromPoint(point, true);

                    pendingRequest.Position = position;
                    OnBringPointIntoViewCompleted(pendingRequest);
                }
                else
                {
                    // Request to bring point into view in the Viewer.
                    // This code is specific to known viewers. Since text selection is not
                    // exposed in a public way, it should not cause any "extensibility" problems.
                    GeneralTransform transform = _renderScope.TransformToAncestor(_viewer);

                    // REVIEW: should we do anything special if the point could not be
                    // transformed completely?
                    transform.TryTransform(point, out point);
                    bringIntoViewPending = false;
                    if (_viewer is FlowDocumentPageViewer)
                    {
                        // Special handling for FlowDocumentPageViewer
                        bringIntoViewPending = ((FlowDocumentPageViewer)_viewer).BringPointIntoView(point);
                    }
                    else if (_viewer is DocumentViewer)
                    {
                        // Special handling for DocumentViewer
                        bringIntoViewPending = ((DocumentViewer)_viewer).BringPointIntoView(point);
                    }
                    else
                    {
                        if (DoubleUtil.LessThan(point.X, 0))
                        {
                            if (_viewer.CanGoToPreviousPage)
                            {
                                _viewer.PreviousPage();
                                bringIntoViewPending = true;
                            }
                        }
                        else if (DoubleUtil.GreaterThan(point.X, _viewer.RenderSize.Width))
                        {
                            if (_viewer.CanGoToNextPage)
                            {
                                _viewer.NextPage();
                                bringIntoViewPending = true;
                            }
                        }
                        else if (DoubleUtil.LessThan(point.Y, 0))
                        {
                            if (_viewer.CanGoToPreviousPage)
                            {
                                _viewer.PreviousPage();
                                bringIntoViewPending = true;
                            }
                        }
                        else if (DoubleUtil.GreaterThan(point.Y, _viewer.RenderSize.Height))
                        {
                            if (_viewer.CanGoToNextPage)
                            {
                                _viewer.NextPage();
                                bringIntoViewPending = true;
                            }
                        }
                    }
                    if (!bringIntoViewPending)
                    {
                        OnBringPointIntoViewCompleted(pendingRequest);
                    }
                }
            }
        }

        /// <summary>
        /// <see cref="ITextView.BringLineIntoViewAsync"/>
        /// </summary>
        internal override void BringLineIntoViewAsync(ITextPointer position, double suggestedX, int count, object userState)
        {
            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            if (_pendingRequest != null)
            {
                // Ignore new request if the previous is not completed yet.
                OnBringLineIntoViewCompleted(new BringLineIntoViewCompletedEventArgs(
                    position, suggestedX, count, position, suggestedX, 0, false, null, false, userState));
            }
            else
            {
                _pendingRequest = new BringLineIntoViewRequest(position, suggestedX, count, userState);
                BringLineIntoViewCore((BringLineIntoViewRequest)_pendingRequest);
            }
        }

        /// <summary>
        /// <see cref="ITextView.BringLineIntoViewAsync"/>
        /// </summary>
        internal override void BringPageIntoViewAsync(ITextPointer position, Point suggestedOffset, int count, object userState)
        {
            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            if (_pendingRequest != null)
            {
                // Ignore new request if the previous is not completed yet.
                OnBringPageIntoViewCompleted(new BringPageIntoViewCompletedEventArgs(
                    position, suggestedOffset, count, position, suggestedOffset, 0, false, null, false, userState));
            }
            else
            {
                _pendingRequest = new BringPageIntoViewRequest(position, suggestedOffset, count, userState);
                BringPageIntoViewCore((BringPageIntoViewRequest)_pendingRequest);
            }
        }


        /// <summary>
        /// <see cref="ITextView.CancelAsync"/>
        /// </summary>
        internal override void CancelAsync(object userState)
        {
            BringLineIntoViewRequest lineRequest;
            BringPageIntoViewRequest pageRequest;
            BringPointIntoViewRequest pointRequest;
            BringPositionIntoViewRequest positionRequest;

            if (_pendingRequest != null)
            {
                if (_pendingRequest is BringLineIntoViewRequest)
                {
                    lineRequest = (BringLineIntoViewRequest)_pendingRequest;
                    OnBringLineIntoViewCompleted(new BringLineIntoViewCompletedEventArgs(
                        lineRequest.Position, lineRequest.SuggestedX, lineRequest.Count,
                        lineRequest.NewPosition, lineRequest.NewSuggestedX, lineRequest.Count - lineRequest.NewCount,
                        false, null, true, lineRequest.UserState));
                }
                else if (_pendingRequest is BringPageIntoViewRequest)
                {
                    pageRequest = (BringPageIntoViewRequest)_pendingRequest;
                    OnBringPageIntoViewCompleted(new BringPageIntoViewCompletedEventArgs(
                        pageRequest.Position, pageRequest.SuggestedOffset, pageRequest.Count,
                        pageRequest.NewPosition, pageRequest.NewSuggestedOffset, pageRequest.Count - pageRequest.NewCount,
                        false, null, true, pageRequest.UserState));
                }
                else if (_pendingRequest is BringPointIntoViewRequest)
                {
                    pointRequest = (BringPointIntoViewRequest)_pendingRequest;
                    OnBringPointIntoViewCompleted(new BringPointIntoViewCompletedEventArgs(
                        pointRequest.Point, pointRequest.Position, false, null, true, pointRequest.UserState));
                }
                else if (_pendingRequest is BringPositionIntoViewRequest)
                {
                    positionRequest = (BringPositionIntoViewRequest)_pendingRequest;
                    OnBringPositionIntoViewCompleted(new BringPositionIntoViewCompletedEventArgs(
                        positionRequest.Position, false, null, true, positionRequest.UserState));
                }
                _pendingRequest = null;
            }
        }

        /// <summary>
        /// Collection of DocumentPageViews has been changed. Need to update 
        /// collection of TextViews.
        /// </summary>
        internal void OnPagesUpdated()
        {
            OnPagesUpdatedCore();
            if (IsValid)
            {
                OnUpdated(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Invoked when Page Layout has changed in order to keep
        /// the TextView in sync.
        /// </summary>
        internal void OnPageLayoutChanged()
        {
            if (IsValid)
            {
                OnUpdated(EventArgs.Empty);
            }
        }


        /// <summary>
        /// Retrieves an active TextView containing the object or character 
        /// represented by the given TextPointer.
        /// </summary>
        /// <param name="position">Position of an object/character.</param>
        /// <returns>
        /// Active TextView containing the object or character represented by 
        /// the given TextPointer.
        /// </returns>
        internal ITextView GetPageTextViewFromPosition(ITextPointer position)
        {
            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            return GetTextViewFromPosition(position);
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// <see cref="ITextView.RenderScope"/>
        /// </summary>
        internal override UIElement RenderScope
        {
            get { return _renderScope; }
        }

        /// <summary>
        /// <see cref="ITextView.TextContainer"/>
        /// </summary>
        internal override ITextContainer TextContainer
        {
            get { return _textContainer; }
        }

        /// <summary>
        /// <see cref="ITextView.IsValid"/>
        /// </summary>
        internal override bool IsValid
        {
            get
            {
                bool valid = false;
                if (_pageTextViews != null)
                {
                    valid = true;
                    for (int i = 0; i < _pageTextViews.Count; i++)
                    {
                        if (!_pageTextViews[i].IsValid)
                        {
                            valid = false;
                            break;
                        }
                    }
                }
                return valid;
            }
        }


        /// <summary>
        /// <see cref="ITextView.RendersOwnSelection"/>
        /// </summary>
        internal override bool RendersOwnSelection
        {
            get
            {
                if (_pageTextViews != null && _pageTextViews.Count > 0)
                {
                    return _pageTextViews[0].RendersOwnSelection;
                }
                return false;
            }
        }


        /// <summary>
        /// <see cref="ITextView.TextSegments"/>
        /// </summary>
        internal override ReadOnlyCollection<TextSegment> TextSegments
        {
            get
            {
                List<TextSegment> textSegments = new List<TextSegment>();
                if (IsValid)
                {
                    // Get collection of active TextViews for all PageDocumentViews.
                    for (int i = 0; i < _pageTextViews.Count; i++)
                    {
                        textSegments.AddRange(_pageTextViews[i].TextSegments);
                    }
                }
                return new ReadOnlyCollection<TextSegment>(textSegments);
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
        /// Collection of DocumentPageViews has been changed. Need to update 
        /// collection of TextViews.
        /// </summary>
        private void OnPagesUpdatedCore()
        {
            ReadOnlyCollection<DocumentPageView> pageViews;
            DocumentPageTextView pageTextView;
            int index;

            // Drop old collection of DocumentPageTextView objects.
            for (index = 0; index < _pageTextViews.Count; index++)
            {
                _pageTextViews[index].Updated -= new EventHandler(HandlePageTextViewUpdated);
            }

            _pageTextViews.Clear();
            pageViews = _viewer.PageViews;
            if (pageViews != null)
            {
                for (index = 0; index < pageViews.Count; index++)
                {
                    pageTextView = ((IServiceProvider)pageViews[index]).GetService(typeof(ITextView)) as DocumentPageTextView;
                    if (pageTextView != null)
                    {
                        _pageTextViews.Add(pageTextView);
                        pageTextView.Updated += new EventHandler(HandlePageTextViewUpdated);
                    }
                }
            }
        }

        /// <summary>
        /// Handler for Updated event raised by the inner TextView.
        /// </summary>
        private void HandlePageTextViewUpdated(object sender, EventArgs e)
        {
            OnUpdated(EventArgs.Empty);
        }

        /// <summary>
        /// Bring line into view.
        /// </summary>
        private void BringLineIntoViewCore(BringLineIntoViewRequest request)
        {
            ITextPointer newPosition;
            double newSuggestedX;
            int linesMoved;
            int pageNumber;

            // Try to use existing TextViews to handle this request.
            newPosition = GetPositionAtNextLineCore(request.NewPosition, request.NewSuggestedX, request.NewCount, out newSuggestedX, out linesMoved, out pageNumber);
            Invariant.Assert(Math.Abs(request.NewCount) >= Math.Abs(linesMoved));
            request.NewPosition = newPosition;
            request.NewSuggestedX = newSuggestedX;
            request.NewCount = request.NewCount - linesMoved;
            request.NewPageNumber = pageNumber;

            if (request.NewCount == 0)
            {
                OnBringLineIntoViewCompleted(request);
            }
            else
            {
                if (newPosition is DocumentSequenceTextPointer || newPosition is FixedTextPointer)
                {
                    //Fixed generally uses a stitched viewing mechanism and the approach used for Flow does not work properly for Fixed
                    //NextPage() call in DocumentViewer results in DocumentGrid.ScrollToNextRow() which scrolls to firstVisibleRow + 1
                    //E.g. if there are two views on the screen and we need to navigate to the third, this makes it impossible to achieve
                    if (_viewer.CanGoToPage(pageNumber + 1))
                    {
                        _viewer.GoToPage(pageNumber + 1);
                    }
                    else
                    {
                        OnBringLineIntoViewCompleted(request);
                    }
                }
                else
                {
                    if (request.NewCount > 0)
                    {
                        // If the viewer can navigate to the next page, request navigation and wait for 
                        // Updated event for the TextView.
                        // If cannot to the next page, raise BringLineIntoViewCompleted with success = 'False'.
                        if (_viewer.CanGoToNextPage)
                        {
                            _viewer.NextPage();
                        }
                        else
                        {
                            OnBringLineIntoViewCompleted(request);
                        }
                    }
                    else
                    {
                        // If the viewer can navigate to the previous page, request navigation and wait for 
                        // Updated event fo the TextView.
                        // If cannot to the previous page, raise BringLineIntoViewCompleted with success = 'False'.
                        if (_viewer.CanGoToPreviousPage)
                        {
                            _viewer.PreviousPage();
                        }
                        else
                        {
                            OnBringLineIntoViewCompleted(request);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Bring page into view.
        /// </summary>
        private void BringPageIntoViewCore(BringPageIntoViewRequest request)
        {
            ITextPointer newPosition;
            Point newSuggestedOffset;
            int pagesMoved;
            int newPageNumber;

            // Try to use existing TextViews to handle this request.
            newPosition = GetPositionAtNextPageCore(request.NewPosition, request.NewSuggestedOffset, request.NewCount, out newSuggestedOffset, out pagesMoved, out newPageNumber);
            Invariant.Assert(Math.Abs(request.NewCount) >= Math.Abs(pagesMoved));
            request.NewPosition = newPosition;
            request.NewSuggestedOffset = newSuggestedOffset;
            request.NewCount = request.NewCount - pagesMoved;

            if (request.NewCount == 0 || newPageNumber == -1)
            {
                OnBringPageIntoViewCompleted(request);
            }
            else
            {
                // If the viewer can navigate to the next page, request navigation and wait for 
                // Updated event for the TextView.
                // If cannot to the next page, raise BringLineIntoViewCompleted with success = 'False'.
                newPageNumber += (request.NewCount > 0) ? 1 : -1;
                if (_viewer.CanGoToPage(newPageNumber + 1))
                {
                    request.NewPageNumber = newPageNumber;
                    _viewer.GoToPage(newPageNumber + 1);
                }
                else
                {
                    OnBringPageIntoViewCompleted(request);
                }
            }
        }

        /// <summary>
        /// <see cref="ITextView.GetPositionAtNextLine"/>
        /// </summary>
        private ITextPointer GetPositionAtNextLineCore(ITextPointer position, double suggestedX, int count, out double newSuggestedX, out int linesMoved, out int pageNumber)
        {
            ITextPointer positionOut;
            DocumentPageTextView pageTextView;
            int originalCount;
            Point offset;
            int newLinesMoved;
            int previousCount;
            ReadOnlyCollection<TextSegment> segments;

            pageTextView = GetTextViewFromPosition(position);
            if (pageTextView != null)
            {
                originalCount = count;

                // Transform to DocumentPageView coordinates
                offset = TransformToDescendant(pageTextView.RenderScope, new Point(suggestedX, 0));
                suggestedX = offset.X;

                // Query inner TextView
                positionOut = pageTextView.GetPositionAtNextLine(position, suggestedX, count, out newSuggestedX, out linesMoved);
                pageNumber = ((DocumentPageView)pageTextView.RenderScope).PageNumber;

                // Transform from DocumentPageView coordinates
                offset = TransformToAncestor(pageTextView.RenderScope, new Point(newSuggestedX, 0));
                newSuggestedX = offset.X;

                // If number of lines moved is different than requested number of lines,
                // try to find query the previous/next page.
                while (originalCount != linesMoved)
                {
                    newLinesMoved = 0;
                    count = originalCount - linesMoved;

                    // Try to find TextView for DocumentPageView with the next/previous page number.
                    pageNumber += (count > 0) ? 1 : -1;
                    pageTextView = GetTextViewFromPageNumber(pageNumber);
                    if (pageTextView != null)
                    {
                        // All positions have to be in the TextView boundary.
                        // Since we are quering another TextView, move requested position to its range.
                        segments = pageTextView.TextSegments;
                        previousCount = count;
                        if (count > 0)
                        {
                            position = pageTextView.GetTextPositionFromPoint(new Point(suggestedX, 0), true);
                            if (position != null)
                            {
                                --count;
                                ++linesMoved;
                            }
                        }
                        else
                        {
                            position = pageTextView.GetTextPositionFromPoint(new Point(suggestedX, pageTextView.RenderScope.RenderSize.Height), true);
                            if (position != null)
                            {
                                ++count;
                                --linesMoved;
                            }
                        }

                        if (position != null)
                        {
                            // If moving to the first/last line of the next/previous TextView, there is
                            // special handling needed to position at the right suggestedX. Otherwise, TextView
                            // will return the same position.
                            if (count == 0)
                            {
                                positionOut = GetPositionAtPageBoundary(previousCount > 0, pageTextView, position, suggestedX);
                                newSuggestedX = suggestedX;
                            }
                            else
                            {
                                // Query nested TextView.
                                // Use the same logical 'suggestedX' as for previous DocumentPageView.
                                positionOut = pageTextView.GetPositionAtNextLine(position, suggestedX, count, out newSuggestedX, out newLinesMoved);
                                linesMoved += newLinesMoved;
                            }

                            // Transform from DocumentPageView coordinates
                            offset = TransformToAncestor(pageTextView.RenderScope, new Point(newSuggestedX, 0));
                            newSuggestedX = offset.X;
                        }
                    }
                    else
                    {
                        // If DocumentPageView has not been found, there is no point
                        // to continue.
                        // 'positionOut' contains the closes position we can return.
                        break;
                    }
                }
            }
            else
            {
                positionOut = position;
                linesMoved = 0;
                newSuggestedX = suggestedX;
                pageNumber = -1;
            }
            return positionOut;
        }

        private ITextPointer GetPositionAtNextPageCore(ITextPointer position, Point suggestedOffset, int count, out Point newSuggestedOffset, out int pagesMoved, out int pageNumber)
        {
            // Initialize output 
            ITextPointer positionOut = position;
            pagesMoved = 0;
            newSuggestedOffset = suggestedOffset;
            pageNumber = -1;

            DocumentPageTextView pageTextView = GetTextViewFromPosition(position);
            if (pageTextView != null)
            {
                int currentPageNumber = ((DocumentPageView)pageTextView.RenderScope).PageNumber;
                DocumentPageTextView newPageTextView = GetTextViewForNextPage(currentPageNumber, count, out pageNumber);
                pagesMoved = pageNumber - currentPageNumber;
                Invariant.Assert(Math.Abs(pagesMoved) <= Math.Abs(count));

                if (pageNumber != currentPageNumber && newPageTextView != null)
                {
                    // Transform suggested offset to to DocumentPageView coordinates to use as X-coordinate
                    Point point = TransformToDescendant(pageTextView.RenderScope, suggestedOffset);

                    // Query inner TextView for requested page to find position at that point. 
                    positionOut = newPageTextView.GetTextPositionFromPoint(point, /*snapToText*/true);
                    if (positionOut != null)
                    {
                        Rect rect = newPageTextView.GetRectangleFromTextPosition(positionOut);
                        point = TransformToAncestor(pageTextView.RenderScope, new Point(rect.X, rect.Y));
                        newSuggestedOffset = point;
                    }
                    else
                    {
                        positionOut = position;
                        pagesMoved = 0;
                        pageNumber = currentPageNumber;
                    }
                }
                else
                {
                    pagesMoved = 0;
                    pageNumber = currentPageNumber;
                }
            }

            return positionOut;
        }

        /// <summary>
        /// Retrieves position at the page boundary from given suggestedX.
        /// </summary>
        /// <param name="pageTop">Whether asking for top of the page or bottom of the page.</param>
        /// <param name="pageTextView">TextView representing the page.</param>
        /// <param name="position">Position at the beginning/end of the page.</param>
        /// <param name="suggestedX">Suggested offset in the page.</param>
        /// <returns>Position at the page boundary from given suggestedX.</returns>
        private ITextPointer GetPositionAtPageBoundary(bool pageTop, ITextView pageTextView, ITextPointer position, double suggestedX)
        {
            double newSuggestedX;
            int newLinesMoved;
            ITextPointer positionOut;

            // If moving to the first/last line of the next/previous TextView, there is
            // special handling needed to position at the right suggestedX. Otherwise, TextView
            // will return the same position.
            if (pageTop)
            {
                // Move line down and line up.
                positionOut = pageTextView.GetPositionAtNextLine(position, suggestedX, 1, out newSuggestedX, out newLinesMoved);
                if (newLinesMoved == 1)
                {
                    positionOut = pageTextView.GetPositionAtNextLine(positionOut, newSuggestedX, -1, out newSuggestedX, out newLinesMoved);
                }
                else
                {
                    // Line down failed, so use the first position of TextView.
                    positionOut = position;
                }
            }
            else
            {
                // Move line up and line down.
                positionOut = pageTextView.GetPositionAtNextLine(position, suggestedX, -1, out newSuggestedX, out newLinesMoved);
                if (newLinesMoved == -1)
                {
                    positionOut = pageTextView.GetPositionAtNextLine(positionOut, newSuggestedX, 1, out newSuggestedX, out newLinesMoved);
                }
                else
                {
                    // Line up failed, so use the last position of TextView.
                    positionOut = position;
                }
            }
            return positionOut;
        }

        /// <summary>
        /// Returns an active TextView that matches the supplied Point.
        /// </summary>
        /// <param name="point">Point in pixel coordinates to test.</param>
        /// <param name="snap">Snap to closest TextView.</param>
        /// <returns>An active TextView that matches the supplied Point.</returns>
        private DocumentPageTextView GetTextViewFromPoint(Point point, bool snap)
        {
            DocumentPageTextView textView = null;
            Rect textViewBounds;
            int i;

            // Try to find pageElement with exact hit.
            // Enumerate all inner TextViews and try to find exact hit for given Point.
            for (i = 0; i < _pageTextViews.Count; i++)
            {
                textViewBounds = TransformToAncestor(_pageTextViews[i].RenderScope, new Rect(_pageTextViews[i].RenderScope.RenderSize));
                if (textViewBounds.Contains(point))
                {
                    textView = _pageTextViews[i];
                    break;
                }
            }

            if (textView == null && snap)
            {
                // For each TextView calculate 'proximity' function.
                double[] textViewProximities = new double[_pageTextViews.Count];
                for (i = 0; i < _pageTextViews.Count; i++)
                {
                    textViewBounds = TransformToAncestor(_pageTextViews[i].RenderScope, new Rect(_pageTextViews[i].RenderScope.RenderSize));
                    double horz, vert;
                    if (point.X >= textViewBounds.Left && point.X <= textViewBounds.Right)
                    {
                        horz = 0;
                    }
                    else
                    {
                        horz = Math.Min(Math.Abs(point.X - textViewBounds.Left), Math.Abs(point.X - textViewBounds.Right));
                    }
                    if (point.Y >= textViewBounds.Top && point.Y <= textViewBounds.Bottom)
                    {
                        vert = 0;
                    }
                    else
                    {
                        vert = Math.Min(Math.Abs(point.Y - textViewBounds.Top), Math.Abs(point.Y - textViewBounds.Bottom));
                    }
                    textViewProximities[i] = Math.Sqrt(Math.Pow(horz, 2) + Math.Pow(vert, 2));
                }
                // Get the closest TextView according to 'proximity' function.
                double proximity = double.MaxValue;
                for (i = 0; i < textViewProximities.Length; i++)
                {
                    if (proximity > textViewProximities[i])
                    {
                        proximity = textViewProximities[i];
                        textView = _pageTextViews[i];
                    }
                }
            }

            return textView;
        }

        /// <summary>
        /// Retrieves an active TextView containing the object or character 
        /// represented by the given TextPointer.
        /// </summary>
        /// <param name="position">Position of an object/character.</param>
        /// <returns>
        /// Active TextView containing the object or character represented by 
        /// the given TextPointer.
        /// </returns>
        private DocumentPageTextView GetTextViewFromPosition(ITextPointer position)
        {
            DocumentPageTextView textView = null;
            int i;

            // Try to find pageElement with exact hit.
            for (i = 0; i < _pageTextViews.Count; i++)
            {
                if (_pageTextViews[i].Contains(position))
                {
                    textView = _pageTextViews[i];
                    break;
                }
            }

            return textView;
        }

        /// <summary>
        /// Retrieves an active TextView from DocumentPageView with specified page number.
        /// </summary>
        /// <param name="pageNumber">Page number.</param>
        /// <returns>
        /// Active TextView from DocumentPageView with specified page number.
        /// </returns>
        private DocumentPageTextView GetTextViewFromPageNumber(int pageNumber)
        {
            DocumentPageTextView textView = null;
            int i;

            // Try to find pageElement with exact hit.
            for (i = 0; i < _pageTextViews.Count; i++)
            {
                if (_pageTextViews[i].DocumentPageView.PageNumber == pageNumber)
                {
                    textView = _pageTextViews[i];
                    break;
                }
            }
            return textView;
        }

        /// <summary>
        /// Given page number and count, retrieves an active TextView at distance count from the specified page number
        /// </summary>
        /// <param name="pageNumber">Page number.</param>
        /// <param name="count">Number of pages between specified page number and desired page number</param>
        /// <param name="newPageNumber">Page number of the view that is actually returned</param>
        /// <remarks>
        /// If there is no view at distance count from the specified page number in the list of views, return the page text view that's
        /// closest in the direction of count, i.e. if we're on page 2 and count = 5, and we have pages 5 and 10 in view but not page 7, return
        /// 5, i.e. we never move by > count
        /// </remarks>
        private DocumentPageTextView GetTextViewForNextPage(int pageNumber, int count, out int newPageNumber)
        {
            Invariant.Assert(count != 0);
            newPageNumber = pageNumber + count;
            int closestPageNumber = newPageNumber;
            DocumentPageTextView textView = null;
            int closestDistance = Math.Abs(count);

            for (int i = 0; i < _pageTextViews.Count; i++)
            {
                if (_pageTextViews[i].DocumentPageView.PageNumber == newPageNumber)
                {
                    textView = _pageTextViews[i];
                    closestPageNumber = newPageNumber;
                    break;
                }
                else
                {
                    int currentPageNumber = _pageTextViews[i].DocumentPageView.PageNumber;
                    if (count > 0 && currentPageNumber > pageNumber)
                    {
                        int distance = currentPageNumber - pageNumber;
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            textView = _pageTextViews[i];
                            closestPageNumber = currentPageNumber;
                        }
                    }
                    else if (count < 0 && currentPageNumber < pageNumber)
                    {
                        int distance = Math.Abs(currentPageNumber - pageNumber);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            textView = _pageTextViews[i];
                            closestPageNumber = currentPageNumber;
                        }
                    }
                }
            }

            if (textView != null)
            {
                newPageNumber = closestPageNumber;
            }
            else
            {
                newPageNumber = pageNumber;
                textView = GetTextViewFromPageNumber(pageNumber);
            }
            Invariant.Assert(newPageNumber >= 0);
            return textView;
        }


        /// <summary>
        /// Gets transform to ancestor for inner scope
        /// </summary>
        private Transform GetTransformToAncestor(Visual innerScope)
        {
            // NOTE: TransformToAncestor is safe (will never throw an exception).
            Transform transform = innerScope.TransformToAncestor(_renderScope) as Transform;
            if (transform == null)
            {
                transform = Transform.Identity;
            }
            return transform;
        }

        /// <summary>
        /// Transforms rectangle from inner scope.
        /// </summary>
        private Rect TransformToAncestor(Visual innerScope, Rect rect)
        {
            if (rect != Rect.Empty)
            {
                // NOTE: TransformToAncestor is safe (will never throw an exception).
                GeneralTransform transform = innerScope.TransformToAncestor(_renderScope);
                if (transform != null)
                {
                    rect = transform.TransformBounds(rect);
                }
            }
            return rect;
        }

        /// <summary>
        /// Transforms point from inner scope.
        /// </summary>
        private Point TransformToAncestor(Visual innerScope, Point point)
        {
            // NOTE: TransformToAncestor is safe (will never throw an exception).
            GeneralTransform transform = innerScope.TransformToAncestor(_renderScope);
            if (transform != null)
            {
                point = transform.Transform(point);
            }
            return point;
        }

        /// <summary>
        /// Transforms rectangle from inner scope 
        /// </summary>
        private Point TransformToDescendant(Visual innerScope, Point point)
        {
            // NOTE: TransformToAncestor is safe (will never throw an exception).
            GeneralTransform transform = innerScope.TransformToAncestor(_renderScope);
            if (transform != null)
            {
                transform = transform.Inverse;
                if (transform != null)
                {
                    point = transform.Transform(point);
                }
            }
            return point;
        }

        /// <summary>
        /// Fires BringPositionIntoViewCompleted event.
        /// </summary>
        private void OnBringPositionIntoViewCompleted(BringPositionIntoViewRequest request)
        {
            _pendingRequest = null;
            OnBringPositionIntoViewCompleted(new BringPositionIntoViewCompletedEventArgs(
                request.Position, request.Succeeded, null, false, request.UserState));
        }

        /// <summary>
        /// Fires BringPointIntoViewCompleted event.
        /// </summary>
        private void OnBringPointIntoViewCompleted(BringPointIntoViewRequest request)
        {
            _pendingRequest = null;
            OnBringPointIntoViewCompleted(new BringPointIntoViewCompletedEventArgs(
                request.Point, request.Position,
                request.Position != null, null, false, request.UserState));
        }

        /// <summary>
        /// Fires BringLineIntoViewCompleted event.
        /// </summary>
        private void OnBringLineIntoViewCompleted(BringLineIntoViewRequest request)
        {
            _pendingRequest = null;
            OnBringLineIntoViewCompleted(new BringLineIntoViewCompletedEventArgs(
                request.Position, request.SuggestedX, request.Count,
                request.NewPosition, request.NewSuggestedX, request.Count - request.NewCount,
                request.NewCount == 0, null, false, request.UserState));
        }

        /// <summary>
        /// Fires BringPageIntoViewCompleted event.
        /// </summary>
        private void OnBringPageIntoViewCompleted(BringPageIntoViewRequest request)
        {
            _pendingRequest = null;
            OnBringPageIntoViewCompleted(new BringPageIntoViewCompletedEventArgs(
                request.Position, request.SuggestedOffset, request.Count,
                request.NewPosition, request.NewSuggestedOffset, request.Count - request.NewCount,
                request.NewCount == 0, null, false, request.UserState));
        }

        /// <summary>
        /// Responds to an OnUpdated call.
        /// </summary>
        private object OnUpdatedWorker(object o)
        {
            BringLineIntoViewRequest lineRequest;
            BringPageIntoViewRequest pageRequest;
            BringPointIntoViewRequest pointRequest;
            BringPositionIntoViewRequest positionRequest;
            ITextView pageTextView;
            ITextPointer newPosition;
            Point point;
            double suggestedX;

            if (this.IsValid && _pendingRequest != null)
            {
                if (_pendingRequest is BringLineIntoViewRequest)
                {
                    lineRequest = (BringLineIntoViewRequest)_pendingRequest;

                    // Try to find TextView for DocumentPageView with stored page number.
                    pageTextView = GetTextViewFromPageNumber(lineRequest.NewPageNumber);
                    if (pageTextView != null)
                    {
                        // Transform to DocumentPageView coordinates
                        point = TransformToDescendant(pageTextView.RenderScope, new Point(lineRequest.NewSuggestedX, 0));
                        suggestedX = point.X;

                        // All positions have to be in the TextView boundary.
                        // Since we are quering another TextView, move requested position to its range.
                        if (lineRequest.Count > 0)
                        {
                            // Search for a point just outside the limits so that GetTextView.GetTextPositionFromPoint will not hit test
                            // inside anchored blocks unless they are offset out of the page.
                            newPosition = pageTextView.GetTextPositionFromPoint(new Point(-1, -1), true);
                            if (newPosition != null)
                            {
                                lineRequest.NewCount = lineRequest.NewCount - 1;
                            }
                        }
                        else
                        {
                            newPosition = pageTextView.GetTextPositionFromPoint((Point)pageTextView.RenderScope.RenderSize, true);
                            if (newPosition != null)
                            {
                                lineRequest.NewCount = lineRequest.NewCount + 1;
                            }
                        }

                        // If still have some lines to be moved, do another BringLineIntoView request.
                        // Otherwise the goal has been reached and fire completed event.
                        if (newPosition == null)
                        {
                            // New position cannot be found, return best result so far.
                            if (lineRequest.NewPosition == null)
                            {
                                lineRequest.NewPosition = lineRequest.Position;
                                lineRequest.NewCount = lineRequest.Count;
                            }
                            OnBringLineIntoViewCompleted(lineRequest);
                        }
                        else if (lineRequest.NewCount != 0)
                        {
                            lineRequest.NewPosition = newPosition;
                            BringLineIntoViewCore(lineRequest);
                        }
                        else
                        {
                            lineRequest.NewPosition = GetPositionAtPageBoundary(lineRequest.Count > 0, pageTextView, newPosition, lineRequest.NewSuggestedX);
                            OnBringLineIntoViewCompleted(lineRequest);
                        }
                    }
                    else if (IsPageNumberOutOfRange(lineRequest.NewPageNumber))
                    {
                        OnBringLineIntoViewCompleted(lineRequest);
                    }
                }
                else if (_pendingRequest is BringPageIntoViewRequest)
                {
                    pageRequest = (BringPageIntoViewRequest)_pendingRequest;

                    // Try to find TextView for DocumentPageView with stored page number.
                    pageTextView = GetTextViewFromPageNumber(pageRequest.NewPageNumber);
                    if (pageTextView != null)
                    {
                        // Transform to DocumentPageView coordinates
                        point = TransformToDescendant(pageTextView.RenderScope, pageRequest.NewSuggestedOffset);
                        Point suggestedOffset = point;

                        Invariant.Assert(pageRequest.NewCount != 0);
                        newPosition = pageTextView.GetTextPositionFromPoint(suggestedOffset, true);
                        if (newPosition != null)
                        {
                            pageRequest.NewCount = (pageRequest.Count > 0) ? pageRequest.NewCount - 1 : pageRequest.NewCount + 1;
                        }

                        // If still have some lines to be moved, do another BringLineIntoView request.
                        // Otherwise the goal has been reached and fire completed event.
                        if (newPosition == null)
                        {
                            // New position cannot be found, return best result so far.
                            if (pageRequest.NewPosition == null)
                            {
                                pageRequest.NewPosition = pageRequest.Position;
                                pageRequest.NewCount = pageRequest.Count;
                            }
                            OnBringPageIntoViewCompleted(pageRequest);
                        }
                        else if (pageRequest.NewCount != 0)
                        {
                            pageRequest.NewPosition = newPosition;
                            BringPageIntoViewCore(pageRequest);
                        }
                        else
                        {
                            pageRequest.NewPosition = newPosition;
                            OnBringPageIntoViewCompleted(pageRequest);
                        }
                    }
                    else if (IsPageNumberOutOfRange(pageRequest.NewPageNumber))
                    {
                        OnBringPageIntoViewCompleted(pageRequest);
                    }
                }
                else if (_pendingRequest is BringPointIntoViewRequest)
                {
                    pointRequest = (BringPointIntoViewRequest)_pendingRequest;

                    pageTextView = GetTextViewFromPoint(pointRequest.Point, true);
                    if (pageTextView != null)
                    {
                        // Transform to DocumentPageView coordinates and query inner TextView
                        point = TransformToDescendant(pageTextView.RenderScope, pointRequest.Point);
                        pointRequest.Position = pageTextView.GetTextPositionFromPoint(point, true);
                    }
                    OnBringPointIntoViewCompleted(pointRequest);
                }
                else if (_pendingRequest is BringPositionIntoViewRequest)
                {
                    positionRequest = (BringPositionIntoViewRequest)_pendingRequest;
                    positionRequest.Succeeded = positionRequest.Position.HasValidLayout;
                    OnBringPositionIntoViewCompleted(positionRequest);
                }
            }

            return null;
        }

        /// <summary>
        /// Distinguishes between whether a page is not available now, or will never be available.
        /// </summary>
        private bool IsPageNumberOutOfRange(int pageNumber)
        {
            if (pageNumber < 0)
            {
                return true;
            }

            IDocumentPaginatorSource document = _viewer.Document;
            if (document == null)
            {
                return true;
            }

            DocumentPaginator documentPaginator = document.DocumentPaginator;

            if (documentPaginator == null)
            {
                return true;
            }

            if (documentPaginator.IsPageCountValid && pageNumber >= documentPaginator.PageCount)
            {
                return true;
            }

            return false;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// Viewer associated with TextView.
        /// </summary>
        private readonly DocumentViewerBase _viewer;

        /// <summary>
        /// Root of layout structure visualizing content.
        /// </summary>
        private readonly UIElement _renderScope;

        /// <summary>
        /// TextContainer representing content.
        /// </summary>
        private readonly ITextContainer _textContainer;

        /// <summary>
        /// Collection of hosted TextViews.
        /// </summary>
        private List<DocumentPageTextView> _pageTextViews;

        /// <summary>
        /// Pending BringIntoView request.
        /// </summary>
        private BringIntoViewRequest _pendingRequest;

        #endregion Private Fields

        //-------------------------------------------------------------------
        //
        //  Private Types
        //
        //-------------------------------------------------------------------

        #region Private Types

        /// <summary>
        /// Pending BringIntoView request.
        /// </summary>
        private class BringIntoViewRequest
        {
            internal BringIntoViewRequest(object userState)
            {
                this.UserState = userState;
            }
            internal readonly object UserState;
        }

        /// <summary>
        /// Pending BringPositionIntoView request.
        /// </summary>
        private class BringPositionIntoViewRequest : BringIntoViewRequest
        {
            internal BringPositionIntoViewRequest(ITextPointer position, object userState)
                : base(userState)
            {
                this.Position = position;
                this.Succeeded = false;
            }
            internal readonly ITextPointer Position;
            internal bool Succeeded;
        }

        /// <summary>
        /// Pending BringPointIntoView request.
        /// </summary>
        private class BringPointIntoViewRequest : BringIntoViewRequest
        {
            internal BringPointIntoViewRequest(Point point, object userState)
                : base(userState)
            {
                this.Point = point;
                this.Position = null;
            }
            internal readonly Point Point;
            internal ITextPointer Position;
        }

        /// <summary>
        /// Pending BringLineIntoView request.
        /// </summary>
        private class BringLineIntoViewRequest : BringIntoViewRequest
        {
            internal BringLineIntoViewRequest(ITextPointer position, double suggestedX, int count, object userState)
                : base(userState)
            {
                this.Position = position;
                this.SuggestedX = suggestedX;
                this.Count = count;
                this.NewPosition = position;
                this.NewSuggestedX = suggestedX;
                this.NewCount = count;
            }
            internal readonly ITextPointer Position;
            internal readonly double SuggestedX;
            internal readonly int Count;
            internal ITextPointer NewPosition;
            internal double NewSuggestedX;
            internal int NewCount;
            internal int NewPageNumber;
        }

        /// <summary>
        /// Pending BringPageIntoView request.
        /// </summary>
        private class BringPageIntoViewRequest : BringIntoViewRequest
        {
            internal BringPageIntoViewRequest(ITextPointer position, Point suggestedOffset, int count, object userState)
                : base(userState)
            {
                this.Position = position;
                this.SuggestedOffset = suggestedOffset;
                this.Count = count;
                this.NewPosition = position;
                this.NewSuggestedOffset = suggestedOffset;
                this.NewCount = count;
            }
            internal readonly ITextPointer Position;
            internal readonly Point SuggestedOffset;
            internal readonly int Count;
            internal ITextPointer NewPosition;
            internal Point NewSuggestedOffset;
            internal int NewCount;
            internal int NewPageNumber;
        }

        #endregion Private Types
    }
}

