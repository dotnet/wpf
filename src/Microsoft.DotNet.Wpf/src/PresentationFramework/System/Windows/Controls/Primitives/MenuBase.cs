// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Utility;
using System.ComponentModel;
using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Input;
using System.Security;
using System;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     Control that defines a menu of choices for users to invoke.
    /// </summary>
    [Localizability(LocalizationCategory.Menu)]
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(MenuItem))]
    public abstract class MenuBase : ItemsControl
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Default DependencyObject constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        protected MenuBase()
            : base()
        {
        }

        static MenuBase()
        {
            EventManager.RegisterClassHandler(typeof(MenuBase), MenuItem.PreviewClickEvent, new RoutedEventHandler(OnMenuItemPreviewClick));
            EventManager.RegisterClassHandler(typeof(MenuBase), Mouse.MouseDownEvent, new MouseButtonEventHandler(OnMouseButtonDown));
            EventManager.RegisterClassHandler(typeof(MenuBase), Mouse.MouseUpEvent, new MouseButtonEventHandler(OnMouseButtonUp));
            EventManager.RegisterClassHandler(typeof(MenuBase), Mouse.LostMouseCaptureEvent, new MouseEventHandler(OnLostMouseCapture));
            EventManager.RegisterClassHandler(typeof(MenuBase), MenuBase.IsSelectedChangedEvent, new RoutedPropertyChangedEventHandler<bool>(OnIsSelectedChanged));

            EventManager.RegisterClassHandler(typeof(MenuBase), Mouse.MouseDownEvent, new MouseButtonEventHandler(OnPromotedMouseButton));
            EventManager.RegisterClassHandler(typeof(MenuBase), Mouse.MouseUpEvent, new MouseButtonEventHandler(OnPromotedMouseButton));

            EventManager.RegisterClassHandler(typeof(MenuBase), Mouse.PreviewMouseDownOutsideCapturedElementEvent, new MouseButtonEventHandler(OnClickThroughThunk));
            EventManager.RegisterClassHandler(typeof(MenuBase), Mouse.PreviewMouseUpOutsideCapturedElementEvent, new MouseButtonEventHandler(OnClickThroughThunk));

            EventManager.RegisterClassHandler(typeof(MenuBase), Keyboard.PreviewKeyboardInputProviderAcquireFocusEvent, new KeyboardInputProviderAcquireFocusEventHandler(OnPreviewKeyboardInputProviderAcquireFocus), true);
            EventManager.RegisterClassHandler(typeof(MenuBase), Keyboard.KeyboardInputProviderAcquireFocusEvent, new KeyboardInputProviderAcquireFocusEventHandler(OnKeyboardInputProviderAcquireFocus), true);

            FocusManager.IsFocusScopeProperty.OverrideMetadata(typeof(MenuBase), new FrameworkPropertyMetadata(BooleanBoxes.TrueBox));

            // While the menu is opened, Input Method should be suspended.
            // the docusmen focus of Cicero should not be changed but key typing should not be
            // dispatched to IME/TIP.
            InputMethod.IsInputMethodSuspendedProperty.OverrideMetadata(typeof(MenuBase), new FrameworkPropertyMetadata(BooleanBoxes.TrueBox, FrameworkPropertyMetadataOptions.Inherits));
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     DependencyProperty for ItemContainerTemplateSelector property.
        /// </summary>
        public static readonly DependencyProperty ItemContainerTemplateSelectorProperty =
            DependencyProperty.Register(
                "ItemContainerTemplateSelector",
                typeof(ItemContainerTemplateSelector),
                typeof(MenuBase),
                new FrameworkPropertyMetadata(new DefaultItemContainerTemplateSelector()));

        /// <summary>
        ///     ItemContainerTemplateSelector property which provides the DataTemplate to be used to create an instance of the ItemContainer.
        /// </summary>
        public ItemContainerTemplateSelector ItemContainerTemplateSelector
        {
            get { return (ItemContainerTemplateSelector)GetValue(ItemContainerTemplateSelectorProperty); }
            set { SetValue(ItemContainerTemplateSelectorProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for UsesItemContainerTemplateSelector property.
        /// </summary>
        public static readonly DependencyProperty UsesItemContainerTemplateProperty =
            DependencyProperty.Register(
                "UsesItemContainerTemplate",
                typeof(bool),
                typeof(MenuBase));

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
        ///     Called when any mouse button is pressed on this subtree
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private static void OnMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((MenuBase)sender).HandleMouseButton(e);
        }

        /// <summary>
        ///     Called when any mouse right button is released on this subtree
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private static void OnMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            ((MenuBase)sender).HandleMouseButton(e);
        }

        /// <summary>
        ///     Called when any mouse button is pressed or released on this subtree
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void HandleMouseButton(MouseButtonEventArgs e)
        {
        }

        private static void OnClickThroughThunk(object sender, MouseButtonEventArgs e)
        {
            ((MenuBase)sender).OnClickThrough(e);
        }

        private void OnClickThrough(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right)
            {
                if (HasCapture)
                {
                    bool close = true;

                    if (e.ButtonState == MouseButtonState.Released)
                    {
                        // Check to see if we should ignore the this mouse release
                        if (e.ChangedButton == MouseButton.Left && IgnoreNextLeftRelease)
                        {
                            IgnoreNextLeftRelease = false;
                            close = false; // don't close
                        }
                        else if (e.ChangedButton == MouseButton.Right && IgnoreNextRightRelease)
                        {
                            IgnoreNextRightRelease = false;
                            close = false; // don't close
                        }
                    }

                    if (close)
                    {
                        IsMenuMode = false;
                    }
                }
            }
        }

        // This is called on MouseLeftButtonDown, MouseLeftButtonUp, MouseRightButtonDown, MouseRightButtonUp
        private static void OnPromotedMouseButton(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // If it wasn't outside the subtree, we should handle the mouse event.
                // This makes things consistent so that just in case one of our children
                // didn't handle the event, it doesn't escape the menu hierarchy.
                e.Handled = true;
            }
        }

        /// <summary>
        ///     Called when IsMouseOver changes on this element.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            // if we don't have capture and the mouse left (but the item isn't selected), then we shouldn't have anything selected.
            if (!HasCapture && !IsMouseOver && CurrentSelection != null && !CurrentSelection.IsKeyboardFocused && !CurrentSelection.IsSubmenuOpen)
            {
                CurrentSelection = null;
            }
        }

        // This method ensures that whenever focus is given to an element
        // within a menu, that the menu enters menu mode.  This can't be
        // done with a simple IsFocusWithin changed handler because we
        // need to actually enter menu mode before focus changes.
        private static void OnPreviewKeyboardInputProviderAcquireFocus(object sender, KeyboardInputProviderAcquireFocusEventArgs e)
        {
            MenuBase menu = (MenuBase) sender;

            // If we haven't already pushed menu mode, we need to do it before
            // focus enters the menu for the first time
            if (!menu.IsKeyboardFocusWithin && !menu.HasPushedMenuMode)
            {
                // Call PushMenuMode just before focus enters the menu...
                menu.PushMenuMode(/*isAcquireFocusMenuMode*/ true);
            }
        }

        // This method ensures that whenever focus is not acquired
        // but MenuMode has been pushed with the expection, a
        // corresponding PopMenu is performed.
        private static void OnKeyboardInputProviderAcquireFocus(object sender, KeyboardInputProviderAcquireFocusEventArgs e)
        {
            MenuBase menu = (MenuBase) sender;
            if (!menu.IsKeyboardFocusWithin && !e.FocusAcquired && menu.IsAcquireFocusMenuMode)
            {
                Debug.Assert(menu.HasPushedMenuMode);
                // The input provider did not acquire focus.  So we will not
                // succeed in setting focus to the desired element within the
                // menu.
                menu.PopMenuMode();
            }
        }


        /// <summary>
        /// Called when the focus is no longer on or within this element.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);

            if (IsKeyboardFocusWithin)
            {
                // When focus enters the menu, we should enter menu mode.
                if (!IsMenuMode)
                {
                    IsMenuMode = true;
                    OpenOnMouseEnter = false;
                }

                if (KeyboardNavigation.IsKeyboardMostRecentInputDevice())
                {
                    // Turn on keyboard cues b/c we took focus with the keyboard
                    KeyboardNavigation.EnableKeyboardCues(this, true);
                }
            }
            else
            {
                // Turn off keyboard cues
                KeyboardNavigation.EnableKeyboardCues(this, false);

                if (IsMenuMode)
                {
                    // When showing a ContextMenu of a MenuItem, the ContextMenu will take focus
                    // out of this menu's subtree.  The ContextMenu takes capture before taking
                    // focus, so if we are in MenuMode but don't have capture then we are waiting
                    // for the context menu to close.  Thus, we should only exit menu mode when
                    // we have capture.
                    if (HasCapture)
                    {
                        IsMenuMode = false;
                    }
                }
                else
                {
                    // Okay, we weren't in menu mode but we could have had a selection (mouse hovering), so clear that
                    if (CurrentSelection != null)
                    {
                        CurrentSelection = null;
                    }
                }
            }

            InvokeMenuOpenedClosedAutomationEvent(IsKeyboardFocusWithin);
        }

        private void InvokeMenuOpenedClosedAutomationEvent(bool open)
        {
            AutomationEvents automationEvent = open ? AutomationEvents.MenuOpened : AutomationEvents.MenuClosed;

            if (AutomationPeer.ListenerExists(automationEvent))
            {
                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(this);
                if (peer != null)
                {
                    if (open)
                    {
                        // We raise the event async to allow PopupRoot to hookup
                        Dispatcher.BeginInvoke(DispatcherPriority.Input, new DispatcherOperationCallback(delegate(object param)
                        {
                            peer.RaiseAutomationEvent(automationEvent);
                            return null;
                        }), null);
                    }
                    else
                    {
                        peer.RaiseAutomationEvent(automationEvent);
                    }
                }
            }
        }

        internal static readonly RoutedEvent IsSelectedChangedEvent = EventManager.RegisterRoutedEvent(
            "IsSelectedChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(MenuBase));

        private static void OnIsSelectedChanged(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            // We assume that within a menu the only top-level menu items are direct children of
            // the one and only top-level menu.
            MenuItem newSelectedMenuItem = e.OriginalSource as MenuItem;

            if (newSelectedMenuItem != null)
            {
                MenuBase menu = (MenuBase)sender;

                // If the selected item is a child of ours, make it the current selection.
                // If the selection changes from a top-level menu item with its submenu
                // open to another, the new selection's submenu should be open.
                if (e.NewValue)
                {
                    if ((menu.CurrentSelection != newSelectedMenuItem) && (newSelectedMenuItem.LogicalParent == menu))
                    {
                        bool wasSubmenuOpen = false;

                        if (menu.CurrentSelection != null)
                        {
                            wasSubmenuOpen = menu.CurrentSelection.IsSubmenuOpen;
                            menu.CurrentSelection.SetCurrentValueInternal(MenuItem.IsSubmenuOpenProperty, BooleanBoxes.FalseBox);
                        }

                        menu.CurrentSelection = newSelectedMenuItem;
                        if (menu.CurrentSelection != null && wasSubmenuOpen)
                        {
                            // Only open the submenu if it's a header (i.e. has items)
                            MenuItemRole role = menu.CurrentSelection.Role;

                            if (role == MenuItemRole.SubmenuHeader || role == MenuItemRole.TopLevelHeader)
                            {
                                if (menu.CurrentSelection.IsSubmenuOpen != wasSubmenuOpen)
                                {
                                    menu.CurrentSelection.SetCurrentValueInternal(MenuItem.IsSubmenuOpenProperty, BooleanBoxes.Box(wasSubmenuOpen));
                                }
                            }
                        }
                    }
                }
                else
                {
                    // As in MenuItem.OnIsSelectedChanged, if the item is deselected
                    // and it's our current selection, set CurrentSelection to null.
                    if (menu.CurrentSelection == newSelectedMenuItem)
                    {
                        menu.CurrentSelection = null;
                    }
                }

                e.Handled = true;
            }
        }

        private bool IsDescendant(DependencyObject node)
        {
            return IsDescendant(this, node);
        }

        internal static bool IsDescendant(DependencyObject reference, DependencyObject node)
        {
            bool success = false;

            DependencyObject curr = node;

            while (curr != null)
            {
                if (curr == reference)
                {
                    success = true;
                    break;
                }

                // Find popup if curr is a PopupRoot
                PopupRoot popupRoot = curr as PopupRoot;
                if (popupRoot != null)
                {
                    //Now Popup does not have a visual link to its parent (for context menu)
                    //it is stored in its parent's arraylist (DP)
                    //so we get its parent by looking at PlacementTarget
                    Popup popup = popupRoot.Parent as Popup;

                    curr = popup;

                    if (popup != null)
                    {
                        // Try the poup Parent
                        curr = popup.Parent;

                        // Otherwise fall back to placement target
                        if (curr == null)
                        {
                            curr = popup.PlacementTarget;
                        }
                    }
                }
                else // Otherwise walk tree
                {
                    curr = PopupControlService.FindParent(curr);
                }
            }

            return success;
        }

        /// <summary>
        ///     This is the method that responds to the KeyDown event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            Key key = e.Key;
            switch (key)
            {
                case Key.Escape:
                    {
                        if (CurrentSelection != null && CurrentSelection.IsSubmenuOpen)
                        {
                            CurrentSelection.SetCurrentValueInternal(MenuItem.IsSubmenuOpenProperty, BooleanBoxes.FalseBox);
                            OpenOnMouseEnter = false;
                            e.Handled = true;
                        }
                        else
                        {
                            KeyboardLeaveMenuMode();

                            e.Handled = true;
                        }
                    }
                    break;

                case Key.System:
                    if ((e.SystemKey == Key.LeftAlt) ||
                        (e.SystemKey == Key.RightAlt) ||
                        (e.SystemKey == Key.F10))
                    {
                        KeyboardLeaveMenuMode();

                        e.Handled = true;
                    }
                    break;
            }
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

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        ///     Called when this element loses capture.
        /// </summary>
        private static void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            MenuBase menu = sender as MenuBase;

            // need a better solution for subcapture!

            // Use the same technique employed in ComoboBox.OnLostMouseCapture to allow another control in the
            // application to temporarily take capture and then take it back afterwards.

            if (Mouse.Captured != menu)
            {
                if (e.OriginalSource == menu)
                {
                    // If capture is null or it's not below the menu, close.
                    // More workaround for task 22022 -- check if it's a descendant (following Logical links too)
                    if (Mouse.Captured == null || !MenuBase.IsDescendant(menu, Mouse.Captured as DependencyObject))
                    {
                        menu.IsMenuMode = false;
                    }
                }
                else
                {
                    if (MenuBase.IsDescendant(menu, e.OriginalSource as DependencyObject))
                    {
                        // Take capture if one of our children gave up capture
                        if (menu.IsMenuMode && Mouse.Captured == null && MS.Win32.SafeNativeMethods.GetCapture() == IntPtr.Zero)
                        {
                            Mouse.Capture(menu, CaptureMode.SubTree);
                            e.Handled = true;
                        }
                    }
                    else
                    {
                        menu.IsMenuMode = false;
                    }
                }
            }
        }

        /// <summary>
        ///     Called when any menu item within this subtree got clicked.
        ///     Closes all submenus in this tree.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnMenuItemPreviewClick(object sender, RoutedEventArgs e)
        {
            MenuBase menu = ((MenuBase)sender);

            MenuItem menuItemSource = e.OriginalSource as MenuItem;

            if ((menuItemSource != null) && !menuItemSource.StaysOpenOnClick)
            {
                MenuItemRole role = menuItemSource.Role;

                if (role == MenuItemRole.TopLevelItem || role == MenuItemRole.SubmenuItem)
                {
                    menu.IsMenuMode = false;
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        ///     Called when IsMenuMode changes.
        /// </summary>
        internal event EventHandler InternalMenuModeChanged
        {
            add { EventHandlersStoreAdd(InternalMenuModeChangedKey, value); }
            remove { EventHandlersStoreRemove(InternalMenuModeChangedKey, value); }
        }

        private static readonly EventPrivateKey InternalMenuModeChangedKey = new EventPrivateKey();

        private void RestorePreviousFocus()
        {
            // Only restore focus if focus is still within the menu.  If
            // focus has already been moved outside of the menu, then
            // we don't want to disturb it.
            if (IsKeyboardFocusWithin)
            {
                // Only restore WPF focus if the HWND with focus is an
                // HwndSource.  This enables child HWNDs, other top-level
                // non-WPF HWNDs, or even child HWNDs of other WPF top-level
                // windows to retain focus when menus are dismissed.
                IntPtr hwndWithFocus = MS.Win32.UnsafeNativeMethods.GetFocus();
                HwndSource hwndSourceWithFocus = hwndWithFocus != IntPtr.Zero ? HwndSource.CriticalFromHwnd(hwndWithFocus) : null;
                if(hwndSourceWithFocus != null)
                {
                    // We restore focus by setting focus to the parent's focus
                    // scope.  This may not seem correct, because it presumes
                    // the focus came from the logical-focus element of the
                    // parent scope.  In fact, it could have come from any
                    // number of places.  However, we have not figured out a
                    // better solution for restoring focus across scenarios
                    // such as:
                    //
                    // 1) A context menu of a menu item.
                    // 2) Two menus side-by-side
                    // 3) A menu and a toolbar side-by-side
                    //
                    // Simply remembering the last element with focus and
                    // restoring focus to it does not work.  For example,
                    // two menus side-by-side will end up remembering each
                    // other, and you can get stuck in an infinite loop.
                    //
                    // Restoring focus through the parent's focus scope will
                    // not directly work if you open one window's menu from
                    // another window. Visual Studio, as an example, will
                    // intercept the focus change events and forward
                    // appropriately for the scenario of restoring focus to
                    // an element in a different top-level window.

					// DependencyObject parent = Parent;
                    // if (parent == null)
                    // {
                        // If there is no logical parent, use the visual parent.
                    //     parent = VisualTreeHelper.GetParent(this);
                    // }

                    // if (parent != null)
                    // {
                    //     IInputElement parentScope = FocusManager.GetFocusScope(parent) as IInputElement;
                    //     if (parentScope != null)
                    //     {
                    //         Keyboard.Focus(parentScope);
                    //     }
                    // }

					// Unfortunately setting focus to the parent focusscope tripped up VS in the scenario where 
					// Menus are contained within ToolBars. In this case when the Menu is dismissed they want 
					// focus to be restored to the element in the main window that previously had focus. However 
					// since ToolBar is  the parent focusscope for the Menu we end up restoring focus to its 
					// focusedelment. It is also noted that this implementation is a behavioral change from .Net 3.5. 
					// Hence we are putting back the old behavior which is to set Keyboard.Focus to null which will 
					// delegate focus through the main window to its focusedelement. 

					Keyboard.Focus(null);
                }
                else
                {
                    // In the case where Win32 focus is not on a WPF
                    // HwndSource, we just clear WPF focus completely.
                    //
                    // Note that calling Focus(null) will set focus to the root
                    // element of the active source, which is not what we want.
                    Keyboard.ClearFocus();
                }
            }
        }

        // From all of our children, set the InMenuMode property
        // If turning this property off, recurse to all submenus
        internal static void SetSuspendingPopupAnimation(ItemsControl menu, MenuItem ignore, bool suspend)
        {
            // menu can be either a MenuBase or MenuItem
            if (menu != null)
            {
                int itemsCount = menu.Items.Count;

                for (int i = 0; i < itemsCount; i++)
                {
                    MenuItem mi = menu.ItemContainerGenerator.ContainerFromIndex(i) as MenuItem;

                    if (mi != null && mi != ignore && mi.IsSuspendingPopupAnimation != suspend)
                    {
                        mi.IsSuspendingPopupAnimation = suspend;

                        // If leaving menu mode, clear property on all
                        // submenus of this menu
                        if (!suspend)
                        {
                            SetSuspendingPopupAnimation(mi, null, suspend);
                        }
                    }
                }
            }
        }

        internal void KeyboardLeaveMenuMode()
        {
            // If we're in MenuMode, exit.  This will relinquish capture,
            // clear CurrentSelection, and RestorePreviousFocus
            if (IsMenuMode)
            {
                IsMenuMode = false;
            }
            else
            {
                // (IsFocusWithin flickers when moving across Popup boundaries)
                // Consider guaranteeing that IsKeyboardFocusWithin -> Mouse.Captured == this.
                // Today we can just guarantee that if a submenu is open, then we have capture.
                //
                // We can't take capture as long as we can't guarantee
                // that focus is always within the menu.
                // We should still return focus to the previously focused element.
                CurrentSelection = null;
                RestorePreviousFocus();
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        ///     Currently selected item in this menu or submenu.
        /// </summary>
        /// <value></value>
        internal MenuItem CurrentSelection
        {
            get
            {
                return _currentSelection;
            }
            set
            {
                // Even if we don't have capture we should move focus when one item is already focused.
                bool wasFocused = false;

                if (_currentSelection != null)
                {
                    wasFocused = _currentSelection.IsKeyboardFocused;
                    _currentSelection.SetCurrentValueInternal(MenuItem.IsSelectedProperty, BooleanBoxes.FalseBox);
                }

                _currentSelection = value;
                if (_currentSelection != null)
                {
                    _currentSelection.SetCurrentValueInternal(MenuItem.IsSelectedProperty, BooleanBoxes.TrueBox);
                    if (wasFocused)
                    {
                        _currentSelection.Focus();
                    }
                }
            }
        }

        internal bool HasCapture
        {
            get
            {
                return Mouse.Captured == this;
            }
        }

        internal bool IgnoreNextLeftRelease
        {
            get { return _bitFlags[(int)MenuBaseFlags.IgnoreNextLeftRelease]; }
            set { _bitFlags[(int)MenuBaseFlags.IgnoreNextLeftRelease] = value; }
        }

        internal bool IgnoreNextRightRelease
        {
            get { return _bitFlags[(int)MenuBaseFlags.IgnoreNextRightRelease]; }
            set { _bitFlags[(int)MenuBaseFlags.IgnoreNextRightRelease] = value; }
        }

        internal bool IsMenuMode
        {
            get
            {
                return _bitFlags[(int)MenuBaseFlags.IsMenuMode];
            }

            set
            {
                Debug.Assert(CheckAccess(), "IsMenuMode requires context access");
                bool isMenuMode = _bitFlags[(int)MenuBaseFlags.IsMenuMode];
                if (isMenuMode != value)
                {
                    isMenuMode = _bitFlags[(int)MenuBaseFlags.IsMenuMode] = value;

                    if (isMenuMode)
                    {
                        // Take capture so that all mouse messages stay below the menu.
                        if (!IsDescendant(this, Mouse.Captured as Visual) && !Mouse.Capture(this, CaptureMode.SubTree))
                        {
                            // If we're unable to take capture, leave menu mode immediately.
                            isMenuMode = _bitFlags[(int)MenuBaseFlags.IsMenuMode] = false;
                        }
                        else
                        {
                            // If we haven't pushed the menu mode yet (which
                            // should have already happened if keyboard focus
                            // is set within the menu), push it now.
                            if (!HasPushedMenuMode)
                            {
                                PushMenuMode(/*isAcquireFocusMenuMode*/ false);
                            }
                            
                            RaiseClrEvent(InternalMenuModeChangedKey, EventArgs.Empty);
                        }
                    }

                    if (!isMenuMode)
                    {
                        bool wasSubmenuOpen = false;

                        if (CurrentSelection != null)
                        {
                            wasSubmenuOpen = CurrentSelection.IsSubmenuOpen;
                            CurrentSelection.IsSubmenuOpen = false;
                            CurrentSelection = null;
                        }

                        // Note that this code path is also used to cleanup
                        // the case where setting IsMenuMode=true fails due
                        // to failure to gain capture. We pop out of the menu
                        // mode irrespective of where it was pushed.
                        if (HasPushedMenuMode)
                        {
                            // Call PopMenuMode before we do much else, so that
                            // focus changes will properly activate windows.
                            PopMenuMode();
                        }

                        if (!value)
                        {
                            // Fire the event before capture is released and after submenus have been closed.
                            RaiseClrEvent(InternalMenuModeChangedKey, EventArgs.Empty);
                        }

                        // Clear suspending animation flags on all descendant menuitems
                        SetSuspendingPopupAnimation(this, null, false);

                        // In the future, make sure that after we release capture we don't do anything except
                        //       return focus to the previously focused element.
                        // Release capture.  We might not have capture b/c the popup was still open when we lost activation.
                        // This means a debugger stopped us or someone fooled around with popups opening.  May be an issue later.
                        //Debug.Assert(Mouse.Captured == this, "Menu did not have capture. Why?");
                        if (HasCapture)
                        {
                            Mouse.Capture(null);
                        }

                        RestorePreviousFocus();
                    }

                    // Assume menu items should open when the mouse hovers over them
                    OpenOnMouseEnter = isMenuMode;
                }
            }
        }

        // This bool is used by top level menu items to
        // determine if they should open on mouse enter
        // Menu items shouldn't open if the use hit Alt
        // to get in menu mode and then hovered over the item
        internal bool OpenOnMouseEnter
        {
            get { return _bitFlags[(int)MenuBaseFlags.OpenOnMouseEnter]; }
            set { _bitFlags[(int)MenuBaseFlags.OpenOnMouseEnter] = value; }
        }

        private void PushMenuMode(bool isAcquireFocusMenuMode)
        {
            Debug.Assert(_pushedMenuMode == null);
            _pushedMenuMode = PresentationSource.CriticalFromVisual(this);
            Debug.Assert(_pushedMenuMode != null);
            IsAcquireFocusMenuMode = isAcquireFocusMenuMode;
            InputManager.UnsecureCurrent.PushMenuMode(_pushedMenuMode);
        }

        // **** Note:  This method is called via private reflection from RibbonMenuButton.
        //             Do not rename, remove, or change the method signature without fixing RibbonMenuButton.
        private void PopMenuMode()
        {
            Debug.Assert(_pushedMenuMode != null);

            PresentationSource pushedMenuMode = _pushedMenuMode;
            _pushedMenuMode = null;
            IsAcquireFocusMenuMode = false;
            InputManager.UnsecureCurrent.PopMenuMode(pushedMenuMode);
        }

        // **** Note:  This property is read via private reflection from RibbonMenuButton.
        //             Do not rename/remove this property without fixing RibbonMenuButton.
        private bool HasPushedMenuMode
        {
            get
            {
                return _pushedMenuMode != null;
            }
        }

        /// <summary>
        ///     This boolean determines if the PushMenuMode was
        ///     performed due to acquire focus or due to programmatic
        ///     set of IsMenuMode.
        /// </summary>
        private bool IsAcquireFocusMenuMode
        {
            get { return _bitFlags[(int)MenuBaseFlags.IsAcquireFocusMenuMode]; }
            set { _bitFlags[(int)MenuBaseFlags.IsAcquireFocusMenuMode] = value; }
        }

        private PresentationSource _pushedMenuMode;

        private MenuItem _currentSelection;
        private BitVector32 _bitFlags = new BitVector32(0);

        private enum MenuBaseFlags
        {
            IgnoreNextLeftRelease  = 0x01,
            IgnoreNextRightRelease = 0x02,
            IsMenuMode             = 0x04,
            OpenOnMouseEnter       = 0x08,
            IsAcquireFocusMenuMode = 0x10,
        }

        #endregion

        // Notes:
        // We want to enforce:
        // 1) IsKeyboardFocused -> IsHighlighted
        // 2) IsKeyboardFocusWithin -> Mouse.Captured is an ancestor of Keyboard.FocusedElement
        // 3) IsSubmenuOpen -> IsHighlighted and IsKeyboardFocusWithin
        // these conditions are violated only in the case of mousing from one submenu to another and there is a delay.
    }
}
