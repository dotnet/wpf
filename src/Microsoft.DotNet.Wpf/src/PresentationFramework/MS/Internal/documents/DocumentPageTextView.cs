// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: TextView implementation for DocumentPageView.
// 

using System;                               // InvalidOperationException, ...
using System.Collections.Generic;           // List<T>
using System.Collections.ObjectModel;       // ReadOnlyCollection
using System.Windows;                       // Point, Rect, ...
using System.Windows.Controls.Primitives;   // DocumentPageView
using System.Windows.Documents;             // ITextView, ITextContainer
using System.Windows.Media;                 // VisualTreeHelper
using MS.Internal.PtsHost;                  // BackgroundFormatInfo

namespace MS.Internal.Documents
{
    /// <summary>
    /// TextView implementation for DocumentPageView.
    /// </summary>
    internal class DocumentPageTextView : TextViewBase
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
        /// <param name="owner">
        /// Root of layout structure visualizing content of a page.
        /// </param>
        /// <param name="textContainer">
        /// TextContainer representing content.
        /// </param>
        internal DocumentPageTextView(DocumentPageView owner, ITextContainer textContainer)
        {
            Invariant.Assert(owner != null && textContainer != null);
            _owner = owner;
            _page = owner.DocumentPageInternal;
            _textContainer = textContainer;
            // Retrive inner TextView
            if (_page is IServiceProvider)
            {
                _pageTextView = ((IServiceProvider)_page).GetService(typeof(ITextView)) as ITextView;
            }
            if (_pageTextView != null)
            {
                _pageTextView.Updated += new EventHandler(HandlePageTextViewUpdated);
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">
        /// Root of layout structure visualizing content of a page.
        /// </param>
        /// <param name="textContainer">
        /// TextContainer representing content.
        /// </param>
        internal DocumentPageTextView(FlowDocumentView owner, ITextContainer textContainer)
        {
            Invariant.Assert(owner != null && textContainer != null);
            _owner = owner;
            _page = owner.DocumentPage;
            _textContainer = textContainer;
            // Retrive inner TextView
            if (_page is IServiceProvider)
            {
                _pageTextView = ((IServiceProvider)_page).GetService(typeof(ITextView)) as ITextView;
            }
            if (_pageTextView != null)
            {
                _pageTextView.Updated += new EventHandler(HandlePageTextViewUpdated);
            }
        }

        #endregion Constructors

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
            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            if (IsPageMissing)
            {
                return null;
            }
            // Transform to page coordinates.
            point = TransformToDescendant(point);
            return _pageTextView.GetTextPositionFromPoint(point, snapToText);
        }

        /// <summary>
        /// <see cref="ITextView.GetRawRectangleFromTextPosition"/>
        /// </summary>
        internal override Rect GetRawRectangleFromTextPosition(ITextPointer position, out Transform transform)
        {
            Rect rect;
            Transform pageTextViewTransform, ancestorTransform;

            // Initialize transform to identity
            transform = Transform.Identity;

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            if (IsPageMissing)
            {
                return Rect.Empty;
            }

            rect = _pageTextView.GetRawRectangleFromTextPosition(position, out pageTextViewTransform);
            Invariant.Assert(pageTextViewTransform != null);
            ancestorTransform = GetTransformToAncestor();
            transform = GetAggregateTransform(pageTextViewTransform, ancestorTransform);
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

            if (!IsPageMissing)
            {
                geometry = _pageTextView.GetTightBoundingGeometryFromTextPositions(startPosition, endPosition);
                if (geometry != null)
                {
                    Transform transform = GetTransformToAncestor().AffineTransform;
                    CaretElement.AddTransformToGeometry(geometry, transform);
                }
            }

            return (geometry);
        }

        /// <summary>
        /// <see cref="ITextView.GetPositionAtNextLine"/>
        /// </summary>
        internal override ITextPointer GetPositionAtNextLine(ITextPointer position, double suggestedX, int count, out double newSuggestedX, out int linesMoved)
        {
            ITextPointer positionOut;
            Point offset;

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            if (IsPageMissing)
            {
                newSuggestedX = suggestedX;
                linesMoved = 0;
                return position;
            }

            offset = TransformToDescendant(new Point(suggestedX, 0));
            suggestedX = offset.X;
            positionOut = _pageTextView.GetPositionAtNextLine(position, suggestedX, count, out newSuggestedX, out linesMoved);
            offset = TransformToAncestor(new Point(newSuggestedX, 0));
            newSuggestedX = offset.X;

            return positionOut;
        }

        /// <summary>
        /// <see cref="ITextView.GetPositionAtNextPage"/>
        /// </summary>
        internal override ITextPointer GetPositionAtNextPage(ITextPointer position, Point suggestedOffset, int count, out Point newSuggestedOffset, out int pagesMoved)
        {
            ITextPointer positionOut = position;
            newSuggestedOffset = suggestedOffset;
            Point offset = suggestedOffset;
            pagesMoved = 0;

            if (count == 0)
            {
                return positionOut;
            }

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            if (IsPageMissing)
            {
                return position;
            }

            // Calculate distance from current position
            offset.Y = GetYOffsetAtNextPage(offset.Y, count, out pagesMoved);

            if (pagesMoved != 0)
            {
                // Transfrom offset to _pageTextView's coordinate position, obtain position from _pageTextView and convert back
                offset = TransformToDescendant(offset);
                positionOut = _pageTextView.GetTextPositionFromPoint(offset, true);
                Invariant.Assert(positionOut != null);
                Rect rect = _pageTextView.GetRectangleFromTextPosition(position);
                newSuggestedOffset = TransformToAncestor(new Point(rect.X, rect.Y));
            }

            return positionOut;
        }

        /// <summary>
        /// <see cref="ITextView.IsAtCaretUnitBoundary"/>
        /// </summary>
        internal override bool IsAtCaretUnitBoundary(ITextPointer position)
        {
            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            if (IsPageMissing)
            {
                return false;
            }
            return _pageTextView.IsAtCaretUnitBoundary(position);
        }

        /// <summary>
        /// <see cref="ITextView.GetNextCaretUnitPosition"/>
        /// </summary>
        internal override ITextPointer GetNextCaretUnitPosition(ITextPointer position, LogicalDirection direction)
        {
            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            if (IsPageMissing)
            {
                return null;
            }
            return _pageTextView.GetNextCaretUnitPosition(position, direction);
        }

        /// <summary>
        /// <see cref="ITextView.GetBackspaceCaretUnitPosition"/>
        /// </summary>
        internal override ITextPointer GetBackspaceCaretUnitPosition(ITextPointer position)
        {
            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            if (IsPageMissing)
            {
                return null;
            }
            return _pageTextView.GetBackspaceCaretUnitPosition(position);
        }

        /// <summary>
        /// <see cref="ITextView.GetLineRange"/>
        /// </summary>
        internal override TextSegment GetLineRange(ITextPointer position)
        {
            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            if (IsPageMissing)
            {
                return TextSegment.Null;
            }
            return _pageTextView.GetLineRange(position);
        }

        /// <summary>
        /// <see cref="ITextView.GetGlyphRuns"/>
        /// </summary>
        internal override ReadOnlyCollection<GlyphRun> GetGlyphRuns(ITextPointer start, ITextPointer end)
        {
            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            if (IsPageMissing)
            {
                return new ReadOnlyCollection<GlyphRun>(new List<GlyphRun>());
            }
            return _pageTextView.GetGlyphRuns(start, end);
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
            if (IsPageMissing)
            {
                return false;
            }
            return _pageTextView.Contains(position);
        }

        /// <summary>
        /// Handler for PageConnected raised by the DocumentPageView.
        /// </summary>
        internal void OnPageConnected()
        {
            OnPageDisconnected();
            if (_owner is DocumentPageView)
            {
                _page = ((DocumentPageView)_owner).DocumentPageInternal;
            }
            else if (_owner is FlowDocumentView)
            {
                _page = ((FlowDocumentView)_owner).DocumentPage;
            }

            if (_page is IServiceProvider)
            {
                _pageTextView = ((IServiceProvider)_page).GetService(typeof(ITextView)) as ITextView;
            }
            if (_pageTextView != null)
            {
                _pageTextView.Updated += new EventHandler(HandlePageTextViewUpdated);
            }
            if (IsValid)
            {
                OnUpdated(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Handler for PageDisconnected raised by the DocumentPageView.
        /// </summary>
        internal void OnPageDisconnected()
        {
            if (_pageTextView != null)
            {
                _pageTextView.Updated -= new EventHandler(HandlePageTextViewUpdated);
            }
            _pageTextView = null;
            _page = null;
        }

        /// <summary>
        /// Handler for TransformChanged raised by the DocumentPageView.
        /// </summary>
        internal void OnTransformChanged()
        {
            if (IsValid)
            {
                OnUpdated(EventArgs.Empty);
            }
        }

        /// <summary>
        /// <see cref="ITextView.Validate()"/>
        /// </summary>
        internal override bool Validate()
        {
            if (!_owner.IsMeasureValid || !_owner.IsArrangeValid)
            {
                _owner.UpdateLayout();
            }
            return (_pageTextView != null && _pageTextView.Validate());
        }

        /// <summary>
        /// <see cref="ITextView.Validate(ITextPointer)"/>
        /// </summary>
        internal override bool Validate(ITextPointer position)
        {
            FlowDocumentView owner = _owner as FlowDocumentView;
            bool isValid;

            if (owner == null || owner.Document == null)
            {
                isValid = base.Validate(position);
            }
            else
            {
                if (Validate())
                {
                    BackgroundFormatInfo backgroundFormatInfo = owner.Document.StructuralCache.BackgroundFormatInfo;
                    FlowDocumentFormatter formatter = owner.Document.BottomlessFormatter;

                    int lastCPInterrupted = -1;

                    while (this.IsValid && !Contains(position))
                    {
                        backgroundFormatInfo.BackgroundFormat(formatter, true /* ignoreThrottle */);
                        _owner.UpdateLayout(); // May invalidate the view.

                        // Break if background layout is not progressing.
                        // There are some edge cases where Validate() == true, but background
                        // layout will not progress (such as collapsed text in a tree view).
                        if (backgroundFormatInfo.CPInterrupted <= lastCPInterrupted)
                        {
                            // CPInterrupted is reset to -1 when background layout finishes, so
                            // check explicitly below to see if the position is valid or not.
                            break;
                        }
                        lastCPInterrupted = backgroundFormatInfo.CPInterrupted;
                    }
                }

                isValid = this.IsValid && Contains(position);
            }

            return isValid;
        }

        /// <summary>
        /// <see cref="ITextView.ThrottleBackgroundTasksForUserInput"/>
        /// </summary>
        internal override void ThrottleBackgroundTasksForUserInput()
        {
            FlowDocumentView owner = _owner as FlowDocumentView;

            if (owner != null && owner.Document != null)
            {
                owner.Document.StructuralCache.ThrottleBackgroundFormatting();
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
        /// <see cref="ITextView.RenderScope"/>
        /// </summary>
        internal override UIElement RenderScope
        {
            get { return _owner; }
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
                if (!_owner.IsMeasureValid || !_owner.IsArrangeValid || _page == null)
                {
                    return false;
                }
                if (IsPageMissing)
                {
                    return true;
                }
                return (_pageTextView != null && _pageTextView.IsValid);
            }
        }

        /// <summary>
        /// <see cref="ITextView.RendersOwnSelection"/>
        /// </summary>
        internal override bool RendersOwnSelection
        {
            get
            {
                if (_pageTextView != null)
                {
                    return _pageTextView.RendersOwnSelection;
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
                if (!IsValid || IsPageMissing)
                {
                    return new ReadOnlyCollection<TextSegment>(new List<TextSegment>());
                }
                return _pageTextView.TextSegments;
            }
        }

        /// <summary>
        /// DocumentPageView associated with this TextView.
        /// </summary>
        internal DocumentPageView DocumentPageView
        {
            get { return _owner as DocumentPageView; }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Handler for Updated event raised by the inner TextView.
        /// </summary>
        private void HandlePageTextViewUpdated(object sender, EventArgs e)
        {
            Invariant.Assert(_pageTextView != null);
            if (sender == _pageTextView)
            {
                OnUpdated(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets transform to ancestor
        /// </summary>
        private Transform GetTransformToAncestor()
        {
            Invariant.Assert(IsValid && !IsPageMissing);
            // NOTE: TransformToAncestor is safe (will never throw an exception).
            Transform transform = _page.Visual.TransformToAncestor(_owner) as Transform;
            if (transform == null)
            {
                transform = Transform.Identity;
            }
            return transform;
        }

        /// <summary>
        /// Transforms point from inner scope.
        /// </summary>
        private Point TransformToAncestor(Point point)
        {
            Invariant.Assert(IsValid && !IsPageMissing);
            // NOTE: TransformToAncestor is safe (will never throw an exception).
            GeneralTransform transform = _page.Visual.TransformToAncestor(_owner);
            if (transform != null)
            {
                point = transform.Transform(point);
            }
            return point;
        }

        /// <summary>
        /// Transforms rectangle from inner scope 
        /// </summary>
        private Point TransformToDescendant(Point point)
        {
            Invariant.Assert(IsValid && !IsPageMissing);
            // NOTE: TransformToAncestor is safe (will never throw an exception).
            GeneralTransform transform = _page.Visual.TransformToAncestor(_owner);
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
        /// Gets updated offset at next page
        /// </summary>
        /// <param name="offset"></param>
        /// Current value of offset
        /// <param name="count">
        /// Number of pages to move
        /// </param>
        /// <param name="pagesMoved">
        /// Number of pages actually moved
        /// </param>
        private double GetYOffsetAtNextPage(double offset, int count, out int pagesMoved)
        {
            double newOffset = offset;
            pagesMoved = 0;
            if (_owner is IScrollInfo && ((IScrollInfo)_owner).ScrollOwner != null)
            {
                IScrollInfo scrollInfo = (IScrollInfo)_owner;
                double viewportHeight = scrollInfo.ViewportHeight;
                double extentHeight = scrollInfo.ExtentHeight;
                if (count > 0)
                {
                    while (pagesMoved < count)
                    {
                        if (DoubleUtil.LessThanOrClose(offset + viewportHeight, extentHeight))
                        {
                            newOffset += viewportHeight;
                            pagesMoved++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    while (Math.Abs(pagesMoved) < Math.Abs(count))
                    {
                        if (DoubleUtil.GreaterThanOrClose(offset - viewportHeight, 0))
                        {
                            newOffset -= viewportHeight;
                            pagesMoved--;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            return newOffset;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Properties
        //
        //-------------------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// Whether page is missing.
        /// </summary>
        private bool IsPageMissing
        {
            get { return (_page == DocumentPage.Missing); }
        }

        #endregion Private Properties

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// Root of layout structure visualizing content.
        /// </summary>
        private readonly UIElement _owner;

        /// <summary>
        /// TextContainer representing content.
        /// </summary>
        private readonly ITextContainer _textContainer;

        /// <summary>
        /// DocumentPage associated with this view.
        /// </summary>
        private DocumentPage _page;

        /// <summary>
        /// TextView associated with DocumentPage.
        /// </summary>
        private ITextView _pageTextView;

        #endregion Private Fields
    }
}


