// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: CellParagraph represents a single list item.
//


using System;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Documents;
using MS.Internal.PtsHost;
using MS.Internal.Text;
using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // CellParagraph represents a single cell.
    // ----------------------------------------------------------------------
    internal sealed class CellParagraph : SubpageParagraph
    {
        // ------------------------------------------------------------------
        // Constructor.
        //
        //      element - Element associated with paragraph.
        //      structuralCache - Content's structural cache
        // ------------------------------------------------------------------
        internal CellParagraph(DependencyObject element, StructuralCache structuralCache)
            : base(element, structuralCache)
        {
            _isInterruptible = false;
        }

        /// <summary>
        /// Table owner accessor
        /// </summary>
        internal TableCell Cell { get { return (TableCell)Element; } }

        /// <summary>
        /// FormatCellFinite
        /// </summary>
        /// <param name="tableParaClient">Table para client</param>
        /// <param name="pfsbrkcellIn">Subpage break record - not NULL if cell broken from previous page/column</param>
        /// <param name="pfsFtnRejector">Footnote rejector</param>
        /// <param name="fEmptyOK">Allow empty cell</param>
        /// <param name="fswdirTable">Table direction</param>
        /// <param name="dvrAvailable">Available vertical space</param>
        /// <param name="pfmtr">Formatting result</param>
        /// <param name="ppfscell">Cell para client</param>
        /// <param name="pfsbrkcellOut">Break record for the current cell</param>
        /// <param name="dvrUsed">Ised vertical space</param>
        internal void FormatCellFinite(
            TableParaClient tableParaClient,        // IN:  
            IntPtr pfsbrkcellIn,                    // IN:  not NULL if cell broken from previous page/column
            IntPtr pfsFtnRejector,                  // IN:  
            int fEmptyOK,                           // IN:  
            uint fswdirTable,                       // IN:  
            int dvrAvailable,                       // IN:  
            out PTS.FSFMTR pfmtr,                   // OUT: 
            out IntPtr ppfscell,                    // OUT: cell object
            out IntPtr pfsbrkcellOut,               // OUT: break if cell does not fit in dvrAvailable
            out int dvrUsed)                        // OUT: height -- min height required 
    
        {
            Debug.Assert(Cell.Index != -1 && Cell.ColumnIndex != -1, 
                "Cell is not in a table");
    
            CellParaClient cellParaClient;
            Size subpageSize;
    
            Debug.Assert(Cell.Table != null);

            cellParaClient = new CellParaClient(this, tableParaClient);

            subpageSize = new Size(
                cellParaClient.CalculateCellWidth(tableParaClient), 
                Math.Max(TextDpi.FromTextDpi(dvrAvailable), 0));
    
    
            cellParaClient.FormatCellFinite(subpageSize,
                                            pfsbrkcellIn,
                                            PTS.ToBoolean(fEmptyOK),
                                            fswdirTable, 
                                            PTS.FSKSUPPRESSHARDBREAKBEFOREFIRSTPARA.fsksuppresshardbreakbeforefirstparaNone,
                                            out pfmtr, 
                                            out dvrUsed,
                                            out pfsbrkcellOut);
    
            //  initialize output parameters
            ppfscell = cellParaClient.Handle;
    
            if(pfmtr.kstop == PTS.FSFMTRKSTOP.fmtrNoProgressOutOfSpace)
            {
                cellParaClient.Dispose();
                ppfscell = IntPtr.Zero;
                dvrUsed = 0;
            }
    
            if (dvrAvailable < dvrUsed)
            {
                if (PTS.ToBoolean(fEmptyOK))
                {
                    if (cellParaClient != null)     { cellParaClient.Dispose(); }
                    if (pfsbrkcellOut != IntPtr.Zero)
                    {
                        PTS.Validate(PTS.FsDestroySubpageBreakRecord(cellParaClient.PtsContext.Context, pfsbrkcellOut), cellParaClient.PtsContext);
                        pfsbrkcellOut = IntPtr.Zero;
                    }
    
                    ppfscell = IntPtr.Zero;
                    pfmtr.kstop = PTS.FSFMTRKSTOP.fmtrNoProgressOutOfSpace;
                    dvrUsed = 0;
                }
                else
                {
                    pfmtr.fForcedProgress = PTS.True;
                }
            }
        }
    
        /// <summary>
        /// FormatCellBottomless
        /// </summary>
        /// <param name="tableParaClient">Table para client</param>
        /// <param name="fswdirTable">Flow direction</param>
        /// <param name="fmtrbl">Formatting result</param>
        /// <param name="ppfscell">Cell para client</param>
        /// <param name="dvrUsed">Height consumed</param>
        internal void FormatCellBottomless(
            TableParaClient tableParaClient,    // IN:  
            uint fswdirTable,                   // IN:  
            out PTS.FSFMTRBL fmtrbl,            // OUT: 
            out IntPtr ppfscell,                // OUT: cell object
            out int dvrUsed)                    // OUT: height -- min height 
                                                //      required 
        {
            Debug.Assert(Cell.Index != -1 && Cell.ColumnIndex != -1, 
                "Cell is not in a table");
    
            Debug.Assert(Cell.Table != null);
    
            CellParaClient cellParaClient = new CellParaClient(this, tableParaClient);
    
            cellParaClient.FormatCellBottomless(fswdirTable, cellParaClient.CalculateCellWidth(tableParaClient), out fmtrbl, out dvrUsed);
             
    
            //  initialize output parameters
            ppfscell = cellParaClient.Handle;
        }
    
        /// <summary>
        /// UpdateBottomlessCell
        /// </summary>
        /// <param name="cellParaClient">Current cell para client</param>
        /// <param name="tableParaClient">Table para cleint</param>
        /// <param name="fswdirTable">Flow direction</param>
        /// <param name="fmtrbl">Formatting result</param>
        /// <param name="dvrUsed">Height consumed</param>
        internal void UpdateBottomlessCell(
            CellParaClient cellParaClient,          // IN:  
            TableParaClient tableParaClient,        // IN:  
            uint fswdirTable,                       // IN:  
            out PTS.FSFMTRBL fmtrbl,                // OUT: 
            out int dvrUsed)                        // OUT: height -- min height required 
        {
            Debug.Assert(Cell.Index != -1 && Cell.ColumnIndex != -1, 
                "Cell is not in a table");
    
            Debug.Assert(Cell.Table != null);
    
            cellParaClient.UpdateBottomlessCell(fswdirTable, cellParaClient.CalculateCellWidth(tableParaClient), out fmtrbl, out dvrUsed);
        }

        /// <summary>
        /// SetCellHeight
        /// </summary>
        /// <param name="cellParaClient">Cell para client</param>
        /// <param name="tableParaClient">Table para client</param>
        /// <param name="subpageBreakRecord">Break record if cell is broken</param>
        /// <param name="fBrokenHere">Cell broken on this page/column</param>
        /// <param name="fswdirTable">Flow direction</param>
        /// <param name="dvrActual">Actual height</param>
        internal void SetCellHeight(
            CellParaClient cellParaClient,          // IN: cell object
            TableParaClient tableParaClient,        // table's para client
            IntPtr subpageBreakRecord,              // not NULL if cell broken from previous page/column
            int fBrokenHere,                        // TRUE if cell broken on this page/column: no reformatting
            uint fswdirTable, 
            int dvrActual)
        {
            cellParaClient.ArrangeHeight = TextDpi.FromTextDpi(dvrActual);
        }


        /// <summary>
        /// UpdGetCellChange
        /// </summary>
        /// <param name="fWidthChanged">Indication that cell's width changed
        /// </param>
        /// <param name="fskchCell">Change kind. One of: None, New, Inside
        /// </param>
        internal void UpdGetCellChange(
            out int fWidthChanged,                  // OUT: 
            out PTS.FSKCHANGE fskchCell)            // OUT: 
        {
            //  calculate state change
            fWidthChanged = PTS.True;
            fskchCell = PTS.FSKCHANGE.fskchNew;
        }
}
}

