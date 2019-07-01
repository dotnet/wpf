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
    public class AddRemoveInDataGridItemsSourceAction : ApplyDataGridItemsSourceAction
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 ItemsCount { get; set; }
        public Double Rate { get; set; }
        public Boolean RemoveByIndex { get; set; }
        public Boolean Add { get; set; }
        public List<ConstrainedDataGridItem> ItemList { get; set; }

        public override void Perform()
        {
            SetupDataGridItemsSource();
            ObservableCollection<ConstrainedDataGridItem> dataSource = DataGrid.ItemsSource as ObservableCollection<ConstrainedDataGridItem>;

            if (ItemList.Count != 0)
            {
                for (int i = 0; i < ItemsCount; i++)
                {
                    AddItem(dataSource, Add, i, ItemList[(int)(Rate * ItemList.Count)]);
                }
            }

            if (dataSource.Count != 0)
            {
                if (RemoveByIndex)
                {
                    dataSource.RemoveAt((int)(Rate * dataSource.Count));
                }
                else
                {
                    dataSource.Remove(dataSource[(int)(Rate * dataSource.Count)]);
                }
            }

            DataGrid.UpdateLayout();
        }

        private void AddItem(ObservableCollection<ConstrainedDataGridItem> collection, bool add, int index, object item)
        {
            if (add)
            {
                collection.Add(ItemList[(int)(Rate * ItemList.Count)]);
            }
            else
            {
                collection.Insert(index, ItemList[(int)(Rate * ItemList.Count)]);
            }
        }
    }
#endif
}
