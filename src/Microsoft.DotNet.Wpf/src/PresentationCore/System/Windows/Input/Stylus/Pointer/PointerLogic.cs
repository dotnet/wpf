// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using Microsoft.Win32; // for RegistryKey class
using MS.Internal;
using MS.Internal.Interop;
using MS.Internal.PresentationCore;                        // SecurityHelper
using MS.Utility;
using MS.Win32; // for *NativeMethods
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Input.Tracing;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input.StylusPointer
{
    /// <summary>
    /// WM_POINTER based implementation of StylusLogic
    /// 
    /// Future enhancements: 
    ///     Inking Support - Need real time stylus information.  We have this via InkPresenterDesktop but this requires
    ///                      tooling support in our Razzle to support Dev14, UCRT, and Win10 SDK.  This also requires we
    ///                      re-implement mouse promotion and a way to receive input solely from the inking thread.
    /// </summary>
    internal class PointerLogic : StylusLogic
    {
        #region Private Variables

        #region Taps

        /// <summary>
        /// If the last tap had the barrel button down
        /// </summary>
        private bool _lastTapBarrelDown = false;

        /// <summary>
        /// Holds the last point that recorded a tap
        /// </summary>
        private Point _lastTapPoint = new Point(0, 0);

        /// <summary>
        /// Holds the last time we recorded a tap in ticks
        /// </summary>
        private int _lastTapTimeTicks = 0;

        #endregion

        #region Capture/Over

        /// <summary>
        /// The captured element for the current StylusDevice
        /// </summary>
        IInputElement _stylusCapture;

        /// <summary>
        /// The element the current StylusDevice is over
        /// </summary>
        IInputElement _stylusOver;

        DeferredElementTreeState _stylusOverTreeState = new DeferredElementTreeState();
        DeferredElementTreeState _stylusCaptureWithinTreeState = new DeferredElementTreeState();

        // Event handlers/ops for stylus over and capture
        private DependencyPropertyChangedEventHandler _overIsEnabledChangedEventHandler;
        private DependencyPropertyChangedEventHandler _overIsVisibleChangedEventHandler;
        private DependencyPropertyChangedEventHandler _overIsHitTestVisibleChangedEventHandler;
        private DispatcherOperationCallback _reevaluateStylusOverDelegate;
        private DispatcherOperation _reevaluateStylusOverOperation;

        private DependencyPropertyChangedEventHandler _captureIsEnabledChangedEventHandler;
        private DependencyPropertyChangedEventHandler _captureIsVisibleChangedEventHandler;
        private DependencyPropertyChangedEventHandler _captureIsHitTestVisibleChangedEventHandler;
        private DispatcherOperationCallback _reevaluateCaptureDelegate;
        private DispatcherOperation _reevaluateCaptureOperation;

        #endregion

        #region Device Management

        /// <summary>
        /// Determines if we have yet refreshed our pointer devices.
        /// This allows us to bring the stack up when needed on first pointer input.
        /// </summary>
        private bool _initialDeviceRefreshDone = false;

        /// <summary>
        /// A collection of current pointer devices attached to the system
        /// </summary>
        private PointerTabletDeviceCollection _pointerDevices = new PointerTabletDeviceCollection();

        #endregion

        /// <summary>
        /// The currently selected StylusDevice that corresponds to the message being processed.
        /// </summary>
        private PointerStylusDevice _currentStylusDevice;

        private SecurityCriticalData<InputManager> _inputManager;

        /// <summary>
        /// Determines if we are currently processing a Drag/Drop operation.
        /// This is synced to InputManager in PreNotifyInput.
        /// </summary>
        private bool _inDragDrop = false;

        #endregion

        #region Properties

        /// <summary>
        /// A list of all stylus plugin managers per PresentationSource.  Allows us to maintain
        /// the stylus plugins depending on the input from the WM_POINTER native stack.
        /// </summary>
        internal Dictionary<PresentationSource, PointerStylusPlugInManager> PlugInManagers
        {
            get;
            private set;
        } = new Dictionary<PresentationSource, PointerStylusPlugInManager>();

        /// <summary>
        /// Indicates if a drag/drop action is currently in progress
        /// </summary>
        internal bool InDragDrop { get { return _inDragDrop; } }

        /// <summary>
        /// Indicates if the private reflection hack has been used to disable the stack (in this case we should accept no further input)
        /// </summary>
        internal static bool IsEnabled { get; private set; } = true;

        #endregion

        #region Constructor/Initialization

        /// <summary>
        /// Sets up the various event handlers and operations needed for processing pointer events
        /// </summary>
        /// <param name="inputManager">The InputManager for the current thread</param>
        internal PointerLogic(InputManager inputManager)
        {
            Statistics.FeaturesUsed |= Tracing.StylusTraceLogger.FeatureFlags.PointerStackEnabled;

            _inputManager = new SecurityCriticalData<InputManager>(inputManager);
            _inputManager.Value.PreProcessInput += new PreProcessInputEventHandler(PreProcessInput);
            _inputManager.Value.PreNotifyInput += new NotifyInputEventHandler(PreNotifyInput);
            _inputManager.Value.PostProcessInput += new ProcessInputEventHandler(PostProcessInput);

            _overIsEnabledChangedEventHandler = new DependencyPropertyChangedEventHandler(OnOverIsEnabledChanged);
            _overIsVisibleChangedEventHandler = new DependencyPropertyChangedEventHandler(OnOverIsVisibleChanged);
            _overIsHitTestVisibleChangedEventHandler = new DependencyPropertyChangedEventHandler(OnOverIsHitTestVisibleChanged);
            _reevaluateStylusOverDelegate = new DispatcherOperationCallback(ReevaluateStylusOverAsync);
            _reevaluateStylusOverOperation = null;

            _captureIsEnabledChangedEventHandler = new DependencyPropertyChangedEventHandler(OnCaptureIsEnabledChanged);
            _captureIsVisibleChangedEventHandler = new DependencyPropertyChangedEventHandler(OnCaptureIsVisibleChanged);
            _captureIsHitTestVisibleChangedEventHandler = new DependencyPropertyChangedEventHandler(OnCaptureIsHitTestVisibleChanged);
            _reevaluateCaptureDelegate = new DispatcherOperationCallback(ReevaluateCaptureAsync);
            _reevaluateCaptureOperation = null;
        }

        #endregion

        #region Input Manager Callbacks

        /// <summary>
        /// Pre-notify takes preview stylus inputs and correctly sets the overs for the particular StylusDevice.
        /// The proper StylusDevice is also selected at this time as subsequent processing will occur during the
        /// same sequence and will always use that device.
        /// </summary>
        private void PreNotifyInput(object sender, NotifyInputEventArgs e)
        {
            if (e.StagingItem.Input.RoutedEvent == InputManager.PreviewInputReportEvent)
            {
                InputReportEventArgs inputReportEventArgs = e.StagingItem.Input as InputReportEventArgs;

                if (!inputReportEventArgs.Handled && inputReportEventArgs.Report.Type == InputType.Stylus)
                {
                    RawStylusInputReport rawStylusInputReport = (RawStylusInputReport)inputReportEventArgs.Report;
                    PointerStylusDevice stylusDevice = rawStylusInputReport.StylusDevice.As<PointerStylusDevice>();

                    // Update the over property
                    if (!_inDragDrop && stylusDevice.CurrentPointerProvider.IsWindowEnabled)
                    {
                        Point position = stylusDevice.GetPosition(null);
                        IInputElement target = stylusDevice.FindTarget(stylusDevice.CriticalActiveSource, position);
                        SelectStylusDevice(stylusDevice, target, true);
                    }
                    else
                    {
                        SelectStylusDevice(stylusDevice, null, false);
                    }

                    _inputManager.Value.MostRecentInputDevice = stylusDevice.StylusDevice;

                    // Call appropriate stylus plugins
                    GetManagerForSource(stylusDevice.ActiveSource)?.VerifyStylusPlugInCollectionTarget(rawStylusInputReport);
                }
            }

            UpdateTapCount(e);
        }

        /// <summary>
        /// Handles mouse input messages in the preprocess stage
        /// </summary>
        /// <param name="e"></param>
        /// <param name="input"></param>
        private void PreProcessMouseInput(PreProcessInputEventArgs e, InputReportEventArgs input)
        {
            RawMouseInputReport rawMouseInputReport = (RawMouseInputReport)input.Report;

            bool isPromotedMouseMessage = IsPromotedMouseEvent(rawMouseInputReport);

            var stylusDevice = (input.Device as StylusDevice)?.StylusDeviceImpl?.As<PointerStylusDevice>();

            // If we see a promoted mouse event and the most recent device (the one associated with the promotion)
            // is promoting to manipulation, then we want to drop this mouse event from input.  This stops us from
            // getting any mouse promotions while manipulation is active, which is the desired behavior.
            if (isPromotedMouseMessage
                && !(CurrentStylusDevice?.As<PointerStylusDevice>()?.TouchDevice?.PromotingToOther ?? false)
                && (CurrentStylusDevice?.As<PointerStylusDevice>()?.TouchDevice?.PromotingToManipulation ?? false))
            {
                input.Handled = true;
                e.Cancel();
            }
            else if (!isPromotedMouseMessage && stylusDevice == null)
            {
                switch (rawMouseInputReport.Actions)
                {
                    case RawMouseActions.AbsoluteMove:
                        {
                            // During synchronization, the MouseDevice will create an AbsoluteMove and send it
                            // through the stack.  If this is processed by the MouseDevice when there is a 
                            // StylusDevice down, it will mess up the tracked device (MouseDevice tracks promoted
                            // messages from StylusDevices) and the button states will no longer be synchronized to
                            // the stylus stack.  This was happening due to handling of mouse events from Button and InkCanvas.
                            // This would cause promotions to not cause click events in the button.  Canceling and handling
                            // these messages will prevent the MouseDevice from processing them and destroying state.
                            if (CurrentStylusDevice?.InRange ?? false)
                            {
                                e.Cancel();
                                input.Handled = true;
                            }
                        }
                        break;
                    case RawMouseActions.CancelCapture:
                        {
                            // We need to resend this back through as coming from a StylusDevice if in range.
                            // This prevents the MouseDevice from processing this as a mouse event and removing
                            // the StylusDevice as what tracks state.
                            if (CurrentStylusDevice?.InRange ?? false)
                            {
                                RawMouseInputReport cancelCaptureInputReport =
                                            new RawMouseInputReport(rawMouseInputReport.Mode,
                                                                    rawMouseInputReport.Timestamp,
                                                                    rawMouseInputReport.InputSource,
                                                                    rawMouseInputReport.Actions,
                                                                    0, // Rest of the parameters are not used...
                                                                    0,
                                                                    0,
                                                                    IntPtr.Zero);

                                InputReportEventArgs args = new InputReportEventArgs(CurrentStylusDevice.StylusDevice, cancelCaptureInputReport);
                                args.RoutedEvent = InputManager.PreviewInputReportEvent;
                                _inputManager.Value.ProcessInput(args);

                                // Cancel this so that it doesn't propagate further in the InputManager.  We're ok to allow
                                // the MouseDevice to continue processing this at this point, so don't set input.Handled.
                                e.Cancel();
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Handles drag/drop, manipulation concerns, and gesture processing.
        /// </summary>
        private void PreProcessInput(object sender, PreProcessInputEventArgs e)
        {
            if (e.StagingItem.Input.RoutedEvent == InputManager.PreviewInputReportEvent)
            {
                InputReportEventArgs input = e.StagingItem.Input as InputReportEventArgs;

                if (input != null && !input.Handled)
                {
                    // See if we are in a DragDrop operation.  If so set our internal flag
                    // which stops us from promoting Stylus or Mouse events!
                    if (_inDragDrop != _inputManager.Value.InDragDrop)
                    {
                        _inDragDrop = _inputManager.Value.InDragDrop;
                    }

                    if (input.Report.Type == InputType.Mouse)
                    {
                        PreProcessMouseInput(e, input);
                    }
                    else if (input.Report.Type == InputType.Stylus)
                    {
                        RawStylusInputReport rsir = (RawStylusInputReport)input.Report;

                        SystemGesture? gesture = rsir.StylusDevice.TabletDevice.TabletDeviceImpl.GenerateStaticGesture(rsir);

                        if (gesture.HasValue)
                        {
                            GenerateGesture(rsir, gesture.Value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles mouse capture changes and promotion of events.
        /// 
        /// Capture changes detected from mouse are synchronized with StylusDevices.
        /// 
        /// Promotion works as following:
        ///     Raw->Preview->Main->Touch
        ///     Marking any of the events as handled will stop the promotion engine.
        /// </summary>
        private void PostProcessInput(object sender, ProcessInputEventArgs e)
        {
            // Watch the LostMouseCapture and GotMouseCapture events to keep stylus capture in sync.
            if (e.StagingItem.Input.RoutedEvent == Mouse.LostMouseCaptureEvent ||
                e.StagingItem.Input.RoutedEvent == Mouse.GotMouseCaptureEvent)
            {
                // Make sure mouse and stylus capture is the same.
                foreach (TabletDevice tabletDevice in TabletDevices)
                {
                    foreach (StylusDevice stylus in tabletDevice.StylusDevices)
                    {
                        // We use the Mouse device state for each call just in case we
                        // get re-entered in the middle of changing so when we continue
                        // we'll use the current mouse capture state (which should NOP).
                        stylus.Capture(Mouse.Captured, Mouse.CapturedMode);
                    }
                }
            }

            if (e.StagingItem.Input.RoutedEvent == InputManager.InputReportEvent)
            {
                InputReportEventArgs input = e.StagingItem.Input as InputReportEventArgs;

                if (!input.Handled)
                {
                    switch (input.Report.Type)
                    {
                        case InputType.Stylus:
                            {
                                RawStylusInputReport report = (RawStylusInputReport)input.Report;
                                PointerStylusDevice stylusDevice = report.StylusDevice.As<PointerStylusDevice>();

                                if (!_inDragDrop)
                                {
                                    // Only promote if the window is enabled!
                                    if (stylusDevice.CurrentPointerProvider.IsWindowEnabled)
                                    {
                                        PromoteRawToPreview(report, e);
                                    }
                                    else
                                    {
                                        // We don't want to send input messages to a disabled window, but if this
                                        // is a StylusUp action then we need to make sure that the device knows it
                                        // is no longer active.  If we don't do this, we will incorrectly think this
                                        // device is still active, and so therefore no other touch input will be
                                        // considered "primary" input, causing it to be ignored for most actions
                                        // (like button clicks).  (DevDiv2 520639)
                                        if ((report.Actions & RawStylusActions.Up) != 0 && stylusDevice != null)
                                        {
                                            PointerTouchDevice touchDevice = stylusDevice.TouchDevice;
                                            // Don't try to deactivate if the device isn't active.  This can happen if
                                            // the window was disabled for the touch-down as well, in which case we
                                            // never activated the device and therefore don't need to deactivate it.
                                            if (touchDevice.IsActive)
                                            {
                                                touchDevice.OnDeactivate();
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    
                                    // Previously, lifting a StylusDevice that was not the CurrentMousePromotionStylusDevice
                                    // during a multi-touch down drag/drop would ignore the Up for that device.  This was
                                    // resulting in an invalid active devices count in StylusTouchDevice, causing subsequent
                                    // touch interactions to never mouse promote and leaving the stack in an invalid state.
                                    // To fix this, deactivate for stylus device up received during a drag/drop as long as they
                                    // do not originate with the CurrentMousePromotionStylusDevice (which is the device for the
                                    // drag/drop operation).
                                    if (!(stylusDevice?.IsPrimary ?? false)
                                        && ((report.Actions & RawStylusActions.Up) != 0))
                                    {
                                        PointerTouchDevice touchDevice = stylusDevice.TouchDevice;

                                        // Don't try to deactivate if the device isn't active.  This can happen if
                                        // the window was disabled for the touch-down as well, in which case we
                                        // never activated the device and therefore don't need to deactivate it.
                                        if (touchDevice.IsActive)
                                        {
                                            touchDevice.OnDeactivate();
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }
            }

            // Here we invoke the PlugIns for the mouse so that any stylus plugins can affect raw mouse data as well.
            PointerStylusPlugInManager.InvokePlugInsForMouse(e);

            // Always attempt to promote preview to a main event.  If not applicable, nothing occurs.
            PromotePreviewToMain(e);

            // Always attempt to promote to other (manipulation or touch)
            PromoteMainToOther(e);
        }

        #endregion

        #region StylusLogic Implementation

        /// <summary>
        /// The StylusDevice that owns the latest input
        /// </summary>
        internal override StylusDeviceBase CurrentStylusDevice
        {
            get
            {
                return _currentStylusDevice;
            }
        }

        /// <summary>
        /// The list of TabletDevices currently connected to the system.
        /// </summary>
        internal override TabletDeviceCollection TabletDevices
        {
            get
            {
                if (!_initialDeviceRefreshDone)
                {
                    _pointerDevices.Refresh();
                    _initialDeviceRefreshDone = true;

                    StylusTraceLogger.LogStartup();

                    // Once we've logged a startup we should log shutdown when the dispatcher shuts down
                    // as that indicates this particular StylusLogic is going away.
                    ShutdownListener = new StylusLogicShutDownListener(this, ShutDownEvents.DispatcherShutdown);
                }

                return _pointerDevices;
            }
        }

        /// <summary>
        /// Returns device units from measure units
        /// </summary>
        /// <param name="measurePoint">The point in measure units</param>
        /// <returns>The point in device units</returns>
        internal override Point DeviceUnitsFromMeasureUnits(Point measurePoint)
        {
            
            // We can possibly get here with no current device.  This happens from a certain order of mouse capture.
            // In that case, default to identity matrix as the capture units are going to be from the mouse.
            // Otherwise, transform using the tablet for the current stylus device.
            Point pt = measurePoint * (_currentStylusDevice?.ActiveSource?.CompositionTarget?.TransformToDevice ?? Matrix.Identity);

            // Make sure we return whole numbers (pixels are whole numbers)
            return new Point(Math.Round(pt.X), Math.Round(pt.Y));
        }

        /// <summary>
        /// Returns measure units from device units
        /// </summary>
        /// <param name="devicePoint">The point in device units</param>
        /// <returns>The point in measure units</returns>
        internal override Point MeasureUnitsFromDeviceUnits(Point devicePoint)
        {
            
            // We can possibly get here with no current device.  This happens from a certain order of mouse capture.
            // In that case, default to identity matrix as the capture units are going to be from the mouse.
            // Otherwise, transform using the tablet for the current stylus device.
            Point pt = devicePoint * (_currentStylusDevice?.ActiveSource?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity);

            // Make sure we return whole numbers (pixels are whole numbers)
            return new Point(Math.Round(pt.X), Math.Round(pt.Y));
        }

        /// <summary>
        /// Updates the capture for the StylusDevice
        /// </summary>
        /// <param name="stylusDevice">The StylusDevice to update</param>
        /// <param name="oldStylusDeviceCapture">The old captured element</param>
        /// <param name="newStylusDeviceCapture">The new captured element</param>
        /// <param name="timestamp">The time (in ticks)</param>
        internal override void UpdateStylusCapture(StylusDeviceBase stylusDevice, IInputElement oldStylusDeviceCapture, IInputElement newStylusDeviceCapture, int timestamp)
        {
            if (newStylusDeviceCapture != _stylusCapture)
            {
                DependencyObject o = null;
                IInputElement oldCapture = _stylusCapture;
                _stylusCapture = newStylusDeviceCapture;

                // Adjust the handlers we use to track everything.
                if (oldCapture != null)
                {
                    o = oldCapture as DependencyObject;
                    if (InputElement.IsUIElement(o))
                    {
                        UIElement element = o as UIElement;
                        element.IsEnabledChanged -= _captureIsEnabledChangedEventHandler;
                        element.IsVisibleChanged -= _captureIsVisibleChangedEventHandler;
                        element.IsHitTestVisibleChanged -= _captureIsHitTestVisibleChangedEventHandler;
                    }
                    else if (InputElement.IsContentElement(o))
                    {
                        // NOTE: there are no IsVisible or IsHitTestVisible properties for ContentElements.
                        ((ContentElement)o).IsEnabledChanged -= _captureIsEnabledChangedEventHandler;
                    }
                    else
                    {
                        UIElement3D element = o as UIElement3D;
                        element.IsEnabledChanged -= _captureIsEnabledChangedEventHandler;
                        element.IsVisibleChanged -= _captureIsVisibleChangedEventHandler;
                        element.IsHitTestVisibleChanged -= _captureIsHitTestVisibleChangedEventHandler;
                    }
                }

                if (_stylusCapture != null)
                {
                    o = _stylusCapture as DependencyObject;
                    if (InputElement.IsUIElement(o))
                    {
                        UIElement element = o as UIElement;
                        element.IsEnabledChanged += _captureIsEnabledChangedEventHandler;
                        element.IsVisibleChanged += _captureIsVisibleChangedEventHandler;
                        element.IsHitTestVisibleChanged += _captureIsHitTestVisibleChangedEventHandler;
                    }
                    else if (InputElement.IsContentElement(o))
                    {
                        // NOTE: there are no IsVisible or IsHitTestVisible properties for ContentElements.
                        ((ContentElement)o).IsEnabledChanged += _captureIsEnabledChangedEventHandler;
                    }
                    else
                    {
                        UIElement3D element = o as UIElement3D;
                        element.IsEnabledChanged += _captureIsEnabledChangedEventHandler;
                        element.IsVisibleChanged += _captureIsVisibleChangedEventHandler;
                        element.IsHitTestVisibleChanged += _captureIsHitTestVisibleChangedEventHandler;
                    }
                }

                // Oddly enough, update the IsStylusCaptureWithin property first.  This is
                // so any callbacks will see the more-common IsStylusCaptureWithin property
                // set correctly.
                UIElement.StylusCaptureWithinProperty.OnOriginValueChanged(oldCapture as DependencyObject, _stylusCapture as DependencyObject, ref _stylusCaptureWithinTreeState);

                // Invalidate the IsStylusCaptured properties.
                if (oldCapture != null)
                {
                    o = oldCapture as DependencyObject;
                    o.SetValue(UIElement.IsStylusCapturedPropertyKey, false); // Same property for ContentElements
                }

                if (_stylusCapture != null)
                {
                    o = _stylusCapture as DependencyObject;
                    o.SetValue(UIElement.IsStylusCapturedPropertyKey, true); // Same property for ContentElements
                }
            }
        }

        /// <summary>
        /// Updates the over property for the StylusDevice
        /// </summary>
        /// <param name="stylusDevice">The StylusDevice to update</param>
        /// <param name="newOver">The new over element</param>
        internal override void UpdateOverProperty(StylusDeviceBase stylusDevice, IInputElement newOver)
        {
            // Only update the OverProperty for the current stylus device and only if we see a change.
            if (stylusDevice == _currentStylusDevice && newOver != _stylusOver)
            {
                DependencyObject o = null;
                IInputElement oldOver = _stylusOver;
                _stylusOver = newOver;

                // Adjust the handlers we use to track everything.
                if (oldOver != null)
                {
                    o = oldOver as DependencyObject;
                    if (InputElement.IsUIElement(o))
                    {
                        UIElement element = o as UIElement;
                        element.IsEnabledChanged -= _overIsEnabledChangedEventHandler;
                        element.IsVisibleChanged -= _overIsVisibleChangedEventHandler;
                        element.IsHitTestVisibleChanged -= _overIsHitTestVisibleChangedEventHandler;
                    }
                    else if (InputElement.IsContentElement(o))
                    {
                        ((ContentElement)o).IsEnabledChanged -= _overIsEnabledChangedEventHandler;

                        // NOTE: there are no IsVisible or IsHitTestVisible properties for ContentElements.
                        //
                        // ((ContentElement)o).IsVisibleChanged -= _overIsVisibleChangedEventHandler;
                        // ((ContentElement)o).IsHitTestVisibleChanged -= _overIsHitTestVisibleChangedEventHandler;
                    }
                    else
                    {
                        UIElement3D element = o as UIElement3D;
                        element.IsEnabledChanged -= _overIsEnabledChangedEventHandler;
                        element.IsVisibleChanged -= _overIsVisibleChangedEventHandler;
                        element.IsHitTestVisibleChanged -= _overIsHitTestVisibleChangedEventHandler;
                    }
                }
                if (_stylusOver != null)
                {
                    o = _stylusOver as DependencyObject;
                    if (InputElement.IsUIElement(o))
                    {
                        UIElement element = o as UIElement;
                        element.IsEnabledChanged += _overIsEnabledChangedEventHandler;
                        element.IsVisibleChanged += _overIsVisibleChangedEventHandler;
                        element.IsHitTestVisibleChanged += _overIsHitTestVisibleChangedEventHandler;
                    }
                    else if (InputElement.IsContentElement(o))
                    {
                        ((ContentElement)o).IsEnabledChanged += _overIsEnabledChangedEventHandler;

                        // NOTE: there are no IsVisible or IsHitTestVisible properties for ContentElements.
                        //
                        // ((ContentElement)o).IsVisibleChanged += _overIsVisibleChangedEventHandler;
                        // ((ContentElement)o).IsHitTestVisibleChanged += _overIsHitTestVisibleChangedEventHandler;
                    }
                    else
                    {
                        UIElement3D element = o as UIElement3D;
                        element.IsEnabledChanged += _overIsEnabledChangedEventHandler;
                        element.IsVisibleChanged += _overIsVisibleChangedEventHandler;
                        element.IsHitTestVisibleChanged += _overIsHitTestVisibleChangedEventHandler;
                    }
                }

                // Oddly enough, update the IsStylusOver property first.  This is
                // so any callbacks will see the more-common IsStylusOver property
                // set correctly.
                UIElement.StylusOverProperty.OnOriginValueChanged(oldOver as DependencyObject, _stylusOver as DependencyObject, ref _stylusOverTreeState);

                // Invalidate the IsStylusDirectlyOver property.
                if (oldOver != null)
                {
                    o = oldOver as DependencyObject;
                    o.SetValue(UIElement.IsStylusDirectlyOverPropertyKey, false); // Same property for ContentElements
                }

                if (_stylusOver != null)
                {
                    o = _stylusOver as DependencyObject;
                    o.SetValue(UIElement.IsStylusDirectlyOverPropertyKey, true); // Same property for ContentElements
                }
            }
        }

        private void OnOverIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // The element that the stylus is over just became disabled.
            //
            // We need to resynchronize the stylus so that we can figure out who
            // the stylus is over now.
            ReevaluateStylusOver(null, null, true);
        }

        private void OnOverIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // The element that the stylus is over just became non-visible (collapsed or hidden).
            //
            // We need to resynchronize the stylus so that we can figure out who
            // the stylus is over now.
            ReevaluateStylusOver(null, null, true);
        }

        private void OnOverIsHitTestVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // The element that the stylus is over was affected by a change in hit-test visibility.
            //
            // We need to resynchronize the stylus so that we can figure out who
            // the stylus is over now.
            ReevaluateStylusOver(null, null, true);
        }

        private void OnCaptureIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // The element that the stylus is captured to just became disabled.
            //
            // We need to re-evaluate the element that has stylus capture since
            // we can't allow the stylus to remain captured by a disabled element.
            ReevaluateCapture(null, null, true);
        }

        private void OnCaptureIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // The element that the stylus is captured to just became non-visible (collapsed or hidden).
            //
            // We need to re-evaluate the element that has stylus capture since
            // we can't allow the stylus to remain captured by a non-visible element.
            ReevaluateCapture(null, null, true);
        }

        private void OnCaptureIsHitTestVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // The element that the stylus is captured to was affected by a change in hit-test visibility.
            //
            // We need to re-evaluate the element that has stylus capture since
            // we can't allow the stylus to remain captured by a non-hittest-visible element.
            ReevaluateCapture(null, null, true);
        }

        internal override void ReevaluateCapture(DependencyObject element, DependencyObject oldParent, bool isCoreParent)
        {
            if (element != null)
            {
                if (isCoreParent)
                {
                    _stylusCaptureWithinTreeState.SetCoreParent(element, oldParent);
                }
                else
                {
                    _stylusCaptureWithinTreeState.SetLogicalParent(element, oldParent);
                }
            }

            // We re-evaluate the captured element to be consistent with how
            // we re-evaluate the element the stylus is over.
            //
            // See ReevaluateStylusOver for details.
            if (_reevaluateCaptureOperation == null)
            {
                _reevaluateCaptureOperation = Dispatcher.BeginInvoke(DispatcherPriority.Input, _reevaluateCaptureDelegate, null);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private object ReevaluateCaptureAsync(object arg)
        {
            _reevaluateCaptureOperation = null;

            if (_stylusCapture == null)
                return null;

            bool killCapture = false;

            DependencyObject dependencyObject = _stylusCapture as DependencyObject;

            //
            // First, check things like IsEnabled, IsVisible, etc. on a
            // UIElement vs. ContentElement basis.
            //
            if (InputElement.IsUIElement(dependencyObject))
            {
                killCapture = !ValidateUIElementForCapture((UIElement)_stylusCapture);
            }
            else if (InputElement.IsContentElement(dependencyObject))
            {
                killCapture = !ValidateContentElementForCapture((ContentElement)_stylusCapture);
            }
            else
            {
                killCapture = !ValidateUIElement3DForCapture((UIElement3D)_stylusCapture);
            }

            //
            // Second, if we still haven't thought of a reason to kill capture, validate
            // it on a Visual basis for things like still being in the right tree.
            //
            if (killCapture == false)
            {
                DependencyObject containingVisual = InputElement.GetContainingVisual(dependencyObject);
                killCapture = !ValidateVisualForCapture(containingVisual, CurrentStylusDevice);
            }

            //
            // Lastly, if we found any reason above, kill capture.
            //
            if (killCapture)
            {
                Stylus.Capture(null);
            }

            // Refresh StylusCaptureWithinProperty so that ReverseInherited flags are updated.
            //
            // We only need to do this is there is any information about the old
            // tree state.  This is because it is possible (even likely) that
            // we would have already killed capture if the capture criteria was
            // no longer met.
            if (_stylusCaptureWithinTreeState != null && !_stylusCaptureWithinTreeState.IsEmpty)
            {
                UIElement.StylusCaptureWithinProperty.OnOriginValueChanged(_stylusCapture as DependencyObject, _stylusCapture as DependencyObject, ref _stylusCaptureWithinTreeState);
            }

            return null;
        }

        internal override void ReevaluateStylusOver(DependencyObject element, DependencyObject oldParent, bool isCoreParent)
        {
            if (element != null)
            {
                if (isCoreParent)
                {
                    _stylusOverTreeState.SetCoreParent(element, oldParent);
                }
                else
                {
                    _stylusOverTreeState.SetLogicalParent(element, oldParent);
                }
            }

            // It would be best to re-evaluate anything dependent on the hit-test results
            // immediately after layout & rendering are complete.  Unfortunately this can
            // lead to an infinite loop.  Consider the following scenario:
            //
            // If the stylus is over an element, hide it.
            //
            // This never resolves to a "correct" state.  When the stylus moves over the
            // element, the element is hidden, so the stylus is no longer over it, so the
            // element is shown, but that means the stylus is over it again.  Repeat.
            //
            // We push our re-evaluation to a priority lower than input processing so that
            // the user can change the input device to avoid the infinite loops, or close
            // the app if nothing else works.
            //
            if (_reevaluateStylusOverOperation == null)
            {
                _reevaluateStylusOverOperation = Dispatcher.BeginInvoke(DispatcherPriority.Input, _reevaluateStylusOverDelegate, null);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private object ReevaluateStylusOverAsync(object arg)
        {
            _reevaluateStylusOverOperation = null;

            // Synchronize causes state issues with the stylus events so we don't do this.
            //if (_currentStylusDevice != null)
            //{
            //    _currentStylusDevice.Synchronize();
            //}

            // Refresh StylusOverProperty so that ReverseInherited Flags are updated.
            //
            // We only need to do this is there is any information about the old
            // tree state.  This is because it is possible (even likely) that
            // Synchronize() would have already done this if we hit-tested to a
            // different element.
            if (_stylusOverTreeState != null && !_stylusOverTreeState.IsEmpty)
            {
                UIElement.StylusOverProperty.OnOriginValueChanged(_stylusOver as DependencyObject, _stylusOver as DependencyObject, ref _stylusOverTreeState);
            }

            return null;
        }

        #region Private Reflection Hack

        /// <summary>
        /// Implementation of the private reflection hack (see StylusLogicBase.OnTabletRemoved).
        /// </summary>
        /// <param name="wisptisIndex">Not Used</param>
        protected override void OnTabletRemoved(uint wisptisIndex)
        {
            // We just set the static flag here with no regard to threading since at worst we 
            // get or miss a message from WM_POINTER stack and this is only ever set to false.
            IsEnabled = false;
        }

        #endregion

        #region Windows Message Handling

        /// <summary>
        /// This method handles the various windows messages related to the system setting changes.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        internal override void HandleMessage(WindowMessage msg, IntPtr wParam, IntPtr lParam)
        {
            // Always refresh devices here.  On remove/change scenarios we just want all new devices anyway.
            // On settings/display changes, refreshing devices gives us all new transforms directly from
            // windows which allows us to be DPI/Orientation aware.
            switch (msg)
            {
                case WindowMessage.WM_DEVICECHANGE:
                    {
                        _pointerDevices.Refresh();
                    }
                    break;

                case WindowMessage.WM_DISPLAYCHANGE:
                    {
                        _pointerDevices.Refresh();
                    }
                    break;

                case WindowMessage.WM_SETTINGCHANGE:
                    {
                        // Refresh configuration before making tablets
                        ReadSystemConfig();

                        _pointerDevices.Refresh();
                    }
                    break;

                case WindowMessage.WM_TABLET_ADDED:
                    {
                        _pointerDevices.Refresh();
                    }
                    break;

                case WindowMessage.WM_TABLET_DELETED:
                    {
                        _pointerDevices.Refresh();
                    }
                    break;
            }
        }

        #endregion

        #endregion

        #region Event Promotion Functions

        /// <summary>
        /// Determines if the current even should be promoted to a touch event
        /// </summary>
        /// <param name="stylusEventArgs">The current event</param>
        /// <returns>True if promotion should occur, false otherwise</returns>
        private bool IsTouchPromotionEvent(StylusEventArgs stylusEventArgs)
        {
            if (stylusEventArgs != null)
            {
                RoutedEvent routedEvent = stylusEventArgs.RoutedEvent;
                return (stylusEventArgs?.StylusDevice?.TabletDevice?.Type == TabletDeviceType.Touch &&
                        (routedEvent == Stylus.StylusMoveEvent ||
                         routedEvent == Stylus.StylusDownEvent ||
                         routedEvent == Stylus.StylusUpEvent));
}
            return false;
        }

        /// <summary>
        /// Will promote a raw event into a preview event if applicable.
        /// Promoted events are pushed onto the stack to be handled immediately by the
        /// InputManager.
        /// </summary>
        /// <param name="report">The input report to promote</param>
        /// <param name="e">The input event args</param>
        private void PromoteRawToPreview(RawStylusInputReport report, ProcessInputEventArgs e)
        {
            RoutedEvent routedEvent = StylusLogic.GetPreviewEventFromRawStylusActions(report.Actions);

            if (routedEvent != null && report.StylusDevice != null)
            {
                StylusEventArgs args;

                if (routedEvent != Stylus.PreviewStylusSystemGestureEvent)
                {
                    if (routedEvent == Stylus.PreviewStylusDownEvent)
                    {
                        args = new StylusDownEventArgs(report.StylusDevice, report.Timestamp);
                    }
                    else
                    {
                        args = new StylusEventArgs(report.StylusDevice, report.Timestamp);
                    }
                }
                else
                {
                    RawStylusSystemGestureInputReport reportSg = (RawStylusSystemGestureInputReport)report;
                    args = new StylusSystemGestureEventArgs(report.StylusDevice,
                                                            report.Timestamp,
                                                            reportSg.SystemGesture,
                                                            reportSg.GestureX,
                                                            reportSg.GestureY,
                                                            reportSg.ButtonState);
                }

                args.InputReport = report;
                args.RoutedEvent = routedEvent;
                e.PushInput(args, e.StagingItem);
            }
        }

        /// <summary>
        /// Promotes a preview event to a main event if applicable.
        /// </summary>
        private void PromotePreviewToMain(ProcessInputEventArgs e)
        {
            if (!e.StagingItem.Input.Handled)
            {
                RoutedEvent eventMain = StylusLogic.GetMainEventFromPreviewEvent(e.StagingItem.Input.RoutedEvent);
                if (eventMain != null)
                {
                    StylusEventArgs eventArgsPreview = (StylusEventArgs)e.StagingItem.Input;
                    StylusDevice stylusDevice = eventArgsPreview.InputReport.StylusDevice;

                    StylusEventArgs eventArgsMain;
                    if (eventMain == Stylus.StylusDownEvent ||
                        eventMain == Stylus.PreviewStylusDownEvent)
                    {
                        StylusDownEventArgs downEventArgsPreview = (StylusDownEventArgs)eventArgsPreview;
                        eventArgsMain = new StylusDownEventArgs(stylusDevice, eventArgsPreview.Timestamp);
                    }
                    else if (eventMain == Stylus.StylusButtonDownEvent ||
                        eventMain == Stylus.StylusButtonUpEvent)
                    {
                        StylusButtonEventArgs buttonEventArgsPreview = (StylusButtonEventArgs)eventArgsPreview;
                        eventArgsMain = new StylusButtonEventArgs(stylusDevice, eventArgsPreview.Timestamp, buttonEventArgsPreview.StylusButton);
                    }
                    else if (eventMain != Stylus.StylusSystemGestureEvent)
                    {
                        eventArgsMain = new StylusEventArgs(stylusDevice, eventArgsPreview.Timestamp);
                    }
                    else
                    {
                        StylusSystemGestureEventArgs previewSystemGesture = (StylusSystemGestureEventArgs)eventArgsPreview;
                        eventArgsMain = new StylusSystemGestureEventArgs(stylusDevice,
                                                                         previewSystemGesture.Timestamp,
                                                                         previewSystemGesture.SystemGesture,
                                                                         previewSystemGesture.GestureX,
                                                                         previewSystemGesture.GestureY,
                                                                         previewSystemGesture.ButtonState);
                    }
                    eventArgsMain.InputReport = eventArgsPreview.InputReport;
                    eventArgsMain.RoutedEvent = eventMain;
                    e.PushInput(eventArgsMain, e.StagingItem);
                }
            }
            else
            {
                // A TouchDevice is activated before TouchDown and deactivated after TouchUp
                // But if PreviewStylusUp event is handled by the user, it will
                // never be promoted to TouchUp event leaving the TouchDevice in inconsistent
                // active state. Hence deactivating touch device if it is active.
                StylusEventArgs stylusEventArgs = e.StagingItem.Input as StylusEventArgs;
                if (stylusEventArgs != null &&
                    stylusEventArgs.RoutedEvent == Stylus.PreviewStylusUpEvent &&
                    stylusEventArgs.StylusDeviceImpl.As<PointerStylusDevice>().TouchDevice.IsActive)
                {
                    stylusEventArgs.StylusDeviceImpl.As<PointerStylusDevice>().TouchDevice.OnDeactivate();
                }
            }
        }

        /// <summary>
        /// Promotes a main input to a touch event if not handled
        /// </summary>
        private void PromoteMainToOther(ProcessInputEventArgs e)
        {
            StylusEventArgs stylusEventArgs = e.StagingItem.Input as StylusEventArgs;

            if (stylusEventArgs == null)
            {
                return;
            }

            PointerStylusDevice stylusDevice = stylusEventArgs.StylusDeviceImpl.As<PointerStylusDevice>();
            PointerTouchDevice touchDevice = stylusDevice.TouchDevice;

            if (IsTouchPromotionEvent(stylusEventArgs))
            {
                if (e.StagingItem.Input.Handled)
                {
                    // A TouchDevice is activated before TouchDown and deactivated after TouchUp
                    // But if StylusUp event is handled by the user, it will
                    // never be promoted to TouchUp event leaving the TouchDevice in inconsistent
                    // active state. Hence deactivating touch device if it is active.
                    if (stylusEventArgs.RoutedEvent == Stylus.StylusUpEvent &&
                        touchDevice.IsActive)
                    {
                        touchDevice.OnDeactivate();
                    }
                }
                else
                {
                    // This event is to also route as a Touch event.
                    // PromoteMainToMouse will eventually see the resulting
                    // touch event when it finishes and promote to mouse.
                    PromoteMainToTouch(e, stylusEventArgs);
                }
            }
        }

        /// <summary>
        /// Promotes a main input to the associated touch input
        /// </summary>
        private void PromoteMainToTouch(ProcessInputEventArgs e, StylusEventArgs stylusEventArgs)
        {
            PointerStylusDevice stylusDevice = stylusEventArgs.StylusDeviceImpl.As<PointerStylusDevice>();

            if (stylusEventArgs.RoutedEvent == Stylus.StylusMoveEvent)
            {
                PromoteMainMoveToTouch(stylusDevice, e.StagingItem);
            }
            else if (stylusEventArgs.RoutedEvent == Stylus.StylusDownEvent)
            {
                PromoteMainDownToTouch(stylusDevice, e.StagingItem);
            }
            else if (stylusEventArgs.RoutedEvent == Stylus.StylusUpEvent)
            {
                PromoteMainUpToTouch(stylusDevice, e.StagingItem);
            }
        }

        /// <summary>
        /// Promotes a main (stylus) down to a touch down
        /// </summary>
        private void PromoteMainDownToTouch(PointerStylusDevice stylusDevice, StagingAreaInputItem stagingItem)
        {
            PointerTouchDevice touchDevice = stylusDevice.TouchDevice;

            if (touchDevice.IsActive)
            {
                // Deactivate and end the previous cycle if already active
                touchDevice.OnDeactivate();
            }

            touchDevice.OnActivate();
            touchDevice.OnDown();
        }

        /// <summary>
        /// Promotes a main (stylus) move to a touch move
        /// </summary>
        private void PromoteMainMoveToTouch(PointerStylusDevice stylusDevice, StagingAreaInputItem stagingItem)
        {
            PointerTouchDevice touchDevice = stylusDevice.TouchDevice;

            if (touchDevice.IsActive)
            {
                touchDevice.OnMove();
            }
        }

        /// <summary>
        /// Promotes a main (stylus) up to a touch up
        /// </summary>
        private void PromoteMainUpToTouch(PointerStylusDevice stylusDevice, StagingAreaInputItem stagingItem)
        {
            PointerTouchDevice touchDevice = stylusDevice.TouchDevice;

            if (touchDevice.IsActive)
            {
                touchDevice.OnUp();
            }
        }

        #endregion

        #region StylusDevice Handling


        /// <summary>
        /// Updates the currently active stylus device and makes sure the StylusOver
        /// property is updated as needed.
        /// </summary>
        /// <param name="pointerStylusDevice">The PointerStylusDevice to select</param>
        /// <param name="newOver">The updated over</param>
        /// <param name="updateOver">Whether or not the over should be updated</param>
        internal void SelectStylusDevice(PointerStylusDevice pointerStylusDevice, IInputElement newOver, bool updateOver)
        {
            bool stylusDeviceChange = (_currentStylusDevice != pointerStylusDevice);
            PointerStylusDevice oldStylusDevice = _currentStylusDevice;

            // If current StylusDevice is becoming null, make sure we update the over state
            // before we update _currentStylusDevice or else the over property will not update
            // correctly!
            if (updateOver && pointerStylusDevice == null && stylusDeviceChange && newOver == null)
            {
                // This will cause UpdateOverProperty() to be called.
                _currentStylusDevice.ChangeStylusOver(newOver); // This should be null.
            }

            _currentStylusDevice = pointerStylusDevice;

            if (updateOver && pointerStylusDevice != null)
            {
                // This will cause StylusLogic.UpdateStylusOverProperty to unconditionally be called.
                pointerStylusDevice.ChangeStylusOver(newOver);

                // If changing the current stylusdevice make sure that the old one's
                // over state is set to null if it is not InRange anymore.
                // NOTE: We only want to do this if we have multiple stylusdevices InRange!
                if (stylusDeviceChange && oldStylusDevice != null)
                {
                    if (!oldStylusDevice.InRange)
                    {
                        oldStylusDevice.ChangeStylusOver(null);
                    }
                }
            }
        }

        #endregion

        #region Plugin/Inking Support

        /// <summary>
        /// Retrieves a plugin manager based on presentation source
        /// </summary>
        /// <param name="source">The PresentationSource to use</param>
        /// <returns>The associated plugin manager or null if none found</returns>
        internal PointerStylusPlugInManager GetManagerForSource(PresentationSource source)
        {
            if (source == null)
            {
                return null;
            }

            PointerStylusPlugInManager manager = null;

            PlugInManagers.TryGetValue(source, out manager);

            return manager;
        }

        #endregion

        #region Gesture Support

        /// <summary>
        /// Uses various threshold parameters to detect multiple rapid taps in the same region.
        /// We use this to determine the tap count that is sent with public events.
        /// </summary>
        /// <param name="args">The input event arguments</param>
        private void UpdateTapCount(NotifyInputEventArgs args)
        {
            if (args.StagingItem.Input.RoutedEvent == Stylus.PreviewStylusDownEvent)
            {
                StylusEventArgs stylusDownEventArgs = args.StagingItem.Input as StylusDownEventArgs;

                PointerStylusDevice stylusDevice = stylusDownEventArgs.StylusDevice.As<PointerStylusDevice>();

                Point ptClient = stylusDevice.GetPosition(null);

                // Assume Up (not pressed) if the button does not exist
                bool barrelPressed =
                    (stylusDevice.StylusButtons.GetStylusButtonByGuid(StylusPointPropertyIds.BarrelButton)?.StylusButtonState ?? StylusButtonState.Up) == StylusButtonState.Down;

                int elapsedTime = Math.Abs(unchecked(stylusDownEventArgs.Timestamp - _lastTapTimeTicks));

                Point ptPixels = DeviceUnitsFromMeasureUnits(ptClient);

                Size doubleTapSize = stylusDevice.PointerTabletDevice.DoubleTapSize;

                bool isSameSpot = (Math.Abs(ptPixels.X - _lastTapPoint.X) < doubleTapSize.Width) &&
                    (Math.Abs(ptPixels.Y - _lastTapPoint.Y) < doubleTapSize.Height);

                // If the tap was fast enough and within range with the same barrel state as the previous
                if (elapsedTime < stylusDevice.PointerTabletDevice.DoubleTapDeltaTime
                    && isSameSpot
                    && barrelPressed == _lastTapBarrelDown)
                {
                    stylusDevice.TapCount++;
                }
                else
                {
                    // Reset otherwise
                    stylusDevice.TapCount = 1;
                    _lastTapPoint = ptPixels;
                    _lastTapTimeTicks = stylusDownEventArgs.Timestamp;
                    _lastTapBarrelDown = barrelPressed;
                }
            }
            else if (args.StagingItem.Input.RoutedEvent == Stylus.PreviewStylusSystemGestureEvent)
            {
                StylusSystemGestureEventArgs stylusGestureEventArgs = args.StagingItem.Input as StylusSystemGestureEventArgs;

                PointerStylusDevice stylusDevice = stylusGestureEventArgs.StylusDevice.As<PointerStylusDevice>();

                // If we saw a drag or flick for this stylus, reset the tap count to 1
                if (stylusGestureEventArgs.SystemGesture == SystemGesture.Drag
                    || stylusGestureEventArgs.SystemGesture == SystemGesture.RightDrag)
                {
                    stylusDevice.TapCount = 1;
                }
            }
        }

        /// <summary>
        /// Generates a system gesture.  Mainly used to generate gesture output from the multi-touch system.
        /// </summary>
        /// <param name="rawStylusInputReport">The raw stylus input</param>
        /// <param name="gesture">The gesture to generate</param>
        private void GenerateGesture(RawStylusInputReport rawStylusInputReport, SystemGesture gesture)
        {
            PointerStylusDevice stylusDevice = rawStylusInputReport.StylusDevice.As<PointerStylusDevice>();

            RawStylusSystemGestureInputReport inputReport = new RawStylusSystemGestureInputReport(
                                                        InputMode.Foreground,
                                                        rawStylusInputReport.Timestamp,
                                                        rawStylusInputReport.InputSource,
                                                        () => { return stylusDevice.PointerTabletDevice.StylusPointDescription; },
                                                        rawStylusInputReport.TabletDeviceId,
                                                        rawStylusInputReport.StylusDeviceId,
                                                        gesture,
                                                        0, // Gesture X location (only used for flicks)
                                                        0, // Gesture Y location (only used for flicks)
                                                        0) // ButtonState (only used for flicks) 
            {
                StylusDevice = stylusDevice.StylusDevice,
            };

            InputReportEventArgs input = new InputReportEventArgs(stylusDevice.StylusDevice, inputReport);

            input.RoutedEvent = InputManager.PreviewInputReportEvent;

            // Process this directly instead of doing a push. We want this event to get
            // to the user before the StylusUp and MouseUp event.
            _inputManager.Value.ProcessInput(input);
        }

        #endregion
    }
}
