// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Internal static class representing a group of methods
//              for list editing
//

namespace System.Windows.Documents
{
    using System;
    using MS.Internal;
    using MS.Internal.Documents;
    using System.Collections.Generic;
    using System.Windows.Documents.Internal; // ColumnResizeAdorner

    /// <summary>
    /// Internal static class representing a group of methods for table editing
    /// </summary>
    internal static class TextRangeEditTables
    {
        #region Internal Methods

        // --------------------------------------------------------------------
        //
        // Internal Methods
        //
        // --------------------------------------------------------------------

        #region Table Selection

        // ....................................................................
        //
        // Table selection
        //
        // ....................................................................

        internal static bool GetColumnRange(ITextRange range, Table table, out int firstColumnIndex, out int lastColumnIndex)
        {
            firstColumnIndex = -1;
            lastColumnIndex = -1;

            if (range == null || !range.IsTableCellRange)
            {
                return false;
            }

            if (!(range.Start is TextPointer))
            {
                // Current implementation is supposed to work only with TextTree implementation.
                // Potentially we could implement this read-only method for Fixed content,
                // but then the code should be changed.
                return false;
            }

            if (table != GetTableFromPosition((TextPointer)range.TextSegments[0].Start))
            {
                return false;
            }

            TableCell firstCell = GetTableCellFromPosition((TextPointer)range.TextSegments[0].Start);
            if (firstCell == null)
            {
                return false;
            }

            // Get a pointer at a position PRECEDING to range.End (because in case of TableCellRange it points immediately after the last selected cell).
            TextPointer lastCellPointer = (TextPointer)range.TextSegments[0].End.GetNextInsertionPosition(LogicalDirection.Backward);
            Invariant.Assert(lastCellPointer != null, "lastCellPointer cannot be null here. Even empty table cells have a potential insertion position.");

            TableCell lastCell = GetTableCellFromPosition(lastCellPointer);
            if (lastCell == null)
            {
                return false;
            }

            if (firstCell.ColumnIndex < 0 || lastCell.ColumnIndex < 0)
            {
                //
                // A -ve column index implies that these cells were not yet laid out.
                // (Table code calculates this property during layout phase 
                // even if they are quite available without any layout - not a good idea).
                // 
                // Note that for table selection initiated with a user gesture this condition can never hold true,
                // because we will always have a valid layout.
                //
                // It can only happen when someone programmatically creates a FlowDocument, 
                // adds some table content to it, creates a selection and tries to serialize it. 
                // This is logically a valid scenario, but it is low priority for us. So for now,
                // we return false here and loose table column information.
                //
                // We cannot efficiently compute a cell's column index at editing level. 
                // (The presence of rowspans makes this an O(n*n) algorithm to walk all rows.)
                // The right fix to enable this scenario would be in table code since they can cache
                // table properties and make them available to us before layout.

                return false;
            }

            firstColumnIndex = firstCell.ColumnIndex;
            lastColumnIndex = lastCell.ColumnIndex + lastCell.ColumnSpan - 1;

            Invariant.Assert(firstColumnIndex <= lastColumnIndex, "expecting: firstColumnIndex <= lastColumnIndex. Actual values: " + firstColumnIndex + " and " + lastColumnIndex);

            return true;
        }

        // Finds a nearest ancestor Table from a given position
        internal static Table GetTableFromPosition(TextPointer position)
        {
            TextElement element = position.Parent as TextElement;
            while (element != null && !(element is Table))
            {
                element = element.Parent as TextElement;
            }

            return element as Table;
        }

        // Finds a nearest ancestor TableRow from a given position
        private static TableRow GetTableRowFromPosition(TextPointer position)
        {
            TextElement element = position.Parent as TextElement;
            while (element != null && !(element is TableRow))
            {
                element = element.Parent as TextElement;
            }

            return element as TableRow;
        }

        // Finds a nearest ancestor TableCell from a given position
        internal static TableCell GetTableCellFromPosition(TextPointer position)
        {
            TextElement element = position.Parent as TextElement;
            while (element != null && !(element is TableCell))
            {
                element = element.Parent as TextElement;
            }

            return element as TableCell;
        }

        // Table Selection Model Overview
        // ------------------------------
        // Table selection is designed as a special state of text selection.
        // It is constructed in general as a collection of TextSegments
        // ordered pairs of TextPositions.
        //
        // Table range can have three substates depending on what structural
        // level of table elements is crossed by a selection from each
        // side of a selection - Cell, Row, Table.
        //
        // TableCellRange - is a range belonging entirely to one Table
        // and containing rectangular collection of Cells.
        // It is represented by a collection of TextSegments
        // each of which starts in a normalized position within leftmost Cell
        // and ends in a normalized position immediately following
        // end of rightmost selected cell (TextSegment.End is the first
        // position NOT belonging to a segment).
        //
        // TableCrossingSegment - is a segment crossing Table boundary.
        // At least one of its ends is located withing a table, while the other one
        // is located either in another table or in a text outside of a table.
        // Such range is represented by a single TextSegment with a table-crossing
        // end aligned to row boundary - if it is starting end of a range,
        // then it is positioned in the very first normalized position
        // of a Row crossed from this side; if it is ending end of a range,
        // then it is positioned in a IsAtRowEnd location of that last row.
        //
        // IsAtRowEnd location is defined as a location immediately before
        // closing tag of a TableRow. This position is considered as normalized
        // (caret can stop in it, even though typing is almost disabled there:
        // only Enter key is allowed to produce new TableRows, and Delete -
        // to delete next TableRow).

        // Returns true iff exactly one of two positions falls within the scope of a Table.
        // Returns false otherwise.
        internal static bool IsTableStructureCrossed(ITextPointer anchorPosition, ITextPointer movingPosition)
        {
            if (!(anchorPosition is TextPointer) || !(movingPosition is TextPointer))
            {
                return false;
            }

            TableCell anchorCell;
            TableCell movingCell;
            TableRow anchorRow;
            TableRow movingRow;
            TableRowGroup anchorRowGroup;
            TableRowGroup movingRowGroup;
            Table anchorTable;
            Table movingTable;

            return IdentifyTableElements((
                TextPointer)anchorPosition, (TextPointer)movingPosition,
                /*includeCellAtMovingPosition:*/true,
                out anchorCell, out movingCell,
                out anchorRow, out movingRow,
                out anchorRowGroup, out movingRowGroup,
                out anchorTable, out movingTable);
        }

        /// <summary>
        /// Predicate telling whether these two position would produce table cell range
        /// if TextRange would be moved to them with TextRange.MoveToPositions method
        /// </summary>
        /// <param name="anchorPosition"></param>
        /// <param name="movingPosition"></param>
        /// <param name="includeCellAtMovingPosition">
        /// <see ref="TextRangeEditTables.BuildTableRange"/>
        /// </param>
        /// <param name="anchorCell">
        /// Null if cell boundary was not crossed on starting edge of a segment,
        /// Outermost Cell whose boundary was crossed by a starting edge of a sgment.
        /// (Note that starting edge is minimal from firstPosition and secondPosition).
        /// </param>
        /// <param name="movingCell">
        /// Null if cell boundary was not crossed on ending edge of a segment,
        /// Outermost Cell whose boundary was crossed by a ending edge of a sgment.
        /// (Note that starting edge is maximal from firstPosition and secondPosition).
        /// </param>
        /// <returns>
        /// True if both ends cross Cell boundaries and those cells belong to the same row.
        /// </returns>
        internal static bool IsTableCellRange(
            TextPointer anchorPosition, TextPointer movingPosition, 
            bool includeCellAtMovingPosition, 
            out TableCell anchorCell, out TableCell movingCell)
        {
            // Find boundary cells and validate parameters
            TableRow anchorRow;
            TableRow movingRow;
            TableRowGroup anchorRowGroup;
            TableRowGroup movingRowGroup;
            Table anchorTable;
            Table movingTable;
            if (!IdentifyTableElements(
                anchorPosition, movingPosition, 
                includeCellAtMovingPosition, 
                out anchorCell, out movingCell, 
                out anchorRow, out movingRow, 
                out anchorRowGroup, out movingRowGroup, 
                out anchorTable, out movingTable))
            {
                return false;
            }

            return anchorCell != null && movingCell != null;
        }

        /// <summary>
        /// From a given two positions  builds a collection of text ranges representinng
        /// contigous text segments covering cells within one row.
        /// </summary>
        /// <param name="anchorPosition">
        /// </param>
        /// <param name="movingPosition">
        /// </param>
        /// <param name="includeCellAtMovingPosition">
        /// True indicates that a cell at a movingPosition must be included
        /// into a selection even when it is at cell start.
        /// False indicates that when a movingPosition is at cell start
        /// and the cell has bigger index than anchor cell, then selection
        /// should not include it - it only indicates cell crossing.
        /// When we build a table range from existing range's Start/End pair
        /// we must use false for this parameter - because the end position
        /// of a table range is not included into it - by construction.
        /// When you use independent position - say, from hit-testing -
        /// then you typically use "true" for this parameter, unnless
        /// you intentially cross cell boundary - as for one cell celection.
        /// </param>
        /// <param name="isTableCellRange">
        /// Returns true if a range is table cell range, containing one or more cell segments.
        /// </param>
        /// <returns>
        /// returns TextSegmentCollection if a pair of positions corresponds to valid table cell range
        /// and some nonempty text segment collection was built.
        /// Otherwise returns null.
        /// </returns>
        internal static List<TextSegment> BuildTableRange(
            TextPointer anchorPosition, TextPointer movingPosition, 
            bool includeCellAtMovingPosition, 
            out bool isTableCellRange)
        {
            // Find boundary cells and validate parameters
            TableCell anchorCell;
            TableCell movingCell;
            TableRow anchorRow;
            TableRow movingRow;
            TableRowGroup anchorRowGroup;
            TableRowGroup movingRowGroup;
            Table anchorTable;
            Table movingTable;
            if (!IdentifyTableElements(
                anchorPosition, movingPosition, 
                includeCellAtMovingPosition, 
                out anchorCell, out movingCell, 
                out anchorRow, out movingRow, 
                out anchorRowGroup, out movingRowGroup, 
                out anchorTable, out movingTable))
            {
                isTableCellRange = false;
                return null;
            }

            if (anchorCell != null && movingCell != null)
            {
                // Two cells found on selection corners and they belong to the same RowGroup -
                // build cell selection
                isTableCellRange = true;
                return BuildCellSelection(anchorCell, movingCell);
            }
            else if (
                anchorRow != null || movingRow != null || 
                anchorRowGroup != null || movingRowGroup != null || 
                anchorTable != null || movingTable != null)
            {
                // Crossed table boundary
                isTableCellRange = false;
                return BuildCrossTableSelection(anchorPosition, movingPosition, anchorRow, movingRow);
            }

            isTableCellRange = false;
            return null;
        }

        private static List<TextSegment> BuildCellSelection(TableCell anchorCell, TableCell movingCell)
        {
            // Identify common RowGroup
            TableRowGroup rowGroup = anchorCell.Row.RowGroup;

            // Identify boundary indices
            int firstRowIndex = Math.Min(anchorCell.Row.Index, movingCell.Row.Index);
            int lastRowIndex = Math.Max(anchorCell.Row.Index + anchorCell.RowSpan - 1, movingCell.Row.Index + movingCell.RowSpan - 1);
            int firstColumnIndex = Math.Min(anchorCell.ColumnIndex, movingCell.ColumnIndex);
            int lastColumnIndex = Math.Max(anchorCell.ColumnIndex + anchorCell.ColumnSpan - 1, movingCell.ColumnIndex + movingCell.ColumnSpan - 1);

            // Build a cell celection
            List<TextSegment> cellRange = new List<TextSegment>(lastRowIndex - firstRowIndex + 1);
            for (int rowIndex = firstRowIndex; rowIndex <= lastRowIndex && rowIndex < rowGroup.Rows.Count; rowIndex++)
            {
                TableCellCollection cells = rowGroup.Rows[rowIndex].Cells;
                TableCell segmentStartCell = null;
                TableCell segmentEndCell = null;

                // Find start cell belonginng to our range
                for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                {
                    TableCell cell = cells[cellIndex];
                    if (firstColumnIndex <= cell.ColumnIndex && cell.ColumnIndex + cell.ColumnSpan - 1 <= lastColumnIndex)
                    {
                        if (segmentStartCell == null)
                        {
                            segmentStartCell = cell;
                        }
                        segmentEndCell = cell;
                    }
                }

                if (segmentStartCell != null && segmentEndCell != null)
                {
                    Invariant.Assert(segmentStartCell.Row == segmentEndCell.Row, "Inconsistent Rows for segmentStartCell and segmentEndCell");
                    Invariant.Assert(segmentStartCell.Index <= segmentEndCell.Index, "Index of segmentStartCell must be <= index of segentEndCell");
                    cellRange.Add(NewNormalizedCellSegment(segmentStartCell, segmentEndCell));
                }
            }

            return cellRange;
        }

        private static List<TextSegment> BuildCrossTableSelection(
            TextPointer anchorPosition, TextPointer movingPosition, 
            TableRow anchorRow, TableRow movingRow)
        {
            List<TextSegment> textSegments = new List<TextSegment>(1);
            if (anchorPosition.CompareTo(movingPosition) < 0)
            {
                textSegments.Add(
                    NewNormalizedTextSegment(
                        anchorRow != null ? anchorRow.ContentStart : anchorPosition, 
                        movingRow != null ? movingRow.ContentEnd : movingPosition));
            }
            else
            {
                textSegments.Add(
                    NewNormalizedTextSegment(
                        movingRow != null ? movingRow.ContentStart : movingPosition, 
                        anchorRow != null ? anchorRow.ContentEnd : anchorPosition));
            }

            return textSegments;
        }

        /// <summary>
        /// Computes new start and end for a table cell range, which are used for rebuilding the range during re-normalization.
        /// </summary>
        internal static void IdentifyValidBoundaries(ITextRange range, out ITextPointer start, out ITextPointer end)
        {
            Invariant.Assert(range._IsTableCellRange, "Range must be in table cell range state");

            List<TextSegment> textSegments = range._TextSegments;
            start = null;
            end = null;

            for (int i = 0; i < textSegments.Count; i++ )
            {
                TextSegment segment = textSegments[i];

                if (segment.Start.CompareTo(segment.End) != 0)
                {
                    if (start == null)
                    {
                        start = segment.Start;
                    }
                    end = segment.End;
                }
            }

            if (start == null)
            {
                start = textSegments[0].Start;
                end = textSegments[textSegments.Count - 1].End;
            }
        }

        // Finds new movingPosition for the selection when it is in TableCellRange state.
        // Returns null if there is no next insertion position in the requested direction.
        internal static TextPointer GetNextTableCellRangeInsertionPosition(TextSelection selection, LogicalDirection direction)
        {
            Invariant.Assert(selection.IsTableCellRange, "TextSelection call this method only if selection is in TableCellRange state");

            TextPointer movingPosition = selection.MovingPosition;

            // Table range could disappear if some content change happened after last range building;
            // so try building again
            TableCell anchorCell;
            TableCell movingCell;
            if (TextRangeEditTables.IsTableCellRange(selection.AnchorPosition, (TextPointer)movingPosition,
                /*includeCellAtMovingPosition:*/false,
                out anchorCell, out movingCell))
            {
                // anchorCell is a corner cell of a table where selection has been started.
                // movingCell is a diagonally oppoosite cell from which we need to find next movingPosition.
                // Note that movingCell is a cell *included* into selection (not the "next position after the last selected cell").

                Invariant.Assert(anchorCell != null && movingCell != null, "anchorCell != null && movingCell != null");
                Invariant.Assert(anchorCell.Row.RowGroup == movingCell.Row.RowGroup, "anchorCell.Row.RowGroup == movingCell.Row.RowGroup");

                if (direction == LogicalDirection.Backward && movingCell == anchorCell)
                {
                    // This is a case when selection returns back to acnhor cell from the next cell
                    movingPosition = anchorCell.ContentEnd.GetInsertionPosition();
                }
                else if (direction == LogicalDirection.Forward && 
                    (movingCell.Row == anchorCell.Row && movingCell.Index + 1 == anchorCell.Index ||
                    anchorCell.Index == 0 && movingCell.Index == movingCell.Row.Cells.Count - 1 && movingCell.Row.Index + 1 == anchorCell.Row.Index))
                {
                    // This is a case when selection returns back to acnhor cell from the previous cell
                    movingPosition = anchorCell.ContentStart.GetInsertionPosition();
                }
                else
                {
                    // Find out what should be new movingCell after selection extension in requested direction
                    TableRow row = movingCell.Row;
                    TableCellCollection cells = row.Cells;
                    TableRowCollection rows = row.RowGroup.Rows;
                    if (direction == LogicalDirection.Forward)
                    {
                        if (movingCell.Index + 1 < cells.Count)
                        {
                            // There is at least one cell in this direction; take it as a movingCell
                            movingCell = cells[movingCell.Index + 1];
                        }
                        else
                        {
                            // Select first cell in the first following nonempty row
                            int rowIndex = row.Index + 1;

                            // Skip empty rows
                            while (rowIndex < rows.Count && rows[rowIndex].Cells.Count == 0)
                            {
                                rowIndex++;
                            }

                            if (rowIndex < rows.Count)
                            {
                                movingCell = rows[rowIndex].Cells[0];
                            }
                            else
                            {
                                movingCell = null;
                            }
                        }
                    }
                    else // extending in LogicalDirection.Backward
                    {
                        if (movingCell.Index > 0)
                        {
                            movingCell = cells[movingCell.Index - 1];
                        }
                        else
                        {
                            // Select the last cell in the first preceding nonempty row
                            int rowIndex = row.Index - 1;

                            // Skip empty rows
                            while (rowIndex >= 0 && rows[rowIndex].Cells.Count == 0)
                            {
                                rowIndex--;
                            }

                            if (rowIndex >= 0)
                            {
                                movingCell = rows[rowIndex].Cells[rows[rowIndex].Cells.Count - 1];
                            }
                            else
                            {
                                movingCell = null;
                            }
                        }
                    }

                    // Calculate movingPosition that would represent this movingCell
                    if (movingCell != null)
                    {
                        if (movingCell.ColumnIndex >= anchorCell.ColumnIndex)
                        {
                            movingPosition = movingCell.ContentEnd.GetInsertionPosition().GetNextInsertionPosition(LogicalDirection.Forward);
                        }
                        else
                        {
                            movingPosition = movingCell.ContentStart.GetInsertionPosition();
                        }
                    }
                    else
                    {
                        // We have reached a table boundary
                        if (direction == LogicalDirection.Forward)
                        {
                            movingPosition = anchorCell.Table.ContentEnd;
                        }
                        else
                        {
                            movingPosition = anchorCell.Table.ContentStart;
                        }
                        movingPosition = movingPosition.GetNextInsertionPosition(direction);
                    }
                }
            }

            return movingPosition;
        }

        // Returns a new movingPosition at the next Table row end.
        // Will return null when end-of-doc is encountered.
        // This method is only called when the selection's anchor position is not
        // within a Table.
        internal static TextPointer GetNextRowEndMovingPosition(TextSelection selection, LogicalDirection direction)
        {
            // This method is called when the selection anchor is outside the scope of
            // a Table and the selection moving position is within the scope of a Table.
            Invariant.Assert(!selection.IsTableCellRange);
            Invariant.Assert(TextPointerBase.IsAtRowEnd(selection.MovingPosition));

            TableRow row = (TableRow)selection.MovingPosition.Parent;

            return (direction == LogicalDirection.Forward) ? row.ContentEnd.GetNextInsertionPosition(LogicalDirection.Forward) :
                                                             row.ContentStart.GetNextInsertionPosition(LogicalDirection.Backward);
        }

        // Returns true iff selection.MovingPosition is within the scope of a TableCell
        // and selection.AnchorPosition is not scoped by the same TableCell.
        internal static bool MovingPositionCrossesCellBoundary(TextSelection selection)
        {
            // We only support table selection in TextContainers.
            Invariant.Assert(((ITextSelection)selection).Start is TextPointer);

            TableCell cell = GetTableCellFromPosition(selection.MovingPosition);

            return (cell == null) ? false : !cell.Contains(selection.AnchorPosition);
        }

        // Returns a new movingPosition at the first insertion position of the next Table row.
        // Will return null when end-of-doc is encountered.
        // This method is only called when the selection's anchor position is not
        // within a Table.
        internal static TextPointer GetNextRowStartMovingPosition(TextSelection selection, LogicalDirection direction)
        {
            // We only support table selection in TextContainers.
            Invariant.Assert(((ITextSelection)selection).Start is TextPointer);
            // This method is called when the selection anchor is outside the scope of
            // a Table and the selection moving position is within the scope of a Table.
            Invariant.Assert(!selection.IsTableCellRange);

            TableCell cell = GetTableCellFromPosition(selection.MovingPosition);
            Invariant.Assert(cell != null);
            TableRow row = cell.Row;

            return (direction == LogicalDirection.Forward) ? row.ContentEnd.GetNextInsertionPosition(LogicalDirection.Forward) :
                                                             row.ContentStart.GetNextInsertionPosition(LogicalDirection.Backward);
        }

        #endregion Table Selection

        // ....................................................................
        //
        // Table Insertion
        //
        // ....................................................................

        #region Table Insertion

        /// <summary>
        /// Inserts a table into a position specified by textRange.
        /// </summary>
        /// <param name="insertionPosition">
        /// Position where table must be inserted.
        /// </param>
        /// <param name="rowCount">
        /// Number of rows generated in a table
        /// </param>
        /// <param name="columnCount">
        /// Number of columnns generated in each row
        /// </param>
        /// <returns>
        /// Returns a table inserted.
        /// </returns>
        internal static Table InsertTable(TextPointer insertionPosition, int rowCount, int columnCount)
        {
            // Inserting tables in lists is currently disabled, so check that we are not in a list
            TextElement ancestor = insertionPosition.Parent as TextElement;
            while (ancestor != null)
            {
                if (ancestor is List || ancestor is Inline && !TextSchema.IsMergeableInline(ancestor.GetType()))
                {
                    // insertionPosition is inside a List.
                    // or it is inside a Hyperlink which is a non-splittable element.
                    // Operation disabled.
                    // Need better error reporting; or consistent code in QueryCommand for disabling it.
                    return null;
                }
                ancestor = ancestor.Parent as TextElement;
            }

            insertionPosition = TextRangeEditTables.EnsureInsertionPosition(insertionPosition);

            Paragraph paragraph = insertionPosition.Paragraph;
            if (paragraph == null)
            {
                return null;
            }

            // Split current paragraph at insertion position
            insertionPosition = insertionPosition.InsertParagraphBreak(); // REVIEW: This will throw exception for hyperlink ancestor.
            paragraph = insertionPosition.Paragraph;
            Invariant.Assert(paragraph != null, "Expecting non-null paragraph at insertionPosition");

            // Build a table with a given number of rows and columns
            Table table = new Table();
            table.CellSpacing = 0;
            TableRowGroup rowGroup = new TableRowGroup();
            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                TableRow row = new TableRow();

                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    TableCell cell = new TableCell(new Paragraph());
                    cell.BorderThickness = GetCellBorder(1, rowIndex, columnIndex, 1, 1, rowCount, columnCount);
                    cell.BorderBrush = System.Windows.Media.Brushes.Black;
                    row.Cells.Add(cell);
                }
                rowGroup.Rows.Add(row);
            }
            table.RowGroups.Add(rowGroup);

            // Insert a table before the second of split paragraphs
            paragraph.SiblingBlocks.InsertBefore(paragraph, table);

            return table;
        }

        private static Thickness GetCellBorder(double thickness, int rowIndex, int columnIndex, int rowSpan, int columnSpan, int rowCount, int columnCount)
        {
            return new Thickness(
                /*left:*/thickness,
                /*top:*/thickness,
                /*right:*/columnIndex + columnSpan < columnCount ? 0 : thickness,
                /*bottom:*/rowIndex + rowSpan < rowCount ? 0 : thickness);
        }

        #endregion Table Insertion

        // ....................................................................
        //
        // Row editing
        //
        // ....................................................................

        #region Row Editing

        /// <summary>
        /// Checks whether the given TextPointer is at row end position, where text insertion is impossible
        /// and returns a following position where text insertion or pasting is valid.
        /// New paragraphs is creeated at the end of TextContainer if necessary.
        /// </summary>
        internal static TextPointer EnsureInsertionPosition(TextPointer position)
        {
            Invariant.Assert(position != null, "null check: position");

            // Normalize the pointer
            position = position.GetInsertionPosition(position.LogicalDirection);

            if (!TextPointerBase.IsAtInsertionPosition(position))
            {
                // There is no insertion positions in the whole document at all.
                // Generate minimally necessary content to ensure at least one insertion position.
                position = CreateInsertionPositionInIncompleteContent(position);
            }
            else
            {
                // Check if position is at one of special structural boundary positions, where we can potentially have an 
                // insertion position and create one.

                if (position.IsAtRowEnd)
                {
                    // Find a next insertion position within the scope of the parent table.
                    Table currentTable = TextRangeEditTables.GetTableFromPosition(position);
                    position = GetAdjustedRowEndPosition(currentTable, position);

                    if (position.CompareTo(currentTable.ElementEnd) == 0)
                    {
                        // The range is at the end of table which is the last block of text container OR
                        // next insertion position crossed table boundary.
                        // In both cases, we want to insert a paragraph after table end and move our insertion position there.
                        position = CreateImplicitParagraph(currentTable.ElementEnd);
                    }
                }
                Invariant.Assert(!position.IsAtRowEnd, "position is not expected to be at RowEnd anymore");

                // Note that this is not an else if, because our next insertion position (if it is within the same table),
                // can fall into one of the following cases. We need to handle it.
                if (TextPointerBase.IsInBlockUIContainer(position))
                {
                    BlockUIContainer blockUIContainer = (BlockUIContainer)position.Parent;
                    bool insertBefore = position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart;
                    position = insertBefore
                        ? CreateImplicitParagraph(blockUIContainer.ElementStart)
                        : CreateImplicitParagraph(blockUIContainer.ElementEnd);

                    // Clean potentialy incomplete content
                    if (blockUIContainer.IsEmpty)
                    {
                        blockUIContainer.RepositionWithContent(null);
                    }
                }
                else if (TextPointerBase.IsBeforeFirstTable(position) || TextPointerBase.IsAtPotentialParagraphPosition(position))
                {
                    position = CreateImplicitParagraph(position);
                }
                else if (TextPointerBase.IsAtPotentialRunPosition(position))
                {
                    position = CreateImplicitRun(position);
                }
            }

            Invariant.Assert(TextSchema.IsInTextContent(position), "position must be in text content now");
            return position;
        }

        // Returns the position where content may actually be inserted corresponding
        // to a Table row end position (where insertion is not legal).
        internal static TextPointer GetAdjustedRowEndPosition(Table currentTable, TextPointer rowEndPosition)
        {
            TextPointer position;

            // Find a next insertion position within the scope of the parent table.
            TextPointer nextInsertionPosition = rowEndPosition;

            while (nextInsertionPosition != null &&
                nextInsertionPosition.IsAtRowEnd && // the following insertion position may be IsAtRowEnd again 
                currentTable == TextRangeEditTables.GetTableFromPosition(nextInsertionPosition))
            {
                nextInsertionPosition = nextInsertionPosition.GetNextInsertionPosition(LogicalDirection.Forward);
            }

            if (nextInsertionPosition != null &&
                currentTable == TextRangeEditTables.GetTableFromPosition(nextInsertionPosition))
            {
                // We found an insertion position within the scope of the table.
                position = nextInsertionPosition;
            }
            else
            {
                // The range is at the end of table which is the last block of text container OR
                // next insertion position crossed table boundary.
                // In both casez, we want to insert a paragraph after table end and move our insertion position there.
                position = currentTable.ElementEnd;
            }

            return position;
        }

        // Helper for EnsureInsertionPosition.
        // Generates minimally necessary content to ensure at least one insertion position.
        private static TextPointer CreateInsertionPositionInIncompleteContent(TextPointer position)
        {
            // Go inside the scoped element to its possible lowest level
            while (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart)
            {
                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }
            while (position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementEnd)
            {
                position = position.GetNextContextPosition(LogicalDirection.Backward);
            }

            DependencyObject parent = position.Parent;
            if (parent != null)
            {
                if (parent is Table)
                {
                    // Creating implicit TableRowGroup
                    TableRowGroup tableRowGroup = new TableRowGroup();
                    tableRowGroup.Reposition(position, position);
                    position = tableRowGroup.ContentStart;
                    parent = position.Parent;
                }

                if (parent is TableRowGroup)
                {
                    // Creating implicit TableRow
                    TableRow tableRow = new TableRow();
                    tableRow.Reposition(position, position);
                    position = tableRow.ContentStart;
                    parent = position.Parent;
                }

                if (parent is TableRow)
                {
                    // Creating implicit TableCell
                    TableCell tableCell = new TableCell();
                    tableCell.Reposition(position, position);
                    position = tableCell.ContentStart;
                    parent = position.Parent;
                }

                if (parent is List)
                {
                    // Creating implicit ListItem
                    ListItem listItem = new ListItem();
                    listItem.Reposition(position, position);
                    position = listItem.ContentStart;
                    parent = position.Parent;
                }

                if (parent is LineBreak || parent is InlineUIContainer)
                {
                    position = ((Inline)parent).ElementStart;
                    parent = position.Parent;
                }
            }

            if (parent == null)
            {
                // This should be an Invariant.Assert
                // This could happen only in case when unparented TextContainer contains only LineBreaks or InlineUIContainers
                throw new InvalidOperationException(SR.Get(SRID.TextSchema_CannotInsertContentInThisPosition));
            }

            TextPointer insertionPosition;
            if (TextSchema.IsValidChild(/*position:*/position, /*childType:*/typeof(Inline)))
            {
                insertionPosition = CreateImplicitRun(position);
            }
            else
            {
                Invariant.Assert(TextSchema.IsValidChild(/*position:*/position, /*childType:*/typeof(Block)), "Expecting valid parent-child relationship");
                insertionPosition = CreateImplicitParagraph(position);
            }

            return insertionPosition;
        }

        // Helper for EnsureInsertionPosition, inserts a Run element at this position.
        private static TextPointer CreateImplicitRun(TextPointer position)
        {
            TextPointer insertionPosition;

            if (position.GetAdjacentElementFromOuterPosition(LogicalDirection.Forward) is Run)
            {
                insertionPosition = position.CreatePointer();
                insertionPosition.MoveToNextContextPosition(LogicalDirection.Forward);
                insertionPosition.Freeze();
            }
            else if (position.GetAdjacentElementFromOuterPosition(LogicalDirection.Backward) is Run)
            {
                insertionPosition = position.CreatePointer();
                insertionPosition.MoveToNextContextPosition(LogicalDirection.Backward);
                insertionPosition.Freeze();
            }
            else
            {
                Run implicitRun = Run.CreateImplicitRun(position.Parent);
                implicitRun.Reposition(position, position);
                insertionPosition = implicitRun.ContentStart.GetFrozenPointer(position.LogicalDirection); // return a position with the same orientation inside a Run
            }

            return insertionPosition;
        }

        // Helper for EnsureInsertionPosition, inserts a Paragraph element with a single Run at this position.
        private static TextPointer CreateImplicitParagraph(TextPointer position)
        {
            TextPointer insertionPosition;

            Paragraph implicitParagraph = new Paragraph();
            implicitParagraph.Reposition(position, position);
            Run implicitRun = Run.CreateImplicitRun(implicitParagraph);
            implicitParagraph.Inlines.Add(implicitRun);
            insertionPosition = implicitRun.ContentStart.GetFrozenPointer(position.LogicalDirection); // return a position with the same orientation inside a Run

            return insertionPosition;
        }

        /// <summary>
        /// Makes sure that the content spanned by the range is totally deleted
        /// including all structural elements and boundaries.
        /// We need this method to be able to make the range empty for any kind of content.
        /// This operation is straightforward for all kinds of ranges expect for
        /// TableCellRange. For cell range it means merging all selected cells
        /// and deleting the text content of resulting cell.
        /// </summary>
        internal static void DeleteContent(TextPointer start, TextPointer end)
        {
            // Order positions, as we do not to distinguish between anchor/moving ends in this operation;
            // but the following code depends on start<=end ordering
            if (start.CompareTo(end) > 0)
            {
                TextPointer whatWasEnd = end;
                end = start;
                start = whatWasEnd;
            }

            // Check whether we cross table structure boundaries
            TableCell startCell;
            TableCell endCell;
            TableRow startRow;
            TableRow endRow;
            TableRowGroup startRowGroup;
            TableRowGroup endRowGroup;
            Table startTable;
            Table endTable;

            // We need to run a loop here because after boundary tables deletions
            // we may encounter following tables, so that start/end range will be
            // again table-crossing.
            while (
                start.CompareTo(end) < 0 
                &&
                IdentifyTableElements(
                    /*anchorPosition:*/start, /*movingPosition:*/end, 
                    /*includeCellAtMovingPosition:*/false, 
                    out startCell, out endCell, 
                    out startRow, out endRow, 
                    out startRowGroup, out endRowGroup, 
                    out startTable, out endTable))
            {
                if (startTable == null && endTable == null || startTable == endTable)
                {
                    bool isTableCellRange;
                    List<TextSegment> textSegments = TextRangeEditTables.BuildTableRange(
                        /*anchorPosition:*/start,
                        /*movingPosition:*/end,
                        /*includeCellAtMovingPosition*/false,
                        out isTableCellRange);

                    if (isTableCellRange && textSegments != null)
                    {
                        // It is cell selection. Create the content of all selected cells
                        for (int i = 0; i < textSegments.Count; i++)
                        {
                            ClearTableCells(textSegments[i]);
                        }
                        // Collapse the range to bypass the folowing paragraph deletion
                        end = start;
                    }
                    else
                    {
                        // Our range is within one table, so we need to delete
                        // crossed row boundaries

                        // Find start row
                        if (startCell != null)
                        {
                            startRow = startCell.Row;
                        }
                        else if (startRow != null)
                        {
                            // do nothing here. we already have a start row for deletion.
                        }
                        else if (startRowGroup != null)
                        {
                            startRow = startRowGroup.Rows[0];
                        }

                        // Find end row
                        if (endCell != null)
                        {
                            endRow = startCell.Row;
                        }
                        else if (endRow != null)
                        {
                            // do nothing here. we already have an end row for deletion.
                        }
                        else if (endRowGroup != null)
                        {
                            endRow = endRowGroup.Rows[endRowGroup.Rows.Count - 1];
                        }

                        Invariant.Assert(startRow != null && endRow != null, "startRow and endRow cannot be null, since our range is within one table");
                        TextRange rowsSegment = new TextRange(startRow.ContentStart, endRow.ContentEnd);
                        TextRangeEditTables.DeleteRows(rowsSegment); // it will take care of rowspans
                    }
                }
                else
                {
                    // Table boundary is crossed on one or both of edges.
                    // So we must delete half(s) of crossed table(s)

                    if (startRow != null)
                    {
                        // Table boundary is crossed on start edge.
                        // So we need to delete all rows from start to the end of this table.

                        // Store position immediately after the first table
                        start = startRow.Table.ElementEnd;

                        // Delete all rows from startRow to the very end of the table
                        TextRange rowsSegment = new TextRange(startRow.ContentStart, startRow.Table.ContentEnd);
                        TextRangeEditTables.DeleteRows(rowsSegment); // it will take care of rowspans
                    }

                    if (endRow != null)
                    {
                        // Table boundary is crossed on end egde.
                        // So we need to delete all rows from start of the table to this endRow.

                        // Store position immediately before the second table
                        end = endRow.Table.ElementStart;

                        // Delete all rows from the beginning of the table to endRow
                        TextRange rowsSegment = new TextRange(endRow.Table.ContentStart, endRow.ContentEnd);
                        TextRangeEditTables.DeleteRows(rowsSegment);
                    }
                }
            }

            // Now that we do not cross table structure, we can apply simple paragraph content deletion.
            // Delete remaining content between tables
            if (start.CompareTo(end) < 0)
            {
                // Note that both start and end are not normalized here,
                // which is important, say, when the block between deleted tables
                // was another Table or something - we want to delete the whole thing, whatever it is
                // and normalizting start/end would cross its boundary.
                TextRangeEdit.DeleteParagraphContent(start, end);
            }
        }

        // Helper function used in DeleteContent for clearing TableCell contents
        private static void ClearTableCells(TextSegment textSegment)
        {
            TableCell cell = GetTableCellFromPosition((TextPointer)textSegment.Start);
            TextPointer end = ((TextPointer)textSegment.End).GetNextInsertionPosition(LogicalDirection.Backward);
            while (cell != null)
            {
                cell.Blocks.Clear();
                cell.Blocks.Add(new Paragraph());

                TextPointer cellEnd = cell.ElementEnd;
                if (cellEnd.CompareTo(end) < 0 &&
                    cellEnd.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart)
                {
                    cell = (TableCell)cellEnd.GetAdjacentElement(LogicalDirection.Forward);
                }
                else
                {
                    cell = null;
                }
            }
        }

        /// <summary>
        /// Inserts table row after or before the row idenified by the currentRowPosition.
        /// The new row inhericts all structural attributes of a current row -
        /// such as number of cells, column spanned, cell properties, inline cell formatting.
        /// Inserted row also respects row spanned intersected with the current row.
        /// New row properties and structure are inherited from the current row.
        /// </summary>
        /// <param name="textRange">
        /// TextRange identifying insertion location.
        /// Insertion will happen before or after this ranbge depending on a sign of rowCount
        /// (negative - means insert before, positive - insert after).
        /// This range remains in the same position after the operation.
        /// </param>
        /// <param name="rowCount">
        /// Absolute value of this parameter defined how many new rows to insert;
        /// its sign specifies before or after the currentRow new rows must be inserted.
        /// </param>
        /// <returns>
        /// Returns text range spanning new rows.
        /// </returns>
        internal static TextRange InsertRows(TextRange textRange, int rowCount)
        {
            // Parameters validation
            Invariant.Assert(textRange != null, "null check: textRange");

            // Identify the current row
            TextPointer currentRowPosition = rowCount > 0 ? textRange.End : textRange.Start;

            TableRow currentRow = GetTableRowFromPosition(currentRowPosition);

            // Check if we have something to do...
            if (currentRow == null || rowCount == 0)
            {
                return new TextRange(textRange.Start, textRange.Start);
            }

            // Correct all spannedCells to include new rows
            TableCell[] spannedCells = currentRow.SpannedCells;
            if (spannedCells != null)
            {
                for (int i = 0; i < spannedCells.Length; i++)
                {
                    TableCell cell = spannedCells[i];
                    cell.ContentStart.TextContainer.SetValue(cell.ContentStart, TableCell.RowSpanProperty, cell.RowSpan + (rowCount > 0 ? rowCount : -rowCount));
                }
            }

            // Calculate insertion index
            TableRowGroup rowGroup = currentRow.RowGroup;
            int insertionIndex = rowGroup.Rows.IndexOf(currentRow);
            if (rowCount > 0)
            {
                insertionIndex++;
            }

            // Prepare for returning new rows range
            TableRow firstInsertedRow = null;
            TableRow lastInsertedRow = null;

            // Loop inserting rows
            while (rowCount != 0)
            {
                // Create new row
                TableRow newRow = CopyRow(currentRow);

                // Store boundary rows for returning new rows range
                if (firstInsertedRow == null)
                {
                    firstInsertedRow = newRow;
                }
                lastInsertedRow = newRow;

                // Copy all cells from current row
                TableCellCollection cells = currentRow.Cells;
                for (int i = 0; i < cells.Count; i++)
                {
                    TableCell currentCell = cells[i];

                    // Check whether this cell is going to be hidden by the currentRow's spanned cell
                    // The row spanned cell should be not copied, because its span will be
                    // increated later
                    if (rowCount < 0 || currentCell.RowSpan == 1)
                    {
                        // Create new cell and transfer all formatting properties to it from a source cell.
                        // All properties except for RowSpan will be transferred.
                        AddCellCopy(newRow, currentCell, -1/*negative means at the endPosition*/, /*copyRowSpan:*/false, /*copyColumnSpan:*/true);
                    }
                }

                // Correct row spans for all previous rows potentially crossing the new row
                // CorrectRowSpans(currentRow, +1);

                // Insert new row to the current table
                rowGroup.Rows.Insert(insertionIndex, newRow);

                // Correct insertion index so that the newRow appeared at the end of inserted group
                if (rowCount > 0)
                {
                    // Note that for inserting before the insertionIndex must remain the same
                    // to refer to the farthest inserted row
                    insertionIndex++;
                }

                // Decrement rowCount
                rowCount -= rowCount > 0 ? 1 : -1;
            }

            // Correct borders for all the table
            CorrectBorders(rowGroup.Rows);

            // Return a position before the endPosition of inserted row
            return rowCount > 0 ? new TextRange(firstInsertedRow.ContentStart, lastInsertedRow.ContentEnd) : new TextRange(lastInsertedRow.ContentStart, firstInsertedRow.ContentEnd);
        }

        /// <summary>
        /// Deletes the current or the next row depending on deletionDirection.
        /// </summary>
        /// <param name="textRange">
        /// TextRange identifying rows to delete.
        /// </param>
        /// <returns>
        /// True if deletion was possible, false if the row was the last or not existed.
        /// </returns>
        internal static bool DeleteRows(TextRange textRange)
        {
            // Parameter validation
            Invariant.Assert(textRange != null, "null check: textRange");

            TableRow startRow = GetTableRowFromPosition(textRange.Start);
            TableRow endRow = GetTableRowFromPosition(textRange.End);

            if (startRow == null || endRow == null || startRow.RowGroup != endRow.RowGroup)
            {
                return false;
            }

            TableRowCollection rows = startRow.RowGroup.Rows;
            int deletedRowsCount = endRow.Index - startRow.Index + 1;

            if (deletedRowsCount == rows.Count)
            {
                // To delete all rows we need to delete the whole table
                Table table= startRow.Table;
                table.RepositionWithContent(null);
            }
            else
            {
                bool lastRowDeleted = endRow.Index == rows.Count - 1;

                // Transfer spanned cells to the next row
                if (!lastRowDeleted)
                {
                    CorrectRowSpansOnDeleteRows(rows[endRow.Index + 1], deletedRowsCount);
                }

                // Delete rows
                rows.RemoveRange(startRow.Index, endRow.Index - startRow.Index + 1);
                Invariant.Assert(rows.Count > 0);

                // Correct bottom borders
                CorrectBorders(rows);
            }

            // Collapse a range to its start position
            textRange.Select(textRange.Start, textRange.Start);

            return true;
        }

        // Corrects borders on cells to avoid double-sizing.
        private static void CorrectBorders(TableRowCollection rows)
        {
            Table table = rows[0].Table;

            if (table.CellSpacing > 0)
            {
                // We only apply "smart borders" algorithm when the table has CellSpacing==0
                return;
            }

            int columnCount = table.ColumnCount;

            int rowCount = rows.Count;
            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                TableRow row = rows[rowIndex];

                TableCellCollection cells = row.Cells;
                for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                {
                    TableCell cell = cells[cellIndex];
                    Thickness border = cell.BorderThickness;

                    double borderRight = cell.ColumnIndex + cell.ColumnSpan < columnCount ? 0.0 : border.Left;
                    double borderBottom = rowIndex + cell.RowSpan < rowCount ? 0.0 : border.Top;

                    if (border.Right != borderRight || border.Bottom != borderBottom)
                    {
                        border.Right = borderRight;
                        border.Bottom = borderBottom;
                        cell.BorderThickness = border;
                    }
                }
            }
        }

        private static void CorrectRowSpansOnDeleteRows(TableRow nextRow, int deletedRowsCount)
        {
            Invariant.Assert(nextRow != null, "null check: nextRow");
            Invariant.Assert(nextRow.Index >= deletedRowsCount, "nextRow.Index is expected to be >= deletedRowsCount");

            TableCellCollection nextRowCells = nextRow.Cells;

            TableCell[] spannedCells = nextRow.SpannedCells;
            if (spannedCells != null)
            {
                int cellIndex = 0;
                for (int i = 0; i < spannedCells.Length; i++)
                {
                    TableCell spannedCell = spannedCells[i];
                    int rowIndex = spannedCell.Row.Index;
                    if (rowIndex < nextRow.Index)
                    {
                        if (rowIndex < nextRow.Index - deletedRowsCount)
                        {
                            Invariant.Assert(spannedCell.RowSpan > deletedRowsCount, "spannedCell.RowSpan is expected to be > deletedRowsCount");
                            spannedCell.ContentStart.TextContainer.SetValue(spannedCell.ContentStart, TableCell.RowSpanProperty, spannedCell.RowSpan - deletedRowsCount);
                        }
                        else
                        {
                            // spanned cell is going to be deleted. Need to re-create it in the next row
                            int columnIndex = spannedCell.ColumnIndex;
                            // Find cell index for insertion
                            while (cellIndex < nextRowCells.Count && nextRowCells[cellIndex].ColumnIndex < columnIndex)
                            {
                                cellIndex++;
                            }
                            TableCell newCell = AddCellCopy(nextRow, spannedCell, cellIndex, /*copyRowSpan:*/false, /*copyColumnSpan:*/true);
                            Invariant.Assert(spannedCell.RowSpan - (nextRow.Index - spannedCell.Row.Index) > 0, "expecting: spannedCell.RowSpan - (nextRow.Index - spannedCell.Row.Index) > 0");
                            newCell.ContentStart.TextContainer.SetValue(newCell.ContentStart, TableCell.RowSpanProperty, spannedCell.RowSpan - (nextRow.Index - spannedCell.Row.Index));
                            cellIndex++;
                        }
                    }
                }
            }
        }

        #endregion Row Editing

        #region Column Editing

        // ....................................................................
        //
        // Column Editing
        //
        // ....................................................................

        /// <summary>
        /// InsertColumn
        /// </summary>
        /// <param name="colIndex">
        /// Index of column to insert after
        /// </param>
        /// <param name="table">
        /// Table to insert column into
        /// </param>
        private static void InsertColumn(int colIndex, Table table)
        {
            for (int iRowGroup = 0; iRowGroup < table.RowGroups.Count; iRowGroup++)
            {
                TableRowGroup rowGroup = table.RowGroups[iRowGroup];

                for (int iRow = 0; iRow < rowGroup.Rows.Count; iRow++)
                {
                    TableRow row = rowGroup.Rows[iRow];

                    if (colIndex == -1) // Insert at front of table
                    {
                        if (row.Cells[0].ColumnIndex == 0)
                        {
                            AddCellCopy(row, row.Cells[0], 0, true, false);
                        }
                    }
                    else
                    {
                        TableCell cellInsertAfter = null;

                        for (int iCell = 0; iCell < row.Cells.Count; iCell++)
                        {
                            TableCell cell = row.Cells[iCell];

                            if (cell.ColumnIndex + cell.ColumnSpan > colIndex)
                            {
                                if (cell.ColumnIndex <= colIndex)
                                {
                                    cellInsertAfter = cell;
                                }

                                break;
                            }
                        }

                        if (cellInsertAfter != null)
                        {
                            if (cellInsertAfter.ColumnSpan == 1)
                            {
                                AddCellCopy(row, cellInsertAfter, row.Cells.IndexOf(cellInsertAfter) + 1, true, true);
                            }
                            else
                            {
                                cellInsertAfter.ContentStart.TextContainer.SetValue(cellInsertAfter.ContentStart, TableCell.ColumnSpanProperty, cellInsertAfter.ColumnSpan + 1);
                            }
                        }
                    }
                }

                // Correct borders
                CorrectBorders(rowGroup.Rows);
            }
        }

        /// <summary>
        /// InsertColumns
        /// </summary>
        /// <param name="textRange">
        /// Range to insert columns over
        /// </param>
        /// <param name="columnCount">
        /// Number of columns to insert
        /// </param>
        internal static TextRange InsertColumns(TextRange textRange, int columnCount)
        {
            int cCols = Math.Abs(columnCount);

            Invariant.Assert(textRange != null, "null check: textRange");

            TableCell startCell;
            TableCell endCell;
            TableRow startRow;
            TableRow endRow;
            TableRowGroup startRowGroup;
            TableRowGroup endRowGroup;
            Table startTable;
            Table endTable;
            if (!IdentifyTableElements(textRange.Start, textRange.End, /*includeCellAtMovingPosition:*/false, out startCell, out endCell, out startRow, out endRow, out startRowGroup, out endRowGroup, out startTable, out endTable))
            {
                if (textRange.IsTableCellRange)
                {
                    return null;
                }

                // Try to get a cell from the current text selection
                startCell = GetTableCellFromPosition(textRange.Start);
                endCell = GetTableCellFromPosition(textRange.End);

                if (startCell == null || startCell != endCell)
                {
                    return null;
                }
            }

            int colIndexInsert = endCell.ColumnIndex + endCell.ColumnSpan - 1;

            for (int iCol = 0; iCol < cCols; iCol++)
            {
                if(columnCount < 0)
                {
                    InsertColumn(colIndexInsert - 1, endCell.Table as Table);
                }
                else
                {
                    InsertColumn(colIndexInsert, endCell.Table as Table);
                }
            }

            return null;
        }

        /// <summary>
        /// DeleteColumn
        /// </summary>
        /// <param name="colIndex">
        /// Index of column to delete
        /// </param>
        /// <param name="table">
        /// Table from which to delete
        /// </param>
        internal static void DeleteColumn(int colIndex, Table table) // Index to delete
        {
            for (int iRowGroup = 0; iRowGroup < table.RowGroups.Count; iRowGroup++)
            {
                TableRowGroup rowGroup = table.RowGroups[iRowGroup];

                for (int iRow = 0; iRow < rowGroup.Rows.Count; iRow++)
                {
                    TableRow row = rowGroup.Rows[iRow];
                    TableCell cellDelete = null;

                    for (int iCell = 0; iCell < row.Cells.Count; iCell++)
                    {
                        TableCell cell = row.Cells[iCell];

                        if (cell.ColumnIndex + cell.ColumnSpan > colIndex)
                        {
                            if (cell.ColumnIndex <= colIndex)
                            {
                                cellDelete = cell;
                            }

                            break;
                        }
                    }

                    if (cellDelete != null)
                    {
                        if (cellDelete.ColumnSpan == 1)
                        {
                            row.Cells.Remove(cellDelete);
                        }
                        else
                        {
                            cellDelete.ContentStart.TextContainer.SetValue(cellDelete.ContentStart, TableCell.ColumnSpanProperty, cellDelete.ColumnSpan - 1);
                        }
                    }
                }

                // Correct borders
                CorrectBorders(rowGroup.Rows);
            }
        }

        /// <summary>
        /// DeleteColumns
        /// </summary>
        /// <param name="textRange">
        /// Text range over which to delete
        /// </param>
        internal static bool DeleteColumns(TextRange textRange)
        {
            Invariant.Assert(textRange != null, "null check: textRange");

            TableCell startCell;
            TableCell endCell;

            if (!IsTableCellRange(
                textRange.Start, textRange.End, 
                /*includeCellAtMovingPosition:*/false, 
                out startCell, out endCell))
            {
                return false;
            }

            int iIndexDelete = startCell.ColumnIndex;
            int cColsDelete = endCell.ColumnIndex - startCell.ColumnIndex + 1;

            if (cColsDelete == 0 || cColsDelete == startCell.Table.ColumnCount)
            {
                return false;
            }

            for (int i = 0; i < cColsDelete; i++)
            {
                DeleteColumn(iIndexDelete, endCell.Table as Table);
            }

            return true;
        }

        // Returns whether a given point is a resize handle for a table column
        internal static bool TableBorderHitTest(ITextView textView, Point pt)
        {
            Table table;
            int columnIndex;
            Rect columnRect;
            double tableAutofitWidth;
            double[] columnWidths;
            return TableBorderHitTest(textView, pt, out table, out columnIndex, out columnRect, out tableAutofitWidth, out columnWidths);
        }

        // A private worker for TableBorderHitTest
        private static bool TableBorderHitTest(
            ITextView textView, Point point, 
            out Table table, 
            out int columnIndex, 
            out Rect columnRect, 
            out double tableAutofitWidth, 
            out double[] columnWidths)
        {
            // Default values for output parameters
            table = null;
            columnIndex = -1;
            columnRect = Rect.Empty;
            tableAutofitWidth = 0.0;
            columnWidths = null;

            // Do this internal for now, change to better location when tests out ok.
            if (!(textView is MS.Internal.Documents.TextDocumentView))
            {
                return false;
            }
            MS.Internal.Documents.TextDocumentView textDocView = (MS.Internal.Documents.TextDocumentView)textView;
            MS.Internal.PtsHost.CellInfo cellInfo = textDocView.GetCellInfoFromPoint(point, null);

            // If there is no cell, exit out
            if (cellInfo == null)
            {
                return false;
            }

            // Translate the point from purlic coordinates relative to UIElement (render scope) 
            // into internal coordinates relative to DocumentPage in TextDocumentView
            // In the current implementation the offset between content and renderScope is zero in RichTextBox,
            // but it may be just random, or may change in the future after eliminating TextFlow.
            // We must double-checlk that this call is really not needed.
            //textDocView.TransformToContent(ref point);

            // if we're outside the table, exit out.
            if (point.Y < cellInfo.TableArea.Top || point.Y > cellInfo.TableArea.Bottom)
            {
                return false;
            }

            // Set the level of sensitivity
            double sensitivity = 1.0;

            TableCell cell = cellInfo.Cell;

            // For all cells except the rightmost check left border
            if (cell.ColumnIndex != 0 && point.X < cellInfo.CellArea.Left + sensitivity)
            {
                columnIndex = cellInfo.Cell.ColumnIndex - 1;
                columnRect = new Rect(cellInfo.CellArea.Left, cellInfo.TableArea.Top, 1, cellInfo.TableArea.Height);
            }

            // Ensure we're not hitting off the end of the table.
            if (cell.ColumnIndex + cell.ColumnSpan <= cell.Table.ColumnCount && 
                point.X > cellInfo.CellArea.Right - sensitivity)
            {
                if(!IsLastCellInRow(cell) || point.X < cellInfo.CellArea.Right + sensitivity)
                {
                    columnIndex = cell.ColumnIndex + cell.ColumnSpan - 1;
                    columnRect = new Rect(cellInfo.CellArea.Right, cellInfo.TableArea.Top, 1, cellInfo.TableArea.Height);
                }
            }

            if (columnIndex == -1)
            {
                return false;
            }

            table = cell.Table;
            tableAutofitWidth = cellInfo.TableAutofitWidth;
            columnWidths = cellInfo.TableColumnWidths;

            return true;
        }

        // returns a column resize info class - information about table column resizing.
        internal static TableColumnResizeInfo StartColumnResize(ITextView textView, Point pt)
        {
            Table table;
            int columnIndex;
            Rect columnRect;
            double tableAutofitWidth;
            double[] columnWidths;

            if (TableBorderHitTest(textView, pt, out table, out columnIndex, out columnRect, out tableAutofitWidth, out columnWidths))
            {
                return new TableColumnResizeInfo(textView, table, columnIndex, columnRect, tableAutofitWidth, columnWidths);
            }

            return null;
        }

        // Ensures a column exists for all actual columns in table, and that all columns are set to fixed size.
        internal static void EnsureTableColumnsAreFixedSize(Table table, double[] columnWidths)
        {
            while (table.Columns.Count < columnWidths.Length)
            {
                table.Columns.Add(new TableColumn());
            }

            for(int columnIndex = 0; columnIndex < table.ColumnCount; columnIndex++)
            {
                table.Columns[columnIndex].Width = new GridLength(columnWidths[columnIndex]);
            }
        }


        // Encapsulates information about how a given column may be resized
        internal class TableColumnResizeInfo
        {
            internal TableColumnResizeInfo(ITextView textView, Table table, int columnIndex, Rect columnRect, double tableAutofitWidth, double[] columnWidths)
            {
                Invariant.Assert(table != null, "null check: table");
                Invariant.Assert(columnIndex >= 0 && columnIndex < table.ColumnCount, "ColumnIndex validity check");

                _table = table;
                _columnIndex = columnIndex;
                _columnRect = columnRect;
                _columnWidths = columnWidths;
                _tableAutofitWidth = tableAutofitWidth;

                _dxl = _columnWidths[columnIndex];

                if(columnIndex == table.ColumnCount - 1)
                {
                    _dxr = _tableAutofitWidth;

                    for(int columnIndexCounter = 0; columnIndexCounter < table.ColumnCount; columnIndexCounter++)
                    {
                        _dxr -= _columnWidths[columnIndexCounter] + table.InternalCellSpacing;
                    }

                    _dxr = Math.Max(_dxr, 0.0);
                }
                else
                {
                    _dxr = _columnWidths[columnIndex + 1];
                }

                // Create adorner for table resizing
                _tableColResizeAdorner = new ColumnResizeAdorner(textView.RenderScope);
                _tableColResizeAdorner.Initialize(
                    textView.RenderScope,
                    _columnRect.Left + _columnRect.Width / 2,
                    _columnRect.Top,
                    _columnRect.Height);
            }

            // Updates a horizontal position of column resizinng adorner
            internal void UpdateAdorner(Point mouseMovePoint)
            {
                if (_tableColResizeAdorner != null)
                {
                    double xPosAdorner = mouseMovePoint.X;

                    xPosAdorner = Math.Max(xPosAdorner, _columnRect.Left - this.LeftDragMax);
                    xPosAdorner = Math.Min(xPosAdorner, _columnRect.Right + this.RightDragMax);

                    _tableColResizeAdorner.Update(xPosAdorner);
                }
            }

            //
            internal void ResizeColumn(Point mousePoint)
            {
                double dx = mousePoint.X - (_columnRect.X + _columnRect.Width / 2);

                dx = Math.Max(dx, - this.LeftDragMax);
                dx = Math.Min(dx, this.RightDragMax);

                int columnIndex = _columnIndex;
                Table table = this.Table;

                Invariant.Assert(table != null, "Table is not expected to be null");
                Invariant.Assert(table.ColumnCount > 0, "ColumnCount is expected to be > 0");

                _columnWidths[columnIndex] += dx;

                if(columnIndex < (table.ColumnCount - 1))
                {
                    _columnWidths[columnIndex + 1] -= dx;
                }

                TextRangeEditTables.EnsureTableColumnsAreFixedSize(table, _columnWidths);

                UndoManager undoManager = table.TextContainer.UndoManager;

                if(undoManager != null && undoManager.IsEnabled)
                {
                    IParentUndoUnit columnResizeUndoUnit = new ColumnResizeUndoUnit(table.ContentStart, columnIndex, _columnWidths, dx);

                    undoManager.Open(columnResizeUndoUnit);
                    undoManager.Close(columnResizeUndoUnit, UndoCloseAction.Commit);
                }

                // Discard table resizing adorner
                this.DisposeAdorner();
            }

            // Must be called to remove table resizing adorned from a render scope
            internal void DisposeAdorner()
            {
                if (_tableColResizeAdorner != null)
                {
                    _tableColResizeAdorner.Uninitialize();
                    _tableColResizeAdorner = null;
                }
            }

            internal double LeftDragMax { get { return (_dxl); } }
            internal double RightDragMax { get { return (_dxr); } }
            internal Table Table { get { return (_table); } }

            private Rect _columnRect;
            private double _tableAutofitWidth;
            private double[] _columnWidths;
            private Table _table;
            private int _columnIndex; // Always dragging to the RIGHT of this index
            private double _dxl; // Max resize drag left
            private double _dxr; // Max resize drag right

            private ColumnResizeAdorner _tableColResizeAdorner;
        }

        #endregion Column Editing

        #region Cell Editing

        // ....................................................................
        //
        // Cell editing
        //
        // ....................................................................

        /// <summary>
        /// Merges several cells horizontally.
        /// </summary>
        /// <param name="textRange">
        /// TextRange contining cells to be merged.
        /// </param>
        /// <returns>
        /// TextRange containing resulted merged cell.
        /// Null in case if textRange cannot be merged.
        /// </returns>
        internal static TextRange MergeCells(TextRange textRange)
        {
            Invariant.Assert(textRange != null, "null check: textRange");

            TableCell startCell;
            TableCell endCell;
            TableRow startRow;
            TableRow endRow;
            TableRowGroup startRowGroup;
            TableRowGroup endRowGroup;
            Table startTable;
            Table endTable;
            if (!IdentifyTableElements(textRange.Start, textRange.End, /*includeCellAtMovingPosition:*/false, out startCell, out endCell, out startRow, out endRow, out startRowGroup, out endRowGroup, out startTable, out endTable))
            {
                return null;
            }

            if (startCell == null || endCell == null)
            {
                return null;
            }

            Invariant.Assert(startCell.Row.RowGroup == endCell.Row.RowGroup, "startCell and endCell must belong to the same RowGroup");
            Invariant.Assert(startCell.Row.Index <= endCell.Row.Index, "startCell.Row.Index must be <= endCell.Row.Index");
            Invariant.Assert(startCell.ColumnIndex <= endCell.ColumnIndex + endCell.ColumnSpan - 1, "startCell.ColumnIndex must be <= an index+span of an endCell");

            // Perform a merge
            TextRange result = MergeCellRange(startCell.Row.RowGroup, //
                startCell.Row.Index, // topRow
                endCell.Row.Index + endCell.RowSpan - 1, // bottomRow
                startCell.ColumnIndex, // leftColumn
                endCell.ColumnIndex + endCell.ColumnSpan - 1); // rightColumn

            if (result != null)
            {
                textRange.Select(textRange.Start, textRange.End);
            }

            return result;
        }

        /// <summary>
        /// Assuming that exactly one cell is selected, the method splits this cell
        /// by creating the given number of cells after it.
        /// Not more than currentCell.ColumnSpan-1 new cells can be created.
        /// </summary>
        /// <param name="textRange"></param>
        /// <param name="splitCountHorizontal"></param>
        /// <param name="splitCountVertical"></param>
        /// <returns>
        /// </returns>
        internal static TextRange SplitCell(TextRange textRange, int splitCountHorizontal, int splitCountVertical)
        {
            Invariant.Assert(textRange != null, "null check: textRange");

            TableCell startCell;
            TableCell endCell;
            TableRow startRow;
            TableRow endRow;
            TableRowGroup startRowGroup;
            TableRowGroup endRowGroup;
            Table startTable;
            Table endTable;
            if (!IdentifyTableElements(textRange.Start, textRange.End, /*includeCellAtMovingPosition:*/false, out startCell, out endCell, out startRow, out endRow, out startRowGroup, out endRowGroup, out startTable, out endTable))
            {
                return null;
            }

            // We require single cell selection for split operation
            if (startCell == null || startCell != endCell)
            {
                return null;
            }

            // Cell must be merged to allow any split
            if (startCell.ColumnSpan == 1 && startCell.RowSpan == 1)
            {
                return null;
            }

            TableRowGroup rowGroup = startCell.Row.RowGroup;

            // Calculate cell index
            TableCellCollection cells = startCell.Row.Cells;
            int startCellIndex = startCell.Index;
            if (splitCountHorizontal > startCell.ColumnSpan - 1)
            {
                splitCountHorizontal = startCell.ColumnSpan - 1;
            }
            Invariant.Assert(splitCountHorizontal >= 0, "expecting: splitCountHorizontal >= 0");

            if (splitCountVertical > startCell.RowSpan - 1)
            {
                splitCountVertical = startCell.RowSpan - 1;
            }
            Invariant.Assert(splitCountVertical >= 0, "expecting; splitCoutVertical >= 0");

            // Perform a split horizontally
            while (splitCountHorizontal > 0)
            {
                AddCellCopy(startCell.Row, startCell, startCellIndex + 1, /*copyRowSpan:*/true, /*copyColumnSpan:*/false);
                startCell.ContentStart.TextContainer.SetValue(startCell.ContentStart, TableCell.ColumnSpanProperty, startCell.ColumnSpan - 1);
                if (startCell.ColumnSpan == 1)
                {
                    startCell.ClearValue(TableCell.ColumnSpanProperty);
                }
                splitCountHorizontal--;
            }

            // Perform a split vertically
            // Provide an implementation for vertical split

            // Correct borders
            CorrectBorders(rowGroup.Rows);

            return new TextRange(startCell.ContentStart, startCell.ContentStart);
        }

        #endregion Cell Editing

        #endregion Internal Methods

        #region Private Methods

        // --------------------------------------------------------------------
        //
        // Private Methods
        //
        // --------------------------------------------------------------------

        #region Table Selection

        // ....................................................................
        //
        // Table selection
        //
        // ....................................................................

        private static TextSegment NewNormalizedTextSegment(TextPointer startPosition, TextPointer endPosition)
        {
            startPosition = startPosition.GetInsertionPosition(LogicalDirection.Forward);
            if (!TextPointerBase.IsAfterLastParagraph(endPosition))
            {
                endPosition = endPosition.GetInsertionPosition(LogicalDirection.Backward);
            }

            if (startPosition.CompareTo(endPosition) < 0)
            {
                return new TextSegment(startPosition, endPosition);
            }
            else
            {
                return new TextSegment(startPosition, startPosition);
            }
        }

        /// <summary>
        /// Builds a segment containing a sequence of contigous cells belonging to one row.
        /// The segment starts in the beginning of a first cell in a sequence
        /// and ends AFTER the end of last cell of a sequence.
        /// Both segment ends are normalized as usual for range segments.
        /// </summary>
        private static TextSegment NewNormalizedCellSegment(TableCell startCell, TableCell endCell)
        {
            Invariant.Assert(startCell.Row == endCell.Row, "startCell and endCell must be in the same Row");
            Invariant.Assert(startCell.Index <= endCell.Index, "insed of a startCell mustbe <= an index of an endCell");

            // As a segment start we take TableCell.Start (normalized - as it is required for ranges)
            TextPointer start = startCell.ContentStart.GetInsertionPosition(LogicalDirection.Forward);

            // As a segment end we take a NEXT insertion position after the end of last selected cell.
            // This next position is either in the beginning of a next cell or at RowEnd
            TextPointer end = endCell.ContentEnd.GetNextInsertionPosition(LogicalDirection.Forward);

            Invariant.Assert(GetTableRowFromPosition(end) == GetTableRowFromPosition(endCell.ContentEnd), "Inconsistent Rows on end");
            // Even if the next cell has incomplete content finding a next insertion position would not go beyond the current row - it will stop at least at the row end.

            Invariant.Assert(start.CompareTo(end) < 0, "The end must be in the beginning of the next cell (or at row end).");
            Invariant.Assert(GetTableRowFromPosition(start) == GetTableRowFromPosition(end), "Inconsistent Rows for start and end");

            return new TextSegment(start, end);
        }

        /// <summary>
        /// From two text positions finds out table elements involved
        /// into building potential table range.
        /// </summary>
        /// <param name="anchorPosition">
        /// Position where selection starts. The cell at this position (if any)
        /// must be included into a range unconditionally.
        /// </param>
        /// <param name="movingPosition">
        /// A position opposite to an anchorPosition.
        /// </param>
        /// <param name="includeCellAtMovingPosition">
        /// <see ref="TextRangeEditTables.BuildTableRange"/>
        /// </param>
        /// <param name="anchorCell">
        /// The cell at anchor position. Returns not null only if a range is not crossing table
        /// boundary. Returns null if the range does not cross any TableCell boundary at all
        /// or if cells crossed belong to a table whose boundary is crossed by a range.
        /// In other words, anchorCell and movingCell are either both nulls or both non-nulls.
        /// </param>
        /// <param name="movingCell">
        /// The cell at the movingPosition.  Returns not null only if a range is not crossing table
        /// boundary. Returns null if the range does not cross any TableCell boundary at all
        /// or if cells crossed belong to a table whose boundary is crossed by a range.
        /// In other words, anchorCell and movingCell are either both nulls or both non-nulls.
        /// </param>
        /// <param name="anchorRow"></param>
        /// <param name="movingRow"></param>
        /// <param name="anchorRowGroup"></param>
        /// <param name="movingRowGroup"></param>
        /// <param name="anchorTable"></param>
        /// <param name="movingTable"></param>
        /// <returns>
        /// True if at least one structural unit was found.
        /// False if no structural units were crossed by either startPosition or endPosition
        /// (up to their commin ancestor element).
        /// </returns>
        private static bool IdentifyTableElements(
            TextPointer anchorPosition, TextPointer movingPosition, 
            bool includeCellAtMovingPosition, 
            out TableCell anchorCell, out TableCell movingCell, 
            out TableRow anchorRow, out TableRow movingRow, 
            out TableRowGroup anchorRowGroup, out TableRowGroup movingRowGroup, 
            out Table anchorTable, out Table movingTable)
        {
            // We need to normalize pointers to make sure that we do not stay above TableCell level
            anchorPosition = anchorPosition.GetInsertionPosition(LogicalDirection.Forward);
            if (!TextPointerBase.IsAfterLastParagraph(movingPosition))
            {
                movingPosition = movingPosition.GetInsertionPosition(LogicalDirection.Backward);
            }

            if (!FindTableElements(
                anchorPosition, movingPosition,
                out anchorCell, out movingCell,
                out anchorRow, out movingRow,
                out anchorRowGroup, out movingRowGroup,
                out anchorTable, out movingTable))
            {
                //Invariant.Assert(
                //    anchorCell == null && movingCell == null &&
                //    anchorRow == null && movingRow == null &&
                //    anchorRowGroup == null && movingRowGroup == null &&
                //    anchorTable == null && movingTable == null);
                return false;
            }

            if (anchorTable != null || movingTable != null)
            {
                // We crossed table boundary, so need to clear anchor/movingCells
                //Invariant.Assert(anchorTable == null || movingTable == null || anchorTable != movingTable);

                anchorCell = null;
                movingCell = null;
            }
            else
            {
                // We did not cross table boundary. Make sure that anchor/movingCells set consistently

                if (anchorCell != null && movingCell != null)
                {
                    // Both ends cross cell boundaries.
                    // The cell at movingPosition may require a correction - excluding it from
                    // a range - if its column index is greater than the anchor's one
                    // and if the movingPosition is at the very beginning of the cell,
                    // and if includeCellAtMovingPosition is set to false.
                    if (!includeCellAtMovingPosition &&
                        movingPosition.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &&
                        movingCell.ColumnIndex > anchorCell.ColumnIndex + anchorCell.ColumnSpan - 1 &&
                        movingCell.Index > 0)
                    {
                        // Moving cell is in forward direction relative to anchor and the position at its very beginning.
                        // This cell should not be included into selection. Take the previous one as the moving cell.
                        movingCell = movingCell.Row.Cells[movingCell.Index - 1];
                    }
                }
                else if (anchorCell != null && movingCell == null && movingPosition.IsAtRowEnd)
                {
                    // Special case when movingPosition is after the very last cell in row
                    TableRow movingCellRow = movingPosition.Parent as TableRow;

                    // Take the last cell from this row
                    movingCell = movingCellRow.Cells[movingCellRow.Cells.Count - 1];
                }
                else
                {
                    // This is not a valid TableCellRange
                    anchorCell = null;
                    movingCell = null;
                }
            }

            // Null anchor/movingCells indicate that the range IsTableCellRange,
            // so they must be non-null only if they both belong to the same table.
            //Invariant.Assert(anchorCell == null && movingCell == null || anchorCell != null && movingCell != null && anchorCell.Table == movingCell.Table);

            return 
                anchorCell != null || movingCell != null || 
                anchorRow != null || movingRow != null || 
                anchorRowGroup != null || movingRowGroup != null || 
                anchorTable != null || movingTable != null;
        }

        private static bool FindTableElements(
            TextPointer anchorPosition, TextPointer movingPosition, 
            out TableCell anchorCell, out TableCell movingCell, 
            out TableRow anchorRow, out TableRow movingRow, 
            out TableRowGroup anchorRowGroup, out TableRowGroup movingRowGroup, 
            out Table anchorTable, out Table movingTable)
        {
            // Most typical check start - fast return for simlest text segment
            if (anchorPosition.Parent == movingPosition.Parent)
            {
                anchorCell = null;
                movingCell = null;
                anchorRow = null;
                movingRow = null;
                anchorRowGroup = null;
                movingRowGroup = null;
                anchorTable = null;
                movingTable = null;
                return false;
            }

            // Find minimal common ancestor
            TextElement commonAncestor = anchorPosition.Parent as TextElement;
            while (commonAncestor != null && !commonAncestor.Contains(movingPosition))
            {
                commonAncestor = commonAncestor.Parent as TextElement;
            }

            FindTableElements(commonAncestor, anchorPosition, out anchorCell, out anchorRow, out anchorRowGroup, out anchorTable);
            FindTableElements(commonAncestor, movingPosition, out movingCell, out movingRow, out movingRowGroup, out movingTable);

            return
                anchorCell != null || movingCell != null ||
                anchorRow != null || movingRow != null ||
                anchorRowGroup != null || movingRowGroup != null ||
                anchorTable != null || movingTable != null;
        }

        private static void FindTableElements(
            TextElement commonAncestor, 
            TextPointer position, 
            out TableCell cell, 
            out TableRow row, 
            out TableRowGroup rowGroup, 
            out Table table)
        {
            cell = null;
            row = null;
            rowGroup = null;
            table = null;

            TextElement element = position.Parent as TextElement;

            // Walk up from anchorElement towards the root
            while (element != commonAncestor)
            {
                Invariant.Assert(element != null, "Not expecting null for element: otherwise it must hit commonAncestor which must be null in this case...");

                // We are crossing element boundary, so store it if it is table element
                if (element is TableCell)
                {
                    cell = (TableCell)element;
                    row = null;
                    rowGroup = null;
                    table = null;
                }
                else if (element is TableRow)
                {
                    row = (TableRow)element;
                    rowGroup = null;
                    table = null;
                }
                else if (element is TableRowGroup)
                {
                    rowGroup = (TableRowGroup)element;
                    table = null;
                }
                else if (element is Table)
                {
                    table = (Table)element;
                }

                // Go to parent level
                element = element.Parent as TextElement;
            }
        }

        #endregion Table Selection

        #region Row Editing

        // ....................................................................
        //
        // Row editing
        //
        // ....................................................................

        // Creates a new Row element and copies all properties from a given row.
        // No cell is created in a new row
        private static TableRow CopyRow(TableRow currentRow)
        {
            Invariant.Assert(currentRow != null, "null check: currentRow");

            TableRow newRow = new TableRow();

            // Copy all properties
            LocalValueEnumerator properties = currentRow.GetLocalValueEnumerator();
            while (properties.MoveNext())
            {
                LocalValueEntry propertyEntry = properties.Current;
                // Copy a property if it is not ReadOnly
                if (!propertyEntry.Property.ReadOnly)
                {
                    newRow.SetValue(propertyEntry.Property, propertyEntry.Value);
                }
            }

            return newRow;
        }

        // Creates a new cell copying all its properties from a currentRow.
        // RowSpan property is not copied; it's set to default value of 1.
        // The new cell as added to a newRow (at the endPosition of its Cells collection).
        private static TableCell AddCellCopy(TableRow newRow, TableCell currentCell, int cellInsertionIndex, bool copyRowSpan, bool copyColumnSpan)
        {
            Invariant.Assert(currentCell != null, "null check: currentCell");

            TableCell newCell = new TableCell();

            // Add the cell to a newRow's cell collection
            // It's good to do it here before inserting inline formatting elements
            // to avoid unnecessary TextContainer creation and content copying.
            if (cellInsertionIndex < 0)
            {
                newRow.Cells.Add(newCell);
            }
            else
            {
                newRow.Cells.Insert(cellInsertionIndex, newCell);
            }

            // Copy all properties
            LocalValueEnumerator properties = currentCell.GetLocalValueEnumerator();
            while (properties.MoveNext())
            {
                LocalValueEntry propertyEntry = properties.Current;
                if (propertyEntry.Property == TableCell.RowSpanProperty && !copyRowSpan ||
                    propertyEntry.Property == TableCell.ColumnSpanProperty && !copyColumnSpan)
                {
                    // Skipping table structuring properties when requested
                    continue;
                }

                // Copy a property if it is not ReadOnly
                if (!propertyEntry.Property.ReadOnly)
                {
                    newCell.SetValue(propertyEntry.Property, propertyEntry.Value);
                }
            }

            // Copy a paragraph for a cell
            if (currentCell.Blocks.FirstBlock != null)
            {
                Paragraph newParagraph = new Paragraph();

                // Transfer all known formatting properties that a locally set on a sourceBlock
                Paragraph sourceParagraph = currentCell.Blocks.FirstBlock as Paragraph;

                if (sourceParagraph != null)
                {
                    DependencyProperty[] inheritableProperties = TextSchema.GetInheritableProperties(typeof(Paragraph));
                    DependencyProperty[] nonInheritableProperties = TextSchema.GetNoninheritableProperties(typeof(Paragraph));

                    for (int i = 0; i < nonInheritableProperties.Length; i++)
                    {
                        DependencyProperty property = nonInheritableProperties[i];
                        object value = sourceParagraph.ReadLocalValue(property);
                        if (value != DependencyProperty.UnsetValue)
                        {
                            newParagraph.SetValue(property, value);
                        }
                    }
                    for (int i = 0; i < inheritableProperties.Length; i++)
                    {
                        DependencyProperty property = inheritableProperties[i];
                        object value = sourceParagraph.ReadLocalValue(property);
                        if (value != DependencyProperty.UnsetValue)
                        {
                            newParagraph.SetValue(property, value);
                        }
                    }
                }

                // Add paragraph to a cell
                newCell.Blocks.Add(newParagraph);
            }

            return newCell;
        }

        #endregion Row Editing

        #region Cell Editing

        // ....................................................................
        //
        // Cell editing
        //
        // ....................................................................


        // Merges a rectangular cell range producing one cell spanned all cells included into the range
        private static TextRange MergeCellRange(TableRowGroup rowGroup, int topRow, int bottomRow, int leftColumn, int rightColumn)
        {
            Invariant.Assert(rowGroup != null, "null check: rowGroup");
            Invariant.Assert(topRow >= 0, "topRow must be >= 0");
            Invariant.Assert(bottomRow >= 0, "bottomRow must be >= 0");
            Invariant.Assert(leftColumn >= 0, "leftColumn must be >= 0");
            Invariant.Assert(rightColumn >= 0, "rightColumn must be >= 0");
            Invariant.Assert(topRow <= bottomRow, "topRow must be <= bottomRow");
            Invariant.Assert(leftColumn <= rightColumn, "leftColumn must be <= rightColumn");

            // Check the ability of merging cell range
            if (!CanMergeCellRange(rowGroup, topRow, bottomRow, leftColumn, rightColumn))
            {
                return null;
            }

            // Do the merge cell range and return the merged range
            return DoMergeCellRange(rowGroup, topRow, bottomRow, leftColumn, rightColumn);
        }

        // Check the ability of merging cell range
        private static bool CanMergeCellRange(TableRowGroup rowGroup, int topRow, int bottomRow, int leftColumn, int rightColumn)
        {
            bool canMergeCell = false;

            // Check if the row range belongs to the group
            if (topRow >= rowGroup.Rows.Count || bottomRow >= rowGroup.Rows.Count)
            {
                return canMergeCell; // Both rows must belong to the rowGroup
            }

            // Check if the table has regular structure - the same number of columns in each row
            if (rowGroup.Rows[topRow].ColumnCount != rowGroup.Rows[bottomRow].ColumnCount)
            {
                return canMergeCell;
            }

            // Check column indices
            if (leftColumn >= rowGroup.Rows[topRow].ColumnCount || rightColumn >= rowGroup.Rows[bottomRow].ColumnCount)
            {
                return canMergeCell;
            }

            // Check that top row is not crossed by upper cells
            TableCell[] spannedCells = rowGroup.Rows[topRow].SpannedCells;
            for (int i = 0; i < spannedCells.Length; i++)
            {
                if (spannedCells[i].Row.Index < topRow)
                {
                    int startColumn = spannedCells[i].ColumnIndex;
                    int endColumn = startColumn + spannedCells[i].ColumnSpan - 1;

                    if (startColumn <= rightColumn && endColumn >= leftColumn)
                    {
                        return canMergeCell;
                    }
                }
            }

            // Run the probing merge loop to check operation applicability
            for (int rowIndex = topRow; rowIndex <= bottomRow; rowIndex++)
            {
                TableCell firstCell;
                TableCell lastCell;
                if (!GetBoundaryCells(rowGroup.Rows[rowIndex], bottomRow, leftColumn, rightColumn, out firstCell, out lastCell))
                {
                    return canMergeCell;
                }
                if (rowIndex == topRow && (firstCell == null || firstCell.ColumnIndex != leftColumn))
                {
                    return canMergeCell;
                }
            }

            canMergeCell = true;

            return canMergeCell;
        }

        // Do the merge cell range
        private static TextRange DoMergeCellRange(TableRowGroup rowGroup, int topRow, int bottomRow, int leftColumn, int rightColumn)
        {
            TextRange result = null;

            // Perform the merge.
            // Run the loop from bottom to top to keep indices in correct condition after deleting emptied rows
            for (int rowIndex = bottomRow; rowIndex >= topRow; rowIndex--)
            {
                TableRow row = rowGroup.Rows[rowIndex];

                // Find cell range in this row crossed by our range
                TableCell firstCell;
                TableCell lastCell;
                GetBoundaryCells(row, bottomRow, leftColumn, rightColumn, out firstCell, out lastCell);

                // Extend spans in the top-left cell
                if (rowIndex == topRow)
                {
                    Invariant.Assert(firstCell != null, "firstCell is not expected to be null");
                    Invariant.Assert(lastCell != null, "lastCell is not expected to be null");
                    Invariant.Assert(firstCell.ColumnIndex == leftColumn, "expecting: firstCell.ColumnIndex == leftColumn");
                    int rowSpan = bottomRow - topRow + 1;
                    int columnSpan = rightColumn - leftColumn + 1;
                    if (rowSpan == 1)
                    {
                        firstCell.ClearValue(TableCell.RowSpanProperty);
                    }
                    else
                    {
                        firstCell.ContentStart.TextContainer.SetValue(firstCell.ContentStart, TableCell.RowSpanProperty, rowSpan);
                    }
                    firstCell.ContentStart.TextContainer.SetValue(firstCell.ContentStart, TableCell.ColumnSpanProperty, columnSpan);
                    result = new TextRange(firstCell.ContentStart, firstCell.ContentStart);

                    if (firstCell != lastCell)
                    {
                        row.Cells.RemoveRange(firstCell.Index + 1, lastCell.Index - firstCell.Index + 1 - 1);
                    }
                }
                else
                {
                    if (firstCell != null)
                    {
                        Invariant.Assert(lastCell != null, "lastCell is not expected to be null");
                        if (firstCell.Index == 0 && lastCell.Index == lastCell.Row.Cells.Count - 1)
                        {
                            // Before deleting row, must go through all other rowspan cells that cross this row
                            // and decrement their rowspan
                            TableCell[] spannedCells = row.SpannedCells;
                            for (int i = 0; i < spannedCells.Length; i++)
                            {
                                TableCell spannedCell = spannedCells[i];
                                if ((spannedCell.ColumnIndex < firstCell.ColumnIndex) ||
                                    (spannedCell.ColumnIndex > lastCell.ColumnIndex))
                                {
                                    int rowSpan = spannedCell.RowSpan - 1;
                                    if (rowSpan == 1)
                                    {
                                        spannedCell.ClearValue(TableCell.RowSpanProperty);
                                    }
                                    else
                                    {
                                        spannedCell.ContentStart.TextContainer.SetValue(spannedCell.ContentStart, TableCell.RowSpanProperty, rowSpan);
                                    }
                                }
                            }

                            // All cells disappear. Delete the whole row
                            row.RowGroup.Rows.Remove(row);

                            // And correct bottomRow index to set RowSpan property of the topRow correctly
                            bottomRow--;
                        }
                        else
                        {
                            row.Cells.RemoveRange(firstCell.Index, lastCell.Index - firstCell.Index + 1);
                        }
                    }
                }
            }

            // Correct borders
            CorrectBorders(rowGroup.Rows);

            return result;
        }

        // Finds first and last cells entirely belonging to the given column range in this row
        private static bool GetBoundaryCells(TableRow row, int bottomRow, int leftColumn, int rightColumn, out TableCell firstCell, out TableCell lastCell)
        {
            firstCell = null;
            lastCell = null;

            bool bottomCrossed = false;

            for (int cellIndex = 0; cellIndex < row.Cells.Count; cellIndex++)
            {
                TableCell cell = row.Cells[cellIndex];

                // Check if the cell crosses bottom row
                int startColumn = cell.ColumnIndex;
                int endColumn = startColumn + cell.ColumnSpan -1;

                if (startColumn <= rightColumn && endColumn >= leftColumn)
                {
                    if (row.Index + cell.RowSpan - 1 > bottomRow)
                    {
                        bottomCrossed = true;
                    }
                    if (firstCell == null)
                    {
                        firstCell = cell;
                    }
                    lastCell = cell;
                }
            }

            return !bottomCrossed && //
                (firstCell == null || firstCell.ColumnIndex >= leftColumn && firstCell.ColumnIndex + firstCell.ColumnSpan - 1 <= rightColumn) && //
                (lastCell == null || lastCell.ColumnIndex >= leftColumn && lastCell.ColumnIndex + lastCell.ColumnSpan - 1 <= rightColumn);
        }

        // Returns whether a given cell is the last one in the row.
        private static bool IsLastCellInRow(TableCell cell)
        {
            return cell.ColumnIndex + cell.ColumnSpan == cell.Table.ColumnCount;
        }

        #endregion Cell Editing

        #endregion Private Methods
    }
}
