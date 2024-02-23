// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Windows.Threading;

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using MS.Utility;
using MS.Internal.KnownBoxes;
using MS.Internal.Telemetry.PresentationFramework;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Represents the standard button component that inherently reacts to the Click event.
    /// The Button control is one of the most basic forms of user interface (UI).
    /// </summary>
    public class Button: ButtonBase
    {
        #region Constructors

        static Button()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Button), new FrameworkPropertyMetadata(typeof(Button)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(Button));

            // WORKAROUND: the following if statement is a workaround to get the ButtonBase cctor to run before we
            // override metadata.
            if (ButtonBase.CommandProperty != null)
            {
                IsEnabledProperty.OverrideMetadata(typeof(Button), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnIsEnabledChanged)));
            }

            ControlsTraceLogger.AddControl(TelemetryControls.Button);
        }

        /// <summary>
        ///     Default Button constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public Button() : base()
        {
        }

        #endregion

        #region Properties

        #region IsDefault

        /// <summary>
        ///     The DependencyProperty for the IsDefault property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsDefaultProperty
            = DependencyProperty.Register("IsDefault", typeof(bool), typeof(Button),
                                          new FrameworkPropertyMetadata(BooleanBoxes.FalseBox,
                                                                        new PropertyChangedCallback(OnIsDefaultChanged)));

        /// <summary>
        /// Specifies whether or not this button is the default button.
        /// </summary>
        /// <value></value>
        public bool IsDefault
        {
            get { return (bool) GetValue(IsDefaultProperty); } 
            set { SetValue(IsDefaultProperty, BooleanBoxes.Box(value)); }
        }

        private static void OnIsDefaultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) 
        { 
            Button b = d as Button;
            KeyboardFocusChangedEventHandler focusChangedEventHandler = FocusChangedEventHandlerField.GetValue(b);
            if (focusChangedEventHandler == null)
            {
                focusChangedEventHandler = new KeyboardFocusChangedEventHandler(b.OnFocusChanged);
                FocusChangedEventHandlerField.SetValue(b, focusChangedEventHandler);
            }
            
            if ((bool) e.NewValue)
            {
                AccessKeyManager.Register("\x000D", b);
                KeyboardNavigation.Current.FocusChanged += focusChangedEventHandler;
                b.UpdateIsDefaulted(Keyboard.FocusedElement);
            }
            else
            {
                AccessKeyManager.Unregister("\x000D", b);
                KeyboardNavigation.Current.FocusChanged -= focusChangedEventHandler;
                b.UpdateIsDefaulted(null);
            }
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // This value is cached in FE, so all we have to do here is look at the new value
            Button b = ((Button)d);

            // If it's not a default button we don't need to update the IsDefaulted property
            if (b.IsDefault)
            {
                b.UpdateIsDefaulted(Keyboard.FocusedElement);
            }
        }

        #endregion

        #region IsCancel

        /// <summary>
        ///     The DependencyProperty for the IsCancel property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsCancelProperty =
                DependencyProperty.Register(
                        "IsCancel", 
                        typeof(bool), 
                        typeof(Button),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox,
                                new PropertyChangedCallback(OnIsCancelChanged)));

        /// <summary>
        /// Specifies whether or not this button is the cancel button.
        /// </summary>
        /// <value></value>
        public bool IsCancel
        {
            get { return (bool) GetValue(IsCancelProperty); }
            set { SetValue(IsCancelProperty, BooleanBoxes.Box(value)); }
        }

        private static void OnIsCancelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Button b = d as Button;
            if ((bool) e.NewValue)
            {
                AccessKeyManager.Register("\x001B", b);
            }
            else
            {
                AccessKeyManager.Unregister("\x001B", b);
            }
        }

        #endregion

        #region IsDefaulted

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey IsDefaultedPropertyKey
            = DependencyProperty.RegisterReadOnly("IsDefaulted", typeof(bool), typeof(Button),
                                          new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        ///     The DependencyProperty for the IsDefaulted property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsDefaultedProperty
            = IsDefaultedPropertyKey.DependencyProperty;

        /// <summary>
        /// Specifies whether or not this button is the button that would be invoked when Enter is pressed.
        /// </summary>
        /// <value></value>
        public bool IsDefaulted
        {
            get
            {
                return (bool)GetValue(IsDefaultedProperty);
            }
        }

        #endregion

        #endregion

        #region Private helpers

        private void OnFocusChanged(object sender, KeyboardFocusChangedEventArgs e)
        {
            UpdateIsDefaulted(Keyboard.FocusedElement);
        }

        private void UpdateIsDefaulted(IInputElement focus)
        {
            // If it's not a default button, or nothing is focused, or it's disabled then it's not defaulted.
            if (!IsDefault || focus == null || !IsEnabled)
            {
                SetValue(IsDefaultedPropertyKey, BooleanBoxes.FalseBox);
                return;
            }

            DependencyObject focusDO = focus as DependencyObject;
            object thisScope, focusScope;

            // If the focused thing is not in this scope then IsDefaulted = false
            AccessKeyPressedEventArgs e;

            object isDefaulted = BooleanBoxes.FalseBox;
            try
            {
                // Step 1: Determine the AccessKey scope from currently focused element
                e = new AccessKeyPressedEventArgs();
                focus.RaiseEvent(e);
                focusScope = e.Scope;

                // Step 2: Determine the AccessKey scope from this button
                e = new AccessKeyPressedEventArgs();
                this.RaiseEvent(e);
                thisScope = e.Scope;

                // Step 3: Compare scopes
                if (thisScope == focusScope && (focusDO == null || (bool)focusDO.GetValue(KeyboardNavigation.AcceptsReturnProperty) == false))
                {
                    isDefaulted = BooleanBoxes.TrueBox;
                }
            }
            finally
            {
                SetValue(IsDefaultedPropertyKey, isDefaulted);
            }
        }

        #endregion Private helpers

        #region Override methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() 
        {
            return new System.Windows.Automation.Peers.ButtonAutomationPeer(this);
        }

        /// <summary>
        /// This method is called when button is clicked.
        /// </summary>
        protected override void OnClick()
        {
            if (AutomationPeer.ListenerExists(AutomationEvents.InvokePatternOnInvoked))
            {
                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(this);
                if (peer != null)
                    peer.RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
            }

            // base.OnClick should be called first. 
            // Our default command for Cancel Button to close dialog should happen 
            // after Button's click event handler has been called.
            // If there is excption and it's a Cancel button and RoutedCommand is null, 
            // We will raise Window.DialogCancelCommand.
            try
            {
                base.OnClick();
            }
            finally
            {
                // When the Button RoutedCommand is null, if it's a Cancel Button, Window.DialogCancelCommand will
                // be the default command. Do not assign Window.DialogCancelCommand to Button.Command.
                // If in Button click handler user nulls the Command, we still want to provide the default behavior.
                if ((Command == null) && IsCancel)
                {
                    // Can't invoke Window.DialogCancelCommand directly. Have to raise event.
                    // Filed bug 936090: Commanding perf issue: can't directly invoke a command.
                    MS.Internal.Commands.CommandHelpers.ExecuteCommand(Window.DialogCancelCommand, null, this);
                }
            }
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

        #endregion

        #region Data

        // This field is used to hang on to the event handler that we 
        // hand out to KeyboardNavigation.  On the KeyNav side it's tracked
        // as a WeakReference so when we hand it out we need to make sure 
        // that we hold a strong reference ourselves.  We only need this
        // handler when we are a Default button (very uncommon).
        private static readonly UncommonField<KeyboardFocusChangedEventHandler> FocusChangedEventHandlerField = new UncommonField<KeyboardFocusChangedEventHandler>();

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
