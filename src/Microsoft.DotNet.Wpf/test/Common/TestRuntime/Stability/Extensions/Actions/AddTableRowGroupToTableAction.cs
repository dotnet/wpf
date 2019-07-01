// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Documents;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Add or Insert a TableRowGroup to Table.
    /// </summary>
    public class AddTableRowGroupToTableAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Table Table { get; set; }

        public TableRowGroup TableRowGroup { get; set; }

        public bool IsInsertRowGroup { get; set; }

        public int InsertPosition { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            if (IsInsertRowGroup)
            {
                if (Table.RowGroups.Count > 0)
                {
                    InsertPosition %= Table.RowGroups.Count;
                }
                else
                {
                    InsertPosition = 0;
                }

                Table.RowGroups.Insert(InsertPosition, TableRowGroup);
            }
            else
            {
                Table.RowGroups.Add(TableRowGroup);
            }
        }

        #endregion
    }
}
