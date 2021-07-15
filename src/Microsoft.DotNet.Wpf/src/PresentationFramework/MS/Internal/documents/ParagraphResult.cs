// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: The ParagraphResult class provides access to layout-calculated 
//              information for a paragraph. 
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using MS.Internal.PtsHost;
using MS.Internal.Text;

namespace MS.Internal.Documents
{
    /// <summary>
    /// The ParagraphResult class provides access to layout-calculated 
    /// information for a paragraph.
    /// </summary>
    internal abstract class ParagraphResult
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
        /// <param name="paraClient">Object representing a paragraph.</param>
        internal ParagraphResult(BaseParaClient paraClient)
        {
            _paraClient = paraClient;
            _layoutBox = _paraClient.Rect.FromTextDpi();
            _element = paraClient.Paragraph.Element;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="paraClient">Object representing a paragraph.</param>
        /// <param name="layoutBox">Layout box for paragraph.</param>
        /// <param name="element">Element associated with this paragraph result.</param>
        internal ParagraphResult(BaseParaClient paraClient, Rect layoutBox, DependencyObject element) : this(paraClient)
        {
            _layoutBox = layoutBox;
            _element = element;
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Returns whether the position is contained in this paragraph.
        /// </summary>
        /// <param name="position">A position to test.</param>
        /// <param name="strict">Apply strict validation rules.</param>
        /// <returns>
        /// True if column contains specified text position. 
        /// Otherwise returns false.
        /// </returns>
        internal virtual bool Contains(ITextPointer position, bool strict)
        {
            EnsureTextContentRange();
            return _contentRange.Contains(position, strict);
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Represents the beginning of the paragraph’s contents.
        /// </summary>
        internal ITextPointer StartPosition
        {
            get
            {
                EnsureTextContentRange();
                return _contentRange.StartPosition;
            }
        }

        /// <summary>
        /// Represents the end of the paragraph’s contents.
        /// </summary>
        internal ITextPointer EndPosition
        {
            get
            {
                EnsureTextContentRange();
                return _contentRange.EndPosition;
            }
        }

        /// <summary>
        /// The bounding rectangle of the paragraph; this is relative 
        /// to the parent bounding box.
        /// </summary>
        internal Rect LayoutBox { get { return _layoutBox; } }

        /// <summary>
        /// Element associated with the paragraph.
        /// </summary>
        internal DependencyObject Element { get { return _element; } }

        /// <summary>
        /// In derived classes, will return the _hasTextContent member whose value is set if the paragraph
        /// has text content.
        /// </summary>
        internal virtual bool HasTextContent
        {
            get
            {
                return false;
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
        /// Retrive TextContentRange if necessary.
        /// </summary>
        private void EnsureTextContentRange()
        {
            if (_contentRange == null)
            {
                _contentRange = _paraClient.GetTextContentRange();
                Invariant.Assert(_contentRange != null);
            }
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// Object representing a paragraph.
        /// </summary>
        protected readonly BaseParaClient _paraClient;

        /// <summary>
        /// Layout rectangle of the paragraph.
        /// </summary>
        protected readonly Rect _layoutBox;

        /// <summary>
        /// Element paragraph is associated with
        /// </summary>
        protected readonly DependencyObject _element;

        /// <summary>
        /// TextContentRanges representing the column's contents.
        /// </summary>
        private TextContentRange _contentRange;

        /// <summary>
        /// True if the paragraph or any nested paragraphs has text content
        /// </summary>
        protected bool _hasTextContent;

        #endregion Private Fields
    }

    /// <summary>
    /// The ParagraphResult for a paragraph which only contains other 
    /// paragraphs.
    /// </summary>
    internal sealed class ContainerParagraphResult : ParagraphResult
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
        /// <param name="paraClient">Object representing a paragraph.</param>
        internal ContainerParagraphResult(ContainerParaClient paraClient)
            : base(paraClient)
        {
        }

        #endregion Constructors


        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Returns tight bounding geometry for the given text range.
        /// </summary>
        /// <param name="startPosition">Start position of the range.</param>
        /// <param name="endPosition">End position of the range.</param>
        /// <param name="visibleRect">Visible clipping area</param>
        /// <returns>Geometry object containing tight bound.</returns>
        internal Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition, Rect visibleRect)
        {
            return (((ContainerParaClient)_paraClient).GetTightBoundingGeometryFromTextPositions(startPosition, endPosition, visibleRect));
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Collection of paragraphs in this paragraph.
        /// </summary>
        internal ReadOnlyCollection<ParagraphResult> Paragraphs
        {
            get
            {
                if (_paragraphs == null)
                {
                    // While getting children paragraph results, each paragraph is queried for text content
                    _paragraphs = ((ContainerParaClient)_paraClient).GetChildrenParagraphResults(out _hasTextContent);
                }
                Invariant.Assert(_paragraphs != null, "Paragraph collection is empty");
                return _paragraphs;
            }
        }

        /// <summary>
        /// True if the paragraph has text content, i.e. not just figures and floaters. For a container paragraph, this is
        /// true if any of its children paras has text content
        /// </summary>
        internal override bool HasTextContent
        {
            get
            {
                if (_paragraphs == null)
                {
                    // Getting Paragraphs collection queries each paragraph for text content and sets the value
                    ReadOnlyCollection<ParagraphResult> paragraphs = Paragraphs;
                }
                return _hasTextContent;
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// The collection of ParagraphResults for for nested paragraphs.
        /// </summary>
        private ReadOnlyCollection<ParagraphResult> _paragraphs;

        #endregion Private Fields
    }

    /// <summary>
    /// The ParagraphResult for a paragraph which contains Text, Figures, 
    /// and Floaters (no nested paragraphs).
    /// </summary>
    internal sealed class TextParagraphResult : ParagraphResult
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
        /// <param name="paraClient">Object representing a paragraph.</param>
        internal TextParagraphResult(TextParaClient paraClient)
            : base(paraClient)
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Retrieves the height and offset, in pixels, of the edge of 
        /// the object/character represented by position.
        /// </summary>
        /// <param name="position">Position of an object/character.</param>
        /// <returns>
        /// The height, in pixels, of the edge of the object/character 
        /// represented by position.
        /// </returns>
        internal Rect GetRectangleFromTextPosition(ITextPointer position)
        {
            return ((TextParaClient)_paraClient).GetRectangleFromTextPosition(position);
        }

        /// <summary>
        /// Returns tight bounding geometry for the given text range.
        /// </summary>
        /// <param name="startPosition">Start position of the range.</param>
        /// <param name="endPosition">End position of the range.</param>
        /// <param name="paragraphTopSpace">Paragraph's top space.</param>
        /// <param name="visibleRect">Visible clipping rectangle</param>
        /// <returns>Geometry object containing tight bound.</returns>
        internal Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition, double paragraphTopSpace, Rect visibleRect)
        {
            return ((TextParaClient)_paraClient).GetTightBoundingGeometryFromTextPositions(startPosition, endPosition, paragraphTopSpace, visibleRect);
        }

        /// <summary>
        /// Returns true if caret is at unit boundary
        /// </summary>
        /// <param name="position">Position of an object/character.</param>
        internal bool IsAtCaretUnitBoundary(ITextPointer position)
        {
            return ((TextParaClient)_paraClient).IsAtCaretUnitBoundary(position);
        }

        /// <summary>
        /// Retrieves next caret unit position
        /// </summary>
        /// <param name="position">Position of an object/character.</param>
        /// <param name="direction">
        /// LogicalDirection in which we seek the next caret unit position
        /// </param>
        internal ITextPointer GetNextCaretUnitPosition(ITextPointer position, LogicalDirection direction)
        {
            return ((TextParaClient)_paraClient).GetNextCaretUnitPosition(position, direction);
        }

        /// <summary>
        /// Retrieves caret unit position after backspace
        /// </summary>
        /// <param name="position">Position of an object/character.</param>
        internal ITextPointer GetBackspaceCaretUnitPosition(ITextPointer position)
        {
            return ((TextParaClient)_paraClient).GetBackspaceCaretUnitPosition(position);
        }

        /// <summary>
        /// Retrieves collection of GlyphRuns from a range of text.
        /// </summary>
        /// <param name="glyphRuns">
        /// Preallocated collection of GlyphRuns. 
        /// May already contain runs and new runs need to be appended.
        /// </param>
        /// <param name="start">The beginning of the range.</param>
        /// <param name="end">The end of the range.</param>
        internal void GetGlyphRuns(List<GlyphRun> glyphRuns, ITextPointer start, ITextPointer end)
        {
            ((TextParaClient)_paraClient).GetGlyphRuns(glyphRuns, start, end);
        }

        /// <summary>
        /// Returns whether the position is contained in this paragraph.
        /// </summary>
        /// <param name="position">A position to test.</param>
        /// <param name="strict">Apply strict validation rules.</param>
        /// <returns>
        /// True if column contains specified text position. 
        /// Otherwise returns false.
        /// </returns>
        internal override bool Contains(ITextPointer position, bool strict)
        {
            bool contains = base.Contains(position, strict);
            // If the last line of text paragraph does not have content (example: some text<LineBreak /><Para />),
            // position after LineBreak with Forward direction belongs to this text paragraph and indicates
            // the last line of it. 
            // Because of that, always treat position after th last character of text paragraph as a valid
            // position for it. It is safe to do it because it is impossible to get 2 adjacent text paragraphs.
            if (!contains && strict)
            {
                contains = (position.CompareTo(this.EndPosition) == 0);
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
        /// Collection of lines in this paragraph.
        /// </summary>
        internal ReadOnlyCollection<LineResult> Lines
        {
            get
            {
                if (_lines == null)
                {
                    _lines = ((TextParaClient)_paraClient).GetLineResults();
                }
                Invariant.Assert(_lines != null, "Lines collection is null");
                return _lines;
            }
        }

        /// <summary>
        /// Collection of floating UIElements in this paragraph.
        /// </summary>
        internal ReadOnlyCollection<ParagraphResult> Floaters
        {
            get
            {
                if (_floaters == null)
                {
                    _floaters = ((TextParaClient)_paraClient).GetFloaters();
                }
                return _floaters;
            }
        }

        /// <summary>
        /// Collection of figure UIElements in this paragraph.
        /// </summary>
        internal ReadOnlyCollection<ParagraphResult> Figures
        {
            get
            {
                if (_figures == null)
                {
                    _figures = ((TextParaClient)_paraClient).GetFigures();
                }
                return _figures;
            }
        }

        /// <summary>
        /// True if the paragraph has some text content, not only attached objects. A paragraph with no lines has no text content.
        /// A paragraph with just one line containing figures and/or floaters does *not* have text content. We will ignore the end of paragraph character
        /// in this case and not hit test the lines in the paragraph. However, an empty paragraph with no figures and floaters does have text content.
        /// We treat the EOP character as being on a line by itself. This is needed for editing.
        /// </summary>
        internal override bool HasTextContent
        {
            get
            {
                return (Lines.Count > 0 && !ContainsOnlyFloatingElements);
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Properties
        //
        //-------------------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// True if the paragraph contains only figures or floaters with no text.
        /// </summary>
        /// <remarks>
        /// An empty paragraph with no floating elements and just EOP character will return false.
        /// </remarks>
        private bool ContainsOnlyFloatingElements
        {
            get
            {
                bool floatingElementsOnly = false;
                TextParagraph textParagraph = _paraClient.Paragraph as TextParagraph;
                Invariant.Assert(textParagraph != null);
                if (textParagraph.HasFiguresOrFloaters())
                {
                    if (Lines.Count == 0)
                    {
                        // No lines, only attached objects
                        floatingElementsOnly = true;
                    }
                    else if (Lines.Count == 1)
                    {
                        // If this line has no content, paragraph contains only attached objects
                        int lastDcpAttachedObjectBeforeLine = textParagraph.GetLastDcpAttachedObjectBeforeLine(0);
                        if (lastDcpAttachedObjectBeforeLine + textParagraph.ParagraphStartCharacterPosition == textParagraph.ParagraphEndCharacterPosition)
                        {
                            floatingElementsOnly = true;
                        }
                    }
                }
                return floatingElementsOnly;
            }
        }

        #endregion Private Properties

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// The collection of LineResults for for the paragraph's lines.
        /// </summary>
        private ReadOnlyCollection<LineResult> _lines;

        /// <summary>
        /// The collection of floating objects.
        /// </summary>
        private ReadOnlyCollection<ParagraphResult> _floaters;

        /// <summary>
        /// The collection of figures.
        /// </summary>
        private ReadOnlyCollection<ParagraphResult> _figures;

        #endregion Private Fields
    }

    /// <summary>
    /// The ParagraphResult for a table paragraph.
    /// </summary>
    internal sealed class TableParagraphResult : ParagraphResult
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
        /// <param name="paraClient">Object representing a paragraph.</param>
        internal TableParagraphResult(BaseParaClient paraClient)
            : base(paraClient)
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Returns the paragraphs for the appropriate cell, given a point
        /// </summary>
        /// <param name="point">Point to check for cell.</param>
        /// <param name="snapToText">Whether to snap to text</param>
        /// <returns>
        /// Array of paragraph results
        /// </returns>
        internal ReadOnlyCollection<ParagraphResult> GetParagraphsFromPoint(Point point, bool snapToText)
        {
            return ((TableParaClient)_paraClient).GetParagraphsFromPoint(point, snapToText);
        }

        /// <summary>
        /// Returns the paragraphs for the appropriate cell, given a text position
        /// </summary>
        /// <param name="position">Position to check for cell.</param>
        /// <returns>
        /// Array of paragraph results
        /// </returns>
        internal ReadOnlyCollection<ParagraphResult> GetParagraphsFromPosition(ITextPointer position)
        {
            return ((TableParaClient)_paraClient).GetParagraphsFromPosition(position);
        }

        /// <summary>
        /// Returns tight bounding geometry for the given text range.
        /// </summary>
        /// <param name="startPosition">Start position of the range.</param>
        /// <param name="endPosition">End position of the range.</param>
        /// <param name="visibleRect">Visible clipping rectangle.</param>
        /// <returns>Geometry object containing tight bound.</returns>
        internal Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition, Rect visibleRect)
        {
            return ((TableParaClient)_paraClient).GetTightBoundingGeometryFromTextPositions(startPosition, endPosition, visibleRect);
        }

        /// <summary>
        /// Returns the para client for a cell, given a position
        /// </summary>
        /// <param name="position">Position to check for cell.</param>
        /// <returns>
        /// Cell para client
        /// </returns>
        internal CellParaClient GetCellParaClientFromPosition(ITextPointer position)
        {
            return ((TableParaClient)_paraClient).GetCellParaClientFromPosition(position);
        }

        /// <summary>
        /// Returns an appropriate found cell
        /// </summary>
        /// <param name="suggestedX">Suggested X position for cell to find.</param>
        /// <param name="rowGroupIndex">RowGroupIndex to be above.</param>
        /// <param name="rowIndex">RowIndex to be above.</param>
        /// <returns>
        /// Cell Para Client of cell
        /// </returns>
        internal CellParaClient GetCellAbove(double suggestedX, int rowGroupIndex, int rowIndex)
        {
            return ((TableParaClient)_paraClient).GetCellAbove(suggestedX, rowGroupIndex, rowIndex);
        }

        /// <summary>
        /// Returns an appropriate found cell
        /// </summary>
        /// <param name="suggestedX">Suggested X position for cell to find.</param>
        /// <param name="rowGroupIndex">RowGroupIndex to be below.</param>
        /// <param name="rowIndex">RowIndex to be below.</param>
        /// <returns>
        /// Cell Para Client of cell
        /// </returns>
        internal CellParaClient GetCellBelow(double suggestedX, int rowGroupIndex, int rowIndex)
        {
            return ((TableParaClient)_paraClient).GetCellBelow(suggestedX, rowGroupIndex, rowIndex);
        }

        /// <summary>
        /// Returns a cellinfo structure for a given point
        /// </summary>
        /// <param name="point">Point to check for cell.</param>
        /// <returns>
        /// CellInfo class
        /// </returns>
        internal CellInfo GetCellInfoFromPoint(Point point)
        {
            return ((TableParaClient)_paraClient).GetCellInfoFromPoint(point);
        }

        /// <summary>
        /// Returns a rect corresponding to a row end position.
        /// </summary>
        /// <param name="position">Position to check against.</param>
        /// <returns>
        /// Rect, area
        /// </returns>
        internal Rect GetRectangleFromRowEndPosition(ITextPointer position)
        {
            return ((TableParaClient)_paraClient).GetRectangleFromRowEndPosition(position);
        }


        /// <summary>
        /// Collection of paragraphs in this paragraph.
        /// </summary>
        internal ReadOnlyCollection<ParagraphResult> Paragraphs
        {
            get
            {
                if (_paragraphs == null)
                {
                    // While getting children paras, query each one for text content and use the result to set _hasTextContent
                    _paragraphs = ((TableParaClient)_paraClient).GetChildrenParagraphResults(out _hasTextContent);
                }
                Invariant.Assert(_paragraphs != null, "Paragraph collection is empty");
                return _paragraphs;
            }
        }

        /// <summary>
        /// True if the paragraph has text content and not only figures/floaters. For Table paragraph this is true if any of its
        /// children paras has text content
        /// </summary>
        internal override bool HasTextContent
        {
            get
            {
                if (_paragraphs == null)
                {
                    // Getting Paragraphs collection sets the value of _hasTextContent by checking each child for text content
                    ReadOnlyCollection<ParagraphResult> paragraphs = Paragraphs;
                }
                return _hasTextContent;
            }
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// The collection of ParagraphResults for for nested paragraphs.
        /// </summary>
        private ReadOnlyCollection<ParagraphResult> _paragraphs;

        #endregion Private Fields
    }

    /// <summary>
    /// The ParagraphResult for a table row.
    /// </summary>
    internal sealed class RowParagraphResult : ParagraphResult
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
        /// <param name="paraClient">Object representing a paragraph.</param>
        /// <param name="index">Index of row in paragraph.</param>
        /// <param name="rowRect">Rectangle of row - as rendered.</param>
        /// <param name="rowParagraph">Actual paragraph result is bound to.</param>
        internal RowParagraphResult(BaseParaClient paraClient, int index, Rect rowRect, RowParagraph rowParagraph)
            : base(paraClient, rowRect, rowParagraph.Element)
        {
            _index = index;
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Collection of paragraphs in this paragraph.
        /// </summary>
        internal ReadOnlyCollection<ParagraphResult> CellParagraphs
        {
            get
            {
                if (_cells == null)
                {
                    // Check each cell for text content when getting cell paragraph results
                    _cells = ((TableParaClient)_paraClient).GetChildrenParagraphResultsForRow(_index, out _hasTextContent);
                }
                Invariant.Assert(_cells != null, "Paragraph collection is empty");
                return _cells;
            }
        }

        /// <summary>
        /// True if the table row has text content, i.e. if any of the table cells in the row has text content
        /// </summary>
        internal override bool HasTextContent
        {
            get
            {
                if (_cells == null)
                {
                    // Getting cell paragraph results queries each one for text content
                    ReadOnlyCollection<ParagraphResult> cells = CellParagraphs;
                }
                return _hasTextContent;
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// The collection of ParagraphResults for for nested paragraphs.
        /// </summary>
        private ReadOnlyCollection<ParagraphResult> _cells;

        /// <summary>
        /// Index of this row paragraph in tableparaclient's row array.
        /// </summary>
        int _index;

        #endregion Private Fields
    }

    /// <summary>
    /// The ParagraphResult for a subpage paragraph.
    /// </summary>
    internal sealed class SubpageParagraphResult : ParagraphResult
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
        /// <param name="paraClient">Object representing a paragraph.</param>
        internal SubpageParagraphResult(BaseParaClient paraClient)
            : base(paraClient)
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Collection of ColumnResults for each line in the paragraph.
        /// </summary>
        internal ReadOnlyCollection<ColumnResult> Columns
        {
            get
            {
                if (_columns == null)
                {
                    // Check subpage columns for text content
                    _columns = ((SubpageParaClient)_paraClient).GetColumnResults(out _hasTextContent);
                    Invariant.Assert(_columns != null, "Columns collection is null");
                }
                return _columns;
            }
        }

        /// <summary>
        /// True if subpage has text content, i.e. if any of its columns has text content. Checking columns for text content will
        /// recursively check each column's paragraph collections
        /// </summary>
        internal override bool HasTextContent
        {
            get
            {
                if (_columns == null)
                {
                    ReadOnlyCollection<ColumnResult> columns = Columns;
                }
                return _hasTextContent;
            }
        }

        /// <summary>
        /// Collection of ParagraphResults for floating elements
        /// </summary>
        internal ReadOnlyCollection<ParagraphResult> FloatingElements
        {
            get
            {
                if (_floatingElements == null)
                {
                    _floatingElements = ((SubpageParaClient)_paraClient).FloatingElementResults;
                    Invariant.Assert(_floatingElements != null, "Floating elements collection is null");
                }
                return _floatingElements;
            }
        }

        /// <summary>
        /// Offset for contained content - New PTS coordinate system (0, 0)
        /// </summary>
        internal Vector ContentOffset
        {
            get
            {
                MbpInfo mbp = MbpInfo.FromElement(_paraClient.Paragraph.Element, _paraClient.Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);
                return new Vector(LayoutBox.X + TextDpi.FromTextDpi(mbp.BPLeft), LayoutBox.Y + TextDpi.FromTextDpi(mbp.BPTop));
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// The collection of ParagraphResults for for nested paragraphs.
        /// </summary>
        private ReadOnlyCollection<ColumnResult> _columns;

        /// <summary>
        /// The collection of ParagraphResults for floating elements.
        /// </summary>
        private ReadOnlyCollection<ParagraphResult> _floatingElements;

        #endregion Private Fields
    }

    /// <summary>
    /// The ParagraphResult for a figure paragraph.
    /// </summary>
    internal sealed class FigureParagraphResult : ParagraphResult
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
        /// <param name="paraClient">Object representing a paragraph.</param>
        internal FigureParagraphResult(BaseParaClient paraClient)
            : base(paraClient)
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Collection of ColumnResults for each line in the paragraph.
        /// </summary>
        internal ReadOnlyCollection<ColumnResult> Columns
        {
            get
            {
                if (_columns == null)
                {
                    // Check figure's columns for text content
                    _columns = ((FigureParaClient)_paraClient).GetColumnResults(out _hasTextContent);
                    Invariant.Assert(_columns != null, "Columns collection is null");
                }
                return _columns;
            }
        }

        /// <summary>
        /// True if the figure has text content, i.e. if its columns collection has text content.
        /// </summary>
        internal override bool HasTextContent
        {
            get
            {
                if (_columns == null)
                {
                    ReadOnlyCollection<ColumnResult> columns = Columns;
                }
                return _hasTextContent;
            }
        }

        /// <summary>
        /// Collection of ParagraphResults for floating elements
        /// </summary>
        internal ReadOnlyCollection<ParagraphResult> FloatingElements
        {
            get
            {
                if (_floatingElements == null)
                {
                    _floatingElements = ((FigureParaClient)_paraClient).FloatingElementResults;
                    Invariant.Assert(_floatingElements != null, "Floating elements collection is null");
                }
                return _floatingElements;
            }
        }

        /// <summary>
        /// Offset for contained content - New PTS coordinate system (0, 0)
        /// </summary>
        internal Vector ContentOffset
        {
            get
            {
                MbpInfo mbp = MbpInfo.FromElement(_paraClient.Paragraph.Element, _paraClient.Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);
                return new Vector(LayoutBox.X + TextDpi.FromTextDpi(mbp.BPLeft), LayoutBox.Y + TextDpi.FromTextDpi(mbp.BPTop));
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Returns tight bounding geometry for the given text range.
        /// </summary>
        /// <param name="startPosition">Start position of the range.</param>
        /// <param name="endPosition">End position of the range.</param>
        /// <param name="visibleRect">Visible clipping area</param>
        /// <param name="success"> True if range starts in this [ara</param>
        /// <returns>Geometry object containing tight bound.</returns>
        internal Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition, Rect visibleRect, out bool success)
        {
            success = false;
            if (this.Contains(startPosition, true))
            {
                // Selection starts inside this floater and must end inside it
                success = true;
                ITextPointer endPositionInThisPara = endPosition.CompareTo(this.EndPosition) < 0 ? endPosition : this.EndPosition;
                // Pass paragraph results and floating elements to para client so it doesn't have to generate them
                return (((FigureParaClient)_paraClient).GetTightBoundingGeometryFromTextPositions(Columns, FloatingElements, startPosition, endPositionInThisPara, visibleRect));
            }
            return null;
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// The collection of ColumnResults for nested columns.
        /// </summary>
        private ReadOnlyCollection<ColumnResult> _columns;

        /// <summary>
        /// The collection of ParagraphResults for floating elements.
        /// </summary>
        private ReadOnlyCollection<ParagraphResult> _floatingElements;

        #endregion Private Fields
    }

    /// <summary>
    /// The ParagraphResult for a floater base paragraph.
    /// </summary>
    internal abstract class FloaterBaseParagraphResult : ParagraphResult
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
        /// <param name="paraClient">Object representing a paragraph.</param>
        internal FloaterBaseParagraphResult(BaseParaClient paraClient)
            : base(paraClient)
        {
        }

        #endregion Constructors
    }

    /// <summary>
    /// The ParagraphResult for a floater paragraph.
    /// </summary>
    internal sealed class FloaterParagraphResult : FloaterBaseParagraphResult
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
        /// <param name="paraClient">Object representing a paragraph.</param>
        internal FloaterParagraphResult(BaseParaClient paraClient)
            : base(paraClient)
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Collection of ColumnResults for each line in the paragraph.
        /// </summary>
        internal ReadOnlyCollection<ColumnResult> Columns
        {
            get
            {
                if (_columns == null)
                {
                    // Query floater's columns for text content
                    _columns = ((FloaterParaClient)_paraClient).GetColumnResults(out _hasTextContent);
                    Invariant.Assert(_columns != null, "Columns collection is null");
                }
                return _columns;
            }
        }

        /// <summary>
        /// True if the floater has text content, i.e. if its columns collection has text content.
        /// </summary>
        internal override bool HasTextContent
        {
            get
            {
                if (_columns == null)
                {
                    ReadOnlyCollection<ColumnResult> columns = Columns;
                }
                return _hasTextContent;
            }
        }

        /// <summary>
        /// Collection of ParagraphResults for floating elements
        /// </summary>
        internal ReadOnlyCollection<ParagraphResult> FloatingElements
        {
            get
            {
                if (_floatingElements == null)
                {
                    _floatingElements = ((FloaterParaClient)_paraClient).FloatingElementResults;
                    Invariant.Assert(_floatingElements != null, "Floating elements collection is null");
                }
                return _floatingElements;
            }
        }

        /// <summary>
        /// Offset for contained content - New PTS coordinate system (0, 0)
        /// </summary>
        internal Vector ContentOffset
        {
            get
            {
                MbpInfo mbp = MbpInfo.FromElement(_paraClient.Paragraph.Element, _paraClient.Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);
                return new Vector(LayoutBox.X + TextDpi.FromTextDpi(mbp.BPLeft), LayoutBox.Y + TextDpi.FromTextDpi(mbp.BPTop));
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Returns tight bounding geometry for the given text range.
        /// </summary>
        /// <param name="startPosition">Start position of the range.</param>
        /// <param name="endPosition">End position of the range.</param>
        /// <param name="visibleRect">Visible clipping area</param>
        /// <param name="success">True if the range starts in this floater paragraph</param>
        /// <returns>Geometry object containing tight bound.</returns>
        internal Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition, Rect visibleRect, out bool success)
        {
            success = false;
            if (this.Contains(startPosition, true))
            {
                // Selection starts inside this floater and must end inside it
                success = true;
                ITextPointer endPositionInThisPara = endPosition.CompareTo(this.EndPosition) < 0 ? endPosition : this.EndPosition;
                return (((FloaterParaClient)_paraClient).GetTightBoundingGeometryFromTextPositions(Columns, FloatingElements, startPosition, endPositionInThisPara, visibleRect));
            }
            return null;
        }

        #endregion Internal Methods


        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// The collection of ColumnResults for nested columns.
        /// </summary>
        private ReadOnlyCollection<ColumnResult> _columns;

        /// <summary>
        /// The collection of ParagraphResults for floating elements.
        /// </summary>
        private ReadOnlyCollection<ParagraphResult> _floatingElements;

        #endregion Private Fields
    }

    /// <summary>
    /// The ParagraphResult for a UIElement paragraph.
    /// </summary>
    internal sealed class UIElementParagraphResult : FloaterBaseParagraphResult
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
        /// <param name="paraClient">Object representing a paragraph.</param>
        internal UIElementParagraphResult(BaseParaClient paraClient)
            : base(paraClient)
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// True if the BUIC has text content
        /// </summary>
        internal override bool HasTextContent
        {
            get
            {
                // We always treat BlockUIContainer as a 'line' from ContentStart to ContentEnd. So HasTextContent is always true
                return true;
            }
        }

        /// <summary>
        /// Returns tight bounding geometry for the given text range.
        /// </summary>
        internal Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition)
        {
            return (((UIElementParaClient)_paraClient).GetTightBoundingGeometryFromTextPositions(startPosition, endPosition));
        }

        #endregion Internal Properties
    }
}
