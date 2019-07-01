// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Documents;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Remove some TableRows from TableRowGroup.
    /// </summary>
    public class RemoveTableRowFromTableRowGroupAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public TableRowGroup TableRowGroup { get; set; }

        public int RemoveMethod { get; set; }

        public int RemovePosition { get; set; }

        #endregion

        #region Override Members

        public override bool CanPerform()
        {
            return TableRowGroup.Rows.Count > 0;
        }

        public override void Perform()
        {
            RemovePosition %= TableRowGroup.Rows.Count;
            switch (RemoveMethod % 4)
            {
                case 0:
                    TableRow row = TableRowGroup.Rows[RemovePosition];
                    TableRowGroup.Rows.Remove(row);
                    break;
                case 1:
                    TableRowGroup.Rows.RemoveAt(RemovePosition);
                    break;
                case 2:
                    TableRowGroup.Rows.RemoveRange(RemovePosition, 1);
                    break;
                case 3:
                    TableRowGroup.Rows.Clear();
                    break;
            }
        }

        #endregion
    }
}
