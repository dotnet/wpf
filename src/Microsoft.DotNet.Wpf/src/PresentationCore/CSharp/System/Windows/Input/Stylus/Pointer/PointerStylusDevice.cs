// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using MS.Internal.Interop;
using MS.Internal.PresentationCore;
using MS.Win32.Pointer;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Interop;
using System.Windows.Media;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input.StylusPointer
{
    /// <summary>
    /// A WM_POINTER specific implementation of the StylusDeviceBase.
    /// 
    /// Supports direct access to WM_POINTER structures and basing behavior off of the WM_POINTER data.
    /// </summary>
    internal class PointerStylusDevice : StylusDeviceBase
    {
        #region Member Variables

        /// <summary>
        /// The current taps tracked by this StylusDevice
        /// </summary>
        private int _tapCount = 1;

        /// <summary>
        /// The buttons owned by this StylusDevice
        /// </summary>
        private StylusButtonCollection _stylusButtons;

        /// <summary>
        /// The interaction engine to feed with input data
        /// </summary>
        private PointerInteractionEngine _interactionEngine;

        /// <summary>
        /// The currently captures plugin collection
        /// </summary>
        private StylusPlugInCollection _stylusCapturePlugInCollection;

        /// <summary>
        /// A reference to the main logic for the pointer stack
        /// </summary>
        private PointerLogic _pointerLogic;

        /// <summary>
        /// The currently captured element
        /// </summary>
        private IInputElement _stylusCapture;

        /// <summary>
        /// What sort of capture is currently occuring
        /// </summary>
        private CaptureMode _captureMode = CaptureMode.None;

        /// <summary>
        /// The element this device is currently over
        /// </summary>
        private IInputElement _stylusOver;

        /// <summary>
        /// The raw position relative to the stylus over
        /// </summary>
        private Point _rawElementRelativePosition = new Point(0, 0);

        /// <summary>
        /// The PresentationSource that the latest input for this device is associated with
        /// </summary>
        private SecurityCriticalDataClass<PresentationSource> _inputSource = null;

        /// <summary>
        /// The time (in ticks) when the last event occurred
        /// </summary>
        private int _lastEventTimeTicks = 0;

        /// <summary>
        /// The current pointer data
        /// </summary>
        private PointerData _pointerData;

        /// <summary>
        /// The cursor info associated with this StylusDevice
        /// </summary>
        private UnsafeNativeMethods.POINTER_DEVICE_CURSOR_INFO _cursorInfo = new UnsafeNativeMethods.POINTER_DEVICE_CURSOR_INFO();

        /// <summary>
        /// The TabletDevice that owns this
        /// </summary>
        private PointerTabletDevice _tabletDevice;

        /// <summary>
        /// The current set of stylus points associated with this
        /// </summary>
        private StylusPointCollection _currentStylusPoints;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new PointerStylusDevice
        /// </summary>
        /// <param name="tabletDevice">The TabletDevice that owns this</param>
        /// <param name="cursorInfo">The cursor info for this stylus device</param>
        internal PointerStylusDevice(PointerTabletDevice tabletDevice, UnsafeNativeMethods.POINTER_DEVICE_CURSOR_INFO cursorInfo)
        {
            _cursorInfo = cursorInfo;
            _tabletDevice = tabletDevice;
            _pointerLogic = StylusLogic.GetCurrentStylusLogicAs<PointerLogic>();

            // Touch devices have a special set of handling code
            if (tabletDevice.Type == TabletDeviceType.Touch)
            {
                TouchDevice = new PointerTouchDevice(this);
            }

            _interactionEngine = new PointerInteractionEngine(this);

            _interactionEngine.InteractionDetected += HandleInteraction;

            List<StylusButton> buttons = new List<StylusButton>();

            // Create a button collection for this StylusDevice based off the button properties stored in the tablet
            // This needs to be done as each button instance has a StylusDevice owner that it uses to access the raw
            // data in the StylusDevice.
            foreach (var prop in _tabletDevice.DeviceInfo.StylusPointProperties)
            {
                if (prop.IsButton)
                {
                    StylusButton button = new StylusButton(StylusPointPropertyIds.GetStringRepresentation(prop.Id), prop.Id);
                    button.SetOwner(this);
                    buttons.Add(button);
                }
            }

            _stylusButtons = new StylusButtonCollection(buttons);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Eagerly dispose any resources
        /// </summary>
        /// <param name="disposing">If this is a dispose or finalize call</param>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _interactionEngine.Dispose();
                }
            }

            _disposed = true;
        }

        #endregion

        #region InputDevice

        /// <summary>
        ///     Returns the element that input from this device is sent to.
        /// </summary>
        internal override IInputElement Target
        {
            get
            {
                return DirectlyOver;
            }
        }

        /// <summary>
        ///     Returns the PresentationSource that is reporting input for this device.
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        internal override PresentationSource ActiveSource
        {
            get
            {
                return _inputSource.Value;
            }
        }

        #endregion

        #region Properties

        internal UnsafeNativeMethods.POINTER_INFO CurrentPointerInfo { get { return _pointerData.Info; } }

        internal HwndPointerInputProvider CurrentPointerProvider
        {
            get;
            private set;
        }

        internal uint CursorId
        {
            get
            {
                return _cursorInfo.cursorId;
            }
        }

        internal bool IsNew
        {
            get
            {
                return _pointerData?.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_NEW) ?? false;
            }
        }

        internal bool IsInContact
        {
            get
            {
                return _pointerData?.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_INCONTACT) ?? false;
            }
        }

        internal bool IsPrimary
        {
            get
            {
                return _pointerData?.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_PRIMARY) ?? false;
            }
        }

        internal bool IsFirstButton
        {
            get
            {
                return _pointerData?.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_FIRSTBUTTON) ?? false;
            }
        }

        internal bool IsSecondButton
        {
            get
            {
                return _pointerData?.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_SECONDBUTTON) ?? false;
            }
        }

        internal bool IsThirdButton
        {
            get
            {
                return _pointerData?.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_THIRDBUTTON) ?? false;
            }
        }

        internal bool IsFourthButton
        {
            get
            {
                return _pointerData?.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_FOURTHBUTTON) ?? false;
            }
        }

        internal bool IsFifthButton
        {
            get
            {
                return _pointerData?.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_FIFTHBUTTON) ?? false;
            }
        }

        internal uint TimeStamp
        {
            get
            {
                return _pointerData?.Info.dwTime ?? 0;
            }
        }

        internal bool IsDown
        {
            get
            {
                return _pointerData?.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_DOWN) ?? false;
            }
        }

        internal bool IsUpdate
        {
            get
            {
                return _pointerData?.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_UPDATE) ?? false;
            }
        }

        internal bool IsUp
        {
            get
            {
                return _pointerData?.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_UP) ?? false;
            }
        }

        internal bool HasCaptureChanged
        {
            get
            {
                return _pointerData?.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_CAPTURECHANGED) ?? false;
            }
        }

        internal bool HasTransform
        {
            get
            {
                return _pointerData?.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_HASTRANSFORM) ?? false;
            }
        }

        internal PointerTouchDevice TouchDevice
        {
            get; private set;
        } = null;

        #endregion

        #region StylusDeviceBase Properties

        internal override StylusPlugInCollection CurrentVerifiedTarget { get; set; }

        /// <summary>
        ///     Returns the PresentationSource that is reporting input for this device.
        /// </summary>
        internal override PresentationSource CriticalActiveSource
        {
            get
            {
                if (_inputSource != null)
                {
                    return _inputSource.Value;
                }
                return null;
            }
        }

        /// <summary>
        /// Returns the button collection that is associated with the StylusDevice.
        /// </summary>
        internal override StylusButtonCollection StylusButtons
        {
            get
            {
                return _stylusButtons;
            }
        }

        internal override StylusPoint RawStylusPoint
        {
            get
            {
                return _currentStylusPoints[_currentStylusPoints.Count - 1];
            }
        }

        /// <summary>
        ///     Returns whether the StylusDevice object has been internally disposed.
        /// </summary>
        internal override bool IsValid
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        ///     Returns the element that the stylus is over.
        /// </summary>
        internal override IInputElement DirectlyOver
        {
            get
            {
                return _stylusOver;
            }
        }

        /// <summary>
        ///     Returns the element that has captured the stylus.
        /// </summary>
        internal override IInputElement Captured
        {
            get
            {
                return _stylusCapture;
            }
        }

        /// <summary>
        /// Returns the tablet associated with the StylusDevice
        /// </summary>
        internal override TabletDevice TabletDevice
        {
            get
            {
                return _tabletDevice.TabletDevice;
            }
        }

        /// <summary>
        /// Returns the pointer tablet associated with the StylusDevice
        /// </summary>
        internal PointerTabletDevice PointerTabletDevice
        {
            get
            {
                return _tabletDevice;
            }
        }

        /// <summary>
        /// Returns the name of the StylusDevice
        /// 
        /// WISP returns either "Eraser" or "Stylus" depending on the cursor type.  Do the same here.
        /// See Stylus\Biblio.txt - 5/6
        /// </summary>
        internal override string Name
        {
            get
            {
                return (_cursorInfo.cursor == UnsafeNativeMethods.POINTER_DEVICE_CURSOR_TYPE.POINTER_DEVICE_CURSOR_TYPE_ERASER)
                    ? "Eraser"
                    : "Stylus";
            }
        }

        /// <summary>
        /// Returns the hardware id of the StylusDevice
        /// </summary>
        internal override int Id
        {
            get
            {
                unchecked
                {
                    return (int)CursorId;
                }
            }
        }

        /// <summary>
        ///     Indicates the stylus is not touching the surface.
        ///     InAir events are general sent at a lower frequency.
        /// </summary>
        internal override bool InAir
        {
            get
            {
                return !(_pointerData?.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_INCONTACT) ?? false)
                    && (_pointerData?.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_INRANGE) ?? false);
            }
        }

        /// <summary>
        ///     Indicates stylusDevice is in the inverted state.
        /// </summary>
        internal override bool Inverted
        {
            get
            {
                return _tabletDevice.Type == TabletDeviceType.Stylus
                    && (_pointerData?.PenInfo.penFlags.HasFlag(UnsafeNativeMethods.PEN_FLAGS.PEN_FLAG_INVERTED) ?? false);
            }
        }

        /// <summary>
        ///     Indicates stylusDevice is in the inverted state.
        /// </summary>
        internal override bool InRange
        {
            get
            {
                return _pointerData?.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_INRANGE) ?? false;
            }
        }

        internal override int DoubleTapDeltaX
        {
            get
            {
                return (int)PointerTabletDevice.DoubleTapSize.Width;
            }
        }

        internal override int DoubleTapDeltaY
        {
            get
            {
                return (int)PointerTabletDevice.DoubleTapSize.Height;
            }
        }

        internal override int DoubleTapDeltaTime
        {
            get
            {
                return PointerTabletDevice.DoubleTapDeltaTime;
            }
        }

        /// <summary>
        /// Returns the tap count as detected in the interaction engine.
        /// </summary>
        internal override int TapCount
        {
            get
            {
                return _tapCount;
            }

            set
            {
                _tapCount = value;
            }
        }

        internal override CaptureMode CapturedMode
        {
            get
            {
                return _captureMode;
            }
        }

        #endregion

        #region StylusDeviceBase Functions

        /// <summary>
        ///     Captures the stylus to a particular element.
        /// </summary>
        internal override bool Capture(IInputElement element, CaptureMode captureMode)
        {
            int timeStamp = Environment.TickCount;

            VerifyAccess();

            if (!(captureMode == CaptureMode.None || captureMode == CaptureMode.Element || captureMode == CaptureMode.SubTree))
            {
                throw new System.ComponentModel.InvalidEnumArgumentException("captureMode", (int)captureMode, typeof(CaptureMode));
            }

            if (element == null)
            {
                captureMode = CaptureMode.None;
            }

            if (captureMode == CaptureMode.None)
            {
                element = null;
            }

            // Validate that element is either a UIElement or a ContentElement
            DependencyObject doStylusCapture = element as DependencyObject;

            if (doStylusCapture != null && !InputElement.IsValid(element))
            {
                throw new InvalidOperationException(SR.Get(SRID.Invalid_IInputElement, doStylusCapture.GetType()));
            }

            doStylusCapture?.VerifyAccess();

            bool success = false;

            // The element we are capturing to must be both enabled and visible.
            UIElement e = element as UIElement;

            if ((e?.IsVisible ?? false) || (e?.IsEnabled ?? false))
            {
                success = true;
            }
            else
            {
                ContentElement ce = element as ContentElement;

                if (ce?.IsEnabled ?? false)
                {
                    success = true;
                }
                else
                {
                    // Setting capture to null.
                    success = true;
                }
            }

            if (success)
            {
                ChangeStylusCapture(element, captureMode, timeStamp);
            }

            return success;
        }

        /// <summary>
        ///     Captures the stylus to a particular element.
        /// </summary>
        internal override bool Capture(IInputElement element)
        {
            // No need for calling ApplyTemplate since we forward the call.
            return Capture(element, CaptureMode.Element);
        }

        /// <summary>
        ///     Forces the stylusdevice to resynchronize at it's current location and state.
        ///     It can conditionally generate a Stylus Move/InAirMove (at the current location) if a change
        ///     in hittesting is detected that requires an event be generated to update elements 
        ///     to the current state (typically due to layout changes without Stylus changes).  
        ///     Has the same behavior as MouseDevice.Synchronize().
        /// </summary>
        internal override void Synchronize()
        {
            // Simulate a stylus move (if we are current stylus, inrange, visuals still valid to update
            // and has moved).
            if (InRange && _inputSource != null && _inputSource.Value != null &&
                _inputSource.Value.CompositionTarget != null && !_inputSource.Value.CompositionTarget.IsDisposed)
            {
                Point rawScreenPoint = new Point(_pointerData.Info.ptPixelLocationRaw.X, _pointerData.Info.ptPixelLocationRaw.Y);
                Point ptDevice = PointUtil.ScreenToClient(rawScreenPoint, _inputSource.Value);

                // GlobalHitTest always returns an IInputElement, so we are sure to have one.
                IInputElement stylusOver = Input.StylusDevice.GlobalHitTest(_inputSource.Value, ptDevice);
                bool fOffsetChanged = false;

                if (_stylusOver == stylusOver)
                {
                    Point ptOffset = GetPosition(stylusOver);
                    fOffsetChanged = MS.Internal.DoubleUtil.AreClose(ptOffset.X, _rawElementRelativePosition.X) == false || MS.Internal.DoubleUtil.AreClose(ptOffset.Y, _rawElementRelativePosition.Y) == false;
                }

                if (fOffsetChanged || _stylusOver != stylusOver)
                {
                    int timeStamp = Environment.TickCount;

                    if (_currentStylusPoints != null &&
                        _currentStylusPoints.Count > 0 &&
                        StylusPointDescription.AreCompatible(PointerTabletDevice.StylusPointDescription, _currentStylusPoints.Description))
                    {
                        StylusPoint stylusPoint = _currentStylusPoints[_currentStylusPoints.Count - 1];
                        int[] data = stylusPoint.GetPacketData();

                        // get back to the correct coordinate system
                        Matrix m = _tabletDevice.TabletToScreen;
                        m.Invert();
                        Point ptTablet = ptDevice * m;

                        data[0] = (int)ptTablet.X;
                        data[1] = (int)ptTablet.Y;

                        RawStylusInputReport report = new RawStylusInputReport(InputMode.Foreground,
                                                                             timeStamp,
                                                                             _inputSource.Value,
                                                                             InAir ? RawStylusActions.InAirMove : RawStylusActions.Move,
                                                                             () => { return PointerTabletDevice.StylusPointDescription; },
                                                                             TabletDevice.Id,
                                                                             Id,
                                                                             data);


                        report.Synchronized = true;

                        InputReportEventArgs inputReportEventArgs = new InputReportEventArgs(StylusDevice, report);
                        inputReportEventArgs.RoutedEvent = InputManager.PreviewInputReportEvent;

                        InputManager.Current.ProcessInput(inputReportEventArgs);
                    }
                }
            }
        }

        /// <summary>
        ///     Returns a StylusPointCollection object for processing the data in the packet.
        ///     This method creates a new StylusPointCollection and copies the data.
        /// </summary>
        internal override StylusPointCollection GetStylusPoints(IInputElement relativeTo)
        {
            VerifyAccess();

            // Fake up an empty one if we have to.
            if (_currentStylusPoints == null)
            {
                return new StylusPointCollection(_tabletDevice.StylusPointDescription);
            }
            return _currentStylusPoints.Clone(StylusDevice.GetElementTransform(relativeTo), _currentStylusPoints.Description);
        }

        /// <summary>
        ///     Returns a StylusPointCollection object for processing the data in the packet.
        ///     This method creates a new StylusPointCollection and copies the data.
        /// </summary>
        internal override StylusPointCollection GetStylusPoints(IInputElement relativeTo, StylusPointDescription subsetToReformatTo)
        {
            if (null == subsetToReformatTo)
            {
                throw new ArgumentNullException("subsetToReformatTo");
            }
            // Fake up an empty one if we have to.
            if (_currentStylusPoints == null)
            {
                return new StylusPointCollection(subsetToReformatTo);
            }

            return _currentStylusPoints.Reformat(subsetToReformatTo, StylusDevice.GetElementTransform(relativeTo));
        }

        /// <summary>
        ///     Calculates the position of the stylus relative to a particular element.
        /// </summary>
        internal override Point GetPosition(IInputElement relativeTo)
        {
            VerifyAccess();

            // Validate that relativeTo is either a UIElement or a ContentElement
            if (relativeTo != null && !InputElement.IsValid(relativeTo))
            {
                throw new InvalidOperationException();
            }

            PresentationSource relativePresentationSource = null;

            if (relativeTo != null)
            {
                DependencyObject dependencyObject = relativeTo as DependencyObject;
                DependencyObject containingVisual = InputElement.GetContainingVisual(dependencyObject);
                if (containingVisual != null)
                {
                    relativePresentationSource = PresentationSource.CriticalFromVisual(containingVisual);
                }
            }
            else
            {
                if (_inputSource != null)
                {
                    relativePresentationSource = _inputSource.Value;
                }
            }

            // Verify that we have a valid PresentationSource with a valid RootVisual
            // - if we don't we won't be able to invoke ClientToRoot or TranslatePoint and 
            //   we will just return 0,0
            if (relativePresentationSource == null || relativePresentationSource.RootVisual == null)
            {
                return new Point(0, 0);
            }

            Point curPoint = new Point(_pointerData.Info.ptPixelLocationRaw.X, _pointerData.Info.ptPixelLocationRaw.Y);

            Point ptClient = PointUtil.ScreenToClient(curPoint, relativePresentationSource);
            Point ptRoot = PointUtil.ClientToRoot(ptClient, relativePresentationSource);
            Point ptRelative = InputElement.TranslatePoint(ptRoot, relativePresentationSource.RootVisual, (DependencyObject)relativeTo);

            return ptRelative;
        }

        internal override Point GetMouseScreenPosition(MouseDevice mouseDevice)
        {
            return mouseDevice.GetScreenPositionFromSystem();
        }

        /// <summary>
        ///     Gets the current state of the specified button
        /// </summary>
        /// <param name="mouseButton">
        ///     The mouse button to get the state of
        /// </param>
        /// <param name="mouseDevice">
        ///     The MouseDevice that is making the request
        /// </param>
        /// <returns>
        ///     The state of the specified mouse button
        /// </returns>
        /// <remarks>
        ///     This is the hook where the Input system (via the MouseDevice) can call back into
        ///     the Stylus system when we are processing Stylus events instead of Mouse events
        /// </remarks>
        internal override MouseButtonState GetMouseButtonState(MouseButton mouseButton, MouseDevice mouseDevice)
        {
            return mouseDevice.GetButtonStateFromSystem(mouseButton);
        }

        #endregion

        #region Pointer Specific Functions

        /// <summary>
        /// Updates the internal StylusDevice state based on the WM_POINTER input and the formed raw data.
        /// </summary>
        /// <param name="provider">The hwnd associated WM_POINTER provider</param>
        /// <param name="inputSource">The PresentationSource where this message originated</param>
        /// <param name="pointerData">The aggregated pointer data retrieved from the WM_POINTER stack</param>
        /// <param name="rsir">The raw stylus input generated from the pointer data</param>
        internal void Update(HwndPointerInputProvider provider, PresentationSource inputSource,
            PointerData pointerData, RawStylusInputReport rsir)
        {
            _lastEventTimeTicks = Environment.TickCount;

            _inputSource = new SecurityCriticalDataClass<PresentationSource>(inputSource);

            _pointerData = pointerData;

            // First get the initial stylus points.  Raw data from pointer input comes in screen coordinates, keep that here since that is what we expect.
            _currentStylusPoints = new StylusPointCollection(rsir.StylusPointDescription, rsir.GetRawPacketData(), GetTabletToElementTransform(null), Matrix.Identity);

            // If a plugin has modified these points, we need to fixup the points with the new input
            if (rsir?.RawStylusInput?.StylusPointsModified ?? false)
            {
                // Note that RawStylusInput.Target (of type StylusPluginCollection)
                // guarantees that ViewToElement is invertible.
                GeneralTransform transformToElement = rsir.RawStylusInput.Target.ViewToElement.Inverse;

                Debug.Assert(transformToElement != null);

                _currentStylusPoints = rsir.RawStylusInput.GetStylusPoints(transformToElement);
            }

            // Store the current hwnd provider so we know for what hwnd we are processing this message
            CurrentPointerProvider = provider;

            if (PointerTabletDevice.Type == TabletDeviceType.Touch)
            {
                // If we are a touch device, sync the ActiveSource
                TouchDevice.ChangeActiveSource(_inputSource.Value);
            }
        }

        #endregion

        #region Interaction Handling

        /// <summary>
        /// Triggers firing of all gestures detected in the interaction engine
        /// </summary>
        internal void UpdateInteractions(RawStylusInputReport rsir)
        {
            _interactionEngine.Update(rsir);
        }

        /// <summary>
        /// Processes gesture reports generated by the interaction engine
        /// </summary>
        /// <param name="clientData">Unused</param>
        /// <param name="originalReport">The gesture report generate by the engine</param>
        private void HandleInteraction(object clientData, RawStylusSystemGestureInputReport originalReport)
        {
            RawStylusSystemGestureInputReport report = new RawStylusSystemGestureInputReport(
                           InputMode.Foreground,
                           Environment.TickCount,
                           CriticalActiveSource,
                           () => { return PointerTabletDevice.StylusPointDescription; },
                           TabletDevice.Id,
                           Id,
                           originalReport.SystemGesture,
                           originalReport.GestureX,
                           originalReport.GestureY,
                           originalReport.ButtonState)
            {
                StylusDevice = StylusDevice,
            };

            // For a flick, update the points in the stylus device to the flick location.
            // This forces processing of the stylus over to use the initial flick location
            // instead of the last WM_POINTER message location allowing command processing to
            // be done on the flick location itself.
            if (report.SystemGesture == SystemGesture.Flick)
            {
                StylusPoint flickPoint = _currentStylusPoints[_currentStylusPoints.Count - 1];

                flickPoint.X = report.GestureX;
                flickPoint.Y = report.GestureY;

                _currentStylusPoints = new StylusPointCollection(flickPoint.Description,
                    flickPoint.GetPacketData(),
                    GetTabletToElementTransform(null),
                    Matrix.Identity);
            }

            InputReportEventArgs irea = new InputReportEventArgs(StylusDevice, report)
            {
                RoutedEvent = InputManager.PreviewInputReportEvent,
            };

            // Now send the input report
            InputManager.UnsecureCurrent.ProcessInput(irea);
        }

        #endregion

        #region Capture/Over Functions

        /// <summary>
        /// Returns the currently captured plugin and a ref indicating if there is a capture
        /// </summary>
        /// <param name="elementHasCapture">If this device has capture</param>
        /// <returns>The captured plug in collection or null if no capture exists</returns>
        internal override StylusPlugInCollection GetCapturedPlugInCollection(ref bool elementHasCapture)
        {
            elementHasCapture = (_stylusCapture != null);
            return _stylusCapturePlugInCollection;
        }

        /// <summary>
        /// Takes into account capture mode and hit testing to find the current stylusover.
        /// </summary>
        internal IInputElement FindTarget(PresentationSource inputSource, Point position)
        {
            IInputElement stylusOver = null;

            switch (_captureMode)
            {
                case CaptureMode.None:
                    {
                        stylusOver = StylusDevice.GlobalHitTest(inputSource, position);

                        // We understand UIElements and ContentElements.
                        // If we are over something else (like a raw visual)
                        // find the containing element.
                        if (!InputElement.IsValid(stylusOver))
                            stylusOver = InputElement.GetContainingInputElement(stylusOver as DependencyObject);
                    }
                    break;

                case CaptureMode.Element:
                    stylusOver = _stylusCapture;
                    break;

                case CaptureMode.SubTree:
                    {
                        IInputElement stylusCapture = InputElement.GetContainingInputElement(_stylusCapture as DependencyObject);

                        if (stylusCapture != null && inputSource != null)
                        {
                            // We need to re-hit-test to get the "real" UIElement we are over.
                            // This allows us to have our capture-to-subtree span multiple windows.

                            // GlobalHitTest always returns an IInputElement, so we are sure to have one.
                            stylusOver = StylusDevice.GlobalHitTest(inputSource, position);
                        }

                        if (stylusOver != null && !InputElement.IsValid(stylusOver))
                            stylusOver = InputElement.GetContainingInputElement(stylusOver as DependencyObject);

                        // Make sure that the element we hit is acutally underneath
                        // our captured element.  Because we did a global hit test, we
                        // could have hit an element in a completely different window.
                        //
                        // Note that we support the child being in a completely different window.
                        // So we use the GetUIParent method instead of just looking at
                        // visual/content parents.
                        if (stylusOver != null)
                        {
                            IInputElement ieTest = stylusOver;
                            UIElement eTest = null;
                            ContentElement ceTest = null;

                            while (ieTest != null && ieTest != stylusCapture)
                            {
                                eTest = ieTest as UIElement;

                                if (eTest != null)
                                {
                                    ieTest = InputElement.GetContainingInputElement(eTest.GetUIParent(true));
                                }
                                else
                                {
                                    ceTest = ieTest as ContentElement; // Should never fail.

                                    ieTest = InputElement.GetContainingInputElement(ceTest.GetUIParent(true));
                                }
                            }

                            // If we missed the capture point, we didn't hit anything.
                            if (ieTest != stylusCapture)
                            {
                                stylusOver = _stylusCapture;
                            }
                        }
                        else
                        {
                            // We didn't hit anything.  Consider the stylus over the capture point.
                            stylusOver = _stylusCapture;
                        }
                    }
                    break;
            }

            return stylusOver;
        }

        internal void ChangeStylusOver(IInputElement stylusOver)
        {
            // We are not syncing the OverSourceChanged event
            // the reasons for doing so are listed in the MouseDevice.cs OnOverSourceChanged implementation
            if (_stylusOver != stylusOver)
            {
                _stylusOver = stylusOver;
                _rawElementRelativePosition = GetPosition(_stylusOver);
            }
            else
            {
                // Always update the relative position if InRange since ChangeStylusOver is only
                // called when something changed (like capture or stylus moved) and in
                // that case we want this updated properly.  This value is used in Synchronize().
                if (InRange)
                {
                    _rawElementRelativePosition = GetPosition(_stylusOver);
                }
            }

            // The stylus over property is a singleton (only one stylus device at a time can
            // be over an element) so we let StylusLogic manager the element over state. 
            // NOTE: StylusLogic only allows the CurrentStylusDevice to change the over state.
            // Also note that Capture is also managed by StylusLogic in a similar fashion.
            _pointerLogic.UpdateOverProperty(this, _stylusOver);
        }

        internal void ChangeStylusCapture(IInputElement stylusCapture, CaptureMode captureMode, int timestamp)
        {
            // if the capture changed...
            if (stylusCapture != _stylusCapture)
            {
                // Actually change the capture first.  Invalidate the properties,
                // and then send the events.
                IInputElement oldStylusCapture = _stylusCapture;

                _stylusCapture = stylusCapture;
                _captureMode = captureMode;

                // We also need to figure out ahead of time if any plugincollections on this captured element (or a parent)
                // for the penthread hittesting code.
                _stylusCapturePlugInCollection = null;

                if (stylusCapture != null)
                {
                    UIElement uiElement = InputElement.GetContainingUIElement(stylusCapture as DependencyObject) as UIElement;
                    if (uiElement != null)
                    {
                        PresentationSource source = PresentationSource.CriticalFromVisual(uiElement as Visual);

                        if (source != null)
                        {
                            PointerStylusPlugInManager manager;

                            if (_pointerLogic.PlugInManagers.TryGetValue(source, out manager))
                            {
                                _stylusCapturePlugInCollection = manager.FindPlugInCollection(uiElement);
                            }
                        }
                    }
                }

                _pointerLogic.UpdateStylusCapture(this, oldStylusCapture, _stylusCapture, timestamp);

                // Send the LostStylusCapture and GotStylusCapture events.
                if (oldStylusCapture != null)
                {
                    StylusEventArgs lostCapture = new StylusEventArgs(StylusDevice, timestamp);
                    lostCapture.RoutedEvent = Stylus.LostStylusCaptureEvent;
                    lostCapture.Source = oldStylusCapture;
                    InputManager.UnsecureCurrent.ProcessInput(lostCapture);
                }
                if (_stylusCapture != null)
                {
                    StylusEventArgs gotCapture = new StylusEventArgs(StylusDevice, timestamp);
                    gotCapture.RoutedEvent = Stylus.GotStylusCaptureEvent;
                    gotCapture.Source = _stylusCapture;
                    InputManager.UnsecureCurrent.ProcessInput(gotCapture);
                }

                // Now update the stylus over state (only if this is the current stylus and 
                // it is inrange).
                if (_pointerLogic.CurrentStylusDevice == this || InRange)
                {
                    if (_stylusCapture != null)
                    {
                        IInputElement inputElementHit = _stylusCapture;

                        // See if we need to update over for subtree mode.
                        if (CapturedMode == CaptureMode.SubTree && _inputSource != null && _inputSource.Value != null)
                        {
                            Point pt = _pointerLogic.DeviceUnitsFromMeasureUnits(GetPosition(null));
                            inputElementHit = FindTarget(_inputSource.Value, pt);
                        }

                        ChangeStylusOver(inputElementHit);
                    }
                    else
                    {
                        // Only try to update over if we have a valid input source.
                        if (_inputSource != null && _inputSource.Value != null)
                        {
                            Point pt = GetPosition(null); // relative to window (root element)
                            pt = _pointerLogic.DeviceUnitsFromMeasureUnits(pt); // change back to device coords.
                            IInputElement currentOver = Input.StylusDevice.GlobalHitTest(_inputSource.Value, pt);
                            ChangeStylusOver(currentOver);
                        }
                    }
                }

                // For Mouse StylusDevice we want to make sure Mouse capture is set up the same.
                if (Mouse.Captured != _stylusCapture || Mouse.CapturedMode != _captureMode)
                {
                    Mouse.Capture(_stylusCapture, _captureMode);
                }
            }
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Creates a new set of stylus points based on the latest raw input report
        /// </summary>
        internal override void UpdateEventStylusPoints(RawStylusInputReport report, bool resetIfNoOverride)
        {
            if (report.RawStylusInput != null && report.RawStylusInput.StylusPointsModified)
            {
                GeneralTransform transformToElement = report.RawStylusInput.Target.ViewToElement.Inverse;
                //note that RawStylusInput.Target (of type StylusPluginCollection)
                //guarantees that ViewToElement is invertible
                Debug.Assert(transformToElement != null);

                _currentStylusPoints = report.RawStylusInput.GetStylusPoints(transformToElement);
            }
            else if (resetIfNoOverride)
            {
                _currentStylusPoints =
                    new StylusPointCollection(report.StylusPointDescription,
                                              report.GetRawPacketData(),
                                              GetTabletToElementTransform(null),
                                              Matrix.Identity);
            }
        }


        /// <summary>
        ///     Returns the transform for converting from tablet to element
        ///     relative coordinates.
        /// </summary>
        internal GeneralTransform GetTabletToElementTransform(IInputElement relativeTo)
        {
            GeneralTransformGroup group = new GeneralTransformGroup();
            Matrix toDevice = _inputSource.Value.CompositionTarget.TransformToDevice;
            toDevice.Invert();
            group.Children.Add(new MatrixTransform(PointerTabletDevice.TabletToScreen * toDevice));
            group.Children.Add(StylusDevice.GetElementTransform(relativeTo));
            return group;
        }

        #endregion
    }
}
