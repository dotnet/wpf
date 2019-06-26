// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Utility;
using System.ComponentModel;

using System.Diagnostics;
using System.Windows.Threading;

#if OLD_AUTOMATION
using System.Windows.Automation.Provider;
#endif
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

using System;
using System.Security;
using MS.Internal.Telemetry.PresentationFramework;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Control that defines a menu of choices for users to invoke.
    /// </summary>
#if OLD_AUTOMATION
    [Automation(AccessibilityControlType = "Menu")]
#endif
    public class Menu : MenuBase
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
        public Menu() : base()
        {
        }

        static Menu()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Menu), new FrameworkPropertyMetadata(typeof(Menu)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(Menu));

            ItemsPanelProperty.OverrideMetadata(typeof(Menu), new FrameworkPropertyMetadata(GetDefaultPanel()));
            IsTabStopProperty.OverrideMetadata(typeof(Menu), new FrameworkPropertyMetadata(false));

            KeyboardNavigation.ControlTabNavigationProperty.OverrideMetadata(typeof(Menu), new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(Menu), new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));

            EventManager.RegisterClassHandler(typeof(Menu), AccessKeyManager.AccessKeyPressedEvent, new AccessKeyPressedEventHandler(OnAccessKeyPressed));

            ControlsTraceLogger.AddControl(TelemetryControls.Menu);
        }

        private static ItemsPanelTemplate GetDefaultPanel()
        {
            FrameworkElementFactory panel = new FrameworkElementFactory(typeof(WrapPanel));
            ItemsPanelTemplate template = new ItemsPanelTemplate(panel);
            template.Seal();
            return template;
        }

        #endregion


        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        /// <summary>
        ///     DependencyProperty for the IsMainMenuProperty
        /// </summary>
        public static readonly DependencyProperty IsMainMenuProperty =
                DependencyProperty.Register(
                        "IsMainMenu",
                        typeof(bool),
                        typeof(Menu),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.TrueBox,
                                new PropertyChangedCallback(OnIsMainMenuChanged)));

        /// <summary>
        ///     True if this menu will participate in main menu activation notification.
        ///     If there are multiple menus on a page, menus that do not wish to receive ALT or F10
        ///     key notification should set this property to false.
        /// </summary>
        /// <value></value>
        public bool IsMainMenu
        {
            get { return (bool) GetValue(IsMainMenuProperty); }
            set { SetValue(IsMainMenuProperty, BooleanBoxes.Box(value)); }
        }

        private static void OnIsMainMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Menu menu = d as Menu;
            if ((bool) e.NewValue)
            {
                menu.SetupMainMenu();
            }
            else
            {
                menu.CleanupMainMenu();
            }
        }

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new System.Windows.Automation.Peers.MenuAutomationPeer(this);
        }

        /// <summary>
        ///     This virtual method in called when IsInitialized is set to true and it raises an Initialized event
        /// </summary>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            if (IsMainMenu)
            {
                SetupMainMenu();
            }
        }

        private void SetupMainMenu()
        {
            if (_enterMenuModeHandler == null)
            {
                _enterMenuModeHandler = new KeyboardNavigation.EnterMenuModeEventHandler(OnEnterMenuMode);
                KeyboardNavigation.Current.EnterMenuMode += _enterMenuModeHandler;
           }
       }

        private void CleanupMainMenu()
        {
            if (_enterMenuModeHandler != null)
            {
                KeyboardNavigation.Current.EnterMenuMode -= _enterMenuModeHandler;
            }
        }

        private static object OnGetIsMainMenu(DependencyObject d)
        {
            return BooleanBoxes.Box(((Menu)d).IsMainMenu);
        }

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

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
        ///     This is the method that responds to the KeyDown event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled) return;

            Key key = e.Key;
            switch (key)
            {
                case Key.Down:
                case Key.Up:
                    if (CurrentSelection != null)
                    {
                        // Only for non vertical layout Up/Down open the submenu
                        Panel itemsHost = ItemsHost;
                        bool isVertical = itemsHost != null && itemsHost.HasLogicalOrientation && itemsHost.LogicalOrientation == Orientation.Vertical;
                        if (!isVertical)
                        {
                            CurrentSelection.OpenSubmenuWithKeyboard();
                            e.Handled = true;
                        }
                    }
                    break;
                case Key.Left:
                case Key.Right:
                    if (CurrentSelection != null)
                    {
                        // Only for vertical layout Left/Right open the submenu
                        Panel itemsHost = ItemsHost;
                        bool isVertical = itemsHost != null && itemsHost.HasLogicalOrientation && itemsHost.LogicalOrientation == Orientation.Vertical;
                        if (isVertical)
                        {
                            CurrentSelection.OpenSubmenuWithKeyboard();
                            e.Handled = true;
                        }
                    }
                    break;
            }
        }

        /// <summary>
        ///     This is the method that responds to the TextInput event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            base.OnTextInput(e);
            if (e.Handled) return;

            // We don't use win32 menu's, so we need to emulate the win32
            // behavior for hitting Space while in menu mode.  Alt+Space
            // will be handled as a SysKey by the DefaultWindowProc, but
            // Alt, then Space needs to be special cased here because we prevent win32.
            // from entering menu mode.  In WPF the equiv. of win32 menu mode is having
            // a main menu with focus and no menu items opened.
            if (e.UserInitiated &&
                e.Text == " " &&
                IsMainMenu &&
                (CurrentSelection == null || !CurrentSelection.IsSubmenuOpen))
            {
                // We need to exit menu mode because it holds capture and prevents
                // the system menu from showing.
                IsMenuMode = false;
                System.Windows.Interop.HwndSource source = PresentationSource.CriticalFromVisual(this) as System.Windows.Interop.HwndSource;
                if (source != null)
                {
                    source.ShowSystemMenu();
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        ///     Called when any mouse button is pressed or released on this subtree
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void HandleMouseButton(MouseButtonEventArgs e)
        {
            base.HandleMouseButton(e);

            if (e.Handled)
            {
                return;
            }

            if (e.ChangedButton != MouseButton.Left && e.ChangedButton != MouseButton.Right)
            {
                return;
            }

            // We want to dismiss when someone clicks on the menu bar, so
            // really we're interested in clicks that bubble up from an
            // element whose TemplatedParent is the Menu.
            if (IsMenuMode)
            {
                FrameworkElement element = e.OriginalSource as FrameworkElement;

                if ((element != null && (element == this || element.TemplatedParent == this)))
                {
                    IsMenuMode = false;
                    e.Handled = true;
                }
            }
        }

        internal override bool FocusItem(ItemInfo info, ItemNavigateArgs itemNavigateArgs)
        {
            bool returnValue = base.FocusItem(info, itemNavigateArgs);
            // Trying to navigate from the current menuitem (this) to an adjacent menuitem.

            if (itemNavigateArgs.DeviceUsed is KeyboardDevice)
            {
                // If the item is a TopLevelHeader then when you navigate onto it, the submenu will open
                // and we should select the first item in the submenu.  The parent MenuItem will take care
                // of opening the submenu but doesn't know whether focus changed because of a mouse action
                // or a keyboard action.  Help out by focusing the first thing in the new submenu.

                // Assume that KeyboardNavigation.Current.Navigate moved focus onto the element onto which
                // it navigated.
                MenuItem newSelection = info.Container as MenuItem;
                if (newSelection != null
                    && newSelection.Role == MenuItemRole.TopLevelHeader
                    && newSelection.IsSubmenuOpen)
                {
                    newSelection.NavigateToStart(itemNavigateArgs);
                }
            }
            return returnValue;
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private static void OnAccessKeyPressed(object sender, AccessKeyPressedEventArgs e)
        {
            // If ALT is down, then blend our scope into the one above. Maybe bad, but only if Menu is not top-level.
            if (!(Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
            {
                e.Scope = sender;
                e.Handled = true;
            }
        }

        private bool OnEnterMenuMode(object sender, EventArgs e)
        {
            // Don't enter menu mode if someone has capture
            if (Mouse.Captured != null)
                return false;

            // Need to check that ALT/F10 happened in our source.
            PresentationSource source = sender as PresentationSource;
            PresentationSource mySource = null;

            mySource = PresentationSource.CriticalFromVisual(this);
            if (source == mySource)
            {
                // Give focus to the first possible element in the ItemsControl
                for (int i = 0; i < Items.Count; i++)
                {
                    MenuItem menuItem = ItemContainerGenerator.ContainerFromIndex(i) as MenuItem;

                    if (menuItem != null && !(Items[i] is Separator))
                    {
                        if (menuItem.Focus())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 28; }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private KeyboardNavigation.EnterMenuModeEventHandler _enterMenuModeHandler;

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
    }
}
