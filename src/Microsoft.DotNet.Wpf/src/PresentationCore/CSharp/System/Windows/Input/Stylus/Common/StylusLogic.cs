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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Input.StylusPointer;
using System.Windows.Input.StylusWisp;
using System.Windows.Input.Tracing;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;


namespace System.Windows.Input
{
    /// <summary>
    /// Implements a base class for StylusLogic that allows us to derive stack specific StylusLogic implementations
    /// </summary>
    internal abstract class StylusLogic : DispatcherObject
    {
        #region Enumerations

        /// <summary>
        /// Enumeration of flick commands.
        /// Taken from WISP map, but modified names as per WPF legacy stack
        /// See Stylus\Biblio.txt - 8
        /// </summary>
        internal enum FlickAction
        {
            GenericKey = 0,
            Scroll = 1,
            AppCommand = 2,
            CustomKey = 3,
            KeyModifier = 4,
        }

        /// <summary>
        /// Enumeration of flick commands.
        /// Taken from WISP map, but modified names as per WPF legacy stack
        /// See Stylus\Biblio.txt - 8
        /// </summary>
        internal enum FlickScrollDirection
        {
            Up = 0,
            Down = 1,
        }

        #endregion

        #region Constants

        /// <summary>
        /// The mask used to extract the flick command
        /// See Stylus\Biblio.txt - 8
        /// </summary>
        private const int FlickCommandMask = 0x001F;

        #region Registry Keys

        #region WISP

        /// <summary>
        /// The key to assert access to for WISP data
        /// </summary>
        private const string WispKeyAssert = @"HKEY_CURRENT_USER\" + WispRootKey;

        /// <summary>
        /// The root of all WISP registry entries
        /// </summary>
        private const string WispRootKey = @"Software\Microsoft\Wisp\";

        /// <summary>
        /// The event parameters for WISP pen input
        /// </summary>
        private const string WispPenSystemEventParametersKey = WispRootKey + @"Software\Microsoft\Wisp\Pen\SysEventParameters";

        /// <summary>
        /// The WISP touch paramaters
        /// </summary>
        private const string WispTouchConfigKey = WispRootKey + @"Software\Microsoft\Wisp\Touch";

        /// <summary>
        /// The max distance a double tap can vary
        /// </summary>
        private const string WispDoubleTapDistanceValue = "DlbDist";

        /// <summary>
        /// The max time a double tap can take
        /// </summary>
        private const string WispDoubleTapTimeValue = "DlbTime";

        /// <summary>
        /// The threshold distance delta to cancel
        /// </summary>
        private const string WispCancelDeltaValue = "Cancel";

        /// <summary>
        /// The max double tap distance for touch
        /// </summary>
        private const string WispTouchDoubleTapDistanceValue = "TouchModeN_DtapDist";

        /// <summary>
        /// The max double tap time for touch
        /// </summary>
        private const string WispTouchDoubleTapTimeValue = "TouchModeN_DtapTime";

        #endregion

        #region WM_POINTER

        /// <summary>
        /// String to use for assert of registry permissions
        /// </summary>
        private const string WpfPointerKeyAssert = @"HKEY_CURRENT_USER\" + WpfPointerKey;

        /// <summary>
        /// The key location for the registry switch to configure the touch stack system wide
        /// </summary>
        private const string WpfPointerKey = @"Software\Microsoft\Avalon.Touch\";

        /// <summary>
        /// The value of the switch for system wide touch stack configuration
        /// </summary>
        private const string WpfPointerValue = @"EnablePointerSupport";

        #endregion

        /// <summary>
        /// Constant to indicate promoted mouse messages.
        /// <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/ms703320(v=vs.85).aspx"/> 
        /// </summary>
        private const uint PromotedMouseEventTag = 0xFF515700;

        /// <summary>
        /// Mask for pulling mouse promotion tags from mouse extra info
        /// <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/ms703320(v=vs.85).aspx"/> 
        /// </summary>
        private const uint PromotedMouseEventMask = 0xFFFFFF00;

        /// <summary>
        /// Mask for pulling cursor id from promoted mouse messages
        /// <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/ms703320(v=vs.85).aspx"/> 
        /// </summary>
        private const byte PromotedMouseEventCursorIdMask = 0x7F;

        #endregion

        #endregion

        #region Member Variables

        // Information used to distinguish double-taps (actually, multi taps) from
        // multiple independent taps.
        protected int _stylusDoubleTapDeltaTime = 800;  // this is in milli-seconds for stylus touch
        protected int _stylusDoubleTapDelta = 15;  // The default double tap distance is .1 mm values (default is 1.5mm)
        protected int _cancelDelta = 10;        // The move distance is 1.0mm default (value in .1 mm)

        protected int _touchDoubleTapDeltaTime = 300; // this is in milli seconds for finger touch
        protected int _touchDoubleTapDelta = 45; // The default double tap distance for finger touch (4.5mm)

        protected const double DoubleTapMinFactor = 0.7; // 70% of the default threshold.
        protected const double DoubleTapMaxFactor = 1.3; // 130% of the default threshold.

        // Caches the pointer stack enabled state
        private static bool? _isPointerStackEnabled = null;

        #endregion

        #region Construction/Initilization

        /// <summary>
        ///
        /// True if the StylusLogic for the thread of the caller has been instantiated.
        /// False if otherwise.
        /// </summary>
        internal static bool IsInstantiated
        {
            get
            {
                return _currentStylusLogic?.Value != null;
            }
        }

        /// <summary>
        /// Wrapper around accesses to CoreAppContextSwitches so it's easier to
        /// use from friend assemblies and more explicit everywhere.
        /// </summary>
        internal static bool IsStylusAndTouchSupportEnabled
        {
            get
            {
                return !CoreAppContextSwitches.DisableStylusAndTouchSupport;
            }
        }

        /// <summary>
        /// Determines if the WM_POINTER based stack is enabled.
        /// Pointer is only supported on >= RS2, otherwise gracefully degrade to WISP stack.
        /// </summary>
        internal static bool IsPointerStackEnabled
        {
            get
            {
                if (!_isPointerStackEnabled.HasValue)
                {
                    _isPointerStackEnabled = IsStylusAndTouchSupportEnabled
                        && (CoreAppContextSwitches.EnablePointerSupport || IsPointerEnabledInRegistry)
                        && OSVersionHelper.IsOsWindows10RS2OrGreater;
                }

                return _isPointerStackEnabled.Value;
            }
        }

        /// <summary>
        /// The current stylus logic for the thread.  There can only be a single StylusLogic per
        /// thread as there is one per specific touch stack InputProvider.
        /// </summary>
        [ThreadStatic]
        private static SecurityCriticalDataClass<StylusLogic> _currentStylusLogic = null;

        /// <summary>
        /// This property is backed by a ThreadStatic instance.  This will be instantiated
        /// on first use by either HwndSource or SystemResources, depending on what is created
        /// first.  There is one StylusLogic per dispatcher thread.
        /// </summary>
        internal static StylusLogic CurrentStylusLogic
        {
            get
            {
                if (_currentStylusLogic?.Value == null)
                {
                    Initialize();
                }

                return _currentStylusLogic?.Value;
            }
        }

        /// <summary>
        /// Returns an implementation of StylusLogic casted to the derived type
        /// </summary>
        /// <typeparam name="T">The type to cast to</typeparam>
        /// <returns>An object of T if the cast succeeds, otherwise null</returns>
        internal static T GetCurrentStylusLogicAs<T>()
            where T : StylusLogic
        {
            return CurrentStylusLogic as T;
        }

        /// <summary>
        /// Initializes a new StylusLogic based on the AppContext switches.
        /// </summary>
        /// <returns></returns>
        private static void Initialize()
        {
            // Initialize a new StylusLogic based on AppContext switches
            // or do nothing if touch is turned off.
            if (IsStylusAndTouchSupportEnabled)
            {
                // Choose between WISP and Pointer stacks
                if (IsPointerStackEnabled)
                {
                    _currentStylusLogic = new SecurityCriticalDataClass<StylusLogic>(new PointerLogic(InputManager.UnsecureCurrent));
                }
                else
                {
                    _currentStylusLogic = new SecurityCriticalDataClass<StylusLogic>(new WispLogic(InputManager.UnsecureCurrent));
                }
            }
        }

        /// <summary>
        /// Detect if the registry key for enabling WM_POINTER has been set.  This key is primarily to allow for stack selection
        /// during testing without having to broadly change test code.
        /// </summary>
        private static bool IsPointerEnabledInRegistry
        {
            get
            {
                bool result = false;

                try
                {
                    result = ((int)(Registry.CurrentUser.OpenSubKey(WpfPointerKey, RegistryKeyPermissionCheck.ReadSubTree)?.GetValue(WpfPointerValue, 0) ?? 0)) == 1;
                }
                catch (Exception e) when (e is IOException)
                {
                    // No permission to access registry or someone 
                    // changed the key type to REG_SZ/REG_EXPAND_SZ/REG_MULTI_SZ.
                    // Do nothing here.
                }

                return result;
            }
        }

        #endregion

        #region Internal API

        /// <summary>
        /// The max distance (in himetric, .1 mm units) between double (multi) taps for stylus devices
        /// </summary>
        internal int StylusDoubleTapDelta { get { return _stylusDoubleTapDelta; } }

        /// <summary>
        /// The max distance (in himetric, .1 mm units) between double (multi) taps for touch devices
        /// </summary>
        internal int TouchDoubleTapDelta { get { return _touchDoubleTapDelta; } }

        /// <summary>
        /// The max time (in ms) between double (multi) taps for stylus devices
        /// </summary>
        internal int StylusDoubleTapDeltaTime { get { return _stylusDoubleTapDeltaTime; } }

        /// <summary>
        /// The max time (in ms) between double (multi) taps for touch devices
        /// </summary>
        internal int TouchDoubleTapDeltaTime { get { return _touchDoubleTapDeltaTime; } }

        /// <summary>
        /// Grab the defualts from the registry for the double tap thresholds.
        /// </summary>
        protected void ReadSystemConfig()
        {
            object obj;
            RegistryKey stylusKey = null; // This object has finalizer to close the key.
            RegistryKey touchKey = null; // This object has finalizer to close the key.

            try
            {
                stylusKey = Registry.CurrentUser.OpenSubKey(WispPenSystemEventParametersKey);

                if (stylusKey != null)
                {
                    obj = stylusKey.GetValue(WispDoubleTapDistanceValue);
                    _stylusDoubleTapDelta = (obj == null) ? _stylusDoubleTapDelta : (Int32)obj;      // The default double tap distance is 15 pixels (value is given in pixels)

                    obj = stylusKey.GetValue(WispDoubleTapTimeValue);
                    _stylusDoubleTapDeltaTime = (obj == null) ? _stylusDoubleTapDeltaTime : (Int32)obj;      // The default double tap timeout is 800ms

                    obj = stylusKey.GetValue(WispCancelDeltaValue);
                    _cancelDelta = (obj == null) ? _cancelDelta : (Int32)obj;      // The default move delta is 40 (4mm)
                }

                touchKey = Registry.CurrentUser.OpenSubKey(WispTouchConfigKey);

                if (touchKey != null)
                {
                    obj = touchKey.GetValue(WispTouchDoubleTapDistanceValue);
                    // min = 70%; max = 130%, these values are taken from Stylus\Biblio.txt - 2
                    _touchDoubleTapDelta = (obj == null) ? _touchDoubleTapDelta : FitToCplCurve(_touchDoubleTapDelta * DoubleTapMinFactor, _touchDoubleTapDelta, _touchDoubleTapDelta * DoubleTapMaxFactor, (Int32)obj);

                    obj = touchKey.GetValue(WispTouchDoubleTapTimeValue);
                    _touchDoubleTapDeltaTime = (obj == null) ? _touchDoubleTapDeltaTime : FitToCplCurve(_touchDoubleTapDeltaTime * DoubleTapMinFactor, _touchDoubleTapDeltaTime, _touchDoubleTapDeltaTime * DoubleTapMaxFactor, (Int32)obj);
                }
            }
            finally
            {
                if (stylusKey != null)
                {
                    stylusKey.Close();
                }
                if (touchKey != null)
                {
                    touchKey.Close();
                }
            }
        }

        /// <summary>
        /// Changes the over property of the given StylusDevice
        /// </summary>
        /// <param name="stylusDevice"></param>
        /// <param name="newOver"></param>
        internal abstract void UpdateOverProperty(StylusDeviceBase stylusDevice, IInputElement newOver);

        /// <summary>
        /// The latest stylus device used for input processing
        /// </summary>
        internal abstract StylusDeviceBase CurrentStylusDevice { get; }

        /// <summary>
        /// The current set of tablet devices for this stack instantiation
        /// </summary>
        internal abstract TabletDeviceCollection TabletDevices { get; }

        /// <summary>
        /// Converts measure units to tablet device coordinates
        /// </summary>
        /// <param name="measurePoint"></param>
        /// <returns></returns>
        internal abstract Point DeviceUnitsFromMeasureUnits(Point measurePoint);

        /// <summary>
        /// Converts device units to measure units
        /// </summary>
        /// <param name="measurePoint"></param>
        /// <returns></returns>
        internal abstract Point MeasureUnitsFromDeviceUnits(Point measurePoint);

        /// <summary>
        /// Updates the stylus capture for the particular stylus device
        /// </summary>
        /// <param name="stylusDevice"></param>
        /// <param name="oldStylusDeviceCapture"></param>
        /// <param name="newStylusDeviceCapture"></param>
        /// <param name="timestamp"></param>
        internal abstract void UpdateStylusCapture(StylusDeviceBase stylusDevice, IInputElement oldStylusDeviceCapture, IInputElement newStylusDeviceCapture, int timestamp);

        /// <summary>
        /// Triggers a capture change for this stack
        /// </summary>
        /// <param name="element"></param>
        /// <param name="oldParent"></param>
        /// <param name="isCoreParent"></param>
        internal abstract void ReevaluateCapture(DependencyObject element, DependencyObject oldParent, bool isCoreParent);

        /// <summary>
        /// Triggers an over change for this stack
        /// </summary>
        /// <param name="element"></param>
        /// <param name="oldParent"></param>
        /// <param name="isCoreParent"></param>
        internal abstract void ReevaluateStylusOver(DependencyObject element, DependencyObject oldParent, bool isCoreParent);

        #endregion

        #region Private Reflection Hack

        /// <summary>
        /// This must be included here as there is a documented private reflection hack on MSDN
        /// <see href="https://msdn.microsoft.com/library/dd901337(v=vs.90).aspx">here</see>.
        /// Stack specific implementors must either provide a way for this to work, or throw
        /// a InvalidOperationException in order to inform developers this is not supported.
        /// </summary>
        /// <param name="wisptisIndex"></param>
        protected abstract void OnTabletRemoved(uint wisptisIndex);

        #endregion

        #region Windows Message Handling

        /// <summary>
        /// Provides a message handling path for notifications from SystemResources and any other 
        /// message loop that might want to notify the stack specific logic of a windows message.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        internal abstract void HandleMessage(WindowMessage msg, IntPtr wParam, IntPtr lParam);

        #endregion

        #region Telemetry

        /// <summary>
        /// Used to make sure we log the stack shutting down as well as any statistics that were gathered during execution.
        /// </summary>
        protected StylusLogicShutDownListener ShutdownListener { get; set; }

        /// <summary>
        /// The telemetry stats for this particular StylusLogic instance
        /// </summary>
        public StylusTraceLogger.StylusStatistics Statistics { get; protected set; } = new StylusTraceLogger.StylusStatistics();

        /// <summary>
        /// A ShutDownListener implementation to indicate when the StylusLogic is being unloaded.
        /// </summary>
        protected class StylusLogicShutDownListener : ShutDownListener
        {
            public StylusLogicShutDownListener(StylusLogic target, ShutDownEvents events) : base(target, events)
            {
            }

            internal override void OnShutDown(object target, object sender, EventArgs e)
            {
                StylusLogic stylusLogic = (StylusLogic)target;

                StylusTraceLogger.LogStatistics(stylusLogic.Statistics);

                StylusTraceLogger.LogShutdown();
            }
}

        #endregion

        #region Static Methods

        /// <summary>
        /// Fit to control panel controlled curve. 0 matches min, 50 - default, 100 - max
        /// (the curve is 2 straight line segments connecting  the 3 points)
        /// </summary>
        private static int FitToCplCurve(double vMin, double vMid, double vMax, int value)
        {
            if (value < 0)
            {
                return (int)vMin;
            }

            if (value > 100)
            {
                return (int)vMax;
            }

            double f = (double)value / 100.0d;

            double v = 0;

            if (f <= 0.5d)
            {
                v = vMin + 2.0d * f * (vMid - vMin);
            }
            else
            {
                v = vMid + 2.0d * (f - 0.5d) * (vMax - vMid);
            }

            return (int)v;
        }


        /// <summary>
        /// Determines if this mouse event is a promoted event based on the extra information
        /// given from the WM_MOUSE message.  Windows guarantees that any promotions have this
        /// extra information and legacy engines (such as WISP in Windows 7) set the precedent
        /// for this, so it has been and can be relied on.
        /// </summary>
        /// <param name="mouseInputReport">The raw mouse input to check</param>
        /// <returns>True if this is a promoted mouse event, false otherwise.</returns>
        internal static bool IsPromotedMouseEvent(RawMouseInputReport mouseInputReport)
        {
            int mouseExtraInfo = NativeMethods.IntPtrToInt32(mouseInputReport.ExtraInformation);
            return (mouseExtraInfo & PromotedMouseEventMask) == PromotedMouseEventTag; // MI_WP_SIGNATURE
        }

        /// <summary>
        /// Retrieves the cursor id from a promoted mouse message
        /// </summary>
        /// <param name="mouseInputReport">THe input report</param>
        /// <returns>THe cursor id if promoted, 0 if pure mouse (by definition)</returns>
        internal static uint GetCursorIdFromMouseEvent(RawMouseInputReport mouseInputReport)
        {
            int mouseExtraInfo = NativeMethods.IntPtrToInt32(mouseInputReport.ExtraInformation);
            return (uint)(mouseExtraInfo & PromotedMouseEventCursorIdMask);
        }

        /// <summary>
        /// </summary>
        internal static void CurrentStylusLogicReevaluateStylusOver(DependencyObject element, DependencyObject oldParent, bool isCoreParent)
        {
            StylusLogic.CurrentStylusLogic.ReevaluateStylusOver(element, oldParent, isCoreParent);
        }

        /// <summary>
        /// </summary>
        internal static void CurrentStylusLogicReevaluateCapture(DependencyObject element, DependencyObject oldParent, bool isCoreParent)
        {
            StylusLogic.CurrentStylusLogic.ReevaluateCapture(element, oldParent, isCoreParent);
        }

        #region Event Promotions

        internal static RoutedEvent GetMainEventFromPreviewEvent(RoutedEvent routedEvent)
        {
            if (routedEvent == Stylus.PreviewStylusDownEvent)
                return Stylus.StylusDownEvent;
            if (routedEvent == Stylus.PreviewStylusUpEvent)
                return Stylus.StylusUpEvent;
            if (routedEvent == Stylus.PreviewStylusMoveEvent)
                return Stylus.StylusMoveEvent;
            if (routedEvent == Stylus.PreviewStylusInAirMoveEvent)
                return Stylus.StylusInAirMoveEvent;
            if (routedEvent == Stylus.PreviewStylusInRangeEvent)
                return Stylus.StylusInRangeEvent;
            if (routedEvent == Stylus.PreviewStylusOutOfRangeEvent)
                return Stylus.StylusOutOfRangeEvent;
            if (routedEvent == Stylus.PreviewStylusSystemGestureEvent)
                return Stylus.StylusSystemGestureEvent;
            if (routedEvent == Stylus.PreviewStylusButtonDownEvent)
                return Stylus.StylusButtonDownEvent;
            if (routedEvent == Stylus.PreviewStylusButtonUpEvent)
                return Stylus.StylusButtonUpEvent;
            return null;
        }

        internal static RoutedEvent GetPreviewEventFromRawStylusActions(RawStylusActions actions)
        {
            RoutedEvent previewEvent = null;

            switch (actions)
            {
                case RawStylusActions.Down:
                    {
                        previewEvent = Stylus.PreviewStylusDownEvent;
                    }
                    break;
                case RawStylusActions.Up:
                    {
                        previewEvent = Stylus.PreviewStylusUpEvent;
                    }
                    break;
                case RawStylusActions.Move:
                    {
                        previewEvent = Stylus.PreviewStylusMoveEvent;
                    }
                    break;
                case RawStylusActions.InAirMove:
                    {
                        previewEvent = Stylus.PreviewStylusInAirMoveEvent;
                    }
                    break;
                case RawStylusActions.InRange:
                    {
                        previewEvent = Stylus.PreviewStylusInRangeEvent;
                    }
                    break;
                case RawStylusActions.OutOfRange:
                    {
                        previewEvent = Stylus.PreviewStylusOutOfRangeEvent;
                    }
                    break;
                case RawStylusActions.SystemGesture:
                    {
                        previewEvent = Stylus.PreviewStylusSystemGestureEvent;
                    }
                    break;
            }

            return previewEvent;
        }

        #endregion

        #region Capture

        protected bool ValidateUIElementForCapture(UIElement element)
        {
            return element.IsEnabled && element.IsVisible && element.IsHitTestVisible;
        }

        protected bool ValidateContentElementForCapture(ContentElement element)
        {
            return element.IsEnabled;
        }

        protected bool ValidateUIElement3DForCapture(UIElement3D element)
        {
            return element.IsEnabled && element.IsVisible && element.IsHitTestVisible;
        }

        protected bool ValidateVisualForCapture(DependencyObject visual, StylusDeviceBase currentStylusDevice)
        {
            if (visual == null)
                return false;

            PresentationSource presentationSource = PresentationSource.CriticalFromVisual(visual);

            if (presentationSource == null)
            {
                return false;
            }

            if (currentStylusDevice != null &&
                currentStylusDevice.CriticalActiveSource != presentationSource &&
                currentStylusDevice.Captured == null)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Gestures

        /// <summary>
        /// Extracts the action from the flick data
        /// See Stylus\Biblio.txt - 8
        /// </summary>
        /// <param name="flickData"></param>
        /// <returns></returns>
        internal static FlickAction GetFlickAction(int flickData)
        {
            return (FlickAction)(flickData & FlickCommandMask);
        }

        /// <summary>
        /// Determines if the flick scroll is a scroll up
        /// See Stylus\Biblio.txt - 8
        /// </summary>
        /// <param name="flickData"></param>
        /// <returns></returns>
        protected static bool GetIsScrollUp(int flickData)
        {
            Debug.Assert(GetFlickAction(flickData) == FlickAction.Scroll); // Make sure scroll action is set in flickdata.
            return ((FlickScrollDirection)NativeMethods.SignedHIWORD(flickData) == FlickScrollDirection.Up);
        }

        /// <summary>
        /// Handles the flick system gesture
        /// </summary>
        /// <param name="flickData">Data about the flick</param>
        /// <param name="element">The element flicked on</param>
        /// <returns>True if handled, false otherwise</returns>
        internal bool HandleFlick(int flickData, IInputElement element)
        {
            bool handled = false; // By default say we didn't handle this flick action.

            switch (GetFlickAction(flickData))
            {
                case FlickAction.Scroll:
                    // Process scroll command.
                    RoutedUICommand command = GetIsScrollUp(flickData) ? ComponentCommands.ScrollPageUp : ComponentCommands.ScrollPageDown;
                    if (element != null)
                    {
                        if (command.CanExecute(null, element))
                        {
                            // Mark that an element accepted a flick scroll
                            // We want statistics on how often this gesture is used
                            // as it may be removed soon.
                            Statistics.FeaturesUsed |= StylusTraceLogger.FeatureFlags.FlickScrollingUsed;

                            command.Execute(null, element);
                        }

                        // 
                        // We should always report handled if there is a potential flick element.
                        // Otherwise, the flick UIHub will send WM_KEYXXX for the PageDown/PageUp to this window.
                        // So the focused element will react to those messages.
                        handled = true; // Say we handled it.
                    }
                    break;
                case FlickAction.GenericKey:
                case FlickAction.AppCommand:
                case FlickAction.CustomKey:
                case FlickAction.KeyModifier:
                    // Say we didn't handle it so UIHub will do default processing.
                    break;
                default:
                    {
                        Debug.Assert(false, "Unknown Flick Action encountered");
                    }
                    break;
            }

            return handled;
        }

        #endregion

        #endregion
    }
}
