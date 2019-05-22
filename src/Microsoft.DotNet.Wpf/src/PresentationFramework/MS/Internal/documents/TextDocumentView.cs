// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: TextView implementation for FlowDocument pages. 
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls.Primitives; // IScrollInfo
using System.Windows.Documents;
using System.Windows.Media;
using MS.Internal.PtsHost;
using MS.Internal.Text;

namespace MS.Internal.Documents
{
    /// <summary>
    /// TextView implementation for FlowDocument pages.
    /// </summary>
    internal class TextDocumentView : TextViewBase
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
        /// Root of layout structure visualizing content.
        /// </param>
        /// <param name="textContainer">
        /// TextContainer providing content for this view.
        /// </param>
        internal TextDocumentView(FlowDocumentPage owner, ITextContainer textContainer)
        {
            _owner = owner;
            _textContainer = textContainer;
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

            _owner.EnsureValidVisuals();

            // Transforms point to content's coordinate system.
            TransformToContent(ref point);

            // Search columns
            return GetTextPositionFromPoint(Columns, FloatingElements, point, snapToText);
        }

        /// <summary>
        /// <see cref="ITextView.GetRawRectangleFromTextPosition"/>
        /// </summary>
        /// <remarks>
        /// TextDocumentView does not calculate any transform for this function. Transform returned is always identity.
        /// </remarks>
        internal override Rect GetRawRectangleFromTextPosition(ITextPointer position, out Transform transform)
        {
            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            ValidationHelper.VerifyPosition(_textContainer, position, "position");
            if (!ContainsCore(position))
            {
                throw new ArgumentOutOfRangeException("position");
            }
            _owner.EnsureValidVisuals();

            Rect rect = GetRectangleFromTextPosition(Columns, FloatingElements, position);

            // Transforms Rect from content's coordinate system.
            TransformFromContent(ref rect, out transform);

            return rect;
        }

        /// <summary>
        /// <see cref="TextViewBase.GetTightBoundingGeometryFromTextPositions"/>
        /// </summary>
        internal override Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition)
        {
            Geometry geometry = null;

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }

            ValidationHelper.VerifyPosition(_textContainer, startPosition, "startPosition");
            ValidationHelper.VerifyPosition(_textContainer, endPosition, "endPosition");

            _owner.EnsureValidVisuals();

            // Get visible rect, adjusted to owner's content offset
            Rect visibleRect = CalculateViewportRect();

            // First check floating elements
            bool success = false;
            if (FloatingElements.Count > 0)
            {
                Geometry floatingElementGeometry = GetTightBoundingGeometryFromTextPositionsInFloatingElements(FloatingElements, startPosition, endPosition, 0.0, visibleRect, out success);
                // Add it to a geometry
                CaretElement.AddGeometry(ref geometry, floatingElementGeometry);
                // Add content't transform to geometry.
                if (geometry != null)
                {
                    TransformFromContent(geometry);
                }
            }

            if (!success)
            {
                // Not found in floating elements, check columns 
                Invariant.Assert(geometry == null);

                //  Note: since flow may decide to do calculations in background we 
                //  have to clamp by the current pointer range read from text segments. 
                ReadOnlyCollection<TextSegment> textSegments = TextSegments;
                for (int segmentIndex = 0; segmentIndex < textSegments.Count; segmentIndex++)
                {
                    TextSegment textSegment = textSegments[segmentIndex];

                    // Identify boundary positions for this segment
                    ITextPointer startPositionInThisSegment = startPosition.CompareTo(textSegment.Start) > 0 ? startPosition : textSegment.Start;
                    ITextPointer endPositionInThisSegment = endPosition.CompareTo(textSegment.End) < 0 ? endPosition : textSegment.End;

                    // Skip the segment if not crossed by the selection
                    if (startPositionInThisSegment.CompareTo(endPositionInThisSegment) >= 0)
                    {
                        continue;
                    }
                    // Geometry not found in floating elements
                    // Run loop for all columns
                    ReadOnlyCollection<ColumnResult> columns = Columns;
                    for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
                    {
                        // Skip the column if it is not in visible area
                        Rect columnBox = columns[columnIndex].LayoutBox;

                        // Ignore horizontal offset because TextBox page width != extent width.
                        // It's ok to include content that doesn't strictly intersect -- this
                        // is a perf optimization and the edge cases won't significantly hurt us.
                        columnBox.X = visibleRect.X;

                        if (!columnBox.IntersectsWith(visibleRect))
                        {
                            continue;
                        }

                        // Build a highlight for this column
                        Geometry columnGeometry = GetTightBoundingGeometryFromTextPositionsHelper(columns[columnIndex].Paragraphs, startPositionInThisSegment, endPositionInThisSegment, 0.0, visibleRect);

                        // Add it to a geometry
                        CaretElement.AddGeometry(ref geometry, columnGeometry);
                    }
                    // Add content't transform to geometry.
                    if (geometry != null)
                    {
                        TransformFromContent(geometry);
                    }
                }
            }

            return (geometry);
        }

        // ------------------------------------------------------------------
        /// CalculateViewportRect - Called to calculate the visible rect for this element
        // ------------------------------------------------------------------
        private Rect CalculateViewportRect()
        {
            Rect visibleRect = Rect.Empty;
            if (RenderScope is IScrollInfo)
            {
                IScrollInfo scrollInfo = (IScrollInfo)RenderScope;
                if (scrollInfo.ViewportWidth != 0 && scrollInfo.ViewportHeight != 0)
                {
                    visibleRect = new Rect(scrollInfo.HorizontalOffset, scrollInfo.VerticalOffset, scrollInfo.ViewportWidth, scrollInfo.ViewportHeight);
                }
            }

            if (visibleRect.IsEmpty)
            {
                visibleRect = _owner.Viewport;
            }

            TransformToContent(ref visibleRect);

            return visibleRect;
        }

        /// <summary>
        /// <see cref="ITextView.GetPositionAtNextLine"/>
        /// </summary>
        internal override ITextPointer GetPositionAtNextLine(ITextPointer position, double suggestedX, int count, out double newSuggestedX, out int linesMoved)
        {
            ITextPointer positionOut;
            bool positionFound;

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            ValidationHelper.VerifyPosition(_textContainer, position, "position");
            if (!ContainsCore(position))
            {
                throw new ArgumentOutOfRangeException("position");
            }

            _owner.EnsureValidVisuals();

            // Initialy set linesMoved to 0 and newSuggestedX to suggestedX. Transform suggestedX to content
            Point point = new Point(suggestedX, 0);
            TransformToContent(ref point);
            suggestedX = newSuggestedX = point.X;
            linesMoved = count;

            if (count == 0)
            {
                return position;
            }

            positionOut = GetPositionAtNextLine(Columns, FloatingElements, position, suggestedX, ref count, out newSuggestedX, out positionFound);
            linesMoved -= count;
            point = new Point(newSuggestedX, 0);
            TransformFromContent(ref point);
            newSuggestedX = point.X;

            // There might be a case when returned position is not in the view. 
            // Example: <P>A<Figure/><LineBreak/></P> and the figure is delayed to the next page.
            //          In such case, TextSegments do not contain content of Figure element and </P>.
            //          Paragraph itself has 2 lines. The second line is empty and its position
            //          cannot be represented as TextPointer belonging to TextSegments, because
            //          Backward direction belongs to the first line and the Forward direction
            //          belongs to the next page.
            if (positionOut == null || !ContainsCore(positionOut))
            {
                positionOut = position;
                linesMoved = 0;
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
            ValidationHelper.VerifyPosition(_textContainer, position, "position");
            if (!ContainsCore(position))
            {
                throw new ArgumentOutOfRangeException("position");
            }

            return IsAtCaretUnitBoundary(Columns, FloatingElements, position);
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
            ValidationHelper.VerifyPosition(_textContainer, position, "position");
            ValidationHelper.VerifyDirection(direction, "direction");
            if (!ContainsCore(position))
            {
                throw new ArgumentOutOfRangeException("position");
            }

            return GetNextCaretUnitPosition(Columns, FloatingElements, position, direction);
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
            ValidationHelper.VerifyPosition(_textContainer, position, "position");
            if (!ContainsCore(position))
            {
                throw new ArgumentOutOfRangeException("position");
            }

            return GetBackspaceCaretUnitPosition(Columns, FloatingElements, position);
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
            ValidationHelper.VerifyPosition(_textContainer, position, "position");
            if (!ContainsCore(position))
            {
                throw new ArgumentOutOfRangeException("position");
            }

            return GetLineRangeFromPosition(Columns, FloatingElements, position);
        }

        /// <summary>
        /// <see cref="ITextView.GetGlyphRuns"/>
        /// </summary>
        internal override ReadOnlyCollection<GlyphRun> GetGlyphRuns(ITextPointer start, ITextPointer end)
        {
            List<GlyphRun> glyphRuns = new List<GlyphRun>();

            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }
            ValidationHelper.VerifyPosition(_textContainer, start, "start");
            ValidationHelper.VerifyPosition(_textContainer, end, "end");
            ValidationHelper.VerifyPositionPair(start, end);
            if (!ContainsCore(start))
            {
                throw new ArgumentOutOfRangeException("start");
            }
            if (!ContainsCore(end))
            {
                throw new ArgumentOutOfRangeException("end");
            }

            GetGlyphRuns(glyphRuns, start, end, Columns, FloatingElements);

            return new ReadOnlyCollection<GlyphRun>(glyphRuns);
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
            ValidationHelper.VerifyPosition(_textContainer, position, "position");
            return ContainsCore(position);
        }

        /// <summary>
        /// <see cref="ITextView.Validate()"/>
        /// </summary>
        internal override bool Validate()
        {
            return this.IsValid;
        }

        /// <summary>
        /// <see cref="ITextView.ThrottleBackgroundTasksForUserInput"/>
        /// </summary>
        internal override void ThrottleBackgroundTasksForUserInput()
        {
            _owner.StructuralCache.ThrottleBackgroundFormatting();
        }

        /// <summary>
        /// Returns a cellinfo class for a point that may be inside of a cell
        /// </summary>
        /// <param name="point">
        /// Point to hit test
        /// </param>
        /// <param name="tableFilter">
        /// Filter out all results not specific to a given table
        /// </param>
        /// <returns>
        /// Returns cellinfo structure.
        /// </returns>
        internal CellInfo GetCellInfoFromPoint(Point point, Table tableFilter)
        {
            // Verify that layout information is valid. Cannot continue if not valid.
            if (!IsValid)
            {
                throw new InvalidOperationException(SR.Get(SRID.TextViewInvalidLayout));
            }

            return GetCellInfoFromPoint(Columns, FloatingElements, point, tableFilter);
        }

        /// <summary>
        /// Raise TextView.Updated event.
        /// </summary>
        internal void OnUpdated()
        {
            OnUpdated(EventArgs.Empty);
        }

        /// <summary>
        /// Invalidate TextView internal state.
        /// </summary>
        internal void Invalidate()
        {
            _columns = null;
            _segments = null;
            _floatingElements = null;
        }

        /// <summary>
        /// Determines whenever TextSegment collection contains specified position.
        /// </summary>
        /// <param name="position">A position to test.</param>
        /// <param name="segments">Collection of TextSegments to test against.</param>
        /// <returns>
        /// True if TextSegment collection contains specified text position. 
        /// Otherwise returns false.
        /// </returns>
        internal static bool Contains(ITextPointer position, ReadOnlyCollection<TextSegment> segments)
        {
            bool contains = false;

            Invariant.Assert(segments != null);
            // Iterate through all segments and check if position is inside one of them.
            // Position is inside of a segment if:
            // a) it is between Start and End boundaries (exclusive), or
            // b) at the Start and direction is Forward, or
            // c) at the End and direction is Backward
            foreach (TextSegment segment in segments)
            {
                if (segment.Start.CompareTo(position) < 0 && segment.End.CompareTo(position) > 0)
                {
                    contains = true;
                    break;
                }
                if (segment.Start.CompareTo(position) == 0)
                {
                    if (position.LogicalDirection == LogicalDirection.Forward)
                    {
                        // Position has forward context, and is always contained in the view whether the segment start has
                        // forward or backward context
                        contains = true;
                        break;
                    }
                    else
                    {
                        // Position has backward context. It is contained in the segment only if the segment start also has backward context
                        if (segment.Start.LogicalDirection == LogicalDirection.Backward)
                        {
                            contains = true;
                            break;
                        }
                    }
                }
                if (segment.End.CompareTo(position) == 0)
                {
                    if (position.LogicalDirection == LogicalDirection.Backward)
                    {
                        // Position has backward context, and is always contained in the view whether the segment end has
                        // forward or backward context
                        contains = true;
                        break;
                    }
                    else
                    {
                        // Position has forward context. It is contained in the segment only if the segment end also has forward context
                        if (segment.End.LogicalDirection == LogicalDirection.Forward)
                        {
                            contains = true;
                            break;
                        }
                    }
                }
            }
            // If position is at the beginning or the end of TextContainer, ignore
            // its direction, because it is necessary to treat such positions as valid 
            // for editing scenarios.
            if (!contains && segments.Count > 0)
            {
                if (position.TextContainer.Start.CompareTo(position) == 0 && position.LogicalDirection == LogicalDirection.Backward)
                {
                    contains = (position.TextContainer.Start.CompareTo(segments[0].Start) == 0);
                }
                else if (position.TextContainer.End.CompareTo(position) == 0 && position.LogicalDirection == LogicalDirection.Forward)
                {
                    contains = (position.TextContainer.End.CompareTo(segments[segments.Count - 1].End) == 0);
                }
            }
            return contains;
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
            get
            {
                UIElement renderScope = null;
                if (!_owner.IsDisposed)
                {
                    // The RenderScope in this case is typically DocumentPageView
                    Visual visual = _owner.Visual;
                    while (visual != null && !(visual is UIElement))
                    {
                        visual = VisualTreeHelper.GetParent(visual) as Visual;
                    }
                    renderScope = visual as UIElement;
                }
                return renderScope;
            }
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
            get { return _owner.IsLayoutDataValid; }
        }

        /// <summary>
        /// <see cref="ITextView.TextSegments"/>
        /// </summary>
        internal override ReadOnlyCollection<TextSegment> TextSegments
        {
            get
            {
                // Verify that layout information is valid. Cannot continue if not valid.
                if (!IsValid)
                {
                    return new ReadOnlyCollection<TextSegment>(new List<TextSegment>());
                }
                return this.TextSegmentsCore;
            }
        }
        private ReadOnlyCollection<TextSegment> TextSegmentsCore
        {
            get
            {
                if (_segments == null)
                {
                    _segments = GetTextSegments();
                    Invariant.Assert(_segments != null, "TextSegment collection is empty.");
                }
                return _segments;
            }
        }

        /// <summary>
        /// Collection of ColumnResults for each line in the paragraph.
        /// </summary>
        internal ReadOnlyCollection<ColumnResult> Columns
        {
            get
            {
                Invariant.Assert(IsValid, "TextView is not updated.");
                if (_columns == null)
                {
                    // When getting column results, query each on for text content, used to determine if the view has text content
                    _columns = _owner.GetColumnResults(out _hasTextContent);
                    Invariant.Assert(_columns != null, "Column collection is null.");
                }
                return _columns;
            }
        }

        /// <summary>
        /// Collection of ParagraphResults for floating elements
        /// </summary>
        internal ReadOnlyCollection<ParagraphResult> FloatingElements
        {
            get
            {
                Invariant.Assert(IsValid, "TextView is not updated.");
                if (_floatingElements == null)
                {
                    _floatingElements = _owner.FloatingElementResults;
                    Invariant.Assert(_floatingElements != null, "Floating elements collection is null.");
                }
                return _floatingElements;
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
        /// Retrieves a position matching a point. Checks floating elements first.
        /// </summary>
        /// <param name="paragraphs">
        /// Collection of paragraphs.
        /// </param>
        /// <param name="floatingElements">
        /// Collection of floating elements
        /// </param>
        /// <param name="point">
        /// Point in pixel coordinates to test.
        /// </param>
        /// <param name="snapToText">
        /// If true, this method must always return a positioned text position 
        /// (the closest position as calculated by the control's heuristics). 
        /// If false, this method should return null position, if the test 
        /// point does not fall within any character bounding box.
        /// </param>
        /// <param name="snapToTextInFloatingElements">
        /// Indicates that none of the paragraphs in the collection has any text content so we must return something from the floating elements collection
        /// </param>
        /// <returns>
        /// A text position and its orientation matching or closest to the point.
        /// </returns>
        private ITextPointer GetTextPositionFromPoint(ReadOnlyCollection<ParagraphResult> paragraphs, ReadOnlyCollection<ParagraphResult> floatingElements, Point point, bool snapToText, bool snapToTextInFloatingElements)
        {
            ITextPointer position;
            int paragraphIndex;

            Invariant.Assert(paragraphs != null, "Paragraph collection is empty.");
            Invariant.Assert(floatingElements != null, "Floating element collection is empty.");

            // Figure out which paragraph is the closest to the input pixel position. First search floating elements
            paragraphIndex = GetParagraphFromPointInFloatingElements(floatingElements, point, snapToTextInFloatingElements);
            ParagraphResult paragraph;
            if (paragraphIndex < 0)
            {
                // Not found in floating elements
                Invariant.Assert(!snapToTextInFloatingElements || floatingElements.Count == 0, "When snap to text is enabled a valid text position is required if paragraphs exist.");
                if (snapToTextInFloatingElements)
                {
                    return null;
                }
                else
                {
                    // Keep searching paragraphs
                    paragraphIndex = GetParagraphFromPoint(paragraphs, point, snapToText);
                    // If no paragraph is hit, return null text position.
                    // Otherwise hittest paragraph content.
                    if (paragraphIndex < 0)
                    {
                        Invariant.Assert(!snapToText || paragraphs.Count == 0, "When snap to text is enabled a valid text position is required if paragraphs exist.");
                        return null;
                    }
                    else
                    {
                        Invariant.Assert(paragraphIndex < paragraphs.Count);
                        paragraph = paragraphs[paragraphIndex];
                    }
                }
            }
            else
            {
                Invariant.Assert(paragraphIndex < floatingElements.Count);
                paragraph = floatingElements[paragraphIndex];
            }

            position = GetTextPositionFromPoint(paragraph, point, snapToText);
            return position;
        }

        /// <summary>
        /// Retrieves a position matching a point from a given paragraph
        /// </summary>
        /// <param name="paragraph">
        /// Para containing position
        /// </param>
        /// <param name="point">
        /// Point in pixel coordinates to test.
        /// </param>
        /// <param name="snapToText">
        /// If true, this method must always return a positioned text position 
        /// (the closest position as calculated by the control's heuristics). 
        /// If false, this method should return null position, if the test 
        /// point does not fall within any character bounding box.
        /// </param>
        private ITextPointer GetTextPositionFromPoint(ParagraphResult paragraph, Point point, bool snapToText)
        {
            ITextPointer position = null;
            Rect paragraphBox = paragraph.LayoutBox;
            // Position is retrieved differently for different paragraph types:
            // a) ContainerParagraph, FigureParagraph, FloaterParagraph - hittest colleciton of nested paragraphs.
            // b) TextParagraph - hittest line collection.
            // c) TableParagraph - hittest in table
            // d) Other paragraphs - return position before/after paragraph element.
            if (paragraph is ContainerParagraphResult)
            {
                // a) ContainerParagraph - hittest colleciton of nested paragraphs.
                ReadOnlyCollection<ParagraphResult> nestedParagraphs = ((ContainerParagraphResult)paragraph).Paragraphs;
                // Paragraphs collection may be null in case of empty List element,
                Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null.");
                if (nestedParagraphs.Count > 0)
                {
                    position = GetTextPositionFromPoint(nestedParagraphs, _emptyParagraphCollection, point, snapToText, /* snap to text for floating elements*/ false);
                }
                else
                {
                    // Return position before/after paragraph element.
                    if (point.X <= paragraphBox.Width)
                    {
                        position = paragraph.StartPosition.CreatePointer(LogicalDirection.Forward);
                    }
                    else
                    {
                        position = paragraph.EndPosition.CreatePointer(LogicalDirection.Backward);
                    }
                }
            }
            else if (paragraph is TextParagraphResult)
            {
                // b) TextParagraph - hittest line collection.
                ReadOnlyCollection<LineResult> lines = ((TextParagraphResult)paragraph).Lines;
                Invariant.Assert(lines != null, "Lines collection is null");
                if (!((TextParagraphResult)paragraph).HasTextContent)
                {
                    position = null;
                }
                else
                {
                    position = TextParagraphView.GetTextPositionFromPoint(lines, point, snapToText);
                }
            }
            else if (paragraph is TableParagraphResult)
            {
                ReadOnlyCollection<ParagraphResult> rowParagraphs = ((TableParagraphResult)paragraph).Paragraphs;
                Invariant.Assert(rowParagraphs != null, "Paragraph collection is null.");

                int index = GetParagraphFromPoint(rowParagraphs, point, snapToText);
                if (index != -1)
                {
                    ParagraphResult rowResult = rowParagraphs[index];

                    if (point.X > rowResult.LayoutBox.Right)
                    {
                        position = ((TextElement)rowResult.Element).ElementEnd;
                    }
                    else
                    {
                        ReadOnlyCollection<ParagraphResult> nestedParagraphs = ((TableParagraphResult)paragraph).GetParagraphsFromPoint(point, snapToText);

                        position = GetTextPositionFromPoint(nestedParagraphs, _emptyParagraphCollection, point, snapToText, false);
                    }
                }
                else
                {
                    // Table is empty.
                    position = null;
                    // When snap to text is enabled a valid text position is required.
                    if (snapToText)
                    {
                        position = ((TextElement)paragraph.Element).ContentStart;
                    }
                }
            }
            else if (paragraph is SubpageParagraphResult)
            {
                // Subpage implies new coordinate system.
                SubpageParagraphResult subpageParagraphResult = (SubpageParagraphResult)paragraph;
                point.X -= subpageParagraphResult.ContentOffset.X;
                point.Y -= subpageParagraphResult.ContentOffset.Y;

                // WOOT! COLUMNS!
                position = GetTextPositionFromPoint(subpageParagraphResult.Columns, subpageParagraphResult.FloatingElements, point, snapToText);
            }
            else if (paragraph is FigureParagraphResult || paragraph is FloaterParagraphResult)
            {
                ReadOnlyCollection<ColumnResult> columns;
                ReadOnlyCollection<ParagraphResult> nestedFloatingElements;
                if (paragraph is FloaterParagraphResult)
                {
                    FloaterParagraphResult floaterParagraphResult = (FloaterParagraphResult)paragraph;
                    columns = floaterParagraphResult.Columns;
                    nestedFloatingElements = floaterParagraphResult.FloatingElements;
                    TransformToSubpage(ref point, floaterParagraphResult.ContentOffset);
                }
                else
                {
                    FigureParagraphResult figureParagraphResult = (FigureParagraphResult)paragraph;
                    columns = figureParagraphResult.Columns;
                    nestedFloatingElements = figureParagraphResult.FloatingElements;
                    TransformToSubpage(ref point, figureParagraphResult.ContentOffset);
                }

                // Paragraphs collection may be null in case of empty List element,
                Invariant.Assert(columns != null, "Columns collection is null.");
                Invariant.Assert(nestedFloatingElements != null, "Floating elements collection is null.");
                if (columns.Count > 0 || nestedFloatingElements.Count > 0)
                {
                    position = GetTextPositionFromPoint(columns, nestedFloatingElements, point, snapToText);
                }
                else
                {
                    position = null;
                }
            }
            else if (paragraph is UIElementParagraphResult)
            {
                BlockUIContainer blockUIContainer = paragraph.Element as BlockUIContainer;
                if (blockUIContainer != null)
                {
                    position = null;
                    if (paragraphBox.Contains(point) || snapToText)
                    {
                        // Point is with  BUIC's layout box. Return paragraph's ContentStart/End as appropriate
                        if (DoubleUtil.LessThanOrClose(point.X, paragraphBox.X + paragraphBox.Width / 2))
                        {
                            position = blockUIContainer.ContentStart.CreatePointer(LogicalDirection.Forward);
                        }
                        else
                        {
                            position = blockUIContainer.ContentEnd.CreatePointer(LogicalDirection.Backward);
                        }
                    }
                }
            }
            else
            {
                // d) Other paragraphs - return position before/after paragraph element.
                if (point.X <= paragraphBox.Width)
                {
                    position = paragraph.StartPosition.CreatePointer(LogicalDirection.Forward);
                }
                else
                {
                    position = paragraph.EndPosition.CreatePointer(LogicalDirection.Backward);
                }
            }
            return position;
        }

        /// <summary>
        /// Retrieves a position matching a point in a paragraph collection.
        /// </summary>
        private ITextPointer GetTextPositionFromPoint(ReadOnlyCollection<ColumnResult> columns, ReadOnlyCollection<ParagraphResult> floatingElements, Point point, bool snapToText)
        {
            ITextPointer position = null;
            Invariant.Assert(floatingElements != null);

            int columnIndex = GetColumnFromPoint(columns, point, snapToText);
            // If no column is hit, return null text position. This can also occur if column count is 0
            // Otherwise hittest column content.
            if (columnIndex < 0 && floatingElements.Count == 0)
            {
                position = null;
            }
            else
            {
                // Retrieve position from column.
                ReadOnlyCollection<ParagraphResult> paragraphs;
                bool snapToTextInFloatingElements = false;
                if (columnIndex < columns.Count && columnIndex >= 0)
                {
                    ColumnResult column = columns[columnIndex];
                    if (!(column.HasTextContent))
                    {
                        snapToTextInFloatingElements = true;
                    }
                    paragraphs = column.Paragraphs;
                }
                else
                {
                    paragraphs = _emptyParagraphCollection;
                }
                position = GetTextPositionFromPoint(paragraphs, floatingElements, point, snapToText, snapToTextInFloatingElements);
            }

            // There might be a case when returned position is not in the view. 
            // Example: <P>A<Figure/><LineBreak/></P> and the figure is delayed to the next page.
            //          In such case, TextSegments do not contain content of Figure element and </P>.
            //          Paragraph itself has 2 lines. The second line is empty and its position
            //          cannot be represented as TextPointer belonging to TextSegments, because
            //          Backward direction belongs to the first line and the Forward direction
            //          belongs to the next page.
            if (position != null && !ContainsCore(position))
            {
                position = null;
            }
            return position;
        }

        /// <summary>
        /// Returns a cellinfo class for a point that may be inside of a cell
        /// </summary>
        /// <param name="paragraphs">
        /// Paras to hit test into
        /// </param>
        /// <param name="floatingElements">
        /// Floating elements to hit test into
        /// </param>
        /// <param name="point">
        /// Point to hit test
        /// </param>
        /// <param name="tableFilter">
        /// Filter out all results not specific to a given table
        /// </param>
        private CellInfo GetCellInfoFromPoint(ReadOnlyCollection<ParagraphResult> paragraphs, ReadOnlyCollection<ParagraphResult> floatingElements, Point point, Table tableFilter)
        {
            CellInfo cellInfo = null;
            Invariant.Assert(paragraphs != null, "Paragraph collection is empty.");
            Invariant.Assert(floatingElements != null, "Floating element collection is empty.");

            // Figure out which paragraph is the closest to the input pixel position.
            // Search floating elements first, then paragraphs collection. Do not snap to text in either floating elements or
            // main flow when searching for cell info.
            int paragraphIndex = GetParagraphFromPointInFloatingElements(floatingElements, point, false);
            ParagraphResult paragraph = null;
            if (paragraphIndex >= 0)
            {
                Invariant.Assert(paragraphIndex < floatingElements.Count);
                paragraph = floatingElements[paragraphIndex];
            }
            else
            {
                paragraphIndex = GetParagraphFromPoint(paragraphs, point, false);
                if (paragraphIndex >= 0)
                {
                    Invariant.Assert(paragraphIndex < paragraphs.Count);
                    paragraph = paragraphs[paragraphIndex];
                }
            }

            if (paragraph != null)
            {
                cellInfo = GetCellInfoFromPoint(paragraph, point, tableFilter);
            }
            return cellInfo;
        }

        /// <summary>
        /// Returns a cellinfo class for a point that may be inside of a cell
        /// </summary>
        /// <param name="paragraph">
        /// Para to hit test into
        /// </param>
        /// <param name="point">
        /// Point to hit test
        /// </param>
        /// <param name="tableFilter">
        /// Filter out all results not specific to a given table
        /// </param>
        private CellInfo GetCellInfoFromPoint(ParagraphResult paragraph, Point point, Table tableFilter)
        {
            // Figure out which paragraph is the closest to the input pixel position.
            CellInfo cellInfo = null;
            if (paragraph is ContainerParagraphResult)
            {
                // a) ContainerParagraph - hittest colleciton of nested paragraphs.
                ReadOnlyCollection<ParagraphResult> nestedParagraphs = ((ContainerParagraphResult)paragraph).Paragraphs;
                // Paragraphs collection may be empty, but should never be null
                Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null");
                if (nestedParagraphs.Count > 0)
                {
                    cellInfo = GetCellInfoFromPoint(nestedParagraphs, _emptyParagraphCollection, point, tableFilter);
                }
            }
            else if (paragraph is TableParagraphResult)
            {
                ReadOnlyCollection<ParagraphResult> nestedParagraphs = ((TableParagraphResult)paragraph).GetParagraphsFromPoint(point, false);
                Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null");
                if (nestedParagraphs.Count > 0)
                {
                    cellInfo = GetCellInfoFromPoint(nestedParagraphs, _emptyParagraphCollection, point, tableFilter);
                }
                if (cellInfo == null)
                {
                    cellInfo = ((TableParagraphResult)paragraph).GetCellInfoFromPoint(point);
                }
            }
            else if (paragraph is SubpageParagraphResult)
            {
                // Subpage implies new coordinate system.
                SubpageParagraphResult subpageParagraphResult = (SubpageParagraphResult)paragraph;
                point.X -= subpageParagraphResult.ContentOffset.X;
                point.Y -= subpageParagraphResult.ContentOffset.Y;

                // WOOT! COLUMNS!
                cellInfo = GetCellInfoFromPoint(subpageParagraphResult.Columns, subpageParagraphResult.FloatingElements, point, tableFilter);
                if (cellInfo != null)
                {
                    cellInfo.Adjust(new Point(subpageParagraphResult.ContentOffset.X, subpageParagraphResult.ContentOffset.Y));
                }
            }
            else if (paragraph is FigureParagraphResult)
            {
                // Subpage implies new coordinate system.
                FigureParagraphResult figureParagraphResult = (FigureParagraphResult)paragraph;
                TransformToSubpage(ref point, figureParagraphResult.ContentOffset);
                cellInfo = GetCellInfoFromPoint(figureParagraphResult.Columns, figureParagraphResult.FloatingElements, point, tableFilter);
                if (cellInfo != null)
                {
                    cellInfo.Adjust(new Point(figureParagraphResult.ContentOffset.X, figureParagraphResult.ContentOffset.Y));
                }
            }
            else if (paragraph is FloaterParagraphResult)
            {
                // Subpage implies new coordinate system.
                FloaterParagraphResult floaterParagraphResult = (FloaterParagraphResult)paragraph;
                TransformToSubpage(ref point, floaterParagraphResult.ContentOffset);
                cellInfo = GetCellInfoFromPoint(floaterParagraphResult.Columns, floaterParagraphResult.FloatingElements, point, tableFilter);
                if (cellInfo != null)
                {
                    cellInfo.Adjust(new Point(floaterParagraphResult.ContentOffset.X, floaterParagraphResult.ContentOffset.Y));
                }
            }

            if (tableFilter != null && cellInfo != null && cellInfo.Cell.Table != tableFilter)
            {
                cellInfo = null; // Clear out result if not matching input filter
            }
            return cellInfo;
        }

        /// <summary>
        /// Retrieves a CellInfo from a given point, traversing through columns.
        /// </summary>
        private CellInfo GetCellInfoFromPoint(ReadOnlyCollection<ColumnResult> columns, ReadOnlyCollection<ParagraphResult> floatingElements, Point point, Table tableFilter)
        {
            Invariant.Assert(floatingElements != null);
            int columnIndex = GetColumnFromPoint(columns, point, false);
            CellInfo cellInfo;

            // If no column is hit, return null CellInfo.
            // Otherwise hittest column content.
            if (columnIndex < 0 && floatingElements.Count == 0)
            {
                cellInfo = null;
            }
            else
            {
                // Retrieve position from column.
                ReadOnlyCollection<ParagraphResult> paragraphs = (columnIndex < columns.Count && columnIndex >= 0) ? columns[columnIndex].Paragraphs : _emptyParagraphCollection;
                cellInfo = GetCellInfoFromPoint(paragraphs, floatingElements, point, tableFilter);
            }

            return cellInfo;
        }

        /// <summary>
        /// Retrieves the height and offset, in pixels, of the edge of 
        /// the object/character represented by position.
        /// </summary>
        /// <param name="paragraphs">Collection of paragraphs.</param>
        /// <param name="floatingElements">collection of floating elements</param>
        /// <param name="position">Position of an object/character.</param>
        private Rect GetRectangleFromTextPosition(ReadOnlyCollection<ParagraphResult> paragraphs, ReadOnlyCollection<ParagraphResult> floatingElements, ITextPointer position)
        {
            Invariant.Assert(paragraphs != null, "Paragraph collection is null");
            Invariant.Assert(floatingElements != null, "Floating element collection is null");

            Rect rect = Rect.Empty;
            // Figure out which paragraph contains text position.
            bool isFloatingPara = false;
            int paragraphIndex = GetParagraphFromPosition(paragraphs, floatingElements, position, out isFloatingPara);

            ParagraphResult paragraph = null;
            if (isFloatingPara)
            {
                Invariant.Assert(paragraphIndex < floatingElements.Count);
                paragraph = floatingElements[paragraphIndex];
            }
            else
            {
                if (paragraphIndex < paragraphs.Count)
                {
                    paragraph = paragraphs[paragraphIndex];
                }
            }

            if (paragraph != null)
            {
                rect = GetRectangleFromTextPosition(paragraph, position);
            }
            return rect;
        }

        /// <summary>
        /// Retrieves the height and offset, in pixels, of the edge of 
        /// the object/character represented by position.
        /// </summary>
        /// <param name="paragraph">Paragraph to search</param>
        /// <param name="position">Position of an object/character.</param>
        private Rect GetRectangleFromTextPosition(ParagraphResult paragraph, ITextPointer position)
        {
            Rect rect = Rect.Empty;

            // Rectangle is retrieved differently for different paragraph types:
            // a) ContainerParagraph - get rectangle from nested paragraphs.
            // b) TextParagraph - get rectangle from text paragraph's content.
            // c) TableParagraph - get rectangle from nested paras
            if (paragraph is ContainerParagraphResult)
            {
                rect = GetRectangleFromEdge(paragraph, position);

                if (rect == Rect.Empty)
                {
                    // a) ContainerParagraph - check collection of nested paragraphs.
                    ReadOnlyCollection<ParagraphResult> nestedParagraphs = ((ContainerParagraphResult)paragraph).Paragraphs;
                    Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null.");

                    if (nestedParagraphs.Count > 0)
                    {
                        rect = GetRectangleFromTextPosition(nestedParagraphs, _emptyParagraphCollection, position);
                    }
                }
            }
            else if (paragraph is TextParagraphResult)
            {
                rect = ((TextParagraphResult)paragraph).GetRectangleFromTextPosition(position);
            }
            else if (paragraph is TableParagraphResult)
            {
                // c) TableParagraph - get rectangle from nested paras

                rect = GetRectangleFromEdge(paragraph, position);

                if (rect == Rect.Empty)
                {
                    ReadOnlyCollection<ParagraphResult> nestedParagraphs = ((TableParagraphResult)paragraph).GetParagraphsFromPosition(position);
                    Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null.");
                    if (nestedParagraphs.Count > 0)
                    {
                        rect = GetRectangleFromTextPosition(nestedParagraphs, _emptyParagraphCollection, position);
                    }
                    else if (position is TextPointer && ((TextPointer)position).IsAtRowEnd)
                    {
                        rect = ((TableParagraphResult)paragraph).GetRectangleFromRowEndPosition(position);
                    }
                }
            }
            else if (paragraph is SubpageParagraphResult)
            {
                // Subpage implies new coordinate system.
                SubpageParagraphResult subpageParagraphResult = (SubpageParagraphResult)paragraph;
                rect = GetRectangleFromTextPosition(subpageParagraphResult.Columns, subpageParagraphResult.FloatingElements, position);
                if (rect != Rect.Empty)
                {
                    rect.X += subpageParagraphResult.ContentOffset.X;
                    rect.Y += subpageParagraphResult.ContentOffset.Y;
                }
            }
            else if (paragraph is FloaterParagraphResult)
            {
                FloaterParagraphResult floaterParagraphResult = (FloaterParagraphResult)paragraph;
                ReadOnlyCollection<ColumnResult> columns = floaterParagraphResult.Columns;
                ReadOnlyCollection<ParagraphResult> nestedFloatingElements = floaterParagraphResult.FloatingElements;
                Invariant.Assert(columns != null, "Columns collection is null.");
                Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                if (nestedFloatingElements.Count > 0 || columns.Count > 0)
                {
                    rect = GetRectangleFromTextPosition(columns, nestedFloatingElements, position);
                    // Add content offset to rect
                    TransformFromSubpage(ref rect, floaterParagraphResult.ContentOffset);
                }
            }
            else if (paragraph is FigureParagraphResult)
            {
                FigureParagraphResult figureParagraphResult = (FigureParagraphResult)paragraph;
                ReadOnlyCollection<ColumnResult> columns = figureParagraphResult.Columns;
                ReadOnlyCollection<ParagraphResult> nestedFloatingElements = figureParagraphResult.FloatingElements;
                Invariant.Assert(columns != null, "Columns collection is null.");
                Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                if (nestedFloatingElements.Count > 0 || columns.Count > 0)
                {
                    rect = GetRectangleFromTextPosition(columns, nestedFloatingElements, position);
                    // Add content offset to rect
                    TransformFromSubpage(ref rect, figureParagraphResult.ContentOffset);
                }
            }
            else if (paragraph is UIElementParagraphResult)
            {
                rect = GetRectangleFromEdge(paragraph, position);
                if (rect == Rect.Empty)
                {
                    // For a UIElementParagraph, we should check if element is either at Element start/end or Content Start end
                    // This is needed to enable selection of embedded object w/ mouse click
                    rect = GetRectangleFromContentEdge(paragraph, position);
                }
            }
            return rect;
        }

        /// <summary>
        /// Returns a rectangle for a text position, traversing through columns.
        /// </summary>
        private Rect GetRectangleFromTextPosition(ReadOnlyCollection<ColumnResult> columns, ReadOnlyCollection<ParagraphResult> floatingElements, ITextPointer position)
        {
            Rect rect = Rect.Empty;
            Invariant.Assert(floatingElements != null);

            // Figure out which column contains text position.
            int columnIndex = GetColumnFromPosition(columns, position);
            if (columnIndex < columns.Count || floatingElements.Count > 0)
            {
                // Retrieve rectangle from the retrieved column.
                ReadOnlyCollection<ParagraphResult> paragraphs = (columnIndex < columns.Count && columnIndex >= 0) ? columns[columnIndex].Paragraphs : _emptyParagraphCollection;
                rect = GetRectangleFromTextPosition(paragraphs, floatingElements, position);
            }
            return rect;
        }

        /// <summary>
        /// Delegates tight bounding geometry calculation to the appropriate paragraph result
        /// object depending on paragraph result type.
        /// Returns tight bounding path geometry.
        /// </summary>
        internal static Geometry GetTightBoundingGeometryFromTextPositionsHelper(
            ReadOnlyCollection<ParagraphResult> paragraphs,
            ITextPointer startPosition,
            ITextPointer endPosition,
            double paragraphTopSpace,
            Rect visibleRect)
        {
            Geometry geometry = null;

            int paragraphCount = paragraphs.Count;
            for (int i = 0; i < paragraphCount; i++)
            {
                if (endPosition.CompareTo(paragraphs[i].StartPosition) <= 0)
                {
                    //  this paragraph starts after the range's end.
                    //  safe to break from the loop.
                    break;
                }

                if (startPosition.CompareTo(paragraphs[i].EndPosition) > 0)
                {
                    //  this paragraph ends before the range's start
                    //  safe to skip to the next paragraph
                    continue;
                }

                Rect layoutBox = GetLayoutBox(paragraphs[i]);

                // Ignore horizontal offset because TextBox page width != extent width.
                // It's ok to include content that doesn't strictly intersect -- this
                // is a perf optimization and the edge cases won't significantly hurt us.
                layoutBox.X = visibleRect.X;

                if (!layoutBox.IntersectsWith(visibleRect))
                {
                    //  this paragraph falls beyond visible rectangle
                    //  safe to skip to the next paragraph
                    continue;
                }

                Geometry paragraphGeometry = null;

                if (paragraphs[i] is ContainerParagraphResult)
                {
                    paragraphGeometry = ((ContainerParagraphResult)paragraphs[i]).GetTightBoundingGeometryFromTextPositions(startPosition, endPosition, visibleRect);
                }
                else if (paragraphs[i] is TextParagraphResult)
                {
                    paragraphGeometry = ((TextParagraphResult)paragraphs[i]).GetTightBoundingGeometryFromTextPositions(startPosition, endPosition, paragraphTopSpace, visibleRect);
                }
                else if (paragraphs[i] is TableParagraphResult)
                {
                    paragraphGeometry = ((TableParagraphResult)paragraphs[i]).GetTightBoundingGeometryFromTextPositions(startPosition, endPosition, visibleRect);
                }
                else if (paragraphs[i] is UIElementParagraphResult)
                {
                    paragraphGeometry = ((UIElementParagraphResult)paragraphs[i]).GetTightBoundingGeometryFromTextPositions(startPosition, endPosition);
                }
                CaretElement.AddGeometry(ref geometry, paragraphGeometry);
            }
            return geometry;
        }

        /// <summary>
        /// Delegates tight bounding geometry calculation to the appropriate paragraph result
        /// First checks floating paragraph results and then regular paragraph results
        /// </summary>
        internal static Geometry GetTightBoundingGeometryFromTextPositionsHelper(
            ReadOnlyCollection<ParagraphResult> paragraphs,
            ReadOnlyCollection<ParagraphResult> floatingElements,
            ITextPointer startPosition,
            ITextPointer endPosition,
            double paragraphTopSpace,
            Rect visibleRect)
        {
            Geometry geometry = null;
            bool success = false;
            if (floatingElements != null && floatingElements.Count > 0)
            {
                geometry = GetTightBoundingGeometryFromTextPositionsInFloatingElements(floatingElements, startPosition, endPosition, paragraphTopSpace, visibleRect, out success);
            }
            if (!success)
            {
                // Not found in floaitng elements, check regular paragraph colleciton.
                geometry = GetTightBoundingGeometryFromTextPositionsHelper(paragraphs, startPosition, endPosition, paragraphTopSpace, visibleRect);
            }
            return geometry;
        }

        /// <summary>
        /// Delegates tight bounding geometry calculation to the appropriate paragraph result
        /// object depending on paragraph result type. Helper function for searching floating elements.
        /// Returns tight bounding path geometry.
        /// </summary>
        private static Geometry GetTightBoundingGeometryFromTextPositionsInFloatingElements(
            ReadOnlyCollection<ParagraphResult> floatingElements,
            ITextPointer startPosition,
            ITextPointer endPosition,
            double paragraphTopSpace,
            Rect visibleRect,
            out bool success)
        {
            Geometry geometry = null;
            success = false;
            int paragraphCount = floatingElements.Count;

            for (int i = 0; i < paragraphCount; i++)
            {
                if (!(startPosition.CompareTo(floatingElements[i].StartPosition) > 0 &&
                    endPosition.CompareTo(floatingElements[i].EndPosition) < 0))
                {
                    // Selection range is not contained entirely within the floating element. 
                    // We cannot include any of it since we should give priority to any text content in this case.
                    continue;
                }

                Rect layoutBox = GetLayoutBox(floatingElements[i]);
                Rect visibleRectThisPara = visibleRect;

                // Ignore horizontal offset because TextBox page width != extent width.
                // It's ok to include content that doesn't strictly intersect -- this
                // is a perf optimization and the edge cases won't significantly hurt us.
                layoutBox.X = visibleRectThisPara.X;

                if (!layoutBox.IntersectsWith(visibleRectThisPara))
                {
                    //  this paragraph falls beyond visible rectangle
                    //  safe to skip to the next paragraph
                    continue;
                }

                Geometry paragraphGeometry = null;
                Invariant.Assert(floatingElements[i] is FloaterParagraphResult ||
                                 floatingElements[i] is FigureParagraphResult);
                if (floatingElements[i] is FloaterParagraphResult)
                {
                    // Transform visible rect to subpage coordinates, and transform geometry from subpage coordinates
                    FloaterParagraphResult floaterParagraphResult = (FloaterParagraphResult)floatingElements[i];
                    TransformToSubpage(ref visibleRectThisPara, floaterParagraphResult.ContentOffset);
                    paragraphGeometry = floaterParagraphResult.GetTightBoundingGeometryFromTextPositions(startPosition, endPosition, visibleRectThisPara, out success);
                    // Geometry within the floater needs to be transformed from subpage content
                    TransformFromSubpage(paragraphGeometry, floaterParagraphResult.ContentOffset);
                }
                else if (floatingElements[i] is FigureParagraphResult)
                {
                    // Transform visible rect to subpage coordinates, and transform geometry from subpage coordinates
                    FigureParagraphResult figureParagraphResult = (FigureParagraphResult)floatingElements[i];
                    TransformToSubpage(ref visibleRectThisPara, figureParagraphResult.ContentOffset);
                    paragraphGeometry = figureParagraphResult.GetTightBoundingGeometryFromTextPositions(startPosition, endPosition, visibleRectThisPara, out success);
                    // Geometry within the figure needs to be transformed from subpage content
                    TransformFromSubpage(paragraphGeometry, figureParagraphResult.ContentOffset);
                }
                CaretElement.AddGeometry(ref geometry, paragraphGeometry);
                if (success)
                {
                    // If we find geometry inside one floating element, we cannot find it inside another. Selection inside a floating element cannot leave the
                    // floating element
                    break;
                }
            }
            return geometry;
        }

        // Retreives a layout box for the paragraphResult
        private static Rect GetLayoutBox(ParagraphResult paragraph)
        {
            if (!(paragraph is SubpageParagraphResult) && !(paragraph is RowParagraphResult))
            {
                return paragraph.LayoutBox;
            }
            return Rect.Empty;
        }

        /// <summary>
        /// Returns true if caret is at unit boundary
        /// </summary>
        /// <param name="paragraphs">Collection of paragraphs.</param>
        /// <param name="floatingElements">Collection of floating elements</param>
        /// <param name="position">Position of an object/character.</param>
        private bool IsAtCaretUnitBoundary(ReadOnlyCollection<ParagraphResult> paragraphs, ReadOnlyCollection<ParagraphResult> floatingElements, ITextPointer position)
        {
            Invariant.Assert(paragraphs != null, "Paragraph collection is null");
            Invariant.Assert(floatingElements != null, "Floating element collection is null");

            bool isAtCaretUnitBoundary = false;
            bool isFloatingPara;
            int paragraphIndex = GetParagraphFromPosition(paragraphs, floatingElements, position, out isFloatingPara);
            ParagraphResult paragraph = null;

            if (isFloatingPara)
            {
                Invariant.Assert(paragraphIndex < floatingElements.Count);
                paragraph = floatingElements[paragraphIndex];
            }
            else
            {
                if (paragraphIndex < paragraphs.Count)
                {
                    paragraph = paragraphs[paragraphIndex];
                }
            }

            if (paragraph != null)
            {
                isAtCaretUnitBoundary = IsAtCaretUnitBoundary(paragraph, position);
            }
            return isAtCaretUnitBoundary;
        }

        /// <summary>
        /// Returns true if caret is at unit boundary
        /// </summary>
        /// <param name="paragraph">Paragraph to search.</param>
        /// <param name="position">Position of an object/character.</param>
        private bool IsAtCaretUnitBoundary(ParagraphResult paragraph, ITextPointer position)
        {
            bool isAtCaretUnitBoundary = false;

            if (paragraph is ContainerParagraphResult)
            {
                // a) ContainerParagraph - go to collection of nested paragraphs.
                ReadOnlyCollection<ParagraphResult> nestedParagraphs = ((ContainerParagraphResult)paragraph).Paragraphs;
                // Paragraphs collection may be null in case of empty List.
                Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null.");
                if (nestedParagraphs.Count > 0)
                {
                    isAtCaretUnitBoundary = IsAtCaretUnitBoundary(nestedParagraphs, _emptyParagraphCollection, position);
                }
            }
            else if (paragraph is TextParagraphResult)
            {
                // b) TextParagraph - search inside it
                isAtCaretUnitBoundary = ((TextParagraphResult)paragraph).IsAtCaretUnitBoundary(position);
            }
            else if (paragraph is TableParagraphResult)
            {
                ReadOnlyCollection<ParagraphResult> nestedParagraphs = ((TableParagraphResult)paragraph).GetParagraphsFromPosition(position);
                Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null.");
                if (nestedParagraphs.Count > 0)
                {
                    isAtCaretUnitBoundary = IsAtCaretUnitBoundary(nestedParagraphs, _emptyParagraphCollection, position);
                }
            }
            else if (paragraph is SubpageParagraphResult)
            {
                SubpageParagraphResult subpageParagraphResult = (SubpageParagraphResult)paragraph;
                ReadOnlyCollection<ColumnResult> columns = subpageParagraphResult.Columns;
                ReadOnlyCollection<ParagraphResult> nestedFloatingElements = subpageParagraphResult.FloatingElements;
                Invariant.Assert(columns != null, "Column collection is null.");
                Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                if (columns.Count > 0 || nestedFloatingElements.Count > 0)
                {
                    isAtCaretUnitBoundary = IsAtCaretUnitBoundary(columns, nestedFloatingElements, position);
                }
            }
            else if (paragraph is FigureParagraphResult)
            {
                FigureParagraphResult figureParagraphResult = (FigureParagraphResult)paragraph;
                ReadOnlyCollection<ColumnResult> columns = figureParagraphResult.Columns;
                ReadOnlyCollection<ParagraphResult> nestedFloatingElements = figureParagraphResult.FloatingElements;
                Invariant.Assert(columns != null, "Column collection is null.");
                Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                if (columns.Count > 0 || nestedFloatingElements.Count > 0)
                {
                    isAtCaretUnitBoundary = IsAtCaretUnitBoundary(columns, nestedFloatingElements, position);
                }
            }
            else if (paragraph is FloaterParagraphResult)
            {
                FloaterParagraphResult floaterParagraphResult = (FloaterParagraphResult)paragraph;
                ReadOnlyCollection<ColumnResult> columns = floaterParagraphResult.Columns;
                ReadOnlyCollection<ParagraphResult> nestedFloatingElements = floaterParagraphResult.FloatingElements;
                Invariant.Assert(columns != null, "Column collection is null.");
                Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                if (columns.Count > 0 || nestedFloatingElements.Count > 0)
                {
                    isAtCaretUnitBoundary = IsAtCaretUnitBoundary(columns, nestedFloatingElements, position);
                }
            }
            return isAtCaretUnitBoundary;
        }

        /// <summary>
        /// Returns true if caret is at unit boundary
        /// </summary>
        private bool IsAtCaretUnitBoundary(ReadOnlyCollection<ColumnResult> columns, ReadOnlyCollection<ParagraphResult> floatingElements, ITextPointer position)
        {
            int columnIndex = GetColumnFromPosition(columns, position);
            if (columnIndex < columns.Count || floatingElements.Count > 0)
            {
                ReadOnlyCollection<ParagraphResult> paragraphs = (columnIndex < columns.Count && columnIndex >= 0) ? columns[columnIndex].Paragraphs : _emptyParagraphCollection;
                return IsAtCaretUnitBoundary(paragraphs, floatingElements, position);
            }
            return false;
        }

        /// <summary>
        /// Finds and returns the next position at the edge of a caret unit in 
        /// specified direction.
        /// </summary>
        /// <param name="paragraphs">Collection of paragraphs.</param>
        /// <param name="floatingElements">Collection of floating elements.</param>
        /// <param name="position">Position of an object/character.</param>
        /// <param name="direction">Direction in which we seek next caret position</param>
        private ITextPointer GetNextCaretUnitPosition(ReadOnlyCollection<ParagraphResult> paragraphs, ReadOnlyCollection<ParagraphResult> floatingElements, ITextPointer position, LogicalDirection direction)
        {
            Invariant.Assert(paragraphs != null, "Paragraph collection is null");
            Invariant.Assert(floatingElements != null, "Floating element collection is null");
            ITextPointer nextCaretPosition = position;

            bool isFloatingPara;
            int paragraphIndex = GetParagraphFromPosition(paragraphs, floatingElements, position, out isFloatingPara);
            ParagraphResult paragraph = null;

            if (isFloatingPara)
            {
                Invariant.Assert(paragraphIndex < floatingElements.Count);
                paragraph = floatingElements[paragraphIndex];
            }
            else
            {
                if (paragraphIndex < paragraphs.Count)
                {
                    paragraph = paragraphs[paragraphIndex];
                }
            }

            if (paragraph != null)
            {
                nextCaretPosition = GetNextCaretUnitPosition(paragraph, position, direction);
            }
            return nextCaretPosition;
        }

        /// <summary>
        /// Finds and returns the next position at the edge of a caret unit in 
        /// specified direction.
        /// </summary>
        /// <param name="paragraph">Paragraph to search</param>
        /// <param name="position">Position of an object/character.</param>
        /// <param name="direction">Direction in which we seek next caret position</param>
        private ITextPointer GetNextCaretUnitPosition(ParagraphResult paragraph, ITextPointer position, LogicalDirection direction)
        {
            ITextPointer nextCaretPosition = position;
            if (paragraph is ContainerParagraphResult)
            {
                // a) ContainerParagraph - go to collection of nested paragraphs.
                ReadOnlyCollection<ParagraphResult> nestedParagraphs = ((ContainerParagraphResult)paragraph).Paragraphs;
                // Paragraphs collection may be null in case of empty List.
                Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null.");
                if (nestedParagraphs.Count > 0)
                {
                    nextCaretPosition = GetNextCaretUnitPosition(nestedParagraphs, _emptyParagraphCollection, position, direction);
                }
                // Else: Illegal call from outside TextView, return same position
            }
            else if (paragraph is TextParagraphResult)
            {
                // b) TextParagraph - search inside it
                nextCaretPosition = ((TextParagraphResult)paragraph).GetNextCaretUnitPosition(position, direction);
            }
            else if (paragraph is TableParagraphResult)
            {
                ReadOnlyCollection<ParagraphResult> nestedParagraphs = ((TableParagraphResult)paragraph).GetParagraphsFromPosition(position);
                Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null.");
                if (nestedParagraphs.Count > 0)
                {
                    nextCaretPosition = GetNextCaretUnitPosition(nestedParagraphs, _emptyParagraphCollection, position, direction);
                }
            }
            else if (paragraph is SubpageParagraphResult)
            {
                SubpageParagraphResult subpageParagraphResult = (SubpageParagraphResult)paragraph;
                ReadOnlyCollection<ColumnResult> columns = subpageParagraphResult.Columns;
                ReadOnlyCollection<ParagraphResult> nestedFloatingElements = subpageParagraphResult.FloatingElements;
                Invariant.Assert(columns != null, "Column collection is null.");
                Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                if (columns.Count > 0 || nestedFloatingElements.Count > 0)
                {
                    nextCaretPosition = GetNextCaretUnitPosition(columns, nestedFloatingElements, position, direction);
                }
            }
            else if (paragraph is FigureParagraphResult)
            {
                FigureParagraphResult figureParagraphResult = (FigureParagraphResult)paragraph;
                ReadOnlyCollection<ColumnResult> columns = figureParagraphResult.Columns;
                ReadOnlyCollection<ParagraphResult> nestedFloatingElements = figureParagraphResult.FloatingElements;
                Invariant.Assert(columns != null, "Column collection is null.");
                Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                if (columns.Count > 0 || nestedFloatingElements.Count > 0)
                {
                    nextCaretPosition = GetNextCaretUnitPosition(columns, nestedFloatingElements, position, direction);
                }
            }
            else if (paragraph is FloaterParagraphResult)
            {
                FloaterParagraphResult floaterParagraphResult = (FloaterParagraphResult)paragraph;
                ReadOnlyCollection<ColumnResult> columns = floaterParagraphResult.Columns;
                ReadOnlyCollection<ParagraphResult> nestedFloatingElements = floaterParagraphResult.FloatingElements;
                Invariant.Assert(columns != null, "Column collection is null.");
                Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                if (columns.Count > 0 || nestedFloatingElements.Count > 0)
                {
                    nextCaretPosition = GetNextCaretUnitPosition(columns, nestedFloatingElements, position, direction);
                }
            }
            return nextCaretPosition;
        }

        /// <summary>
        /// Finds and returns the next position at the edge of a caret unit in 
        /// specified direction.
        /// </summary>
        private ITextPointer GetNextCaretUnitPosition(ReadOnlyCollection<ColumnResult> columns, ReadOnlyCollection<ParagraphResult> floatingElements, ITextPointer position, LogicalDirection direction)
        {
            int columnIndex = GetColumnFromPosition(columns, position);
            if (columnIndex < columns.Count || floatingElements.Count > 0)
            {
                ReadOnlyCollection<ParagraphResult> paragraphs = (columnIndex < columns.Count && columnIndex >= 0) ? columns[columnIndex].Paragraphs : _emptyParagraphCollection;
                return GetNextCaretUnitPosition(paragraphs, floatingElements, position, direction);
            }
            return position;
        }

        /// <summary>
        /// Finds and returns the backspace position of a caret unit
        /// </summary>
        /// <param name="paragraphs">Collection of paragraphs.</param>
        /// <param name="floatingElements">Collection of floating elements</param>
        /// <param name="position">Position of an object/character.</param>
        private ITextPointer GetBackspaceCaretUnitPosition(ReadOnlyCollection<ParagraphResult> paragraphs, ReadOnlyCollection<ParagraphResult> floatingElements, ITextPointer position)
        {
            Invariant.Assert(paragraphs != null, "Paragraph collection is null");
            Invariant.Assert(floatingElements != null, "Floating element collection is null");
            ITextPointer backspaceCaretPosition = position;

            bool isFloatingPara;
            int paragraphIndex = GetParagraphFromPosition(paragraphs, floatingElements, position, out isFloatingPara);
            ParagraphResult paragraph = null;

            if (isFloatingPara)
            {
                Invariant.Assert(paragraphIndex < floatingElements.Count);
                paragraph = floatingElements[paragraphIndex];
            }
            else
            {
                if (paragraphIndex < paragraphs.Count)
                {
                    paragraph = paragraphs[paragraphIndex];
                }
            }

            if (paragraph != null)
            {
                backspaceCaretPosition = GetBackspaceCaretUnitPosition(paragraph, position);
            }
            return backspaceCaretPosition;
        }

        /// <summary>
        /// Finds and returns the backspace position of a caret unit
        /// </summary>
        /// <param name="paragraph">Paragraph to search.</param>
        /// <param name="position">Position of an object/character.</param>
        private ITextPointer GetBackspaceCaretUnitPosition(ParagraphResult paragraph, ITextPointer position)
        {
            ITextPointer backspaceCaretPosition = position;
            if (paragraph is ContainerParagraphResult)
            {
                // a) ContainerParagraph - go to collection of nested paragraphs.
                ReadOnlyCollection<ParagraphResult> nestedParagraphs = ((ContainerParagraphResult)paragraph).Paragraphs;
                // Paragraphs collection may be null in case of empty List.
                Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null.");
                if (nestedParagraphs.Count > 0)
                {
                    backspaceCaretPosition = GetBackspaceCaretUnitPosition(nestedParagraphs, _emptyParagraphCollection, position);
                }
            }
            else if (paragraph is TextParagraphResult)
            {
                // b) TextParagraph - search inside it
                backspaceCaretPosition = ((TextParagraphResult)paragraph).GetBackspaceCaretUnitPosition(position);
            }
            else if (paragraph is TableParagraphResult)
            {
                ReadOnlyCollection<ParagraphResult> nestedParagraphs = ((TableParagraphResult)paragraph).GetParagraphsFromPosition(position);
                Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null.");
                if (nestedParagraphs.Count > 0)
                {
                    backspaceCaretPosition = GetBackspaceCaretUnitPosition(nestedParagraphs, _emptyParagraphCollection, position);
                }
            }
            else if (paragraph is SubpageParagraphResult)
            {
                SubpageParagraphResult subpageParagraphResult = (SubpageParagraphResult)paragraph;
                ReadOnlyCollection<ColumnResult> columns = subpageParagraphResult.Columns;
                ReadOnlyCollection<ParagraphResult> nestedFloatingElements = subpageParagraphResult.FloatingElements;
                Invariant.Assert(columns != null, "Column collection is null.");
                Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                if (columns.Count > 0 || nestedFloatingElements.Count > 0)
                {
                    backspaceCaretPosition = GetBackspaceCaretUnitPosition(columns, nestedFloatingElements, position);
                }
            }
            else if (paragraph is FigureParagraphResult)
            {
                FigureParagraphResult figureParagraphResult = (FigureParagraphResult)paragraph;
                ReadOnlyCollection<ColumnResult> columns = figureParagraphResult.Columns;
                ReadOnlyCollection<ParagraphResult> nestedFloatingElements = figureParagraphResult.FloatingElements;
                Invariant.Assert(columns != null, "Column collection is null.");
                Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                if (columns.Count > 0 || nestedFloatingElements.Count > 0)
                {
                    backspaceCaretPosition = GetBackspaceCaretUnitPosition(columns, nestedFloatingElements, position);
                }
            }
            else if (paragraph is FloaterParagraphResult)
            {
                FloaterParagraphResult floaterParagraphResult = (FloaterParagraphResult)paragraph;
                ReadOnlyCollection<ColumnResult> columns = floaterParagraphResult.Columns;
                ReadOnlyCollection<ParagraphResult> nestedFloatingElements = floaterParagraphResult.FloatingElements;
                Invariant.Assert(columns != null, "Column collection is null.");
                Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                if (columns.Count > 0 || nestedFloatingElements.Count > 0)
                {
                    backspaceCaretPosition = GetBackspaceCaretUnitPosition(columns, nestedFloatingElements, position);
                }
            }
            return backspaceCaretPosition;
        }

        /// <summary>
        /// Finds and returns the backspace position of a caret unit
        /// </summary>
        private ITextPointer GetBackspaceCaretUnitPosition(ReadOnlyCollection<ColumnResult> columns, ReadOnlyCollection<ParagraphResult> floatingElements, ITextPointer position)
        {
            int columnIndex = GetColumnFromPosition(columns, position);
            if (columnIndex < columns.Count || floatingElements.Count > 0)
            {
                ReadOnlyCollection<ParagraphResult> paragraphs = (columnIndex < columns.Count && columnIndex >= 0) ? columns[columnIndex].Paragraphs : _emptyParagraphCollection;
                return GetBackspaceCaretUnitPosition(paragraphs, floatingElements, position);
            }
            return position;
        }

        /// <summary>
        /// HitTest a column collection.
        /// </summary>
        /// <param name="columns">Collection of columns.</param>
        /// <param name="point">Point in pixel coordinates to test.</param>
        /// <param name="snapToText">
        /// If true, this method must always return a column index 
        /// (the closest position as calculated by the control's heuristics). 
        /// If false, this method should return -1, if the test 
        /// point does not fall within any column bounding box.
        /// </param>
        /// <returns>
        /// An index of column matching or closest to the point.
        /// </returns>
        private int GetColumnFromPoint(ReadOnlyCollection<ColumnResult> columns, Point point, bool snapToText)
        {
            int columnIndex;
            int lastColumnWithContent = -1;
            Rect columnBox;
            bool foundHit = false;

            Invariant.Assert(columns != null, "Column collection is null");
            // Figure out which column is the closest horizontally to the input pixel position.
            // There are following assumptions made:
            // * column[N].LayoutBox.Left > column[N+1].LayoutBox.Left
            // 
            // NOTE: Snapping, if necessary, is done first in horizontal direction.
            for (columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                columnBox = columns[columnIndex].LayoutBox;
                if (!(columns[columnIndex].HasTextContent))
                {
                    if (columnIndex == columns.Count - 1)
                    {
                        // We are at the last column, and if we didn't find anything else with content, we must stop here
                        lastColumnWithContent = (lastColumnWithContent == -1) ? columnIndex : lastColumnWithContent;
                        foundHit = snapToText;
                    }
                    // Since column has no text content, skip checking it's layout box. 
                    continue;
                }
                else
                {
                    lastColumnWithContent = columnIndex;
                }

                Invariant.Assert(lastColumnWithContent == columnIndex);

                // There are following possibilities:
                // a) Point is to the left of the column.Left.
                //    In this case consider the current column. This will be true only
                //    for the first column, because all others should be taken care off
                //    during b) & c)
                // b) Point is to the right of the column.Right.
                //    If the point does not intersect with the next column, consider 
                //    the closest column (or this column if it is the last one).
                // c) Point intersects with the current column.
                //    If this column does not overlap with the next one, return it.
                //    But if it does overlap, it means that there is overflow in this column
                //    and the next column should be considered.
                if (point.X < columnBox.Left)
                {
                    // a) Point is to the left of the column.Left.
                    //    In this case consider the current column. This will be true only
                    //    for the first column, because all others should be taken care off
                    //    during b) & c)
                    foundHit = snapToText;
                    break;
                }
                else if (point.X > columnBox.Right)
                {
                    // b) Point is to the right of the column.Right
                    //    If the point does not intersect with the next column, consider 
                    //    the closest column (or this column if it is the last one).
                    if (columnIndex < columns.Count - 1)
                    {
                        Rect nextColumnBox = columns[columnIndex + 1].LayoutBox;
                        if (point.X < nextColumnBox.Left)
                        {
                            // Point is in the gap between columns. Use the closest one.
                            double gap = nextColumnBox.Left - columnBox.Right;
                            if (point.X > columnBox.Right + gap / 2 && columns[columnIndex + 1].HasTextContent)
                            {
                                ++columnIndex;
                                lastColumnWithContent = columnIndex;
                            }
                            foundHit = snapToText;
                            break;
                        }
                        // else continue to the next column
                    }
                    else
                    {
                        // This is the last column.
                        foundHit = snapToText;
                        break;
                    }
                }
                else
                {
                    // c) Point intersects with the current column.
                    //    If this column does not overlap with the next one, return it.
                    //    But if it does overlap, it means that there is an overflow in the columm
                    //    and the next column should be considered.
                    if (columnIndex < columns.Count - 1)
                    {
                        Rect nextColumnBox = columns[columnIndex + 1].LayoutBox;
                        if (point.X < nextColumnBox.Left)
                        {
                            // Point is not over the next column and this column has been hit.
                            foundHit = true;
                            break;
                        }
                        // else - Continue to the next column.
                    }
                    else
                    {
                        // This is the last column and it has been hit.
                        foundHit = true;
                        break;
                    }
                }
            }

            // Check if the input pixel position is vertically inside column boundary.
            if (foundHit)
            {
                columnBox = columns[lastColumnWithContent].LayoutBox;
                foundHit = snapToText || (point.Y >= columnBox.Top && point.Y <= columnBox.Bottom);
            }

            Invariant.Assert(!foundHit || lastColumnWithContent < columns.Count, "Column not found.");
            return foundHit ? lastColumnWithContent : -1;
        }

        /// <summary>
        /// HitTest a paragraph collection.
        /// </summary>
        /// <param name="paragraphs">Collection of paragraphs.</param>
        /// <param name="point">Point in pixel coordinates to test.</param>
        /// <param name="snapToText">
        /// If true, this method must always return a paragraph index 
        /// (the closest position as calculated by the control's heuristics). 
        /// If false, this method should return -1, if the test 
        /// point does not fall within any paragraph bounding box.
        /// </param>
        private int GetParagraphFromPoint(ReadOnlyCollection<ParagraphResult> paragraphs, Point point, bool snapToText)
        {
            int paragraphIndex;
            int lastParagraphWithContent = -1;
            Rect paragraphBox;
            bool foundHit = false;

            Invariant.Assert(paragraphs != null, "Paragraph collection is null");
            // Figure out which paragraph is the closest vertically to the input pixel position.
            // There are following assumptions made:
            // * paragraph[N].LayoutBox.Top < paragraph[N+1].LayoutBox.Top
            // 
            // NOTE: Snapping, if necessary, is done first in vertical direction.
            for (paragraphIndex = 0; paragraphIndex < paragraphs.Count; paragraphIndex++)
            {
                paragraphBox = paragraphs[paragraphIndex].LayoutBox;
                if (!(paragraphs[paragraphIndex].HasTextContent))
                {
                    if (paragraphIndex == paragraphs.Count - 1)
                    {
                        // We are at the last paragraph, and if we didn't find anything else with content, we must stop here
                        lastParagraphWithContent = (lastParagraphWithContent == -1) ? paragraphIndex : lastParagraphWithContent;
                        foundHit = snapToText;
                    }
                    // Since paragraph has no text content, skip checking it's layout box. 
                    continue;
                }
                else
                {
                    lastParagraphWithContent = paragraphIndex;
                }

                Invariant.Assert(lastParagraphWithContent == paragraphIndex);
                // There are following possibilities:
                // a) Point is to the top of the paragraph.Top.
                //    In this case consider the current paragraph. This will be true only
                //    for the first paragraph, because all others should be taken care off
                //    during b) & c)
                // b) Point is to the bottom of the paragraph.Bottom.
                //    If the point does not intersect with the next paragraph, consider 
                //    the closest paragraph (or this paragraph if it is the last one).
                // c) Point intersects with the current paragraph.
                //    If this paragraph does not overlap with the next one, return it.
                //    But if it does overlap, it means that there is overflow in this paragraph
                //    and the next paragraph should be considered.
                if (point.Y < paragraphBox.Top)
                {
                    // a) Point is to the top of the paragraph.Top.
                    //    In this case consider the current paragraph. This will be true only
                    //    for the first paragraph, because all others should be taken care off
                    //    during b) & c)
                    foundHit = snapToText;
                    break;
                }
                else if (point.Y > paragraphBox.Bottom)
                {
                    // b) Point is to the bottom of the paragraph.Bottom.
                    //    If the point does not intersect with the next paragraph, consider 
                    //    the closest paragraph (or this paragraph if it is the last one).
                    if (paragraphIndex < paragraphs.Count - 1)
                    {
                        Rect nextParagraphBox = paragraphs[paragraphIndex + 1].LayoutBox;
                        if (point.Y < nextParagraphBox.Top)
                        {
                            // Point is in the gap between paragraphs. Use the closest one.
                            double gap = nextParagraphBox.Top - paragraphBox.Bottom;
                            if (point.Y > paragraphBox.Bottom + gap / 2 && paragraphs[paragraphIndex + 1].HasTextContent)
                            {
                                ++paragraphIndex;
                                lastParagraphWithContent = paragraphIndex;
                            }
                            foundHit = snapToText;
                            break;
                        }
                        // else continue to the next paragraph
                    }
                    else
                    {
                        // This is the last paragraph.
                        foundHit = snapToText;
                        break;
                    }
                }
                else
                {
                    // c) Point intersects with the current paragraph.
                    //    If this paragraph does not overlap with the next one, return it.
                    //    But if it does overlap, it means that there is overflow in this paragraph
                    //    and the next paragraph should be considered.
                    if (paragraphIndex < paragraphs.Count - 1)
                    {
                        Rect nextParagraphBox = paragraphs[paragraphIndex + 1].LayoutBox;
                        if (point.Y < nextParagraphBox.Top)
                        {
                            // Point is not over the next paragraph and this paragraph has been hit.
                            foundHit = true;
                            break;
                        }
                        // else - Continue to the next paragraph.
                    }
                    else
                    {
                        // This is the last paragraph and it has been hit.
                        foundHit = true;
                        break;
                    }
                }
            }

            // Check if the input pixel position is horizontally inside paragraph boundary.
            if (foundHit)
            {
                paragraphBox = paragraphs[lastParagraphWithContent].LayoutBox;
                foundHit = snapToText || (point.X >= paragraphBox.Left && point.X <= paragraphBox.Right);
            }

            Invariant.Assert(!foundHit || lastParagraphWithContent < paragraphs.Count, "Paragraph not found.");
            return foundHit ? lastParagraphWithContent : -1;
        }

        /// <summary>
        /// HitTest a floating elements collection for a point. We do not snap to text in floating elements collections,
        /// and we assume there is no overlap between paragraphs. If there is overlap, and the point lies in the overlapping
        /// region, we will return the first paragraph in the collection to contain the point.
        /// </summary>
        /// <param name="floatingElements">Collection of floating element paragraphs.</param>
        /// <param name="point">Point in pixel coordinates to test.</param>
        /// <param name="snapToText">If snapToText is true, we must match the point to some floating element.</param>
        private int GetParagraphFromPointInFloatingElements(ReadOnlyCollection<ParagraphResult> floatingElements, Point point, bool snapToText)
        {
            Invariant.Assert(floatingElements != null, "Paragraph collection is null");
            double closestDistance = Double.MaxValue;
            int closestIndex = -1;
            for (int paragraphIndex = 0; paragraphIndex < floatingElements.Count; paragraphIndex++)
            {
                Rect paragraphBox = floatingElements[paragraphIndex].LayoutBox;
                if (paragraphBox.Contains(point))
                {
                    return paragraphIndex;
                }
                else
                {
                    Point midPoint = new Point(paragraphBox.X + paragraphBox.Width / 2, paragraphBox.Y + paragraphBox.Height / 2);
                    double distance = Math.Abs(point.X - midPoint.X) + Math.Abs(point.Y - midPoint.Y);
                    if (distance < closestDistance)
                    {
                        closestIndex = paragraphIndex;
                        closestDistance = distance;
                    }
                }
            }
            return snapToText ? closestIndex : -1;
        }

        /// <summary>
        /// Get column index from position.
        /// </summary>
        /// <param name="columns">Collection of columns.</param>
        /// <param name="position">Position to test.</param>
        /// <returns>An index of column</returns>
        private int GetColumnFromPosition(ReadOnlyCollection<ColumnResult> columns, ITextPointer position)
        {
            // Column collection cannot be null
            Invariant.Assert(columns != null, "Column collection is null");

            // If there is just one column, there is no point to check if it contains 
            // the position, because range for this column  is the same as range for
            // TextView.
            int columnIndex = 0;
            if (columns.Count > 0)
            {
                if (columns.Count == 1)
                {
                    columnIndex = 0;
                }
                else
                {
                    for (columnIndex = 0; columnIndex < columns.Count; columnIndex++)
                    {
                        if (columns[columnIndex].Contains(position, true))
                        {
                            break;
                        }
                    }

                    // Since strict containment rules are applied, allow loose boundaries
                    // for the beginning and end of TextView content.
                    if (columnIndex >= columns.Count)
                    {
                        if (position.CompareTo(columns[0].StartPosition) == 0)
                        {
                            columnIndex = 0;
                        }
                        else if (position.CompareTo(columns[columns.Count - 1].EndPosition) == 0)
                        {
                            columnIndex = columns.Count - 1;
                        }
                    }
                }
            }
            return columnIndex;
        }

        /// <summary>
        /// Get paragraph index from position.
        /// </summary>
        /// <param name="paragraphs">Collection of paragraphs.</param>
        /// <param name="floatingElements">Collection of floating elements</param>
        /// <param name="position">Position to test.</param>
        /// <param name="isFloatingPara">True if paragraph found is a floating element para</param>
        /// <remarks> If paragraph count is 0, index returned is 0 which is equal to paragraphs.Count</remarks>
        private static int GetParagraphFromPosition(ReadOnlyCollection<ParagraphResult> paragraphs, ReadOnlyCollection<ParagraphResult> floatingElements, ITextPointer position, out bool isFloatingPara)
        {
            Invariant.Assert(paragraphs != null, "Paragraph collection is null.");
            Invariant.Assert(floatingElements != null, "Floating element collection is null.");
            isFloatingPara = false;

            // Search floating elements first 
            int paragraphIndex = GetParagraphFromPosition(floatingElements, position);
            if (paragraphIndex < floatingElements.Count)
            {
                // Found
                isFloatingPara = true;
                return paragraphIndex;
            }

            // Search rest of paras
            return GetParagraphFromPosition(paragraphs, position);
        }

        /// <summary>
        /// Get paragraph index from position.
        /// </summary>
        /// <param name="paragraphs">Collection of paragraphs.</param>
        /// <param name="position">Position to test.</param>
        /// <returns>An index of paragraph. </returns>
        /// <remarks> If paragraph count is 0, index returned is 0 which is equal to paragraphs.Count</remarks>
        private static int GetParagraphFromPosition(ReadOnlyCollection<ParagraphResult> paragraphs, ITextPointer position)
        {
            Invariant.Assert(paragraphs != null, "Paragraph collection is null.");

            // Iterate through paragraph to find out placement of ITextPointer.
            // Apply strict containment rules.
            int paragraphIndex = 0;
            int paragraphSearchIndexUpper = paragraphs.Count - 1;
            int paragraphSearchIndexLower = 0;
            bool found = false;
            if (paragraphs.Count > 0)
            {
                while (true)
                {
                    paragraphIndex = (paragraphSearchIndexUpper + paragraphSearchIndexLower) / 2;

                    if (paragraphs[paragraphIndex].Contains(position, true))
                    {
                        found = true;
                        break;
                    }

                    // If we're examining only one element, we've failed to find it.
                    if (paragraphSearchIndexUpper == paragraphSearchIndexLower)
                    {
                        break;
                    }

                    if (position.CompareTo(paragraphs[paragraphIndex].StartPosition) < 0)
                    {
                        paragraphSearchIndexUpper = paragraphIndex - 1;
                    }
                    else
                    {
                        paragraphSearchIndexLower = paragraphIndex + 1;
                    }

                    // Check if lower and upper have swapped positions, if so, we've failed to find the element.
                    if (paragraphSearchIndexUpper < paragraphSearchIndexLower)
                    {
                        break;
                    }
                }

                if (!found)
                {
                    // Since strict containment rules are applied, allow loose boundaries
                    // for the beginning and end of paragraph's content.
                    if (position.CompareTo(paragraphs[0].StartPosition) == 0)
                    {
                        paragraphIndex = 0;
                    }
                    else if (position.CompareTo(paragraphs[paragraphs.Count - 1].EndPosition) == 0)
                    {
                        paragraphIndex = paragraphs.Count - 1;
                    }
                    else
                    {
                        paragraphIndex = paragraphs.Count;
                    }
                }
            }
            return paragraphIndex;
        }

        /// <summary>
        /// Returns a TextSegment that spans the line on which position is located.
        /// </summary>
        /// <param name="paragraphs">Collection of paragraphs.</param>
        /// <param name="floatingElements">Collection of floating elements</param>
        /// <param name="position">Any oriented text position on the line.</param>
        private TextSegment GetLineRangeFromPosition(ReadOnlyCollection<ParagraphResult> paragraphs, ReadOnlyCollection<ParagraphResult> floatingElements, ITextPointer position)
        {
            Invariant.Assert(paragraphs != null, "Paragraph collection is null");
            Invariant.Assert(floatingElements != null, "Floating element collection is null");
            TextSegment lineRange = TextSegment.Null;

            bool isFloatingPara;
            int paragraphIndex = GetParagraphFromPosition(paragraphs, floatingElements, position, out isFloatingPara);
            ParagraphResult paragraph = null;

            if (isFloatingPara)
            {
                Invariant.Assert(paragraphIndex < floatingElements.Count);
                paragraph = floatingElements[paragraphIndex];
            }
            else
            {
                if (paragraphIndex < paragraphs.Count)
                {
                    paragraph = paragraphs[paragraphIndex];
                }
            }

            if (paragraph != null)
            {
                lineRange = GetLineRangeFromPosition(paragraph, position);
            }
            return lineRange;
        }

        /// <summary>
        /// Returns a TextSegment that spans the line on which position is located.
        /// </summary>
        /// <param name="paragraph">Paragraph to search</param>
        /// <param name="position">Any oriented text position on the line.</param>
        private TextSegment GetLineRangeFromPosition(ParagraphResult paragraph, ITextPointer position)
        {
            TextSegment lineRange = TextSegment.Null;

            // Each paragraph type is handled differently:
            // a) ContainerParagraph - process nested paragraphs.
            // b) TextParagraph - find line index of a line containing input text position 
            //    and then return its range.
            // c) TableParagraph - process nested paragraphs.
            if (paragraph is ContainerParagraphResult)
            {
                // a) ContainerParagraph - process nested paragraphs.
                ReadOnlyCollection<ParagraphResult> nestedParagraphs = ((ContainerParagraphResult)paragraph).Paragraphs;
                // Paragraphs collection may be null in case of empty List.
                Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null.");
                if (nestedParagraphs.Count > 0)
                {
                    lineRange = GetLineRangeFromPosition(nestedParagraphs, _emptyParagraphCollection, position);
                }
            }
            else if (paragraph is TextParagraphResult)
            {
                // b) TextParagraph - find line index of a line containing input text position 
                //    and then return its range.
                ReadOnlyCollection<LineResult> lines = ((TextParagraphResult)paragraph).Lines;
                Invariant.Assert(lines != null, "Lines collection is null");
                if (!((TextParagraphResult)paragraph).HasTextContent)
                {
                    // Paragraph has no lines.
                    // This is a workaround to avoid a crash in this case.
                    // We should actually process figures and floaters here
                    // For now we return empty range
                    lineRange = new TextSegment(((TextParagraphResult)paragraph).EndPosition, ((TextParagraphResult)paragraph).EndPosition, true);
                }
                else
                {
                    // Get index of the line that contains position.
                    int lineIndex = TextParagraphView.GetLineFromPosition(lines, position);
                    Invariant.Assert(lineIndex >= 0 && lineIndex < lines.Count, "Line not found.");
                    lineRange = new TextSegment(lines[lineIndex].StartPosition, lines[lineIndex].GetContentEndPosition(), true);
                }
            }
            else if (paragraph is TableParagraphResult)
            {
                // c) TableParagraph - process nested paragraphs.
                ReadOnlyCollection<ParagraphResult> nestedParagraphs = ((TableParagraphResult)paragraph).GetParagraphsFromPosition(position);
                // Paragraphs collection may be null in case of empty List.
                Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null.");
                if (nestedParagraphs.Count > 0)
                {
                    lineRange = GetLineRangeFromPosition(nestedParagraphs, _emptyParagraphCollection, position);
                }
            }
            else if (paragraph is SubpageParagraphResult)
            {
                SubpageParagraphResult subpageParagraphResult = (SubpageParagraphResult)paragraph;
                ReadOnlyCollection<ColumnResult> columns = subpageParagraphResult.Columns;
                ReadOnlyCollection<ParagraphResult> nestedFloatingElements = subpageParagraphResult.FloatingElements;
                Invariant.Assert(columns != null, "Column collection is null.");
                Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                if (columns.Count > 0 || nestedFloatingElements.Count > 0)
                {
                    lineRange = GetLineRangeFromPosition(columns, nestedFloatingElements, position);
                }
            }
            else if (paragraph is FigureParagraphResult)
            {
                FigureParagraphResult figureParagraphResult = (FigureParagraphResult)paragraph;
                ReadOnlyCollection<ColumnResult> columns = figureParagraphResult.Columns;
                ReadOnlyCollection<ParagraphResult> nestedFloatingElements = figureParagraphResult.FloatingElements;
                Invariant.Assert(columns != null, "Column collection is null.");
                Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                if (columns.Count > 0 || nestedFloatingElements.Count > 0)
                {
                    lineRange = GetLineRangeFromPosition(columns, nestedFloatingElements, position);
                }
            }
            else if (paragraph is FloaterParagraphResult)
            {
                FloaterParagraphResult floaterParagraphResult = (FloaterParagraphResult)paragraph;
                ReadOnlyCollection<ColumnResult> columns = floaterParagraphResult.Columns;
                ReadOnlyCollection<ParagraphResult> nestedFloatingElements = floaterParagraphResult.FloatingElements;
                Invariant.Assert(columns != null, "Column collection is null.");
                Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                if (columns.Count > 0 || nestedFloatingElements.Count > 0)
                {
                    lineRange = GetLineRangeFromPosition(columns, nestedFloatingElements, position);
                }
            }
            else if (paragraph is UIElementParagraphResult)
            {
                // UIElement paragraph result - return content range between BlockUIContainer.ContentStart and ContentEnd
                BlockUIContainer blockUIContainer = paragraph.Element as BlockUIContainer;
                if (blockUIContainer != null)
                {
                    lineRange = new TextSegment(blockUIContainer.ContentStart.CreatePointer(LogicalDirection.Forward), blockUIContainer.ContentEnd.CreatePointer(LogicalDirection.Backward));
                }
            }
            return lineRange;
        }

        /// <summary>
        /// Returns a TextSegment that spans the line on which position is located.
        /// </summary>
        private TextSegment GetLineRangeFromPosition(ReadOnlyCollection<ColumnResult> columns, ReadOnlyCollection<ParagraphResult> floatingElements, ITextPointer position)
        {
            int columnIndex = GetColumnFromPosition(columns, position);

            if (columnIndex < columns.Count || floatingElements.Count > 0)
            {
                ReadOnlyCollection<ParagraphResult> paragraphs = (columnIndex < columns.Count && columnIndex >= 0) ? columns[columnIndex].Paragraphs : _emptyParagraphCollection;
                return GetLineRangeFromPosition(paragraphs, floatingElements, position);
            }

            return TextSegment.Null;
        }

        /// <summary>
        /// Retrieves an oriented text position matching position advanced by 
        /// a number of lines from its initial position.
        /// </summary>
        /// <param name="paragraphs">Collection of paragraphs.</param>
        /// <param name="position">Initial text position of an object/character.</param>
        /// <param name="suggestedX">
        /// The suggested X offset, in pixels, of text position on the destination 
        /// line. If suggestedX is set to Double.NaN it will be ignored, otherwise 
        /// the method will try to find a position on the destination line closest 
        /// to suggestedX.
        /// </param>
        /// <param name="count">Number of lines to advance. Negative means move backwards.</param>
        /// <param name="positionFound">True if ths position was found in the paragraphs collection</param>
        /// <returns>
        /// A TextPointer and its orientation matching suggestedX on the 
        /// destination line.
        /// </returns>
        private ITextPointer GetPositionAtNextLine(ReadOnlyCollection<ParagraphResult> paragraphs, ITextPointer position, double suggestedX, ref int count, out bool positionFound)
        {
            Invariant.Assert(paragraphs != null, "Paragraph collection is empty.");

            // If no position found in table, return original position.
            ITextPointer positionOut = position;
            positionFound = false;

            // Figure out which paragraph contains text position.
            int paragraphIndex = GetParagraphFromPosition(paragraphs, position);

            if (paragraphIndex < paragraphs.Count)
            {
                positionFound = true;
                // Each paragraph type is handled differently:
                // a) ContainerParagraph - process nested paragraphs.
                // b) TextParagraph - find line index of a line containing input text position 
                //    and find the previous/next line in the line array.
                //    If new line (specified by count) is not in the range of this TextParagraph,
                //    update count value by the delta between the current line index and the first/last line.
                // c) TableParagraph - process nested paragraphs.
                if (paragraphs[paragraphIndex] is ContainerParagraphResult)
                {
                    // a) ContainerParagraph - process nested paragraphs.
                    Rect paragraphBox = paragraphs[paragraphIndex].LayoutBox;
                    ReadOnlyCollection<ParagraphResult> nestedParagraphs = ((ContainerParagraphResult)paragraphs[paragraphIndex]).Paragraphs;
                    // Paragraphs collection may be null in case of empty List.
                    Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null.");
                    if (nestedParagraphs.Count > 0)
                    {
                        positionOut = GetPositionAtNextLine(nestedParagraphs, position, suggestedX, ref count, out positionFound);
                    }
                }
                else if (paragraphs[paragraphIndex] is TextParagraphResult)
                {
                    // b) TextParagraph - find line index of a line containing input text position 
                    //    and find the previous/next line in the line array.
                    //    If new line (specified by count) is not in the range of this TextParagraph,
                    //    update count value by the delta between the current line index and the first/last line.

                    // Get index of the line that contains position.
                    ReadOnlyCollection<LineResult> lines = ((TextParagraphResult)paragraphs[paragraphIndex]).Lines;
                    Invariant.Assert(lines != null, "Lines collection is null");
                    if (!((TextParagraphResult)paragraphs[paragraphIndex]).HasTextContent)
                    {
                        // TextParagraph has no lines
                        // this code is a workaround to avoid a crash in this case
                        // We should actually process figures and floaters, if any, here
                        positionOut = position;
                    }
                    else
                    {
                        int lineIndex = TextParagraphView.GetLineFromPosition(lines, position);
                        Invariant.Assert(lineIndex >= 0 && lineIndex < lines.Count, "Line not found.");
                        Rect paragraphBox = paragraphs[paragraphIndex].LayoutBox;

                        // Advance line index by count. If new line index is within this paragraph
                        // set the count to 0 and update line index.
                        // But if the new line index is not within this paragraph, change the count
                        // value by the delta between the current line index and the first/last line.
                        int oldLineIndex = lineIndex;
                        if (lineIndex + count < 0)
                        {
                            lineIndex = 0;
                            count += oldLineIndex;
                        }
                        else if (lineIndex + count > lines.Count - 1)
                        {
                            lineIndex = lines.Count - 1;
                            count -= (lines.Count - 1 - oldLineIndex);
                        }
                        else
                        {
                            lineIndex = lineIndex + count;
                            count = 0;
                        }

                        // If count is 0, the new line is in this paragraph
                        if (count == 0)
                        {
                            // Get position at suggested X. If suggested X is not provided, 
                            // use the first position in the line.
                            if (!DoubleUtil.IsNaN(suggestedX))
                            {
                                positionOut = lines[lineIndex].GetTextPositionFromDistance(suggestedX);
                            }
                            else
                            {
                                positionOut = lines[lineIndex].StartPosition.CreatePointer(LogicalDirection.Forward);
                            }
                        }
                        else
                        {
                            // If count is not 0, the new line is in the next/previous paragraph.
                            // If line has not been moved, return the same position. 
                            if (lineIndex == oldLineIndex)
                            {
                                positionOut = position;
                            }
                            else if (count < 0)
                            {
                                // Just in case there are no lines above, set position to the first line.
                                if (!DoubleUtil.IsNaN(suggestedX))
                                {
                                    positionOut = lines[0].GetTextPositionFromDistance(suggestedX);
                                }
                                else
                                {
                                    positionOut = lines[0].StartPosition.CreatePointer(LogicalDirection.Forward);
                                }
                            }
                            else
                            {
                                // Just in case there are no lines below, set position to the last line.
                                if (!DoubleUtil.IsNaN(suggestedX))
                                {
                                    positionOut = lines[lines.Count - 1].GetTextPositionFromDistance(suggestedX);
                                }
                                else
                                {
                                    positionOut = lines[lines.Count - 1].StartPosition.CreatePointer(LogicalDirection.Forward);
                                }
                            }
                        }
                    }
                }
                else if (paragraphs[paragraphIndex] is TableParagraphResult)
                {
                    // c) TableParagraph - process nested paragraphs.
                    TableParagraphResult tableResult = (TableParagraphResult)paragraphs[paragraphIndex];
                    CellParaClient cpcStart = tableResult.GetCellParaClientFromPosition(position);
                    CellParaClient cpcCur = cpcStart;
                    Rect paragraphBox = paragraphs[paragraphIndex].LayoutBox;

                    while (count != 0 && cpcCur != null && positionFound)
                    {
                        SubpageParagraphResult subpageParagraphResult = (SubpageParagraphResult)cpcCur.CreateParagraphResult();
                        ReadOnlyCollection<ParagraphResult> nestedParagraphs = subpageParagraphResult.Columns[0].Paragraphs;

                        Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null.");
                        // Paragraphs collection may be null in case of empty List.
                        if (nestedParagraphs.Count > 0)
                        {
                            if (cpcCur != cpcStart)
                            {
                                int nesteParagraphIndex = (count > 0) ? 0 : nestedParagraphs.Count - 1;
                                positionOut = GetPositionAtNextLineFromSiblingPara(nestedParagraphs, nesteParagraphIndex, suggestedX - TextDpi.FromTextDpi(cpcCur.Rect.u), ref count);
                                if (positionOut == null)
                                {
                                    positionOut = position;
                                }
                            }
                            else
                            {
                                positionOut = GetPositionAtNextLine(nestedParagraphs, position, suggestedX - subpageParagraphResult.ContentOffset.X, ref count, out positionFound);
                            }
                        }

                        if (count < 0 && positionFound)
                        {
                            cpcCur = tableResult.GetCellAbove(suggestedX, cpcCur.Cell.RowGroupIndex, cpcCur.Cell.RowIndex);
                        }
                        else if (count > 0 && positionFound)
                        {
                            cpcCur = tableResult.GetCellBelow(suggestedX, cpcCur.Cell.RowGroupIndex, cpcCur.Cell.RowIndex + cpcCur.Cell.RowSpan - 1);
                        }
                    }
                }
                else if (paragraphs[paragraphIndex] is SubpageParagraphResult)
                {
                    double newSuggestedX;
                    SubpageParagraphResult subpageParagraphResult = (SubpageParagraphResult)paragraphs[paragraphIndex];
                    positionOut = GetPositionAtNextLine(((SubpageParagraphResult)paragraphs[paragraphIndex]).Columns, subpageParagraphResult.FloatingElements, position, suggestedX - subpageParagraphResult.ContentOffset.X, ref count, out newSuggestedX, out positionFound);
                }

                // If the new line has not been found yet, iterate through sibling paragraphs to
                // find out a new line.
                if (count != 0 && positionFound)
                {
                    if (count > 0)
                    {
                        ++paragraphIndex;
                    }
                    else
                    {
                        --paragraphIndex;
                    }
                    if (paragraphIndex >= 0 && paragraphIndex < paragraphs.Count)
                    {
                        positionOut = GetPositionAtNextLineFromSiblingPara(paragraphs, paragraphIndex, suggestedX, ref count);
                        if (positionOut == null)
                        {
                            // This may happen if next para has no content
                            positionOut = position;
                        }
                    }
                }
            }

            // Do not return null from this point. If positionOut was set to null at any point during the code, we should have
            // set it to the original position
            Invariant.Assert(positionOut != null);
            return positionOut;
        }

        /// <summary>
        /// Retrieves an oriented text position matching position advanced by 
        /// a number of lines from its initial position. Helper function for searching floating elements, where we do not search siblings.
        /// </summary>
        /// <param name="floatingElements">Paragraphs to search</param>
        /// <param name="position">Initial text position of an object/character.</param>
        /// <param name="suggestedX">
        /// The suggested X offset, in pixels, of text position on the destination 
        /// line. If suggestedX is set to Double.NaN it will be ignored, otherwise 
        /// the method will try to find a position on the destination line closest 
        /// to suggestedX.
        /// </param>
        /// <param name="positionFound">True if position is found in a floating para</param>
        /// <param name="count">Number of lines to advance. Negative means move backwards.</param>
        /// <remarks>Searches only in one floating element para and does not search in any siblings</remarks>
        private ITextPointer GetPositionAtNextLineInFloatingElements(ReadOnlyCollection<ParagraphResult> floatingElements, ITextPointer position, double suggestedX, ref int count, out bool positionFound)
        {
            // If no position found in table, return original position.
            ITextPointer positionOut = position;
            positionFound = false;
            ParagraphResult paragraph = null;
            int paragraphIndex = GetParagraphFromPosition(_emptyParagraphCollection, floatingElements, position, out positionFound);
            if (positionFound)
            {
                // Special search in floating element para. Even if the position is not found within the floating element para, i.e. it
                // is within the para but not normalized, we still want to return true for positionFound since this search should not cross floating
                // element boundary. Hence use a separate flag for checking if the position is found within floating element.
                bool positionFoundInNestedPara;
                Invariant.Assert(paragraphIndex < floatingElements.Count);
                paragraph = floatingElements[paragraphIndex];
                Invariant.Assert(paragraph is FigureParagraphResult || paragraph is FloaterParagraphResult);
                if (paragraph is FigureParagraphResult)
                {
                    double newSuggestedX;
                    FigureParagraphResult figureParagraphResult = (FigureParagraphResult)paragraph;
                    ReadOnlyCollection<ColumnResult> columns = figureParagraphResult.Columns;
                    ReadOnlyCollection<ParagraphResult> nestedFloatingElements = figureParagraphResult.FloatingElements;
                    Invariant.Assert(columns != null, "Column collection is null.");
                    Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                    if (columns.Count > 0 || nestedFloatingElements.Count > 0)
                    {
                        positionOut = GetPositionAtNextLine(columns, nestedFloatingElements, position, suggestedX - figureParagraphResult.ContentOffset.X, ref count, out newSuggestedX, out positionFoundInNestedPara);
                    }
                }
                else
                {
                    double newSuggestedX;
                    FloaterParagraphResult floaterParagraphResult = (FloaterParagraphResult)paragraph;
                    ReadOnlyCollection<ColumnResult> columns = floaterParagraphResult.Columns;
                    ReadOnlyCollection<ParagraphResult> nestedFloatingElements = floaterParagraphResult.FloatingElements;
                    Invariant.Assert(columns != null, "Column collection is null.");
                    Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                    if (columns.Count > 0 || nestedFloatingElements.Count > 0)
                    {
                        positionOut = GetPositionAtNextLine(columns, nestedFloatingElements, position, suggestedX - floaterParagraphResult.ContentOffset.X, ref count, out newSuggestedX, out positionFoundInNestedPara);
                    }
                }
            }
            Invariant.Assert(positionOut != null);
            return positionOut;
        }

        /// <summary>
        /// Retrieves an oriented text position matching position advanced by 
        /// a number of lines from its initial position.
        /// </summary>
        private ITextPointer GetPositionAtNextLine(ReadOnlyCollection<ColumnResult> columns, ReadOnlyCollection<ParagraphResult> floatingElements, ITextPointer position, double suggestedX, ref int count, out double newSuggestedX, out bool positionFound)
        {
            ITextPointer positionOut = null;
            newSuggestedX = suggestedX;
            positionFound = false;

            // Check floating elements collection
            if (floatingElements.Count > 0)
            {
                positionOut = GetPositionAtNextLineInFloatingElements(floatingElements, position, suggestedX, ref count, out positionFound);
            }

            if (!positionFound)
            {
                // No success in floating elements, try columns
                int columnIndex = GetColumnFromPosition(columns, position);

                if (columnIndex < columns.Count)
                {
                    // Get the next line from the list of paragraphs.
                    positionFound = true;
                    positionOut = GetPositionAtNextLine(columns[columnIndex].Paragraphs, position, suggestedX, ref count, out positionFound);

                    int oldColumnIndex = columnIndex;

                    // If the new line has not been found yet, iterate through sibling columns to
                    // find out a new line.
                    if (count != 0 && positionFound)
                    {
                        if (count > 0)
                        {
                            ++columnIndex;
                        }
                        else
                        {
                            --columnIndex;
                        }
                        if (columnIndex >= 0 && columnIndex < columns.Count)
                        {
                            suggestedX = (suggestedX - columns[oldColumnIndex].LayoutBox.Left) + columns[columnIndex].LayoutBox.Left;

                            ITextPointer siblingColumnPosition = GetPositionAtNextLineFromSiblingColumn(columns, columnIndex, suggestedX, ref newSuggestedX, ref count);

                            if (siblingColumnPosition != null)
                            {
                                positionOut = siblingColumnPosition;
                            }
                        }
                    }
                }
            }

            Invariant.Assert(positionOut != null);
            return positionOut;
        }

        /// <summary>
        /// Retrieves an oriented text position advancing by number of lines from its initial position.
        /// </summary>
        /// <param name="paragraphs">Collection of paragraphs.</param>
        /// <param name="paragraphIndex">Current paragraph index.</param>
        /// <param name="suggestedX">
        /// The suggested X offset, in pixels, of text position on the destination 
        /// line. If suggestedX is set to Double.NaN it will be ignored, otherwise 
        /// the method will try to find a position on the destination line closest 
        /// to suggestedX.
        /// </param>
        /// <param name="count">Number of lines to advance. Negative means move backwards.</param>
        /// <returns>
        /// A TextPointer and its orientation matching suggestedX on the 
        /// destination line.
        /// </returns>
        private ITextPointer GetPositionAtNextLineFromSiblingPara(ReadOnlyCollection<ParagraphResult> paragraphs, int paragraphIndex, double suggestedX, ref int count)
        {
            Invariant.Assert(count != 0);
            Invariant.Assert(paragraphIndex >= 0 && paragraphIndex < paragraphs.Count, "Paragraph collection is empty.");

            ITextPointer positionOut = null;

            // Iterate through sibling paragraphs to find out a new line.
            while (paragraphIndex >= 0 && paragraphIndex < paragraphs.Count)
            {
                // Each paragraph type is handled differently:
                // a) ContainerParagraph - process nested paragraphs starting from the first
                //    or last nested paragraph (depending on the count value sign).
                // b) TextParagraph - start from the first or last line (depending on the count 
                //    value sign) and find the previous/next line in the line array.
                //    If new line (specified by count) is not in the range of this TextParagraph,
                //    update count value by the line count.
                // c) TableParagraph - not impl.
                // d) DocumentPageParagraph - skip this paragraph.
                // e) UIElementParagraph - skip this paragraph.
                if (paragraphs[paragraphIndex] is ContainerParagraphResult)
                {
                    // a) ContainerParagraph - process nested paragraphs starting from the first
                    //    or last nested paragraph (depending on the count value sign).
                    Rect paragraphBox = paragraphs[paragraphIndex].LayoutBox;
                    ReadOnlyCollection<ParagraphResult> nestedParagraphs = ((ContainerParagraphResult)paragraphs[paragraphIndex]).Paragraphs;
                    // Paragraphs collection may be null in case of empty List.

                    Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null.");
                    if (nestedParagraphs.Count > 0)
                    {
                        int nesteParagraphIndex = (count > 0) ? 0 : nestedParagraphs.Count - 1;
                        positionOut = GetPositionAtNextLineFromSiblingPara(nestedParagraphs, nesteParagraphIndex, suggestedX, ref count);
                    }
                }
                else if (paragraphs[paragraphIndex] is TextParagraphResult)
                {
                    // b) TextParagraph - start from the first or last line (depending on the count 
                    //    value sign) and find the previous/next line in the line array.
                    //    If new line (specified by count) is not in the range of this TextParagraph,
                    //    update count value by the line count.
                    positionOut = GetPositionAtNextLineFromSiblingTextPara((TextParagraphResult)paragraphs[paragraphIndex], suggestedX, ref count);
                    if (count == 0)
                    {
                        // Line has been found.
                        break;
                    }
                }
                else if (paragraphs[paragraphIndex] is TableParagraphResult)
                {
                    TableParagraphResult tableResult = (TableParagraphResult)paragraphs[paragraphIndex];
                    Rect paragraphBox = paragraphs[paragraphIndex].LayoutBox;
                    CellParaClient cpcCur = null;

                    if (count < 0)
                    {
                        cpcCur = tableResult.GetCellAbove(suggestedX, int.MaxValue, int.MaxValue);
                    }
                    else if (count > 0)
                    {
                        cpcCur = tableResult.GetCellBelow(suggestedX, int.MinValue, int.MinValue);
                    }

                    while (count != 0 && cpcCur != null)
                    {
                        SubpageParagraphResult subpageParagraphResult = (SubpageParagraphResult)cpcCur.CreateParagraphResult();
                        ReadOnlyCollection<ParagraphResult> nestedParagraphs = subpageParagraphResult.Columns[0].Paragraphs;
                        // Paragraphs collection may be null in case of empty List.
                        Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null.");
                        if (nestedParagraphs.Count > 0)
                        {
                            int nesteParagraphIndex = (count > 0) ? 0 : nestedParagraphs.Count - 1;
                            positionOut = GetPositionAtNextLineFromSiblingPara(nestedParagraphs, nesteParagraphIndex, suggestedX - subpageParagraphResult.ContentOffset.X, ref count);
                        }

                        if (count < 0)
                        {
                            cpcCur = tableResult.GetCellAbove(suggestedX, cpcCur.Cell.RowGroupIndex, cpcCur.Cell.RowIndex);
                        }
                        else if (count > 0)
                        {
                            cpcCur = tableResult.GetCellBelow(suggestedX, cpcCur.Cell.RowGroupIndex, cpcCur.Cell.RowIndex + cpcCur.Cell.RowSpan - 1);
                        }
                    }
                }
                else if (paragraphs[paragraphIndex] is SubpageParagraphResult)
                {
                    // a) ContainerParagraph - process nested paragraphs starting from the first
                    //    or last nested paragraph (depending on the count value sign).
                    Rect paragraphBox = paragraphs[paragraphIndex].LayoutBox;
                    SubpageParagraphResult subpageParagraphResult = (SubpageParagraphResult)paragraphs[paragraphIndex];
                    ReadOnlyCollection<ParagraphResult> nestedParagraphs = subpageParagraphResult.Columns[0].Paragraphs;

                    Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null.");
                    if (nestedParagraphs.Count > 0)
                    {
                        int nesteParagraphIndex = (count > 0) ? 0 : nestedParagraphs.Count - 1;
                        positionOut = GetPositionAtNextLineFromSiblingPara(nestedParagraphs, nesteParagraphIndex, suggestedX - subpageParagraphResult.ContentOffset.X, ref count);
                    }
                }
                else if (paragraphs[paragraphIndex] is UIElementParagraphResult)
                {
                    // UIElementParagraphResult - has only one line with 2 positions, at start and end of BUIC
                    if (count < 0)
                    {
                        count++;
                    }
                    else
                    {
                        count--;
                    }
                    if (count == 0)
                    {
                        // We need to get a position in this paragraph
                        Rect paragraphBox = paragraphs[paragraphIndex].LayoutBox;
                        BlockUIContainer blockUIContainer = paragraphs[paragraphIndex].Element as BlockUIContainer;
                        if (blockUIContainer != null)
                        {
                            if (DoubleUtil.LessThanOrClose(suggestedX, paragraphBox.Width / 2))
                            {
                                positionOut = blockUIContainer.ContentStart.CreatePointer(LogicalDirection.Forward);
                            }
                            else
                            {
                                positionOut = blockUIContainer.ContentEnd.CreatePointer(LogicalDirection.Backward);
                            }
                        }
                    }
                }

                // If count is not 0, the new line is in the next/previous paragraph.
                if (count < 0)
                {
                    --paragraphIndex;
                }
                else if (count > 0)
                {
                    ++paragraphIndex;
                }
                else
                {
                    break;
                }
            }

            return positionOut;
        }

        /// <summary>
        /// Retrieves an oriented text position advancing by number of lines from its initial position.
        /// </summary>
        /// <param name="paragraph">Text paragraph.</param>
        /// <param name="suggestedX">
        /// The suggested X offset, in pixels, of text position on the destination 
        /// line. If suggestedX is set to Double.NaN it will be ignored, otherwise 
        /// the method will try to find a position on the destination line closest 
        /// to suggestedX.
        /// </param>
        /// <param name="count">Number of lines to advance. Negative means move backwards.</param>
        /// <returns>
        /// A TextPointer and its orientation matching suggestedX on the 
        /// destination line.
        /// </returns>
        private ITextPointer GetPositionAtNextLineFromSiblingTextPara(TextParagraphResult paragraph, double suggestedX, ref int count)
        {
            ITextPointer positionOut = null;

            // TextParagraph - start from the first or last line (depending on the count 
            // value sign) and find the previous/next line in the line array.
            // If new line (specified by count) is not in the range of this TextParagraph,
            // update count value by the line count.

            ReadOnlyCollection<LineResult> lines = paragraph.Lines;
            Invariant.Assert(lines != null, "Lines collection is null");
            if (!paragraph.HasTextContent)
            {
                // Paragraph has no text content, which means it either has no lines at all or just figures and floaters. 
                // In either case, we cannot step into it from GetPositionAtNextLine. We must set positionOut to null and 
                // try to advance elsewhere. 
                positionOut = null;
            }
            else
            {
                Rect paragraphBox = paragraph.LayoutBox;

                // We are entering this paragraph. Get index of the first/last line 
                // (depending on the sing of count value) and try to find out line index.
                int lineIndex = (count > 0) ? 0 : lines.Count - 1;

                // We are about to analyze the first/last line in the paragraph, so adjust the 
                // count to take it into account.
                if (count < 0)
                {
                    ++count;
                }
                else
                {
                    --count;
                }

                // If the new line is not in the range of this paragraph , change the count
                // value by the number of lines in the paragraph.
                if (lineIndex + count < 0)
                {
                    count += lineIndex;
                }
                else if (lineIndex + count > lines.Count - 1)
                {
                    count -= (lines.Count - 1 - lineIndex);
                }
                else
                {
                    lineIndex = lineIndex + count;
                    count = 0;
                }

                // If count is 0, the new line is in this paragraph
                if (count == 0)
                {
                    // Get position at suggested X. If suggested X is not provided, 
                    // use the first position in the line.
                    if (!DoubleUtil.IsNaN(suggestedX))
                    {
                        positionOut = lines[lineIndex].GetTextPositionFromDistance(suggestedX);
                    }
                    else
                    {
                        positionOut = lines[lineIndex].StartPosition.CreatePointer(LogicalDirection.Forward);
                    }
                }
                else
                {
                    // If count is not 0, the new line is in the next/previous paragraph.
                    if (count < 0)
                    {
                        // Just in case there are no lines above, set position to the first line.
                        if (!DoubleUtil.IsNaN(suggestedX))
                        {
                            positionOut = lines[0].GetTextPositionFromDistance(suggestedX);
                        }
                        else
                        {
                            positionOut = lines[0].StartPosition.CreatePointer(LogicalDirection.Forward);
                        }
                    }
                    else
                    {
                        // Just in case there are no lines below, set position to the last line.
                        if (!DoubleUtil.IsNaN(suggestedX))
                        {
                            positionOut = lines[lines.Count - 1].GetTextPositionFromDistance(suggestedX);
                        }
                        else
                        {
                            positionOut = lines[lines.Count - 1].StartPosition.CreatePointer(LogicalDirection.Forward);
                        }
                    }
                }
            }

            return positionOut;
        }

        /// <summary>
        /// Retrieves an oriented text position advancing by number of lines from its initial position.
        /// </summary>
        /// <param name="columns">Collection of columns.</param>
        /// <param name="columnIndex">Current column index.</param>
        /// <param name="columnSuggestedX">
        /// The suggested X offset of text position on the destination line. 
        /// In pixels and relative to the current column.
        /// </param>
        /// <param name="newSuggestedX">
        /// newSuggestedX is the offset at the position moved (useful when moving 
        /// between columns).
        /// </param>
        /// <param name="count">Number of lines to advance. Negative means move backwards.</param>
        /// <returns>
        /// A TextPointer and its orientation matching suggestedX on the 
        /// destination line.
        /// </returns>
        private ITextPointer GetPositionAtNextLineFromSiblingColumn(ReadOnlyCollection<ColumnResult> columns, int columnIndex, double columnSuggestedX, ref double newSuggestedX, ref int count)
        {
            ITextPointer positionOut = null;

            // Iterate through sibling columns to find out a new line.
            while (columnIndex >= 0 && columnIndex < columns.Count)
            {
                double currentSuggestedX = columnSuggestedX + columns[columnIndex].LayoutBox.Left;
                ReadOnlyCollection<ParagraphResult> paragraphs = columns[columnIndex].Paragraphs;
                // Paragraphs collection may be null in case of empty List.
                Invariant.Assert(paragraphs != null, "Paragraph collection is null.");
                if (paragraphs.Count > 0)
                {
                    // Process paragraphs starting from the first or last nested paragraph 
                    // (depending on the count value sign).
                    int paragraphIndex = (count > 0) ? 0 : paragraphs.Count - 1;
                    positionOut = GetPositionAtNextLineFromSiblingPara(paragraphs, paragraphIndex, columnSuggestedX, ref count);
                }

                // Update new suggestedX position with position in the current column.
                newSuggestedX = columnSuggestedX;

                // If count is not 0, the new line is in the next/previous paragraph.
                if (count < 0)
                {
                    --columnIndex;
                }
                else if (count > 0)
                {
                    ++columnIndex;
                }
                else
                {
                    break;
                }
            }

            return positionOut;
        }

        /// <summary>
        /// Determines whenever TextView contains specified position.
        /// </summary>
        /// <param name="position">A position to test.</param>
        /// <returns>
        /// True if TextView contains specified text position. 
        /// Otherwise returns false.
        /// </returns>
        private bool ContainsCore(ITextPointer position)
        {
            ReadOnlyCollection<TextSegment> segments = this.TextSegmentsCore;
            Invariant.Assert(segments != null, "TextSegment collection is empty.");
            return Contains(position, segments);
        }

        /// <summary>
        /// Get glyph runs from paragraph collection.
        /// </summary>
        /// <param name="glyphRuns">Preallocated collection of glyph runs.</param>
        /// <param name="start">Start position.</param>
        /// <param name="end">End position.</param>
        /// <param name="paragraphs">Collection of paragraphs.</param>
        /// <returns>True if needs to continue search. False if all glyph runs have been retrieved.</returns>
        private bool GetGlyphRunsFromParagraphs(List<GlyphRun> glyphRuns, ITextPointer start, ITextPointer end, ReadOnlyCollection<ParagraphResult> paragraphs)
        {
            Invariant.Assert(paragraphs != null, "Paragraph collection is null.");

            bool cont = true;
            // Iterate through columns and get all glyph runs between Start and End
            for (int index = 0; index < paragraphs.Count; index++)
            {
                ParagraphResult paragraph = paragraphs[index];
                if (paragraph is TextParagraphResult)
                {
                    TextParagraphResult tpr = (TextParagraphResult)paragraph;
                    if (start.CompareTo(tpr.EndPosition) < 0 && end.CompareTo(tpr.StartPosition) > 0)
                    {
                        ITextPointer startRange = start.CompareTo(tpr.StartPosition) < 0 ? tpr.StartPosition : start;
                        ITextPointer endRange = end.CompareTo(tpr.EndPosition) < 0 ? end : tpr.EndPosition;
                        tpr.GetGlyphRuns(glyphRuns, startRange, endRange);
                    }
                    // Do not continue, if end of requested range has been reached.
                    if (end.CompareTo(tpr.EndPosition) < 0)
                    {
                        cont = false;
                        break;
                    }
                }
                else if (paragraph is ContainerParagraphResult)
                {
                    ReadOnlyCollection<ParagraphResult> nestedParagraphs = ((ContainerParagraphResult)paragraph).Paragraphs;
                    Invariant.Assert(nestedParagraphs != null, "Paragraph collection is null.");
                    if (nestedParagraphs.Count > 0)
                    {
                        cont = GetGlyphRunsFromParagraphs(glyphRuns, start, end, nestedParagraphs);
                        // Do not continue, if end of requested range has been reached.
                        if (!cont)
                        {
                            break;
                        }
                    }
                }
            }
            return cont;
        }

        /// <summary>
        /// Get glyph runs from floating element collection.
        /// </summary>
        /// <param name="glyphRuns">Preallocated collection of glyph runs. Helper function for searching floating elements.
        /// In floating elements collection, if we match the start position to a para, we return runs either up to end position or to end of para,
        /// whichever is first. We do not collect glyph runs across multiple floating elements</param>
        /// <param name="start">Start position.</param>
        /// <param name="end">End position.</param>
        /// <param name="floatingElements">Collection of paragraphs.</param>
        /// <param name="success">True if the range starts in one of the floating elements</param>
        private void GetGlyphRunsFromFloatingElements(List<GlyphRun> glyphRuns, ITextPointer start, ITextPointer end, ReadOnlyCollection<ParagraphResult> floatingElements, out bool success)
        {
            Invariant.Assert(floatingElements != null, "Paragraph collection is null.");
            success = false;
            // Iterate through columns and get all glyph runs between Start and End
            for (int index = 0; index < floatingElements.Count; index++)
            {
                ParagraphResult paragraph = floatingElements[index];
                Invariant.Assert(paragraph is FigureParagraphResult || paragraph is FloaterParagraphResult);
                if (paragraph.Contains(start, true))
                {
                    success = true;
                    ITextPointer endThisPara = end.CompareTo(paragraph.EndPosition) < 0 ? end : paragraph.EndPosition;
                    if (paragraph is FigureParagraphResult)
                    {
                        FigureParagraphResult figureParagraphResult = (FigureParagraphResult)paragraph;
                        ReadOnlyCollection<ColumnResult> columns = figureParagraphResult.Columns;
                        ReadOnlyCollection<ParagraphResult> nestedFloatingElements = figureParagraphResult.FloatingElements;
                        Invariant.Assert(columns != null, "Column collection is null.");
                        Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                        if (columns.Count > 0 || nestedFloatingElements.Count > 0)
                        {
                            GetGlyphRuns(glyphRuns, start, endThisPara, columns, nestedFloatingElements);
                        }
                    }
                    else if (paragraph is FloaterParagraphResult)
                    {
                        FloaterParagraphResult floaterParagraphResult = (FloaterParagraphResult)paragraph;
                        ReadOnlyCollection<ColumnResult> columns = floaterParagraphResult.Columns;
                        ReadOnlyCollection<ParagraphResult> nestedFloatingElements = floaterParagraphResult.FloatingElements;
                        Invariant.Assert(columns != null, "Column collection is null.");
                        Invariant.Assert(nestedFloatingElements != null, "Paragraph collection is null.");
                        if (columns.Count > 0 || nestedFloatingElements.Count > 0)
                        {
                            GetGlyphRuns(glyphRuns, start, endThisPara, columns, nestedFloatingElements);
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Get glyph runs from paragraph collection.
        /// </summary>
        private void GetGlyphRuns(List<GlyphRun> glyphRuns, ITextPointer start, ITextPointer end, ReadOnlyCollection<ColumnResult> columns, ReadOnlyCollection<ParagraphResult> floatingElements)
        {
            int columnIndexStart;
            int columnIndexEnd;

            // Search floating elements first
            bool success = false;
            if (floatingElements.Count > 0)
            {
                GetGlyphRunsFromFloatingElements(glyphRuns, start, end, floatingElements, out success);
            }

            if (!success)
            {
                // Figure out which columns contain start and end
                for (columnIndexStart = 0; columnIndexStart < columns.Count; columnIndexStart++)
                {
                    ColumnResult columnResult = columns[columnIndexStart];
                    if (start.CompareTo(columnResult.StartPosition) >= 0 && start.CompareTo(columnResult.EndPosition) <= 0)
                        break;
                }
                for (columnIndexEnd = columnIndexStart; columnIndexEnd < columns.Count; columnIndexEnd++)
                {
                    ColumnResult columnResult = columns[columnIndexStart];
                    if (end.CompareTo(columnResult.StartPosition) >= 0 && end.CompareTo(columnResult.EndPosition) <= 0)
                        break;
                }
                Invariant.Assert(columnIndexStart < columns.Count && columnIndexEnd < columns.Count, "Start or End position does not belong to TextView's content range");

                // Iterate through columns and get all glyph runs between Start and End
                while (columnIndexStart <= columnIndexEnd)
                {
                    ReadOnlyCollection<ParagraphResult> paragraphs = columns[columnIndexStart].Paragraphs;
                    if (paragraphs != null && paragraphs.Count > 0)
                    {
                        GetGlyphRunsFromParagraphs(glyphRuns, start, end, paragraphs);
                    }
                    ++columnIndexStart;
                }
            }
        }

        /// <summary>
        /// Retrieve TextSegments collection for the content of the page represented
        /// by the TextView.
        /// </summary>
        private ReadOnlyCollection<TextSegment> GetTextSegments()
        {
            ReadOnlyCollection<TextSegment> textSegments;

            // For a bottomless page there is always one TextSegment, starting at
            // the beginning of TextContainer and ending at the last calculated
            // position, which is:
            // a) the end of TextContainer, or
            // b) the interruption  position when background layout is in progress.
            // For a finite page there is no such optimization, so need to query all
            // columns and merge their TextSegments.
            if (!_owner.FinitePage)
            {
                ITextPointer segmentEnd = _textContainer.End;
                BackgroundFormatInfo backgroundFormatInfo = _owner.StructuralCache.BackgroundFormatInfo;
                if (backgroundFormatInfo != null && backgroundFormatInfo.CPInterrupted != -1)
                {
                    segmentEnd = _textContainer.Start.CreatePointer(backgroundFormatInfo.CPInterrupted, LogicalDirection.Backward);
                }
                List<TextSegment> segments = new List<TextSegment>(1);
                segments.Add(new TextSegment(_textContainer.Start, segmentEnd, true));
                textSegments = new ReadOnlyCollection<TextSegment>(segments);
            }
            else
            {
                TextContentRange textContentRange = new TextContentRange();

                // Merge ranges from all columns.
                ReadOnlyCollection<ColumnResult> columns = Columns;
                Invariant.Assert(columns != null, "Column collection is empty.");
                for (int index = 0; index < columns.Count; index++)
                {
                    textContentRange.Merge(columns[index].TextContentRange);
                }

                textSegments = textContentRange.GetTextSegments();
            }
            Invariant.Assert(textSegments != null);
            return textSegments;
        }

        /// <summary>
        /// Transforms point to content's coordinate system.
        /// </summary>
        /// <param name="point">Point to which transform is applied.</param>
        private void TransformToContent(ref Point point)
        {
            Point newPoint;

            // DocumentPage.Visual for printing scenarions needs to be always returned
            // in LeftToRight FlowDirection. Hence, if the document is RightToLeft, 
            // mirroring transform need to be applied to the content of DocumentPage.Visual.
            FlowDirection flowDirection = (FlowDirection)_owner.StructuralCache.PropertyOwner.GetValue(FlowDocument.FlowDirectionProperty);
            if (flowDirection == FlowDirection.RightToLeft)
            {
                MatrixTransform transform = new MatrixTransform(-1.0, 0.0, 0.0, 1.0, _owner.Size.Width, 0.0);
                transform.TryTransform(point, out newPoint);
                point = newPoint;
            }
        }

        /// <summary>
        /// Transforms point to content's coordinate system.
        /// </summary>
        /// <param name="rect">Rect to which transform is applied.</param>
        private void TransformToContent(ref Rect rect)
        {
            // DocumentPage.Visual for printing scenarions needs to be always returned
            // in LeftToRight FlowDirection. Hence, if the document is RightToLeft, 
            // mirroring transform need to be applied to the content of DocumentPage.Visual.
            FlowDirection flowDirection = (FlowDirection)_owner.StructuralCache.PropertyOwner.GetValue(FlowDocument.FlowDirectionProperty);
            if (flowDirection == FlowDirection.RightToLeft)
            {
                MatrixTransform transform = new MatrixTransform(-1.0, 0.0, 0.0, 1.0, _owner.Size.Width, 0.0);
                rect = transform.TransformBounds(rect);
            }
        }

        /// <summary>
        /// Transforms Rect from content's coordinate system.
        /// </summary>
        /// <param name="rect">Rect to which content's offset is applied.</param>
        /// <param name="transform">Content's transform.</param>
        private void TransformFromContent(ref Rect rect, out Transform transform)
        {
            // Set transform to identity
            transform = Transform.Identity;

            if (rect == Rect.Empty)
            {
                return;
            }

            // DocumentPage.Visual for printing scenarions needs to be always returned
            // in LeftToRight FlowDirection. Hence, if the document is RightToLeft, 
            // mirroring transform need to be applied to the content of DocumentPage.Visual.
            FlowDirection flowDirection = (FlowDirection)_owner.StructuralCache.PropertyOwner.GetValue(FlowDocument.FlowDirectionProperty);
            if (flowDirection == FlowDirection.RightToLeft)
            {
                transform = new MatrixTransform(-1.0, 0.0, 0.0, 1.0, _owner.Size.Width, 0.0);
            }
        }

        /// <summary>
        /// Transforms point to content's coordinate system.
        /// </summary>
        /// <param name="point">Point to which transform is applied.</param>
        private void TransformFromContent(ref Point point)
        {
            Point newPoint;

            // DocumentPage.Visual for printing scenarions needs to be always returned
            // in LeftToRight FlowDirection. Hence, if the document is RightToLeft, 
            // mirroring transform need to be applied to the content of DocumentPage.Visual.
            FlowDirection flowDirection = (FlowDirection)_owner.StructuralCache.PropertyOwner.GetValue(FlowDocument.FlowDirectionProperty);
            if (flowDirection == FlowDirection.RightToLeft)
            {
                MatrixTransform transform = new MatrixTransform(-1.0, 0.0, 0.0, 1.0, _owner.Size.Width, 0.0);
                transform.TryTransform(point, out newPoint);
                point = newPoint;
            }
        }

        /// <summary>
        /// Transforms Geometry from content's coordinate system.
        /// </summary>
        /// <param name="geometry">Geometry to which content's transform is applied.</param>
        private void TransformFromContent(Geometry geometry)
        {
            // DocumentPage.Visual for printing scenarions needs to be always returned
            // in LeftToRight FlowDirection. Hence, if the document is RightToLeft, 
            // mirroring transform need to be applied to the content of DocumentPage.Visual.
            FlowDirection flowDirection = (FlowDirection)_owner.StructuralCache.PropertyOwner.GetValue(FlowDocument.FlowDirectionProperty);
            if (flowDirection == FlowDirection.RightToLeft)
            {
                MatrixTransform transform = new MatrixTransform(-1.0, 0.0, 0.0, 1.0, _owner.Size.Width, 0.0);
                CaretElement.AddTransformToGeometry(geometry, transform);
            }
        }

        /// <summary>
        /// Transforms point to subpage's coordinates.
        /// </summary>
        /// <param name="point">Point to which transform is applied.</param>
        /// <param name="subpageOffset"> Offset of subpage</param>
        private static void TransformToSubpage(ref Point point, Vector subpageOffset)
        {
            point -= subpageOffset;
        }

        /// <summary>
        /// Transforms rect to subpage's coordinates.
        /// </summary>
        /// <param name="rect">Rect to which transform is applied.</param>
        /// <param name="subpageOffset">Subpage offset</param>
        private static void TransformToSubpage(ref Rect rect, Vector subpageOffset)
        {
            if (rect == Rect.Empty)
            {
                return;
            }
            rect.Offset(-subpageOffset);
        }

        /// <summary>
        /// Transforms Rect from subpage coordinates
        /// </summary>
        /// <param name="rect">Rect to which content's offset is applied.</param>
        /// <param name="subpageOffset"> Subpage offset.</param>
        private static void TransformFromSubpage(ref Rect rect, Vector subpageOffset)
        {
            if (rect == Rect.Empty)
            {
                return;
            }
            rect.Offset(subpageOffset);
        }

        /// <summary>
        /// Transforms Geometry from subpage's coordinate system.
        /// </summary>
        /// <param name="geometry">Geometry to which content's transform is applied.</param>
        /// <param name="subpageOffset">Subpage offset.</param>
        private static void TransformFromSubpage(Geometry geometry, Vector subpageOffset)
        {
            if (geometry != null)
            {
                if (!DoubleUtil.IsZero(subpageOffset.X) || !DoubleUtil.IsZero(subpageOffset.Y))
                {
                    TranslateTransform translateTransform = new TranslateTransform(subpageOffset.X, subpageOffset.Y);
                    CaretElement.AddTransformToGeometry(geometry, translateTransform);
                }
            }
        }

        /// <summary>
        /// Returns the layout box edge associated with this block if a text position is located at the end of the block element with appropriate
        /// Logical direction. 
        /// </summary>
        /// <param name="paragraphResult">Result to check edges.</param>
        /// <param name="textPointer">Text Pointer to compare against</param>
        /// <returns>
        /// Returns rect for edge, or Rect.Empty if textposition not on edge
        /// </returns>
        private Rect GetRectangleFromEdge(ParagraphResult paragraphResult, ITextPointer textPointer)
        {
            TextElement textElement = paragraphResult.Element as TextElement;

            if (textElement != null)
            {
                if (textPointer.LogicalDirection == LogicalDirection.Forward && textPointer.CompareTo(textElement.ElementStart) == 0)
                {
                    return new Rect(paragraphResult.LayoutBox.Left, paragraphResult.LayoutBox.Top, 0.0, paragraphResult.LayoutBox.Height);
                }

                if (textPointer.LogicalDirection == LogicalDirection.Backward && textPointer.CompareTo(textElement.ElementEnd) == 0)
                {
                    return new Rect(paragraphResult.LayoutBox.Right, paragraphResult.LayoutBox.Top, 0.0, paragraphResult.LayoutBox.Height);
                }
            }

            return Rect.Empty;
        }

        /// <summary>
        /// Returns the layout box edge associated with this block if a text position is located at the end of the block content 
        /// This is for elements like BlockUIContainer where the content start/end (regardless of direction)has the same rectangle as
        /// the element start/end, we treat BUC as a line with 2 positions at start and end
        /// </summary>
        /// <param name="paragraphResult">Result to check edges.</param>
        /// <param name="textPointer">Text Pointer to compare against</param>
        /// <returns>
        /// Returns rect for edge, or Rect.Empty if textposition not on edge
        /// </returns>
        private Rect GetRectangleFromContentEdge(ParagraphResult paragraphResult, ITextPointer textPointer)
        {
            TextElement textElement = paragraphResult.Element as TextElement;
            if (textElement != null)
            {
                // We enable this only for BlockUIContainer, it would be wrong to return layout box Rect from other elements' Content start/end
                Invariant.Assert(textElement is BlockUIContainer, "Expecting BlockUIContainer");

                // No need to consider position's LogicalDirection here, ContentStart's backward context for BUIC should be ElementStart, for which 
                // we also want the same rectangle; the same applies to ContentEnd
                if (textPointer.CompareTo(textElement.ContentStart) == 0)
                {
                    return new Rect(paragraphResult.LayoutBox.Left, paragraphResult.LayoutBox.Top, 0.0, paragraphResult.LayoutBox.Height);
                }

                if (textPointer.CompareTo(textElement.ContentEnd) == 0)
                {
                    return new Rect(paragraphResult.LayoutBox.Right, paragraphResult.LayoutBox.Top, 0.0, paragraphResult.LayoutBox.Height);
                }
            }

            return Rect.Empty;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// Root of layout structure visualizing content.
        /// </summary>
        private readonly FlowDocumentPage _owner;

        /// <summary>
        /// TextContainer providing content for this view.
        /// </summary>
        private readonly ITextContainer _textContainer;

        /// <summary>
        /// Cached collection of ColumnResults.
        /// </summary>
        private ReadOnlyCollection<ColumnResult> _columns;

        /// <summary>
        /// Cached collection of ColumnResults.
        /// </summary>
        private ReadOnlyCollection<ParagraphResult> _floatingElements;

        /// <summary>
        /// Cached collection of ColumnResults.
        /// </summary>
        private static ReadOnlyCollection<ParagraphResult> _emptyParagraphCollection = new ReadOnlyCollection<ParagraphResult>(new List<ParagraphResult>(0));

        /// <summary>
        /// Cached collection of TextSegments.
        /// </summary>
        private ReadOnlyCollection<TextSegment> _segments;

        /// <summary>
        /// True if the view has some text content (not just figures/floaters)
        /// </summary>
        private bool _hasTextContent;

        #endregion Private Fields
    }
}

