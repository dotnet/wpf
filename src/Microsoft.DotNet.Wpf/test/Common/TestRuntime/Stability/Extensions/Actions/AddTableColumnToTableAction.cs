// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Documents;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Add or Insert a TableColumn to Table.
    /// </summary>
    public class AddTableColumnToTableAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Table Table { get; set; }

        public TableColumn TableColumn { get; set; }

        public bool IsInsertColumn { get; set; }

        public int InsertPosition { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            if (IsInsertColumn)
            {
                if (Table.Columns.Count > 0)
                {
                    InsertPosition %= Table.Columns.Count;
                }
                else
                {
                    InsertPosition = 0;
                }

                Table.Columns.Insert(InsertPosition, TableColumn);
            }
            else
            {
                Table.Columns.Add(TableColumn);
            }
        }

        #endregion
    }
}
