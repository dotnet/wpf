// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Threading;

using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using MS.Internal;
using MS.Utility;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     The RangeBase class is the base class from which all "range-like"
    /// controls derive.  It defines the relevant events and properties, as
    /// well as providing handlers for the relevant input events.
    /// </summary>
    /// <ExternalAPI/>
    [DefaultEvent("ValueChanged"), DefaultProperty("Value")]
    public abstract class RangeBase : Control
    {
        #region Constructors

        /// <summary>
        /// This is the static constructor for the RangeBase class.  It
        /// hooks the changed notifications needed for visual state changes.
        /// </summary>
        static RangeBase()
        {
            IsEnabledProperty.OverrideMetadata(typeof(RangeBase), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));
            IsMouseOverPropertyKey.OverrideMetadata(typeof(RangeBase), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));
        }

        /// <summary>
        ///     Default RangeBase constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        protected RangeBase()
        {
        }

        #endregion Constructors

        #region Events
        /// <summary>
        /// Event correspond to Value changed event
        /// </summary>
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<double>), typeof(RangeBase));

        /// <summary>
        /// Add / Remove ValueChangedEvent handler
        /// </summary>
        [Category("Behavior")]
        public event RoutedPropertyChangedEventHandler<double> ValueChanged { add { AddHandler(ValueChangedEvent, value); } remove { RemoveHandler(ValueChangedEvent, value); } }
        #endregion Events

        #region Properties
        /// <summary>
        ///     The DependencyProperty for the Minimum property.
        ///     Flags:              none
        ///     Default Value:      0
        /// </summary>
        public static readonly DependencyProperty MinimumProperty = 
                DependencyProperty.Register(
                        "Minimum", 
                        typeof(double), 
                        typeof(RangeBase),
                        new FrameworkPropertyMetadata(
                                0.0d,
                                new PropertyChangedCallback(OnMinimumChanged)),
                        new ValidateValueCallback(IsValidDoubleValue));

        /// <summary>
        ///     Minimum restricts the minimum value of the Value property
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public double Minimum
        {
            get { return (double) GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        /// <summary>
        ///     Called when MinimumProperty is changed on "d."
        /// </summary>
        private static void OnMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RangeBase ctrl = (RangeBase) d;

            RangeBaseAutomationPeer peer = UIElementAutomationPeer.FromElement(ctrl) as RangeBaseAutomationPeer;
            if (peer != null)
            {
                peer.RaiseMinimumPropertyChangedEvent((double)e.OldValue, (double)e.NewValue);
            }

            ctrl.CoerceValue(MaximumProperty);
            ctrl.CoerceValue(ValueProperty);
            ctrl.OnMinimumChanged((double)e.OldValue, (double)e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the Minimum property changes.
        /// </summary>
        /// <param name="oldMinimum">The old value of the Minimum property.</param>
        /// <param name="newMinimum">The new value of the Minimum property.</param>
        protected virtual void OnMinimumChanged(double oldMinimum, double newMinimum)
        {
        }

        /// <summary>
        ///     The DependencyProperty for the Maximum property.
        ///     Flags:              none
        ///     Default Value:      1
        /// </summary>
        public static readonly DependencyProperty MaximumProperty =
                DependencyProperty.Register(
                        "Maximum", 
                        typeof(double), 
                        typeof(RangeBase),
                        new FrameworkPropertyMetadata(
                                1.0d,
                                new PropertyChangedCallback(OnMaximumChanged),
                                new CoerceValueCallback(CoerceMaximum)),
                        new ValidateValueCallback(IsValidDoubleValue));

        private static object CoerceMaximum(DependencyObject d, object value)
        {
            RangeBase ctrl = (RangeBase) d;
            double min = ctrl.Minimum;
            if ((double) value < min)
            {
                return min;
            }
            return value;
        }
        
        /// <summary>
        ///     Maximum restricts the maximum value of the Value property
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public double Maximum
        {
            get { return (double) GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        /// <summary>
        ///     Called when MaximumProperty is changed on "d."
        /// </summary>
        private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RangeBase ctrl = (RangeBase) d;

            RangeBaseAutomationPeer peer = UIElementAutomationPeer.FromElement(ctrl) as RangeBaseAutomationPeer;
            if (peer != null)
            {
                peer.RaiseMaximumPropertyChangedEvent((double)e.OldValue, (double)e.NewValue);
            }

            ctrl.CoerceValue(ValueProperty);
            ctrl.OnMaximumChanged((double) e.OldValue, (double) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the Maximum property changes.
        /// </summary>
        /// <param name="oldMaximum">The old value of the Maximum property.</param>
        /// <param name="newMaximum">The new value of the Maximum property.</param>
        protected virtual void OnMaximumChanged(double oldMaximum, double newMaximum)
        {
        }

        /// <summary>
        ///     The DependencyProperty for the Value property.
        ///     Flags:              None
        ///     Default Value:      0
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
                DependencyProperty.Register(
                        "Value", 
                        typeof(double), 
                        typeof(RangeBase),
                        new FrameworkPropertyMetadata(
                                0.0d,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
                                new PropertyChangedCallback(OnValueChanged),
                                new CoerceValueCallback(ConstrainToRange)),
                        new ValidateValueCallback(IsValidDoubleValue));

        // made this internal because Slider wants to leverage it
        internal static object ConstrainToRange(DependencyObject d, object value)
        {
            RangeBase ctrl = (RangeBase) d;
            double min = ctrl.Minimum;
            double v = (double) value;
            if (v < min)
            {
                return min;
            }

            double max = ctrl.Maximum;
            if (v > max)
            {
                return max;
            }
            
            return value;
        }
        
        /// <summary>
        ///     Value property
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public double Value
        {
            get { return (double) GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        ///     Called when ValueID is changed on "d."
        /// </summary>
        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RangeBase ctrl = (RangeBase)d;

            RangeBaseAutomationPeer peer = UIElementAutomationPeer.FromElement(ctrl) as RangeBaseAutomationPeer;
            if (peer != null)
            {
                peer.RaiseValuePropertyChangedEvent((double)e.OldValue, (double)e.NewValue);
            }

            ctrl.OnValueChanged((double) e.OldValue, (double) e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the Value property changes.
        /// </summary>
        /// <param name="oldValue">The old value of the Value property.</param>
        /// <param name="newValue">The new value of the Value property.</param>
        protected virtual void OnValueChanged(double oldValue, double newValue)
        {
            RoutedPropertyChangedEventArgs<double> args = new RoutedPropertyChangedEventArgs<double>(oldValue, newValue);
            args.RoutedEvent=RangeBase.ValueChangedEvent;
            RaiseEvent(args);
        }

        /// <summary>
        /// Validate input value in RangeBase (Minimum, Maximum, and Value).
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns False if value is NaN or NegativeInfinity or PositiveInfinity. Otherwise, returns True.</returns>
        private static bool IsValidDoubleValue(object value)
        {
            double d = (double)value;

            return !(DoubleUtil.IsNaN(d) || double.IsInfinity(d));
        }

        /// <summary>
        /// Validate input value in RangeBase (SmallChange and LargeChange).
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns False if value is NaN or NegativeInfinity or PositiveInfinity or negative. Otherwise, returns True.</returns>
        private static bool IsValidChange(object value)
        {
            double d = (double)value;

            return IsValidDoubleValue(value) && d >= 0.0;
        }

        #region LargeChange Property
        /// <summary>
        ///     The DependencyProperty for the LargeChange property.
        /// </summary>
        public static readonly DependencyProperty LargeChangeProperty 
            = DependencyProperty.Register("LargeChange", typeof(double), typeof(RangeBase), 
                                          new FrameworkPropertyMetadata(1.0),
                                          new ValidateValueCallback(IsValidChange));

        /// <summary>
        ///     LargeChange property
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public double LargeChange
        {
            get
            {
                return (double)GetValue(LargeChangeProperty);
            }
            set
            {
                SetValue(LargeChangeProperty, value);
            }
        }
        
        #endregion

        #region SmallChange Property
        /// <summary>
        ///     The DependencyProperty for the SmallChange property.
        /// </summary>
        public static readonly DependencyProperty SmallChangeProperty 
            = DependencyProperty.Register("SmallChange", typeof(double), typeof(RangeBase), 
                                          new FrameworkPropertyMetadata(0.1),
                                          new ValidateValueCallback(IsValidChange));

        /// <summary>
        ///     SmallChange property
        /// </summary>
        [Bindable(true), Category("Behavior")]
        public double SmallChange
        {
            get
            {
                return (double)GetValue(SmallChangeProperty);
            }
            set
            {
                SetValue(SmallChangeProperty, value);
            }
        }
        
        #endregion
        
        #endregion

        #region Method Overrides

        internal override void ChangeVisualState(bool useTransitions)
        {
            if (!IsEnabled)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateDisabled, VisualStates.StateNormal);
            }
            else if (IsMouseOver)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateMouseOver, VisualStates.StateNormal);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateNormal, useTransitions);
            }

            if (IsKeyboardFocused)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateFocused, VisualStates.StateUnfocused);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateUnfocused, useTransitions);
            }

            base.ChangeVisualState(useTransitions);
        }

        /// <summary>
        ///     Gives a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            string typeText = this.GetType().ToString();
            double min = double.NaN;
            double max = double.NaN;
            double val = double.NaN;
            bool valuesDefined = false;

            // Accessing RangeBase properties may be thread sensitive
            if (CheckAccess())
            {
                min = Minimum;
                max = Maximum;
                val = Value;
                valuesDefined = true;
            }
            else
            {
                //Not on dispatcher, try posting to the dispatcher with 20ms timeout
                Dispatcher.Invoke(DispatcherPriority.Send, new TimeSpan(0, 0, 0, 0, 20), new DispatcherOperationCallback(delegate(object o)
                {
                    min = Minimum;
                    max = Maximum;
                    val = Value;
                    valuesDefined = true;
                    return null;
                }), null);
            }

            // If min, max, value are defined
            if (valuesDefined)
            {
                return SR.Get(SRID.ToStringFormatString_RangeBase, typeText, min, max, val);
            }

            // Not able to access the dispatcher
            return typeText;
        }

        #endregion
    }
}


