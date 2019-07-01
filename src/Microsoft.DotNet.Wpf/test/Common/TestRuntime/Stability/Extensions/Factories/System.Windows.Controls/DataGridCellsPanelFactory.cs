// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
#if TESTBUILD_CLR40
    /// <summary>
    /// A factory which create DataGridCellsPanel.
    /// </summary>
    internal class DataGridCellsPanelFactory : PanelFactory<DataGridCellsPanel>
    {
        /// <summary>
        /// Create a DataGridCellsPanel.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override DataGridCellsPanel Create(DeterministicRandom random)
        {
            DataGridCellsPanel dataGridCellsPanel = new DataGridCellsPanel();

            ApplyCommonProperties(dataGridCellsPanel, random);

            return dataGridCellsPanel;
        }
    }
#endif
}
