// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;

using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using MS.Internal.KnownBoxes;

// Disable CS3001: Warning as Error: not CLS-compliant
#pragma warning disable 3001

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     ToggleButton
    /// </summary>
    [DefaultEvent("Checked")]
    public class ToggleButton : ButtonBase
    {
        #region Constructors

        static ToggleButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToggleButton), new FrameworkPropertyMetadata(typeof(ToggleButton)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(ToggleButton));
        }

        /// <summary>
        ///     Default ToggleButton constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public ToggleButton() : base()
        {
        }
        #endregion

        #region Properties and Events

        /// <summary>
        ///     Checked event
        /// </summary>
        public static readonly RoutedEvent CheckedEvent = EventManager.RegisterRoutedEvent("Checked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ToggleButton));

        /// <summary>
        ///     Unchecked event
        /// </summary>
        public static readonly RoutedEvent UncheckedEvent = EventManager.RegisterRoutedEvent("Unchecked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ToggleButton));

        /// <summary>
        ///     Indeterminate event
        /// </summary>
        public static readonly RoutedEvent IndeterminateEvent = EventManager.RegisterRoutedEvent("Indeterminate", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ToggleButton));

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
        ///     Add / Remove Indeterminate handler
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler Indeterminate
        {
            add
            {
                AddHandler(IndeterminateEvent, value);
            }

            remove
            {
                RemoveHandler(IndeterminateEvent, value);
            }
        }

        /// <summary>
        ///     The DependencyProperty for the IsChecked property.
        ///     Flags:              BindsTwoWayByDefault
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsCheckedProperty =
                DependencyProperty.Register(
                        "IsChecked",
                        typeof(bool?),
                        typeof(ToggleButton),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
                                new PropertyChangedCallback(OnIsCheckedChanged)));

        /// <summary>
        ///     Indicates whether the ToggleButton is checked
        /// </summary>
        [Category("Appearance")]
        [TypeConverter(typeof(NullableBoolConverter))]
        [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
        public bool? IsChecked
        {
            get
            {
                // Because Nullable<bool> unboxing is very slow (uses reflection) first we cast to bool
                object value = GetValue(IsCheckedProperty);
                if (value == null)
                    return new Nullable<bool>();
                else
                    return new Nullable<bool>((bool)value);
            }
            set
            {
                SetValue(IsCheckedProperty, value.HasValue ? BooleanBoxes.Box(value.Value) : null);
            }
        }

        private static object OnGetIsChecked(DependencyObject d) {return ((ToggleButton)d).IsChecked;}

        /// <summary>
        ///     Called when IsChecked is changed on "d."
        /// </summary>
        /// <param name="d">The object on which the property was changed.</param>
        /// <param name="e">EventArgs that contains the old and new values for this property</param>
        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ToggleButton button = (ToggleButton)d;
            bool? oldValue = (bool?) e.OldValue;
            bool? newValue = (bool?) e.NewValue;

            //doing soft casting here because the peer can be that of RadioButton and it is not derived from
            //ToggleButtonAutomationPeer - specifically to avoid implementing TogglePattern
            ToggleButtonAutomationPeer peer = UIElementAutomationPeer.FromElement(button) as ToggleButtonAutomationPeer;
            if (peer != null)
            {
                peer.RaiseToggleStatePropertyChangedEvent(oldValue, newValue);
            }

            if (newValue == true)
            {
                button.OnChecked(new RoutedEventArgs(CheckedEvent));
            }
            else if (newValue == false)
            {
                button.OnUnchecked(new RoutedEventArgs(UncheckedEvent));
            }
            else
            {
                button.OnIndeterminate(new RoutedEventArgs(IndeterminateEvent));
            }

            button.UpdateVisualState();
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
        ///     Called when IsChecked becomes null.
        /// </summary>
        /// <param name="e">Event arguments for the routed event that is raised by the default implementation of this method.</param>
        protected virtual void OnIndeterminate(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        ///     The DependencyProperty for the IsThreeState property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsThreeStateProperty =
                DependencyProperty.Register(
                        "IsThreeState",
                        typeof(bool),
                        typeof(ToggleButton),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        ///     The IsThreeState property determines whether the control supports two or three states.
        ///     IsChecked property can be set to null as a third state when IsThreeState is true
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public bool IsThreeState
        {
            get { return (bool) GetValue(IsThreeStateProperty); }
            set { SetValue(IsThreeStateProperty, BooleanBoxes.Box(value)); }
        }

        #endregion

        #region Override methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ToggleButtonAutomationPeer(this);
        }

        /// <summary>
        /// This override method is called when the control is clicked by mouse or keyboard
        /// </summary>
        protected override void OnClick()
        {
            OnToggle();
            base.OnClick();
        }

        #endregion

        #region Method Overrides

        internal override void ChangeVisualState(bool useTransitions)
        {
            base.ChangeVisualState(useTransitions);

            // Update the Check state group
            var isChecked = IsChecked;
            if (isChecked == true)
            {
                VisualStateManager.GoToState(this, VisualStates.StateChecked, useTransitions);
            }
            else if (isChecked == false)
            {
                VisualStateManager.GoToState(this, VisualStates.StateUnchecked, useTransitions);
            }
            else
            {
                // isChecked is null
                VisualStates.GoToState(this, useTransitions, VisualStates.StateIndeterminate, VisualStates.StateUnchecked);
            }
        }

        /// <summary>
        ///     Gives a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            string typeText = this.GetType().ToString();
            string contentText = String.Empty;
            bool? isChecked = false;
            bool valuesDefined = false;

            // Accessing ToggleButton properties may be thread sensitive
            if (CheckAccess())
            {
                contentText = GetPlainText();
                isChecked = IsChecked;
                valuesDefined = true;
            }
            else
            {
                //Not on dispatcher, try posting to the dispatcher with 20ms timeout
                Dispatcher.Invoke(DispatcherPriority.Send, new TimeSpan(0, 0, 0, 0, 20), new DispatcherOperationCallback(delegate(object o)
                {
                    contentText = GetPlainText();
                    isChecked = IsChecked;
                    valuesDefined = true;
                    return null;
                }), null);
            }

            // If Content and isChecked are defined
            if (valuesDefined)
            {
                return SR.Get(SRID.ToStringFormatString_ToggleButton, typeText, contentText, isChecked.HasValue ? isChecked.Value.ToString() : "null");
            }

            // Not able to access the dispatcher
            return typeText;
        }

        #endregion

        #region Protected virtual methods

        /// <summary>
        /// This vitrual method is called from OnClick(). ToggleButton toggles IsChecked property.
        /// Subclasses can override this method to implement their own toggle behavior
        /// </summary>
        protected internal virtual void OnToggle()
        {
            // If IsChecked == true && IsThreeState == true   --->  IsChecked = null
            // If IsChecked == true && IsThreeState == false  --->  IsChecked = false
            // If IsChecked == false                          --->  IsChecked = true
            // If IsChecked == null                           --->  IsChecked = false
            bool? isChecked;
            if (IsChecked == true)
                isChecked = IsThreeState ? (bool?)null : (bool?)false;
            else // false or null
                isChecked = IsChecked.HasValue; // HasValue returns true if IsChecked==false
            SetCurrentValueInternal(IsCheckedProperty, isChecked);
        }

        #endregion

        #region Data

        #endregion

        #region Accessibility

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
