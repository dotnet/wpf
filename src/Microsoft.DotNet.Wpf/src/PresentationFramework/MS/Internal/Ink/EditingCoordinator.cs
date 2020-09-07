// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
// Coordinates editing for InkCanvas
// Features:
//


using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Navigation;
using System.Windows.Media;
using MS.Utility;

namespace MS.Internal.Ink
{
    /// <summary>
    /// Internal class that represents the editing stack of InkCanvas
    /// Please see the design detain at http://tabletpc/longhorn/Specs/Mid-Stroke%20and%20Pen%20Cursor%20Dev%20Design.mht
    /// </summary>
    internal class EditingCoordinator
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructors
        /// </summary>
        /// <param name="inkCanvas">InkCanvas instance</param>
        internal EditingCoordinator(InkCanvas inkCanvas)
        {
            if (inkCanvas == null)
            {
                throw new ArgumentNullException("inkCanvas");
            }

            _inkCanvas = inkCanvas;

            // Create a stack for tracking the behavior
            _activationStack = new Stack<EditingBehavior>(2);

            // 
            // listen to in-air move so that we could get notified when Stylus is inverted.
            //
            _inkCanvas.AddHandler(Stylus.StylusInRangeEvent, new StylusEventHandler(OnInkCanvasStylusInAirOrInRangeMove));
            _inkCanvas.AddHandler(Stylus.StylusInAirMoveEvent, new StylusEventHandler(OnInkCanvasStylusInAirOrInRangeMove));
            _inkCanvas.AddHandler(Stylus.StylusOutOfRangeEvent, new StylusEventHandler(OnInkCanvasStylusOutOfRange));
        }

        #endregion Constructors

        //-------------------------------------------------------------------------------
        //
        // Internal Methods
        //
        //-------------------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Activate a dynamic behavior
        /// Called from:
        ///     SelectionEditor.OnCanvasMouseMove
        /// </summary>
        /// <param name="dynamicBehavior"></param>
        /// <param name="inputDevice"></param>
        internal void ActivateDynamicBehavior(EditingBehavior dynamicBehavior, InputDevice inputDevice)
        {
            // Only SelectionEditor should be enable to initiate dynamic behavior
            Debug.Assert(ActiveEditingBehavior == SelectionEditor,
                            "Only SelectionEditor should be able to initiate dynamic behavior");

            DebugCheckDynamicBehavior(dynamicBehavior);

            // Push the dynamic behavior.
            PushEditingBehavior(dynamicBehavior);

            // If this is LassoSelectionBehavior, we should capture the current stylus and feed the cached 
            if ( dynamicBehavior == LassoSelectionBehavior )
            {
                bool fSucceeded = false;

                // The below code might call out StrokeErasing or StrokeErased event.
                // The out-side code could throw exception. 
                // We use try/finally block to protect our status.
                try
                {
                    //we pass true for userInitiated because we've simply consulted the InputDevice
                    //(and only StylusDevice or MouseDevice) for the current position of the device
                    InitializeCapture(inputDevice, (IStylusEditing)dynamicBehavior, 
                        true /*userInitiated*/, false/*Don't reset the RTI*/);
                    fSucceeded = true;
                }
                finally
                {
                    if ( !fSucceeded )
                    {
                        // Abort the current editing.
                        ActiveEditingBehavior.Commit(false);

                        // Release capture and do clean-up.
                        ReleaseCapture(true);
                    }
                }
            }

            _inkCanvas.RaiseActiveEditingModeChanged(new RoutedEventArgs(InkCanvas.ActiveEditingModeChangedEvent, _inkCanvas));
        }

        /// <summary>
        /// Deactivate a dynamic behavior
        /// Called from:
        ///     EditingBehavior.Commit
        /// </summary>
        internal void DeactivateDynamicBehavior()
        {
            // Make sure we are under correct state:
            // ActiveEditingBehavior has to be either LassoSelectionBehavior or SelectionEditingBehavior.
            DebugCheckDynamicBehavior(ActiveEditingBehavior);

            // Pop the dynamic behavior.
            PopEditingBehavior();
        }

        /// <summary>
        /// Update the current active EditingMode
        /// Called from:
        ///     EditingCoordinator.UpdateInvertedState
        ///     InkCanvas.Initialize()
        /// </summary>
        internal void UpdateActiveEditingState()
        {
            UpdateEditingState(_stylusIsInverted);
        }

        /// <summary>
        /// Update the EditingMode
        /// Called from:
        ///     InkCanvas.RaiseEditingModeChanged
        ///     InkCanvas.RaiseEditingModeInvertedChanged
        /// </summary>
        /// <param name="inverted">A flage which indicates whether the new mode is for the Inverted state</param>
        internal void UpdateEditingState(bool inverted)
        {
            // If the mode is inactive currently, we should skip the update.
            if ( inverted != _stylusIsInverted )
            {
                return;
            }

            // Get the current behavior and the new behavior.
            EditingBehavior currentBehavior = ActiveEditingBehavior;
            EditingBehavior newBehavior = GetBehavior(ActiveEditingMode);

            // Check whether the user is editing.
            if ( UserIsEditing )
            {
                // Check if we are in the mid-stroke.
                if ( IsInMidStroke )
                {
                    // We are in mid-stroke now.

                    Debug.Assert(currentBehavior is StylusEditingBehavior,
                                    "The current behavoir has to be one of StylusEditingBehaviors");
                    ( (StylusEditingBehavior)currentBehavior ).SwitchToMode(ActiveEditingMode);
                }
                else
                {
                    if ( currentBehavior == SelectionEditingBehavior )
                    {
                        // Commit the current moving/resizing behavior
                        currentBehavior.Commit(true);
                    }

                    ChangeEditingBehavior(newBehavior);
                }
            }
            else
            {
                // Check if we are in the mid-stroke.
                if ( IsInMidStroke )
                {
                    // We are in mid-stroke now.

                    Debug.Assert(currentBehavior is StylusEditingBehavior,
                                    "The current behavoir has to be one of StylusEditingBehaviors");
                    ( (StylusEditingBehavior)currentBehavior ).SwitchToMode(ActiveEditingMode);
                }
                else
                {
                    // Make sure we are under correct state:
                    // currentBehavior cannot be any of Dynamic Behavior.
                    DebugCheckNonDynamicBehavior(currentBehavior);
                    ChangeEditingBehavior(newBehavior);
                }
            }

            _inkCanvas.UpdateCursor();
        }

        /// <summary>
        /// Update the PointEraerCursor
        /// Called from:
        ///     InkCanvas.set_EraserShape
        /// </summary>
        internal void UpdatePointEraserCursor()
        {
            // We only have to update the point eraser when the active mode is EraseByPoint
            // In other case, EraseBehavior will update cursor properly in its OnActivate routine.
            if ( ActiveEditingMode == InkCanvasEditingMode.EraseByPoint )
            {
                InvalidateBehaviorCursor(EraserBehavior);
            }
        }

        /// <summary>
        /// InvalidateTransform
        ///     Called by: InkCanvas.OnPropertyChanged
        /// </summary>
        internal void InvalidateTransform()
        {
            // We only have to invalidate transform flags for InkCollectionBehavior and EraserBehavior for now.
            SetTransformValid(InkCollectionBehavior, false);
            SetTransformValid(EraserBehavior, false);
        }

        /// <summary>
        /// Invalidate the behavior cursor
        /// Call from:
        ///     EditingCoordinator.UpdatePointEraserCursor
        ///     EraserBehavior.OnActivate
        ///     InkCollectionBehavior.OnInkCanvasDefaultDrawingAttributesReplaced
        ///     InkCollectionBehavior.OnInkCanvasDefaultDrawingAttributesChanged
        ///     SelectionEditingBehavior.OnActivate
        ///     SelectionEditor.UpdateCursor(InkCanvasSelectionHandle handle)
        /// </summary>
        /// <param name="behavior">the behavior which its cursor needs to be updated.</param>
        internal void InvalidateBehaviorCursor(EditingBehavior behavior)
        {
            // Should never be null
            Debug.Assert(behavior != null);

            // InvalidateCursor first
            SetCursorValid(behavior, false);

            if ( !GetTransformValid(behavior) )
            {
                behavior.UpdateTransform();
                SetTransformValid(behavior, true);
            }

            // If the behavior is active, we have to update cursor right now.
            if ( behavior == ActiveEditingBehavior )
            {
                _inkCanvas.UpdateCursor();
            }
        }

        /// <summary>
        /// Check whether the cursor of the specified behavior is valid
        /// </summary>
        /// <param name="behavior">EditingBehavior</param>
        /// <returns>True if the cursor doesn't need to be updated</returns>
        internal bool IsCursorValid(EditingBehavior behavior)
        {
            return GetCursorValid(behavior);
        }

        /// <summary>
        /// Check whether the cursor of the specified behavior is valid
        /// </summary>
        /// <param name="behavior">EditingBehavior</param>
        /// <returns>True if the cursor doesn't need to be updated</returns>
        internal bool IsTransformValid(EditingBehavior behavior)
        {
            return GetTransformValid(behavior);
        }

        /// <summary>
        /// Change to another StylusEditing mode or None mode.
        /// </summary>
        /// <param name="sourceBehavior"></param>
        /// <param name="newMode"></param>
        /// <returns></returns>
        internal IStylusEditing ChangeStylusEditingMode(StylusEditingBehavior sourceBehavior, InkCanvasEditingMode newMode)
        {
            // NOTICE-2006/04/27-WAYNEZEN,
            // Before the caller invokes the method, the external event could have been fired.
            // The user code may interrupt the Mid-Stroke by releasing capture or switching to another mode. 
            // So we should check if we still is under mid-stroke and the source behavior still is active.
            // If not, we just no-op.
            if ( IsInMidStroke &&
                ( ( // We expect that either InkCollectionBehavior or EraseBehavior is active.
                    sourceBehavior != LassoSelectionBehavior && sourceBehavior == ActiveEditingBehavior )
                    // Or We expect SelectionEditor is active here since
                    // LassoSelectionBehavior will be deactivated once it gets committed. 
                    || ( sourceBehavior == LassoSelectionBehavior && SelectionEditor == ActiveEditingBehavior ) ) )
            {
                Debug.Assert(_activationStack.Count == 1, "The behavior stack has to contain one behavior.");

                // Pop up the old behavior
                PopEditingBehavior();

                // Get the new behavior
                EditingBehavior newBehavior = GetBehavior(ActiveEditingMode);

                if ( newBehavior != null )
                {
                    // Push the new behavior
                    PushEditingBehavior(newBehavior);

                    // If the new mode is Select, we should push the LassoSelectionBehavior now.
                    if ( newMode == InkCanvasEditingMode.Select
                        // NOTICE-2006/04/27-WAYNEZEN,
                        // Make sure the newBehavior is SelectionEditor since it could be changed by the user event handling code.
                        && newBehavior == SelectionEditor )
                    {
                        PushEditingBehavior(LassoSelectionBehavior);
                    }
                }
                else
                {
                    // None-mode now. We stop collecting the packets.
                    ReleaseCapture(true);
                }

                _inkCanvas.RaiseActiveEditingModeChanged(new RoutedEventArgs(InkCanvas.ActiveEditingModeChangedEvent, _inkCanvas));

                return ActiveEditingBehavior as IStylusEditing;
            }
            else
            {
                // No-op
                return null;
            }
        }

        /// <summary>
        /// Debug Checker
        /// </summary>
        /// <param name="behavior"></param>
        [Conditional("DEBUG")]
        internal void DebugCheckActiveBehavior(EditingBehavior behavior)
        {
            Debug.Assert(behavior == ActiveEditingBehavior);
        }

        /// <summary>
        /// Debug check for the dynamic behavior
        /// </summary>
        /// <param name="behavior"></param>
        [Conditional("DEBUG")]
        private void DebugCheckDynamicBehavior(EditingBehavior behavior)
        {
            Debug.Assert(behavior == LassoSelectionBehavior || behavior == SelectionEditingBehavior,
                "Only LassoSelectionBehavior or SelectionEditingBehavior is dynamic behavior");
        }

        /// <summary>
        /// Debug check for the non dynamic behavior
        /// </summary>
        /// <param name="behavior"></param>
        [Conditional("DEBUG")]
        private void DebugCheckNonDynamicBehavior(EditingBehavior behavior)
        {
            Debug.Assert(behavior != LassoSelectionBehavior && behavior != SelectionEditingBehavior,
                "behavior cannot be LassoSelectionBehavior or SelectionEditingBehavior");
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------------------
        //
        // Internal Properties
        //
        //-------------------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Gets / sets whether move is enabled or note
        /// </summary>
        internal bool MoveEnabled
        {
            get { return _moveEnabled; }
            set { _moveEnabled = value; }
        }

        /// <summary>
        /// The property that indicates if the user is interacting with the current InkCanvas
        /// </summary>
        internal bool UserIsEditing
        {
            get
            {
                return _userIsEditing;
            }
            set
            {
                _userIsEditing = value;
            }
        }

        /// <summary>
        /// Helper flag that tells if we're between a preview down and an up.
        /// </summary>
        internal bool StylusOrMouseIsDown
        {
            get
            {
                bool stylusDown = false;
                StylusDevice stylusDevice = Stylus.CurrentStylusDevice;
                if (stylusDevice != null && _inkCanvas.IsStylusOver && !stylusDevice.InAir)
                {
                    stylusDown = true;
                }

                bool mouseDown = (_inkCanvas.IsMouseOver && Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed);
                if (stylusDown || mouseDown)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns the StylusDevice to call DynamicRenderer.Reset with.  Stylus or Mouse 
        /// must be in a down state.  If the down device is a Mouse, null is returned, since
        /// that is what DynamicRenderer.Reset expects for the Mouse.
        /// </summary>
        /// <returns></returns>
        internal InputDevice GetInputDeviceForReset()
        {
            Debug.Assert((_capturedStylus != null && _capturedMouse == null) 
                            || (_capturedStylus == null && _capturedMouse != null),
                            "There must be one and at most one device being captured.");
            
            if ( _capturedStylus != null && !_capturedStylus.InAir )
            {
                return _capturedStylus;
            }
            else if ( _capturedMouse != null && _capturedMouse.LeftButton == MouseButtonState.Pressed )
            {
                return _capturedMouse;
            }

            return null;
        }
        /// <summary>
        /// Gets / sets whether resize is enabled or note
        /// </summary>
        internal bool ResizeEnabled
        {
            get { return _resizeEnabled; }
            set { _resizeEnabled = value; }
        }

        /// <summary>
        /// Lazy init access to the LassoSelectionBehavior
        /// </summary>
        /// <value></value>
        internal LassoSelectionBehavior LassoSelectionBehavior
        {
            get
            {
                if ( _lassoSelectionBehavior == null )
                {
                    _lassoSelectionBehavior = new LassoSelectionBehavior(this, _inkCanvas);
                }
                return _lassoSelectionBehavior;
            }
        }

        /// <summary>
        /// Lazy init access to the SelectionEditingBehavior
        /// </summary>
        /// <value></value>
        internal SelectionEditingBehavior SelectionEditingBehavior
        {
            get
            {
                if ( _selectionEditingBehavior == null )
                {
                    _selectionEditingBehavior = new SelectionEditingBehavior(this, _inkCanvas);
                }
                return _selectionEditingBehavior;
            }
        }

        /// <summary>
        /// Internal helper prop to indicate the active editing mode
        /// </summary>
        internal InkCanvasEditingMode ActiveEditingMode
        {
            get
            {
                if ( _stylusIsInverted )
                {
                    return _inkCanvas.EditingModeInverted;
                }
                return _inkCanvas.EditingMode;
            }
        }

        /// <summary>
        /// Lazy init access to the SelectionEditor
        /// </summary>
        /// <value></value>
        internal SelectionEditor SelectionEditor
        {
            get
            {
                if ( _selectionEditor == null )
                {
                    _selectionEditor = new SelectionEditor(this, _inkCanvas);
                }
                return _selectionEditor;
            }
        }

        /// <summary>
        /// Gets the mid-stroke state
        /// </summary>
        internal bool IsInMidStroke
        {
            get
            {
                return _capturedStylus != null || _capturedMouse != null;
            }
        }

        /// <summary>
        /// Returns Stylus Inverted state
        /// </summary>
        internal bool IsStylusInverted
        {
            get
            {
                return _stylusIsInverted;
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Retrieve the behavior instance based on the EditingMode
        /// </summary>
        /// <param name="editingMode">EditingMode</param>
        /// <returns></returns>
        private EditingBehavior GetBehavior(InkCanvasEditingMode editingMode)
        {
            EditingBehavior newBehavior;

            switch ( editingMode )
            {
                case InkCanvasEditingMode.Ink:
                case InkCanvasEditingMode.GestureOnly:
                case InkCanvasEditingMode.InkAndGesture:
                    newBehavior = InkCollectionBehavior;
                    break;
                case InkCanvasEditingMode.Select:
                    newBehavior = SelectionEditor;
                    break;
                case InkCanvasEditingMode.EraseByPoint:
                case InkCanvasEditingMode.EraseByStroke:
                    newBehavior = EraserBehavior;
                    break;
                default:
                    // Treat as InkCanvasEditingMode.None. Return null value
                    newBehavior = null;
                    break;
}

            return newBehavior;
        }


        /// <summary>
        /// Pushes an EditingBehavior onto our stack, disables any current ones
        /// </summary>
        /// <param name="newEditingBehavior">The EditingBehavior to activate</param>
        private void PushEditingBehavior(EditingBehavior newEditingBehavior)
        {
            Debug.Assert(newEditingBehavior != null);
            
            EditingBehavior behavior = ActiveEditingBehavior;

            // Deactivate the previous behavior
            if ( behavior != null )
            {
                behavior.Deactivate();
            }

            // Activate the new behavior.
            _activationStack.Push(newEditingBehavior);
            newEditingBehavior.Activate();
        }

        /// <summary>
        /// Pops an EditingBehavior onto our stack, disables any current ones
        /// </summary>
        private void PopEditingBehavior()
        {
            EditingBehavior behavior = ActiveEditingBehavior;

            if ( behavior != null )
            {
                behavior.Deactivate();
                _activationStack.Pop();

                behavior = ActiveEditingBehavior;
                if ( ActiveEditingBehavior != null )
                {
                    behavior.Activate();
                }
            }

            return;
        }

        /// <summary>
        /// Handles in-air stylus movements
        /// </summary>
        private void OnInkCanvasStylusInAirOrInRangeMove(object sender, StylusEventArgs args)
        {
            // If the current capture is mouse, we should reset the capture.
            if ( _capturedMouse != null )
            {
                if (ActiveEditingBehavior == InkCollectionBehavior && _inkCanvas.InternalDynamicRenderer != null)
                {
                    // 
                    // When InkCanvas loses the current capture, we should stop the RTI since the App thread are no longer collecting ink.
                    // By flipping the Enabled property, the RTI will be reset.
                    _inkCanvas.InternalDynamicRenderer.Enabled = false;
                    _inkCanvas.InternalDynamicRenderer.Enabled = true;
                }

                // Call ActiveEditingBehavior.Commit at the end of the routine. The method will cause an external event fired.
                // So it should be invoked after we set up our states.
                ActiveEditingBehavior.Commit(true);

                // Release capture and do clean-up.
                ReleaseCapture(true);
            }

            UpdateInvertedState(args.StylusDevice, args.Inverted);
        }

        /// <summary>
        /// Handle StylusOutofRange event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnInkCanvasStylusOutOfRange(object sender, StylusEventArgs args)
        {
            // Reset the inverted state once OutOfRange has been received.
            UpdateInvertedState(args.StylusDevice, false);
        }

        /// <summary>
        /// InkCanvas.StylusDown handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        internal void OnInkCanvasDeviceDown(object sender, InputEventArgs args)
        {
            MouseButtonEventArgs mouseButtonEventArgs = args as MouseButtonEventArgs;
            bool resetDynamicRenderer = false;
            if ( mouseButtonEventArgs != null )
            {
                // NTRADI:WINDOWSOS#1563896-2006/03/20-WAYNEZEN,
                // Note we don't mark Handled for the None EditingMode.
                // Set focus to InkCanvas.
                if ( _inkCanvas.Focus() && ActiveEditingMode != InkCanvasEditingMode.None )
                {
                    mouseButtonEventArgs.Handled = true;
                }

                // ISSUE-2005/09/20-WAYNEZEN,
                // We will reevaluate whether we should allow the right-button down for the modes other than the Ink mode.
                // Skip collecting for the non-left buttons.
                if ( mouseButtonEventArgs.ChangedButton != MouseButton.Left )
                {
                    return;
                }

                // 
                // If the mouse down event is from a Stylus, make sure we have a correct inverted state.
                if ( mouseButtonEventArgs.StylusDevice != null )
                {
                    UpdateInvertedState(mouseButtonEventArgs.StylusDevice, mouseButtonEventArgs.StylusDevice.Inverted);
                }
            }
            else
            {
                // 
                // When StylusDown is received, We should check the Invert state again. 
                // If there is any change, the ActiveEditingMode will be updated.
                // The dynamic renderer will be reset in InkCollectionBehavior.OnActivated since the device is under down state. 
                StylusEventArgs stylusEventArgs = args as StylusEventArgs;
                UpdateInvertedState(stylusEventArgs.StylusDevice, stylusEventArgs.Inverted);
            }

            // If the active behavior is not one of StylusEditingBehavior, don't even bother here.
            IStylusEditing stylusEditingBehavior = ActiveEditingBehavior as IStylusEditing;
            
            // We might receive StylusDown from a different device meanwhile we have already captured one. 
            // Make sure that we are starting from a fresh state.
            if ( !IsInMidStroke && stylusEditingBehavior != null )
            {
                bool fSucceeded = false;

                try
                {
                    InputDevice capturedDevice = null;
                    // Capture the stylus (if mouse event make sure to use stylus if generated by a stylus)
                    if ( mouseButtonEventArgs != null && mouseButtonEventArgs.StylusDevice != null )
                    {
                        capturedDevice = mouseButtonEventArgs.StylusDevice;
                        resetDynamicRenderer = true;
                    }
                    else
                    {
                        capturedDevice = args.Device;
                    }

                    InitializeCapture(capturedDevice, stylusEditingBehavior, args.UserInitiated, resetDynamicRenderer);

                    // The below code might call out StrokeErasing or StrokeErased event.
                    // The out-side code could throw exception. 
                    // We use try/finally block to protect our status.
                    fSucceeded = true;
                }
                finally
                {
                    if ( !fSucceeded )
                    {
                        // Abort the current editing.
                        ActiveEditingBehavior.Commit(false);

                        // Release capture and do clean-up.
                        ReleaseCapture(IsInMidStroke);
                    }
                }
            }
        }

        /// <summary>
        /// InkCanvas.StylusMove handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnInkCanvasDeviceMove<TEventArgs>(object sender, TEventArgs args)
            where TEventArgs : InputEventArgs
        {
            // Make sure that the stylus is the one we captured previously.
            if ( IsInputDeviceCaptured(args.Device) )
            {
                IStylusEditing stylusEditingBehavior = ActiveEditingBehavior as IStylusEditing;
                Debug.Assert(stylusEditingBehavior != null || ActiveEditingBehavior == null,
                    "The ActiveEditingBehavior should be either null (The None mode) or type of IStylusEditing.");

                if ( stylusEditingBehavior != null )
                {
                    StylusPointCollection stylusPoints;
                    if ( _capturedStylus != null )
                    {
                        stylusPoints = _capturedStylus.GetStylusPoints(_inkCanvas, _commonDescription);
                    }
                    else
                    {
                        // Make sure we ignore stylus generated mouse events.
                        MouseEventArgs mouseEventArgs = args as MouseEventArgs;
                        if ( mouseEventArgs != null && mouseEventArgs.StylusDevice != null )
                        {
                            return;
                        }
                        
                        stylusPoints = new StylusPointCollection(new Point[] { _capturedMouse.GetPosition(_inkCanvas) });
                    }

                    bool fSucceeded = false;

                    // The below code might call out StrokeErasing or StrokeErased event.
                    // The out-side code could throw exception. 
                    // We use try/finally block to protect our status.
                    try
                    {
                        stylusEditingBehavior.AddStylusPoints(stylusPoints, args.UserInitiated);
                        fSucceeded = true;
                    }
                    finally
                    {
                        if ( !fSucceeded )
                        {
                            // Abort the current editing.
                            ActiveEditingBehavior.Commit(false);

                            // Release capture and do clean-up.
                            ReleaseCapture(true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// InkCanvas.StylusUp handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        internal void OnInkCanvasDeviceUp(object sender, InputEventArgs args)
        {
            MouseButtonEventArgs mouseButtonEventArgs = args as MouseButtonEventArgs;

            StylusDevice stylusDevice = null;

            // If this is a mouse event, store the StylusDevice
            if(mouseButtonEventArgs != null)
            {
                stylusDevice = mouseButtonEventArgs.StylusDevice;
            }

            
            // In the new WM_POINTER based touch stack, we can have a mouse down promotion
            // that fires after the stylus up (due to how WM_POINTER promotes).  In this case,
            // we would capture the stylus on the down (since it understands mouse promotion)
            // but we would never release capture here since this only checked args.Device.
            // To ensure that we properly release capture, we need to test both the args.Device
            // (which is a Win32MouseDevice on promoted messages) and the StylusDevice present in
            // the mouse event (if it is one).  That allows us to release properly for post-stylus
            // up promoted mouse events.  Note that we do not do this for Update handling as we don't 
            // want promoted moves to change state there.
            //
            // WISP COMPAT NOTE:
            // This does not affect WISP as it will release capture on the actual StylusUp, so no capture
            // will exist to allow this to proceed when the promoted mouse up is received.

            // Make sure that the stylus is the one we captured previously.
            if (IsInputDeviceCaptured(args.Device) 
                || (stylusDevice != null && IsInputDeviceCaptured(stylusDevice)))
            {
                Debug.Assert(ActiveEditingBehavior == null || ActiveEditingBehavior is IStylusEditing,
                    "The ActiveEditingBehavior should be either null (The None mode) or type of IStylusEditing.");

                // Make sure we only look at mouse left button events if watching mouse events. 
                if ( _capturedMouse != null )
                {
                    if ( mouseButtonEventArgs != null )
                    {
                        if ( mouseButtonEventArgs.ChangedButton != MouseButton.Left )
                        {
                            return;
                        }
                    }
                }

                try
                {
                    // The follow code raises variety editing events.
                    // The out-side code could throw exception in the their handlers. We use try/finally block to protect our status.
                    if ( ActiveEditingBehavior != null )
                    {
                        // Commit the current editing.
                        ActiveEditingBehavior.Commit(true);
                    }
                }
                finally
                {
                    // Call ReleaseCapture(true) at the end of the routine. The method will cause an external event fired.
                    // So it should be invoked after we set up our states.
                    ReleaseCapture(true);
                }
}
        }

        /// <summary>
        /// InkCanvas.LostStylusCapture handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnInkCanvasLostDeviceCapture<TEventArgs>(object sender, TEventArgs args)
            where TEventArgs : InputEventArgs
        {
            // If user is editing, we have to commit the current operation and reset our state.
            if ( UserIsEditing )
            {
                // Note ReleaseCapture(false) won't raise any external events. It only reset the internal states.
                ReleaseCapture(false);

                if ( ActiveEditingBehavior == InkCollectionBehavior && _inkCanvas.InternalDynamicRenderer != null )
                {
                    // 
                    // When InkCanvas loses the current capture, we should stop the RTI since the App thread are no longer collecting ink.
                    // By flipping the Enabled property, the RTI will be reset.
                    _inkCanvas.InternalDynamicRenderer.Enabled = false;
                    _inkCanvas.InternalDynamicRenderer.Enabled = true;
                }

                // Call ActiveEditingBehavior.Commit at the end of the routine. The method will cause an external event fired.
                // So it should be invoked after we set up our states.
                ActiveEditingBehavior.Commit(true);
            }
        }

        /// <summary>
        /// Initialize the capture state
        /// </summary>
        /// <param name="inputDevice"></param>
        /// <param name="stylusEditingBehavior"></param>
        /// <param name="userInitiated"></param>
        /// <param name="resetDynamicRenderer"></param>
        private void InitializeCapture(InputDevice inputDevice, IStylusEditing stylusEditingBehavior, bool userInitiated, bool resetDynamicRenderer)
        {
            Debug.Assert(inputDevice != null, "A null device is passed in.");
            Debug.Assert(stylusEditingBehavior != null, "stylusEditingBehavior cannot be null.");
            Debug.Assert(!IsInMidStroke, "The previous device hasn't been released yet.");

            StylusPointCollection stylusPoints;

            _capturedStylus = inputDevice as StylusDevice;
            _capturedMouse = inputDevice as MouseDevice;

            // NOTICE-2005/09/15-WAYNEZEN,
            // We assume that the StylusDown always happens before the MouseDown. So, we could safely add the listeners of
            // XXXMove and XXXUp as the below which branchs out the coming mouse or stylus events.
            if ( _capturedStylus != null )
            {
                StylusPointCollection newPoints = _capturedStylus.GetStylusPoints(_inkCanvas);

                _commonDescription =
                    StylusPointDescription.GetCommonDescription(_inkCanvas.DefaultStylusPointDescription,
                        newPoints.Description);
                stylusPoints = newPoints.Reformat(_commonDescription);
                
                if ( resetDynamicRenderer )
                {
                    // Reset the dynamic renderer for InkCollectionBehavior
                    InkCollectionBehavior inkCollectionBehavior = stylusEditingBehavior as InkCollectionBehavior;
                    if ( inkCollectionBehavior != null )
                    {
                        inkCollectionBehavior.ResetDynamicRenderer();
                    }
                }

                stylusEditingBehavior.AddStylusPoints(stylusPoints, userInitiated);

                // If the current down device is stylus, we should only listen to StylusMove, StylusUp and LostStylusCapture
                // events.

                _inkCanvas.CaptureStylus();

                if ( _inkCanvas.IsStylusCaptured && ActiveEditingMode != InkCanvasEditingMode.None )
                {
                    _inkCanvas.AddHandler(Stylus.StylusMoveEvent, new StylusEventHandler(OnInkCanvasDeviceMove<StylusEventArgs>));
                    _inkCanvas.AddHandler(UIElement.LostStylusCaptureEvent, new StylusEventHandler(OnInkCanvasLostDeviceCapture<StylusEventArgs>));
                }
                else
                {
                    _capturedStylus = null;
                }
}
            else
            {
                Debug.Assert(!resetDynamicRenderer, "The dynamic renderer shouldn't be reset for Mouse");

                _commonDescription = null;

                Point[] points = new Point[] { _capturedMouse.GetPosition(_inkCanvas) };
                stylusPoints = new StylusPointCollection(points);
                stylusEditingBehavior.AddStylusPoints(stylusPoints, userInitiated);

                // 
                // CaptureMouse triggers MouseDevice.Synchronize which sends a simulated MouseMove event.
                // So we have to call CaptureMouse after at the end of the initialization. 
                // Otherwise, the MouseMove handlers will be executed in the middle of the initialization.
                _inkCanvas.CaptureMouse();

                // The user code could change mode or call release capture when the simulated Move event is received.
                // So we have to check the capture still is on and the mode is correct before hooking up our handlers.
                // If the current down device is mouse, we should only listen to MouseMove, MouseUp and LostMouseCapture
                // events.
                if ( _inkCanvas.IsMouseCaptured && ActiveEditingMode != InkCanvasEditingMode.None )
                {
                    _inkCanvas.AddHandler(Mouse.MouseMoveEvent, new MouseEventHandler(OnInkCanvasDeviceMove<MouseEventArgs>));
                    _inkCanvas.AddHandler(UIElement.LostMouseCaptureEvent, new MouseEventHandler(OnInkCanvasLostDeviceCapture<MouseEventArgs>));
                }
                else
                {
                    _capturedMouse = null;
                }
            }
        }

        /// <summary>
        /// Clean up the capture state
        /// </summary>
        /// <param name="releaseDevice"></param>
        private void ReleaseCapture(bool releaseDevice)
        {
            Debug.Assert(IsInMidStroke || !releaseDevice, "The captured device has been release unexpectly.");

            if ( _capturedStylus != null )
            {
                // The Stylus was captured. Remove the stylus listeners.
                _commonDescription = null;

                _inkCanvas.RemoveHandler(Stylus.StylusMoveEvent, new StylusEventHandler(OnInkCanvasDeviceMove<StylusEventArgs>));
                _inkCanvas.RemoveHandler(UIElement.LostStylusCaptureEvent, new StylusEventHandler(OnInkCanvasLostDeviceCapture<StylusEventArgs>));

                _capturedStylus = null;

                if ( releaseDevice )
                {
                    // Call ReleaseStylusCapture at the end of the routine. The method will cause an external event fired.
                    // So it should be invoked after we set up our states.
                    _inkCanvas.ReleaseStylusCapture();
                }
            }
            else if ( _capturedMouse != null )
            {
                // The Mouse was captured. Remove the mouse listeners.
                _inkCanvas.RemoveHandler(Mouse.MouseMoveEvent, new MouseEventHandler(OnInkCanvasDeviceMove<MouseEventArgs>));
                _inkCanvas.RemoveHandler(UIElement.LostMouseCaptureEvent, new MouseEventHandler(OnInkCanvasLostDeviceCapture<MouseEventArgs>));

                _capturedMouse = null;
                if ( releaseDevice )
                {
                    // Call ReleaseMouseCapture at the end of the routine. The method will cause an external event fired.
                    // So it should be invoked after we set up our states.
                    _inkCanvas.ReleaseMouseCapture();
                }
            }
        }

        /// <summary>
        /// Retrieve whether a specified device is captured by us.
        /// </summary>
        /// <param name="inputDevice"></param>
        /// <returns></returns>
        private bool IsInputDeviceCaptured(InputDevice inputDevice)
        {
            Debug.Assert(_capturedStylus == null || _capturedMouse == null, "InkCanvas cannot capture both stylus and mouse at the same time.");
            return (inputDevice == _capturedStylus && ((StylusDevice)inputDevice).Captured == _inkCanvas)
                || (inputDevice == _capturedMouse && ( (MouseDevice)inputDevice).Captured == _inkCanvas);
        }

        /// <summary>
        /// Return the cursor of the active behavior
        /// </summary>
        /// <returns></returns>
        internal Cursor GetActiveBehaviorCursor()
        {
            EditingBehavior behavior = ActiveEditingBehavior;

            if ( behavior == null )
            {
                // Return the default Arrow cursor for the None mode.
                return Cursors.Arrow;
            }

            // Retrieve the cursor.
            Cursor cursor = behavior.Cursor;

            // Clean up the dirty flag when it's done.
            if ( !GetCursorValid(behavior) )
            {
                SetCursorValid(behavior, true);
            }

            return cursor;
        }

        /// <summary>
        /// A private method which returns the valid flag of behaviors' cursor.
        /// </summary>
        /// <param name="behavior"></param>
        /// <returns></returns>
        private bool GetCursorValid(EditingBehavior behavior)
        {
            BehaviorValidFlag flag = GetBehaviorCursorFlag(behavior);
            return GetBitFlag(flag);
        }

        /// <summary>
        /// A private method which sets/resets the valid flag of behaviors' cursor
        /// </summary>
        /// <param name="behavior"></param>
        /// <param name="isValid"></param>
        private void SetCursorValid(EditingBehavior behavior, bool isValid)
        {
            BehaviorValidFlag flag = GetBehaviorCursorFlag(behavior);
            SetBitFlag(flag, isValid);
        }

        /// <summary>
        /// A private method which returns the valid flag of behaviors' cursor.
        /// </summary>
        /// <param name="behavior"></param>
        /// <returns></returns>
        private bool GetTransformValid(EditingBehavior behavior)
        {
            BehaviorValidFlag flag = GetBehaviorTransformFlag(behavior);
            return GetBitFlag(flag);
        }

        /// <summary>
        /// A private method which sets/resets the valid flag of behaviors' cursor
        /// </summary>
        /// <param name="behavior"></param>
        /// <param name="isValid"></param>
        private void SetTransformValid(EditingBehavior behavior, bool isValid)
        {
            BehaviorValidFlag flag = GetBehaviorTransformFlag(behavior);
            SetBitFlag(flag, isValid);
        }

        /// <summary>
        /// GetBitFlag
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        private bool GetBitFlag(BehaviorValidFlag flag)
        {
            return (_behaviorValidFlag & flag) != 0;
        }

        /// <summary>
        /// SetBitFlag
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="value"></param>
        private void SetBitFlag(BehaviorValidFlag flag, bool value)
        {
            if ( value )
            {
                _behaviorValidFlag |= flag;
            }
            else
            {
                _behaviorValidFlag &= (~flag);
            }
}

        /// <summary>
        /// A helper returns behavior cursor flag from a behavior instance
        /// </summary>
        /// <param name="behavior"></param>
        /// <returns></returns>
        private BehaviorValidFlag GetBehaviorCursorFlag(EditingBehavior behavior)
        {
            BehaviorValidFlag flag = 0;

            if ( behavior == InkCollectionBehavior )
            {
                flag = BehaviorValidFlag.InkCollectionBehaviorCursorValid;
            }
            else if ( behavior == EraserBehavior )
            {
                flag = BehaviorValidFlag.EraserBehaviorCursorValid;
            }
            else if ( behavior == LassoSelectionBehavior )
            {
                flag = BehaviorValidFlag.LassoSelectionBehaviorCursorValid;
            }
            else if ( behavior == SelectionEditingBehavior )
            {
                flag = BehaviorValidFlag.SelectionEditingBehaviorCursorValid;
            }
            else if ( behavior == SelectionEditor )
            {
                flag = BehaviorValidFlag.SelectionEditorCursorValid;
            }
            else
            {
                Debug.Assert(false, "Unknown behavior");
            }

            return flag;
        }

        /// <summary>
        /// A helper returns behavior transform flag from a behavior instance
        /// </summary>
        /// <param name="behavior"></param>
        /// <returns></returns>
        private BehaviorValidFlag GetBehaviorTransformFlag(EditingBehavior behavior)
        {
            BehaviorValidFlag flag = 0;

            if ( behavior == InkCollectionBehavior )
            {
                flag = BehaviorValidFlag.InkCollectionBehaviorTransformValid;
            }
            else if ( behavior == EraserBehavior )
            {
                flag = BehaviorValidFlag.EraserBehaviorTransformValid;
            }
            else if ( behavior == LassoSelectionBehavior )
            {
                flag = BehaviorValidFlag.LassoSelectionBehaviorTransformValid;
            }
            else if ( behavior == SelectionEditingBehavior )
            {
                flag = BehaviorValidFlag.SelectionEditingBehaviorTransformValid;
            }
            else if ( behavior == SelectionEditor )
            {
                flag = BehaviorValidFlag.SelectionEditorTransformValid;
            }
            else
            {
                Debug.Assert(false, "Unknown behavior");
            }

            return flag;
        }

        private void ChangeEditingBehavior(EditingBehavior newBehavior)
        {
            Debug.Assert(!IsInMidStroke, "ChangeEditingBehavior cannot be called in a mid-stroke");
            Debug.Assert(_activationStack.Count <= 1, "The behavior stack has to contain at most one behavior when user is not editing.");
            
            try
            {
                _inkCanvas.ClearSelection(true);
            }
            finally
            {
                if ( ActiveEditingBehavior != null )
                {
                    // Pop the old behavior.
                    PopEditingBehavior();
                }

                // Push the new behavior
                if ( newBehavior != null )
                {
                    PushEditingBehavior(newBehavior);
                }
                
                _inkCanvas.RaiseActiveEditingModeChanged(new RoutedEventArgs(InkCanvas.ActiveEditingModeChangedEvent, _inkCanvas));
            }
        }

        /// <summary>
        /// Update the Inverted state
        /// </summary>
        /// <param name="stylusDevice"></param>
        /// <param name="stylusIsInverted"></param>
        /// <returns>true if the behavior is updated</returns>
        private bool UpdateInvertedState(StylusDevice stylusDevice, bool stylusIsInverted)
        {
            if ( !IsInMidStroke || 
                ( IsInMidStroke && IsInputDeviceCaptured(stylusDevice) ))
            {
                if ( stylusIsInverted != _stylusIsInverted )
                {
                    _stylusIsInverted = stylusIsInverted;

                    //
                    // this can change editing state
                    //
                    UpdateActiveEditingState();

                    return true;
                }
            }

            return false;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------------------
        //
        // Private Properties
        //
        //-------------------------------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// Gets the top level active EditingBehavior
        /// </summary>
        /// <value></value>
        private EditingBehavior ActiveEditingBehavior
        {
            get
            {
                EditingBehavior EditingBehavior = null;
                if ( _activationStack.Count > 0 )
                {
                    EditingBehavior = _activationStack.Peek();
                }
                return EditingBehavior;
            }
        }
        
        /// <summary>
        /// Lazy init access to the InkCollectionBehavior
        /// </summary>
        /// <value></value>
        internal InkCollectionBehavior InkCollectionBehavior
        {
            get
            {
                if ( _inkCollectionBehavior == null )
                {
                    _inkCollectionBehavior = new InkCollectionBehavior(this, _inkCanvas);
                }
                return _inkCollectionBehavior;
            }
        }

        /// <summary>
        /// Lazy init access to the EraserBehavior
        /// </summary>
        /// <value></value>
        private EraserBehavior EraserBehavior
        {
            get
            {
                if ( _eraserBehavior == null )
                {
                    _eraserBehavior = new EraserBehavior(this, _inkCanvas);
                }
                return _eraserBehavior;
            }
        }

        #endregion Private Properties

        //-------------------------------------------------------------------------------
        //
        // Private Enum
        //
        //-------------------------------------------------------------------------------

        #region Private Enum

        /// <summary>
        /// Enum values which represent the cursor valid flag for each EditingBehavior.
        /// </summary>
        [Flags]
        private enum BehaviorValidFlag
        {
            InkCollectionBehaviorCursorValid      = 0x00000001,
            EraserBehaviorCursorValid             = 0x00000002,
            LassoSelectionBehaviorCursorValid     = 0x00000004,
            SelectionEditingBehaviorCursorValid   = 0x00000008,
            SelectionEditorCursorValid            = 0x00000010,
            InkCollectionBehaviorTransformValid   = 0x00000020,
            EraserBehaviorTransformValid          = 0x00000040,
            LassoSelectionBehaviorTransformValid  = 0x00000080,
            SelectionEditingBehaviorTransformValid= 0x00000100,
            SelectionEditorTransformValid         = 0x00000200,
        }

        #endregion Private Enum

        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// The InkCanvas this EditingStack is being used in
        /// </summary>
        private InkCanvas _inkCanvas;
        private Stack<EditingBehavior> _activationStack;
        private InkCollectionBehavior _inkCollectionBehavior;
        private EraserBehavior _eraserBehavior;
        private LassoSelectionBehavior _lassoSelectionBehavior;
        private SelectionEditingBehavior _selectionEditingBehavior;
        private SelectionEditor _selectionEditor;
        private bool _moveEnabled = true;
        private bool _resizeEnabled = true;
        private bool _userIsEditing = false;

        /// <summary>
        /// Flag that indicates if the stylus is inverted
        /// </summary>
        private bool _stylusIsInverted = false;

        private StylusPointDescription  _commonDescription;
        private StylusDevice            _capturedStylus;
        private MouseDevice             _capturedMouse;

        // Fields related to cursor and transform.
        private BehaviorValidFlag       _behaviorValidFlag;

        #endregion Private Fields
    }
}
