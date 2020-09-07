// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using System.Runtime.InteropServices;

using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Media;

using MS.Utility;
using MS.Win32;
using MS.Internal;
using MS.Internal.PresentationFramework;                   // SafeSecurityHelper

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     The thumb control enables basic drag-movement functionality for scrollbars and window resizing widgets.
    /// </summary>
    /// <remarks>
    ///     The thumb can receive mouse focus but it cannot receive keyboard focus.
    /// As well, there is no threshhold at which the control stops firing its DragDeltaEvent.
    /// Once in mouse capture, the DragDeltaEvent fires until the mouse button is released.
    /// </remarks>
    [DefaultEvent("DragDelta")]
    [Localizability(LocalizationCategory.NeverLocalize)]
    public class Thumb : Control
    {
        #region Constructors

        /// <summary>
        ///     Default Thumb constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public Thumb() : base()
        {
        }

        static Thumb()
        {
            // Register metadata for dependency properties
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Thumb), new FrameworkPropertyMetadata(typeof(Thumb)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(Thumb));
            FocusableProperty.OverrideMetadata(typeof(Thumb), new FrameworkPropertyMetadata(MS.Internal.KnownBoxes.BooleanBoxes.FalseBox));

            EventManager.RegisterClassHandler(typeof(Thumb), Mouse.LostMouseCaptureEvent, new MouseEventHandler(OnLostMouseCapture));

            IsEnabledProperty.OverrideMetadata(typeof(Thumb), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));
            IsMouseOverPropertyKey.OverrideMetadata(typeof(Thumb), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualStatePropertyChanged)));
        }

        #endregion

        #region Properties and Events

        /// <summary>
        ///     Event fires when user press mouse's left button on the thumb.
        /// </summary>
        public static readonly RoutedEvent DragStartedEvent = EventManager.RegisterRoutedEvent("DragStarted", RoutingStrategy.Bubble, typeof(DragStartedEventHandler), typeof(Thumb));

        /// <summary>
        ///     Event fires when the thumb is in a mouse capture state and the user moves the mouse around.
        /// </summary>
        public static readonly RoutedEvent DragDeltaEvent = EventManager.RegisterRoutedEvent("DragDelta", RoutingStrategy.Bubble, typeof(DragDeltaEventHandler), typeof(Thumb));

        /// <summary>
        ///     Event fires when user released mouse's left button or when CancelDrag method is called.
        /// </summary>
        public static readonly RoutedEvent DragCompletedEvent = EventManager.RegisterRoutedEvent("DragCompleted", RoutingStrategy.Bubble, typeof(DragCompletedEventHandler), typeof(Thumb));

        /// <summary>
        /// Add / Remove DragStartedEvent handler
        /// </summary>
        [Category("Behavior")]
        public event DragStartedEventHandler DragStarted { add { AddHandler(DragStartedEvent, value); } remove { RemoveHandler(DragStartedEvent, value); } }

        /// <summary>
        /// Add / Remove DragDeltaEvent handler
        /// </summary>
        [Category("Behavior")]
        public event DragDeltaEventHandler DragDelta { add { AddHandler(DragDeltaEvent, value); } remove { RemoveHandler(DragDeltaEvent, value); } }

        /// <summary>
        /// Add / Remove DragCompletedEvent handler
        /// </summary>
        [Category("Behavior")]
        public event DragCompletedEventHandler DragCompleted { add { AddHandler(DragCompletedEvent, value); } remove { RemoveHandler(DragCompletedEvent, value); } }


        private static readonly DependencyPropertyKey IsDraggingPropertyKey = 
                DependencyProperty.RegisterReadOnly(
                        "IsDragging", 
                        typeof(bool), 
                        typeof(Thumb),
                        new FrameworkPropertyMetadata(
                                MS.Internal.KnownBoxes.BooleanBoxes.FalseBox,
                                new PropertyChangedCallback(OnIsDraggingPropertyChanged)));

        /// <summary>
        ///     DependencyProperty for the IsDragging property.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty IsDraggingProperty = IsDraggingPropertyKey.DependencyProperty;

        /// <summary>
        ///     IsDragging indicates that left mouse button is pressed over the thumb.
        /// </summary>
        [Bindable(true), Browsable(false), Category("Appearance")]
        public bool IsDragging
        {
            get { return (bool) GetValue(IsDraggingProperty); }
            protected set { SetValue(IsDraggingPropertyKey, MS.Internal.KnownBoxes.BooleanBoxes.Box(value)); }
        }

        #endregion Properties and Events

        /// <summary>
        ///     Called when IsDraggingProperty is changed on "d."
        /// </summary>
        /// <param name="d">The object on which the property was changed.</param>
        /// <param name="e">EventArgs that contains the old and new values for this property</param>
        private static void OnIsDraggingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thumb = (Thumb)d;
            thumb.OnDraggingChanged(e);
            thumb.UpdateVisualState();
        }

        #region Public methods

        /// <summary>
        ///     This method cancels the dragging operation.
        /// </summary>
        public void CancelDrag()
        {
            if (IsDragging)
            {
                if (IsMouseCaptured)
                {
                    ReleaseMouseCapture();
                }
                ClearValue(IsDraggingPropertyKey);
                RaiseEvent(new DragCompletedEventArgs(_previousScreenCoordPosition.X - _originScreenCoordPosition.X, _previousScreenCoordPosition.Y - _originScreenCoordPosition.Y, true));
            }
        }

        #endregion Public methods


        #region Virtual methods

        /// <summary>
        ///     This method is invoked when the IsDragging property changes.
        /// </summary>
        /// <param name="e">DependencyPropertyChangedEventArgs for IsDragging property.</param>
        protected virtual void OnDraggingChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        #endregion Virtual methods

        #region Override methods

        /// <summary>
        ///     Change to the correct visual state for the ButtonBase.
        /// </summary>
        /// <param name="useTransitions">
        ///     true to use transitions when updating the visual state, false to
        ///     snap directly to the new visual state.
        /// </param>
        internal override void ChangeVisualState(bool useTransitions)
        {
            // See ButtonBase.ChangeVisualState.
            // This method should be exactly like it, except we use IsDragging instead of IsPressed for the pressed state
            if (!IsEnabled)
            {
                VisualStateManager.GoToState(this, VisualStates.StateDisabled, useTransitions);
            }
            else if (IsDragging)
            {
                VisualStateManager.GoToState(this, VisualStates.StatePressed, useTransitions);
            }
            else if (IsMouseOver)
            {
                VisualStateManager.GoToState(this, VisualStates.StateMouseOver, useTransitions);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateNormal, useTransitions);
            }

            if (IsKeyboardFocused)
            {
                VisualStateManager.GoToState(this, VisualStates.StateFocused, useTransitions);
            }
            else
            {
                VisualStateManager.GoToState(this, VisualStates.StateUnfocused, useTransitions);
            }

            base.ChangeVisualState(useTransitions);
        }

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer() 
        {
            return new ThumbAutomationPeer(this);
        }

        /// <summary>
        /// This is the method that responds to the MouseButtonEvent event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!IsDragging)
            {
                e.Handled = true;
                Focus();
                CaptureMouse();
                SetValue(IsDraggingPropertyKey, true);
                _originThumbPoint = e.GetPosition(this);
                _previousScreenCoordPosition = _originScreenCoordPosition = SafeSecurityHelper.ClientToScreen(this,_originThumbPoint);
                bool exceptionThrown = true;
                try
                {
                    RaiseEvent(new DragStartedEventArgs(_originThumbPoint.X, _originThumbPoint.Y));
                    exceptionThrown = false;
                }
                finally
                {
                    if (exceptionThrown)
                    {
                        CancelDrag();
                    }
                }
            }
            else
            {
                // This is weird, Thumb shouldn't get MouseLeftButtonDown event while dragging.
                // This may be the case that something ate MouseLeftButtonUp event, so Thumb never had a chance to
                // reset IsDragging property
                Debug.Assert(false,"Got MouseLeftButtonDown event while dragging!");
            }
            base.OnMouseLeftButtonDown(e);
        }


        /// <summary>
        /// This is the method that responds to the MouseButtonEvent event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (IsMouseCaptured && IsDragging)
            {
                e.Handled = true;
                ClearValue(IsDraggingPropertyKey);
                ReleaseMouseCapture();
                Point pt = SafeSecurityHelper.ClientToScreen(this, e.MouseDevice.GetPosition(this));
                RaiseEvent(new DragCompletedEventArgs(pt.X - _originScreenCoordPosition.X, pt.Y - _originScreenCoordPosition.Y, false));
            }
            base.OnMouseLeftButtonUp(e);
        }

        // Cancel Drag if we lost capture
        private static void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            Thumb thumb = (Thumb)sender;

            if (Mouse.Captured != thumb)
            {
                thumb.CancelDrag();
            }
        }

        /// <summary>
        /// This is the method that responds to the MouseEvent event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (IsDragging)
            {
                if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
                {
                    Point thumbCoordPosition = e.GetPosition(this);
                    // Get client point then convert to screen point
                    Point screenCoordPosition = SafeSecurityHelper.ClientToScreen(this, thumbCoordPosition);

                    // We will fire DragDelta event only when the mouse is really moved
                    if (screenCoordPosition != _previousScreenCoordPosition)
                    {
                        _previousScreenCoordPosition = screenCoordPosition;
                        e.Handled = true;
                        RaiseEvent(new DragDeltaEventArgs(thumbCoordPosition.X - _originThumbPoint.X,
                                                          thumbCoordPosition.Y - _originThumbPoint.Y));
                    }
                }
                else
                {
                    if (e.MouseDevice.Captured == this)
                        ReleaseMouseCapture();
                    ClearValue(IsDraggingPropertyKey);
                    _originThumbPoint.X = 0;
                    _originThumbPoint.Y = 0;
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
            get { return 19; }
        }

        #endregion

        #region Data

        /// <summary>
        /// The point where the mouse was clicked down (Thumb's co-ordinate).
        /// </summary>
        private Point _originThumbPoint; //

        /// <summary>
        /// The position of the mouse (screen co-ordinate) where the mouse was clicked down.
        /// </summary>
        private Point _originScreenCoordPosition;

        /// <summary>
        /// The position of the mouse (screen co-ordinate) when the previous DragDelta event was fired
        /// </summary>
        private Point _previousScreenCoordPosition;

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

