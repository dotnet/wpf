// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Implementation of the PTS paragraph corresponding to table.
//

#pragma warning disable 1634, 1691  // avoid generating warnings about unknown 
                                    // message numbers and unknown pragmas for PRESharp contol

using MS.Internal.Documents;
using MS.Internal.PtsTable;
using MS.Internal.Text;
using MS.Utility;
using System;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// Table paragraph PTS object implementation
    /// </summary>
    internal sealed class TableParagraph : BaseParagraph
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal TableParagraph(DependencyObject element, StructuralCache structuralCache)
            : base(element, structuralCache)
        {
            Table.TableStructureChanged += new System.EventHandler(TableStructureChanged);
        }

        // ------------------------------------------------------------------
        // IDisposable.Dispose
        // ------------------------------------------------------------------
        public override void Dispose()
        {
            Table.TableStructureChanged -= new System.EventHandler(TableStructureChanged);

            BaseParagraph paraChild = _firstChild;
            while (paraChild != null)
            {
                BaseParagraph para = paraChild;
                paraChild = paraChild.Next;
                para.Dispose();
                para.Next = null;
                para.Previous = null;
            }
            _firstChild = null;

            base.Dispose();
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods 
        #endregion Protected Methods 

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods 

        /// <summary>
        /// Collapse Margins
        /// </summary>
        /// <param name="paraClient">Para client</param>
        /// <param name="mcs">input margin collapsing state</param>
        /// <param name="fswdir">current direction (of the track, in which margin collapsing is happening)</param>
        /// <param name="suppressTopSpace">suppress empty space at the top of page</param>
        /// <param name="dvr">dvr, calculated based on margin collapsing state</param>
        internal override void CollapseMargin(
            BaseParaClient paraClient,          // IN:
            MarginCollapsingState mcs,          // IN:  input margin collapsing state
            uint fswdir,                        // IN:  current direction (of the track, in which margin collapsing is happening)
            bool suppressTopSpace,              // IN:  suppress empty space at the top of page
            out int dvr)                        // OUT: dvr, calculated based on margin collapsing state
        {
            if (suppressTopSpace && (StructuralCache.CurrentFormatContext.FinitePage || mcs == null))
                dvr = 0;
            else
            {
                MbpInfo mbp = MbpInfo.FromElement(Table, StructuralCache.TextFormatterHost.PixelsPerDip);
                MarginCollapsingState mcsOut = null;
                MarginCollapsingState.CollapseTopMargin(PtsContext, mbp, mcs, out mcsOut, out dvr);

                if (mcsOut != null)
                {
                    dvr = mcsOut.Margin;
                    mcsOut.Dispose();
                    mcsOut = null;
                }
            }
        }

        /// <summary>
        /// GetParaProperties. 
        /// </summary>
        /// <param name="fspap">paragraph properties</param>
        internal override void GetParaProperties(
            ref PTS.FSPAP fspap)                // OUT: paragraph properties
        {
            fspap.idobj = PtsHost.TableParagraphId;
            // should think about setting page break controlling bits here.
            // (Previously this was not needed, because of double para structure we had)
            fspap.fKeepWithNext = PTS.False;
            fspap.fBreakPageBefore = PTS.False;
            fspap.fBreakColumnBefore = PTS.False;
        }

        /// <summary>
        /// CreateParaclient
        /// </summary>
        /// <param name="pfsparaclient">Opaque to PTS paragraph client</param>
        internal override void CreateParaclient(
            out IntPtr pfsparaclient)           // OUT: opaque to PTS paragraph client
        {
#pragma warning disable 6518
            // Disable PRESharp warning 6518. TableParaClient is an UnmamangedHandle, that adds itself
            // to HandleMapper that holds a reference to it. PTS manages lifetime of this object, and 
            // calls DestroyParaclient to get rid of it. DestroyParaclient will call Dispose() on the object
            // and remove it from HandleMapper.
            TableParaClient paraClient = new TableParaClient(this);
            pfsparaclient = paraClient.Handle;
#pragma warning restore 6518
        }

        /// <summary>
        /// GetTableProperties
        /// </summary>
        /// <param name="fswdirTrack">Direction of Track</param>
        /// <param name="fstableobjprops">Properties of the table</param>
        internal void GetTableProperties(
            uint fswdirTrack,                       // IN:  direction of Track
            out PTS.FSTABLEOBJPROPS fstableobjprops)// OUT: properties of the table
        {
            fstableobjprops = new PTS.FSTABLEOBJPROPS();

            fstableobjprops.fskclear = PTS.FSKCLEAR.fskclearNone;
            fstableobjprops.ktablealignment = PTS.FSKTABLEOBJALIGNMENT.fsktableobjAlignLeft;
            fstableobjprops.fFloat = PTS.False;
            fstableobjprops.fskwr = PTS.FSKWRAP.fskwrBoth;
            fstableobjprops.fDelayNoProgress = PTS.False;
            fstableobjprops.dvrCaptionTop = 0;
            fstableobjprops.dvrCaptionBottom = 0;
            fstableobjprops.durCaptionLeft = 0;
            fstableobjprops.durCaptionRight = 0;
            fstableobjprops.fswdirTable = PTS.FlowDirectionToFswdir((FlowDirection)Element.GetValue(FrameworkElement.FlowDirectionProperty));
        }

        /// <summary>
        /// GetMCSClientAfterTable
        /// </summary>
        /// <param name="fswdirTrack">Direction of Track</param>
        /// <param name="pmcsclientIn">Margin collapsing state</param>
        /// <param name="ppmcsclientOut">Margin collapsing state</param>
        internal void GetMCSClientAfterTable( 
            uint fswdirTrack,
            IntPtr pmcsclientIn,
            out IntPtr ppmcsclientOut)
        {
            ppmcsclientOut = IntPtr.Zero;
            MbpInfo mbp = MbpInfo.FromElement(Table, StructuralCache.TextFormatterHost.PixelsPerDip);
            MarginCollapsingState mcs = null;
            
            if (pmcsclientIn != IntPtr.Zero)
                mcs = PtsContext.HandleToObject(pmcsclientIn) as MarginCollapsingState;

            MarginCollapsingState mcsNew = null;
            int margin;
            MarginCollapsingState.CollapseBottomMargin(PtsContext, mbp, mcs, out mcsNew, out margin);
            if (mcsNew != null)
                ppmcsclientOut = mcsNew.Handle;
        }

        /// <summary>
        /// GetFirstHeaderRow
        /// </summary>
        /// <param name="fRepeatedHeader">Repeated header flag</param>
        /// <param name="fFound">Indication that first header row is found</param>
        /// <param name="pnmFirstHeaderRow">First header row name</param>
        internal void GetFirstHeaderRow(
            int fRepeatedHeader,
            out int fFound,
            out IntPtr pnmFirstHeaderRow)
        {
            fFound = PTS.False;
            pnmFirstHeaderRow = IntPtr.Zero;
        }

        /// <summary>
        /// GetNextHeaderRow
        /// </summary>
        /// <param name="fRepeatedHeader">Repeated header flag</param>
        /// <param name="nmHeaderRow">Previous header row</param>
        /// <param name="fFound">Indication that header row is found</param>
        /// <param name="pnmNextHeaderRow">Header row name</param>
        internal void GetNextHeaderRow(
            int fRepeatedHeader,
            IntPtr nmHeaderRow, 
            out int fFound,
            out IntPtr pnmNextHeaderRow)
        {
            fFound = PTS.False;
            pnmNextHeaderRow = IntPtr.Zero;
        }

        /// <summary>
        /// GetFirstFooterRow
        /// </summary>
        /// <param name="fRepeatedFooter">Repeated footer flag</param>
        /// <param name="fFound">Indication that first header row is found</param>
        /// <param name="pnmFirstFooterRow">First footer row name</param>
        internal void GetFirstFooterRow(
            int fRepeatedFooter,
            out int fFound,
            out IntPtr pnmFirstFooterRow)
        {
            fFound = PTS.False;
            pnmFirstFooterRow = IntPtr.Zero;
        }

        /// <summary>
        /// GetNextFooterRow
        /// </summary>
        /// <param name="fRepeatedFooter">Repeated footer flag</param>
        /// <param name="nmFooterRow">Previous footer row</param>
        /// <param name="fFound">Indication that header row is found</param>
        /// <param name="pnmNextFooterRow">Footer row name</param>
        internal void GetNextFooterRow(
            int fRepeatedFooter,
            IntPtr nmFooterRow,
            out int fFound,
            out IntPtr pnmNextFooterRow)
        {
            fFound = PTS.False;
            pnmNextFooterRow = IntPtr.Zero;
        }

        /// <summary>
        /// GetFirstRow
        /// </summary>
        /// <param name="fFound">Indication that first body row is found</param>
        /// <param name="pnmFirstRow">First body row name</param>
        internal void GetFirstRow(
            out int fFound,
            out IntPtr pnmFirstRow)
        {            
            if(_firstChild == null)
            {
                TableRow tableRow = null;
                for(int rowGroupIndex = 0; rowGroupIndex < Table.RowGroups.Count && tableRow == null; rowGroupIndex++)
                {
                    TableRowGroup rowGroup = Table.RowGroups[rowGroupIndex];
                    if(rowGroup.Rows.Count > 0)
                    {
                        tableRow = rowGroup.Rows[0];
                        Invariant.Assert(tableRow.Index != -1);
                    }
                }

                if(tableRow != null)
                {
                    _firstChild = new RowParagraph(tableRow, StructuralCache);

                    ((RowParagraph)_firstChild).CalculateRowSpans();
                }
            }

            if(_firstChild != null)
            {
                fFound = PTS.True;
                pnmFirstRow = _firstChild.Handle;
            }
            else
            {
                fFound = PTS.False;
                pnmFirstRow = IntPtr.Zero;
            }
        }

        /// <summary>
        /// GetNextRow
        /// </summary>
        /// <param name="nmRow">Previous body row name</param>
        /// <param name="fFound">Indication that body row is found</param>
        /// <param name="pnmNextRow">Body row name</param>
        internal void GetNextRow(
            IntPtr nmRow,  
            out int fFound,
            out IntPtr pnmNextRow)
        {
            Debug.Assert(Table.RowGroups.Count > 0);

            BaseParagraph prevParagraph = ((RowParagraph)PtsContext.HandleToObject(nmRow));
            BaseParagraph nextParagraph = prevParagraph.Next;

            if(nextParagraph == null)
            {
                TableRow currentRow = ((RowParagraph)prevParagraph).Row;
                TableRowGroup currentRowGroup = currentRow.RowGroup;
                TableRow tableRow = null;

                int nextRowIndex = currentRow.Index + 1;
                int nextRowGroupIndex = currentRowGroup.Index + 1;

                if (nextRowIndex < currentRowGroup.Rows.Count)
                {
                    Debug.Assert(currentRowGroup.Rows[nextRowIndex].Index != -1, 
                        "Row is not in a table");

                    tableRow = currentRowGroup.Rows[nextRowIndex];
                }

                while(tableRow == null)
                {
                    if(nextRowGroupIndex == Table.RowGroups.Count)
                    {
                        break;
                    }

                    TableRowCollection Rows = Table.RowGroups[nextRowGroupIndex].Rows;

                    if (Rows.Count > 0)
                    {
                        Debug.Assert(Rows[0].Index != -1,
                            "Row is not in a table");

                        tableRow = Rows[0];
                    }

                    nextRowGroupIndex++;
                }

                if(tableRow != null)
                {
                    nextParagraph = new RowParagraph(tableRow, StructuralCache);
                    prevParagraph.Next = nextParagraph;
                    nextParagraph.Previous = prevParagraph;

                    ((RowParagraph)nextParagraph).CalculateRowSpans();
                }
            }

            if(nextParagraph != null)
            {
                fFound = PTS.True;
                pnmNextRow = nextParagraph.Handle;
            }
            else
            {
                fFound = PTS.False;
                pnmNextRow = IntPtr.Zero;
            }
        }

        /// <summary>
        /// UpdFChangeInHeaderFooter
        /// </summary>
        /// <param name="fHeaderChanged">Header changed?</param>
        /// <param name="fFooterChanged">Footer changed?</param>
        /// <param name="fRepeatedHeaderChanged">Repeated header changed?</param>
        /// <param name="fRepeatedFooterChanged">Repeated footer changed?</param>
        internal void UpdFChangeInHeaderFooter( // we don't do update in header/footer
            out int fHeaderChanged,                 // OUT: 
            out int fFooterChanged,                 // OUT: 
            out int fRepeatedHeaderChanged,         // OUT: unneeded for bottomless page, but...
            out int fRepeatedFooterChanged)         // OUT: unneeded for bottomless page, but...
        {
            fHeaderChanged = PTS.False;
            fRepeatedHeaderChanged = PTS.False;
            fFooterChanged = PTS.False;
            fRepeatedFooterChanged = PTS.False;
        }

        /// <summary>
        /// UpdGetFirstChangeInTable
        /// </summary>
        /// <param name="fFound">Indication that changed body row is found</param>
        /// <param name="fChangeFirst">Is the change in the first body row?</param>
        /// <param name="pnmRowBeforeChange">The last unchanged body row</param>
        internal void UpdGetFirstChangeInTable(
            out int fFound,                         // OUT: 
            out int fChangeFirst,                   // OUT: 
            out IntPtr pnmRowBeforeChange)          // OUT: 
        {
            // No incremental update for table.
            fFound = PTS.True;
            fChangeFirst = PTS.True;
            pnmRowBeforeChange = IntPtr.Zero;
        }

        /// <summary>
        /// GetDistributionKind
        /// </summary>
        /// <param name="fswdirTable">Direction of the Table</param>
        /// <param name="tabledistr">Height distribution kind</param>
        internal void GetDistributionKind(
            uint fswdirTable,                               // IN:  direction of the Table
            out PTS.FSKTABLEHEIGHTDISTRIBUTION tabledistr)  // OUT: height distribution kind
        {
            tabledistr = PTS.FSKTABLEHEIGHTDISTRIBUTION.fskdistributeUnchanged;
        }

        // ------------------------------------------------------------------
        // UpdGetParaChange
        // ------------------------------------------------------------------
        internal override void UpdGetParaChange(
            out PTS.FSKCHANGE fskch,            // OUT: kind of change
            out int fNoFurtherChanges)          // OUT: no changes after?
        {
            base.UpdGetParaChange(out fskch, out fNoFurtherChanges);

            fskch = PTS.FSKCHANGE.fskchNew;
        }

        // ------------------------------------------------------------------
        // Invalidate content's structural cache.
        //
        //      startPosition - Position to start invalidation from.
        //
        // Returns: 'true' if entire paragraph is invalid.
        // ------------------------------------------------------------------
        internal override bool InvalidateStructure(int startPosition)
        {
            bool isEntireTableInvalid = true;
            RowParagraph currentParagraph = _firstChild as RowParagraph;

            while(currentParagraph != null)
            {
                if(!InvalidateRowStructure(currentParagraph, startPosition))
                {
                    isEntireTableInvalid = false;
                }

                currentParagraph = currentParagraph.Next as RowParagraph;
            }

            return isEntireTableInvalid;
        }

        // ------------------------------------------------------------------
        // Invalidate accumulated format caches for the table.
        // ------------------------------------------------------------------
        internal override void InvalidateFormatCache()
        {
            RowParagraph currentParagraph = _firstChild as RowParagraph;

            while(currentParagraph != null)
            {
                InvalidateRowFormatCache(currentParagraph);

                currentParagraph = currentParagraph.Next as RowParagraph;
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
        internal Table Table 
        { 
            get 
            {
                return (Table)Element;
            }
        }

        #endregion Internal Properties 

        //------------------------------------------------------
        //
        //  Private methods
        //
        //------------------------------------------------------
        #region Private Methods

        // ------------------------------------------------------------------
        // Invalidate accumulated format caches for the row.
        // ------------------------------------------------------------------
        private bool InvalidateRowStructure(RowParagraph rowParagraph, int startPosition)
        {
            bool isEntireTableInvalid = true;

            for(int iCell = 0; iCell < rowParagraph.Cells.Length; iCell++)
            {
                CellParagraph cellParagraph = rowParagraph.Cells[iCell];

                if(cellParagraph.ParagraphEndCharacterPosition < startPosition || 
                   !cellParagraph.InvalidateStructure(startPosition))
                {
                    isEntireTableInvalid = false;
                }
            }
            return isEntireTableInvalid;
        }

        // ------------------------------------------------------------------
        // Invalidate accumulated format caches for the row.
        // ------------------------------------------------------------------
        private void InvalidateRowFormatCache(RowParagraph rowParagraph)
        {
            for(int iCell = 0; iCell < rowParagraph.Cells.Length; iCell++)
            {
                rowParagraph.Cells[iCell].InvalidateFormatCache();
            }
        }

        // ------------------------------------------------------------------
        // Table has been structurally altered
        // ------------------------------------------------------------------
        private void TableStructureChanged(object sender, EventArgs e)
        {
            // Disconnect obsolete paragraphs.
            BaseParagraph paraInvalid = _firstChild;
            while (paraInvalid != null)
            {
                paraInvalid.Dispose();
                paraInvalid = paraInvalid.Next;
            }
            _firstChild = null;

            //
            // Since whole table content is disposed, we need 
            // - to create dirty text range corresponding to the Table content
            // - notify formatter that Table's content is changed.
            //
            int charCount = Table.SymbolCount - 2;// This is equivalent to (ContentEndOffset – ContentStartOffset) but is more performant.
            if (charCount > 0)
            {
                DirtyTextRange dtr = new DirtyTextRange(Table.ContentStartOffset, charCount, charCount);
                StructuralCache.AddDirtyTextRange(dtr);
            }
            if (StructuralCache.FormattingOwner.Formatter != null)
            {
                StructuralCache.FormattingOwner.Formatter.OnContentInvalidated(true, Table.ContentStart, Table.ContentEnd);
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties 
        #endregion Private Properties 

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields 

        BaseParagraph _firstChild;

        #endregion Private Fields 

        //------------------------------------------------------
        //
        //  Private Structures / Classes
        //
        //------------------------------------------------------

        #region Private Structures Classes 

        #endregion Private Structures Classes 
    }
}

#pragma warning enable 1634, 1691

