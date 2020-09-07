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
    using System.Globalization;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Threading;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon.Primitives;
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Automation.Peers;
    using Microsoft.Windows.Controls.Ribbon.Primitives;
#endif

    #endregion

    /// <summary>
    ///   ContextMenu for Ribbon controls.
    /// </summary>
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(RibbonMenuItem))]
    public class RibbonContextMenu : ContextMenu
    {
        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonContextMenu class.
        /// </summary>
        static RibbonContextMenu()
        {
            Type ownerType = typeof(RibbonContextMenu);
            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            IsOpenProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnIsOpenChanged)));
            EventManager.RegisterClassHandler(ownerType, Mouse.PreviewMouseDownOutsideCapturedElementEvent, new MouseButtonEventHandler(OnClickThroughThunk));

            ItemsPanelTemplate template = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(RibbonMenuItemsPanel)));
            template.Seal();
            ItemsPanelProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(template));
        }

        public RibbonContextMenu()
        {
            Loaded += new RoutedEventHandler(OnLoaded);
        }

        #endregion

        #region UI Automation

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonContextMenuAutomationPeer(this);
        }

        #endregion UI Automation 

        #region Dismiss popup

        private static bool CanRaiseDismissPopups(UIElement dismissPopupSource)
        {
            // Contextmenu raises DismissPopup event on the source only
            // if it is in a popup and is in a Ribbon.
            if (dismissPopupSource == null ||
                RibbonControlService.GetRibbon(dismissPopupSource) == null)
            {
                return false;
            }

            Popup ancestorPopup = TreeHelper.FindAncestor(dismissPopupSource, delegate(DependencyObject element) { return (element is Popup); }) as Popup;
            if (ancestorPopup == null ||
                !ancestorPopup.IsOpen)
            {
                return false;
            }

            return true;
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonContextMenu contextMenu = (RibbonContextMenu)d;
            if (!(bool)(e.NewValue))
            {
                if (!contextMenu._ignoreDismissPopupsOnNextClose)
                {
                    UIElement dismissPopupSource = contextMenu.GetDismissPopupSource();
                    if (CanRaiseDismissPopups(dismissPopupSource))
                    {
                        // Raise DismissPopup on owner if can raise and if 
                        // was not asked to ignore.
                        dismissPopupSource.RaiseEvent(new RibbonDismissPopupEventArgs(RibbonDismissPopupMode.Always));
                        ((Ribbon)(RibbonControlService.GetRibbon(dismissPopupSource))).RestoreFocusOnContextMenuClose();
                    }
                }
                else
                {
                    contextMenu.RestoreFocusToRibbon();
                    contextMenu._ignoreDismissPopupsOnNextClose = false;
                }
            }
            else
            {
                contextMenu._ignoreDismissPopupsOnNextClose = false;
            }
        }

        /// <summary>
        ///     Tries to restore focus to the first focusable element across 
        ///     the ancestor chain of ContextMenuOriginalSource. Should
        ///     be called only when Ribbon is supposed to retain the focus.
        /// </summary>
        private void RestoreFocusToRibbon()
        {
            DependencyObject current = GetDismissPopupSource();
            if (current == null)
            {
                return;
            }
            Ribbon ribbon = RibbonControlService.GetRibbon(current);
            if (ribbon == null)
            {
                return;
            }
            while (current != null)
            {
                UIElement uie = current as UIElement;
                if (uie != null && uie.Focusable)
                {
                    uie.Dispatcher.BeginInvoke(
                        (Action)delegate()
                        {
                            if (!ribbon.IsKeyboardFocusWithin)
                            {
                                uie.Focus();
                            }
                        },
                        DispatcherPriority.Input,
                        null);
                    break;
                }
                current = TreeHelper.GetParent(current);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _ignoreDismissPopupsOnNextClose = true;
                try
                {
                    base.OnKeyDown(e);
                }
                finally
                {
                    // reset the value of _ignoreDismissPopupsOnNextClose because
                    // the escape might not have been to close the context menu itself.
                    _ignoreDismissPopupsOnNextClose = false;
                }
            }
            else
            {
                base.OnKeyDown(e);
            }
        }

        private UIElement GetDismissPopupSource()
        {
            UIElement placementTarget = PlacementTarget;
            if (placementTarget == null)
            {
                return null;
            }
            Ribbon ribbon = RibbonControlService.GetRibbon(placementTarget);
            if (ribbon == null)
            {
                return null;
            }
            // The original source for corresponding ContextMenuOpening will
            // be the original source for DismissPopup event.
            UIElement returnValue = ribbon.ContextMenuOriginalSource;
            if (returnValue == null)
            {
                returnValue = placementTarget;
            }
            return returnValue;
        }

        private static void OnClickThroughThunk(object sender, MouseButtonEventArgs e)
        {
            ((RibbonContextMenu)sender).OnClickThrough();
        }

        private void OnClickThrough()
        {
            UIElement dismissPopupSource = GetDismissPopupSource();
            if (Mouse.Captured == this &&
                CanRaiseDismissPopups(dismissPopupSource))
            {
                dismissPopupSource.Dispatcher.BeginInvoke(
                    (Action)delegate()
                    {
                        if (CanRaiseDismissPopups(dismissPopupSource))
                        {
                            dismissPopupSource.RaiseEvent(new RibbonDismissPopupEventArgs(RibbonDismissPopupMode.MousePhysicallyNotOver));
                        }
                    },
                    DispatcherPriority.Input,
                    null);

                if (IsOpen)
                {
                    _ignoreDismissPopupsOnNextClose = true;
                }
            }
        }

        #endregion

        #region Instance Generation

        [ThreadStatic]
        private static RibbonContextMenu _galleryContextMenu;

        [ThreadStatic]
        private static RibbonContextMenu _ribbonControlContextMenu;

        [ThreadStatic]
        private static RibbonContextMenu _qatControlContextMenu;

        [ThreadStatic]
        private static RibbonContextMenu _defaultRibbonClientAreaContextMenu;

        internal static RibbonContextMenu ChooseContextMenu(DependencyObject owner)
        {
            if (owner is Ribbon)
            {
                return GetDefaultRibbonClientAreaContextMenu();
            }
            else if (RibbonControlService.GetCanAddToQuickAccessToolBarDirectly(owner))
            {
                if (owner is RibbonGallery)
                {
                    return GetGalleryContextMenu();
                }
                else
                {
                    if (RibbonControlService.GetIsInQuickAccessToolBar(owner))
                    {
                        return GetQATControlContextMenu();
                    }
                    else
                    {
                        return GetRibbonControlContextMenu();
                    }
                }
            }

            return null;
        }

        private static RibbonContextMenu GetDefaultRibbonClientAreaContextMenu()
        {
            if (_defaultRibbonClientAreaContextMenu == null)
            {
                _defaultRibbonClientAreaContextMenu = new RibbonContextMenu();
                _defaultRibbonClientAreaContextMenu.Items.Add(GenerateQATPlacementMenuItem(_defaultRibbonClientAreaContextMenu));
                _defaultRibbonClientAreaContextMenu.Items.Add(new RibbonSeparator());
                _defaultRibbonClientAreaContextMenu.Items.Add(GenerateMinimizeTheRibbonItem(_defaultRibbonClientAreaContextMenu));
            }

            return _defaultRibbonClientAreaContextMenu;
        }

        private static RibbonContextMenu GetRibbonControlContextMenu()
        {
            if (_ribbonControlContextMenu == null)
            {
                _ribbonControlContextMenu = new RibbonContextMenu();
                _ribbonControlContextMenu.Items.Add(GenerateAddToOrRemoveFromQATItem(false, _ribbonControlContextMenu));
                _ribbonControlContextMenu.Items.Add(new RibbonSeparator());
                _ribbonControlContextMenu.Items.Add(GenerateQATPlacementMenuItem(_ribbonControlContextMenu));
                _ribbonControlContextMenu.Items.Add(new RibbonSeparator());
                _ribbonControlContextMenu.Items.Add(GenerateMinimizeTheRibbonItem(_ribbonControlContextMenu));
            }

            return _ribbonControlContextMenu;
        }

        private static RibbonContextMenu GetQATControlContextMenu()
        {
            if (_qatControlContextMenu == null)
            {
                _qatControlContextMenu = new RibbonContextMenu();
                _qatControlContextMenu.Items.Add(GenerateAddToOrRemoveFromQATItem(true, _qatControlContextMenu));
                _qatControlContextMenu.Items.Add(new RibbonSeparator());
                _qatControlContextMenu.Items.Add(GenerateQATPlacementMenuItem(_qatControlContextMenu));
                _qatControlContextMenu.Items.Add(new RibbonSeparator());
                _qatControlContextMenu.Items.Add(GenerateMinimizeTheRibbonItem(_qatControlContextMenu));
            }

            return _qatControlContextMenu;
        }

        #region RibbonGallery

        private static RibbonContextMenu GetGalleryContextMenu()
        {
            if (_galleryContextMenu == null)
            {
                _galleryContextMenu = new RibbonContextMenu();
                RibbonMenuItem addGalleryToQATItem = GenerateAddGalleryToQATItem(_galleryContextMenu);
                _galleryContextMenu.Items.Add(addGalleryToQATItem);
            }

            return _galleryContextMenu;
        }

        private static RibbonMenuItem GenerateAddGalleryToQATItem(RibbonContextMenu contextMenu)
        {
            RibbonMenuItem addGalleryToQATItem = new RibbonMenuItem();

            addGalleryToQATItem.Header = _addGalleryToQATText;

            // Even for galleries in QAT, this menu item always binds the "add to QAT" command.
            addGalleryToQATItem.Command = RibbonCommands.AddToQuickAccessToolBarCommand;

            Binding placementTargetBinding = new Binding("PlacementTarget") { Source = contextMenu };
            addGalleryToQATItem.SetBinding(RibbonMenuItem.CommandTargetProperty, placementTargetBinding);

            return addGalleryToQATItem;
        }

        #endregion

        private static RibbonMenuItem GenerateAddToOrRemoveFromQATItem(bool controlIsInQAT, RibbonContextMenu contextMenu)
        {
            RibbonMenuItem addToOrRemoveFromQATItem = new RibbonMenuItem() { CanAddToQuickAccessToolBarDirectly = false };

            if (controlIsInQAT)
            {
                addToOrRemoveFromQATItem.Header = RemoveFromQATText;
                addToOrRemoveFromQATItem.Command = RibbonCommands.RemoveFromQuickAccessToolBarCommand;
            }
            else
            {
                addToOrRemoveFromQATItem.Header = AddToQATText;
                addToOrRemoveFromQATItem.Command = RibbonCommands.AddToQuickAccessToolBarCommand;
            }

            Binding placementTargetBinding = new Binding("PlacementTarget") { Source = contextMenu };
            addToOrRemoveFromQATItem.SetBinding(RibbonMenuItem.CommandTargetProperty, placementTargetBinding);

            return addToOrRemoveFromQATItem;
        }

        private static RibbonMenuItem GenerateQATPlacementMenuItem(RibbonContextMenu contextMenu)
        {
            RibbonMenuItem qatPlacementItem = new RibbonMenuItem() { CanAddToQuickAccessToolBarDirectly = false };

            Binding headerBinding = new Binding("PlacementTarget") { Source = contextMenu };
            headerBinding.Converter = new PlacementTargetToQATPositionConverter(PlacementTargetToQATPositionConverter.ConverterMode.Header);
            qatPlacementItem.SetBinding(RibbonMenuItem.HeaderProperty, headerBinding);

            Binding commandBinding = new Binding("PlacementTarget") { Source = contextMenu };
            commandBinding.Converter = new PlacementTargetToQATPositionConverter(PlacementTargetToQATPositionConverter.ConverterMode.Command);
            qatPlacementItem.SetBinding(RibbonMenuItem.CommandProperty, commandBinding);

            Binding placementTargetBinding = new Binding("PlacementTarget") { Source = contextMenu };
            qatPlacementItem.SetBinding(RibbonMenuItem.CommandTargetProperty, placementTargetBinding);

            return qatPlacementItem;
        }

        // This converter allows us to determine whether a RibbonContextMenu's menu item corresponding
        // to QAT placement should correspond to "Show QAT Above the Ribbon" or "Show QAT Below the Ribbon".
        // The converter takes the PlacementTarget of the RibbonContextMenu and determines the QAT position,
        // which tells us which value to return.
        //
        // The ConverterMode enum allows us to reuse this Converter to determine both the correct Header
        // to display and the correct Command to fire.
        private sealed class PlacementTargetToQATPositionConverter : IValueConverter
        {
            public enum ConverterMode { Header, Command };

            private ConverterMode _mode;

            public PlacementTargetToQATPositionConverter(ConverterMode mode)
            {
                _mode = mode;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                DependencyObject d = value as DependencyObject;
                if (d != null)
                {
                    Ribbon ribbon = RibbonControlService.GetRibbon(d);
                    if (ribbon != null &&
                        !ribbon.ShowQuickAccessToolBarOnTop)
                    {
                        if (_mode == ConverterMode.Header)
                        {
                            return ShowQATAboveText;
                        }
                        else
                        {
                            return RibbonCommands.ShowQuickAccessToolBarAboveRibbonCommand;
                        }
                    }
                }

                if (_mode == ConverterMode.Header)
                {
                    return ShowQATBelowText;
                }
                else
                {
                    return RibbonCommands.ShowQuickAccessToolBarBelowRibbonCommand;
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }

        private static RibbonMenuItem GenerateMinimizeTheRibbonItem(RibbonContextMenu contextMenu)
        {
            RibbonMenuItem minimizeTheRibbonItem = new RibbonMenuItem() { CanAddToQuickAccessToolBarDirectly = false };
            minimizeTheRibbonItem.Header = MinimizeTheRibbonText;

            PropertyPath path = new PropertyPath("(0).(1).(2)");
            path.PathParameters.Add(ContextMenuService.PlacementTargetProperty);
            path.PathParameters.Add(RibbonControlService.RibbonProperty);
            path.PathParameters.Add(Ribbon.IsMinimizedProperty);

            Binding isCheckedBinding = new Binding () { Source = contextMenu, Path = path };
            minimizeTheRibbonItem.SetBinding(RibbonMenuItem.IsCheckedProperty, isCheckedBinding);
            Binding isMinimizedBinding = new Binding() { Source = contextMenu, Path = path };
            isMinimizedBinding.Converter = new IsMinimizedToMinimizeOrMaximizeCommandConverter();
            minimizeTheRibbonItem.SetBinding(RibbonMenuItem.CommandProperty, isMinimizedBinding);
            Binding placementTargetBinding = new Binding("PlacementTarget") { Source = contextMenu };
            minimizeTheRibbonItem.SetBinding(RibbonMenuItem.CommandTargetProperty, placementTargetBinding);
            return minimizeTheRibbonItem;
        }

        private sealed class IsMinimizedToMinimizeOrMaximizeCommandConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                bool ribbonIsMinimized = (bool)value;
                if (ribbonIsMinimized)
                {
                    return RibbonCommands.MaximizeRibbonCommand;
                }

                return RibbonCommands.MinimizeRibbonCommand;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }
        
        #endregion

        #region ContainerGeneration

        private object _currentItem;

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            bool ret = (item is RibbonMenuItem) || (item is RibbonSeparator) || (item is RibbonGallery);
            if (!ret)
            {
                _currentItem = item;
            }

            return ret;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            object currentItem = _currentItem;
            _currentItem = null;

            if (UsesItemContainerTemplate)
            {
                DataTemplate itemContainerTemplate = ItemContainerTemplateSelector.SelectTemplate(currentItem, this);
                if (itemContainerTemplate != null)
                {
                    object itemContainer = itemContainerTemplate.LoadContent();
                    if (itemContainer is RibbonMenuItem || itemContainer is RibbonGallery || itemContainer is RibbonSeparator)
                    {
                        return itemContainer as DependencyObject;
                    }
                    else
                    {
                        throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.InvalidMenuButtonOrItemContainer, this.GetType().Name, itemContainer));
                    }
                }
            }

            return new RibbonMenuItem();
        }

        protected override bool ShouldApplyItemContainerStyle(DependencyObject container, object item)
        {
            if (container is RibbonSeparator ||
                container is RibbonGallery)
            {
                return false;
            }
            else
            {
                return base.ShouldApplyItemContainerStyle(container, item);
            }
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is RibbonGallery)
            {
                HasGallery = (++_galleryCount > 0);
            }
            else
            {
                RibbonSeparator separator = element as RibbonSeparator;
                if (separator != null)
                {
                    ValueSource vs = DependencyPropertyHelper.GetValueSource(separator, StyleProperty);
                    if (vs.BaseValueSource <= BaseValueSource.ImplicitStyleReference)
                        separator.SetResourceReference(StyleProperty, MenuItem.SeparatorStyleKey);

                    separator.DefaultStyleKeyInternal = MenuItem.SeparatorStyleKey;
                }
            }
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);

            if (element is RibbonGallery)
            {
                HasGallery = (--_galleryCount > 0);
            }
        }

#if !RIBBON_IN_FRAMEWORK
        /// <summary>
        ///     DependencyProperty for ItemContainerTemplateSelector property.
        /// </summary>
        public static readonly DependencyProperty ItemContainerTemplateSelectorProperty =
            RibbonMenuButton.ItemContainerTemplateSelectorProperty.AddOwner(
                typeof(RibbonContextMenu),
                new FrameworkPropertyMetadata(new DefaultItemContainerTemplateSelector()));

        /// <summary>
        ///     DataTemplateSelector property which provides the DataTemplate to be used to create an instance of the ItemContainer.
        /// </summary>
        public ItemContainerTemplateSelector ItemContainerTemplateSelector
        {
            get { return (ItemContainerTemplateSelector)GetValue(ItemContainerTemplateSelectorProperty); }
            set { SetValue(ItemContainerTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for UsesItemContainerTemplate property.
        /// </summary>
        public static readonly DependencyProperty UsesItemContainerTemplateProperty =
            RibbonMenuButton.UsesItemContainerTemplateProperty.AddOwner(typeof(RibbonContextMenu));

        /// <summary>
        ///     UsesItemContainerTemplate property which says whether the ItemContainerTemplateSelector property is to be used.
        /// </summary>
        public bool UsesItemContainerTemplate
        {
            get { return (bool)GetValue(UsesItemContainerTemplateProperty); }
            set { SetValue(UsesItemContainerTemplateProperty, value); }
        }
#endif

        public static readonly DependencyProperty HasGalleryProperty = RibbonMenuButton.HasGalleryPropertyKey.DependencyProperty.AddOwner(typeof(RibbonContextMenu));

        /// <summary>
        /// Indicates that there is atleast one RibbonGallery in Items collection.
        /// </summary>
        public bool HasGallery
        {
            get { return (bool)GetValue(HasGalleryProperty); }
            private set { SetValue(RibbonMenuButton.HasGalleryPropertyKey, value); }
        }

        #endregion ContainerGeneration

        #region Private Methods

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RibbonHelper.FindAndHookPopup(this, ref _popup);
        }

        #endregion Private Methods

        #region Private Data

        private int _galleryCount;
        private Popup _popup;
        internal static string AddToQATText = Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.RibbonContextMenu_AddToQAT);
        private static string _addGalleryToQATText = Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.RibbonContextMenu_AddGalleryToQAT);
        internal static string RemoveFromQATText = Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.RibbonContextMenu_RemoveFromQAT);
        internal static string ShowQATAboveText = Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.RibbonContextMenu_ShowQATAbove);
        internal static string ShowQATBelowText = Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.RibbonContextMenu_ShowQATBelow);
        internal static string MaximizeTheRibbonText = Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.RibbonContextMenu_MaximizeTheRibbon);
        internal static string MinimizeTheRibbonText = Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.RibbonContextMenu_MinimizeTheRibbon);

        private bool _ignoreDismissPopupsOnNextClose = false;
        
        #endregion
    }

}
