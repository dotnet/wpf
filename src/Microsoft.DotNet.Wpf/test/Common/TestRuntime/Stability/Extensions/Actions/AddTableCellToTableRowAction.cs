// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Documents;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Add or Insert a TableCell to TableRow.
    /// </summary>
    public class AddTableCellToTableRowAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public TableRow TableRow { get; set; }

        public TableCell TableCell { get; set; }

        public bool IsInsertCell { get; set; }

        public int InsertPosition { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            if (IsInsertCell)
            {
                if (TableRow.Cells.Count > 0)
                {
                    InsertPosition %= TableRow.Cells.Count;
                }
                else
                {
                    InsertPosition = 0;
                }

                TableRow.Cells.Insert(InsertPosition, TableCell);
            }
            else
            {
                TableRow.Cells.Add(TableCell);
            }
        }

        #endregion
    }
}
