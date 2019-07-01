// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
#if TESTBUILD_CLR40
    class DataGridRowFactory : DiscoverableFactory<DataGridRow>
    {
        public ContentControl Header { get; set; }

        public override DataGridRow Create(DeterministicRandom random)
        {
            DataGridRow dataGridRow = new DataGridRow();
            dataGridRow.Header = Header;
            return dataGridRow;
        }
    }
#endif
}
