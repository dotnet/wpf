// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Diagnostics;
using System.ComponentModel;

using System.Collections;
using System.Collections.Specialized;
using System.Windows.Threading;

using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Shapes;
using MS.Utility;
using MS.Internal.KnownBoxes;

namespace System.Windows.Controls
{
    /// <summary>
    /// A control to display information when the user hovers over a control
    /// </summary>
    [DefaultEvent("Opened")]
    [Localizability(LocalizationCategory.ToolTip)]
    public class ToolTip : ContentControl
    {
        #region Constructors

        static ToolTip()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolTip), new FrameworkPropertyMetadata(typeof(ToolTip)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(ToolTip));
            BackgroundProperty.OverrideMetadata(typeof(ToolTip), new FrameworkPropertyMetadata(SystemColors.InfoBrush));
            FocusableProperty.OverrideMetadata(typeof(ToolTip), new FrameworkPropertyMetadata(false));
        }

        /// <summary>
        /// Creates a default ToolTip
        /// </summary>
        public ToolTip() : base()
        {
        }

        #endregion

        #region Public Properties

        /// Tooltips should show on Keyboard focus.
        /// To allow for tooltips to show on Keyboard focus correctly PopupControlService, Popup and Tooltip 
        /// need to know that this particular tooltip showing was caused by keyboard, PopupControlService sets 
        /// FromKeyboard when it determines it must show the tooltip, when the Popup for the tooltip is created 
        /// a binding between Tooltip's FromKeyboard and Popup's TreatMousePlacementAsBottom is also created, 
        /// this chain effectively lets Popup place itself correctly upon Keyboard focus.

        /// <summary>
        /// The DependencyProperty for the FromKeyboard property.
        /// Default: false
        /// </summary>
        internal static readonly DependencyProperty FromKeyboardProperty =
            DependencyProperty.Register(
                                "FromKeyboard",
                                typeof(bool),
                                typeof(ToolTip),
                                new FrameworkPropertyMetadata(
                                            false));

        /// <summary>
        /// Whether or not the tooltip showing was caused by a Keyboard focus.
        /// </summary>
        [Bindable(true), Category("Behavior")]
        internal bool FromKeyboard
        {
            get
            {
                return (bool)GetValue(FromKeyboardProperty);
            }
            set
            {
                SetValue(FromKeyboardProperty, value);
            }
        }

        /// <summary>
        /// The DependencyProperty for the HorizontalOffset property.
        /// Default: Length(0.0)
        /// </summary>
        public static readonly DependencyProperty HorizontalOffsetProperty =
            ToolTipService.HorizontalOffsetProperty.AddOwner(typeof(ToolTip),
                            new FrameworkPropertyMetadata(null,
                                                          new CoerceValueCallback(CoerceHorizontalOffset)));

        private static object CoerceHorizontalOffset(DependencyObject d, object value)
        {
            return PopupControlService.CoerceProperty(d, value, ToolTipService.HorizontalOffsetProperty);
        }

        /// <summary>
        /// Horizontal offset from the default location when this ToolTIp is displayed
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        [Bindable(true), Category("Layout")]
        public double HorizontalOffset
        {
            get
            {
                return (double)GetValue(HorizontalOffsetProperty);
            }
            set
            {
                SetValue(HorizontalOffsetProperty, value);
            }
        }

        /// <summary>
        /// The DependencyProperty for the VerticalOffset property.
        /// Default: Length(0.0)
        /// </summary>
        public static readonly DependencyProperty VerticalOffsetProperty =
            ToolTipService.VerticalOffsetProperty.AddOwner(typeof(ToolTip),
                            new FrameworkPropertyMetadata(null,
                                                          new CoerceValueCallback(CoerceVerticalOffset)));

        private static object CoerceVerticalOffset(DependencyObject d, object value)
        {
            return PopupControlService.CoerceProperty(d, value, ToolTipService.VerticalOffsetProperty);
        }

        /// <summary>
        /// Vertical offset from the default location when this ToolTip is displayed
        /// </summary>
        [TypeConverter(typeof(LengthConverter))]
        [Bindable(true), Category("Layout")]
        public double VerticalOffset
        {
            get
            {
                return (double)GetValue(VerticalOffsetProperty);
            }
            set
            {
                SetValue(VerticalOffsetProperty, value);
            }
        }

        /// <summary>
        /// DependencyProperty for the IsOpen property
        /// Default value: false
        /// </summary>
        public static readonly DependencyProperty IsOpenProperty =
                    DependencyProperty.Register(
                                "IsOpen",
                                typeof(bool),
                                typeof(ToolTip),
                                new FrameworkPropertyMetadata(
                                            false,
                                            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                            new PropertyChangedCallback(OnIsOpenChanged)));

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ToolTip t = (ToolTip) d;

            if ((bool)e.NewValue)
            {
                if (t._parentPopup == null)
                {
                    t.HookupParentPopup();
                }
            }
            else
            {
                // When ToolTip is about to close but still hooked up - we need to raise Accessibility event
                if (AutomationPeer.ListenerExists(AutomationEvents.ToolTipClosed))
                {
                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(t);
                    if (peer != null)
                        peer.RaiseAutomationEvent(AutomationEvents.ToolTipClosed);
                }
            }

            OnVisualStatePropertyChanged(d, e);
        }

        /// <summary>
        /// Whether or not this ToolTip is visible
        /// </summary>
        [Bindable(true), Browsable(false), Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsOpen
        {
            get { return (bool) GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for HasDropShadow
        /// </summary>
        public static readonly DependencyProperty HasDropShadowProperty =
                ToolTipService.HasDropShadowProperty.AddOwner(
                        typeof(ToolTip),
                        new FrameworkPropertyMetadata(null,
                                                      new CoerceValueCallback(CoerceHasDropShadow)));

        private static object CoerceHasDropShadow(DependencyObject d, object value)
        {
            ToolTip tt = (ToolTip)d;

            if (tt._parentPopup == null || !tt._parentPopup.AllowsTransparency || !SystemParameters.DropShadow)
            {
                return BooleanBoxes.FalseBox;
            }

            return PopupControlService.CoerceProperty(d, value, ToolTipService.HasDropShadowProperty);
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
        /// The DependencyProperty for the PlacementTarget property
        /// Default value: null
        /// </summary>
        public static readonly DependencyProperty PlacementTargetProperty =
                    ToolTipService.PlacementTargetProperty.AddOwner(typeof(ToolTip),
                            new FrameworkPropertyMetadata(null,
                                                          new CoerceValueCallback(CoercePlacementTarget)));

        private static object CoercePlacementTarget(DependencyObject d, object value)
        {
            return PopupControlService.CoerceProperty(d, value, ToolTipService.PlacementTargetProperty);
        }


        /// <summary>
        /// The UIElement relative to which this ToolTip will be displayed.
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
                    ToolTipService.PlacementRectangleProperty.AddOwner(typeof(ToolTip),
                            new FrameworkPropertyMetadata(null,
                                                          new CoerceValueCallback(CoercePlacementRectangle)));

        private static object CoercePlacementRectangle(DependencyObject d, object value)
        {
            return PopupControlService.CoerceProperty(d, value, ToolTipService.PlacementRectangleProperty);
        }

        /// <summary>
        /// Get or set PlacementRectangle property of the ToolTip
        /// </summary>
        [Bindable(true), Category("Layout")]
        public Rect PlacementRectangle
        {
            get { return (Rect) GetValue(PlacementRectangleProperty); }
            set { SetValue(PlacementRectangleProperty, value); }
        }

        /// <summary>
        /// The DependencyProperty for the Placement property
        /// Default value: null
        /// </summary>
        public static readonly DependencyProperty PlacementProperty =
                    ToolTipService.PlacementProperty.AddOwner(typeof(ToolTip),
                            new FrameworkPropertyMetadata(null,
                                                          new CoerceValueCallback(CoercePlacement)));

        private static object CoercePlacement(DependencyObject d, object value)
        {
            return PopupControlService.CoerceProperty(d, value, ToolTipService.PlacementProperty);
        }

        /// <summary>
        ///     Chooses the behavior of where the Popup should be placed on screen.
        /// </summary>
        [Bindable(true), Category("Layout")]
        public PlacementMode Placement
        {
            get { return (PlacementMode) GetValue(PlacementProperty); }
            set { SetValue(PlacementProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the CustomPopupPlacementCallback property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty CustomPopupPlacementCallbackProperty =
                    Popup.CustomPopupPlacementCallbackProperty.AddOwner(typeof(ToolTip));

        /// <summary>
        ///     Chooses the behavior of where the Popup should be placed on screen.
        /// </summary>
        [Bindable(false), Category("Layout")]
        public CustomPopupPlacementCallback CustomPopupPlacementCallback
        {
            get { return (CustomPopupPlacementCallback) GetValue(CustomPopupPlacementCallbackProperty); }
            set { SetValue(CustomPopupPlacementCallbackProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the StaysOpen property.
        ///     When false, the tool tip will close on the next mouse click
        ///     Flags:              None
        ///     Default Value:      true
        /// </summary>
        public static readonly DependencyProperty StaysOpenProperty =
                    Popup.StaysOpenProperty.AddOwner(typeof(ToolTip));

        /// <summary>
        ///     Chooses the behavior of when the Popup should automatically close.
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public bool StaysOpen
        {
            get { return (bool) GetValue(StaysOpenProperty); }
            set { SetValue(StaysOpenProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the ShowsToolTipOnKeyboardFocus property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty ShowsToolTipOnKeyboardFocusProperty =
                    ToolTipService.ShowsToolTipOnKeyboardFocusProperty.AddOwner(typeof(ToolTip));

        /// <summary>
        ///     Get or set ShowsToolTipOnKeyboardFocus property of the ToolTip
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public bool? ShowsToolTipOnKeyboardFocus
        {
            get { return (bool?)GetValue(ShowsToolTipOnKeyboardFocusProperty); }
            set { SetValue(ShowsToolTipOnKeyboardFocusProperty, NullableBooleanBoxes.Box(value)); }
        }

        #endregion

        #region Public Events

        /// <summary>
        ///     Opened event
        /// </summary>
        public static readonly RoutedEvent OpenedEvent =
            EventManager.RegisterRoutedEvent("Opened", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ToolTip));

        /// <summary>
        ///     Add/Remove event handler for Opened event
        /// </summary>
        /// <value></value>
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
        ///     Called when the Tooltip is opened. Also raises the OpenedEvent.
        /// </summary>
        /// <param name="e">Generic routed event arguments.</param>
        protected virtual void OnOpened(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     Closed event
        /// </summary>
        public static readonly RoutedEvent ClosedEvent =
            EventManager.RegisterRoutedEvent("Closed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ToolTip));

        /// <summary>
        ///     Add/Remove event handler for Closed event
        /// </summary>
        /// <value></value>
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
        ///     Called when the ToolTip is closed. Also raises the ClosedEvent.
        /// </summary>
        /// <param name="e">Generic routed event arguments.</param>
        protected virtual void OnClosed(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        #endregion

        #region Protected Methods

         /// <summary>
        ///     Change to the correct visual state for the ButtonBase.
        /// </summary>
        /// <param name="useTransitions">
        ///     true to use transitions when updating the visual state, false to
        ///     snap directly to the new visual state.
        /// </param>
        internal override void ChangeVisualState(bool useTransitions)
        {
            if (IsOpen)
            {
                VisualStateManager.GoToState(this, VisualStates.StateOpen, useTransitions);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateClosed, useTransitions);
            }
                        
            base.ChangeVisualState(useTransitions);
        }


        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ToolTipAutomationPeer(this);
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
                throw new InvalidOperationException(SR.Get(SRID.ElementMustBeInPopup, "ToolTip"));
            }
        }

        internal override void OnAncestorChanged()
        {
            base.OnAncestorChanged();

            if (!Popup.IsRootedInPopup(_parentPopup, this))
            {
                throw new InvalidOperationException(SR.Get(SRID.ElementMustBeInPopup, "ToolTip"));
            }
        }

        #endregion

        #region Private Methods

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            PopupControlService popupControlService = PopupControlService.Current;

            // Whenever the tooltip for a control is not an instance of a ToolTip, the framework creates a wrapper 
            // ToolTip instance. Such a ToolTip is tagged ServiceOwned and its Content property is bound to the 
            // ToolTipProperty of the Owner element. So while such a ServiceOwned ToolTip is visible if the 
            // ToolTipProperty on the Owner changes to be a real ToolTip instance then it causes a crash 
            // complaining that the ServiceOwned ToolTip is wrapping another nested ToolTip. The condition here 
            // detects this case and merely dismisses the old ToolTip and displays the new ToolTip instead thus 
            // avoiding the use of a wrapper ToolTip.
            
            if (this == popupControlService.CurrentToolTip &&
                (bool)GetValue(PopupControlService.ServiceOwnedProperty) &&
                newContent is ToolTip)
            {
                popupControlService.ReplaceCurrentToolTip();
            }
            else
            {
                base.OnContentChanged(oldContent, newContent);
            }
        }

        private void HookupParentPopup()
        {
            Debug.Assert(_parentPopup == null, "_parentPopup should be null");

            _parentPopup = new Popup();

            _parentPopup.AllowsTransparency = true;

            // When StaysOpen is true (default), make the popup window WS_EX_Transparent
            // to allow mouse input to go through the tooltip
            _parentPopup.HitTestable = !StaysOpen;

            // Coerce HasDropShadow property in case popup can't be transparent
            CoerceValue(HasDropShadowProperty);

            // Listening to the Opened and Closed events lets us guarantee that
            // the popup is actually opened when we perform those functions.
            _parentPopup.Opened += new EventHandler(OnPopupOpened);
            _parentPopup.Closed += new EventHandler(OnPopupClosed);
            _parentPopup.PopupCouldClose += new EventHandler(OnPopupCouldClose);

            _parentPopup.SetResourceReference(Popup.PopupAnimationProperty, SystemParameters.ToolTipPopupAnimationKey);

            // Hooks up the popup properties from this menu to the popup so that
            // setting them on this control will also set them on the popup.
            Popup.CreateRootPopupInternal(_parentPopup, this, true);
        }

        internal void ForceClose()
        {
            if (_parentPopup != null)
            {
                _parentPopup.ForceClose();
            }
        }

        private void OnPopupCouldClose(object sender, EventArgs e)
        {
            SetCurrentValueInternal(IsOpenProperty, BooleanBoxes.FalseBox);
        }

        private void OnPopupOpened(object source, EventArgs e)
        {
            // Raise Accessibility event
            if (AutomationPeer.ListenerExists(AutomationEvents.ToolTipOpened))
            {
                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(this);
                if (peer != null)
                {
                    // We raise the event async to allow PopupRoot to hookup
                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new DispatcherOperationCallback(delegate(object param)
                    {
                        peer.RaiseAutomationEvent(AutomationEvents.ToolTipOpened);
                        return null;
                    }), null);
                }
            }

            OnOpened(new RoutedEventArgs(OpenedEvent, this));
        }

        private void OnPopupClosed(object source, EventArgs e)
        {
            OnClosed(new RoutedEventArgs(ClosedEvent, this));
        }

        // return the tooltip's bounding rectangle, in screen coords.
        // used by PopupControlService while building the SafeArea
        internal Rect GetScreenRect()
        {
            if (_parentPopup != null)
            {
                return _parentPopup.GetWindowRect();
            }
            else
            {
                return Rect.Empty;
            }
        }

        #endregion

        #region Data

        private Popup _parentPopup;

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
