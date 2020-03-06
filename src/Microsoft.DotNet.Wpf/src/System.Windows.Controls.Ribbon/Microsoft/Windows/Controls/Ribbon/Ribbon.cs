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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Threading;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon.Primitives;
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Automation.Peers;
    using Microsoft.Windows.Controls.Ribbon.Primitives;
#endif
    using MS.Internal;

    #endregion Using declarations

    /// <summary>
    ///   The main Ribbon control which consists of multiple tabs, each of which
    ///   containing groups of controls.  The Ribbon also provides improved context
    ///   menus, enhanced screen tips, and keyboard shortcuts.
    /// </summary>
    [StyleTypedProperty(Property = "ContextualTabGroupStyle", StyleTargetType = typeof(RibbonContextualTabGroup))]
    [StyleTypedProperty(Property = "TabHeaderStyle", StyleTargetType = typeof(RibbonTabHeader))]
    [TemplatePart(Name = Ribbon.ContextualTabGroupItemsControlTemplateName, Type = typeof(RibbonContextualTabGroupItemsControl))]
    [TemplatePart(Name = Ribbon.TitlePanelTemplateName, Type = typeof(RibbonTitlePanel))]
    [TemplatePart(Name = Ribbon.TitleHostTemplateName, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = Ribbon.QatHostTemplateName, Type = typeof(Grid))]
    [TemplatePart(Name = Ribbon.HelpPaneTemplateName, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = Ribbon.ItemsPresenterPopupTemplateName, Type = typeof(Popup))]
    public class Ribbon : Selector
    {
        #region Fields
        
        private const double CollapseWidth = 300.0; // The minimum allowed width before the Ribbon will be collapsed.
        private const double CollapseHeight = 250.0; // The minimum allowed height before the Ribbon will be collapsed.
        private bool _selectedTabClicked = false; // A flag used for tracking whether the selected tab has been clicked recently.
        private Popup _itemsPresenterPopup; // The Popup containing Ribbon's ItemsPresenter.
        private RibbonTabHeaderItemsControl _tabHeaderItemsControl; // The headers items control.
        private ObservableCollection<object> _tabHeaderItemsSource = new ObservableCollection<object>(); // ItemsSource for the headers items control.
        private RibbonContextualTabGroupItemsControl _groupHeaderItemsControl; // Contextual tab group header items control.
        private ObservableCollection<RibbonContextualTabGroup> _tabGroupHeaders;    // Collection of ContextualTabGroups
        private Dictionary<int, int> _tabIndexToDisplayIndexMap = new Dictionary<int, int>(); // A map from collection index to display index of tab items.
        private Dictionary<int, int> _tabDisplayIndexToIndexMap = new Dictionary<int, int>(); // A map from display index to collection index of tab items.
        private double _mouseWheelCumulativeDelta = 0; // The aggregate of mouse wheel delta since the last mouse wheel tab selection change.
        private const double MouseWheelSelectionChangeThreshold = 100; // The threshold of mouse wheel delta to change tab selection.
        UIElement _qatTopHost = null;   // ContentPresenter hosting QuickAccessToolBar
        UIElement _titleHost = null;    // ContentPresenter hosting the Title
        UIElement _helpPaneHost = null; // ContentPresenter hosting the HelpPaneContent
        ItemsPresenter _itemsPresenter = null;
        private bool _inContextMenu = false;
        private bool _retainFocusOnEscape = false;
        KeyTipService.KeyTipFocusEventHandler _keyTipEnterFocusHandler = null;
        KeyTipService.KeyTipFocusEventHandler _keyTipExitRestoreFocusHandler = null;

        private const string ContextualTabGroupItemsControlTemplateName = "PART_ContextualTabGroupItemsControl";
        private const string TitlePanelTemplateName = "PART_TitlePanel";
        private const string TitleHostTemplateName = "PART_TitleHost";
        private const string QatHostTemplateName = "QatTopHost";
        private const string HelpPaneTemplateName = "PART_HelpPane";
        private const string ItemsPresenterPopupTemplateName = "PART_ITEMSPRESENTERPOPUP";

        #endregion

        #region Constuctors

        /// <summary>
        ///   Initializes static members of the Ribbon class.  This also overrides the
        ///   default style and adds command bindings for some Window control commands.
        /// </summary>
        static Ribbon()
        {
            Type ownerType = typeof(Ribbon);
            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            ItemsPanelProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new ItemsPanelTemplate(new FrameworkElementFactory(typeof(RibbonTabsPanel)))));
			FocusManager.IsFocusScopeProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(true));
            BorderBrushProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnBorderBrushChanged)));
            EventManager.RegisterClassHandler(ownerType, Mouse.PreviewMouseDownOutsideCapturedElementEvent, new MouseButtonEventHandler(OnClickThroughThunk));
            EventManager.RegisterClassHandler(ownerType, Mouse.LostMouseCaptureEvent, new MouseEventHandler(OnLostMouseCaptureThunk));
            EventManager.RegisterClassHandler(ownerType, RibbonControlService.DismissPopupEvent, new RibbonDismissPopupEventHandler(OnDismissPopupThunk));
            EventManager.RegisterClassHandler(ownerType, RibbonQuickAccessToolBar.CloneEvent, new RibbonQuickAccessToolBarCloneEventHandler(OnCloneThunk));
            ContextMenuProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(RibbonHelper.OnContextMenuChanged, RibbonHelper.OnCoerceContextMenu));

            CommandManager.RegisterClassCommandBinding(ownerType,
                new CommandBinding(RibbonCommands.AddToQuickAccessToolBarCommand, AddToQATExecuted, AddToQATCanExecute));
            CommandManager.RegisterClassCommandBinding(ownerType,
                new CommandBinding(RibbonCommands.MaximizeRibbonCommand, MaximizeRibbonExecuted, MaximizeRibbonCanExecute));
            CommandManager.RegisterClassCommandBinding(ownerType,
                new CommandBinding(RibbonCommands.MinimizeRibbonCommand, MinimizeRibbonExecuted, MinimizeRibbonCanExecute));
            CommandManager.RegisterClassCommandBinding(ownerType,
                new CommandBinding(RibbonCommands.RemoveFromQuickAccessToolBarCommand, RemoveFromQATExecuted, RemoveFromQATCanExecute));
            CommandManager.RegisterClassCommandBinding(ownerType,
                new CommandBinding(RibbonCommands.ShowQuickAccessToolBarAboveRibbonCommand, ShowQATAboveExecuted, ShowQATAboveCanExecute));
            CommandManager.RegisterClassCommandBinding(ownerType,
                new CommandBinding(RibbonCommands.ShowQuickAccessToolBarBelowRibbonCommand, ShowQATBelowExecuted, ShowQATBelowCanExecute));

            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(KeyboardNavigationMode.Contained));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));
            EventManager.RegisterClassHandler(ownerType, FrameworkElement.ContextMenuOpeningEvent, new ContextMenuEventHandler(OnContextMenuOpeningThunk), true);
            EventManager.RegisterClassHandler(ownerType, FrameworkElement.ContextMenuClosingEvent, new ContextMenuEventHandler(OnContextMenuClosingThunk), true);
        }

        /// <summary>
        ///   Initializes a new instance of the Ribbon class and hooks the Loaded event
        ///   to perform class initialization.
        /// </summary>
        public Ribbon()
        {
            this.Loaded += new RoutedEventHandler(this.OnLoaded);
            PresentationSource.AddSourceChangedHandler(this, new SourceChangedEventHandler(OnSourceChangedHandler));
            RibbonControlService.SetRibbon(this, this);

            // Attach EnterFocus and ExitRestoreFocus events of KeyTip Service
            _keyTipEnterFocusHandler = new KeyTipService.KeyTipFocusEventHandler(OnKeyTipEnterFocus);
            KeyTipService.Current.KeyTipEnterFocus += _keyTipEnterFocusHandler;
            _keyTipExitRestoreFocusHandler = new KeyTipService.KeyTipFocusEventHandler(OnKeyTipExitRestoreFocus);
            KeyTipService.Current.KeyTipExitRestoreFocus += _keyTipExitRestoreFocusHandler;

            ItemContainerGenerator.StatusChanged += new EventHandler(OnItemContainerGeneratorStatusChanged);
            IsVisibleChanged += new DependencyPropertyChangedEventHandler(HandleIsVisibleChanged);
        }

        #endregion

        #region Public Events

        /// <summary>
        ///   Callbacks for the ExpandedEvent.
        /// </summary>
        public event RoutedEventHandler Expanded
        {
            add { AddHandler(ExpandedEvent, value, false); }
            remove { RemoveHandler(ExpandedEvent, value); }
        }

        /// <summary>
        ///   Raised when the Ribbon is expanded (IsCollapsed changes to False).
        /// </summary>
        public static readonly RoutedEvent ExpandedEvent =
                    EventManager.RegisterRoutedEvent(
                            "Expanded",
                            RoutingStrategy.Direct,
                            typeof(RoutedEventHandler),
                            typeof(Ribbon));

        /// <summary>
        ///   Callbacks for the CollapsedEvent.
        /// </summary>
        public event RoutedEventHandler Collapsed
        {
            add { AddHandler(CollapsedEvent, value, false); }
            remove { RemoveHandler(CollapsedEvent, value); }
        }

        /// <summary>
        ///   Raised when the Ribbon is collapsed (IsCollapsed changes to True).
        /// </summary>
        public static readonly RoutedEvent CollapsedEvent =
                    EventManager.RegisterRoutedEvent(
                            "Collapsed",
                            RoutingStrategy.Direct,
                            typeof(RoutedEventHandler),
                            typeof(Ribbon));

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the Visibility for the Icon of the RibbonWindow that contains this Ribbon.
        /// </summary>
        public Visibility WindowIconVisibility
        {
            get { return (Visibility)GetValue(WindowIconVisibilityProperty); }
            set { SetValue(WindowIconVisibilityProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for WindowIconVisibility.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty WindowIconVisibilityProperty = DependencyProperty.Register(
            "WindowIconVisibility", 
            typeof(Visibility), 
            typeof(Ribbon), 
            new UIPropertyMetadata(Visibility.Visible, OnWindowIconVisibilityChanged));

        /// <summary>
        ///   Gets a value indicating whether the Ribbon is currently hosted in a RibbonWindow.
        /// </summary>
        public bool IsHostedInRibbonWindow
        {
            get { return (bool)GetValue(IsHostedInRibbonWindowProperty); }
            private set { SetValue(IsHostedInRibbonWindowPropertyKey, value); }
        }

        /// <summary>
        /// DependencyPropertyKey for read only DependencyProperty IsHostedInRibbonWindow.
        /// </summary>
        private static readonly DependencyPropertyKey IsHostedInRibbonWindowPropertyKey =
                    DependencyProperty.RegisterReadOnly(
                            "IsHostedInRibbonWindow",
                            typeof(bool),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Using a DependencyProperty as the backing store for IsHostedInRibbonWindow.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty IsHostedInRibbonWindowProperty =
            IsHostedInRibbonWindowPropertyKey.DependencyProperty;

        /// <summary>
        ///   Gets or sets the Ribbon's application menu.
        /// </summary>
        public RibbonApplicationMenu ApplicationMenu
        {
            get { return (RibbonApplicationMenu)GetValue(ApplicationMenuProperty); }
            set { SetValue(ApplicationMenuProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for ApplicationMenuProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty ApplicationMenuProperty =
                    DependencyProperty.Register(
                            "ApplicationMenu",
                            typeof(RibbonApplicationMenu),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnApplicationMenuChanged)));

        /// <summary>
        ///   Gets or sets the Ribbon's QuickAccessToolbar.
        /// </summary>
        public RibbonQuickAccessToolBar QuickAccessToolBar
        {
            get { return (RibbonQuickAccessToolBar)GetValue(QuickAccessToolBarProperty); }
            set { SetValue(QuickAccessToolBarProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for QuickAccessToolBarProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty QuickAccessToolBarProperty =
                    DependencyProperty.Register(
                            "QuickAccessToolBar",
                            typeof(RibbonQuickAccessToolBar),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnQuickAccessToolBarChanged)));

        public object HelpPaneContent
        {
            get { return GetValue(HelpPaneContentProperty); }
            set { SetValue(HelpPaneContentProperty, value); }
        }

        public static readonly DependencyProperty HelpPaneContentProperty =
             DependencyProperty.Register(
                            "HelpPaneContent",
                            typeof(object),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(null));

        public DataTemplate HelpPaneContentTemplate
        {
            get { return (DataTemplate)GetValue(HelpPaneContentTemplateProperty); }
            set { SetValue(HelpPaneContentTemplateProperty, value); }
        }

        public static readonly DependencyProperty HelpPaneContentTemplateProperty =
             DependencyProperty.Register(
                            "HelpPaneContentTemplate",
                            typeof(DataTemplate),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(null));
        

        /// <summary>
        ///   Gets or sets a value indicating whether the Ribbon is minimized.  When the Ribbon
        ///   is minimized its tabs must be clicked in order for their contents to be displayed
        ///   in a Popup.
        /// </summary>
        public bool IsMinimized
        {
            get { return (bool)GetValue(IsMinimizedProperty); }
            set { SetValue(IsMinimizedProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for IsMinimizedProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty IsMinimizedProperty =
                    DependencyProperty.Register(
                            "IsMinimized",
                            typeof(bool),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsMinimizedChanged), new CoerceValueCallback(CoerceIsMinimized)));

        /// <summary>
        ///   Gets or sets a value indicating whether the RibbonTab's Popup is displayed.
        /// </summary>
        public bool IsDropDownOpen
        {
            get { return (bool)GetValue(IsDropDownOpenProperty); }
            set { SetValue(IsDropDownOpenProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for IsDropDownOpenProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty IsDropDownOpenProperty =
                    DependencyProperty.Register(
                            "IsDropDownOpen",
                            typeof(bool),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsDropDownOpenChanged), new CoerceValueCallback(OnCoerceIsDropDownOpen)));

        /// <summary>
        ///   Gets/Sets a value indicating whether the Ribbon is collapsed.
        /// </summary>
        public bool IsCollapsed
        {
            get { return (bool)GetValue(IsCollapsedProperty); }
            set { SetValue(IsCollapsedProperty, value); }
        }

        /// <summary>
        ///     Using a DependencyProperty as the backing store for IsCollapsed.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty IsCollapsedProperty =
            DependencyProperty.Register("IsCollapsed", typeof(bool), typeof(Ribbon), 
                new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsCollapsedChanged), new CoerceValueCallback(CoerceIsCollapsed)));

        /// <summary>
        ///   Gets or sets the Title of the Ribbon.
        /// </summary>
        public object Title
        {
            get { return GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for TitleProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
                    DependencyProperty.Register(
                            "Title",
                            typeof(object),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(null, null, new CoerceValueCallback(OnCoerceTitle)));

        public DataTemplate TitleTemplate
        {
            get { return (DataTemplate)GetValue(TitleTemplateProperty); }
            set { SetValue(TitleTemplateProperty, value); }
        }

        public static readonly DependencyProperty TitleTemplateProperty =
                    DependencyProperty.Register(
                            "TitleTemplate",
                            typeof(DataTemplate),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets a value indicating whether to show the QuickAccessToolbar on top of the Ribbon.
        /// </summary>
        public bool ShowQuickAccessToolBarOnTop
        {
            get { return (bool)GetValue(ShowQuickAccessToolBarOnTopProperty); }
            set { SetValue(ShowQuickAccessToolBarOnTopProperty, value); }
        }

        /// <summary>
        ///   Using a DependencyProperty as the backing store for ShowQuickAccessToolbarOnTopProperty.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty ShowQuickAccessToolBarOnTopProperty =
                    DependencyProperty.Register(
                            "ShowQuickAccessToolBarOnTop",
                            typeof(bool),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(true));

        /// <summary>
        /// ItemsSource for ContextualTabGroups
        /// </summary>
        public IEnumerable ContextualTabGroupsSource
        {
            get { return (IEnumerable)GetValue(ContextualTabGroupsSourceProperty);  }
            set { SetValue(ContextualTabGroupsSourceProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the ContextualTabGroupsSource property.
        /// </summary>
        public static readonly DependencyProperty ContextualTabGroupsSourceProperty
                                                = DependencyProperty.Register("ContextualTabGroupsSource",
                                                typeof(IEnumerable),
                                                typeof(Ribbon),
                                                new FrameworkPropertyMetadata((IEnumerable)null, new PropertyChangedCallback(OnContextualTabGroupsSourceChanged)));

        ///<summary>
        ///  Gets a Collection of the Ribbon's RibbonContextualTabGroups.
        ///</summary>
        [Bindable(true)]
        public Collection<RibbonContextualTabGroup> ContextualTabGroups
        {
            get
            {
                if (_tabGroupHeaders == null)
                {
                    _tabGroupHeaders = new ObservableCollection<RibbonContextualTabGroup>();
                    _tabGroupHeaders.CollectionChanged += new NotifyCollectionChangedEventHandler(this.OnContextualTabGroupsCollectionChanged);
                }

                return _tabGroupHeaders;
            }
        }

        public DataTemplate ContextualTabGroupHeaderTemplate
        {
            get { return (DataTemplate)GetValue(ContextualTabGroupHeaderTemplateProperty); }
            set { SetValue(ContextualTabGroupHeaderTemplateProperty, value); }
        }

        public static readonly DependencyProperty ContextualTabGroupHeaderTemplateProperty =
            DependencyProperty.Register("ContextualTabGroupHeaderTemplate", typeof(DataTemplate), typeof(Ribbon), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyContextualTabGroupPropertyChanged)));

        public Style ContextualTabGroupStyle
        {
            get { return (Style)GetValue(ContextualTabGroupStyleProperty); }
            set { SetValue(ContextualTabGroupStyleProperty, value); }
        }

        public static readonly DependencyProperty ContextualTabGroupStyleProperty =
                        DependencyProperty.Register("ContextualTabGroupStyle", typeof(Style), typeof(Ribbon), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyContextualTabGroupPropertyChanged)));

        /// <summary>
        ///   Gets or sets a value of the BorderBrush brush used in a "Hover" state of the Ribbon controls.
        /// </summary>
        public Brush MouseOverBorderBrush
        {
            get { return (Brush)GetValue(MouseOverBorderBrushProperty); }
            set { SetValue(MouseOverBorderBrushProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for MouseOverBorderBrush property.
        /// </summary>
        public static readonly DependencyProperty MouseOverBorderBrushProperty =
                    DependencyProperty.Register(
                            "MouseOverBorderBrush",
                            typeof(Brush),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets a value of the background brush used in a "Hover" state of the Ribbon controls.
        /// </summary>
        public Brush MouseOverBackground
        {
            get { return (Brush)GetValue(MouseOverBackgroundProperty); }
            set { SetValue(MouseOverBackgroundProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for MouseOverBackground property.
        /// </summary>
        public static readonly DependencyProperty MouseOverBackgroundProperty =
                    DependencyProperty.Register(
                            "MouseOverBackground",
                            typeof(Brush),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets a value of the BorderBrush brush used in a "Pressed" state of the Ribbon controls.
        /// </summary>
        public Brush PressedBorderBrush
        {
            get { return (Brush)GetValue(PressedBorderBrushProperty); }
            set { SetValue(PressedBorderBrushProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for PressedBorderBrush property.
        /// </summary>
        public static readonly DependencyProperty PressedBorderBrushProperty =
                    DependencyProperty.Register(
                            "PressedBorderBrush",
                            typeof(Brush),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets a value of the background brush used in a "Pressed" state of the Ribbon controls.
        /// </summary>
        public Brush PressedBackground
        {
            get { return (Brush)GetValue(PressedBackgroundProperty); }
            set { SetValue(PressedBackgroundProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for PressedBackground property.
        /// </summary>
        public static readonly DependencyProperty PressedBackgroundProperty =
                    DependencyProperty.Register(
                            "PressedBackground",
                            typeof(Brush),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets a value of the BorderBrush brush used in a "Checked" state of the Ribbon controls.
        /// </summary>
        public Brush CheckedBorderBrush
        {
            get { return (Brush)GetValue(CheckedBorderBrushProperty); }
            set { SetValue(CheckedBorderBrushProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for CheckedBorderBrush property.
        /// </summary>
        public static readonly DependencyProperty CheckedBorderBrushProperty =
                    DependencyProperty.Register(
                            "CheckedBorderBrush",
                            typeof(Brush),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets a value of the background brush used in a "Checked" state of the Ribbon controls.
        /// </summary>
        public Brush CheckedBackground
        {
            get { return (Brush)GetValue(CheckedBackgroundProperty); }
            set { SetValue(CheckedBackgroundProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for CheckedBackground property.
        /// </summary>
        public static readonly DependencyProperty CheckedBackgroundProperty =
                    DependencyProperty.Register(
                            "CheckedBackground",
                            typeof(Brush),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets a value of the BorderBrush brush used in a "Focused" state of the Ribbon controls.
        ///   To place keyboard focus on a ribbon control, press ALT-"KeyTip letter" and navigate with arrow keys.
        /// </summary>
        public Brush FocusedBorderBrush
        {
            get { return (Brush)GetValue(FocusedBorderBrushProperty); }
            set { SetValue(FocusedBorderBrushProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for FocusedBorderBrush property.
        /// </summary>
        public static readonly DependencyProperty FocusedBorderBrushProperty =
                    DependencyProperty.Register(
                            "FocusedBorderBrush",
                            typeof(Brush),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///   Gets or sets a value of the background brush used in a "Focused" state of the Ribbon controls.
        /// </summary>
        public Brush FocusedBackground
        {
            get { return (Brush)GetValue(FocusedBackgroundProperty); }
            set { SetValue(FocusedBackgroundProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for FocusedBackground property.
        /// </summary>
        public static readonly DependencyProperty FocusedBackgroundProperty =
                    DependencyProperty.Register(
                            "FocusedBackground",
                            typeof(Brush),
                            typeof(Ribbon),
                            new FrameworkPropertyMetadata(null));



        public Style TabHeaderStyle
        {
            get { return (Style)GetValue(TabHeaderStyleProperty); }
            set { SetValue(TabHeaderStyleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TabHeaderStyle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TabHeaderStyleProperty =
            DependencyProperty.Register("TabHeaderStyle", typeof(Style), typeof(Ribbon), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyTabHeaderPropertyChanged)));

        public DataTemplate TabHeaderTemplate
        {
            get { return (DataTemplate)GetValue(TabHeaderTemplateProperty); }
            set { SetValue(TabHeaderTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TabHeaderTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TabHeaderTemplateProperty =
            DependencyProperty.Register("TabHeaderTemplate", typeof(DataTemplate), typeof(Ribbon), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnNotifyTabHeaderPropertyChanged)));

        #endregion

        #region Private Data

        /// <summary>
        ///   Cached Window hosting the Ribbon.
        /// </summary>
        private Window _window = null;
		
        #endregion

        #region Internal Properties

        /// <summary>
        ///     RibbonTitlePanel instance for this Ribbon.
        /// </summary>
        internal RibbonTitlePanel RibbonTitlePanel
        {
            get;
            private set;
        }
        
        /// <summary>
        ///     RibbonContextualTabGroupItemsControl instance for this Ribbon.
        /// </summary>
        internal RibbonContextualTabGroupItemsControl ContextualTabGroupItemsControl
        {
            get
            {
                return _groupHeaderItemsControl;
            }
        }

        internal UIElement QatTopHost
        {
            get
            {
                return _qatTopHost;
            }
        }

        internal UIElement TitleHost
        {
            get
            {
                return _titleHost;
            }
        }

        internal UIElement HelpPaneHost
        {
            get { return _helpPaneHost; }
        }

        /// <summary>
        ///     A map between collection index and display index of tab items.
        /// </summary>
        internal Dictionary<int, int> TabIndexToDisplayIndexMap
        {
            get
            {
                return _tabIndexToDisplayIndexMap;
            }
        }

        /// <summary>
        ///     A map between display index and collection index of tab items.
        /// </summary>
        internal Dictionary<int, int> TabDisplayIndexToIndexMap
        {
            get
            {
                return _tabDisplayIndexToIndexMap;
            }
        }

        /// <summary>
        ///     RibbonTabHeaderItemsControl instance of this Ribbon.
        /// </summary>
        internal RibbonTabHeaderItemsControl RibbonTabHeaderItemsControl
        {
            get
            {
                return _tabHeaderItemsControl;
            }
        }

        internal Popup ItemsPresenterPopup
        {
            get
            {
                return _itemsPresenterPopup;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///   Invoked whenever the control's template is applied.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _itemsPresenter = GetTemplateChild("ItemsPresenter") as ItemsPresenter;
            _itemsPresenterPopup = this.GetTemplateChild(ItemsPresenterPopupTemplateName) as Popup;

            _tabHeaderItemsControl = this.GetTemplateChild("TabHeaderItemsControl") as RibbonTabHeaderItemsControl;
            if (_tabHeaderItemsControl != null && _tabHeaderItemsControl.ItemsSource == null)
            {
                _tabHeaderItemsControl.ItemsSource = _tabHeaderItemsSource;
            }

            _groupHeaderItemsControl = this.GetTemplateChild(Ribbon.ContextualTabGroupItemsControlTemplateName) as RibbonContextualTabGroupItemsControl;
            if (_groupHeaderItemsControl != null && _groupHeaderItemsControl.ItemsSource == null)
            {
                if (ContextualTabGroupsSource != null && ContextualTabGroups.Count > 0)
                {
                    throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.Ribbon_ContextualTabHeadersSourceInvalid));
                }
                if (ContextualTabGroupsSource != null)
                {
                    ContextualTabGroupItemsControl.ItemsSource = ContextualTabGroupsSource;
                }
                else if (ContextualTabGroups != null)
                {
                    ContextualTabGroupItemsControl.ItemsSource = ContextualTabGroups;
                }
            }

            this.RibbonTitlePanel = this.GetTemplateChild(Ribbon.TitlePanelTemplateName) as RibbonTitlePanel;
            _qatTopHost = this.GetTemplateChild(Ribbon.QatHostTemplateName) as UIElement;
            _titleHost = this.GetTemplateChild(Ribbon.TitleHostTemplateName) as UIElement;
            _helpPaneHost = this.GetTemplateChild(Ribbon.HelpPaneTemplateName) as UIElement;

            PropertyHelper.TransferProperty(this, ContextMenuProperty);   // Coerce to get a default ContextMenu if none has been specified.
            PropertyHelper.TransferProperty(this, RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        ///     A callback for handling clicks to a RibbonTabHeader.
        /// </summary>
        /// <param name="ribbonTab">The RibbonTabHeader that was clicked.</param>
        /// <param name="e">The event data.</param>
        internal void NotifyMouseClickedOnTabHeader(RibbonTabHeader tabHeader, MouseButtonEventArgs e)
        {
            if (_tabHeaderItemsControl == null)
                return;

            int index = _tabHeaderItemsControl.ItemContainerGenerator.IndexFromContainer(tabHeader);

            if (e.ClickCount == 1)
            {
                // Single clicking should:
                //
                // 1. If maximized, select the tab for clicked tabheader.
                // 2. If minimized and clicking the previously selected tabheader.
                //    * Toggle the pop-up for the selected tab.
                // 3. If minimized and clicking an un-selected tabheader.
                //    * Display the pop-up for the clicked tab.
                if ((SelectedIndex < 0) || SelectedIndex != index)
                {
                    this.SelectedIndex = index;
                    if (this.IsMinimized)
                    {
                        this.IsDropDownOpen = true;
                    }

                    _selectedTabClicked = false;
                }
                else
                {
                    if (this.IsMinimized)
                    {
                        this.IsDropDownOpen = !this.IsDropDownOpen;
                    }

                    _selectedTabClicked = true;
                }
            }
            else if (e.ClickCount == 2)
            {
                // Double-clicking should:
                //
                // 1. If clicking a tab that is being clicked in its 'selected' state for
                //    the second time, toggle its 'IsMinimized' behavior.
                // 2. Otherwise do nothing.
                if (_selectedTabClicked == true || this.IsMinimized)
                {
                    IsMinimized = !IsMinimized;
                    IsDropDownOpen = false;
                    _selectedTabClicked = false;
                }
                else
                {
                    _selectedTabClicked = true;
                }
            }
            else if (e.ClickCount == 3)
            {
                // Triple-clicking should do the following for initial conditions (1st click):
                //
                // 1. If minimized:
                //    * Maximize and select the tab.
                // 2. If maximized and the tab was initially selected.
                //    * Minimize and display the pop-up.
                // 3. If maximized and the tab was NOT initially selected.
                //    * Minimize do not display any pop-ups.
                if (_selectedTabClicked == true)
                {
                    IsMinimized = !IsMinimized;
                    IsDropDownOpen = false;
                }
                else
                {
                    this.IsDropDownOpen = true;
                }
            }
        }

        /// <summary>
        ///   Notify the Ribbon that the RibbonContextualTabGroup was clicked.
        /// </summary>
        /// <param name="group">The RibbonContextualTabGroup that was clicked.</param>
        internal void NotifyMouseClickedOnContextualTabGroup(RibbonContextualTabGroup tabGroupHeader)
        {
            RibbonTab firstVisibleTab = tabGroupHeader.FirstVisibleTab;
            if (firstVisibleTab != null)
            {
                // If Ribbon is minimized - we should open it first
                IsMinimized = false;

                // Select first visible tab of the group
                firstVisibleTab.IsSelected = true;
            }
        }

        /// <summary>
        ///     Notify the Ribbon that the ContextualTabGroupHeader property changed of RibbonTab
        /// </summary>
        internal void NotifyTabContextualTabGroupHeaderChanged()
        {
            if (_tabHeaderItemsControl != null)
            {
                Panel headerItemsHost = _tabHeaderItemsControl.InternalItemsHost;
                if (headerItemsHost != null)
                {
                    headerItemsHost.InvalidateMeasure();
                    headerItemsHost.InvalidateArrange();
                }
            }
        }

        /// <summary>
        ///     Notify the Ribbon that the Header property changed on RibbonTab.
        /// </summary>
        internal void NotifyTabHeaderChanged()
        {
            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                RefreshHeaderCollection();
            }
        }

        internal void NotifyTabHeadersScrollOwnerChanged(ScrollViewer oldScrollViewer, ScrollViewer newScrollViewer)
        {
            if (oldScrollViewer != null )
            {
                oldScrollViewer.ScrollChanged -= new ScrollChangedEventHandler(OnTabHeadersScrollChanged);
            }
            if (newScrollViewer != null)
            {
                newScrollViewer.ScrollChanged += new ScrollChangedEventHandler(OnTabHeadersScrollChanged);
            }
        }

        private void OnTabHeadersScrollChanged(object d, ScrollChangedEventArgs e)
        {
            if (ContextualTabGroupItemsControl != null)
            {
                // When scrollbars appear for the TabHeaders, collapse the ContextualTabGroups. 
                ContextualTabGroupItemsControl.ForceCollapse = !(DoubleUtil.GreaterThanOrClose(e.ViewportWidth, e.ExtentWidth));
            }
        }
        
        #endregion

        #region Protected Properties

        /// <summary>
        ///   Gets an enumerator for the Ribbon's logical children.
        /// </summary>
#if RIBBON_IN_FRAMEWORK
        protected internal override IEnumerator LogicalChildren
#else
        protected override IEnumerator LogicalChildren
#endif
        {
            get
            {
                return this.GetLogicalChildren();
            }
        }

        private IEnumerator<object> GetLogicalChildren()
        {
            IEnumerator children = base.LogicalChildren;
            while (children.MoveNext())
                yield return children.Current;
            
            if (this.ApplicationMenu != null)
                yield return this.ApplicationMenu;

            if (this.QuickAccessToolBar != null)
                yield return this.QuickAccessToolBar;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        ///   Called when the MouseWheel changes position while the mouse pointer is over the
        ///   Ribbon.  In this case, the MouseWheelEvent is used to indicate that we should
        ///   iterate to the previous or next tab.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (!this.IsMinimized && (SelectedIndex >= 0) && (Mouse.Captured == this || Mouse.Captured == null))
            {
                int selectedTabIndex = SelectedIndex;
                _mouseWheelCumulativeDelta += e.Delta;

                // Slow down the mouse wheel selection by waiting
                // to change the selection until a cumulative delta
                // is attained.
                if (DoubleUtil.GreaterThan(Math.Abs(_mouseWheelCumulativeDelta), MouseWheelSelectionChangeThreshold))
                {
                    if (_mouseWheelCumulativeDelta < 0)
                    {
                        // select the tab whose display index is 1 greater than current.
                        int displayIndex = GetTabDisplayIndexForIndex(selectedTabIndex);
                        if (displayIndex >= 0)
                        {
                            displayIndex++;
                            int newSelectedIndex = GetTabIndexForDisplayIndex(displayIndex);
                            if (newSelectedIndex >= 0)
                            {
                                SelectedIndex = newSelectedIndex;
                                if (_tabHeaderItemsControl != null)
                                {
                                    _tabHeaderItemsControl.ScrollIntoView(SelectedIndex);
                                }
                            }
                        }
                    }
                    else
                    {
                        // select the tab whose display index is 1 less than current.
                        int displayIndex = GetTabDisplayIndexForIndex(selectedTabIndex);
                        if (displayIndex >= 0)
                        {
                            displayIndex--;
                            int newSelectedIndex = GetTabIndexForDisplayIndex(displayIndex);
                            if (newSelectedIndex >= 0)
                            {
                                SelectedIndex = newSelectedIndex;
                                if (_tabHeaderItemsControl != null)
                                {
                                    _tabHeaderItemsControl.ScrollIntoView(SelectedIndex);
                                }
                            }
                        }
                    }
                    _mouseWheelCumulativeDelta = 0;
                }
                e.Handled = true;
            }

            base.OnPreviewMouseWheel(e);
        }

        /// <summary>
        ///     Generate RibbonTab as the item container.
        /// </summary>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new RibbonTab();
        }

        /// <summary>
        ///     An item is its own container if it is a RibbonTab
        /// </summary>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return (item is RibbonTab);
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            ItemsControl childItemsControl = element as ItemsControl;
            if (childItemsControl != null)
            {
                // copy templates and styles from this ItemsControl
                var itemTemplate = RibbonHelper.GetValueAndValueSource(childItemsControl, ItemsControl.ItemTemplateProperty);
                var itemTemplateSelector = RibbonHelper.GetValueAndValueSource(childItemsControl, ItemsControl.ItemTemplateSelectorProperty);
                var itemStringFormat = RibbonHelper.GetValueAndValueSource(childItemsControl, ItemsControl.ItemStringFormatProperty);
                var itemContainerStyle = RibbonHelper.GetValueAndValueSource(childItemsControl, ItemsControl.ItemContainerStyleProperty);
                var itemContainerStyleSelector = RibbonHelper.GetValueAndValueSource(childItemsControl, ItemsControl.ItemContainerStyleSelectorProperty);
                var alternationCount = RibbonHelper.GetValueAndValueSource(childItemsControl, ItemsControl.AlternationCountProperty);
                var itemBindingGroup = RibbonHelper.GetValueAndValueSource(childItemsControl, ItemsControl.ItemBindingGroupProperty);
                HeaderedItemsControl headeredItemsControl = childItemsControl as HeaderedItemsControl;
                var headerTemplate = RibbonHelper.GetValueAndValueSource(headeredItemsControl, HeaderedItemsControl.HeaderTemplateProperty);
                var headerTemplateSelector = RibbonHelper.GetValueAndValueSource(headeredItemsControl, HeaderedItemsControl.HeaderTemplateSelectorProperty);
                var headerStringFormat = RibbonHelper.GetValueAndValueSource(headeredItemsControl, HeaderedItemsControl.HeaderStringFormatProperty);

    
                base.PrepareContainerForItemOverride(element, item);

                // Call this function to work around a restriction of supporting hetrogenous 
                // ItemsCotnrol hierarchy. The method takes care of both ItemsControl and
                // HeaderedItemsControl (in this case) and assign back the default properties
                // whereever appropriate.
                RibbonHelper.IgnoreDPInheritedFromParentItemsControl(
                    childItemsControl,
                    this,
                    itemTemplate,
                    itemTemplateSelector,
                    itemStringFormat,
                    itemContainerStyle,
                    itemContainerStyleSelector,
                    alternationCount,
                    itemBindingGroup,
                    headerTemplate,
                    headerTemplateSelector,
                    headerStringFormat);
            }
            else
            {
                base.PrepareContainerForItemOverride(element, item);
            }

            RibbonTab container = element as RibbonTab;
            if (container != null)
            {
                container.PrepareRibbonTab();
            }
        }
        /// <summary>
        ///     Gets called when items change on this itemscontrol.
        ///     Syncs header collection accordingly.
        /// </summary>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            if (e.Action == NotifyCollectionChangedAction.Remove ||
                e.Action == NotifyCollectionChangedAction.Replace ||
                e.Action == NotifyCollectionChangedAction.Reset)
            {
                InitializeSelection();
            }

            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated &&
                e.Action == NotifyCollectionChangedAction.Move ||
                e.Action == NotifyCollectionChangedAction.Remove)
            {
                // Only Move and Remove actions require us to explicitly refresh the header collection, since Add,
                // Replace, and Reset actions already refresh the header collection through container generation.
                RefreshHeaderCollection();
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonAutomationPeer(this);
        }

        /// <summary>
        ///     Gets called when selection changes.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                // Selector.CanSelectMultiple is true by default and is internal.
                // Force single selection by setting the selected item.
                // This makes RibbonTab.IsSelected work appropriately.
                SelectedItem = e.AddedItems[0];

                if (IsDropDownOpen)
                {
                    // Recapture and focus when selection changes.
                    RibbonHelper.AsyncSetFocusAndCapture(this,
                        delegate() { return IsDropDownOpen; },
                        this,
                        _itemsPresenterPopup.TryGetChild());
                }
            }
        }

        #endregion

        #region Private Classes

        /// <summary>
        ///     If no Header is set on RibbonTab class, a object
        ///     of this class gets created and used by default in
        ///     RibbonTabHeaderItemsCollection.
        /// </summary>
        private class SingleSpaceObject
        {
            public override string ToString()
            {
                return " ";
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///   Called when the source in which the Ribbon is hosted changes.
        /// </summary>
        /// <param name="o">The Ribbon whose source has changed.</param>
        /// <param name="e">Event args for the change.</param>
        private static void OnSourceChangedHandler(object o, SourceChangedEventArgs e)
        {
            Ribbon rib = (Ribbon)o;

            // Unhook handlers if the previous container was a Window.
            if (e.OldSource is HwndSource &&
                rib._window != null)
            {
                rib.UnhookWindowListeners(rib._window);
                rib._window = null;
            }

            // Hook up new handlers if the new container is an Window.
            if (e.NewSource != null &&
                e.NewSource.RootVisual is Window)
            {
                rib._window = (Window)e.NewSource.RootVisual;
                rib.HookWindowListeners(rib._window);
            }
        }

        private void UnhookWindowListeners(Window win)
        {
            win.SizeChanged -= new SizeChangedEventHandler(this.OnWindowSizeChanged);
            this.IsCollapsed = false;

            if (CheckIfWindowIsRibbonWindow(win))
            {
                this.IsHostedInRibbonWindow = false;
                RibbonWindow rw = (RibbonWindow)win;
                rw.TitleChanged -= new EventHandler(this.OnRibbonWindowTitleChanged);
                CoerceValue(TitleProperty);
            }
        }

        private void HookWindowListeners(Window win)
        {
            // If the Window is loaded, run logic to set IsCollapsed=true if the window is not large enough to display the Ribbon.
            if (win.IsLoaded)
            {
                OnWindowSizeChanged(win, null);
            }

            win.SizeChanged += new SizeChangedEventHandler(this.OnWindowSizeChanged);

            if (CheckIfWindowIsRibbonWindow(win))
            {
                this.IsHostedInRibbonWindow = true;
                RibbonWindow rw = (RibbonWindow)win;
                rw.TitleChanged += new EventHandler(this.OnRibbonWindowTitleChanged);
                CoerceValue(TitleProperty);     // perform Title coercion immediately as well
                rw.ChangeIconVisibility(this.WindowIconVisibility);
            }
        }

        /// <summary>
        /// Property Changed CallBack for IconVisibility. This call back handler 
        /// calls ChangeRibbonWindowIconVisibility which propogates the changes to the Ribbon window.
        /// </summary>
        /// <param name="d">The Sender</param>
        /// <param name="e">DependencyPropertyChangedEventArgs For the changed event</param>
        private static void OnWindowIconVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Ribbon rib = (Ribbon) d;
            if (rib != null)
            {
                if (rib.IsHostedInRibbonWindow)
                {
                    RibbonWindow rw = (RibbonWindow)rib._window;
                    rw.ChangeIconVisibility(rib.WindowIconVisibility);
                }
            }
        }

        /// <summary>
        ///   Called when the IsOpen property changes.  This means that the one of the RibbonTab's
        ///   popups was either opened or closed.
        /// </summary>
        /// <param name="sender">The Ribbon whose tab opened or closed its popup.</param>
        /// <param name="e">The event data.</param>
        private static void OnIsDropDownOpenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Ribbon ribbon = (Ribbon)sender;

            if (ribbon.IsMinimized)
            {
                // If the drop down is closed due to
                // an action of context menu or if the 
                // ContextMenu for a parent  
                // was opened by right clicking this 
                // instance then ContextMenuClosed 
                // event is never raised. 
                // Hence reset the flag.
                ribbon._inContextMenu = false;
                ribbon.ContextMenuOriginalSource = null;
            }

            RibbonHelper.HandleIsDropDownChanged(ribbon,
                        delegate() { return ribbon.IsDropDownOpen; },
                        ribbon,
                        ribbon._itemsPresenterPopup.TryGetChild());

            if (ribbon.IsDropDownOpen)
            {
                ribbon._retainFocusOnEscape = RibbonHelper.IsKeyboardMostRecentInputDevice();
                ribbon.OnRibbonTabPopupOpening();
            }

            // Raise UI Automation Events
            RibbonTab selectedTab = ribbon.ItemContainerGenerator.ContainerFromItem(ribbon.SelectedItem) as RibbonTab;
            if (selectedTab != null)
            {
                RibbonTabAutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(selectedTab) as RibbonTabAutomationPeer;
                if (peer != null)
                {
                    peer.RaiseTabExpandCollapseAutomationEvent((bool)e.OldValue, (bool)e.NewValue);
                }
            }
        }

        /// <summary>
        ///   Coerces the value of the IsOpen property.  Always returns true if the Ribbon
        ///   is not minimized.
        /// </summary>
        /// <param name="sender">The Ribbon whose tab state is being coerced.</param>
        /// <param name="value">The new value of the IsOpen property, prior to any coercion attempt.</param>
        /// <returns>The coerced value of the IsOpen property.</returns>
        private static object OnCoerceIsDropDownOpen(DependencyObject sender, object value)
        {
            Ribbon ribbon = (Ribbon)sender;
            if (!ribbon.IsMinimized)
            {
                return false;
            }

            if (!ribbon.IsLoaded ||
                (ribbon._itemsPresenterPopup != null &&
                 !((UIElement)(ribbon._itemsPresenterPopup.Parent)).IsArrangeValid))
            {
                ribbon.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(ribbon.RecoerceIsDropDownOpen), ribbon);
                return false;
            }
            return value;
        }

        private object RecoerceIsDropDownOpen(object arg)
        {
            CoerceValue(IsDropDownOpenProperty);
            return null;
        }

        /// <summary>
        ///   Called when the IsMinimized property changes.  
        /// </summary>
        /// <param name="sender">The Ribbon being minimized or expanded.</param>
        /// <param name="e">The event data.</param>
        private static void OnIsMinimizedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Ribbon ribbon = (Ribbon)sender;
            ribbon.CoerceValue(IsDropDownOpenProperty);
            if (ribbon.IsMinimized &&
                !ribbon.IsDropDownOpen)
            {
                // If the drop down is closed due to
                // an action of context menu then ContextMenuClosed
                // event is never raised. Hence reset the flag.
                ribbon._inContextMenu = false;
                ribbon.ContextMenuOriginalSource = null;
            }

            // Raise UI Automation Events
            RibbonAutomationPeer peer = UIElementAutomationPeer.FromElement(ribbon) as RibbonAutomationPeer;
            if (peer != null)
            {
                peer.RaiseExpandCollapseAutomationEvent(!(bool)e.OldValue, !(bool)e.NewValue);
            }

        }

        /// <summary>
        ///   Called if the Ribbon's QuickAccessToolbar changes.
        /// </summary>
        /// <param name="sender">The Ribbon whose QuickAccessToolbar is changing.</param>
        /// <param name="e">The event data.</param>
        private static void OnQuickAccessToolBarChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Ribbon ribbon = (Ribbon)sender;

            RibbonQuickAccessToolBar oldRibbonQuickAccessToolBar = e.OldValue as RibbonQuickAccessToolBar;
            RibbonQuickAccessToolBar newRibbonQuickAccessToolBar = e.NewValue as RibbonQuickAccessToolBar;

            // Remove Logical tree link
            if (oldRibbonQuickAccessToolBar != null)
            {
                ribbon.RemoveLogicalChild(oldRibbonQuickAccessToolBar);
            }

            // Add Logical tree link
            if (newRibbonQuickAccessToolBar != null)
            {
                ribbon.AddLogicalChild(newRibbonQuickAccessToolBar);
            }
        }

        /// <summary>
        ///   Called when the Ribbon's ApplicationMenu changes.
        /// </summary>
        /// <param name="sender">The Ribbon whose ApplicationMenu has changed.</param>
        /// <param name="e">The event data.</param>
        private static void OnApplicationMenuChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Ribbon ribbon = (Ribbon)sender;

            RibbonApplicationMenu oldRibbonApplicationMenu = e.OldValue as RibbonApplicationMenu;
            RibbonApplicationMenu newRibbonApplicationMenu = e.NewValue as RibbonApplicationMenu;

            // Remove Logical tree link
            if (oldRibbonApplicationMenu != null)
            {
                ribbon.RemoveLogicalChild(oldRibbonApplicationMenu);
            }

            // Add Logical tree link
            if (newRibbonApplicationMenu != null)
            {
                ribbon.AddLogicalChild(newRibbonApplicationMenu);
            }
        }

        /// <summary>
        ///   Coerces the Title property.  Return Window.Title if value is not set.
        /// </summary>
        /// <param name="sender">
        ///   The Ribbon that the Title property exists on.  When the callback is invoked,
        ///   the property system will pass this value.
        /// </param>
        /// <param name="value">The new value of the Title property, prior to any coercion attempt.</param>
        /// <returns>The coerced value of the Title property.</returns>
        private static object OnCoerceTitle(DependencyObject sender, object value)
        {
            Ribbon ribbon = (Ribbon)sender;
            return OnCoerceTitleImpl(ribbon, value);
        }

        private static object OnCoerceTitleImpl(Ribbon rib, object value)
        {
            if (rib.IsHostedInRibbonWindow)
            {
                RibbonWindow rw = (RibbonWindow)rib._window;
                if (!(string.IsNullOrEmpty(rw.Title as string)))
                {
                    return rw.Title;
                }
            }

            return value;
        }

        /// <summary>
        ///   Called when the IsCollapsed property changes.
        /// </summary>
        /// <param name="sender">The Ribbon being collapsed or expanded.</param>
        /// <param name="e">The event data.</param>
        private static void OnIsCollapsedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Ribbon ribbon = (Ribbon)sender;
            ribbon.RaiseEvent(new RoutedEventArgs(ribbon.IsCollapsed ? CollapsedEvent : ExpandedEvent));
        }

        private static object CoerceIsCollapsed(DependencyObject d, object baseValue)
        {
            Window window = ((Ribbon)d)._window;
            if (window != null &&
                (DoubleUtil.LessThan(window.ActualWidth, CollapseWidth) ||
                 DoubleUtil.LessThan(window.ActualHeight, CollapseHeight)))
            {
                return true;
            }
            return baseValue;
        }

        /// <summary>
        ///   Called when the Ribbon's selected tab is opening in popup mode.
        /// </summary>
        private void OnRibbonTabPopupOpening()
        {
            if (this.IsMinimized)
            {
                _itemsPresenterPopup.Width = this.CalculatePopupWidth();
            }
        }

        /// <summary>
        ///   Called when the ContextualTabGroups collection changes.  
        /// </summary>
        /// <param name="sender">The Ribbon whose ContextualTabGroups collection changed.</param>
        /// <param name="e">The event data.</param>
        private void OnContextualTabGroupsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ContextualTabGroupsSource != null && ContextualTabGroups.Count > 0)
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.Ribbon_ContextualTabHeadersSourceInvalid));
            }
        }

        private static void OnContextualTabGroupsSourceChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            Ribbon ribbon = (Ribbon)sender;

            if (ribbon.ContextualTabGroupsSource != null && ribbon.ContextualTabGroups.Count > 0)
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.Ribbon_ContextualTabHeadersSourceInvalid));
            }

            if (ribbon.ContextualTabGroupItemsControl != null)
            {
                ribbon.ContextualTabGroupItemsControl.ItemsSource = (IEnumerable)args.NewValue;
            }
        }

        private static void OnNotifyContextualTabGroupPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Ribbon ribbon = (Ribbon)d;
            if (ribbon.ContextualTabGroupItemsControl != null)
            {
                ribbon.ContextualTabGroupItemsControl.NotifyPropertyChanged(e);
            }
        }

        private static void OnNotifyTabHeaderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Ribbon ribbon = (Ribbon)d;
            int itemCount = ribbon.Items.Count;
            for (int i = 0; i < itemCount; i++)
            {
                RibbonTab ribbonTab = ribbon.ItemContainerGenerator.ContainerFromIndex(i) as RibbonTab;
                if (ribbonTab != null)
                {
                    ribbonTab.NotifyPropertyChanged(e);
                }
            }
        }

        /// <summary>
        ///   Called when the Ribbon is loaded.  This creates its QuickAccessToolbar and
        ///   ApplicationMenu, and also selects the initial tab.
        /// </summary>
        /// <param name="sender">The Ribbon being loaded.</param>
        /// <param name="e">The event data.</param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= new RoutedEventHandler(this.OnLoaded);

            // Create default RibbonApplicationMenu if ApplicationMenu is not set
            if (this.ApplicationMenu == null)
            {
                this.ApplicationMenu = new RibbonApplicationMenu();
            }

            // Create default RibbonQuickAccessToolBar if QuickAccessToolBar is not set
            if (this.QuickAccessToolBar == null)
            {
                this.QuickAccessToolBar = new RibbonQuickAccessToolBar();
            }
        }

        private void InitializeSelection()
        {
            // Select the first Tab if nothing is selected
            if (SelectedIndex < 0 && Items.Count > 0)
            {
                // Get index of first visible non-contextual tab
                int selectedIndex = GetFirstVisibleTabIndex(true /*ignoreContextualTabs*/);
                if (selectedIndex < 0)
                {
                    // Get index of first visible contextual tab.
                    selectedIndex = GetFirstVisibleTabIndex(false /*ignoreContextualTabs*/);
                }
                if (selectedIndex >= 0)
                {
                    SelectedIndex = selectedIndex;
                }
            }
        }

        internal void ResetSelection()
        {
            SelectedIndex = -1;
            InitializeSelection();
        }

        private int GetFirstVisibleTabIndex(bool ignoreContextualTabs)
        {
            int itemCount = Items.Count;
            for (int i = 0; i < itemCount; i++)
            {
                RibbonTab tab = ItemContainerGenerator.ContainerFromIndex(i) as RibbonTab;
                if (tab != null &&
                    tab.IsVisible &&
                    (!tab.IsContextualTab || ignoreContextualTabs))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        ///   Returns a value indicating whether a Window is a RibbonWindow.
        /// </summary>
        /// <param name="win">The Window to check.</param>
        /// <returns>Returns true if the win is a RibbonWindow.</returns>
        private static bool CheckIfWindowIsRibbonWindow(Window win)
        {
            return win is RibbonWindow;
        }

        /// <summary>
        ///   Calculate the Width of the Ribbon's popup.
        /// </summary>
        /// <returns>The width of the popup.</returns>
        private double CalculatePopupWidth()
        {
            // 1. Calculate _popupPlacementTarget bounding rect in screen coordinates
            // 2. Get monitor for _popupPlacementTarget rect
            // 3. Get monitor size
            // 4. intersect monitor rect with _popupPlacementTarget rect
            // 5. return the width of the intersection
            FrameworkElement popupPlacementTarget = _itemsPresenterPopup.Parent as FrameworkElement;

            Point startPoint = popupPlacementTarget.PointToScreen(new Point());
            Point endPoint = popupPlacementTarget.PointToScreen(new Point(popupPlacementTarget.ActualWidth, popupPlacementTarget.ActualHeight));

            NativeMethods.RECT popupPlacementTargetRect = new NativeMethods.RECT();
            popupPlacementTargetRect.left = (int)startPoint.X;
            popupPlacementTargetRect.right = (int)endPoint.X;
            popupPlacementTargetRect.top = (int)startPoint.Y;
            popupPlacementTargetRect.bottom = (int)endPoint.Y;
            IntPtr monitorPtr = NativeMethods.MonitorFromRect(ref popupPlacementTargetRect, NativeMethods.MONITOR_DEFAULTTONEAREST);
            if (monitorPtr != IntPtr.Zero)
            {
                NativeMethods.MONITORINFOEX monitorInfo = new NativeMethods.MONITORINFOEX();

                NativeMethods.GetMonitorInfo(new HandleRef(null, monitorPtr), monitorInfo);
                NativeMethods.RECT rect = monitorInfo.rcMonitor;

                Rect screenRect = new Rect(new Point(rect.left, rect.top), new Point(rect.right, rect.bottom));
                Rect popupRect = new Rect(startPoint, endPoint);
                screenRect.Intersect(popupRect);

                double screenWidth = Math.Abs(popupPlacementTarget.PointFromScreen(screenRect.BottomRight).X -
                                        popupPlacementTarget.PointFromScreen(screenRect.TopLeft).X);
                return screenWidth + (screenRect.Right == popupRect.Right ? 5 : 0); // Account for 5px popup shadow
            }

            return popupPlacementTarget.RenderSize.Width;
        }

        /// <summary>
        ///   Called when the Window hosting the Ribbon changes sizes.  Here we decide
        ///   whether or nto to collapse the Ribbon.
        /// </summary>
        /// <param name="sender">The Window whose size has changed.</param>
        /// <param name="e">The event data.</param>
        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            CoerceValue(IsCollapsedProperty);
        }

        private void OnRibbonWindowTitleChanged(object sender, EventArgs e)
        {
            CoerceValue(TitleProperty);
        }

        /// <summary>
        ///     Returns collection index for a given display index of tab item
        /// </summary>
        internal int GetTabIndexForDisplayIndex(int displayIndex)
        {
            if (TabDisplayIndexToIndexMap.ContainsKey(displayIndex))
            {
                return TabDisplayIndexToIndexMap[displayIndex];
            }
            return -1;
        }

        /// <summary>
        ///     Returns collection index for a given display index of tab item
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal int GetTabDisplayIndexForIndex(int index)
        {
            if (TabIndexToDisplayIndexMap.ContainsKey(index))
            {
                return TabIndexToDisplayIndexMap[index];
            }
            return -1;
        }

        private void RefreshHeaderCollection()
        {
            Debug.Assert(ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated, "Expected: containers should be generated before calling this method.");
            int itemCount = Items.Count;
            for (int i = 0; i < itemCount; i++)
            {
                RibbonTab tab = ItemContainerGenerator.ContainerFromIndex(i) as RibbonTab;
                object headerItem = null;
                if (tab != null)
                {
                    headerItem = tab.Header;
                }
                if (headerItem == null)
                {
                    headerItem = CreateDefaultHeaderObject();
                }
                if (i >= _tabHeaderItemsSource.Count)
                {
                    _tabHeaderItemsSource.Add(headerItem);
                }
                else
                {
                    _tabHeaderItemsSource[i] = headerItem;
                }
            }
            int headersCount = _tabHeaderItemsSource.Count;
            for (int i = 0; i < (headersCount - itemCount); i++)
            {
                _tabHeaderItemsSource.RemoveAt(itemCount);
            }
        }

        /// <summary>
        ///     Creates a default header object to host in header itemscontrol.
        ///     This is used when RibbonTab.Header property is not set.
        /// </summary>
        private static object CreateDefaultHeaderObject()
        {
            return new SingleSpaceObject();
        }

        private static void OnBorderBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Ribbon ribbon = (Ribbon)d;
            if (ribbon._tabHeaderItemsControl != null)
            {
                RibbonTabHeadersPanel tabHeadersPanel = ribbon._tabHeaderItemsControl.InternalItemsHost as RibbonTabHeadersPanel;
                if (tabHeadersPanel != null)
                {
                    tabHeadersPanel.OnNotifyRibbonBorderBrushChanged();
                }
            }
            RibbonContextualTabGroupItemsControl contextualItemsControl = ribbon.ContextualTabGroupItemsControl;
            if (contextualItemsControl != null)
            {
                RibbonContextualTabGroupsPanel contextualTabHeadersPanel = contextualItemsControl.InternalItemsHost as RibbonContextualTabGroupsPanel;
                if (contextualTabHeadersPanel != null)
                {
                    contextualTabHeadersPanel.OnNotifyRibbonBorderBrushChanged();
                }
            }
        }

        private static object CoerceIsMinimized(DependencyObject d, object baseValue)
        {
            bool isMinimized = (bool)baseValue;
            Ribbon ribbon = (Ribbon)d;
            if (isMinimized && ribbon.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
            {
                return false;
            }
            return baseValue;
        }

        private void OnItemContainerGeneratorStatusChanged(object sender, EventArgs e)
        {
            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                CoerceValue(IsMinimizedProperty);
                InitializeSelection();
                RefreshHeaderCollection();
            }
        }

        private void HandleIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                // If the ribbon start invisible InitializeSelection would have
                // failed. Hence call InitializeSelection again. Using a
                // dispatcher operation because the tabs might not be visible yet.
                // This is common scenario when hosted in WinForms.
                Dispatcher.BeginInvoke(
                    (Action)delegate()
                    {
                        InitializeSelection();
                    },
                    DispatcherPriority.Normal,
                    null);
            }
        }

        #endregion

        #region Dismiss Popups

        private static void OnLostMouseCaptureThunk(object sender, MouseEventArgs e)
        {
            Ribbon ribbon = (Ribbon)sender;
            ribbon.OnLostMouseCaptureThunk(e);
        }

        private void OnLostMouseCaptureThunk(MouseEventArgs e)
        {
            RibbonHelper.HandleLostMouseCapture(this,
                    e,
                    delegate() { return (IsDropDownOpen && !_inContextMenu); },
                    delegate(bool value) { IsDropDownOpen = value; },
                    this,
                    _itemsPresenterPopup.TryGetChild());
        }

        private static void OnClickThroughThunk(object sender, MouseButtonEventArgs e)
        {
            Ribbon ribbon = (Ribbon)sender;
            ribbon.OnClickThrough(e);
        }

        private void OnClickThrough(MouseButtonEventArgs e)
        {
            RibbonHelper.HandleClickThrough(this, e, _itemsPresenterPopup.TryGetChild());
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            _retainFocusOnEscape = false;
            if (IsDropDownOpen)
            {                
                // Close the drop down if the click happened any where outside the popup
                // and tab header items control.
                if (!RibbonHelper.IsAncestorOf(_itemsPresenterPopup.TryGetChild(), e.OriginalSource as DependencyObject) &&
                    !RibbonHelper.IsAncestorOf(_tabHeaderItemsControl, e.OriginalSource as DependencyObject))
                {
                    IsDropDownOpen = false;
                }
            }
        }

        private static void OnDismissPopupThunk(object sender, RibbonDismissPopupEventArgs e)
        {
            Ribbon ribbon = (Ribbon)sender;
            ribbon.OnDismissPopup(e);
        }

        private void OnDismissPopup(RibbonDismissPopupEventArgs e)
        {
            RibbonHelper.HandleDismissPopup(e,
                DismissPopupAction,
                delegate(DependencyObject d) { return false; },
                _itemsPresenterPopup.TryGetChild(),
                _tabHeaderItemsControl);
        }

        private void DismissPopupAction(bool value)
        {
            IsDropDownOpen = value;
            if (!value && !IsMinimized)
            {
                RestoreFocusAndCapture(false);
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            // Restore the focus and capture if
            //  1) The event is not handled
            //  2) The Ribbon DropDown in not open
            //  3) The button changed is not mouse right button
            //  4) And the original source belongs to the visual tree of ribbon.
            if (!e.Handled && 
                !IsDropDownOpen && 
                e.ChangedButton != MouseButton.Right && 
                TreeHelper.IsVisualAncestorOf(this, e.OriginalSource as DependencyObject))
            {
                RestoreFocusAndCapture(false);
            }
        }

        #endregion Dismiss Poups

        #region Keyboard Navigation

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled)
            {
                RibbonHelper.HandleDropDownKeyDown(this, e,
                    delegate { return IsDropDownOpen; },
                    delegate(bool value) { IsDropDownOpen = value; },
                    _retainFocusOnEscape ? SelectedTabHeader : null,
                    _itemsPresenter);
            }

            if (!e.Handled)
            {
                if (e.Key == Key.Escape)
                {
                    RestoreFocusAndCapture(false);
                }
                else if ((e.Key == Key.Left || e.Key == Key.Right) &&
                    ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
                {
                    e.Handled = OnArrowControlKeyDown(e.Key);
                }
            }
        }

        private bool OnArrowControlKeyDown(Key key)
        {
            RibbonQuickAccessToolBar quickAccessToolBar = QuickAccessToolBar;
            if (quickAccessToolBar != null && !quickAccessToolBar.IsLoaded)
            {
                quickAccessToolBar = null;
            }

            RibbonTabHeaderItemsControl tabHeaderItemsControl = RibbonTabHeaderItemsControl;
            RibbonTab selectedTab = null;
            int selectedIndex = SelectedIndex;
            if (selectedIndex >= 0)
            {
                selectedTab = ItemContainerGenerator.ContainerFromIndex(selectedIndex) as RibbonTab;
            }

            if ((quickAccessToolBar != null && quickAccessToolBar.IsKeyboardFocusWithin) ||
                (tabHeaderItemsControl != null && tabHeaderItemsControl.IsKeyboardFocusWithin) ||
                (selectedTab != null && selectedTab.IsKeyboardFocusWithin))
            {
                ArrowKeyControlNavigationScope startingNavigationScope = ArrowKeyControlNavigationScope.Tab;
                if (quickAccessToolBar != null && quickAccessToolBar.IsKeyboardFocusWithin)
                {
                    startingNavigationScope = ArrowKeyControlNavigationScope.QuickAccessToolbar;
                }
                else if (tabHeaderItemsControl != null && tabHeaderItemsControl.IsKeyboardFocusWithin)
                {
                    startingNavigationScope = ArrowKeyControlNavigationScope.TabHeaders;
                }

                return ArrowKeyControlNavigate((key == Key.Left)/* navigateLeft */,
                    quickAccessToolBar,
                    selectedTab,
                    startingNavigationScope);
            }
            return false;
        }

        private enum ArrowKeyControlNavigationScope
        {
            QuickAccessToolbar,
            TabHeaders,
            Tab
        }

        private RibbonTabHeader SelectedTabHeader
        {
            get
            {
                RibbonTabHeaderItemsControl tabHeaderItemsControl = RibbonTabHeaderItemsControl;
                if (tabHeaderItemsControl == null ||
                    !tabHeaderItemsControl.IsVisible)
                {
                    return null;
                }

                int selectedIndex = SelectedIndex;
                if (selectedIndex >= 0)
                {
                    return (tabHeaderItemsControl.ItemContainerGenerator.ContainerFromIndex(selectedIndex) as RibbonTabHeader);
                }
                return null;
            }
        }

        /// <summary>
        ///     Helper method to focus the header of selected tab.
        ///     Returns success of the focus change.
        /// </summary>
        private bool FocusSelectedTabHeader()
        {
            RibbonTabHeader tabHeader = SelectedTabHeader;
            if (tabHeader != null)
            {
                tabHeader.Focus();
                return tabHeader.IsKeyboardFocusWithin;
            }
            return false;
        }

        /// <summary>
        ///     Helper method to navigate through groups of the selected
        ///     tab when left/right arrow keys are pressed along with CTRL.
        ///     Returns success of such navigation.
        /// </summary>
        private bool TabArrowKeyControlNavigate(RibbonTab tab,
            bool leftToRight,
            bool startFromCurrent,
            bool cycle)
        {
            if (tab == null)
            {
                return false;
            }
            return ArrowKeyControlNavigate<RibbonTab>(tab,
                leftToRight,
                startFromCurrent,
                cycle,
                tab.Items.Count,
                null,
                GetFocusedRibbonGroupIndex,
                TrySetFocusOnRibbonGroupAtIndex);
        }

        private int GetFocusedRibbonGroupIndex(RibbonTab tab)
        {
            RibbonGroup ribbonGroup = TreeHelper.FindVisualAncestor<RibbonGroup>(Keyboard.FocusedElement as DependencyObject);
            if (ribbonGroup == null)
            {
                return -1;
            }
            return tab.ItemContainerGenerator.IndexFromContainer(ribbonGroup);
        }

        private bool TrySetFocusOnRibbonGroupAtIndex(RibbonTab tab,
            int index)
        {
            RibbonGroup group = tab.ItemContainerGenerator.ContainerFromIndex(index) as RibbonGroup;
            if (group != null &&
                group.IsVisible)
            {
                group.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                if (group.IsKeyboardFocusWithin)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Helper method to navigate through items of the qat (including
        ///     customize menu) when left/right arrow keys are pressed along with CTRL.
        ///     Returns success of such navigation. 
        /// </summary>
        private bool QatArrowKeyControlNavigate(bool leftToRight,
            bool startFromCurrent,
            bool cycle)
        {
            RibbonQuickAccessToolBar quickAccessToolBar = QuickAccessToolBar;
            if (quickAccessToolBar == null)
            {
                return false;
            }

            return ArrowKeyControlNavigate<RibbonQuickAccessToolBar>(quickAccessToolBar,
                leftToRight,
                startFromCurrent,
                cycle,
                quickAccessToolBar.Items.Count,
                quickAccessToolBar.CustomizeMenuButton,
                GetFocusedQatItemIndex,
                TrySetFocusOnQatItemAtIndex);
        }

        /// <summary>
        ///     Helper method to set focus on the item at given index of qat.
        /// </summary>
        private bool TrySetFocusOnQatItemAtIndex(RibbonQuickAccessToolBar quickAccessToolBar,
            int index)
        {
            RibbonControl ribbonControl = quickAccessToolBar.ItemContainerGenerator.ContainerFromIndex(index) as RibbonControl;
            if (ribbonControl != null &&
                ribbonControl.IsVisible &&
                (index == 0 || ribbonControl.HostsRibbonGroup()))
            {
                ribbonControl.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                if (ribbonControl.IsKeyboardFocusWithin)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Helper method to get the index of item which has focus within.
        /// </summary>
        private int GetFocusedQatItemIndex(RibbonQuickAccessToolBar quickAccessToolBar)
        {
            RibbonControl ribbonControl = TreeHelper.FindVisualAncestor<RibbonControl>(Keyboard.FocusedElement as DependencyObject);
            if (ribbonControl == null)
            {
                return -1;
            }
            return quickAccessToolBar.ItemContainerGenerator.IndexFromContainer(ribbonControl);
        }

        /// <summary>
        ///     Helper method to do left/right arrow key control
        ///     navigation through items bases controls.
        ///     e.g, RibbonTab and RibbonQuickAccessToolbar.
        ///     Returns success of the operation.
        /// </summary>
        private static bool ArrowKeyControlNavigate<T>(T targetControl,
            bool leftToRight,
            bool startFromCurrent,
            bool cycle,
            int itemCount,
            Control extraControl,
            Func<T, int> getFocusedItemIndex,
            Func<T, int, bool> trySetFocusAtItemIndex) where T : Control
        {
            if (targetControl == null ||
                !targetControl.IsVisible)
            {
                return false;
            }

            // If focus belongs to one of the sub popups
            // then do not navigate
            if (targetControl.IsKeyboardFocusWithin &&
                !TreeHelper.IsVisualAncestorOf(targetControl, Keyboard.FocusedElement as DependencyObject))
            {
                return false;
            }

            int attemptCount = 0;
            int currentIndex = DeterminePreStartIndexForArrowControlNavigation<T>(targetControl,
                startFromCurrent,
                leftToRight,
                cycle,
                extraControl,
                itemCount,
                getFocusedItemIndex);
            bool considerExtraControl = (extraControl != null && extraControl.IsVisible);

            if (currentIndex == Int32.MinValue)
            {
                return false;
            }

            int incr = (leftToRight ? 1 : -1);
            // iterate through the items in requested order and try to 
            // give focus to one of them. Cycle if needed.
            while (attemptCount <= itemCount)
            {
                attemptCount++;
                currentIndex += incr;
                if (leftToRight && currentIndex == itemCount)
                {
                    if (considerExtraControl && extraControl.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)))
                    {
                        return true;
                    }
                    if (cycle)
                    {
                        currentIndex = -1;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (!leftToRight && (currentIndex < 0 || currentIndex == itemCount))
                {
                    if (currentIndex < 0 && !cycle)
                    {
                        return false;
                    }
                    if (considerExtraControl &&
                        extraControl.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)))
                    {
                        return true;
                    }
                    currentIndex = itemCount;
                }
                else
                {
                    if (trySetFocusAtItemIndex(targetControl, currentIndex))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Helper method to determine the start item index of the
        ///     items based controls (like RibbonQuickAccessToolbar
        ///     and RibbonTab) to start left/right arrow control key
        ///     navigation. A return value of Int32.MinValue means
        ///     uninterested scenario.
        /// </summary>
        private static int DeterminePreStartIndexForArrowControlNavigation<T>(T targetControl,
            bool startFromCurrent,
            bool leftToRight,
            bool cycle,
            Control extraControl,
            int itemCount,
            Func<T, int> getFocusedItemIndex) where T : Control
        {
            int startIndex = 0;
            bool considerExtraControl = (extraControl != null && extraControl.IsVisible);
            if (startFromCurrent)
            {
                if (!targetControl.IsKeyboardFocusWithin)
                {
                    // Cannot start from current if there is not focus within.
                    return Int32.MinValue;
                }

                if (considerExtraControl && extraControl.IsKeyboardFocusWithin)
                {
                    if (leftToRight)
                    {
                        if (!cycle)
                        {
                            return Int32.MinValue;
                        }
                        startIndex = -1;
                    }
                    else
                    {
                        startIndex = itemCount;
                    }
                }
                else
                {
                    startIndex = getFocusedItemIndex(targetControl);
                    if (startIndex < 0)
                    {
                        return Int32.MinValue;
                    }
                }
            }
            else
            {
                if (leftToRight)
                {
                    startIndex = -1;
                }
                else
                {
                    startIndex = itemCount + 1;
                }
            }
            return startIndex;
        }

        /// <summary>
        ///     Method to navigate through ribbon when left/right
        ///     arrow keys are pressed along with CTRL.
        /// </summary>
        private bool ArrowKeyControlNavigate(bool navigateLeft,
            RibbonQuickAccessToolBar quickAccessToolBar,
            RibbonTab selectedTab,
            ArrowKeyControlNavigationScope startingNavigationScope)
        {
            DependencyObject focusedElement = Keyboard.FocusedElement as DependencyObject;
            if (focusedElement != null &&
                !TreeHelper.IsVisualAncestorOf(this, focusedElement) &&
                _itemsPresenterPopup != null &&
                _itemsPresenterPopup.Child != null &&
                !TreeHelper.IsVisualAncestorOf(_itemsPresenterPopup.Child, focusedElement))
            {
                // If the focused element is in uninteresting popups,
                // then fail.
                return false;
            }

            bool isRTL = (FlowDirection == FlowDirection.RightToLeft);
            ArrowKeyControlNavigationScope currentNavigationScope = startingNavigationScope;
            int attemptCount = 0;
            while (attemptCount < 3)
            {
                attemptCount++;
                switch (currentNavigationScope)
                {
                    case ArrowKeyControlNavigationScope.QuickAccessToolbar:
                        if (quickAccessToolBar != null && quickAccessToolBar.IsVisible && quickAccessToolBar.IsKeyboardFocusWithin)
                        {
                            // Try to navigate through remaining qat items if focus is already in qat.
                            if (QatArrowKeyControlNavigate((navigateLeft == isRTL) /* leftToRight */,
                                    true,
                                    IsMinimized && !IsDropDownOpen /* cycle */))
                            {
                                return true;
                            }
                        }

                        if (navigateLeft)
                        {
                            // Try to navigate into groups of the selected tab.
                            if (TabArrowKeyControlNavigate(selectedTab, isRTL /*leftToRight*/, false, IsDropDownOpen))
                            {
                                return true;
                            }
                            currentNavigationScope = ArrowKeyControlNavigationScope.Tab;
                        }
                        else
                        {
                            // Navigate to the selected tab header
                            if (FocusSelectedTabHeader())
                            {
                                return true;
                            }
                            currentNavigationScope = ArrowKeyControlNavigationScope.TabHeaders;
                        }
                        break;
                    case ArrowKeyControlNavigationScope.TabHeaders:
                        if (navigateLeft)
                        {
                            // Try to navigate into the items of qat.
                            if (QatArrowKeyControlNavigate(isRTL /* leftToRight */,
                                   false,
                                   IsMinimized && !IsDropDownOpen /* cycle */))
                            {
                                return true;
                            }
                            currentNavigationScope = ArrowKeyControlNavigationScope.QuickAccessToolbar;
                        }
                        else
                        {
                            // Try to navigate to groups of selected tab.
                            if (TabArrowKeyControlNavigate(selectedTab,
                                !isRTL /* leftToRight */,
                                false,
                                IsDropDownOpen))
                            {
                                return true;
                            }
                            currentNavigationScope = ArrowKeyControlNavigationScope.Tab;
                        }
                        break;
                    case ArrowKeyControlNavigationScope.Tab:

                        if (selectedTab != null && selectedTab.IsVisible && selectedTab.IsKeyboardFocusWithin)
                        {
                            // Try to navigate through the remaining groups if the focus is already in selected tab.
                            if (TabArrowKeyControlNavigate(selectedTab,
                                    (navigateLeft == isRTL) /* leftToRight */,
                                    true,
                                    IsDropDownOpen))
                            {
                                return true;
                            }
                        }

                        if (navigateLeft)
                        {
                            // Navigate to the selected tab header
                            if (FocusSelectedTabHeader())
                            {
                                return true;
                            }
                            currentNavigationScope = ArrowKeyControlNavigationScope.TabHeaders;
                        }
                        else
                        {
                            // Try to navigate in the items of qat.
                            if (QatArrowKeyControlNavigate(!isRTL /*leftToRight*/, 
                                    false, 
                                    (IsMinimized && !IsDropDownOpen)))
                            {
                                return true;
                            }
                            currentNavigationScope = ArrowKeyControlNavigationScope.QuickAccessToolbar;
                        }
                        break;
                }
            }
            return false;
        }

        #endregion

        #region KeyTips

        private bool OnKeyTipEnterFocus(object sender, EventArgs e)
        {
            PresentationSource targetSource = sender as PresentationSource;
            if (targetSource == RibbonHelper.GetPresentationSourceFromVisual(this))
            {
                // Focus the selected tab header if this Ribbon belongs
                // to concerned presentation source.
                return FocusSelectedTabHeader();
            }
            return false;
        }

        private bool OnKeyTipExitRestoreFocus(object sender, EventArgs e)
        {
            PresentationSource targetSource = sender as PresentationSource;
            if (targetSource == RibbonHelper.GetPresentationSourceFromVisual(this))
            {
                // Restore the focus if the Ribbon belongs to
                // the concerned presentation source.
                RestoreFocusAndCapture(true);
            }
            return false;
        }

        internal void RestoreFocusAndCapture(bool force)
        {
            // Restore the focus only if not in keytip mode or if forced to.
            if (KeyTipService.Current.State == KeyTipService.KeyTipState.None ||
                force)
            {
                RibbonHelper.RestoreFocusAndCapture(this, this);
            }
        }

        #endregion

        #region Context Menu

        private static void OnContextMenuOpeningThunk(object sender, ContextMenuEventArgs e)
        {
            ((Ribbon)sender).OnContextMenuOpeningInternal(e);
        }

        private void OnContextMenuOpeningInternal(ContextMenuEventArgs e)
        {
            ContextMenuOriginalSource = e.OriginalSource as UIElement;
            _inContextMenu = true;
        }

        internal UIElement ContextMenuOriginalSource
        {
            get;
            private set;
        }

        private static void OnContextMenuClosingThunk(object sender, ContextMenuEventArgs e)
        {
            ((Ribbon)sender).OnContextMenuClosingInternal();
        }

        private void OnContextMenuClosingInternal()
        {
            _inContextMenu = false;
            ContextMenuOriginalSource = null;
            if (IsDropDownOpen)
            {
                RibbonHelper.AsyncSetFocusAndCapture(this,
                    delegate() { return IsDropDownOpen; },
                    this,
                    _itemsPresenterPopup.TryGetChild());
            }
        }

        internal void RestoreFocusOnContextMenuClose()
        {
            if (!IsDropDownOpen)
            {
                RestoreFocusAndCapture(false);
            }
        }

        #endregion

        #region QAT

        private static void AddToQATCanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            DependencyObject obj = args.OriginalSource as DependencyObject;

            // Find nearest element that can be added to the QAT directly
            obj = FindElementThatCanBeAddedToQAT(obj);

            if (obj != null &&
                RibbonControlService.GetQuickAccessToolBarId(obj) != null &&
                !RibbonHelper.ExistsInQAT(obj))
            {
                 args.CanExecute = true;
            }
        }

        private static void AddToQATExecuted(object sender, ExecutedRoutedEventArgs args)
        {
            UIElement originalSource = args.OriginalSource as UIElement;

            // Find nearest element that can be added to the QAT directly
            originalSource = FindElementThatCanBeAddedToQAT(originalSource) as UIElement;

            if (originalSource != null)
            {
                RibbonQuickAccessToolBarCloneEventArgs e = new RibbonQuickAccessToolBarCloneEventArgs(originalSource);
                originalSource.RaiseEvent(e);

                Ribbon ribbon = RibbonControlService.GetRibbon(originalSource);
                if (ribbon != null &&
                    ribbon.QuickAccessToolBar != null &&
                    e.CloneInstance != null)
                {
                    ribbon.QuickAccessToolBar.Items.Add(e.CloneInstance);
                    args.Handled = true;
                }
            }
        }

        private static DependencyObject FindElementThatCanBeAddedToQAT(DependencyObject obj)
        {
            while (obj != null && !RibbonControlService.GetCanAddToQuickAccessToolBarDirectly(obj))
            {
                obj = TreeHelper.GetParent(obj);
            }

            return obj;
        }

        private static void MaximizeRibbonCanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            DependencyObject originalSource = args.OriginalSource as DependencyObject;

            if (originalSource != null)
            {
                Ribbon ribbon = RibbonControlService.GetRibbon(originalSource);
                if (ribbon != null &&
                    ribbon.IsMinimized)
                {
                    args.CanExecute = true;
                }
            }
        }

        private static void MaximizeRibbonExecuted(object sender, ExecutedRoutedEventArgs args)
        {
            DependencyObject originalSource = args.OriginalSource as DependencyObject;
            if (originalSource != null)
            {
                Ribbon ribbon = RibbonControlService.GetRibbon(originalSource);
                if (ribbon != null)
                {
                    ribbon.IsMinimized = false;
                    args.Handled = true;
                }
            }
        }

        private static void MinimizeRibbonCanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            DependencyObject originalSource = args.OriginalSource as DependencyObject;

            if (originalSource != null)
            {
                Ribbon ribbon = RibbonControlService.GetRibbon(originalSource);
                if (ribbon != null &&
                    !ribbon.IsMinimized)
                {
                    args.CanExecute = true;
                }
            }
        }

        private static void MinimizeRibbonExecuted(object sender, ExecutedRoutedEventArgs args)
        {
            DependencyObject originalSource = args.OriginalSource as DependencyObject;
            if (originalSource != null)
            {
                Ribbon ribbon = RibbonControlService.GetRibbon(originalSource);
                if (ribbon != null)
                {
                    ribbon.IsMinimized = true;
                    args.Handled = true;
                }
            }
        }

        private static void RemoveFromQATCanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            DependencyObject obj = args.OriginalSource as DependencyObject;

            if (obj != null)
            {
                args.CanExecute = RibbonControlService.GetIsInQuickAccessToolBar(obj);
            }
        }

        private static void RemoveFromQATExecuted(object sender, ExecutedRoutedEventArgs args)
        {
            UIElement originalSource = args.OriginalSource as UIElement;
            if (originalSource != null)
            {
                Ribbon ribbon = RibbonControlService.GetRibbon(originalSource);
                if (ribbon != null &&
                    ribbon.QuickAccessToolBar != null)
                {
                    RibbonQuickAccessToolBar qat = ribbon.QuickAccessToolBar;
                    if (qat.Items.Contains(originalSource))
                    {
                        qat.Items.Remove(originalSource);
                        args.Handled = true;
                    }
                }
            }
        }

        private static void ShowQATAboveCanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            DependencyObject originalSource = args.OriginalSource as DependencyObject;

            if (originalSource != null)
            {
                Ribbon ribbon = RibbonControlService.GetRibbon(originalSource);
                if (ribbon != null &&
                    ribbon.QuickAccessToolBar != null &&
                    !ribbon.ShowQuickAccessToolBarOnTop)
                {
                    args.CanExecute = true;
                }
            }
        }

        private static void ShowQATAboveExecuted(object sender, ExecutedRoutedEventArgs args)
        {
            DependencyObject originalSource = args.OriginalSource as DependencyObject;
            if (originalSource != null)
            {
                Ribbon ribbon = RibbonControlService.GetRibbon(originalSource);
                if (ribbon != null)
                {
                    ribbon.ShowQuickAccessToolBarOnTop = true;
                    args.Handled = true;
                }
            }
        }

        private static void ShowQATBelowCanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            DependencyObject originalSource = args.OriginalSource as DependencyObject;

            if (originalSource != null)
            {
                Ribbon ribbon = RibbonControlService.GetRibbon(originalSource);
                if (ribbon != null &&
                    ribbon.QuickAccessToolBar != null &&
                    ribbon.ShowQuickAccessToolBarOnTop)
                {
                    args.CanExecute = true;
                }
            }
        }

        private static void ShowQATBelowExecuted(object sender, ExecutedRoutedEventArgs args)
        {
            DependencyObject originalSource = args.OriginalSource as DependencyObject;
            if (originalSource != null)
            {
                Ribbon ribbon = RibbonControlService.GetRibbon(originalSource);
                if (ribbon != null)
                {
                    ribbon.ShowQuickAccessToolBarOnTop = false;
                    args.Handled = true;
                }
            }
        }

        // Produce a duplicate UIElement.  Cloning requires several steps:
        //   1) Create an instance with special processing for RibbonMenuItem and RibbonSplitMenuItem.
        //   2) Transfer all of the properties that are either template generated or locally set.
        //   3) Create a wrapper around a Ribbongallery.
        //   4) Transfer relevant properties to the wrapper instance.

        private static void OnCloneThunk(object sender, RibbonQuickAccessToolBarCloneEventArgs e)
        {
            // If the cloning has not yet been performed (i.e. by a 
            // user-supplied handler), then perform the cloning ourselves.

            if (e.CloneInstance == null)
            {
                RibbonHelper.PopulatePropertyLists();

                bool allowTransformations = true;
                e.CloneInstance = (UIElement)RibbonHelper.CreateClone(e.InstanceToBeCloned, allowTransformations);
                e.Handled = true;
            }
        }

        #endregion QAT
    }
}
