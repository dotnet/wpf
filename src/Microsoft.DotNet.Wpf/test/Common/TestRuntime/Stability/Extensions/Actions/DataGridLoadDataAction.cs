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
    public abstract class ApplyDataGridItemsSourceAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Window Window { get; set; }
        public DataGrid DataGrid { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 Count { get; set; }
        //HACK: Hack issue in MakeList() method in DiscoverableActionSequencer 
        //This really needs to be argument driven. Some collections are big, some should be small.
        public List<ConstrainedDataGridItem> ItemsSource{ get; set; }

        protected void SetupDataGridItemsSource()
        {
            DataGrid.Items.Clear();
            ObservableCollection<ConstrainedDataGridItem> observableCollection = new ObservableCollection<ConstrainedDataGridItem>(ItemsSource);
            DataGrid.ItemsSource = observableCollection;
            
            DataGrid.Measure(Window.RenderSize);
            if (DataGrid.AutoGenerateColumns == false)
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
                DataGrid.UpdateLayout();
            }

            Window.Content = DataGrid;
        }
    }

    [TargetTypeAttribute(typeof(DataGrid))]
    public class ApplyLargeDataSetToDataGridAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Window Window { get; set; }
        public DataGrid DataGrid { get; set; }
        //HACK: Hack issue in MakeList() method in DiscoverableActionSequencer 
        //This really needs to be argument driven. Some collections are big, some should be small.
        public List<ConstrainedDataGridItem> ItemsSource { get; set; }

        public override void Perform()
        {
            Window.Content = DataGrid;
            DataGrid.Items.Clear();
            DataGrid.ItemsSource = ItemsSource;
            DataGrid.Measure(Window.RenderSize);
        }
    }
#endif
}
