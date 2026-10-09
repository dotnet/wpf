// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Standard;

using HANDLE_MESSAGE = System.Collections.Generic.KeyValuePair<Standard.WM, Standard.MessageHandler>;

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Shell
#else
namespace Microsoft.Windows.Shell
#endif
{
    internal class WindowChromeWorker : DependencyObject
    {
        // Delegate signature used for Dispatcher.BeginInvoke.
        private delegate void _Action();

        #region Fields

        private const SWP _SwpFlags = SWP.FRAMECHANGED | SWP.NOSIZE | SWP.NOMOVE | SWP.NOZORDER | SWP.NOOWNERZORDER | SWP.NOACTIVATE;

        private readonly List<HANDLE_MESSAGE> _messageTable;

        /// <summary>The Window that's chrome is being modified.</summary>
        private Window _window;

        /// <summary>Underlying HWND for the _window.</summary>
        private IntPtr _hwnd;

        /// <summary>Underlying HWND for the _window.</summary>
        private HwndSource _hwndSource = null;

        private bool _isHooked = false;

        /// <summary>Object that describes the current modifications being made to the chrome.</summary>
        private WindowChrome _chromeInfo;

        // Keep track of this so we can detect when we need to apply changes. Tracking these separately
        // as I've seen using just one cause things to get enough out of sync that occasionally the caption will redraw.
        private WindowState _lastRoundingState;
        private WindowState _lastMenuState;
        private bool _isGlassEnabled;

        /// <summary>
        /// Insets between the window rect and the visible DWM frame, in device pixels, measured while the
        /// window is in the Normal state. Used to answer WM_NCCALCSIZE in ExtendedClientArea mode.
        /// </summary>
        private struct FrameInsets : IEquatable<FrameInsets>
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public bool Equals(FrameInsets other)
            {
                return Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
            }

            public override bool Equals(object obj)
            {
                return obj is FrameInsets other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Left, Top, Right, Bottom);
            }
        }

        /// <summary>True when the worker was created for a SystemFrame-only chrome (see EnsureWorker).</summary>
        private readonly bool _createdForSystemFrame;

        /// <summary>The FrameMode that the current HWND state reflects; null until the first application.</summary>
        private WindowChromeFrameMode? _appliedFrameMode;

        /// <summary>ExtendedClientArea is requested and DWM composition is available.</summary>
        private bool _isExtendedFrameActive;

        private FrameInsets _normalFrameInsets;
        private bool _hasNormalFrameInsets;

        /// <summary>
        /// <see cref="_normalFrameInsets"/> was synthesized from system metrics because the window was maximized
        /// when the insets were needed; it is replaced by a real measurement once the window is Normal again.
        /// </summary>
        private bool _frameInsetsProvisional;
        private bool _remeasureProvisionalPending;
        private int _provisionalRemeasureAttempts;

        /// <summary>The SystemFrame chrome attached by <see cref="EnsureWorker"/>, so it can be removed again.</summary>
        private WindowChrome _autoChrome;

        /// <summary>Window DPI at which <see cref="_normalFrameInsets"/> was measured.</summary>
        private double _frameInsetsDpi;
        /// <summary>Set by WM_THEMECHANGED: the frame may have switched between visible and invisible borders.</summary>
        private bool _remeasureInsetsPending;

        /// <summary>Factor converting DWM (physical monitor) pixels to the window's (possibly DPI virtualized) pixels.</summary>
        private double _dwmToWindowScale = 1.0;

        private Rect _captionButtonsBounds;
        private Size _captionButtonsClipClientSize;
        private Rect _titleBarBounds;
        private bool _captionButtonsUpdatePending;
        private bool _frameStateUpdatePending;

        /// <summary>Whether the HWND is the active window, as reported by WM_ACTIVATE.</summary>
        private bool _isActive = true;

        private WindowCaptionAdorner _captionAdorner;
        private UIElement _captionAdornedElement;
        private bool _isTitleListenerAttached;

        // Elements with a CaptionButtonRole the pointer is over / pressing: the system owns the pointer over
        // them (the hit test answers a caption button code), so their states are driven from the non-client
        // mouse messages instead of the input pipeline.
        private DependencyObject _hoveredCaptionButton;
        private DependencyObject _pressedCaptionButton;

        // Last values pushed to DWM, so redundant calls are skipped and restore knows what to undo.
        private WTNCA _appliedThemeAttributes;
        private bool _themeAttributesApplied;
        private Color? _appliedCaptionForegroundColor;
        private uint? _appliedCaptionColor;
        private uint? _appliedTextColor;
        private uint? _appliedBorderColor;
        private DWMWCP? _appliedCornerPreference;
        private bool? _appliedDarkMode;
        private DWMSBT _appliedBackdrop = DWMSBT.DWMSBT_AUTO;
        private bool _isBackdropApplied;
        private Color? _savedBackgroundColor;

        // State provided by the ThemeManager (Fluent theme) for the Auto values of CaptionTheme and BackdropType.
        private bool? _themeUseLightColors;
        private WindowBackdropKind? _themeBackdropKind;

        #endregion

        static WindowChromeWorker()
        {
        }

        public WindowChromeWorker()
            : this(false)
        {
        }

        private WindowChromeWorker(bool createdForSystemFrame)
        {
            _createdForSystemFrame = createdForSystemFrame;

            _messageTable = new List<HANDLE_MESSAGE>
            {
                new HANDLE_MESSAGE(WM.SETTEXT,               _HandleSetTextOrIcon),
                new HANDLE_MESSAGE(WM.SETICON,               _HandleSetTextOrIcon),
                new HANDLE_MESSAGE(WM.NCACTIVATE,            _HandleNCActivate),
                new HANDLE_MESSAGE(WM.NCCALCSIZE,            _HandleNCCalcSize),
                new HANDLE_MESSAGE(WM.NCHITTEST,             _HandleNCHitTest),
                new HANDLE_MESSAGE(WM.NCRBUTTONUP,           _HandleNCRButtonUp),
                new HANDLE_MESSAGE(WM.NCMOUSEMOVE,           _HandleNCMouseMove),
                new HANDLE_MESSAGE(WM.NCLBUTTONDOWN,         _HandleNCLButtonDown),
                new HANDLE_MESSAGE(WM.NCLBUTTONDBLCLK,       _HandleNCLButtonDown),
                new HANDLE_MESSAGE(WM.NCLBUTTONUP,           _HandleNCLButtonUp),
                new HANDLE_MESSAGE(WM.NCMOUSELEAVE,          _HandleNCMouseLeave),
                new HANDLE_MESSAGE(WM.ACTIVATE,              _HandleActivate),
                new HANDLE_MESSAGE(WM.SIZE,                  _HandleSize),
                new HANDLE_MESSAGE(WM.WINDOWPOSCHANGED,      _HandleWindowPosChanged),
                new HANDLE_MESSAGE(WM.DPICHANGED,            _HandleDpiChanged),
                new HANDLE_MESSAGE(WM.DWMCOMPOSITIONCHANGED, _HandleDwmCompositionChanged),
                new HANDLE_MESSAGE(WM.DWMNCRENDERINGCHANGED, _HandleEnvironmentChanged),
                new HANDLE_MESSAGE(WM.DWMCOLORIZATIONCOLORCHANGED, _HandleEnvironmentChanged),
                new HANDLE_MESSAGE(WM.THEMECHANGED,          _HandleEnvironmentChanged),
                new HANDLE_MESSAGE(WM.SETTINGCHANGE,         _HandleEnvironmentChanged),
                new HANDLE_MESSAGE(WM.SYSCOLORCHANGE,        _HandleEnvironmentChanged),
            };
        }

        /// <summary>
        /// Returns the worker of <paramref name="window"/>, creating it when needed. When the window has no
        /// WindowChrome a SystemFrame chrome is attached with SetCurrentValue, so that a Style setter or a later
        /// SetValue from the application replaces it. Used by the ThemeManager to manage the caption theme
        /// and the backdrop of Fluent-themed windows.
        /// </summary>
        internal static WindowChromeWorker EnsureWorker(Window window)
        {
            Verify.IsNotNull(window, "window");

            WindowChromeWorker worker = GetWindowChromeWorker(window);
            if (worker == null)
            {
                worker = new WindowChromeWorker(createdForSystemFrame: true);
                SetWindowChromeWorker(window, worker);
            }

            if (WindowChrome.GetWindowChrome(window) == null && !System.ComponentModel.DesignerProperties.GetIsInDesignMode(window))
            {
                worker._autoChrome = WindowChrome.CreateSystemFrameChrome();
                window.SetCurrentValue(WindowChrome.WindowChromeProperty, worker._autoChrome);
            }

            return worker;
        }

        /// <summary>
        /// Stores the decisions of the theme (light/dark caption and backdrop) that apply when the chrome's
        /// CaptionTheme / BackdropType are set to Auto, and pushes them to DWM.
        /// </summary>
        internal void ApplyThemeState(bool useLightColors, WindowBackdropKind backdropKind)
        {
            VerifyAccess();
            _themeUseLightColors = useLightColors;
            _themeBackdropKind = backdropKind;
            _UpdateDwmAttributes(WindowChromeDwmAttributes.CaptionTheme | WindowChromeDwmAttributes.Backdrop);
        }

        /// <summary>
        /// Detaches the SystemFrame chrome that <see cref="EnsureWorker"/> attached, when it is still the chrome
        /// in use. Used by the ThemeManager when a window leaves the Fluent theme, so that the window looks like
        /// it never had a chrome. A chrome set by the application (or by a style) is left alone.
        /// </summary>
        internal void RemoveAutomaticChrome()
        {
            VerifyAccess();

            if (_autoChrome != null && _window != null && ReferenceEquals(WindowChrome.GetWindowChrome(_window), _autoChrome))
            {
                // SetCurrentValue left the base value untouched: clearing goes back to that base value.
                _window.ClearValue(WindowChrome.WindowChromeProperty);
            }

            _autoChrome = null;
        }

        public void SetWindowChrome(WindowChrome newChrome)
        {
            VerifyAccess();
            Assert.IsNotNull(_window);

            if (newChrome == _chromeInfo)
            {
                // Nothing's changed.
                return;
            }

            if (_chromeInfo != null)
            {
                _chromeInfo.PropertyChangedThatRequiresRepaint -= _OnChromePropertyChangedThatRequiresRepaint;
                _chromeInfo.PropertyChangedThatRequiresDwmUpdate -= _OnChromePropertyChangedThatRequiresDwmUpdate;
            }

            _chromeInfo = newChrome;

            if (_chromeInfo != null)
            {
                _chromeInfo.PropertyChangedThatRequiresRepaint += _OnChromePropertyChangedThatRequiresRepaint;
                _chromeInfo.PropertyChangedThatRequiresDwmUpdate += _OnChromePropertyChangedThatRequiresDwmUpdate;
            }

            _ApplyNewCustomChrome();
        }

        private void _OnChromePropertyChangedThatRequiresRepaint(object sender, EventArgs e)
        {
            _UpdateFrameState(true);
        }

        private void _OnChromePropertyChangedThatRequiresDwmUpdate(object sender, WindowChromeDwmAttributesChangedEventArgs e)
        {
            _UpdateDwmAttributes(e.Attributes);
        }

        private WindowChromeFrameMode _FrameMode
        {
            get { return _chromeInfo != null ? _chromeInfo.FrameMode : WindowChromeFrameMode.Custom; }
        }

        private bool _IsExtendedMode
        {
            get { return _FrameMode == WindowChromeFrameMode.ExtendedClientArea; }
        }

        /// <summary>Whether the worker owns the non-client area (everything but SystemFrame).</summary>
        private bool _ManagesNonClientArea
        {
            get { return _chromeInfo != null && _FrameMode != WindowChromeFrameMode.SystemFrame; }
        }

        private bool _IsHwndAlive
        {
            get { return _hwnd != IntPtr.Zero && _hwndSource != null && !_hwndSource.IsDisposed; }
        }

        /// <summary>
        /// Whether the HWND is maximized. Unlike Window.WindowState this is already up to date when the
        /// WM_NCCALCSIZE of the maximize transition arrives.
        /// </summary>
        private bool _IsHwndMaximized()
        {
            var dwStyle = (WS)NativeMethods.GetWindowLongPtr(_hwnd, GWL.STYLE).ToInt32();
            return Utility.IsFlagSet((int)dwStyle, (int)WS.MAXIMIZE);
        }

        public static readonly DependencyProperty WindowChromeWorkerProperty = DependencyProperty.RegisterAttached(
            "WindowChromeWorker",
            typeof(WindowChromeWorker),
            typeof(WindowChromeWorker),
            new PropertyMetadata(null, _OnChromeWorkerChanged));

        private static void _OnChromeWorkerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var w = (Window)d;
            var cw = (WindowChromeWorker)e.NewValue;

            // The WindowChromeWorker object should only be set on the window once, and never to null.
            Assert.IsNotNull(w);
            Assert.IsNotNull(cw);
            Assert.IsNull(cw._window);

            cw._SetWindow(w);
        }

        private void _SetWindow(Window window)
        {
            Assert.IsNull(_window);
            Assert.IsNotNull(window);

            UnsubscribeWindowEvents();

            _window = window;

            // There are potentially a couple funny states here.
            // The window may have been shown and closed, in which case it's no longer usable.
            // We shouldn't add any hooks in that case, just exit early.
            // If the window hasn't yet been shown, then we need to make sure to remove hooks after it's closed.
            _hwnd = new WindowInteropHelper(_window).Handle;

            // On older versions of the framework the client size of the window is incorrectly calculated.
            // We need to modify the template to fix this on behalf of the user.
            Utility.AddDependencyPropertyChangeListener(_window, Window.TemplateProperty, _OnWindowPropertyChangedThatRequiresTemplateFixup);
            Utility.AddDependencyPropertyChangeListener(_window, Window.FlowDirectionProperty, _OnWindowPropertyChangedThatRequiresTemplateFixup);

            _window.Closed += _UnsetWindow;

            // Use whether we can get an HWND to determine if the Window has been loaded.
            if (IntPtr.Zero != _hwnd)
            {
                // We've seen that the HwndSource can't always be retrieved from the HWND, so cache it early.
                // Specifically it seems to sometimes disappear when the OS theme is changing.
                _hwndSource = HwndSource.FromHwnd(_hwnd);
                Assert.IsNotNull(_hwndSource);

                if (!_createdForSystemFrame)
                {
                    // A SystemFrame worker is created while the HWND exists but before the RootVisual is set
                    // (ThemeManager.ApplyStyleOnWindow); applying the template that early would change the
                    // startup sequence of the window, and the SystemFrame mode never touches the template.
                    _window.ApplyTemplate();
                }

                if (_chromeInfo != null)
                {
                    _ApplyNewCustomChrome();
                }
            }
            else
            {
                _window.SourceInitialized += _WindowSourceInitialized;
            }
        }

        private void _WindowSourceInitialized(object sender, EventArgs e)
        {
            _hwnd = new WindowInteropHelper(_window).Handle;
            Assert.IsNotDefault(_hwnd);
            _hwndSource = HwndSource.FromHwnd(_hwnd);
            Assert.IsNotNull(_hwndSource);

            if (_chromeInfo != null)
            {
                _ApplyNewCustomChrome();
            }
        }

        private void UnsubscribeWindowEvents()
        {
            if (_window != null)
            {
                Utility.RemoveDependencyPropertyChangeListener(_window, Window.TemplateProperty, _OnWindowPropertyChangedThatRequiresTemplateFixup);
                Utility.RemoveDependencyPropertyChangeListener(_window, Window.FlowDirectionProperty, _OnWindowPropertyChangedThatRequiresTemplateFixup);
                _window.SourceInitialized -= _WindowSourceInitialized;
            }
        }

        private void _UnsetWindow(object sender, EventArgs e)
        {
            UnsubscribeWindowEvents();

            if (_chromeInfo != null)
            {
                _chromeInfo.PropertyChangedThatRequiresRepaint -= _OnChromePropertyChangedThatRequiresRepaint;
                _chromeInfo.PropertyChangedThatRequiresDwmUpdate -= _OnChromePropertyChangedThatRequiresDwmUpdate;
            }

            _RestoreStandardChromeState(true);
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static WindowChromeWorker GetWindowChromeWorker(Window window)
        {
            Verify.IsNotNull(window, "window");
            return (WindowChromeWorker)window.GetValue(WindowChromeWorkerProperty);
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void SetWindowChromeWorker(Window window, WindowChromeWorker chrome)
        {
            Verify.IsNotNull(window, "window");
            window.SetValue(WindowChromeWorkerProperty, chrome);
        }

        private void _OnWindowPropertyChangedThatRequiresTemplateFixup(object sender, EventArgs e)
        {
            if (_chromeInfo != null && _hwnd != IntPtr.Zero)
            {
                // Assume that when the template changes it's going to be applied.
                // We don't have a good way to externally hook into the template
                // actually being applied, so we asynchronously post the fixup operation
                // at Loaded priority, so it's expected that the visual tree will be
                // updated before _FixupTemplateIssues is called.
                // 
                // Also see comments in RetryFixupTemplateIssuesOnVisualChildrenAdded which is another
                // place where _FixupTemplateIssues is posted. 
                _window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (_Action)_FixupTemplateIssues);
            }
        }

        private void _ApplyNewCustomChrome()
        {
            if (_hwnd == IntPtr.Zero || _hwndSource.IsDisposed)
            {
                // Not yet hooked.
                return;
            }

            if (_chromeInfo == null)
            {
                _RestoreStandardChromeState(false);
                return;
            }

            if (!_isHooked)
            {
                _hwndSource.AddHook(_WndProc);
                _isHooked = true;
            }

            if (_ManagesNonClientArea)
            {
                _FixupTemplateIssues();

                // Force this the first time.
                if (!_IsExtendedMode)
                {
                    // With a real DWM frame (ExtendedClientArea) user32 keeps the system menu up to date itself.
                    _UpdateSystemMenu(_window.WindowState);
                }

                _UpdateFrameState(true);

                _ChangeFrame();
            }
            else
            {
                // SystemFrame: the standard frame is untouched, only DWM attributes and the backdrop are managed.
                _UpdateFrameState(true);
            }

            _UpdateDwmAttributes(WindowChromeDwmAttributes.All);
        }

        /// <summary>
        /// If visual children have been added to <see cref="_window"/>, then repost <see cref="_FixupTemplateIssues"/>
        /// </summary>
        private void RetryFixupTemplateIssuesOnVisualChildrenAdded(object sender, EventArgs e)
        {
            if (sender == _window)
            {
                // Remove the handler for Window.VisualChildrenChanged. This will be hooked up 
                // again in _FixupTemplatedIssues if needed
                _window.VisualChildrenChanged -= RetryFixupTemplateIssuesOnVisualChildrenAdded;

                // At this point, the Window and the its root element are ready. Repost _FixupTemplateIssues
                // at Render priority to ensure that it gets the root element fixed up ASAP. 
                // 
                // Also see comments in _OnWindowPropertyChangedThatRequiresTemplateFixup which is another
                // place where _FixupTemplateIssues is posted. 
                _window.Dispatcher.BeginInvoke(DispatcherPriority.Render, (_Action)_FixupTemplateIssues);
            }
        }

        private void _FixupTemplateIssues()
        {
            Assert.IsNotNull(_chromeInfo);
            Assert.IsNotNull(_window);

            if (!_ManagesNonClientArea)
            {
                // SystemFrame mode never touches the template.
                return;
            }

            if (_window.Template == null)
            {
                // Nothing to fixup yet. This will get called again when a template does get set.
                return;
            }

            // Guard against the visual tree being empty.
            if (VisualTreeHelper.GetChildrenCount(_window) == 0)
            {
                // The template isn't null, but we don't have a visual tree.
                // Wait for the visual tree after ApplyTemplate, and then repost this
                _window.VisualChildrenChanged += RetryFixupTemplateIssuesOnVisualChildrenAdded;
                return;
            }

            Thickness templateFixupMargin = default(Thickness);

            FrameworkElement rootElement = (FrameworkElement)VisualTreeHelper.GetChild(_window, 0);

            // In ExtendedClientArea mode the client rect is computed exactly from the DWM frame bounds, so the
            // template does not need to compensate for anything: NonClientFrameEdges is ignored.
            if (!_IsExtendedMode && _chromeInfo.NonClientFrameEdges != NonClientFrameEdges.None)
            {
                if (Utility.IsFlagSet((int)_chromeInfo.NonClientFrameEdges, (int)NonClientFrameEdges.Top))
                {
#if RIBBON_IN_FRAMEWORK
                    templateFixupMargin.Top -= SystemParameters.WindowResizeBorderThickness.Top;
#else
                    templateFixupMargin.Top -= SystemParameters2.Current.WindowResizeBorderThickness.Top;
#endif
                }
                if (Utility.IsFlagSet((int)_chromeInfo.NonClientFrameEdges, (int)NonClientFrameEdges.Left))
                {
#if RIBBON_IN_FRAMEWORK
                    templateFixupMargin.Left -= SystemParameters.WindowResizeBorderThickness.Left;
#else
                    templateFixupMargin.Left -= SystemParameters2.Current.WindowResizeBorderThickness.Left;
#endif
                }
                if (Utility.IsFlagSet((int)_chromeInfo.NonClientFrameEdges, (int)NonClientFrameEdges.Bottom))
                {
#if RIBBON_IN_FRAMEWORK
                    templateFixupMargin.Bottom -= SystemParameters.WindowResizeBorderThickness.Bottom;
#else
                    templateFixupMargin.Bottom -= SystemParameters2.Current.WindowResizeBorderThickness.Bottom;
#endif
                }
                if (Utility.IsFlagSet((int)_chromeInfo.NonClientFrameEdges, (int)NonClientFrameEdges.Right))
                {
#if RIBBON_IN_FRAMEWORK
                    templateFixupMargin.Right -= SystemParameters.WindowResizeBorderThickness.Right;
#else
                    templateFixupMargin.Right -= SystemParameters2.Current.WindowResizeBorderThickness.Right;
#endif
                }
            }

            if (_IsExtendedMode)
            {
                // Maximized with a sheet-of-glass extension: the client must start at the window top (DWM stops
                // hit-testing its buttons otherwise) while DWM draws the buttons at the visible frame, one border
                // down. Push the content down by the same amount so it lines up with them and nothing is cut off.
                // With a top-band extension the buttons are cut off together with the content, so nothing moves.
                templateFixupMargin.Top = _GetContentTopOffsetLogical();
            }

            rootElement.Margin = templateFixupMargin;

            if (_IsExtendedMode)
            {
                _EnsureCaptionAdorner();
            }
        }

        /// <summary>
        /// Top margin applied to the template root in ExtendedClientArea mode: the off-screen overhang when the
        /// window is maximized and the frame is extended as a sheet of glass, zero otherwise.
        /// </summary>
        private double _GetContentTopOffsetLogical()
        {
            return _IsExtendedMode && _isExtendedFrameActive && _IsSheetOfGlassExtension
                ? _GetMaximizedTopOverhangLogical()
                : 0;
        }

        /// <summary>Re-applies the root margin when the window state or the frame extension changed.</summary>
        private void _UpdateContentTopOffset()
        {
            if (!_IsExtendedMode || _window == null || VisualTreeHelper.GetChildrenCount(_window) == 0)
            {
                return;
            }

            var rootElement = VisualTreeHelper.GetChild(_window, 0) as FrameworkElement;
            if (rootElement == null)
            {
                return;
            }

            double top = _GetContentTopOffsetLogical();
            if (rootElement.Margin.Top != top)
            {
                rootElement.Margin = new Thickness(0, top, 0, 0);
            }
        }

        #region WindowProc and Message Handlers

        private IntPtr _WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Only expecting messages for our cached HWND.
            Assert.AreEqual(hwnd, _hwnd);

            var message = (WM)msg;
            foreach (var handlePair in _messageTable)
            {
                if (handlePair.Key == message)
                {
                    return handlePair.Value(message, wParam, lParam, out handled);
                }
            }
            return IntPtr.Zero;
        }

        private IntPtr _HandleSetTextOrIcon(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            if (!_ManagesNonClientArea || _IsExtendedMode)
            {
                // With a real DWM frame the caption drawing is already suppressed through WTNCA_NODRAWCAPTION and
                // DWM must see title/icon changes (taskbar, Alt+Tab). The adorner redraws through the
                // Window.Title / Window.Icon listeners.
                handled = false;
                return IntPtr.Zero;
            }

            bool modified = _ModifyStyle(WS.VISIBLE, 0);

            // Setting the caption text and icon cause Windows to redraw the caption.
            // Letting the default WndProc handle the message without the WS_VISIBLE
            // style applied bypasses the redraw.
            IntPtr lRet = NativeMethods.DefWindowProc(_hwnd, uMsg, wParam, lParam);

            // Put back the style we removed.
            if (modified)
            {
                _ModifyStyle(0, WS.VISIBLE);
            }
            handled = true;
            return lRet;
        }

        private IntPtr _HandleNCActivate(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            if (!_ManagesNonClientArea || _IsExtendedMode)
            {
                // DWM owns the frame: it must receive the standard WM_NCACTIVATE flow so the active/inactive
                // rendering of the caption buttons and of the border stays in sync.
                handled = false;
                return IntPtr.Zero;
            }

            // Despite MSDN's documentation of lParam not being used,
            // calling DefWindowProc with lParam set to -1 causes Windows not to draw over the caption.

            // Directly call DefWindowProc with a custom parameter
            // which bypasses any other handling of the message.
            IntPtr lRet = NativeMethods.DefWindowProc(_hwnd, WM.NCACTIVATE, wParam, new IntPtr(-1));
            handled = true;
            return lRet;
        }

        // Black Border Workaround
        //
        // 762437 - DWM: Windows that have both clip and alpha margins are drawn without respecting alpha
        // There was a regression in DWM in Windows 7 with regard to handling WM_NCCALCSIZE to effect custom chrome.
        // When windows with glass are maximized on a multi-monitor setup, the glass frame tends to turn black.
        // Also, when windows are resized they tend to flicker black, sometimes staying that way until resized again.
        //
        // At least on RTM Win7 we can avoid the problem by making the client area not extactly match the non-client
        // area, so we added the NonClientFrameEdges property.
        private IntPtr _HandleNCCalcSize(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            if (_FrameMode == WindowChromeFrameMode.SystemFrame)
            {
                return _HandleNCCalcSizeSystemFrame(wParam, lParam, out handled);
            }

            if (!_ManagesNonClientArea)
            {
                handled = false;
                return IntPtr.Zero;
            }

            if (_IsExtendedMode)
            {
                return _HandleNCCalcSizeExtended(wParam, lParam, out handled);
            }

            // lParam is an [in, out] that can be either a RECT* (wParam == FALSE) or an NCCALCSIZE_PARAMS*.
            // Since the first field of NCCALCSIZE_PARAMS is a RECT and is the only field we care about
            // we can unconditionally treat it as a RECT.

            if (_chromeInfo.NonClientFrameEdges != NonClientFrameEdges.None)
            {
                DpiScale dpi = _window.GetDpi();
#if RIBBON_IN_FRAMEWORK
                Thickness windowResizeBorderThicknessDevice = DpiHelper.LogicalThicknessToDevice(SystemParameters.WindowResizeBorderThickness, dpi.DpiScaleX, dpi.DpiScaleY);
#else
                Thickness windowResizeBorderThicknessDevice = DpiHelper.LogicalThicknessToDevice(SystemParameters2.Current.WindowResizeBorderThickness, dpi.DpiScaleX, dpi.DpiScaleY);
#endif
                var rcClientArea = Marshal.PtrToStructure<RECT>(lParam);
                if (Utility.IsFlagSet((int)_chromeInfo.NonClientFrameEdges, (int)NonClientFrameEdges.Top))
                {
                    rcClientArea.Top += (int)windowResizeBorderThicknessDevice.Top;
                }
                if (Utility.IsFlagSet((int)_chromeInfo.NonClientFrameEdges, (int)NonClientFrameEdges.Left))
                {
                    rcClientArea.Left += (int)windowResizeBorderThicknessDevice.Left;
                }
                if (Utility.IsFlagSet((int)_chromeInfo.NonClientFrameEdges, (int)NonClientFrameEdges.Bottom))
                {
                    rcClientArea.Bottom -= (int)windowResizeBorderThicknessDevice.Bottom;
                }
                if (Utility.IsFlagSet((int)_chromeInfo.NonClientFrameEdges, (int)NonClientFrameEdges.Right))
                {
                    rcClientArea.Right -= (int)windowResizeBorderThicknessDevice.Right;
                }

                Marshal.StructureToPtr(rcClientArea, lParam, false);
            }

            handled = true;

            // Per MSDN for NCCALCSIZE, always return 0 when wParam == FALSE
            // 
            // Returning 0 when wParam == TRUE is not appropriate - it will preserve
            // the old client area and align it with the upper-left corner of the new 
            // client area. So we simply ask for a redraw (WVR_REDRAW)

            IntPtr retVal = IntPtr.Zero;
            if (wParam.ToInt32() != 0) // wParam == TRUE
            {
                retVal = new IntPtr((int) (WVR.REDRAW));
            }

            return retVal;
        }

        /// <summary>
        /// WM_NCCALCSIZE for SystemFrame: the standard frame is laid out by DefWindowProc. One correction is
        /// needed while a backdrop is applied: the standard layout of a maximized window puts the client right
        /// below a caption that is one border shorter (that border lies off-screen), but with a sheet-of-glass
        /// extension DWM draws the caption at its full height anchored to the visible frame, so the caption band
        /// would overlap the top of the client by that border. Push the client down accordingly.
        /// </summary>
        private IntPtr _HandleNCCalcSizeSystemFrame(IntPtr wParam, IntPtr lParam, out bool handled)
        {
            handled = false;

            // Only the sheet of glass moves the caption; explicit glass margins (a band) keep the standard layout.
            if (wParam == IntPtr.Zero || lParam == IntPtr.Zero || !_systemFrameSheetOfGlassApplied || !_IsHwndMaximized())
            {
                return IntPtr.Zero;
            }

            int border = _GetSystemFrameBorderDevice();
            if (border <= 0)
            {
                return IntPtr.Zero;
            }

            IntPtr result = NativeMethods.DefWindowProc(_hwnd, WM.NCCALCSIZE, wParam, lParam);

            var rc = Marshal.PtrToStructure<RECT>(lParam);
            rc.Top += border;
            Marshal.StructureToPtr(rc, lParam, false);

            handled = true;
            return result;
        }

        /// <summary>
        /// Thickness of the invisible resize border in device pixels: the measured frame insets when available
        /// (the standard frame is always measurable in SystemFrame mode), system metrics otherwise.
        /// </summary>
        private int _GetSystemFrameBorderDevice()
        {
            if (_hasNormalFrameInsets)
            {
                return _normalFrameInsets.Left;
            }

            return _GetResizeFrameThickness(_GetNonClientDpi());
        }

        /// <summary>
        /// Whether the system scales the non-client area of this window to the DPI of its monitor. Only the
        /// per-monitor V2 awareness context does; with the V1 context the content follows the monitor while the
        /// frame stays at the system DPI, and system-aware or unaware windows are virtualized as a whole.
        /// </summary>
        private bool _IsNonClientScaledPerMonitor()
        {
            return _hwnd != IntPtr.Zero
                   && MS.Internal.DpiUtil.GetDpiAwarenessContext(_hwnd).Equals(MS.Utility.DpiAwarenessContextValue.PerMonitorAwareVersion2);
        }

        /// <summary>
        /// The DPI the non-client metrics of the window are expressed at: the window's DPI when the frame is
        /// scaled per monitor (V2), the system DPI otherwise. 96 when the HWND is not available yet.
        /// </summary>
        private uint _GetNonClientDpi()
        {
            if (_window == null || _hwnd == IntPtr.Zero)
            {
                return 96u;
            }

            if (_IsNonClientScaledPerMonitor())
            {
                return (uint)Math.Round(_window.GetDpi().PixelsPerInchY);
            }

            MS.Internal.DpiScale2 systemDpi = MS.Internal.DpiUtil.GetSystemDpi();
            return systemDpi != null ? (uint)Math.Round(systemDpi.PixelsPerInchY) : 96u;
        }

        /// <summary>
        /// WM_NCCALCSIZE for ExtendedClientArea: the client rect is the proposed window rect minus the insets
        /// measured from DWMWA_EXTENDED_FRAME_BOUNDS (the invisible resize border), so the WPF content starts
        /// exactly at the visible frame while DWM keeps drawing the caption buttons on top of it.
        /// </summary>
        private IntPtr _HandleNCCalcSizeExtended(IntPtr wParam, IntPtr lParam, out bool handled)
        {
            if (!_isExtendedFrameActive || lParam == IntPtr.Zero)
            {
                // No composition: leave the standard frame alone.
                handled = false;
                return IntPtr.Zero;
            }

            if (wParam == IntPtr.Zero)
            {
                // wParam == FALSE is the query form used by the system (and by DWM when it lays out its own frame
                // elements) to map a window rect to a client rect. Leave it to DefWindowProc: answering with the
                // extended client area here alters the caption button layout DWM computes.
                handled = false;
                return IntPtr.Zero;
            }

            // rgrc[0] is the first field of NCCALCSIZE_PARAMS.
            var rc = Marshal.PtrToStructure<RECT>(lParam);

            if (!_hasNormalFrameInsets && !_MeasureNormalFrameInsets())
            {
                if (!_IsHwndMaximized())
                {
                    handled = false;
                    return IntPtr.Zero;
                }

                // The window is maximized (shown that way, or the theme changed while maximized): the standard
                // frame cannot be measured now. Use the system metrics so the extended frame is applied anyway
                // and measure for real once the window is restored (see _RemeasureProvisionalInsets).
                _SynthesizeFrameInsets();
            }

            bool maximized = _IsHwndMaximized();
            FrameInsets insets = _GetEffectiveFrameInsets(maximized);

            rc.Left += insets.Left;
            rc.Top += insets.Top;
            rc.Right -= insets.Right;
            rc.Bottom -= insets.Bottom;

            if (maximized)
            {
                IntPtr hMon = NativeMethods.MonitorFromRect(ref rc, MONITOR_DEFAULTTONEAREST);
                if (hMon == IntPtr.Zero)
                {
                    hMon = NativeMethods.MonitorFromWindow(_hwnd, MONITOR_DEFAULTTONEAREST);
                }

                if (hMon != IntPtr.Zero)
                {
                    MONITORINFO mi = NativeMethods.GetMonitorInfo(hMon);
                    _ApplyAutoHideTaskbarInset(ref rc, mi.rcMonitor);
                }
            }

            Marshal.StructureToPtr(rc, lParam, false);
            handled = true;

            // See the remarks in the legacy handler: 0 for wParam == FALSE, WVR_REDRAW otherwise.
            return wParam != IntPtr.Zero ? new IntPtr((int)WVR.REDRAW) : IntPtr.Zero;
        }

        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        /// <summary>
        /// The insets between the window rect and the client area for the current state. A maximized window
        /// is inflated by the invisible border on every side, including the top: the vertical inset measured in
        /// the Normal state (all at the bottom) is split between top and bottom, exactly as DWM lays the frame out.
        /// During the maximize transition GetWindowRect and DWMWA_EXTENDED_FRAME_BOUNDS are stale, so the cached
        /// Normal-state measurement is the only reliable source.
        /// </summary>
        private FrameInsets _GetEffectiveFrameInsets(bool maximized)
        {
            if (!_hasNormalFrameInsets)
            {
                return default(FrameInsets);
            }

            if (!maximized)
            {
                return _normalFrameInsets;
            }

            // A maximized window is inflated by the resize frame on every side, so one frame thickness of the
            // window lies above the screen. The client area is nevertheless kept at the very top of the window
            // rect: DWM keeps hit-testing its caption buttons only while the client starts there, with both kinds
            // of frame extension (moving the client top down disables them). The top frame thus falls off-screen;
            // _GetMaximizedTopOverhangLogical() lets the title and the caption band follow the visible frame. Where
            // DWM draws the buttons depends on the extension: at the client top with a top band, at the visible
            // frame with a sheet of glass (see _UpdateCaptionButtonsBounds).
            // Left, right and bottom use the frame thickness itself rather than the Normal-state inset (which is
            // one border line smaller): the client then coincides with the work area, like a standard maximized window.
            int frame = _GetMaximizedFrameThicknessDevice();
            return new FrameInsets
            {
                Left = frame,
                Top = 0,
                Right = frame,
                Bottom = frame,
            };
        }

        /// <summary>The thickness by which the system inflates a maximized window on every side, in device pixels.</summary>
        private int _GetMaximizedFrameThicknessDevice()
        {
            return _GetResizeFrameThickness(_GetNonClientDpi());
        }

        /// <summary>Whether the DWM frame is extended over the whole window (MARGINS -1) rather than as a top band.</summary>
        private bool _IsSheetOfGlassExtension
        {
            get
            {
                if (_chromeInfo == null)
                {
                    return false;
                }

                // A negative GlassFrameThickness is an explicit request for the sheet of glass and always wins.
                if (!Utility.IsThicknessNonNegative(_chromeInfo.GlassFrameThickness))
                {
                    return true;
                }

                // A backdrop gets the sheet of glass unless the application sized the band itself (explicit
                // CaptionHeight) or asked for specific glass margins.
                return _isBackdropApplied && !_chromeInfo.IsCaptionHeightSet && !_HasExplicitGlassMargins;
            }
        }

        /// <summary>
        /// Whether the application asked for specific glass margins.  They are extended as such in every theme,
        /// with or without a backdrop, instead of the sheet of glass a backdrop would otherwise get.
        /// </summary>
        private bool _HasExplicitGlassMargins
        {
            get
            {
                return _chromeInfo != null
                       && _chromeInfo.IsGlassFrameThicknessSet
                       && Utility.IsThicknessNonNegative(_chromeInfo.GlassFrameThickness);
            }
        }

        /// <summary>
        /// Device independent pixels of client area lying above the screen when the window is maximized: the
        /// resize frame thickness (the same on every side). Used to place the title, and to extend the
        /// caption hit-test band, so they line up with the visible part of the caption; the DWM buttons need no
        /// correction because DWM anchors them to the client top.
        /// </summary>
        private double _GetMaximizedTopOverhangLogical()
        {
            if (!_hasNormalFrameInsets || !_IsHwndMaximized() || _hwndSource?.CompositionTarget == null)
            {
                return 0;
            }

            return _hwndSource.CompositionTarget.TransformFromDevice.Transform(new Vector(0, _GetMaximizedFrameThicknessDevice())).Y;
        }

        /// <summary>
        /// A maximized window whose client covers the whole monitor is treated as full screen by the shell, and
        /// an auto-hide app bar (the taskbar) can then no longer be summoned with the mouse. DefWindowProc avoids
        /// it by leaving a one pixel strip free along the edge that hosts the bar; replicate it relative to the
        /// monitor, since the client is positioned from the window rect. The top is never touched: moving the
        /// client top makes DWM stop hit-testing its caption buttons. A bar on the top edge gets its free row at
        /// the bottom instead: the shell only needs the window not to cover the entire monitor, on whichever edge
        /// the free row lies.
        /// </summary>
        private static void _ApplyAutoHideTaskbarInset(ref RECT rc, RECT rcMonitor)
        {
            foreach (ABE edge in new[] { ABE.LEFT, ABE.TOP, ABE.RIGHT, ABE.BOTTOM })
            {
                var abd = new APPBARDATA
                {
                    cbSize = Marshal.SizeOf<APPBARDATA>(),
                    uEdge = edge,
                    rc = rcMonitor,
                };

                if (NativeMethods.SHAppBarMessage(ABM.GETAUTOHIDEBAREX, ref abd) == IntPtr.Zero)
                {
                    continue;
                }

                switch (edge)
                {
                    case ABE.LEFT:
                        rc.Left = Math.Max(rc.Left, rcMonitor.Left + 1);
                        break;
                    case ABE.RIGHT:
                        rc.Right = Math.Min(rc.Right, rcMonitor.Right - 1);
                        break;
                    case ABE.TOP:
                    case ABE.BOTTOM:
                        rc.Bottom = Math.Min(rc.Bottom, rcMonitor.Bottom - 1);
                        break;
                }
            }
        }

        /// <summary>
        /// Factor converting DWM (physical monitor) pixels into the window's pixels. It differs from 1 only for
        /// a DPI-virtualized window (system-DPI-aware process on a monitor with a different DPI), and only when
        /// the ratio measured between the window rect and the visible frame agrees with the DPI ratio.
        /// </summary>
        internal static double ComputeDwmToWindowScale(int windowWidth, int visibleWidth, double windowDpi, double monitorDpi)
        {
            if (windowWidth <= 0 || visibleWidth <= 0 || windowDpi <= 0 || monitorDpi <= 0)
            {
                return 1.0;
            }

            double dpiScale = windowDpi / monitorDpi;
            if (Math.Abs(dpiScale - 1.0) < 0.01)
            {
                return 1.0;
            }

            double measured = (double)windowWidth / visibleWidth;
            return Math.Abs(measured - dpiScale) <= Math.Abs(measured - 1.0) ? dpiScale : 1.0;
        }

        /// <summary>
        /// Insets of the visible frame inside the window rect for a window in the Normal state: the invisible
        /// resize border is split evenly left/right and lies entirely at the bottom (there is none on top).
        /// </summary>
        private static FrameInsets _ComputeNormalFrameInsets(RECT windowRect, RECT visibleRect, double scale)
        {
            double visibleWidth = visibleRect.Width * scale;
            double visibleHeight = visibleRect.Height * scale;

            int horizontal = Math.Max(0, (int)Math.Round((windowRect.Width - visibleWidth) / 2));
            int vertical = Math.Max(0, (int)Math.Round(windowRect.Height - visibleHeight));

            return new FrameInsets { Left = horizontal, Top = 0, Right = horizontal, Bottom = vertical };
        }

        /// <summary>Test hook for <see cref="_ComputeNormalFrameInsets"/>: returns (left, top, right, bottom).</summary>
        internal static Int32Rect ComputeNormalFrameInsets(int windowWidth, int windowHeight, int visibleWidth, int visibleHeight, double scale)
        {
            var windowRect = new RECT { Left = 0, Top = 0, Right = windowWidth, Bottom = windowHeight };
            var visibleRect = new RECT { Left = 0, Top = 0, Right = visibleWidth, Bottom = visibleHeight };
            FrameInsets insets = _ComputeNormalFrameInsets(windowRect, visibleRect, scale);
            return new Int32Rect(insets.Left, insets.Top, insets.Right, insets.Bottom);
        }

        /// <summary>
        /// Measures the frame insets while the HWND is in the Normal state. Returns false when the value
        /// could not be measured (DWM unavailable, window maximized/minimized, implausible result).
        /// </summary>
        /// <remarks>
        /// DWMWA_EXTENDED_FRAME_BOUNDS describes the frame the window currently has. Once our WM_NCCALCSIZE
        /// answer has removed the frame the measurement degenerates (typically to zero insets), so this must only
        /// be called while the standard frame is in place: at the first application, when the system has just
        /// recomputed the frame (theme / settings / composition change) or after <see cref="_MeasureWithStandardFrame"/>
        /// forced a standard pass. It is never called on size or move.
        /// </remarks>
        private bool _MeasureNormalFrameInsets()
        {
            if (!_IsHwndAlive || _hwndSource.CompositionTarget == null)
            {
                return false;
            }

            if (_IsHwndMaximized() || NativeMethods.GetWindowPlacement(_hwnd).showCmd == SW.SHOWMINIMIZED)
            {
                return false;
            }

            RECT windowRect = NativeMethods.GetWindowRect(_hwnd);
            RECT visibleRect;
            if (!NativeMethods.DwmGetWindowAttributeRect(_hwnd, DWMWA.EXTENDED_FRAME_BOUNDS, out visibleRect))
            {
                return false;
            }

            double windowDpi = _hwndSource.CompositionTarget.TransformToDevice.M11 * 96.0;
            double monitorDpi = NativeMethods.GetEffectiveMonitorDpi(_hwnd);
            double scale = ComputeDwmToWindowScale(windowRect.Width, visibleRect.Width, windowDpi, monitorDpi);

            FrameInsets insets = _ComputeNormalFrameInsets(windowRect, visibleRect, scale);

            // The invisible border has the same thickness left, right and bottom. A vertical inset clearly larger
            // than the horizontal one means DWM reported stale (maximized) bounds: inflated by the border on every
            // side, the vertical excess is twice the horizontal one.
            if (insets.Bottom > insets.Left + 2)
            {
                return false;
            }

            if (insets.Left == 0 && insets.Right == 0 && insets.Bottom == 0)
            {
                // The visible frame coincides with the window rect: a theme that draws visible borders (the
                // Windows 7/8 DWM frame, high contrast). Those borders are the standard non-client frame, so
                // the insets are the distance between the window rect and the standard client rect, which the
                // window still has at this point. The top stays inside the client: only the caption band is
                // extended, exactly as with the invisible border.
                RECT clientRect = NativeMethods.GetClientRectInScreen(_hwnd);
                insets = new FrameInsets
                {
                    Left = Math.Max(0, clientRect.Left - windowRect.Left),
                    Top = 0,
                    Right = Math.Max(0, windowRect.Right - clientRect.Right),
                    Bottom = Math.Max(0, windowRect.Bottom - clientRect.Bottom),
                };
            }

            _normalFrameInsets = insets;
            _dwmToWindowScale = scale;
            _frameInsetsDpi = windowDpi;
            _hasNormalFrameInsets = true;
            _frameInsetsProvisional = false;
            _provisionalRemeasureAttempts = 0;
            return true;
        }

        /// <summary>
        /// Fills the frame insets from the system metrics when they cannot be measured because the window is
        /// maximized (DWMWA_EXTENDED_FRAME_BOUNDS then describes the maximized frame): the invisible border is the
        /// resize frame plus the padded border minus the one pixel visible border, on the left, right and bottom.
        /// The value is provisional and is replaced by a measurement once the window is Normal again.
        /// </summary>
        private void _SynthesizeFrameInsets()
        {
            double windowDpi = _hwndSource?.CompositionTarget != null
                ? _hwndSource.CompositionTarget.TransformToDevice.M11 * 96.0
                : 96.0;
            int inset = Math.Max(0, _GetResizeFrameThickness(_GetNonClientDpi()) - 1);

            _normalFrameInsets = new FrameInsets { Left = inset, Top = 0, Right = inset, Bottom = inset };
            _dwmToWindowScale = _GetCurrentDwmToWindowScale();
            _frameInsetsDpi = windowDpi;
            _hasNormalFrameInsets = true;
            _frameInsetsProvisional = true;
        }

        /// <summary>
        /// Replaces provisional insets with a real measurement once the window is Normal and at rest: the standard
        /// frame is put back for one pass, measured, and the extended frame is applied again.
        /// </summary>
        private void _RemeasureProvisionalInsets()
        {
            try
            {
                if (!_IsHwndAlive || !_IsExtendedMode || !_isExtendedFrameActive
                    || !_hasNormalFrameInsets || !_frameInsetsProvisional || _IsHwndMaximized())
                {
                    return;
                }

                _provisionalRemeasureAttempts++;
                _hasNormalFrameInsets = false;
                _MeasureWithStandardFrame();

                if (!_hasNormalFrameInsets)
                {
                    // Could not measure even in the Normal state: keep the metrics so the frame stays consistent.
                    _SynthesizeFrameInsets();
                }

                _ChangeFrame();
                _UpdateContentTopOffset();
                _PostUpdateCaptionButtonsBounds();
                _captionAdorner?.InvalidateVisual();
            }
            finally
            {
                _remeasureProvisionalPending = false;
            }
        }

        /// <summary>
        /// The current factor converting DWM (physical monitor) pixels into window pixels. Unlike the frame
        /// insets this must be evaluated at every use: a system-DPI-aware window gets no WM_DPICHANGED when it
        /// moves to a monitor with a different scale, yet DWM keeps reporting physical pixels. The ratio between
        /// the window rect and the DWM bounds is only used as a sanity check, so it does not matter whether the
        /// bounds describe the standard or the customized frame.
        /// </summary>
        private double _GetCurrentDwmToWindowScale()
        {
            if (!_IsHwndAlive || _hwndSource.CompositionTarget == null)
            {
                return _dwmToWindowScale;
            }

            RECT visibleRect;
            if (!NativeMethods.DwmGetWindowAttributeRect(_hwnd, DWMWA.EXTENDED_FRAME_BOUNDS, out visibleRect))
            {
                return _dwmToWindowScale;
            }

            RECT windowRect = NativeMethods.GetWindowRect(_hwnd);
            double windowDpi = _hwndSource.CompositionTarget.TransformToDevice.M11 * 96.0;
            double monitorDpi = NativeMethods.GetEffectiveMonitorDpi(_hwnd);
            return ComputeDwmToWindowScale(windowRect.Width, visibleRect.Width, windowDpi, monitorDpi);
        }

        /// <summary>
        /// Measures the insets after letting the system compute a standard frame: with the extended frame
        /// inactive the WM_NCCALCSIZE handler passes the message through, so one SWP_FRAMECHANGED restores the
        /// standard client rect that DWMWA_EXTENDED_FRAME_BOUNDS needs. The caller re-applies the extended
        /// frame afterwards.
        /// </summary>
        private void _MeasureWithStandardFrame()
        {
            bool wasActive = _isExtendedFrameActive;
            _isExtendedFrameActive = false;
            try
            {
                NativeMethods.SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0, _SwpFlags);
                if (!_MeasureNormalFrameInsets() && _IsHwndMaximized())
                {
                    // Maximized: measure later (see _RemeasureProvisionalInsets), use the metrics meanwhile.
                    _SynthesizeFrameInsets();
                }
            }
            finally
            {
                _isExtendedFrameActive = wasActive;
            }
        }

        /// <summary>
        /// Rescales the cached insets when the window DPI changes. Measuring again is not an option: the frame
        /// is already customized at that point. The invisible border is the resize frame plus the padded border
        /// minus the one-pixel visible border, and only the first two scale with the DPI, so the insets follow
        /// the change of those metrics rather than a rounded ratio (7 px at 100% becomes 11 px at 150%, not 10).
        /// </summary>
        private void _RescaleFrameInsets(double newDpi)
        {
            if (!_hasNormalFrameInsets || _frameInsetsDpi <= 0 || newDpi <= 0 || Math.Abs(newDpi - _frameInsetsDpi) < 0.5)
            {
                return;
            }

            int oldFrame = _GetResizeFrameThickness((uint)Math.Round(_frameInsetsDpi));
            int newFrame = _GetResizeFrameThickness((uint)Math.Round(newDpi));
            _normalFrameInsets = _RescaleFrameInsets(_normalFrameInsets, newFrame - oldFrame);
            _frameInsetsDpi = newDpi;
        }

        // SM_CXFRAME + SM_CXPADDEDBORDER at the given DPI: the thickness Windows uses for the resize frame.
        private static int _GetResizeFrameThickness(uint dpi)
        {
            return NativeMethods.GetSystemMetricsForDpi(SM.CXFRAME, dpi) + NativeMethods.GetSystemMetricsForDpi(SM.CXPADDEDBORDER, dpi);
        }

        private static FrameInsets _RescaleFrameInsets(FrameInsets insets, int frameDelta)
        {
            // A zero inset stays zero: that side has no invisible border (the top of a normal window).
            return new FrameInsets
            {
                Left = _RescaleInset(insets.Left, frameDelta),
                Top = _RescaleInset(insets.Top, frameDelta),
                Right = _RescaleInset(insets.Right, frameDelta),
                Bottom = _RescaleInset(insets.Bottom, frameDelta),
            };
        }

        private static int _RescaleInset(int inset, int frameDelta)
        {
            return inset > 0 ? Math.Max(0, inset + frameDelta) : 0;
        }

        /// <summary>Test hook for <see cref="_RescaleFrameInsets(FrameInsets, int)"/>: (left, top, right, bottom).</summary>
        internal static Int32Rect RescaleFrameInsets(Int32Rect insets, int oldFrameThickness, int newFrameThickness)
        {
            var value = new FrameInsets { Left = insets.X, Top = insets.Y, Right = insets.Width, Bottom = insets.Height };
            FrameInsets result = _RescaleFrameInsets(value, newFrameThickness - oldFrameThickness);
            return new Int32Rect(result.Left, result.Top, result.Right, result.Bottom);
        }

        private HT _GetHTFromResizeGripDirection(ResizeGripDirection direction)
        {
            bool compliment = _window.FlowDirection == FlowDirection.RightToLeft;
            switch (direction)
            {
                case ResizeGripDirection.Bottom:
                    return HT.BOTTOM;
                case ResizeGripDirection.BottomLeft:
                    return compliment ? HT.BOTTOMRIGHT : HT.BOTTOMLEFT;
                case ResizeGripDirection.BottomRight:
                    return compliment ? HT.BOTTOMLEFT : HT.BOTTOMRIGHT;
                case ResizeGripDirection.Left:
                    return compliment ? HT.RIGHT : HT.LEFT;
                case ResizeGripDirection.Right:
                    return compliment ? HT.LEFT : HT.RIGHT;
                case ResizeGripDirection.Top:
                    return HT.TOP;
                case ResizeGripDirection.TopLeft:
                    return compliment ? HT.TOPRIGHT : HT.TOPLEFT;
                case ResizeGripDirection.TopRight:
                    return compliment ? HT.TOPLEFT : HT.TOPRIGHT;
                default:
                    return HT.NOWHERE;
            }
        }

        private IntPtr _HandleNCHitTest(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            if (!_ManagesNonClientArea || (_IsExtendedMode && !_isExtendedFrameActive))
            {
                handled = false;
                return IntPtr.Zero;
            }

            DpiScale dpi = _window.GetDpi();

            // Let the system know if we consider the mouse to be in our effective non-client area.
            var mousePosScreen = new Point(Utility.GET_X_LPARAM(lParam), Utility.GET_Y_LPARAM(lParam));
            Rect windowPosition = _GetWindowRect();

            Point mousePosWindow = mousePosScreen;
            mousePosWindow.Offset(-windowPosition.X, -windowPosition.Y);

            if (_IsExtendedMode)
            {
                // WPF coordinates are relative to the client area, which in this mode does not coincide with the
                // window rect: it is inset by the invisible border (on every side when maximized). The resize
                // borders, on the other hand, are still evaluated on the window rect below, because they live in
                // that invisible border outside the client area.
                FrameInsets insets = _GetEffectiveFrameInsets(_IsHwndMaximized());
                mousePosWindow.Offset(-insets.Left, -insets.Top);
            }

            mousePosWindow = DpiHelper.DevicePixelsToLogical(mousePosWindow, dpi.DpiScaleX, dpi.DpiScaleY);

            if (_IsExtendedMode && _AreDwmCaptionButtonsInUse)
            {
                // With a real system frame the caption buttons are DWM's and must always win, before any WPF
                // content is consulted: the content is expected to stay out of CaptionButtonsBounds anyway.
                IntPtr dwmRet;
                if (NativeMethods.DwmDefWindowProc(_hwnd, uMsg, wParam, lParam, out dwmRet) && dwmRet != IntPtr.Zero)
                {
                    handled = true;
                    return dwmRet;
                }
            }

            if (_IsExtendedMode && _captionAdorner != null && _captionAdorner.ShowIcon)
            {
                // The icon drawn by the adorner gets the standard system behavior through HTSYSMENU:
                // DefWindowProc opens the system menu on click and closes the window on double click.
                Rect iconBounds = _captionAdorner.IconBounds;
                if (!iconBounds.IsEmpty && iconBounds.Contains(mousePosWindow))
                {
                    handled = true;
                    return new IntPtr((int)HT.SYSMENU);
                }
            }

            // If the app is asking for content to be treated as client then that takes precedence over _everything_, even DWM caption buttons.
            // This allows apps to set the glass frame to be non-empty, still cover it with WPF content to hide all the glass,
            // yet still get DWM to draw a drop shadow.
            IInputElement inputElement = _window.InputHitTest(mousePosWindow);
            if (inputElement != null)
            {
                // An element standing for a caption button gets the system's hit code, so the system treats it as
                // the button (window layout flyout over the maximize one); only while no system-drawn buttons exist.
                CaptionButtonRole role;
                if (!_AreDwmCaptionButtonsInUse && _FindCaptionButtonElement(inputElement as DependencyObject, out role) != null)
                {
                    handled = true;
                    return new IntPtr((int)_GetHTFromCaptionButtonRole(role));
                }

                if (WindowChrome.GetIsHitTestVisibleInChrome(inputElement))
                {
                    handled = true;
                    return new IntPtr((int)HT.CLIENT);
                }

                ResizeGripDirection direction = WindowChrome.GetResizeGripDirection(inputElement);
                if (direction != ResizeGripDirection.None)
                {
                    handled = true;
                    return new IntPtr((int)_GetHTFromResizeGripDirection(direction));
                }
            }

            // It's not opted out, so offer up the hittest to DWM, then to our custom non-client area logic.
            // (In ExtendedClientArea mode DWM was already consulted above.)
            if (!_IsExtendedMode && _chromeInfo.UseAeroCaptionButtons && _AreDwmCaptionButtonsInUse)
            {
                IntPtr lRet;

                // Give the DWM a chance to handle the message first (caption buttons).
                handled = NativeMethods.DwmDefWindowProc(_hwnd, uMsg, wParam, lParam, out lRet);

                if (IntPtr.Zero != lRet)
                {
                    // If DWM claims to have handled this, then respect their call.
                    return lRet;
                }
            }

            Rect windowPositionLogical = DpiHelper.DeviceRectToLogical(windowPosition, dpi.DpiScaleX, dpi.DpiScaleY);
            Point mousePosScreenLogical = DpiHelper.DevicePixelsToLogical(mousePosScreen, dpi.DpiScaleX, dpi.DpiScaleY);
            HT ht;

            if (_IsExtendedMode)
            {
                // The resize borders are the invisible border measured from the DWM frame (left, right and bottom,
                // outside the client area) plus the top strip inside the client; they disappear when maximized and
                // the caption band is then extended by the off-screen part so the whole visible title stays draggable.
                bool maximized = _IsHwndMaximized();
                Thickness resizeBorder = new Thickness();
                double captionHeight = _chromeInfo.CaptionHeight;

                if (maximized)
                {
                    captionHeight += _GetMaximizedTopOverhangLogical();
                }
                else
                {
                    // The same thickness on every side: the invisible border for left, right and bottom, and a
                    // strip of the same height inside the client at the top, like the standard frame.
                    FrameInsets insets = _GetEffectiveFrameInsets(false);
                    Point insetsLogical = DpiHelper.DevicePixelsToLogical(new Point(insets.Left, insets.Bottom), dpi.DpiScaleX, dpi.DpiScaleY);
                    resizeBorder = new Thickness(insetsLogical.X, insetsLogical.Y, insetsLogical.X, insetsLogical.Y);
                }

                ht = _HitTestNcaExtended(windowPositionLogical, mousePosScreenLogical, resizeBorder, captionHeight);
            }
            else
            {
                ht = _HitTestNca(windowPositionLogical, mousePosScreenLogical, _chromeInfo.ResizeBorderThickness, _chromeInfo.CaptionHeight);
            }

            handled = true;
            return new IntPtr((int)ht);
        }

        /// <summary>Whether DWM is currently drawing the caption buttons over an area the worker hit-tests.</summary>
        private bool _AreDwmCaptionButtonsInUse
        {
            get
            {
                if (_chromeInfo == null)
                {
                    return false;
                }

                if (_IsExtendedMode)
                {
                    // The system frame is kept, so DWM draws its buttons whatever UseAeroCaptionButtons says:
                    // they must be hit-tested, otherwise they would be visible but dead.
                    return _isExtendedFrameActive;
                }

                return _chromeInfo.UseAeroCaptionButtons && Utility.IsOSVistaOrNewer && _chromeInfo.GlassFrameThickness != default(Thickness) && _isGlassEnabled;
            }
        }

        private IntPtr _HandleNCRButtonUp(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            // Emulate the system behavior of clicking the right mouse button over the caption area, or over the
            // window icon (the one drawn by the chrome, which the hit test reports as the system menu), to bring
            // up the system menu.
            var ht = (HT)wParam.ToInt32();
            if (_ManagesNonClientArea && (ht == HT.CAPTION || ht == HT.SYSMENU))
            {
                SystemCommands.ShowSystemMenuPhysicalCoordinates(_window, new Point(Utility.GET_X_LPARAM(lParam), Utility.GET_Y_LPARAM(lParam)));
            }
            handled = false;
            return IntPtr.Zero;
        }

        #region Caption button roles (elements that stand for the system caption buttons)

        private static HT _GetHTFromCaptionButtonRole(CaptionButtonRole role)
        {
            switch (role)
            {
                case CaptionButtonRole.Minimize:
                    return HT.MINBUTTON;
                case CaptionButtonRole.Maximize:
                    return HT.MAXBUTTON;
                case CaptionButtonRole.Close:
                    return HT.CLOSE;
                default:
                    return HT.CLIENT;
            }
        }

        private static bool _IsCaptionButtonHit(HT ht)
        {
            return ht == HT.MINBUTTON || ht == HT.MAXBUTTON || ht == HT.CLOSE;
        }

        /// <summary>
        /// The element that carries the CaptionButtonRole covering <paramref name="hit"/>: the property is inherited,
        /// so the hit element may be a child of the button; walk up to the element the role was set on.
        /// </summary>
        private static DependencyObject _FindCaptionButtonElement(DependencyObject hit, out CaptionButtonRole role)
        {
            role = CaptionButtonRole.None;
            DependencyObject current = hit;
            while (current != null)
            {
                var value = (CaptionButtonRole)current.GetValue(WindowChrome.CaptionButtonRoleProperty);
                if (value == CaptionButtonRole.None)
                {
                    return null;
                }

                BaseValueSource source = DependencyPropertyHelper.GetValueSource(current, WindowChrome.CaptionButtonRoleProperty).BaseValueSource;
                if (source != BaseValueSource.Inherited && source != BaseValueSource.Default)
                {
                    role = value;
                    return current;
                }

                current = (current is Visual || current is System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(current) : null)
                          ?? LogicalTreeHelper.GetParent(current);
            }

            return null;
        }

        /// <summary>The caption button element under the screen point of a non-client mouse message, if any.</summary>
        private DependencyObject _GetCaptionButtonAt(IntPtr lParam, out CaptionButtonRole role)
        {
            role = CaptionButtonRole.None;
            if (!_IsHwndAlive || _window == null || _AreDwmCaptionButtonsInUse)
            {
                return null;
            }

            DpiScale dpi = _window.GetDpi();
            var mousePosWindow = new Point(Utility.GET_X_LPARAM(lParam), Utility.GET_Y_LPARAM(lParam));
            Rect windowPosition = _GetWindowRect();
            mousePosWindow.Offset(-windowPosition.X, -windowPosition.Y);
            if (_IsExtendedMode)
            {
                FrameInsets insets = _GetEffectiveFrameInsets(_IsHwndMaximized());
                mousePosWindow.Offset(-insets.Left, -insets.Top);
            }
            mousePosWindow = DpiHelper.DevicePixelsToLogical(mousePosWindow, dpi.DpiScaleX, dpi.DpiScaleY);

            return _FindCaptionButtonElement(_window.InputHitTest(mousePosWindow) as DependencyObject, out role);
        }

        private void _SetHoveredCaptionButton(DependencyObject element)
        {
            if (ReferenceEquals(element, _hoveredCaptionButton))
            {
                return;
            }

            _hoveredCaptionButton?.ClearValue(WindowChrome.IsCaptionButtonHoveredPropertyKey);
            _hoveredCaptionButton = element;

            if (element != null)
            {
                element.SetValue(WindowChrome.IsCaptionButtonHoveredPropertyKey, true);
                // The input pipeline does not track the pointer here: ask for WM_NCMOUSELEAVE ourselves.
                NativeMethods.TrackNonClientMouseLeave(_hwnd);
            }
            else
            {
                _SetPressedCaptionButton(null);
            }
        }

        private void _SetPressedCaptionButton(DependencyObject element)
        {
            if (ReferenceEquals(element, _pressedCaptionButton))
            {
                return;
            }

            _pressedCaptionButton?.ClearValue(WindowChrome.IsCaptionButtonPressedPropertyKey);
            _pressedCaptionButton = element;
            element?.SetValue(WindowChrome.IsCaptionButtonPressedPropertyKey, true);
        }

        /// <summary>
        /// Performs the action of a clicked caption button element: its own invoke pattern (command or click
        /// handler) when it has one, the window command of its role otherwise.
        /// </summary>
        private void _InvokeCaptionButton(DependencyObject element, CaptionButtonRole role)
        {
            if (element is UIElement uiElement)
            {
                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(uiElement);
                if (peer?.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoke)
                {
                    try
                    {
                        invoke.Invoke();
                    }
                    catch (ElementNotEnabledException)
                    {
                        // A disabled button does nothing, like a disabled system button.
                    }
                    return;
                }
            }

            switch (role)
            {
                case CaptionButtonRole.Minimize:
                    SystemCommands.MinimizeWindow(_window);
                    break;
                case CaptionButtonRole.Maximize:
                    if (_window.WindowState == WindowState.Maximized)
                    {
                        SystemCommands.RestoreWindow(_window);
                    }
                    else
                    {
                        SystemCommands.MaximizeWindow(_window);
                    }
                    break;
                case CaptionButtonRole.Close:
                    SystemCommands.CloseWindow(_window);
                    break;
            }
        }

        private IntPtr _HandleNCMouseMove(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            handled = false;
            if (!_ManagesNonClientArea)
            {
                return IntPtr.Zero;
            }

            CaptionButtonRole role;
            DependencyObject element = _IsCaptionButtonHit((HT)wParam.ToInt32()) ? _GetCaptionButtonAt(lParam, out role) : null;
            _SetHoveredCaptionButton(element);
            return IntPtr.Zero;
        }

        private IntPtr _HandleNCLButtonDown(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            handled = false;
            if (!_ManagesNonClientArea || !_IsCaptionButtonHit((HT)wParam.ToInt32()))
            {
                return IntPtr.Zero;
            }

            CaptionButtonRole role;
            DependencyObject element = _GetCaptionButtonAt(lParam, out role);
            if (element == null)
            {
                return IntPtr.Zero;
            }

            // Swallowed: the default handling would act on the window itself (and track the press in the frame).
            _SetHoveredCaptionButton(element);
            _SetPressedCaptionButton(element);
            handled = true;
            return IntPtr.Zero;
        }

        private IntPtr _HandleNCLButtonUp(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            handled = false;
            if (!_ManagesNonClientArea || !_IsCaptionButtonHit((HT)wParam.ToInt32()))
            {
                return IntPtr.Zero;
            }

            CaptionButtonRole role;
            DependencyObject element = _GetCaptionButtonAt(lParam, out role);
            DependencyObject pressed = _pressedCaptionButton;
            _SetPressedCaptionButton(null);

            if (element == null)
            {
                return IntPtr.Zero;
            }

            handled = true;
            if (ReferenceEquals(element, pressed))
            {
                _InvokeCaptionButton(element, role);
            }
            return IntPtr.Zero;
        }

        #endregion

        private IntPtr _HandleNCMouseLeave(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            _SetHoveredCaptionButton(null);

            // Let DWM clear the hover state of its caption buttons, otherwise a button may stay highlighted.
            if (_ManagesNonClientArea && _AreDwmCaptionButtonsInUse)
            {
                IntPtr lRet;
                handled = NativeMethods.DwmDefWindowProc(_hwnd, uMsg, wParam, lParam, out lRet);
                return lRet;
            }

            handled = false;
            return IntPtr.Zero;
        }

        private IntPtr _HandleActivate(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            const int WA_INACTIVE = 0;

            _isActive = Utility.LOWORD(wParam.ToInt32()) != WA_INACTIVE;

            _UpdateCaptionForeground();

            handled = false;
            return IntPtr.Zero;
        }

        private IntPtr _HandleDpiChanged(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            // The frame insets are expressed in device pixels. This hook runs before HwndTarget resizes the
            // window for the new DPI, so rescaling here makes the WM_NCCALCSIZE of that resize use the new values.
            if (_IsExtendedMode)
            {
                // Only a frame scaled per monitor (V2) changes thickness with the DPI; with the V1 context the
                // content is rescaled but the frame, and so the insets, stay at the system DPI.
                if (_IsNonClientScaledPerMonitor())
                {
                    _RescaleFrameInsets(Utility.LOWORD(wParam.ToInt32()));
                }

                // The DWM frame extension is expressed in device pixels too: re-extend it once the window has
                // been resized for the new DPI, otherwise the band hosting the caption buttons keeps the old height.
                if (_isExtendedFrameActive && !_reextendFramePending)
                {
                    _reextendFramePending = true;
                    _window.Dispatcher.BeginInvoke(DispatcherPriority.Render, (_Action)_ReextendFrameAfterDpiChange);
                }

                _PostUpdateCaptionButtonsBounds();
            }

            handled = false;
            return IntPtr.Zero;
        }

        private bool _reextendFramePending;

        private void _ReextendFrameAfterDpiChange()
        {
            _reextendFramePending = false;
            if (_IsHwndAlive && _IsExtendedMode && _isExtendedFrameActive && _chromeInfo != null)
            {
                _ExtendFrameForExtendedClientArea();
                _captionAdorner?.InvalidateVisual();

                // The system reworks the frame after the resize WPF performs for the new DPI (see
                // _RepaintAfterFrameRebuild).
                _RepaintAfterFrameRebuild();
            }
        }

        private IntPtr _HandleEnvironmentChanged(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            // Theme, system colors, DWM colorization or settings changed. The frame insets are deliberately NOT
            // measured again here: the window already has its customized frame, so DWMWA_EXTENDED_FRAME_BOUNDS
            // would describe that instead of the standard frame and the client area would drift. The border
            // thickness does not depend on colors, and DPI changes are handled by rescaling. A theme change is
            // the exception: the new theme may draw visible borders instead of the invisible ones (or the other
            // way round), so the insets are measured again, with the standard frame put back for one pass.
            // Coalesce the work and run it after SystemParameters / SystemColors have been invalidated by the framework.
            if (uMsg == WM.THEMECHANGED)
            {
                _remeasureInsetsPending = true;
            }

            if (!_frameStateUpdatePending)
            {
                _frameStateUpdatePending = true;
                _window.Dispatcher.BeginInvoke(DispatcherPriority.Background, (_Action)_OnEnvironmentChanged);
            }

            handled = false;
            return IntPtr.Zero;
        }

        private void _OnEnvironmentChanged()
        {
            _frameStateUpdatePending = false;

            if (!_IsHwndAlive || _chromeInfo == null)
            {
                return;
            }

            // Only what depends on colors and theme is refreshed. The frame is re-applied solely when the
            // composition state actually changed: forcing NCRENDERING_POLICY, the DWM extension and a
            // SWP_FRAMECHANGED on every color/settings notification made DWM chain frame transitions, which
            // showed as flicker until the next real resize.
            if (_ManagesNonClientArea)
            {
                bool remeasure = _remeasureInsetsPending;
                _remeasureInsetsPending = false;
                if (remeasure && _IsExtendedMode && _isExtendedFrameActive && _hasNormalFrameInsets)
                {
                    // The theme changed: the frame may now have visible borders instead of the invisible one
                    // (or vice versa), and the insets measured for the other kind are meaningless.
                    _hasNormalFrameInsets = false;
                    _UpdateExtendedFrameState(NativeMethods.DwmIsCompositionEnabled(), force: true, remeasureWithStandardFrame: true);
                }

                _UpdateFrameState(false);
                _ApplyWindowThemeAttributes();
            }

            _UpdateCaptionForeground();
            _captionAdorner?.InvalidateVisual();
            _UpdateDwmAttributes(WindowChromeDwmAttributes.CaptionTheme | WindowChromeDwmAttributes.Backdrop);

            if (_IsExtendedMode && _isExtendedFrameActive)
            {
                _RepaintAfterFrameRebuild();
            }
        }

        /// <summary>
        /// Requests a full repaint of the client area. After a colorization or theme change (and after the
        /// resize for a new DPI) the system rebuilds the frame with a series of frame-changing SetWindowPos
        /// calls that leave the window and client rectangles untouched; DWM then composes the last frame the
        /// application presented at the window origin, shifted left by the invisible border, until a fresh
        /// frame is presented. The paint request makes the composition target present one.
        /// </summary>
        private void _RepaintAfterFrameRebuild()
        {
            if (!_IsHwndAlive || NativeMethods.GetWindowPlacement(_hwnd).showCmd == SW.SHOWMINIMIZED)
            {
                return;
            }

            NativeMethods.InvalidateRect(_hwnd, IntPtr.Zero, true);
        }

        /// <summary>
        /// Asks the system to recompute the frame (SWP_FRAMECHANGED, no size or move) and then requests a fresh
        /// frame from the application. The second step is not optional: with the client inset from the window
        /// rect, DWM composes the last presented frame at the window origin (content shifted left by the inset)
        /// until a new one is presented, which is what happens when a chrome property changes with the window
        /// otherwise idle. The only frame change without it is the transient standard-frame pass used to measure.
        /// </summary>
        private void _ChangeFrame()
        {
            NativeMethods.SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0, _SwpFlags);
            _RepaintAfterFrameRebuild();
        }

        private IntPtr _HandleSize(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            const int SIZE_RESTORED = 0;
            const int SIZE_MAXIMIZED = 2;
            const int MaxProvisionalRemeasureAttempts = 3;

            if (_ManagesNonClientArea && !_IsExtendedMode)
            {
                // Force when maximized.
                // We can tell what's happening right now, but the Window doesn't yet know it's
                // maximized. Not forcing this update will eventually cause the
                // default caption to be drawn.
                WindowState? state = null;
                if (wParam.ToInt32() == SIZE_MAXIMIZED)
                {
                    state = WindowState.Maximized;
                }
                _UpdateSystemMenu(state);
            }

            if (_IsExtendedMode)
            {
                // WM_SIZE already carries the final client size: refresh synchronously (see _HandleWindowPosChangedExtended).
                _UpdateContentTopOffset();
                _UpdateCaptionButtonsBounds();
                _PostUpdateCaptionButtonsBounds();
                _captionAdorner?.InvalidateVisual();

                // Back in the Normal state with insets taken from the metrics: measure them for real once this
                // SetWindowPos has completed (the rectangles are stale while it runs).
                if (wParam.ToInt32() == SIZE_RESTORED
                    && _isExtendedFrameActive && _hasNormalFrameInsets && _frameInsetsProvisional
                    && !_remeasureProvisionalPending && _provisionalRemeasureAttempts < MaxProvisionalRemeasureAttempts)
                {
                    _remeasureProvisionalPending = true;
                    _window.Dispatcher.BeginInvoke(DispatcherPriority.Render, (_Action)_RemeasureProvisionalInsets);
                }
            }

            // Still let the default WndProc handle this.
            handled = false;
            return IntPtr.Zero;
        }

        private IntPtr _HandleWindowPosChanged(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            // http://blogs.msdn.com/oldnewthing/archive/2008/01/15/7113860.aspx
            // The WM_WINDOWPOSCHANGED message is sent at the end of the window
            // state change process. It sort of combines the other state change
            // notifications, WM_MOVE, WM_SIZE, and WM_SHOWWINDOW. But it doesn't
            // suffer from the same limitations as WM_SHOWWINDOW, so you can
            // reliably use it to react to the window being shown or hidden.

            Assert.IsNotDefault(lParam);
            var wp = Marshal.PtrToStructure<WINDOWPOS>(lParam);

            if (!_ManagesNonClientArea)
            {
                handled = false;
                return IntPtr.Zero;
            }

            if (_IsExtendedMode)
            {
                _HandleWindowPosChangedExtended(wp);
                handled = false;
                return IntPtr.Zero;
            }

            // We only care to take action when the window dimensions are changing.
            // Otherwise, we may get a StackOverflowException.
            if (!Utility.IsFlagSet(wp.flags, (int)SWP.NOSIZE))
            {
                _UpdateSystemMenu(null);

                if (!_isGlassEnabled)
                {
                    _SetRoundingRegion(wp);
                }

                _PostUpdateCaptionButtonsBounds();
            }

            // Still want to pass this to DefWndProc
            handled = false;
            return IntPtr.Zero;
        }

        private void _HandleWindowPosChangedExtended(WINDOWPOS wp)
        {
            if (!_isExtendedFrameActive)
            {
                return;
            }

            // The insets are never measured here: after our WM_NCCALCSIZE the DWM frame bounds describe the
            // customized window and the measurement would be meaningless. DPI changes are handled by rescaling
            // (WM_DPICHANGED), theme changes by re-measuring when the system recomputes the frame.
            // The caption buttons rect is refreshed on moves as well: a system-DPI-aware window that crosses to a
            // monitor with another scale keeps its size but DWM reports the buttons in the new physical pixels.
            if (!Utility.IsFlagSet(wp.flags, (int)SWP.NOSIZE)
                || !Utility.IsFlagSet(wp.flags, (int)SWP.NOMOVE)
                || Utility.IsFlagSet(wp.flags, (int)SWP.SHOWWINDOW))
            {
                // Synchronously first: the client size is already final here and the rect only depends on it and
                // on the button size, so consumers reading CaptionButtonsBounds from SizeChanged (which WPF raises
                // later, during layout) see the new value. The deferred pass covers the cases where DWM itself
                // updates the button bounds a little later (DPI changes).
                _UpdateCaptionButtonsBounds();
                _PostUpdateCaptionButtonsBounds();
            }
        }

        private IntPtr _HandleDwmCompositionChanged(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            if (_ManagesNonClientArea)
            {
                _UpdateFrameState(false);
            }

            _UpdateBackdrop();

            handled = false;
            return IntPtr.Zero;
        }

        #endregion

        /// <summary>Add and remove a native WindowStyle from the HWND.</summary>
        /// <param name="removeStyle">The styles to be removed. These can be bitwise combined.</param>
        /// <param name="addStyle">The styles to be added. These can be bitwise combined.</param>
        /// <returns>Whether the styles of the HWND were modified as a result of this call.</returns>
        private bool _ModifyStyle(WS removeStyle, WS addStyle)
        {
            Assert.IsNotDefault(_hwnd);
            var dwStyle = (WS)NativeMethods.GetWindowLongPtr(_hwnd, GWL.STYLE).ToInt32();
            var dwNewStyle = (dwStyle & ~removeStyle) | addStyle;
            if (dwStyle == dwNewStyle)
            {
                return false;
            }

            NativeMethods.SetWindowLongPtr(_hwnd, GWL.STYLE, new IntPtr((int)dwNewStyle));
            return true;
        }


        /// <summary>
        /// Get the WindowState as the native HWND knows it to be. This isn't necessarily the same as what Window thinks.
        /// </summary>
        private WindowState _GetHwndState()
        {
            var wpl = NativeMethods.GetWindowPlacement(_hwnd);
            switch (wpl.showCmd)
            {
                case SW.SHOWMINIMIZED: return WindowState.Minimized;
                case SW.SHOWMAXIMIZED: return WindowState.Maximized;
            }
            return WindowState.Normal;
        }

        /// <summary>
        /// Get the bounding rectangle for the window in physical coordinates.
        /// </summary>
        /// <returns>The bounding rectangle for the window.</returns>
        private Rect _GetWindowRect()
        {
            // Get the window rectangle.
            RECT windowPosition = NativeMethods.GetWindowRect(_hwnd);
            return new Rect(windowPosition.Left, windowPosition.Top, windowPosition.Width, windowPosition.Height);
        }

        /// <summary>
        /// Update the items in the system menu based on the current, or assumed, WindowState.
        /// </summary>
        /// <param name="assumeState">
        /// The state to assume that the Window is in. This can be null to query the Window's state.
        /// </param>
        /// <remarks>
        /// We want to update the menu while we have some control over whether the caption will be repainted.
        /// </remarks>
        private void _UpdateSystemMenu(WindowState? assumeState)
        {
            const MF mfEnabled = MF.ENABLED | MF.BYCOMMAND;
            const MF mfDisabled = MF.GRAYED | MF.DISABLED | MF.BYCOMMAND;

            WindowState state = assumeState ?? _GetHwndState();

            if (null != assumeState || _lastMenuState != state)
            {
                _lastMenuState = state;

                bool modified = _ModifyStyle(WS.VISIBLE, 0);
                IntPtr hmenu = NativeMethods.GetSystemMenu(_hwnd, false);
                if (IntPtr.Zero != hmenu)
                {
                    var dwStyle = (WS)NativeMethods.GetWindowLongPtr(_hwnd, GWL.STYLE).ToInt32();

                    bool canMinimize = Utility.IsFlagSet((int)dwStyle, (int)WS.MINIMIZEBOX);
                    bool canMaximize = Utility.IsFlagSet((int)dwStyle, (int)WS.MAXIMIZEBOX);
                    bool canSize = Utility.IsFlagSet((int)dwStyle, (int)WS.THICKFRAME);

                    switch (state)
                    {
                        case WindowState.Maximized:
                            NativeMethods.EnableMenuItem(hmenu, SC.RESTORE, mfEnabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MOVE, mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.SIZE, mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MINIMIZE, canMinimize ? mfEnabled : mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MAXIMIZE, mfDisabled);
                            break;
                        case WindowState.Minimized:
                            NativeMethods.EnableMenuItem(hmenu, SC.RESTORE, mfEnabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MOVE, mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.SIZE, mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MINIMIZE, mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MAXIMIZE, canMaximize ? mfEnabled : mfDisabled);
                            break;
                        default:
                            NativeMethods.EnableMenuItem(hmenu, SC.RESTORE, mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MOVE, mfEnabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.SIZE, canSize ? mfEnabled : mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MINIMIZE, canMinimize ? mfEnabled : mfDisabled);
                            NativeMethods.EnableMenuItem(hmenu, SC.MAXIMIZE, canMaximize ? mfEnabled : mfDisabled);
                            break;
                    }
                }

                if (modified)
                {
                    _ModifyStyle(0, WS.VISIBLE);
                }
            }
        }

        private void _UpdateFrameState(bool force)
        {
            if (!_IsHwndAlive || _chromeInfo == null)
            {
                return;
            }

            // Don't rely on SystemParameters for this, just make the check ourselves.
            bool frameState = NativeMethods.DwmIsCompositionEnabled();

            WindowChromeFrameMode mode = _FrameMode;
            bool modeChanged = _appliedFrameMode.HasValue && mode != _appliedFrameMode.Value;
            if (modeChanged)
            {
                _RestoreModeSpecificState(_appliedFrameMode.Value);
                force = true;
            }
            _appliedFrameMode = mode;

            switch (mode)
            {
                case WindowChromeFrameMode.ExtendedClientArea:
                    _UpdateExtendedFrameState(frameState, force, remeasureWithStandardFrame: modeChanged);
                    break;

                case WindowChromeFrameMode.SystemFrame:
                    _UpdateSystemFrameState(frameState);
                    break;

                default:
                    if (force || frameState != _isGlassEnabled)
                    {
                        // A backdrop needs the sheet-of-glass extension even when no glass thickness was asked for.
                        _isGlassEnabled = frameState && (_chromeInfo.GlassFrameThickness != default(Thickness) || _isBackdropApplied);

                        if (!_isGlassEnabled)
                        {
                            if (frameState && _savedBackgroundColor.HasValue)
                            {
                                // A backdrop extension was applied earlier and is no longer wanted: undo it.
                                _RestoreGlassFrameCore();
                            }

                            _SetRoundingRegion(null);
                        }
                        else
                        {
                            _ClearRoundingRegion();
                            _ExtendGlassFrame();
                        }

                        _ChangeFrame();
                    }
                    break;
            }

            if (modeChanged)
            {
                _FixupTemplateIssues();
            }

            _ApplyWindowThemeAttributes();
            _UpdateCaptionForeground();
            _EnsureCaptionAdorner();
            _UpdateContentTopOffset();
            _PostUpdateCaptionButtonsBounds();
        }

        private void _UpdateExtendedFrameState(bool compositionEnabled, bool force, bool remeasureWithStandardFrame)
        {
            if (!force && compositionEnabled == _isExtendedFrameActive)
            {
                return;
            }

            _isGlassEnabled = false;
            _isExtendedFrameActive = compositionEnabled;
            _ClearRoundingRegion();

            if (_isExtendedFrameActive)
            {
                // Keep DWM rendering the frame (and the caption buttons) even though the client area covers it.
                NativeMethods.DwmSetWindowAttributeNCRenderingPolicy(_hwnd, DWMNCRP.ENABLED);
                _SetCompositionBackground(Colors.Transparent);
                _ExtendFrameForExtendedClientArea();

                if (remeasureWithStandardFrame)
                {
                    // Coming from another mode the client area is already customized: the standard frame must be
                    // put back for one pass before DWMWA_EXTENDED_FRAME_BOUNDS can be trusted.
                    _hasNormalFrameInsets = false;
                    _MeasureWithStandardFrame();
                }
                else if (!_hasNormalFrameInsets && !_MeasureNormalFrameInsets() && _IsHwndMaximized())
                {
                    // First application: the window still has its standard frame, unless it was shown maximized,
                    // in which case the metrics stand in until the window is restored.
                    _SynthesizeFrameInsets();
                }
            }
            else
            {
                // No composition (never on a supported OS): fall back to the standard frame, not to a
                // frameless window, so the title bar stays usable.
                _hasNormalFrameInsets = false;
                _RestoreCompositionBackground();
                var dwmMargin = new MARGINS();
                NativeMethods.DwmExtendFrameIntoClientArea(_hwnd, ref dwmMargin);
            }

            _ChangeFrame();
        }

        private void _UpdateSystemFrameState(bool compositionEnabled)
        {
            _isGlassEnabled = false;
            _isExtendedFrameActive = false;

            // The only frame work in this mode is the frame extension: the sheet of glass required by a backdrop,
            // or the margins the application asked for through GlassFrameThickness, with or without a backdrop
            // (top = CaptionHeight when set, otherwise the system caption band). A window that never had one is
            // left completely untouched (_savedBackgroundColor tracks whether we did).
            bool extend = compositionEnabled && (_isBackdropApplied || _HasExplicitGlassMargins);
            bool sheet = extend && !_HasExplicitGlassMargins;
            bool extensionChanged = sheet != _systemFrameSheetOfGlassApplied;
            _systemFrameSheetOfGlassApplied = sheet;

            if (extend)
            {
                _SetCompositionBackground(Colors.Transparent);
                MARGINS dwmMargin = sheet
                    ? new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 }
                    : _GetExplicitGlassMargins(defaultTop: _GetSystemCaptionBandDevice());
                NativeMethods.DwmExtendFrameIntoClientArea(_hwnd, ref dwmMargin);

                // The standard frame is in place, so the border can be measured at any time while not maximized;
                // _HandleNCCalcSizeSystemFrame needs it.
                if (!_hasNormalFrameInsets && !_IsHwndMaximized())
                {
                    _MeasureNormalFrameInsets();
                }
            }
            else if (_savedBackgroundColor.HasValue)
            {
                _RestoreCompositionBackground();
                if (compositionEnabled)
                {
                    var dwmMargin = new MARGINS();
                    NativeMethods.DwmExtendFrameIntoClientArea(_hwnd, ref dwmMargin);
                }
            }

            // The maximized client rect depends on the extension (see _HandleNCCalcSizeSystemFrame): recompute it.
            if (extensionChanged && _IsHwndMaximized())
            {
                _ChangeFrame();
            }
        }

        private bool _systemFrameSheetOfGlassApplied;

        /// <summary>
        /// DWM frame extension for ExtendedClientArea: either the whole surface (GlassFrameThickness negative,
        /// i.e. GlassFrameCompleteThickness) or just a band tall enough to keep the caption buttons rendered.
        /// System metrics are acceptable here: they only size the alpha region where DWM paints the buttons,
        /// they never influence the client rectangle.
        /// </summary>
        private void _ExtendFrameForExtendedClientArea()
        {
            MARGINS dwmMargin;
            if (_IsSheetOfGlassExtension)
            {
                dwmMargin = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            }
            else if (_HasExplicitGlassMargins)
            {
                // Backdrop with explicit glass margins: the application decides where the material shows.
                dwmMargin = _GetExplicitGlassMargins(defaultTop: null);
            }
            else
            {
                // The application sized the caption band itself, or the band is the system caption.
                int top = _chromeInfo.IsCaptionHeightSet ? _GetExplicitCaptionHeightDevice() : _GetSystemCaptionBandDevice();
                dwmMargin = new MARGINS { cyTopHeight = top };
            }

            if (!NativeMethods.DwmExtendFrameIntoClientArea(_hwnd, ref dwmMargin))
            {
                _RestoreCompositionBackground();
            }
        }

        /// <summary>
        /// Height (device pixels) of the caption band the system lays out: the standard caption plus the top
        /// frame, or the height of the caption buttons when those are taller.
        /// </summary>
        private int _GetSystemCaptionBandDevice()
        {
            // Metrics at the DPI of the non-client area: with a frame scaled per monitor the plain (system DPI)
            // values would give a band shorter than the caption buttons, leaving a black strip under them.
            uint dpi = _GetNonClientDpi();
            int top = NativeMethods.GetSystemMetricsForDpi(SM.CYCAPTION, dpi)
                      + NativeMethods.GetSystemMetricsForDpi(SM.CYSIZEFRAME, dpi)
                      + NativeMethods.GetSystemMetricsForDpi(SM.CXPADDEDBORDER, dpi);

            RECT buttons;
            if (NativeMethods.DwmGetWindowAttributeRect(_hwnd, DWMWA.CAPTION_BUTTON_BOUNDS, out buttons) && buttons.Height > 0)
            {
                top = Math.Max(top, (int)Math.Ceiling(buttons.Height * _GetCurrentDwmToWindowScale()));
            }

            return top;
        }

        /// <summary>The application's CaptionHeight in device pixels.</summary>
        private int _GetExplicitCaptionHeightDevice()
        {
            DpiScale dpi = _window.GetDpi();
            return (int)Math.Round(DpiHelper.LogicalPixelsToDevice(new Point(0, _chromeInfo.CaptionHeight), dpi.DpiScaleX, dpi.DpiScaleY).Y);
        }

        /// <summary>
        /// The frame extension asked for by an explicit GlassFrameThickness.  The top is the application's
        /// CaptionHeight when set, otherwise <paramref name="defaultTop"/>, or the glass thickness itself when
        /// that is null; the glass thickness wins when it is taller, so the glass can extend beyond the band.
        /// </summary>
        private MARGINS _GetExplicitGlassMargins(int? defaultTop)
        {
            DpiScale dpi = _window.GetDpi();
            Thickness glass = DpiHelper.LogicalThicknessToDevice(_chromeInfo.GlassFrameThickness, dpi.DpiScaleX, dpi.DpiScaleY);

            int glassTop = (int)Math.Round(glass.Top);
            int top;
            if (_chromeInfo.IsCaptionHeightSet)
            {
                top = _GetExplicitCaptionHeightDevice();
            }
            else
            {
                top = defaultTop ?? glassTop;
            }

            top = Math.Max(top, glassTop);

            return new MARGINS
            {
                cxLeftWidth = (int)Math.Round(glass.Left),
                cxRightWidth = (int)Math.Round(glass.Right),
                cyBottomHeight = (int)Math.Round(glass.Bottom),
                cyTopHeight = top,
            };
        }

        /// <summary>Undoes what a previous FrameMode left on the HWND before a new one is applied.</summary>
        private void _RestoreModeSpecificState(WindowChromeFrameMode previousMode)
        {
            switch (previousMode)
            {
                case WindowChromeFrameMode.ExtendedClientArea:
                    _RestoreExtendedFrame();
                    break;

                case WindowChromeFrameMode.Custom:
                    _RestoreFrameworkIssueFixups();
                    _RestoreGlassFrameCore();
                    _ClearRoundingRegion();
                    break;
            }
        }

        private void _RestoreExtendedFrame()
        {
            _RemoveCaptionAdorner();
            _isExtendedFrameActive = false;
            _hasNormalFrameInsets = false;

            if (_IsHwndAlive)
            {
                NativeMethods.DwmSetWindowAttributeNCRenderingPolicy(_hwnd, DWMNCRP.USEWINDOWSTYLE);
                _RestoreGlassFrameCore();
                _RestoreFrameworkIssueFixups();
            }

            _SetCaptionButtonsBounds(default(Rect), default(Size));
            _SetTitleBarBounds(default(Rect));
        }

        #region Caption appearance (WTNCA flags, caption foreground, title adorner, caption buttons bounds)

        /// <summary>
        /// Applies WTNCA_NODRAWCAPTION / WTNCA_NODRAWICON. In ExtendedClientArea mode both are always set,
        /// otherwise they follow ShowTitle / ShowSystemIcon. Nothing is called when both are true and nothing
        /// was applied before, so the default configuration does not touch the window at all.
        /// </summary>
        private void _ApplyWindowThemeAttributes()
        {
            if (!_IsHwndAlive || _chromeInfo == null)
            {
                return;
            }

            const WTNCA mask = WTNCA.NODRAWCAPTION | WTNCA.NODRAWICON;
            WTNCA flags = WTNCA.NODRAWCAPTION | WTNCA.NODRAWICON;

            if (!(_IsExtendedMode && _isExtendedFrameActive))
            {
                flags = 0;
                if (!_chromeInfo.EffectiveShowTitle)
                {
                    flags |= WTNCA.NODRAWCAPTION;
                }
                if (!_chromeInfo.EffectiveShowSystemIcon)
                {
                    // Best effort on a system-drawn caption: the composed frame of Windows 10 and later keeps
                    // drawing the icon regardless of this attribute (the title does go away). The only known way
                    // around it is the dialog frame extended style, which is not applied here on purpose.
                    flags |= WTNCA.NODRAWICON;
                }
            }

            _SetWindowThemeAttributes(flags, mask);
        }

        private void _SetWindowThemeAttributes(WTNCA flags, WTNCA mask)
        {
            if (!_themeAttributesApplied && flags == 0)
            {
                // Default configuration and nothing applied before: do not touch the window.
                return;
            }

            if (_themeAttributesApplied && flags == _appliedThemeAttributes)
            {
                return;
            }

            if (NativeMethods.SetWindowThemeNonClientAttributes(_hwnd, flags, mask))
            {
                _appliedThemeAttributes = flags;
                _themeAttributesApplied = true;
            }
        }

        /// <summary>
        /// Computes the brush matching the caption text color DWM would use for this window and publishes it
        /// through WindowChrome.CaptionForeground.
        /// </summary>
        private void _UpdateCaptionForeground()
        {
            if (_window == null || _chromeInfo == null)
            {
                return;
            }

            Color color;

            if (_chromeInfo.CaptionTextColor.HasValue && Utility.IsOSWindows11OrNewer)
            {
                color = _chromeInfo.CaptionTextColor.Value;
                color.A = 0xFF;
            }
            else if (!Utility.IsOSWindows10OrNewer || SystemParameters.HighContrast)
            {
                // Classic themes and high contrast: the system colors are authoritative.
                color = _isActive ? SystemColors.ActiveCaptionTextColor : SystemColors.InactiveCaptionTextColor;
            }
            else
            {
                // The fill DWM paints behind the caption text, when known: the caption color, or the accent color
                // when the user shows it on title bars. A transparent caption color over a backdrop asks DWM for
                // no fill at all (see ResolveCaptionColor): the material shows through, the accent is not painted,
                // and the text must follow only the light/dark rendering of the caption.
                Color? background = null;
                bool noFill;
                Color? captionColor = ResolveCaptionColor(_chromeInfo.CaptionColor, _isBackdropApplied, out noFill);
                if (captionColor.HasValue && !noFill && Utility.IsOSWindows11OrNewer)
                {
                    Color c = captionColor.Value;
                    c.A = 0xFF;
                    background = c;
                }
                else if (_isActive && !(noFill && Utility.IsOSWindows11OrNewer))
                {
                    Color accent;
                    if (CaptionTextColorHelper.TryGetPrevalentAccentColor(out accent))
                    {
                        background = accent;
                    }
                }

                // The caption is dark only when this window asked DWM for it and DWM accepted: the system theme
                // alone never darkens a Win32 caption, and before Windows 10 1809 DWM cannot draw a dark one at all.
                bool darkMode = _appliedDarkMode ?? false;

                color = _isActive
                    ? CaptionTextColorHelper.GetActiveCaptionText(background, darkMode)
                    : CaptionTextColorHelper.GetInactiveCaptionText(background, darkMode);
            }

            if (_appliedCaptionForegroundColor == color)
            {
                return;
            }

            var brush = new SolidColorBrush(color);
            brush.Freeze();
            _window.SetValue(WindowChrome.CaptionForegroundPropertyKey, brush);
            _appliedCaptionForegroundColor = color;
            _captionAdorner?.InvalidateVisual();
        }

        /// <summary>The caption band in window coordinates (DIPs), used by the title adorner.</summary>
        private Rect _GetCaptionBand()
        {
            if (_chromeInfo == null || !_IsHwndAlive)
            {
                return Rect.Empty;
            }

            // The band DWM lays out (TitleBarBounds, content coordinates) when known, brought back to window
            // coordinates; otherwise the caption height below the top resize strip. Maximized: the top border
            // thickness of the client lies off-screen (see _GetEffectiveFrameInsets), so the band starts below it.
            if (_titleBarBounds.Height > 0)
            {
                // An application-sized band may be taller than the system caption buttons: the icon and the
                // title drawn by the adorner then line up with the buttons, in a strip of their height at the
                // top of the band (TitleBarBounds itself still describes the whole band).
                double bandHeight = _titleBarBounds.Height;
                if (_chromeInfo.IsCaptionHeightSet && _captionButtonsBounds.Height > 0)
                {
                    bandHeight = Math.Min(bandHeight, _captionButtonsBounds.Height - _titleBarBounds.Y);
                }

                return new Rect(_titleBarBounds.X, _titleBarBounds.Y + _GetContentTopOffsetLogical(), _titleBarBounds.Width, Math.Max(0, bandHeight));
            }

            double top = _IsHwndMaximized() ? _GetMaximizedTopOverhangLogical() : _chromeInfo.ResizeBorderThickness.Top;
            double height = _chromeInfo.CaptionHeight;
            double width = _window.ActualWidth;

            Rect buttons = _captionButtonsBounds;
            if (buttons.Width > 0)
            {
                if (_window.FlowDirection == FlowDirection.RightToLeft)
                {
                    return new Rect(buttons.Right, top, Math.Max(0, width - buttons.Right), height);
                }

                width = Math.Min(width, buttons.Left);
            }

            return new Rect(0, top, Math.Max(0, width), height);
        }

        /// <summary>Creates, updates or removes the adorner drawing the title/icon in ExtendedClientArea mode.</summary>
        private void _EnsureCaptionAdorner()
        {
            bool wanted = _IsExtendedMode && _isExtendedFrameActive && _chromeInfo != null
                          && (_chromeInfo.EffectiveShowTitle || _chromeInfo.EffectiveShowSystemIcon);

            if (!wanted)
            {
                _RemoveCaptionAdorner();
                return;
            }

            if (_captionAdorner != null && (_captionAdornedElement == null || !_window.IsAncestorOf(_captionAdornedElement)))
            {
                // The template was replaced: the adorner still hangs from the old tree. Host it again in the new one.
                _RemoveCaptionAdorner();
            }

            if (_captionAdorner == null)
            {
                UIElement host = _FindAdornedElement();
                if (host == null)
                {
                    // No AdornerDecorator in the template yet; _FixupTemplateIssues retries once the tree exists.
                    return;
                }

                AdornerLayer layer = AdornerLayer.GetAdornerLayer(host);
                if (layer == null)
                {
                    return;
                }

                _captionAdorner = new WindowCaptionAdorner(host, _window, _GetCaptionBand);
                _captionAdornedElement = host;
                layer.Add(_captionAdorner);
                _AttachTitleListeners();
            }

            _captionAdorner.ShowTitle = _chromeInfo.EffectiveShowTitle;
            _captionAdorner.ShowIcon = _chromeInfo.EffectiveShowSystemIcon;
            _captionAdorner.InvalidateVisual();
        }

        private void _RemoveCaptionAdorner()
        {
            if (_captionAdorner == null)
            {
                return;
            }

            _DetachTitleListeners();

            AdornerLayer layer = _captionAdornedElement != null ? AdornerLayer.GetAdornerLayer(_captionAdornedElement) : null;
            layer?.Remove(_captionAdorner);

            _captionAdorner = null;
            _captionAdornedElement = null;
        }

        /// <summary>The child of the first AdornerDecorator in the window's template (depth first).</summary>
        private UIElement _FindAdornedElement()
        {
            if (_window == null || VisualTreeHelper.GetChildrenCount(_window) == 0)
            {
                return null;
            }

            var pending = new Stack<DependencyObject>();
            pending.Push(_window);
            while (pending.Count > 0)
            {
                DependencyObject current = pending.Pop();
                int count = VisualTreeHelper.GetChildrenCount(current);
                for (int i = count - 1; i >= 0; i--)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(current, i);
                    if (child is AdornerDecorator decorator)
                    {
                        return decorator.Child;
                    }

                    pending.Push(child);
                }
            }

            return null;
        }

        private void _AttachTitleListeners()
        {
            if (_isTitleListenerAttached)
            {
                return;
            }

            Utility.AddDependencyPropertyChangeListener(_window, Window.TitleProperty, _OnWindowTitleOrIconChanged);
            Utility.AddDependencyPropertyChangeListener(_window, Window.IconProperty, _OnWindowTitleOrIconChanged);
            _window.SizeChanged += _OnWindowSizeChanged;
            _isTitleListenerAttached = true;
        }

        private void _DetachTitleListeners()
        {
            if (!_isTitleListenerAttached)
            {
                return;
            }

            Utility.RemoveDependencyPropertyChangeListener(_window, Window.TitleProperty, _OnWindowTitleOrIconChanged);
            Utility.RemoveDependencyPropertyChangeListener(_window, Window.IconProperty, _OnWindowTitleOrIconChanged);
            _window.SizeChanged -= _OnWindowSizeChanged;
            _isTitleListenerAttached = false;
        }

        private void _OnWindowTitleOrIconChanged(object sender, EventArgs e)
        {
            _captionAdorner?.InvalidateIcon();
        }

        private void _OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _captionAdorner?.InvalidateVisual();
        }

        private void _PostUpdateCaptionButtonsBounds()
        {
            if (_captionButtonsUpdatePending || _window == null || !_ManagesNonClientArea)
            {
                return;
            }

            // DWM updates DWMWA_CAPTION_BUTTON_BOUNDS slightly after the size change; read it at Render priority.
            _captionButtonsUpdatePending = true;
            _window.Dispatcher.BeginInvoke(DispatcherPriority.Render, (_Action)_UpdateCaptionButtonsBounds);
        }

        /// <summary>
        /// Publishes the area covered by the DWM caption buttons through WindowChrome.CaptionButtonsBounds.
        /// Only the size reported by DWM is used: in mixed-DPI configurations the origin is expressed in a
        /// different space than the size, while the buttons are always anchored to the top corner of the client
        /// area (top-right, or top-left in RTL).
        /// </summary>
        private void _UpdateCaptionButtonsBounds()
        {
            _captionButtonsUpdatePending = false;

            Rect value = default(Rect);
            Rect titleBar = default(Rect);
            Size clipClientSize = default(Size);

            if (_IsHwndAlive && _hwndSource.CompositionTarget != null && _AreDwmCaptionButtonsInUse)
            {
                RECT buttons;
                if (NativeMethods.DwmGetWindowAttributeRect(_hwnd, DWMWA.CAPTION_BUTTON_BOUNDS, out buttons)
                    && buttons.Width > 0 && buttons.Height > 0)
                {
                    RECT client = NativeMethods.GetClientRect(_hwnd);
                    Matrix fromDevice = _hwndSource.CompositionTarget.TransformFromDevice;

                    // Evaluated now, not cached: the window may have moved to a monitor with a different scale.
                    double scale = _GetCurrentDwmToWindowScale();
                    Vector size = fromDevice.Transform(new Vector(buttons.Width * scale, buttons.Height * scale));
                    Vector clientSize = fromDevice.Transform(new Vector(client.Width, client.Height));

                    if (size.X > 0 && size.Y > 0 && clientSize.X > 0)
                    {
                        double width = Math.Min(size.X, clientSize.X);
                        bool rtl = _window.FlowDirection == FlowDirection.RightToLeft;
                        double x = rtl ? 0 : clientSize.X - width;
                        // Maximized: with a top-band extension DWM anchors the buttons to the client top (partly
                        // off-screen, like the content); with a sheet of glass it anchors them to the visible frame,
                        // one border down, and the content is pushed down by the same amount (see
                        // _GetContentTopOffsetLogical). In the coordinates of the content the buttons are thus
                        // always at the top.
                        value = new Rect(x, 0, width, size.Y);
                        clipClientSize = new Size(clientSize.X, Math.Max(0, clientSize.Y));

                        // The rest of the caption band, as tall as the buttons, is free for application content.
                        // Maximized with a top-band extension the first border of the band lies off-screen (the
                        // buttons are cut off as well): only the visible part is offered.
                        double titleWidth = Math.Max(0, clientSize.X - width);
                        double titleTop = _IsSheetOfGlassExtension ? 0 : _GetMaximizedTopOverhangLogical();
                        // The band is as tall as the caption buttons unless the application sized it itself.
                        double bandHeight = _chromeInfo.IsCaptionHeightSet ? _chromeInfo.CaptionHeight : size.Y;
                        titleBar = new Rect(rtl ? width : 0, titleTop, titleWidth, Math.Max(0, bandHeight - titleTop));
                    }
                }
            }

            _SetCaptionButtonsBounds(value, clipClientSize);
            _SetTitleBarBounds(titleBar);
        }

        private void _SetCaptionButtonsBounds(Rect value, Size clipClientSize)
        {
            if (_window == null || (value == _captionButtonsBounds && clipClientSize == _captionButtonsClipClientSize))
            {
                return;
            }

            _captionButtonsBounds = value;
            _captionButtonsClipClientSize = clipClientSize;
            _window.SetValue(WindowChrome.CaptionButtonsBoundsPropertyKey, value);
            _window.SetValue(WindowChrome.CaptionButtonsClipPropertyKey, _CreateCaptionButtonsClip(value, clipClientSize));
            _captionAdorner?.InvalidateVisual();
        }

        /// <summary>
        /// The client area minus the caption buttons, for the Clip of an element filling the client area. A
        /// frozen geometry, recreated only when the buttons or the client size change. Null when there is
        /// nothing to clip.
        /// </summary>
        private static Geometry _CreateCaptionButtonsClip(Rect buttons, Size clientSize)
        {
            if (buttons.Width <= 0 || buttons.Height <= 0 || clientSize.Width <= 0 || clientSize.Height <= 0)
            {
                return null;
            }

            var clip = new CombinedGeometry(
                GeometryCombineMode.Exclude,
                new RectangleGeometry(new Rect(clientSize)),
                new RectangleGeometry(buttons));
            clip.Freeze();
            return clip;
        }

        private void _SetTitleBarBounds(Rect value)
        {
            if (_window == null || value == _titleBarBounds)
            {
                return;
            }

            _titleBarBounds = value;
            _window.SetValue(WindowChrome.TitleBarBoundsPropertyKey, value);
        }

        #endregion

        #region DWM attributes and backdrop

        /// <summary>Pushes the DWM attributes selected by <paramref name="which"/> to the HWND, skipping unchanged ones.</summary>
        private void _UpdateDwmAttributes(WindowChromeDwmAttributes which)
        {
            if (!_IsHwndAlive)
            {
                return;
            }

            if ((which & WindowChromeDwmAttributes.CaptionColor) != 0)
            {
                bool allowNone;
                Color? captionColor = ResolveCaptionColor(_chromeInfo?.CaptionColor, _isBackdropApplied, out allowNone);
                _UpdateColorAttribute(captionColor, ref _appliedCaptionColor, allowNone, NativeMethods.DwmSetWindowAttributeCaptionColor);
                _UpdateCaptionForeground();
            }

            if ((which & WindowChromeDwmAttributes.CaptionTextColor) != 0)
            {
                _UpdateColorAttribute(_chromeInfo?.CaptionTextColor, ref _appliedTextColor, false, NativeMethods.DwmSetWindowAttributeTextColor);
                _UpdateCaptionForeground();
            }

            if ((which & WindowChromeDwmAttributes.BorderColor) != 0)
            {
                _UpdateColorAttribute(_chromeInfo?.BorderColor, ref _appliedBorderColor, true, NativeMethods.DwmSetWindowAttributeBorderColor);
            }

            if ((which & WindowChromeDwmAttributes.CornerPreference) != 0)
            {
                _UpdateCornerPreference();
            }

            if ((which & WindowChromeDwmAttributes.CaptionTheme) != 0)
            {
                _UpdateImmersiveDarkMode();
                _UpdateCaptionForeground();
            }

            if ((which & WindowChromeDwmAttributes.Backdrop) != 0)
            {
                _UpdateBackdrop();
            }
        }

        /// <summary>
        /// The caption color to hand to DWM. A fully transparent color asks DWM not to paint the caption
        /// fill at all so that the system backdrop shows through the whole title bar, as the system's own
        /// backdrop windows do; that only makes sense while a backdrop is applied, otherwise the request is
        /// treated as "not set" and the system default (accent or theme color) stays.
        /// </summary>
        internal static Color? ResolveCaptionColor(Color? requested, bool backdropApplied, out bool allowNone)
        {
            allowNone = false;
            if (requested.HasValue && requested.Value.A == 0)
            {
                if (!backdropApplied)
                {
                    return null;
                }

                allowNone = true;
            }

            return requested;
        }

        private void _UpdateColorAttribute(Color? requested, ref uint? applied, bool allowNone, Func<IntPtr, uint, bool> setter)
        {
            if (!Utility.IsOSWindows11OrNewer)
            {
                return;
            }

            uint? target;
            if (requested.HasValue)
            {
                target = allowNone && requested.Value.A == 0
                    ? NativeMethods.DWMWA_COLOR_NONE
                    : Utility.ColorRefFromColor(requested.Value);
            }
            else if (applied.HasValue)
            {
                // Something was applied before: explicitly go back to the system default.
                target = NativeMethods.DWMWA_COLOR_DEFAULT;
            }
            else
            {
                // Never touched and nothing requested: leave DWM alone.
                return;
            }

            if (applied == target)
            {
                return;
            }

            if (setter(_hwnd, target.Value))
            {
                applied = target;
            }
        }

        private void _UpdateCornerPreference()
        {
            if (!Utility.IsOSWindows11OrNewer)
            {
                return;
            }

            var requested = _chromeInfo != null ? _chromeInfo.CornerPreference : WindowCornerPreference.Default;
            var target = (DWMWCP)(int)requested;

            if (target == DWMWCP.DEFAULT && !_appliedCornerPreference.HasValue)
            {
                return;
            }

            if (_appliedCornerPreference == target)
            {
                return;
            }

            if (NativeMethods.DwmSetWindowAttributeWindowCornerPreference(_hwnd, target))
            {
                _appliedCornerPreference = target;
            }
        }

        /// <summary>
        /// The dark/light decision: CaptionTheme when explicit, the ThemeManager state otherwise,
        /// null when nobody decided (DWM is then left untouched).
        /// </summary>
        private bool? _GetEffectiveDarkMode()
        {
            switch (_chromeInfo != null ? _chromeInfo.CaptionTheme : WindowCaptionTheme.Auto)
            {
                case WindowCaptionTheme.Dark:
                    return true;
                case WindowCaptionTheme.Light:
                    return false;
                default:
                    return _themeUseLightColors.HasValue ? !_themeUseLightColors.Value : (bool?)null;
            }
        }

        private void _UpdateImmersiveDarkMode()
        {
            bool? dark = _GetEffectiveDarkMode();
            if (!dark.HasValue || dark == _appliedDarkMode)
            {
                return;
            }

            if (NativeMethods.DwmSetWindowAttributeUseImmersiveDarkMode(_hwnd, dark.Value))
            {
                _appliedDarkMode = dark;
            }
        }

        /// <summary>
        /// The backdrop to use: the explicit BackdropType, otherwise the theme's choice. A legacy Custom chrome
        /// never gets a theme backdrop: its template paints an opaque surface anyway.
        /// </summary>
        private WindowBackdropKind _GetEffectiveBackdropKind()
        {
            WindowBackdropKind requested = _chromeInfo != null ? _chromeInfo.BackdropType : WindowBackdropKind.Auto;
            if (requested != WindowBackdropKind.Auto)
            {
                return requested;
            }

            if (_FrameMode == WindowChromeFrameMode.Custom)
            {
                return WindowBackdropKind.None;
            }

            return _themeBackdropKind ?? WindowBackdropKind.None;
        }

        private bool _IsBackdropPossible(WindowBackdropKind kind)
        {
            return kind != WindowBackdropKind.None
                   && kind != WindowBackdropKind.Auto
                   && Utility.IsWindows11_22H2OrNewer
                   && _window != null
                   && !_window.AllowsTransparency
                   && !MS.Internal.FrameworkAppContextSwitches.DisableFluentThemeWindowBackdrop
                   && !SystemParameters.HighContrast
                   && NativeMethods.DwmIsCompositionEnabled();
        }

        private void _UpdateBackdrop()
        {
            if (!_IsHwndAlive || _chromeInfo == null)
            {
                return;
            }

            // DWM itself takes care of the window state: inactive, maximized and minimized windows get the
            // appropriate treatment for every material, so the backdrop is simply kept while it is possible.
            WindowBackdropKind kind = _GetEffectiveBackdropKind();
            bool apply = _IsBackdropPossible(kind);
            DWMSBT target = apply ? (DWMSBT)(uint)kind : DWMSBT.DWMSBT_NONE;

            if (apply != _isBackdropApplied)
            {
                // The frame extension and the transparent composition background are owned by _UpdateFrameState.
                _isBackdropApplied = apply;
                _UpdateFrameState(true);

                // A transparent CaptionColor means "no caption fill" only over a backdrop (see ResolveCaptionColor).
                _UpdateDwmAttributes(WindowChromeDwmAttributes.CaptionColor);
            }

            if (target == _appliedBackdrop)
            {
                return;
            }

            if (target == DWMSBT.DWMSBT_NONE && _appliedBackdrop == DWMSBT.DWMSBT_AUTO)
            {
                // Never applied anything: no need to tell DWM.
                return;
            }

            if (NativeMethods.DwmSetWindowAttributeSystemBackdropType(_hwnd, target).Succeeded)
            {
                _appliedBackdrop = target;
            }
        }

        /// <summary>Sets the composition background, remembering the original value the first time.</summary>
        private void _SetCompositionBackground(Color color)
        {
            HwndTarget target = _hwndSource?.CompositionTarget;
            if (target == null)
            {
                return;
            }

            if (!_savedBackgroundColor.HasValue)
            {
                _savedBackgroundColor = target.BackgroundColor;
            }

            target.BackgroundColor = color;
        }

        /// <summary>
        /// Restores the composition background. The value saved by <see cref="_SetCompositionBackground"/> is
        /// used when the application had set one; the HwndTarget default (opaque black) and a transparent color on
        /// a non-layered window fall back to SystemColors.WindowColor, as the legacy code always did.
        /// </summary>
        private void _RestoreCompositionBackground()
        {
            HwndTarget target = _hwndSource?.CompositionTarget;
            if (target == null)
            {
                return;
            }

            if (_savedBackgroundColor.HasValue && target.BackgroundColor != Colors.Transparent)
            {
                // The application set a background of its own while the frame was extended: that is the value it
                // wants, not the one saved before the extension.
                _savedBackgroundColor = null;
                return;
            }

            Color restore = SystemColors.WindowColor;
            if (_savedBackgroundColor.HasValue)
            {
                Color saved = _savedBackgroundColor.Value;
                bool isHwndTargetDefault = saved == Color.FromRgb(0, 0, 0);
                bool isTransparentOnOpaqueWindow = saved == Colors.Transparent && _window != null && !_window.AllowsTransparency;
                if (!isHwndTargetDefault && !isTransparentOnOpaqueWindow)
                {
                    restore = saved;
                }
            }

            target.BackgroundColor = restore;

            // Restored: the next extension saves the background in force at that time.
            _savedBackgroundColor = null;
        }

        /// <summary>Resets every DWM attribute the worker applied, so the window looks like it never had a chrome.</summary>
        private void _RestoreDwmAttributes()
        {
            if (!_IsHwndAlive)
            {
                return;
            }

            if (_appliedCaptionColor.HasValue)
            {
                NativeMethods.DwmSetWindowAttributeCaptionColor(_hwnd, NativeMethods.DWMWA_COLOR_DEFAULT);
                _appliedCaptionColor = null;
            }
            if (_appliedTextColor.HasValue)
            {
                NativeMethods.DwmSetWindowAttributeTextColor(_hwnd, NativeMethods.DWMWA_COLOR_DEFAULT);
                _appliedTextColor = null;
            }
            if (_appliedBorderColor.HasValue)
            {
                NativeMethods.DwmSetWindowAttributeBorderColor(_hwnd, NativeMethods.DWMWA_COLOR_DEFAULT);
                _appliedBorderColor = null;
            }
            if (_appliedCornerPreference.HasValue)
            {
                NativeMethods.DwmSetWindowAttributeWindowCornerPreference(_hwnd, DWMWCP.DEFAULT);
                _appliedCornerPreference = null;
            }
            if (_appliedBackdrop != DWMSBT.DWMSBT_AUTO)
            {
                NativeMethods.DwmSetWindowAttributeSystemBackdropType(_hwnd, DWMSBT.DWMSBT_NONE);
                _appliedBackdrop = DWMSBT.DWMSBT_AUTO;
            }
            _isBackdropApplied = false;

            // The caption theme is left as is: there is no "unset" value for DWMWA_USE_IMMERSIVE_DARK_MODE and
            // the ThemeManager keeps owning it for Fluent windows.

            if (_themeAttributesApplied)
            {
                _SetWindowThemeAttributes(0, WTNCA.NODRAWCAPTION | WTNCA.NODRAWICON);
            }
        }

        #endregion

        private void _ClearRoundingRegion()
        {
            NativeMethods.SetWindowRgn(_hwnd, IntPtr.Zero, NativeMethods.IsWindowVisible(_hwnd));
        }

        private void _SetRoundingRegion(WINDOWPOS? wp)
        {
            const int MONITOR_DEFAULTTONEAREST = 0x00000002;

            // We're early - WPF hasn't necessarily updated the state of the window.
            // Need to query it ourselves.
            WINDOWPLACEMENT wpl = NativeMethods.GetWindowPlacement(_hwnd);

            if (wpl.showCmd == SW.SHOWMAXIMIZED)
            {
                int left;
                int top;

                if (wp.HasValue)
                {
                    left = wp.Value.x;
                    top = wp.Value.y;
                }
                else
                {
                    Rect r = _GetWindowRect();
                    left = (int)r.Left;
                    top = (int)r.Top;
                }

                IntPtr hMon = NativeMethods.MonitorFromWindow(_hwnd, MONITOR_DEFAULTTONEAREST);

                MONITORINFO mi = NativeMethods.GetMonitorInfo(hMon);
                RECT rcMax = mi.rcWork;
                // The location of maximized window takes into account the border that Windows was
                // going to remove, so we also need to consider it.
                rcMax.Offset(-left, -top);

                IntPtr hrgn = IntPtr.Zero;
                try
                {
                    hrgn = NativeMethods.CreateRectRgnIndirect(rcMax);
                    NativeMethods.SetWindowRgn(_hwnd, hrgn, NativeMethods.IsWindowVisible(_hwnd));
                    hrgn = IntPtr.Zero;
                }
                finally
                {
                    Utility.SafeDeleteObject(ref hrgn);
                }
            }
            else
            {
                Size windowSize;

                // Use the size if it's specified.
                if (null != wp && !Utility.IsFlagSet(wp.Value.flags, (int)SWP.NOSIZE))
                {
                    windowSize = new Size((double)wp.Value.cx, (double)wp.Value.cy);
                }
                else if (null != wp && (_lastRoundingState == _window.WindowState))
                {
                    return;
                }
                else
                {
                    windowSize = _GetWindowRect().Size;
                }

                _lastRoundingState = _window.WindowState;

                IntPtr hrgn = IntPtr.Zero;
                try
                {
                    DpiScale dpi = _window.GetDpi();

                    double shortestDimension = Math.Min(windowSize.Width, windowSize.Height);

                    double topLeftRadius = DpiHelper.LogicalPixelsToDevice(new Point(_chromeInfo.CornerRadius.TopLeft, 0), dpi.DpiScaleX, dpi.DpiScaleY).X;
                    topLeftRadius = Math.Min(topLeftRadius, shortestDimension / 2);

                    if (_IsUniform(_chromeInfo.CornerRadius))
                    {
                        // RoundedRect HRGNs require an additional pixel of padding.
                        hrgn = _CreateRoundRectRgn(new Rect(windowSize), topLeftRadius);
                    }
                    else
                    {
                        // We need to combine HRGNs for each of the corners.
                        // Create one for each quadrant, but let it overlap into the two adjacent ones
                        // by the radius amount to ensure that there aren't corners etched into the middle
                        // of the window.
                        hrgn = _CreateRoundRectRgn(new Rect(0, 0, windowSize.Width / 2 + topLeftRadius, windowSize.Height / 2 + topLeftRadius), topLeftRadius);

                        double topRightRadius = DpiHelper.LogicalPixelsToDevice(new Point(_chromeInfo.CornerRadius.TopRight, 0), dpi.DpiScaleX, dpi.DpiScaleY).X;
                        topRightRadius = Math.Min(topRightRadius, shortestDimension / 2);
                        Rect topRightRegionRect = new Rect(0, 0, windowSize.Width / 2 + topRightRadius, windowSize.Height / 2 + topRightRadius);
                        topRightRegionRect.Offset(windowSize.Width / 2 - topRightRadius, 0);
                        Assert.AreEqual(topRightRegionRect.Right, windowSize.Width);

                        _CreateAndCombineRoundRectRgn(hrgn, topRightRegionRect, topRightRadius);

                        double bottomLeftRadius = DpiHelper.LogicalPixelsToDevice(new Point(_chromeInfo.CornerRadius.BottomLeft, 0), dpi.DpiScaleX, dpi.DpiScaleY).X;
                        bottomLeftRadius = Math.Min(bottomLeftRadius, shortestDimension / 2);
                        Rect bottomLeftRegionRect = new Rect(0, 0, windowSize.Width / 2 + bottomLeftRadius, windowSize.Height / 2 + bottomLeftRadius);
                        bottomLeftRegionRect.Offset(0, windowSize.Height / 2 - bottomLeftRadius);
                        Assert.AreEqual(bottomLeftRegionRect.Bottom, windowSize.Height);

                        _CreateAndCombineRoundRectRgn(hrgn, bottomLeftRegionRect, bottomLeftRadius);

                        double bottomRightRadius = DpiHelper.LogicalPixelsToDevice(new Point(_chromeInfo.CornerRadius.BottomRight, 0), dpi.DpiScaleX, dpi.DpiScaleY).X;
                        bottomRightRadius = Math.Min(bottomRightRadius, shortestDimension / 2);
                        Rect bottomRightRegionRect = new Rect(0, 0, windowSize.Width / 2 + bottomRightRadius, windowSize.Height / 2 + bottomRightRadius);
                        bottomRightRegionRect.Offset(windowSize.Width / 2 - bottomRightRadius, windowSize.Height / 2 - bottomRightRadius);
                        Assert.AreEqual(bottomRightRegionRect.Right, windowSize.Width);
                        Assert.AreEqual(bottomRightRegionRect.Bottom, windowSize.Height);

                        _CreateAndCombineRoundRectRgn(hrgn, bottomRightRegionRect, bottomRightRadius);
                    }

                    NativeMethods.SetWindowRgn(_hwnd, hrgn, NativeMethods.IsWindowVisible(_hwnd));
                    hrgn = IntPtr.Zero;
                }
                finally
                {
                    // Free the memory associated with the HRGN if it wasn't assigned to the HWND.
                    Utility.SafeDeleteObject(ref hrgn);
                }
            }
        }

        private static IntPtr _CreateRoundRectRgn(Rect region, double radius)
        {
            // Round outwards.

            if (DoubleUtilities.AreClose(0, radius))
            {
                return NativeMethods.CreateRectRgn(
                    (int)Math.Floor(region.Left),
                    (int)Math.Floor(region.Top),
                    (int)Math.Ceiling(region.Right),
                    (int)Math.Ceiling(region.Bottom));
            }

            // RoundedRect HRGNs require an additional pixel of padding on the bottom right to look correct.
            return NativeMethods.CreateRoundRectRgn(
                (int)Math.Floor(region.Left),
                (int)Math.Floor(region.Top),
                (int)Math.Ceiling(region.Right) + 1,
                (int)Math.Ceiling(region.Bottom) + 1,
                (int)Math.Ceiling(radius),
                (int)Math.Ceiling(radius));
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "HRGNs")]
        private static void _CreateAndCombineRoundRectRgn(IntPtr hrgnSource, Rect region, double radius)
        {
            IntPtr hrgn = IntPtr.Zero;
            try
            {
                hrgn = _CreateRoundRectRgn(region, radius);
                CombineRgnResult result = NativeMethods.CombineRgn(hrgnSource, hrgnSource, hrgn, RGN.OR);
                if (result == CombineRgnResult.ERROR)
                {
                    throw new InvalidOperationException("Unable to combine two HRGNs.");
                }
            }
            catch
            {
                Utility.SafeDeleteObject(ref hrgn);
                throw;
            }
        }

        private static bool _IsUniform(CornerRadius cornerRadius)
        {
            if (!DoubleUtilities.AreClose(cornerRadius.BottomLeft, cornerRadius.BottomRight))
            {
                return false;
            }

            if (!DoubleUtilities.AreClose(cornerRadius.TopLeft, cornerRadius.TopRight))
            {
                return false;
            }

            if (!DoubleUtilities.AreClose(cornerRadius.BottomLeft, cornerRadius.TopRight))
            {
                return false;
            }

            return true;
        }

        private void _ExtendGlassFrame()
        {
            Assert.IsNotNull(_window);

            // Expect that this might be called on OSes other than Vista.
            if (!Utility.IsOSVistaOrNewer)
            {
                // Not an error. Just not on Vista so we're not going to get glass.
                return;
            }

            if (IntPtr.Zero == _hwnd)
            {
                // Can't do anything with this call until the Window has been shown.
                return;
            }

            // Ensure standard HWND background painting when DWM isn't enabled.
            if (!NativeMethods.DwmIsCompositionEnabled())
            {
                _RestoreCompositionBackground();
            }
            else if (_isBackdropApplied)
            {
                // A backdrop needs the whole surface to be "frame" (sheet of glass) and a transparent background.
                _SetCompositionBackground(Colors.Transparent);

                var backdropMargin = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
                if (!NativeMethods.DwmExtendFrameIntoClientArea(_hwnd, ref backdropMargin))
                {
                    _RestoreCompositionBackground();
                }
            }
            else
            {
                DpiScale dpi = _window.GetDpi();

                // This makes the glass visible at a Win32 level so long as nothing else is covering it.
                // The Window's Background needs to be changed independent of this.

                // Apply the transparent background to the HWND
                _SetCompositionBackground(Colors.Transparent);

                // Thickness is going to be DIPs, need to convert to system coordinates.
                Thickness deviceGlassThickness = DpiHelper.LogicalThicknessToDevice(_chromeInfo.GlassFrameThickness, dpi.DpiScaleX, dpi.DpiScaleY);

                if (_chromeInfo.NonClientFrameEdges != NonClientFrameEdges.None)
                {
#if RIBBON_IN_FRAMEWORK
                    Thickness windowResizeBorderThicknessDevice = DpiHelper.LogicalThicknessToDevice(SystemParameters.WindowResizeBorderThickness, dpi.DpiScaleX, dpi.DpiScaleY);
#else
                    Thickness windowResizeBorderThicknessDevice = DpiHelper.LogicalThicknessToDevice(SystemParameters2.Current.WindowResizeBorderThickness, dpi.DpiScaleX, dpi.DpiScaleY);
#endif
                    if (Utility.IsFlagSet((int)_chromeInfo.NonClientFrameEdges, (int)NonClientFrameEdges.Top))
                    {
                        deviceGlassThickness.Top -= windowResizeBorderThicknessDevice.Top;
                        deviceGlassThickness.Top = Math.Max(0, deviceGlassThickness.Top);
                    }
                    if (Utility.IsFlagSet((int)_chromeInfo.NonClientFrameEdges, (int)NonClientFrameEdges.Left))
                    {
                        deviceGlassThickness.Left -= windowResizeBorderThicknessDevice.Left;
                        deviceGlassThickness.Left = Math.Max(0, deviceGlassThickness.Left);
                    }
                    if (Utility.IsFlagSet((int)_chromeInfo.NonClientFrameEdges, (int)NonClientFrameEdges.Bottom))
                    {
                        deviceGlassThickness.Bottom -= windowResizeBorderThicknessDevice.Bottom;
                        deviceGlassThickness.Bottom = Math.Max(0, deviceGlassThickness.Bottom);
                    }
                    if (Utility.IsFlagSet((int)_chromeInfo.NonClientFrameEdges, (int)NonClientFrameEdges.Right))
                    {
                        deviceGlassThickness.Right -= windowResizeBorderThicknessDevice.Right;
                        deviceGlassThickness.Right = Math.Max(0, deviceGlassThickness.Right);
                    }
                }

                var dwmMargin = new MARGINS
                {
                    // err on the side of pushing in glass an extra pixel.
                    cxLeftWidth = (int)Math.Ceiling(deviceGlassThickness.Left),
                    cxRightWidth = (int)Math.Ceiling(deviceGlassThickness.Right),
                    cyTopHeight = (int)Math.Ceiling(deviceGlassThickness.Top),
                    cyBottomHeight = (int)Math.Ceiling(deviceGlassThickness.Bottom),
                };

                bool dwmApiResult = NativeMethods.DwmExtendFrameIntoClientArea(_hwnd, ref dwmMargin);
                if(!dwmApiResult)
                {
                    _RestoreCompositionBackground();
                }
            }
        }

        /// <summary>
        /// Matrix of the HT values to return when responding to NC window messages.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")]
        private static readonly HT[,] _HitTestBorders = new[,]
        {
            { HT.TOPLEFT,    HT.TOP,     HT.TOPRIGHT    },
            { HT.LEFT,       HT.CLIENT,  HT.RIGHT       },
            { HT.BOTTOMLEFT, HT.BOTTOM,  HT.BOTTOMRIGHT },
        };

        /// <summary>
        /// Non-client hit test for ExtendedClientArea, laid out like the standard frame. The zones are tested in
        /// order, as rectangles relative to the window (left/top inclusive, right/bottom exclusive):
        /// top-left corner (twice the border wide), top-right corner (one border wide), top strip and caption
        /// band, left strip, bottom-left and bottom-right corners (twice the border wide), bottom strip, right
        /// strip.  The caption buttons are not excluded here: DWM is asked first and claims them, so a point that
        /// reaches this test lies beside or below them and the caption band must still be draggable there.
        /// All values are logical pixels; the borders are zero when the window is maximized.
        /// </summary>
        private static HT _HitTestNcaExtended(Rect windowPosition, Point mousePosition, Thickness border, double captionHeight)
        {
            double x = mousePosition.X - windowPosition.Left;
            double y = mousePosition.Y - windowPosition.Top;
            double width = windowPosition.Width;
            double height = windowPosition.Height;

            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return HT.NOWHERE;
            }

            double left = border.Left;
            double right = border.Right;
            double top = border.Top;
            double bottom = border.Bottom;

            // Right bound of the top strip and of the caption band.
            double captionRight = width - right;

            if (_IsInRect(x, y, 0, 0, left * 2, top))
            {
                return HT.TOPLEFT;
            }

            if (_IsInRect(x, y, width - right, 0, width, top))
            {
                return HT.TOPRIGHT;
            }

            if (_IsInRect(x, y, left, 0, captionRight, top))
            {
                return HT.TOP;
            }

            if (_IsInRect(x, y, left, top, captionRight, top + captionHeight))
            {
                return HT.CAPTION;
            }

            if (_IsInRect(x, y, 0, top, left, height - bottom))
            {
                return HT.LEFT;
            }

            if (_IsInRect(x, y, 0, height - bottom, left * 2, height))
            {
                return HT.BOTTOMLEFT;
            }

            if (_IsInRect(x, y, width - right * 2, height - bottom, width, height))
            {
                return HT.BOTTOMRIGHT;
            }

            if (_IsInRect(x, y, left, height - bottom, width - left, height))
            {
                return HT.BOTTOM;
            }

            if (_IsInRect(x, y, width - right, top, width, height - bottom))
            {
                return HT.RIGHT;
            }

            return HT.CLIENT;
        }

        private static bool _IsInRect(double x, double y, double left, double top, double right, double bottom)
        {
            return x >= left && x < right && y >= top && y < bottom;
        }

        private HT _HitTestNca(Rect windowPosition, Point mousePosition, Thickness resizeBorderThickness, double captionHeight)
        {
            // Determine if hit test is for resizing, default middle (1,1).
            int uRow = 1;
            int uCol = 1;
            bool onResizeBorder = false;

            // Determine if the point is at the top or bottom of the window.
            if (mousePosition.Y >= windowPosition.Top && mousePosition.Y < windowPosition.Top + resizeBorderThickness.Top + captionHeight)
            {
                onResizeBorder = (mousePosition.Y < (windowPosition.Top + resizeBorderThickness.Top));
                uRow = 0; // top (caption or resize border)
            }
            else if (mousePosition.Y < windowPosition.Bottom && mousePosition.Y >= windowPosition.Bottom - (int)resizeBorderThickness.Bottom)
            {
                uRow = 2; // bottom
            }

            // Determine if the point is at the left or right of the window.
            if (mousePosition.X >= windowPosition.Left && mousePosition.X < windowPosition.Left + (int)resizeBorderThickness.Left)
            {
                uCol = 0; // left side
            }
            else if (mousePosition.X < windowPosition.Right && mousePosition.X >= windowPosition.Right - resizeBorderThickness.Right)
            {
                uCol = 2; // right side
            }

            // If the cursor is in one of the top edges by the caption bar, but below the top resize border,
            // then resize left-right rather than diagonally.
            if (uRow == 0 && uCol != 1 && !onResizeBorder)
            {
                uRow = 1;
            }

            HT ht = _HitTestBorders[uRow, uCol];

            if (ht == HT.TOP && !onResizeBorder)
            {
                ht = HT.CAPTION;
            }

            return ht;
        }

        // Return the effective client area, excluding the invisible caption
        // and resize-border areas.
        // This method is called via private reflection from PresentationCore,
        // method HwndMouseInputProvider.HasCustomChrome. Both places have to
        // agree on the signature.
        private bool GetEffectiveClientArea(ref MS.Win32.NativeMethods.RECT rcClient)
        {
            if (_window == null || _chromeInfo == null || !_ManagesNonClientArea)
                return false;

            DpiScale dpi = _window.GetDpi();
            double captionHeight = _chromeInfo.CaptionHeight;
            Thickness resizeBorderThickness = _chromeInfo.ResizeBorderThickness;

            if (_IsExtendedMode)
            {
                if (!_isExtendedFrameActive || !_IsHwndAlive)
                    return false;

                // The resize borders are outside the client area in this mode; only the caption band (and the
                // top resize border when not maximized) lies inside it.
                RECT rc = NativeMethods.GetClientRect(_hwnd);
                double topLogical = captionHeight + (_IsHwndMaximized() ? _GetMaximizedTopOverhangLogical() : resizeBorderThickness.Top);
                Point deviceTop = DpiHelper.LogicalPixelsToDevice(new Point(0, topLogical), dpi.DpiScaleX, dpi.DpiScaleY);

                rcClient.left = 0;
                rcClient.top = (int)deviceTop.Y;
                rcClient.right = rc.Width;
                rcClient.bottom = rc.Height;
                return true;
            }

            RECT rcWindow = NativeMethods.GetWindowRect(_hwnd);
            Size logicalSize = DpiHelper.DeviceSizeToLogical(new Size(rcWindow.Width, rcWindow.Height), dpi.DpiScaleX, dpi.DpiScaleY);

            Point logicalTopLeft     = new Point(resizeBorderThickness.Left,
                                                 resizeBorderThickness.Top + captionHeight);
            Point logicalBottomRight = new Point(logicalSize.Width - resizeBorderThickness.Right,
                                                 logicalSize.Height - resizeBorderThickness.Bottom);

            Point deviceTopLeft     = DpiHelper.LogicalPixelsToDevice(logicalTopLeft,     dpi.DpiScaleX, dpi.DpiScaleY);
            Point deviceBottomRight = DpiHelper.LogicalPixelsToDevice(logicalBottomRight, dpi.DpiScaleX, dpi.DpiScaleY);

            rcClient.left   = (int)deviceTopLeft.X;
            rcClient.top    = (int)deviceTopLeft.Y;
            rcClient.right  = (int)deviceBottomRight.X;
            rcClient.bottom = (int)deviceBottomRight.Y;

            return true;
        }

        #region Remove Custom Chrome Methods

        private void _RestoreStandardChromeState(bool isClosing)
        {
            VerifyAccess();

            _UnhookCustomChrome();
            _RemoveCaptionAdorner();
            _SetHoveredCaptionButton(null);

            if (!isClosing && _IsHwndAlive && _appliedFrameMode.HasValue)
            {
                switch (_appliedFrameMode.Value)
                {
                    case WindowChromeFrameMode.ExtendedClientArea:
                        _RestoreExtendedFrame();
                        _RestoreHrgn();
                        break;

                    case WindowChromeFrameMode.SystemFrame:
                        _RestoreGlassFrameCore();
                        break;

                    default:
                        _RestoreFrameworkIssueFixups();
                        _RestoreGlassFrame();
                        _RestoreHrgn();
                        break;
                }

                _RestoreDwmAttributes();
                if (_savedBackgroundColor.HasValue)
                {
                    // Still extended (the mode-specific restore above did not undo it): put the background back.
                    _RestoreCompositionBackground();
                }

                _window.ClearValue(WindowChrome.CaptionButtonsBoundsPropertyKey);
                _window.ClearValue(WindowChrome.CaptionButtonsClipPropertyKey);
                _window.ClearValue(WindowChrome.TitleBarBoundsPropertyKey);
                _window.ClearValue(WindowChrome.CaptionForegroundPropertyKey);
                _captionButtonsBounds = default(Rect);
                _captionButtonsClipClientSize = default(Size);
                _titleBarBounds = default(Rect);
                _appliedCaptionForegroundColor = null;
                _appliedFrameMode = null;
                _isExtendedFrameActive = false;
                _hasNormalFrameInsets = false;

                _window.InvalidateMeasure();
            }
        }

        private void _UnhookCustomChrome()
        {
            Assert.IsNotDefault(_hwnd);
            Assert.IsNotNull(_window);

            if (_isHooked)
            {
                _hwndSource.RemoveHook(_WndProc);
                _isHooked = false;
            }
        }

        private void _RestoreFrameworkIssueFixups()
        {
            if (_window == null || VisualTreeHelper.GetChildrenCount(_window) == 0)
            {
                // The template was never applied (e.g. SystemFrame chrome on a window that is not shown yet).
                return;
            }

            FrameworkElement rootElement = (FrameworkElement)VisualTreeHelper.GetChild(_window, 0);

            // Undo anything that was done before.
            rootElement.Margin = new Thickness();
        }

        private void _RestoreGlassFrame()
        {
            Assert.IsNull(_chromeInfo);
            Assert.IsNotNull(_window);

            _RestoreGlassFrameCore();
        }

        private void _RestoreGlassFrameCore()
        {
            // Expect that this might be called on OSes other than Vista
            // and if the window hasn't yet been shown, then we don't need to undo anything.
            if (!Utility.IsOSVistaOrNewer || !_IsHwndAlive)
            {
                return;
            }

            _RestoreCompositionBackground();

            if (NativeMethods.DwmIsCompositionEnabled())
            {
                // If glass is enabled, push it back to the normal bounds.
                var dwmMargin = new MARGINS();
                NativeMethods.DwmExtendFrameIntoClientArea(_hwnd, ref dwmMargin);
            }
        }

        private void _RestoreHrgn()
        {
            _ClearRoundingRegion();
            _ChangeFrame();
        }

        #endregion
    }
}
