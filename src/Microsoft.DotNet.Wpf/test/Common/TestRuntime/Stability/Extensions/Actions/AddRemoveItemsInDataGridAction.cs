// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using Microsoft.Test.Stability.Extensions.Constraints;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40   
    [TargetTypeAttribute(typeof(DataGrid))]
    public class AddRemoveItemsInDataGridAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Window Window { get; set; }
        public DataGrid DataGrid { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 ItemsCount { get; set; }
        public Double Rate { get; set; }
        public Boolean RemoveByIndex { get; set; }
        public Boolean Add { get; set; }
        public List<ConstrainedDataGridItem> ItemList { get; set; }
        public override void Perform()
        {
            PropertyInfo[] props = typeof(ConstrainedDataGridItem).GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                DataGridColumn col = null;
                if (props[i].PropertyType == typeof(Boolean))
                {
                    col = new DataGridCheckBoxColumn();
                }
                if (props[i].PropertyType == typeof(Enumerations))
                {
                    col = new DataGridComboBoxColumn();
                    Array array = Enum.GetNames(typeof(Enumerations));
                    ((DataGridComboBoxColumn)col).ItemsSource = array;
                }
                if (props[i].PropertyType == typeof(Uri))
                {
                    col = new DataGridHyperlinkColumn();
                    ((DataGridHyperlinkColumn)col).ContentBinding = new Binding(props[i].Name);
                }
                else
                {
                    col = new DataGridTextColumn();
                }
                DataGrid.Columns.Add(col);
            }
            Window.Content = DataGrid;
            DataGrid.Measure(Window.RenderSize);

            if (ItemList.Count != 0)
            {
                for (int i = 0; i < ItemsCount; i++)
                {
                    AddItem(DataGrid, Add, i, ItemList[(int)(Rate * ItemList.Count)]);
                }
            }

            if (DataGrid.Items.Count != 0)
            {
                if (RemoveByIndex)
                {
                    DataGrid.Items.RemoveAt((int)(Rate * DataGrid.Items.Count));
                }
                else
                {
                    DataGrid.Items.Remove(DataGrid.Items[(int)(Rate * DataGrid.Items.Count)]);
                }
            }

            DataGrid.UpdateLayout();
        }

        private void AddItem(DataGrid dataGrid, bool add, int index, object item)
        {
            if (add)
            {
                dataGrid.Items.Add(item);
            }
            else
            {
                dataGrid.Items.Insert(index, item);
            }
        }
    }    
#endif
}
