// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40
    [TargetTypeAttribute(typeof(DataGrid))]
    public class DataGridKeyboardNavigationAction : ApplyDataGridItemsSourceAction
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 KeyboardingIndex { get; set; }

        public override void Perform()
        {
            SetupDataGridItemsSource();
            switch (KeyboardingIndex)
            {
                case 0:
                    HomelessTestHelpers.KeyPress(System.Windows.Input.Key.Tab);
                    break;
                case 1:
                    HomelessTestHelpers.KeyPress(System.Windows.Input.Key.LeftShift, Key.Tab);
                    break;
                case 2:
                    HomelessTestHelpers.KeyPress(System.Windows.Input.Key.LeftCtrl, Key.Tab);
                    break;
                case 3:
                    HomelessTestHelpers.KeyPress(System.Windows.Input.Key.LeftCtrl, Key.LeftShift, Key.Tab);
                    break;
                case 4:
                    HomelessTestHelpers.KeyPress(System.Windows.Input.Key.Up);
                    break;
                case 5:
                    HomelessTestHelpers.KeyPress(System.Windows.Input.Key.Down);
                    break;
                case 6:
                    HomelessTestHelpers.KeyPress(System.Windows.Input.Key.PageUp);
                    break;
                case 7:
                    HomelessTestHelpers.KeyPress(System.Windows.Input.Key.PageDown);
                    break;
                case 8:
                    HomelessTestHelpers.KeyPress(System.Windows.Input.Key.Left);
                    break;
                case 9:
                    HomelessTestHelpers.KeyPress(System.Windows.Input.Key.Right);
                    break;
                case 10:
                    HomelessTestHelpers.KeyPress(System.Windows.Input.Key.Home);
                    break;
                case 11:
                    HomelessTestHelpers.KeyPress(System.Windows.Input.Key.End);
                    break;
            }
        }
    }

    [TargetTypeAttribute(typeof(DataGrid))]
    public class CopyFromDataGridAction : ApplyDataGridItemsSourceAction
    {
        public Double ItemRate { get; set; }
        public Double ColumnRate { get; set; }

        public override void Perform()
        {
            SetupDataGridItemsSource();
            if (DataGrid.Items.Count != 0)
            {
                if (DataGrid.SelectionMode == DataGridSelectionMode.Extended)
                {
                    if (DataGrid.SelectionUnit == DataGridSelectionUnit.FullRow)
                    {
                        DataGrid.SelectAll();
                    }
                    else
                    {
                        DataGrid.SelectAllCells();
                    }
                }
                else
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

                    if (DataGrid.SelectionUnit == DataGridSelectionUnit.FullRow)
                    {
                        DataGrid.SelectedIndex = itemIndex;
                    }
                    else
                    {
                        if (DataGrid.Columns.Count > 0)
                        {
                            int colIndex = (int)(ColumnRate * DataGrid.Columns.Count);
                            if (colIndex < 0)
                            {
                                colIndex = 0;
                            }
                            else if (colIndex >= DataGrid.Columns.Count)
                            {
                                colIndex = DataGrid.Columns.Count - 1;
                            }

                            DataGridCellInfo cellInfo = new DataGridCellInfo(DataGrid.Items[itemIndex], DataGrid.Columns[colIndex]);
                            DataGrid.SelectedCells.Add(cellInfo);
                        }
                    }
                }

                try
                {
                    //HomelessTestHelpers.KeyPress(Key.LeftCtrl, Key.C);
                    ApplicationCommands.Copy.Execute(null, DataGrid);
                }
                catch (COMException exception)
                {
                    string message = exception.Message;
                    //HACK:Work around Bug 843004 
                    if (message == null || !message.Contains("OpenClipboard Failed"))
                    {
                        // Context is lost for the current exception because of the catch
                        // Calling the method again so that debugger breaks at the throwing location
                        ApplicationCommands.Copy.Execute(null, DataGrid);
                    }
                }
            }
        }
    }
#endif
}
