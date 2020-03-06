// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{

    #region Using declarations

    using System;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;
    using System.Xml;
    using Microsoft.Windows.Input;
    using MS.Internal;
#if RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Automation.Peers;
#endif

    #endregion

    /// <summary>
    ///   RibbonGallery inherits from ItemsControl. It contains RibbonGalleryCategory instances which in turn contain
    ///   RibbonGalleryItem instances.
    /// </summary>
    [StyleTypedProperty(Property = "AllFilterItemContainerStyle", StyleTargetType = typeof(RibbonMenuItem))]
    [StyleTypedProperty(Property = "FilterItemContainerStyle", StyleTargetType = typeof(RibbonMenuItem))]
    [StyleTypedProperty(Property = "GalleryItemStyle", StyleTargetType = typeof(RibbonGalleryItem))]
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(RibbonGalleryCategory))]
    [TemplatePart(Name = ItemsHostName, Type = typeof(ItemsPresenter))]
    [TemplatePart(Name = _filterMenuButtonTemplatePartName, Type = typeof(RibbonMenuButton))]
    [TemplatePart(Name = ScrollViewerTemplatePartName, Type=typeof(ScrollViewer))]
    [TemplatePart(Name = FilterContentPaneTemplatePartName, Type = typeof(ContentPresenter))]
    public class RibbonGallery : ItemsControl, IWeakEventListener, IPreviewCommandSource
    {
        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonGallery class.
        /// </summary>
        static RibbonGallery()
        {
            Type ownerType = typeof(RibbonGallery);
            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            ItemContainerStyleProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceItemContainerStyle)));
            ItemTemplateProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceItemTemplate)));
            EventManager.RegisterClassHandler(ownerType, MouseMoveEvent, new MouseEventHandler(OnMouseMove), true);
            ToolTipProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(RibbonHelper.CoerceRibbonToolTip)));
            ToolTipService.ShowOnDisabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(true));
            ContextMenuProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(RibbonHelper.OnContextMenuChanged, RibbonHelper.OnCoerceContextMenu));
            ContextMenuService.ShowOnDisabledProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(true));
#if IN_RIBBON_GALLERY
            ScrollViewer.VerticalScrollBarVisibilityProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(CoerceVerticalScrollBarVisibility)));
#else
            ScrollViewer.VerticalScrollBarVisibilityProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null));
#endif
            EventManager.RegisterClassHandler(ownerType, RibbonControlService.DismissPopupEvent, new RibbonDismissPopupEventHandler(OnDismissPopupThunk));

            FilterCommand = new RoutedCommand("Filter", ownerType);
            CommandManager.RegisterClassCommandBinding(ownerType, new CommandBinding(FilterCommand, FilterExecuted, FilterCanExecute));
            EventManager.RegisterClassHandler(ownerType, LoadedEvent, new RoutedEventHandler(OnLoaded));
            EventManager.RegisterClassHandler(ownerType, UnloadedEvent, new RoutedEventHandler(OnUnloaded));
        }

        /// <summary>
        ///   Initializes an instance of the RibbonGallery class.
        /// </summary>
        public RibbonGallery()
        {
            this.ItemContainerGenerator.StatusChanged += new EventHandler(OnItemContainerGeneratorStatusChanged);

            // Ensure coercion happens for these APIs.
            this.CoerceValue(FilterItemTemplateSelectorProperty);
            this.CoerceValue(FilterItemContainerStyleSelectorProperty);
        }

        #endregion

        #region Template

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // remove any old handlers
            if (_filterMenuButton != null)
            {
                Debug.Assert(_filterMenuButton.ItemContainerGenerator != null);
                _filterMenuButton.ItemContainerGenerator.StatusChanged -= OnFilterButtonItemContainerGeneratorStatusChanged;
            }

            _filterMenuButton = this.Template.FindName(_filterMenuButtonTemplatePartName, this) as RibbonFilterMenuButton;

            if (_filterMenuButton != null)
            {
                Debug.Assert(_filterMenuButton.ItemContainerGenerator != null);
                _filterMenuButton.ItemContainerGenerator.StatusChanged += new EventHandler(OnFilterButtonItemContainerGeneratorStatusChanged);
                Binding itemsSourceBinding = new Binding() { Source = this._categoryFilters };
                _filterMenuButton.SetBinding(RibbonMenuButton.ItemsSourceProperty, itemsSourceBinding);
            }

            _itemsPresenter = (ItemsPresenter)GetTemplateChild(ItemsHostName);
            _filterContentPane = GetTemplateChild(FilterContentPaneTemplatePartName) as ContentPresenter;
            _scrollViewer = GetTemplateChild(RibbonGallery.ScrollViewerTemplatePartName) as ScrollViewer;

            PropertyHelper.TransferProperty(this, ContextMenuProperty);   // Coerce to get a default ContextMenu if none has been specified.
            PropertyHelper.TransferProperty(this, RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty);

#if IN_RIBBON_GALLERY
            if (_scrollViewer != null)
            {
                InRibbonGallery parentInRibbonGallery = ParentInRibbonGallery;
                if (parentInRibbonGallery != null)
                {
                    parentInRibbonGallery.CommandTarget = _scrollViewer;
                }
            }
#endif
        }

#if IN_RIBBON_GALLERY
        private static object CoerceVerticalScrollBarVisibility(DependencyObject d, object baseValue)
        {
            RibbonGallery me = (RibbonGallery)d;

            // Don't show ScrollBar Template when in InRibbonGalleryMode as it has it's own scroll buttons.
            if (me.IsInInRibbonGalleryMode())
            {
                return ScrollBarVisibility.Hidden;
            }
            return baseValue;
        }
#endif

        #region CurrentFilter

        /// <summary>
        ///   Using a DependencyProperty as the backing store for CurrentFilter.  This enables animation, styling, binding, etc...
        /// </summary>
        private static readonly DependencyProperty CurrentFilterProperty =
            DependencyProperty.Register("CurrentFilter", typeof(object), typeof(RibbonGallery), new FrameworkPropertyMetadata(_allFilter, OnCurrentFilterChanged));

        /// <summary>
        ///   Specifies the current filter.
        /// </summary>
        private object CurrentFilter
        {
            get { return GetValue(CurrentFilterProperty); }
            set { SetValue(CurrentFilterProperty, value); }
        }

        private static readonly DependencyProperty CurrentFilterStyleProperty =
            DependencyProperty.Register("CurrentFilterStyle", typeof(Style), typeof(RibbonGallery), new FrameworkPropertyMetadata(null, null, OnCoerceCurrentFilterStyle));

        private Style CurrentFilterStyle
        {
            get { return (Style)GetValue(CurrentFilterStyleProperty); }
        }

        // coercion precedence:
        //  1) FilterItemContainerStyleSelector, if one is specified.
        //  2) FilterItemContainerStyle/AllFilterItemContainerStyle, if specified.
        //  3) null (the base value)
        // We get this all for free thanks to the coercion on FilterItemContainerStyleSelector and the logic in the default StyleSelector.
        // We just need to tell the following properties to call this coercion when they change:
        //  1) FilterItemContainerStyleSelector
        //  2) FilterItemContainerStyle
        //  3) AllFilterItemContainerStyle
        //  4) CurrentFilter
        private static object OnCoerceCurrentFilterStyle(DependencyObject d, object baseValue)
        {
            RibbonGallery gallery = (RibbonGallery)d;
            object currentFilter = gallery.CurrentFilter;
            StyleSelector filterItemContainerStyleSelector = gallery.FilterItemContainerStyleSelector;
            if (currentFilter != null &&
                filterItemContainerStyleSelector != null &&
                gallery._filterMenuButton != null)
            {
                return filterItemContainerStyleSelector.SelectStyle(currentFilter, gallery._filterMenuButton.CurrentFilterItem);
            }
            return null;
        }

        // Header is tricky.  For the filter items, Header defaults to the DataContext.
        // However, Header can be overridden by a Setter at a the ItemContainerStyle level.
        // Therefore, we should only bind Header to CurrentFilter when no Style is setting Header.
        // We need to re-run this logic whenever any of the following change:
        //  1) FilterItemContainerStyleSelector
        //  2) FilterItemContainerStyle
        //  3) AllFilterItemContainerStyle
        //  4) CurrentFilter
        // We also need to run this logic in OnApplyTemplate AFTER Style bindings have been set up.
        internal void SetHeaderBindingForCurrentFilterItem()
        {
            if (_filterMenuButton != null)
            {
                RibbonMenuItem currentFilterItem = _filterMenuButton.CurrentFilterItem;
                if (currentFilterItem != null)
                {
                    currentFilterItem.ClearValue(RibbonMenuItem.HeaderProperty);

                    if (PropertyHelper.IsDefaultValue(currentFilterItem, RibbonMenuItem.HeaderProperty))
                    {
                        // In the default case (where no Style is setting Header), we fall back
                        // to setting currentFilterItem.Header to CurrentFilter.
                        currentFilterItem.Header = this.CurrentFilter;
                    }
                }
            }
        }

        private static readonly DependencyProperty CurrentFilterTemplateProperty =
            DependencyProperty.Register("CurrentFilterTemplate", typeof(DataTemplate), typeof(RibbonGallery),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnCurrentFilterTemplateChanged), new CoerceValueCallback(OnCoerceCurrentFilterTemplate)));

        private DataTemplate CurrentFilterTemplate
        {
            get { return (DataTemplate)GetValue(CurrentFilterTemplateProperty); }
        }

        private static void OnCurrentFilterTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGallery gallery = (RibbonGallery)d;
            gallery.SetTemplateBindingForCurrentFilterItem();
        }

        private bool FilterMenuButtonTemplateIsBound
        {
            get { return _bits[(int)Bits.FilterMenuButtonTemplateIsBound]; }
            set { _bits[(int)Bits.FilterMenuButtonTemplateIsBound] = value; }
        }

        // This method sets up a binding between _filterMenuButton's hosted RibbonMenuItem.HeaderTemplate and CurrentFilterTemplate.
        // We need to call it whenever RibbonGallery is retemplated or CurrentFilterTemplate changes.
        // If CurrentFilterTemplate is not being acquired through a user-set value, we do not want to have this binding.  That way,
        // a Setter for HeaderTemplate in _filterMenuButton's Style, if present, will be honored.
        internal void SetTemplateBindingForCurrentFilterItem()
        {
            if (_filterMenuButton != null)
            {
                RibbonMenuItem currentFilterItem = _filterMenuButton.CurrentFilterItem;
                if (currentFilterItem != null)
                {
                    // Assume we are not going to set up a binding for template.  The user may using FilterItemContainerStyle with a Setter
                    // for HeaderTemplate.  In this case, we need to honor that setter so long as an item template for the filter has not
                    // been set through one of the other APIs: FilterItemTemplate, AllFilterItemTemplate, FilterItemTemplateSelector.
                    // Thus, only set this binding on HeaderTemplate when one of those template APIs is specified.  When a template for the filter item
                    // is not specified, we unset this binding so that a Setter for HeaderTemplate in the Style is able to bleed through.
                    bool templateShouldBeBound = false;

                    if (FilterItemTemplateSelector is RibbonGalleryDefaultFilterItemTemplateSelector)
                    {
                        if (object.ReferenceEquals(CurrentFilter, AllFilterItem))
                        {
                            if (this.AllFilterItemTemplate != null)
                            {
                                templateShouldBeBound = true;
                            }
                        }
                        else
                        {
                            if (this.FilterItemTemplate != null)
                            {
                                templateShouldBeBound = true;
                            }
                        }
                    }
                    else
                    {
                        // Someone is setting FilterItemTemplateSelector.  We should bind to the template that gets selected.
                        templateShouldBeBound = true;
                    }

                    if (templateShouldBeBound && !FilterMenuButtonTemplateIsBound)
                    {
                        Binding currentFilterTemplateBinding = new Binding("CurrentFilterTemplate") { Source = this };
                        currentFilterItem.SetBinding(RibbonMenuItem.HeaderTemplateProperty, currentFilterTemplateBinding);
                        FilterMenuButtonTemplateIsBound = true;
                    }
                    else if (!templateShouldBeBound && FilterMenuButtonTemplateIsBound)
                    {
                        BindingOperations.ClearBinding(currentFilterItem, RibbonMenuItem.HeaderTemplateProperty);
                        FilterMenuButtonTemplateIsBound = false;
                    }
                }
            }
        }

        // coercion precedence:
        //  1) FilterItemTemplateSelector, if one is specified.
        //  2) FilterItemTemplate/AllFilterItemTemplate, if specified.
        //  3) null (the base value)
        // We get this all for free thanks to the coercion on FilterItemTemplateSelector and the logic in the default DataTemplateSelector.
        // We just need to tell the following properties to call this coercion when they change:
        //  1) FilterItemTemplateSelector
        //  2) FilterItemTemplate
        //  3) AllFilterItemTemplate
        //  4) CurrentFilter
        private static object OnCoerceCurrentFilterTemplate(DependencyObject d, object baseValue)
        {
            RibbonGallery gallery = (RibbonGallery)d;
            object currentFilter = gallery.CurrentFilter;
            DataTemplateSelector filterItemTemplateSelector = gallery.FilterItemTemplateSelector;
            if (currentFilter != null &&
                filterItemTemplateSelector != null &&
                gallery._filterMenuButton != null)
            {
                return filterItemTemplateSelector.SelectTemplate(currentFilter, gallery._filterMenuButton.CurrentFilterItem);
            }
            return null;
        }

        private void OnItemContainerGeneratorStatusChanged(object sender, EventArgs e)
        {
            if (this.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                RepopulateCategoryFilters();
                SynchronizeWithCurrentItem();

                if (_itemsPresenter != null)
                {
                    ItemsHostSite = (Panel)(ItemsPanel.FindName(RibbonGallery.ItemsHostPanelName, _itemsPresenter));
                }
            }
        }

        #endregion CurrentFilterItem

        private void OnFilterButtonItemContainerGeneratorStatusChanged(object sender, EventArgs e)
        {
            if (_filterMenuButton.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                foreach (object filter in this._categoryFilters)
                {
                    RibbonMenuItem filterItem = _filterMenuButton.ItemContainerGenerator.ContainerFromItem(filter) as RibbonMenuItem;

                    // Bind filterItem.IsChecked to true when Object.ReferenceEquals(this.CurrentFilter, filterItem.DataContext).
                    MultiBinding isCheckedBinding = new MultiBinding();
                    isCheckedBinding.Converter = new ReferentialEqualityConverter();
                    Binding currentFilterBinding = new Binding("CurrentFilter") { Source = this };
                    Binding myHeaderBinding = new Binding("DataContext") { Source = filterItem };
                    isCheckedBinding.Bindings.Add(currentFilterBinding);
                    isCheckedBinding.Bindings.Add(myHeaderBinding);
                    filterItem.SetBinding(RibbonMenuItem.IsCheckedProperty, isCheckedBinding);

                    // Set up FilterCommand properties.
                    filterItem.Command = RibbonGallery.FilterCommand;

                    Binding commandParameterBinding = new Binding("DataContext") { RelativeSource = new RelativeSource(RelativeSourceMode.Self) };
                    filterItem.SetBinding(RibbonMenuItem.CommandParameterProperty, commandParameterBinding);
                }
            }
        }

        #endregion Template

        #region Tree

        // To fetch defined ItemsPanel in RibbonGalleryCategory's Template. It is set during ApplyTemplate.
        internal Panel ItemsHostSite
        {
            get;
            private set;
        }

        internal ScrollViewer ScrollViewer
        {
            get
            {
                return _scrollViewer;
            }
        }

        internal ItemsPresenter ItemsPresenter
        {
            get
            {
                return _itemsPresenter;
            }
        }

        #endregion

        #region Layout

        /// <summary>
        /// MinColumnCount is the property defined on RibbonGallery. RibbonGalleryCategory also Adds
        /// itself Owner to this property.
        /// It's used by RibbonGalleryItemsPanel during Measure/Arrange which is default panel for RibbonGalleryCategory
        /// Default is 0
        /// </summary>
        public int MinColumnCount
        {
            get { return (int)GetValue(MinColumnCountProperty); }
            set { SetValue(MinColumnCountProperty, value); }
        }

        /// <summary>
        /// MinColumnCount is the property defined on RibbonGallery. RibbonGalleryCategory also Adds
        /// itself Owner to this property.
        /// It's used by RibbonGalleryItemsPanel during Measure/Arrange which is default panel for RibbonGalleryCategory
        /// Default is 0
        /// </summary>
        public static readonly DependencyProperty MinColumnCountProperty =
            DependencyProperty.Register(
                            "MinColumnCount",
                            typeof(int),
                            typeof(RibbonGallery),
                            new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsMeasure, new PropertyChangedCallback(OnLayoutPropertyChange)), new ValidateValueCallback(IsMinMaxColumnCountValid));

        /// <summary>
        /// MaxColumnCount is the property defined on RibbonGallery. RibbonGalleryCategory also Adds
        /// itself Owner to this property.
        /// It's used by RibbonGalleryItemsPanel during Measure/Arrange which is default panel for RibbonGalleryCategory
        /// Default is int.MaxValue
        /// </summary>
        public int MaxColumnCount
        {
            get { return (int)GetValue(MaxColumnCountProperty); }
            set { SetValue(MaxColumnCountProperty, value); }
        }

        /// <summary>
        /// MaxColumnCount is the property defined on RibbonGallery. RibbonGalleryCategory also Adds
        /// itself Owner to this property.
        /// It's used by RibbonGalleryItemsPanel during Measure/Arrange which is default panel for RibbonGalleryCategory
        /// Default is int.MaxValue
        /// </summary>
        public static readonly DependencyProperty MaxColumnCountProperty =
            DependencyProperty.Register(
                            "MaxColumnCount",
                            typeof(int),
                            typeof(RibbonGallery),
                            new FrameworkPropertyMetadata(
                                int.MaxValue,
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(OnLayoutPropertyChange),
                                new CoerceValueCallback(CoerceMaxColumnCount)),
                            new ValidateValueCallback(IsMinMaxColumnCountValid));

        // coerce MaxColumnCount so as it's never lesser than MinColumnCount
        private static object CoerceMaxColumnCount(DependencyObject d, object baseValue)
        {
            RibbonGallery gallery = (RibbonGallery)d;
            int minColCount = gallery.MinColumnCount;
            if (minColCount > (int)baseValue)
                return minColCount;
            return baseValue;
        }

        private static bool IsMinMaxColumnCountValid(object value)
        {
            int v = (int)value;
            return (v > 0);
        }

        /// <summary>
        /// When ColumnsStretchToFill is true, RibbonGalleryItems are stretched during layout to occupy all the width available.
        /// ColumnsStretchToFill is honored only when IsSharedColumnSizeScope is true.
        /// </summary>
        public bool ColumnsStretchToFill
        {
            get { return (bool)GetValue(ColumnsStretchToFillProperty); }
            set { SetValue(ColumnsStretchToFillProperty, value); }
        }

        public static readonly DependencyProperty ColumnsStretchToFillProperty = DependencyProperty.Register("ColumnsStretchToFill",
                                                                                                        typeof(bool),
                                                                                                        typeof(RibbonGallery),
                                                                                                        new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnLayoutPropertyChange)));

        /// <summary>
        ///   IsSharedColumnSizeScope: defined on RibbonGallery. RibbonGalleryCategory also adds itself owner to it.
        ///   It's a boolean property where True means that I (the control on which the property is set) am the Scope for
        ///   uniform layout of items. The truth table for this could be defined by:
        ///     gallery     category    Scope
        ///     -------     --------    --------
        ///     T           T           category
        ///     T           F           gallery
        ///     F           T           category
        ///     F           F           gallery*
        ///         * The most correct thing for the F - F combination would be to have no shared size scope at all.  This would
        ///           require us to make non-trivial changes to our measure and arrange algorithms for our panels, and since we
        ///           don't envision a desired user scenario for this we simplify things and treat the F - F combo as gallery scope.
        ///
        ///   We want the default scope to be Gallery scope; hence the default value on Gallery is True and on Category it's false.
        /// </summary>
        public bool IsSharedColumnSizeScope
        {
            get { return (bool)GetValue(IsSharedColumnSizeScopeProperty); }
            set { SetValue(IsSharedColumnSizeScopeProperty, value); }
        }

        public static readonly DependencyProperty IsSharedColumnSizeScopeProperty =
            DependencyProperty.Register(
                            "IsSharedColumnSizeScope",
                            typeof(bool),
                            typeof(RibbonGallery),
                            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure,new PropertyChangedCallback(OnLayoutPropertyChange)));

        // MaxColumnWidth is the desired maximum width of any RibbonGalleryItem in this RibbonGallery for all the Categories
        // whose Panel's uniform layout scope results in scope of gallery.
        internal double MaxColumnWidth
        {
            get { return (double)GetValue(MaxColumnWidthProperty); }
            set { SetValue(MaxColumnWidthProperty, value); }
        }

        // MaxColumnWidth is the desired maximum width of any RibbonGalleryItem in this RibbonGallery for all the Categories
        // whose Panel's uniform layout scope results in scope of gallery.
        internal static readonly DependencyProperty MaxColumnWidthProperty =
            DependencyProperty.Register(
                            "MaxColumnWidth",
                            typeof(double),
                            typeof(RibbonGallery),
                            new FrameworkPropertyMetadata(0.0, new PropertyChangedCallback(OnLayoutPropertyChange)));

        // MaxItemHeight is the desired maximum height of any RibbonGalleryItem in this RibbonGallery for all the Categories
        internal double MaxItemHeight
        {
            get { return (double)GetValue(MaxItemHeightProperty); }
            set { SetValue(MaxItemHeightProperty, value); }
        }

        // MaxItemHeight is the desired maximum height of any RibbonGalleryItem in this RibbonGallery for all the Categories
        internal static readonly DependencyProperty MaxItemHeightProperty =
            DependencyProperty.Register(
                            "MaxItemHeight",
                            typeof(double),
                            typeof(RibbonGallery),
                            new FrameworkPropertyMetadata(0.0, new PropertyChangedCallback(OnLayoutPropertyChange)));

        /// <summary>
        /// The actual width at which a GalleryItem in a SharedScope is arranged.
        /// Its calculated such that the remaining horizontal space in the parent panel is filled up.
        /// ArrangeWidth is recalculated whenever MaxColumnWidth changes.
        /// </summary>
        internal double ArrangeWidth
        {
            get;
            set;
        }

        /// <summary>
        /// Flag to indicate that ArrangeWidth should be recalculated because MaxColumnWidth changed.
        /// </summary>
        internal bool IsArrangeWidthValid
        {
            get;
            set;
        }

        /// <summary>
        /// Flag to indicate that MaxColumnWidth should be recalculated. for e.g. when Items collection changes.
        /// </summary>
        internal bool IsMaxColumnWidthValid
        {
            get;
            set;
        }

        // This is a PropertyChangedCallBack for the layout related properties MinColumnCount/MaxColumnCount
        // IsSharedColumnScope and MaxColumnWidth on RibbonGallery. This calls InvalidateMeasure for all the
        // categories' ItemsPanel and they must use changed values.
        private static void OnLayoutPropertyChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGallery me = (RibbonGallery)d;
            me.IsArrangeWidthValid = false;
            me.InvalidateMeasureOnAllCategoriesPanel();
        }

        // Invalidate Measure on all the Categories' panel.
        internal void InvalidateMeasureOnAllCategoriesPanel()
        {
            if (Items != null)
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    RibbonGalleryCategory category = (RibbonGalleryCategory)ItemContainerGenerator.ContainerFromIndex(i);
                    if (category != null)
                    {
                        if (category.ItemsHostSite != null)
                        {
                            TreeHelper.InvalidateMeasureForVisualAncestorPath<RibbonGallery>(category.ItemsHostSite, false);
                        }
                    }
                }
            }
        }

        #endregion Layout

        #region Selection

        /// <summary>
        ///     Event fired when <see cref="SelectedItem"/> changes.
        /// </summary>
        public static readonly RoutedEvent SelectionChangedEvent =
            EventManager.RegisterRoutedEvent("SelectionChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<object>), typeof(RibbonGallery));

        /// <summary>
        ///     Event fired when <see cref="SelectedItem"/> changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<object> SelectionChanged
        {
            add
            {
                AddHandler(SelectionChangedEvent, value);
            }

            remove
            {
                RemoveHandler(SelectionChangedEvent, value);
            }
        }

        /// <summary>
        ///     Called when <see cref="SelectedItem"/> changes.
        ///     Default implementation fires the <see cref="SelectionChanged"/> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnSelectionChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            RaiseEvent(e);

            if (ShouldExecuteCommand && !e.Handled && e.NewValue != null)
            {
                CommandHelpers.InvokeCommandSource(CommandParameter, PreviewCommandParameter, this, CommandOperation.Execute);
            }
        }

        // There are times when the SelectedItem and the HighlightedItem
        // are being mutated temporarily. This is specifically the case
        // with RibbonComboBox's use of the RibbonGallery. At this time
        // we do not want to fire the Commands.

        internal bool ShouldExecuteCommand
        {
            get { return _bits[(int)Bits.ShouldExecuteCommand]; }
            set { _bits[(int)Bits.ShouldExecuteCommand] = value; }
        }

        /// <summary>
        ///     The DependencyProperty for the <see cref="SelectedItem"/> property.
        ///     Default Value: null
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
                    DependencyProperty.Register(
                                        "SelectedItem",
                                        typeof(object),
                                        typeof(RibbonGallery),
                                        new FrameworkPropertyMetadata(
                                                null,
                                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                                null,
                                                new CoerceValueCallback(CoerceSelectedItem)));


        /// <summary>
        ///     Specifies the selected item.
        /// </summary>
        public object SelectedItem
        {
            get
            {
                return GetValue(SelectedItemProperty);
            }
            set
            {
                SetValue(SelectedItemProperty, value);
            }
        }

        private bool ShouldForceCoerceSelectedItem
        {
            get { return _bits[(int)Bits.ShouldForceCoerceSelectedItem]; }
            set { _bits[(int)Bits.ShouldForceCoerceSelectedItem] = value; }
        }

        // To force Coercion even if the SelectionItem didn't change, useful in case of when ItemsCollection changes.
        internal void ForceCoerceSelectedItem()
        {
            try
            {
                ShouldForceCoerceSelectedItem = true;
                CoerceValue(SelectedItemProperty);
            }
            finally
            {
                ShouldForceCoerceSelectedItem = false;
            }
        }

        private static object CoerceSelectedItem(DependencyObject d, object value)
        {
            RibbonGallery gallery = (RibbonGallery)d;

            if (!gallery.IsSelectionChangeActive)
            {
                object oldItem = gallery.SelectedItem;
                object newItem = value;

                if (!VerifyEqual(oldItem, newItem))
                {
                    if (newItem != null)
                    {
                        RibbonGalleryCategory category = null;
                        RibbonGalleryItem galleryItem = null;

                        // If the newItem doesn't exist in RibbonGallery under any category then return UnsetValue
                        // so as not to change existing value
                        bool ignoreItemContainerGeneratorStatus = false;
                        if (!gallery.ContainsItem(newItem, ignoreItemContainerGeneratorStatus, out category, out galleryItem))
                        {
                            return DependencyProperty.UnsetValue;
                        }

                        // Changes the selection to newItem
                        gallery.ChangeSelection(newItem, galleryItem, true);
                    }
                    else
                    {
                        // Deselect oldItem
                        gallery.ChangeSelection(oldItem, null, false);
                    }
                }
                else if (gallery.ShouldForceCoerceSelectedItem)
                {
                    RibbonGalleryCategory category = null;
                    RibbonGalleryItem galleryItem = null;

                    // This block is called when ItemCollection changes either at the Gallery or the Category levels
                    // to handle the case that a previously SelectedItem is removed or replaced.
                    bool ignoreItemContainerGeneratorStatus = true;
                    if (!gallery.ContainsItem(newItem, ignoreItemContainerGeneratorStatus, out category, out galleryItem))
                    {
                        // Deselect oldItem
                        value = null;
                        gallery.ChangeSelection(oldItem, null, false);
                    }
                }
            }

            return value;
        }

        /// <summary>
        ///     The DependencyProperty for the <see cref="SelectedValue"/> property.
        ///     Default Value: null
        /// </summary>
        public static readonly DependencyProperty SelectedValueProperty =
                    DependencyProperty.Register(
                                        "SelectedValue",
                                        typeof(object),
                                        typeof(RibbonGallery),
                                        new FrameworkPropertyMetadata(
                                                null,
                                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                                null,
                                                new CoerceValueCallback(CoerceSelectedValue)));

        /// <summary>
        ///     Specifies the value on the selected item as defined by <see cref="SelectedValuePath" />.
        /// </summary>
        public object SelectedValue
        {
            get
            {
                return GetValue(SelectedValueProperty);
            }
            set
            {
                SetValue(SelectedValueProperty, value);
            }
        }

        private bool ShouldForceCoerceSelectedValue
        {
            get { return _bits[(int)Bits.ShouldForceCoerceSelectedValue]; }
            set { _bits[(int)Bits.ShouldForceCoerceSelectedValue] = value; }
        }

        // To force Coercion even if the SelectionValue didn't change, useful when synchronizing
        // SelectedItem after container generation is complete.
        internal void ForceCoerceSelectedValue()
        {
            try
            {
                ShouldForceCoerceSelectedValue = true;
                CoerceValue(SelectedValueProperty);
            }
            finally
            {
                ShouldForceCoerceSelectedValue = false;
            }
        }

        private static object CoerceSelectedValue(DependencyObject d, object value)
        {
            RibbonGallery gallery = (RibbonGallery)d;

            if (!gallery.IsSelectionChangeActive)
            {
                object oldValue = gallery.SelectedValue;
                object newValue = value;

                if (!VerifyEqual(oldValue, newValue))
                {
                    if (newValue != null)
                    {
                        object newItem;
                        RibbonGalleryCategory category = null;
                        RibbonGalleryItem galleryItem = null;

                        // If the newValue doesn't exist in RibbonGallery under any category then return UnsetValue
                        // so as not to change existing value
                        bool ignoreItemContainerGeneratorStatus = false;
                        if (!gallery.ContainsValue(value, ignoreItemContainerGeneratorStatus, out newItem, out category, out galleryItem))
                        {
                            return DependencyProperty.UnsetValue;
                        }

                        // Changes the selection to newItem
#if RIBBON_IN_FRAMEWORK
                        gallery.SetCurrentValue(SelectedItemProperty, newItem);
#else
                        gallery.SelectedItem = newItem;
#endif
                    }
                    else
                    {
                        // Deselect
#if RIBBON_IN_FRAMEWORK
                        gallery.InvalidateProperty(SelectedItemProperty);
#else
                        gallery.SelectedItem = null;
#endif
                    }
                }
                else if (gallery.ShouldForceCoerceSelectedValue)
                {
                    object newItem;
                    RibbonGalleryCategory category = null;
                    RibbonGalleryItem galleryItem = null;

                    // This block is called when generating RibbonGalleryCategory containers
                    // to synchronize the SelectedItem with the previously specified SelectedValue.
                    // If a match isn't found yet we keep the oldValue as is by returning UnsetValue.
                    bool ignoreItemContainerGeneratorStatus = true;
                    if (!gallery.ContainsValue(value, ignoreItemContainerGeneratorStatus, out newItem, out category, out galleryItem))
                    {
                        return DependencyProperty.UnsetValue;
                    }
#if RIBBON_IN_FRAMEWORK
                        gallery.SetCurrentValue(SelectedItemProperty, newItem);
#else
                        gallery.SelectedItem = newItem;
#endif
                }
            }

            return value;
        }

        /// <summary>
        ///     SelectedValuePath DependencyProperty
        /// </summary>
        public static readonly DependencyProperty SelectedValuePathProperty =
                DependencyProperty.Register(
                        "SelectedValuePath",
                        typeof(string),
                        typeof(RibbonGallery),
                        new FrameworkPropertyMetadata(
                                String.Empty,
                                new PropertyChangedCallback(OnSelectedValuePathChanged)));

        /// <summary>
        ///  The path used to retrieve the SelectedValue from the SelectedItem
        /// </summary>
        [Localizability(LocalizationCategory.NeverLocalize)] // not localizable
        public string SelectedValuePath
        {
            get { return (string) GetValue(SelectedValuePathProperty); }
            set { SetValue(SelectedValuePathProperty, value); }
        }

        private static void OnSelectedValuePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGallery gallery = (RibbonGallery)d;
            ValueSource valueSource = DependencyPropertyHelper.GetValueSource(gallery, RibbonGallery.SelectedValueProperty);

            if (valueSource.IsCoerced || gallery.SelectedValue != null)
            {
                gallery.CoerceValue(SelectedValueProperty);
            }
        }

        public static readonly DependencyProperty IsSynchronizedWithCurrentItemProperty =
            DependencyProperty.Register("IsSynchronizedWithCurrentItem", typeof(bool?), typeof(RibbonGallery),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnIsSynchronizedWithCurrentItemChanged)));

        /// <summary>
        ///   This flag chooses the synchronization behavior between the
        ///   SelectedItem and the CurrentItem for the associated CollectionView.
        ///   When null we choose the automatic behavior of synchronizing only
        ///   when bound to an explicit CollectionViewSource.
        /// </summary>
        public bool? IsSynchronizedWithCurrentItem
        {
            get { return (bool?)GetValue(IsSynchronizedWithCurrentItemProperty); }
            set { SetValue(IsSynchronizedWithCurrentItemProperty, value); }
        }

        private static void OnIsSynchronizedWithCurrentItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGallery gallery = (RibbonGallery)d;
            gallery.UpdateIsSynchronizedWithCurrentItemInternal();
        }

        /// <summary>
        ///   Update the private flag IsSynchronizedWithCurrentItemInternal so that it
        ///   honor the public property when set and chooses the automatic behavior
        ///   which is synchornize currency only when bound to an explicit CollectionViewSource.
        /// </summary>
        private void UpdateIsSynchronizedWithCurrentItemInternal()
        {
            bool oldValue = IsSynchronizedWithCurrentItemInternal;
            if (oldValue)
            {
                // Stop listening for currency changes
                RemoveCurrentItemChangedListener();
            }

            bool? isSynchronizedWithCurrentItem = IsSynchronizedWithCurrentItem;
            if (isSynchronizedWithCurrentItem.HasValue)
            {
                IsSynchronizedWithCurrentItemInternal = isSynchronizedWithCurrentItem.Value;
            }
            else
            {
                IsSynchronizedWithCurrentItemInternal = IsInitialized && (RibbonGallery.GetSourceCollectionView(this) != null);
            }


            bool newValue = IsSynchronizedWithCurrentItemInternal;
            if (newValue)
            {
                // Listen for currency changes
                AddCurrentItemChangedListener();

                // Synchronize
                SynchronizeWithCurrentItem();
            }

            if (oldValue != newValue)
            {
                // Notify categories
                for (int i = 0; i < Items.Count; i++ )
                {
                    RibbonGalleryCategory category = ItemContainerGenerator.ContainerFromIndex(i) as RibbonGalleryCategory;
                    if (category != null)
                    {
                        if (newValue)
                        {
                            category.AddCurrentItemChangedListener();
                        }
                        else
                        {
                            category.RemoveCurrentItemChangedListener();
                        }
                    }
                }
            }
        }

        internal bool IsSynchronizedWithCurrentItemInternal
        {
            get { return _bits[(int)Bits.IsSynchronizedWithCurrentItemInternal]; }
            set { _bits[(int)Bits.IsSynchronizedWithCurrentItemInternal] = value; }
        }

        private void SynchronizeWithCurrentItem()
        {
            if (IsSynchronizedWithCurrentItemInternal)
            {
                object selectedItem = SelectedItem;
                if (selectedItem != null)
                {
                    // If there is a SelectedItem synchronize
                    // CurrentItem to match it
                    RibbonGalleryCategory category;
                    RibbonGalleryItem galleryItem;
                    bool ignoreItemContainerGeneratorStatus = false;
                    if (ContainsItem(selectedItem, ignoreItemContainerGeneratorStatus, out category, out galleryItem))
                    {
                        SynchronizeWithCurrentItem(category, selectedItem);
                    }
                }
                else
                {
                    // Since there isn't already a SelectedItem
                    // synchronize it to match CurrentItem
                    //
                    // It is possible that the RibbonGalleryCategory containers
                    // haven't been generated at the time that we attempt this
                    // synchronization. In such a case we retry when the
                    // containers have been generated.
                    if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                    {
                        OnCurrentItemChanged();
                    }

                    OnSourceCollectionViewCurrentItemChanged();
                }
            }
        }

        private void SynchronizeWithCurrentItem(
            RibbonGalleryCategory category,
            object selectedItem)
        {
            Debug.Assert(selectedItem != null, "Must have a selectedItem to synchronize with.");

            if (category != null)
            {
                // Synchronize currency on the category containing the SelectedItem
                MoveCurrentTo(category.CollectionView, selectedItem);

                // Synchronize currency on the gallery to be the category containing the SelectedItem
                MoveCurrentTo(CollectionView, ItemContainerGenerator.ItemFromContainer(category));
            }

            // Synchronize currency on the source CollectionView to be the SelectedItem
            int index = SourceCollectionView != null ? SourceCollectionView.IndexOf(selectedItem) : -1;
            if (index > -1)
            {
                MoveCurrentToPosition(SourceCollectionView, index);
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            UpdateIsSynchronizedWithCurrentItemInternal();
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);
            UpdateIsSynchronizedWithCurrentItemInternal();

            // Note that it is possible that we haven't had a chance to sync
            // to the CurrentItem on the CollectionView yet. This could happen,
            // if the ItemContainers were generated synchronously and the base
            // implementation fired ItemContainerGeneratorStatusChanged before
            // we got here. So this is one more attempt to keep things in sync.

            SynchronizeWithCurrentItem();
        }

        private void AddCurrentItemChangedListener()
        {
            Debug.Assert(IsSynchronizedWithCurrentItemInternal, "We should add currency change listeners only when IsSynchronizedWithCurrentItemInternal is true");

            CollectionView = Items;
            SourceCollectionView = RibbonGallery.GetSourceCollectionView(this);

            if (SourceCollectionView == CollectionView.SourceCollection)
            {
                // We need to track the SourceCollectionView only if it
                // is distinct from the immediate CollectionView
                SourceCollectionView = null;
            }

            // Listen for currency changes on the immediate CollectionView for the Gallery
            CurrentChangedEventManager.AddListener(CollectionView, this);

            // Listen for currency changes on the Source CollectionView of the Gallery
            if (SourceCollectionView != null)
            {
                CurrentChangedEventManager.AddListener(SourceCollectionView, this);
            }
        }

        private void RemoveCurrentItemChangedListener()
        {
            // Stop listening for currency changes on the immediate CollectionView for the Gallery
            if (CollectionView != null)
            {
                CurrentChangedEventManager.RemoveListener(CollectionView, this);
                CollectionView = null;
            }

            // Stop listening for currency changes on the Source CollectionView of the Gallery
            if (SourceCollectionView != null)
            {
                CurrentChangedEventManager.RemoveListener(SourceCollectionView, this);
                SourceCollectionView = null;
            }
        }

        internal CollectionView CollectionView
        {
            get;
            set;
        }

        internal CollectionView SourceCollectionView
        {
            get;
            set;
        }

        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (managerType == typeof(CurrentChangedEventManager))
            {
                if (sender == CollectionView)
                {
                    // Update currency on the immediate CollectionView for the Gallery
                    OnCurrentItemChanged();
                }
                else
                {
                    // Update currency on the Source CollectionView of the Gallery
                    OnSourceCollectionViewCurrentItemChanged();
                }
            }
            else
            {
                // Unrecognized event
                return false;
            }

            return true;
        }

        // Update currency on the immediate CollectionView for the Gallery
        private void OnCurrentItemChanged()
        {
            Debug.Assert(IsSynchronizedWithCurrentItemInternal, "We shouldn't be listening for currency changes if IsSynchronizedWithCurrentItemInternal is false");

            if (CollectionView == null || IsSelectionChangeActive)
            {
                return;
            }

            // Synchronize the SelectedItem to be the first Item
            // within the current Category.
            RibbonGalleryCategory category = this.ItemContainerGenerator.ContainerFromItem(CollectionView.CurrentItem) as RibbonGalleryCategory;
            if (category != null && category.Items.Count > 0)
            {
#if RIBBON_IN_FRAMEWORK
                SetCurrentValue(SelectedItemProperty, category.Items[0]);
#else
                SelectedItem = category.Items[0];
#endif
            }
        }

        // Update currency on the Source CollectionView of the Gallery
        private void OnSourceCollectionViewCurrentItemChanged()
        {
            Debug.Assert(IsSynchronizedWithCurrentItemInternal, "We shouldn't be listening for currency changes if IsSynchronizedWithCurrentItemInternal is false");

            if (IsSelectionChangeActive)
            {
                return;
            }

            // Synchronize SelectedItem with the CurrentItem
            // of the Source CollectionView
            if (SourceCollectionView != null)
            {
#if RIBBON_IN_FRAMEWORK
                SetCurrentValue(SelectedItemProperty, SourceCollectionView.CurrentItem);
#else
                SelectedItem = SourceCollectionView.CurrentItem;
#endif
            }
        }

        // This is to handle the case where the Gallery is bound to the Groups
        // property of a CollectionViewSource with GroupDescriptions. Consider
        // this example.
        //
        // <RibbonGallery
        //  Name="rg"
        //  IsSynchronizedWithCurrentItem="True"
        //  Grid.Row="1"
        //  Grid.Column="0"
        //  ItemsSource="{Binding Source={StaticResource CitiesCVS},Path=Groups}"
        //  ItemTemplate="{StaticResource HDT2}" />
        // <ListBox
        //  Name="lb"
        //  SelectionMode="Single"
        //  IsSynchronizedWithCurrentItem="True"
        //  Grid.Row="1"
        //  Grid.Column="1"
        //  ItemsSource="{Binding Source={StaticResource CitiesCVS}}"
        //  ItemTemplate="{StaticResource CT2}"/>
        //
        // To keep the ListBox synchronized with the Gallery we will need to update currency on
        // the CollectionView that the Listox is bound which in this case is the Source CollectionView
        // for Groups collection that the Gallery is bound to.
        internal static CollectionView GetSourceCollectionView(ItemsControl itemsControl)
        {
            if (itemsControl == null)
                return null;

            CollectionViewSource cvs = null;
            CollectionView cv = null;
            Binding binding = BindingOperations.GetBinding(itemsControl, ItemsSourceProperty);
            if (binding != null)
            {
                cvs = binding.Source as CollectionViewSource;
                if (cvs != null)
                {
                    cv = cvs.View as CollectionView;
                }
            }

            return cv;
        }

        // Update all selection properties viz.
        // - SelectedItem
        // - SelectedValue
        // - CurrentItem
        // - IsSelected
        // - SelectedContainers
        internal void ChangeSelection(object item, RibbonGalleryItem container, bool isSelected)
        {
            if (IsSelectionChangeActive)
            {
                return;
            }

            object oldItem = SelectedItem;
            object newItem = item;
            bool selectedItemChanged = !VerifyEqual(oldItem, newItem);

            try
            {
                IsSelectionChangeActive = true;

                if (isSelected == selectedItemChanged)
                {
                    // Deselecting a single container. This can only happen
                    // when setting IsSelected to false on a specific container.
                    // Note that neither SelectedItem nor CurrentItem are updated
                    // in this case. We only updated the _selectedContainers and
                    // ContainsSelection properties both of which are specific to
                    // the containers in view.
                    if (!isSelected && container != null)
                    {
                        container.IsSelected = false;
                        int index = _selectedContainers.IndexOf(container);
                        if (index > -1)
                        {
                            _selectedContainers.RemoveAt(index);
                            container.OnUnselected(new RoutedEventArgs(RibbonGalleryItem.UnselectedEvent, container));
                        }
                    }
                    else
                    {
                        // This is the case where SelectedItem is changing.
                        // We start the processing by deselecting all existing
                        // containers.
                        for (int i = 0; i < _selectedContainers.Count; i++)
                        {
                            RibbonGalleryItem galleryItem = _selectedContainers[i];
                            galleryItem.IsSelected = false;
                            galleryItem.OnUnselected(new RoutedEventArgs(RibbonGalleryItem.UnselectedEvent, galleryItem));

                            if (!isSelected)
                            {
                                MoveCurrentToPosition(galleryItem.RibbonGalleryCategory.CollectionView, -1);
                            }
                        }
                        _selectedContainers.Clear();

                        if (!isSelected)
                        {
#if RIBBON_IN_FRAMEWORK
                            InvalidateProperty(SelectedItemProperty);
                            InvalidateProperty(SelectedValueProperty);
#else
                            SelectedItem = null;
                            SelectedValue = null;
#endif

                            MoveCurrentToPosition(CollectionView, -1);
                            MoveCurrentToPosition(SourceCollectionView, -1);

                            // When changing RibbonGallery.SelectedItem to null, we need to push RibbonComboBox to update its
                            // selection properties.  Even though RibbonComboBox handles RibbonGalleryItem.UnselectedEvent,
                            // at the time of its handling RibbonGallery.SelectedItem is still non-null.  We need to refresh
                            // RibbonComboBox's selection properties once RibbonGallery.SelectedItem is actually null.
                            RibbonComboBox comboBoxParent = LogicalTreeHelper.GetParent(this) as RibbonComboBox;
                            if (comboBoxParent != null &&
                                this == comboBoxParent.FirstGallery &&
                                comboBoxParent.IsSelectedItemCached == false)
                            {
                                comboBoxParent.UpdateSelectionProperties();
                            }
                        }
                    }

                    // Select the item
                    if (isSelected)
                    {
#if RIBBON_IN_FRAMEWORK
                        SetCurrentValue(SelectedItemProperty, item);
                        SetCurrentValue(SelectedValueProperty, GetSelectableValueFromItem(item));
#else
                        SelectedItem = item;
                        SelectedValue = GetSelectableValueFromItem(item);
#endif

                        // Synchronize currency with the specified SelectedItem.
                        if (container != null)
                        {
                            // This is the case where a single container is selected
                            SynchronizeWithCurrentItem(container.RibbonGalleryCategory, item);
                        }
                        else
                        {
                            // This is the case where the selected item is directly
                            // set in which case we need to additionally find the category
                            // that contains it to perform the currency synchronization
                            SynchronizeWithCurrentItem();
                        }
                    }
                }

                // Select the container and synchronize currency
                if (isSelected && container != null && !_selectedContainers.Contains(container))
                {
                    _selectedContainers.Add(container);
                    container.IsSelected = true;
                    container.OnSelected(new RoutedEventArgs(RibbonGalleryItem.SelectedEvent, container));
                }
            }
            finally
            {
                IsSelectionChangeActive = false;
            }

            if (selectedItemChanged)
            {
                RoutedPropertyChangedEventArgs<object> args = new RoutedPropertyChangedEventArgs<object>(oldItem, isSelected ? newItem : null, SelectionChangedEvent);
                this.OnSelectionChanged(args);
            }
        }

        // To find out if the RibbonGallery contains the specified item.
        // If ignoreItemContainerGeneratorStatus is true then skip the
        // container generation check.
        private bool ContainsItem(
            object item,
            bool ignoreItemContainerGeneratorStatus,
            out RibbonGalleryCategory category,
            out RibbonGalleryItem galleryItem)
        {
            category = null;
            galleryItem = null;
            int index = -1;

            if (!ignoreItemContainerGeneratorStatus &&
                ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
            {
                return true;
            }

            foreach (object current in Items)
            {
                category = ItemContainerGenerator.ContainerFromItem(current) as RibbonGalleryCategory;
                if (category != null)
                {
                    index = category.Items.IndexOf(item);
                    if (index > -1)
                    {
                        galleryItem = category.ItemContainerGenerator.ContainerFromIndex(index) as RibbonGalleryItem;
                        break;
                    }
                    category = null;
                }
            }

            return index > -1;
        }

        // To find out if the RibbonGallery contains the specified value
        // in any of the available items. If ignoreItemContainerGeneratorStatus
        // is true then skip the container generation check.
        private bool ContainsValue(
            object value,
            bool ignoreItemContainerGeneratorStatus,
            out object item,
            out RibbonGalleryCategory category,
            out RibbonGalleryItem galleryItem)
        {
            item = null;
            category = null;
            galleryItem = null;

            if (!ignoreItemContainerGeneratorStatus &&
                ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
            {
                return true;
            }

            ContentControl dummyElement = new ContentControl();

            foreach (object current in Items)
            {
                category = ItemContainerGenerator.ContainerFromItem(current) as RibbonGalleryCategory;
                if (category != null)
                {
                    for (int index=0; index<category.Items.Count; index++)
                    {
                        item = category.Items[index];
                        object itemValue = GetSelectableValueFromItem(item, dummyElement);
                        if (VerifyEqual(value, itemValue))
                        {
                            galleryItem = category.ItemContainerGenerator.ContainerFromIndex(index) as RibbonGalleryItem;
                            return true;
                        }
                        item = null;
                    }
                    category = null;
                }
            }

            return false;
        }

        internal object GetSelectableValueFromItem(object item)
        {
            return GetSelectableValueFromItem(item, new ContentControl());
        }

        // Find out the value of the item using SelectedValuePath.
        // If there is no SelectedValuePath then item itself is
        // it's value or the innerText in case of XML node.
        private object GetSelectableValueFromItem(object item, ContentControl dummyElement)
        {
            bool useXml = item is XmlNode;
            Binding itemBinding = new Binding();
            itemBinding.Source = item;
            if (useXml)
            {
                itemBinding.XPath = SelectedValuePath;
                itemBinding.Path = new PropertyPath("/InnerText");
            }
            else
            {
                itemBinding.Path = new PropertyPath(SelectedValuePath);
            }

            // optimize for case where there is no SelectedValuePath (meaning
            // that the value of the item is the item itself, or the InnerText
            // of the item)
            if (string.IsNullOrEmpty(SelectedValuePath))
            {
                // when there's no SelectedValuePath, the binding's Path
                // is either empty (CLR) or "/InnerText" (XML)
                string path = itemBinding.Path.Path;
                Debug.Assert(String.IsNullOrEmpty(path) || path == "/InnerText");
                if (string.IsNullOrEmpty(path))
                {
                    // CLR - item is its own selected value
                    return item;
                }
                else
                {
                    return GetInnerText(item);
                }
            }

            dummyElement.SetBinding(ContentControl.ContentProperty, itemBinding);
            return dummyElement.Content;
        }

        private static object GetInnerText(object item)
        {
            XmlNode node = item as XmlNode;

            if (node != null)
            {
                return node.InnerText;
            }
            else
            {
                return null;
            }
        }

        internal static bool VerifyEqual(object knownValue, object itemValue)
        {
            return Object.Equals(knownValue, itemValue);
        }

        private void MoveCurrentTo(CollectionView cv, object item)
        {
            if (cv != null && IsSynchronizedWithCurrentItemInternal)
            {
                cv.MoveCurrentTo(item);
            }
        }

        private void MoveCurrentToPosition(CollectionView cv, int position)
        {
            if (cv != null && IsSynchronizedWithCurrentItemInternal)
            {
                cv.MoveCurrentToPosition(position);
            }
        }

        internal Collection<RibbonGalleryItem> SelectedContainers
        {
            get { return _selectedContainers; }
        }

        internal RibbonGalleryCategory SelectedCategory
        {
            get
            {
                if (_selectedContainers.Count > 0)
                {
                    return _selectedContainers[0].RibbonGalleryCategory;
                }

                return null;
            }

        }

        internal bool IsSelectionChangeActive
        {
            get { return _bits[(int)Bits.IsSelectionChangeActive]; }
            set { _bits[(int)Bits.IsSelectionChangeActive] = value; }
        }

        #endregion Selection

        #region Highlight

        /// <summary>
        ///     The DependencyProperty for the <see cref="HighlightedItem"/> property.
        ///     Default Value: null
        /// </summary>
        private static readonly DependencyPropertyKey HighlightedItemPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                                        "HighlightedItem",
                                        typeof(object),
                                        typeof(RibbonGallery),
                                        new FrameworkPropertyMetadata(
                                                new PropertyChangedCallback(OnHighlightedItemChangedPrivate),
                                                new CoerceValueCallback(CoerceHighlightedItem)));


        /// <summary>
        ///     The DependencyProperty for the HighlightedItem property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty HighlightedItemProperty =
                HighlightedItemPropertyKey.DependencyProperty;


        /// <summary>
        ///     Specifies the highlighted item.
        /// </summary>
        public object HighlightedItem
        {
            get { return GetValue(HighlightedItemProperty); }
            internal set { SetValue(HighlightedItemPropertyKey, value); }
        }

        // To force Coercion even if the HighlightedItem didn't change, useful in case of when ItemsCollection changes.
        internal void ForceCoerceHighlightedItem()
        {
            try
            {
                ShouldForceCoerceHighlightedItem = true;
                CoerceValue(HighlightedItemProperty);
            }
            finally
            {
                ShouldForceCoerceHighlightedItem = false;
            }
        }

        private static object CoerceHighlightedItem(DependencyObject d, object value)
        {
            RibbonGallery gallery = (RibbonGallery)d;

            if (!gallery.IsHighlightChangeActive)
            {
                object oldItem = gallery.HighlightedItem;
                object newItem = value;

                if (!VerifyEqual(oldItem, newItem))
                {
                    if (newItem != null)
                    {
                        RibbonGalleryCategory category = null;
                        RibbonGalleryItem galleryItem = null;

                        // If the newItem doesn't exist in RibbonGallery under any category then return UnsetValue
                        // so as not to change existing value
                        bool ignoreItemContainerGeneratorStatus = false;
                        if (!gallery.ContainsItem(newItem, ignoreItemContainerGeneratorStatus, out category, out galleryItem))
                        {
                            return DependencyProperty.UnsetValue;
                        }

                        // Changes the highlight to newItem
                        gallery.ChangeHighlight(newItem, galleryItem, true);
                    }
                    else
                    {
                        // Dehighlight oldItem
                        gallery.ChangeHighlight(oldItem, null, false);
                    }
                }
                else if (gallery.ShouldForceCoerceHighlightedItem)
                {
                    RibbonGalleryCategory category = null;
                    RibbonGalleryItem galleryItem = null;

                    // This block is called when ItemCollection changes either at the Gallery or the Category levels
                    // to handle the case that a previously HighlightedItem is removed or replaced.
                    bool ignoreItemContainerGeneratorStatus = true;
                    if (!gallery.ContainsItem(newItem, ignoreItemContainerGeneratorStatus, out category, out galleryItem))
                    {
                        // Dehighlight oldItem
                        value = null;
                        gallery.ChangeHighlight(oldItem, null, false);
                    }
                }
            }

            return value;
        }

        private static void OnHighlightedItemChangedPrivate(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGallery gallery = (RibbonGallery)d;
            gallery.OnHighlightedItemChanged(e);
        }

        protected virtual void OnHighlightedItemChanged(DependencyPropertyChangedEventArgs e)
        {
            if (HighlightChanged != null)
            {
                HighlightChanged(this, EventArgs.Empty);
            }

            if (ShouldExecuteCommand && e.OldValue != null)
            {
                CommandHelpers.InvokeCommandSource(CommandParameter, PreviewCommandParameter, this, CommandOperation.CancelPreview);
            }

            if (ShouldExecuteCommand && e.NewValue != null)
            {
                // Fire the Preview operation on a Dispatcher callback to allow the
                // PreviewCommandParameter's Binding to be updated to match the HighlightedItem
                Dispatcher.BeginInvoke(DispatcherPriority.Send, (DispatcherOperationCallback)delegate(object unused)
                {
                    CommandHelpers.InvokeCommandSource(CommandParameter, PreviewCommandParameter, this, CommandOperation.Preview);
                    return null;
                }, null);
            }
        }

        // Update all highlighting properties viz.
        // - HighlightedItem
        // - IsHighlighted
        // - HighlightedContainer
        internal void ChangeHighlight(object item, RibbonGalleryItem container, bool isHighlighted)
        {
            if (IsHighlightChangeActive)
            {
                return;
            }

            try
            {
                IsHighlightChangeActive = true;

                if (_highlightedContainer != null)
                {
                    _highlightedContainer.IsHighlighted = false;
                }

                if (!isHighlighted)
                {
                    _highlightedContainer = null;
                    HighlightedItem = null;
                }
                else
                {
                    _highlightedContainer = container;
                    HighlightedItem = item;

                    if (container != null)
                    {
                        container.IsHighlighted = true;
                    }
                }
            }
            finally
            {
                IsHighlightChangeActive = false;
            }
        }

        internal event EventHandler HighlightChanged;

        internal RibbonGalleryCategory HighlightedCategory
        {
            get
            {
                if (_highlightedContainer != null)
                {
                    return _highlightedContainer.RibbonGalleryCategory;
                }

                return null;
            }

        }

        internal RibbonGalleryItem HighlightedContainer
        {
            get { return _highlightedContainer; }
        }

        private bool IsHighlightChangeActive
        {
            get { return _bits[(int)Bits.IsHighlightChangeActive]; }
            set { _bits[(int)Bits.IsHighlightChangeActive] = value; }
        }

        private bool ShouldForceCoerceHighlightedItem
        {
            get { return _bits[(int)Bits.ShouldForceCoerceHighlightedItem]; }
            set { _bits[(int)Bits.ShouldForceCoerceHighlightedItem] = value; }
        }

        #endregion Highlight

        #region Filtering

        /// <summary>
        ///   Command fired when the user changes the current Gallery filter.
        /// </summary>
        public static RoutedCommand FilterCommand { get; private set; }

        // We only want to execute the FilterCommand if we are in the auto-filtering case and we have _filterMenuButton available.
        private static void FilterCanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            RibbonGallery rg = sender as RibbonGallery;
            if (rg != null &&
                rg.CanUserFilter &&
                rg._filterMenuButton != null &&
                rg.FilterPaneContent == null &&
                rg.FilterPaneContentTemplate == null)
            {
                args.CanExecute = true;
            }
        }

        private static void FilterExecuted(object sender, ExecutedRoutedEventArgs args)
        {
            RibbonGallery rg = (RibbonGallery)sender;
            rg.CurrentFilter = args.Parameter;
            args.Handled = true;
        }

        /// <summary>
        ///   Gets/Sets a value indicating whether the Gallery is filterable.
        /// </summary>
        public bool CanUserFilter
        {
            get { return (bool)GetValue(CanUserFilterProperty); }
            set { SetValue(CanUserFilterProperty, value); }
        }

#if IN_RIBBON_GALLERY
        private static object CoerceCanUserFilter(DependencyObject d, object baseValue)
        {
            RibbonGallery me = (RibbonGallery)d;
            // Don't allow Filter when in InRibbonGalleryMode
            if (me.IsInInRibbonGalleryMode())
            {
                return false;
            }
            return baseValue;
        }
#endif

        /// <summary>
        ///   Using a DependencyProperty as the backing store for CanUserFilter.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty CanUserFilterProperty =
#if IN_RIBBON_GALLERY
            DependencyProperty.Register("CanUserFilter", typeof(bool), typeof(RibbonGallery), new FrameworkPropertyMetadata(false, null, new CoerceValueCallback(CoerceCanUserFilter)));
#else
            DependencyProperty.Register("CanUserFilter", typeof(bool), typeof(RibbonGallery), new FrameworkPropertyMetadata(false));
#endif

        /// <summary>
        ///   Gets/Sets the FilterItemContainerStyle.  This is the container style for the filter items generated from RibbonGalleryCategory Headers.
        /// </summary>
        public Style FilterItemContainerStyle
        {
            get { return (Style)GetValue(FilterItemContainerStyleProperty); }
            set { SetValue(FilterItemContainerStyleProperty, value); }
        }

        /// <summary>
        ///   Style to allow customization of the filter items containers.
        /// </summary>
        public static readonly DependencyProperty FilterItemContainerStyleProperty =
            DependencyProperty.Register("FilterItemContainerStyle", typeof(Style), typeof(RibbonGallery), new FrameworkPropertyMetadata(OnFilterItemContainerStyleChanged));

        /// <summary>
        ///   Gets/Sets the AllFilterItemContainerStyle.  This is the container style for the "All" filter.
        /// </summary>
        public Style AllFilterItemContainerStyle
        {
            get { return (Style)GetValue(AllFilterItemContainerStyleProperty); }
            set { SetValue(AllFilterItemContainerStyleProperty, value); }
        }

        /// <summary>
        ///   Style to allow customization of the "All" filter item's container.
        /// </summary>
        public static readonly DependencyProperty AllFilterItemContainerStyleProperty =
            DependencyProperty.Register("AllFilterItemContainerStyle", typeof(Style), typeof(RibbonGallery), new FrameworkPropertyMetadata(OnFilterItemContainerStyleChanged));

        private static void OnFilterItemContainerStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGallery gallery = (RibbonGallery)d;

            // Reapply the FilterItemContainerStyleSelector coercion.
            gallery.CoerceValue(RibbonGallery.FilterItemContainerStyleSelectorProperty);
            gallery.CoerceValue(RibbonGallery.CurrentFilterStyleProperty);
            gallery.SetHeaderBindingForCurrentFilterItem();
        }

        /// <summary>
        ///   Gets/Sets the FilterItemContainerStyleSelector.
        /// </summary>
        public StyleSelector FilterItemContainerStyleSelector
        {
            get { return (StyleSelector)GetValue(FilterItemContainerStyleSelectorProperty); }
            set { SetValue(FilterItemContainerStyleSelectorProperty, value); }
        }

        /// <summary>
        ///   StyleSelector to allow customization of the filter item containers.
        /// </summary>
        public static readonly DependencyProperty FilterItemContainerStyleSelectorProperty =
            DependencyProperty.Register("FilterItemContainerStyleSelector",
                                        typeof(StyleSelector),
                                        typeof(RibbonGallery),
                                        new FrameworkPropertyMetadata(OnFilterItemContainerStyleSelectorChanged, OnCoerceFilterItemContainerStyleSelector));

        private static void OnFilterItemContainerStyleSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGallery gallery = (RibbonGallery)d;
            gallery.CoerceValue(RibbonGallery.CurrentFilterStyleProperty);
            gallery.SetHeaderBindingForCurrentFilterItem();
        }

        // If FilterItemContainerStyle is specified but FilterItemContainerStyleSelector is unspecified, then we supply our own StyleSelector for FilterItemContainerStyleSelector.
        // This allows a user-supplied FilterItemContainerStyle to reskin the filter items without affecting the "All" item.
        //
        // We also coerce when both FilterItemContainerStyle & FilterItemContainerStyleSelector are set so that FilterItemContainerStyle dominates.  Thus, we need to
        // call this coercion whenever the FilterItemContainerStyle property (or AllFilterItemContainerStyle) changes.
        //
        // Similarly, if AllFilterItemContainerStyle is set, the default StyleSelector dominates FilterItemContainerStyleSelector.
        private static object OnCoerceFilterItemContainerStyleSelector(DependencyObject d, object baseValue)
        {
            RibbonGallery gallery = (RibbonGallery)d;
            if (baseValue == null ||
                gallery.FilterItemContainerStyle != null ||
                gallery.AllFilterItemContainerStyle != null)
            {
                return new RibbonGalleryDefaultFilterItemContainerStyleSelector(gallery);
            }

            return baseValue;
        }

        private class RibbonGalleryDefaultFilterItemContainerStyleSelector : StyleSelector
        {
            private RibbonGallery _gallery;

            internal RibbonGalleryDefaultFilterItemContainerStyleSelector(RibbonGallery inputGallery) : base()
            {
                _gallery = inputGallery;
            }

            public override Style SelectStyle(object item, DependencyObject container)
            {
                if (Object.ReferenceEquals(item, _allFilter))
                {
                    if (_gallery.AllFilterItemContainerStyle != null)
                    {
                        return _gallery.AllFilterItemContainerStyle;
                    }
                }
                else
                {
                    if (_gallery.FilterItemContainerStyle != null)
                    {
                        return _gallery.FilterItemContainerStyle;
                    }
                }

                return base.SelectStyle(item, container);
            }
        }

        /// <summary>
        ///   Gets/Sets the FilterMenuButtonStyle.
        /// </summary>
        public Style FilterMenuButtonStyle
        {
            get { return (Style)GetValue(FilterMenuButtonStyleProperty); }
            set { SetValue(FilterMenuButtonStyleProperty, value); }
        }

        /// <summary>
        ///   Style to allow customization of the Filter menu button.
        /// </summary>
        public static readonly DependencyProperty FilterMenuButtonStyleProperty =
            DependencyProperty.Register("FilterMenuButtonStyle", typeof(Style), typeof(RibbonGallery), new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets/Sets the FilterPaneContent.
        /// </summary>
        public object FilterPaneContent
        {
            get { return (object)GetValue(FilterPaneContentProperty); }
            set { SetValue(FilterPaneContentProperty, value); }
        }

        /// <summary>
        ///   Object to allow customization of the Filter pane.
        /// </summary>
        public static readonly DependencyProperty FilterPaneContentProperty =
            DependencyProperty.Register("FilterPaneContent", typeof(object), typeof(RibbonGallery), new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets/Sets the FilterPaneContentTemplate.
        /// </summary>
        public DataTemplate FilterPaneContentTemplate
        {
            get { return (DataTemplate)GetValue(FilterPaneContentTemplateProperty); }
            set { SetValue(FilterPaneContentTemplateProperty, value); }
        }

        /// <summary>
        ///   DataTemplate to allow customization of the Filter pane.
        /// </summary>
        public static readonly DependencyProperty FilterPaneContentTemplateProperty =
            DependencyProperty.Register("FilterPaneContentTemplate", typeof(DataTemplate), typeof(RibbonGallery), new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets/Sets the FilterItemTemplate.  This specifies the template for filter items generated from RibbonGalleryCategory Headers.
        /// </summary>
        public DataTemplate FilterItemTemplate
        {
            get { return (DataTemplate)GetValue(FilterItemTemplateProperty); }
            set { SetValue(FilterItemTemplateProperty, value); }
        }

        /// <summary>
        ///   DataTemplate to allow customization of the filter items.
        /// </summary>
        public static readonly DependencyProperty FilterItemTemplateProperty =
            DependencyProperty.Register("FilterItemTemplate", typeof(DataTemplate), typeof(RibbonGallery), new FrameworkPropertyMetadata(OnFilterItemTemplateChanged));

        /// <summary>
        ///   Gets/Sets the AllFilterItemTemplate.  This specifies the template for the "All" filter item.
        /// </summary>
        public DataTemplate AllFilterItemTemplate
        {
            get { return (DataTemplate)GetValue(AllFilterItemTemplateProperty); }
            set { SetValue(AllFilterItemTemplateProperty, value); }
        }

        /// <summary>
        ///   DataTemplate to allow customization of the "All" filter item.
        /// </summary>
        public static readonly DependencyProperty AllFilterItemTemplateProperty =
            DependencyProperty.Register("AllFilterItemTemplate", typeof(DataTemplate), typeof(RibbonGallery), new FrameworkPropertyMetadata(OnFilterItemTemplateChanged));

        private static void OnFilterItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGallery gallery = (RibbonGallery)d;

            // Reapply the FilterItemTemplateSelector coercion.
            gallery.CoerceValue(RibbonGallery.FilterItemTemplateSelectorProperty);
            gallery.CoerceValue(RibbonGallery.CurrentFilterTemplateProperty);
        }

        /// <summary>
        ///   Gets/Sets the FilterItemTemplateSelector.
        /// </summary>
        public DataTemplateSelector FilterItemTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(FilterItemTemplateSelectorProperty); }
            set { SetValue(FilterItemTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///   DataTemplateSelector to allow customization of the filter items.
        /// </summary>
        public static readonly DependencyProperty FilterItemTemplateSelectorProperty =
            DependencyProperty.Register("FilterItemTemplateSelector",
                                        typeof(DataTemplateSelector),
                                        typeof(RibbonGallery),
                                        new FrameworkPropertyMetadata(OnFilterItemTemplateSelectorChanged, OnCoerceFilterItemTemplateSelector));

        private static void OnFilterItemTemplateSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGallery gallery = (RibbonGallery)d;
            gallery.CoerceValue(RibbonGallery.CurrentFilterTemplateProperty);
        }

        // If FilterItemTemplate is specified but FilterItemTemplateSelector is unspecified, then we supply our own DataTemplateSelector for FilterItemTemplateSelector.
        // This allows a user-supplied FilterItemTemplate to reskin the filter items without affecting the "All" item.
        //
        // We also coerce when both FilterItemTemplate & FilterItemTemplateSelector are set so that FilterItemTemplate dominates.  Thus, we need to
        // call this coercion whenever the FilterItemTemplate (or AllFilterItemTemplate) property changes.
        //
        // Similarly, if AllFilterItemTemplate is set, then the default DataTemplateSelector dominates FilterItemTemplateSelector.
        private static object OnCoerceFilterItemTemplateSelector(DependencyObject d, object baseValue)
        {
            RibbonGallery gallery = (RibbonGallery)d;
            if (baseValue == null ||
                gallery.FilterItemTemplate != null ||
                gallery.AllFilterItemTemplate != null)
            {
                return new RibbonGalleryDefaultFilterItemTemplateSelector(gallery);
            }

            return baseValue;
        }

        private class RibbonGalleryDefaultFilterItemTemplateSelector : DataTemplateSelector
        {
            private RibbonGallery _gallery;

            internal RibbonGalleryDefaultFilterItemTemplateSelector(RibbonGallery inputGallery) : base()
            {
                _gallery = inputGallery;
            }

            public override DataTemplate SelectTemplate(object item, DependencyObject container)
            {
                if (Object.ReferenceEquals(item, _allFilter))
                {
                    if (_gallery.AllFilterItemTemplate != null)
                    {
                        return _gallery.AllFilterItemTemplate;
                    }
                }
                else
                {
                    if (_gallery.FilterItemTemplate != null)
                    {
                        return _gallery.FilterItemTemplate;
                    }
                }

                return base.SelectTemplate(item, container);
            }
        }

        internal ContentPresenter FilterContentPane
        {
            get
            {
                return _filterContentPane;
            }
        }

        internal RibbonFilterMenuButton FilterMenuButton
        {
            get
            {
                return _filterMenuButton;
            }
        }

        #endregion Filtering

        #region ContainerGeneration

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is RibbonGalleryCategory;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new RibbonGalleryCategory();
        }

        /// <summary>
        ///   Called when the container is being attached to the parent ItemsControl
        /// </summary>
        /// <param name="element"></param>
        /// <param name="item"></param>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            RibbonGalleryCategory category = (RibbonGalleryCategory)element;
            category.RibbonGallery = this;

            if (SelectedValue != null && SelectedItem == null)
            {
                // Synchronize SelectedItem with SelectedValue
                ForceCoerceSelectedValue();
            }

            if (HasItems)
            {
                object selectedItem = SelectedItem;
                for (int index = 0; index < category.Items.Count; index++)
                {
                    RibbonGalleryItem galleryItem = category.ItemContainerGenerator.ContainerFromIndex(index) as RibbonGalleryItem;
                    if (galleryItem != null)
                    {
                        // Set IsSelected to true on GalleryItems that match the SelectedItem
                        if (selectedItem != null)
                        {
                            if (VerifyEqual(selectedItem, category.Items[index]))
                            {
                                galleryItem.IsSelected = true;
                            }
                        }
                        else if (galleryItem.IsSelected)
                        {
                            // If a GalleryItem is marked IsSelected true then synchronize SelectedItem with it
#if RIBBON_IN_FRAMEWORK
                            SetCurrentValue(SelectedItemProperty, category.Items[index]);
#else
                            SelectedItem = category.Items[index];
#endif
                        }
                    }
                }
            }

            // copy templates and styles from this ItemsControl
            var itemTemplate = RibbonHelper.GetValueAndValueSource(category, ItemsControl.ItemTemplateProperty);
            var itemTemplateSelector = RibbonHelper.GetValueAndValueSource(category, ItemsControl.ItemTemplateSelectorProperty);
            var itemStringFormat = RibbonHelper.GetValueAndValueSource(category, ItemsControl.ItemStringFormatProperty);
            var itemContainerStyle = RibbonHelper.GetValueAndValueSource(category, ItemsControl.ItemContainerStyleProperty);
            var itemContainerStyleSelector = RibbonHelper.GetValueAndValueSource(category, ItemsControl.ItemContainerStyleSelectorProperty);
            var alternationCount = RibbonHelper.GetValueAndValueSource(category, ItemsControl.AlternationCountProperty);
            var itemBindingGroup = RibbonHelper.GetValueAndValueSource(category, ItemsControl.ItemBindingGroupProperty);

            base.PrepareContainerForItemOverride(element, item);

            // Call this function to work around a restriction of supporting hetrogenous
            // ItemsCotnrol hierarchy. The method takes care of both ItemsControl and
            // HeaderedItemsControl (in this case) and assign back the default properties
            // whereever appropriate.
            RibbonHelper.IgnoreDPInheritedFromParentItemsControl(
                category,
                this,
                itemTemplate,
                itemTemplateSelector,
                itemStringFormat,
                itemContainerStyle,
                itemContainerStyleSelector,
                alternationCount,
                itemBindingGroup,
                null,
                null,
                null);
        }

        /// <summary>
        ///   Called when the container is being detached from the parent ItemsControl
        /// </summary>
        /// <param name="element"></param>
        /// <param name="item"></param>
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            RibbonGalleryCategory category = (RibbonGalleryCategory)element;

            for (int index = 0; index < category.Items.Count; index++)
            {
                RibbonGalleryItem galleryItem = category.ItemContainerGenerator.ContainerFromIndex(index) as RibbonGalleryItem;
                if (galleryItem != null)
                {
                    object dataItem = category.Items[index];

                    // Turn off selection and highlight on GalleryItems that are being cleared.
                    // Note that we directly call Change[Selection/Highlight] instead of setting
                    // Is[Selected/Highlighted] because we aren't able to get ItemFromContainer
                    // in OnIs[Selected/Highlighted]Changed because the ItemContainerGenerator
                    // has already detached this container.
                    if (galleryItem.IsHighlighted)
                    {
                        galleryItem.RibbonGallery.ChangeHighlight(dataItem, galleryItem, false);
                    }
                    if (galleryItem.IsSelected)
                    {
                        galleryItem.RibbonGallery.ChangeSelection(dataItem, galleryItem, false);
                    }
                }
            }

            category.RibbonGallery = null;
            base.ClearContainerForItemOverride(element, item);
        }

        #endregion ContainerGeneration

        #region DropDownContainer

        /// <summary>
        ///     True if this is the current "Selection" of its parent.
        ///     A gallery is selected when Keyboard focus is within or mouse enters.
        ///
        ///     We use the Ribbon prefix to disambiguate this property from MenuItem.IsSelected.
        /// </summary>
        internal bool RibbonIsSelected
        {
            get { return (bool)GetValue(RibbonIsSelectedProperty); }
            set { SetValue(RibbonIsSelectedProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for RibbonIsSelected property.
        /// </summary>
        internal static readonly DependencyProperty RibbonIsSelectedProperty = RibbonMenuItem.RibbonIsSelectedProperty.AddOwner(
                typeof(RibbonGallery),
                new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnRibbonIsSelectedChanged)));

        private static void OnRibbonIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RibbonGallery)d).RaiseEvent(new RoutedPropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue, RibbonMenuButton.RibbonIsSelectedChangedEvent));
        }

        /// <summary>
        /// Called when the focus changes within the subtree
        /// </summary>
        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);

            if (IsKeyboardFocusWithin)
            {
                if (!RibbonIsSelected)
                {
                    // If an item within us got focus (probably programatically), we need to become selected
                    RibbonIsSelected = true;
                }

#if IN_RIBBON_GALLERY
                // When keyboard-navigating into an InRibbonGallery, transfer focus to PartToggleButton.  When
                // IsInInRibbonMode is true, only PartToggleButton gets focus and RibbonGalleryItems are skipped.
                InRibbonGallery parentInRibbonGallery = ParentInRibbonGallery;
                if (parentInRibbonGallery != null &&
                    parentInRibbonGallery.IsInInRibbonMode)
                {
                    RibbonToggleButton partToggleButton = parentInRibbonGallery.PartToggleButton;
                    if (partToggleButton != null)
                    {
                        Keyboard.Focus(partToggleButton);
                    }
                }
#endif
            }
        }

        #endregion

        #region CollectionChange

        private void RepopulateCategoryFilters()
        {
            _categoryFilters.Clear();

            // Add the "All" filter and make this the current filter.
            _categoryFilters.Add(_allFilter);

            // Search Items collection for categories.
            for (int i = 0; i < this.Items.Count; i++)
            {
                RibbonGalleryCategory category;
                object filterToAdd;

                // If this item is a RibbonGalleryCategory, then add its Header to the filters collection.
                // Otherwise add the item itself.
                if (this.Items[i] is RibbonGalleryCategory)
                {
                    category = (RibbonGalleryCategory)this.Items[i];
                    filterToAdd = category.Header;
                }
                else
                {
                    category = (RibbonGalleryCategory)this.ItemContainerGenerator.ContainerFromIndex(i);
                    filterToAdd = this.Items[i];
                }

                // RibbonGalleryCategories that omit Header are omitted from the filters collection.
                // If category.Header is a string, make sure it is non-empty as well.
                if ((category.Header is string && !String.IsNullOrEmpty((string)category.Header)) ||
                     category.Header != null)
                {
                    _categoryFilters.Add(filterToAdd);
                }
            }

            CurrentFilter = _allFilter;
        }

        private static void OnCurrentFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGallery gallery = (RibbonGallery)d;
            object newFilter = e.NewValue;

            Debug.Assert(gallery.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated);

            for (int i = 0; i < gallery.Items.Count; i++)
            {
                RibbonGalleryCategory category;
                object dataToCompareAgainst;

                // If this item is a RibbonGalleryCategory, then we want to compare against its Header.
                // Otherwise compare against the item itself.
                if (gallery.Items[i] is RibbonGalleryCategory)
                {
                    category = (RibbonGalleryCategory)gallery.Items[i];
                    dataToCompareAgainst = category.Header;
                }
                else
                {
                    category = (RibbonGalleryCategory)gallery.ItemContainerGenerator.ContainerFromIndex(i);
                    dataToCompareAgainst = gallery.Items[i];
                }

                // Show the category if the current filter is the "All" filter or dataToCompareAgainst.
                if (Object.ReferenceEquals(newFilter, _allFilter) ||
                    Object.ReferenceEquals(newFilter, dataToCompareAgainst))
                {
                    category.Visibility = Visibility.Visible;
                }
                else
                {
                    category.Visibility = Visibility.Collapsed;
                }
            }

            gallery.CoerceValue(RibbonGallery.CurrentFilterStyleProperty);
            gallery.SetHeaderBindingForCurrentFilterItem();
            gallery.CoerceValue(RibbonGallery.CurrentFilterTemplateProperty);
        }

        /// <summary>
        ///   The "All" item in a RibbonGallery filter.  This is a localized string.
        ///
        ///   Custom FilterItemTemplateSelector or FilterItemContainerStyleSelector implementations can use this to
        ///   distinguish between the "All" filter item and normal filter items.
        /// </summary>
        public static object AllFilterItem
        {
            get { return _allFilter; }
        }

        /// <summary>
        ///   This method is invoked when the Items property changes.
        /// </summary>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Reset:
                case NotifyCollectionChangedAction.Replace:
                    if (SelectedItem != null)
                    {
                        // Synchronize SelectedItem after Remove, Replace or Reset operations.
                        ForceCoerceSelectedItem();
                    }
                    if (HighlightedItem != null)
                    {
                        // Synchronize HighlightedItem after Remove, Replace or Reset operations.
                        ForceCoerceHighlightedItem();
                    }
                    break;

                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Move:
                    break;
            }
        }

        #endregion CollectionChange

        #region CategoryTemplate

        /// <summary>
        ///     Equivalent of ItemTemplate.
        /// </summary>
        /// <remarks>
        ///     If this property has a non-null value, it will override the value
        ///     of ItemTemplate.
        /// </remarks>
        [BindableAttribute(true)]
        public DataTemplate CategoryTemplate
        {
            get { return (DataTemplate)GetValue(CategoryTemplateProperty); }
            set { SetValue(CategoryTemplateProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for the CategoryStyle property.
        /// </summary>
        public static readonly DependencyProperty CategoryTemplateProperty =
            DependencyProperty.Register("CategoryTemplate", typeof(DataTemplate), typeof(RibbonGallery), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnCategoryTemplateChanged)));

        private static void OnCategoryTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ItemTemplateProperty);
        }

        // Always respect CategoryTemplate property over ItemTemplateProperty
        private static object OnCoerceItemTemplate(DependencyObject d, object baseValue)
        {
            if (!PropertyHelper.IsDefaultValue(d, RibbonGallery.CategoryTemplateProperty))
            {
                return d.GetValue(RibbonGallery.CategoryTemplateProperty);
            }

            return baseValue;
        }

        /// <summary>
        ///     Equivalent of ItemContainerStyle.
        /// </summary>
        /// <remarks>
        ///     If this property has a non-null value, it will override the value
        ///     of ItemContainerStyle.
        /// </remarks>
        public Style CategoryStyle
        {
            get { return (Style)GetValue(CategoryStyleProperty); }
            set { SetValue(CategoryStyleProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for the CategoryStyle property.
        /// </summary>
        public static readonly DependencyProperty CategoryStyleProperty =
            DependencyProperty.Register("CategoryStyle", typeof(Style), typeof(RibbonGallery), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnCategoryStyleChanged)));

        private static void OnCategoryStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ItemContainerStyleProperty);
        }

        // // Always respect CategoryStyleProperty property over ItemConatinerStyleProperty
        private static object OnCoerceItemContainerStyle(DependencyObject d, object baseValue)
        {
            if (!PropertyHelper.IsDefaultValue(d, RibbonGallery.CategoryStyleProperty))
            {
                return d.GetValue(RibbonGallery.CategoryStyleProperty);
            }

            return baseValue;
        }

        #endregion CategoryTemplate

        #region GalleryItemTemplate

        public DataTemplate GalleryItemTemplate
        {
            get { return (DataTemplate)GetValue(GalleryItemTemplateProperty); }
            set { SetValue(GalleryItemTemplateProperty, value); }
        }

        public static readonly DependencyProperty GalleryItemTemplateProperty =
            DependencyProperty.Register("GalleryItemTemplate", typeof(DataTemplate), typeof(RibbonGallery), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyGalleryItemTemplateOrStylePropertyChanged)));

        public Style GalleryItemStyle
        {
            get { return (Style)GetValue(GalleryItemStyleProperty); }
            set { SetValue(GalleryItemStyleProperty, value); }
        }

        public static readonly DependencyProperty GalleryItemStyleProperty =
                        DependencyProperty.Register("GalleryItemStyle", typeof(Style), typeof(RibbonGallery), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyGalleryItemTemplateOrStylePropertyChanged)));

        private static void OnNotifyGalleryItemTemplateOrStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGallery gallery = (RibbonGallery)d;

            for (int index = 0; index < gallery.Items.Count; index++)
            {
                RibbonGalleryCategory category = (RibbonGalleryCategory)gallery.ItemContainerGenerator.ContainerFromIndex(index);

                if (category != null)
                {
                    category.NotifyPropertyChanged(e);
                }
            }
        }

        #endregion GalleryItemTemplate

#if IN_RIBBON_GALLERY
        #region InRibbonGallery

        // Note this sometimes returns null even if the RibbonGallery is indeed the first gallery of an InRibbonGallery.
        // This returns null until the InRibbonGallery's template has been applied (including its ContentPresenter and ItemsPresenter)
        // and the InRibbonGallery.FirstGallery link has been established.
        internal InRibbonGallery ParentInRibbonGallery
        {
            get
            {
                ItemsControl parentItemsControl = ItemsControl.ItemsControlFromItemContainer(this);
                InRibbonGallery parentInRibbonGallery = null;

                if (parentItemsControl != null)
                {
                    parentInRibbonGallery = parentItemsControl as InRibbonGallery;
                }
                else
                {
                    // When an InRibbonGallery's children are specified in XAML, they are part of the logical tree
                    // and it is sufficient to call ItemsControl.ItemsControlFromItemContainer(this) to find the
                    // InRibbonGallery that is the logical parent.
                    // However, in the MVVM scenario we are not part of the logical tree.  When IsDropDownOpen == false,
                    // FirstGallery is hosted in a ContentPresenter and must walk the visual tree to determine the
                    // parent InRibbonGallery.  This will not happen when IsDropDownOpen == true and we are hosted
                    // in a RibbonMenuItemsPanel, because ItemsControl.ItemsControlFromItemContainer(this) also covers
                    // this scenario.
                    ContentPresenter parentContentPresenter = VisualTreeHelper.GetParent(this) as ContentPresenter;
                    if (parentContentPresenter != null)
                    {
                        parentInRibbonGallery = parentContentPresenter.TemplatedParent as InRibbonGallery;
                    }
                }

                if (parentInRibbonGallery != null &&
                    parentInRibbonGallery.FirstGallery == this)
                {
                    return parentInRibbonGallery;
                }

                return null;
            }
        }

        // Check if the Gallery is hosted in and InRibbonGallery and is in InRibbon mode.
        internal bool IsInInRibbonGalleryMode()
        {
            InRibbonGallery parentInRibbonGallery = ParentInRibbonGallery;

            return parentInRibbonGallery != null &&
                   !parentInRibbonGallery.IsCollapsed &&
                   !parentInRibbonGallery.IsDropDownOpen;
        }

        #endregion
#endif

        #region Automation

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonGalleryAutomationPeer(this);
        }

        #endregion Automation

        #region Input Handling

        // The RibbonGallery tracks the position of the mouse relative to
        // itself in order to differentiate cases where the mouse move event
        // is generated as an artifact of elements moving (eg. scrolling) vs
        // the mouse actually moving.  We track this point by listening to the
        // MouseEnter/MouseMove/MouseLeave events.  The pattern is for
        // RibbonGalleryItems to call DidMouseMove in response to the bubbling
        // MouseMove event, which should be routed through the
        // RibbonGalleryItems before it is routed through the RibbonGallery.

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            _localMousePosition = e.GetPosition(this);
            if (!RibbonIsSelected)
            {
                // If it's already focused, make sure it's also selected.
                RibbonIsSelected = true;
            }

            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            _localMousePosition = null;
            if (RibbonIsSelected)
            {
                RibbonIsSelected = false;
            }
            base.OnMouseLeave(e);
        }

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            RibbonGallery gallery = (RibbonGallery)sender;
            gallery._localMousePosition = e.GetPosition(gallery);
        }

        internal bool DidMouseMove(MouseEventArgs e)
        {
            Debug.Assert(e.RoutedEvent == Mouse.MouseMoveEvent || e.RoutedEvent == Mouse.MouseLeaveEvent);
            Debug.Assert(_localMousePosition.HasValue);

            Point currentMousePosition = e.GetPosition(this);
            return currentMousePosition != _localMousePosition;
        }

        private Point? _localMousePosition;

        /// <summary>
        /// Gallery's ScrollViewer handles up/down arrow keys and marks the KeyDown event handled
        /// even if the next focusable element is not its descendant.
        /// The logic here enables navigation to outside the gallery
        /// (for e.g. from the last/first galleryItem to gallery's sibling MenuItem).
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (!e.Handled)
            {
                DependencyObject focusedElement = Keyboard.FocusedElement as DependencyObject;
                OnNavigationKeyDown(e, focusedElement);
            }
        }

        internal void OnNavigationKeyDown(KeyEventArgs e, DependencyObject focusedElement)
        {
            if (e.Key == Key.Down || e.Key == Key.Up)
            {
                FocusNavigationDirection direction = (e.Key == Key.Down) ? FocusNavigationDirection.Down : FocusNavigationDirection.Up;
                DependencyObject predictedFocus = null;

                if (focusedElement != null)
                {
                    // if predictedFocus is not a descendant of the Gallery._scrollViewer
                    // then handle the navigation
                    predictedFocus = RibbonHelper.PredictFocus(focusedElement, direction) as UIElement;
                    if (_scrollViewer != null && predictedFocus != null)
                    {
                        ScrollViewer predictedFocusAncestor = TreeHelper.FindVisualAncestor<ScrollViewer>(predictedFocus);
                        if (predictedFocusAncestor != _scrollViewer)
                        {
                            RibbonHelper.Focus(predictedFocus);
                            e.Handled = true;
                        }
                    }
                }
            }
            else if ((e.Key == Key.Left) == (FlowDirection == FlowDirection.LeftToRight))
            {
                UIElement predictedFocus = null;
                FocusNavigationDirection direction = (FlowDirection == FlowDirection.LeftToRight ? FocusNavigationDirection.Left : FocusNavigationDirection.Right);
                if (focusedElement != null)
                {
                    RibbonMenuItem menuItem = TreeHelper.FindAncestor(this, delegate(DependencyObject d) { return (d is RibbonMenuItem); }) as RibbonMenuItem;
                    if (menuItem != null)
                    {
                        predictedFocus = RibbonHelper.PredictFocus(focusedElement, direction) as UIElement;
                        bool callRibbonMenuItemHandler = false;

                        // If predicted focus of logical left is null or the focused element
                        // itself or if its origin with respect to screen is logically to the
                        // right of focused element, then assume that we are at the left boundary
                        // (any maybe trying to cycle) and hence make the
                        // ancestor RibbonMenuItem handle the event.
                        if (predictedFocus == null ||
                            predictedFocus == focusedElement)
                        {
                            callRibbonMenuItemHandler = true;
                        }
                        else
                        {
                            Point focusedOrigin = new Point();
                            UIElement focusContainer = RibbonHelper.GetContainingUIElement(focusedElement);
                            if (focusContainer != null)
                            {
                                focusedOrigin = focusContainer.PointToScreen(new Point());
                            }
                            Point predictedFocusedOrigin = predictedFocus.PointToScreen(new Point());
                            if ((FlowDirection == FlowDirection.LeftToRight && DoubleUtil.GreaterThan(predictedFocusedOrigin.X, focusedOrigin.X)) ||
                                (FlowDirection == FlowDirection.RightToLeft && DoubleUtil.LessThan(predictedFocusedOrigin.X, focusedOrigin.X)))
                            {
                                callRibbonMenuItemHandler = true;
                            }
                        }

                        if (callRibbonMenuItemHandler)
                        {
                            e.Handled = menuItem.HandleLeftKeyDown(e.OriginalSource as DependencyObject);
                        }
                    }
                }
            }
            else if (e.Key == Key.PageDown || e.Key == Key.PageUp)
            {
                FocusNavigationDirection direction = e.Key == Key.PageDown ? FocusNavigationDirection.Down : FocusNavigationDirection.Up;

                RibbonGalleryItem focusedGalleryItem = focusedElement as RibbonGalleryItem;
                if (focusedGalleryItem != null)
                {
                    RibbonGalleryItem highlightedGalleryItem;
                    e.Handled = RibbonHelper.NavigatePageAndHighlightRibbonGalleryItem(
                        this,
                        focusedGalleryItem,
                        direction,
                        out highlightedGalleryItem);

                    if (highlightedGalleryItem != null)
                    {
                        highlightedGalleryItem.Focus();
                        highlightedGalleryItem.BringIntoView();
                    }
                }
            }
        }

        private static void OnDismissPopupThunk(object sender, RibbonDismissPopupEventArgs e)
        {
            RibbonGallery ribbonGallery = (RibbonGallery)sender;
            ribbonGallery.OnDismissPopup(e);
        }

        private void OnDismissPopup(RibbonDismissPopupEventArgs e)
        {
            if (e.DismissMode == RibbonDismissPopupMode.Always)
            {
                // Stop popup dismissal if the original source
                // is from FilterPane.
                ContentPresenter filterPane = FilterContentPane;
                if (filterPane != null &&
                    RibbonHelper.IsAncestorOf(filterPane, e.OriginalSource as DependencyObject))
                {
                    e.Handled = true;
                }
            }
        }

        #endregion Input Handling

        #region Commanding

        /// <summary>
        ///   Gets or sets the Command that will be executed when the command source is invoked.
        /// </summary>
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for CommandProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
                    DependencyProperty.Register(
                            "Command",
                            typeof(ICommand),
                            typeof(RibbonGallery),
                            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnCommandChanged)));

        /// <summary>
        ///   Gets or sets a user defined data value that can be passed to the command when it is executed.
        /// </summary>
        public object CommandParameter
        {
            get { return (object)GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for CommandParameterProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty =
                    DependencyProperty.Register(
                            "CommandParameter",
                            typeof(object),
                            typeof(RibbonGallery),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets a user defined data value that can be passed to the command when it is previewed.
        /// </summary>
        public object PreviewCommandParameter
        {
            get { return (object)GetValue(PreviewCommandParameterProperty); }
            set { SetValue(PreviewCommandParameterProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for PreviewCommandParameterProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty PreviewCommandParameterProperty =
                    DependencyProperty.Register(
                            "PreviewCommandParameter",
                            typeof(object),
                            typeof(RibbonGallery),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets the object that the command is being executed on.
        /// </summary>
        public IInputElement CommandTarget
        {
            get { return (IInputElement)GetValue(CommandTargetProperty); }
            set { SetValue(CommandTargetProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for CommandTargetProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty CommandTargetProperty =
                    DependencyProperty.Register(
                            "CommandTarget",
                            typeof(IInputElement),
                            typeof(RibbonGallery),
                            new FrameworkPropertyMetadata(null));

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonGallery gallery = (RibbonGallery)d;
            ICommand oldCommand = (ICommand)e.OldValue;
            ICommand newCommand = (ICommand)e.NewValue;

            if (oldCommand != null)
            {
                gallery.UnhookCommand(oldCommand);
            }
            if (newCommand != null)
            {
                gallery.HookCommand(newCommand);
            }

            RibbonHelper.OnCommandChanged(d, e);
        }

        private void HookCommand(ICommand command)
        {
#if RIBBON_IN_FRAMEWORK
            CanExecuteChangedEventManager.AddHandler(command, OnCanExecuteChanged);
#else
            _canExecuteChangedHandler = new EventHandler(OnCanExecuteChanged);
            command.CanExecuteChanged += _canExecuteChangedHandler;
#endif
            UpdateCanExecute();
        }

        private void UnhookCommand(ICommand command)
        {
#if RIBBON_IN_FRAMEWORK
            CanExecuteChangedEventManager.RemoveHandler(command, OnCanExecuteChanged);
#else
            if (_canExecuteChangedHandler != null)
            {
                command.CanExecuteChanged -= _canExecuteChangedHandler;
                _canExecuteChangedHandler = null;
            }
#endif
            UpdateCanExecute();
        }

        private void OnCanExecuteChanged(object sender, EventArgs e)
        {
            UpdateCanExecute();
        }

        private void UpdateCanExecute()
        {
            if (Command != null)
            {
                CanExecute = CommandHelpers.CanExecuteCommandSource(CommandParameter, this);
            }
            else
            {
                CanExecute = true;
            }
        }

        /// <summary>
        ///     Fetches the value of the IsEnabled property
        /// </summary>
        /// <remarks>
        ///     The reason this property is overridden is so that RibbonGallery
        ///     can infuse the value for CanExecute into it.
        /// </remarks>
        protected override bool IsEnabledCore
        {
            get
            {
                return base.IsEnabledCore && CanExecute;
            }
        }

        private bool CanExecute
        {
            get { return _bits[(int)Bits.CanExecute]; ; }
            set
            {
                _bits[(int)Bits.CanExecute] = value;
                CoerceValue(IsEnabledProperty);
            }
        }

#if !RIBBON_IN_FRAMEWORK
        private EventHandler _canExecuteChangedHandler;
#endif

        #endregion Commanding

        #region RibbonControlService Properties

        /// <summary>
        ///     DependencyProperty for SmallImageSource property.
        /// </summary>
        public static readonly DependencyProperty SmallImageSourceProperty =
            RibbonControlService.SmallImageSourceProperty.AddOwner(typeof(RibbonGallery));

        /// <summary>
        ///     ImageSource property which is normally a 16X16 icon.
        /// </summary>
        public ImageSource SmallImageSource
        {
            get { return RibbonControlService.GetSmallImageSource(this); }
            set { RibbonControlService.SetSmallImageSource(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipTitle property.
        /// </summary>
        public static readonly DependencyProperty ToolTipTitleProperty =
            RibbonControlService.ToolTipTitleProperty.AddOwner(typeof(RibbonGallery), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Title text for the tooltip of the control.
        /// </summary>
        public string ToolTipTitle
        {
            get { return RibbonControlService.GetToolTipTitle(this); }
            set { RibbonControlService.SetToolTipTitle(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipDescription property.
        /// </summary>
        public static readonly DependencyProperty ToolTipDescriptionProperty =
            RibbonControlService.ToolTipDescriptionProperty.AddOwner(typeof(RibbonGallery), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Description text for the tooltip of the control.
        /// </summary>
        public string ToolTipDescription
        {
            get { return RibbonControlService.GetToolTipDescription(this); }
            set { RibbonControlService.SetToolTipDescription(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipImageSource property.
        /// </summary>
        public static readonly DependencyProperty ToolTipImageSourceProperty =
            RibbonControlService.ToolTipImageSourceProperty.AddOwner(typeof(RibbonGallery), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Image source for the tooltip of the control.
        /// </summary>
        public ImageSource ToolTipImageSource
        {
            get { return RibbonControlService.GetToolTipImageSource(this); }
            set { RibbonControlService.SetToolTipImageSource(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipFooterTitle property.
        /// </summary>
        public static readonly DependencyProperty ToolTipFooterTitleProperty =
            RibbonControlService.ToolTipFooterTitleProperty.AddOwner(typeof(RibbonGallery), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Title text for the footer of tooltip of the control.
        /// </summary>
        public string ToolTipFooterTitle
        {
            get { return RibbonControlService.GetToolTipFooterTitle(this); }
            set { RibbonControlService.SetToolTipFooterTitle(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipFooterDescription property.
        /// </summary>
        public static readonly DependencyProperty ToolTipFooterDescriptionProperty =
            RibbonControlService.ToolTipFooterDescriptionProperty.AddOwner(typeof(RibbonGallery), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Description text for the footer of the tooltip of the control.
        /// </summary>
        public string ToolTipFooterDescription
        {
            get { return RibbonControlService.GetToolTipFooterDescription(this); }
            set { RibbonControlService.SetToolTipFooterDescription(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for ToolTipFooterImageSource property.
        /// </summary>
        public static readonly DependencyProperty ToolTipFooterImageSourceProperty =
            RibbonControlService.ToolTipFooterImageSourceProperty.AddOwner(typeof(RibbonGallery), new FrameworkPropertyMetadata(new PropertyChangedCallback(RibbonHelper.OnRibbonToolTipPropertyChanged)));

        /// <summary>
        ///     Image source for the footer of the tooltip of the control.
        /// </summary>
        public ImageSource ToolTipFooterImageSource
        {
            get { return RibbonControlService.GetToolTipFooterImageSource(this); }
            set { RibbonControlService.SetToolTipFooterImageSource(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for Ribbon property.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonGallery));

        /// <summary>
        ///     This property is used to access visual style brushes defined on the Ribbon class.
        /// </summary>
        public Ribbon Ribbon
        {
            get { return RibbonControlService.GetRibbon(this); }
        }

        #endregion RibbonControlService Properties

        #region Scrolling

        /// <summary>
        ///  This method allows a particular item to be scrolled into view
        /// </summary>
        /// <remarks>
        ///   Note that this method currently does not support the virtualization
        /// </remarks>
        /// <param name="item"></param>
        public void ScrollIntoView(object item)
        {
            if (item != null)
            {
                // This is a fast path to scroll to the selected or the highlighted item

                if (VerifyEqual(item, SelectedItem) && _selectedContainers.Count > 0)
                {
                    _selectedContainers[0].BringIntoView();
                }
                else if (VerifyEqual(item, HighlightedItem) && _highlightedContainer != null)
                {
                    _highlightedContainer.BringIntoView();
                }
                else
                {
                    // If this a request for an arbitrary item then we need to
                    // find its container and then scroll that into view.

                    RibbonGalleryCategory category = null;
                    RibbonGalleryItem galleryItem = null;

                    bool ignoreItemContainerGeneratorStatus = true;
                    if (ContainsItem(item, ignoreItemContainerGeneratorStatus, out category, out galleryItem) &&
                        galleryItem != null)
                    {
                        galleryItem.BringIntoView();
                    }
                }
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            // A RibbonGallery is always hosted within a drop down control
            // viz. RibbonMenuButton, RibbonComboBox, RibbonMenuItem, etc.
            // In all of these cases we wish to scroll the selected item
            // into view when the drop down first opens. Hence this clause
            // in RibbonGallery.

            RibbonGallery gallery = (RibbonGallery)sender;
            gallery.ScrollIntoView(gallery.SelectedItem);
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            RibbonGallery gallery = (RibbonGallery)sender;

            // Invalidate these layout calculations, so that these are recalculated when drop down is opened.
            // We would need invalidation if any Items are removed from the parent RibbonMenuButton causing its constraint to change.
            gallery.IsArrangeWidthValid = false;
            gallery.IsMaxColumnWidthValid = false;
        }

        internal bool ShouldGalleryItemsAcquireFocus
        {
            get { return _bits[(int)Bits.ShouldGalleryItemsAcquireFocus]; }
            set { _bits[(int)Bits.ShouldGalleryItemsAcquireFocus] = value; }
        }

        internal bool HasHighlightChangedViaMouse
        {
            get { return _bits[(int)Bits.HighlightChangedViaMouse]; }
            set { _bits[(int)Bits.HighlightChangedViaMouse] = value; }
        }

        #endregion Scrolling

        #region QAT

        /// <summary>
        ///   DependencyProperty for QuickAccessToolBarId property.
        /// </summary>
        public static readonly DependencyProperty QuickAccessToolBarIdProperty =
            RibbonControlService.QuickAccessToolBarIdProperty.AddOwner(typeof(RibbonGallery));

        /// <summary>
        ///   This property is used as a unique identifier to link a control in the Ribbon with its counterpart in the QAT.
        /// </summary>
        public object QuickAccessToolBarId
        {
            get { return RibbonControlService.GetQuickAccessToolBarId(this); }
            set { RibbonControlService.SetQuickAccessToolBarId(this, value); }
        }

        /// <summary>
        ///   DependencyProperty for CanAddToQuickAccessToolBarDirectly property.
        /// </summary>
        public static readonly DependencyProperty CanAddToQuickAccessToolBarDirectlyProperty =
            RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty.AddOwner(typeof(RibbonGallery),
            new FrameworkPropertyMetadata(true));


        /// <summary>
        ///   Property determining whether a control can be added to the RibbonQuickAccessToolBar directly.
        /// </summary>
        public bool CanAddToQuickAccessToolBarDirectly
        {
            get { return RibbonControlService.GetCanAddToQuickAccessToolBarDirectly(this); }
            set { RibbonControlService.SetCanAddToQuickAccessToolBarDirectly(this, value); }
        }

        #endregion QAT

        #region Data

        private enum Bits
        {
            IsSelectionChangeActive = 0x1,
            IsHighlightChangeActive = 0x2,
            ShouldForceCoerceSelectedItem = 0x4,
            ShouldForceCoerceSelectedValue = 0x8,
            ShouldForceCoerceHighlightedItem = 0x10,
            IsSynchronizedWithCurrentItemInternal = 0x20,
            CanExecute = 0x40,
            ShouldExecuteCommand = 0x80,
            ShouldGalleryItemsAcquireFocus = 0x100,
            HighlightChangedViaMouse = 0x200,
            FilterMenuButtonTemplateIsBound = 0x400,
        }

        // Packed boolean information
        private BitVector32 _bits = new BitVector32((int)Bits.CanExecute | (int)Bits.ShouldExecuteCommand | (int)Bits.ShouldGalleryItemsAcquireFocus);
        private Collection<RibbonGalleryItem> _selectedContainers = new Collection<RibbonGalleryItem>();
        private RibbonGalleryItem _highlightedContainer;

        // Filtering
        private ObservableCollection<object> _categoryFilters = new ObservableCollection<object>();
        private static object _allFilter = Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.RibbonGallery_AllFilter);
        private const string _filterMenuButtonTemplatePartName = "PART_FilterMenuButton";
        private const string FilterContentPaneTemplatePartName = "PART_FilterContentPane";
        private const string ScrollViewerTemplatePartName = "PART_ScrollViewer";
        private const string ItemsHostPanelName = "ItemsHostPanel";
        private const string ItemsHostName = "ItemsPresenter";
        private ItemsPresenter _itemsPresenter;
        private RibbonFilterMenuButton _filterMenuButton;
        private ContentPresenter _filterContentPane;
        private ScrollViewer _scrollViewer;

        #endregion Data
    }
}
