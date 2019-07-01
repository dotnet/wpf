// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Documents;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Remove some TableCells from TableRow.
    /// </summary>
    public class RemoveTableCellFromTableRowAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public TableRow TableRow { get; set; }

        public int RemoveMethod { get; set; }

        public int RemovePosition { get; set; }

        #endregion

        #region Override Members

        public override bool CanPerform()
        {
            return TableRow.Cells.Count > 0;
        }

        public override void Perform()
        {
            RemovePosition %= TableRow.Cells.Count;
            switch (RemoveMethod % 4)
            {
                case 0:
                    TableCell cell = TableRow.Cells[RemovePosition];
                    TableRow.Cells.Remove(cell);
                    break;
                case 1:
                    TableRow.Cells.RemoveAt(RemovePosition);
                    break;
                case 2:
                    TableRow.Cells.RemoveRange(RemovePosition, 1);
                    break;
                case 3:
                    TableRow.Cells.Clear();
                    break;
            }
        }

        #endregion
    }
}
