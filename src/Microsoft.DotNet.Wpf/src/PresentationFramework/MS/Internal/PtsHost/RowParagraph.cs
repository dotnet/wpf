// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: RowParagraph represents a single row in a table
//
using System;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Documents;
using MS.Internal.PtsTable;
using MS.Internal.Text;
using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // RowParagraph represents a single row.
    // ----------------------------------------------------------------------
    internal sealed class RowParagraph : BaseParagraph
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // ------------------------------------------------------------------
        // Constructor.
        //
        //      element - Element associated with paragraph.
        //      structuralCache - Content's structural cache
        // ------------------------------------------------------------------
        internal RowParagraph(DependencyObject element, StructuralCache structuralCache)
            : base(element, structuralCache)
        {
        }

        // ------------------------------------------------------------------
        // IDisposable.Dispose
        // ------------------------------------------------------------------
        public override void Dispose()
        {
            if(_cellParagraphs != null)
            {
                for(int index = 0; index < _cellParagraphs.Length; index++)
                {
                    _cellParagraphs[index].Dispose();
                }
            }
            _cellParagraphs = null;

            base.Dispose();
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        //-------------------------------------------------------------------
        // GetParaProperties
        //-------------------------------------------------------------------
        internal override void GetParaProperties(
            ref PTS.FSPAP fspap)                // OUT: paragraph properties
        {
            Invariant.Assert(false); // No para properties for row.
        }

        //-------------------------------------------------------------------
        // CreateParaclient
        //-------------------------------------------------------------------
        internal override void CreateParaclient(
            out IntPtr paraClientHandle)        // OUT: opaque to PTS paragraph client
        {
            Invariant.Assert(false); // No valid para client for a row paragraph.
            paraClientHandle = IntPtr.Zero;
        }

        // ------------------------------------------------------------------
        // UpdGetParaChange - RowParagraph is always new
        // ------------------------------------------------------------------
        internal override void UpdGetParaChange(
            out PTS.FSKCHANGE fskch,            // OUT: kind of change
            out int fNoFurtherChanges)          // OUT: no changes after?
        {
            base.UpdGetParaChange(out fskch, out fNoFurtherChanges);

            fskch = PTS.FSKCHANGE.fskchNew;
        }

        /// <summary>
        /// GetRowProperties
        /// </summary>
        /// <param name="fswdirTable">Flow direction for table</param>
        /// <param name="rowprops">Row properties structure</param>
        internal void GetRowProperties(
            uint fswdirTable,                       // IN:
            out PTS.FSTABLEROWPROPS rowprops)       // OUT:
        {
            // local variables
            PTS.FSKROWHEIGHTRESTRICTION fskrowheight;
            int                         dvrAboveBelow;
            bool isLastRowOfRowGroup = (Row.Index == Row.RowGroup.Rows.Count - 1);

            //  Note: PTS generally does not accept rows with no real cells
            //  (Defintinion: real cell is
            //      a) cell with no vertical merge;
            //      OR
            //      b) vertically merged cell ending in this row)
            //  However PTS accepts a row with no real cells if it has explicit height set.
            //  So fskrowheight is set to "0" if
            //      a) user said so;
            //      b) no real cells found;

            GetRowHeight(out fskrowheight, out dvrAboveBelow);

            // initialize output parameter(s)
            rowprops = new PTS.FSTABLEROWPROPS();

            rowprops.fskrowbreak = PTS.FSKROWBREAKRESTRICTION.fskrowbreakAnywhere;
            rowprops.fskrowheight = fskrowheight;
            rowprops.dvrRowHeightRestriction = 0;
            rowprops.dvrAboveRow = dvrAboveBelow;
            rowprops.dvrBelowRow = dvrAboveBelow;


            int cellSpacing = TextDpi.ToTextDpi(Table.InternalCellSpacing);

            // Clip MBP values to structural cache's current format context size. Also use current format context's page height to
            // clip cell spacing
            MbpInfo mbpInfo = MbpInfo.FromElement(Table, StructuralCache.TextFormatterHost.PixelsPerDip);

            if (Row.Index == 0 && Table.IsFirstNonEmptyRowGroup(Row.RowGroup.Index))
            {
                rowprops.dvrAboveTopRow = mbpInfo.BPTop + cellSpacing / 2;
            }
            else
            {
                rowprops.dvrAboveTopRow = dvrAboveBelow;
            }

            if (isLastRowOfRowGroup && Table.IsLastNonEmptyRowGroup(Row.RowGroup.Index))
            {
                rowprops.dvrBelowBottomRow = mbpInfo.BPBottom + cellSpacing / 2;
            }
            else
            {
                rowprops.dvrBelowBottomRow = dvrAboveBelow;
            }

            rowprops.dvrAboveRowBreak = cellSpacing / 2;
            rowprops.dvrBelowRowBreak = cellSpacing / 2;

            rowprops.cCells = Row.FormatCellCount;
        }

        /// <summary>
        /// FInterruptFormattingTable
        /// </summary>
        /// <param name="dvr">Current height progress</param>
        /// <param name="fInterrupt">Do interrupt?</param>
        internal void FInterruptFormattingTable(
            int dvr,
            out int fInterrupt)
        {
            fInterrupt = PTS.False;
        }

        /// <summary>
        /// CalcHorizontalBBoxOfRow
        /// </summary>
        /// <param name="cCells">Cell amount</param>
        /// <param name="rgnmCell">Cell names</param>
        /// <param name="rgpfsCell">Cell clients</param>
        /// <param name="urBBox">Bounding box</param>
        /// <param name="durBBox">Bounding box</param>
        internal unsafe void CalcHorizontalBBoxOfRow(
            int cCells,
            IntPtr* rgnmCell,
            IntPtr* rgpfsCell,
            out int urBBox,
            out int durBBox)
        {
            Debug.Assert(cCells == Row.FormatCellCount);

            urBBox = 0;
            durBBox = 0;

            for(int index = 0; index < cCells; index++)
            {
                if(rgpfsCell[index] != IntPtr.Zero)
                {
                    CellParaClient cellParaClient = PtsContext.HandleToObject(rgpfsCell[index]) as CellParaClient;
                    PTS.ValidateHandle(cellParaClient);

                    durBBox = TextDpi.ToTextDpi(cellParaClient.TableParaClient.TableDesiredWidth);
                    break;
                }
            }
}

        /// <summary>
        /// GetCells
        /// </summary>
        /// <param name="cCells">Cell amount reserves by PTS for the row</param>
        /// <param name="rgnmCell">Array of cell names</param>
        /// <param name="rgkcellmerge">Array of vertical merge flags</param>
        internal unsafe void GetCells(
            int cCells,
            IntPtr* rgnmCell,
            PTS.FSTABLEKCELLMERGE* rgkcellmerge)
        {
            Invariant.Assert(cCells == Row.FormatCellCount);

            // To protect against a buffer overflow, we must check that we aren't writing more
            // cells than were allocated.  So we check against the cell count we have -
            // Row.FormatCellCount.  But that's calculated elsewhere.  What if it's stale?
            // There aren't any direct values available to compare against at the start of
            // this function, so we need two separate asserts.  Bug 1149633.
            Invariant.Assert(cCells >= Row.Cells.Count); // Protect against buffer overflow


            int i = 0;

            //  first, submit all non vertically merged cells
            for (int j = 0; j < Row.Cells.Count; ++j)
            {
                TableCell cell = Row.Cells[j];
                if (cell.RowSpan == 1)
                {
                    rgnmCell[i] = _cellParagraphs[j].Handle;
                    rgkcellmerge[i] = PTS.FSTABLEKCELLMERGE.fskcellmergeNo;
                    i++;
                }
            }

            // i now contains the exact number of non-rowspan cells on this row.  Use it to verify
            // total number of cells, before we possibly write off end of allocated array.
            Invariant.Assert(cCells == i + _spannedCells.Length); // Protect against buffer overflow

            //  second, submit all vertically merged cells
            if (_spannedCells.Length > 0)
            {
                bool lastRow = Row.Index == Row.RowGroup.Rows.Count - 1;

                for (int j = 0; j < _spannedCells.Length; ++j)
                {
                    Debug.Assert (_spannedCells[j] != null);

                    TableCell cell = _spannedCells[j].Cell;
                    rgnmCell[i] = _spannedCells[j].Handle;

                    if (cell.RowIndex == Row.Index)
                    {
                        rgkcellmerge[i] = lastRow
                            ? PTS.FSTABLEKCELLMERGE.fskcellmergeNo
                            : PTS.FSTABLEKCELLMERGE.fskcellmergeFirst;
                    }
                    else if (Row.Index - cell.RowIndex + 1 < cell.RowSpan)
                    {
                        rgkcellmerge[i] = lastRow
                            ? PTS.FSTABLEKCELLMERGE.fskcellmergeLast
                            : PTS.FSTABLEKCELLMERGE.fskcellmergeMiddle;
                    }
                    else
                    {
                        Debug.Assert(Row.Index - cell.RowIndex + 1 == cell.RowSpan);
                        rgkcellmerge[i] = PTS.FSTABLEKCELLMERGE.fskcellmergeLast;
                    }

                    i++;
                }
            }
        }

        /// <summary>
        /// Calculates the spanned cells for this paragraph from the spanned cells for the previous paragraph.
        /// This needs to be done to share the spanned cell paragraphs.
        /// </summary>
        internal void CalculateRowSpans()
        {
            RowParagraph rowPrevious = null;

            if(Row.Index != 0 && Previous != null)
            {
                rowPrevious = ((RowParagraph)Previous);
            }

            Invariant.Assert(_cellParagraphs == null);

            _cellParagraphs = new CellParagraph[Row.Cells.Count];

            for(int cellIndex = 0; cellIndex < Row.Cells.Count; cellIndex++)
            {
                _cellParagraphs[cellIndex] = new CellParagraph(Row.Cells[cellIndex], StructuralCache);
            }

            Invariant.Assert(_spannedCells == null);

            if (Row.SpannedCells != null)
            {
                _spannedCells = new CellParagraph[Row.SpannedCells.Length];
            }
            else
            {
                _spannedCells = new CellParagraph[0];
            }

            for(int index = 0; index < _spannedCells.Length; index++)
            {
                _spannedCells[index] = FindCellParagraphForCell(rowPrevious, Row.SpannedCells[index]);
            }
        }

        /// <summary>
        /// Returns row height for this row, depending on cell content (Real/Foreign/Etc)
        /// </summary>
        internal void GetRowHeight(out PTS.FSKROWHEIGHTRESTRICTION fskrowheight, out int dvrAboveBelow)
        {
            bool isLastRowOfRowGroup = (Row.Index == Row.RowGroup.Rows.Count - 1);

            if (Row.HasRealCells ||
                (isLastRowOfRowGroup && _spannedCells.Length > 0))
            {
                // Use current format context's page height to limit vertical cell spacing
                fskrowheight = PTS.FSKROWHEIGHTRESTRICTION.fskrowheightNatural;
                dvrAboveBelow = TextDpi.ToTextDpi(Table.InternalCellSpacing / 2.0);
            }
            else
            {
                fskrowheight = PTS.FSKROWHEIGHTRESTRICTION.fskrowheightExactNoBreak;
                dvrAboveBelow = 0;
            }
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties


        /// <summary>
        /// Table owner accessor
        /// </summary>
        internal TableRow Row { get { return (TableRow)Element; } }

        /// <summary>
        /// Table
        /// </summary>
        internal Table Table { get { return Row.Table; } }

        /// <summary>
        /// Cell Paragraphs
        /// </summary>
        internal CellParagraph[] Cells { get { return _cellParagraphs; } }


        #endregion Internal Properties


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Helper method to scan our cell list for a cell paragraph, and if not found, look at the spanning cell paragraphs
        /// from previous row, if one exists.
        /// </summary>
        private CellParagraph FindCellParagraphForCell(RowParagraph previousRow, TableCell cell)
        {
            for(int index = 0; index < _cellParagraphs.Length; index++)
            {
                if(_cellParagraphs[index].Cell == cell)
                {
                    return _cellParagraphs[index];
                }
            }

            if(previousRow != null)
            {
                for(int index = 0; index < previousRow._spannedCells.Length; index++)
                {
                    if(previousRow._spannedCells[index].Cell == cell)
                    {
                        return previousRow._spannedCells[index];
                    }
                }
            }

            Invariant.Assert(false, "Structural integrity for table not correct.");
            return null;
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private CellParagraph[] _cellParagraphs;     //  collection of cells belonging to the row
        private CellParagraph[] _spannedCells;        //  row spanned cell storage

        #endregion Private Fields
    }
}
