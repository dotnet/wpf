// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Documents;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Remove some TableColumns from Table.
    /// </summary>
    public class RemoveTableColumnFromTableAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Table Table { get; set; }

        public int RemoveMethod { get; set; }

        public int RemovePosition { get; set; }

        #endregion

        #region Override Members

        public override bool CanPerform()
        {
            return Table.Columns.Count > 0;
        }

        public override void Perform()
        {
            RemovePosition %= Table.Columns.Count;
            switch (RemoveMethod % 4)
            {
                case 0:
                    TableColumn column = Table.Columns[RemovePosition];
                    Table.Columns.Remove(column);
                    break;
                case 1:
                    Table.Columns.RemoveAt(RemovePosition);
                    break;
                case 2:
                    Table.Columns.RemoveRange(RemovePosition, 1);
                    break;
                case 3:
                    Table.Columns.Clear();
                    break;
            }
        }

        #endregion
    }
}
