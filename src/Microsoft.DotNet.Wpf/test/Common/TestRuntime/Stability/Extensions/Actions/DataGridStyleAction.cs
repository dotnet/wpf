// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40
    [TargetTypeAttribute(typeof(DataGrid))]
    public class StyleDataGridColumnAction : ApplyDataGridItemsSourceAction
    {
        public Style ColumnHeaderStyle { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double ColumnHeaderHeight { get; set; }
        public Double Rate { get; set; }
        public Boolean HeaderTemplateIsNull { get; set; }

        public override void Perform()
        {
            Window.Content = DataGrid;
            SetupDataGridItemsSource();
            if (ColumnHeaderStyle != null)
            {
                ColumnHeaderStyle.Setters.Add(new Setter(DataGrid.ColumnHeaderHeightProperty, ColumnHeaderHeight));
            }
            DataGrid.ColumnHeaderStyle = ColumnHeaderStyle;

            if (DataGrid.Columns.Count != 0)
            {
                DataGridColumn col = DataGrid.Columns[(int)(Rate * DataGrid.Columns.Count)];
                col.HeaderTemplate = TemplateGenerator.CreateDataGridColumnHeaderTemplate();
                if (HeaderTemplateIsNull)
                {
                    col.HeaderTemplate = null;
                }
                if (col.HeaderTemplate == null)
                {
                    col.HeaderTemplateSelector = new DataGridColumnHeaderTemplateSelector();
                }
            }
        }
    }

    [TargetTypeAttribute(typeof(DataGrid))]
    public class StyleDataGridRowAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Window Window { get; set; }
        public DataGrid DataGrid { get; set; }
        public Style RowStyle { get; set; }
        public Brush RowBackground { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double RowHeight { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double RowHeaderWidth { get; set; }
        public Style RowHeaderStyle { get; set; }
        public DataGridRow DataGridRow { get; set; }
        public Double Rate { get; set; }
        public Boolean HeaderTemplateIsNull { get; set; }

        public override void Perform()
        {
            Window.Content = DataGrid;
            if (RowStyle != null)
            {
                RowStyle.Setters.Add(new Setter(DataGrid.RowBackgroundProperty, RowBackground));
                RowStyle.Setters.Add(new Setter(DataGrid.RowHeaderWidthProperty, RowHeaderWidth));
                RowStyle.Setters.Add(new Setter(DataGrid.RowHeightProperty, RowHeight));
                DataGrid.RowStyle = RowStyle;
            }
            if (RowStyle == null)
            {
                DataGridRowStyleSelector selector = new DataGridRowStyleSelector();
                DataGrid.RowStyleSelector = selector;
            }
            if (RowHeaderStyle != null)
            {
                RowHeaderStyle.Setters.Add(new Setter(DataGrid.RowHeaderWidthProperty, RowHeaderWidth));
            }
            DataGrid.RowHeaderStyle = RowHeaderStyle;

            if (DataGrid.Items.Count != 0)
            {
                DataGrid.ScrollIntoView(DataGrid.Items[(int)(Rate * DataGrid.Items.Count)]);
                DataGridRow = DataGrid.ItemContainerGenerator.ContainerFromIndex((int)(Rate * DataGrid.Items.Count)) as DataGridRow;
                DataGridRow.HeaderTemplate = TemplateGenerator.CreateDataGridRowHeaderTemplate();
                if (HeaderTemplateIsNull)
                {
                    DataGridRow.HeaderTemplate = null; 
                }
                if (DataGridRow.HeaderTemplate == null)
                {
                    DataGridRow.HeaderTemplateSelector = new DataGridRowHeaderTemplateSelector();
                }

                DataGridRow.ItemsPanel = CreateDataGridRowItemsPanel();
            }
        }

        public Brush Background { get; set; }
        public Boolean IsNull { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 UniformGridColumnsCount { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 UniformGridRowsCount { get; set; }
        private ItemsPanelTemplate CreateDataGridRowItemsPanel()
        {
            ItemsPanelTemplate itemsPanel = null;
            if (!IsNull)
            {
                FrameworkElementFactory factoryUnigrid = new FrameworkElementFactory(typeof(UniformGrid));
                factoryUnigrid.SetValue(UniformGrid.ColumnsProperty, UniformGridColumnsCount);
                factoryUnigrid.SetValue(UniformGrid.RowsProperty, UniformGridRowsCount);
                factoryUnigrid.SetValue(UniformGrid.BackgroundProperty, Background);
                itemsPanel = new ItemsPanelTemplate(factoryUnigrid);
            }
            return itemsPanel;
        }
    }

    public class DataGridColumnHeaderTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item != null && item is DataGridColumn)
            {
                return TemplateGenerator.CreateDataGridRowHeaderTemplate();
            }
            return base.SelectTemplate(item, container);
        }
    }

    public class DataGridRowHeaderTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item != null && item is DataGridRow)
            {
                return TemplateGenerator.CreateDataGridRowHeaderTemplate();
            }
            return base.SelectTemplate(item, container);
        }
    }

    public class DataGridRowStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            Style style = new Style();
            style.TargetType = typeof(DataGridRow);
            DataGrid dataGrid = ItemsControl.ItemsControlFromItemContainer(container) as DataGrid;
            if (dataGrid != null)
            {
                int index = dataGrid.ItemContainerGenerator.IndexFromContainer(container);
                style.Setters.Add(SelectSetter(index));
                return style;
            }
            return base.SelectStyle(item, container);
        }

        /// <summary>
        /// Define Setter applied by the Background color according to index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private Setter SelectSetter(int index)
        {
            Setter setter = new Setter();
            setter.Property = DataGridRow.BackgroundProperty;
            //HACK: The class DataGridRowStyleSelector which inherits from StyleSelector cannot inherit from another class DiscoverableFactory to consume factories or constraints to create data.
            // Only Factories inheriting from DiscoverableFactory or Actions inheriting from SimpleDiscoverableAction can consume Factories or Constraints to create Properties’ value. 
            if (index % 2 == 0)
            {
                setter.Value = Brushes.Blue;
            }
            else
            {
                setter.Value = Brushes.Yellow;
            }
            return setter;
        }
    }

    //HACK: Only Factories inheriting from DiscoverableFactory or Actions inheriting from SimpleDiscoverableAction can consume Factories or Constraints to create Properties’ value. 
    public class TemplateGenerator
    {
        public static DataTemplate CreateDataGridColumnHeaderTemplate()
        {
            DataTemplate template = new DataTemplate(typeof(ConstrainedDataGridItem));

            //Create FrameworkElementFactory for StackPanel.
            FrameworkElementFactory factoryStack = new FrameworkElementFactory(typeof(StackPanel));
            factoryStack.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);

            //Create FrameworkElementFactory for ListBox.
            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(ListBox));
            Array values = Enum.GetValues(typeof(Enumerations));
            factory.SetValue(ListBox.ItemsSourceProperty, values);
            factoryStack.AppendChild(factory);

            template.VisualTree = factoryStack;
            return template;
        }

        public static DataTemplate CreateDataGridRowHeaderTemplate()
        {
            DataTemplate template = new DataTemplate(typeof(ConstrainedDataGridItem));

            //Create FrameworkElementFactory for StackPanel.
            FrameworkElementFactory factoryStack = new FrameworkElementFactory(typeof(StackPanel));
            factoryStack.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);

            //Create FrameworkElementFactory for Rectangle.
            FrameworkElementFactory factoryRectangle = new FrameworkElementFactory(typeof(Rectangle));
            factoryRectangle.SetValue(Rectangle.MarginProperty, new Thickness(2, 2, 2, 2));
            factoryRectangle.SetValue(Rectangle.StrokeProperty, Brushes.Purple);
            factoryRectangle.SetValue(Rectangle.FillProperty, Brushes.RoyalBlue);
            factoryRectangle.SetValue(Rectangle.ClipToBoundsProperty, new Binding("BooleanProp"));
            factoryStack.AppendChild(factoryRectangle);

            //Create FrameworkElementFactory for TextBlock.
            FrameworkElementFactory factoryTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            factoryTextBlock.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            factoryTextBlock.SetValue(TextBlock.TextProperty, new Binding("StringProp"));
            factoryStack.AppendChild(factoryTextBlock);

            template.VisualTree = factoryStack;
            return template;
        }
    }
#endif
}

