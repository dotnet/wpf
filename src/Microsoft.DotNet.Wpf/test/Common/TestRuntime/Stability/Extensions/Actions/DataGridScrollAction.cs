// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40
    public class ScrollIntoViewInDataGridAction : ApplyDataGridItemsSourceAction
    {
        public ScrollBarVisibility VerticalScrollBarVisibility { get; set; }
        public ScrollBarVisibility HorizontalScrollBarVisibility { get; set; }
        public Double ItemRate { get; set; }
        public Double ColumnRate { get; set; }

        public override void Perform()
        {
            SetupDataGridItemsSource();
            DataGrid.VerticalScrollBarVisibility = VerticalScrollBarVisibility;
            DataGrid.HorizontalScrollBarVisibility = HorizontalScrollBarVisibility;
            if (DataGrid.Items.Count != 0 && DataGrid.Columns.Count != 0)
            {
                int itemIndex = (int)(ItemRate * DataGrid.Items.Count);
                if (itemIndex < 0)
                {
                    itemIndex = 0;
                }
                else if (itemIndex >= DataGrid.Items.Count)
                {
                    itemIndex = DataGrid.Items.Count - 1;
                }

                int colIndex = (int)(ColumnRate * DataGrid.Columns.Count);
                if (colIndex < 0)
                {
                    colIndex = 0;
                }
                else if (colIndex >= DataGrid.Columns.Count)
                {
                    colIndex = DataGrid.Columns.Count - 1;
                }

                object item = DataGrid.Items[itemIndex];
                DataGridColumn column = DataGrid.Columns[colIndex];
                DataGrid.ScrollIntoView(item, column);
            }
        }
    }
#endif
}
