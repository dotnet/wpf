// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Diagnostics;
using System.Windows.Threading;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;
using System.Windows.Media.Composition;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using MS.Internal;
using MS.Internal.Automation;
using MS.Internal.Interop;
using MS.Utility;
using MS.Win32;
using MS.Internal.PresentationCore;             // SecurityHelper

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using HRESULT = MS.Internal.HRESULT;
using NativeMethodsSetLastError = MS.Internal.WindowsBase.NativeMethodsSetLastError;
using PROCESS_DPI_AWARENESS = MS.Win32.NativeMethods.PROCESS_DPI_AWARENESS;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Interop
{
    // This is the internal, more expressive, enum used by the InvalidateRenderMode method.
    // See the RenderMode enum and the RenderMode property for the public version.
    internal enum RenderingMode
    {
        Default = MILRTInitializationFlags.MIL_RT_INITIALIZE_DEFAULT,
        Software = MILRTInitializationFlags.MIL_RT_SOFTWARE_ONLY,
        Hardware = MILRTInitializationFlags.MIL_RT_HARDWARE_ONLY,
        HardwareReference = MILRTInitializationFlags.MIL_RT_HARDWARE_ONLY | MILRTInitializationFlags.MIL_RT_USE_REF_RAST,
        DisableMultimonDisplayClipping = MILRTInitializationFlags.MIL_RT_DISABLE_MULTIMON_DISPLAY_CLIPPING,
        IsDisableMultimonDisplayClippingValid = MILRTInitializationFlags.MIL_RT_IS_DISABLE_MULTIMON_DISPLAY_CLIPPING_VALID,
    }

    // This is the public, more limited, enum exposed for use with the RenderMode property.
    // See the RenderingMode enum and InvalidateRenderMode method for the internal version.
    /// <summary>
    ///     Render mode preference.
    /// </summary>
    public enum RenderMode
    {
        /// <summary>
        /// The rendering layer should use the GPU and CPU as appropriate.
        /// </summary>
        Default,

        /// <summary>
        /// The rendering layer should only use the CPU.
        /// </summary>
        SoftwareOnly
    }

    /// <summary>
    /// The HwndTarget class represents a binding to an HWND.
    /// </summary>
    /// <remarks>
    /// The HwndTarget is not thread-safe. Accessing the HwndTarget from a different
    /// thread than it was created will throw a <see cref="System.InvalidOperationException"/>.
    ///
    /// All value-type statics in this class that depend on the initial HWND for initialization - notably
    /// those related to DPI like <see cref="ProcessDpiAwareness"/> - must be represented as a
    /// nullable. This ensures that callers have a clear understanding of whether these have
    /// been initialized or not.
    /// </remarks>
    public class HwndTarget : CompositionTarget
    {
        /// <summary>
        /// Lock object used to ensure that initialization of other statics
        /// happens race-free
        /// </summary>
        private static readonly object s_lockObject = new object();

        private static WindowMessage s_updateWindowSettings;

        private static WindowMessage s_needsRePresentOnWake;

        /// <summary>
        /// wpfgfx will raise this message whenever a new display-set is enumerated.
        /// wParam: 1 if valid displays are available, 0 otherwise
        /// lParam: Not used
        /// </summary>
        private static WindowMessage s_DisplayDevicesAvailabilityChanged;

        /// <summary>
        /// This is returned by <see cref="HandleMessage(WindowMessage, IntPtr, IntPtr)"/>
        /// when a Window message handled exclusively by it.
        /// </summary>
        private static readonly IntPtr Handled  = new IntPtr(0x1);

        /// <summary>
        /// This is returned by <see cref="HandleMessage(WindowMessage, IntPtr, IntPtr)"/>
        /// when other Window procs should be allowed to continue processing a
        /// given Window message - i.e., <see cref="HandleMessage(WindowMessage, IntPtr, IntPtr)"/>
        /// does not process that Window message exclusively
        /// </summary>
        private static readonly IntPtr Unhandled = IntPtr.Zero;

        private MatrixTransform _worldTransform;
        private DpiScale2 _currentDpiScale;

        private SecurityCriticalDataForSet<RenderMode> _renderModePreference = new SecurityCriticalDataForSet<RenderMode>(RenderMode.Default);

        private NativeMethods.HWND _hWnd;

        private NativeMethods.RECT _hwndClientRectInScreenCoords = new NativeMethods.RECT();
        private NativeMethods.RECT _hwndWindowRectInScreenCoords = new NativeMethods.RECT();

        private Color _backgroundColor = Color.FromRgb(0, 0, 0);

        private DUCE.MultiChannelResource _compositionTarget =
            new DUCE.MultiChannelResource();

        private bool _isRenderTargetEnabled = true;
        // private Nullable<Color> _colorKey = null;
        // private double _opacity = 1.0;
        private bool _usesPerPixelOpacity = false;

        // It is important that this start at zero to allow an initial
        // UpdateWindowSettings(enable) command to enable the render target
        // without a preceeding UpdateWindowSettings(disable) command.
        private int _disableCookie = 0;

        // Used to deal with layered window problems. See comments where they are used.
        private bool _isMinimized = false;
        private bool _isSessionDisconnected = false;
        private bool _isSuspended = false;

        // True when user input is causing a resize. We use this to determine whether or
        // not we want to sync during resize to provide a better looking resize.
        private bool _userInputResize = false;

        // This bool is set by a private window message sent to us from the render thread,
        // indicating that the present has failed with S_PRESENT_OCCLUDED (usually due to the
        // monitor being asleep or locked) and that we need to invalidate the entire window for
        // presenting when the monitor turns back on.
        private bool _needsRePresentOnWake = false;

        // See comment above for _needsRePresentOnWake. If the present has failed because of a
        // reason other than the monitor being asleep (usually because a D3D full screen exclusive
        // app is occluding the WPF app), we need to be able to recognize this situation and avoid
        // continually invalidating the window and causing presents that will fail and continue the
        // cycle (the so called "WM_PAINT storm"). We set this member to true the first time we
        // invalidate due to the private window message indicating failure if we are *not* asleep, once
        // the timeout period specified by _allowedPresentFailureDelay has passed
        // Any failure after that until another sleep state event occurs will not trigger an invalidate.
        private bool _hasRePresentedSinceWake = false;

        /// <summary>
        /// True if wpfgfx indicates that valid displays
        /// are available. This is communiated by use of the
        /// window message <see cref="s_DisplayDevicesAvailabilityChanged"/>
        /// </summary>
        /// <remarks>
        /// Normally, we'd want to initialize this to true when we are running in a
        /// Window Station in interactive mode (WinSta0), which is typical for desktop applications.
        /// On the other hand, we'd want to initialize this to false when running in a
        /// non-interactive Window Station (for e.g., typical SCM services). This can be
        /// identified by <see cref="Environment.UserInteractive"/>.
        ///
        /// Instead of initializing it this way directly, we instead initialize <see cref="_displayDevicesAvailable"/>
        /// using <see cref="MediaContext.ShouldRenderEvenWhenNoDisplayDevicesAreAvailable"/>, which in turn factors in (a)
        /// <see cref="Environment.UserInteractive"/>, and (b) a registry override that requests
        /// that WPF's renderer act as if interactive displays are always present
        /// even when displays aren't - either because the process is running in an
        /// non-interactive Window Station, or because the session is in a
        /// <see cref="NativeMethods.WTS_CONNECTSTATE_CLASS.WTSDisconnected"/> state, and (c) an compat
        /// override that can be set in the application configuration file (app.config)
        /// </remarks>
        private bool _displayDevicesAvailable = MediaContext.ShouldRenderEvenWhenNoDisplayDevicesAreAvailable;

        /// <summary>
        /// True if WM_PAINT processing was deferred due to
        /// <see cref="_displayDevicesAvailable"/> being false.
        ///
        /// We will use this flag to determine whether we need to
        /// invalidate the entire window when display devices become
        /// available
        /// </summary>
        private bool _wasWmPaintProcessingDeferred = false;

        /// <summary>
        /// Session ID of this process
        /// </summary>
        /// <remarks>
        /// If the query for the session ID using WTS API's fails,
        /// then this value will remain null
        /// </remarks>
        private int? _sessionId = null;

        // The time of the last wake or unlock message we received. When we receive a lock/sleep message,
        // we set this value to DateTime.MinValue
        private DateTime _lastWakeOrUnlockEvent;

        // This is the amount of time we continue to propagate Invalidate() calls when we receive notifications
        // from the render thread that present has failed, as measured from the value of _lastWakeOrUnlockEvent.
        // This allows for a window of time during which we have received the, for eg, session unlock message,
        // but the D3D device is still returning S_PRESENT_OCCLUDED. Time is in seconds.
        private const double _allowedPresentFailureDelay = 10.0;

        private DispatcherTimer _restoreDT;

        /// <summary>
        /// Initializes static variables for this class.
        /// </summary>
        static HwndTarget()
        {
            s_updateWindowSettings = UnsafeNativeMethods.RegisterWindowMessage("UpdateWindowSettings");
            s_needsRePresentOnWake = UnsafeNativeMethods.RegisterWindowMessage("NeedsRePresentOnWake");
            s_DisplayDevicesAvailabilityChanged =
                UnsafeNativeMethods.RegisterWindowMessage("DisplayDevicesAvailabilityChanged");
        }

        /// <summary>
        /// Attaches a hwndTarget to the hWnd
        /// <remarks>
        ///     This API link demands for UIWindowPermission.AllWindows
        /// </remarks>
        /// </summary>
        /// <param name="hwnd">The HWND to which the HwndTarget will draw.</param>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public HwndTarget(IntPtr hwnd)
        {
            bool exceptionThrown = true;

            _sessionId = SafeNativeMethods.GetCurrentSessionId();
            _isSessionDisconnected = !SafeNativeMethods.IsCurrentSessionConnectStateWTSActive(_sessionId);
            if (_isSessionDisconnected)
            {
                _needsRePresentOnWake = true;
            }

            AttachToHwnd(hwnd);

            try
            {
                if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info))
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientCreateVisual, EventTrace.Keyword.KeywordGeneral, EventTrace.Level.Info, Dispatcher.GetHashCode(), hwnd.ToInt64());
                }

                _hWnd = NativeMethods.HWND.Cast(hwnd);

                // Get the client rectangle...
                UpdateWindowAndClientCoordinates();

                _lastWakeOrUnlockEvent = DateTime.MinValue;

                // Get the Process and window DPI awareness, and System
                // and Window DPI scale factors
                //
                // Initialize statics (done exactly once per process)
                //      PROCESS_DPI_AWARENESS values
                //          ProcessDpiAwareness property
                //          ActualProcessDpiAwareness property
                //      system DPI scale
                //  Initialize per-HwndTarget values (done once per HwndTarget instance)
                //      DPI_AWARENESS_CONTEXT (DpiAwarenessContext property)
                //      Window DPI scale (_currentDpiScale field)
                InitializeDpiAwarenessAndDpiScales();

                _worldTransform = new MatrixTransform(
                    new Matrix(
                        _currentDpiScale.DpiScaleX, 0,
                        0 , _currentDpiScale.DpiScaleY,
                        0 , 0));

                //
                // Register CompositionTarget with MediaContext.
                //
                MediaContext.RegisterICompositionTarget(Dispatcher, this);

                // Initialize dispatcher timer to work-around a restore issue.
                _restoreDT = new DispatcherTimer();
                _restoreDT.Tick += new EventHandler(InvalidateSelf);
                _restoreDT.Interval = TimeSpan.FromMilliseconds(100);

                exceptionThrown = false;
            }
            finally
            {
                //
                // If exception has occurred after we attached this target to
                // the window, we need to detach from this window. Otherwise, window
                // will be left in a state when no other HwndTarget can be created
                // for it.
                //
                if(exceptionThrown)
                {
                    #pragma warning suppress 6031 // Return value ignored on purpose.
                    VisualTarget_DetachFromHwnd(hwnd);
                }
            }
        }

        /// <summary>
        /// Ensures the system/primary monitor's DPI scale. Get the process DPI awareness.
        /// </summary>
        /// <remarks>Helper for constructor</remarks>
        private void InitializeDpiAwarenessAndDpiScales()
        {
            // Only do this once to get:
            // 1. Process DPI Awareness
            // 2. System/Primary monitor's DPI, store it in the first entry of the static array UIElement::MonitorDPIScaleX/Y.
            // Primary Monitor's DPI is needed in two cases :
            //  i) When process is System DPI Aware, then we draw it as per system DPI.
            //  ii) As a fallback value in case of failure.
            lock (s_lockObject)
            {
                if (!AppManifestProcessDpiAwareness.HasValue)
                {
                    PROCESS_DPI_AWARENESS appManifestProcessDpiAwareness;
                    PROCESS_DPI_AWARENESS processDpiAwareness;

                    GetProcessDpiAwareness(_hWnd, out appManifestProcessDpiAwareness, out processDpiAwareness);

                    AppManifestProcessDpiAwareness = appManifestProcessDpiAwareness;
                    ProcessDpiAwareness = processDpiAwareness;

                    DpiUtil.UpdateUIElementCacheForSystemDpi(DpiUtil.GetSystemDpi());
                }
            }

            // Initialize DpiAwarenessContext (DPI_AWARENESS_CONTEXT) every
            // time the HwndTarget constructor runs- this can change for each HWND
            DpiAwarenessContext = (DpiAwarenessContextValue)DpiUtil.GetDpiAwarenessContext(_hWnd);
            _currentDpiScale = GetDpiScaleForWindow(_hWnd);
        }

        /// <summary>
        /// Obtains the DPI awareness of the first process from
        /// which an HWND is used to instantiate an <see cref="HwndTarget"/>.
        /// In most cases, this is same process as the WPF application itself.
        /// </summary>
        /// <remarks>
        /// Helper for <see cref="InitializeDpiAwarenessAndDpiScales"/> which in turn
        /// is a helper for constructor.
        ///
        /// This can't be done in the static constructor due to the dependence
        /// on the HWND being passed through the instance constructor
        ///
        /// Note that all statics that depend on the inital HWND for intialization
        /// must be represented as a nullable value.
        /// </remarks>
        private static void GetProcessDpiAwareness(
            IntPtr hWnd,
            out PROCESS_DPI_AWARENESS appManifestProcessDpiAwareness,
            out PROCESS_DPI_AWARENESS processDpiAwareness)
        {
            // 1. Initialize (static) AppManifestProcessDpiAwareness
            // 2. Initalize (static) ProcessDpiAwareness
            appManifestProcessDpiAwareness = DpiUtil.GetProcessDpiAwareness(hWnd);

            // Don't check for AppContext flag here. We just want to
            // inventory process characteristics here
            if (IsPerMonitorDpiScalingEnabled)
            {
                processDpiAwareness = appManifestProcessDpiAwareness;
            }
            else
            {
                // 'legacy' values can either be 'system aware' or 'unaware'
                processDpiAwareness = DpiUtil.GetLegacyProcessDpiAwareness();
            }
        }

        /// <summary>
        /// If process is Per Monitor DPI aware, returns the DPI scale factor of
        /// the window - which is typically also the DPI scale factor
        /// of the monitor on which the <paramref name="hWnd"/> is displayed.
        /// Otherwise returns the system DPI.
        /// </summary>
        /// <remarks>
        ///     We used to identify the DPI of an HWND by doing the following:
        ///         int dpiX, dpiY;
        ///
        ///         var hMon = User32!MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST)
        ///         shcore!GetDpiForMonitor(hMon, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
        ///
        ///     On Windows desktop, DPI scale factor on X and Y axis are equal (i.e., dpiX == dpiY), and
        ///     Windows provides a simpler API to obtain this value as:
        ///         var dpi = dpiX = dpiY = user32!GetDpiForWindow(hwnd);
        ///
        ///     The nice thing about the GetDpiForWindow API is that it will work correctly when dealing
        ///     with mixed mode DPI scenarios, when the process might be per-monitor DPI aware, yet
        ///     an individual HWND might be system-aware or unaware, resulting in a DPI scale factor
        ///     that is different than that of the nearest monitor.
        ///
        ///     In other words, in the few places scenarios that user32!GetDpiForWindow generates different
        ///     results compard to the older approach, it enhances our implementation and offers implict
        ///     bug fixes.
        /// </remarks>
        private static DpiScale2 GetDpiScaleForWindow(IntPtr hWnd)
        {
            DpiScale2 dpiScale = null;
            if (IsPerMonitorDpiScalingEnabled)
            {
                // When IsPerMonitorDpiScalignEnabled==true, it just means that we are
                // running on OS >= Windows 10 RS2, and the application has not requested
                // (via an AppContext switch) turning off of WPF's High DPI logic, if relevant
                // Under these circumstances, we can simply rely upon the DPI information
                // reported by the HWND itself, irrespective of whether the HWND is Per-Monitor aware,
                // System-Aware, or Unaware.
                dpiScale = DpiUtil.GetWindowDpi(hWnd, fallbackToNearestMonitorHeuristic: false);
            }
            else if (ProcessDpiAwareness.HasValue)
            {
                if (IsProcessSystemAware == true)
                {
                    dpiScale = DpiUtil.GetSystemDpiFromUIElementCache();
                }
                else if (IsProcessUnaware == true)
                {
                    dpiScale = DpiScale2.FromPixelsPerInch(DpiUtil.DefaultPixelsPerInch, DpiUtil.DefaultPixelsPerInch);
                }
            }

            if (dpiScale == null)
            {
                // The Window DPI could not be found likely because HwndTarget statics have not
                // been initialized yet. Fall back to legacy logic.
                var dpiAwareness = DpiUtil.GetLegacyProcessDpiAwareness();
                switch (dpiAwareness)
                {
                    case PROCESS_DPI_AWARENESS.PROCESS_SYSTEM_DPI_AWARE:
                        dpiScale = DpiUtil.GetSystemDpi();
                        break;
                    case PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE:
                        dpiScale = IsPerMonitorDpiScalingEnabled
                            ? DpiUtil.GetWindowDpi(hWnd, fallbackToNearestMonitorHeuristic: false)
                            : DpiUtil.GetSystemDpi();
                        break;
                    case PROCESS_DPI_AWARENESS.PROCESS_DPI_UNAWARE:
                    default:
                        dpiScale = DpiScale2.FromPixelsPerInch(
                            DpiUtil.DefaultPixelsPerInch,
                            DpiUtil.DefaultPixelsPerInch);
                        break;
                }
            }

            return dpiScale;
        }

        /// <summary>
        /// We will use the parent HWND of WS_POPUP windows
        /// to determine the DPI. There can be a popup
        /// within a popup, so we obtain the top-level HWND parent.
        /// </summary>
        /// <remarks>
        /// In the past, we used to treat WS_CHILD windows similar to
        /// WS_POPUP windows, and looked to the top-level HWND to determine
        /// the DPI. We don't do so anymore given child-level mixed mode
        /// DPI is possible in Windows.
        ///
        /// We might consider rendering popup's at their native DPI's as well,
        /// eventually
        /// </remarks>
        private static HandleRef NormalizeWindow(HandleRef hWnd, bool normalizeChildWindows, bool normalizePopups)
        {
            HandleRef normalizedHwnd = hWnd;
            Debug.Assert(normalizedHwnd.Handle != IntPtr.Zero);

            object wrapperObject = hWnd.Wrapper;

            int dwMask =
                (normalizeChildWindows ? NativeMethods.WS_CHILD : 0) |
                (normalizePopups ? NativeMethods.WS_POPUP : 0);

            int style = NativeMethods.IntPtrToInt32((IntPtr)SafeNativeMethods.GetWindowStyle(hWnd, false));
            if ((style & dwMask) != 0)
            {
                IntPtr hwndParent = IntPtr.Zero;
                do
                {
                    try
                    {
                        hwndParent = UnsafeNativeMethods.GetParent(normalizedHwnd);
                    }
                    // Call to GetParent can throw an exception in the following scenarios:
                    // 1) The window is a top - level window that is unowned or does not have the WS_POPUP style.
                    // 2) The owner window has WS_POPUP style.
                    // In either of the situations, the right thing to do is obtain the owner and let the loop continue.
                    catch (Win32Exception)
                    {
                        hwndParent = UnsafeNativeMethods.GetWindow(normalizedHwnd, NativeMethods.GW_OWNER);
                    }

                    if (hwndParent != IntPtr.Zero)
                    {
                        normalizedHwnd = new HandleRef(wrapperObject, hwndParent);
                    }
} while (hwndParent != IntPtr.Zero);
}

            Debug.Assert(normalizedHwnd.Handle != IntPtr.Zero);
            return normalizedHwnd;
        }

        /// <summary>
        /// AttachToHwnd
        /// </summary>
        private void AttachToHwnd(IntPtr hwnd)
        {
            int processId = 0;
            int threadId = UnsafeNativeMethods.GetWindowThreadProcessId(
                new HandleRef(this, hwnd),
                out processId
                );

            if (!UnsafeNativeMethods.IsWindow(new HandleRef(this, hwnd)))
            {
                throw new ArgumentException(
                    SR.Get(SRID.HwndTarget_InvalidWindowHandle),
                    "hwnd"
                    );
            }
            else if (processId != SafeNativeMethods.GetCurrentProcessId())
            {
                throw new ArgumentException(
                    SR.Get(SRID.HwndTarget_InvalidWindowProcess),
                    "hwnd"
                    );
            }
            else if (threadId != SafeNativeMethods.GetCurrentThreadId())
            {
                throw new ArgumentException(
                    SR.Get(SRID.HwndTarget_InvalidWindowThread),
                    "hwnd"
                    );
            }

            int hr = VisualTarget_AttachToHwnd(hwnd);

            if (HRESULT.Failed(hr))
            {
                if (hr == unchecked((int)0x80070005)) // E_ACCESSDENIED
                {
                    throw new InvalidOperationException(
                        SR.Get(SRID.HwndTarget_WindowAlreadyHasContent)
                        );
                }
                else
                {
                    HRESULT.Check(hr);
                }
            }

            EnsureNotificationWindow();
            _notificationWindowHelper.AttachHwndTarget(this);
            UnsafeNativeMethods.WTSRegisterSessionNotification(hwnd, NativeMethods.NOTIFY_FOR_THIS_SESSION);
        }

        [DllImport(DllImport.MilCore, EntryPoint = "MilVisualTarget_AttachToHwnd")]
        internal static extern int VisualTarget_AttachToHwnd(
            IntPtr hwnd
            );


        [DllImport(DllImport.MilCore, EntryPoint = "MilVisualTarget_DetachFromHwnd")]
        internal static extern int VisualTarget_DetachFromHwnd(
            IntPtr hwnd
            );

        internal void InvalidateRenderMode()
        {
            RenderingMode mode =
                RenderMode == RenderMode.SoftwareOnly ? RenderingMode.Software : RenderingMode.Default;

            //
            // If ForceSoftwareRendering is set then the transport is connected to a client (magnifier) that cannot
            // handle our transport protocol version. Therefore we force software rendering so that the rendered
            // content is available through NTUser redirection. If software is not allowed an exception is thrown.
            //
            if (MediaSystem.ForceSoftwareRendering)
            {
                if (mode == RenderingMode.Hardware ||
                    mode == RenderingMode.HardwareReference)
                {
                    throw new InvalidOperationException(SR.Get(SRID.HwndTarget_HardwareNotSupportDueToProtocolMismatch));
                }
                else
                {
                    Debug.Assert(mode == RenderingMode.Software || mode == RenderingMode.Default);
                    // If the mode is default we can chose what works. When we have a mismatched transport protocol version
                    // we need to fallback to software rendering.
                    mode = RenderingMode.Software;
                }
            }

            //Obtain compatibility flags set in the application
            bool? enableMultiMonitorDisplayClipping =
                System.Windows.CoreCompatibilityPreferences.EnableMultiMonitorDisplayClipping;

            if (enableMultiMonitorDisplayClipping != null)
            {
                // The flag is explicitly set by the user in application manifest
                mode |= RenderingMode.IsDisableMultimonDisplayClippingValid;

                if (!enableMultiMonitorDisplayClipping.Value)
                {
                    mode |= RenderingMode.DisableMultimonDisplayClipping;
                }
            }

            // Select the render target initialization flags based on the requested
            // rendering mode.

            DUCE.ChannelSet channelSet = MediaContext.From(Dispatcher).GetChannels();
            DUCE.Channel channel = channelSet.Channel;

            DUCE.CompositionTarget.SetRenderingMode(
                _compositionTarget.GetHandle(channel),
                (MILRTInitializationFlags)mode,
                channel);
        }

        /// <summary>
        /// Specifies the render mode preference for the window.
        /// </summary>
        /// <remarks>
        ///     This property specifies a preference, it does not necessarily change the actual
        ///     rendering mode.  Among other things, this can be trumped by the registry settings.
        ///     <para/>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to set this property.
        /// </remarks>
        public RenderMode RenderMode
        {
            get
            {
                return _renderModePreference.Value;
            }

            // Note: We think it is safe to expose this in partial trust, but doing so would suggest
            // we should also expose HwndSource (the only way to get to the HwndTarget instance).
            // We don't want to bite off that much exposure at this point in the product, so we enforce
            // that this is not accessible from partial trust for now.
            set
            {
                if (value != RenderMode.Default && value != RenderMode.SoftwareOnly)
                {
                    throw new System.ComponentModel.InvalidEnumArgumentException("value", (int)value, typeof(RenderMode));
                }

                _renderModePreference.Value = value;

                InvalidateRenderMode();
            }
        }

        /// <summary>
        /// Dispose cleans up the state associated with HwndTarget.
        /// </summary>
        public override void Dispose()
        {
           // Its outside the try finally block because we want the exception to be
           // thrown if we are on a different thread and we don't want to call Dispose
           // on base class in that case.
           VerifyAccess();

           try
           {
                // According to spec: Dispose should not raise exception if called multiple times.
                // This test is needed because the HwndTarget is Disposed from both the media contex and
                // the hwndsrc.
                if (!IsDisposed)
                {
                    RootVisual = null;

                    HRESULT.Check(VisualTarget_DetachFromHwnd(_hWnd));

                    //
                    // Unregister this CompositionTarget from the MediaSystem.
                    //
                    MediaContext.UnregisterICompositionTarget(Dispatcher, this);

                    if (_notificationWindowHelper != null &&
                        _notificationWindowHelper.DetachHwndTarget(this))
                    {
                        _notificationWindowHelper.Dispose();
                        _notificationWindowHelper = null;
                    }

                    // Unregister for Fast User Switching messages
                    UnsafeNativeMethods.WTSUnRegisterSessionNotification(_hWnd);
                }
}
            finally
            {
                base.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// This method is used to create all uce resources either on Startup or session connect
        /// </summary>
        internal override void CreateUCEResources(DUCE.Channel channel, DUCE.Channel outOfBandChannel)
        {
            // create visual target resources
            // this forces the creation of the media context if we don't already have one.

            base.CreateUCEResources(channel, outOfBandChannel);

            Debug.Assert(!_compositionTarget.IsOnChannel(channel));
            Debug.Assert(!_compositionTarget.IsOnChannel(outOfBandChannel));

            //
            // For each HwndTarget we are building some structures in the UCE.
            // This includes spinning up a UCE render target. We need to commit the
            // batch for those changes right away, since we need to be able to process
            // the invalidate packages that we send down on WM_PAINTs. If we don't commit
            // right away a WM_PAINT can get fired before we get a chance to commit
            // the batch.
            //

            //
            // First we create the composition target, composition context, and the composition root node.
            // Note, that composition target will be created out of band because invalidate
            // command is also sent out of band and that can occur before current channel is committed.
            // We would like to avoid commiting channel here to prevent visual artifacts.
            //

            bool resourceCreated = _compositionTarget.CreateOrAddRefOnChannel(this, outOfBandChannel, DUCE.ResourceType.TYPE_HWNDRENDERTARGET);
            Debug.Assert(resourceCreated);
            _compositionTarget.DuplicateHandle(outOfBandChannel, channel);
            outOfBandChannel.CloseBatch();
            outOfBandChannel.Commit();

            DUCE.CompositionTarget.HwndInitialize(
                _compositionTarget.GetHandle(channel),
                _hWnd,
                _hwndClientRectInScreenCoords.right - _hwndClientRectInScreenCoords.left,
                _hwndClientRectInScreenCoords.bottom - _hwndClientRectInScreenCoords.top,
                MediaSystem.ForceSoftwareRendering,
                (int)DpiAwarenessContext,
                _currentDpiScale,
                channel
                );

            DUCE.ResourceHandle hWorldTransform = ((DUCE.IResource)_worldTransform).AddRefOnChannel(channel);

            DUCE.CompositionNode.SetTransform(
                _contentRoot.GetHandle(channel),
                hWorldTransform,
                channel);

            DUCE.CompositionTarget.SetClearColor(
                _compositionTarget.GetHandle(channel),
                _backgroundColor,
                channel);

            //
            // Set initial state on the visual target.
            //

            Rect clientRect = new Rect(
                0,
                0,
                (float)(Math.Ceiling((double)(_hwndClientRectInScreenCoords.right - _hwndClientRectInScreenCoords.left))),
                (float)(Math.Ceiling((double)(_hwndClientRectInScreenCoords.bottom - _hwndClientRectInScreenCoords.top))));

            StateChangedCallback(
                new object[]
                {
                    HostStateFlags.WorldTransform |
                    HostStateFlags.ClipBounds,
                    _worldTransform.Matrix,
                    clientRect
                });

            DUCE.CompositionTarget.SetRoot(
                _compositionTarget.GetHandle(channel),
                _contentRoot.GetHandle(channel),
                channel);

            // reset the disable cookie when creating the slave resource. This happens when creating the
            // managed resource and on handling a connect.
            _disableCookie = 0;

            //
            // Finally, update window settings to reflect the state of this object.
            // Because CreateUCEResources is called for each channel, only call
            // UpdateWindowSettings on that channel this time.
            //
            DUCE.ChannelSet channelSet;
            channelSet.Channel = channel;
            channelSet.OutOfBandChannel = outOfBandChannel;
            UpdateWindowSettings(_isRenderTargetEnabled, channelSet);
        }

        /// <summary>
        /// This method is used to release all uce resources either on Shutdown or session disconnect
        /// </summary>
        internal override void ReleaseUCEResources(DUCE.Channel channel, DUCE.Channel outOfBandChannel)
        {
            if (_compositionTarget.IsOnChannel(channel))
            {
                //
                // If we need to flush the batch we need to render first all visual targets that
                // are still registered with the MediaContext to avoid strutural tearing.


                // Set the composition target root node to null.
                DUCE.CompositionTarget.SetRoot(
                    _compositionTarget.GetHandle(channel),
                    DUCE.ResourceHandle.Null,
                    channel);

                _compositionTarget.ReleaseOnChannel(channel);
            }

            if (_compositionTarget.IsOnChannel(outOfBandChannel))
            {
                _compositionTarget.ReleaseOnChannel(outOfBandChannel);
            }

            DUCE.ResourceHandle hWorldTransform = ((DUCE.IResource)_worldTransform).GetHandle(channel);
            if (!hWorldTransform.IsNull)
            {
                // Release the world transform from this channel if it's currently on the channel.
                ((DUCE.IResource)_worldTransform).ReleaseOnChannel(channel);
            }

            // release all the visual target resources.
            base.ReleaseUCEResources(channel, outOfBandChannel);
        }

        /// <summary>
        /// Handler for WM_DPICHANGED message
        /// </summary>
        /// <param name="wParam">
        /// The HIWORD of the wParam contains the Y-axis value of the new dpi of the window.
        /// the LOWORD of the wParam contains the X-axis value of the new DPI of the
        /// window. For example, 96, 120, 144, or 192. The values of the X-axis and Y-axis are identical
        /// for Windows apps.
        /// </param>
        /// <param name="lParam">
        /// Contains pointer to a RECT structure that provides the suggested
        /// size and position of the current window scaled for the new DPI. The expectation
        /// is that apps will reposition and resize the windows based on the suggestions
        /// provided by lParam when handling this message
        /// </param>
        /// <returns>true if message is handled, false otherwise</returns>
        private bool HandleDpiChangedMessage(IntPtr wParam, IntPtr lParam)
        {
            bool handled = false;
            if (IsPerMonitorDpiScalingEnabled)
            {
                var hwndSource = HwndSource.FromHwnd(_hWnd);
                if (hwndSource != null)
                {
                    var oldDpi = _currentDpiScale;
                    var newDpi =
                        DpiScale2.FromPixelsPerInch(
                            NativeMethods.SignedLOWORD(wParam),
                            NativeMethods.SignedHIWORD(wParam));
                    if (oldDpi != newDpi)
                    {
                        var nativeRect =
                            UnsafeNativeMethods.PtrToStructure<NativeMethods.RECT>(lParam);
                        var suggestedRect =
                            new Rect(nativeRect.left, nativeRect.top, nativeRect.Width, nativeRect.Height);

                        hwndSource.ChangeDpi(
                            new HwndDpiChangedEventArgs(oldDpi, newDpi, suggestedRect));

                        handled = true;
                    }
                }
            }

            return handled;
        }

        /// <summary>
        /// Handler for WM_DPICHANGED_AFTERPARENT
        /// </summary>
        /// <returns>True if the message is handled, False otherwise</returns>
        private bool HandleDpiChangedAfterParentMessage()
        {
            bool handled = false;

            if (IsPerMonitorDpiScalingEnabled)
            {
                var oldDpi = _currentDpiScale;
                var newDpi = GetDpiScaleForWindow(_hWnd);

                if (oldDpi != newDpi)
                {
                    var hwndSource = HwndSource.FromHwnd(_hWnd);
                    if (hwndSource != null)
                    {
                        // During DPI change (and at other times), the parent
                        // is expected to layout the child. At this point, that layout process
                        // is expected to have been completed, and the new
                        // client rect is whatever the *current* client rect
                        // already happens to be.
                        var rcClient = SafeNativeMethods.GetClientRect(_hWnd.MakeHandleRef(this));
                        var clientRect =
                            new Rect(
                                rcClient.left,
                                rcClient.top,
                                rcClient.right - rcClient.left,
                                rcClient.bottom - rcClient.top);

                        hwndSource.ChangeDpi(new HwndDpiChangedAfterParentEventArgs(oldDpi, newDpi, clientRect));
                        handled = true;
                    }
                }
            }

            return handled;
        }

        /// <summary>
        /// The HwndTarget needs to see all windows messages so that
        /// it can appropriately react to them.
        /// </summary>
        internal IntPtr HandleMessage(WindowMessage msg, IntPtr wparam, IntPtr lparam)
        {
            IntPtr result = Unhandled;

            // Handle custom messages with IDs stored in a non-const
            // field here.
            //
            // Handle all other messages with const IDs in the switch-block
            // further down.
            //
            // Note that the guard for 'IsDisposed' is further down, and
            // this if/else block is not guarded by that. Be careful of this
            // fact when adding additional custom-message handling here. So far,
            // the custom-messages being handled here seem immune to the possibility
            // of this HwndTarget having been disposed.
            if (msg == s_DisplayDevicesAvailabilityChanged)
            {
                _displayDevicesAvailable = (wparam.ToInt32() != 0);
                if (_displayDevicesAvailable && _wasWmPaintProcessingDeferred)
                {
                    UnsafeNativeMethods.InvalidateRect(_hWnd.MakeHandleRef(this), IntPtr.Zero, true);
                    DoPaint();
                }
            }
            else if (msg == s_updateWindowSettings)
            {
                // Make sure we enable the render target if the window is visible.
                if (SafeNativeMethods.IsWindowVisible(_hWnd.MakeHandleRef(this)))
                {
                    UpdateWindowSettings(true);
                }
            }
            else if (msg == s_needsRePresentOnWake)
            {
                //
                // If the session is disconnected (due to machine lock) or in a suspended power
                // state, don't invalidate the window immediately, unless
                // we're within the allowed failure window after an unlock (See member comments on
                // _lastWakeOrUnlockEvent and _allowedPresentFailureDelay for explanation).
                // Save the invalidate for when the wake/unlock does occur, so that we avoid the
                // WM_PAINT/Invalidate storm, by setting _needsRePresentOnWake.
                //
                // If we've previously received this message and we don't know that we're
                // disconnected or suspended, we may be a window that has been created since a
                // lock/disconnect occurred, and thus didn't get the message. Set the
                // _nedsRePresentOnWake flag in this case too.
                //

                TimeSpan delta = DateTime.Now - _lastWakeOrUnlockEvent;
                bool fWithinPresentRetryWindow = delta.TotalSeconds < _allowedPresentFailureDelay;

                // Either display devices are available, or we are in a 'don't-care' state - ie..,
                // running under a non-interactive Window Station.
                // Note that running under a non-interactive Window Station is not supported by WPF,
                // but we try to keep things working anyway.
                bool displayDevicesAvailable = _displayDevicesAvailable || MediaContext.ShouldRenderEvenWhenNoDisplayDevicesAreAvailable;

                if (_isSessionDisconnected || _isSuspended ||
                    (_hasRePresentedSinceWake && !fWithinPresentRetryWindow) ||
                    !displayDevicesAvailable)
                {
                    _needsRePresentOnWake = true;
                }
                else
                {
                    if (!_hasRePresentedSinceWake || fWithinPresentRetryWindow)
                    {
                        UnsafeNativeMethods.InvalidateRect(_hWnd.MakeHandleRef(this), IntPtr.Zero , true);
                        DoPaint();
                        _hasRePresentedSinceWake = true;
                    }
                }

                return Handled;
            }


            if (IsDisposed)
            {
                return result;
            }

            switch (msg)
                {
                case WindowMessage.WM_DPICHANGED:
                    result = HandleDpiChangedMessage(wparam, lparam) ? Handled : Unhandled;
                    break;
                case WindowMessage.WM_DPICHANGED_AFTERPARENT:
                    result = HandleDpiChangedAfterParentMessage() ? Handled : Unhandled;
                    break;
                case WindowMessage.WM_NCCREATE:
                    // user32!GetDpiForWindow is only supported on Windows 10 v1607 and later
                    // IsPerMonitorDpiScalingEnabled tests for this indirectly
                    if (IsProcessPerMonitorDpiAware == true)
                    {
                        UnsafeNativeMethods.EnableNonClientDpiScaling(NormalizeWindow(new HandleRef(this, _hWnd), normalizeChildWindows: false, normalizePopups: true));
                    }
                    break;
                case WindowMessage.WM_ERASEBKGND:
                    result = Handled; // Indicates that this message is handled.
                    break;

                case WindowMessage.WM_PAINT:
                    // If the current Window Station is non-interactive (i.e., NOT WinSta0)
                    // then we will never find usable display devices. Normally,
                    // WPF is not supported when running in a non-interactive Window
                    // Station, for e.g., a typical SCM service calling into WPF UI
                    // oriented API's is unsupported, and has never been tested. Some
                    // applications nevertheless do this. When we notice that we are running
                    // in a non-interactive Window Station, we will try to keep on rendering as best
                    // as we can, ignoring the fact that actual display devices aren't
                    // available in this configuration.
                    if (_displayDevicesAvailable || MediaContext.ShouldRenderEvenWhenNoDisplayDevicesAreAvailable)
                    {
                        _wasWmPaintProcessingDeferred = false;
                        DoPaint();
                        result = Handled;
                    }
                    else
                    {
                        _wasWmPaintProcessingDeferred = true;
                    }
                    break;

                case WindowMessage.WM_SIZE:

                    //
                    //      When locked on downlevel, MIL stops rendering and invalidates the
                    //      window causing WM_PAINT. When the window is layered and minimized
                    //      before the lock, it'll never get the WM_PAINT on unlock and the MIL will
                    //      never get out of the "don't render" state.
                    //
                    //      To work around this, we will invalidate ourselves on restore and not
                    //      render while minimized.
                    //

                    // If the Window is in minimized state, don't do layout. otherwise, in some cases, it would
                    // pollute the measure data based on the Minized window size.
                    if (NativeMethods.IntPtrToInt32(wparam) != NativeMethods.SIZE_MINIMIZED)
                    {
                        // Rendering sometimes does not refresh propertly,and results in 
                        // rendering artifacts that look like a patchwork of black unpainted squares. 
                        // This is is caused by a race condition in Windows 7 (and possibly
                        // Windows Vista, though we haven't observed the effect there).
                        // Sometimes when we restore from minimized, when we present into the newly
                        // resized window, the present silently fails, and we end up with garbage in
                        // our window buffer. This work around queues another invalidate to occur after 100ms.
                        if (_isMinimized)
                        {
                            _restoreDT.Start();
                        }

                        _isMinimized = false;
                        DoPaint();

                        OnResize();
                    }
                    else
                    {
                        _isMinimized = true;
                    }

                    break;

                case WindowMessage.WM_SETTINGCHANGE:
                    if (OnSettingChange(NativeMethods.IntPtrToInt32(wparam)))
                    {
                        UnsafeNativeMethods.InvalidateRect(_hWnd.MakeHandleRef(this), IntPtr.Zero , true);
                    }
                    break;

                case WindowMessage.WM_GETOBJECT:
                    result = CriticalHandleWMGetobject( wparam, lparam, RootVisual, _hWnd );
                    break;

                case WindowMessage.WM_WINDOWPOSCHANGING:
                    OnWindowPosChanging(lparam);
                    break;

                case WindowMessage.WM_WINDOWPOSCHANGED:
                    OnWindowPosChanged(lparam);
                    break;

                case WindowMessage.WM_SHOWWINDOW:
                    bool enableRenderTarget = (wparam != IntPtr.Zero);
                    OnShowWindow(enableRenderTarget);
                    //
                    //  
                    //      When locked on downlevel, MIL stops rendering and invalidates the
                    //      window causing WM_PAINT. When the window is layered and hidden
                    //      before the lock, it won't get the WM_PAINT on unlock and the MIL will
                    //      never get out of the "don't render" state if the window is shown again.
                    //
                    //      To work around this, we will invalidate the window ourselves on Show().
                    if (enableRenderTarget)
                    {
                        DoPaint();
                    }
                    break;

                case WindowMessage.WM_ENTERSIZEMOVE:
                    OnEnterSizeMove();
                    break;

                case WindowMessage.WM_EXITSIZEMOVE:
                    OnExitSizeMove();
                    break;

                case WindowMessage.WM_STYLECHANGING:
                    unsafe
                    {
                        NativeMethods.STYLESTRUCT * styleStruct = (NativeMethods.STYLESTRUCT *) lparam;

                        if ((int)wparam == NativeMethods.GWL_EXSTYLE)
                        {
                            if(UsesPerPixelOpacity)
                            {
                                // We need layered composition to accomplish per-pixel opacity.
                                //
                                styleStruct->styleNew |= NativeMethods.WS_EX_LAYERED;
                            }
                            else
                            {
                                // No properties that require layered composition exist.
                                // Make sure the layered bit is off.
                                //
                                // Note: this prevents an external program from making
                                // us system-layered (if we are a top-level window).
                                //
                                // If we are a child window, we still can't stop our
                                // parent from being made system-layered, and we will
                                // end up leaving visual artifacts on the screen under
                                // WindowsXP.
                                //
                                styleStruct->styleNew &= (~NativeMethods.WS_EX_LAYERED);
                            }
                        }
                    }

                    break;

                case WindowMessage.WM_STYLECHANGED:
                    unsafe
                    {
                        bool updateWindowSettings = false;

                        NativeMethods.STYLESTRUCT * styleStruct = (NativeMethods.STYLESTRUCT *) lparam;

                        if ((int)wparam == NativeMethods.GWL_STYLE)
                        {
                            bool oldIsChild = (styleStruct->styleOld & NativeMethods.WS_CHILD) == NativeMethods.WS_CHILD;
                            bool newIsChild = (styleStruct->styleNew & NativeMethods.WS_CHILD) == NativeMethods.WS_CHILD;
                            updateWindowSettings = (oldIsChild != newIsChild);
                        }
                        else
                        {
                            bool oldIsRTL = (styleStruct->styleOld & NativeMethods.WS_EX_LAYOUTRTL) == NativeMethods.WS_EX_LAYOUTRTL;
                            bool newIsRTL  = (styleStruct->styleNew & NativeMethods.WS_EX_LAYOUTRTL) == NativeMethods.WS_EX_LAYOUTRTL;
                            updateWindowSettings = (oldIsRTL != newIsRTL);
                        }

                        if(updateWindowSettings)
                        {
                            UpdateWindowSettings();
                        }
                    }

                    break;

                //
                //      When a Fast User Switch happens, MIL gets an invalid display error when trying to
                //      render and they invalidate the window resulting in us getting a WM_PAINT. For
                //      layered windows, we get the WM_PAINT immediately which causes us to
                //      tell MIL to render and the cycle repeats. On Vista, this creates an infinite loop.
                //      Downlevel there isn't a loop, but the layered window will never update again.
                //
                //      To work around this problem, we'll make sure not to tell MIL to render when
                //      we're switched out and will render on coming back.
                //
                case WindowMessage.WM_WTSSESSION_CHANGE:
                    // If this message did not originate in our workstation session, then ignore it.
                    if (_sessionId.HasValue && (_sessionId.Value != lparam.ToInt32()))
                    {
                        break;
                    }
                    switch (NativeMethods.IntPtrToInt32(wparam))
                    {
                        // Session is disconnected. Due to:
                        // 1. Switched to a different user
                        // 2. TS logoff
                        // 3. Screen locked
                        case NativeMethods.WTS_CONSOLE_DISCONNECT:
                        case NativeMethods.WTS_REMOTE_DISCONNECT:
                        case NativeMethods.WTS_SESSION_LOCK:
                            _hasRePresentedSinceWake = false;
                            _isSessionDisconnected = true;

                            _lastWakeOrUnlockEvent = DateTime.MinValue;

                            break;

                        // Session is reconnected. See above
                        case NativeMethods.WTS_CONSOLE_CONNECT:
                        case NativeMethods.WTS_REMOTE_CONNECT:
                        case NativeMethods.WTS_SESSION_UNLOCK:
                            _isSessionDisconnected = false;
                            if (_needsRePresentOnWake || _wasWmPaintProcessingDeferred)
                            {
                                UnsafeNativeMethods.InvalidateRect(_hWnd.MakeHandleRef(this), IntPtr.Zero , true);
                                _needsRePresentOnWake = false;
                            }
                            DoPaint();

                            _lastWakeOrUnlockEvent = DateTime.Now;

                            break;

                        default:
                            break;
                    }

                    break;

                //
                //      Downlevel, if we try to present a layered window while suspended the app will crash.
                //      This has been fixed in Vista but we still need to work around it for older versions
                //      by not invalidating while suspended.
                //
                case WindowMessage.WM_POWERBROADCAST:
                    switch (NativeMethods.IntPtrToInt32(wparam))
                    {
                        case NativeMethods.PBT_APMSUSPEND:
                            _isSuspended = true;
                            _hasRePresentedSinceWake = false;

                            _lastWakeOrUnlockEvent = DateTime.MinValue;

                            break;

                        case NativeMethods.PBT_APMRESUMESUSPEND:
                        case NativeMethods.PBT_APMRESUMECRITICAL:
                        case NativeMethods.PBT_APMRESUMEAUTOMATIC:
                            _isSuspended = false;
                            if (_needsRePresentOnWake)
                            {
                                UnsafeNativeMethods.InvalidateRect(_hWnd.MakeHandleRef(this), IntPtr.Zero , true);
                                _needsRePresentOnWake = false;
                            }
                            DoPaint();

                            _lastWakeOrUnlockEvent = DateTime.Now;

                            break;
                        default:
                            break;
                    }
                    break;

                default:
                    break;
            }

            return result;
        }

        private void OnMonitorPowerEvent(object sender, MonitorPowerEventArgs eventArgs)
        {
            OnMonitorPowerEvent(sender, eventArgs.PowerOn, /*paintOnWake*/true);
        }

        private void OnMonitorPowerEvent(object sender, bool powerOn, bool paintOnWake)
        {
            if (powerOn)
            {
                _isSuspended = false;
                if (paintOnWake)
                {
                    if (_needsRePresentOnWake)
                    {
                        UnsafeNativeMethods.InvalidateRect(_hWnd.MakeHandleRef(this), IntPtr.Zero, true);
                        _needsRePresentOnWake = false;
                    }
                    DoPaint();
                }

                _lastWakeOrUnlockEvent = DateTime.Now;
            }
            else
            {
                _isSuspended = true;
                _hasRePresentedSinceWake = false;

                _lastWakeOrUnlockEvent = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Invalidates self, designed to be called as a DispatcherTimer event handler.
        /// </summary>
        private void InvalidateSelf(object s, EventArgs args)
        {
            UnsafeNativeMethods.InvalidateRect(_hWnd.MakeHandleRef(this), IntPtr.Zero, true);
            DispatcherTimer sourceDT = (DispatcherTimer)s;
            if (sourceDT != null)
            {
                Debug.Assert(_restoreDT == sourceDT);

                sourceDT.Stop();
            }
}

        /// <summary>
        /// Paints a rect
        ///
        /// Note: This gets called a lot to help with layered window problems even when
        ///         the window isn't layered, but that's okay because rcPaint will be empty.
        ///
        /// </summary>
        private void DoPaint()
        {
            NativeMethods.PAINTSTRUCT ps = new NativeMethods.PAINTSTRUCT();
            NativeMethods.HDC hdc;

            HandleRef handleRef = new HandleRef(this, _hWnd);
            hdc.h = UnsafeNativeMethods.BeginPaint(handleRef, ref ps);
            int retval = UnsafeNativeMethods.GetWindowLong(handleRef, NativeMethods.GWL_EXSTYLE);

            NativeMethods.RECT rcPaint = new NativeMethods.RECT(ps.rcPaint_left, ps.rcPaint_top, ps.rcPaint_right, ps.rcPaint_bottom);

            //
            // If we get a BeginPaint with an empty rect then check
            // if this is a special layered, non-redirected window
            // which would mean we need to do a full paint when it
            // won't cause a problem.
            //
            if (rcPaint.IsEmpty
                && ((retval & NativeMethods.WS_EX_LAYERED) != 0)
                && !UnsafeNativeMethods.GetLayeredWindowAttributes(_hWnd.MakeHandleRef(this), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero)
                && !_isSessionDisconnected
                && !_isMinimized
                && (!_isSuspended || (UnsafeNativeMethods.GetSystemMetrics(SM.REMOTESESSION) != 0))) // Checking if we are in a remote session works around the fact that power
                                                                                                     // notifications for the server monitor are being broad-casted when the
                                                                                                     // machine is in a non-local TS session.
            {
                rcPaint = new NativeMethods.RECT(
                          0,
                          0,
                          _hwndClientRectInScreenCoords.right - _hwndClientRectInScreenCoords.left,
                          _hwndClientRectInScreenCoords.bottom - _hwndClientRectInScreenCoords.top);
            }

            AdjustForRightToLeft(ref rcPaint, handleRef);

            if (!rcPaint.IsEmpty)
            {
                InvalidateRect(rcPaint);
            }

            UnsafeNativeMethods.EndPaint(_hWnd.MakeHandleRef(this), ref ps);
        }

        internal AutomationPeer EnsureAutomationPeer(Visual root)
        {
            return EnsureAutomationPeer(root, _hWnd);
        }

        internal static AutomationPeer EnsureAutomationPeer(Visual root, IntPtr handle)
        {
            AutomationPeer peer = null;

            if (root.CheckFlagsAnd(VisualFlags.IsUIElement))
            {
                UIElement uiroot = (UIElement)root;

                peer = UIElementAutomationPeer.CreatePeerForElement(uiroot);

                //there is no specific peer for this UIElement, create a generic root
                if(peer == null)
                    peer = uiroot.CreateGenericRootAutomationPeer();

                if(peer != null)
                    peer.Hwnd = handle;
            }

            // This can happen if the root visual is not UIElement. In this case,
            // attempt to find one in the visual tree.
            if (peer == null)
            {
                peer = UIElementAutomationPeer.GetRootAutomationPeer(root, handle);
            }

            if (peer != null)
            {
                peer.AddToAutomationEventList();
            }

            return peer;
        }

        private static IntPtr CriticalHandleWMGetobject(IntPtr wparam, IntPtr lparam, Visual root, IntPtr handle)
        {
            try
            {
                if (root == null)
                {
                    // Valid case, but need to handle separately. For now, return 0 to avoid exceptions
                    // in referencing this later on. Real solution is more complex, see WindowsClient#873800.
                    return IntPtr.Zero;
                }

                AutomationPeer peer = EnsureAutomationPeer(root, handle);
                if (peer == null)
                {
                    return IntPtr.Zero;
                }

                // get the element proxy
                // it's ok to pass the same peer as reference connected peer here because
                // it's guaranteed to be a connected one (it's initialized as root already)
                IRawElementProviderSimple el = ElementProxy.StaticWrap(peer, peer);

                return AutomationInteropProvider.ReturnRawElementProvider(handle, wparam, lparam, el);
            }
#pragma warning disable 56500
            catch (Exception e)
            {
                if(CriticalExceptions.IsCriticalException(e))
                {
                    throw;
                }

                return new IntPtr(Marshal.GetHRForException(e));
            }
#pragma warning restore 56500
        }

        /// <summary>
        ///     Adjusts a RECT to compensate for Win32 RTL conversion logic
        /// </summary>
        /// <remarks>
        ///     When a window is marked with the WS_EX_LAYOUTRTL style, Win32
        ///     mirrors the coordinates during the various translation APIs.
        ///
        ///     Avalon also sets up mirroring transforms so that we properly
        ///     mirror the output since we render to DirectX, not a GDI DC.
        ///
        ///     Unfortunately, this means that our coordinates are already mirrored
        ///     by Win32, and Avalon mirrors them again.  To solve this
        ///     problem, we un-mirror the coordinates from Win32 before painting
        ///     in Avalon.
        /// </remarks>
        /// <param name="rc">
        ///     The RECT to be adjusted
        /// </param>
        /// <param name="handleRef">
        /// </param>
        internal void AdjustForRightToLeft(ref NativeMethods.RECT rc, HandleRef handleRef)
        {
            int windowStyle = SafeNativeMethods.GetWindowStyle(handleRef, true);

            if(( windowStyle & NativeMethods.WS_EX_LAYOUTRTL ) == NativeMethods.WS_EX_LAYOUTRTL)
            {
                NativeMethods.RECT rcClient = new NativeMethods.RECT();
                SafeNativeMethods.GetClientRect(handleRef, ref rcClient);

                int width   = rc.right - rc.left;       // preserve width
                rc.right    = rcClient.right - rc.left; // set right of rect to be as far from right of window as left of rect was from left of window
                rc.left     = rc.right - width;         // restore width by adjusting left and preserving right
            }
        }

        /// <summary>
        /// Force total re-rendering to handle system parameters change
        /// (font smoothing settings, gamma correction, etc.)
        ///</summary>
        ///<returns>true if rerendering was forced</returns>
        private bool OnSettingChange(Int32 firstParam)
        {
            if ( (int)firstParam == (int)NativeMethods.SPI_SETFONTSMOOTHING ||
                 (int)firstParam == (int)NativeMethods.SPI_SETFONTSMOOTHINGTYPE ||
                 (int)firstParam == (int)NativeMethods.SPI_SETFONTSMOOTHINGCONTRAST ||
                 (int)firstParam == (int)NativeMethods.SPI_SETFONTSMOOTHINGORIENTATION ||
                 (int)firstParam == (int)NativeMethods.SPI_SETDISPLAYPIXELSTRUCTURE ||
                 (int)firstParam == (int)NativeMethods.SPI_SETDISPLAYGAMMA ||
                 (int)firstParam == (int)NativeMethods.SPI_SETDISPLAYCLEARTYPELEVEL ||
                 (int)firstParam == (int)NativeMethods.SPI_SETDISPLAYTEXTCONTRASTLEVEL
                )
            {
                HRESULT.Check(MILUpdateSystemParametersInfo.Update());
                return true;
            }

            return false;
        }

        /// <summary>
        /// This function should be called to paint the specified
        /// region of the window along with any other pending
        /// changes.  While this function is generally called
        /// in response to a WM_PAINT it is up to the user to
        /// call BeginPaint and EndPaint or to otherwise validate
        /// the bitmap region.
        /// </summary>
        /// <param name="rcDirty">The rectangle that is dirty.</param>
        private void InvalidateRect(NativeMethods.RECT rcDirty)
        {
            DUCE.ChannelSet channelSet = MediaContext.From(Dispatcher).GetChannels();
            DUCE.Channel channel = channelSet.Channel;
            DUCE.Channel outOfBandChannel = channelSet.OutOfBandChannel;

            // handle InvalidateRect requests only if we have uce resources.
            if (_compositionTarget.IsOnChannel(channel))
            {
                //
                // Send a message with the invalid region to the compositor. We create a little batch to send this
                // out of order.
                //
                DUCE.CompositionTarget.Invalidate(
                    _compositionTarget.GetHandle(outOfBandChannel),
                    ref rcDirty,
                    outOfBandChannel);
            }
        }

        /// <summary>
        /// Calling this function causes us to update state to reflect a
        /// size change of the underlying HWND
        /// </summary>
        private void OnResize()
        {
#if DEBUG
            MediaTrace.HwndTarget.Trace("OnResize");
#endif

            // handle OnResize requests only if we have uce resources.
            if (_compositionTarget.IsOnAnyChannel)
            {
                MediaContext mctx = MediaContext.From(Dispatcher);

                //
                // Let the render target know that window size has changed.
                //

                UpdateWindowSettings();

                //
                // Push client size chnage to the visual target.
                //
                Rect clientRect = new Rect(
                    0,
                    0,
                    (float)(Math.Ceiling((double)(_hwndClientRectInScreenCoords.right - _hwndClientRectInScreenCoords.left))),
                    (float)(Math.Ceiling((double)(_hwndClientRectInScreenCoords.bottom - _hwndClientRectInScreenCoords.top))));

                StateChangedCallback(
                    new object[] { HostStateFlags.ClipBounds, null, clientRect });

                mctx.Resize(this);

                Int32 style = UnsafeNativeMethods.GetWindowLong(_hWnd.MakeHandleRef(this), NativeMethods.GWL_STYLE);
                if (_userInputResize || _usesPerPixelOpacity ||
                    ((style & NativeMethods.WS_CHILD) != 0 && Utilities.IsCompositionEnabled))
                {
                    //
                    // To ensure that the client area and the non-client area resize
                    // together, we need to wait, on resize, for the composition
                    // engine to present the resized frame. The call to CompleteRender
                    // blocks until that happens.
                    //
                    // When the user isn't resizing, the disconnect between client
                    // and non-client isn't as noticeable so we will err on the side
                    // of performance for multi-hwnd apps like Visual Studio.
                    //
                    // We think syncing is always necessary for layered windows.
                    //
                    // For child windows we also need to sync to work-around some DWM issues (see , #782372).
                    //

                    mctx.CompleteRender();
                }
            }
        }


        /// <summary>
        /// Calculates the client and window rectangle in screen coordinates.
        /// Calculates the client rectangle relative to its parent
        /// </summary>
        private void UpdateWindowAndClientCoordinates()
        {
            HandleRef hWnd = _hWnd.MakeHandleRef(this);

            // Update the window rect
            SafeNativeMethods.GetWindowRect(hWnd, ref _hwndWindowRectInScreenCoords);

            // Get the client rect
            NativeMethods.RECT rcClient = new NativeMethods.RECT();
            SafeNativeMethods.GetClientRect(hWnd, ref rcClient);

            // Convert the client rect to screen coordinates, adjusting for RTL
            NativeMethods.POINT ptClientTopLeft = new NativeMethods.POINT(rcClient.left, rcClient.top);
            UnsafeNativeMethods.ClientToScreen(hWnd, ptClientTopLeft);

            NativeMethods.POINT ptClientBottomRight = new NativeMethods.POINT(rcClient.right, rcClient.bottom);
            UnsafeNativeMethods.ClientToScreen(hWnd, ptClientBottomRight);

            if(ptClientBottomRight.x >= ptClientTopLeft.x)
            {
                _hwndClientRectInScreenCoords.left = ptClientTopLeft.x;
                _hwndClientRectInScreenCoords.right = ptClientBottomRight.x;
            }
            else
            {
                // RTL windows will cause the right edge to be on the left...
                _hwndClientRectInScreenCoords.left = ptClientBottomRight.x;
                _hwndClientRectInScreenCoords.right = ptClientTopLeft.x;
            }

            if(ptClientBottomRight.y >= ptClientTopLeft.y)
            {
                _hwndClientRectInScreenCoords.top = ptClientTopLeft.y;
                _hwndClientRectInScreenCoords.bottom = ptClientBottomRight.y;
            }
            else
            {
                // RTL windows will cause the right edge to be on the left...
                // This doesn't affect top/bottom, but the code should be symmetrical.
                _hwndClientRectInScreenCoords.top = ptClientBottomRight.y;
                _hwndClientRectInScreenCoords.bottom = ptClientTopLeft.y;
            }

            // Need to assert that _hwndClientRectInScreenCoords == _hwndWindowRectInScreenCoords
            // when UsesPerPixelOpacity is true
        }

        /// <summary>
        /// Updates <see cref="_worldTransform"/> based on the supplied DPI scale factor
        /// </summary>
        /// <remarks>Called as part of DPI update processing</remarks>
        private void UpdateWorldTransform(DpiScale2 dpiScale)
        {
            // Occasionally, the world transform can be more than
            // a simple DpiScale based transform. This can happen if an
            // HWND is :
            // (a) a child or a popup
            // (b) The hosting behavior of the window is DPI_HOSTING_BEHAVIOR_MIXED
            // If these are true, then we will walk the HWND's all the way up and
            // multiply the DPI scale factors to create the world transform.

            _worldTransform = new MatrixTransform(new Matrix(
                dpiScale.DpiScaleX, 0,
                0, dpiScale.DpiScaleY,
                0, 0));

            // Push the transform to render thread.
            DUCE.ChannelSet channelSet = MediaContext.From(Dispatcher).GetChannels();
            DUCE.Channel channel = channelSet.Channel;

            DUCE.ResourceHandle hWorldTransform = ((DUCE.IResource)_worldTransform).AddRefOnChannel(channel);

            DUCE.CompositionNode.SetTransform(
                _contentRoot.GetHandle(channel),
                hWorldTransform,
                channel);
        }

        /// <summary>
        /// Updates DPI flags and propagates this all the way to the root-visual
        /// </summary>
        /// <remarks>Called as part of DPI update processing</remarks>
        private void PropagateDpiChangeToRootVisual(DpiScale2 oldDpi, DpiScale2 newDpi)
        {
            // Update the static array that stores the actual DpiScales.
            // output is the index that will be set to the visual flags.
            var dpiFlags = DpiUtil.UpdateDpiScalesAndGetIndex(newDpi.PixelsPerInchX, newDpi.PixelsPerInchY);

            if (RootVisual != null)
            {
                // Propagate the visual flags from the RootVisual.
                RecursiveUpdateDpiFlagAndInvalidateMeasure(RootVisual, new DpiRecursiveChangeArgs(dpiFlags, oldDpi, newDpi));
            }
        }

        /// <summary>
        /// Notifies all listeners that <see cref="_worldTransform"/> and
        /// the client rect have changed
        /// </summary>
        /// <remarks>Called as part of DPI update processing</remarks>
        private void NotifyListenersOfWorldTransformAndClipBoundsChanged()
        {
            var clipBounds = new Rect(
                0,
                0,
                _hwndClientRectInScreenCoords.right - _hwndClientRectInScreenCoords.left,
                _hwndClientRectInScreenCoords.bottom - _hwndClientRectInScreenCoords.top);

            StateChangedCallback(
                new object[]
                {
                    HostStateFlags.WorldTransform |
                    HostStateFlags.ClipBounds,
                    _worldTransform.Matrix,
                    clipBounds
                });
        }

        /// <summary>
        /// Resizes and repositions the HWND based on suggestedRect and the new DPI.
        /// </summary>
        internal void OnDpiChanged(HwndDpiChangedEventArgs e)
        {
            var oldDpi = _currentDpiScale;
            var newDpi = new DpiScale2(e.NewDpi);
            _currentDpiScale = newDpi;

            UpdateWorldTransform(newDpi);
            PropagateDpiChangeToRootVisual(oldDpi, newDpi);
            NotifyListenersOfWorldTransformAndClipBoundsChanged();
            NotifyRendererOfDpiChange(afterParent:false);

            // Set the new window size.
            UnsafeNativeMethods.SetWindowPos(
                _hWnd.MakeHandleRef(this),
                new HandleRef(null, IntPtr.Zero), // HWND_TOP
                (int)e.SuggestedRect.Left, (int)e.SuggestedRect.Top, (int)e.SuggestedRect.Width, (int)e.SuggestedRect.Height,
                NativeMethods.SWP_NOZORDER | NativeMethods.SWP_ASYNCWINDOWPOS);
        }

        /// <summary>
        /// Redraws the child-HWND in the new DPI
        /// </summary>
        internal void OnDpiChangedAfterParent(HwndDpiChangedAfterParentEventArgs e)
        {
            var oldDpi = _currentDpiScale;
            var newDpi = new DpiScale2(e.NewDpi);
            _currentDpiScale = newDpi;

            UpdateWorldTransform(newDpi);
            PropagateDpiChangeToRootVisual(oldDpi, newDpi);
            NotifyListenersOfWorldTransformAndClipBoundsChanged();
            NotifyRendererOfDpiChange(afterParent:true);

            // Update the window position
            UnsafeNativeMethods.SetWindowPos(
                _hWnd.MakeHandleRef(this),
                new HandleRef(null, IntPtr.Zero), // HWND_TOP
                (int)e.SuggestedRect.Left, (int)e.SuggestedRect.Top, (int)e.SuggestedRect.Width, (int)e.SuggestedRect.Height,
                NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);

            // Invalidates and repaints the client area
            UnsafeNativeMethods.InvalidateRect(new HandleRef(this, _hWnd), IntPtr.Zero, true);
            DoPaint();
        }

        private void NotifyRendererOfDpiChange(bool afterParent)
        {
            DUCE.ChannelSet channelSet = MediaContext.From(Dispatcher).GetChannels();
            DUCE.Channel channel = channelSet.Channel;

            DUCE.CompositionTarget.ProcessDpiChanged(
                _compositionTarget.GetHandle(channel),
                _currentDpiScale,
                afterParent,
                channel);
        }

        private void RecursiveUpdateDpiFlagAndInvalidateMeasure(DependencyObject d, DpiRecursiveChangeArgs args)
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(d);
            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(d, i);
                if (child != null)
                {
                    RecursiveUpdateDpiFlagAndInvalidateMeasure(child, args);
                }
            }
            Visual visual = d as Visual;
            if (visual != null)
            {
                visual.SetDpiScaleVisualFlags(args);
                UIElement element = d as UIElement;
                element?.InvalidateMeasure();
            }
        }

        private void OnWindowPosChanging(IntPtr lParam)
        {
            _windowPosChanging = true;

            UpdateWindowPos(lParam);
        }

        private void OnWindowPosChanged(IntPtr lParam)
        {
            _windowPosChanging = false;

            UpdateWindowPos(lParam);
        }

        private void UpdateWindowPos(IntPtr lParam)
        {
            //
            // We need to update the window settings used by the render thread when
            // 1) The size or position of the render target needs to change
            // 2) The render target needs to be enabled or disabled.
            //
            // Further, we need to synchronize the render thread during sizing operations.
            // This is because some APIs that the render thread uses (such as
            // UpdateLayeredWindow) have the unintended side-effect of also changing the
            // window size.  We can't let the render thread and the UI thread fight
            // over setting the window size.
            //
            // Generally, Windows sends our window to messages that bracket the size
            // operation:
            // 1) WM_WINDOWPOSCHANGING
            //    Here we synchronize with the render thread, and ask the render thread
            //    to not render to this window for a while.
            // 2) WM_WINDOWPOSCHANGED
            //    This is after the window size has actually been changed, so we tell
            //    the render thread that it can render to the window again.
            //
            // However, there are complications.  Sometimes Windows will send a
            // WM_WINDOWPOSCHANGING without sending a WM_WINDOWPOSCHANGED.  This happens
            // when the window size is not really going to change.  Also note that
            // more than just size/position information is provided by these messages.
            // We'll get these messages when nothing but the z-order changes for instance.
            //


            //
            // The first order of business is to determine if the render target
            // size or position changed.  If so, we need to pass this information to
            // the render thread.
            //
            NativeMethods.WINDOWPOS windowPos = (NativeMethods.WINDOWPOS)UnsafeNativeMethods.PtrToStructure(lParam, typeof(NativeMethods.WINDOWPOS));
            bool isMove = (windowPos.flags & NativeMethods.SWP_NOMOVE) == 0;
            bool isSize = (windowPos.flags & NativeMethods.SWP_NOSIZE) == 0;
            bool positionChanged = (isMove || isSize);
            if (positionChanged)
            {
                //
                // We have found that sometimes we get told that the size or position
                // of the window has changed, when it really hasn't.  So we double
                // check here.  This is critical because we won't be given a
                // WM_WINDOWPOSCHANGED unless the size or position really had changed.
                //
                if (!isMove)
                {
                    // This is just to avoid any possible integer overflow problems.
                    windowPos.x = windowPos.y = 0;
                }
                if (!isSize)
                {
                    // This is just to avoid any possible integer overflow problems.
                    windowPos.cx = windowPos.cy = 0;
                }

                //
                // WINDOWPOS stores the window coordinates relative to its parent.
                // If the parent is NULL, then these are already screen coordinates.
                // Otherwise, we need to convert to screen coordinates.
                //
                NativeMethods.RECT windowRectInScreenCoords = new NativeMethods.RECT(windowPos.x, windowPos.y, windowPos.x + windowPos.cx, windowPos.y + windowPos.cy);
                IntPtr hwndParent = UnsafeNativeMethods.GetParent(new HandleRef(null, windowPos.hwnd));
                if(hwndParent != IntPtr.Zero)
                {
                    SafeSecurityHelper.TransformLocalRectToScreen(new HandleRef(null, hwndParent), ref windowRectInScreenCoords);
                }

                if (!isMove)
                {
                    // We weren't actually moving, so the WINDOWPOS structure
                    // did not contain valid (x,y) information.  Just use our
                    // old values.
                    int width = (windowRectInScreenCoords.right - windowRectInScreenCoords.left);
                    int height = (windowRectInScreenCoords.bottom - windowRectInScreenCoords.top);
                    windowRectInScreenCoords.left = _hwndWindowRectInScreenCoords.left;
                    windowRectInScreenCoords.right = windowRectInScreenCoords.left + width;
                    windowRectInScreenCoords.top = _hwndWindowRectInScreenCoords.top;
                    windowRectInScreenCoords.bottom = windowRectInScreenCoords.top + height;
                }

                if (!isSize)
                {
                    // We weren't actually sizing, so the WINDOWPOS structure
                    // did not contain valid (cx,cy) information.  Just use our
                    // old values.
                    int width = (_hwndWindowRectInScreenCoords.right - _hwndWindowRectInScreenCoords.left);
                    int height = (_hwndWindowRectInScreenCoords.bottom - _hwndWindowRectInScreenCoords.top);

                    windowRectInScreenCoords.right = windowRectInScreenCoords.left + width;
                    windowRectInScreenCoords.bottom = windowRectInScreenCoords.top + height;
                }

                positionChanged = (   _hwndWindowRectInScreenCoords.left != windowRectInScreenCoords.left
                                   || _hwndWindowRectInScreenCoords.top != windowRectInScreenCoords.top
                                   || _hwndWindowRectInScreenCoords.right != windowRectInScreenCoords.right
                                   || _hwndWindowRectInScreenCoords.bottom != windowRectInScreenCoords.bottom);
            }


            //
            // The second order of business is to determine whether or not the render
            // target should be enabled.  If we are disabling the render target, then
            // we need to synchronize with the render thread.  Basically,
            // a WM_WINDOWPOSCHANGED always enables the render target it the window is
            // visible.  And a WM_WINDOWPOSCHANGING will disable the render target
            // unless it is not really a size/move, in which case we will not be sent
            // a WM_WINDOWPOSCHANGED, so we can't disable the render target.
            //
            bool enableRenderTarget = SafeNativeMethods.IsWindowVisible(_hWnd.MakeHandleRef(this));
            if(enableRenderTarget)
            {
                if(_windowPosChanging && (positionChanged))
                {
                    enableRenderTarget = false;
                }
            }


            if (positionChanged || (enableRenderTarget != _isRenderTargetEnabled))
            {
                UpdateWindowSettings(enableRenderTarget);
            }
        }

        bool _windowPosChanging;

        private void OnShowWindow(bool enableRenderTarget)
        {
            if (enableRenderTarget != _isRenderTargetEnabled)
            {
                UpdateWindowSettings(enableRenderTarget);
            }
        }

        #region PROCESS_DPI_AWARENESS

        /// <summary>
        /// DPI awareness level of the process. Corresponds to Win32 PROCESS_DPI_AWARENESS
        /// enum. Also see <see cref="PROCESS_DPI_AWARENESS"/>
        /// </summary>
        /// <remarks>
        /// i.  This value is initialized only once per process, therefore this is a
        ///     static member.
        /// ii. This is an 'effective' value, and not necessarily the 'actual'
        ///     value. On OS versions older than RS1(Window 10 v1607), WPF does not support
        ///     per-monitor DPI, and will always default to system aware or unaware modes.
        ///     To get the actual value, refer to <see cref="AppManifestProcessDpiAwareness"/>
        /// </remarks>
        private static PROCESS_DPI_AWARENESS? ProcessDpiAwareness { get; set; } = null;

        /// <summary>
        /// The actual PROCESS_DPI_AWARENESS of the process set by the application manifest,
        /// or by an equivalent API, irrespective of the OS version.
        /// Also see remarks on <see cref="ProcessDpiAwareness"/>
        /// </summary>
        /// <remarks>
        /// i.  This value is initialized only once per process, therefore this is a
        ///     static member.
        /// ii. The initialization of this member depends on <see cref="_hWnd"/>, which is an
        ///     instance member, so this can't be initialized in the static constructor.
        ///     We maintain this as a nullable-property to keep track of whether it has been
        ///     initialized or not. This helps us ensure that <see cref="AppManifestProcessDpiAwareness"/>
        ///     and <see cref="ProcessDpiAwareness"/> are only initialized once.
        /// </remarks>
        private static PROCESS_DPI_AWARENESS? AppManifestProcessDpiAwareness { get; set; } = null;

        #endregion

        /// <summary>
        /// Window's DPI Awareness Context, equivalent to a
        /// Win32 DPI_AWARENESS_CONTEXT handle
        /// </summary>
        /// <remarks>
        /// - Once set, this will not change again
        /// - This is always the 'actual' value, unfiltered by whether WPF
        ///   is currently operating in per-monitor DPI or better mode.
        /// </remarks>
        private DpiAwarenessContextValue DpiAwarenessContext { get; set; }

        internal static bool IsPerMonitorDpiScalingSupportedOnCurrentPlatform
        {
            get
            {
                return OSVersionHelper.IsOsWindows10RS1OrGreater; ;
            }
        }

        internal static bool IsPerMonitorDpiScalingEnabled
        {
            get
            {
                return
                    !CoreAppContextSwitches.DoNotScaleForDpiChanges &&
                    IsPerMonitorDpiScalingSupportedOnCurrentPlatform;
            }
        }

        internal static bool? IsProcessPerMonitorDpiAware
        {
            get
            {
                if (ProcessDpiAwareness.HasValue)
                {
                    return ProcessDpiAwareness.Value == PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE;
                }

                return null;
            }
        }

        internal static bool? IsProcessSystemAware
        {
            get
            {
                if (ProcessDpiAwareness.HasValue)
                {
                    return ProcessDpiAwareness.Value == PROCESS_DPI_AWARENESS.PROCESS_SYSTEM_DPI_AWARE;
                }

                return null;
            }
        }

        internal static bool? IsProcessUnaware
        {
            get
            {
                if (ProcessDpiAwareness.HasValue)
                {
                    return ProcessDpiAwareness.Value == PROCESS_DPI_AWARENESS.PROCESS_DPI_UNAWARE;
                }

                return null;
            }
        }

        internal bool IsWindowPerMonitorDpiAware
        {
            get
            {
                return
                    DpiAwarenessContext == DpiAwarenessContextValue.PerMonitorAware ||
                    DpiAwarenessContext == DpiAwarenessContextValue.PerMonitorAwareVersion2;
            }
        }


        private void OnEnterSizeMove()
        {
            _userInputResize = true;
        }

        private void OnExitSizeMove()
        {
            if (_windowPosChanging)
            {
                _windowPosChanging = false;
                UpdateWindowSettings(true);
            }

            _userInputResize = false;
        }

        private void UpdateWindowSettings()
        {
            UpdateWindowSettings(_isRenderTargetEnabled, null);
        }

        private void UpdateWindowSettings(bool enableRenderTarget)
        {
            UpdateWindowSettings(enableRenderTarget, null);
        }

        private void UpdateWindowSettings(bool enableRenderTarget, DUCE.ChannelSet? channelSet)
        {
            MediaContext mctx = MediaContext.From(Dispatcher);

            // It's possible that this method could be called multiple times in a row
            // with the same enableRenderTarget value and we'd like to minimize the
            // number of flushes on the OOB channel by only flushing when transitioning
            // rather than ever time we get a disable.
            bool firstTimeRenderTargetDisabled = false;
            bool firstTimeRenderTargetEnabled = false;
            if (_isRenderTargetEnabled != enableRenderTarget)
            {
                _isRenderTargetEnabled = enableRenderTarget;
                firstTimeRenderTargetDisabled = !enableRenderTarget;
                firstTimeRenderTargetEnabled = enableRenderTarget;

                // Basic idea: the render thread and the UI thread have a
                // race condition when the UI thread wants to modify
                // HWND data and the render thread is using it.  The render
                // thread can paint garbage on the screen, and it can also
                // cause the old data to be set again (ULW issue, hence ULWEx).
                //
                // So we tell the render thread to stop rendering and then we
                // wait for them to stop when disabling the render target by
                // issuing the UpdateWindowSettings command synchronously on
                // an out-of-band channel.
            }


            // if we are disconnected we are done.
            if (!_compositionTarget.IsOnAnyChannel)
            {
                return;
            }

            //
            // Calculate the client rectangle in screen coordinates.
            //

            UpdateWindowAndClientCoordinates();

            Int32 style = UnsafeNativeMethods.GetWindowLong(_hWnd.MakeHandleRef(this), NativeMethods.GWL_STYLE);
            Int32 exStyle = UnsafeNativeMethods.GetWindowLong(_hWnd.MakeHandleRef(this), NativeMethods.GWL_EXSTYLE);

            bool isLayered = (exStyle & NativeMethods.WS_EX_LAYERED) != 0;

            bool isChild = (style & NativeMethods.WS_CHILD) != 0;
            bool isRTL = (exStyle & NativeMethods.WS_EX_LAYOUTRTL) != 0;

            int width = _hwndClientRectInScreenCoords.right - _hwndClientRectInScreenCoords.left;
            int height = _hwndClientRectInScreenCoords.bottom - _hwndClientRectInScreenCoords.top;

            MILTransparencyFlags flags = MILTransparencyFlags.Opaque;
            // if (!DoubleUtil.AreClose(_opacity, 1.0))
            // {
            //     flags |= MILTransparencyFlags.ConstantAlpha;
            // }

            // if (_colorKey.HasValue)
            // {
            //     flags |= MILTransparencyFlags.ColorKey;
            // }

            if (_usesPerPixelOpacity)
            {
                flags |= MILTransparencyFlags.PerPixelAlpha;
            }

            if (!isLayered && flags != MILTransparencyFlags.Opaque)
            {
                // The window is not layered, but it should be -- set the layered flag.
                UnsafeNativeMethods.SetWindowLong(_hWnd.MakeHandleRef(this), NativeMethods.GWL_EXSTYLE, new IntPtr(exStyle | NativeMethods.WS_EX_LAYERED));
            }
            else if (isLayered && flags == MILTransparencyFlags.Opaque)
            {
                // The window is layered but should not be -- unset the layered flag.
                UnsafeNativeMethods.SetWindowLong(_hWnd.MakeHandleRef(this), NativeMethods.GWL_EXSTYLE, new IntPtr(exStyle & ~NativeMethods.WS_EX_LAYERED));
            }
            else if(isLayered && flags != MILTransparencyFlags.Opaque && _isRenderTargetEnabled && (width == 0 || height == 0))
            {
                // The window is already layered, and it should be.  But we are enabling a window
                // that is has a 0-size dimension.  This may cause us to leave the last sprite
                // on the screen.  The best way to get rid of this is to just make the entire
                // sprite transparent.

                NativeMethods.BLENDFUNCTION blend = new NativeMethods.BLENDFUNCTION();
                blend.BlendOp = NativeMethods.AC_SRC_OVER;
                blend.SourceConstantAlpha = 0; // transparent
                UnsafeNativeMethods.UpdateLayeredWindow(_hWnd.h, IntPtr.Zero, null, null, IntPtr.Zero, null, 0, ref blend, NativeMethods.ULW_ALPHA);
            }
            isLayered = (flags != MILTransparencyFlags.Opaque);

            if (channelSet == null)
            {
                channelSet = mctx.GetChannels();
            }

            // If this is the first time going from disabled -> enabled, flush
            // the out of band to make sure all disable packets have been
            // processed before sending the enable later below. Otherwise,
            // the enable could be ignored if the disable cookie doesn't match
            DUCE.Channel outOfBandChannel = channelSet.Value.OutOfBandChannel;
            if (firstTimeRenderTargetEnabled)
            {
                outOfBandChannel.Commit();
                outOfBandChannel.SyncFlush();
            }

            // Every UpdateWindowSettings command that disables the render target is
            // assigned a new cookie.  Every UpdateWindowSettings command that enables
            // the render target uses the most recent cookie.  This allows the
            // compositor to ignore UpdateWindowSettings(enable) commands that come
            // out of order due to us disabling out-of-band and enabling in-band.
            if (!_isRenderTargetEnabled)
            {
                _disableCookie++;
            }

            //
            // When enabling the render target, stay in-band.  This allows any
            // client-side rendering instructions to be included in the same packet.
            // Otherwise pass in the OutOfBand handle.
            //
            DUCE.Channel channel = channelSet.Value.Channel;
            DUCE.CompositionTarget.UpdateWindowSettings(
                _isRenderTargetEnabled ? _compositionTarget.GetHandle(channel) : _compositionTarget.GetHandle(outOfBandChannel),
                _hwndClientRectInScreenCoords,
                Colors.Transparent, // _colorKey.GetValueOrDefault(Colors.Black),
                1.0f, // (float)_opacity,
                isLayered ? (_usesPerPixelOpacity ? MILWindowLayerType.ApplicationManagedLayer : MILWindowLayerType.SystemManagedLayer) : MILWindowLayerType.NotLayered,
                flags,
                isChild,
                isRTL,
                _isRenderTargetEnabled,
                _disableCookie,
                _isRenderTargetEnabled ? channel : outOfBandChannel);

            if (_isRenderTargetEnabled)
            {
                //
                // Re-render the visual tree.
                //

                mctx.PostRender();
            }
            else
            {
                if (firstTimeRenderTargetDisabled)
                {
                    outOfBandChannel.CloseBatch();
                    outOfBandChannel.Commit();

                    //
                    // Wait for the command to be processed -- sync flush will take care
                    // of that while being safe w.r.t. zombie partitions.
                    //

                    outOfBandChannel.SyncFlush();
                }

                // If we disabled the render target, we run the risk of leaving it disabled.
                // One such example is when a window is programatically sized, but then
                // GetMinMaxInfo denies the change.  We do not receive any message that would
                // allow us to re-enable the render targer.  To cover these odd cases, we
                // post ourselves a message to possible re-enable the render target when
                // we are done with the current message processing.
                UnsafeNativeMethods.PostMessage(new HandleRef(this, _hWnd), s_updateWindowSettings, IntPtr.Zero, IntPtr.Zero);
            }
        }

        /// <summary>
        /// Gets and sets the root Visual of this HwndTarget.
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public override Visual RootVisual
        {
            set
            {
                base.RootVisual = value;

                if (value != null)
                {
                    // Update the static array that stores the actual DpiScales.
                    // output is the index that will be set to the visual flags.
                    if (IsProcessPerMonitorDpiAware == true)
                    {
                        DpiFlags dpiFlags = DpiUtil.UpdateDpiScalesAndGetIndex(_currentDpiScale.PixelsPerInchX, _currentDpiScale.PixelsPerInchY);
                        DpiScale newDpiScale = new DpiScale(UIElement.DpiScaleXValues[dpiFlags.Index], UIElement.DpiScaleYValues[dpiFlags.Index]);
                        RootVisual.RecursiveSetDpiScaleVisualFlags(new DpiRecursiveChangeArgs( dpiFlags, RootVisual.GetDpi(), newDpiScale));
                    }

                    // UIAutomation listens for the EventObjectUIFragmentCreate WinEvent to
                    // understand when UI that natively implements UIAutomation comes up
                    
                    // Need to figure out how to handle when _rootVisual is replaced above (is there some
                    // event when this happens?); MS.Internal.Automation.NativeEventListener may have a context
                    // monitor that is holding onto the old _rootVisual and that would need to be cleaned up.
                    // Do we treat swapping in a new root as a new app?  Need to understand when this could happen.
                    UnsafeNativeMethods.NotifyWinEvent(UnsafeNativeMethods.EventObjectUIFragmentCreate, _hWnd.MakeHandleRef(this), 0, 0);
                }
            }
        }

        /// <summary>
        /// Returns matrix that can be used to transform coordinates from this
        /// target to the rendering destination device.
        /// </summary>
        public override Matrix TransformToDevice
        {
            get
            {
                VerifyAPIReadOnly();
                Matrix m = Matrix.Identity;
                m.Scale(_currentDpiScale.DpiScaleX, _currentDpiScale.DpiScaleY);
                return m;
            }
        }

        /// <summary>
        /// Returns matrix that can be used to transform coordinates from
        /// the rendering destination device to this target.
        /// </summary>
        public override Matrix TransformFromDevice
        {
            get
            {
                VerifyAPIReadOnly();
                Matrix m = Matrix.Identity;
                m.Scale(1.0f/_currentDpiScale.DpiScaleX, 1.0f/_currentDpiScale.DpiScaleY);
                return m;
            }
        }

        /// <summary>
        /// This is the color that is drawn before everything else.  If
        /// this color has an alpha component other than 1 it will be ignored.
        /// </summary>
        public Color BackgroundColor
        {
            get
            {
                VerifyAPIReadOnly();

                return _backgroundColor;
            }
            set
            {
                VerifyAPIReadWrite();

                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    MediaContext mctx = MediaContext.From(Dispatcher);

                    DUCE.ChannelSet channelSet = mctx.GetChannels();
                    DUCE.Channel channel = channelSet.Channel;
                    if (channel == null)
                    {
                        // MediaContext is in disconnected state, so we will send
                        // the clear color when CreateUCEResources gets called
                        Debug.Assert(!_compositionTarget.IsOnChannel(channel));
                    }
                    else
                    {
                        DUCE.CompositionTarget.SetClearColor(
                            _compositionTarget.GetHandle(channel),
                            _backgroundColor,
                            channel);
                        mctx.PostRender();
                    }
                }
            }
        }

        // /// <summary>
        // ///     Specifies the color to display as transparent.
        // /// </summary>
        // /// <remarks>
        // ///     Use null to indicate that no color should be transparent.
        // /// </remarks>
        // public Nullable<Color> ColorKey
        // {
        //     get
        //     {
        //         VerifyAPIReadOnly();
        //
        //         return _colorKey;
        //     }
        //
        //     set
        //     {
        //         VerifyAPIReadWrite();
        //
        //         if(_colorKey != value)
        //         {
        //             _colorKey = value;
        //
        //             UpdateWindowSettings();
        //         }
        //     }
        // }

        // /// <summary>
        // ///     Specifies the constant opacity to apply to the window.
        // /// </summary>
        // /// <remarks>
        // ///     The valid values range from [0..1].  Values outside of this range are clamped.
        // /// </remarks>
        // public double Opacity
        // {
        //     get
        //     {
        //         VerifyAPIReadOnly();
        //
        //         return _opacity;
        //     }
        //
        //     set
        //     {
        //         VerifyAPIReadWrite();
        //
        //         if(value < 0.0) value = 0.0;
        //         if(value > 1.0) value = 1.0;
        //
        //         if(!MS.Internal.DoubleUtil.AreClose(value, _opacity))
        //         {
        //             _opacity = value;
        //
        //             UpdateWindowSettings();
        //         }
        //     }
        // }

        /// <summary>
        ///     Specifies whether or not the per-pixel opacity of the window content
        ///     is respected.
        /// </summary>
        /// <remarks>
        ///     By enabling per-pixel opacity, the system will no longer draw the non-client area.
        /// </remarks>
        public bool UsesPerPixelOpacity
        {
            get
            {
                VerifyAPIReadOnly();

                return _usesPerPixelOpacity;
            }

            internal set
            {
                VerifyAPIReadWrite();

                if(_usesPerPixelOpacity != value)
                {
                    _usesPerPixelOpacity = value;

                    UpdateWindowSettings();
                }
            }
        }

        #region Notification Window

        [ThreadStatic]
        private static NotificationWindowHelper _notificationWindowHelper;

        private void EnsureNotificationWindow()
        {
            if (_notificationWindowHelper == null)
            {
                _notificationWindowHelper = new NotificationWindowHelper();
            }
        }

        private class MonitorPowerEventArgs : EventArgs
        {
            public MonitorPowerEventArgs(bool powerOn)
            {
                PowerOn = powerOn;
            }
            public bool PowerOn { get; private set; }
        }

        /// <summary>
        ///     Abstraction for the logic to get thread level
        ///     system notifications like PBT_POWERSETTINGCHANGE.
        ///     Ideally all such thread singleton messages for
        ///     hwnds of HwndTargets should be recieved by this
        ///     class. Only PBT_POWERSETTINGCHANGE is implemented
        ///     at this point, so as to limit the testing surface.
        ///     Others must be implemented in future as and when
        ///     possible.
        /// </summary>
        private class NotificationWindowHelper : IDisposable
        {
            #region Data

            /// <SecurityNode>
            ///     Critical: We dont want _notificationHwnd to be exposed and used
            ///         by anyone besides this class.
            /// </SecurityNode>
            private HwndWrapper _notificationHwnd; // The hwnd used to listen system wide messages

            /// <SecurityNode>
            ///     Critical: _notificationHook is the hook to listen to window
            ///         messages. We want this to be critical that no one can get it
            ///         listen to window messages.
            /// </SecurityNode>
            private HwndWrapperHook _notificationHook;

            private int _hwndTargetCount;
            public event EventHandler<MonitorPowerEventArgs> MonitorPowerEvent;
            private bool _monitorOn = true;

            private IntPtr _hPowerNotify;

            #endregion

            /// <SecurityNode>
            ///     Critical: Calls critical code.
            ///     TreatAsSafe: Doesn't expose the critical resource.
            /// </SecurityNode>
            public NotificationWindowHelper()
            {
                // Check for Vista or newer is needed for RegisterPowerSettingNotification.
                // This check needs to rescoped to the said method call, if other
                // notifications are implemented.
                if (Utilities.IsOSVistaOrNewer)
                {
                    // _notificationHook needs to be member variable otherwise
                    // it is GC'ed and we don't get messages from HwndWrapper
                    // (HwndWrapper keeps a WeakReference to the hook)

                    _notificationHook = new HwndWrapperHook(NotificationFilterMessage);
                    HwndWrapperHook[] wrapperHooks = { _notificationHook };

                    _notificationHwnd = new HwndWrapper(
                                                0,
                                                0,
                                                0,
                                                0,
                                                0,
                                                0,
                                                0,
                                                "",
                                                IntPtr.Zero,
                                                wrapperHooks);

                    Guid monitorGuid = new Guid(NativeMethods.GUID_MONITOR_POWER_ON.ToByteArray());
                    unsafe
                    {
                        _hPowerNotify = UnsafeNativeMethods.RegisterPowerSettingNotification(_notificationHwnd.Handle, &monitorGuid, 0);
                    }
                }
            }

            /// <SecurityNode>
            ///     Critical: Calls critical code.
            ///     TreatAsSafe: Doesn't expose the critical resource.
            /// </SecurityNode>
            public void Dispose()
            {
                if (_hPowerNotify != IntPtr.Zero)
                {
                    UnsafeNativeMethods.UnregisterPowerSettingNotification(_hPowerNotify);
                    _hPowerNotify = IntPtr.Zero;
                }

                // Remove any attached event handlers.
                MonitorPowerEvent = null;

                _hwndTargetCount = 0;
                if (_notificationHwnd != null)
                {
                    _notificationHwnd.Dispose();
                    _notificationHwnd = null;
                }
            }

            /// <SecurityNode>
            ///     Critical: Calls critical code.
            ///     TreatAsSafe: Doesn't expose the critical resource.
            /// </SecurityNode>
            public void AttachHwndTarget(HwndTarget hwndTarget)
            {
                Debug.Assert(hwndTarget != null);
                MonitorPowerEvent += hwndTarget.OnMonitorPowerEvent;
                if (_hwndTargetCount > 0)
                {
                    // Every hwnd which registers to listen PBT_POWERSETTINGCHANGE
                    // gets the message atleast once so as to set the appropriate
                    // state. This call to the event handler simulates similar
                    // behavior. It is too early for the hwnd to paint, hence
                    // pass paintOnWake=false assuming that it will soon get
                    // a WM_PAINT message.
                    hwndTarget.OnMonitorPowerEvent(null, _monitorOn, /*paintOnWake*/ false);
                }
                _hwndTargetCount++;
            }

            /// <SecurityNode>
            ///     Critical: Calls critical code.
            ///     TreatAsSafe: Doesn't expose the critical resource.
            /// </SecurityNode>
            public bool DetachHwndTarget(HwndTarget hwndTarget)
            {
                Debug.Assert(hwndTarget != null);
                MonitorPowerEvent -= hwndTarget.OnMonitorPowerEvent;
                _hwndTargetCount--;
                Debug.Assert(_hwndTargetCount >= 0);
                return (_hwndTargetCount == 0);
            }

            /// <summary>
            ///     Handles the messages for the notification window
            /// </summary>
            private IntPtr NotificationFilterMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                IntPtr retInt = IntPtr.Zero;
                switch ((WindowMessage)msg)
                {
                    case WindowMessage.WM_POWERBROADCAST:
                        switch (NativeMethods.IntPtrToInt32(wParam))
                        {
                            case NativeMethods.PBT_POWERSETTINGCHANGE:
                                // PBT_POWERSETTINGCHANGE logic is implemented as a thread singleton
                                // instead of application singleton so as to avoid race between
                                // notification hwnd's PBT_POWERSETTINGCHANGE and other thread
                                // hwnd's WM_PAINT.
                                unsafe
                                {
                                    NativeMethods.POWERBROADCAST_SETTING* powerBroadcastSetting = (NativeMethods.POWERBROADCAST_SETTING*)lParam;
                                    if ((*powerBroadcastSetting).PowerSetting == NativeMethods.GUID_MONITOR_POWER_ON)
                                    {
                                        if ((*powerBroadcastSetting).Data == 0)
                                        {
                                            // Monitor is off
                                            _monitorOn = false;
                                        }
                                        else
                                        {
                                            // Monitor is on
                                            _monitorOn = true;
                                        }
                                        if (MonitorPowerEvent != null)
                                        {
                                            MonitorPowerEvent(null, new MonitorPowerEventArgs(_monitorOn));
                                        }
                                    }
                                }
                                break;
                        }
                        break;
                    default:
                        handled = false;
                        break;
                }
                return retInt;
            }
        }

        #endregion
    }
}
