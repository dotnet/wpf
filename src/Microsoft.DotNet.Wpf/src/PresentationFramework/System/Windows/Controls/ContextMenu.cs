// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Utility;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows;
#if OLD_AUTOMATION
using System.Windows.Automation.Provider;
#endif
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Shapes;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Control that defines a menu of choices for users to invoke.
    /// </summary>
    [DefaultEvent("Opened")]
#if OLD_AUTOMATION
    [Automation(AccessibilityControlType = "Menu")]
#endif
    public class ContextMenu : MenuBase
    {
        #region Constructors

        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        static ContextMenu()
        {
            EventManager.RegisterClassHandler(typeof(ContextMenu), AccessKeyManager.AccessKeyPressedEvent, new AccessKeyPressedEventHandler(OnAccessKeyPressed));

            DefaultStyleKeyProperty.OverrideMetadata(typeof(ContextMenu), new FrameworkPropertyMetadata(typeof(ContextMenu)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(ContextMenu));

            IsTabStopProperty.OverrideMetadata(typeof(ContextMenu), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(ContextMenu), new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));
            KeyboardNavigation.ControlTabNavigationProperty.OverrideMetadata(typeof(ContextMenu), new FrameworkPropertyMetadata(KeyboardNavigationMode.Contained));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(ContextMenu), new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));

            // Disable the default focus visual for ContextMenu
            FocusVisualStyleProperty.OverrideMetadata(typeof(ContextMenu), new FrameworkPropertyMetadata((object)null /* default value */));
        }

        /// <summary>
        ///     Default ContextMenu constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public ContextMenu() : base()
        {
            Initialize();
        }

        #endregion


        #region Public Properties

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        /// <summary>
        ///     The DependencyProperty for the HorizontalOffset property.
        /// </summary>
        public static readonly DependencyProperty HorizontalOffsetProperty =
                ContextMenuService.HorizontalOffsetProperty.AddOwner(typeof(ContextMenu),
                            new FrameworkPropertyMetadata(null,
                                                          new CoerceValueCallback(CoerceHorizontalOffset)));

        private static object CoerceHorizontalOffset(DependencyObject d, object value)
        {
            return PopupControlService.CoerceProperty(d, value, ContextMenuService.HorizontalOffsetProperty);
        }

        /// <summary>
        /// Get or set X offset of the ContextMenu
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        [Bindable(true), Category("Layout")]
        public double HorizontalOffset
        {
            get { return (double) GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the VerticalOffset property.
        /// </summary>
        public static readonly DependencyProperty VerticalOffsetProperty =
                ContextMenuService.VerticalOffsetProperty.AddOwner(typeof(ContextMenu),
                            new FrameworkPropertyMetadata(null,
                                                          new CoerceValueCallback(CoerceVerticalOffset)));

        private static object CoerceVerticalOffset(DependencyObject d, object value)
        {
            return PopupControlService.CoerceProperty(d, value, ContextMenuService.VerticalOffsetProperty);
        }

        /// <summary>
        /// Get or set Y offset of the ContextMenu
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        [Bindable(true), Category("Layout")]
        public double VerticalOffset
        {
            get { return (double) GetValue(VerticalOffsetProperty); }
            set { SetValue(VerticalOffsetProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for IsOpen property
        /// </summary>
        public static readonly DependencyProperty IsOpenProperty =
                Popup.IsOpenProperty.AddOwner(
                        typeof(ContextMenu),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                new PropertyChangedCallback(OnIsOpenChanged)));

        /// <summary>
        /// Get or set IsOpen property of the ContextMenu
        /// </summary>
        [Bindable(true), Browsable(false), Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsOpen
        {
            get { return (bool) GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, BooleanBoxes.Box(value)); }
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ContextMenu ctrl = (ContextMenu) d;

            if ((bool) e.NewValue)
            {
                if (ctrl._parentPopup == null)
                {
                    ctrl.HookupParentPopup();
                }

                ctrl._parentPopup.Unloaded += new RoutedEventHandler(ctrl.OnPopupUnloaded);

                // Turn on keyboard cues in case ContextMenu was opened with the keyboard
                ctrl.SetValue(KeyboardNavigation.ShowKeyboardCuesProperty, KeyboardNavigation.IsKeyboardMostRecentInputDevice());
            }
            else
            {
                ctrl.ClosingMenu();
            }
        }

        /// <summary>
        ///     The DependencyProperty for the PlacementTarget property.
        /// </summary>
        public static readonly DependencyProperty PlacementTargetProperty =
                ContextMenuService.PlacementTargetProperty.AddOwner(
                        typeof(ContextMenu),
                            new FrameworkPropertyMetadata(null,
                                                          new CoerceValueCallback(CoercePlacementTarget)));

        private static object CoercePlacementTarget(DependencyObject d, object value)
        {
            return PopupControlService.CoerceProperty(d, value, ContextMenuService.PlacementTargetProperty);
        }

        /// <summary>
        /// Get or set PlacementTarget property of the ContextMenu
        /// </summary>
        [Bindable(true), Category("Layout")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIElement PlacementTarget
        {
            get { return (UIElement) GetValue(PlacementTargetProperty); }
            set { SetValue(PlacementTargetProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the PlacementRectangle property.
        /// </summary>
        public static readonly DependencyProperty PlacementRectangleProperty =
                ContextMenuService.PlacementRectangleProperty.AddOwner(typeof(ContextMenu),
                            new FrameworkPropertyMetadata(null,
                                                          new CoerceValueCallback(CoercePlacementRectangle)));

        private static object CoercePlacementRectangle(DependencyObject d, object value)
        {
            return PopupControlService.CoerceProperty(d, value, ContextMenuService.PlacementRectangleProperty);
        }

        /// <summary>
        /// Get or set PlacementRectangle property of the ContextMenu
        /// </summary>
        [Bindable(true), Category("Layout")]
        public Rect PlacementRectangle
        {
            get { return (Rect) GetValue(PlacementRectangleProperty); }
            set { SetValue(PlacementRectangleProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the Placement property.
        /// </summary>
        public static readonly DependencyProperty PlacementProperty =
                ContextMenuService.PlacementProperty.AddOwner(typeof(ContextMenu),
                            new FrameworkPropertyMetadata(null,
                                                          new CoerceValueCallback(CoercePlacement)));

        private static object CoercePlacement(DependencyObject d, object value)
        {
            return PopupControlService.CoerceProperty(d, value, ContextMenuService.PlacementProperty);
        }

        /// <summary>
        /// Get or set Placement property of the ContextMenu
        /// </summary>
        [Bindable(true), Category("Layout")]
        public PlacementMode Placement
        {
            get { return (PlacementMode) GetValue(PlacementProperty); }
            set { SetValue(PlacementProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for HasDropShadow
        /// </summary>
        public static readonly DependencyProperty HasDropShadowProperty =
                ContextMenuService.HasDropShadowProperty.AddOwner(
                        typeof(ContextMenu),
                        new FrameworkPropertyMetadata(null,
                                                      new CoerceValueCallback(CoerceHasDropShadow)));

        private static object CoerceHasDropShadow(DependencyObject d, object value)
        {
            ContextMenu cm = (ContextMenu)d;

            if (cm._parentPopup == null || !cm._parentPopup.AllowsTransparency || !SystemParameters.DropShadow)
            {
                return BooleanBoxes.FalseBox;
            }

            return PopupControlService.CoerceProperty(d, value, ContextMenuService.HasDropShadowProperty);
        }

        /// <summary>
        ///     Whether the control has a drop shadow.
        /// </summary>
        public bool HasDropShadow
        {
            get { return (bool)GetValue(HasDropShadowProperty); }
            set { SetValue(HasDropShadowProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the CustomPopupPlacementCallback property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty CustomPopupPlacementCallbackProperty =
                Popup.CustomPopupPlacementCallbackProperty.AddOwner(typeof(ContextMenu));

        /// <summary>
        ///     Chooses the behavior of where the ContextMenu should be placed on screen.
        /// </summary>
        [Bindable(false), Category("Layout")]
        public CustomPopupPlacementCallback CustomPopupPlacementCallback
        {
            get { return (CustomPopupPlacementCallback) GetValue(CustomPopupPlacementCallbackProperty); }
            set { SetValue(CustomPopupPlacementCallbackProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the StaysOpen property.
        ///     Indicates that, once opened, ContextMenu should stay open until IsOpenProperty changed to 'false'.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty StaysOpenProperty =
                Popup.StaysOpenProperty.AddOwner(typeof(ContextMenu));

        /// <summary>
        ///     Chooses the behavior of when the ContextMenu should automatically close.
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public bool StaysOpen
        {
            get { return (bool) GetValue(StaysOpenProperty); }
            set { SetValue(StaysOpenProperty, value); }
        }

        #endregion

        #region Events

        /// <summary>
        ///     Opened event
        /// </summary>
        public static readonly RoutedEvent OpenedEvent = PopupControlService.ContextMenuOpenedEvent.AddOwner(typeof(ContextMenu));

        /// <summary>
        ///     Event that fires when the popup opens.
        /// </summary>
        public event RoutedEventHandler Opened
        {
            add
            {
                AddHandler(OpenedEvent, value);
            }
            remove
            {
                RemoveHandler(OpenedEvent, value);
            }
        }

        /// <summary>
        ///     Called when the OpenedEvent fires.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnOpened(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Closed event
        /// </summary>
        public static readonly RoutedEvent ClosedEvent = PopupControlService.ContextMenuClosedEvent.AddOwner(typeof(ContextMenu));

        /// <summary>
        ///     Event that fires when the popup closes
        /// </summary>
        public event RoutedEventHandler Closed
        {
            add
            {
                AddHandler(ClosedEvent, value);
            }
            remove
            {
                RemoveHandler(ClosedEvent, value);
            }
        }

        /// <summary>
        ///     Called when the ClosedEvent fires.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnClosed(RoutedEventArgs e)
        {
            RaiseEvent(e);
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
            return new System.Windows.Automation.Peers.ContextMenuAutomationPeer(this);
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
            if (e.Handled || !IsOpen)
            {
                // Ignore if the event was already handled or if the menu closed. This might happen
                // if input events get queued up and one in the middle caused the menu to close.
                return;
            }

            Key key = e.Key;

            switch (key)
            {
                case Key.Down:
                    if (CurrentSelection == null)
                    {
                        NavigateToStart(new ItemNavigateArgs(e.Device, Keyboard.Modifiers));
                        e.Handled = true;
                    }

                    break;

                case Key.Up:
                    if (CurrentSelection == null)
                    {
                        NavigateToEnd(new ItemNavigateArgs(e.Device, Keyboard.Modifiers));
                        e.Handled = true;
                    }

                    break;
            }
        }

        /// <summary>
        ///     This is the method that responds to the KeyUp event.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (!e.Handled && IsOpen && e.Key == Key.Apps)
            {
                    KeyboardLeaveMenuMode();
                    e.Handled = true;
            }
        }

        #endregion

        #region Implementation

        private static readonly DependencyProperty InsideContextMenuProperty =
            MenuItem.InsideContextMenuProperty.AddOwner(typeof(ContextMenu),
                                                        new FrameworkPropertyMetadata(BooleanBoxes.TrueBox,
                                                                                      FrameworkPropertyMetadataOptions.Inherits));

        //-------------------------------------------------------------------
        //
        //  Implementation
        //
        //-------------------------------------------------------------------

        private void Initialize()
        {
            // We have to set this locally in order for inheritance to work
            MenuItem.SetInsideContextMenuProperty(this, true);

            InternalMenuModeChanged += new EventHandler(OnIsMenuModeChanged);
        }

        private void HookupParentPopup()
        {
            Debug.Assert(_parentPopup == null, "_parentPopup should be null");

            _parentPopup = new Popup();

            _parentPopup.AllowsTransparency = true;

            // Coerce HasDropShadow property in case popup can't be transparent
            CoerceValue(HasDropShadowProperty);

            _parentPopup.DropOpposite = false;

            // Listening to the Opened and Closed events lets us guarantee that
            // the popup is actually opened when we perform those functions.
            _parentPopup.Opened += new EventHandler(OnPopupOpened);
            _parentPopup.Closed += new EventHandler(OnPopupClosed);
            _parentPopup.PopupCouldClose += new EventHandler(OnPopupCouldClose);

            _parentPopup.SetResourceReference(Popup.PopupAnimationProperty, SystemParameters.MenuPopupAnimationKey);

            // Hooks up the popup properties from this menu to the popup so that
            // setting them on this control will also set them on the popup.
            Popup.CreateRootPopup(_parentPopup, this);
        }

        private void OnPopupCouldClose(object sender, EventArgs e)
        {
            SetCurrentValueInternal(IsOpenProperty, BooleanBoxes.FalseBox);
        }

        private void OnPopupOpened(object source, EventArgs e)
        {
            if (CurrentSelection != null)
            {
                CurrentSelection = null;
            }
            IsMenuMode = true;

            // When we open, if the Left or Right buttons are pressed, MenuBase should not
            // dismiss when it sees the up for those buttons.
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                IgnoreNextLeftRelease = true;
            }
            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                IgnoreNextRightRelease = true;
            }

            OnOpened(new RoutedEventArgs(OpenedEvent, this));
        }

        private void OnPopupClosed(object source, EventArgs e)
        {
            // Clear out any state we stored for this time around
            IgnoreNextLeftRelease = false;
            IgnoreNextRightRelease = false;

            IsMenuMode = false;
            OnClosed(new RoutedEventArgs(ClosedEvent, this));
        }

        private void ClosingMenu()
        {
            if (_parentPopup != null)
            {
                _parentPopup.Unloaded -= new RoutedEventHandler(OnPopupUnloaded);

                // As the menu closes, we need the parent connection to be maintained
                // while we do things like release capture so that notifications
                // go up the tree correctly. Post this for later.
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    (DispatcherOperationCallback)delegate(object arg)
                    {
                        ContextMenu cm = (ContextMenu)arg;
                        if (!cm.IsOpen) // Check that the menu is still closed
                        {
                            // Prevent focus scoping from remembering the last focused element.
                            // The next time the menu opens, we want to start clean.
                            FocusManager.SetFocusedElement(cm, null);
                        }
                        return null;
                    },
                    this);
            }
        }

        private void OnPopupUnloaded(object sender, RoutedEventArgs e)
        {
            // The tree that the ContextMenu is in is being torn down, close the menu.

            if (IsOpen)
            {
                // This will be called during a tree walk, closing the menu will cause a tree change,
                // so post for later.
                Dispatcher.BeginInvoke(DispatcherPriority.Send,
                    (DispatcherOperationCallback)delegate(object arg)
                    {
                        ContextMenu cm = (ContextMenu)arg;
                        if (cm.IsOpen) // Check that the menu is still open
                        {
                            cm.SetCurrentValueInternal(IsOpenProperty, BooleanBoxes.FalseBox);
                        }
                        return null;
                    },
                    this);
            }
        }

        /// <summary>
        ///     Called when IsMenuMode changes on this class
        /// </summary>
        private void OnIsMenuModeChanged(object sender, EventArgs e)
        {
            // IsMenuMode changed from false to true
            if (IsMenuMode)
            {
                // Keep the previous focus
                if (Keyboard.FocusedElement != null)
                {
                    _weakRefToPreviousFocus = new WeakReference<IInputElement>(Keyboard.FocusedElement);
                }

                // Take focus so we get keyboard events.
                Focus();
            }
            else // IsMenuMode changed from true to false
            {
                SetCurrentValueInternal(IsOpenProperty, BooleanBoxes.FalseBox);

                if(_weakRefToPreviousFocus != null)
                {
                    IInputElement previousFocus;
                    if (_weakRefToPreviousFocus.TryGetTarget(out previousFocus))
                    {
                        // Previous focused element is still alive, so return focus to it.
                        previousFocus.Focus();
                    }
                    
                    _weakRefToPreviousFocus = null;
                }
            }
        }

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            // If keyboard focus moves out from within the ContextMenu, the
            // ContextMenu will be dismissed.  We do not want to restore focus
            // in this case.
            //
            // See MenuBase.OnIsKeyboardFocusWithinChanged
            if((bool)e.NewValue == false)
            {
                _weakRefToPreviousFocus = null;
            }

            // Allow the base class to dismiss us as needed.
            base.OnIsKeyboardFocusWithinChanged(e);
        }

        internal override bool IgnoreModelParentBuildRoute(RoutedEventArgs e)
        {
            // Context menus are logically connected to their host element.  Generally, we don't
            // want input events to route out of the context menu.  Consider the sitituation where
            // a TextBox has a ContextMenu.  It is confusing for the text box to move the cursor
            // when I press the arrow keys while the context menu is being displayed.
            //
            // For now we only block keyboard events and ToolTip events.  What about mouse & stylus events?
            //
            // Note: This will cause the route to not follow the logical link, but it will still
            // follow the visual link.  At the time of writing this comment, the visual link
            // contained things like an adorner decorator.  Eventually the visual ancestory lead
            // to a PopupRoot, which also has a logical link over to the Popup element.  Since
            // the PopupRoot does not override this virtual, the route continues through its logical
            // link and ends up escaping into the larger logical tree anyways.
            //
            // The solution is that the PopupRoot element (on the top of this visual tree) will
            // defer back to this method to determine if it should route any further.
            //
            return (e is KeyEventArgs) || (e is FindToolTipEventArgs);
        }

        private static void OnAccessKeyPressed(object sender, AccessKeyPressedEventArgs e)
        {
            e.Scope = sender;
            e.Handled = true;
        }

        /// <summary>
        /// Called when this element's visual parent changes
        /// </summary>
        /// <param name="oldParent"></param>
        protected internal override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);

            if (!Popup.IsRootedInPopup(_parentPopup, this))
            {
                throw new InvalidOperationException(SR.Get(SRID.ElementMustBeInPopup, "ContextMenu"));
            }
        }

        internal override void OnAncestorChanged()
        {
            base.OnAncestorChanged();

            if (!Popup.IsRootedInPopup(_parentPopup, this))
            {
                throw new InvalidOperationException(SR.Get(SRID.ElementMustBeInPopup, "ContextMenu"));
            }
        }

        #endregion

        #region Private Fields

        private Popup _parentPopup;
        private WeakReference<IInputElement> _weakRefToPreviousFocus; // Keep the previously focused element before CM to open

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
