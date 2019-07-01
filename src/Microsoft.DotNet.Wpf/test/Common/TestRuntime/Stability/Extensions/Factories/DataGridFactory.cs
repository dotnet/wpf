// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
#if TESTBUILD_CLR40
    /// <summary>
    /// A factory which create DataGrid.
    /// </summary>
    [TargetTypeAttribute(typeof(DataGrid))]
    internal class DataGridFactory : DiscoverableFactory<DataGrid>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a Brush to set DataGrid AlternatingRowBackground property.
        /// </summary>
        public Brush AlternatingRowBackground { get; set; }

        /// <summary>
        /// Gets or sets a value to set DataGrid AutoGenerateColumns property.
        /// </summary>
        public Boolean AutoGenerateColumns { get; set; }

        /// <summary>
        /// Gets or sets a DataGridClipboardCopyMode to set DataGrid ClipboardCopyMode property.
        /// </summary>
        public DataGridClipboardCopyMode ClipboardCopyMode { get; set; }

        /// <summary>
        /// Gets or sets a value to set DataGrid ColumnHeaderHeight property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double ColumnHeaderHeight { get; set; }

        /// <summary>
        /// Gets or sets a DataGridLength to set DataGrid ColumnWidth property.
        /// </summary>
        public DataGridLength ColumnWidth { get; set; }

        /// <summary>
        /// Gets or sets a Style to set DataGrid DragIndicatorStyle property.
        /// </summary>
        public Style DragIndicatorStyle { get; set; }

        /// <summary>
        /// Gets or sets a Style to set DataGrid DropLocationIndicatorStyle property.
        /// </summary>
        public Style DropLocationIndicatorStyle { get; set; }

        /// <summary>
        /// Gets or sets a value to set DataGrid FrozenColumnCount property.
        /// </summary>
        public Int32 FrozenColumnCount { get; set; }

        /// <summary>
        /// Gets or sets a DataGridGridLinesVisibility to set DataGrid GridLinesVisibility property.
        /// </summary>
        public DataGridGridLinesVisibility GridLinesVisibility { get; set; }

        /// <summary>
        /// Gets or sets a DataGridHeadersVisibility to set DataGrid HeadersVisibility property.
        /// </summary>
        public DataGridHeadersVisibility HeadersVisibility { get; set; }

        /// <summary>
        /// Gets or sets a Brush to set DataGrid HorizontalGridLinesBrush property.
        /// </summary>
        public Brush HorizontalGridLinesBrush { get; set; }

        /// <summary>
        /// Gets or sets a ScrollBarVisibility to set DataGrid HorizontalScrollBarVisibility property.
        /// </summary>
        public ScrollBarVisibility HorizontalScrollBarVisibility { get; set; }

        /// <summary>
        /// Gets or sets a value to set DataGrid MaxColumnWidth property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double MaxColumnWidth { get; set; }

        /// <summary>
        /// Gets or sets a value to set DataGrid MinColumnWidth property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double MinColumnWidth { get; set; }

        /// <summary>
        /// Gets or sets a value to set DataGrid MinRowHeight property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double MinRowHeight { get; set; }

        /// <summary>
        /// Gets or sets a Brush to set DataGrid RowBackground property.
        /// </summary>
        public Brush RowBackground { get; set; }

        /// <summary>
        /// Gets or sets a value to set DataGrid RowHeaderWidth property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double RowHeaderWidth { get; set; }

        /// <summary>
        /// Gets or sets a value to set DataGrid RowHeight property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double RowHeight { get; set; }

        /// <summary>
        /// Gets or sets a DataGridSelectionMode to set DataGrid SelectionMode property.
        /// </summary>
        public DataGridSelectionMode SelectionMode { get; set; }

        /// <summary>
        /// Gets or sets a DataGridSelectionUnit to set DataGrid SelectionUnit property.
        /// </summary>
        public DataGridSelectionUnit SelectionUnit { get; set; }

        /// <summary>
        /// Gets or sets a Brush to set DataGrid VerticalGridLinesBrush property.
        /// </summary>
        public Brush VerticalGridLinesBrush { get; set; }

        /// <summary>
        /// Gets or sets a ScrollBarVisibility to set DataGrid VerticalScrollBarVisibility property.
        /// </summary>
        public ScrollBarVisibility VerticalScrollBarVisibility { get; set; }

        /// <summary>
        /// Gets or sets a ControlTemplate to set DataGrid Template property.
        /// </summary>
        public ControlTemplate DataGridTemplate { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a DataGrid.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override DataGrid Create(DeterministicRandom random)
        {
            DataGrid dataGrid = new DataGrid();
            dataGrid.AlternatingRowBackground = AlternatingRowBackground;
            dataGrid.AutoGenerateColumns = AutoGenerateColumns;
            dataGrid.ClipboardCopyMode = ClipboardCopyMode;
            dataGrid.ColumnHeaderHeight = ColumnHeaderHeight;
            dataGrid.ColumnWidth = ColumnWidth;
            dataGrid.DragIndicatorStyle = DragIndicatorStyle;
            dataGrid.DropLocationIndicatorStyle = DropLocationIndicatorStyle;
            dataGrid.FrozenColumnCount = FrozenColumnCount;
            dataGrid.GridLinesVisibility = GridLinesVisibility;
            dataGrid.HeadersVisibility = HeadersVisibility;
            dataGrid.HorizontalGridLinesBrush = HorizontalGridLinesBrush;
            dataGrid.HorizontalScrollBarVisibility = HorizontalScrollBarVisibility;
            dataGrid.MaxColumnWidth = MaxColumnWidth;
            dataGrid.MinColumnWidth = MinColumnWidth;
            dataGrid.MinRowHeight = MinRowHeight;
            dataGrid.RowBackground = RowBackground;
            dataGrid.RowHeaderWidth = RowHeaderWidth;
            dataGrid.RowHeight = RowHeight;
            dataGrid.SelectionMode = SelectionMode;
            dataGrid.SelectionUnit = SelectionUnit;
            dataGrid.VerticalGridLinesBrush = VerticalGridLinesBrush;
            dataGrid.VerticalScrollBarVisibility = VerticalScrollBarVisibility;
            if (DataGridTemplate != null && (DataGridTemplate.TargetType == null || DataGridTemplate.TargetType == typeof(DataGrid)))
            {
                dataGrid.Template = DataGridTemplate;
            } 
            return dataGrid;
        }

        #endregion
    }
   
#endif
}
