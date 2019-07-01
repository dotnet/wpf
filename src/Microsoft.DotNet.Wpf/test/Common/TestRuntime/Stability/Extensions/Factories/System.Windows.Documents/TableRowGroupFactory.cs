// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Collections.Generic;
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(TextElement))]
    internal class TableRowGroupFactory : TextElementFactory<TableRowGroup>
    {
        #region Public Members

        public List<TableRow> Rows { get; set; }

        #endregion

        #region Override Members

        public override TableRowGroup Create(DeterministicRandom random)
        {
            TableRowGroup rowGroup = new TableRowGroup();

            ApplyTextElementFactory(rowGroup, random);
            HomelessTestHelpers.Merge(rowGroup.Rows, Rows);

            return rowGroup;
        }

        #endregion
    }
}
