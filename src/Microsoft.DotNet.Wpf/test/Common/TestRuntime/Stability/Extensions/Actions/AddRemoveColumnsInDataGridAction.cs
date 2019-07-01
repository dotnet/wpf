// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40
    [TargetTypeAttribute(typeof(DataGrid))]
    public class AddRemoveColumnsInDataGridAction : ApplyDataGridItemsSourceAction
    {
        public Double Rate { get; set; }
        public Boolean RemoveByIndex { get; set; }
        public DataGridCheckBoxColumn DataGridCheckBoxColumn { get; set; }
        public DataGridComboBoxColumn DataGridComboBoxColumn { get; set; }
        public DataGridTextColumn DataGridTextColumn { get; set; }
        public DataGridHyperlinkColumn DataGridHyperlinkColumn { get; set; }
        public DataGridTemplateColumn DataGridTemplateColumn { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 ColumnSwitchIndex { get; set; }
        public Boolean Add { get; set; }
        public Boolean Clear { get; set; }

        public override void Perform()
        {
            SetupDataGridItemsSource();
            PropertyInfo[] props = typeof(ConstrainedDataGridItem).GetProperties();
            switch (ColumnSwitchIndex)
            {
                case 0:
                    for (int i = 0; i < props.Length; i++)
                    {
                        if (props[i].PropertyType == typeof(Boolean))
                        {
                            if (Add)
                            {
                                DataGrid.Columns.Add(DataGridCheckBoxColumn);
                            }
                            else
                            {
                                DataGrid.Columns.Insert((int)(Rate * DataGrid.Columns.Count), DataGridCheckBoxColumn);
                            }
                        }
                    }
                    break;
                case 1:
                    for (int i = 0; i < props.Length; i++)
                    {
                        if (props[i].PropertyType == typeof(Enumerations))
                        {
                            List<String> src = new List<String>();
                            foreach (string s in Enum.GetNames(typeof(Enumerations)))
                            {
                                src.Add(s);
                            }
                            DataGridComboBoxColumn.ItemsSource = src;
                            if (Add)
                            {
                                DataGrid.Columns.Add(DataGridComboBoxColumn);
                            }
                            else
                            {
                                DataGrid.Columns.Insert((int)(Rate * DataGrid.Columns.Count), DataGridComboBoxColumn);
                            }
                        }
                    }
                    break;
                case 2:
                    for (int i = 0; i < props.Length; i++)
                    {
                        if (props[i].PropertyType == typeof(Uri))
                        {
                            DataGridHyperlinkColumn.ContentBinding = new Binding(props[i].Name);
                            if (Add)
                            {
                                DataGrid.Columns.Add(DataGridHyperlinkColumn);
                            }
                            else
                            {
                                DataGrid.Columns.Insert((int)(Rate * DataGrid.Columns.Count), DataGridHyperlinkColumn);
                            }

                        }
                    }
                    break;
                case 3:
                    for (int i = 0; i < props.Length; i++)
                    {
                        if (props[i].PropertyType == typeof(String))
                        {
                            if (Add)
                            {
                                DataGrid.Columns.Add(DataGridTextColumn);
                            }
                            else
                            {
                                DataGrid.Columns.Insert((int)(Rate * DataGrid.Columns.Count), DataGridTextColumn);
                            }
                        }
                    }
                    break;
                case 4:
                    if (Add)
                    {
                        DataGrid.Columns.Add(DataGridTemplateColumn);
                    }
                    else
                    {
                        DataGrid.Columns.Insert((int)(Rate * DataGrid.Columns.Count), DataGridTemplateColumn);
                    }
                    break;
            }

            if (DataGrid.Columns.Count != 0)
            {
                if (!Clear)
                {
                    int colIndex = (int)(Rate * DataGrid.Columns.Count);
                    if (RemoveByIndex)
                    {
                        DataGrid.Columns.RemoveAt(colIndex);
                    }
                    else
                    {
                        DataGrid.Columns.Remove(DataGrid.Columns[colIndex]);
                    }
                }
                else
                {
                    DataGrid.Columns.Clear();
                }
            }

            DataGrid.UpdateLayout();
        }
    }

#endif
}
