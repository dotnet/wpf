// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Collections.Generic;
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(Block))]      
    internal class TableFactory : BlockFactory<Table>
    {
        #region Public Members

        public List<TableColumn> Columns { get; set; }

        public List<TableRowGroup> RowGroups { get; set; }

        #endregion

        #region Override Members

        public override Table Create(DeterministicRandom random)
        {
            Table table = new Table();

            ApplyBlockProperties(table, random);
            table.CellSpacing = random.NextDouble() * 5;
            HomelessTestHelpers.Merge(table.Columns, Columns);
            HomelessTestHelpers.Merge(table.RowGroups, RowGroups);

            return table;
        }

        #endregion
    }
}
