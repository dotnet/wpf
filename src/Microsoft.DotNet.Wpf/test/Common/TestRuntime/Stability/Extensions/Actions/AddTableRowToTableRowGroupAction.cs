// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Documents;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Add or Insert a TableRow to TableRowGroup.
    /// </summary>
    public class AddTableRowToTableRowGroupAction : SimpleDiscoverableAction
    {
        #region Public Members
        
        public TableRow TableRow { get; set; }

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public TableRowGroup TableRowGroup { get; set; }

        public bool IsInsertRow { get; set; }

        public int InsertPosition { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            if (IsInsertRow)
            {
                if (TableRowGroup.Rows.Count > 0)
                {
                    InsertPosition %= TableRowGroup.Rows.Count;
                }
                else
                {
                    InsertPosition = 0;
                }

                TableRowGroup.Rows.Insert(InsertPosition, TableRow);
            }
            else
            {
                TableRowGroup.Rows.Add(TableRow);
            }
        }

        #endregion
    }
}
