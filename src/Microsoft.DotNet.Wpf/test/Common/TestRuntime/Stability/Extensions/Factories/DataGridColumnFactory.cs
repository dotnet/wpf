// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
#if TESTBUILD_CLR40
    [TargetTypeAttribute(typeof(DataGridColumn))]
    abstract class DataGridColumnFactory<T> : DiscoverableFactory<T> where T : DataGridColumn
    {
        public Style CellStyle { get; set; }
        public Style DragIndicatorStyle { get; set; }
        public ContentControl Header { get; set; }
        public Style HeaderStyle { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double MaxWidth { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double MinWidth { get; set; }        
        public DataGridLength Width { get; set; }

        protected void ApplyDataGridColumnProperties(T dataGridColumn)
        {
            dataGridColumn.CellStyle = CellStyle;
            dataGridColumn.DragIndicatorStyle = DragIndicatorStyle;
            dataGridColumn.Header = Header;
            dataGridColumn.HeaderStyle = HeaderStyle;
            dataGridColumn.MaxWidth = MaxWidth;
            dataGridColumn.MinWidth = MinWidth;
            dataGridColumn.Width = Width;
        }
    }

    abstract class DataGridBoundColumnFactory<T> : DataGridColumnFactory<T> where T : DataGridBoundColumn
    {
        public Style EditingElementStyle { get; set; }
        public Style ElementStyle { get; set; }

        protected void ApplyDataGridBoundColumnProperties(T dataGridBoundColumn)
        {
            ApplyDataGridColumnProperties(dataGridBoundColumn);
            dataGridBoundColumn.EditingElementStyle = EditingElementStyle;
            dataGridBoundColumn.ElementStyle = ElementStyle;
        }
    }
    
    // ComboBox isn't a Bound Column, but it's very similar.
    class DataGridComboBoxColumnFactory : DataGridColumnFactory<DataGridComboBoxColumn>
    {
        public Style EditingElementStyle { get; set; }
        public Style ElementStyle { get; set; }

        protected void ApplyDataGridComboBoxColumnFactoryProperties(DataGridComboBoxColumn dataGridComboBoxColumn)
        {
            ApplyDataGridColumnProperties(dataGridComboBoxColumn);
            dataGridComboBoxColumn.EditingElementStyle = EditingElementStyle;
            dataGridComboBoxColumn.ElementStyle = ElementStyle;
        }
        
        public override DataGridComboBoxColumn Create(DeterministicRandom random)
        {
            DataGridComboBoxColumn dataGridComboBoxColumn = new DataGridComboBoxColumn();
            ApplyDataGridComboBoxColumnFactoryProperties(dataGridComboBoxColumn);
            return dataGridComboBoxColumn;
        }
    }

    class DataGridTextColumnFactory : DataGridBoundColumnFactory<DataGridTextColumn>
    {
        public override DataGridTextColumn Create(DeterministicRandom random)
        {
            DataGridTextColumn dataGridTextColumn = new DataGridTextColumn();
            ApplyDataGridBoundColumnProperties(dataGridTextColumn);
            return dataGridTextColumn;
        }
    }

    class DataGridCheckBoxColumnFactory : DataGridBoundColumnFactory<DataGridCheckBoxColumn>
    {
        public override DataGridCheckBoxColumn Create(DeterministicRandom random)
        {
            DataGridCheckBoxColumn dataGridCheckBoxColumn = new DataGridCheckBoxColumn();
            ApplyDataGridBoundColumnProperties(dataGridCheckBoxColumn);
            return dataGridCheckBoxColumn;
        }
    }
    
    class DataGridHyperlinkColumnFactory : DataGridBoundColumnFactory<DataGridHyperlinkColumn>
    {
        public override DataGridHyperlinkColumn Create(DeterministicRandom random)
        {
            DataGridHyperlinkColumn dataGridHyperlinkColumn = new DataGridHyperlinkColumn();
            ApplyDataGridBoundColumnProperties(dataGridHyperlinkColumn);
            return dataGridHyperlinkColumn;
        }
    }
#endif
}
