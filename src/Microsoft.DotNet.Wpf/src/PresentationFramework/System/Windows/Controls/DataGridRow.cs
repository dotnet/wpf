// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using MS.Internal;

namespace System.Windows.Controls
{
    /// <summary>
    ///     A control for displaying a row of the DataGrid.
    ///     A row represents a data item in the DataGrid.
    ///     A row displays a cell for each column of the DataGrid.
    ///
    ///     The data item for the row is added n times to the row's Items collection,
    ///     where n is the number of columns in the DataGrid.
    /// </summary>
    public class DataGridRow : Control
    {
        #region Constants

        private const byte DATAGRIDROW_stateMouseOverCode = 0;
        private const byte DATAGRIDROW_stateMouseOverEditingCode = 1;
        private const byte DATAGRIDROW_stateMouseOverEditingFocusedCode = 2;
        private const byte DATAGRIDROW_stateMouseOverSelectedCode = 3;
        private const byte DATAGRIDROW_stateMouseOverSelectedFocusedCode = 4;
        private const byte DATAGRIDROW_stateNormalCode = 5;
        private const byte DATAGRIDROW_stateNormalEditingCode = 6;
        private const byte DATAGRIDROW_stateNormalEditingFocusedCode = 7;
        private const byte DATAGRIDROW_stateSelectedCode = 8;
        private const byte DATAGRIDROW_stateSelectedFocusedCode = 9;
        private const byte DATAGRIDROW_stateNullCode = 255;

        // Static arrays to handle state transitions:
        private static byte[] _idealStateMapping = new byte[] {
            DATAGRIDROW_stateNormalCode,
            DATAGRIDROW_stateNormalCode,
            DATAGRIDROW_stateMouseOverCode,
            DATAGRIDROW_stateMouseOverCode,
            DATAGRIDROW_stateNullCode,
            DATAGRIDROW_stateNullCode,
            DATAGRIDROW_stateNullCode,
            DATAGRIDROW_stateNullCode,
            DATAGRIDROW_stateSelectedCode,
            DATAGRIDROW_stateSelectedFocusedCode,
            DATAGRIDROW_stateMouseOverSelectedCode,
            DATAGRIDROW_stateMouseOverSelectedFocusedCode,
            DATAGRIDROW_stateNormalEditingCode,
            DATAGRIDROW_stateNormalEditingFocusedCode,
            DATAGRIDROW_stateMouseOverEditingCode,
            DATAGRIDROW_stateMouseOverEditingFocusedCode
        };

        private static byte[] _fallbackStateMapping = new byte[] {
            DATAGRIDROW_stateNormalCode, //DATAGRIDROW_stateMouseOverCode's fallback
            DATAGRIDROW_stateMouseOverEditingFocusedCode, //DATAGRIDROW_stateMouseOverEditingCode's fallback
            DATAGRIDROW_stateNormalEditingFocusedCode, //DATAGRIDROW_stateMouseOverEditingFocusedCode's fallback
            DATAGRIDROW_stateMouseOverSelectedFocusedCode, //DATAGRIDROW_stateMouseOverSelectedCode's fallback
            DATAGRIDROW_stateSelectedFocusedCode, //DATAGRIDROW_stateMouseOverSelectedFocusedCode's fallback
            DATAGRIDROW_stateNullCode, //DATAGRIDROW_stateNormalCode's fallback
            DATAGRIDROW_stateNormalEditingFocusedCode, //DATAGRIDROW_stateNormalEditingCode's fallback
            DATAGRIDROW_stateSelectedFocusedCode, //DATAGRIDROW_stateNormalEditingFocusedCode's fallback
            DATAGRIDROW_stateSelectedFocusedCode, //DATAGRIDROW_stateSelectedCode's fallback
            DATAGRIDROW_stateNormalCode //DATAGRIDROW_stateSelectedFocusedCode's fallback
        };

        private static string[] _stateNames = new string[] {
            VisualStates.DATAGRIDROW_stateMouseOver,
            VisualStates.DATAGRIDROW_stateMouseOverEditing,
            VisualStates.DATAGRIDROW_stateMouseOverEditingFocused,
            VisualStates.DATAGRIDROW_stateMouseOverSelected,
            VisualStates.DATAGRIDROW_stateMouseOverSelectedFocused,
            VisualStates.DATAGRIDROW_stateNormal,
            VisualStates.DATAGRIDROW_stateNormalEditing,
            VisualStates.DATAGRIDROW_stateNormalEditingFocused,
            VisualStates.DATAGRIDROW_stateSelected,
            VisualStates.DATAGRIDROW_stateSelectedFocused
        };

        #endregion Constants

        #region Constructors

        /// <summary>
        ///     Instantiates global information.
        /// </summary>
        static DataGridRow()
        {
            VisibilityProperty.OverrideMetadata(typeof(DataGridRow), new FrameworkPropertyMetadata(null, OnCoerceVisibility));
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DataGridRow), new FrameworkPropertyMetadata(typeof(DataGridRow)));
            ItemsPanelProperty.OverrideMetadata(typeof(DataGridRow), new FrameworkPropertyMetadata(new ItemsPanelTemplate(new FrameworkElementFactory(typeof(DataGridCellsPanel)))));
            FocusableProperty.OverrideMetadata(typeof(DataGridRow), new FrameworkPropertyMetadata(false));
            BackgroundProperty.OverrideMetadata(typeof(DataGridRow), new FrameworkPropertyMetadata(null, OnNotifyRowPropertyChanged, OnCoerceBackground));
            BindingGroupProperty.OverrideMetadata(typeof(DataGridRow), new FrameworkPropertyMetadata(OnNotifyRowPropertyChanged));

            // Set SnapsToDevicePixels to true so that this element can draw grid lines.  The metadata options are so that the property value doesn't inherit down the tree from here.
            SnapsToDevicePixelsProperty.OverrideMetadata(typeof(DataGridRow), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange));
            IsMouseOverPropertyKey.OverrideMetadata(typeof(DataGridRow), new UIPropertyMetadata(new PropertyChangedCallback(OnNotifyRowAndRowHeaderPropertyChanged)));
            VirtualizingPanel.ShouldCacheContainerSizeProperty.OverrideMetadata(typeof(DataGridRow), new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceShouldCacheContainerSize)));
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideMetadata(typeof(DataGridRow), new FrameworkPropertyMetadata(IsOffscreenBehavior.FromClip));
        }

        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        public DataGridRow()
        {
            _tracker = new ContainerTracking<DataGridRow>(this);
        }

        #endregion

        #region Data Item

        /// <summary>
        ///     The item that the row represents. This item is an entry in the list of items from the DataGrid.
        ///     From this item, cells are generated for each column in the DataGrid.
        /// </summary>
        public object Item
        {
            get { return GetValue(ItemProperty); }
            set { SetValue(ItemProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the Item property.
        /// </summary>
        public static readonly DependencyProperty ItemProperty =
            DependencyProperty.Register("Item", typeof(object), typeof(DataGridRow), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyRowPropertyChanged)));

        /// <summary>
        ///     Called when the value of the Item property changes.
        /// </summary>
        /// <param name="oldItem">The old value of Item.</param>
        /// <param name="newItem">The new value of Item.</param>
        protected virtual void OnItemChanged(object oldItem, object newItem)
        {
            DataGridCellsPresenter cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                cellsPresenter.Item = newItem;
            }
        }

        #endregion

        #region Template

        /// <summary>
        ///     A template that will generate the panel that arranges the cells in this row.
        /// </summary>
        /// <remarks>
        ///     The template for the row should contain an ItemsControl that template binds to this property.
        /// </remarks>
        public ItemsPanelTemplate ItemsPanel
        {
            get { return (ItemsPanelTemplate)GetValue(ItemsPanelProperty); }
            set { SetValue(ItemsPanelProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty that represents the ItemsPanel property.
        /// </summary>
        public static readonly DependencyProperty ItemsPanelProperty = ItemsControl.ItemsPanelProperty.AddOwner(typeof(DataGridRow));

        /// <summary>
        ///     Clears the CellsPresenter and DetailsPresenter references on Template change.
        /// </summary>
        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            base.OnTemplateChanged(oldTemplate, newTemplate);
            CellsPresenter = null;
            DetailsPresenter = null;
        }

        #endregion

        #region Visual States

        private bool IsDataGridKeyboardFocusWithin
        {
            get
            {
                var dataGrid = DataGridOwner;
                if (dataGrid != null)
                {
                    return dataGrid.IsKeyboardFocusWithin;
                }

                return false;
            }
        }

        /// <summary>
        /// Updates the background brush of the row, using a storyboard if available.
        /// </summary>
        internal override void ChangeVisualState(bool useTransitions)
        {
            byte idealStateMappingIndex = 0;
            if (IsSelected || IsEditing) // this is slightly different than SL because they assume if it's editing it will be selected.
            {
                idealStateMappingIndex += 8;
            }
            if (IsEditing)
            {
                idealStateMappingIndex += 4;
            }
            if (IsMouseOver)
            {
                idealStateMappingIndex += 2;
            }
            if (IsDataGridKeyboardFocusWithin)
            {
                idealStateMappingIndex += 1;
            }

            byte stateCode = _idealStateMapping[idealStateMappingIndex];
            Debug.Assert(stateCode != DATAGRIDROW_stateNullCode);

            string storyboardName;
            while (stateCode != DATAGRIDROW_stateNullCode)
            {
                if (stateCode == DATAGRIDROW_stateNormalCode)
                {
                    if (AlternationIndex % 2 == 1)
                    {
                        storyboardName = VisualStates.DATAGRIDROW_stateAlternate;
                    }
                    else
                    {
                        storyboardName = VisualStates.DATAGRIDROW_stateNormal;
                    }
                }
                else
                {
                    storyboardName = _stateNames[stateCode];
                }
                if (VisualStateManager.GoToState(this, storyboardName, useTransitions))
                {
                    break;
                }
                else
                {
                    // The state wasn't implemented so fall back to the next one
                    stateCode = _fallbackStateMapping[stateCode];
                }
            }

            base.ChangeVisualState(useTransitions);
        }

        #endregion

        #region Row Header

        /// <summary>
        ///     The object representing the Row Header.
        /// </summary>
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the Header property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(object), typeof(DataGridRow), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyRowAndRowHeaderPropertyChanged)));

        /// <summary>
        ///     Called when the value of the Header property changes.
        /// </summary>
        /// <param name="oldHeader">The old value of Header</param>
        /// <param name="newHeader">The new value of Header</param>
        protected virtual void OnHeaderChanged(object oldHeader, object newHeader)
        {
        }

        /// <summary>
        ///     The object representing the Row Header style.
        /// </summary>
        public Style HeaderStyle
        {
            get { return (Style)GetValue(HeaderStyleProperty); }
            set { SetValue(HeaderStyleProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the HeaderStyle property.
        /// </summary>
        public static readonly DependencyProperty HeaderStyleProperty =
            DependencyProperty.Register("HeaderStyle", typeof(Style), typeof(DataGridRow), new FrameworkPropertyMetadata(null, OnNotifyRowAndRowHeaderPropertyChanged, OnCoerceHeaderStyle));

        /// <summary>
        ///     The object representing the Row Header template.
        /// </summary>
        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the HeaderTemplate property.
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateProperty =
            DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(DataGridRow), new FrameworkPropertyMetadata(null, OnNotifyRowAndRowHeaderPropertyChanged, OnCoerceHeaderTemplate));

        /// <summary>
        ///     The object representing the Row Header template selector.
        /// </summary>
        public DataTemplateSelector HeaderTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(HeaderTemplateSelectorProperty); }
            set { SetValue(HeaderTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the HeaderTemplateSelector property.
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateSelectorProperty =
            DependencyProperty.Register("HeaderTemplateSelector", typeof(DataTemplateSelector), typeof(DataGridRow), new FrameworkPropertyMetadata(null, OnNotifyRowAndRowHeaderPropertyChanged, OnCoerceHeaderTemplateSelector));

        /// <summary>
        /// Template used to visually indicate an error in row Validation.
        /// </summary>
        public ControlTemplate ValidationErrorTemplate
        {
            get { return (ControlTemplate)GetValue(ValidationErrorTemplateProperty); }
            set { SetValue(ValidationErrorTemplateProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for the ValidationErrorTemplate property.
        /// </summary>
        public static readonly DependencyProperty ValidationErrorTemplateProperty =
            DependencyProperty.Register("ValidationErrorTemplate", typeof(ControlTemplate), typeof(DataGridRow), new FrameworkPropertyMetadata(null, OnNotifyRowPropertyChanged, OnCoerceValidationErrorTemplate));

        #endregion

        #region Row Details

        /// <summary>
        ///     The object representing the Row Details template.
        /// </summary>
        public DataTemplate DetailsTemplate
        {
            get { return (DataTemplate)GetValue(DetailsTemplateProperty); }
            set { SetValue(DetailsTemplateProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the DetailsTemplate property.
        /// </summary>
        public static readonly DependencyProperty DetailsTemplateProperty =
            DependencyProperty.Register("DetailsTemplate", typeof(DataTemplate), typeof(DataGridRow), new FrameworkPropertyMetadata(null, OnNotifyDetailsTemplatePropertyChanged, OnCoerceDetailsTemplate));

        /// <summary>
        ///     The object representing the Row Details template selector.
        /// </summary>
        public DataTemplateSelector DetailsTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(DetailsTemplateSelectorProperty); }
            set { SetValue(DetailsTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the DetailsTemplateSelector property.
        /// </summary>
        public static readonly DependencyProperty DetailsTemplateSelectorProperty =
            DependencyProperty.Register("DetailsTemplateSelector", typeof(DataTemplateSelector), typeof(DataGridRow), new FrameworkPropertyMetadata(null, OnNotifyDetailsTemplatePropertyChanged, OnCoerceDetailsTemplateSelector));

        /// <summary>
        ///     The Visibility of the Details presenter
        /// </summary>
        public Visibility DetailsVisibility
        {
            get { return (Visibility)GetValue(DetailsVisibilityProperty); }
            set { SetValue(DetailsVisibilityProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the DetailsVisibility property.
        /// </summary>
        public static readonly DependencyProperty DetailsVisibilityProperty =
            DependencyProperty.Register("DetailsVisibility", typeof(Visibility), typeof(DataGridRow), new FrameworkPropertyMetadata(Visibility.Collapsed, OnNotifyDetailsVisibilityChanged, OnCoerceDetailsVisibility));

        internal bool DetailsLoaded
        {
            get
            {
                return _detailsLoaded;
            }

            set
            {
                _detailsLoaded = value;
            }
        }

        #endregion

        #region Row Generation

        /// <summary>
        /// We can't override the metadata for a read only property, so we'll get the property change notification for AlternationIndexProperty this way instead.
        /// </summary>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == AlternationIndexProperty)
            {
                NotifyPropertyChanged(this, e, DataGridNotificationTarget.Rows);
            }
        }

        /// <summary>
        ///     Prepares a row container for active use.
        /// </summary>
        /// <remarks>
        ///     Instantiates or updates a MultipleCopiesCollection ItemsSource in
        ///     order that cells be generated.
        /// </remarks>
        /// <param name="item">The data item that the row represents.</param>
        /// <param name="owningDataGrid">The DataGrid owner.</param>
        internal void PrepareRow(object item, DataGrid owningDataGrid)
        {
            bool fireOwnerChanged = (_owner != owningDataGrid);
            Debug.Assert(_owner == null || _owner == owningDataGrid, "_owner should be null before PrepareRow is called or the same as the owningDataGrid.");
            bool forcePrepareCells = false;
            _owner = owningDataGrid;

            if (this != item)
            {
                if (Item != item)
                {
                    Item = item;
                }
                else
                {
                    forcePrepareCells = true;
                }
            }

            if (IsEditing)
            {
                // If IsEditing was left on and this container was recycled, reset it here.
                IsEditing = false;
            }

            // Since we just changed _owner we need to invalidate all child properties that rely on a value supplied by the DataGrid.
            // A common scenario is when a recycled Row was detached from the visual tree and has just been reattached (we always clear out the
            // owner when recycling a container).
            if (fireOwnerChanged)
            {
                SyncProperties(forcePrepareCells);
            }

            CoerceValue(VirtualizingPanel.ShouldCacheContainerSizeProperty);

            // Re-run validation, but wait until Binding has occured.
            Dispatcher.BeginInvoke(new DispatcherOperationCallback(DelayedValidateWithoutUpdate), DispatcherPriority.DataBind, BindingGroup);
        }

        /// <summary>
        ///     Clears the row of references.
        /// </summary>
        internal void ClearRow(DataGrid owningDataGrid)
        {
            Debug.Assert(_owner == owningDataGrid, "_owner should be the same as the DataGrid that is clearing the row.");

            var cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                PersistAttachedItemValue(cellsPresenter, DataGridCellsPresenter.HeightProperty);
            }

            PersistAttachedItemValue(this, DetailsVisibilityProperty);

            Item = BindingExpressionBase.DisconnectedItem;
            DataGridDetailsPresenter detailsPresenter = DetailsPresenter;
            if (detailsPresenter != null)
            {
                detailsPresenter.Content = BindingExpressionBase.DisconnectedItem;
            }

            _owner = null;
        }

        private void PersistAttachedItemValue(DependencyObject objectWithProperty, DependencyProperty property)
        {
            ValueSource valueSource = DependencyPropertyHelper.GetValueSource(objectWithProperty, property);
            if (valueSource.BaseValueSource == BaseValueSource.Local)
            {
                // attach the local value to the item so it can be restored later.
                _owner.ItemAttachedStorage.SetValue(Item, property, objectWithProperty.GetValue(property));
                objectWithProperty.ClearValue(property);
            }
        }

        private void RestoreAttachedItemValue(DependencyObject objectWithProperty, DependencyProperty property)
        {
            object value;
            if (_owner.ItemAttachedStorage.TryGetValue(Item, property, out value))
            {
                objectWithProperty.SetValue(property, value);
            }
        }

        /// <summary>
        ///     Used by the DataGrid owner to send notifications to the row container.
        /// </summary>
        internal ContainerTracking<DataGridRow> Tracker
        {
            get { return _tracker; }
        }

        #endregion

        #region Row Resizing

        internal void OnRowResizeStarted()
        {
            var cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                _cellsPresenterResizeHeight = cellsPresenter.Height;
            }
        }

        internal void OnRowResize(double changeAmount)
        {
            var cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                double newHeight = cellsPresenter.ActualHeight + changeAmount;

                // clamp the CellsPresenter size to the RowHeader size or MinHeight because the header wont shrink any smaller.
                double minHeight = Math.Max(RowHeader.DesiredSize.Height, MinHeight);
                if (DoubleUtil.LessThan(newHeight, minHeight))
                {
                    newHeight = minHeight;
                }

                // clamp the CellsPresenter size to the MaxHeight of Row, because row wouldn't grow any larger
                double maxHeight = MaxHeight;
                if (DoubleUtil.GreaterThan(newHeight, maxHeight))
                {
                    newHeight = maxHeight;
                }

                cellsPresenter.Height = newHeight;
            }
        }

        internal void OnRowResizeCompleted(bool canceled)
        {
            var cellsPresenter = CellsPresenter;
            if (cellsPresenter != null && canceled)
            {
                cellsPresenter.Height = _cellsPresenterResizeHeight;
            }
        }

        internal void OnRowResizeReset()
        {
            var cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                cellsPresenter.ClearValue(DataGridCellsPresenter.HeightProperty);
                if (_owner != null)
                {
                    _owner.ItemAttachedStorage.ClearValue(Item, DataGridCellsPresenter.HeightProperty);
                }
            }
        }

        #endregion

        #region Columns Notification

        /// <summary>
        ///     Notification from the DataGrid that the columns collection has changed.
        /// </summary>
        /// <param name="columns">The columns collection.</param>
        /// <param name="e">The event arguments from the collection's change event.</param>
        protected internal virtual void OnColumnsChanged(ObservableCollection<DataGridColumn> columns, NotifyCollectionChangedEventArgs e)
        {
            DataGridCellsPresenter cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                cellsPresenter.OnColumnsChanged(columns, e);
            }
        }

        #endregion

        #region Property Coercion

        private static object OnCoerceHeaderStyle(DependencyObject d, object baseValue)
        {
            var row = (DataGridRow)d;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                row,
                baseValue,
                HeaderStyleProperty,
                row.DataGridOwner,
                DataGrid.RowHeaderStyleProperty);
        }

        private static object OnCoerceHeaderTemplate(DependencyObject d, object baseValue)
        {
            var row = (DataGridRow)d;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                row,
                baseValue,
                HeaderTemplateProperty,
                row.DataGridOwner,
                DataGrid.RowHeaderTemplateProperty);
        }

        private static object OnCoerceHeaderTemplateSelector(DependencyObject d, object baseValue)
        {
            var row = (DataGridRow)d;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                row,
                baseValue,
                HeaderTemplateSelectorProperty,
                row.DataGridOwner,
                DataGrid.RowHeaderTemplateSelectorProperty);
        }

        private static object OnCoerceBackground(DependencyObject d, object baseValue)
        {
            var row = (DataGridRow)d;
            object coercedValue = baseValue;

            switch (row.AlternationIndex)
            {
                case 0:
                    coercedValue = DataGridHelper.GetCoercedTransferPropertyValue(
                        row,
                        baseValue,
                        BackgroundProperty,
                        row.DataGridOwner,
                        DataGrid.RowBackgroundProperty);

                    break;
                case 1:
                    coercedValue = DataGridHelper.GetCoercedTransferPropertyValue(
                        row,
                        baseValue,
                        BackgroundProperty,
                        row.DataGridOwner,
                        DataGrid.AlternatingRowBackgroundProperty);

                    break;
            }

            return coercedValue;
        }

        private static object OnCoerceValidationErrorTemplate(DependencyObject d, object baseValue)
        {
            var row = (DataGridRow)d;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                row,
                baseValue,
                ValidationErrorTemplateProperty,
                row.DataGridOwner,
                DataGrid.RowValidationErrorTemplateProperty);
        }

        private static object OnCoerceDetailsTemplate(DependencyObject d, object baseValue)
        {
            var row = (DataGridRow)d;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                row,
                baseValue,
                DetailsTemplateProperty,
                row.DataGridOwner,
                DataGrid.RowDetailsTemplateProperty);
        }

        private static object OnCoerceDetailsTemplateSelector(DependencyObject d, object baseValue)
        {
            var row = (DataGridRow)d;
            return DataGridHelper.GetCoercedTransferPropertyValue(
                row,
                baseValue,
                DetailsTemplateSelectorProperty,
                row.DataGridOwner,
                DataGrid.RowDetailsTemplateSelectorProperty);
        }

        private static object OnCoerceDetailsVisibility(DependencyObject d, object baseValue)
        {
            var row = (DataGridRow)d;
            object visibility = DataGridHelper.GetCoercedTransferPropertyValue(
                row,
                baseValue,
                DetailsVisibilityProperty,
                row.DataGridOwner,
                DataGrid.RowDetailsVisibilityModeProperty);

            if (visibility is DataGridRowDetailsVisibilityMode)
            {
                var visibilityMode = (DataGridRowDetailsVisibilityMode)visibility;
                var hasDetailsTemplate = row.DetailsTemplate != null || row.DetailsTemplateSelector != null;
                var isRealItem = row.Item != CollectionView.NewItemPlaceholder;
                switch (visibilityMode)
                {
                    case DataGridRowDetailsVisibilityMode.Collapsed:
                        visibility = Visibility.Collapsed;
                        break;
                    case DataGridRowDetailsVisibilityMode.Visible:
                        visibility = hasDetailsTemplate && isRealItem ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    case DataGridRowDetailsVisibilityMode.VisibleWhenSelected:
                        visibility = row.IsSelected && hasDetailsTemplate && isRealItem ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    default:
                        visibility = Visibility.Collapsed;
                        break;
                }
            }

            return visibility;
        }

        /// <summary>
        ///     Coerces Visibility so that the NewItemPlaceholder doesn't show up while you're entering a new Item
        /// </summary>
        private static object OnCoerceVisibility(DependencyObject d, object baseValue)
        {
            var row = (DataGridRow)d;
            var owningDataGrid = row.DataGridOwner;
            if (row.Item == CollectionView.NewItemPlaceholder && owningDataGrid != null)
            {
                return owningDataGrid.PlaceholderVisibility;
            }
            else
            {
                return baseValue;
            }
        }

        /// <summary>
        ///     Coerces ShouldCacheContainerSize so that the NewItemPlaceholder doesn't cache its size.
        /// </summary>
        private static object OnCoerceShouldCacheContainerSize(DependencyObject d, object baseValue)
        {
            var row = (DataGridRow)d;
            if (row.Item == CollectionView.NewItemPlaceholder)
            {
                return false;
            }
            else
            {
                return baseValue;
            }
        }

        #endregion

        #region Notification Propagation

        private static void OnNotifyRowPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as DataGridRow).NotifyPropertyChanged(d, e, DataGridNotificationTarget.Rows);
        }

        private static void OnNotifyRowAndRowHeaderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as DataGridRow).NotifyPropertyChanged(d, e, DataGridNotificationTarget.Rows | DataGridNotificationTarget.RowHeaders);
        }

        private static void OnNotifyDetailsTemplatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGridRow row = (DataGridRow)d;
            row.NotifyPropertyChanged(row, e, DataGridNotificationTarget.Rows | DataGridNotificationTarget.DetailsPresenter);

            // It only makes sense to fire UnloadingRowDetails if the row details are already loaded. The same is true for LoadingRowDetails,
            // since making row details visible will take care of firing LoadingRowDetails.
            if (row.DetailsLoaded &&
                d.GetValue(e.Property) == e.NewValue)
            {
                if (row.DataGridOwner != null)
                {
                    row.DataGridOwner.OnUnloadingRowDetailsWrapper(row);
                }
                if (e.NewValue != null)
                {
                    // Invoke LoadingRowDetails, but only after the details template is expanded (so DetailsElement will be available).
                    Dispatcher.CurrentDispatcher.BeginInvoke(new DispatcherOperationCallback(DataGrid.DelayedOnLoadingRowDetails), DispatcherPriority.Loaded, row);
                }
            }
        }

        private static void OnNotifyDetailsVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var row = (DataGridRow)d;

            // Notify the DataGrid at Loaded priority so the template has time to expland.
            Dispatcher.CurrentDispatcher.BeginInvoke(new DispatcherOperationCallback(DelayedRowDetailsVisibilityChanged), DispatcherPriority.Loaded, row);
            row.NotifyPropertyChanged(d, e, DataGridNotificationTarget.Rows | DataGridNotificationTarget.DetailsPresenter);
        }

        /// <summary>
        ///     Notifies the DataGrid that the visibility is changed.  This is intended to be Invoked at lower than Layout priority to give the template time to expand.
        /// </summary>
        private static object DelayedRowDetailsVisibilityChanged(object arg)
        {
            var row = (DataGridRow)arg;
            var dataGrid = row.DataGridOwner;
            var detailsElement = row.DetailsPresenter != null ? row.DetailsPresenter.DetailsElement : null;
            if (dataGrid != null)
            {
                var detailsEventArgs = new DataGridRowDetailsEventArgs(row, detailsElement);
                dataGrid.OnRowDetailsVisibilityChanged(detailsEventArgs);
            }

            return null;
        }

        /// <summary>
        ///     Set by the CellsPresenter when it is created.  Used by the Row to send down property change notifications.
        /// </summary>
        internal DataGridCellsPresenter CellsPresenter
        {
            get { return _cellsPresenter; }
            set { _cellsPresenter = value; }
        }

        /// <summary>
        ///     Set by the DetailsPresenter when it is created.  Used by the Row to send down property change notifications.
        /// </summary>
        internal DataGridDetailsPresenter DetailsPresenter
        {
            get { return _detailsPresenter; }
            set { _detailsPresenter = value; }
        }

        /// <summary>
        ///     Set by the RowHeader when it is created.  Used by the Row to send down property change notifications.
        /// </summary>
        internal DataGridRowHeader RowHeader
        {
            get { return _rowHeader; }
            set { _rowHeader = value; }
        }

        /// <summary>
        ///     General notification for DependencyProperty changes from the grid or from columns.
        /// </summary>
        internal void NotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e, DataGridNotificationTarget target)
        {
            NotifyPropertyChanged(d, string.Empty, e, target);
        }

        /// <summary>
        ///     General notification for DependencyProperty changes from the grid or from columns.
        /// </summary>
        internal void NotifyPropertyChanged(DependencyObject d, string propertyName, DependencyPropertyChangedEventArgs e, DataGridNotificationTarget target)
        {
            if (DataGridHelper.ShouldNotifyRows(target))
            {
                if (e.Property == DataGrid.RowBackgroundProperty || e.Property == DataGrid.AlternatingRowBackgroundProperty ||
                    e.Property == BackgroundProperty || e.Property == AlternationIndexProperty)
                {
                    DataGridHelper.TransferProperty(this, BackgroundProperty);
                }
                else if (e.Property == DataGrid.RowHeaderStyleProperty || e.Property == HeaderStyleProperty)
                {
                    DataGridHelper.TransferProperty(this, HeaderStyleProperty);
                }
                else if (e.Property == DataGrid.RowHeaderTemplateProperty || e.Property == HeaderTemplateProperty)
                {
                    DataGridHelper.TransferProperty(this, HeaderTemplateProperty);
                }
                else if (e.Property == DataGrid.RowHeaderTemplateSelectorProperty || e.Property == HeaderTemplateSelectorProperty)
                {
                    DataGridHelper.TransferProperty(this, HeaderTemplateSelectorProperty);
                }
                else if (e.Property == DataGrid.RowValidationErrorTemplateProperty || e.Property == ValidationErrorTemplateProperty)
                {
                    DataGridHelper.TransferProperty(this, ValidationErrorTemplateProperty);
                }
                else if (e.Property == DataGrid.RowDetailsTemplateProperty || e.Property == DetailsTemplateProperty)
                {
                    DataGridHelper.TransferProperty(this, DetailsTemplateProperty);
                    DataGridHelper.TransferProperty(this, DetailsVisibilityProperty);
                }
                else if (e.Property == DataGrid.RowDetailsTemplateSelectorProperty || e.Property == DetailsTemplateSelectorProperty)
                {
                    DataGridHelper.TransferProperty(this, DetailsTemplateSelectorProperty);
                    DataGridHelper.TransferProperty(this, DetailsVisibilityProperty);
                }
                else if (e.Property == DataGrid.RowDetailsVisibilityModeProperty || e.Property == DetailsVisibilityProperty || e.Property == IsSelectedProperty)
                {
                    DataGridHelper.TransferProperty(this, DetailsVisibilityProperty);
                }
                else if (e.Property == ItemProperty)
                {
                    OnItemChanged(e.OldValue, e.NewValue);
                }
                else if (e.Property == HeaderProperty)
                {
                    OnHeaderChanged(e.OldValue, e.NewValue);
                }
                else if (e.Property == BindingGroupProperty)
                {
                    // Re-run validation, but wait until Binding has occured.
                    Dispatcher.BeginInvoke(new DispatcherOperationCallback(DelayedValidateWithoutUpdate), DispatcherPriority.DataBind, e.NewValue);
                }
                else if (e.Property == DataGridRow.IsEditingProperty ||
                         e.Property == DataGridRow.IsMouseOverProperty ||
                         e.Property == DataGrid.IsKeyboardFocusWithinProperty)
                {
                    UpdateVisualState();
                }
            }

            if (DataGridHelper.ShouldNotifyDetailsPresenter(target))
            {
                if (DetailsPresenter != null)
                {
                    DetailsPresenter.NotifyPropertyChanged(d, e);
                }
            }

            if (DataGridHelper.ShouldNotifyCellsPresenter(target) ||
                DataGridHelper.ShouldNotifyCells(target) ||
                DataGridHelper.ShouldRefreshCellContent(target))
            {
                DataGridCellsPresenter cellsPresenter = CellsPresenter;
                if (cellsPresenter != null)
                {
                    cellsPresenter.NotifyPropertyChanged(d, propertyName, e, target);
                }
            }

            if (DataGridHelper.ShouldNotifyRowHeaders(target) && RowHeader != null)
            {
                RowHeader.NotifyPropertyChanged(d, e);
            }
        }

        private object DelayedValidateWithoutUpdate(object arg)
        {
            // Only validate if we have an Item.
            var bindingGroup = (BindingGroup)arg;
            if (bindingGroup != null && bindingGroup.Items.Count > 0)
            {
                bindingGroup.ValidateWithoutUpdate();
            }

            return null;
        }

        /// <summary>
        ///     Fired when the Row is attached to the DataGrid.  The scenario here is if the user is scrolling and
        ///     the Row is a recycled container that was just added back to the visual tree.  Properties that rely on a value from
        ///     the Grid should be reevaluated because they may be stale.
        /// </summary>
        /// <remarks>
        ///     Properties can obviously be stale if the DataGrid's value changes while the row is disconnected.  They can also
        ///     be stale for unobvious reasons.
        ///
        ///     For example, the Style property is invalidated when we detect a new Visual parent.  This happens for
        ///     elements in the row (such as the RowHeader) before Prepare is called on the Row.  The coercion callback
        ///     will thus be unable to find the DataGrid and will return the wrong value.
        ///
        ///     There is a potential for perf work here.  If we know a DP isn't invalidated when the visual tree is reconnected
        ///     and we know that the Grid hasn't modified that property then its value is likely fine.  We could also cache whether
        ///     or not the Grid's property is the one that's winning.  If not, no need to redo the coercion.  This notification
        ///     is pretty fast already and thus not worth the work for now.
        /// </remarks>
        private void SyncProperties(bool forcePrepareCells)
        {
            // Coerce all properties on Row that depend on values from the DataGrid
            // Style is ok since it's equivalent to ItemContainerStyle and has already been invalidated.
            DataGridHelper.TransferProperty(this, BackgroundProperty);
            DataGridHelper.TransferProperty(this, HeaderStyleProperty);
            DataGridHelper.TransferProperty(this, HeaderTemplateProperty);
            DataGridHelper.TransferProperty(this, HeaderTemplateSelectorProperty);
            DataGridHelper.TransferProperty(this, ValidationErrorTemplateProperty);
            DataGridHelper.TransferProperty(this, DetailsTemplateProperty);
            DataGridHelper.TransferProperty(this, DetailsTemplateSelectorProperty);
            DataGridHelper.TransferProperty(this, DetailsVisibilityProperty);

            CoerceValue(VisibilityProperty); // Handle NewItemPlaceholder case

            RestoreAttachedItemValue(this, DetailsVisibilityProperty);

            var cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                cellsPresenter.SyncProperties(forcePrepareCells);
                RestoreAttachedItemValue(cellsPresenter, DataGridCellsPresenter.HeightProperty);
            }

            if (DetailsPresenter != null)
            {
                DetailsPresenter.SyncProperties();
            }

            if (RowHeader != null)
            {
                RowHeader.SyncProperties();
            }
        }

        #endregion

        #region Alternation

        /// <summary>
        ///     AlternationIndex is set on containers generated for an ItemsControl, when
        ///     the ItemsControl's AlternationCount property is positive.  The AlternationIndex
        ///     lies in the range [0, AlternationCount), and adjacent containers always get
        ///     assigned different values.
        /// </summary>
        /// <remarks>
        ///     Exposes ItemsControl.AlternationIndexProperty attached property as a direct property.
        /// </remarks>
        public int AlternationIndex
        {
            get { return (int)GetValue(AlternationIndexProperty); }
        }

        /// <summary>
        ///     DependencyProperty for AlternationIndex.
        /// </summary>
        /// <remarks>
        ///     Same as ItemsControl.AlternationIndexProperty.
        /// </remarks>
        public static readonly DependencyProperty AlternationIndexProperty = ItemsControl.AlternationIndexProperty.AddOwner(typeof(DataGridRow));

        #endregion

        #region Selection

        /// <summary>
        ///     Indicates whether this DataGridRow is selected.
        /// </summary>
        /// <remarks>
        ///     When IsSelected is set to true, an InvalidOperationException may be
        ///     thrown if the value of the SelectionUnit property on the parent DataGrid
        ///     prevents selection or rows.
        /// </remarks>
        [Bindable(true), Category("Appearance")]
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the IsSelected property.
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty = Selector.IsSelectedProperty.AddOwner(
            typeof(DataGridRow),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal, new PropertyChangedCallback(OnIsSelectedChanged)));

        private static void OnIsSelectedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DataGridRow row = (DataGridRow)sender;
            bool isSelected = (bool)e.NewValue;

            if (isSelected && !row.IsSelectable)
            {
                throw new InvalidOperationException(SR.Get(SRID.DataGridRow_CannotSelectRowWhenCells));
            }

            DataGrid grid = row.DataGridOwner;
            if (grid != null && row.DataContext != null)
            {
                DataGridAutomationPeer gridPeer = UIElementAutomationPeer.FromElement(grid) as DataGridAutomationPeer;
                if (gridPeer != null)
                {
                    DataGridItemAutomationPeer rowItemPeer = gridPeer.FindOrCreateItemAutomationPeer(row.DataContext) as DataGridItemAutomationPeer;
                    if (rowItemPeer != null)
                    {
                        rowItemPeer.RaisePropertyChangedEvent(
                            System.Windows.Automation.SelectionItemPatternIdentifiers.IsSelectedProperty,
                            (bool)e.OldValue,
                            isSelected);
                    }
                }
            }

            // Update the header's IsRowSelected property
            row.NotifyPropertyChanged(row, e, DataGridNotificationTarget.Rows | DataGridNotificationTarget.RowHeaders);

            // This will raise the appropriate selection event, which will
            // bubble to the DataGrid. The base class Selector code will listen
            // for these events and will update SelectedItems as necessary.
            row.RaiseSelectionChangedEvent(isSelected);

            row.UpdateVisualState();

            // Update the header's IsRowSelected property
            row.NotifyPropertyChanged(row, e, DataGridNotificationTarget.Rows | DataGridNotificationTarget.RowHeaders);
        }

        private void RaiseSelectionChangedEvent(bool isSelected)
        {
            if (isSelected)
            {
                OnSelected(new RoutedEventArgs(SelectedEvent, this));
            }
            else
            {
                OnUnselected(new RoutedEventArgs(UnselectedEvent, this));
            }
        }

        /// <summary>
        ///     Raised when the item's IsSelected property becomes true.
        /// </summary>
        public static readonly RoutedEvent SelectedEvent = Selector.SelectedEvent.AddOwner(typeof(DataGridRow));

        /// <summary>
        ///     Raised when the item's IsSelected property becomes true.
        /// </summary>
        public event RoutedEventHandler Selected
        {
            add
            {
                AddHandler(SelectedEvent, value);
            }

            remove
            {
                RemoveHandler(SelectedEvent, value);
            }
        }

        /// <summary>
        ///     Called when IsSelected becomes true. Raises the Selected event.
        /// </summary>
        /// <param name="e">Empty event arguments.</param>
        protected virtual void OnSelected(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Raised when the item's IsSelected property becomes false.
        /// </summary>
        public static readonly RoutedEvent UnselectedEvent = Selector.UnselectedEvent.AddOwner(typeof(DataGridRow));

        /// <summary>
        ///     Raised when the item's IsSelected property becomes false.
        /// </summary>
        public event RoutedEventHandler Unselected
        {
            add
            {
                AddHandler(UnselectedEvent, value);
            }

            remove
            {
                RemoveHandler(UnselectedEvent, value);
            }
        }

        /// <summary>
        ///     Called when IsSelected becomes false. Raises the Unselected event.
        /// </summary>
        /// <param name="e">Empty event arguments.</param>
        protected virtual void OnUnselected(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Determines if a row can be selected, based on the DataGrid's SelectionUnit property.
        /// </summary>
        private bool IsSelectable
        {
            get
            {
                DataGrid dataGrid = DataGridOwner;
                if (dataGrid != null)
                {
                    DataGridSelectionUnit unit = dataGrid.SelectionUnit;
                    return (unit == DataGridSelectionUnit.FullRow) ||
                        (unit == DataGridSelectionUnit.CellOrRowHeader);
                }

                return true;
            }
        }

        #endregion

        #region Editing

        /// <summary>
        ///     Whether the row is in editing mode.
        /// </summary>
        public bool IsEditing
        {
            get { return (bool)GetValue(IsEditingProperty); }
            internal set { SetValue(IsEditingPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey IsEditingPropertyKey =
            DependencyProperty.RegisterReadOnly("IsEditing", typeof(bool), typeof(DataGridRow), new FrameworkPropertyMetadata(false, OnNotifyRowAndRowHeaderPropertyChanged));

        /// <summary>
        ///     The DependencyProperty for IsEditing.
        /// </summary>
        public static readonly DependencyProperty IsEditingProperty = IsEditingPropertyKey.DependencyProperty;

        #endregion

        #region Automation

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new System.Windows.Automation.Peers.DataGridRowAutomationPeer(this);
        }

        #endregion

        #region Column Virtualization

        /// <summary>
        ///     Method which tries to scroll a cell for given index into the scroll view
        /// </summary>
        /// <param name="index"></param>
        internal void ScrollCellIntoView(int index)
        {
            DataGridCellsPresenter cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                cellsPresenter.ScrollCellIntoView(index);
            }
        }

        #endregion

        #region Layout

        /// <summary>
        ///     Arrange
        /// </summary>
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            DataGrid dataGrid = DataGridOwner;
            if (dataGrid != null)
            {
                dataGrid.QueueInvalidateCellsPanelHorizontalOffset();
            }

            return base.ArrangeOverride(arrangeBounds);
        }

        #endregion

        #region New Item

        /// <summary>
        ///     Indicates whether the row belongs to new item (both placeholder
        ///     as well as adding item) or not.
        /// </summary>
        public bool IsNewItem
        {
            get { return (bool)GetValue(IsNewItemProperty); }
            internal set { SetValue(IsNewItemPropertyKey, value); }
        }

        /// <summary>
        ///     Using a DependencyProperty as the backing store for IsNewItem.  This enables animation, styling, binding, etc...
        /// </summary>
        internal static readonly DependencyPropertyKey IsNewItemPropertyKey =
            DependencyProperty.RegisterReadOnly("IsNewItem", typeof(bool), typeof(DataGridRow), new FrameworkPropertyMetadata(false));

        /// <summary>
        ///     DependencyProperty for IsNewItem property.
        /// </summary>
        public static readonly DependencyProperty IsNewItemProperty =
            IsNewItemPropertyKey.DependencyProperty;

        #endregion

        #region Helpers

        /// <summary>
        ///     Returns the index of this row within the DataGrid's list of item containers.
        /// </summary>
        /// <remarks>
        ///     This method performs a linear search.
        /// </remarks>
        /// <returns>The index, if found, -1 otherwise.</returns>
        public int GetIndex()
        {
            DataGrid dataGridOwner = DataGridOwner;
            if (dataGridOwner != null)
            {
                return dataGridOwner.ItemContainerGenerator.IndexFromContainer(this);
            }

            return -1;
        }

        /// <summary>
        ///     Searchs up the visual parent chain from the given element until
        ///     a DataGridRow element is found.
        /// </summary>
        /// <param name="element">The descendent of a DataGridRow.</param>
        /// <returns>
        ///     The first ancestor DataGridRow of the element parameter.
        ///     Returns null of none is found.
        /// </returns>
        public static DataGridRow GetRowContainingElement(FrameworkElement element)
        {
            return DataGridHelper.FindVisualParent<DataGridRow>(element);
        }

        internal DataGrid DataGridOwner
        {
            get { return _owner; }
        }

        /// <summary>
        /// Returns true if the DetailsPresenter is supposed to draw gridlines for the row.  Only true
        /// if the DetailsPresenter hooked itself up properly to the Row.
        /// </summary>
        internal bool DetailsPresenterDrawsGridLines
        {
            get { return _detailsPresenter != null && _detailsPresenter.Visibility == Visibility.Visible; }
        }

        /// <summary>
        ///     Acceses the CellsPresenter and attempts to get the cell at the given index.
        ///     This is not necessarily the display order.
        /// </summary>
        internal DataGridCell TryGetCell(int index)
        {
            DataGridCellsPresenter cellsPresenter = CellsPresenter;
            if (cellsPresenter != null)
            {
                return cellsPresenter.ItemContainerGenerator.ContainerFromIndex(index) as DataGridCell;
            }

            return null;
        }

        #endregion

        #region Data

        // Tracks whether row details have been displayed.
        //      true - row details template has been loaded and has rendered at least once
        //      false - row details template has either is unset, or has never been asked to render
        internal bool _detailsLoaded;

        private DataGrid _owner;
        private DataGridCellsPresenter _cellsPresenter;
        private DataGridDetailsPresenter _detailsPresenter;
        private DataGridRowHeader _rowHeader;
        private ContainerTracking<DataGridRow> _tracker;
        private double _cellsPresenterResizeHeight;

        #endregion
    }
}
