// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Undo unit for resizing columns
//

using MS.Internal.Documents;


namespace System.Windows.Documents
{
    internal class ColumnResizeUndoUnit : ParentUndoUnit
    {
        #region Constructors

        internal ColumnResizeUndoUnit(TextPointer textPointerTable, int columnIndex, double[] columnWidths, double resizeAmount) : base("ColumnResize")
        
        {
            _textContainer = textPointerTable.TextContainer;
            _cpTable = _textContainer.Start.GetOffsetToPosition(textPointerTable);
            _columnWidths = columnWidths;
            _columnIndex = columnIndex;
            _resizeAmount = resizeAmount;            
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Perform the appropriate action for this unit.  If this is a parent undo unit, the
        /// parent must create an appropriate parent undo unit to contain the redo units.
        /// </summary>
        public override void Do() 
        {
            UndoManager undoManager;        
            IParentUndoUnit redo;
            TextPointer textPointerTable;
            Table table;
    
            undoManager = TopContainer as UndoManager;
            redo = null;

            textPointerTable = new TextPointer(_textContainer.Start, _cpTable, LogicalDirection.Forward); 
            table = (Table) textPointerTable.Parent;  

            _columnWidths[_columnIndex] -= _resizeAmount;
            if(_columnIndex < table.ColumnCount - 1)
            {
                _columnWidths[_columnIndex + 1] += _resizeAmount;
            }

            if(undoManager != null && undoManager.IsEnabled)
            {
                redo = new ColumnResizeUndoUnit(textPointerTable, _columnIndex, _columnWidths, -_resizeAmount);
                undoManager.Open(redo);
            }    

            TextRangeEditTables.EnsureTableColumnsAreFixedSize(table, _columnWidths);
    
            if(redo != null)
            {
                undoManager.Close(redo, UndoCloseAction.Commit);
            }
        }

        #endregion Public Methods


        #region Private Data

        private TextContainer _textContainer;
        private double[] _columnWidths;
        private int _cpTable;
        private int _columnIndex;
        private double _resizeAmount;

        #endregion Private Data
    }
}
