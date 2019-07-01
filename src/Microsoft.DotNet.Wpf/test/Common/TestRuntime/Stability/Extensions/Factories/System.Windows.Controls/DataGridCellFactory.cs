// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
#if TESTBUILD_CLR40
    /// <summary>
    /// A factory which create DataGridCell.
    /// </summary>
    internal class DataGridCellFactory : ContentControlFactory<DataGridCell>
    {
        /// <summary>
        /// Create a DataGridCell.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override DataGridCell Create(DeterministicRandom random)
        {
            DataGridCell dataGridCell = new DataGridCell();

            ApplyContentControlProperties(dataGridCell);
            dataGridCell.IsEditing = random.NextBool();
            dataGridCell.IsSelected = random.NextBool();

            return dataGridCell;
        }
    }
#endif
}
