// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: TableParaClient holds display related information.
//


using MS.Internal;
using MS.Internal.PtsTable;
using MS.Internal.Text;
using MS.Internal.Documents;
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// Table display data
    /// </summary>
    internal sealed class TableParaClient : BaseParaClient
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="paragraph">Owner of the para client</param>
        internal TableParaClient(TableParagraph paragraph) : base(paragraph)
        {
        }

        #endregion

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Arrange table paragraph by calling Arrange on all cells
        /// </summary>
        protected override void OnArrange()
        {
            Debug.Assert(   Table != null
                        &&  CalculatedColumns != null  );

            base.OnArrange();

            _columnRect = Paragraph.StructuralCache.CurrentArrangeContext.ColumnRect;

            CalculatedColumn[] calculatedColumns = CalculatedColumns;
            PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc;
            PTS.FSKUPDATE fskupdTable;
            PTS.FSRECT rect;

            if (    //  table has no rows thus no cells to arrange
                    !QueryTableDetails(out arrayTableRowDesc, out fskupdTable, out rect)
                    //  no changes requiring arrange in the table
                ||  fskupdTable == PTS.FSKUPDATE.fskupdNoChange
                ||  fskupdTable == PTS.FSKUPDATE.fskupdShifted  )
            {
                return;
            }

            // Rect may have moved from collision with figure or floater.
            _rect = rect;

            // Update chunk information
            UpdateChunkInfo(arrayTableRowDesc);

            MbpInfo mbp = MbpInfo.FromElement(TableParagraph.Element, TableParagraph.StructuralCache.TextFormatterHost.PixelsPerDip);

            if(ParentFlowDirection != PageFlowDirection)
            {
                PTS.FSRECT pageRect = _pageContext.PageRect;

                PTS.Validate(PTS.FsTransformRectangle(PTS.FlowDirectionToFswdir(ParentFlowDirection), ref pageRect, ref _rect, PTS.FlowDirectionToFswdir(PageFlowDirection), out _rect));
                mbp.MirrorMargin();
            }

            _rect.u += mbp.MarginLeft;
            _rect.du -= mbp.MarginLeft + mbp.MarginRight;

            int vrRowTop = GetTableOffsetFirstRowTop() + TextDpi.ToTextDpi(Table.InternalCellSpacing) / 2;

            for (int iR = 0; iR < arrayTableRowDesc.Length; ++iR)
            {
                PTS.FSKUPDATE fskupdRow;
                PTS.FSKUPDATE[] arrayUpdate;
                IntPtr[] arrayFsCell;
                PTS.FSTABLEKCELLMERGE[] arrayTableCellMerge;

                fskupdRow = (arrayTableRowDesc[iR].fsupdinf.fskupd != PTS.FSKUPDATE.fskupdInherited)
                    ? arrayTableRowDesc[iR].fsupdinf.fskupd
                    : fskupdTable;

                if (fskupdRow == PTS.FSKUPDATE.fskupdNoChange)
                {
                    //  no changes requiring arrange in the row
                    vrRowTop += arrayTableRowDesc[iR].u.dvrRow;
                    continue;
                }

                QueryRowDetails(
                    arrayTableRowDesc[iR].pfstablerow,
                    out arrayFsCell,
                    out arrayUpdate,
                    out arrayTableCellMerge);

                for (int iC = 0; iC < arrayFsCell.Length; ++iC)
                {
                    if (arrayFsCell[iC] == IntPtr.Zero)
                    {
                        //  paginated case - cell may be null
                        continue;
                    }

                    if (    iR != 0 //  paginated case - row spanned cells appearing in first row on the page must be accounted
                        &&  (   arrayTableCellMerge[iC] == PTS.FSTABLEKCELLMERGE.fskcellmergeMiddle
                            ||  arrayTableCellMerge[iC] == PTS.FSTABLEKCELLMERGE.fskcellmergeLast  )   )
                    {
                        // this cell has been accounted
                        continue;
                    }

                    CellParaClient cellParaClient = (CellParaClient)(PtsContext.HandleToObject(arrayFsCell[iC]));

                    double urCellOffset = calculatedColumns[cellParaClient.ColumnIndex].UrOffset;

                    cellParaClient.Arrange(TextDpi.ToTextDpi(urCellOffset), vrRowTop, _rect, ThisFlowDirection, _pageContext);
}

                vrRowTop += arrayTableRowDesc[iR].u.dvrRow;

                if (iR == 0 && IsFirstChunk)
                {
                    // Remove extra spacing we reported to PTS in RowProperties for top row in table
                    vrRowTop -= mbp.BPTop;
                }
            }
        }


        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Validate table visual
        /// </summary>
        /// <param name="fskupdInherited">PTS update info</param>
        internal override void ValidateVisual(PTS.FSKUPDATE fskupdInherited)
        {
            Invariant.Assert( fskupdInherited != PTS.FSKUPDATE.fskupdInherited );
            Invariant.Assert( TableParagraph.Table != null && CalculatedColumns != null );

            PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc;
            PTS.FSKUPDATE fskupdTable;
            PTS.FSRECT rect;
            Table table = TableParagraph.Table;

            Visual.Clip = new RectangleGeometry(_columnRect.FromTextDpi());

            if (!QueryTableDetails(out arrayTableRowDesc, out fskupdTable, out rect))
            {
                //  table has no rows thus no cell to validate
                _visual.Children.Clear();
                return;
            }

            MbpInfo mbpInfo = MbpInfo.FromElement(TableParagraph.Element, TableParagraph.StructuralCache.TextFormatterHost.PixelsPerDip);
            if (ThisFlowDirection != PageFlowDirection)
            {
                mbpInfo.MirrorBP();
            }

            if (fskupdTable == PTS.FSKUPDATE.fskupdInherited)
            {
                fskupdTable = fskupdInherited;
            }

            if (fskupdTable == PTS.FSKUPDATE.fskupdNoChange)
            {
                //  no need to arrange because nothing changed
                return;
            }

            if (fskupdTable == PTS.FSKUPDATE.fskupdShifted)
            {
                fskupdTable = PTS.FSKUPDATE.fskupdNew;
            }

            VisualCollection rowVisualsCollection = _visual.Children;
            if (fskupdTable == PTS.FSKUPDATE.fskupdNew)
            {
                rowVisualsCollection.Clear();
            }

            // Draw border and background info.
            Brush backgroundBrush = (Brush)Paragraph.Element.GetValue(TextElement.BackgroundProperty);

            using (DrawingContext dc = _visual.RenderOpen())
            {
                Rect tableContentRect = GetTableContentRect(mbpInfo).FromTextDpi();

                _visual.DrawBackgroundAndBorderIntoContext(dc, backgroundBrush, mbpInfo.BorderBrush, mbpInfo.Border, _rect.FromTextDpi(), IsFirstChunk, IsLastChunk);

                DrawColumnBackgrounds(dc, tableContentRect);
                DrawRowGroupBackgrounds(dc, arrayTableRowDesc, tableContentRect, mbpInfo);
                DrawRowBackgrounds(dc, arrayTableRowDesc, tableContentRect, mbpInfo);
            }

            TableRow rowPrevious = null;

            for (int iR = 0; iR < arrayTableRowDesc.Length; ++iR)
            {
                PTS.FSKUPDATE fskupdRow;
                RowParagraph rowParagraph;
                TableRow row;

                fskupdRow = (arrayTableRowDesc[iR].fsupdinf.fskupd != PTS.FSKUPDATE.fskupdInherited)
                    ? arrayTableRowDesc[iR].fsupdinf.fskupd
                    : fskupdTable;


                rowParagraph = (RowParagraph)(PtsContext.HandleToObject(arrayTableRowDesc[iR].fsnmRow));
                row = rowParagraph.Row;

                //
                //  STEP 1 SYNCHRONIZATION
                //  ---------------------------------------------------------
                //  synchronize rowVisualCollection.
                //  for newly created rows visual is inserted;
                //  otherwise for removed rows (if any) corresponding visuals are removed
                //
                if (fskupdRow == PTS.FSKUPDATE.fskupdNew)
                {
                    RowVisual rowVisual = new RowVisual(row);
                    rowVisualsCollection.Insert(iR, rowVisual);
                }
                else
                {
                    SynchronizeRowVisualsCollection(rowVisualsCollection, iR, row);
                }

                Invariant.Assert(((RowVisual)rowVisualsCollection[iR]).Row == row);

                //
                //  STEP 2 CELL VISUALS VALIDATION
                //  ---------------------------------------------------------
                //  for new or changed rows go inside and validate cells
                //
                if (    fskupdRow == PTS.FSKUPDATE.fskupdNew
                    ||  fskupdRow == PTS.FSKUPDATE.fskupdChangeInside   )
                {
                    // paginated case - if first row of a given rowgroup for this para client has foreign cells, they need to
                    // be rendered regardless of merge state
                    if(rowParagraph.Row.HasForeignCells && (rowPrevious == null || rowPrevious.RowGroup != row.RowGroup))
                    {
                        ValidateRowVisualComplex(
                            (RowVisual)(rowVisualsCollection[iR]),
                            arrayTableRowDesc[iR].pfstablerow,
                            CalculatedColumns.Length,
                            fskupdRow,
                            CalculatedColumns);
                    }
                    else
                    {
                        ValidateRowVisualSimple(
                            (RowVisual)(rowVisualsCollection[iR]),
                            arrayTableRowDesc[iR].pfstablerow,
                            fskupdRow,
                            CalculatedColumns);
                    }
                }

                rowPrevious = row;
            }

            //
            //  STEP 4 CHECK IF ROWS WERE DELETED FROM THE END OF THE TABLE
            //  ---------------------------------------------------------
            //
            if (rowVisualsCollection.Count > arrayTableRowDesc.Length)
            {
                rowVisualsCollection.RemoveRange(
                    arrayTableRowDesc.Length,
                    rowVisualsCollection.Count - arrayTableRowDesc.Length);
            }
        }

        /// ------------------
        /// Updates viewport
        /// --------------------
        internal override void UpdateViewport(ref PTS.FSRECT viewport)
        {
            Debug.Assert(   TableParagraph.Table != null
                        &&  CalculatedColumns != null  );

            PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc;
            PTS.FSKUPDATE fskupdTable;
            PTS.FSRECT rectTable;

            if (QueryTableDetails(out arrayTableRowDesc, out fskupdTable, out rectTable))
            {
                for (int iR = 0; iR < arrayTableRowDesc.Length; ++iR)
                {
                    PTS.FSKUPDATE[] arrayUpdate;
                    IntPtr[] arrayFsCell;
                    PTS.FSTABLEKCELLMERGE[] arrayTableCellMerge;

                    QueryRowDetails(
                        arrayTableRowDesc[iR].pfstablerow,
                        out arrayFsCell,
                        out arrayUpdate,
                        out arrayTableCellMerge);

                    for (int iC = 0; iC < arrayFsCell.Length; ++iC)
                    {
                        if (arrayFsCell[iC] == IntPtr.Zero)
                        {
                            //  paginated case - cell may be null
                            continue;
                        }

                        CellParaClient cellParaClient = (CellParaClient)(PtsContext.HandleToObject(arrayFsCell[iC]));
                        cellParaClient.UpdateViewport(ref viewport);
                    }
                }
            }
        }

        /// <summary>
        /// Performs hit testing.
        /// </summary>
        /// <param name="pt">Point of interest.</param>
        /// <returns>Element if any.</returns>
        internal override IInputElement InputHitTest(PTS.FSPOINT pt)
        {
            Debug.Assert(   TableParagraph.Table != null
                        &&  CalculatedColumns != null  );

            IInputElement element = null;
            PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc;
            PTS.FSKUPDATE fskupdTable;
            PTS.FSRECT rectTable;

            if (QueryTableDetails(out arrayTableRowDesc, out fskupdTable, out rectTable))
            {
                // Start vrRowTop from 'true' v of table
                int vrRowTop = GetTableOffsetFirstRowTop() + rectTable.v;
                for (int iR = 0; iR < arrayTableRowDesc.Length; ++iR)
                {
                    if (pt.v >= vrRowTop && pt.v <= (vrRowTop + arrayTableRowDesc[iR].u.dvrRow))
                    {
                        PTS.FSKUPDATE[] arrayUpdate;
                        IntPtr[] arrayFsCell;
                        PTS.FSTABLEKCELLMERGE[] arrayTableCellMerge;

                        QueryRowDetails(
                            arrayTableRowDesc[iR].pfstablerow,
                            out arrayFsCell,
                            out arrayUpdate,
                            out arrayTableCellMerge);

                        for (int iC = 0; iC < arrayFsCell.Length; ++iC)
                        {
                            if (arrayFsCell[iC] == IntPtr.Zero)
                            {
                                //  paginated case - cell may be null
                                continue;
                            }

                            CellParaClient cellParaClient = (CellParaClient)(PtsContext.HandleToObject(arrayFsCell[iC]));
                            PTS.FSRECT rect = cellParaClient.Rect;

                            if (cellParaClient.Rect.Contains(pt))
                            {
                                element = cellParaClient.InputHitTest(pt);
								break;
                            }
                        }
                        break;
                    }
                    vrRowTop += arrayTableRowDesc[iR].u.dvrRow;
                }
            }

            if(element == null && _rect.Contains(pt))
            {
                element = TableParagraph.Table;
            }

            return (element);
        }

        /// <summary>
        /// Returns ArrayList of rectangles for element if found.
        /// </summary>
        /// <param name="e">
        /// ContentElement for which rectangles are to be found.
        /// </param>
        /// <param name="start">
        /// int representing start offset of e
        /// </param>
        /// <param name="length">
        /// int representing number of positions occupied by element.
        /// </param>
        internal override List<Rect> GetRectangles(ContentElement e, int start, int length)
        {
            Debug.Assert(TableParagraph.Table != null
                        && CalculatedColumns != null);
            List<Rect> rectangles = new List<Rect>();

            if (TableParagraph.Table == e)
            {
                // We have found the element. Return rectangles for this paragraph.
                GetRectanglesForParagraphElement(out rectangles);
            }
            else
            {
                // Search subpage for table cells
                PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc;
                PTS.FSKUPDATE fskupdTable;
                PTS.FSRECT rectTable;

                rectangles = new List<Rect>();
                if (QueryTableDetails(out arrayTableRowDesc, out fskupdTable, out rectTable))
                {
                    for (int iR = 0; iR < arrayTableRowDesc.Length; ++iR)
                    {
                        PTS.FSKUPDATE[] arrayUpdate;
                        IntPtr[] arrayFsCell;
                        PTS.FSTABLEKCELLMERGE[] arrayTableCellMerge;
                        QueryRowDetails(
                                arrayTableRowDesc[iR].pfstablerow,
                                out arrayFsCell,
                                out arrayUpdate,
                                out arrayTableCellMerge);
                        for (int iC = 0; iC < arrayFsCell.Length; ++iC)
                        {
                            if (arrayFsCell[iC] == IntPtr.Zero)
                            {
                                //  paginated case - cell may be null
                                continue;
                            }

                            CellParaClient cellParaClient = (CellParaClient)(PtsContext.HandleToObject(arrayFsCell[iC]));
                            if (start < cellParaClient.Paragraph.ParagraphEndCharacterPosition)
                            {
                                // Element start is within cell boundary
                                rectangles = cellParaClient.GetRectangles(e, start, length);
                                Invariant.Assert(rectangles != null);
                                if (rectangles.Count != 0)
                                {
                                    break;
                                }
                            }
                        }
                        if (rectangles.Count != 0)
                        {
                            break;
                        }
                    }
                }
            }

            Invariant.Assert(rectangles != null);
            return rectangles;
        }

        /// <summary>
        /// Paragraph results - verification
        /// </summary>
        internal override ParagraphResult CreateParagraphResult()
        {
            return new TableParagraphResult(this);
        }

        /// <summary>
        /// Return TextContentRange for the content of the paragraph.
        /// </summary>
        internal override TextContentRange GetTextContentRange()
        {
            TextContentRange range = null;

            // Search subpage for table cells
            PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc;
            PTS.FSKUPDATE fskupdTable;
            PTS.FSRECT rectTable;
            if (QueryTableDetails(out arrayTableRowDesc, out fskupdTable, out rectTable))
            {
                range = new TextContentRange();
                UpdateChunkInfo(arrayTableRowDesc);
                TextElement elementOwner = this.Paragraph.Element as TextElement;

                // Add range for Table leading edge, if necessary
                if (_isFirstChunk)
                    range.Merge(TextContainerHelper.GetTextContentRangeForTextElementEdge(
                        elementOwner, ElementEdge.BeforeStart));

                for (int iR = 0; iR < arrayTableRowDesc.Length; ++iR)
                {
                    PTS.FSTABLEROWDETAILS tableRowDetails;
                    TableRow row = ((RowParagraph)(PtsContext.HandleToObject(arrayTableRowDesc[iR].fsnmRow))).Row;
                    PTS.Validate(PTS.FsQueryTableObjRowDetails(
                        PtsContext.Context,
                        arrayTableRowDesc[iR].pfstablerow,
                        out tableRowDetails));

                    if (tableRowDetails.fskboundaryAbove != PTS.FSKTABLEROWBOUNDARY.fsktablerowboundaryBreak)
                    {
                        // Add range for TableRowGroup leading edge, if necessary
                        if (row.Index == 0)
                            range.Merge(TextContainerHelper.GetTextContentRangeForTextElementEdge(
                                row.RowGroup, ElementEdge.BeforeStart));

                        range.Merge(TextContainerHelper.GetTextContentRangeForTextElementEdge(
                            row, ElementEdge.BeforeStart));
                    }

                    PTS.FSKUPDATE[] arrayUpdate;
                    IntPtr[] arrayFsCell;
                    PTS.FSTABLEKCELLMERGE[] arrayTableCellMerge;
                    QueryRowDetails(
                            arrayTableRowDesc[iR].pfstablerow,
                            out arrayFsCell,
                            out arrayUpdate,
                            out arrayTableCellMerge);

                    for (int iC = 0; iC < arrayFsCell.Length; ++iC)
                    {
                        if (arrayFsCell[iC] == IntPtr.Zero)
                        {
                            //  paginated case - cell may be null
                            continue;
                        }

                        CellParaClient cellParaClient = (CellParaClient)(PtsContext.HandleToObject(arrayFsCell[iC]));
                        range.Merge(cellParaClient.GetTextContentRange());
                    }

                    if (tableRowDetails.fskboundaryBelow != PTS.FSKTABLEROWBOUNDARY.fsktablerowboundaryBreak)
                    {
                        range.Merge(TextContainerHelper.GetTextContentRangeForTextElementEdge(
                            row, ElementEdge.AfterEnd));

                        // Add range for TableRowGroup trailing edge, if necessary
                        if (row.Index == row.RowGroup.Rows.Count - 1)
                            range.Merge(TextContainerHelper.GetTextContentRangeForTextElementEdge(
                                row.RowGroup, ElementEdge.AfterEnd));
                    }
                }

                // Add range for Table trailing edge, if necessary
                if (_isLastChunk)
                    range.Merge(TextContainerHelper.GetTextContentRangeForTextElementEdge(
                        elementOwner, ElementEdge.AfterEnd));
            }

            if (range == null)
            {
                // if table has no rows or no cells
                range = TextContainerHelper.GetTextContentRangeForTextElement(TableParagraph.Table);
            }

            return range;
        }

        /// <summary>
        /// Returns the paragraphs for the appropriate cell, given a point
        /// </summary>
        /// <param name="point">Point to check for cell.</param>
        /// <param name="snapToText">Whether to snap to text</param>
        /// <returns>
        /// CellParaClient found
        /// </returns>
        internal CellParaClient GetCellParaClientFromPoint(Point point, bool snapToText)
        {
            Debug.Assert(   TableParagraph.Table != null
                        &&  CalculatedColumns != null  );

            PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc;
            PTS.FSKUPDATE fskupdTable;
            PTS.FSRECT rectTable;
            int u = TextDpi.ToTextDpi(point.X);
            int v = TextDpi.ToTextDpi(point.Y);

            CellParaClient cpcFound = null;
            CellParaClient cpcClosest = null;
            int iClosestDistance = int.MaxValue;

            if (QueryTableDetails(out arrayTableRowDesc, out fskupdTable, out rectTable))
            {
                for (int iR = 0; iR < arrayTableRowDesc.Length && cpcFound == null; ++iR)
                {
                    PTS.FSKUPDATE[] arrayUpdate;
                    IntPtr[] arrayFsCell;
                    PTS.FSTABLEKCELLMERGE[] arrayTableCellMerge;

                    QueryRowDetails(
                        arrayTableRowDesc[iR].pfstablerow,
                        out arrayFsCell,
                        out arrayUpdate,
                        out arrayTableCellMerge);

                    for (int iC = 0; iC < arrayFsCell.Length && cpcFound == null; ++iC)
                    {
                        if (arrayFsCell[iC] == IntPtr.Zero)
                        {
                            //  paginated case - cell may be null
                            continue;
                        }

                        CellParaClient cpc = (CellParaClient)(PtsContext.HandleToObject(arrayFsCell[iC]));
                        PTS.FSRECT rect = cpc.Rect;

                        if (u >= rect.u && u <= (rect.u + rect.du) &&
                            v >= rect.v && v <= (rect.v + rect.dv))
                        {
                            cpcFound = cpc;
                        }
                        else if(snapToText)
                        {
                            int du = Math.Min(Math.Abs(rect.u - u), Math.Abs(rect.u + rect.du - u));
                            int dv = Math.Min(Math.Abs(rect.v - v), Math.Abs(rect.v + rect.dv - v));

                            if( (du + dv) < iClosestDistance)
                            {
                                iClosestDistance = du + dv;
                                cpcClosest = cpc;
                            }
                        }
                    }
                }
            }

            if(snapToText && cpcFound == null)
            {
                cpcFound = cpcClosest;
            }

            return cpcFound;
        }



        /// <summary>
        /// Returns the paragraphs for all rows in a table
        /// </summary>
        /// <param name="hasTextContent">
        /// True if any child paragraph result has text content
        /// </param>
        /// <returns>
        /// Array of paragraph results
        /// </returns>
        internal ReadOnlyCollection<ParagraphResult> GetChildrenParagraphResults(out bool hasTextContent)
        {
            MbpInfo mbpInfo = MbpInfo.FromElement(TableParagraph.Element, TableParagraph.StructuralCache.TextFormatterHost.PixelsPerDip);
            if (ThisFlowDirection != PageFlowDirection)
            {
                mbpInfo.MirrorBP();
            }

            Rect tableContentRect = GetTableContentRect(mbpInfo).FromTextDpi();
            double rowTop = tableContentRect.Y;
            Rect rowRect = tableContentRect;
            hasTextContent = false;

            List<ParagraphResult> rowParagraphResults = new List<ParagraphResult>(0);

            PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc;
            PTS.FSKUPDATE fskupdTable;
            PTS.FSRECT rectTable;
            if (QueryTableDetails(out arrayTableRowDesc, out fskupdTable, out rectTable))
            {
                for (int iR = 0; iR < arrayTableRowDesc.Length; ++iR)
                {
                    RowParagraph rowParagraph = (RowParagraph)(PtsContext.HandleToObject(arrayTableRowDesc[iR].fsnmRow));

                    rowRect.Y = rowTop;
                    rowRect.Height = GetActualRowHeight(arrayTableRowDesc, iR, mbpInfo);
                    RowParagraphResult rowParagraphResult = new RowParagraphResult(this, iR, rowRect, rowParagraph);
                    if (rowParagraphResult.HasTextContent)
                    {
                        hasTextContent = true;
                    }
                    rowParagraphResults.Add(rowParagraphResult);

                    rowTop += rowRect.Height; // Adjust for top of next row
                }
            }

            return new ReadOnlyCollection<ParagraphResult>(rowParagraphResults);
        }

        /// <summary>
        /// Returns the paragraphs for a given row
        /// </summary>
        /// <param name="rowIndex">Row index</param>
        /// <returns>
        /// Array of paragraph results
        /// </returns>
        /// <param name="hasTextContent">
        /// True if any child paragraph has text content
        /// </param>
        internal ReadOnlyCollection<ParagraphResult> GetChildrenParagraphResultsForRow(int rowIndex, out bool hasTextContent)
        {
            PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc;
            PTS.FSKUPDATE fskupdTable;
            PTS.FSRECT rectTable;

            List<ParagraphResult> cellParagraphResults = new List<ParagraphResult>(0);
            hasTextContent = false;

            if (QueryTableDetails(out arrayTableRowDesc, out fskupdTable, out rectTable))
            {
                PTS.FSKUPDATE[] arrayUpdate;
                IntPtr[] arrayFsCell;
                PTS.FSTABLEKCELLMERGE[] arrayTableCellMerge;

                QueryRowDetails(
                    arrayTableRowDesc[rowIndex].pfstablerow,
                    out arrayFsCell,
                    out arrayUpdate,
                    out arrayTableCellMerge);

                for (int iC = 0; iC < arrayFsCell.Length; ++iC)
                {
                    if(arrayFsCell[iC] != IntPtr.Zero &&
                        (rowIndex == 0 ||
                          (arrayTableCellMerge[iC] != PTS.FSTABLEKCELLMERGE.fskcellmergeMiddle &&
                           arrayTableCellMerge[iC] != PTS.FSTABLEKCELLMERGE.fskcellmergeLast)
                        )
                      )
                    {
                        CellParaClient cpc = (CellParaClient)(PtsContext.HandleToObject(arrayFsCell[iC]));
                        ParagraphResult cellParagraphResult = cpc.CreateParagraphResult();
                        if (cellParagraphResult.HasTextContent)
                        {
                            hasTextContent = true;
                        }
                        cellParagraphResults.Add(cellParagraphResult);
                    }
                }
            }

            return new ReadOnlyCollection<ParagraphResult>(cellParagraphResults);
        }

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
            CellParaClient cpc = GetCellParaClientFromPoint(point, snapToText);

            List<ParagraphResult> listResults = new List<ParagraphResult>(0);

            if(cpc != null)
            {
                listResults.Add(cpc.CreateParagraphResult());
            }

            return new ReadOnlyCollection<ParagraphResult>(listResults);
        }

        /// <summary>
        /// Returns the paragraphs for the appropriate cell, given a point
        /// </summary>
        /// <param name="position">Position of cell.</param>
        /// <returns>
        /// Array of paragraph results
        /// </returns>
        internal ReadOnlyCollection<ParagraphResult> GetParagraphsFromPosition(ITextPointer position)
        {
            CellParaClient cpc = GetCellParaClientFromPosition(position);

            List<ParagraphResult> listResults = new List<ParagraphResult>(0);

            if(cpc != null)
            {
                listResults.Add(cpc.CreateParagraphResult());
            }

            return new ReadOnlyCollection<ParagraphResult>(listResults);
        }

        /// <summary>
        /// Returns tight bounding path geometry.
        /// </summary>
        internal Geometry GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition, Rect visibleRect)
        {
            Debug.Assert(   TableParagraph.Table != null
                        &&  CalculatedColumns != null  );

            Geometry geometry = null;

            PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc;
            PTS.FSKUPDATE fskupdTable;
            PTS.FSRECT rectTable;

            if (QueryTableDetails(out arrayTableRowDesc, out fskupdTable, out rectTable))
            {
                bool passedEndPosition = false;

                for (int iR = 0; iR < arrayTableRowDesc.Length && !passedEndPosition; ++iR)
                {
                    PTS.FSKUPDATE[] arrayUpdate;
                    IntPtr[] arrayFsCell;
                    PTS.FSTABLEKCELLMERGE[] arrayTableCellMerge;

                    QueryRowDetails(
                        arrayTableRowDesc[iR].pfstablerow,
                        out arrayFsCell,
                        out arrayUpdate,
                        out arrayTableCellMerge);

                    for (int iC = 0; iC < arrayFsCell.Length; ++iC)
                    {
                        if (arrayFsCell[iC] == IntPtr.Zero)
                        {
                            //  paginated case - cell may be null
                            continue;
                        }

                        CellParaClient cpc = (CellParaClient)(PtsContext.HandleToObject(arrayFsCell[iC]));

                        if (endPosition.CompareTo(cpc.Cell.ContentStart) <= 0)
                        {
                            //  remember that at least one cell in this row starts after the range's end.
                            //  in this case it is safe to break after this whole row is processed.
                            //  Note: can not break right away because cells in arrayFsCell come not
                            //  necessarily in backing store (cp) order.
                            passedEndPosition = true;
                        }
                        else
                        {
                            if (startPosition.CompareTo(cpc.Cell.ContentEnd) <= 0)
                            {
                                Geometry cellGeometry = cpc.GetTightBoundingGeometryFromTextPositions(startPosition, endPosition, visibleRect);
                                CaretElement.AddGeometry(ref geometry, cellGeometry);
                            }
                        }
                    }
                }
            }

            if (geometry != null)
            {
                geometry = Geometry.Combine(geometry, Visual.Clip, GeometryCombineMode.Intersect, null);
            }
            return geometry;
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
            Debug.Assert(   TableParagraph.Table != null
                        &&  CalculatedColumns != null  );

            PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc;
            PTS.FSKUPDATE fskupdTable;
            PTS.FSRECT rectTable;

            if (QueryTableDetails(out arrayTableRowDesc, out fskupdTable, out rectTable))
            {
                for (int iR = 0; iR < arrayTableRowDesc.Length; ++iR)
                {
                    PTS.FSKUPDATE[] arrayUpdate;
                    IntPtr[] arrayFsCell;
                    PTS.FSTABLEKCELLMERGE[] arrayTableCellMerge;

                    QueryRowDetails(
                        arrayTableRowDesc[iR].pfstablerow,
                        out arrayFsCell,
                        out arrayUpdate,
                        out arrayTableCellMerge);

                    for (int iC = 0; iC < arrayFsCell.Length; ++iC)
                    {
                        if (arrayFsCell[iC] == IntPtr.Zero)
                        {
                            //  paginated case - cell may be null
                            continue;
                        }

                        CellParaClient cpc = (CellParaClient)(PtsContext.HandleToObject(arrayFsCell[iC]));

                        if(position.CompareTo(cpc.Cell.ContentStart) >= 0 && position.CompareTo(cpc.Cell.ContentEnd) <= 0)
                        {
                            return cpc;
                        }
                    }
                }
            }

            return (null);
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
            Debug.Assert(   TableParagraph.Table != null
                        &&  CalculatedColumns != null  );

            PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc;
            PTS.FSKUPDATE fskupdTable;
            PTS.FSRECT rectTable;
            int suggestedU = TextDpi.ToTextDpi(suggestedX);

            if (QueryTableDetails(out arrayTableRowDesc, out fskupdTable, out rectTable))
            {
                MbpInfo mbp = MbpInfo.FromElement(TableParagraph.Element, TableParagraph.StructuralCache.TextFormatterHost.PixelsPerDip);

                for (int iR = arrayTableRowDesc.Length - 1; iR >= 0; --iR)
                {
                    PTS.FSKUPDATE[] arrayUpdate;
                    IntPtr[] arrayFsCell;
                    PTS.FSTABLEKCELLMERGE[] arrayTableCellMerge;

                    QueryRowDetails(
                        arrayTableRowDesc[iR].pfstablerow,
                        out arrayFsCell,
                        out arrayUpdate,
                        out arrayTableCellMerge);

                    CellParaClient cpcClosest = null;
                    int iClosestDistance = int.MaxValue;

                    for (int iC = arrayFsCell.Length - 1; iC >= 0; --iC)
                    {
                        if (arrayFsCell[iC] == IntPtr.Zero)
                        {
                            //  paginated case - cell may be null
                            continue;
                        }

                        CellParaClient cpc = (CellParaClient)(PtsContext.HandleToObject(arrayFsCell[iC]));

                        // Check if bottom row of cell is less than row index for moving up.

                        int cellBottomIndex = cpc.Cell.RowIndex + cpc.Cell.RowSpan - 1;
                        if( (cellBottomIndex < rowIndex && cpc.Cell.RowGroupIndex == rowGroupIndex) ||
                            (cpc.Cell.RowGroupIndex < rowGroupIndex)
                          )
                        {
                            if(suggestedU >= cpc.Rect.u && suggestedU <= (cpc.Rect.u + cpc.Rect.du))
                            {
                                return cpc;
                            }
                            // Else record it, and return it if we find nothing better for this row.

                            int iDistance = Math.Abs((cpc.Rect.u + cpc.Rect.du / 2) - suggestedU);

                            if(iDistance < iClosestDistance)
                            {
                                iClosestDistance = iDistance;
                                cpcClosest = cpc;
                            }
                        }
                    }

                    if(cpcClosest != null)
                    {
                        return cpcClosest;
                    }
                }
            }

            return (null);
        }

        /// <summary>
        /// Returns an appropriate found cell
        /// </summary>
        /// <param name="suggestedX">Suggested X position for cell to find.</param>
        /// <param name="rowGroupIndex">RowGroupIndex to be above.</param>
        /// <param name="rowIndex">RowIndex to be below.</param>
        /// <returns>
        /// Cell Para Client of cell
        /// </returns>
        internal CellParaClient GetCellBelow(double suggestedX, int rowGroupIndex, int rowIndex)
        {
            Debug.Assert(   TableParagraph.Table != null
                        &&  CalculatedColumns != null  );

            PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc;
            PTS.FSKUPDATE fskupdTable;
            PTS.FSRECT rectTable;
            int suggestedU = TextDpi.ToTextDpi(suggestedX);

            if (QueryTableDetails(out arrayTableRowDesc, out fskupdTable, out rectTable))
            {
                for (int iR = 0; iR < arrayTableRowDesc.Length; ++iR)
                {
                    PTS.FSKUPDATE[] arrayUpdate;
                    IntPtr[] arrayFsCell;
                    PTS.FSTABLEKCELLMERGE[] arrayTableCellMerge;

                    QueryRowDetails(
                        arrayTableRowDesc[iR].pfstablerow,
                        out arrayFsCell,
                        out arrayUpdate,
                        out arrayTableCellMerge);

                    CellParaClient cpcClosest = null;
                    int iClosestDistance = int.MaxValue;

                    for (int iC = arrayFsCell.Length - 1; iC >= 0; --iC)
                    {
                        if (arrayFsCell[iC] == IntPtr.Zero)
                        {
                            //  paginated case - cell may be null
                            continue;
                        }

                        CellParaClient cpc = (CellParaClient)(PtsContext.HandleToObject(arrayFsCell[iC]));

                        if( (cpc.Cell.RowIndex > rowIndex && cpc.Cell.RowGroupIndex == rowGroupIndex) ||
                            (cpc.Cell.RowGroupIndex > rowGroupIndex)
                            )
                        {
                            if(suggestedU >= cpc.Rect.u && suggestedU <= (cpc.Rect.u + cpc.Rect.du))
                            {
                                return cpc;
                            }

                            // Else record it and report it if we find nothing better in this row.
                            int iDistance = Math.Abs((cpc.Rect.u + cpc.Rect.du / 2) - suggestedU);

                            if(iDistance < iClosestDistance)
                            {
                                iClosestDistance = iDistance;
                                cpcClosest = cpc;
                            }
                        }
}

                    if(cpcClosest != null)
                    {
                        return cpcClosest;
                    }
                }
            }

            return (null);
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
            CellParaClient cpc = GetCellParaClientFromPoint(point, true);

            if(cpc != null)
            {
                return new CellInfo(this, cpc);
            }

            return null;
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
            Debug.Assert(   TableParagraph.Table != null
                        &&  CalculatedColumns != null  );

            PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc;
            PTS.FSKUPDATE fskupdTable;
            PTS.FSRECT rectTable;

            if (QueryTableDetails(out arrayTableRowDesc, out fskupdTable, out rectTable))
            {
                int vrCur = GetTableOffsetFirstRowTop() + rectTable.v;

                for (int iR = 0; iR < arrayTableRowDesc.Length; ++iR)
                {
                    TableRow row = ((RowParagraph)(PtsContext.HandleToObject(arrayTableRowDesc[iR].fsnmRow))).Row;

                    if(((TextPointer)position).CompareTo(row.ContentEnd) == 0)
                    {
                        return new Rect( TextDpi.FromTextDpi(rectTable.u + rectTable.du),
                                         TextDpi.FromTextDpi(vrCur),
                                         1.0,
                                         TextDpi.FromTextDpi(arrayTableRowDesc[iR].u.dvrRow));
                    }

                    vrCur += arrayTableRowDesc[iR].u.dvrRow;
                }
            }

            // Valid row end position should have been specified.
            Debug.Assert(false);

            return System.Windows.Rect.Empty;
        }



        /// <summary>
        /// AutofitTable
        /// </summary>
        /// <param name="fswdirTrack">Direction of Track</param>
        /// <param name="durAvailableSpace">Available space</param>
        /// <param name="durTableWidth">Table width after autofit. It is the same for all rows</param>
        /// <remarks>
        /// durAvailableSpace is invalid due to the unfinished table/footer work in
        /// TableSection.GetPageDimensions (see notes in that method for more info).
        /// </remarks>
        internal void AutofitTable(
            uint fswdirTrack,                       // IN:  direction of Track
            int durAvailableSpace,                  // IN:  available space (?)
            out int durTableWidth)                  // OUT: Table width after autofit. It is the same for all rows :)
        {
            Debug.Assert(Table != null);

            double availableSpace = TextDpi.FromTextDpi(durAvailableSpace);
            double tableWidth;

            Autofit(availableSpace, out tableWidth);
            durTableWidth = TextDpi.ToTextDpi(tableWidth);
        }

        /// <summary>
        /// UpdAutofitTable
        /// </summary>
        /// <param name="fswdirTrack">Direction of Track</param>
        /// <param name="durAvailableSpace">Available space</param>
        /// <param name="durTableWidth">Table width after autofit</param>
        /// <param name="fNoChangeInCellWidths">Column width changes</param>
        /// <remarks>
        /// durAvailableSpace is invalid due to the unfinished table/footer work in
        /// TableSection.GetPageDimensions (see notes in that method for more info).
        /// </remarks>
        internal void UpdAutofitTable(
            uint fswdirTrack,                       // IN:  direction of Track
            int durAvailableSpace,                  // IN:  available space (?)
            out int durTableWidth,                  // OUT: Table width after autofit.
            out int fNoChangeInCellWidths)          // OUT:
        {
            Debug.Assert(Table != null);

            double availableSpace = TextDpi.FromTextDpi(durAvailableSpace);
            double tableWidth;
            fNoChangeInCellWidths = Autofit(availableSpace, out tableWidth);
            durTableWidth = TextDpi.ToTextDpi(tableWidth);
        }


        /// <summary>
        /// Performs autofit calculations
        /// </summary>
        /// <param name="availableWidth">Available width for table</param>
        /// <param name="tableWidth">Resulting table width</param>
        /// <returns>
        /// <c>PTS.True</c> when no changes in columns width detected,
        /// <c>PTS.False</c> otherwise
        /// </returns>
        internal int Autofit(double availableWidth, out double tableWidth)
        {
            int ret = PTS.True;

            ValidateCalculatedColumns();

            if (!DoubleUtil.AreClose(availableWidth, _previousAutofitWidth))
            {
                ret = ValidateTableWidths(availableWidth, out tableWidth);
            }
            else
            {
                tableWidth = _previousTableWidth;
            }

            _previousAutofitWidth = availableWidth;
            _previousTableWidth = tableWidth;

            return ret;
        }


        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Table paragraph created the client
        /// </summary>
        internal TableParagraph TableParagraph
        {
            get { return (TableParagraph)_paragraph; }
        }

        /// <summary>
        /// Table object proper
        /// </summary>
        internal Table Table
        {
            get { return TableParagraph.Table; }
        }


        /// <summary>
        /// CurrentPage Desired Width
        /// </summary>
        internal double TableDesiredWidth
        {
            get
            {
                // Use current format context's page width to limit cell spacing
                double durRet = 0;

                CalculatedColumn[] cols = CalculatedColumns;

                for (int i = 0; i < cols.Length; i++)
                {
                    durRet += cols[i].DurWidth + Table.InternalCellSpacing;
                }

                return durRet;
            }
        }

        /// <summary>
        /// CalculatedColumns
        /// </summary>
        internal CalculatedColumn[] CalculatedColumns
        {
            get
            {
                return (_calculatedColumns);
            }
        }

        /// <summary>
        /// Autofit width - what width we used when autofitting the table.
        /// </summary>
        internal double AutofitWidth
        {
            get { return _previousAutofitWidth; }
        }

        // ------------------------------------------------------------------
        // Is this the first chunk of paginated content.
        // ------------------------------------------------------------------
        internal override bool IsFirstChunk { get { return _isFirstChunk; } }
        private bool _isFirstChunk;

        // ------------------------------------------------------------------
        // Is this the last chunk of paginated content.
        // ------------------------------------------------------------------
        internal override bool IsLastChunk { get { return _isLastChunk; } }
        private bool _isLastChunk;

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // ------------------------------------------------------------------
        // Update information about first/last chunk.
        // ------------------------------------------------------------------
        /// <summary>
        /// Queries PTS and saves information about this paginated chunk
        /// </summary>
        /// <param name="arrayTableRowDesc">Table Row descriptions</param>
        private unsafe void UpdateChunkInfo(PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc)
        {
            PTS.FSTABLEROWDETAILS tableRowDetails;

            RowParagraph rowParagraph = (RowParagraph)(PtsContext.HandleToObject(arrayTableRowDesc[0].fsnmRow));
            TableRow row = rowParagraph.Row;

            PTS.Validate(PTS.FsQueryTableObjRowDetails(
                PtsContext.Context,
                arrayTableRowDesc[0].pfstablerow,
                out tableRowDetails));

            _isFirstChunk = (tableRowDetails.fskboundaryAbove == PTS.FSKTABLEROWBOUNDARY.fsktablerowboundaryOuter) &&
                (row.Index == 0) && Table.IsFirstNonEmptyRowGroup(row.RowGroup.Index);

            row = ((RowParagraph)(PtsContext.HandleToObject(arrayTableRowDesc[arrayTableRowDesc.Length - 1].fsnmRow))).Row;

            PTS.Validate(PTS.FsQueryTableObjRowDetails(
                PtsContext.Context,
                arrayTableRowDesc[arrayTableRowDesc.Length - 1].pfstablerow,
                out tableRowDetails));

            _isLastChunk = (tableRowDetails.fskboundaryBelow == PTS.FSKTABLEROWBOUNDARY.fsktablerowboundaryOuter) &&
                (row.Index == row.RowGroup.Rows.Count - 1) && Table.IsLastNonEmptyRowGroup(row.RowGroup.Index);
        }


        /// <summary>
        /// Queries PTS and returns table detailed structure / layout information about rows.
        /// </summary>
        /// <param name="arrayTableRowDesc">Storage for table rows information</param>
        /// <param name="fskupdTable">Result of update</param>
        /// <param name="rect">Rect of the table</param>
        /// <returns><c>true</c> when table has content; <c>false</c> otherwise</returns>
        private unsafe bool QueryTableDetails(
            out PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc,
            out PTS.FSKUPDATE fskupdTable,
            out PTS.FSRECT rect)
        {
            PTS.FSTABLEOBJDETAILS tableObjDetails;
            PTS.FSTABLEDETAILS tableDetails;

            PTS.Validate(PTS.FsQueryTableObjDetails(
                PtsContext.Context,
                _paraHandle.Value,
                out tableObjDetails));
            Debug.Assert(TableParagraph == (TableParagraph)(PtsContext.HandleToObject(tableObjDetails.fsnmTable)));

            fskupdTable = tableObjDetails.fskupdTableProper;
            rect = tableObjDetails.fsrcTableObj;

            PTS.Validate(PTS.FsQueryTableObjTableProperDetails(
                PtsContext.Context,
                tableObjDetails.pfstableProper,
                out tableDetails));

            if (tableDetails.cRows == 0)
            {
                //  table has no rows (thus no content)
                arrayTableRowDesc = null;
                return (false);
            }

            arrayTableRowDesc = new PTS.FSTABLEROWDESCRIPTION[tableDetails.cRows];

            fixed (PTS.FSTABLEROWDESCRIPTION * rgTableRowDesc = arrayTableRowDesc)
            {
                int cRowsActual;

                PTS.Validate(PTS.FsQueryTableObjRowList(
                    PtsContext.Context,
                    tableObjDetails.pfstableProper,
                    tableDetails.cRows,
                    rgTableRowDesc,
                    out cRowsActual));

                Debug.Assert(tableDetails.cRows == cRowsActual);
            }

            return (true);
        }

        /// <summary>
        /// Queries PTS and returns detailed structure / layout information about cells.
        /// </summary>
        /// <param name="pfstablerow">Row to query</param>
        /// <param name="arrayFsCell">Names of the cells</param>
        /// <param name="arrayUpdate">Update state of the cells</param>
        /// <param name="arrayTableCellMerge">Merge state of the cells</param>
        private unsafe void QueryRowDetails(
            IntPtr pfstablerow,
            out IntPtr[] arrayFsCell,
            out PTS.FSKUPDATE[] arrayUpdate,
            out PTS.FSTABLEKCELLMERGE[] arrayTableCellMerge)
        {
            PTS.FSTABLEROWDETAILS tableRowDetails;

            PTS.Validate(PTS.FsQueryTableObjRowDetails(
                PtsContext.Context,
                pfstablerow,
                out tableRowDetails));

            arrayUpdate = new PTS.FSKUPDATE[tableRowDetails.cCells];
            arrayFsCell = new IntPtr[tableRowDetails.cCells];
            arrayTableCellMerge = new PTS.FSTABLEKCELLMERGE[tableRowDetails.cCells];

            if (tableRowDetails.cCells > 0)
            {
                fixed (PTS.FSKUPDATE * rgUpdate = arrayUpdate)
                {
                    fixed (IntPtr * rgFsCell = arrayFsCell)
                    {
                        fixed (PTS.FSTABLEKCELLMERGE * rgTableCellMerge = arrayTableCellMerge)
                        {
                            int cCellsActual;

                            PTS.Validate(PTS.FsQueryTableObjCellList(
                                PtsContext.Context,
                                pfstablerow,
                                tableRowDetails.cCells,
                                rgUpdate,
                                rgFsCell,
                                rgTableCellMerge,
                                out cCellsActual));

                            Debug.Assert(tableRowDetails.cCells == cCellsActual);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// SynchronizeRowVisualsCollection.
        /// </summary>
        /// <param name="rowVisualsCollection">Collection to synchromize.</param>
        /// <param name="firstIndex">Index to start syncronization from.</param>
        /// <param name="row">Row that still alive.</param>
        private void SynchronizeRowVisualsCollection(
            VisualCollection rowVisualsCollection,
            int firstIndex,
            TableRow row)
        {
            //  quick check to see if collection is already synchronized
            if (((RowVisual)(rowVisualsCollection[firstIndex])).Row != row)
            {
                int lastIndex = firstIndex;
                int count = rowVisualsCollection.Count;

                while (++lastIndex < count)
                {
                    RowVisual rowVisual = (RowVisual)(rowVisualsCollection[lastIndex]);
                    if (rowVisual.Row == row)   break;
                }

                rowVisualsCollection.RemoveRange(firstIndex, lastIndex - firstIndex);
            }
        }

        /// <summary>
        /// SynchronizeCellVisualsCollection.
        /// </summary>
        /// <param name="cellVisualsCollection">Collection to synchromize.</param>
        /// <param name="firstIndex">Index to start syncronization from.</param>
        /// <param name="visual">Visual that still alive.</param>
        private void SynchronizeCellVisualsCollection(
            VisualCollection cellVisualsCollection,
            int firstIndex,
            Visual visual)
        {
            //  quick check to see if collection is already synchronized
            if ((cellVisualsCollection[firstIndex]) != visual)
            {
                int lastIndex = firstIndex;
                int count = cellVisualsCollection.Count;

                while (++lastIndex < count)
                {
                    if (cellVisualsCollection[lastIndex] == visual) break;
                }

                cellVisualsCollection.RemoveRange(firstIndex, lastIndex - firstIndex);
            }
        }

        /// <summary>
        /// Validates the row's visual.
        /// </summary>
        /// <param name="rowVisual">Row Visual to validate.</param>
        /// <param name="pfstablerow">Row to query.</param>
        /// <param name="fskupdRow">Row's change state.</param>
        /// <param name="calculatedColumns">Columns offsets information.</param>
        private void ValidateRowVisualSimple(
            RowVisual rowVisual,
            IntPtr pfstablerow,
            PTS.FSKUPDATE fskupdRow,
            CalculatedColumn[] calculatedColumns)
        {
            PTS.FSKUPDATE[] arrayUpdate;
            IntPtr[] arrayFsCell;
            PTS.FSTABLEKCELLMERGE[] arrayTableCellMerge;
            VisualCollection cellVisualsCollection;
            int sourceIndex;

            QueryRowDetails(
                pfstablerow,
                out arrayFsCell,
                out arrayUpdate,
                out arrayTableCellMerge);

            cellVisualsCollection = rowVisual.Children;
            sourceIndex = 0;

            for (int iC = 0; iC < arrayFsCell.Length; ++iC)
            {
                CellParaClient cellParaClient;
                double urCellOffset;
                PTS.FSKUPDATE fskupdCell;

                if (    //  paginated case - cell may be null
                        arrayFsCell[iC] == IntPtr.Zero
                        //  exclude hanging cells
                    ||  arrayTableCellMerge[iC] == PTS.FSTABLEKCELLMERGE.fskcellmergeMiddle
                    ||  arrayTableCellMerge[iC] == PTS.FSTABLEKCELLMERGE.fskcellmergeLast   )
                {
                    continue;
                }

                fskupdCell = (arrayUpdate[iC] != PTS.FSKUPDATE.fskupdInherited)
                    ? arrayUpdate[iC]
                    : fskupdRow;

                if (fskupdCell != PTS.FSKUPDATE.fskupdNoChange)
                {
                    cellParaClient = (CellParaClient)(PtsContext.HandleToObject(arrayFsCell[iC]));
                    urCellOffset = calculatedColumns[cellParaClient.ColumnIndex].UrOffset;

                    cellParaClient.ValidateVisual();

                    if (    fskupdCell == PTS.FSKUPDATE.fskupdNew
                        //  PTS bug is a suspect here - this is a temp workaround:
                        ||  VisualTreeHelper.GetParent(cellParaClient.Visual) == null   )
                    {
                        Visual currentParent = VisualTreeHelper.GetParent(cellParaClient.Visual) as Visual;
                        if(currentParent != null)
                        {
                            ContainerVisual parent = currentParent as ContainerVisual;
                            Invariant.Assert(parent != null, "parent should always derives from ContainerVisual");
                            parent.Children.Remove(cellParaClient.Visual);
                        }
                        cellVisualsCollection.Insert(sourceIndex, cellParaClient.Visual);
                    }
                    else
                    {
                        Debug.Assert(   cellParaClient.Visual != null
                                    //  If the check below fails, then PTS cheats by reporting "ChangInside" for
                                    //  a cell that in fact was re-Formatted.
                                    &&  VisualTreeHelper.GetParent(cellParaClient.Visual) != null   );

                        SynchronizeCellVisualsCollection(cellVisualsCollection, sourceIndex, cellParaClient.Visual);
                    }
                }
                sourceIndex++;
            }

            if (cellVisualsCollection.Count > sourceIndex)
            {
                cellVisualsCollection.RemoveRange(
                    sourceIndex,
                    cellVisualsCollection.Count - sourceIndex);
            }

            #if DEBUGDEBUG
            for (int iC = 0, sourceIndex = 0; iC < arrayFsCell.Length; ++iC)
            {
                if (    arrayFsCell[iC] != IntPtr.Zero
                    &&  arrayTableCellMerge[iC] != PTS.FSTABLEKCELLMERGE.fskcellmergeMiddle
                    &&  arrayTableCellMerge[iC] != PTS.FSTABLEKCELLMERGE.fskcellmergeLast   )
                {
                    CellParaClient cellParaClient = (CellParaClient)(PtsContext.HandleToObject(arrayFsCell[iC]));
                    Debug.Assert(rowVisual.Children.IndexOf(cellParaClient.Visual) == sourceIndex);
                    sourceIndex++;
                }
            }
            #endif // DEBUGDEBUG
        }

        /// <summary>
        /// Validates the row's visual.
        /// </summary>
        /// <param name="rowVisual">Row Visual to validate.</param>
        /// <param name="pfstablerow">Row to query.</param>
        /// <param name="tableColumnCount">Number of columns in the table.</param>
        /// <param name="fskupdRow">Row's change state.</param>
        /// <param name="calculatedColumns">Columns offsets information.</param>
        /// <remarks>
        ///
        /// </remarks>
        private void ValidateRowVisualComplex(
            RowVisual rowVisual,
            IntPtr pfstablerow,
            int tableColumnCount,
            PTS.FSKUPDATE fskupdRow,
            CalculatedColumn[] calculatedColumns)
        {
            PTS.FSKUPDATE[] arrayUpdate;
            IntPtr[] arrayFsCell;
            PTS.FSTABLEKCELLMERGE[] arrayTableCellMerge;
            CellParaClientEntry[] arrayCellParaClients;
            VisualCollection cellVisualsCollection;
            int sourceIndex;

            QueryRowDetails(
                pfstablerow,
                out arrayFsCell,
                out arrayUpdate,
                out arrayTableCellMerge);

            //  arrayFsCell lists cells in order different from one we want to maintain in visual collection.
            //  before going to update visual collection it is necessary to reorder ascending by cell's column index.
            //  knowing the following facts:
            //  * total number of columns the row holds (including row spanned cells from previous rows) less or equal to tableColumnCount;
            //  * total number of cells (including row spanned cells from previous rows) is less or equal to tableColumnCount;
            //  * cells do not overlap - no two cells have the same column index;
            //  * cells' column indices fall into the range [0, tableColumnCount - 1];
            //  it is possible to write custom and optimized sorting routine:
            //  * iterate through arrayFsCell;
            //  * for each item record its CellParaClient value into arrayCellParaClients[CellParaClient.ColumnIndex];
            //  once complete, arrayCellParaClients will contain CellParaClients in correct order. some entries however will be null
            //  due to potential column spanning of cells.
            arrayCellParaClients = new CellParaClientEntry[tableColumnCount];

            for (int iC = 0; iC < arrayFsCell.Length; ++iC)
            {
                CellParaClient cellParaClient;
                PTS.FSKUPDATE fskupdCell;
                int columnIndex;

                if (arrayFsCell[iC] == IntPtr.Zero)
                {
                    //  paginated case - cell may be null
                    continue;
                }

                fskupdCell = (arrayUpdate[iC] != PTS.FSKUPDATE.fskupdInherited)
                    ? arrayUpdate[iC]
                    : fskupdRow;

                cellParaClient = (CellParaClient)(PtsContext.HandleToObject(arrayFsCell[iC]));
                columnIndex = cellParaClient.ColumnIndex;
                arrayCellParaClients[columnIndex].cellParaClient = cellParaClient;
                arrayCellParaClients[columnIndex].fskupdCell = fskupdCell;
            }

            cellVisualsCollection = rowVisual.Children;
            sourceIndex = 0;

            for (int columnIndex = 0; columnIndex < arrayCellParaClients.Length; ++columnIndex)
            {
                CellParaClient cellParaClient;
                double urCellOffset;
                PTS.FSKUPDATE fskupdCell;

                cellParaClient = arrayCellParaClients[columnIndex].cellParaClient;
                if (cellParaClient == null)
                {
                    //  paginated case - cell may be null
                    continue;
                }

                fskupdCell = arrayCellParaClients[columnIndex].fskupdCell;

                if (fskupdCell != PTS.FSKUPDATE.fskupdNoChange)
                {
                    urCellOffset = calculatedColumns[columnIndex].UrOffset;
                    cellParaClient.ValidateVisual();

                    if (fskupdCell == PTS.FSKUPDATE.fskupdNew)
                    {
                        cellVisualsCollection.Insert(sourceIndex, cellParaClient.Visual);
                    }
                    else
                    {
                        Debug.Assert(   cellParaClient.Visual != null
                                    //  If the check below fails, then PTS cheats by reporting "ChangInside" for
                                    //  a cell that in fact was re-Formatted.
                                    &&  VisualTreeHelper.GetParent(cellParaClient.Visual) != null   );

                        SynchronizeCellVisualsCollection(cellVisualsCollection, sourceIndex, cellParaClient.Visual);
                    }
                }
                sourceIndex++;
            }

            if (cellVisualsCollection.Count > sourceIndex)
            {
                cellVisualsCollection.RemoveRange(
                    sourceIndex,
                    cellVisualsCollection.Count - sourceIndex);
            }

            #if DEBUGDEBUG
            for (int columnIndex = 0, sourceIndex = 0; columnIndex < arrayCellParaClients.Length; ++columnIndex)
            {
                CellParaClient cellParaClient = arrayCellParaClients[columnIndex].cellParaClient;
                if (cellParaClient != null)
                {
                    Debug.Assert(rowVisual.Children.IndexOf(cellParaClient.Visual) == sourceIndex);
                    sourceIndex++;
                }
            }
            #endif // DEBUGDEBUG
        }

        /// <summary>
        /// Draws the backgrounds for all columns in the table
        /// </summary>
        /// <param name="dc">Drawing Context.</param>
        /// <param name="tableContentRect">Content rect of table.</param>
        private void DrawColumnBackgrounds(DrawingContext dc, Rect tableContentRect)
        {
            double columnStart = tableContentRect.X;
            Rect colRect = tableContentRect;
            Brush columnBackgroundBrush;

            if (ThisFlowDirection != PageFlowDirection)
            {
                for (int iC = CalculatedColumns.Length-1; iC >= 0; iC--)
                {
                    columnBackgroundBrush = (iC < Table.Columns.Count)
                        ? Table.Columns[iC].Background
                        : null;

                    colRect.Width = CalculatedColumns[iC].DurWidth + Table.InternalCellSpacing;

                    if (columnBackgroundBrush != null)
                    {
                        colRect.X = columnStart;
                        dc.DrawRectangle(columnBackgroundBrush, null, colRect);
                    }

                    columnStart += colRect.Width;
                }
            }
            else
            {
                for (int iC = 0; iC < CalculatedColumns.Length; iC++)
                {
                    columnBackgroundBrush = (iC < Table.Columns.Count)
                        ? Table.Columns[iC].Background
                        : null;

                    colRect.Width = CalculatedColumns[iC].DurWidth + Table.InternalCellSpacing;

                    if (columnBackgroundBrush != null)
                    {
                        colRect.X = columnStart;
                        dc.DrawRectangle(columnBackgroundBrush, null, colRect);
                    }

                    columnStart += colRect.Width;
                }
            }
        }

        /// <summary>
        /// Calculates actual row height, subtracting what was previously reported as dvrBeforeTopRow and dvrAfterBottomRow
        /// </summary>
        private double GetActualRowHeight(PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc, int rowIndex, MbpInfo mbpInfo)
        {
            int dvAdjustment = 0;

            if(IsFirstChunk && rowIndex == 0)
            {
                dvAdjustment = -mbpInfo.BPTop;
            }

            if(IsLastChunk && rowIndex == arrayTableRowDesc.Length - 1)
            {
                dvAdjustment = -mbpInfo.BPBottom;
            }

            return TextDpi.FromTextDpi(arrayTableRowDesc[rowIndex].u.dvrRow + dvAdjustment);
        }

        /// <summary>
        /// Draws TableRowGroup backgrounds
        /// </summary>
        /// <param name="dc">DrawingContext to draw in.</param>
        /// <param name="arrayTableRowDesc">Row descriptions.</param>
        /// <param name="tableContentRect">External rect for this chunk of table.</param>
        /// <param name="mbpInfo">Margin/Border/Padding info for table.</param>
        /// <remarks>
        ///
        /// </remarks>
        private void DrawRowGroupBackgrounds(DrawingContext dc, PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc, Rect tableContentRect, MbpInfo mbpInfo)
        {
            double rowGroupTop = tableContentRect.Y;
            double rowGroupHeight = 0;
            Rect rowRect = tableContentRect;
            Brush rowGroupBackgroundBrush;

            if(arrayTableRowDesc.Length > 0)
            {
                TableRow row = ((RowParagraph)(PtsContext.HandleToObject(arrayTableRowDesc[0].fsnmRow))).Row;
                TableRowGroup tableRowGroup = row.RowGroup;

                for (int iR = 0; iR < arrayTableRowDesc.Length; ++iR)
                {
                    row = ((RowParagraph)(PtsContext.HandleToObject(arrayTableRowDesc[iR].fsnmRow))).Row;

                    if (tableRowGroup != row.RowGroup)
                    {
                        rowGroupBackgroundBrush = (Brush)tableRowGroup.GetValue(TextElement.BackgroundProperty);

                        if (rowGroupBackgroundBrush != null)
                        {
                            rowRect.Y = rowGroupTop;
                            rowRect.Height = rowGroupHeight;
                            dc.DrawRectangle(rowGroupBackgroundBrush, null, rowRect);
                        }

                        rowGroupTop += rowGroupHeight;
                        tableRowGroup = row.RowGroup;
                        rowGroupHeight = GetActualRowHeight(arrayTableRowDesc, iR, mbpInfo);
                    }
                    else
                    {
                        rowGroupHeight += GetActualRowHeight(arrayTableRowDesc, iR, mbpInfo);
                    }
                }

                rowGroupBackgroundBrush = (Brush)tableRowGroup.GetValue(TextElement.BackgroundProperty);
                if (rowGroupBackgroundBrush != null)
                {
                    rowRect.Y = rowGroupTop;
                    rowRect.Height = rowGroupHeight;
                    dc.DrawRectangle(rowGroupBackgroundBrush, null, rowRect);
                }
            }
}

        /// <summary>
        /// Draws TableRow backgrounds
        /// </summary>
        /// <param name="dc">DrawingContext to draw in.</param>
        /// <param name="arrayTableRowDesc">Row descriptions.</param>
        /// <param name="tableContentRect">External rect for this chunk of table.</param>
        /// <param name="mbpInfo">Margin/Border/Padding info for table.</param>
        /// <remarks>
        ///
        /// </remarks>
        private void DrawRowBackgrounds(DrawingContext dc, PTS.FSTABLEROWDESCRIPTION[] arrayTableRowDesc, Rect tableContentRect, MbpInfo mbpInfo)
        {
            double rowTop = tableContentRect.Y;
            Rect rowRect = tableContentRect;

            for (int iR = 0; iR < arrayTableRowDesc.Length; ++iR)
            {
                TableRow row = ((RowParagraph)(PtsContext.HandleToObject(arrayTableRowDesc[iR].fsnmRow))).Row;
                Brush rowBackgroundBrush = (Brush)row.GetValue(TextElement.BackgroundProperty);

                rowRect.Y = rowTop;
                rowRect.Height = GetActualRowHeight(arrayTableRowDesc, iR, mbpInfo);

                if (rowBackgroundBrush != null)
                {
                    dc.DrawRectangle(rowBackgroundBrush, null, rowRect);
                }

                rowTop += rowRect.Height; // Adjust for top of next row
            }
        }

        /// <summary>
        /// ValidateCalculatedColumns
        /// </summary>
        /// <remarks>
        /// Side effect: _durMinWidth and _durMaxWidth are also updated.
        /// </remarks>
        private void ValidateCalculatedColumns()
        {
            double totalPadding;
            int columns = Table.ColumnCount;

            if (_calculatedColumns == null)
            {
                _calculatedColumns = new CalculatedColumn[columns];
            }
            else if (_calculatedColumns.Length != columns)
            {
                CalculatedColumn[] newCalculatedColumns = new CalculatedColumn[columns];

                Array.Copy(
                    _calculatedColumns,
                    newCalculatedColumns,
                    Math.Min(_calculatedColumns.Length, columns));

                _calculatedColumns = newCalculatedColumns;
            }

            if (_calculatedColumns.Length > 0)
            {
                int i = 0;

                while (i < _calculatedColumns.Length && i < Table.Columns.Count)
                {
                    _calculatedColumns[i].UserWidth = Table.Columns[i].Width;
                    i++;
                }

                while (i < _calculatedColumns.Length)
                {
                    _calculatedColumns[i].UserWidth = TableColumn.DefaultWidth;
                    i++;
                }
            }

            _durMinWidth = _durMaxWidth = 0;

            for (int i = 0; i < _calculatedColumns.Length; ++i)
            {
                switch (_calculatedColumns[i].UserWidth.GridUnitType)
                {
                    case (GridUnitType.Auto):
                        //  cell does not support min max.
                        //  assign some numbers here.
                        _calculatedColumns[i].ValidateAuto(1.0, 10e5);
                        break;

                    case (GridUnitType.Star):
                        _calculatedColumns[i].ValidateAuto(1.0, 10e5);
                        break;

                    case (GridUnitType.Pixel):
                        _calculatedColumns[i].ValidateAuto(
                            _calculatedColumns[i].UserWidth.Value,
                            _calculatedColumns[i].UserWidth.Value);
                        break;

                    default:
                        Debug.Assert(false, "Unsupported unit type");
                        break;
                }
                _durMinWidth += _calculatedColumns[i].DurMinWidth;
                _durMaxWidth += _calculatedColumns[i].DurMaxWidth;
            }

            // Use durAvailable as width limit for MBP, and MaxWidth as height limit since height values will not be used
            MbpInfo mbpInfo = MbpInfo.FromElement(Paragraph.Element, Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);

            totalPadding = Table.InternalCellSpacing * Table.ColumnCount + mbpInfo.Margin.Left + mbpInfo.Border.Left + mbpInfo.Padding.Left + mbpInfo.Padding.Right + mbpInfo.Border.Right + mbpInfo.Margin.Right;
            _durMinWidth += totalPadding;
            _durMaxWidth += totalPadding;
        }

        /// <summary>
        /// ValidateTableWidths
        /// </summary>
        /// <param name="durAvailableWidth">Availble width</param>
        /// <param name="durTableWidth">Resulting table width</param>
        /// <returns>
        /// <c>PTS.True</c> when no changes in columns width detected,
        /// <c>PTS.False</c> otherwise
        /// </returns>
        private int ValidateTableWidths(double durAvailableWidth, out double durTableWidth)
        {
            bool exactWidth = false;                //  assume that width should never be "exact"
            double durAbsoluteMin, durAbsoluteMax;  //  sum of min and max for all user absolute specified columns
            double durScalableMin;                  //  sum if min for all user percent specified columns
            double durAutoMin, durAutoMax;          //  sum of min and max for all non-specified columns
            double mul, div, fP;
            double iPercent, iP;
            double durAbsoluteWidths;               //  calculated sum of widths for all absolute columns
            double durAbsoluteAndAutoWidths;        //  calculated sum of widths for all non-scalable columns
            double durAutoWidths;                   //  calculated sum of widths for all non-specified columns
            bool fUseMax = false, fUseMin = false, fUseMaxMax = false;
            bool fUseUserMax = false, fUseUserMin = false, fUseUserMaxMax = false;
            bool fSubtract = false, fUserSubtract = false;
            double durTableUserWidth;
            double cellSpacing = Table.InternalCellSpacing;
            // Use durAvailable for MBP width limits and MaxWidth for height limits. Height values are not used in this calculation.
            MbpInfo mbpInfo = MbpInfo.FromElement(Paragraph.Element, Paragraph.StructuralCache.TextFormatterHost.PixelsPerDip);

            double durTotalPadding = cellSpacing * Table.ColumnCount + TextDpi.FromTextDpi(mbpInfo.MBPLeft + mbpInfo.MBPRight);
            int ptsNoWidthChanges = PTS.True;

            // initialize output
            durTableWidth = 0;

            //
            // first calc sum of known percent, absolute and auto widths up
            //
            iPercent = 0;
            durAbsoluteMin = durAbsoluteMax = 0;
            durScalableMin = 0;
            durAutoMin = durAutoMax = 0;

            //
            // also keep track of the maxWidth/percent ratio necessary to
            // display the table with the columns at max width and the right
            // percent value
            //

            // Step 1 - Run through all of our calculated columns, and determine min and max widths (these widths do NOT include cell spacing)
            mul = 0;
            div = 1;
            for (int i = 0; i < _calculatedColumns.Length; ++i)
            {
                Debug.Assert(_calculatedColumns[i].UserWidth.GridUnitType == GridUnitType.Auto || _calculatedColumns[i].UserWidth.GridUnitType == GridUnitType.Star || _calculatedColumns[i].UserWidth.GridUnitType == GridUnitType.Pixel, "Unexpected GridUnitType");
                if (_calculatedColumns[i].UserWidth.IsAuto)
                {
                    Debug.Assert(0 <= _calculatedColumns[i].DurMinWidth && 0 <= _calculatedColumns[i].DurMaxWidth);
                    durAutoMin += _calculatedColumns[i].DurMinWidth;
                    durAutoMax += _calculatedColumns[i].DurMaxWidth;
                }
                else
                {
                    if (_calculatedColumns[i].UserWidth.IsStar)
                    {
                        iP = _calculatedColumns[i].UserWidth.Value;
                        if (iP < 0)
                        {
                            iP = 0;
                        }

                        // check for over 100% overflow
                        if (iPercent + iP > 100)
                        {
                            iP = 100 - iPercent;
                            iPercent = 100;
                            _calculatedColumns[i].UserWidth = new GridLength(iP, GridUnitType.Star);
                        }
                        else
                        {
                            iPercent += iP;
                        }

                        if (iP == 0)
                        {
                            // non empty cell should get at least 1%
                            iP = 1;
                        }

                        // remember maxWidth/percentage ratio
                        if (_calculatedColumns[i].DurMaxWidth * div > iP * mul)
                        {
                            mul = _calculatedColumns[i].DurMaxWidth;
                            div = iP;
                        }

                        durScalableMin += _calculatedColumns[i].DurMinWidth;
                    }
                    else
                    {
                        durAbsoluteMin += _calculatedColumns[i].DurMinWidth;
                        durAbsoluteMax += _calculatedColumns[i].DurMaxWidth;
                    }
                }
            }

            // iP is what left from the 100%

            // Step 2 - Calculate table user width - This width includes all padding and is a percentage of durAvailableWidth or _durMinWidth / _durMaxWidth,
            // all three values include padding.

            iP = 100 - iPercent;
            Debug.Assert(0 < iP || DoubleUtil.IsZero(iP));
            if (exactWidth)
            {
                // have no choice - table's parent requires the width to be exact
                durTableUserWidth = durAvailableWidth;
                if (durTableUserWidth < _durMinWidth && DoubleUtil.AreClose(durTableUserWidth, _durMinWidth) == false)
                {
                    durTableUserWidth = _durMinWidth;
                }
            }
            else if (0 < iPercent)  // if user width is not given back calculate it from the maxWidth/percentage ratio
            {
                // check if the remaining user and normal columns are requiring bigger ratio
                if (0 < iP)
                {
                    double tl = (durAbsoluteMax + durAutoMax) * div;
                    double tr = iP * mul;

                    // floating point (tl > tr)
                    if (tl > tr && DoubleUtil.AreClose(tl, tr) == false)
                    {
                        mul = durAbsoluteMax + durAutoMax;
                        div = iP;
                    }
                }

                //
                // if there is percentage left or there are only percentage columns use the ratio
                // to back-calculate the table width
                //
                if ((0 < iP) || DoubleUtil.IsZero(durAbsoluteMax + durAutoMax))
                {
                    durTableUserWidth = mul * 100 / div + durTotalPadding;

                    // floating point (durTableUserWidth > durAvailableWidth)
                    if (durTableUserWidth > durAvailableWidth && DoubleUtil.AreClose(durTableUserWidth, durAvailableWidth) == false)
                    {
                        durTableUserWidth = durAvailableWidth;
                    }
                }
                else
                {
                    // otherwise use available width
                    durTableUserWidth = durAvailableWidth;
                }

                // floating point (durTableUserWidth < _durMinWidth)
                if (durTableUserWidth < _durMinWidth && DoubleUtil.AreClose(durTableUserWidth, _durMinWidth) == false)
                {
                    durTableUserWidth = _durMinWidth;
                }
            }
            else
            {
                // floating point (_durMaxWidth < durAvailableWidth)
                if (_durMaxWidth < durAvailableWidth && DoubleUtil.AreClose(_durMaxWidth, durAvailableWidth) == false)
                {
                    // use max value if that smaller the parent size
                    durTableUserWidth = _durMaxWidth;
                }
                // floating point (_durMinWidth > durAvailableWidth)
                else if (_durMinWidth > durAvailableWidth && DoubleUtil.AreClose(_durMinWidth, durAvailableWidth) == false)
                {
                    // have to use min if that is bigger the parent
                    durTableUserWidth = _durMinWidth;
                }
                else
                {
                    // use parent between min and max
                    durTableUserWidth = durAvailableWidth;
                }
            }

            // subtract padding width which contains border and cellspacing
            // floating point (durTableUserWidth >= durTotalPadding)

            // Step 3 - Remove padding - (REVIEW - Is this guaranteed?)
            if (durTableUserWidth > durTotalPadding || DoubleUtil.AreClose(durTableUserWidth, durTotalPadding))
            {
                durTableUserWidth -= durTotalPadding;
            }

            // floating point (0 < (durAutoMax + durAbsoluteMax))
            // Step 4 - Calculate the space for 'auto' sized columns.
            if (0 < (durAutoMax + durAbsoluteMax) && DoubleUtil.IsZero(durAutoMax + durAbsoluteMax) == false)
            {
                // cache width remaining for normal and user columns over percent columns (durAbsoluteAndAutoWidths)
                durAbsoluteAndAutoWidths = iP * durTableUserWidth / 100;

                // floating point (durAbsoluteAndAutoWidths < (durAbsoluteMin + durAutoMin))
                if (durAbsoluteAndAutoWidths < (durAbsoluteMin + durAutoMin) && DoubleUtil.AreClose(durAbsoluteAndAutoWidths, (durAbsoluteMin + durAutoMin)) == false)
                {
                    durAbsoluteAndAutoWidths = durAbsoluteMin + durAutoMin;
                }

                // floating point (durAbsoluteAndAutoWidths > (durTableUserWidth - durScalableMin))
                if (durAbsoluteAndAutoWidths > (durTableUserWidth - durScalableMin) && DoubleUtil.AreClose(durAbsoluteAndAutoWidths, (durTableUserWidth - durScalableMin)) == false)
                {
                    durAbsoluteAndAutoWidths = durTableUserWidth - durScalableMin;
                }
            }
            else
            {
                // all widths is for percent columns
                durAbsoluteAndAutoWidths = 0;
            }

            //
            // distribute remaining width amongst normal and user columns
            // first try to use max width for user columns and normal columns
            //
            // floating point (0 < durAbsoluteMax)
            if (0 < durAbsoluteMax && DoubleUtil.IsZero(durAbsoluteMax) == false)
            {
                durAbsoluteWidths = durAbsoluteMax;
                if (durAbsoluteWidths > durAbsoluteAndAutoWidths && DoubleUtil.AreClose(durAbsoluteWidths, durAbsoluteAndAutoWidths) == false)
                {
                    durAbsoluteWidths = durAbsoluteAndAutoWidths;
                }

                // floating point (0 < durAutoMax)
                if (0 < durAutoMax && DoubleUtil.IsZero(durAutoMax) == false)
                {
                    durAutoWidths = durAutoMin;

                    // floating point ((durAbsoluteWidths + durAutoWidths) <= durAbsoluteAndAutoWidths)
                    if ((durAbsoluteWidths + durAutoWidths) < durAbsoluteAndAutoWidths || DoubleUtil.AreClose((durAbsoluteWidths + durAutoWidths), durAbsoluteAndAutoWidths))
                    {
                        durAutoWidths = durAbsoluteAndAutoWidths - durAbsoluteWidths;
                    }
                    else
                    {
                        durAbsoluteWidths = durAbsoluteMin;

                        // floating point ((durAbsoluteWidths + durAutoWidths) <= durAbsoluteAndAutoWidths)
                        if ((durAbsoluteWidths + durAutoWidths) < durAbsoluteAndAutoWidths || DoubleUtil.AreClose((durAbsoluteWidths + durAutoWidths), durAbsoluteAndAutoWidths))
                        {
                            durAbsoluteWidths = durAbsoluteAndAutoWidths - durAutoWidths;
                        }
                    }
                }
                else
                {
                    durAutoWidths = 0;

                    // floating point (durAbsoluteWidths < durAbsoluteAndAutoWidths)
                    if (durAbsoluteWidths < durAbsoluteAndAutoWidths && DoubleUtil.AreClose(durAbsoluteWidths, durAbsoluteAndAutoWidths) == false)
                    {
                        durAbsoluteWidths = durAbsoluteAndAutoWidths;
                    }
                }
            }
            else
            {
                durAbsoluteWidths = 0;

                // floating point (0 < durAutoMax)
                if (0 < durAutoMax && DoubleUtil.IsZero(durAutoMax) == false)
                {
                    durAutoWidths = durAutoMin;

                    // floating point (durAutoWidths < durAbsoluteAndAutoWidths)
                    if (durAutoWidths < durAbsoluteAndAutoWidths && DoubleUtil.AreClose(durAutoWidths, durAbsoluteAndAutoWidths) == false)
                    {
                        durAutoWidths = durAbsoluteAndAutoWidths;
                    }
                }
                else
                {
                    durAutoWidths = 0;
                }
            }

            // floating point (durAutoWidths > durAutoMax)
            if (durAutoWidths > durAutoMax && DoubleUtil.AreClose(durAutoWidths, durAutoMax) == false)
            {
                fUseMaxMax = true;
            }
            // floating point (durAutoWidths == durAutoMax)
            else if (DoubleUtil.AreClose(durAutoWidths, durAutoMax))
            {
                fUseMax = true;
            }
            // floating point (durAutoWidths == durAutoMin)
            else if (DoubleUtil.AreClose(durAutoWidths, durAutoMin))
            {
                fUseMin = true;
            }
            // floating point (durAutoWidths < durAutoMax)
            else if (durAutoWidths < durAutoMax && DoubleUtil.AreClose(durAutoWidths, durAutoMax) == false)
            {
                fSubtract = true;
            }

            // floating point (durAbsoluteWidths > durAbsoluteMax)
            if (durAbsoluteWidths > durAbsoluteMax && DoubleUtil.AreClose(durAbsoluteWidths, durAbsoluteMax) == false)
            {
                fUseUserMaxMax = true;
            }
            // floating point (durAbsoluteWidths == durAbsoluteMax)
            else if (DoubleUtil.AreClose(durAbsoluteWidths, durAbsoluteMax))
            {
                fUseUserMax = true;
            }
            // floating point (durAbsoluteWidths == durAbsoluteMin)
            else if (DoubleUtil.AreClose(durAbsoluteWidths, durAbsoluteMin))
            {
                fUseUserMin = true;
            }
            // floating point (durAbsoluteWidths < durAbsoluteMax)
            else if (durAbsoluteWidths < durAbsoluteMax && DoubleUtil.AreClose(durAbsoluteWidths, durAbsoluteMax) == false)
            {
                fUserSubtract = true;
            }

            // calculate real percentage of percent columns in the table now using the final widths
            fP = (0 < durTableUserWidth) ? (100 * (durTableUserWidth - durAbsoluteWidths - durAutoWidths) / durTableUserWidth) : 0;

            bool fAbsWidthsHaveFlex = !DoubleUtil.AreClose(durAbsoluteMax, durAbsoluteMin);

            // start with content area of table
            durTableWidth = TextDpi.FromTextDpi(mbpInfo.BPLeft);
            //
            // now go and calculate column widths by distributing the extra width over
            // the min width or subtracting the extra width from max
            //
            for (int i = 0; i < _calculatedColumns.Length; ++i)
            {
                if (_calculatedColumns[i].UserWidth.IsAuto)
                {
                    // adjust normal column by adding to min or subtracting from max
                    _calculatedColumns[i].DurWidth =
                        fSubtract
                        ? _calculatedColumns[i].DurMaxWidth - ((_calculatedColumns[i].DurMaxWidth - _calculatedColumns[i].DurMinWidth) * (durAutoMax - durAutoWidths) / (durAutoMax - durAutoMin))
                        : fUseMaxMax
                        ? _calculatedColumns[i].DurMaxWidth + (_calculatedColumns[i].DurMaxWidth * (durAutoWidths - durAutoMax) / durAutoMax)
                        : fUseMax
                        ? _calculatedColumns[i].DurMaxWidth
                        : fUseMin
                        ? _calculatedColumns[i].DurMinWidth
                          // floating point (0 < durAutoMax)
                        : (0 < durAutoMax && DoubleUtil.IsZero(durAutoMax) == false)
                        ? _calculatedColumns[i].DurMinWidth + (_calculatedColumns[i].DurMaxWidth * (durAutoWidths - durAutoMin) / durAutoMax)
                        : 0;
                }
                else if (_calculatedColumns[i].UserWidth.IsStar)
                {
                    //
                    // if percent first calculate the width from the percent
                    //
                    durAbsoluteAndAutoWidths =
                        (0 < iPercent)
                        ? (durTableUserWidth * (fP * _calculatedColumns[i].UserWidth.Value / iPercent) / 100)
                        : 0;

                    //
                    // make sure it is over the min width
                    //
                    durAbsoluteAndAutoWidths -= _calculatedColumns[i].DurMinWidth;

                    // floating point (durAbsoluteAndAutoWidths < 0)
                    if (durAbsoluteAndAutoWidths < 0 && DoubleUtil.IsZero(durAbsoluteAndAutoWidths) == false)
                    {
                        durAbsoluteAndAutoWidths = 0;
                    }

                    _calculatedColumns[i].DurWidth = (_calculatedColumns[i].DurMinWidth + durAbsoluteAndAutoWidths);
                }
                else
                {
                    Debug.Assert(_calculatedColumns[i].UserWidth.IsAbsolute);

                    // adjust user column by adding to min or subtracting from max
                    _calculatedColumns[i].DurWidth =
                        fUserSubtract
                        // table needs to be (durAbsoluteMax - durAbsoluteWidths) shorter, so subtract width
                        // from columns proportionally to (_calculatedColumns[i].DurMaxWidth - _calculatedColumns[i].DurMinWidth)
                        // But if all columns have DurMaxWidth==DurMinWidth (hence durAbsoluteMax == durAbsoluteMin),
                        // subtract proportionally to column.DurMaxWidth, to avoid divide-by-zero 
                        ?  fAbsWidthsHaveFlex
                            ? _calculatedColumns[i].DurMaxWidth - ((_calculatedColumns[i].DurMaxWidth - _calculatedColumns[i].DurMinWidth) * (durAbsoluteMax - durAbsoluteWidths) / (durAbsoluteMax - durAbsoluteMin))
                            : _calculatedColumns[i].DurMaxWidth - (_calculatedColumns[i].DurMaxWidth * (durAbsoluteMax - durAbsoluteWidths) / durAbsoluteMax)
                        : fUseUserMaxMax
                        ? _calculatedColumns[i].DurMaxWidth + (_calculatedColumns[i].DurMaxWidth * (durAbsoluteWidths - durAbsoluteMax) / durAbsoluteMax)
                        : fUseUserMax
                        ? _calculatedColumns[i].DurMaxWidth
                        : fUseUserMin
                        ? _calculatedColumns[i].DurMinWidth
                        // floating point (0 < durAbsoluteMax)
                        : (0 < durAbsoluteMax && DoubleUtil.IsZero(durAbsoluteMax) == false)
                        ? _calculatedColumns[i].DurMinWidth + (_calculatedColumns[i].DurMaxWidth * (durAbsoluteWidths - durAbsoluteMin) / durAbsoluteMax)
                        : 0;
                }

                Debug.Assert(_calculatedColumns[i].DurMinWidth <= _calculatedColumns[i].DurMaxWidth);
                // Cells themselves are 1/2 cell spacing inside column
                _calculatedColumns[i].UrOffset = durTableWidth + cellSpacing / 2.0;

                durTableWidth += _calculatedColumns[i].DurWidth + cellSpacing; // Advance to next column

                if (_calculatedColumns[i].PtsWidthChanged == PTS.True)
                {
                    ptsNoWidthChanges = PTS.False;
                }
            }

            durTableWidth += mbpInfo.Margin.Left + TextDpi.FromTextDpi(mbpInfo.MBPRight);

            return (ptsNoWidthChanges);
        }

        /// <summary>
        /// Content rect - Rect from which rows/rowgroups/columns are calculated.
        /// </summary>
        private PTS.FSRECT GetTableContentRect(MbpInfo mbpInfo)
        {
            int calculatedBPTop = IsFirstChunk ? mbpInfo.BPTop : 0;
            int calculatedBPBottom = IsLastChunk ? mbpInfo.BPBottom : 0;

            return new PTS.FSRECT(_rect.u + mbpInfo.BPLeft,
                                  _rect.v + calculatedBPTop,
                                  Math.Max(_rect.du - (mbpInfo.BPRight + mbpInfo.BPLeft), 1),
                                  Math.Max(_rect.dv - calculatedBPBottom - calculatedBPTop, 1)
                                 );
        }

        /// <summary>
        /// Returns the offset from the table top to the first row of the table for the current chunk.
        /// </summary>
        private int GetTableOffsetFirstRowTop()
        {
            MbpInfo mbp = MbpInfo.FromElement(TableParagraph.Element, TableParagraph.StructuralCache.TextFormatterHost.PixelsPerDip);

            return IsFirstChunk ? mbp.BPTop : 0;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Classes / Structures
        //
        //------------------------------------------------------

        #region Private Classes / Structures

        private struct CellParaClientEntry
        {
            internal CellParaClient cellParaClient;
            internal PTS.FSKUPDATE fskupdCell;
        }

        #endregion Private Classes / Structures

        //------------------------------------------------------
        //
        //  Private Data
        //
        //------------------------------------------------------

        #region Private Data

        // Rectangle of column table para client is in.
        private PTS.FSRECT _columnRect;
        private CalculatedColumn[] _calculatedColumns;  //  layout related information per column

        private double _durMinWidth;                    //  minimum width of the table
        private double _durMaxWidth;                    //  maximum wudth of the table

        private double _previousAutofitWidth = 0.0;     // Autofit width on previous layout pass.
        private double _previousTableWidth = 0.0;       // Table width on previous layout pass.

        #endregion Private Data
    }
}

