// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40
    public class SetDataGridSelectionAction : ApplyDataGridItemsSourceAction
    {
        public Boolean SelectedByIndex { get; set; }
        public Double Rate { get; set; }

        public override void Perform()
        {
            SetupDataGridItemsSource();
            if (!(DataGrid.SelectionUnit == DataGridSelectionUnit.FullRow && DataGrid.SelectionMode == DataGridSelectionMode.Single))
            {
                DataGrid.SelectAllCells();
                DataGrid.UnselectAllCells();
            }
            if (DataGrid.SelectionUnit != DataGridSelectionUnit.Cell)
            {
                if (DataGrid.SelectionMode != DataGridSelectionMode.Single)
                {
                    DataGrid.SelectAll();
                    DataGrid.UnselectAll(); 
                }
                else
                {
                    if (DataGrid.Items.Count != 0)
                    {
                        int itemIndex = (int)(Rate * DataGrid.Items.Count);
                        if (itemIndex < 0)
                        {
                            itemIndex = 0;
                        }
                        else if (itemIndex >= DataGrid.Items.Count)
                        {
                            itemIndex = DataGrid.Items.Count - 1;
                        }

                        if (SelectedByIndex)
                        {
                            DataGrid.SelectedIndex = itemIndex;
                        }
                        else
                        {
                            DataGrid.SelectedItem = DataGrid.Items[itemIndex];
                        }
                    }
                }
            }
        }
    }

    public class SetDataGridCurrentColumnAction : ApplyDataGridItemsSourceAction
    {
        public Double Rate { get; set; }

        public override void Perform()
        {
            SetupDataGridItemsSource();
            if (DataGrid.Columns.Count != 0)
            {
                int colIndex = (int)(Rate * DataGrid.Columns.Count);
                if (colIndex < 0)
                {
                    colIndex = 0;
                }
                else if (colIndex >= DataGrid.Columns.Count)
                {
                    colIndex = DataGrid.Columns.Count - 1;
                }

                DataGrid.CurrentColumn = DataGrid.Columns[colIndex];
            }
        }
    }

    public class SetDataGridCurrentItemAction : ApplyDataGridItemsSourceAction
    {
        public Double Rate { get; set; }

        public override void Perform()
        {
            SetupDataGridItemsSource();
            if (DataGrid.Items.Count != 0)
            {
                int itemIndex = (int)(Rate * DataGrid.Items.Count);
                if (itemIndex < 0)
                {
                    itemIndex = 0;
                }
                else if (itemIndex >= DataGrid.Items.Count)
                {
                    itemIndex = DataGrid.Items.Count - 1;
                }

                DataGrid.CurrentItem = DataGrid.Items[itemIndex];
            }
        }
    }   
#endif
}
