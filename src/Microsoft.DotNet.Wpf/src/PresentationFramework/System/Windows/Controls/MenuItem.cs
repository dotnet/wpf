// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Utility;
using System.Diagnostics;
using System.Windows.Threading;
using System.Globalization;

using System.ComponentModel;
using System.Collections;
using System.Collections.Specialized;
using System.Security;


using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;

using System.Windows.Controls.Primitives;
using System.Windows.Shapes;
using System.Windows.Markup;

// Disable CS3001: Warning as Error: not CLS-compliant
#pragma warning disable 3001

namespace System.Windows.Controls
{
    /// <summary>
    ///     Defines the different placement types of MenuItems.
    /// </summary>
    public enum MenuItemRole
    {
        /// <summary>
        ///     A top-level menu item that can invoke commands.
        /// </summary>
        TopLevelItem,

        /// <summary>
        ///     Header for top-level menus.
        /// </summary>
        TopLevelHeader,

        /// <summary>
        ///     A menu item in a submenu that can invoke commands.
        /// </summary>
        SubmenuItem,

        /// <summary>
        ///     A header for a submenu.
        /// </summary>
        SubmenuHeader,
    }

    /// <summary>
    ///     A child item of Menu.
    ///     MenuItems can be selected to invoke commands.
    ///     MenuItems can be headers for submenus.
    ///     MenuItems can be checked or unchecked.
    /// </summary>
    [DefaultEvent("Click")]
    [Localizability(LocalizationCategory.Menu)]
    [TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(MenuItem))]
    public class MenuItem : HeaderedItemsControl, ICommandSource
    {
        // ----------------------------------------------------------------------------
        //  Defines the names of the resources to be consumed by the MenuItem style.
        //  Used to restyle several roles of MenuItem without having to restyle
        //  all of the control.
        // ----------------------------------------------------------------------------

        #region StyleKeys

        /// <summary>
        /// Key used to mark the template for use by TopLevel MenuItems
        /// </summary>
        public static ResourceKey TopLevelItemTemplateKey
        {
            get
            {
                if (_topLevelItemTemplateKey == null)
                {
                    _topLevelItemTemplateKey = new ComponentResourceKey(typeof(MenuItem), "TopLevelItemTemplateKey");
                }

                return _topLevelItemTemplateKey;
            }
        }

        /// <summary>
        /// Key used to mark the template for use by TopLevel Menu Header
        /// </summary>
        public static ResourceKey TopLevelHeaderTemplateKey
        {
            get
            {
                if (_topLevelHeaderTemplateKey == null)
                {
                    _topLevelHeaderTemplateKey = new ComponentResourceKey(typeof(MenuItem), "TopLevelHeaderTemplateKey");
                }

                return _topLevelHeaderTemplateKey;
            }
        }

        /// <summary>
        /// Key used to mark the template for use by Submenu Item
        /// </summary>
        public static ResourceKey SubmenuItemTemplateKey
        {
            get
            {
                if (_submenuItemTemplateKey == null)
                {
                    _submenuItemTemplateKey = new ComponentResourceKey(typeof(MenuItem), "SubmenuItemTemplateKey");
                }

                return _submenuItemTemplateKey;
            }
        }

        /// <summary>
        /// Key used to mark the template for use by Submenu Header
        /// </summary>
        public static ResourceKey SubmenuHeaderTemplateKey
        {
            get
            {
                if (_submenuHeaderTemplateKey == null)
                {
                    _submenuHeaderTemplateKey = new ComponentResourceKey(typeof(MenuItem), "SubmenuHeaderTemplateKey");
                }

                return _submenuHeaderTemplateKey;
            }
        }

        private static ComponentResourceKey _topLevelItemTemplateKey;
        private static ComponentResourceKey _topLevelHeaderTemplateKey;
        private static ComponentResourceKey _submenuItemTemplateKey;
        private static ComponentResourceKey _submenuHeaderTemplateKey;

        #endregion

        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Default MenuItem constructor
        /// </summary>
        public MenuItem() : base()
        {
        }

        static MenuItem()
        {
            HeaderProperty.OverrideMetadata(typeof(MenuItem), new FrameworkPropertyMetadata(null, new CoerceValueCallback(CoerceHeader)));

            EventManager.RegisterClassHandler(typeof(MenuItem), AccessKeyManager.AccessKeyPressedEvent, new AccessKeyPressedEventHandler(OnAccessKeyPressed));
            EventManager.RegisterClassHandler(typeof(MenuItem), MenuBase.IsSelectedChangedEvent, new RoutedPropertyChangedEventHandler<bool>(OnIsSelectedChanged));

            ForegroundProperty.OverrideMetadata(typeof(MenuItem), new FrameworkPropertyMetadata(SystemColors.MenuTextBrush));
            FontFamilyProperty.OverrideMetadata(typeof(MenuItem), new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily));
            FontSizeProperty.OverrideMetadata(typeof(MenuItem), new FrameworkPropertyMetadata(SystemFonts.MessageFontSize));
            FontStyleProperty.OverrideMetadata(typeof(MenuItem), new FrameworkPropertyMetadata(SystemFonts.MessageFontStyle));
            FontWeightProperty.OverrideMetadata(typeof(MenuItem), new FrameworkPropertyMetadata(SystemFonts.MessageFontWeight));

            // Disable tooltips on menu item when submenu is open
            ToolTipService.IsEnabledProperty.OverrideMetadata(typeof(MenuItem), new FrameworkPropertyMetadata(null, new CoerceValueCallback(CoerceToolTipIsEnabled)));

#if OLD_AUTOMATION
            AutomationProvider.AcceleratorKeyProperty.OverrideMetadata(typeof(MenuItem), new FrameworkPropertyMetadata(null, (PropertyChangedCallback)null, new CoerceValueCallback(OnCoerceAcceleratorKey)));
#endif

            DefaultStyleKeyProperty.OverrideMetadata(typeof(MenuItem), new FrameworkPropertyMetadata(typeof(MenuItem)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(MenuItem));

            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(MenuItem), new FrameworkPropertyMetadata(KeyboardNavigationMode.None));

            // Disable default focus visual for MenuItem.
            FocusVisualStyleProperty.OverrideMetadata(typeof(MenuItem), new FrameworkPropertyMetadata((object)null /* default value */));


            // While the menu is opened, Input Method should be suspended.
            // the docusmen focus of Cicero should not be changed but key typing should not be
            // dispatched to IME/TIP.
            InputMethod.IsInputMethodSuspendedProperty.OverrideMetadata(typeof(MenuItem), new FrameworkPropertyMetadata(BooleanBoxes.TrueBox, FrameworkPropertyMetadataOptions.Inherits));
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideMetadata(typeof(MenuItem), new FrameworkPropertyMetadata(IsOffscreenBehavior.FromClip));
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Events
        //
        //-------------------------------------------------------------------

        #region Public Events

        /// <summary>
        ///     Event corresponds to left mouse button click
        /// </summary>
        public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent("Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MenuItem));

        /// <summary>
        ///     Add / Remove Click handler
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler Click
        {
            add
            {
                AddHandler(MenuItem.ClickEvent, value);
            }

            remove
            {
                RemoveHandler(MenuItem.ClickEvent, value);
            }
        }

        /// <summary>
        ///     Event that is fired when mouse button is pressed down but before menus are closed.
        ///     This event should be handled by the parent menu and used to know when to close all submenus.
        /// </summary>
        internal static readonly RoutedEvent PreviewClickEvent = EventManager.RegisterRoutedEvent("PreviewClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MenuItem));

        /// <summary>
        ///     Checked event
        /// </summary>
        public static readonly RoutedEvent CheckedEvent = EventManager.RegisterRoutedEvent("Checked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MenuItem));

        /// <summary>
        ///     Unchecked event
        /// </summary>
        public static readonly RoutedEvent UncheckedEvent = EventManager.RegisterRoutedEvent("Unchecked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MenuItem));

        /// <summary>
        ///     Add / Remove Checked handler
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler Checked
        {
            add
            {
                AddHandler(CheckedEvent, value);
            }

            remove
            {
                RemoveHandler(CheckedEvent, value);
            }
        }

        /// <summary>
        ///     Add / Remove Unchecked handler
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler Unchecked
        {
            add
            {
                AddHandler(UncheckedEvent, value);
            }

            remove
            {
                RemoveHandler(UncheckedEvent, value);
            }
        }


        /// <summary>
        ///     Event fires when submenu opens
        /// </summary>
        public static readonly RoutedEvent SubmenuOpenedEvent =
            EventManager.RegisterRoutedEvent("SubmenuOpened", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MenuItem));

        /// <summary>
        ///     Event fires when submenu closes
        /// </summary>
        public static readonly RoutedEvent SubmenuClosedEvent =
            EventManager.RegisterRoutedEvent("SubmenuClosed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MenuItem));

        /// <summary>
        ///     Add / Remove SubmenuOpenedEvent handler
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler SubmenuOpened
        {
            add
            {
                AddHandler(SubmenuOpenedEvent, value);
            }
            remove
            {
                RemoveHandler(SubmenuOpenedEvent, value);
            }
        }

        /// <summary>
        ///     Add / Remove SubmenuClosedEvent handler
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler SubmenuClosed
        {
            add
            {
                AddHandler(SubmenuClosedEvent, value);
            }
            remove
            {
                RemoveHandler(SubmenuClosedEvent, value);
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        // Set the header to the command text if no header has been explicitly specified
        private static object CoerceHeader(DependencyObject d, object value)
        {
            MenuItem menuItem = (MenuItem)d;
            RoutedUICommand uiCommand;

            // If no header has been set, use the command's text
            if (value == null && !menuItem.HasNonDefaultValue(HeaderProperty))
            {
                uiCommand = menuItem.Command as RoutedUICommand;
                if (uiCommand != null)
                {
                    value = uiCommand.Text;
                }
                return value;
            }

            // If the header had been set to a UICommand by the ItemsControl, replace it with the command's text
            uiCommand = value as RoutedUICommand;

            if (uiCommand != null)
            {
                // The header is equal to the command.
                // If this MenuItem was generated for the command, then go ahead and overwrite the header
                // since the generator automatically set the header.
                ItemsControl parent = ItemsControl.ItemsControlFromItemContainer(menuItem);
                if (parent != null)
                {
                    object originalItem = parent.ItemContainerGenerator.ItemFromContainer(menuItem);

                    if (originalItem == value)
                    {
                        return uiCommand.Text;
                    }
                }
            }

            return value;
        }

        /// <summary>
        ///     The DependencyProperty for the RoutedCommand.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
                ButtonBase.CommandProperty.AddOwner(
                        typeof(MenuItem),
                        new FrameworkPropertyMetadata(
                                (ICommand)null,
                                new PropertyChangedCallback(OnCommandChanged)));

        /// <summary>
        ///     The MenuItem's Command.
        /// </summary>
        [Bindable(true), Category("Action")]
        [Localizability(LocalizationCategory.NeverLocalize)]
        public ICommand Command
        {
            get { return (ICommand) GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MenuItem item = (MenuItem) d;
            item.OnCommandChanged((ICommand) e.OldValue, (ICommand) e.NewValue);
        }

        private void OnCommandChanged(ICommand oldCommand, ICommand newCommand)
        {
            if (oldCommand != null)
            {
                UnhookCommand(oldCommand);
            }
            if (newCommand != null)
            {
                HookCommand(newCommand);
            }

            CoerceValue(HeaderProperty);
            CoerceValue(InputGestureTextProperty);
        }

        private void UnhookCommand(ICommand command)
        {
            CanExecuteChangedEventManager.RemoveHandler(command, OnCanExecuteChanged);
            UpdateCanExecute();
        }

        private void HookCommand(ICommand command)
        {
            CanExecuteChangedEventManager.AddHandler(command, OnCanExecuteChanged);
            UpdateCanExecute();
        }

        private void OnCanExecuteChanged(object sender, EventArgs e)
        {
            UpdateCanExecute();
        }

        private void UpdateCanExecute()
        {
            MenuItem.SetBoolField(this, BoolField.CanExecuteInvalid, false);
            if (Command != null)
            {
                // Perf optimization - only raise CanExecute event if the menu is open
                MenuItem parent = ItemsControl.ItemsControlFromItemContainer(this) as MenuItem;
                if (parent == null || parent.IsSubmenuOpen)
                {
                    CanExecute = MS.Internal.Commands.CommandHelpers.CanExecuteCommandSource(this);
                }
                else
                {
                    CanExecute = true;
                    MenuItem.SetBoolField(this, BoolField.CanExecuteInvalid, true);
                }
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
        ///     The reason this property is overridden is so that MenuItem
        ///     can infuse the value for CanExecute into it.
        /// </remarks>
        protected override bool IsEnabledCore
        {
            get
            {
                return base.IsEnabledCore && CanExecute;
            }
        }


        /// <summary>
        ///     The DependencyProperty for the RoutedCommand's parameter.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty =
                ButtonBase.CommandParameterProperty.AddOwner(
                        typeof(MenuItem),
                        new FrameworkPropertyMetadata((object) null));

        /// <summary>
        ///     The parameter to pass to MenuItem's Command.
        /// </summary>
        [Bindable(true), Category("Action")]
        [Localizability(LocalizationCategory.NeverLocalize)]
        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for Target property
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty CommandTargetProperty =
                ButtonBase.CommandTargetProperty.AddOwner(
                        typeof(MenuItem),
                        new FrameworkPropertyMetadata((IInputElement) null));

        /// <summary>
        ///     The target element on which to fire the command.
        /// </summary>
        [Bindable(true), Category("Action")]
        public IInputElement CommandTarget
        {
            get { return (IInputElement)GetValue(CommandTargetProperty); }
            set { SetValue(CommandTargetProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the IsSubmenuOpen property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsSubmenuOpenProperty =
                DependencyProperty.Register(
                        "IsSubmenuOpen",
                        typeof(bool),
                        typeof(MenuItem),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                new PropertyChangedCallback(OnIsSubmenuOpenChanged),
                                new CoerceValueCallback(CoerceIsSubmenuOpen)));
        /// <summary>
        ///     When the MenuItem's submenu is visible.
        /// </summary>
        [Bindable(true), Browsable(false), Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsSubmenuOpen
        {
            get { return (bool) GetValue(IsSubmenuOpenProperty); }
            set { SetValue(IsSubmenuOpenProperty, BooleanBoxes.Box(value)); }
        }

        private static object CoerceIsSubmenuOpen(DependencyObject d, object value)
        {
            if ((bool) value)
            {
                MenuItem mi = (MenuItem) d;
                if (!mi.IsLoaded)
                {
                    mi.RegisterToOpenOnLoad();
                    return BooleanBoxes.FalseBox;
                }
            }

            return value;
        }

        // Disable tooltips on opened menu items
        private static object CoerceToolTipIsEnabled(DependencyObject d, object value)
        {
            MenuItem mi = (MenuItem) d;
            return mi.IsSubmenuOpen ? BooleanBoxes.FalseBox : value;
        }

        private void RegisterToOpenOnLoad()
        {
            Loaded += new RoutedEventHandler(OpenOnLoad);
        }

        private void OpenOnLoad(object sender, RoutedEventArgs e)
        {
            // Open menu after it has rendered (Loaded is fired before 1st render)
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new DispatcherOperationCallback(delegate(object param)
            {
                CoerceValue(IsSubmenuOpenProperty);

                return null;
            }), null);
        }

        /// <summary>
        ///     Called when IsSubmenuOpenID is invalidated on "d."
        /// </summary>
        private static void OnIsSubmenuOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)d;

            bool oldValue = (bool) e.OldValue;
            bool newValue = (bool) e.NewValue;
            // The IsSubmenuOpen value has changed; this should stop any timers
            // we may have set to open/close the menus.
            menuItem.StopTimer(ref menuItem._openHierarchyTimer);
            menuItem.StopTimer(ref menuItem._closeHierarchyTimer);

            MenuItemAutomationPeer peer = UIElementAutomationPeer.FromElement(menuItem) as MenuItemAutomationPeer;
            if (peer != null)
            {
                peer.ResetChildrenCache();
                peer.RaiseExpandCollapseAutomationEvent(oldValue, newValue);
            }

            if (newValue)
            {
                CommandManager.InvalidateRequerySuggested(); // Should post an idle queue item to update IsEnabled on commands

                // When menuitem's submenu opens, it should be selected.
                menuItem.SetCurrentValueInternal(IsSelectedProperty, BooleanBoxes.TrueBox);

                MenuItemRole role = menuItem.Role;
                if (role == MenuItemRole.TopLevelHeader)
                {
                    menuItem.SetMenuMode(true);
                }
                menuItem.CurrentSelection = null;

                // When our submenu opens, update our siblings so they do not animate
                menuItem.NotifySiblingsToSuspendAnimation();

                // Force update of CanExecute when opening menu.
                for (int i = 0; i < menuItem.Items.Count; i++)
                {
                    MenuItem subItem = menuItem.ItemContainerGenerator.ContainerFromIndex(i) as MenuItem;
                    if (subItem != null && MenuItem.GetBoolField(subItem, BoolField.CanExecuteInvalid))
                    {
                        subItem.UpdateCanExecute();
                    }
                }

                menuItem.OnSubmenuOpened(new RoutedEventArgs(SubmenuOpenedEvent, menuItem));


                MenuItem.SetBoolField(menuItem, BoolField.IgnoreMouseEvents, true);
                MenuItem.SetBoolField(menuItem, BoolField.MouseEnterOnMouseMove, false);

                // MenuItem should ignore any mouse enter or move events until the menu has fully
                // opened.  Otherwise we may highlight a menu item under the mouse even though
                // the user opened the menu with the keyboard
                // This is fired below input priority so any mouse events happen before setting the flag
                menuItem.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate(object param)
                {
                    MenuItem.SetBoolField(menuItem, BoolField.IgnoreMouseEvents, false);
                    return null;
                }), null);
            }
            else
            {
                // Our submenu is closing, so close our submenu's submenu
                if (menuItem.CurrentSelection != null)
                {
                    // We're about to close the submenu -- if focus is within
                    // the subtree, we need to take it back so that Focus isn't
                    // left in an orphaned tree.
                    if (menuItem.CurrentSelection.IsKeyboardFocusWithin)
                    {
                        menuItem.Focus();
                    }

                    if (menuItem.CurrentSelection.IsSubmenuOpen)
                    {
                        menuItem.CurrentSelection.SetCurrentValueInternal(IsSubmenuOpenProperty, BooleanBoxes.FalseBox);
                    }
                }
                else
                {
                    // We need to take focus out of the subtree if we close
                    // the submenu.  Above we can be sure that focus will be
                    // on the selected item so we just need to check if IsFocusWithin
                    // is true on the selected item.  If we have no CurrentSelection,
                    // we have to be a little more aggressive and take focus
                    // back if IsFocusWithin is true.
                    //
                    // NOTE: This could potentially steal focus back from something
                    //       within the menuitem's header (say, a TextBox) but it is
                    //       unlikely that focus will be within a header while the submenu
                    //       is open.

                    if (menuItem.IsKeyboardFocusWithin)
                    {
                        if (!menuItem.Focus())
                        {
                            // Shoot, we couldn't take focus out of the submenu
                            // and put it back on ourselves.  Now focus is in a
                            // disconnected subtree.  Ultimately core input will
                            // disallow this, presumably by setting focus to null.
                            // For now we won't handle this case.
                        }
                    }
                }

                menuItem.CurrentSelection = null;

                if ((menuItem.IsMouseOver) && (menuItem.Role == MenuItemRole.SubmenuHeader))
                {
                    // If the mouse is inside the subtree, then we will get a mouse leave, but we want to ignore it
                    // to maintain the highlight.
                    MenuItem.SetBoolField(menuItem, BoolField.IgnoreNextMouseLeave, true);
                }

                // When our submenu closes, update our children so they will animate
                menuItem.NotifyChildrenToResumeAnimation();

                // No Popup in the style so fire closed now
                if (menuItem._submenuPopup == null)
                {
                    menuItem.OnSubmenuClosed(new RoutedEventArgs(SubmenuClosedEvent, menuItem));
                }
            }

            menuItem.CoerceValue(ToolTipService.IsEnabledProperty);
        }

        private void OnPopupClosed(object source, EventArgs e)
        {
            OnSubmenuClosed(new RoutedEventArgs(SubmenuClosedEvent, this));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnSubmenuOpened(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnSubmenuClosed(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey RolePropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "Role",
                        typeof(MenuItemRole),
                        typeof(MenuItem),
                        new FrameworkPropertyMetadata(MenuItemRole.TopLevelItem));

        /// <summary>
        ///     The DependencyProperty for the Role property.
        ///     Flags:              None
        ///     Default Value:      MenuItemRole.TopLevelItem
        /// </summary>
        public static readonly DependencyProperty RoleProperty =
                RolePropertyKey.DependencyProperty;

        /// <summary>
        ///     What the role of the menu item is: TopLevelItem, TopLevelHeader, SubmenuItem, SubmenuHeader.
        /// </summary>
        [Category("Behavior")]
        public MenuItemRole Role
        {
            get { return (MenuItemRole) GetValue(RoleProperty); }
        }

        private void UpdateRole()
        {
            MenuItemRole type;

            if (!IsCheckable && HasItems)
            {
                if (LogicalParent is Menu)
                {
                    type = MenuItemRole.TopLevelHeader;
                }
                else
                {
                    type = MenuItemRole.SubmenuHeader;
                }
            }
            else
            {
                if (LogicalParent is Menu)
                {
                    type = MenuItemRole.TopLevelItem;
                }
                else
                {
                    type = MenuItemRole.SubmenuItem;
                }
            }

            SetValue(RolePropertyKey, type);
        }

        /// <summary>
        ///     The DependencyProperty for the IsCheckable property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsCheckableProperty =
                DependencyProperty.Register(
                        "IsCheckable",
                        typeof(bool),
                        typeof(MenuItem),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox,
                                new PropertyChangedCallback(OnIsCheckableChanged)));

        /// <summary>
        ///     IsCheckable determines the user ability to check/uncheck the item.
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public bool IsCheckable
        {
            get { return (bool)GetValue(IsCheckableProperty); }
            set { SetValue(IsCheckableProperty, value); }
        }

        private static void OnIsCheckableChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            ((MenuItem) target).UpdateRole();
        }

        /// <summary>
        ///     The DependencyPropertyKey for the IsPressed property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        private static readonly DependencyPropertyKey IsPressedPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "IsPressed",
                        typeof(bool),
                        typeof(MenuItem),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        ///     The DependencyProperty for the IsPressed property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsPressedProperty = IsPressedPropertyKey.DependencyProperty;

        /// <summary>
        ///     When the MenuItem is pressed.
        /// </summary>
        [Browsable(false), Category("Appearance")]
        public bool IsPressed
        {
            get { return (bool) GetValue(IsPressedProperty); }
            protected set { SetValue(IsPressedPropertyKey, BooleanBoxes.Box(value)); }
        }

        private void UpdateIsPressed()
        {
            Rect itemBounds = new Rect(new Point(), RenderSize);

            if ((Mouse.LeftButton == MouseButtonState.Pressed) &&
                IsMouseOver &&
                itemBounds.Contains(Mouse.GetPosition(this)))
            {
                IsPressed = true;
            }
            else
            {
                ClearValue(IsPressedPropertyKey);
            }
        }

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey IsHighlightedPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "IsHighlighted",
                        typeof(bool),
                        typeof(MenuItem),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        ///     The DependencyProperty for the IsHighlighted property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsHighlightedProperty =
                IsHighlightedPropertyKey.DependencyProperty;

        /// <summary>
        ///     Whether the MenuItem should be highlighted.
        /// </summary>
        [Browsable(false), Category("Appearance")]
        public bool IsHighlighted
        {
            get { return (bool) GetValue(IsHighlightedProperty); }
            protected set { SetValue(IsHighlightedPropertyKey, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        ///     The DependencyProperty for the IsChecked property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsCheckedProperty =
                DependencyProperty.Register(
                        "IsChecked",
                        typeof(bool),
                        typeof(MenuItem),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
                                new PropertyChangedCallback(OnIsCheckedChanged)));

        /// <summary>
        ///     When the MenuItem is checked.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public bool IsChecked
        {
            get { return (bool) GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        ///     Called when IsChecked becomes true.
        /// </summary>
        /// <param name="e">Event arguments for the routed event that is raised by the default implementation of this method.</param>
        protected virtual void OnChecked(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Called when IsChecked becomes false.
        /// </summary>
        /// <param name="e">Event arguments for the routed event that is raised by the default implementation of this method.</param>
        protected virtual void OnUnchecked(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Called when IsCheckedProperty is invalidated on "d."
        /// </summary>
        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MenuItem menuItem = (MenuItem) d;

            if ((bool) e.NewValue)
            {
                menuItem.OnChecked(new RoutedEventArgs(CheckedEvent));
            }
            else
            {
                menuItem.OnUnchecked(new RoutedEventArgs(UncheckedEvent));
            }
        }

        /// <summary>
        ///     The DependencyProperty for the StaysOpenOnClick property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty StaysOpenOnClickProperty =
                DependencyProperty.Register(
                        "StaysOpenOnClick",
                        typeof(bool),
                        typeof(MenuItem),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        ///     Indicates that the submenu that this MenuItem is within should not close when this item is clicked.
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public bool StaysOpenOnClick
        {
            get { return (bool) GetValue(StaysOpenOnClickProperty); }
            set { SetValue(StaysOpenOnClickProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        ///     True if this MenuItem is the current MenuItem of its parent.
        ///     Focus drives Selection, but not vice versa.  This will enable
        ///     focusless menus.
        /// </summary>
        internal bool IsSelected
        {
            get { return (bool) GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        ///     DependencyProperty for IsSelected property.
        /// </summary>
        internal static readonly DependencyProperty IsSelectedProperty =
                Selector.IsSelectedProperty.AddOwner(
                        typeof(MenuItem),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                new PropertyChangedCallback(OnIsSelectedChanged)));

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)d;
            // When IsSelected changes, IsHighlighted should reflect IsSelected
            // Note: it is okay for IsHighlighted and IsSelected to be different.
            //       Selection and highlight will separate when mousing around in
            //       a submenu when any timers are active.  Until you hover long
            //       enough and your selection is "committed", selection and highlight
            //       can disagree.
            menuItem.SetValue(IsHighlightedPropertyKey, e.NewValue);

            // If IsSelected is changing to false, make sure to close
            // our submenu before doing anything.
            if ((bool) e.OldValue)
            {
                if (menuItem.IsSubmenuOpen)
                {
                    menuItem.SetCurrentValueInternal(IsSubmenuOpenProperty, BooleanBoxes.FalseBox);
                }

                // Also stop any timers immediately when we become deselected.
                menuItem.StopTimer(ref menuItem._openHierarchyTimer);
                menuItem.StopTimer(ref menuItem._closeHierarchyTimer);
            }

            menuItem.RaiseEvent(new RoutedPropertyChangedEventArgs<bool>((bool) e.OldValue, (bool) e.NewValue, MenuBase.IsSelectedChangedEvent));
        }

        /// <summary>
        ///     Called when IsSelected changed on this element or any descendant.
        /// </summary>
        private static void OnIsSelectedChanged(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            // If IsSelected changed on a child of the MenuItem, change CurrentSelection
            // to the element that sent the event and handle the event.
            if (sender != e.OriginalSource)
            {
                MenuItem menuItem = (MenuItem)sender;
                MenuItem source = e.OriginalSource as MenuItem;

                if (source != null)
                {
                    if (e.NewValue)
                    {
                        // If the item is now selected, we should stop any timers which will
                        // close the submenu.  This is for the case where one mouses out of
                        // the current selection but then comes back.
                        if (menuItem.CurrentSelection == source)
                        {
                            menuItem.StopTimer(ref menuItem._closeHierarchyTimer);
                        }

                        // If the MenuItem is selected and it's a new item that's a child of ours,
                        // change the CurrentSelection.
                        if (menuItem.CurrentSelection != source && source.LogicalParent == menuItem)
                        {
                            if (menuItem.CurrentSelection != null && menuItem.CurrentSelection.IsSubmenuOpen)
                            {
                                menuItem.CurrentSelection.SetCurrentValueInternal(IsSubmenuOpenProperty, BooleanBoxes.FalseBox);
                            }

                            menuItem.CurrentSelection = source;
                        }
                    }
                    else
                    {
                        // If the item is no longer selected
                        // If the MenuItem has been deselected and it's the CurrentSelection,
                        // set our CurrentSelection to null.
                        if (menuItem.CurrentSelection == source)
                        {
                            menuItem.CurrentSelection = null;
                        }
                    }

                    // Mark the event as handled as long as it came from a MenuItem underneath us
                    // even if we didn't necessarily do anything.
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        ///     The DependencyProperty for the InputGestureText property.
        ///     Default Value:      String.Empty
        /// </summary>
        public static readonly DependencyProperty InputGestureTextProperty =
                DependencyProperty.Register(
                        "InputGestureText",
                        typeof(string),
                        typeof(MenuItem),
                        new FrameworkPropertyMetadata(String.Empty,
                                                      new PropertyChangedCallback(OnInputGestureTextChanged),
                                                      new CoerceValueCallback(CoerceInputGestureText)));

        /// <summary>
        ///     Text describing an input gesture that will invoke the command tied to this item.
        /// </summary>
        [Bindable(true), CustomCategory("Content")]
        public string InputGestureText
        {
            get { return (string) GetValue(InputGestureTextProperty); }
            set { SetValue(InputGestureTextProperty, value); }
        }

        private static void OnInputGestureTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
#if OLD_AUTOMATION
            d.CoerceValue(AutomationProvider.AcceleratorKeyProperty);
#endif
        }

        // Gets the input gesture text from the command text if it hasn't been explicitly specified
        private static object CoerceInputGestureText(DependencyObject d, object value)
        {
            MenuItem menuItem = (MenuItem)d;
            RoutedCommand routedCommand;

            if (String.IsNullOrEmpty((string)value) && !menuItem.HasNonDefaultValue(InputGestureTextProperty)
                && (routedCommand = menuItem.Command as RoutedCommand) != null )
            {
                InputGestureCollection col = routedCommand.InputGestures;
                if ((col != null) && (col.Count >= 1))
                {
                    // Search for the first key gesture
                    for (int i = 0; i < col.Count; i++)
                    {
                        KeyGesture keyGesture = ((IList)col)[i] as KeyGesture;
                        if (keyGesture != null)
                        {
                            return keyGesture.GetDisplayStringForCulture(CultureInfo.CurrentCulture);
                        }
                    }
                }
            }

            return value;
        }

        /// <summary>
        ///     The DependencyProperty for the Icon property.
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty IconProperty =
                DependencyProperty.Register(
                        "Icon",
                        typeof(object),
                        typeof(MenuItem),
                        new FrameworkPropertyMetadata((object)null));

        /// <summary>
        ///     Text describing an input gesture that will invoke the command tied to this item.
        /// </summary>
        [Bindable(true), CustomCategory("Content")]
        public object Icon
        {
            get { return GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        // This is used to disable animations after the menu has displayed once
        private static readonly DependencyPropertyKey IsSuspendingPopupAnimationPropertyKey
            = DependencyProperty.RegisterReadOnly("IsSuspendingPopupAnimation", typeof(bool), typeof(MenuItem),
                                          new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// Returns true if the Menu should suspend animations on its popup
        /// </summary>
        public static readonly DependencyProperty IsSuspendingPopupAnimationProperty = IsSuspendingPopupAnimationPropertyKey.DependencyProperty;

        /// <summary>
        /// Returns true if the Menu should suspend animations on its popup
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsSuspendingPopupAnimation
        {
            get
            {
                return (bool)GetValue(IsSuspendingPopupAnimationProperty);
            }
            internal set
            {
                SetValue(IsSuspendingPopupAnimationPropertyKey, BooleanBoxes.Box(value));
            }
        }

        // When opening the menu item, tell all other menu items at the same
        // level that their submenus should not animate
        private void NotifySiblingsToSuspendAnimation()
        {
            // Don't need to set this property if it is already false
            if (!IsSuspendingPopupAnimation)
            {
                bool openedWithKeyboard = MenuItem.GetBoolField(this, BoolField.OpenedWithKeyboard);

                // When opened by the keyboard, don't animate - set menumode on all items
                // otherwise ignore this MenuItem so it animates when opening
                MenuItem ignore = openedWithKeyboard ? null : this;

                ItemsControl parent = ItemsControl.ItemsControlFromItemContainer(this);
                MenuBase.SetSuspendingPopupAnimation(parent, ignore, true);

                if (!openedWithKeyboard)
                {
                    // Delay setting InMenuMode on this until after bindings have done their
                    // work and opened the popup (if it exists)
                    Dispatcher.BeginInvoke(DispatcherPriority.Input,
                            (DispatcherOperationCallback)delegate(object arg)
                            {
                                ((MenuItem)arg).IsSuspendingPopupAnimation = true;
                                return null;
                            },
                            this);
                }
                else
                {
                    MenuItem.SetBoolField(this, BoolField.OpenedWithKeyboard, false);
                }
            }
        }

        // Set IsSuspendingAnimation=false on all our children
        private void NotifyChildrenToResumeAnimation()
        {
            MenuBase.SetSuspendingPopupAnimation(this, null, false);
        }

        /// <summary>
        ///     DependencyProperty for ItemContainerTemplateSelector property.
        /// </summary>
        public static readonly DependencyProperty ItemContainerTemplateSelectorProperty =
            MenuBase.ItemContainerTemplateSelectorProperty.AddOwner(
                typeof(MenuItem),
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
            MenuBase.UsesItemContainerTemplateProperty.AddOwner(typeof(MenuItem));

        /// <summary>
        ///     UsesItemContainerTemplate property which says whether the ItemContainerTemplateSelector property is to be used.
        /// </summary>
        public bool UsesItemContainerTemplate
        {
            get { return (bool)GetValue(UsesItemContainerTemplateProperty); }
            set { SetValue(UsesItemContainerTemplateProperty, value); }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new System.Windows.Automation.Peers.MenuItemAutomationPeer(this);
        }

        /// <summary>
        ///     This virtual method in called when IsInitialized is set to true and it raises an Initialized event
        /// </summary>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            UpdateRole();
#if OLD_AUTOMATION
            CoerceValue(AutomationProvider.AcceleratorKeyProperty);
#endif
        }

        /// <summary>
        /// Prepare the element to display the item.  This may involve
        /// applying styles, setting bindings, etc.
        /// </summary>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            MenuItem.PrepareMenuItem(element, item);
        }

        /// <summary>
        ///     Automatically set the Command property if the data item that this MenuItem represents is a command.
        /// </summary>
        internal static void PrepareMenuItem(DependencyObject element, object item)
        {
            MenuItem menuItem = element as MenuItem;
            if (menuItem != null)
            {
                ICommand command = item as ICommand;
                if (command != null)
                {
                    if (!menuItem.HasNonDefaultValue(CommandProperty))
                    {
                        menuItem.Command = command;
                    }
                }

                if (MenuItem.GetBoolField(menuItem, BoolField.CanExecuteInvalid))
                {
                    menuItem.UpdateCanExecute();
                }
            }
            else
            {
                Separator separator = item as Separator;
                if (separator != null)
                {
                    bool hasModifiers;
                    BaseValueSourceInternal vs = separator.GetValueSource(StyleProperty, null, out hasModifiers);
                    if (vs <= BaseValueSourceInternal.ImplicitReference)
                        separator.SetResourceReference(StyleProperty, SeparatorStyleKey);

                    separator.DefaultStyleKey = SeparatorStyleKey;
                }
            }
        }

        /// <summary>
        /// This virtual method in called when the MenuItem is clicked and it raises a Click event
        /// </summary>
        protected virtual void OnClick()
        {
            OnClickImpl(false);
        }

        internal virtual void OnClickCore(bool userInitiated)
        {
            OnClick();
        }

        internal void OnClickImpl(bool userInitiated)
        {
            if (IsCheckable)
            {
                SetCurrentValueInternal(IsCheckedProperty, BooleanBoxes.Box(!IsChecked));
            }
            // Sub menu items will always be focused if they are moused over or keyboard navigated onto.
            // When you click on a top-level menu item it should take focus.
            // Sub menu items will not be focused if the mouse has moved out of
            // the active hierarchy and has not settled on a new hierarchy yet.
            if (!IsKeyboardFocusWithin)
            {
                FocusOrSelect();
            }

            // Raise the preview click.  This will be handled by the parent menu and cause this submenu to disappear.
            // It will also block until render-priority queue items have completed.
            RaiseEvent(new RoutedEventArgs(MenuItem.PreviewClickEvent, this));

            // Raise the automation event first *before* raising the Click event -
            // otherwise automation may not get the event until after raising the click
            // event returns, which could be problematic if the handler for that event
            // displayed a modal dialog or did other significant work.
            if (AutomationPeer.ListenerExists(AutomationEvents.InvokePatternOnInvoked))
            {
                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(this);
                if (peer != null)
                    peer.RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
            }

            // We have just caused all the popup windows to be hidden and queued for async
            // destroy (at < render priority).  Hiding the window will cause the underlying windows
            // to be queued for repaint -- we need to wait for any windows in our context to repaint.
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(InvokeClickAfterRender), userInitiated);
        }

        private object InvokeClickAfterRender(object arg)
        {
            bool userInitiated = (bool)arg;
            RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, this));
            MS.Internal.Commands.CommandHelpers.CriticalExecuteCommandSource(this, userInitiated);
            return null;
        }


        /// <summary>
        ///        Called when the left mouse button is pressed.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!e.Handled)
            {
                HandleMouseDown(e);
                UpdateIsPressed();
                if (e.UserInitiated)
                {
                    _userInitiatedPress = true;
                }
            }
            base.OnMouseLeftButtonDown(e);
        }


        /// <summary>
        ///        Called when the right mouse button is pressed.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if (!e.Handled)
            {
                HandleMouseDown(e);
                if (e.UserInitiated)
                {
                    _userInitiatedPress = true;
                }
            }
            base.OnMouseRightButtonDown(e);
        }

        /// <summary>
        ///        Called when the left mouse button is released.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (!e.Handled)
            {
                HandleMouseUp(e);
                UpdateIsPressed();
                _userInitiatedPress = false;
            }
            base.OnMouseLeftButtonUp(e);
        }

        /// <summary>
        ///        Called when the right mouse button is released.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            if (!e.Handled)
            {
                HandleMouseUp(e);
                _userInitiatedPress = false;
            }
            base.OnMouseRightButtonUp(e);
        }

        private void HandleMouseDown(MouseButtonEventArgs e)
        {
            // ((0, 0), RenderSize) is the closest we can get to checking if the
            // mouse event was on the header portion of the MenuItem (i.e. not on
            // any part of the submenu)
            Rect r = new Rect(new Point(), RenderSize);

            if (r.Contains(e.GetPosition(this)))
            {
                if (e.ChangedButton == MouseButton.Left || (e.ChangedButton == MouseButton.Right && InsideContextMenu))
                {
                    // Click happens on down for headers
                    MenuItemRole role = Role;

                    if (role == MenuItemRole.TopLevelHeader || role == MenuItemRole.SubmenuHeader)
                    {
                        ClickHeader();
                    }
                }
            }
            // Handle mouse messages b/c they were over me, I just didn't use it
            e.Handled = true;
        }

        private void HandleMouseUp(MouseButtonEventArgs e)
        {
            // See comment above in HandleMouseDown.
            Rect r = new Rect(new Point(), RenderSize);

            if (r.Contains(e.GetPosition(this)))
            {
                if (e.ChangedButton == MouseButton.Left || (e.ChangedButton == MouseButton.Right && InsideContextMenu))
                {
                    // Click happens on up for items
                    MenuItemRole role = Role;

                    if (role == MenuItemRole.TopLevelItem || role == MenuItemRole.SubmenuItem)
                    {
                        if (_userInitiatedPress == true)
                        {
                            ClickItem(e.UserInitiated);
                        }
                        else
                        {
                            // This is the case where the mouse down happend on a different element
                            // but the moust up is happening on the menuitem. this is to prevent spoofing
                            // attacks where someone substitutes an element with a menu item
                            ClickItem(false);
                        }
                    }

                    // Need to close on second click
                    /*
                    // Click happens on up for top level items that are already open
                    if (role == MenuItemRole.TopLevelHeader && IsSubmenuOpen)
                    {
                        ClickHeader();
                        e.Handled = true;
                    }
                    */
                }
            }

            if (e.ChangedButton != MouseButton.Right || InsideContextMenu)
            {
                // Handle all clicks unless there's a possibility of a ContextMenu inside a Menu.
                e.Handled = true;
            }
        }

        private static void OnAccessKeyPressed(object sender, AccessKeyPressedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            bool isScope = false;

            if (e.Target == null)
            {
                // MenuItem access key should not work if something else beside MenuBase has capture
                if (Mouse.Captured == null || Mouse.Captured is MenuBase)
                {
                    e.Target = menuItem;

                    // special case is if we are the original source and our submenu is open,
                    // this is the case where the mouse moved over the header and focus is on
                    // the menu item but really you want to access key processing to be in your
                    // submenu.
                    // This assumes that no one will ever directly register a MenuItem with the AKM.
                    if (e.OriginalSource == menuItem && menuItem.IsSubmenuOpen)
                    {
                        isScope = true;
                    }
                }
                else
                {
                    e.Handled = true;
                }
            }
            else if (e.Scope == null)
            {
                // We want menu items to be a scope, but not for any AKs in its header.

                // If e.Target is already filled in, check if it's a MenuItem.
                // If it is and it's not us, we are its scope (i.e. we're the first MenuItem
                // above it in the chain).  If it's not a MenuItem, we have to take the long way.
                if (e.Target != menuItem && e.Target is MenuItem)
                {
                    isScope = true;
                }
                else
                {
                    // This case handles when you have some non-MenuItem in a menu that can be
                    // the target of access keys, like a Button.

                    // MenuItems are a scope for all access keys which are outside of themselves.
                    // e.Source is the logical element in which the event was raised.
                    // If we can walk from the source to ourselves, then we are not correct
                    // scope of this access key; some parent should be.

                    DependencyObject source = e.Source as DependencyObject;

                    while (source != null)
                    {
                        // If we walk up to this Menuitem, we are not the scope.
                        if (source == menuItem)
                        {
                            break;
                        }

                        UIElement uiElement = source as UIElement;

                        // If we walk up to an item which is one of our children, we are their scope.
                        if ((uiElement != null) && (ItemsControlFromItemContainer(uiElement) == menuItem))
                        {
                            isScope = true;
                            break;
                        }

                        source = GetFrameworkParent(source);
                    }
                }
            }

            if (isScope)
            {
                e.Scope = menuItem;
                e.Handled = true;
            }
        }

        /// <summary>
        ///     An event reporting the mouse entered or left this element.
        /// </summary>
        protected override void OnMouseLeave(MouseEventArgs  e)
        {
            base.OnMouseLeave(e);

            MenuItemRole role = Role;

            // When we're a top-level menuitem we have to check if the menu has capture.
            // If it doesn't we fall to the else below where we are just mousing around
            // the top-level menuitems.
            // (Note that Submenu items/headers do not have to look for capture.)
            if (((role == MenuItemRole.TopLevelHeader || role == MenuItemRole.TopLevelItem) && IsInMenuMode)
                || (role == MenuItemRole.SubmenuHeader || role == MenuItemRole.SubmenuItem))
            {
                MouseLeaveInMenuMode(role);
            }
            else
            {
                // Here we don't have capture and we're just mousing over
                // top-level menu items.  IsSelected should correspond to IsMouseOver.
                if (IsMouseOver != IsSelected)
                {
                    SetCurrentValueInternal(IsSelectedProperty, BooleanBoxes.Box(IsMouseOver));
                }
            }

            UpdateIsPressed();
        }

        /// <summary>
        /// This is the method that responds to the MouseEvent event.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            // Ignore any mouse moves on ourselves while the popup is opening.
            MenuItem parent = ItemsControl.ItemsControlFromItemContainer(this) as MenuItem;
            if (parent != null &&
                MenuItem.GetBoolField(parent, BoolField.MouseEnterOnMouseMove))
            {
                MenuItem.SetBoolField(parent, BoolField.MouseEnterOnMouseMove, false);
                MouseEnterHelper();
            }
        }

        /// <summary>
        ///     An event reporting the mouse entered or left this element.
        /// </summary>
        protected override void OnMouseEnter(MouseEventArgs  e)
        {
            base.OnMouseEnter(e);
            MouseEnterHelper();
        }

        private void MouseEnterHelper()
        {
            ItemsControl parent = ItemsControl.ItemsControlFromItemContainer(this);
            // Do not enter and highlight this item until the popup has opened
            // This prevents immediately selecting a submenu item when opening the menu
            // because the mouse was already where the menu item appeared
            if (parent == null || !MenuItem.GetBoolField(parent, BoolField.IgnoreMouseEvents))
            {
                MenuItemRole role = Role;

                // When we're a top-level menuitem we have to check if the menu has capture.
                // If it doesn't we fall to the else below where we are just mousing around
                // the top-level menuitems.
                // (Note that Submenu items/headers do not have to look for capture.)
                if (((role == MenuItemRole.TopLevelHeader || role == MenuItemRole.TopLevelItem) && OpenOnMouseEnter)
                    || (role == MenuItemRole.SubmenuHeader || role == MenuItemRole.SubmenuItem))
                {
                    MouseEnterInMenuMode(role);
                }
                else
                {
                    // Here we don't have capture and we're just mousing over
                    // top-level menu items.  IsSelected should correspond to IsMouseOver.
                    if (IsMouseOver != IsSelected)
                    {
                        SetCurrentValueInternal(IsSelectedProperty, BooleanBoxes.Box(IsMouseOver));
                    }
                }

                UpdateIsPressed();
            }
            else if (parent is MenuItem)
            {
                MenuItem.SetBoolField(parent, BoolField.MouseEnterOnMouseMove, true);
            }
        }

        private void MouseEnterInMenuMode(MenuItemRole role)
        {
            switch (role)
            {
                case MenuItemRole.TopLevelHeader:
                case MenuItemRole.TopLevelItem:
                    {
                        // When mousing over a top-level hierarchy, it should open immediately.
                        if (!IsSubmenuOpen)
                        {
                            OpenHierarchy(role);
                        }
                    }
                    break;

                case MenuItemRole.SubmenuHeader:
                case MenuItemRole.SubmenuItem:
                    {
                        // If the current sibling has an open hierarchy, we cannot
                        // move focus/selection immediately.  Instead we must set
                        // a timer to open after MenuShowDelay ms.  If the sibling has
                        // no hierarchy open, it is safe to select the item immediately.
                        MenuItem sibling = CurrentSibling;

                        if (sibling == null || !sibling.IsSubmenuOpen)
                        {
                            if (!IsSubmenuOpen)
                            {
                                // Try to focus/select this item.
                                FocusOrSelect();
                            }
                            else
                            {
                                // If the submenu is open, then it should already be selected.
                                Debug.Assert(IsSelected, "When IsSubmenuOpen = true, IsSelected should be true as well");

                                // Need to make sure that when we leave the hierarchy and come back
                                // that the item is highlighted.
                                IsHighlighted = true;
                            }
                        }
                        else
                        {
                            // Highlight this item and remove the highlight
                            // from its sibling selected MenuItem
                            sibling.IsHighlighted = false;
                            IsHighlighted = true;
                        }

                        // If the submenu isn't open already, OpenHierarchy after MenuShowDelay ms
                        if (!IsSelected || !IsSubmenuOpen)
                        {
                            // When the timout happens, OpenHierarchy will select this item
                            SetTimerToOpenHierarchy();
                        }
                    }
                    break;
            }


            // Now that we're over this menu hierarchy with the mouse, we
            // should stop any timers which might cause this hierarchy to close.
            StopTimer(ref _closeHierarchyTimer);
        }

        private void MouseLeaveInMenuMode(MenuItemRole role)
        {
            // When mouse moves out of a submenu item, we should deselect
            // the item.  This is what Win32 does, and our menus don't
            // feel right without it.
            if (role == MenuItemRole.SubmenuHeader || role == MenuItemRole.SubmenuItem)
            {
                if (MenuItem.GetBoolField(this, BoolField.IgnoreNextMouseLeave))
                {
                    // The mouse was within a submenu that closed. A submenu header is receiving this
                    // message, but we want to ignore this one.
                    MenuItem.SetBoolField(this, BoolField.IgnoreNextMouseLeave, false);
                }
                else
                {
                    if (!IsSubmenuOpen)
                    {
                        // When the submenu isn't open we can deselect the item right away.
                        if (IsSelected)
                        {
                            SetCurrentValueInternal(IsSelectedProperty, BooleanBoxes.FalseBox);
                        }
                        else
                        {
                            // If it's not selected it might just be highlighted,
                            // so remove the highlight.
                            IsHighlighted = false;
                        }

                        if (IsKeyboardFocusWithin)
                        {
                            ItemsControl parent = ItemsControl.ItemsControlFromItemContainer(this);
                            if (parent != null)
                            {
                                parent.Focus();
                            }
                        }
                    }
                    else
                    {
                        // If the submenu is open and the mouse moved to some sibling
                        // hierarchy, we need to delay and deselect the item after
                        // MenuShowDelay ms, as long as the item doesn't get re-selected.
                        if (IsMouseOverSibling)
                        {
                            SetTimerToCloseHierarchy();
                        }
                    }
                }
            }

            // No matter what, we've left the menu item and we should
            // stop any timer which would cause the item to open.
            StopTimer(ref _openHierarchyTimer);
        }

        /// <summary>
        ///     An event announcing that the keyboard is focused on this element.
        /// </summary>
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);

            // Focus drives selection.  If a MenuItem is focused, it should
            // select itself.
            if (!e.Handled && e.NewFocus == this)
            {
                SetCurrentValueInternal(IsSelectedProperty, BooleanBoxes.TrueBox);
            }
        }

        /// <summary>
        /// Called when the focus is no longer on or within this element.
        /// </summary>
        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);

            if (IsKeyboardFocusWithin && !IsSelected)
            {
                // If an item within us got focus (probably programatically), we need to become selected
                SetCurrentValueInternal(IsSelectedProperty, BooleanBoxes.TrueBox);
            }
        }

        /// <summary>
        ///     If control has a scrollviewer in its style and has a custom keyboard scrolling behavior when HandlesScrolling should return true.
        /// Then ScrollViewer will not handle keyboard input and leave it up to the control.
        /// </summary>
        protected internal override bool HandlesScrolling
        {
            get { return true; }
        }

        /// <summary>
        ///     This is the method that responds to the KeyDown event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            bool handled = false;

            Key key = e.Key;
            MenuItemRole role = Role;
            FlowDirection flowDirection = FlowDirection;

            // In Right to Left mode we switch Right and Left keys
            if (flowDirection == FlowDirection.RightToLeft)
            {
                if (key == Key.Right)
                {
                    key = Key.Left;
                }
                else if (key == Key.Left)
                {
                    key = Key.Right;
                }
            }

            switch (key)
            {
                case Key.Tab:
                    if (role == MenuItemRole.SubmenuHeader && IsSubmenuOpen && CurrentSelection == null)
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                        {
                            NavigateToEnd(new ItemNavigateArgs(e.Device, Keyboard.Modifiers));
                        }
                        else
                        {
                            NavigateToStart(new ItemNavigateArgs(e.Device, Keyboard.Modifiers));
                        }

                        handled = true;
                    }
                    break;

                case Key.Right:
                    if ((role == MenuItemRole.SubmenuHeader) && !IsSubmenuOpen)
                    {
                        OpenSubmenuWithKeyboard();
                        handled = true;
                    }
                    break;

                case Key.Enter:
                    {
                        if (((role == MenuItemRole.SubmenuItem) || (role == MenuItemRole.TopLevelItem)))
                        {
                            Debug.Assert(IsHighlighted, "MenuItem got Key.Enter but was not highlighted -- focus did not follow highlight?");
                            ClickItem(e.UserInitiated);
                            handled = true;
                        }
                        else if (role == MenuItemRole.TopLevelHeader)
                        {
                            // should this and the next one fire click events as well?
                            OpenSubmenuWithKeyboard();
                            handled = true;
                        }
                        else if (role == MenuItemRole.SubmenuHeader && !IsSubmenuOpen)
                        {
                            OpenSubmenuWithKeyboard();
                            handled = true;
                        }
                    }
                    break;

                // If a menuitem gets a down or up key and the submenu is open, we should focus the first or last
                // item in the submenu (respectively).  If the submenu is not opened, this will be handled by Menu.
                case Key.Down:
                    {
                        if (role == MenuItemRole.SubmenuHeader && IsSubmenuOpen && CurrentSelection == null)
                        {
                            NavigateToStart(new ItemNavigateArgs(e.Device, Keyboard.Modifiers));
                            handled = true;
                        }
                    }
                    break;

                case Key.Up:
                    {
                        if (role == MenuItemRole.SubmenuHeader && IsSubmenuOpen && CurrentSelection == null)
                        {
                            NavigateToEnd(new ItemNavigateArgs(e.Device, Keyboard.Modifiers));
                            handled = true;
                        }
                    }
                    break;

                case Key.Left:
                case Key.Escape:
                    {
                        // If Left or Escape is pressed on a Submenu Item or Header, the submenu should be closed.
                        // Closing the submenu will move focus out of the submenu and onto the parent MenuItem.
                        if ((role != MenuItemRole.TopLevelHeader) && (role != MenuItemRole.TopLevelItem))
                        {
                            if (IsSubmenuOpen)
                            {
                                SetCurrentValueInternal(IsSubmenuOpenProperty, BooleanBoxes.FalseBox);
                                handled = true;
                            }
                        }
                    }
                    break;
            }


            if (!handled)
            {
                ItemsControl parent = ItemsControl.ItemsControlFromItemContainer(this);
                /*
                 * This sets the ignore flag and adds a dispatcher that will run after all rendering has completed.
                 * parent can be null when this is in the visual tree but not in an ItemsList.  Not recomended but still possible.
                 * The IgnoreFlag could be set if multiple KeyPresses happen before the key ups.  There only needs to be one dispatcher
                 * on the queue.
                 * */
                if ((parent != null) && (!MenuItem.GetBoolField(parent, BoolField.IgnoreMouseEvents)))
                {
                    //Ignore Mouse Events
                    MenuItem.SetBoolField(parent, BoolField.IgnoreMouseEvents, true);

                    // MenuItem should ignore any mouse enter or move events until the menu has fully
                    // moved.  So this is added to the Dispatcher with Background
                    parent.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate(object param)
                    {
                        MenuItem.SetBoolField(parent, BoolField.IgnoreMouseEvents, false);
                        return null;
                    }), null);
                }

                // Use the unadulterated e.Key here because the later translation
                // to FocusNavigationDirection takes this into account.
                handled = MenuItemNavigate(e.Key, e.KeyboardDevice.Modifiers);
            }

            if (handled)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// The Access key for this control was invoked.
        /// </summary>
        protected override void OnAccessKey(AccessKeyEventArgs e)
        {
            base.OnAccessKey(e);

            if (!e.IsMultiple)
            {
                MenuItemRole type = Role;

                switch (type)
                {
                    case MenuItemRole.TopLevelItem:
                    case MenuItemRole.SubmenuItem:
                        {
                            ClickItem(e.UserInitiated);
                        }
                        break;

                    case MenuItemRole.TopLevelHeader :
                    case MenuItemRole.SubmenuHeader :
                        {
                            OpenSubmenuWithKeyboard();
                        }
                        break;
                }
            }
        }

        /// <summary>
        ///     This method is invoked when the Items property changes.
        /// </summary>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            // We use visual triggers to place the popup based on RoleProperty.
            // Update the RoleProperty when Items property changes so popup can be placed accordingly.
            UpdateRole();
            base.OnItemsChanged(e);
        }

        private object _currentItem;

        /// <summary>
        /// Return true if the item is (or is eligible to be) its own ItemUI
        /// </summary>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            bool ret = (item is MenuItem) || (item is Separator);
            if (!ret)
            {
                _currentItem = item;
            }

            return ret;
        }

        /// <summary>
        /// Determine whether the ItemContainerStyle/StyleSelector should apply to the item or not
        /// </summary>
        protected override bool ShouldApplyItemContainerStyle(DependencyObject container, object item)
        {
            if (item is Separator)
            {
                return false;
            }
            else
            {
                return base.ShouldApplyItemContainerStyle(container, item);
            }
        }

        /// <summary> Create or identify the element used to display the given item. </summary>
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
                    if (itemContainer is MenuItem || itemContainer is Separator)
                    {
                        return itemContainer as DependencyObject;
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Get(SRID.InvalidItemContainer, this.GetType().Name, typeof(MenuItem).Name, typeof(Separator).Name, itemContainer));
                    }
                }
            }

            return new MenuItem();
        }

        /// <summary>
        ///     Called when the parent of the Visual has changed.
        /// </summary>
        /// <param name="oldParent">Old parent or null if the Visual did not have a parent before.</param>
        protected internal override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            UpdateRole();

            // Windows OS bug:1988393; DevDiv bug:107459
            // MenuItem template contains ItemsPresenter where Grid.IsSharedSizeScope="true" and need to inherits PrivateSharedSizeScopeProperty value
            // Property inheritance walk the locial tree if possible and skip the visual tree where ItemsPresenter is
            // Workaround here will be to copy the property value from MenuItem visual parent

            DependencyObject newParent = VisualTreeHelper.GetParentInternal(this);

            // logical parent != null
            // visual parent != null
            // logical parent != visual parent <-- we are in the MenuItem is a logical child of a MenuItem case, not a data container case
            // --- Set one-way binding with visual parent for DefinitionBase.PrivateSharedSizeScopeProperty
            // NOTE: It seems impossible to get shared size scope to work in this hierarchical scenario
            // under normal conditions, so putting this binding here without respecting an author's desire for
            // shared size scope on the MenuItem container should be OK, since they wouldn't be able to
            // get it to work anyway.
            if (Parent != null && newParent != null && Parent != newParent)
            {
                Binding binding = new Binding();
                binding.Path = new PropertyPath(DefinitionBase.PrivateSharedSizeScopeProperty);
                binding.Mode = BindingMode.OneWay;
                binding.Source = newParent;
                BindingOperations.SetBinding(this, DefinitionBase.PrivateSharedSizeScopeProperty, binding);
            }

            // visual parent == null
            // --- Clear binding for DefinitionBase.PrivateSharedSizeScopeProperty
            if (newParent == null)
            {
                BindingOperations.ClearBinding(this, DefinitionBase.PrivateSharedSizeScopeProperty);
            }
        }

        /// <summary>
        /// Called when the Template's tree has been generated
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_submenuPopup != null)
            {
                _submenuPopup.Closed -= OnPopupClosed;
            }

            _submenuPopup = GetTemplateChild(PopupTemplateName) as Popup;

            if (_submenuPopup != null)
            {
                _submenuPopup.Closed += OnPopupClosed;
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private void SetMenuMode(bool menuMode)
        {
            Debug.Assert(Role == MenuItemRole.TopLevelHeader || Role == MenuItemRole.TopLevelItem, "MenuItem was not top-level");

            MenuBase parentMenu = LogicalParent as MenuBase;

            if (parentMenu != null)
            {
                if (parentMenu.IsMenuMode != menuMode)
                {
                    parentMenu.IsMenuMode = menuMode;
                }
            }
        }

        /// <summary>
        /// Returns true if the parent has capture.  Does not work for submenu items/headers.
        /// </summary>
        private bool IsInMenuMode
        {
            get
            {
                MenuBase parentMenu = LogicalParent as MenuBase;
                if (parentMenu != null)
                {
                    return parentMenu.IsMenuMode;
                }

                return false;
            }
        }

        /// <summary>
        /// Returns true if the top level header should open when the mouse enters it
        /// </summary>
        private bool OpenOnMouseEnter
        {
            get
            {
                MenuBase parentMenu = LogicalParent as MenuBase;
                if (parentMenu != null)
                {
                    Debug.Assert(!parentMenu.OpenOnMouseEnter || parentMenu.IsMenuMode, "OpenOnMouseEnter can only be true when IsMenuMode is true");
                    return parentMenu.OpenOnMouseEnter;
                }

                return false;
            }
        }

        // This is so that MenuItems inside a ContextMenu can behave differently
        internal static readonly DependencyProperty InsideContextMenuProperty
            = DependencyProperty.RegisterAttached("InsideContextMenu", typeof(bool), typeof(MenuItem),
                                          new FrameworkPropertyMetadata(BooleanBoxes.FalseBox, FrameworkPropertyMetadataOptions.Inherits));

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        private bool InsideContextMenu
        {
            get
            {
                return (bool)GetValue(InsideContextMenuProperty);
            }
        }

        internal static void SetInsideContextMenuProperty(UIElement element, bool value)
        {
            element.SetValue(InsideContextMenuProperty, BooleanBoxes.Box(value));
        }



        internal void ClickItem()
        {
            ClickItem(false);
        }
        private void ClickItem(bool userInitiated)
        {
            try
            {
                OnClickCore(userInitiated);
            }
            finally
            {
                // When you click a top-level item, we need to exit menu mode.
                if (Role == MenuItemRole.TopLevelItem && !StaysOpenOnClick)
                {
                    SetMenuMode(false);
                }
            }
        }

        internal void ClickHeader()
        {
            if (!IsKeyboardFocusWithin)
            {
                FocusOrSelect();
            }

            if (IsSubmenuOpen)
            {
                if (Role == MenuItemRole.TopLevelHeader)
                {
                    SetMenuMode(false);
                }
            }
            else
            {
                // Immediately open the menu when it's clicked. This will stop any
                // timers to open or close the submenu.
                OpenMenu();
            }
        }

        internal bool OpenMenu()
        {
            if (!IsSubmenuOpen)
            {
                // Verify that the parent of the MenuItem is valid;
                ItemsControl owner = ItemsControl.ItemsControlFromItemContainer(this);
                if (owner == null)
                {
                    owner = VisualTreeHelper.GetParent(this) as ItemsControl;
                }

                if ((owner != null) && ((owner is MenuItem) || (owner is MenuBase)))
                {
                    // Parent must be MenuItem or MenuBase in order for menus to open.
                    // Otherwise, odd behavior will occur.
                    SetCurrentValueInternal(IsSubmenuOpenProperty, BooleanBoxes.TrueBox);
                    return true; // The value was actually changed
                }
            }

            return false;
        }

        /// <summary>
        ///     Set IsSubmenuOpen = true and select the first item.
        /// </summary>
        internal void OpenSubmenuWithKeyboard()
        {
            MenuItem.SetBoolField(this, BoolField.OpenedWithKeyboard, true);
            if (OpenMenu())
            {
                NavigateToStart(new ItemNavigateArgs(Keyboard.PrimaryDevice, Keyboard.Modifiers));
            }
        }

        /// <summary>
        /// Navigate from one MenuItem to a sibling.
        /// </summary>
        /// <param name="key">Raw key that was pressed (RTL is respected within this method).</param>
        /// <param name="modifiers"></param>
        /// <returns>true if navigation was successful.</returns>
        private bool MenuItemNavigate(Key key, ModifierKeys modifiers)
        {
            if (key == Key.Left || key == Key.Right || key == Key.Up || key == Key.Down)
            {
                ItemsControl parent = ItemsControlFromItemContainer(this);
                if (parent != null)
                {
                    if (!parent.HasItems)
                    {
                        return false;
                    }

                    int count = parent.Items.Count;

                    // Optimize for the case where the submenu contains one item.

                    if (count == 1 && !(parent is Menu))
                    {
                        // Return true if we were navigating up/down (we cycled around).
                        if (key == Key.Up && key == Key.Down)
                        {
                            return true;
                        }
                    }

                    object previousFocus = Keyboard.FocusedElement;
                    parent.NavigateByLine(parent.FocusedInfo, KeyboardNavigation.KeyToTraversalDirection(key), new ItemNavigateArgs(Keyboard.PrimaryDevice, modifiers));
                    object currentFocus = Keyboard.FocusedElement;
                    if ((currentFocus != previousFocus) && (currentFocus != this))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Returns logical parent; either Parent or ItemsControlFromItemContainer(this).
        /// </summary>
        /// <value></value>
        internal object LogicalParent
        {
            get
            {
                if (Parent != null)
                {
                    return Parent;
                }

                return ItemsControlFromItemContainer(this);
            }
        }

        /// <summary>
        ///     Return the current sibling of this MenuItem -- the
        ///     CurrentSelection of the parent as long as it isn't us.
        /// </summary>
        private MenuItem CurrentSibling
        {
            get
            {
                object parent = LogicalParent;
                MenuItem menuItemParent = parent as MenuItem;
                MenuItem sibling = null;

                if (menuItemParent != null)
                {
                    sibling = menuItemParent.CurrentSelection;
                }
                else
                {
                    MenuBase menuParent = parent as MenuBase;

                    if (menuParent != null)
                    {
                        sibling = menuParent.CurrentSelection;
                    }
                }

                if (sibling == this)
                {
                    sibling = null;
                }

                return sibling;
            }
        }

        /// <summary>
        ///     Returns true if the mouse is somewhere in the hierarchy
        ///     but not over this node.  Note that this is slightly different
        ///     from CurrentSibling.IsMouseOver because there are regions in
        ///     the menu which are not occupied by siblings and we're interested
        ///     in that case too.
        /// </summary>
        private bool IsMouseOverSibling
        {
            get
            {
                FrameworkElement parent = LogicalParent as FrameworkElement;

                // If the mouse is over our parent but not over us, then
                // the mouse must be somewhere in a sibling hierarchy.
                //
                // NOTE: If this check were changed to CurrentSibling.IsMouseOver
                //       then our behavior becomes identical to the behavior
                //       of the start menu, where a menu doesn't close unless
                //       you have settled on another hierarchy.  Here we will
                //       close unless you are settled on this item's hierarchy.
                if (parent != null && IsMouseReallyOver(parent) && !IsMouseOver)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        ///     Performs an IsMouseOver test but accounts for elements that have capture
        ///     and instead checks their children.
        /// </summary>
        /// <param name="elem">The element to test.</param>
        /// <returns>True if the mouse is over the element, regardless of capture. False otherwise.</returns>
        private static bool IsMouseReallyOver(FrameworkElement elem)
        {
            bool isMouseOver = elem.IsMouseOver;

            if (isMouseOver)
            {
                if ((Mouse.Captured == elem) && (Mouse.DirectlyOver == elem))
                {
                    // The mouse is not over any of the children of this captured element.
                    // Assuming that this means that the mouse is not really over the element.
                    return false;
                }
            }

            return isMouseOver;
        }

        /// <summary>
        ///     Select this item and expand the hierarchy below it.
        /// </summary>
        /// <param name="role"></param>
        private void OpenHierarchy(MenuItemRole role)
        {
            FocusOrSelect();

            if (role == MenuItemRole.TopLevelHeader || role == MenuItemRole.SubmenuHeader)
            {
                OpenMenu();
            }
        }

        /// <summary>
        ///     Focus this item or, if that fails, just mark it selected.
        /// </summary>
        private void FocusOrSelect()
        {
            // Setting focus will cause the item to be selected,
            // but if we fail to focus we should still select.
            // (This is to help enable focusless menus).
            // Check IsKeyboardFocusWithin to allow rich content within the menuitem.
            if (!IsKeyboardFocusWithin
                // but only aquire focus the window we are inside is currently active
                // otherwise we would potentially steal focus from other applications.
                && Window.GetWindow(this)?.IsActive == true)
            {
                Focus();
            }

            if (!IsSelected)
            {
                // If it's already focused, make sure it's also selected.
                SetCurrentValueInternal(IsSelectedProperty, BooleanBoxes.TrueBox);
            }

            // If the item is selected we should ensure that it's highlighted.
            if (IsSelected && !IsHighlighted)
            {
                IsHighlighted = true;
            }
        }

        private void SetTimerToOpenHierarchy()
        {
            if (_openHierarchyTimer == null)
            {
                _openHierarchyTimer = new DispatcherTimer(DispatcherPriority.Normal);
                _openHierarchyTimer.Tick += (EventHandler)delegate(object sender, EventArgs e)
                {
                    OpenHierarchy(Role);
                    StopTimer(ref _openHierarchyTimer);
                };
            }
            else
            {
                _openHierarchyTimer.Stop();
            }

            StartTimer(_openHierarchyTimer);
        }

        private void SetTimerToCloseHierarchy()
        {
            if (_closeHierarchyTimer == null)
            {
                _closeHierarchyTimer = new DispatcherTimer(DispatcherPriority.Normal);
                _closeHierarchyTimer.Tick += (EventHandler)delegate(object sender, EventArgs e)
                {
                    // Deselect the item; will remove highlight and collapse hierarchy.
                    SetCurrentValueInternal(IsSelectedProperty, BooleanBoxes.FalseBox);
                    StopTimer(ref _closeHierarchyTimer);
                };
            }
            else
            {
                _closeHierarchyTimer.Stop();
            }

            StartTimer(_closeHierarchyTimer);
        }

        private void StopTimer(ref DispatcherTimer timer)
        {
            if (timer != null)
            {
                timer.Stop();
                timer = null;
            }
        }

        private void StartTimer(DispatcherTimer timer)
        {
            Debug.Assert(timer != null, "timer should not be null.");
            Debug.Assert(!timer.IsEnabled, "timer should not be running.");

            timer.Interval = TimeSpan.FromMilliseconds(SystemParameters.MenuShowDelay);
            timer.Start();
        }

        private static object OnCoerceAcceleratorKey(DependencyObject d, object value)
        {
            if (value == null)
            {
                string inputGestureText = ((MenuItem)d).InputGestureText;
                if (inputGestureText != String.Empty)
                {
                    value = inputGestureText;
                }
            }

            return value;
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        ///     Tracks the current selection in the items collection (i.e. submenu)
        ///     of this MenuItem.
        /// </summary>
        private MenuItem CurrentSelection
        {
            get
            {
                return _currentSelection;
            }

            set
            {
                if (_currentSelection != null)
                {
                    _currentSelection.SetCurrentValueInternal(IsSelectedProperty, BooleanBoxes.FalseBox);
                }

                _currentSelection = value;

                if (_currentSelection != null)
                {
                    _currentSelection.SetCurrentValueInternal(IsSelectedProperty, BooleanBoxes.TrueBox);
                }

                // NOTE: (Win32 disparity) If CurrentSelection changes to null
                //       and the focus was within the old CurrentSelection, we
                //       the parent should take focus back.  In Win32 the "virtual"
                //       focus was tracked by way of the currently selected guy in
                //       If you were selected but none of your children were, you
                //       were effectively selected.  It should be relatively easy to
                //       enable this behavior by checking if IsKeyboardFocusWithin is true
                //       on the previous child and then setting Focus to ourselves
                //       when _currentSelection becomes null.  We would need to do this
                //       here and in MenuBase.CurrentSelection.
            }
        }

        private static readonly DependencyProperty BooleanFieldStoreProperty = DependencyProperty.RegisterAttached(
            "BooleanFieldStore",
            typeof(BoolField),
            typeof(MenuItem),
            new FrameworkPropertyMetadata(new BoolField())
            );

        private static bool GetBoolField(UIElement element, BoolField field)
        {
            return (((BoolField)element.GetValue(BooleanFieldStoreProperty)) & field) != 0;
        }

        private static void SetBoolField(UIElement element, BoolField field, bool value)
        {
            if (value)
            {
                element.SetValue(BooleanFieldStoreProperty, ((BoolField)element.GetValue(BooleanFieldStoreProperty)) | field);
            }
            else
            {
                element.SetValue(BooleanFieldStoreProperty, ((BoolField)element.GetValue(BooleanFieldStoreProperty)) & (~field));
            }
        }

        [Flags]
        private enum BoolField
        {
            OpenedWithKeyboard = 0x01,
            IgnoreNextMouseLeave = 0x02,
            IgnoreMouseEvents = 0x04,
            MouseEnterOnMouseMove = 0x08,
            CanExecuteInvalid = 0x10,
        }

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 42; }
        }

        private bool CanExecute
        {
            get { return !ReadControlFlag(ControlBoolFlags.CommandDisabled); }
            set
            {
                if (value != CanExecute)
                {
                    WriteControlFlag(ControlBoolFlags.CommandDisabled, !value);
                    CoerceValue(IsEnabledProperty);
                }
            }
        }

        private const string PopupTemplateName = "PART_Popup";

        private MenuItem _currentSelection;
        private Popup _submenuPopup;

        DispatcherTimer _openHierarchyTimer;
        DispatcherTimer _closeHierarchyTimer;

        private bool _userInitiatedPress;
        #endregion

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey

        #region ItemsStyleKey
        /// <summary>
        ///     Resource Key for the SeparatorStyle
        /// </summary>
        public static ResourceKey SeparatorStyleKey
        {
            get
            {
                return SystemResourceKey.MenuItemSeparatorStyleKey;
            }
        }

        #endregion ItemsStyleKey
    }
}

