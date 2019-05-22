// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Automation peer for Table
//

using System.Windows.Automation.Provider;   // IRawElementProviderSimple
using System.Windows.Documents;

namespace System.Windows.Automation.Peers
{
    ///
    public class TableAutomationPeer : TextElementAutomationPeer, IGridProvider
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">Owner of the AutomationPeer.</param>
        public TableAutomationPeer(Table owner)
            : base(owner)
        {
            _rowCount = GetRowCount();
            _columnCount = GetColumnCount();
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetPattern"/>
        /// </summary>
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Grid)
            {
                return this;
            }
            else
            {
                return base.GetPattern(patternInterface);
            }
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetAutomationControlTypeCore"/>
        /// </summary>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Table;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetClassNameCore"/>
        /// </summary>
        protected override string GetClassNameCore()
        {
            return "Table";
        }

        /// <summary>
        /// <see cref="AutomationPeer.IsControlElementCore"/>
        /// </summary>
        protected override bool IsControlElementCore()
        {
            // We only want this peer to show up in the Control view if it is visible
            // For compat we allow falling back to legacy behavior (returning true always)
            // based on AppContext flags, IncludeInvisibleElementsInControlView evaluates them.
            return IncludeInvisibleElementsInControlView || IsTextViewVisible == true;
        }

        /// <summary>
        /// Raises property changed events in response to structure changes.
        /// </summary>
        internal void OnStructureInvalidated()
        {
            int rowCount = GetRowCount();
            if (rowCount != _rowCount)
            {
                RaisePropertyChangedEvent(GridPatternIdentifiers.RowCountProperty, _rowCount, rowCount);
                _rowCount = rowCount;
            }
            int columnCount = GetColumnCount();
            if (columnCount != _columnCount)
            {
                RaisePropertyChangedEvent(GridPatternIdentifiers.ColumnCountProperty, _columnCount, columnCount);
                _columnCount = columnCount;
            }
        }

        /// <summary>
        /// Returns the number of rows. 
        /// </summary>
        private int GetRowCount()
        {
            int rows = 0;
            foreach (TableRowGroup group in ((Table)Owner).RowGroups)
            {
                rows += group.Rows.Count;
            }
            return rows;
        }

        /// <summary>
        /// Returns the number of columns. 
        /// </summary>
        private int GetColumnCount()
        {
            return ((Table)Owner).ColumnCount;
        }

        private int _rowCount;
        private int _columnCount;

        //-------------------------------------------------------------------
        //
        //  IGridProvider Members
        //
        //-------------------------------------------------------------------

        #region IGridProvider Members

        /// <summary>
        /// Returns the provider for the element that is located at the row and 
        /// column location requested by the client.
        /// </summary>
        IRawElementProviderSimple IGridProvider.GetItem(int row, int column)
        {
            if (row < 0 || row >= ((IGridProvider)this).RowCount)
            {
                throw new ArgumentOutOfRangeException("row");
            }
            if (column < 0 || column >= ((IGridProvider)this).ColumnCount)
            {
                throw new ArgumentOutOfRangeException("column");
            }

            int currentRow = 0;

            Table table = (Table)Owner;
            foreach (TableRowGroup group in table.RowGroups)
            {
                if (currentRow + group.Rows.Count < row)
                {
                    currentRow += group.Rows.Count;
                }
                else
                {
                    foreach (TableRow tableRow in group.Rows)
                    {
                        if (currentRow == row)
                        {
                            foreach (TableCell cell in tableRow.Cells)
                            {
                                if (cell.ColumnIndex <= column && cell.ColumnIndex + cell.ColumnSpan > column)
                                {
                                    return ProviderFromPeer(CreatePeerForElement(cell));
                                }
                            }
                            // check spanned cells
                            foreach (TableCell cell in tableRow.SpannedCells)
                            {
                                if (cell.ColumnIndex <= column && cell.ColumnIndex + cell.ColumnSpan > column)
                                {
                                    return ProviderFromPeer(CreatePeerForElement(cell));
                                }
                            }
                        }
                        else
                        {
                            currentRow++;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the number of rows in the grid at the time this was requested.
        /// </summary>
        int IGridProvider.RowCount
        {
            get { return _rowCount; }
        }

        /// <summary>
        /// Returns the number of columns in the grid at the time this was requested.
        /// </summary>
        int IGridProvider.ColumnCount
        {
            get { return _columnCount; }
        }

        #endregion IGridProvider Members
    }
}
