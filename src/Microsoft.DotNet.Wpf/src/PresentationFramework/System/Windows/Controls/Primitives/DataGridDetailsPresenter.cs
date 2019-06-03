// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Automation;

namespace System.Windows.Controls.Primitives
{
    public class DataGridDetailsPresenter : ContentPresenter
    {
        static DataGridDetailsPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DataGridDetailsPresenter), new FrameworkPropertyMetadata(typeof(DataGridDetailsPresenter)));
            ContentTemplateProperty.OverrideMetadata(typeof(DataGridDetailsPresenter), new FrameworkPropertyMetadata(OnNotifyPropertyChanged, OnCoerceContentTemplate));
            ContentTemplateSelectorProperty.OverrideMetadata(typeof(DataGridDetailsPresenter), new FrameworkPropertyMetadata(OnNotifyPropertyChanged, OnCoerceContentTemplateSelector));
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideMetadata(typeof(DataGridDetailsPresenter), new FrameworkPropertyMetadata(IsOffscreenBehavior.FromClip));

            EventManager.RegisterClassHandler(typeof(DataGridDetailsPresenter), MouseLeftButtonDownEvent, new MouseButtonEventHandler(OnAnyMouseLeftButtonDownThunk), true);
        }

        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        public DataGridDetailsPresenter()
        {
        }

        #region Automation

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new System.Windows.Automation.Peers.DataGridDetailsPresenterAutomationPeer(this);
        }

        #endregion

        #region Coercion

        /// <summary>
        ///     Coerces the ContentTemplate property.
        /// </summary>
        private static object OnCoerceContentTemplate(DependencyObject d, object baseValue)
        {
            var details = d as DataGridDetailsPresenter;
            var row = details.DataGridRowOwner;
            var dataGrid = row != null ? row.DataGridOwner : null;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                details, 
                baseValue, 
                ContentTemplateProperty,
                row, 
                DataGridRow.DetailsTemplateProperty,
                dataGrid, 
                DataGrid.RowDetailsTemplateProperty);
        }

        /// <summary>
        ///     Coerces the ContentTemplateSelector property.
        /// </summary>
        private static object OnCoerceContentTemplateSelector(DependencyObject d, object baseValue)
        {
            var details = d as DataGridDetailsPresenter;
            var row = details.DataGridRowOwner;
            var dataGrid = row != null ? row.DataGridOwner : null;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                details, 
                baseValue, 
                ContentTemplateSelectorProperty,
                row, 
                DataGridRow.DetailsTemplateSelectorProperty,
                dataGrid, 
                DataGrid.RowDetailsTemplateSelectorProperty);
        }

        #endregion

        #region Row Communication

        protected internal override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
        
            // DataGridRow.DetailsPresenter is used by automation peers
            // Give the Row a pointer to the RowHeader so that it can propagate down change notifications
            DataGridRow owner = DataGridRowOwner;
            if (owner != null)
            {
                owner.DetailsPresenter = this;

                // At the time that a Row is prepared we can't Sync because the DetailsPresenter isn't created yet.
                // Doing it here ensures that the DetailsPresenter is in the visual tree.
                SyncProperties();
            }
        }

        private static void OnAnyMouseLeftButtonDownThunk(object sender, MouseButtonEventArgs e)
        {
            ((DataGridDetailsPresenter)sender).OnAnyMouseLeftButtonDown(e);
        }

        private void OnAnyMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            // Ignore actions if the button down arises from a different presentation source
            if (!PresentationSource.UnderSamePresentationSource(e.OriginalSource as DependencyObject, this))
            {
                return;
            }

            DataGridRow rowOwner = DataGridRowOwner;
            DataGrid dataGridOwner = rowOwner != null ? rowOwner.DataGridOwner : null;
            if ((dataGridOwner != null) && (rowOwner != null))
            {
                // HandleSelectionForRowHeaderAndDetailsInput below sets the CurrentCell
                // of datagrid to the cell with displayindex 0 in the row.
                // This implicitly queues a request to MakeVisible command
                // of ScrollViewer. The command handler calls MakeVisible method of 
                // VirtualizingStackPanel (of rows presenter) which works only
                // when the visual's parent layout is clean. DataGridCellsPanel layout is
                // not clean as per MakeVisible of VSP becuase we distribute the layout of cells for the
                // sake of row headers and hence it fails. VSP.MakeVisible method requeues a request to
                // ScrollViewer.MakeVisible command hence resulting into an infinite loop.
                // The workaround is to bring the concerned cell into the view by calling
                // ScrollIntoView so that by the time MakeVisible handler of ScrollViewer is 
                // executed the cell is already visible and the handler succeeds.
                if (dataGridOwner.CurrentCell.Item != rowOwner.Item)
                {
                    dataGridOwner.ScrollIntoView(rowOwner.Item, dataGridOwner.ColumnFromDisplayIndex(0));
                }

                dataGridOwner.HandleSelectionForRowHeaderAndDetailsInput(rowOwner, /* startDragging = */ Mouse.Captured == null);
            }
        }

        internal FrameworkElement DetailsElement
        {
            get
            {
                var childrenCount = VisualTreeHelper.GetChildrenCount(this);
                if (childrenCount > 0)
                {
                    return VisualTreeHelper.GetChild(this, 0) as FrameworkElement;
                }

                return null;
            }
        }

        /// <summary>
        ///     Update all properties that get a value from the DataGrid
        /// </summary>
        /// <remarks>
        ///     See comment on DataGridRow.OnDataGridChanged
        /// </remarks>
        internal void SyncProperties()
        {
            DataGridRow owner = DataGridRowOwner;
            Content = owner != null ? owner.Item : null;
            DataGridHelper.TransferProperty(this, ContentTemplateProperty);
            DataGridHelper.TransferProperty(this, ContentTemplateSelectorProperty);
        }

        #endregion

        #region Notification Propagation

        /// <summary>
        ///     Notifies parts that respond to changes in the properties.
        /// </summary>
        private static void OnNotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataGridDetailsPresenter)d).NotifyPropertyChanged(d, e);
        }

        internal void NotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == DataGrid.RowDetailsTemplateProperty || e.Property == DataGridRow.DetailsTemplateProperty || e.Property == ContentTemplateProperty)
            {
                DataGridHelper.TransferProperty(this, ContentTemplateProperty);
            }
            else if (e.Property == DataGrid.RowDetailsTemplateSelectorProperty || e.Property == DataGridRow.DetailsTemplateSelectorProperty || e.Property == ContentTemplateSelectorProperty)
            {
                DataGridHelper.TransferProperty(this, ContentTemplateSelectorProperty);
            }
        }

        #endregion 

        #region GridLines

        // Different parts of the DataGrid draw different pieces of the GridLines.
        // Rows draw a single horizontal line on the bottom.  The DataGridDetailsPresenter is the element that handles it.

        /// <summary>
        ///     Measure.  This is overridden so that the row can extend its size to account for a grid line on the bottom.
        /// </summary>
        /// <param name="availableSize"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            // Make space for the GridLine on the bottom.
            // Remove space from the constraint (since it implicitly includes the GridLine's thickness), 
            // call the base implementation, and add the thickness back for the returned size.
            var row = DataGridRowOwner;
            if (row == null)
            {
                return base.MeasureOverride(availableSize);
            }

            var dataGrid = row.DataGridOwner;
            if (dataGrid == null)
            {
                return base.MeasureOverride(availableSize);
            }

            if (row.DetailsPresenterDrawsGridLines && 
                DataGridHelper.IsGridLineVisible(dataGrid, /*isHorizontal = */ true))
            {
                double thickness = dataGrid.HorizontalGridLineThickness;
                Size desiredSize = base.MeasureOverride(DataGridHelper.SubtractFromSize(availableSize, thickness, /*height = */ true));
                desiredSize.Height += thickness;
                return desiredSize;
            }
            else
            {
                return base.MeasureOverride(availableSize);
            }
        }

        /// <summary>
        ///     Arrange.  This is overriden so that the row can position its content to account for a grid line on the bottom.
        /// </summary>
        /// <param name="finalSize">Arrange size</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // We don't need to adjust the Arrange position of the content.  By default it is arranged at 0,0 and we're
            // adding a line to the bottom.  All we have to do is compress and extend the size, just like Measure.
            var row = DataGridRowOwner;
            if (row == null)
            {
                return base.ArrangeOverride(finalSize);
            }

            var dataGrid = row.DataGridOwner;
            if (dataGrid == null)
            {
                return base.ArrangeOverride(finalSize);
            }

            if (row.DetailsPresenterDrawsGridLines &&
                DataGridHelper.IsGridLineVisible(dataGrid, /*isHorizontal = */ true))
            {
                double thickness = dataGrid.HorizontalGridLineThickness;
                Size returnSize = base.ArrangeOverride(DataGridHelper.SubtractFromSize(finalSize, thickness, /*height = */ true));
                returnSize.Height += thickness;
                return returnSize;
            }
            else
            {
                return base.ArrangeOverride(finalSize);
            }
        }

        /// <summary>
        ///     OnRender.  Overriden to draw a horizontal line underneath the content.
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var row = DataGridRowOwner;
            if (row == null)
            {
                return;
            }

            var dataGrid = row.DataGridOwner;
            if (dataGrid == null)
            {
                return;
            }

            if (row.DetailsPresenterDrawsGridLines &&
                DataGridHelper.IsGridLineVisible(dataGrid, /*isHorizontal = */ true))
            {
                double thickness = dataGrid.HorizontalGridLineThickness;
                Rect rect = new Rect(new Size(RenderSize.Width, thickness));
                rect.Y = RenderSize.Height - thickness;

                drawingContext.DrawRectangle(dataGrid.HorizontalGridLinesBrush, null, rect);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        ///     The DataGrid that owns this control
        /// </summary>
        private DataGrid DataGridOwner
        {
            get
            {
                DataGridRow owner = DataGridRowOwner;
                if (owner != null)
                {
                    return owner.DataGridOwner;
                }

                return null;
            }
        }

        /// <summary>
        ///     The DataGridRow that owns this control.
        /// </summary>
        internal DataGridRow DataGridRowOwner
        {
            get { return DataGridHelper.FindParent<DataGridRow>(this); }
        }

        #endregion
    }
}
