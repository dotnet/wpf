// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
#if TESTBUILD_CLR40
    /// <summary>
    /// A factory which create DataGridTemplateColumn.
    /// </summary>
    internal class DataGridTemplateColumnFactory : DataGridColumnFactory<DataGridTemplateColumn>
    {
        /// <summary>
        /// Create a DataGridTemplateColumn.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override DataGridTemplateColumn Create(DeterministicRandom random)
        {
            DataGridTemplateColumn dataGridTemplate = new DataGridTemplateColumn();

            ApplyDataGridColumnProperties(dataGridTemplate);

            return dataGridTemplate;
        }
    }
#endif
}
