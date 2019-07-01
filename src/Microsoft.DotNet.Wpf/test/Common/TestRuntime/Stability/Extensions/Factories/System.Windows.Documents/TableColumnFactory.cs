// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class TableColumnFactory : DiscoverableFactory<TableColumn>
    {
        #region Public Members

        public Brush Background { get; set; }

        public GridLength Width { get; set; }

        #endregion

        #region Override Members

        public override TableColumn Create(DeterministicRandom random)
        {
            TableColumn tableColumn = new TableColumn();

            tableColumn.Background = Background;
            tableColumn.Width = Width;

            return tableColumn;
        }

        #endregion
    }
}
