// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Implements the base Avalon Window class
//

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using MS.Internal;
using MS.Internal.AppModel;
using MS.Internal.Interop;
using MS.Internal.KnownBoxes;
using MS.Win32;

using HRESULT = MS.Internal.Interop.HRESULT;
using BuildInfo = MS.Internal.PresentationFramework.BuildInfo;

//In order to avoid generating warnings about unknown message numbers and
//unknown pragmas when compiling your C# source code with the actual C# compiler,
//you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

namespace System.Windows
{
    /// <summary>
    ///
    /// </summary>
    [Localizability(LocalizationCategory.Ignore)]
    public class Window : ContentControl, IWindowService
    {
        //---------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------
        #region Constructors

        /// <summary>
        ///     Initializes the dependency ids of this class
        /// </summary>
        static Window()
        {
            HeightProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(new PropertyChangedCallback(_OnHeightChanged)));
            MinHeightProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(new PropertyChangedCallback(_OnMinHeightChanged)));
            MaxHeightProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(new PropertyChangedCallback(_OnMaxHeightChanged)));
            WidthProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(new PropertyChangedCallback(_OnWidthChanged)));
            MinWidthProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(new PropertyChangedCallback(_OnMinWidthChanged)));
            MaxWidthProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(new PropertyChangedCallback(_OnMaxWidthChanged)));

            // override VisibilityProperty Metadata. For Window, Visibility.Visible means the Window is visible.
            // Visibility.Hidden and Visibility.Collapsed mean the Window is not visible.
            // Visibility.Hidden and Visibility.Collapsed are treated the same.
            // We default to Visibility.Collapsed since RenderSize returns (0,0) only for
            // collapsed elements and not for hidden. We want to return (0,0) when window is
            // never shown.
            VisibilityProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(Visibility.Collapsed, new PropertyChangedCallback(_OnVisibilityChanged), new CoerceValueCallback(CoerceVisibility)));

            IsTabStopProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));
            KeyboardNavigation.ControlTabNavigationProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));
            FocusManager.IsFocusScopeProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(BooleanBoxes.TrueBox));

            DefaultStyleKeyProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(typeof(Window)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(Window));

            FlowDirectionProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(new PropertyChangedCallback(_OnFlowDirectionChanged)));
            // ideally this would just use a ValidateValueCallback
            // We don't support setting RenderTransform and ClipToBounds on Window. Exception will be thrown in Coerce callbacks
            RenderTransformProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(Transform.Identity, new PropertyChangedCallback(_OnRenderTransformChanged), new CoerceValueCallback(CoerceRenderTransform)));
            ClipToBoundsProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox, new PropertyChangedCallback(_OnClipToBoundsChanged), new CoerceValueCallback(CoerceClipToBounds)));

            // Note that this event only gets raised in Windows 7 and later.
            WM_TASKBARBUTTONCREATED = UnsafeNativeMethods.RegisterWindowMessage("TaskbarButtonCreated");

            WM_APPLYTASKBARITEMINFO = UnsafeNativeMethods.RegisterWindowMessage("WPF_ApplyTaskbarItemInfo");

            EventManager.RegisterClassHandler(typeof(Window),
                UIElement.ManipulationCompletedEvent,
                new EventHandler<ManipulationCompletedEventArgs>(OnStaticManipulationCompleted),
                /*handledEventsToo*/ true);
            EventManager.RegisterClassHandler(typeof(Window),
                UIElement.ManipulationInertiaStartingEvent,
                new EventHandler<ManipulationInertiaStartingEventArgs>(OnStaticManipulationInertiaStarting),
                /*handledEventsToo*/ true);

            Window.DpiChangedEvent = EventManager.RegisterRoutedEvent("DpiChanged", RoutingStrategy.Bubble,
                typeof (System.Windows.DpiChangedEventHandler), typeof (Window));

            WpfDllVerifier.VerifyWpfDllSet();
        }

        /// <summary>
        ///     Constructs a window object
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        ///
        ///     Initializes the Width/Height, Top/Left properties to use windows
        ///     default. Updates Application object properties if inside app.
        ///
        ///     Also, window style is set to WS_CHILD inside CreateSourceWindow
        ///     for browser hosted case
        ///
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public Window()
        {
            _inTrustedSubWindow = false;
            Initialize();
        }

        /// <summary>
        ///     Constructs a window object
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        ///
        ///     Initializes the Width/Height, Top/Left properties to use windows
        ///     default. Updates Application object properties if inside app.
        ///
        ///     Also, window style is set to WS_CHILD inside CreateSourceWindow
        ///     for browser hosted case
        ///
        ///     This method currently requires full trust to run.
        /// </remarks>
        internal Window(bool inRbw):base()
        {
            if (inRbw)
            {
                _inTrustedSubWindow = true;
            }
            else
            {
                _inTrustedSubWindow = false;
            }
            Initialize();
}
        #endregion Constructors

        //---------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------
        #region Public Methods

        /// <summary>
        ///     Show the window
        /// </summary>
        /// <remarks>
        ///     Calling Show on window is the same as setting the
        ///     Visibility property to Visibility.Visible.
        /// </remarks>
        public void Show()
        {
            VerifyContextAndObjectState();
            VerifyCanShow();
            VerifyNotClosing();
            VerifyConsistencyWithAllowsTransparency();

            // Update the property value only.  Do not do anything further in
            // _OnVisibilityInvalidate since we will synchronously call ShowHelper
            // from here.
            UpdateVisibilityProperty(Visibility.Visible);

            ShowHelper(BooleanBoxes.TrueBox);
        }

        /// <summary>
        ///     Hide the window
        /// </summary>
        /// <remarks>
        ///     Calling Hide on window is the same as setting the
        ///     Visibility property to Visibility.Hidden
        ///     </remarks>
        public void Hide()
        {
            VerifyContextAndObjectState();

            if (_disposed == true)
            {
                return;
            }

            // set Visibility to Hidden even if _isVisible is false since
            // _isVisible can be false b/c of Visibility = Collapsed and Hide()
            // should change Visibility to Hidden.
            //
            // Update the property value only.  Do not do anything further in
            // _OnVisibilityInvalidate since we will synchronously call ShowHelper
            // from here.
            UpdateVisibilityProperty(Visibility.Hidden);

            ShowHelper(BooleanBoxes.FalseBox);
        }

        /// <summary>
        ///     Closes the Window
        /// </summary>
        /// <remarks>
        ///     Window fires the Closing event before it closes. If the
        ///     user cancels the closing event, the window is not closed.
        ///     Otherwise, the window is closed and the Closed event is
        ///     fired.
        ///
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public void Close()
        {
            // this call ends up throwing an exception if Close
            // is not allowed
            VerifyApiSupported();
            VerifyContextAndObjectState();
            InternalClose(false, false);
        }

        /// <summary>
        ///     Kick off the Window's MoveWindow loop
        /// </summary>
        /// <remarks>
        ///     To enable custom chrome on Windows. First check if this is the Left MouseButton.
        ///     Will throw exception if it's not, otherwise, will kick off the Windows's MoveWindow loop.
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public void DragMove()
        {
            // this call ends up throwing an exception if dragmove
            // is not allowed
            VerifyApiSupported();
            VerifyContextAndObjectState();
            VerifyHwndCreateShowState();

            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull || IsCompositionTargetInvalid)
            {
                return;
            }

            // Mouse.LeftButton actually reflects the primary button user is using.
            // So we don't need to check whether the button has been swapped here.
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (WindowState == WindowState.Normal)
                {
                    // SendMessage's return value is dependent on the message send.  WM_SYSCOMMAND
                    // and WM_LBUTTONUP return value just signify whether the WndProc handled the
                    // message or not, so they are not interesting
#pragma warning disable 6523
                    UnsafeNativeMethods.SendMessage( CriticalHandle, WindowMessage.WM_SYSCOMMAND, (IntPtr)NativeMethods.SC_MOUSEMOVE, IntPtr.Zero);
                    UnsafeNativeMethods.SendMessage( CriticalHandle, WindowMessage.WM_LBUTTONUP, IntPtr.Zero, IntPtr.Zero);
#pragma warning restore 6523
                }
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.DragMoveFail));
            }
}

        /// <summary>
        ///     Shows the window as a modal window
        /// </summary>
        /// <returns>bool?</returns>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public Nullable<bool> ShowDialog()
        {
            // this call ends up throwing an exception if ShowDialog
            // is not allowed
            VerifyApiSupported();
            VerifyContextAndObjectState();
            VerifyCanShow();
            VerifyNotClosing();
            VerifyConsistencyWithAllowsTransparency();

            if ( _isVisible == true )
            {
                throw new InvalidOperationException(SR.Get(SRID.ShowDialogOnVisible));
            }
            else if ( _showingAsDialog == true )
            {
                throw new InvalidOperationException(SR.Get(SRID.ShowDialogOnModal));
            }

            _dialogOwnerHandle = _ownerHandle;

            // verify owner handle is window
            if (UnsafeNativeMethods.IsWindow( new HandleRef( null, _dialogOwnerHandle ) ) != true)
            {
                _dialogOwnerHandle = IntPtr.Zero;
            }


            // remember the current active window;
            // this is used when dialog creation fails or dialog closes, we set the active window back to this one.
            _dialogPreviousActiveHandle = UnsafeNativeMethods.GetActiveWindow();

            // if owner window is not specified, we get the current active window on this thread's
            // message queue as the owner.
            if (_dialogOwnerHandle == IntPtr.Zero)
            {
                _dialogOwnerHandle = _dialogPreviousActiveHandle;
            }

            // If hwndOwner == HWNDESKTOP, change it to NULL.  This way the desktop
            // (and all its children) won't be disabled if the dialog is modal.
            if ((_dialogOwnerHandle != IntPtr.Zero) &&
                (_dialogOwnerHandle == UnsafeNativeMethods.GetDesktopWindow()))
            {
                _dialogOwnerHandle = IntPtr.Zero;
            }

            // if dialog owner is not null, get the top level window (case where dialog owner is a
            // child window), and save it's state regarding enabled and active window
            if (_dialogOwnerHandle != IntPtr.Zero)
            {
                // get the top level window from the dialog owner handle
                int style = 0;

                while (_dialogOwnerHandle != IntPtr.Zero)
                {
                    style = UnsafeNativeMethods.GetWindowLong(new HandleRef(this, _dialogOwnerHandle), NativeMethods.GWL_STYLE);
                    if ((style & NativeMethods.WS_CHILD) == NativeMethods.WS_CHILD)
                    {
                        _dialogOwnerHandle = UnsafeNativeMethods.GetParent(new HandleRef(null, _dialogOwnerHandle));
                    }
                    else
                    {
                        break;
                    }
                }
            }

            Debug.Assert(_threadWindowHandles == null, "_threadWindowHandles must be null before enumerating the thread windows");

            // NOTE:
            // _threadWindowHandles is created here.  This reference is nulled out in EnableThreadWindows
            // when it is called with a true parameter.  Please do not null it out anywhere else.
            // EnableThreadWindow(true) is called when dialog is going away.  Once dialog is closed and
            // thread windows have been enabled, then there no need to keep the array list around.
            // Please see BUG 929740 before making any changes to how _threadWindowHandles works.
            _threadWindowHandles = new ArrayList();
            //Get visible and enabled windows in the thread
            // If the callback function returns true for all windows in the thread, the return value is true.
            // If the callback function returns false on any enumerated window, or if there are no windows
            // found in the thread, the return value is false.
            // No need for use to actually check the return value.
#pragma warning disable 6523
            UnsafeNativeMethods.EnumThreadWindows(SafeNativeMethods.GetCurrentThreadId(),
                                                  new NativeMethods.EnumThreadWindowsCallback(ThreadWindowsCallback),
                                                  NativeMethods.NullHandleRef);
#pragma warning enable 6523
            //disable those windows
            EnableThreadWindows(false);

            IntPtr hWndCapture = SafeNativeMethods.GetCapture();
            if (hWndCapture != IntPtr.Zero)
            {
                //
                // NOTE:
                // EnableWindow(false) (called from EnableThreadWindows(false)
                // sends WM_CANCELMODE to the window, so we don't need
                // to send it again.  However, if we change our impl
                // of dialog such that we don't disable all windows on the
                // thread, then we would need this call. Keeping this code here
                // until we finish the Dialog task # 18498

                // UnsafeNativeMethods.SendMessage(hWndCapture,
                //                                WindowMessage.WM_CANCELMODE,
                //                                IntPtr.Zero,
                //                                IntPtr.Zero);

                // hWndCapture = UnsafeNativeMethods.GetCapture();
                // if (hWndCapture != IntPtr.Zero)
                // {
                    // PS # 862892
                    // WCP: Investigate whether ReleaseCapture is needed in ShowDialog
                    SafeNativeMethods.ReleaseCapture();
                // }
            }

            // Ensure Dialog RoutedCommand is registered with CommandManager
            EnsureDialogCommand();

            try
            {
                _showingAsDialog = true;
                Show();
            }
            catch
            {
                // NOTE:
                // See BUG 929740.
                // _threadWindowHandles is created before calling ShowDialog and is deleted in
                // EnableThreadWindows (when it's called with true).
                //
                // Window dlg = new Window();
                // Button b = new button();
                // b.OnClick += new ClickHandler(OnClick);
                // dlg.ShowDialog();
                //
                //
                // void OnClick(...)
                // {
                //      dlg.Close();
                //      throw new Exception();
                // }
                //
                //
                // If above code is written, then we get inside this exception handler only after the dialog
                // is closed.  In that case all the windows that we disabled before showing the dialog have already
                // been enabled and _threadWindowHandles set to null in EnableThreadWindows.  Thus, we don't
                // need to do it again.
                //
                // In any other exception cases, we get in this handler before Dialog is closed and thus we do
                // need to enable all the disable windows.
                if (_threadWindowHandles != null)
                {
                    // Some exception case. Re-enable the windows that were disabled
                    EnableThreadWindows(true);
                }

                // Activate the previously active window.
                // This code/logic came from User.
                if ( (_dialogPreviousActiveHandle != IntPtr.Zero) &&
                    (UnsafeNativeMethods.IsWindow(new HandleRef(null, _dialogPreviousActiveHandle)) == true))
                {
                    // SetFocus fails if the input hwnd is not a Window or if the Window is not on the
                    // calling thread.
                    //
                    // Furthermore, this code path is executed when an exception occurs when we try to
                    // show the window.  Here we are doing the minimum possible to restore state of
                    // the avalon window object.  Hence, if for some reason, we are not able to
                    // SetFocus to the window that previously had focus, we don't care as that failure
                    // is not important enought to warrant throwing an exception.
                    UnsafeNativeMethods.TrySetFocus(new HandleRef(null, _dialogPreviousActiveHandle), ref _dialogPreviousActiveHandle);
                }

                // clears _showingAsDialog and accelerators related fields
                ClearShowKeyboardCueState();
                _showingAsDialog = false;

                // using catch and throw instead of catch(Exception e) throw e;  since the former
                // gives the complete call stack upto the offending method where the exception is thrown
                throw;
            }
            finally
            {
                // If the owner window belongs to another thread, the reactivation
                // of the owner may have failed within DestroyWindow().  Therefore,
                // if the current thread is in the foreground and the owner is not
                // in the foreground we can safely set the foreground back
                // to the owner.
#if FIGURE_OUT

                // WCP Dialog: Figure out what to do when reactivating the owner window
                // which is in another thread.
                if (_dialogOwnerHandle != IntPtr.Zero)
                {
                    if (IsCurrentThreadForeground() &&
                        !IsInForegroundQueue(hwndOwner))
                    {
                        NtUserSetForegroundWindow(hwndOwner);
                    }
                }
#endif //FIGURE_OUT
                _showingAsDialog = false;
            }
            return _dialogResult;
        }


        /// <summary>
        ///     This method tries to activate the Window.
        /// </summary>
        /// <remarks>
        ///     This method calls SetForegroundWindow on the hWnd, thus the rules for SetForegroundWindow
        ///     apply to this method.
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        /// <returns>bool -- indicating whether the window was activated or not</returns>
        public bool Activate()
        {
            // this call ends up throwing an exception if Activate
            // is not allowed
            VerifyApiSupported();
            VerifyContextAndObjectState();
            VerifyHwndCreateShowState();

            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull || IsCompositionTargetInvalid)
            {
                return false;
            }

            return UnsafeNativeMethods.SetForegroundWindow(new HandleRef(null, CriticalHandle));
        }
        #region LogicalTree
        /// <summary>
        ///     Returns enumerator to logical children
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                // Don't use UIElementCollection because we don't have a reference to content's visual parent;
                // window has style and user can change it.
                return new SingleChildEnumerator(this.Content);
            }
        }

        #endregion LogicalTree

        #region static public method

        /// <summary>
        /// Gets Window in which the given DependecyObject is hosted in.
        /// </summary>
        /// <param name="dependencyObject">Returns the Window the given dependencyObject is hosted in.</param>
        /// <returns>Window</returns>
        public static Window GetWindow(DependencyObject dependencyObject)
        {
            if (dependencyObject == null)
            {
                throw new ArgumentNullException("dependencyObject");
            }

            // Window.IWindowServiceProperty is an internal inheritable dependency property
            // Normally this value is set to the root Window element, all the element
            // inside the window view will get this value through property inheritance mechanism.

            return dependencyObject.GetValue(Window.IWindowServiceProperty) as Window;
        }

        #endregion static public method

        #endregion Public Methods
        //---------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------
        #region Public Properties

        /// <summary>
        /// DependencyProperty for TaskbarItemInfo
        /// </summary>
        public static readonly DependencyProperty TaskbarItemInfoProperty = DependencyProperty.Register(
            "TaskbarItemInfo",
            typeof(TaskbarItemInfo),
            typeof(Window),
            new PropertyMetadata(
                null,
                (d, e) => ((Window)d).OnTaskbarItemInfoChanged(e),
                VerifyAccessCoercion));

        /// <summary>
        /// RoutedEvent for when DPI of the screen the Window is on, changes.
        /// </summary>
        public static readonly RoutedEvent DpiChangedEvent;

        /// <summary>
        /// Get or set the TaskbarItemInfo associated with this Window.
        /// </summary>
        public TaskbarItemInfo TaskbarItemInfo
        {
            get
            {
                VerifyContextAndObjectState();
                return (TaskbarItemInfo)GetValue(TaskbarItemInfoProperty);
            }
            set
            {
                VerifyContextAndObjectState();
                SetValue(TaskbarItemInfoProperty, value);
            }
        }

        private void OnTaskbarItemInfoChanged(DependencyPropertyChangedEventArgs e)
        {
            var oldBar = (TaskbarItemInfo)e.OldValue;
            var newBar = (TaskbarItemInfo)e.NewValue;

            // We don't propagate changes to this on anything earlier than Windows 7.
            if (!Utilities.IsOSWindows7OrNewer)
            {
                return;
            }

            if (!e.IsASubPropertyChange)
            {
                if (oldBar != null)
                {
                    oldBar.PropertyChanged -= OnTaskbarItemInfoSubPropertyChanged;
                }
                if (newBar != null)
                {
                    newBar.PropertyChanged += OnTaskbarItemInfoSubPropertyChanged;
                }
                ApplyTaskbarItemInfo();
            }
        }

        private void HandleTaskbarListError(HRESULT hr)
        {
            if (hr.Failed)
            {
                // Even if some of the taskbar methods get this error it doesn't mean that all of them will.
                // They aren't all implemented with SendMessageTimeout, and unfortunately the ITaskbarList3 API inconsistently
                // exposes that implementation detail.
                if (hr == (HRESULT)Win32Error.ERROR_TIMEOUT)
                {
                    // Explorer appears to be busy.  Post back to the Window to try again.
                    if (TraceShell.IsEnabled)
                    {
                        TraceShell.Trace(TraceEventType.Error, TraceShell.ExplorerTaskbarTimeout);
                        TraceShell.Trace(TraceEventType.Warning, TraceShell.ExplorerTaskbarRetrying);
                    }

                    // Explorer being non-responsive should be a transient issue.  Post back to apply the full TaskbarItemInfo.
                    _taskbarRetryTimer.Start();
                }
                else if (hr == (HRESULT)Win32Error.ERROR_INVALID_WINDOW_HANDLE)
                {
                    // We'll get this when Explorer's not running.  This means there's no Shell to integrate with.
                    if (TraceShell.IsEnabled)
                    {
                        TraceShell.Trace(TraceEventType.Warning, TraceShell.ExplorerTaskbarNotRunning);
                    }
                    // If this is a transient condition then we'll get a WM_TASKBARBUTTONCREATED when Explorer comes
                    // back and the taskbar button gets created again, at which point we'll rehook.
                    // In the meantime, just stop trying since there's no reasonable expectation of recovery.
                    Utilities.SafeRelease(ref _taskbarList);
                }
                else
                {
                    // That covers the troublesome errors that we know how to handle.
                    // For anything else we'll ignore the error and count on a subsequent update to correct the state.
                    if (TraceShell.IsEnabled)
                    {
                        TraceShell.Trace(TraceEventType.Error, TraceShell.NativeTaskbarError(hr.ToString()));
                    }
                }
            }
        }

        private void OnTaskbarItemInfoSubPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Don't propagate changes from other TaskbarItemInfos.
            if (sender != this.TaskbarItemInfo)
            {
                // Since this and the TaskbarItemInfo should share affinity for the same thread
                // this really shouldn't happen...
                Debug.Assert(false);
                return;
            }

            // Defer any sub-property changes until the native ITaskbarList3 has been set up.
            if (_taskbarList == null)
            {
                return;
            }

            // If the taskbar has timed out in the last minute, don't try to do this again.
            if (_taskbarRetryTimer != null && _taskbarRetryTimer.IsEnabled)
            {
                return;
            }

            DependencyProperty dp = e.Property;

            HRESULT hr = HRESULT.S_OK;

            if (dp == TaskbarItemInfo.ProgressStateProperty)
            {
                hr = UpdateTaskbarProgressState();
            }
            else if (dp == TaskbarItemInfo.ProgressValueProperty)
            {
                hr = UpdateTaskbarProgressValue();
            }
            else if (dp == TaskbarItemInfo.OverlayProperty)
            {
                hr = UpdateTaskbarOverlay();
            }
            else if (dp == TaskbarItemInfo.DescriptionProperty)
            {
                hr = UpdateTaskbarDescription();
            }
            else if (dp == TaskbarItemInfo.ThumbnailClipMarginProperty)
            {
                hr = UpdateTaskbarThumbnailClipping();
            }
            else if (dp == TaskbarItemInfo.ThumbButtonInfosProperty)
            {
                hr = UpdateTaskbarThumbButtons();
            }

            HandleTaskbarListError(hr);
        }

        /// <summary>
        /// DependencyProperty for AllowsTransparency
        /// </summary>
        public static readonly DependencyProperty AllowsTransparencyProperty =
                DependencyProperty.Register(
                        "AllowsTransparency",
                        typeof(bool),
                        typeof(Window),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.FalseBox,
                                new PropertyChangedCallback(OnAllowsTransparencyChanged),
                                new CoerceValueCallback(CoerceAllowsTransparency)));

        /// <summary>
        /// Whether or not the Window uses per-pixel opacity
        /// </summary>
        public bool AllowsTransparency
        {
            get { return (bool)GetValue(AllowsTransparencyProperty); }
            set { SetValue(AllowsTransparencyProperty, BooleanBoxes.Box(value)); }
        }

        private static void OnAllowsTransparencyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        private static object CoerceAllowsTransparency(DependencyObject d, object value)
        {
            value = VerifyAccessCoercion(d, value);

            if (!((Window) d).IsSourceWindowNull)
            {
                throw new InvalidOperationException(SR.Get(SRID.ChangeNotAllowedAfterShow));
            }

            return value;
        }

        /// <summary>
        ///     The DependencyProperty for TitleProperty.
        ///     Flags:              None
        ///     Default Value:      String.Empty
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
                DependencyProperty.Register("Title", typeof(String), typeof(Window),
                        new FrameworkPropertyMetadata(String.Empty,
                                new PropertyChangedCallback(_OnTitleChanged)),
                        new ValidateValueCallback(_ValidateText));
        /// <summary>
        ///     The data that will be displayed as the title of the window.
        ///     Hosts are free to display the title in any manner that they
        ///     want.  For example, the browser may display the title set via
        ///     the Title property somewhere besides the caption bar
        /// </summary>
        [Localizability(LocalizationCategory.Title)]
        public string Title
        {
            get
            {
                VerifyContextAndObjectState();

                return (String)GetValue(TitleProperty);
            }
            set
            {
                VerifyContextAndObjectState();

                SetValue(TitleProperty, value);
            }
        }

        /// <summary>
        ///     The DependencyProperty for Icon
        ///     Flags:              None
        ///     Default Value:      None
        /// </summary>
        public static readonly DependencyProperty IconProperty =
                DependencyProperty.Register(
                        "Icon",
                        typeof(ImageSource),
                        typeof(Window),
                        new FrameworkPropertyMetadata(
                                new PropertyChangedCallback(_OnIconChanged),
                                new CoerceValueCallback(VerifyAccessCoercion)));

        /// <summary>
        ///     Sets the Icon of the Window
        /// </summary>
        /// <remarks>
        ///     Following is the precedence for displaying the icon:
        ///
        ///     1) Use ImageSource provided by the Icon property.  If Icon property is
        ///     null, see 2 below.
        ///     2) If Icon Property is not set, then use the Application icon
        ///     embedded in the exe.  Querying Icon property returns null.
        ///     3) If no icon is embedded in the exe, then we set IntPtr.Zero
        ///     as the icon and Win32 displays its default icon.  Querying Icon
        ///     property returns null.
        ///
        ///     If Icon property is set, Window does not dispose that object when it
        ///     is closed.
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public ImageSource Icon
        {
            get
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing
                // Icon is not allowed
                VerifyApiSupported();

                return (ImageSource) GetValue(IconProperty);
            }

            set
            {
                // this call ends up throwing an exception if accessing
                // Icon is not allowed
                VerifyApiSupported();
                VerifyContextAndObjectState();

                SetValue(IconProperty, value);
            }
        }

        /// <summary>
        ///     The DependencyProperty for SizeToContentProperty.
        ///     Flags:              None
        ///     Default Value:      "SizeToContent.Manual"
        /// </summary>
        public static readonly DependencyProperty SizeToContentProperty =
                DependencyProperty.Register("SizeToContent",
                        typeof(SizeToContent),
                        typeof(Window),
                        new FrameworkPropertyMetadata(
                                SizeToContent.Manual,
                                new PropertyChangedCallback(_OnSizeToContentChanged)),
                        new ValidateValueCallback(_ValidateSizeToContentCallback));

        /// <summary>
        /// Auto size Window to its content's size
        /// </summary>
        /// <remarks>
        /// 1. SizeToContent can be applied to Width Height independently
        /// 2. After SizeToContent is set, setting Width/Height does not take affect if that
        ///    dimension is sizing to content.
        /// 3. SizeToContent is turned off (restored to SizeToContent.Manual) if user starts to
        ///    interact with window in terms of size
        /// </remarks>
        /// <value>
        /// Default value is SizeToContent.Manual
        /// </value>
        public SizeToContent SizeToContent
        {
            get
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing
                // SizeToContent is not allowed
                VerifyApiSupported();

                return (SizeToContent) GetValue(SizeToContentProperty);
            }
            set
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing
                // SizeToContent is not allowed
                VerifyApiSupported();

                SetValue(SizeToContentProperty, value);
            }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Top" /> property.
        /// </summary>

        public static readonly DependencyProperty TopProperty =
                Canvas.TopProperty.AddOwner(typeof(Window),
                        new FrameworkPropertyMetadata(
                                Double.NaN,
                                new PropertyChangedCallback(_OnTopChanged),
                                new CoerceValueCallback(CoerceTop)));

        /// <summary>
        ///     Position for Top of the host window
        /// </summary>
        /// <remarks>
        ///     The following values are valid:
        ///     Positive Doubles: sets the top location to the specified value
        ///     NaN: indicates to use the system default value. This
        ///     is the default for Top property
        ///     PositiveInfinity, NegativeInfinity: These are invalid inputs.
        /// </remarks>
        /// <value></value>
        [TypeConverter("System.Windows.LengthConverter, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        public double Top
        {
            get
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing Top
                // is not allowed
                VerifyApiSupported();
                return (double)GetValue(TopProperty);
            }
            set
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing Top
                // is not allowed
                VerifyApiSupported();

                // we don't do an if check here to see if the new value is the same
                // as the current Top value b/c the current value maybe as a result
                // of user resizing which means the the local value of Top has not
                // been written to.  So, if window.Top is explicitly set now we want
                // to write to the local value.  We do make this if check in Top
                // property invalidation callback for optimization
                SetValue(TopProperty, value);
            }
        }

        /// <summary>
        /// DependencyProperty for <see cref="Left" /> property.
        /// </summary>
        public static readonly DependencyProperty LeftProperty =
                Canvas.LeftProperty.AddOwner(typeof(Window),
                        new FrameworkPropertyMetadata(
                                Double.NaN,
                                new PropertyChangedCallback(_OnLeftChanged),
                                new CoerceValueCallback(CoerceLeft)));

        /// <summary>
        ///     Position for Left edge of  coordinate of the host window
        /// </summary>
        /// <remarks>
        ///     The following values are valid:
        ///     Positive Doubles: sets the top location to the specified value
        ///     NaN: indicates to use the system default value. This
        ///     is the default for Top property
        ///     PositiveInfinity, NegativeInfinity: These are invalid inputs.
        /// </remarks>
        /// <value></value>
        [TypeConverter("System.Windows.LengthConverter, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        public double Left
        {
            get
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing left
                // is not allowed
                VerifyApiSupported();
                return (double)GetValue(LeftProperty);
            }
            set
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing left
                // is not allowed
                VerifyApiSupported();

                // we don't do an if check here to see if the new value is the same
                // as the current Left value b/c the current value maybe as a result
                // of user resizing which means the the local value of Left has not
                // been written to.  So, if window.Left is explicitly set now we want
                // to write to the local value.  We do make this if check in Left
                // property invalidation callback for optimization
                SetValue(LeftProperty, value);
            }
        }

        /// <summary>
        ///     This property returns the restoring rectangle of the window.  This information
        ///     can be used to track a users size and position preferences when the
        ///     Window is maximized or minimized.
        ///
        ///     If RestoreBounds is queried before the Window has been shown or after it has
        ///     been closed, it will return Rect.Empty.
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>

        public Rect RestoreBounds
        {
            get
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing RestoreBounds
                // is not allowed
                VerifyApiSupported();

                // either before calling show or after closing AND
                // Adding check for IsCompositionTargetInvalid
                if (IsSourceWindowNull || IsCompositionTargetInvalid)
                {
                    return Rect.Empty;
                }

                return GetNormalRectLogicalUnits(CriticalHandle);
            }
        }

        /// <summary>
        ///     This enum can have following values
        ///         Manual (default)
        ///         CenterScreen
        ///         CenterOwner
        ///
        ///     If the WindowStartupLocation is WindowStartupLocation.Manual then
        ///     Top and Left properites are used to position the window.
        ///     This property is used only before window creation. Once the window is
        ///     created hiding it and showing it will not take this property into account.
        /// </summary>
        /// <remarks>
        ///     WindowStartupLocation is used to position the window only it it is set to
        ///     WindowStartupLocation.CenterScreen or WindowStartupLocation.CenterOwner,
        ///     otherwise Top/Left is used.  Furthermore, if determining the location
        ///     of the window is not possible when WindowStartupLocation is set to
        ///     WindowStartupLocation.CenterScreen or WindowStartupLocation.Owner, then
        ///     Top/Left is used instead.
        /// </remarks>
        [DefaultValue(WindowStartupLocation.Manual)]
        public WindowStartupLocation WindowStartupLocation
        {
            get
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing
                // WindowStartupLocation is not allowed
                VerifyApiSupported();

                return _windowStartupLocation;
            }

            set
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing
                // WindowStartupLocation is not allowed
                VerifyApiSupported();

                //validate WindowStartupLocation enum
                if (!IsValidWindowStartupLocation(value))
                {
                    throw new InvalidEnumArgumentException("value", (int)value, typeof( WindowStartupLocation ));
                }
                _windowStartupLocation = value;
            }
        }

        /// <summary>
        ///     The DependencyProperty for ShowInTaskbarProperty.
        ///     Flags:              None
        ///     Default Value:      true
        /// </summary>
        public static readonly DependencyProperty ShowInTaskbarProperty =
                DependencyProperty.Register("ShowInTaskbar",
                        typeof(bool),
                        typeof(Window),
                        new FrameworkPropertyMetadata(BooleanBoxes.TrueBox,
                                new PropertyChangedCallback(_OnShowInTaskbarChanged),
                                new CoerceValueCallback(VerifyAccessCoercion)));

        ///<summary>
        ///     Determines if the window should show up in the system taskbar.
        ///     This also determines if the window appears in the Alt-Tab list.
        ///</summary>
        public bool ShowInTaskbar
        {
            get
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing
                // ShowInTaskbar is not allowed
                VerifyApiSupported();

                return (bool) GetValue(ShowInTaskbarProperty);
            }
            set
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing
                // ShowInTaskbar is not allowed
                VerifyApiSupported();

                SetValue(ShowInTaskbarProperty, BooleanBoxes.Box(value));
            }
        }

        /// <summary>
        ///     The key needed set a read-only property.
        /// </summary>
        private static readonly DependencyPropertyKey IsActivePropertyKey
            = DependencyProperty.RegisterReadOnly("IsActive", typeof(bool), typeof(Window),
                                          new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        ///     The DependencyProperty for IsActive.
        ///     Flags:              None
        ///     Default Value:      True
        ///     Read-Only:          true
        /// </summary>
        public static readonly DependencyProperty IsActiveProperty
            = IsActivePropertyKey.DependencyProperty;

        /// <summary>
        /// IsActive property. It indicates whether the Window is active.
        /// The title bar will have the active theme. The active window will be
        /// the topmost of all top-level windows that don't explicitly set the TopMost property or style.
        /// If a window is active, focus is within the window.
        /// </summary>
        public bool IsActive
        {
            get
            {
                VerifyContextAndObjectState();
                return (bool)GetValue(IsActiveProperty);
            }
        }

        /// <summary>
        ///     This set the owner for the property of the current window.
        ///     If the window has owner and the owner is minimized then
        ///     owned window is also minimized. Owner window can never be
        ///     over owned window. Owned window is NOT modal. So user can
        ///     still interact with owner window. This property can not be
        ///     set of the top level window.
        /// </summary>
        ///<remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        ///</remarks>
        [DefaultValue(null)]
        public Window Owner
        {
            get
            {
                // this call ends up throwing an exception if accessing Owner
                // is not allowed
                VerifyApiSupported();
                VerifyContextAndObjectState();
                return _ownerWindow;
            }
            set
            {
                // this call ends up throwing an exception if accessing Owner
                // is not allowed
                VerifyApiSupported();
                VerifyContextAndObjectState();
                if (value == this)
                {
                    throw new ArgumentException(SR.Get(SRID.CannotSetOwnerToItself));
                }

                if ( _showingAsDialog == true )
                {
                    throw new InvalidOperationException(SR.Get(SRID.CantSetOwnerAfterDialogIsShown));
                }

                if (value != null && value.IsSourceWindowNull == true)
                {
                    // Try to be specific in the error message.
                    if (value._disposed)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.CantSetOwnerToClosedWindow));
                    }
                    throw new InvalidOperationException(SR.Get(SRID.CantSetOwnerWhosHwndIsNotCreated));
                }

                if ( _ownerWindow == value )
                {
                    return;
                }

                if (!_disposed)
                {
                    // Check to see if value is already a child of this window.
                    // If yes, throw Exception
                    if (value != null)
                    {
                        WindowCollection ownedWindows = OwnedWindows;
                        for (int i = 0; i < ownedWindows.Count; i++)
                        {
                            if (ownedWindows[i] == value)
                            {
                                throw new ArgumentException(SR.Get(SRID.CircularOwnerChild, value, this));
                            }
                        }
                    }

                    // Update OwnerWindows of the previous owner
                    if (_ownerWindow != null)
                    {
                        // using OwnedWindowsInternl b/c we want to modifying the
                        // underlying collection
                        _ownerWindow.OwnedWindowsInternal.Remove(this);
                    }
                }

                // Update parent handle. If value is null, then make parent
                // handle IntPtr.Zero
                _ownerWindow = value;

                // We should not do anything if the window is already closed and maybe throw exception.
                // In Dev10, it is unknown whether we can begin to throw exceptions, because it is a BC.
                // In Dev10, we still update _ownerWindow after window is closed just so that the Owner getter
                // returns the right value.
                if (_disposed)
                {
                    return;
                }

                SetOwnerHandle(_ownerWindow != null ? _ownerWindow.CriticalHandle: IntPtr.Zero);

                // Update OwnerWindows of the new owner
                if (_ownerWindow != null)
                {
                    // using OwnedWindowsInternl b/c we want to modifying the
                    // underlying collection
                    _ownerWindow.OwnedWindowsInternal.Add(this);
                }
            }
        }

        /// This code checks to see if the owner property is null
        /// True if the window is null , false other wise
        private bool IsOwnerNull
        {
            get
            {
                return (_ownerWindow == null);
            }
}
        /// <summary>
        ///     This is a collection of windows that are owned by current window.
        /// </summary>
        // This collection is a copy of the original one to avoid synchronizing issues.
        // DO-NOT USE THIS PROPERY IF YOU MEAN TO MODIFY THE UNDERLYING COLLECTION.  USE
        // OwnedWindowsInternal PROPERTY FOR MODIFYING THE UNDERLYING DATASTRUCTURE.
        public WindowCollection OwnedWindows
        {
            get
            {
                VerifyContextAndObjectState();
                return OwnedWindowsInternal.Clone();
            }
        }

        /// <summary>
        /// Sets/gets DialogResult
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), TypeConverter(typeof(DialogResultConverter))]
        public Nullable<bool> DialogResult
        {
            get
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing
                // DialogResult is not allowed
                VerifyApiSupported();

                return _dialogResult;
            }
            set
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing
                // DialogResult is not allowed
                VerifyApiSupported();

                if (_showingAsDialog == true)
                {
                    // This value should be set only after the window is created and shown as dialog.

                    // When _showingAsDialog is set, _sourceWindow must be set too.
                    Debug.Assert( IsSourceWindowNull == false , "IsSourceWindowNull cannot be true when _showingAsDialog is true");


                    // According to the new design, setting DialogResult to its current value will not have any effect.
                    if (_dialogResult != value)
                    {
                        _dialogResult = value;

                        // if DialogResult is set from within a Closing event then
                        // the window is in the closing state.  Thus, if we call
                        // Close() again from here we go into an infinite loop.
                        //
                        // Note: Windows OS bug # 934500 Setting DialogResult
                        // on the Closing EventHandler of a Dialog causes StackOverFlowException

                        if(_isClosing == false)
                        {
                            Close();
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException(SR.Get(SRID.DialogResultMustBeSetAfterShowDialog));
}
            }
        }

        /// <summary>
        ///     The DependencyProperty for WindowStyleProperty.
        ///     Flags:              None
        ///     Default Value:      WindowStyle.SingleBorderWindow
        /// </summary>
        public static readonly DependencyProperty WindowStyleProperty =
                DependencyProperty.Register("WindowStyle", typeof(WindowStyle), typeof(Window),
                        new FrameworkPropertyMetadata(
                                WindowStyle.SingleBorderWindow,
                                new PropertyChangedCallback(_OnWindowStyleChanged),
                                new CoerceValueCallback(CoerceWindowStyle)),
                        new ValidateValueCallback(_ValidateWindowStyleCallback));

        /// <summary>
        ///     Defines the visual style of the window (3DBorderWindow,
        ///     SingleBorderWindow, ToolWindow, none).
        /// </summary>
        /// <remarks>
        ///     Default will be SingleBorderWindow.
        /// </remarks>
        public WindowStyle WindowStyle
        {
            get
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing
                // WindowStyle is not allowed
                VerifyApiSupported();

                return (WindowStyle) GetValue(WindowStyleProperty);
            }
            set
            {
                VerifyContextAndObjectState();
                // this call ends up throwing an exception if accessing
                // WindowStyle is not allowed
                VerifyApiSupported();

                SetValue(WindowStyleProperty, value);
            }
        }

        private static object CoerceWindowStyle(DependencyObject d, object value)
        {
            value = VerifyAccessCoercion(d, value);

            if (!((Window)d).IsSourceWindowNull)
            {
                // Since the new style hasn't actually been set yet, verify against the new value.
                ((Window)d).VerifyConsistencyWithAllowsTransparency((WindowStyle)value);
            }

            return value;
        }

        /// <summary>
        ///     The DependencyProperty for WindowStateProperty.
        ///     Flags:              None
        ///     Default Value:      WindowState.Normal
        /// </summary>
        public static readonly DependencyProperty WindowStateProperty =
                DependencyProperty.Register("WindowState", typeof(WindowState), typeof(Window),
                        new FrameworkPropertyMetadata(
                                WindowState.Normal,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                new PropertyChangedCallback(_OnWindowStateChanged),
                                new CoerceValueCallback(VerifyAccessCoercion)),
                        new ValidateValueCallback(_ValidateWindowStateCallback));

        /// <summary>
        ///     Current state of the window.  Valid options are Maximized, Minimized,
        ///     or Normal.  The host window may choose to ignore a request to change
        ///     the current window state.
        /// </summary>
        public WindowState WindowState
        {
            get
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing
                // WindowState is not allowed
                VerifyApiSupported();

                return (WindowState) GetValue(WindowStateProperty);
            }
            set
            {
                VerifyContextAndObjectState();
                // this call ends up throwing an exception if accessing
                // WindowState is not allowed
                VerifyApiSupported();

                SetValue(WindowStateProperty, value);
            }
        }

        /// <summary>
        ///     The DependencyProperty for the ResizeMode property.
        ///     Flags:                  AffectsMeasure
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty ResizeModeProperty =
                DependencyProperty.Register("ResizeMode", typeof(ResizeMode), typeof(Window),
                        new FrameworkPropertyMetadata(ResizeMode.CanResize,
                                FrameworkPropertyMetadataOptions.AffectsMeasure,
                                new PropertyChangedCallback(_OnResizeModeChanged),
                                new CoerceValueCallback(VerifyAccessCoercion)),
                        new ValidateValueCallback(_ValidateResizeModeCallback));

        /// <summary>
        ///     Current state of the window.  Valid options are Maximized, Minimized,
        ///     or Normal.  The host window may choose to ignore a request to change
        ///     the current window state.
        /// </summary>
        public ResizeMode ResizeMode
        {
            get
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing
                // ResizeMode is not allowed
                VerifyApiSupported();

                return ((ResizeMode) GetValue(ResizeModeProperty));
            }
            set
            {
                VerifyContextAndObjectState();
                // this call ends up throwing an exception if accessing
                // ResizeMode is not allowed
                VerifyApiSupported();

                SetValue(ResizeModeProperty, value);
            }
        }

        /// <summary>
        ///     The DependencyProperty for TopmostProperty.
        ///     Flags:              None
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty TopmostProperty =
                DependencyProperty.Register("Topmost",
                        typeof(bool),
                        typeof(Window),
                        new FrameworkPropertyMetadata(BooleanBoxes.FalseBox,
                                new PropertyChangedCallback(_OnTopmostChanged),
                                new CoerceValueCallback(VerifyAccessCoercion)));

        /// <summary>
        ///     Determines if this window is always on the top.
        /// </summary>
        public bool Topmost
        {
            get
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing
                // Topmost is not allowed
                VerifyApiSupported();

                return (bool) GetValue(TopmostProperty);
            }
            set
            {
                VerifyContextAndObjectState();
                // this call ends up throwing an exception if accessing
                // Topmost is not allowed
                VerifyApiSupported();

                SetValue(TopmostProperty, BooleanBoxes.Box(value));
            }
        }

        public static readonly DependencyProperty ShowActivatedProperty =
                DependencyProperty.Register("ShowActivated",
                        typeof(bool),
                        typeof(Window),
                        new FrameworkPropertyMetadata(BooleanBoxes.TrueBox,
                                null,
                                new CoerceValueCallback(VerifyAccessCoercion)));

        /// <summary>
        ///     Determines if this window is activated when shown (default = true).
        /// </summary>
        /// <remarks>
        ///     Not supported for RBW.
        /// </remarks>
        public bool ShowActivated
        {
            get
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing
                // ShowActivated is not allowed
                VerifyApiSupported();

                return (bool)GetValue(ShowActivatedProperty);
            }
            set
            {
                VerifyContextAndObjectState();

                // this call ends up throwing an exception if accessing
                // ShowActivated is not allowed
                VerifyApiSupported();

                SetValue(ShowActivatedProperty, BooleanBoxes.Box(value));
            }
        }

        #endregion Public Properties

        //---------------------------------------------------
        //
        // Public Events
        //
        //---------------------------------------------------
        #region Public Events

        /// <summary>
        ///     This event is raised after the window source is created before it is shown
        /// </summary>
        public event EventHandler SourceInitialized
        {
            add { Events.AddHandler(EVENT_SOURCEINITIALIZED, value); }
            remove { Events.RemoveHandler(EVENT_SOURCEINITIALIZED, value); }
        }

        /// <summary>
        ///     This event is raised after the DPI of the screen on which the Window is displayed, changes.
        /// </summary>
        public event DpiChangedEventHandler DpiChanged
        {
            add { AddHandler(Window.DpiChangedEvent, value); }
            remove { RemoveHandler(Window.DpiChangedEvent, value); }
        }

        /// <summary>
        ///     This event is raised when the window is activated
        /// </summary>
        public event EventHandler Activated
        {
            add { Events.AddHandler(EVENT_ACTIVATED, value); }
            remove { Events.RemoveHandler(EVENT_ACTIVATED, value); }
        }

        /// <summary>
        ///     This event is raised when the window is deactivated
        /// </summary>
        public event EventHandler Deactivated
        {
            add { Events.AddHandler(EVENT_DEACTIVATED, value); }
            remove { Events.RemoveHandler(EVENT_DEACTIVATED, value); }
        }

        /// <summary>
        ///     This event is raised when the window state is changed
        /// </summary>
        public event EventHandler StateChanged
        {
            add { Events.AddHandler(EVENT_STATECHANGED, value); }
            remove { Events.RemoveHandler(EVENT_STATECHANGED, value); }
        }

        /// <summary>
        ///     This event is raised when the window location is changed
        /// </summary>
        public event EventHandler LocationChanged
        {
            add { Events.AddHandler(EVENT_LOCATIONCHANGED, value); }
            remove { Events.RemoveHandler(EVENT_LOCATIONCHANGED, value); }
        }

        /// <summary>
        ///     This event is raised before the window is closed
        /// </summary>
        /// <remarks>
        ///     The user can set the CancelEventArg.Cancel property to true to prevent
        ///     the window from closing. However, if the Applicaiton is shutting down
        ///     the window closing cannot be cancelled
        /// </remarks>
        public event CancelEventHandler Closing
        {
            add { Events.AddHandler(EVENT_CLOSING, value); }
            remove { Events.RemoveHandler(EVENT_CLOSING, value); }
        }

        /// <summary>
        ///     This event is raised when the window is closed.
        /// </summary>
        public event EventHandler Closed
        {
            add { Events.AddHandler(EVENT_CLOSED, value); }
            remove { Events.RemoveHandler(EVENT_CLOSED, value); }
        }

        /// <summary>
        ///     This event is raised when the window and its content is rendered.
        /// </summary>
        public event EventHandler ContentRendered
        {
            add { Events.AddHandler(EVENT_CONTENTRENDERED, value); }
            remove { Events.RemoveHandler(EVENT_CONTENTRENDERED, value); }
        }

        #endregion Public Events

        //---------------------------------------------------
        //
        // Internal Events
        //
        //---------------------------------------------------
        #region Internal Events

        /// <summary>
        ///     This event is raised when the <see cref="System.Windows.Media.VisualCollection"/> of
        ///     this <see cref="Window"/> object is modified
        /// </summary>
        /// <remarks>
        ///     This corresponds to <see cref="OnVisualChildrenChanged(DependencyObject, DependencyObject)"/>.
        ///     The primary consumer of this event is <see cref="System.Windows.Shell.WindowChromeWorker"/>,
        ///     which only needs to be aware of the fact that Visual children have changed, but doesn't
        ///     need to know which children have changed. Therefore the <see cref="DependencyObject"/>
        ///     parameters of <see cref="OnVisualChildrenChanged(DependencyObject, DependencyObject)"/> are
        ///     not passed along to the <see cref="Shell.WindowChromeWorker"/> via this event.
        /// </remarks>
        internal event EventHandler<EventArgs> VisualChildrenChanged
        {
            add { Events.AddHandler(EVENT_VISUALCHILDRENCHANGED, value); }
            remove { Events.RemoveHandler(EVENT_VISUALCHILDRENCHANGED, value); }
        }

        #endregion

        //---------------------------------------------------
        //
        // Protected Methods
        //
        //---------------------------------------------------
        #region Protected Methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new WindowAutomationPeer(this);
        }

        /// <summary>
        /// OnDpiChanged is called when the DPI at which this Window is rendered, changes.
        /// </summary>
        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            RaiseEvent(new DpiChangedEventArgs(oldDpi, newDpi, Window.DpiChangedEvent, this));
        }

        /// <summary>
        /// OnVisualParentChanged is called when the parent of the Visual is changed.
        /// </summary>
        /// <param name="oldParent">Old parent or null if the Visual did not have a parent before.</param>
        protected internal sealed override void OnVisualParentChanged(DependencyObject oldParent)
        {
            VerifyContextAndObjectState();
            base.OnVisualParentChanged(oldParent);

            // Checking for Visual parent here covers all the scenarios
            // including the following:

            // Window w1 = new Window();
            // Window w2 = new WIndow();
            // w1.Show();
            // w2.Show();
            // w1.VisualChildren.Add(w2);


            // Window w1 = new Window();
            // Window w2 = new WIndow();
            // w1.Show();
            // w1.VisualChildren.Add(w2);
            //  w2.Show();

            if ( VisualTreeHelper.GetParent(this) != null )
            {
                throw new InvalidOperationException(SR.Get(SRID.WindowMustBeRoot));
            }
        }

        /// <summary>
        ///     Measurement override. Implements content sizing logic.
        /// </summary>
        /// <remarks>
        ///     Deducts the frame size from the constraint and then passes it on
        ///     to its child.  Only supports one Visual child (just like control)
        /// </remarks>
        protected override Size MeasureOverride(Size availableSize)
        {
            VerifyContextAndObjectState();

            // Window content should respect Window's Max/Min size
            // setting in a SizeToContent Window.
            //
            // Take Min/Max[Width/Height] into consideration.  The logic here is similar to
            // that used in FE.MeasureCore but is limited to Min/Max restriction.  Furthermore,
            // we have our own version of MinMax struct called WindowMinMax that takes
            // SizeToContent into account when calculating the min/max values for height/width.
            //
            // We don't do anything special in ArrangeOverride the Arrange size is guaranteed
            // to be the available hwnd size which should be atleast as big as the desired size.

            Size frameworkAvailableSize = new Size(availableSize.Width, availableSize.Height);

            WindowMinMax mm = GetWindowMinMax();

            frameworkAvailableSize.Width  = Math.Max(mm.minWidth,  Math.Min(frameworkAvailableSize.Width, mm.maxWidth));
            frameworkAvailableSize.Height = Math.Max(mm.minHeight, Math.Min(frameworkAvailableSize.Height, mm.maxHeight));

            //  call to specific layout to measure
            Size desiredSize = MeasureOverrideHelper(frameworkAvailableSize);

            //  maximize desiredSize with user provided min size
            desiredSize = new Size(
                Math.Max(desiredSize.Width, mm.minWidth),
                Math.Max(desiredSize.Height, mm.minHeight));

            return desiredSize;
        }

        /// <summary>
        ///     ArrangeOverride allows for the customization of the positioning of children.
        /// </summary>
        /// <remarks>
        ///     Deducts the frame size of the window from the constraint and then
        ///     arranges its child.  Supports only one child.
        /// </remarks>
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            VerifyContextAndObjectState();

            // Window content should respect Window's Max/Min size
            // setting in a SizeToContent Window.

            WindowMinMax mm = GetWindowMinMax();

            arrangeBounds.Width  = Math.Max(mm.minWidth,  Math.Min(arrangeBounds.Width, mm.maxWidth));
            arrangeBounds.Height = Math.Max(mm.minHeight, Math.Min(arrangeBounds.Height, mm.maxHeight));

            // Three primary cases
            //      1) hwnd does not exist  -- don't do anything
            //      1a) CompositionTarget is invalid -- don't do anything
            //      2) Child visual exists  -- arrange child at arrangeBounds - window frame size
            //      3) No Child visual      -- don't do anything

            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull || IsCompositionTargetInvalid)
            {
                return arrangeBounds;
            }

            if (this.VisualChildrenCount > 0)
            {
                UIElement child = this.GetVisualChild(0) as UIElement;
                if (child != null)
                {
                    // Find out the size of the window frame x.
                    // (constraint - x) is the size we pass onto
                    // our child
                    Size frameSize = GetHwndNonClientAreaSizeInMeasureUnits();

                    // In some instances (constraint size - frame size) can be negative. One instance
                    // is when window is set to minimized before layout has happened.  Apparently, Win32
                    // gives a rect(-32000, -32000, -31840, -31975) for GetWindowRect when hwnd is
                    // minimized.  However, when we calculate the frame size we get width = 8 and
                    // height = 28!!!  Here, we will take the max of zero and the difference b/w the
                    // hwnd size and the frame size
                    //
                    // PS Windows OS Bug: 955861

                    Size childArrangeBounds = new Size();
                    childArrangeBounds.Width = Math.Max(0.0, arrangeBounds.Width - frameSize.Width);
                    childArrangeBounds.Height = Math.Max(0.0, arrangeBounds.Height - frameSize.Height);

                    child.Arrange(new Rect(childArrangeBounds));

                    // Windows OS bug # 928719, 953458
                    // The default impl of FlowDirection is that it adds a transform on the element
                    // on whom the FlowDirection property is RlTb.  However, transforms work only if
                    // there is a parent visual.  In the window case, we are the root and thus this
                    // does not work.  Thus, we add the same transform to our child, if our
                    // FlowDireciton = Rltb.
                    if (FlowDirection == FlowDirection.RightToLeft)
                    {
                        InternalSetLayoutTransform(child, new MatrixTransform(-1.0, 0.0, 0.0, 1.0, childArrangeBounds.Width, 0.0));
                    }
}
            }
            return arrangeBounds;
        }

        /// <summary>
        ///     This method is invoked when the Content property changes.
        /// </summary>
        /// <param name="oldContent">The old value of the Content property.</param>
        /// <param name="newContent">The new value of the Content property.</param>
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            // FxCop: ConstructorsShouldNotCallBaseClassVirtualMethods::
            // System.Windows (presentationframework.dll 2 violation)
            //
            // We can't set IWS property in the cctor since it results
            // in calling some virtual.  So, as an alternative we set it
            // when content is changed and when we set the root visual.
            // We set it here, b/c once window has logical children, they
            // can query for inherited IWS property.
            SetIWindowService();

            // We post a dispatcher work item to fire ContentRendered
            // only if this is Loaded in the tree.  If not, we will
            // post it from the LoadedHandler.  This guarantees that
            // we don't fire ContentRendered on a subtree that is not
            // connected to a PresentationSource
            if (IsLoaded == true)
            {
                PostContentRendered();
            }
            else
            {
                // _postContentRenderedFromLoadedHandler == true means
                // that we deferred to the Loaded event to PostConetentRendered
                // for the previous content change and Loaded has not fired yet.
                // Thus we don't want to hook up another event handler
                if (_postContentRenderedFromLoadedHandler == false)
                {
                    this.Loaded += new RoutedEventHandler(LoadedHandler);
                    _postContentRenderedFromLoadedHandler = true;
                }
            }
        }

        /// <summary>
        ///     This even fires after the window source is created before it is shown. This event is non cancelable and is
        ///     for user infromational purposes
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that subclass
        ///     the event. If you override this method - you need to call Base.OnSourceInitialized(...) for
        ///     the corresponding event to be raised.
        /// </remarks>
        /// <param name="e"></param>
        protected virtual void OnSourceInitialized(EventArgs e)
        {
            VerifyContextAndObjectState();
            EventHandler handler = (EventHandler)Events[EVENT_SOURCEINITIALIZED];
            if (handler != null) handler(this, e);
        }

        /// <summary>
        ///     This even fires when window is activated. This event is non cancelable and is
        ///     for user infromational purposes
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that subclass
        ///     the event. If you override this method - you need to call Base.OnClosed(...) for
        ///     the corresponding event to be raised.
        /// </remarks>
        /// <param name="e"></param>
        protected virtual void OnActivated(EventArgs e)
        {
            VerifyContextAndObjectState();
            EventHandler handler = (EventHandler)Events[EVENT_ACTIVATED];
            if (handler != null) handler(this, e);
        }

        /// <summary>
        ///     This even fires when window is deactivated. This event is non cancelable and is
        ///     for user infromational purposes
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that subclass
        ///     the event. If you override this method - you need to call Base.OnClosed(...) for
        ///     the corresponding event to be raised.
        /// </remarks>
        protected virtual void OnDeactivated(EventArgs e)
        {
            VerifyContextAndObjectState();
            EventHandler handler = (EventHandler)Events[EVENT_DEACTIVATED];
            if (handler != null) handler(this, e);
        }

        /// <summary>
        ///     This even fires when window state is changed. This event is non
        ///     cancelable and is for user infromational purposes
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers
        ///     that subclass the event. If you override this method - you need to call
        ///     Base.OnClosed(...) for the corresponding event to be raised.
        /// </remarks>
        protected virtual void OnStateChanged(EventArgs e)
        {
            VerifyContextAndObjectState();
            EventHandler handler = (EventHandler)Events[EVENT_STATECHANGED];
            if (handler != null) handler(this, e);
        }

        /// <summary>
        ///     This event fires when window location changes. This event is not
        ///     cancelable and is for user infromational purposes
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that subclass
        ///     the event. If you override this method - you need to call Base.OnClosed(...) for
        ///     the corresponding event to be raised.
        /// </remarks>
        protected virtual void OnLocationChanged(EventArgs e)
        {
            VerifyContextAndObjectState();
            EventHandler handler = (EventHandler)Events[EVENT_LOCATIONCHANGED];
            if (handler != null) handler(this, e);
        }

        /// <summary>
        ///     This event fires when window is Closing. This event is cancelable and thus the
        ///     user can set the CancelEventArgs.Cancel property to true to dismiss window
        ///     closing
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that subclass
        ///     the event. If you override this method - you need to call Base.OnClosing(...) for
        ///     the corresponding event to be raised.
        /// </remarks>
        protected virtual void OnClosing(CancelEventArgs e)
        {
            VerifyContextAndObjectState();
            CancelEventHandler handler = (CancelEventHandler)Events[EVENT_CLOSING];
            if (handler != null) handler(this, e);
        }

        /// <summary>
        ///     This event fires when window is closed. This event is non cancelable and is
        ///     for user infromational purposes
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that subclass
        ///     the event. If you override this method - you need to call Base.OnClosed(...) for
        ///     the corresponding event to be raised.
        /// </remarks>
        protected virtual void OnClosed(EventArgs e)
        {
            VerifyContextAndObjectState();
            EventHandler handler = (EventHandler)Events[EVENT_CLOSED];
            if (handler != null) handler(this, e);
        }

        /// <summary>
        ///     This override fires the ContentRendered event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnContentRendered(EventArgs e)
        {
            VerifyContextAndObjectState();

            // After the content is rendered we want to check if there is an element that needs to be focused
            // If there is - set focus to it
            DependencyObject doContent = Content as DependencyObject;
            if (doContent != null)
            {
                IInputElement focusedElement = FocusManager.GetFocusedElement(doContent) as IInputElement;
                if (focusedElement != null)
                    focusedElement.Focus();
            }

            EventHandler handler = (EventHandler)Events[EVENT_CONTENTRENDERED];
            if (handler != null) handler(this, e);
        }
        #endregion Protected Methods

        //---------------------------------------------------
        //
        // Protected Internal Methods
        //
        //---------------------------------------------------
        #region Protected Internal Methods

        /// <summary>
        /// Called when the <see cref="System.Windows.Media.VisualCollection"/> of the visual object is modified
        /// </summary>
        /// <param name="visualAdded">The <see cref="Visual"/> that was added to the collection</param>
        /// <param name="visualRemoved">The <see cref="Visual"/> that was removed from the collection</param>
        protected internal override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            VerifyContextAndObjectState();

            var handler = Events[EVENT_VISUALCHILDRENCHANGED] as EventHandler<EventArgs>;
            handler?.Invoke(this, new EventArgs());
        }

        #endregion

        //---------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------
        #region Internal Methods
        internal Point DeviceToLogicalUnits(Point ptDeviceUnits)
        {
            Invariant.Assert(IsCompositionTargetInvalid == false, "IsCompositionTargetInvalid is supposed to be false here");
            Point ptLogicalUnits = _swh.CompositionTarget.TransformFromDevice.Transform(ptDeviceUnits);
            return ptLogicalUnits;
        }

        internal Point LogicalToDeviceUnits(Point ptLogicalUnits)
        {
            Invariant.Assert(IsCompositionTargetInvalid == false, "IsCompositionTargetInvalid is supposed to be false here");
            Point ptDeviceUnits = _swh.CompositionTarget.TransformToDevice.Transform(ptLogicalUnits);
            return ptDeviceUnits;
        }

        internal static bool VisibilityToBool(Visibility v)
        {
            switch (v)
            {
                case Visibility.Visible:
                    return true;
                case Visibility.Hidden:
                case Visibility.Collapsed:
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     Called by ResizeGrip control to set its reference in the Window object
        /// </summary>
        /// <remarks>
        ///     RBW doesn't need ResizeGrip and hence it doesn't do
        ///     anything in this virtual
        /// </remarks>
        internal virtual void SetResizeGripControl(Control ctrl)
        {
            _resizeGripControl = ctrl;
        }

        internal virtual void ClearResizeGripControl(Control oldCtrl)
        {
            if (oldCtrl == _resizeGripControl)
            {
                _resizeGripControl = null;
            }
        }

        internal virtual void TryClearingMainWindow()
        {
            if (IsInsideApp && this == App.MainWindow)
            {
                App.MainWindow = null;
            }
        }

        /// <summary>
        ///     Send a WM_CLOSE message to close the window. When the WM_CLOSE message is
        ///     processed by the WindowFilterMessage function, the Closing event is fired.
        ///     Closing event is cancelable and thus can dismiss window closing.
        /// </summary>
        /// <param name="shutdown">Specifies whether the app should shutdown or not</param>
        /// <param name="ignoreCancel">Specifies whether cancelling closing should be ignored </param>
        internal void InternalClose(bool shutdown, bool ignoreCancel)
        {
            VerifyNotClosing();

            if (_disposed == true)
            {
                return;
            }

            _appShuttingDown = shutdown;
            _ignoreCancel = ignoreCancel;

            if ( IsSourceWindowNull )
            {
                _isClosing = true;

                // Event handler exception continuality: if exception occurs in Closing event handler, the
                // cleanup action is to finish closing.
                CancelEventArgs e = new CancelEventArgs(false);
                try
                {
                    OnClosing(e);
                }
                catch
                {
                    CloseWindowBeforeShow();
                    throw;
                }

                if (ShouldCloseWindow(e.Cancel))
                {
                    CloseWindowBeforeShow();
                }
                else
                {
                    _isClosing = false;
                    // Dialog does not close with ESC key after it has been cancelled
                    //
                    // No need to reset DialogResult to null here since source window is null.  That means
                    // that ShowDialog has not been called and thus no need to worry about DialogResult.
                }
            }
            else
            {
                // close window synchronously

                // We demand for UIPermission AllWindows at the public API, Window.Close(), level.
                // It can be called when shutting down the app.
                // The public entry to that code path Application.Shutdown is
                // also protected with a demand for UIPermission with AllWindow access

                // SendMessage's return value is dependent on the message send.  WM_CLOSE
                // return value just signify whether the WndProc handled the
                // message or not, so it is not interesting
#pragma warning disable 6523
                UnsafeNativeMethods.UnsafeSendMessage(CriticalHandle, WindowMessage.WM_CLOSE, new IntPtr(), new IntPtr());
#pragma warning enable 6523
            }
        }

        // NOTE:
        // We fire Closing and Closed envent even if the hwnd is not
        // created yet i.e. window is not shown.
        private void CloseWindowBeforeShow()
        {
            InternalDispose();

            // raise Closed event
            OnClosed(EventArgs.Empty);
        }

        internal bool IsSourceWindowNull
        {
            get
            {
                if ( _swh != null )
                {
                    return _swh.IsSourceWindowNull;
                }
                return true;
            }
        }

        internal bool IsCompositionTargetInvalid
        {
            get
            {
                if (_swh != null)
                {
                    return _swh.IsCompositionTargetInvalid;
                }
                return true;
            }
        }

        internal NativeMethods.RECT WorkAreaBoundsForNearestMonitor
        {
            get
            {
                Debug.Assert( _swh != null );
                return _swh.WorkAreaBoundsForNearestMonitor;
            }
        }

        internal Size WindowSize
        {
            get
            {
                Debug.Assert( _swh != null );
                return _swh.WindowSize;
            }
        }

        // This is currently exposed just for DRTs.
        // PLEASE NOTE THAT IF YOU ARE CALLING THIS WITHIN AVALON CODE - YOU ARE CALLING A SECURITY CRITICAL METHOD
        // YOU WILL HAVE TO WORK WITH THE SECURITY TEAM TO REVIEW YOUR USAGE
        internal HwndSource HwndSourceWindow
        {
            get
            {

                if ( _swh != null )
                    return _swh.HwndSourceWindow;
                else
                    return null;
            }
        }

        // WCP Window:  Define the dispose behavior for Window
        // We need to define the Dispose behavior for the Window class.  We used to
        // dispose off the object when the Window was closed; however, this does not
        // work for EmbeddedDialogs since we need to get the DialogResult after the
        // EmbeddedDialog is closed.  So, we changed this functionality for M6.
        // We need to define if and when we should dispose the Window object.

        // NOTE: added on 10/03/02
        // We should set our _hwndSource reference to null here.
        // Else, if the hwndSource is not null, users can call
        // public APIs that use hwndSource and it will fail

        /// <summary>
        ///     can be used by internal derived class
        /// </summary>
        private void InternalDispose()
        {
            _disposed = true;

            // UpdateWindowLists here instead of in WM_CLOSE for 2 reasons.
            // 1. WM_CLOSE is not fired for child window. An example would be RootBrowserWindow (bug 1754467).
            // 2. It is not fired as a result of calling Dispose directly on HwndSource (HwndSource distroy the window).
            UpdateWindowListsOnClose();

#if DISPOSE
            // detach all events
            Utilities.SafeDispose(ref _events);
#endif
            // NOTE:
            // This InternalDispose method is called while
            // processing WM_DESTROY msg. Once we're done
            // processing this msg, HwndWrapper does its processing.
            // We don't need to dispose _swh here b/c
            // HwndWrapper fires the hwndDisposed event while
            // processing WM_DESTROY msg. HwndSource listens to
            // this event and disposes itself i.e. the CompositionTarget etc.
            // If we call dispose here, HwndWrapper.Dispose sends
            // a WM_DESTROY msg and it is a duplicate msg.

            // When the window is closing, stop any deferred operations.
            if (_taskbarRetryTimer != null)
            {
                _taskbarRetryTimer.Stop();
                _taskbarRetryTimer = null;
            }

            try
            {
                ClearSourceWindow();

                Utilities.SafeDispose(ref _hiddenWindow);
                Utilities.SafeDispose(ref _defaultLargeIconHandle);
                Utilities.SafeDispose(ref _defaultSmallIconHandle);
                Utilities.SafeDispose(ref _currentLargeIconHandle);
                Utilities.SafeDispose(ref _currentSmallIconHandle);
                Utilities.SafeRelease(ref _taskbarList);
            }
            finally
            {
                _isClosing = false;
            }
        }

        /// <summary>
        ///     This is a callback called to set the window Visual. It is called after
        ///     the source window has been created.
        /// </summary>
        internal override void OnAncestorChanged()
        {
            base.OnAncestorChanged();
            if (Parent != null)
            {
                throw new InvalidOperationException(SR.Get(SRID.WindowMustBeRoot));
            }
        }

        /// <summary>
        ///     Initializes the _style and _styleEx bits.
        /// </summary>
        internal virtual void CreateAllStyle()
        {
            // we don't need to set to WS_OVERLAPPEDWINDOW since all the styles are set
            // manually depending on window properties.
            //
            // We always have the sysmenu
            // If the Window has a Caption, then this also
            // shows the icon and the close box

            _Style = NativeMethods.WS_CLIPCHILDREN | NativeMethods.WS_SYSMENU;
            _StyleEx = 0;

            CreateWindowStyle();
            CreateWindowState();

            // do all the other checks and update style bits

            // Visibility bits
            if ( _isVisible )
            {
                _Style |= NativeMethods.WS_VISIBLE;
            }

            SetTaskbarStatus();
            CreateTopmost();
            CreateResizibility();
            CreateRtl();
        }

        /// <summary>
        ///     Create the window
        ///     Virtual so that subclasses ( currently only RootBrowserWindow) - may assert for HwndSource creation.
        /// </summary>
        internal virtual void CreateSourceWindowDuringShow()
        {
            CreateSourceWindow(true);
        }

        /// <remarks>
        ///     This method is called in the following two cases:
        ///     1. As a result of calling Show on Window, Window.Show (Visibility = Visible).
        ///     2. WindowInteropHelper.EnsureHandle is called. Hwnd is created but not shown.
        ///        We only create hwnd. RootVisual is not set until Show.
        ///
        ///     This method does the following:
        ///     CalculateLocation of the window:
        ///         Calculates location of window. If either Top/Left is CW_USEDEFAULT,
        ///         we have to make sure that window displays on the screen. Details in
        ///         the methods.
        ///
        ///     Calculate size:
        ///         Calculates size. If either one of Width/Height is set, then we
        ///         create a window with both set to default and then resizing it
        ///         to the set value
        ///
        ///     Update Properties:
        ///         After the hwnd is created, we updated our property values for various
        ///         properties
        ///
        ///     Modify the message filter:
        ///         Elevated processes don't normally receive messages from unelevated processes.
        ///         For taskbar integration in Windows 7 we need to explicitly allow explorer to post
        ///         messages to this window..
        /// </remarks>
        /// <param name="duringShow">Specifies whether this method is called from Window.Show
        /// or WindowInteropHelper.EnsureHandle</param>
        internal void CreateSourceWindow(bool duringShow)
        {
            VerifyContextAndObjectState();
            VerifyCanShow();
            VerifyNotClosing();
            VerifyConsistencyWithAllowsTransparency();

            // We do not support create hwnd before shown for RBW.
            if (duringShow == false)
            {
                VerifyApiSupported();
            }

            // we need to cache initial requested top and left as the very first thing
            // since updating the styles etc (as for borderless case below) fires
            // WM_MOVE and update Top/Left.  Also, for RBW case, Top/Left is returned
            // as 0,0 since they are inconsequential.

            double requestedTop = 0;
            double requestedLeft = 0;
            double requestedWidth = 0;
            double requestedHeight = 0;

            GetRequestedDimensions(ref requestedLeft, ref requestedTop, ref requestedWidth, ref requestedHeight);

            // Initially, an HWND created in a Per-Monitor Aware process may not
            // have a reliable DPI. Instead, it would be associated with the monitor in which the screen point (0,0)
            // happens to be, but the screen point corresponding to a WPF Window's (requestedLeft, requestedTop) may
            // not map into that monitor. Our initial DeviceToLogicalUnits and LogicalToDeviceUnits transforms
            // would normally have to rely on the DPI obtained from this HWND, which may lead us to
            // an incorrect result when calculating screen points. To avoid this, we use a special heuristic here
            // to identify the (screenLeft, screenTop) initially without relying on the HWND, and then
            // supply it to HwndSourceParameters at the time of HWND creation. This would in turn set up the HWND
            // with the correct DPI from the start, and all our transformations would also follow suit.
            var topLeftPointHelper = new WindowStartupTopLeftPointHelper(new Point(requestedLeft, requestedTop));

            using (HwndStyleManager sm = HwndStyleManager.StartManaging(this, StyleFromHwnd, StyleExFromHwnd))
            {
                // set window style and styleEx bits
                // CreateAllStyle is internal virtual. RBW overrides it to set style to WS_CHILD and
                // set parent handle
                CreateAllStyle();

                // create the Win32 Window
                HwndSourceParameters param = CreateHwndSourceParameters();
                if (topLeftPointHelper.ScreenTopLeft.HasValue)
                {
                    var screenTopLeft = topLeftPointHelper.ScreenTopLeft.Value;
                    param.SetPosition((int)screenTopLeft.X, (int)screenTopLeft.Y);
                }

                // HwndSource disposes itself when HwndWrapper process the WM_DESTROY message.
                // Window sends WM_CLOSE (which in turn sends WM_DESTROY) to the hwnd when
                // Window.Close() is called.  Thus, this HwndSource created by window is always
                // disposed by HwndSource itself
#pragma warning disable 56518
                HwndSource source = new HwndSource(param);
#pragma warning enable 56518
                _swh = new SourceWindowHelper(source);
                source.SizeToContentChanged += new EventHandler(OnSourceSizeToContentChanged);

                // since we created the window with the style, mark
                // sm as not dirty so that we don't unneccessarialy update
                // the Win32 style bits.
                sm.Dirty = false;

                // NOTE:
                // HwndSource COULD ADD STYLE/STYLEEX BITS TO THE ONES SUPPLIED. TODAY,
                // HwndSource ADDS WS_CLIPCHILDREN TO THE STYLE WHICH MAY NOT
                // BE REFLECTED IN OUR CACHED STYLE BITS.  IF WE WERE
                // TO UPDATE THE WIN32 SYTLE, THE STYLES ADDED BY HWNDSOURCE
                // WOULD BE LOST.  IF IN THE FUTURE, WE UPDATE STYLE
                // BITS IN THIS METHOD, WE WOULD NEED TO FIRST GET THE
                // CURRENT STYLE FROM THE HWND USING:
                //
                // _Style = StyleFromHwnd;
                //

                // Since RBW cannot access WindowStyle, hence it overrides
                // this virtual and does nothing it it.
                CorrectStyleForBorderlessWindowCase();
} // end using StyleManager

            // We don't do anything that uses the Window styles below so we might as
            // well close the using so that we update the new style to the hwnd.
            // This change was made to solve a problem where SizeToContent.WidthAndHeight borderless
            // windows where actually bigger than the content size.  This was b/c of
            // the fact that we didn't update the style of the hwnd before setting
            // the RootVisual which inturn calls MeasureOverride on Window from
            // where we calculating the non-client area size using the stale
            // style bits from the hwnd.

            // Add Disposed event handler
            _swh.AddDisposedHandler ( new EventHandler(OnSourceWindowDisposed) );

            _hwndCreatedButNotShown = !duringShow;

            // Since this is only for Win7 taskbar integration, don't bother with this call unless we're on an appropriate OS.
            if (Utilities.IsOSWindows7OrNewer)
            {
                // In case the application is run elevated, explicitly allow WM_TASKBARBUTTONCREATED and WM_COMMAND
                // through the message filter.  This method call will fail if the application was started with
                // SECURITY_MANDATORY_LOW_RID, so don't propagate failed error codes.
                // It's not the end of the world if this fails: Shell integration simply won't work.
                MSGFLTINFO info;
                UnsafeNativeMethods.ChangeWindowMessageFilterEx(_swh.CriticalHandle, WM_TASKBARBUTTONCREATED, MSGFLT.ALLOW, out info);
                UnsafeNativeMethods.ChangeWindowMessageFilterEx(_swh.CriticalHandle, WindowMessage.WM_COMMAND, MSGFLT.ALLOW, out info);
            }

            // Sub classes can have different intialization. RBW does very minimalistic
            // stuff in its override
            SetupInitialState(requestedTop, requestedLeft, requestedWidth, requestedHeight);

            // Fire SourceInitialized event
            OnSourceInitialized(EventArgs.Empty);
        }

        internal virtual HwndSourceParameters CreateHwndSourceParameters()
        {
            HwndSourceParameters param = new HwndSourceParameters(Title, NativeMethods.CW_USEDEFAULT, NativeMethods.CW_USEDEFAULT);
            param.UsesPerPixelOpacity = AllowsTransparency;
            param.WindowStyle = _Style;
            param.ExtendedWindowStyle = _StyleEx;
            param.ParentWindow = _ownerHandle;
            param.AdjustSizingForNonClientArea = true;
            param.HwndSourceHook = new HwndSourceHook(WindowFilterMessage); // hook to process window messages
            return param;
        }

        private void OnSourceSizeToContentChanged(object sender, EventArgs args)
        {
            SizeToContent = HwndSourceSizeToContent;
        }

        internal virtual void CorrectStyleForBorderlessWindowCase()
        {
            // We create an OverLapped window for which user adds WS_CAPTION
            // to the passed in style.  If we were creating a borderless window,
            // remove WS_CAPTION from the hwnd.
            //
            // We should really be using WS_POPUP for borderless windows, but
            // there's a bug with default sizing where user creates the popup
            // window with 0,0 size.
            //

            using (HwndStyleManager sm = HwndStyleManager.StartManaging(this, StyleFromHwnd, StyleExFromHwnd))
            {
                if (WindowStyle == WindowStyle.None)
                {
                    _Style = _swh.StyleFromHwnd;
                    _Style &= ~NativeMethods.WS_CAPTION;
                }
            }
        }

        internal virtual void GetRequestedDimensions(ref double requestedLeft, ref double requestedTop, ref double requestedWidth, ref double requestedHeight)
        {
            requestedTop = this.Top;
            requestedLeft = this.Left;
            requestedWidth = this.Width;
            requestedHeight = this.Height;
        }

        internal virtual void SetupInitialState(double requestedTop, double requestedLeft, double requestedWidth, double requestedHeight)
        {
            // Push the current SizeToContent value to HwndSource after it's created. Initial sync up.
            HwndSourceSizeToContent = (SizeToContent) GetValue(SizeToContentProperty);
            UpdateIcon();

            NativeMethods.RECT rc = WindowBounds;
            Size sizeDeviceUnits = new Size(rc.right - rc.left, rc.bottom - rc.top);
            double xDeviceUnits = rc.left;
            double yDeviceUnits = rc.top;

            bool updateHwndPlacement = false;

            // This code is absolutely necessary here.  This is the initial sync up
            // for Left/Top.  We don't do this in WM_MOVE if sourceWindow is
            // null (it is null b/c the call to create HwndSource has not returned at the point)
            // and we are not listening to WM_CREATE either.  The reasons for not
            // doing so is b/c if sourceWindow is null, we can't get to the CompositionTarget
            // to convert b/w device and logical pixels
            Point currentLocationLogicalUnits = DeviceToLogicalUnits(new Point(xDeviceUnits, yDeviceUnits));

            // Update our current hwnd ActualTop/ActualLeft values
            // The _actualLeft and _actualTop need to be updated before we coerce Top and Left.
            // See CoerceTop for more info.
            _actualLeft = currentLocationLogicalUnits.X;
            _actualTop = currentLocationLogicalUnits.Y;
            // Coerce Top and Left value to be the intial value used in CreateWindowEx.
            try
            {
                _updateHwndLocation = false;
                CoerceValue(TopProperty);
                CoerceValue(LeftProperty);
            }
            finally
            {
                _updateHwndLocation = true;
            }

            Point requestedSizeDeviceUnits = LogicalToDeviceUnits(new Point(requestedWidth, requestedHeight));
            Point requestedLocationDeviceUnits = LogicalToDeviceUnits(new Point(requestedLeft, requestedTop));

            // if Width was specified and is not the same as the current width, then update it
            if ((!DoubleUtil.IsNaN(requestedWidth)) && (!DoubleUtil.AreClose(sizeDeviceUnits.Width, requestedSizeDeviceUnits.X)))
            {
                // at this stage, ActualWidth/Height is not set since
                // layout has not happened (it happens when we set the
                // RootVisual of the HwndSource)

                updateHwndPlacement = true;
                sizeDeviceUnits.Width = requestedSizeDeviceUnits.X;

                // SetWindowPlacement for Width/Height when Width/Height is set and WindowState is maximized/minimized.
                if (WindowState != WindowState.Normal)
                {
                    UpdateHwndRestoreBounds(requestedWidth, BoundsSpecified.Width);
                }
            }

            // if Height was specified and is not the same as the current height, then update it
            if (!DoubleUtil.IsNaN(requestedHeight) && (!DoubleUtil.AreClose(sizeDeviceUnits.Height, requestedSizeDeviceUnits.Y)))
            {
                // at this stage, ActualWidth/Height is not set since
                // layout has not happened (it happens when we set the
                // RootVisual of the HwndSource)

                updateHwndPlacement = true;
                sizeDeviceUnits.Height = requestedSizeDeviceUnits.Y;

                // SetWindowPlacement for Width/Height when Width/Height is set and WindowState is maximized/minimized.
                if (WindowState != WindowState.Normal)
                {
                    UpdateHwndRestoreBounds(requestedHeight, BoundsSpecified.Height);
                }
            }

            // if left was specified and is not the same as the current left, then update it
            if (!DoubleUtil.IsNaN(requestedLeft) && (!DoubleUtil.AreClose(xDeviceUnits, requestedLocationDeviceUnits.X)))
            {
                updateHwndPlacement = true;
                xDeviceUnits = requestedLocationDeviceUnits.X;

                // SetWindowPlacement for Top/Left when Top/Left is set and WindowState is maximized/minimized.
                if (WindowState != WindowState.Normal)
                {
                    UpdateHwndRestoreBounds(requestedLeft, BoundsSpecified.Left);
                }
            }

            // if top was specified and is not the same as the current top, then update it
            if (!DoubleUtil.IsNaN(requestedTop) && (!DoubleUtil.AreClose(yDeviceUnits, requestedLocationDeviceUnits.Y)))
            {
                updateHwndPlacement = true;
                yDeviceUnits = requestedLocationDeviceUnits.Y;

                // SetWindowPlacement for Top/Left when Top/Left is set and WindowState is maximized/minimized.
                if (WindowState != WindowState.Normal)
                {
                    UpdateHwndRestoreBounds(requestedTop, BoundsSpecified.Top);
                }
            }

            Point minSizeDeviceUnits = LogicalToDeviceUnits(new Point(MinWidth, MinHeight));
            Point maxSizeDeviceUnits = LogicalToDeviceUnits(new Point(MaxWidth, MaxHeight));

            // We need this here b/c when WM_GETMINMAXINFO is handled as a result of
            // creating the hwnd, _swh is null and thus we cannot get to the CompositionTarget
            // to convert b/w device and logical units.  Thus, once the hwnd is created, we
            // need to check to make sure that the hwnd is within the min/max boundaries.
            //
            // Here is idea it to detect if we are outside the min/max range and if yes, cause
            // a resize.  This resize will call WM_GETMINMAXINFO and that will further take
            // care of the bounds

            if (!Double.IsPositiveInfinity(maxSizeDeviceUnits.X) && (sizeDeviceUnits.Width > maxSizeDeviceUnits.X))
            {
                updateHwndPlacement = true;
                sizeDeviceUnits.Width = maxSizeDeviceUnits.X;
            }

            if (!Double.IsPositiveInfinity(minSizeDeviceUnits.Y) && (sizeDeviceUnits.Height > maxSizeDeviceUnits.Y))
            {
                updateHwndPlacement = true;
                sizeDeviceUnits.Height = maxSizeDeviceUnits.Y;
            }

            if (sizeDeviceUnits.Width < minSizeDeviceUnits.X)
            {
                updateHwndPlacement = true;
                sizeDeviceUnits.Width = minSizeDeviceUnits.X;
            }

            if (sizeDeviceUnits.Height < minSizeDeviceUnits.Y)
            {
                updateHwndPlacement = true;
                sizeDeviceUnits.Height = minSizeDeviceUnits.Y;
            }

            // Now that we know the window Width/Height/Left/Top, we want to calculate
            // the location based on WindowStartupLocation enum.  All inputs to
            // CalculateWindowLocation must be in DEVICE units.  It will return true,
            // if it updated theinput values of left, top.
            //
            updateHwndPlacement = (CalculateWindowLocation(ref xDeviceUnits, ref yDeviceUnits, sizeDeviceUnits)? true: updateHwndPlacement);

            // We need to update the hwnd size before we set RootVisual b/c setting RootVisual
            // results in a Measure/Arrange/Layout and we want to set the correct hwnd size
            // now so that layout happens at the correct size.
            //

            //
            // We are intentionally not merging this SetWindowPos with the one in SetRootVisualAndUpdateSTC. The reason is
            // that if Width or Height is set we initially create the hwnd with both as default win32 size
            // and later (above call to SetWindowPos) resizes it to reflect the specified Width or Height.
            // After this is done, we set RootVisual which results in a layout with the correct size of
            // the hwnd.
            //
            // If we merge the two SetWindowPos calls, then for the scenario where
            // Width or Height is set initially, we create the hwnd with win32 default size.  Setting RootVisual
            // would cause layout to happen at that default size.  Now, we resize the window to reflect the
            // specified size which will result in another layout.
            //
            // The third option is to set RootVisual after doing all resizes.  This is not possible to do for
            // the SizeToContent case since we don't know the size of hwnd until RootVisual is set and layout
            // has happened.
            //

            if (updateHwndPlacement == true)
            {
                if (WindowState == WindowState.Normal)
                {
                    UnsafeNativeMethods.SetWindowPos(new HandleRef(this, CriticalHandle),
                        new HandleRef(null, IntPtr.Zero),
                        DoubleUtil.DoubleToInt(xDeviceUnits),
                        DoubleUtil.DoubleToInt(yDeviceUnits),
                        DoubleUtil.DoubleToInt(sizeDeviceUnits.Width),
                        DoubleUtil.DoubleToInt(sizeDeviceUnits.Height),
                        NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE
                        );

                    // The value of Top and Left is affected by WindowState and WindowStartupLocation.
                    // we need to coerce Top and Left whenever these deciding factors change.
                    // More info in CoerceTop.
                    try
                    {
                        _updateHwndLocation = false;
                        _updateStartupLocation = true;
                        CoerceValue(TopProperty);
                        CoerceValue(LeftProperty);
                    }
                    finally
                    {
                        _updateHwndLocation = true;
                        _updateStartupLocation = false;
                    }
                }
            }

            // RootVisual is not set until Show.
            // Only set RootVisual when we are going to show the window.
            if (!HwndCreatedButNotShown)
            {
                SetRootVisualAndUpdateSTC();
            }
        }

        internal void SetRootVisual()
        {
            //FxCop: ConstructorsShouldNotCallBaseClassVirtualMethods::
            // System.Windows (presentationframework.dll 2 violation)
            //
            // We can't set IWS property in the cctor since it results
            // in calling some virtual.  So, as an alternative we set it
            // when content is changed and when we set the root visual.
            // We set it here, b/c once RootVisual is set, the visual tree
            // can/is created and visual children can query for inherited
            // IWS property.
            SetIWindowService();

            // set root visual  synchronously, shell request
            if ( IsSourceWindowNull == false )
            {
                _swh.RootVisual = this;
            }
        }

        internal void SetRootVisualAndUpdateSTC()
        {
            SetRootVisual();

            // We update size/location based on the value of SizeToContent after the RootVisual is set.
            // The world (size/location) could have changed after SetRootVisual.  Verify assumptions before moving on.
            // Specifically SetRootVisual may have initialized a Layout, and if an event handler Closed the Window
            // as a result then _swh (used by LogicalToDeviceUnits) is already disposed.
            if (!IsSourceWindowNull)
            {
                // if SizeToContent, we would need to set the location again if
                // WSL is CenterOwner or CenterScreen b/c size may have changed
                // after we set the RootVisual on HwndSource.
                // And if the hwnd is created but is not shown yet, we need to re-calculate the location again because
                // the value of the WindowStartupLocation can change after EnsureHandle is called.
                // We only update location if there is any change needed.
                if ((SizeToContent != SizeToContent.Manual) || (HwndCreatedButNotShown == true))
                {
                    NativeMethods.RECT rc = WindowBounds;
                    double xDeviceUnits = rc.left;
                    double yDeviceUnits = rc.top;

                    // inputs to CalculateWindowLocation must be in DEVICE units
                    Point newSizeDeviceUnits = LogicalToDeviceUnits(new Point(this.ActualWidth, this.ActualHeight));
                    if (CalculateWindowLocation(ref xDeviceUnits, ref yDeviceUnits, new Size(newSizeDeviceUnits.X, newSizeDeviceUnits.Y)))
                    {
                        if (WindowState == WindowState.Normal)
                        {
                            UnsafeNativeMethods.SetWindowPos(new HandleRef(this, CriticalHandle),
                                new HandleRef(null, IntPtr.Zero),
                                DoubleUtil.DoubleToInt(xDeviceUnits),
                                DoubleUtil.DoubleToInt(yDeviceUnits),
                                0,
                                0,
                                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE
                                );

                            // The value of Top and Left is affected by WindowState and WindowStartupLocation.
                            // we need to coerce Top and Left whenever these deciding factors change.
                            // More info in CoerceTop.
                            try
                            {
                                _updateHwndLocation = false;
                                _updateStartupLocation = true;
                                CoerceValue(TopProperty);
                                CoerceValue(LeftProperty);
                            }
                            finally
                            {
                                _updateHwndLocation = true;
                                _updateStartupLocation = false;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Create the style bits depending on the WindowStyle property
        /// </summary>
        /// <remarks>
        ///     This method does not clear the bits, so the style bits should be cleared
        ///     before calling this method
        /// </remarks>
        private void CreateWindowStyle()
        {
            // Clear the style bits related to WindowStyle
            _Style &= ~NativeMethods.WS_CAPTION;
            _StyleEx &= ~(NativeMethods.WS_EX_CLIENTEDGE | NativeMethods.WS_EX_TOOLWINDOW);

            // WS_CAPTION == WS_BORDER | WS_DLGFRAME (0x00C00000L)
            // Thus no need to set/clear WS_BORDER when we are
            // already seting/clearing WS_CAPTION
            switch (WindowStyle)
            {
                case WindowStyle.None:
                    _Style &= (~NativeMethods.WS_CAPTION);
                    break;

                case WindowStyle.SingleBorderWindow:
                    _Style |= NativeMethods.WS_CAPTION;
                    break;

                case WindowStyle.ThreeDBorderWindow:
                    _Style |= NativeMethods.WS_CAPTION;
                    _StyleEx |= NativeMethods.WS_EX_CLIENTEDGE;
                    break;

                case WindowStyle.ToolWindow:
                    _Style |= NativeMethods.WS_CAPTION;
                    _StyleEx |= NativeMethods.WS_EX_TOOLWINDOW;
                    break;
#if Never
                case WindowBorderStyle.Sizable:
                    _startSettings.Style |= NativeMethods.WS_BORDER | NativeMethods.WS_THICKFRAME;
                    break;

                case WindowBorderStyle.FixedDialog:
                    _startSettings.Style |= NativeMethods.WS_BORDER;
                    _startSettings.StyleEx |= NativeMethods.WS_EX_DLGMODALFRAME;
                    break;
                case WindowBorderStyle.SizableToolWindow:
                    _startSettings.Style |= NativeMethods.WS_BORDER | NativeMethods.WS_THICKFRAME;
                    _startSettings.StyleEx |= NativeMethods.WS_EX_TOOLWINDOW;
                    break;
#endif
            }
        }

        // called as a result of title property changing to propagate it to the hwnd
        internal virtual void UpdateTitle(string title)
        {
            // Adding check for IsCompositionTargetInvalid
            if ( IsSourceWindowNull == false && IsCompositionTargetInvalid == false)
            {
                UnsafeNativeMethods.SetWindowText(new HandleRef(this, CriticalHandle), title);
            }
        }

        // called as a result of Height/MinHeight/MaxHeight and Width/MinWidth/MaxWidth property changing to update the hwnd size
        private void UpdateHwndSizeOnWidthHeightChange(double widthLogicalUnits, double heightLogicalUnits)
        {
            if (!_inTrustedSubWindow)
            {
            }
            Debug.Assert( IsSourceWindowNull == false , "IsSourceWindowNull cannot be true when calling this function");

            Point ptDeviceUnits = LogicalToDeviceUnits(new Point(widthLogicalUnits, heightLogicalUnits));
            UnsafeNativeMethods.SetWindowPos(new HandleRef(this, CriticalHandle),
                        new HandleRef(null, IntPtr.Zero),
                        0,
                        0,
                        DoubleUtil.DoubleToInt(ptDeviceUnits.X),
                        DoubleUtil.DoubleToInt(ptDeviceUnits.Y),
                        NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE
                        );
        }

        // Activate or Deactivate window
        internal void HandleActivate(bool windowActivated)
        {
            //
            // This method is called by WM_ACTIVATE msg hander for the stand alone
            // window case.  WM_ACTIVATE is sent twice when activating a minimized
            // window by mouse click; once with window state minimized and then again
            // with window state normal.  Thus, we have the following if conditions
            // to fire Activated/Deactivated events only if we're activating/deactivating
            // the window and the window was previously deactivated/activated.
            //
            // Please look at (Activated event fires twice on window when
            // you minimize and restore a window) on this issue.
            //

            // Event handler exception continuality: if exception occurs in Activated/Deactivated event handlers, our state will not be
            // corrupted because the state related to Activated/Deactivated, IsActive is set before the event is fired.
            // Please check Event handler exception continuality if the logic changes.
            if ((windowActivated == true) && (IsActive == false))
            {
                SetValue(IsActivePropertyKey, BooleanBoxes.TrueBox);
                OnActivated(EventArgs.Empty);
            }
            else if ((windowActivated == false) && (IsActive == true))
            {
                SetValue(IsActivePropertyKey, BooleanBoxes.FalseBox);
                OnDeactivated(EventArgs.Empty);
            }
        }

        internal virtual void UpdateHeight(double newHeight)
        {
            if (WindowState == WindowState.Normal)
            {
                // We cannot save the current hwnd height in logical units in WmSizeChanged b/c the HwndSource
                // might not be completely created at that time (when we call new HwndSource from CreateSourceWindow)
                // and DeviceToLogicalUnits fails.  Thus we convert to logical units here before we call
                // UpdateHwndSizeonWidthHeightChange since that expects logical units.

                NativeMethods.RECT rc = WindowBounds;
                Point sizeLogicalUnits = DeviceToLogicalUnits(new Point(rc.Width, 0));
                UpdateHwndSizeOnWidthHeightChange(sizeLogicalUnits.X, newHeight);
            }
            else
            {
                UpdateHwndRestoreBounds(newHeight, BoundsSpecified.Height);
            }
        }

        internal virtual void UpdateWidth(double newWidth)
        {
            if (WindowState == WindowState.Normal)
            {
                // We cannot save the current hwnd width in logical units in WmSizeChanged b/c the HwndSource
                // might not be completely created at that time (when we call new HwndSource from CreateSourceWindow)
                // and DeviceToLogicalUnits fails.  Thus we convert to logical units here before we call
                // UpdateHwndSizeonWidthHeightChange since that expects logical units.

                NativeMethods.RECT rc = WindowBounds;
                Point sizeLogicalUnits = DeviceToLogicalUnits(new Point(0, rc.Height));
                UpdateHwndSizeOnWidthHeightChange(newWidth, sizeLogicalUnits.Y);
            }
            else
            {
                // set restore width
                UpdateHwndRestoreBounds(newWidth, BoundsSpecified.Width);
            }
        }

        internal virtual void VerifyApiSupported()
        {
            // don't do anything here since we allow this API in Window.
            // Subclasses can throw exception in their override if
            // they don't allow the api.
        }
        #endregion Internal Methods


        //----------------------------------------------
        //
        // Internal Properties
        //
        //----------------------------------------------
        #region Internal Properties

        internal bool HwndCreatedButNotShown
        {
            get
            {
                return _hwndCreatedButNotShown;
            }
        }

        internal bool IsDisposed
        {
            get
            {
                return _disposed;
            }
        }

        /// <summary>
        /// This tells whether user has set Visible or not. It's currently used in Application
        /// where if Visibility has not been set when the Window is navigated to, we set it to
        /// Visible
        /// </summary>
        internal bool IsVisibilitySet
        {
            get
            {
                VerifyContextAndObjectState();
                return _isVisibilitySet;
            }
        }

        /// <summary>
        ///     Exposes the hwnd of the window. This property is used by the WindowInteropHandler
        ///     class
        /// </summary>
        internal IntPtr CriticalHandle
        {
            get
            {
                VerifyContextAndObjectState();
                if (_swh != null)
                {
                    return _swh.CriticalHandle;
                }
                else
                return IntPtr.Zero;
            }
        }

        /// <summary>
        ///     Enables to get/set the owner handle for this window. This property is used by
        ///     the WindowInteropHelper class
        /// </summary>
        ///<remarks>
        ///     This API is currently not available for use in Internet Zone.
        ///</remarks>
        internal IntPtr OwnerHandle
        {
            get
            {

                VerifyContextAndObjectState();
                return _ownerHandle;
            }
            set
            {

                VerifyContextAndObjectState();

                if ( _showingAsDialog == true )
                {
                    throw new InvalidOperationException(SR.Get(SRID.CantSetOwnerAfterDialogIsShown));
                }

                SetOwnerHandle(value);
            }
        }

        /// <summary>
        ///     This is used by RBW to set the correct win32 style
        /// </summary>
        internal int Win32Style
        {
            get
            {
                VerifyContextAndObjectState();
                return _Style;
            }
            set
            {
                VerifyContextAndObjectState();

                // The reason this is not wrapped by a manager is that it should never
                // be invoked outside the scope on an already established manager.
                Debug.Assert(Manager != null, "HwndStyleManager must have a valid value here");
                _Style = value;
            }
        }

        internal int _Style
        {
            get
            {
                if (Manager != null)
                {
                    return _styleDoNotUse.Value;
}
                else if ( IsSourceWindowNull )
                {
                    return _styleDoNotUse.Value;
}
                else
                {
                    return _swh.StyleFromHwnd;
                }
            }
            set
            {
                _styleDoNotUse= new SecurityCriticalDataForSet<int>(value);
                Manager.Dirty = true;
            }
        }

        internal int _StyleEx
        {
            get
            {
                if (Manager != null)
                {
                    return _styleExDoNotUse.Value;
}
                else if (IsSourceWindowNull == true  )
                {
                    return _styleExDoNotUse.Value;
}
                else
                {
                    return _swh.StyleExFromHwnd;
                }
                }
            set
            {
                _styleExDoNotUse= new SecurityCriticalDataForSet<int>((int)value);
                Manager.Dirty = true;
            }
        }

        internal HwndStyleManager Manager
        {
            get { return _manager; }
            set { _manager = value; }
        }

        bool IWindowService.UserResized
        {
            get { return false; }
        }
        #endregion Internal Properties

        //----------------------------------------------
        //
        // Internal Fields
        //
        //----------------------------------------------
        #region Internal Fields

        /// <summary>
        /// DialogCancel Command. It closes window if it's dialog and return false as the dialog value.
        /// </summary>
        /// <remarks>
        /// Right now this is only used by Cancel Button to close the dialog.
        /// </remarks>
        internal static readonly RoutedCommand DialogCancelCommand = new RoutedCommand("DialogCancel", typeof(Window));

        #endregion Internal Fields


        //----------------------------------------------
        //
        // Internal Types
        //
        //----------------------------------------------
        #region Internal Types
        // similar to the one in FE except that it takes care of SizeToContent
        // while determining the min/max values for height and width.
        internal struct WindowMinMax
        {
            internal double minWidth;
            internal double maxWidth;
            internal double minHeight;
            internal double maxHeight;

            internal WindowMinMax(double minSize, double maxSize)
            {
                minWidth = minSize;
                maxWidth = maxSize;
                minHeight = minSize;
                maxHeight = maxSize;
            }
        }
        #endregion Internal Types

        //----------------------------------------------
        //
        // Private Methods
        //
        //----------------------------------------------
        #region Private Methods
        private Size MeasureOverrideHelper(Size constraint)
        {
            // need to handle infinity
            // return the entire client area of the window
            // Measure children properly.

            // Three primary cases
            //      1)  hwnd does not exist  -- return 0,0
            //      1a) CompositionTarget is invalid -- return 0,0
            //      2)  Child visual exists  -- we return child.DesiredSize + frame size
            //      3)  No Child visual      -- return the hwnd size (this should be the same
            //                                  as the one passed into MeasureOverride for our framework)

            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull || IsCompositionTargetInvalid)
            {
                // No need to use CompositionTarget.TransformFromDevice
                // since size is 0,0
                return new Size(0,0);
            }

            if (this.VisualChildrenCount > 0)
            {
                UIElement child = this.GetVisualChild(0) as UIElement;
                if (child != null) // UIElement children
                {
                    // Find out the size of the window frame x.
                    // (constraint - x) is the size we pass onto
                    // our child

                    Size frameSize = GetHwndNonClientAreaSizeInMeasureUnits();

                    // In some instances (constraint size - frame size) can be negative. One instance
                    // is when window is set to minimized before layout has happened.  Apparently, Win32
                    // gives a rect(-32000, -32000, -31840, -31975) for GetWindowRect when hwnd is
                    // minimized.  However, when we calculate the frame size we get width = 8 and
                    // height = 28!!!  Here, we will take the max of zero and the difference b/w the
                    // hwnd size and the frame size
                    //
                    // PS Windows OS Bug: 955861
                    Size childConstraint = new Size();
                    childConstraint.Width = ((constraint.Width == Double.PositiveInfinity) ? Double.PositiveInfinity : Math.Max(0.0, (constraint.Width - frameSize.Width)));
                    childConstraint.Height = ((constraint.Height == Double.PositiveInfinity) ? Double.PositiveInfinity : Math.Max(0.0, (constraint.Height - frameSize.Height)));

                    child.Measure(childConstraint);
                    Size childDesiredSize = child.DesiredSize;
                    return new Size(childDesiredSize.Width + frameSize.Width, childDesiredSize.Height + frameSize.Height);
                }
            }

            // if we reach here, we return the input size
            return _swh.GetSizeFromHwndInMeasureUnits();
        }

        // Similar logic as in FE.MinMax and takes care of max/min size allowed by win32 for the hwnd.  However, we
        // don't take Height/Width into consideration.  This is because of the following reasons:
        //
        // 1) Window Height/Width info doesn't matter here since we just need to ensure that the child element is
        //    layed out within the Max/Min restrictions of the window.  Window is layed out at the size sent into MO/AO
        //    and Height and Width of the window does not play a part MO/AO stage.
        //
        // 2) We had (FlowDocumentReader: When the maximise button is clicked on the window with FDR the
        //    content don't reflow to fill in the entire window) and the fix for that is to simply not use H/W here.
        //    GetWindowMinMax fixes the following bugs:
        //
        //    (Window content should respect Window's Max/Min size)
        //    (Wrong window actual size returned if Autosize window content is smaller than the actual
        //    window (seems to return content size as opposed to window size))
        //
        //  This method is called by both MeasureOverride( ) and WmGetMinMaxInfo( ).
        //  It will calculate a final Min/Max size in logic units for this HWND based on Win32 restricted value and
        //  current Min/Max setting in this instance.
        //
        internal virtual WindowMinMax GetWindowMinMax()
        {
            WindowMinMax mm = new WindowMinMax( );

            Invariant.Assert(IsCompositionTargetInvalid == false, "IsCompositionTargetInvalid is supposed to be false here");

            // convert the max/min size (taken in to account the hwnd size restrictions by win32) into logical units
            double maxWidthDeviceUnits = _trackMaxWidthDeviceUnits;
            double maxHeightDeviceUnits = _trackMaxHeightDeviceUnits;
            if (WindowState == WindowState.Maximized)
            {
                // On some systems, the trackMax size is a few pixels smaller than
                // the windowMax size.   Use the larger size for maximized windows.
                maxWidthDeviceUnits = Math.Max(_trackMaxWidthDeviceUnits, _windowMaxWidthDeviceUnits);
                maxHeightDeviceUnits = Math.Max(_trackMaxHeightDeviceUnits, _windowMaxHeightDeviceUnits);
            }

            Point maxSizeLogicalUnits = DeviceToLogicalUnits(new Point(maxWidthDeviceUnits, maxHeightDeviceUnits));
            Point minSizeLogicalUnits = DeviceToLogicalUnits(new Point(_trackMinWidthDeviceUnits, _trackMinHeightDeviceUnits));

            //
            // Get the final Min/Max Width
            //
            mm.minWidth = Math.Max(this.MinWidth, minSizeLogicalUnits.X);

            // Min's precedence is higher than Max; If Min is greater than Max, use Min.
            if (MinWidth > MaxWidth)
            {
                mm.maxWidth = Math.Min(MinWidth, maxSizeLogicalUnits.X);
            }
            else
            {
                if (!Double.IsPositiveInfinity(MaxWidth))
                {
                    mm.maxWidth = Math.Min(MaxWidth, maxSizeLogicalUnits.X);
                }
                else
                {
                    mm.maxWidth = maxSizeLogicalUnits.X;
                }
            }

            //
            // Get the final Min/Max Height
            //
            mm.minHeight = Math.Max(this.MinHeight, minSizeLogicalUnits.Y);

            // Min's precedence is higher than Max; If Min is greater than Max, use Min.
            if (MinHeight > MaxHeight)
            {
                mm.maxHeight = Math.Min(this.MinHeight, maxSizeLogicalUnits.Y);
            }
            else
            {
                if (!Double.IsPositiveInfinity(MaxHeight))
                {
                    mm.maxHeight = Math.Min(MaxHeight, maxSizeLogicalUnits.Y);
                }
                else
                {
                    mm.maxHeight = maxSizeLogicalUnits.Y;
                }
            }

            return mm;
        }

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            if (_postContentRenderedFromLoadedHandler == true)
            {
                PostContentRendered();
                _postContentRenderedFromLoadedHandler = false;
                this.Loaded -= new RoutedEventHandler(LoadedHandler);
            }
        }

        /// <remarks> Keep this method in sync with Frame.PostContentRendered(). </remarks>
        private void PostContentRendered()
        {
            // Post the firing of ContentRendered as Input priority work item so
            // that ContentRendered will be fired after render query empties.
            if (_contentRenderedCallback != null)
            {
                // Content was changed again before the previous rendering completed (or at least
                // before the Dispatcher got to Input priority callbacks).
                _contentRenderedCallback.Abort();
            }
            _contentRenderedCallback = Dispatcher.BeginInvoke(DispatcherPriority.Input,
                                   (DispatcherOperationCallback) delegate (object unused)
                                   {
                                       // Event handler exception continuality: there are no state related/depending on ContentRendered event.
                                       // If an exception occurs in event handler, our state will not be corrupted.
                                       // Please check event handler exception continuality if the logic changes.
                                       _contentRenderedCallback = null;
                                       OnContentRendered(EventArgs.Empty);
                                       return null;
                                   },
                                   this);
        }

        /// <summary>
        /// Ensure Dialog command is registered with the CommandManager
        /// </summary>
        private void EnsureDialogCommand()
        {
            // _dialogCommandAdded is a static variable, however, we're not synchronizing
            // access to it.  The reason is that, CommandManager is thread safe and according
            // to KiranKu we don't want to take the overhead of locking here.  For multiple
            // threaded cases, we could end up calling the CommandManager more than once but
            // KiranKu is okay with that perf hit in the corner case.
            if (!_dialogCommandAdded)
            {
                // Right now we only have DialogCancel Command, which closes window if it's dialog and return false as the dialog's result.
                CommandBinding binding = new CommandBinding(DialogCancelCommand);
                binding.Executed += new ExecutedRoutedEventHandler(OnDialogCommand);
                CommandManager.RegisterClassCommandBinding(typeof(Window), binding);

                _dialogCommandAdded = true;
            }
        }

        /// <summary>
        /// Dialog Command Execute handler
        /// Right now we only have DialogCancel Command, which closes window if it's dialog and return false for DialogResult.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="e"></param>
        private static void OnDialogCommand(object target, ExecutedRoutedEventArgs e)
        {
            //close dialog & return result
            Window w = target as Window;

            Debug.Assert(w != null, "Target must be of type Window.");
            w.OnDialogCancelCommand();
        }

        /// <summary>
        /// Close window if it's dialog and return false for DialogResult.
        /// </summary>
        private void OnDialogCancelCommand()
        {
            if (_showingAsDialog)
            {
                DialogResult = false;
            }
        }

        /// <summary>
        /// The callback function for EnumThreadWindows
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lparam"></param>
        /// <returns></returns>
        private bool ThreadWindowsCallback(IntPtr hWnd, IntPtr lparam)
        {
            Debug.Assert(_threadWindowHandles != null, "_threadWindowHandles must not be null at this point");

            // the dialog's hwnd has not been created yet when calling into this function.
            // so its hwnd won't be in the list.

            // We only do visible && enabled windows.
            // We need to check Visible because there might be hidden windows that do message looping,
            // We don't want to disable them.
            if (SafeNativeMethods.IsWindowVisible(new HandleRef(null, hWnd)) &&
                SafeNativeMethods.IsWindowEnabled(new HandleRef(null, hWnd)))
            {
                _threadWindowHandles.Add(hWnd);
            }

            return true;
        }

        /// <summary>
        /// Enables/disables all Windows on this thread
        /// </summary>
        /// <param name="state"></param>
        private void EnableThreadWindows(bool state)
        {
            Debug.Assert(_threadWindowHandles != null, "_threadWindowHandles must not be null at this point");

            for (int i = 0; i < _threadWindowHandles.Count; i++)
            {
                IntPtr hWnd = (IntPtr)_threadWindowHandles[i];

                if (UnsafeNativeMethods.IsWindow(new HandleRef(null, hWnd)))
                {
                    // Calls EnableWindow which returns the previous Window state
                    // (enable/disable) and we don't care about that here
#pragma warning disable 6523
                    UnsafeNativeMethods.EnableWindowNoThrow(new HandleRef(null, hWnd), state);
#pragma warning enable 6523

                }
            }

            // EnableThreadWindows is called with true only when dialog is going away.  Now
            // we've enabled the windows that we had earlier disabled; thus, disposing
            // _threadWindowHandles.
            if (state == true)
            {
                _threadWindowHandles = null;
            }
        }

        /// <summary>
        /// Intialization when Window is constructed
        /// </summary>
        ///     Initializes the Width/Height, Top/Left properties to use windows
        ///     default. Updates Application object properties if inside app.
        ///
        ///     Also, window style is set to WS_CHILD inside CreateSourceWindow
        ///     for browser hosted case
        private void Initialize()
        {

            //  this makes MeasureCore / ArrangeCore to defer to direct MeasureOverride and ArrangeOverride calls
            //  without reading Width / Height properties and modifying input constraint size parameter...
            BypassLayoutPolicies = true;

            // check if within an app && on the same thread
            if (IsInsideApp == true)
            {
                if (Application.Current.Dispatcher.Thread == Dispatcher.CurrentDispatcher.Thread)
                {
                    // add to window collection
                    // use internal version since we want to update the underlying collection
                    App.WindowsInternal.Add(this);
                    if (App.MainWindow == null)
                    {
                        App.MainWindow = this;
                    }
                }
                else
                {
                    App.NonAppWindowsInternal.Add(this);
                }
            }
        }


        internal void VerifyContextAndObjectState()
        {
            // Verify that we are executing on the right context
            VerifyAccess();

        // WCP Window:  Define the dispose behavior for Window
#if DISPOSE
            if (_disposed)
            {
                throw new ObjectDisposedException(null, SR.Get(SRID.WindowDisposed));
            }
#endif
        }

        private void VerifyCanShow()
        {
            if (_disposed == true)
            {
                throw new InvalidOperationException(SR.Get(SRID.ReshowNotAllowed));
            }
        }

        private void VerifyNotClosing()
        {
            if (_isClosing == true)
            {
                throw new InvalidOperationException(SR.Get(SRID.InvalidOperationDuringClosing));
            }

            if (IsSourceWindowNull == false && IsCompositionTargetInvalid == true)
            {
                throw new InvalidOperationException(SR.Get(SRID.InvalidCompositionTarget));
            }
        }

        private void VerifyHwndCreateShowState()
        {
            if (HwndCreatedButNotShown)
            {
                throw new InvalidOperationException(SR.Get(SRID.NotAllowedBeforeShow));
            }
        }

        /// <summary>
        ///     sets the IWindowService attached property
        /// </summary>
        private void SetIWindowService()
        {
            if (GetValue(IWindowServiceProperty) == null)
            {
                SetValue(IWindowServiceProperty, (IWindowService)this);
            }
        }

        IntPtr GetCurrentMonitorFromMousePosition()
        {
            // center on the screen on which the mouse is on
            NativeMethods.POINT pt = new NativeMethods.POINT();

            UnsafeNativeMethods.TryGetCursorPos(pt);

            NativeMethods.POINTSTRUCT ptStruct = new NativeMethods.POINTSTRUCT(pt.x, pt.y);
            return SafeNativeMethods.MonitorFromPoint(ptStruct, NativeMethods.MONITOR_DEFAULTTONEAREST);
        }

        // <summary>
        //     Calculates the window location based on the WindowStartupLocation property
        //     If values for CenterScreen, CenterOwner cannot be determined, then we return
        //     unmodified values.
        // </summary>
        // <remarks>
        //     This method can be called before or after the hwnd is created.  So, we have
        //     to accout for both scenarios.
        // </remarks>
        // <param name="left"></param>
        // <param name="top"></param>
        // <param name="currentSize"></param>
        private bool CalculateWindowLocation(ref double leftDeviceUnits, ref double topDeviceUnits, Size currentSizeDeviceUnits)
        {
            Debug.Assert(IsSourceWindowNull == false, "_swh should not be null here");
            double inLeft = leftDeviceUnits;
            double inTop = topDeviceUnits;

            switch (_windowStartupLocation)
            {
                case WindowStartupLocation.Manual:
                    break;
                case WindowStartupLocation.CenterScreen:
                    // NOTE:
                    // Which screen to center the
                    // window on?
                    //
                    // If Window has a parent handle, then center the Window on the
                    // same monitor as the parent hwnd.  If theres no parent hwnd,
                    // center the Window on the monitor where the mouse is currently
                    // on.
                    //
                    // The exception to this rule is when ShowInTaskbar is set to false
                    // and we parent to window to achieve that.  That's the reason for
                    // having the extra condition in the if statement below
                    //

                    IntPtr hMonitor = IntPtr.Zero;
                    if ((_ownerHandle == IntPtr.Zero) ||
                        ((_hiddenWindow != null) && (_hiddenWindow.Handle == _ownerHandle)))
                    {
                        hMonitor = GetCurrentMonitorFromMousePosition();
                    }
                    else
                    {
                        // have a parent hwnd; center on the screen on
                        // which our parent hwnd is.
                        hMonitor = MonitorFromWindow(_ownerHandle);
                    }

                    if (hMonitor != IntPtr.Zero)
                    {
                        CalculateCenterScreenPosition(hMonitor, currentSizeDeviceUnits, ref leftDeviceUnits, ref topDeviceUnits);
                    }
                    break;
                case WindowStartupLocation.CenterOwner:
                    Rect ownerRectDeviceUnits = Rect.Empty;

                    // If the owner is WPF window.
                    // The owner can be non-WPF window. It can be set via WindowInteropHelper.Owner.
                    if (CanCenterOverWPFOwner == true)
                    {
                        // If the owner is in a non-normal state use the screen bounds for centering the window.
                        // Top/Left/Width/Height reflect the restore bounds, so they can't be used in this scenario.
                        if (Owner.WindowState == WindowState.Maximized || Owner.WindowState == WindowState.Minimized)
                        {
                            goto case WindowStartupLocation.CenterScreen;
                        }

                        Point ownerSizeDeviceUnits;
                        // if owner hwnd is created, we use WindowSize to get its size. Note: we cannot use ActualWidth/Height here,
                        // because it is possible that the hwnd is created (WIH.EnsureHandle) but it is not shown yet; layout has not
                        // happen; ActualWidth/Height is not calculated yet.
                        // If the owner hwnd is not yet created, we use Owner.Width/Height.
                        if (Owner.CriticalHandle == IntPtr.Zero)
                        {
                            ownerSizeDeviceUnits = Owner.LogicalToDeviceUnits(new Point(Owner.Width, Owner.Height));
                        }
                        else
                        {
                            Size size = Owner.WindowSize;
                            ownerSizeDeviceUnits = new Point(size.Width, size.Height);
                        }

                        // A minimized window doesn't have valid Top,Left; that's why RestoreBounds.TopLeft is used.
                        Point ownerLocationDeviceUnits = Owner.LogicalToDeviceUnits(new Point(Owner.Left, Owner.Top));
                        ownerRectDeviceUnits = new Rect(ownerLocationDeviceUnits.X, ownerLocationDeviceUnits.Y,
                            ownerSizeDeviceUnits.X, ownerSizeDeviceUnits.Y);
                    }
                    else
                    {
                        // non-WPF owner
                        if ((_ownerHandle != IntPtr.Zero) && UnsafeNativeMethods.IsWindow(new HandleRef(null, _ownerHandle)))
                        {
                            ownerRectDeviceUnits = GetNormalRectDeviceUnits(_ownerHandle);
                        }
                    }

                    if (! ownerRectDeviceUnits.IsEmpty)
                    {
                        leftDeviceUnits = ownerRectDeviceUnits.X + ((ownerRectDeviceUnits.Width - currentSizeDeviceUnits.Width) / 2);
                        topDeviceUnits = ownerRectDeviceUnits.Y + ((ownerRectDeviceUnits.Height - currentSizeDeviceUnits.Height) / 2);

                        // (Window.CenterOwner doesn't make sure the window fits entirely on screen)
                        // Check the screen rect to make sure the window is shown on screen.
                        // It is the same as WinForms' behavior.
                        NativeMethods.RECT workAreaRectDeviceUnits = WorkAreaBoundsForHwnd(_ownerHandle);
                        leftDeviceUnits = Math.Min(leftDeviceUnits, workAreaRectDeviceUnits.right - currentSizeDeviceUnits.Width);
                        leftDeviceUnits = Math.Max(leftDeviceUnits, workAreaRectDeviceUnits.left);
                        topDeviceUnits = Math.Min(topDeviceUnits, workAreaRectDeviceUnits.bottom - currentSizeDeviceUnits.Height);
                        topDeviceUnits = Math.Max(topDeviceUnits, workAreaRectDeviceUnits.top);
                    }

                    break;
                default:
                    break;
            }
            return (!DoubleUtil.AreClose(inLeft, leftDeviceUnits) || !DoubleUtil.AreClose(inTop, topDeviceUnits));
        }

        private static NativeMethods.RECT WorkAreaBoundsForHwnd(IntPtr hwnd)
        {
            IntPtr hMonitor = MonitorFromWindow(hwnd);

            return WorkAreaBoundsForMointor(hMonitor);
        }

        private static NativeMethods.RECT WorkAreaBoundsForMointor(IntPtr hMonitor)
        {
            NativeMethods.MONITORINFOEX monitorInfo = new NativeMethods.MONITORINFOEX();

            Debug.Assert(hMonitor != IntPtr.Zero);
            SafeNativeMethods.GetMonitorInfo(new HandleRef(null, hMonitor), monitorInfo);

            return monitorInfo.rcWork;
        }

        private static IntPtr MonitorFromWindow(IntPtr hwnd)
        {
            IntPtr hMonitor = SafeNativeMethods.MonitorFromWindow(new HandleRef(null, hwnd), NativeMethods.MONITOR_DEFAULTTONEAREST);
            if (hMonitor == IntPtr.Zero)
            {
                throw new Win32Exception();
            }
            return hMonitor;
        }

        /// <summary>
        /// Calculates the left and right coordinates of a window
        /// when centered on a given monitor.
        /// </summary>
        /// <param name="hMonitor">Handle to the monitor to center the window on</param>
        /// <param name="currentSizeDeviceUnits">Size of the window, in device units</param>
        /// <param name="leftDeviceUnits">Receives the new left location in device units</param>
        /// <param name="topDeviceUnits">Receives the new top location in device units</param>
        internal static void CalculateCenterScreenPosition(IntPtr hMonitor, Size currentSizeDeviceUnits, ref double leftDeviceUnits, ref double topDeviceUnits)
        {
            NativeMethods.RECT workAreaRectDeviceUnits = WorkAreaBoundsForMointor(hMonitor);

            // We're using Width/Height here as opposed to ActualWidth/Height
            // as layout hasn't happened yet.
            double workAreaWidthDeviceUnits = workAreaRectDeviceUnits.right - workAreaRectDeviceUnits.left;
            double workAreaHeightDeviceUnits = workAreaRectDeviceUnits.bottom - workAreaRectDeviceUnits.top;

            Debug.Assert(workAreaWidthDeviceUnits >= 0, String.Format(CultureInfo.InvariantCulture, "workAreaWidth ({0})for monitor ({1}) is negative", hMonitor, workAreaWidthDeviceUnits));
            Debug.Assert(workAreaHeightDeviceUnits >= 0, String.Format(CultureInfo.InvariantCulture, "workAreaHeight ({0}) for monitor ({1}) is negative", hMonitor, workAreaHeightDeviceUnits));

            leftDeviceUnits = (workAreaRectDeviceUnits.left + ((workAreaWidthDeviceUnits - currentSizeDeviceUnits.Width) / 2));
            topDeviceUnits = (workAreaRectDeviceUnits.top + ((workAreaHeightDeviceUnits - currentSizeDeviceUnits.Height) / 2));
        }

        private bool CanCenterOverWPFOwner
        {
            get
            {
                Debug.Assert(IsSourceWindowNull == false, "_swh should not be null here");

                // if Owner is null, we cannot CenterOwner
                if (Owner == null)
                {
                    return false;
                }

                // if Owner._sourceWindow is null, and if Owner's Width or Height is not specified
                // then we cannot CenterOwner
                if (Owner.IsSourceWindowNull)
                {
                    if ((DoubleUtil.IsNaN(Owner.Width)) ||
                        (DoubleUtil.IsNaN(Owner.Height)))
                    {
                        return false;
                    }
                }

                // if Owner's Top or Left is not specified, we cannot CenterOwner
                if ((DoubleUtil.IsNaN(Owner.Left)) ||
                    (DoubleUtil.IsNaN(Owner.Top)))
                {
                    return false;
                }
                return true;
            }
        }

        private Rect GetNormalRectDeviceUnits(IntPtr hwndHandle)
        {
            int styleEx = UnsafeNativeMethods.GetWindowLong(new HandleRef(this, hwndHandle), NativeMethods.GWL_EXSTYLE);

            NativeMethods.WINDOWPLACEMENT wp = new NativeMethods.WINDOWPLACEMENT();
            wp.length = Marshal.SizeOf(typeof(NativeMethods.WINDOWPLACEMENT));
            UnsafeNativeMethods.GetWindowPlacement(new HandleRef(this, hwndHandle), ref wp);
            Point locationDeviceUnits = new Point(wp.rcNormalPosition_left, wp.rcNormalPosition_top);

            // GetWindowPlacement returns workarea co-ods for a top level window whose
            // WS_EX_TOOLWINDOW bit is clear.  If this bit is set, then the co-ods
            // returned are relative to the screen co-ods of the monitor.
            // TransfromWorkAreaScreenArea can transform a point from work area co-ods
            // to screen area co-ods and vice versa depending on TransformType value passed.
            // So, in our case, if the window is not a ToolWindow we want to transform
            // the point from work area co-ods to screen co-ods.
            if ((styleEx & NativeMethods.WS_EX_TOOLWINDOW) == 0)
            {
                locationDeviceUnits = TransformWorkAreaScreenArea(locationDeviceUnits, TransformType.WorkAreaToScreenArea);
            }

            Point sizeDeviceUnits = new Point(wp.rcNormalPosition_right - wp.rcNormalPosition_left,
                                              wp.rcNormalPosition_bottom - wp.rcNormalPosition_top);

            return new Rect(locationDeviceUnits.X, locationDeviceUnits.Y, sizeDeviceUnits.X, sizeDeviceUnits.Y);
        }

        private Rect GetNormalRectLogicalUnits(IntPtr hwndHandle)
        {
            Rect rectDeviceUnits = GetNormalRectDeviceUnits(hwndHandle);

            Point sizeLogicalUnits = DeviceToLogicalUnits(new Point(rectDeviceUnits.Width, rectDeviceUnits.Height));
            Point locationLogicalUnits = DeviceToLogicalUnits(new Point(rectDeviceUnits.X, rectDeviceUnits.Y));

            return new Rect(locationLogicalUnits.X, locationLogicalUnits.Y, sizeLogicalUnits.X, sizeLogicalUnits.Y);
        }

        /// <summary>
        ///     Update style bits for window state
        /// </summary>
        private void CreateWindowState()
        {
            switch (WindowState)
            {
                case WindowState.Normal:
                    break;
                case WindowState.Maximized:
                    _Style |= NativeMethods.WS_MAXIMIZE;
                    break;
                case WindowState.Minimized:
                    _Style |= NativeMethods.WS_MINIMIZE;
                    break;
#if THEATRE_FULLSCREEN
                case WindowState.Theatre:
                    throw new NotImplementedException(SR.Get(SRID.NotImplementedException));

                case WindowState.FullScreen:
                    throw new NotImplementedException(SR.Get(SRID.NotImplementedException));
#endif //THEATRE_FULLSCREEN
            }
        }

        /// <summary>
        ///     creates topmost window
        /// </summary>
        private void CreateTopmost()
        {
            // check for topmost
            if ( Topmost )
            {
                _StyleEx |= NativeMethods.WS_EX_TOPMOST;
            }
            else
            {
                _StyleEx &= ~NativeMethods.WS_EX_TOPMOST;
            }
        }

        /// <summary>
        ///     set's resizibility for the window
        /// </summary>
        private void CreateResizibility()
        {
            _Style &= ~(NativeMethods.WS_THICKFRAME | NativeMethods.WS_MAXIMIZEBOX | NativeMethods.WS_MINIMIZEBOX);


            switch(ResizeMode)
            {
                case ResizeMode.NoResize:
                    break;
                case ResizeMode.CanMinimize:
                    _Style |= NativeMethods.WS_MINIMIZEBOX;
                    break;
                case ResizeMode.CanResize:
                case ResizeMode.CanResizeWithGrip:
                    _Style |= NativeMethods.WS_THICKFRAME | NativeMethods.WS_MAXIMIZEBOX | NativeMethods.WS_MINIMIZEBOX;
                    break;
                default:
                    Debug.Assert(false, "Invalid value for ResizeMode");
                    break;
            }
        }

        /// <summary>
        ///     Updates the window icon
        /// </summary>
        private void UpdateIcon()
        {
            // NOTE: Set Window.Icon = null causes NullReferenceException

            // if _icon is null, set _defaultLargeIconHandle and _defaultSmallIconHandle
            //  to the app icon (embedded in the exe).  _icon is used as window icon if it
            // is not null, else default icons from exe are used.  If both are null,
            // we set IntPtr.Zero as the icons for the window and Win32 defaults
            // come into play.
            //

            NativeMethods.IconHandle largeIconHandle;
            NativeMethods.IconHandle smallIconHandle;

            if (_icon != null)
            {
                IconHelper.GetIconHandlesFromImageSource(_icon, out largeIconHandle, out smallIconHandle);
            }
            else
            {
                // these both should be null before we've queried that exe for icons.
                // Once, we looked in the exe, these are no longer null and hence we
                // don't want to re-query in the exe anymore
                if (_defaultLargeIconHandle == null && _defaultSmallIconHandle == null)
                {
                     // sets the default small and large icon handles
                     IconHelper.GetDefaultIconHandles(out largeIconHandle, out smallIconHandle);
                    _defaultLargeIconHandle = largeIconHandle;
                    _defaultSmallIconHandle = smallIconHandle;
                }
                else
                {
                    largeIconHandle = _defaultLargeIconHandle;
                    smallIconHandle = _defaultSmallIconHandle;
                }
                // get default icons
            }

            // One of the steps necessary to hide a Window's taskbar button is to parent it off another HWND.
            // On XP when this is done the Window's alt-tab icon takes on the icon of the parent.  If we've created
            // a hidden parent window for the sake of ShowInTaskbar=false, then we need to keep its icon in sync
            // with the Window's.
            // On Vista this isn't necessary.

            // Make the array big enough to hold anything we need to update.
            // We'll keep track of the true count separately.
            var iconWindows = new HandleRef[]
            {
                new HandleRef(this, CriticalHandle),
                default(HandleRef)
            };
            int iconWindowsCount = 1;

            if (_hiddenWindow != null)
            {
                iconWindows[1] = new HandleRef(_hiddenWindow, _hiddenWindow.Handle);
                ++iconWindowsCount;
            }

            for (int i = 0; i < iconWindowsCount; ++i)
            {
                HandleRef hwnd = iconWindows[i];

                UnsafeNativeMethods.SendMessage(
                                        hwnd,
                                        WindowMessage.WM_SETICON,
                                        (IntPtr)NativeMethods.ICON_BIG,
                                        largeIconHandle);

                UnsafeNativeMethods.SendMessage(
                                        hwnd,
                                        WindowMessage.WM_SETICON,
                                        (IntPtr)NativeMethods.ICON_SMALL,
                                        smallIconHandle);
            }

            // dispose the previous icon handle if it's not the default handle
            if (_currentLargeIconHandle != null && _currentLargeIconHandle != _defaultLargeIconHandle)
            {
                _currentLargeIconHandle.Dispose();
            }

            if (_currentSmallIconHandle != null && _currentSmallIconHandle != _defaultSmallIconHandle)
            {
                _currentSmallIconHandle.Dispose();
            }

            _currentLargeIconHandle = largeIconHandle;
            _currentSmallIconHandle = smallIconHandle;
        }

        /// <summary>
        ///     Sets the parent hwnd (Owner) for this window. This could be called as a result of
        ///     setting one of the following
        ///
        ///         Window Owner {set;}
        ///         IntPtr OwnerHandle {set;}  -- this is used by WindowInteropHelper
        /// </summary>
        /// <param name="ownerHandle">IntPtr of the parent window</param>
        private void SetOwnerHandle(IntPtr ownerHandle)
        {
            // Note:
            // "SetWindowLong failed.  Error = 1400" appears in console when setting
            // Window.Owner to a Window hasn't been shown (chk build)
            //
            // SetWindowLong with GWL_HWNDPARENT fails if the new parent/owner is IntPtr.Zero
            // and the old owner was also IntPtr.Zero.  The if check below works around this
            // issue.
            if (_ownerHandle == ownerHandle && _ownerHandle == IntPtr.Zero)
            {
                return;
            }

            // If this call is removing the owner then we possibly need to reparent it with
            // the hidden window (ShowInTaskbar==false)
            // Once the hidden window is created we keep its icon in sync with the Window's.
            _ownerHandle = (IntPtr.Zero == ownerHandle && !ShowInTaskbar)
                ? EnsureHiddenWindow().Handle
                : ownerHandle;

            if (IsSourceWindowNull == false)
            {
                UnsafeNativeMethods.SetWindowLong(new HandleRef(null, CriticalHandle),
                    NativeMethods.GWL_HWNDPARENT,
                    _ownerHandle);

                // Update and reset the Owner Window if this is set through WindowInteropHelper.
                // We want to do this because once we are passed in the IntPtr for
                // the parent window, the Owner window is not the parent anymore.

                if ((_ownerWindow != null) && (_ownerWindow.CriticalHandle != _ownerHandle))
                {
                    _ownerWindow.OwnedWindowsInternal.Remove(this);
                    _ownerWindow = null;
                }
            }
        }

        /// <summary>
        ///     This is called back into when HwndSouce being used by this object is disposed, or if the
        ///     hwnd is destroyed.
        ///
        ///     When that happens on the same thread, we receive WM_CLOSE, WM_DESTROY and
        ///     everything works fine.  However, if that happened on another thread, we don't get
        ///     WM_CLOSE, WM_DESTROY until later, but we get this disposed callback and we
        ///     set dispose this object.  If we don't do this, there could be timing related issues if
        ///     we get called into after the HwndSource or hwnd has been disposed/destoyed but before
        ///     we receive WM_CLOSE, WM_DESTROY messages.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private void OnSourceWindowDisposed(object sender, EventArgs e)
        {
            if ( _disposed == false)
            {
                InternalDispose();
            }
        }

        /// <summary>
        ///     This is the hook to HwndSource that is called when window messages related to
        ///     this window occur. Currently, we listen to the following messages
        ///
        ///         WM_CLOSE        : We listen to this message in order to fire the Closing event.
        ///                           If the user cancels window closing, we set handled to true so
        ///                           that the DefWindowProc does not handle this message. Otherwise,
        ///                           we set handled to false.
        ///         WM_DESTROY      : We listen to this message in order to fire the Closed event.
        ///                           Handled is always set to false.
        ///         WM_ACTIVATE     : Used for Activated and deactivated events
        ///         WM_SIZE         : Used for SizeChanged, StateChanged events. Also, helps us keep our
        ///                           size updated
        ///         WM_MOVE:        : Used for location changed event and to keep our cached top/left
        ///                           updated
        ///         WM_GETMINMAXINFO: Used to enforce Max/MinHeight and Max/MinWidth
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        private IntPtr WindowFilterMessage( IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            IntPtr retInt = IntPtr.Zero ;
            WindowMessage message = (WindowMessage)msg;

            //
            // we need to process WM_GETMINMAXINFO before _swh is assigned to
            // b/c we want to store the max/min size allowed by win32 for the hwnd
            // which is later used in GetWindowMinMax.  WmGetMinMaxInfo can handle
            // _swh == null case.
            switch (message)
            {
                case WindowMessage.WM_GETMINMAXINFO:
                    handled = WmGetMinMaxInfo(lParam);
                    break;
                case WindowMessage.WM_SIZE:
                    handled = WmSizeChanged(wParam);
                    break;
            }

            if(_swh != null && _swh.CompositionTarget != null) // For extraneous messages during shutdown
            {
                // Can't case this in the switch statement since it's dynamically generated.
                if (message == WM_TASKBARBUTTONCREATED || message == WM_APPLYTASKBARITEMINFO)
                {
                    // Either Explorer's created a new button or it's time to try again.
                    // Stop deferring updates to the Taskbar.
                    if (_taskbarRetryTimer != null)
                    {
                        _taskbarRetryTimer.Stop();
                    }

                    // We'll receive WM_TASKBARBUTTONCREATED at times other than when the Window was created,
                    //    e.g. Explorer restarting, in response to ShowInTaskbar=true, etc.
                    // We'll receive WM_APPLYTASKBARITEMINFO when we'ved posted it to ourself after a failed ITaskbarList3 call.
                    ApplyTaskbarItemInfo();
                }
                else switch (message)
                {
                    case WindowMessage.WM_CLOSE:
                        handled = WmClose();
                        break;
                    case WindowMessage.WM_DESTROY:
                        handled = WmDestroy();
                        break;
                    case WindowMessage.WM_ACTIVATE:
                        handled = WmActivate(wParam);
                        break;
                    case WindowMessage.WM_MOVE:
                        handled = WmMoveChanged();
                        break;
                    case WindowMessage.WM_NCHITTEST:
                        handled = WmNcHitTest(lParam, ref retInt);
                        break;
                    case WindowMessage.WM_SHOWWINDOW:
                        handled = WmShowWindow(wParam, lParam);
                        break;
                    case WindowMessage.WM_COMMAND:
                        handled = WmCommand(wParam, lParam);
                        break;
                    default:
                        handled = false;
                        break;
                }
            }

            return retInt;
        }

        /// <summary>
        ///     Called on WM_COMMAND message.
        /// </summary>
        /// <returns>
        ///     True if we want to handle the command, false otherwise.
        /// </returns>
        private bool WmCommand(IntPtr wParam, IntPtr lParam)
        {
            if (NativeMethods.SignedHIWORD(wParam.ToInt32()) == THUMBBUTTON.THBN_CLICKED)
            {
                TaskbarItemInfo taskbar = TaskbarItemInfo;
                if (taskbar != null)
                {
                    int index = NativeMethods.SignedLOWORD(wParam.ToInt32());
                    if (index >= 0 && index < taskbar.ThumbButtonInfos.Count)
                    {
                        taskbar.ThumbButtonInfos[index].InvokeClick();
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Called on WM_CLOSE message. Fires the Closing event.
        /// </summary>
        /// <returns>
        ///     True if we want to stop the window from closing, else false
        /// </returns>
        private bool WmClose()
        {
            // For WS_CHILD window, WM_SIZE, WM_MOVE (and maybe others) are called
            // synchronously from CreateWindowEx call and we run into issues if
            // _sourceWindow in null.  We only care to listen to WM_CREATE &
            // WM_GETMINMAXINFO synchronously from CreateWindowEx thus we want
            // to explicitly add the null check below at all other places.
            //
            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull || IsCompositionTargetInvalid)
            {
                return false;
            }

            // if DialogResult is set from within a Closing event then
            // the window is in the closing state.  In such a case, we
            // should not call Close() from DialogResult.set and thus
            // we have this variable.
            //
            // Note: Windows OS bug # 934500 Setting DialogResult
            // on the Closing EventHandler of a Dialog causes StackOverFlowException
            _isClosing = true;

            // Event handler exception continuality: if exception occurs in Closing event handler, the
            // cleanup action is to finish closing.
            CancelEventArgs e = new CancelEventArgs(false);
            try
            {
                OnClosing(e);
            }
            catch
            {
                CloseWindowFromWmClose();
                throw;
            }

            if (ShouldCloseWindow(e.Cancel))
            {
                CloseWindowFromWmClose();
                return false;
            }
            else
            {
                // close cancelled
                _isClosing = false;

                // Dialog does not close with ESC key after it has been cancelled
                //
                // Since closing is cancelled, DialogResult should be reset to null b/c
                // 1) DialogResult = true/false means that dialog has been accepeted/rejected (since dialog didn't
                //                   close (closing cancelled), DialogResult should be null)
                _dialogResult = null;

                return true;
            }
        }

        private void CloseWindowFromWmClose()
        {
            if (_showingAsDialog)
            {
                DoDialogHide();
            }

            // We should ClearRootVisual here instead of in InternalDispose. InternalDispose is called either as a result of HwndSource
            // disposing itself or it will try to dispose HwndSource. HwndSource will clear the root visual when it is disposed.
            ClearRootVisual();

            // We should also ClearHiddenWindow here, because in InternalDispose the window handle could be null if the dispose happens as
            // a result of HwndSource dispose. Our InternalDispose has been changed to handle reantrance, so the issue (bug 953988) described in
            // ClearHiddenWindowIfAny should not happen any more.
            ClearHiddenWindowIfAny();
        }

        private bool ShouldCloseWindow(bool cancelled)
        {
            // if shutdown Closing cannot be cancelled
            // if parent window is closing, child window Closing cannot be cancelled.
            return ((!cancelled) || (_appShuttingDown) || (_ignoreCancel));
        }

        private void DoDialogHide()
        {

            Debug.Assert(_showingAsDialog == true, "_showingAsDialog must be true when DoDialogHide is called");

            bool wasActive = false;

            //It's possible that _dispatcherFrame could be null at this time.
            //The scenario is: When showing the window as a modal dialog, the window Activated event is fired
            //before _dispatcherFrame is instantiated. In the Activated handler, if user closes the
            //window (setting DialogResult fires the WM_CLOSE event), the _dispatcherFrame is still null.
            //Bug 874463 addressed this.
            if (_dispatcherFrame != null)
            {
                // un block the push frame call
                _dispatcherFrame.Continue = false;
                _dispatcherFrame = null;
            }

            // Fix for Close Dialog Window should not return null
            //
            // The consensus here is that DialogResult should never be null when ShowDialog returns
            // As such, we coerce it to be false.  Furthermore, we don't use the DialogResult property
            // to update _dialogResult here since that does more than just updating the underlying
            // variable
            if (_dialogResult == null)
            {
                _dialogResult = false;
            }

            // clears _showingAsDialog
            _showingAsDialog = false;

            // enable previous window stuff goes here...
            wasActive = _swh.IsActiveWindow;

            // We assert here b/c I think _threadWindowHandles should never be null when we get
            // called in here.
            //
            // However, if inside ShowDialog we hit an exception after showing the dialog and the
            // exception handler is run, then _threadWindowHandles will be null here.
            //
            // Keeping this as assert.  If this turn out to be an over active assert, we'll switch to
            // an if condition.
            Debug.Assert(_threadWindowHandles != null, "_threadWindowHandles must not be null at this point");
            // reenable windows in the thread that were disabled
            EnableThreadWindows(true);

            // if dialog that is closing was active window and there was a previously active window,
            // set the active window.  The owner window may not be the previously active window. See DevDiv bug 122467 for details.
            // Furthermore, verify that _dialogPreviousActiveHandle is still a window b/c it
            // could have been destroyed by now by some other thread/codepath etc.
            // (BVT BLOCKER: System.ComponentModel.Win32Exception thrown when
            // trying to shutdown app inside a Dialog Window)
            if ((wasActive == true) &&
                (_dialogPreviousActiveHandle != IntPtr.Zero) &&
                (UnsafeNativeMethods.IsWindow(new HandleRef(this, _dialogPreviousActiveHandle)) == true))
            {
                UnsafeNativeMethods.SetActiveWindow(new HandleRef(this, _dialogPreviousActiveHandle));
            }
            else
            {
                // WCP: Fix code for a rare scenario when dialog is going away

                // rare situation, figure this out later
                // talk to user team as to what we need to do here
            }
        }

        private void UpdateWindowListsOnClose()
        {
            // Close all owned windows
            // use internal version since we want to update the underlying collection
            WindowCollection ownedWindows = OwnedWindowsInternal;

            // need to discuss what the correct behavior is if one of the owned window throws exception
            // when closing.
            // Although we explicitly do this here, we don't really need to. (see PS # 857285)
            // We use a while loop like this because closing an owned window will modify the owned windows list.
            while (ownedWindows.Count > 0)
            {
                // if parent window is closing, child window Closing cannot be cancelled.
                ownedWindows[0].InternalClose(false, true /* Ignore cancel */);
            }

            Debug.Assert(ownedWindows.Count == 0, "All owned windows should now be gone");

            // Update OwnerWindows of our Owner
            if (IsOwnerNull == false)
            {
                // use internal version since we want to update the underlying collection
                Owner.OwnedWindowsInternal.Remove(this);
            }

            if (this.IsInsideApp)
            {
                if (Application.Current.Dispatcher.Thread == Dispatcher.CurrentDispatcher.Thread)
                {
                    // use internal version since we want to update the underlying collection
                    App.WindowsInternal.Remove(this);

                    // Check to see if app should shut down--this behavior really belongs in Application
                    if (_appShuttingDown == false)
                    {
                        // If this is the last window that's closing and shutdownmode is onlastwindowclose, or
                        // if this is the main window closing and shutdownmode is onmainwindowclose, shutdown
                        // the app
                        if (((App.Windows.Count == 0) && (App.ShutdownMode == ShutdownMode.OnLastWindowClose))
                         || ((App.MainWindow == this) && (App.ShutdownMode == ShutdownMode.OnMainWindowClose)))
                        {
                            App.CriticalShutdown(0);
                        }
                    }

                    TryClearingMainWindow();
                }
                else
                {
                    App.NonAppWindowsInternal.Remove(this);
                }
            }
}
        private bool  WmDestroy()
        {
            // For WS_CHILD window, WM_SIZE, WM_MOVE (and maybe others) are called
            // synchronously from CreateWindowEx call and we run into issues if
            // _sourceWindow in null.  We only care to listen to WM_CREATE &
            // WM_GETMINMAXINFO synchronously from CreateWindowEx thus we want
            // to explicitly add the null check below at all other places.
            if (IsSourceWindowNull)
            {
                return false;
            }

            if (_disposed == false)
            {
                InternalDispose();
            }

            // STRESS: System.NullReferenceException @ System.Windows.Window.DeviceToLogicalUnits
            //
            // We're intentionally not adding a check for IsCompositionTargetInvlaid here since we
            // feel that it is okay to fire the Closed event to notify the developer that window has
            // closed.

            // Event handler exception continuality: if exception occurs in Closed event handler, the
            // cleanup action is to finish closing.
            // raise Closed event
            OnClosed(EventArgs.Empty);

            return false;
        }

        private bool WmActivate( IntPtr wParam )
        {
            // For WS_CHILD window, WM_SIZE, WM_MOVE (and maybe others) are called
            // synchronously from CreateWindowEx call and we run into issues if
            // _sourceWindow in null.  We only care to listen to WM_CREATE &
            // WM_GETMINMAXINFO synchronously from CreateWindowEx thus we want
            // to explicitly add the null check below at all other places.
            //
            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull || IsCompositionTargetInvalid)
            {
                return false;
            }

            int loWord = NativeMethods.SignedLOWORD(wParam);
            bool windowActivated;


            if ( loWord == NativeMethods.WA_INACTIVE )
            {
                windowActivated = false;
            }
            else
            {
                windowActivated = true;
            }

            HandleActivate(windowActivated);

            return false;
        }

        // When the window is in a maximized or minimized state, we want the dimension properties
        // to reflect what it would be when it's restored.
        private void UpdateDimensionsToRestoreBounds()
        {
            Rect restoreRect = RestoreBounds;
            SetValue(LeftProperty, restoreRect.Left);
            SetValue(TopProperty, restoreRect.Top);
            SetValue(WidthProperty, restoreRect.Width);
            SetValue(HeightProperty, restoreRect.Height);
        }

        private bool WmSizeChanged(IntPtr wParam)
        {
            // For WS_CHILD window, WM_SIZE, WM_MOVE (and maybe others) are called
            // synchronously from CreateWindowEx call and we run into issues if
            // _sourceWindow in null.  We only care to listen to WM_CREATE &
            // WM_GETMINMAXINFO synchronously from CreateWindowEx thus we want
            // to explicitly add the null check below, at all other places.
            //
            // Adding IsCompositionTargetInvalid check here
            // Add this check here means that we won't fire WindowStateChanged event.
            // However, since the hwnd is going away anyways, not firing StateChanged
            // should not be a big deal.  The other side effect is that Width/Height DPs
            // won't be updated that should be fine too since window is going away.
            if (IsSourceWindowNull || IsCompositionTargetInvalid)
            {
                return false;
            }

            NativeMethods.RECT rc = WindowBounds;
            Point windowSize = new Point(rc.right - rc.left, rc.bottom - rc.top);
            Point ptLogicalUnits = DeviceToLogicalUnits(windowSize);

            try
            {
                _updateHwndSize = false;
                SetValue(FrameworkElement.WidthProperty, ptLogicalUnits.X);
                SetValue(FrameworkElement.HeightProperty, ptLogicalUnits.Y);
            }
            finally
            {
                _updateHwndSize = true;
            }

            // Update the Taskbar's data about this window.  This can intermittently fail if Explorer gets busy.
            // It's not worth handling failures here since if there is a clip we'll try again on the next resize
            // and it doesn't affect the window itself.
            HRESULT hr = UpdateTaskbarThumbnailClipping();

            switch ((int)wParam)
            {
                // We introduced _previousWindowState for the following scenario:
                //
                // win1.WindowState = WindowState.Maximized; // or stateA
                // ...
                // win1.WindowState = WindowState.Normal; // or stateB
                //
                // Developer sets WindowState to Maximized/Minimized then return to Normal.
                // OnStateChanged should be fired. However, We were comparing that to WindowState.Normal.
                // The value in property engine had been updated to Normal in the CLR setter, so OnStateChanged
                // was never fired.
                // This is addressed in bug 937458.
                //
                // Another reason for remembering the previous state is that
                // WM_SIZE is sent when the client area size of the hwnd changes.
                // Thus, if the hwnd is maximized, and the border style changes,
                // WM_SIZE is sent.  In such cases, we don't want to fire
                // StateChanged since the previous state was maximized too.
                // (NullReferenceException thrown when changing ResizeMode from CanMinimize
                // to CanResizeWithGrip inside StateChanged event handler).
                //
                // There are two places we update _previousWindowState.

                // WindowState can be changed as a result of the following two passes
                // 1. User interaction changes WindowState
                // 2. Developer programmatically changes WindowState
                // We update _previousWindowState at two places
                // 1. Before Hwnd is created, when developer programmatically changes WindowState, we update it when WindowState is invalidated.
                // 2. After Hwnd is created, we update it here because both passes comes here eventually.

                // Event handler exception continuality: if exception occurs in StateChanged event handler, our state will not be
                // corrupted because the state related to StateChanged, WindowStateProperty and _previousWindowState, are set before the event is fired.
                // Please check Event handler exception continuality if the logic changes.
                case NativeMethods.SIZE_MAXIMIZED:
                    if (_previousWindowState != WindowState.Maximized)
                    {
                        // Do not set local value unless it is from user interaction.
                        // User interaction is considered as the same pri as SetValue
                        // If WindowState is not the same as the current win32 value, it means the change is
                        // not from the DP system but from user interaction.
                        if (WindowState != WindowState.Maximized)
                        {
                            try
                            {
                                _updateHwndLocation = false;
                                _updateHwndSize = false;
                                UpdateDimensionsToRestoreBounds();
                            }
                            finally
                            {
                                _updateHwndSize = true;
                                _updateHwndLocation = true;
                            }
                            WindowState = WindowState.Maximized;
                        }
                        // The maximizing size we get from WM_GETMINMAXINFO is only valid for the primary monitor, if the primary monitor
                        // happens to be smaller than the secondary monitor, we may end up not maximizing correctly in the secondary monitor.
                        // Here we are sure that this size value is coming from the OS so it is safe to update our maximizing size.
                        _windowMaxWidthDeviceUnits = Math.Max(_windowMaxWidthDeviceUnits, windowSize.X);
                        _windowMaxHeightDeviceUnits = Math.Max(_windowMaxHeightDeviceUnits, windowSize.Y);

                        _previousWindowState = WindowState.Maximized;
                        OnStateChanged(EventArgs.Empty);
                    }
                    break;
                case NativeMethods.SIZE_MINIMIZED:
                    if (_previousWindowState != WindowState.Minimized)
                    {
                        if (WindowState != WindowState.Minimized)
                        {
                            try
                            {
                                _updateHwndSize = false;
                                _updateHwndLocation = false;
                                UpdateDimensionsToRestoreBounds();
                            }
                            finally
                            {
                                _updateHwndSize = true;
                                _updateHwndLocation = true;
                            }
                            WindowState = WindowState.Minimized;
                        }
                        _previousWindowState = WindowState.Minimized;
                        OnStateChanged(EventArgs.Empty);
                    }
                    break;
                case NativeMethods.SIZE_RESTORED:
                    if (_previousWindowState != WindowState.Normal)
                    {
                        if (WindowState != WindowState.Normal)
                        {
                            WindowState = WindowState.Normal;
                            WmMoveChangedHelper();
                        }
                        _previousWindowState = WindowState.Normal;
                        OnStateChanged(EventArgs.Empty);
                    }
                    break;
                default:
                    break;
            }

            // DON'T DO ANYTHIHG HERE SINCE WE FIRE STATECHANGED ABOVE.  USER CODE
            // RUNS IN STATE CHANGED AND WINDOW COULD HAVE BEEN CLOSED.  THUS, THE
            // STATE OF VARIABLES IS UNKNOWN.

            // HwndSource passes the win32 msgs to the public hookds first before
            // passing layout hook etc.  Thus we get the WM_SIZE msg before
            // layout filter has processed it and we were reporting stale
            // value for height/width.  Now, UIElement fires SizeChanged event.

            return false;
        }


        private bool WmMoveChanged()
        {
            // We want to listen to WM_MOVE synchronously since if a Window is
            // created minimized/maximized we get this message as a result of
            // calling CreateWindowEx.  Here, sourceWindow is null but we still
            // need to update _actual[Top/Left] to reflect the correct Top/Left
            // when Show() returns.  Thus we input hwnd and process top/left info
            //
            // We won't fire LocationChanged unless Show has returned meaning
            // IsSourceWindowNull is false.  Furthermore, LocationChanged is
            // fired only if the top/left values really changed

            // Adding IsCompositionTargetInvalid check here
            // Since hwnd is going away, not updating Top/Left DPs and not firing
            // LocationChanged should not matter.
            if (IsSourceWindowNull || IsCompositionTargetInvalid)
            {
                return false;
            }

            // the input lparam gives the client location,
            // so just call GetWindowRect for Left and Top.
            NativeMethods.RECT rc = WindowBounds;

            Point ptLogicalUnits = DeviceToLogicalUnits(new Point(rc.left, rc.top));


            if (!DoubleUtil.AreClose(_actualLeft, ptLogicalUnits.X) ||
                !DoubleUtil.AreClose(_actualTop, ptLogicalUnits.Y))
            {
                _actualLeft = ptLogicalUnits.X;
                _actualTop = ptLogicalUnits.Y;

                // In Window, WmMoveChangedHelper write the local value of Top/Left
                // (if necessary) or updates the property system values for
                // Top/Left by calling CoerceValue.  Furthermore, it fires the
                // LocationChanged event.  RBW overrides WmMoveChangedHelper to do
                // nothing as writing Top/Left is not supported for RBW and
                // LocationChanged is never fired for it either.
                WmMoveChangedHelper();

                //Invalidate AutomationPeer if it was created/used by Automation.
                //This will schedule a deferred update of bounding rectangle and
                //corresponding notification to the Automation layer.
                AutomationPeer peer = UIElementAutomationPeer.FromElement(this);
                if(peer != null)
                {
                    peer.InvalidatePeer();
                }
}

            return false;
        }

        // This method updates the Left/Top values and fires the location changed event.
        // It is virtual so that RBW can override it.
        internal virtual void WmMoveChangedHelper()
        {
            if (WindowState == WindowState.Normal)
            {
                try
                {
                    _updateHwndLocation = false;
                    SetValue(LeftProperty, _actualLeft);
                    SetValue(TopProperty, _actualTop);
                }
                finally
                {
                    _updateHwndLocation = true;
                }

                // Event handler exception continuality: if exception occurs in LocationChanged event handler, our state will not be
                // corrupted because the states related to LocationChanged, LeftProperty, TopProperty, Left and Top are set before the event is fired.
                // Please check event handler exception continuality if the logic changes.
                OnLocationChanged(EventArgs.Empty);
            }
        }


        private bool WmGetMinMaxInfo( IntPtr lParam )
        {
            NativeMethods.MINMAXINFO mmi = (NativeMethods.MINMAXINFO)UnsafeNativeMethods.PtrToStructure( lParam, typeof(NativeMethods.MINMAXINFO));

            //
            // For Bug 1380569: Window SizeToContent does not work after changing Max size properties
            //
            // When Min/Max size is changed in this Window instance, we want to make sure the correct
            // final Min/Max size is used to measure the window layout and notify the Win32 of the required
            // Min/Max size.
            //
            // This method is responsible to notify Win32 of the new Min/Max size.
            // MeasureOverride( ) is responisble to use the right Min/Max size to calculate the desired layout size.
            //
            // But only this method knows the Win32 restricted Min/Max value for the HWND when it responds to WM_GETMINMAXINFO message.
            //
            // To generate the right final Min/Max size value in both places ( here and MeasureOverride), we should
            // cache the Win32 restricted size here.
            //



            // We need to store the max/min size the hwnd can take.  This is used in GetWindowMinMax to determine
            // the size passed to children for their layout.  These are stored in device units here so later
            // we change them to logical units.  Fixes the following bugs:
            //
            // Wrong window actual size returned if Autosize window
            // content is smaller than the actual window (seems to return content
            // size as opposed to window size)
            //
            // Window content should respect Window's Max/Min size

            _trackMinWidthDeviceUnits = mmi.ptMinTrackSize.x;
            _trackMinHeightDeviceUnits = mmi.ptMinTrackSize.y;
            _trackMaxWidthDeviceUnits = mmi.ptMaxTrackSize.x;
            _trackMaxHeightDeviceUnits = mmi.ptMaxTrackSize.y;
            _windowMaxWidthDeviceUnits = mmi.ptMaxSize.x;
            _windowMaxHeightDeviceUnits = mmi.ptMaxSize.y;


            // if IsCompositionTargetInvalid is true, then it means that the CompositionTarget is not available.
            // This can happen at hwnd creation or destruction time.
            if (IsSourceWindowNull == false && IsCompositionTargetInvalid == false)
            {
                //
                // Get the final MinMax size for this HWND based on Win32 track value and Min/Max setting
                // in this instance.
                //
                WindowMinMax finalMinMax = GetWindowMinMax( );

                // The finalMinMax struct keeps the desired Min/Max size for this hwnd in Logic Units.
                Point minSizeDeviceUnits = LogicalToDeviceUnits(new Point(finalMinMax.minWidth, finalMinMax.minHeight));
                Point maxSizeDeviceUnits = LogicalToDeviceUnits(new Point(finalMinMax.maxWidth, finalMinMax.maxHeight));

                // Put the new value in mmi
                mmi.ptMinTrackSize.x = DoubleUtil.DoubleToInt(minSizeDeviceUnits.X);
                mmi.ptMinTrackSize.y = DoubleUtil.DoubleToInt(minSizeDeviceUnits.Y);

                mmi.ptMaxTrackSize.x = DoubleUtil.DoubleToInt(maxSizeDeviceUnits.X);
                mmi.ptMaxTrackSize.y = DoubleUtil.DoubleToInt(maxSizeDeviceUnits.Y);

                // Notify Win32 of the new Min/Max value for this HWND.

                Marshal.StructureToPtr(mmi, lParam, true);
            }

            return true;
        }

        private bool WmNcHitTest( IntPtr lParam, ref IntPtr refInt )
        {
            // For WS_CHILD window, WM_SIZE, WM_MOVE (and maybe others) are called
            // synchronously from CreateWindowEx call and we run into issues if
            // _sourceWindow in null.  We only care to listen to WM_CREATE &
            // WM_GETMINMAXINFO synchronously from CreateWindowEx thus we want
            // to explicitly add the null check below at all other places.
            //
            // Adding check for IsCompositionTargetInvalid
            // This can be true either at HwndSource creation time or when hwnd is going
            // away.
            if (IsSourceWindowNull || IsCompositionTargetInvalid)
            {
                return false;
            }

            // WmNcHitTest is necessary to enable ResizeGrip and it only
            // relevant for Window.  Doing this processing in a virtual so
            // that RBW can override it
            return HandleWmNcHitTestMsg(lParam, ref refInt);
        }

        internal virtual bool HandleWmNcHitTestMsg(IntPtr lParam, ref IntPtr refInt)
        {
            // We need to make sure that the calculation to find out
            // whether the mouse is over the ResizeGrip control works
            // for localized version of Windows (Arabic, Hebrew) which
            // uses RTL.  Also, we need to make sure it works for
            // multi-mon case

            if ((_resizeGripControl == null) || (ResizeMode != ResizeMode.CanResizeWithGrip))
            {
                return false;
            }

            // mouse position wrt to the left/top of the screen
            int x = NativeMethods.SignedLOWORD(lParam);
            int y = NativeMethods.SignedHIWORD(lParam);

            // Find the client area 0,0 of the Window wrt the screen
            // This will be used to transform the mouse position from screen co-od
            // to window's client area co-od.  We need this to be able to figure out
            // whether the mouse is currently over the resize grip control or not


            NativeMethods.POINT pt = GetPointRelativeToWindow(x, y);
            Point ptLogicalUnits = DeviceToLogicalUnits(new Point(pt.x, pt.y));

            // Now, (ptLogicalUnits.X, ptLogicalUnits.Y) is the mouse postion wrt to the
            // Window's client region.
            // The next step is to find out whether the mouse is on top of the
            // ResizeGrip control
            // For this we first need to find out mouse location wrt to the ResizeGrip
            // control and then check whether the mouse location is on the control
            // Conditions when mouse is on top of the control:
            //     x,y should be not be less than zero
            //     x,y should not be greater than RenderSize.Width and RenderSize.Height

            GeneralTransform transfromFromWindow = this.TransformToDescendant(_resizeGripControl);
            Point mousePositionWRTResizeGripControl = ptLogicalUnits;
            if (transfromFromWindow == null || transfromFromWindow.TryTransform(ptLogicalUnits, out mousePositionWRTResizeGripControl) == false)
            {
                return false;
            }

            // check if the mouse is outside the ResizeGripControl region
            if ((mousePositionWRTResizeGripControl.X < 0) ||
                (mousePositionWRTResizeGripControl.Y < 0 ) ||
                (mousePositionWRTResizeGripControl.X > _resizeGripControl.RenderSize.Width) ||
                (mousePositionWRTResizeGripControl.Y > _resizeGripControl.RenderSize.Height))
            {
                // mouse not over ResizeGripControl; just let the DefWndProc handle this
                return false;
            }

            if (FlowDirection == FlowDirection.RightToLeft)
            {
                refInt = new IntPtr(NativeMethods.HTBOTTOMLEFT);
            }
            else
            {
                refInt = new IntPtr(NativeMethods.HTBOTTOMRIGHT);
            }
            // we've handled the WM_NCHITTEST msg thus return true
            return true;
        }

        private bool WmShowWindow(IntPtr wParam, IntPtr lParam)
        {
            // For WS_CHILD window, WM_SIZE, WM_MOVE (and maybe others) are called
            // synchronously from CreateWindowEx call and we run into issues if
            // _sourceWindow in null.  We only care to listen to WM_CREATE &
            // WM_GETMINMAXINFO synchronously from CreateWindowEx thus we want
            // to explicitly add the null check below at all other places.
            //
            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull || IsCompositionTargetInvalid)
            {
                return false;
            }

            //lParam has five values: 0(programtically show/hide), 1(SW_PARENTCLOSING),
            //2(SW_OTHERMAXIMIZED), 3(SW_PARENTOPENING), 4(SW_OTHERRESTORED). We only care
            //about 1 and 3.
            switch (NativeMethods.IntPtrToInt32(lParam))
            {
                //The window's owner window is being minimized.
                //In Win32, when lParam is SW_PARENTCLOSING, wParam is false,
                //which means the window is being hidden.
                case NativeMethods.SW_PARENTCLOSING:
                    //This window will be hidden. Update _isVisible to reflect the
                    // new state.  Furthermore update visibility such that we
                    // do not call ShowHelper again and thus calling
                    // UpdateVisibilityProperty.
                    _isVisible = false;
                    UpdateVisibilityProperty(Visibility.Hidden);
                    break;

                //The window's owner window is being restored.
                //In Win32, when lParam is SW_PARENTOPENING, wParam is true,
                //which means the window is being shown.
                case NativeMethods.SW_PARENTOPENING:
                    //This window will be shown. Update _isVisible to reflect the
                    // new state.  Furthermore update visibility such that we
                    // do not call ShowHelper again and thus calling
                    // UpdateVisibilityProperty.
                    _isVisible = true;
                    UpdateVisibilityProperty(Visibility.Visible);
                    break;

                default:
                    break;
            }

            return false;
        }

        private static void _OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window w = (Window)d;
            Debug.Assert(w != null, "DependencyObject must be of type Window.");

            // We'll support most kinds of Images.  If it's not a BitmapFrame we'll rasterize it.
            w.OnIconChanged(e.NewValue as ImageSource);
        }


        private void OnIconChanged(ImageSource newIcon)
        {
            // No need to dispose previous _icon.
            // _icon is a ref to the ImageSource object
            // set by the developer.  Since the dev created
            // the ImageSource object it is his responsibility to
            // dispose it.
            _icon = newIcon;

            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull == false && IsCompositionTargetInvalid == false)
            {
                UpdateIcon();
            }
        }

        private static void _OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window w = (Window)d;
            Debug.Assert(w != null, "DependencyObject must be of type Window.");

            w.OnTitleChanged();
        }

        private static bool _ValidateText(object value)
        {
            return (value != null);
        }

        private void OnTitleChanged()
        {
            UpdateTitle(Title);
        }

        private static void _OnShowInTaskbarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window w = (Window)d;

            Debug.Assert(w != null, "DependencyObject must be of type Window.");
            w.OnShowInTaskbarChanged();
        }

        private void OnShowInTaskbarChanged()
        {
            // this call ends up throwing an exception if accessing
            // ShowInTaskbar is not allowed
            VerifyApiSupported();

            // There are 2 cases
            // Case 1 : being set before source window is created
            // Case 2 : being set after the source window is created
            // Case 3 : bet set when CompositionTarget is invalid meaning we're in a bad state

            // Adding check for IsCompositionTargetInvalid
            if ( IsSourceWindowNull == false && IsCompositionTargetInvalid == false)
            {
                bool fHideWindow = false;
                // Win32 bug. For ShowInTaskbar to change dynamically, we need to hide then show the window.
                // It is recommended to hide the window, chnage the style bits and then show it again.
                if (_isVisible)
                {
                    UnsafeNativeMethods.SetWindowPos(new HandleRef(this, CriticalHandle), NativeMethods.NullHandleRef, 0, 0, 0, 0,
                                       NativeMethods.SWP_NOMOVE |
                                       NativeMethods.SWP_NOSIZE |
                                       NativeMethods.SWP_NOZORDER |
                                       NativeMethods.SWP_NOACTIVATE |
                                       NativeMethods.SWP_NOSENDCHANGING |
                                       NativeMethods.SWP_HIDEWINDOW);
                    fHideWindow = true;
                }
                using (HwndStyleManager sm = HwndStyleManager.StartManaging(this, StyleFromHwnd, StyleExFromHwnd  ))
                {
                    SetTaskbarStatus();
                }
                // Use fHideWindow instead of _isVisible in case if we listen to HideWindow messages and update _isVisible value,
                // it won't break this code.
                if (fHideWindow)
                {
                    UnsafeNativeMethods.SetWindowPos(new HandleRef(this, CriticalHandle), NativeMethods.NullHandleRef, 0, 0, 0, 0,
                                                    NativeMethods.SWP_NOMOVE |
                                                    NativeMethods.SWP_NOSIZE |
                                                    NativeMethods.SWP_NOZORDER |
                                                    NativeMethods.SWP_NOACTIVATE |
                                                    NativeMethods.SWP_NOSENDCHANGING |
                                                    NativeMethods.SWP_SHOWWINDOW);
                }
            }
        }

        private static bool _ValidateWindowStateCallback(object value)
        {
            return IsValidWindowState((WindowState)value);
        }

        private static void _OnWindowStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window w = (Window)d;

            Debug.Assert(w != null, "DependencyObject must be of type Window.");
            w.OnWindowStateChanged((WindowState) e.NewValue);
        }

        private void OnWindowStateChanged(WindowState windowState)
        {

            //      WCP:  Window.Visible.Set : Make sure that window updates the styles
            //      when set while window is hidden
            //
            // We can only change the window state if window is currently
            // visible.  Win32 does not provide a way to change the window
            // state without showing the window.  Thus, for the case where
            // window is hidden, we defer the acutal state change till the
            // window is shown again.
            //
            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull == false && IsCompositionTargetInvalid == false)
            {
                if (_isVisible == true)
                {
                    HandleRef hr = new HandleRef(this,  CriticalHandle);

                    int style = _Style;

                    // Only call ShowWindow if window is in a different state
                    switch (windowState)
                    {
                        case WindowState.Normal:
                            if ((style & NativeMethods.WS_MAXIMIZE) == NativeMethods.WS_MAXIMIZE)
                            {
                                //
                                // The old behavior of this case is to restore the window using SW_RESTORE.
                                // With the ShowActivated property set to false we want this restore operation
                                // to take the current activation state into account when restoring the window.
                                //
                                if (ShowActivated || IsActive)
                                    UnsafeNativeMethods.ShowWindow(hr, NativeMethods.SW_RESTORE);
                                else
                                    UnsafeNativeMethods.ShowWindow(hr, NativeMethods.SW_SHOWNOACTIVATE);
                            }
                            else if ((style & NativeMethods.WS_MINIMIZE) == NativeMethods.WS_MINIMIZE)
                            {
                                //
                                // We query to WINDOWPLACEMENT to get an indication about the state before the
                                // minimize operation happened. If we were coming from a maximized state and now we
                                // switch to normal, we want activation to happen since the maximized state is always
                                // activated and transitioning from activated to non-activated would be weird.
                                //
                                NativeMethods.WINDOWPLACEMENT placement = new NativeMethods.WINDOWPLACEMENT();
                                placement.length = Marshal.SizeOf(placement);
                                UnsafeNativeMethods.GetWindowPlacement(hr, ref placement);

                                if ((placement.flags & NativeMethods.WPF_RESTORETOMAXIMIZED) == NativeMethods.WPF_RESTORETOMAXIMIZED)
                                    UnsafeNativeMethods.ShowWindow(hr, NativeMethods.SW_RESTORE);
                                else
                                {
                                    if (ShowActivated)
                                        UnsafeNativeMethods.ShowWindow(hr, NativeMethods.SW_RESTORE);
                                    else
                                        UnsafeNativeMethods.ShowWindow(hr, NativeMethods.SW_SHOWNOACTIVATE);
                                }
                            }
                            break;

                        case WindowState.Maximized:
                            if ((style & NativeMethods.WS_MAXIMIZE) != NativeMethods.WS_MAXIMIZE)
                            {
                                //
                                // The OS doesn't provide support for non-activated maximized windows.
                                //
                                UnsafeNativeMethods.ShowWindow(hr, NativeMethods.SW_MAXIMIZE);
                            }
                            break;

                        case WindowState.Minimized:
                            if ((style & NativeMethods.WS_MINIMIZE) != NativeMethods.WS_MINIMIZE)
                            {
                                //
                                // Historically, we used SW_MINIMIZE in here which activates the next top-level
                                // window in the Z order. Therefore, our ShowActivated property can't affect the
                                // minimized state since this would incur a breaking change requiring us to use
                                // SW_SHOWMINIMIZED instead in case ShowActivated is set to true (bw compat case).
                                //
                                UnsafeNativeMethods.ShowWindow(hr, NativeMethods.SW_MINIMIZE);
                            }
                            break;
                        // WCP: Window.WindowState should implement FullScreen and Theatre
                        // modes
                    }
                }
            }
            else
            {
                // WindowState can be changed as a result of the following two passes
                // 1. User interaction changes WindowState
                // 2. Developer programmatically changes WindowState
                // We update _previousWindowState at two places
                // 1. Before Hwnd is created, when developer programmatically changes WindowState, we update it here.
                // 2. After Hwnd is created, we update it when we get to WM_SIZE because both passes eventally meet there.
                _previousWindowState = windowState;
            }

            // The value of Top and Left is affected by WindowState and WindowStartupLocation.
            // we need to coerce Top and Left whenever these deciding factors change.
            // More info in CoerceTop.
            try
            {
                _updateHwndLocation = false;
                CoerceValue(TopProperty);
                CoerceValue(LeftProperty);
            }
            finally
            {
                _updateHwndLocation = true;
            }
        }

        private static bool _ValidateWindowStyleCallback(object value)
        {
            return IsValidWindowStyle((WindowStyle)value);
        }

        private static void _OnWindowStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window w = (Window)d;

            Debug.Assert(w != null, "DependencyObject must be of type Window.");
            w.OnWindowStyleChanged((WindowStyle) e.NewValue);
        }

        private void OnWindowStyleChanged(WindowStyle windowStyle)
        {
            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull == false && IsCompositionTargetInvalid == false)
            {
                using (HwndStyleManager sm = HwndStyleManager.StartManaging(this, StyleFromHwnd, StyleExFromHwnd ))
                {
                    CreateWindowStyle();
                }
            }
        }

        private static void _OnTopmostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window w = (Window)d;

            Debug.Assert(w != null, "DependencyObject must be of type Window.");
            w.OnTopmostChanged((bool) e.NewValue);
        }

        private void OnTopmostChanged(bool topmost)
        {

            // this call ends up throwing an exception if accessing
            // Topmost is not allowed
            VerifyApiSupported();

            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull == false  && IsCompositionTargetInvalid == false)
            {
                HandleRef hWnd = topmost ? NativeMethods.HWND_TOPMOST : NativeMethods.HWND_NOTOPMOST;
                UnsafeNativeMethods.SetWindowPos(new HandleRef(null, CriticalHandle),
                       hWnd,
                       0, 0, 0, 0,
                       NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
            }
        }

        private static object CoerceVisibility(DependencyObject d, object value)
        {
            Window w = (Window)d;

            Visibility newValue = (Visibility)value;
            if (newValue == Visibility.Visible)
            {
                w.VerifyCanShow();
                w.VerifyConsistencyWithAllowsTransparency();
                w.VerifyNotClosing();
                w.VerifyConsistencyWithShowActivated();
            }

            return value;
        }

        /// <summary>
        /// Called when VisiblityProperty is invalidated
        /// The actual window is created when the Visibility property is set
        /// to Visibility.Visible for the first time or when Show is called.
        /// For Window, Visibility.Visible means the Window is visible.
        /// Visibility.Hidden and Visibility.Collapsed mean the Window is not visible.
        /// Visibility.Hidden and Visibility.Collapsed are treated the same.
        /// </summary>
        private static void _OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window w = (Window)d;

            // Indicate Visibility has been set
            // This works fine because Window is always the root.  So Visibility property
            // would not be invalidated usless it is set to a value.  But if that changes,
            // we will get invalidation when Window it added to the tree and this would be broken.
            w._isVisibilitySet = true;

            // _visibilitySetInternally is used to identify a call from Show/Hide in
            // _OnVisibilityInvalidated callback.  If a call originates in Show/Hide,
            // we DO NOT want to do anything in the _OnVisibilityCallback since, we
            // synchronously call ShowHelper from Show/Hide
            if (w._visibilitySetInternally == true)
            {
                return;
            }

            bool visibilityValue = VisibilityToBool((Visibility) e.NewValue);

            w.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new DispatcherOperationCallback(w.ShowHelper),
                visibilityValue ? BooleanBoxes.TrueBox : BooleanBoxes.FalseBox);
        }

        private void SafeCreateWindowDuringShow()
        {
            //this is true the first time the window is created
            if (IsSourceWindowNull == true)
            {
                // _isVisible is false at this moment.  Thus CreateAllStyle
                // called by CreateSourceWindow does not set WS_VISIBLE style

                CreateSourceWindowDuringShow();
            }
                // If the hwnd has been created via WindowsInteropHelper.EnsureHandle,
                // we just need to hook up the RootVisual and update the size according to STC before Show.
            else if (HwndCreatedButNotShown)
            {
                SetRootVisualAndUpdateSTC();
                _hwndCreatedButNotShown = false;
            }
        }

        // We set/clear ShowKeyboardCue when Show(ShowDialog)/Hide is called.
        // We do not clear the state of ShowKeyboardCue when Window is closed.
        private void SetShowKeyboardCueState()
        {
            // set property on AccessKey control indicating the
            // invocation device
            if (KeyboardNavigation.IsKeyboardMostRecentInputDevice())
            {
                _previousKeyboardCuesProperty = (bool)GetValue(KeyboardNavigation.ShowKeyboardCuesProperty);
                SetValue(KeyboardNavigation.ShowKeyboardCuesProperty, BooleanBoxes.TrueBox);
                _resetKeyboardCuesProperty = true;
            }
        }

        // We set/clear ShowKeyboardCue when Show(ShowDialog)/Hide is called.
        // We do not clear the state of ShowKeyboardCue when Window is closed.
        private void ClearShowKeyboardCueState()
        {
            // if we set KeyboradNavigation.ShowKeyboardCuesProperty in ShowDialog,
            // set it to false here.
            if (_resetKeyboardCuesProperty == true)
            {
                _resetKeyboardCuesProperty = false;
                SetValue(KeyboardNavigation.ShowKeyboardCuesProperty, BooleanBoxes.Box(_previousKeyboardCuesProperty));
            }
        }

        private void UpdateVisibilityProperty(Visibility value)
        {
            // _visibilitySetInternally is used to identify a call (in _OnVisibilityInvalidated
            // callback) for updating the property value only and not changing the actual
            // visibility state of the hwnd.
            try
            {
                _visibilitySetInternally = true;
                SetValue(VisibilityProperty, value);
            }
            finally
            {
                _visibilitySetInternally = false;
            }
        }

        /// <summary>
        /// update _isVisible and call CreateSourceWindow if
        /// it's the first time window is set to Visibile
        /// </summary>
        private object ShowHelper(object booleanBox)
        {
            // Setting Visiblilty is async. When this is called from the async callback,
            // check whether the window is already closed.
            // E.g. window.Visibility = true;
            //      ...
            //      window.Close();
            // We should not do anything if the window is already closed.
            if (_disposed == true)
            {
                return null;
            }

            bool value = (bool) booleanBox;
            _isClosing = false;

            // (BVT Blocker: Invariant Assert when calling
            // Window.Show after setting Visibility=Hidden)

            // We should optimize for when visibilityValue == _isVisible only in ShowHelper
            // since this is called from a sync and async call.  We cannot optimize it there
            // since _isVisible may not reflect the exact state requested by the OM call.
            if (_isVisible == value)
            {
                return null;
            }

            // _isVisible should always be set after calling SafeCreateWindow, because
            // if exception occurs in Loading event (fired as a result of setting Visibility to visible) handler,
            // we set Visibility back to Collapsed. Otherwise we could get into a loop.
            if (value == true)
            {
                if (Application.IsShuttingDown)
                    return null;

                SetShowKeyboardCueState();

                SafeCreateWindowDuringShow();
                _isVisible = true;
            }
            else
            {

                ClearShowKeyboardCueState();

                if (_showingAsDialog == true)
                {
                    DoDialogHide();
                }
                _isVisible = false;
            }

            // we need this check here again, b/c creating the window fires the
            // Activted event and if user closes the window from it, then by
            // the time we get to this point _sourceWindow is already disposed.
            if ( IsSourceWindowNull == false )
            {

                // Specifying an Avalon app to start
                // maximized from a shortcut does not work.

                // ShowWindow MSDN documentation says that the first time ShowWindow
                // is called, nCmd passed in STARTUPINFO is used instead of the one
                // passed in via ShowWindow call. However, that is not the case.
                // ShowWindow implementation in user32 uses nCmd of STARTUPINFO only
                // if we pass SW_SHOW, SW_SHOWNORMAL, SW_SHOWDEFAULT to ShowWindow.
                // If anything else is passed, it does not use nCmd of STARTUPINFO.

                int nCmd = 0;
                if (value == true)
                {
                    // nCmdForShow access WindowState which is inaccessible for RBW.
                    // Thus doing so in a virtual that RBW overrides
                    nCmd = nCmdForShow();
                }
                else
                {
                    nCmd = NativeMethods.SW_HIDE;
                }

                // If this is a Topmost window, and the option is enabled to use SetWindowPos() when possible
                // for topmost windows, and this is SW_SHOW or SW_SHOWNA, then use SetWindowPos().  (Ideally
                // all SW_SHOW* options would use SetWindowPos, but (1) it is difficult to handle the maximize
                // and minimize options without ShowWindow, and (2) the z-order issue happens as soon as the
                // topmost window gets activated, which happens for SW_SHOWMAXIMIZED and SW_SHOWMINIMIZED.  So
                // to minimize complexity and risk those cases will continue to use ShowWindow.)
                //
                // Determine whether this is a Topmost window without calling
                // the Topmost property.  (This isn't allowed for xbap windows.)
                bool isTopmost = (bool)GetValue(TopmostProperty);
                if (isTopmost &&
                    FrameworkCompatibilityPreferences.GetUseSetWindowPosForTopmostWindows() &&
                    (nCmd == NativeMethods.SW_SHOW || nCmd == NativeMethods.SW_SHOWNA))
                {
                    int flags = (nCmd == NativeMethods.SW_SHOWNA) ? NativeMethods.SWP_NOACTIVATE : 0;
                    UnsafeNativeMethods.SetWindowPos(new HandleRef(this, CriticalHandle),
                        NativeMethods.HWND_TOPMOST,
                        0, 0, 0, 0,
                        flags | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOOWNERZORDER | NativeMethods.SWP_SHOWWINDOW);
                }
                else
                {
                    UnsafeNativeMethods.ShowWindow(new HandleRef(this, CriticalHandle), nCmd);
                }

                // We already did a ShowWindow upabove and then because of the using, we will flush which
                // will cause us to set the visibility. Ideally I would like to simply not have the ShowWindow
                // call above, *but* there is this SHOWNA stuff which is tied to Focus/Activation cleanup
                // scheduled for M8.
                //set the style
                SafeStyleSetter();
            }


            // dialog functionality; start dispatcher loop to block the call
            if ((_showingAsDialog == true) && (_isVisible == true))
            {
                //
                // Since we exited the Context, we need to make sure
                // we enter it before returning even if there is an
                // exception
                //
                Debug.Assert(_dispatcherFrame == null, "_dispatcherFrame must be null here");

                try
                {
                    // tell users we're going modal
                    ComponentDispatcher.PushModal();

                    _dispatcherFrame = new DispatcherFrame();
                    Dispatcher.PushFrame(_dispatcherFrame);
                }
                finally
                {
                    // tell users we're going non-modal
                    ComponentDispatcher.PopModal();
                }
            }

            return null;
        }

        internal virtual int nCmdForShow()
        {
            int nCmd = 0;
            switch(WindowState)
            {
                case WindowState.Maximized:
                    nCmd = NativeMethods.SW_SHOWMAXIMIZED; // The OS doesn't provide support for non-activated maximized windows.
                    break;
                case WindowState.Minimized:
                    nCmd = ShowActivated ? NativeMethods.SW_SHOWMINIMIZED : NativeMethods.SW_SHOWMINNOACTIVE;
                    break;
                 default:
                    nCmd = ShowActivated ? NativeMethods.SW_SHOW : NativeMethods.SW_SHOWNA;
                    break;
            }
            return nCmd;
        }

        private void SafeStyleSetter()
        {
            using (HwndStyleManager sm = HwndStyleManager.StartManaging(this, StyleFromHwnd, StyleExFromHwnd))
            {
                _Style = _isVisible ? (_Style | NativeMethods.WS_VISIBLE) : _Style;
            }
        }
        private static bool _ValidateSizeToContentCallback(object value)
        {
            return IsValidSizeToContent((SizeToContent)value);
        }

        /// <summary>
        /// SizeToContent property GetValue override
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        private static object _SizeToContentGetValueOverride(DependencyObject d)
        {
            Window w = d as Window;

            Debug.Assert(w != null, "DependencyObject must be of type Window.");
            return w.SizeToContent;
        }

        /// <summary>
        /// SizeToContent property invalidation callback
        /// </summary>
        private static void _OnSizeToContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window w = d as Window;

            Debug.Assert(w != null, "DependencyObject must be of type Window.");
            w.OnSizeToContentChanged((SizeToContent) e.NewValue);
        }

        private void OnSizeToContentChanged(SizeToContent sizeToContent)
        {
            // this call ends up throwing an exception if accessing
            // SizeToContent is not allowed
            VerifyApiSupported();

            // Update HwndSource's SizeToContent.
            // HwndSource will only update layout if the value has changed.
            //
            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull == false && IsCompositionTargetInvalid == false)
            {
                HwndSourceSizeToContent = sizeToContent;
            }
        }

        /// <summary>
        /// Validate [Max/Min]Width/Height and Top/Left value.
        /// </summary>
        /// Length takes Double; Win32 handles Int.
        /// We throw exception when the value goes below Int32Min and Int32Max.
        /// WorkItem 26263: ValidateValueCallback needs to move to PropertyMetadata so Window can
        /// add its own validation and validate before invalid value is set. Right now, we can only
        /// validate this in PropertyInalidatinonCallback because of this. (We couldn't make it virtual on
        /// FrameworkELement because ValidateValueCallback doesn't provide context. Work item 25275).
        private static void ValidateLengthForHeightWidth(double l)
        {
            //basically, NaN and PositiveInfinity are ok, and then anything
            //that can be converted to Int32
            if (!Double.IsPositiveInfinity(l) && !DoubleUtil.IsNaN(l) &&
                ((l > Int32.MaxValue) || (l < Int32.MinValue)))
            {
                throw new ArgumentException(SR.Get(SRID.ValueNotBetweenInt32MinMax, l));
            }
        }

        private static void ValidateTopLeft(double length)
        {
            // Values not allowed: PositiveInfinity, NegativeInfinity
            // and values that are beyond the range of Int32
            if (Double.IsPositiveInfinity(length) ||
                Double.IsNegativeInfinity(length))
            {
                throw new ArgumentException(SR.Get(SRID.InvalidValueForTopLeft, length));
            }

            if ((length > Int32.MaxValue) ||
                (length < Int32.MinValue))
            {
                throw new ArgumentException(SR.Get(SRID.ValueNotBetweenInt32MinMax, length));
            }
        }

        private static void _OnHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window w = d as Window;
            Debug.Assert(w != null, "d must be typeof Window");
            if (w._updateHwndSize)
            {
                w.OnHeightChanged((double) e.NewValue);
            }
        }

        private void OnHeightChanged(double height)
        {
            //  Move ValidateLengthForHeightWidth calls from property
            // invalidation callback to PropertyMetadata
            ValidateLengthForHeightWidth(height);

            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull == false && IsCompositionTargetInvalid == false && !DoubleUtil.IsNaN(height))
            {
                UpdateHeight(height);
            }
        }

        private static void _OnMinHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window w = d as Window;
            Debug.Assert(w != null, "d must be typeof Window");
            w.OnMinHeightChanged((double) e.NewValue);
        }

        private void OnMinHeightChanged(double minHeight)
        {
            // this call ends up throwing an exception if accessing
            // MinHeight is not allowed
            VerifyApiSupported();

            ValidateLengthForHeightWidth(minHeight);
            // Only trigger immediate size update when hwnd has been created and MinHeight is not Auto and MinHeight is
            // greater then current HWND height.
            // If hwnd hasn't been created, size will be controlled when it is created.
            // If MinHeight is Auto or ActualHeight is greater than MinHeight, there is no need update size of the window.
            //
            // Adding check for IsCompositionTargetInvalid
            if ((IsSourceWindowNull == false ) && (IsCompositionTargetInvalid == false))
            {
                NativeMethods.RECT rcHwnd = WindowBounds;
                Point logicalSize = DeviceToLogicalUnits(new Point(rcHwnd.Width, rcHwnd.Height));
                if (minHeight > logicalSize.Y)
                {
                    if (WindowState == WindowState.Normal)
                    {
                        UpdateHwndSizeOnWidthHeightChange(logicalSize.X, minHeight);
                    }
                    else
                    {
                        // no need to do anything.  When window is restored, we get WM_GETMINMAXINFO where
                        // we restrict the max/min size of the window to [Max/Min][Height/Width]
                    }
                }
            }
        }

        private static void _OnMaxHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window w = d as Window;
            Debug.Assert(w != null, "d must be typeof Window");
            w.OnMaxHeightChanged((double) e.NewValue);
        }

        private void OnMaxHeightChanged(double maxHeight)
        {
            // this call ends up throwing an exception if accessing
            // MaxHeight is not allowed
            VerifyApiSupported();

            ValidateLengthForHeightWidth(MaxHeight);

            // Only trigger immediate size update when hwnd has been created and MaxHeight is not Auto and
            // the HWND's height > MaxHeight
            //
            // Adding check for IsCompositionTargetInvalid
            if ((IsSourceWindowNull == false) && (IsCompositionTargetInvalid == false))
            {
                NativeMethods.RECT rcHwnd = WindowBounds;
                Point logicalSize = DeviceToLogicalUnits(new Point(rcHwnd.Width, rcHwnd.Height));
                if (maxHeight < logicalSize.Y)
                {
                    if (WindowState == WindowState.Normal)
                    {
                        UpdateHwndSizeOnWidthHeightChange(logicalSize.X, maxHeight);
                    }
                    else
                    {
                        // no need to do anything.  When window is restored, we get WM_GETMINMAXINFO where
                        // we restrict the max/min size of the window to [Max/Min][Height/Width]
                    }
                }
            }
        }

        private static void _OnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window w = d as Window;
            Debug.Assert(w != null, "d must be typeof Window");
            if (w._updateHwndSize)
            {
                w.OnWidthChanged((double) e.NewValue);
            }
        }

        private void OnWidthChanged(double width)
        {
            ValidateLengthForHeightWidth(width);

            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull == false && IsCompositionTargetInvalid == false && !DoubleUtil.IsNaN(width))
            {
                UpdateWidth(width);
            }
        }

        private static void _OnMinWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window w = d as Window;
            Debug.Assert(w != null, "d must be typeof Window");
            w.OnMinWidthChanged((double) e.NewValue);
        }

        private void OnMinWidthChanged(double minWidth)
        {
            // this call ends up throwing an exception if accessing
            // MinWidth is not allowed
            VerifyApiSupported();

            // Move ValidateLengthForHeightWidth calls from property
            // invalidation callback to PropertyMetadata
            ValidateLengthForHeightWidth(minWidth);
            // Only trigger immediate size update when hwnd has been created and MinWidth is not Auto and MinWidth is
            // greater then current ActualWidth.
            // If hwnd hasn't been created, size will be controlled when it is created.
            // If MinWidth is Auto or ActualWidth is greater than MinWidth, there is no need update size of the window.
            //
            // Adding check for IsCompositionTargetInvalid
            if ((IsSourceWindowNull == false) && (IsCompositionTargetInvalid == false))
            {
                NativeMethods.RECT rcHwnd = WindowBounds;
                Point logicalSize = DeviceToLogicalUnits(new Point(rcHwnd.Width, rcHwnd.Height));
                if (minWidth > logicalSize.X)
                {
                    if (WindowState == WindowState.Normal)
                    {
                        UpdateHwndSizeOnWidthHeightChange(minWidth, logicalSize.Y);
                    }
                    else
                    {
                        // no need to do anything.  When window is restored, we get WM_GETMINMAXINFO where
                        // we restrict the max/min size of the window to [Max/Min][Height/Width]
                    }
                }
            }
        }

        private static void _OnMaxWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window w = d as Window;
            Debug.Assert(w != null, "d must be typeof Window");
            w.OnMaxWidthChanged((double) e.NewValue);
        }

        private void OnMaxWidthChanged(double maxWidth)
        {
            // this call ends up throwing an exception if accessing
            // MaxWidth is not allowed
            VerifyApiSupported();

            ValidateLengthForHeightWidth(maxWidth);
            // Only trigger immediate size update when hwnd has been created and MaxWidth is not Auto and
            // ActualWidth > MaxWidth
            //
            // Adding check for IsCompositionTargetInvalid
            if ((IsSourceWindowNull == false ) && (IsCompositionTargetInvalid == false))
            {
                NativeMethods.RECT rcHwnd = WindowBounds;
                Point logicalSize = DeviceToLogicalUnits(new Point(rcHwnd.Width, rcHwnd.Height));
                if (maxWidth < logicalSize.X)
                {
                    if (WindowState == WindowState.Normal)
                    {
                        UpdateHwndSizeOnWidthHeightChange(maxWidth, logicalSize.Y);
                    }
                    else
                    {
                        // no need to do anything.  When window is restored, we get WM_GETMINMAXINFO where
                        // we restrict the max/min size of the window to [Max/Min][Height/Width]
                    }
                }
            }
        }

        // Updates the restore bounds of the hwnd based on BoundsSpecified enum values
        // OR-ing of BoundsSpecified enum is not supported.
        private void UpdateHwndRestoreBounds(double newValue, BoundsSpecified specifiedRestoreBounds)
        {

            NativeMethods.WINDOWPLACEMENT wp = new NativeMethods.WINDOWPLACEMENT();
            wp.length = Marshal.SizeOf(typeof(NativeMethods.WINDOWPLACEMENT));
            UnsafeNativeMethods.GetWindowPlacement(new HandleRef(this, CriticalHandle), ref wp);

            double convertedValue = (LogicalToDeviceUnits(new Point(newValue, 0))).X;
            switch (specifiedRestoreBounds)
            {
                case BoundsSpecified.Height:
                    wp.rcNormalPosition_bottom = wp.rcNormalPosition_top + DoubleUtil.DoubleToInt(convertedValue);
                    break;
                case BoundsSpecified.Width:
                    wp.rcNormalPosition_right = wp.rcNormalPosition_left + DoubleUtil.DoubleToInt(convertedValue);
                    break;
                case BoundsSpecified.Top:
                    // convert input value into work-area co-ods
                    double newTop = newValue;
                    // [Get/Set]WindowPlacement work with workarea co-ods for a top level
                    // window whose WS_EX_TOOLWINDOW bit is clear.  If this bit is set,
                    // then the co-ods are expected to be in screen co-ods of the monitor.
                    // TransfromWorkAreaScreenArea can transform a point from work area co-ods
                    // to screen area co-ods and vice versa depending on TransformType value passed.
                    // So, in our case, if the window is not a ToolWindow we want to transform
                    // the input value from screen co-ods to work area co-ods.
                    if ((StyleExFromHwnd & NativeMethods.WS_EX_TOOLWINDOW) == 0)
                    {
                        newTop = TransformWorkAreaScreenArea(new Point(0, newTop), TransformType.ScreenAreaToWorkArea).Y;
                    }
                    newTop = (LogicalToDeviceUnits(new Point(0, newTop))).Y;
                    int currentHeight = wp.rcNormalPosition_bottom - wp.rcNormalPosition_top;
                    wp.rcNormalPosition_top = DoubleUtil.DoubleToInt(newTop);
                    wp.rcNormalPosition_bottom = wp.rcNormalPosition_top + currentHeight;
                    break;
                case BoundsSpecified.Left:
                    // convert input value into work-area co-ods
                    double newLeft = newValue;
                    // [Get/Set]WindowPlacement work with workarea co-ods for a top level
                    // window whose WS_EX_TOOLWINDOW bit is clear.  If this bit is set,
                    // then the co-ods are expected to be in screen co-ods of the monitor.
                    // TransfromWorkAreaScreenArea can transform a point from work area co-ods
                    // to screen area co-ods and vice versa depending on TransformType value passed.

                    // So, in our case, if the window is not a ToolWindow we want to transform
                    // the input value from screen co-ods to work area co-ods.
                    if ((StyleExFromHwnd & NativeMethods.WS_EX_TOOLWINDOW) == 0)
                    {
                        newLeft = TransformWorkAreaScreenArea(new Point(newLeft, 0), TransformType.ScreenAreaToWorkArea).X;
                    }
                    newLeft = (LogicalToDeviceUnits(new Point(newLeft, 0))).X;
                    int currentWidth = wp.rcNormalPosition_right - wp.rcNormalPosition_left;
                    wp.rcNormalPosition_left = DoubleUtil.DoubleToInt(newLeft);
                    wp.rcNormalPosition_right = wp.rcNormalPosition_left + currentWidth;
                    break;
                default:
                    Debug.Assert(false, String.Format("specifiedRestoreBounds can't be {0}", specifiedRestoreBounds));
                    break;
            }

            // The showCmd flag retreived by GetWindowPlacement is SW_SHOWMAXIMIZED when the window is maximized.
            // If the window is minimized, showCmd is SW_SHOWMINIMIZED. Otherwise, it is SW_SHOWNORMAL, regardless
            // of the window's visibility.
            // SetWindowPlacement with SW_SHOWMAXIMIZED and SW_SHOWMINIMIZED will cause a hidden window to show.
            // To workaround this issue, we check whether the current window is hidden and set showCmd to SW_HIDE if it is.
            if (!this._isVisible)
            {
                wp.showCmd = NativeMethods.SW_HIDE;
            }


            UnsafeNativeMethods.SetWindowPlacement(new HandleRef(this, CriticalHandle), ref wp);
        }

        // deltaX = workAreaOriginValue - screenOriginValue (both in virtual co-ods)
        // X(screenAreaCood) = x(workAreaCood) + deltaX
        private Point TransformWorkAreaScreenArea(Point pt, TransformType transformType)
        {
            int deltaX = 0;
            int deltaY = 0;
            Point retPt;

            // First we get the monitor on which the window is on.  [Get/Set]WindowPlacement
            // co-ods are dependent on the monitor on which the window is on.
            IntPtr hMonitor = SafeNativeMethods.MonitorFromWindow(new HandleRef(this, CriticalHandle), NativeMethods.MONITOR_DEFAULTTONULL);

            if (hMonitor != IntPtr.Zero)
            {
                NativeMethods.MONITORINFOEX monitorInfo = new NativeMethods.MONITORINFOEX();
                monitorInfo.cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFOEX));

                SafeNativeMethods.GetMonitorInfo(new HandleRef(this, hMonitor), monitorInfo);
                NativeMethods.RECT workAreaRect = monitorInfo.rcWork;
                NativeMethods.RECT screenAreaRect = monitorInfo.rcMonitor;
                deltaX = workAreaRect.left - screenAreaRect.left;
                deltaY = workAreaRect.top - screenAreaRect.top;
            }

            if (transformType == TransformType.WorkAreaToScreenArea)
            {
                retPt = new Point(pt.X + deltaX, pt.Y + deltaY);
            }
            else
            {
                retPt = new Point(pt.X - deltaX, pt.Y - deltaY);
            }
            return retPt;
        }

        // The logic for coerce Top & Left
        // 1.   Before Window is first shown (w.IsSourceWindowNull == false)
        //      The value can come from 3 parties
        //          a. default
        //          b. SetValue
        //          c. Style, trigger...
        //      In all those 3 cases, we would like to pass the value because we don't have a
        //      different value from hwnd before it is shown.
        //
        // To understarnd the below 2 cases better, you will need to know the Window position API precedence as following:
        //      WindowState > WindowStartupLocation (only works the first time shown) > Top/Left
        //
        // 2.   During show
        //      There are 3 places that can coerce value during show. Right now they are all in
        //      SetupInitialState.
        //          a. After CreateWindowEx.
        //                  i. We should always update with the win32 default position when it is Nan.
        //                  ii. If WindowState is maxmized, we should always return from the hwnd (_actualTop/Left),
        //                  but update the hwnd restorebounds. So setting Top/Left WindowState to to Maxmized before show would
        //                  work for restorebounds (details in bug 1217802).
        //          b. If Top/Left is set and/or WindowStartupLocation is effective.
        //                  WindowState must be normal here because it takes precedence over WindowStartupLocation and Top/Left.
        //                  Since WindowStartupLocation only works the first time shown, we have a flag (_updateStartupLocation)
        //                  to help indicating that.
        //                  If StartupLocation is effective, we should return from the hwnd (_actualTop/Left).
        //          c. If SizeToContent is set and WindowStartupLocation is effective.
        //                  Same as b.
        //
        // 3.   After show
        //      a. User moves the Window.
        //          If user resize, we set local value (SetValue).
        //      b. User maximizes or minimizes. Or WindowState is changed programmtically
        //          We coerce Top and Left's value when WindowState is changed no matter whether it is
        //          from user action or programmtically.
        //      c. SetValue
        //
        //      For b and c, when WindowState is max or min, the hwnd value should always be returned.
        //      The new value should be set as restorebounds. Otherwise update with the new value.
        //
        // Note: as we can see from above logic, _actualTop/Left should always be updated with the current hwnd position before we
        // coerce Top and Left after hwnd is created.
        private static object CoerceTop(DependencyObject d, object value)
        {
            Window w = d as Window;

            // this call ends up throwing an exception if accessing Top
            // is not allowed
            w.VerifyApiSupported();

            double top = (double)value;

            // Move ValidateTopLeft calls from property
            // invalidation callback to PropertyMetadata
            ValidateTopLeft(top);

            if (w.IsSourceWindowNull || w.IsCompositionTargetInvalid)
            {
                return value;
            }

            if (double.IsNaN(top))
            {
                return w._actualTop;
            }

            if (w.WindowState != WindowState.Normal)
            {
                return value;
            }

            if (w._updateStartupLocation && (w.WindowStartupLocation != WindowStartupLocation.Manual))
            {
                return w._actualTop;
            }

            return value;
        }

        private static void _OnTopChanged (DependencyObject d , DependencyPropertyChangedEventArgs e)
        {
            Window w = d as Window;
            Debug.Assert( w != null, "DependencyObject must be of type Window." );

            if (w._updateHwndLocation)
            {
                w.OnTopChanged((double) e.NewValue);
            }
        }

        private void OnTopChanged(double newTop)
        {
            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull == false && IsCompositionTargetInvalid == false)
            {
                // NaN is special and indicates using Win32 default,
                // so we exclude that.
                if (DoubleUtil.IsNaN(newTop) == false)
                {
                    if (WindowState == WindowState.Normal)
                    {
                        Invariant.Assert(!Double.IsNaN(_actualLeft), "_actualLeft cannot be NaN after show");
                        UpdateHwndPositionOnTopLeftChange(Double.IsNaN(Left) ? _actualLeft : Left, newTop);
                    }
                    else
                    {
                        UpdateHwndRestoreBounds(newTop, BoundsSpecified.Top);
                    }
                }
            }
            else
            {
                // here the value is stored as measure units as newTop is in measure/logical units
                _actualTop = newTop;
            }
        }

        // Please see comments for CoerceTop.
        private static object CoerceLeft(DependencyObject d, object value)
        {
            Window w = d as Window;

            // this call ends up throwing an exception if setting property is not allowed
            w.VerifyApiSupported();

            double left = (double)value;

            // Move ValidateTopLeft calls from property
            // invalidation callback to PropertyMetadata
            ValidateTopLeft(left);

            if (w.IsSourceWindowNull || w.IsCompositionTargetInvalid)
            {
                return value;
            }

            if (double.IsNaN(left))
            {
                return w._actualLeft;
            }

            if (w.WindowState != WindowState.Normal)
            {
                return value;
            }

            if (w._updateStartupLocation && (w.WindowStartupLocation != WindowStartupLocation.Manual))
            {
                return w._actualLeft;
            }

            return value;
        }

        private static void _OnLeftChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window w = d as Window;
            Debug.Assert( w != null, "DependencyObject must be of type Window." );

            if (w._updateHwndLocation)
            {
                w.OnLeftChanged((double) e.NewValue);
            }
        }

        // _actualLeft is used to determine if LocationChanged should be fired in WMMoveChagnged.
        // We need it b/c we need to remember the last hwnd Left location to decide whether
        // we need to fire the event or not.  Why do we need to update here?  Well, for the following
        // scenario:
        //    Window w = new Window();
        //    w.Left = 100;
        //    w.WindowStyle = WindowStyle.None;
        //    w.Show();
        //
        //  In this case, we want to not fire LocationChanged from SetWindowPos called called
        //  from CorrectStyleForBorderlessWindowCase().
        //
        //  _actualLeft is update from the following places:
        //
        //
        // 1) In WM_MOVE handler
        //
        // 2) In OnLeftChanged for the case when the hwnd is not created yet.
        //
        // 3) SetupInitialState
        private void OnLeftChanged(double newLeft)
        {
            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull == false && IsCompositionTargetInvalid == false)
            {
                // NaN is special and indicates using Win32 default,
                // so we exclude that here.
                if (DoubleUtil.IsNaN(newLeft) == false)
                {
                    if (WindowState == WindowState.Normal)
                    {
                        Invariant.Assert(!Double.IsNaN(_actualTop), "_actualTop cannot be NaN after show");
                        UpdateHwndPositionOnTopLeftChange(newLeft, Double.IsNaN(Top) ? _actualTop : Top);
                    }
                    else
                    {
                        UpdateHwndRestoreBounds(newLeft, BoundsSpecified.Left);
                    }
                }
            }
            else
            {
                // here the value is stored as measure units as newLeft is in measure/logical units
                _actualLeft = newLeft;
            }
        }

        private void UpdateHwndPositionOnTopLeftChange(double leftLogicalUnits, double topLogicalUnits)
        {
            Debug.Assert( IsSourceWindowNull == false , "IsSourceWindowNull cannot be true when calling this function");

            Point ptDeviceUnits = LogicalToDeviceUnits(new Point(leftLogicalUnits, topLogicalUnits));

            UnsafeNativeMethods.SetWindowPos(new HandleRef(this, CriticalHandle),
                        new HandleRef(null, IntPtr.Zero),
                        DoubleUtil.DoubleToInt(ptDeviceUnits.X),
                        DoubleUtil.DoubleToInt(ptDeviceUnits.Y),
                        0,
                        0,
                        NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE
                        );
        }

        private static bool _ValidateResizeModeCallback(object value)
        {
            return IsValidResizeMode((ResizeMode)value);
        }

        private static void _OnResizeModeChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window w = d as Window;
            Debug.Assert( w != null, "DependencyObject must be of type Window." );

            w.OnResizeModeChanged();
        }

        private void OnResizeModeChanged()
        {
            // this call ends up throwing an exception if accessing
            // ResizeMode is not allowed
            VerifyApiSupported();

            // Adding check for IsCompositionTargetInvalid
            if ( IsSourceWindowNull == false && IsCompositionTargetInvalid == false)
            {
                using (HwndStyleManager sm = HwndStyleManager.StartManaging(this, StyleFromHwnd, StyleExFromHwnd  ))
                {
                    CreateResizibility();
                }
            }
        }

        private static object VerifyAccessCoercion(DependencyObject d, object value)
        {
            // this call ends up throwing an exception if setting property is not allowed
            ((Window)d).VerifyApiSupported();

            return value;
        }

        private static void _OnFlowDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Window w = d as Window;
            Debug.Assert(w != null, "DependencyObject must be of type Window.");

            w.OnFlowDirectionChanged();
        }

        /// <summary>
        ///     Right to Left
        /// </summary>
        private void OnFlowDirectionChanged()
        {
            // Adding check for IsCompositionTargetInvalid
            if (IsSourceWindowNull == false && IsCompositionTargetInvalid == false)
            {
                using (HwndStyleManager sm = HwndStyleManager.StartManaging(this, StyleFromHwnd, StyleExFromHwnd ))
                {
                    CreateRtl();
                }
            }
        }

        private static object CoerceRenderTransform(DependencyObject d, object value)
        {
            Transform renderTransformValue = (Transform)value;

            if ((value == null) ||
                (renderTransformValue != null && renderTransformValue.Value != null && renderTransformValue.Value.IsIdentity == true))
            {
                // setting this value is allowed.
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.TransformNotSupported));
            }

            return value;
        }

        private static void _OnRenderTransformChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        private static object CoerceClipToBounds(DependencyObject d, object value)
        {
            if ((bool)value != false)
            {
                throw new InvalidOperationException(SR.Get(SRID.ClipToBoundsNotSupported));
            }
            return value;
        }

        private static void _OnClipToBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }


        /// <summary>
        ///     Get or create the hidden window used for parenting when ShowInTaskbar == false.
        /// </summary>
        private HwndWrapper EnsureHiddenWindow()
        {
            if (_hiddenWindow == null)
            {
                _hiddenWindow = new HwndWrapper(
                    0, // classStyle
                    NativeMethods.WS_OVERLAPPEDWINDOW, // style
                    0, // exStyle
                    NativeMethods.CW_USEDEFAULT, // x
                    NativeMethods.CW_USEDEFAULT, // y
                    NativeMethods.CW_USEDEFAULT, // width
                    NativeMethods.CW_USEDEFAULT, // height
                    "Hidden Window", // name
                    IntPtr.Zero,
                    null
                );
            }

            return _hiddenWindow;
        }

        /// <summary>
        ///     sets taskbar status
        /// </summary>
        private void SetTaskbarStatus()
        {
            if (ShowInTaskbar == false) // don't show in taskbar
            {
                // To remove the taskbar button for this window it needs to have a non-null parent
                // (we'll create a hidden window for this purpose) and not have WS_EX_APPWINDOW

                // Create this now, even if we're not currently going to parent it.
                // If the Owner changes, we'll need to switch to this.
                EnsureHiddenWindow();

                // when this window is unowned
                if (_ownerHandle == IntPtr.Zero)
                {
                    SetOwnerHandle(_hiddenWindow.Handle);

                    // When we do this parenting trick on XP the alt-tab icon for the window takes on
                    // the parent's icon.  To keep things working right we need to apply Icon to the
                    // hidden window.  On Vista this is redundant.
                    if (!(IsSourceWindowNull || IsCompositionTargetInvalid))
                    {
                        UpdateIcon();
                    }
                }

                _StyleEx &= ~NativeMethods.WS_EX_APPWINDOW;
            }
            else // (ShowInTaskbar == true) show in task bar
            {
                _StyleEx |= NativeMethods.WS_EX_APPWINDOW;
                if( ! IsSourceWindowNull )
                {
                    if ((_hiddenWindow != null) && (_ownerHandle == _hiddenWindow.Handle))
                    {
                        SetOwnerHandle(IntPtr.Zero);
                    }
                }
            }
        }

        private void OnTaskbarRetryTimerTick(object sender, EventArgs e)
        {
            UnsafeNativeMethods.PostMessage(new HandleRef(this, CriticalHandle), WM_APPLYTASKBARITEMINFO, IntPtr.Zero, IntPtr.Zero);
        }

        private void ApplyTaskbarItemInfo()
        {
            if (!Utilities.IsOSWindows7OrNewer)
            {
                if (TraceShell.IsEnabled)
                {
                    TraceShell.Trace(TraceEventType.Warning, TraceShell.NotOnWindows7);
                }
                return;
            }

            // If the Window hasn't yet been shown then these calls will fail.
            // We'll try this again when the WM_TASKBARBUTTONCREATED message gets sent.
            if (IsSourceWindowNull || IsCompositionTargetInvalid)
            {
                return;
            }

            // If the taskbar has timed out in the last minute, don't try to do this again.
            if (_taskbarRetryTimer != null && _taskbarRetryTimer.IsEnabled)
            {
                return;
            }

            if (_taskbarList == null)
            {
                // If we don't have a handle and there isn't a TaskbarItemInfo, then we don't have anything to apply or remove.
                if (TaskbarItemInfo == null)
                {
                    return;
                }

                ITaskbarList taskbarList = null;
                try
                {
                    taskbarList = (ITaskbarList)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(CLSID.TaskbarList)));
                    taskbarList.HrInit();

                    // This QI will only work on Win7.
                    _taskbarList = (ITaskbarList3)taskbarList;
                    taskbarList = null;
}
                finally
                {
                    Utilities.SafeRelease(ref taskbarList);
                }

                // Don't use SystemParameters here.  We need pixels.
                _overlaySize = new Size(
                    UnsafeNativeMethods.GetSystemMetrics(SM.CXSMICON),
                    UnsafeNativeMethods.GetSystemMetrics(SM.CYSMICON));
                // IsEmpty is inclusive, in case either of these succeeded it would pass...
                Debug.Assert(0 != (int)_overlaySize.Width);
                Debug.Assert(0 != (int)_overlaySize.Height);

                // When we detect that Explorer is nonresponsive, use this to defer further attempts
                // at updating the TaskbarList until a specified amount of time (1 minute)
                if (_taskbarRetryTimer == null)
                {
                    _taskbarRetryTimer = new DispatcherTimer { Interval = new TimeSpan(0, 1, 0) };
                    // Explorer being non-responsive should be a transient issue.  Post back to apply the full TaskbarItemInfo.
                    _taskbarRetryTimer.Tick += OnTaskbarRetryTimerTick;
                }
            }

            // Apply (or clear) all aspects of the TaskbarItemInfo to this Window.
            HRESULT hr = HRESULT.S_OK;
            hr = RegisterTaskbarThumbButtons();

            if (hr.Succeeded)
            {
                // Updating the state will also update the value, so don't call UpdateTaskbarProgressValue.
                hr = UpdateTaskbarProgressState();
            }
            if (hr.Succeeded)
            {
                hr = UpdateTaskbarOverlay();
            }
            if (hr.Succeeded)
            {
                hr = UpdateTaskbarDescription();
            }
            if (hr.Succeeded)
            {
                hr = UpdateTaskbarThumbnailClipping();
            }
            if (hr.Succeeded)
            {
                hr = UpdateTaskbarThumbButtons();
            }

            // We'll asynchronously retry if we failed initially setting up the TaskbarItemInfo.
            HandleTaskbarListError(hr);
        }

        private HRESULT UpdateTaskbarProgressState()
        {
            Debug.Assert(null != _taskbarList);

            TaskbarItemInfo taskbarInfo = TaskbarItemInfo;
            TBPF tbpf = TBPF.NOPROGRESS;

            if (taskbarInfo != null)
            {
                switch (taskbarInfo.ProgressState)
                {
                    case TaskbarItemProgressState.Error:
                        tbpf = TBPF.ERROR;
                        break;
                    case TaskbarItemProgressState.Indeterminate:
                        tbpf = TBPF.INDETERMINATE;
                        break;
                    case TaskbarItemProgressState.None:
                        tbpf = TBPF.NOPROGRESS;
                        break;
                    case TaskbarItemProgressState.Normal:
                        tbpf = TBPF.NORMAL;
                        break;
                    case TaskbarItemProgressState.Paused:
                        tbpf = TBPF.PAUSED;
                        break;
                    default:
                        // The coercion should have caught this.
                        Debug.Assert(false);
                        tbpf = TBPF.NOPROGRESS;
                        break;
                }
            }

            HRESULT hr = _taskbarList.SetProgressState(CriticalHandle, tbpf);
            if (hr.Succeeded)
            {
                // Explicitly update this in case this property being set
                // to None or Indeterminate before made the value not update.
                hr = UpdateTaskbarProgressValue();
            }

            return hr;
        }

        private HRESULT UpdateTaskbarProgressValue()
        {
            Debug.Assert(null != _taskbarList);

            TaskbarItemInfo taskbarInfo = TaskbarItemInfo;

            // If we're not attached then don't modify this.
            if (taskbarInfo == null
                || taskbarInfo.ProgressState == TaskbarItemProgressState.None
                || taskbarInfo.ProgressState == TaskbarItemProgressState.Indeterminate)
            {
                return HRESULT.S_OK;
            }

            const ulong precisionValue = 1000;
            // The coercion should enforce this.
            Debug.Assert(0 <= taskbarInfo.ProgressValue && taskbarInfo.ProgressValue <= 1);

            var intValue = (ulong)(taskbarInfo.ProgressValue * precisionValue);
            return _taskbarList.SetProgressValue(CriticalHandle, intValue, precisionValue);
        }

        private HRESULT UpdateTaskbarOverlay()
        {
            Debug.Assert(null != _taskbarList);

            TaskbarItemInfo taskbarInfo = TaskbarItemInfo;
            NativeMethods.IconHandle hicon = NativeMethods.IconHandle.GetInvalidIcon();

            // The additional string at the end of SetOverlayIcon sets the accDescription
            // for screen readers.  We don't currently have a property that utilizes this.

            try
            {
                if (null != taskbarInfo && null != taskbarInfo.Overlay)
                {
                    hicon = IconHelper.CreateIconHandleFromImageSource(taskbarInfo.Overlay, _overlaySize);
                }

                return _taskbarList.SetOverlayIcon(CriticalHandle, hicon, null);
            }
            finally
            {
                hicon.Dispose();
            }
        }

        private HRESULT UpdateTaskbarDescription()
        {
            Debug.Assert(null != _taskbarList);

            TaskbarItemInfo taskbarInfo = TaskbarItemInfo;
            string tooltip = "";

            if (taskbarInfo != null)
            {
                tooltip = taskbarInfo.Description ?? "";
            }

            return _taskbarList.SetThumbnailTooltip(CriticalHandle, tooltip);
        }


        private HRESULT UpdateTaskbarThumbnailClipping()
        {
            // If TaskbarItemInfo isn't attached and active then there's nothing to do here.
            if (_taskbarList == null)
            {
                return HRESULT.S_OK;
            }

            if (_taskbarRetryTimer != null && _taskbarRetryTimer.IsEnabled)
            {
                // Explorer appears to be non-responsive.  Don't try this.
                return HRESULT.S_FALSE;
            }

            // Don't count on Window properties being in sync at the time of this call.
            // Just use native methods to check
            if (UnsafeNativeMethods.IsIconic(CriticalHandle))
            {
                // If the window is minimized then don't try to update the clip.
                return HRESULT.S_FALSE;
            }

            TaskbarItemInfo taskbarInfo = TaskbarItemInfo;
            NativeMethods.RefRECT interopRc = null;

            // If the taskbarInfo isn't available then remove any clipping.
            if (taskbarInfo != null && !taskbarInfo.ThumbnailClipMargin.IsZero)
            {
                Thickness margin = taskbarInfo.ThumbnailClipMargin;
                // Use the native GetClientRect.  Window.ActualWidth and .ActualHeight include the non-client areas.
                NativeMethods.RECT physicalClientRc = default(NativeMethods.RECT);
                SafeNativeMethods.GetClientRect(new HandleRef(this, CriticalHandle), ref physicalClientRc);
                var logicalClientRc = new Rect(
                    DeviceToLogicalUnits(new Point(physicalClientRc.left, physicalClientRc.top)),
                    DeviceToLogicalUnits(new Point(physicalClientRc.right, physicalClientRc.bottom)));

                // Crop the clipping to ensure that the margin doesn't overlap itself.
                if (margin.Left + margin.Right >= logicalClientRc.Width
                    || margin.Top + margin.Bottom >= logicalClientRc.Height)
                {
                    // Empty client rectangle is different than no clipping applied.
                    interopRc = new NativeMethods.RefRECT(0, 0, 0, 0);
                }
                else
                {
                    var physicalClip = new Rect(
                        LogicalToDeviceUnits(new Point(margin.Left, margin.Top)),
                        LogicalToDeviceUnits(new Point(logicalClientRc.Width - margin.Right, logicalClientRc.Height - margin.Bottom)));
                    interopRc = new NativeMethods.RefRECT((int)physicalClip.Left, (int)physicalClip.Top, (int)physicalClip.Right, (int)physicalClip.Bottom);
                }
            }

            return _taskbarList.SetThumbnailClip(CriticalHandle, interopRc);
        }

        private HRESULT RegisterTaskbarThumbButtons()
        {
            Debug.Assert(null != _taskbarList);

            // The ITaskbarList3 API requires that the maximum number of buttons to ever be used
            // are registered at the beginning.  Modifications can be made to this list later.
            var nativeButtons = new THUMBBUTTON[c_MaximumThumbButtons];

            for (int i = 0; i < c_MaximumThumbButtons; ++i)
            {
                nativeButtons[i] = new THUMBBUTTON
                {
                    iId = (uint)i,
                    dwFlags = THBF.NOBACKGROUND | THBF.DISABLED | THBF.HIDDEN | THBF.NONINTERACTIVE,
                    dwMask = THB.FLAGS | THB.ICON | THB.TOOLTIP
                };
            }

            // If this gets called (successfully) more than once it usually returns E_INVALIDARG.  It's not really
            // a failure and we potentially want to retry this operation.
            HRESULT hr = _taskbarList.ThumbBarAddButtons(CriticalHandle, (uint)nativeButtons.Length, nativeButtons);
            if (hr == HRESULT.E_INVALIDARG)
            {
                hr = HRESULT.S_FALSE;
            }
            return hr;
        }

        private HRESULT UpdateTaskbarThumbButtons()
        {
            Debug.Assert(null != _taskbarList);

            var nativeButtons = new THUMBBUTTON[c_MaximumThumbButtons];
            TaskbarItemInfo taskbarInfo = TaskbarItemInfo;
            ThumbButtonInfoCollection thumbButtons = null;

            if (taskbarInfo != null)
            {
                thumbButtons = taskbarInfo.ThumbButtonInfos;
            }

            var nativeIcons = new List<NativeMethods.IconHandle>();

            try
            {
                uint currentButton = 0;
                if (null != thumbButtons)
                {
                    foreach (ThumbButtonInfo wrappedTB in thumbButtons)
                    {
                        var nativeTB = new THUMBBUTTON
                        {
                            iId = (uint)currentButton,
                            dwMask = THB.FLAGS | THB.TOOLTIP | THB.ICON,
                        };

                        switch (wrappedTB.Visibility)
                        {
                            case Visibility.Collapsed:
                                // HIDDEN removes the button from layout logic.
                                nativeTB.dwFlags = THBF.HIDDEN;
                                break;

                            case Visibility.Hidden:
                                // To match WPF's notion of hidden, we want this not HIDDEN
                                // but disabled, without background, and without icon.
                                nativeTB.dwFlags = THBF.DISABLED | THBF.NOBACKGROUND;
                                nativeTB.hIcon = IntPtr.Zero;

                                break;
                            default:
                            case Visibility.Visible:

                                nativeTB.szTip = wrappedTB.Description ?? "";
                                if (wrappedTB.ImageSource != null)
                                {
                                    NativeMethods.IconHandle nativeIcon = IconHelper.CreateIconHandleFromImageSource(wrappedTB.ImageSource, _overlaySize);
                                    nativeTB.hIcon = nativeIcon.CriticalGetHandle();
                                    nativeIcons.Add(nativeIcon);
                                }

                                if (!wrappedTB.IsBackgroundVisible)
                                {
                                    nativeTB.dwFlags |= THBF.NOBACKGROUND;
                                }

                                if (!wrappedTB.IsEnabled)
                                {
                                    nativeTB.dwFlags |= THBF.DISABLED;
                                }
                                else
                                {
                                    nativeTB.dwFlags |= THBF.ENABLED;
                                }

                                // This is separate from enabled/disabled
                                if (!wrappedTB.IsInteractive)
                                {
                                    nativeTB.dwFlags |= THBF.NONINTERACTIVE;
                                }

                                if (wrappedTB.DismissWhenClicked)
                                {
                                    nativeTB.dwFlags |= THBF.DISMISSONCLICK;
                                }
                                break;
                        }

                        nativeButtons[currentButton] = nativeTB;

                        ++currentButton;
                        if (currentButton == c_MaximumThumbButtons)
                        {
                            break;
                        }
                    }
                }

                // If we're not attached, or the list is less than the maximum number of buttons
                // then fill in the rest with collapsed, empty buttons.
                for (; currentButton < c_MaximumThumbButtons; ++currentButton)
                {
                    nativeButtons[currentButton] = new THUMBBUTTON
                    {
                        iId = (uint)currentButton,
                        dwFlags = THBF.NOBACKGROUND | THBF.DISABLED | THBF.HIDDEN,
                        dwMask = THB.FLAGS | THB.ICON | THB.TOOLTIP
                    };
                }

                // Finally, apply the update.
                return _taskbarList.ThumbBarUpdateButtons(CriticalHandle, (uint)nativeButtons.Length, nativeButtons);
            }
            finally
            {
                foreach (var icon in nativeIcons)
                {
                    icon.Dispose();
                }
            }
        }

        private void CreateRtl()
        {
            if ( this.FlowDirection == FlowDirection.LeftToRight )
            {
                _StyleEx &= ~NativeMethods.WS_EX_LAYOUTRTL;
            }
            else if ( this.FlowDirection == FlowDirection.RightToLeft )
            {
                _StyleEx |= NativeMethods.WS_EX_LAYOUTRTL;
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.IncorrectFlowDirection));
            }
}

        /// <summary>
        ///     Updates both style and styleEx for the window
        ///
        ///     WCP: Figure out how to update window styles after calling SetWindowLong
        ///     Currently hides the window and then shows it to make it update. Have to
        ///     find a better way of doing this.
        /// </summary>
        internal void Flush()
        {
            // (A NullReferenceException occurs when animating
            // the WindowStyle enum via a custom animation).  This bug contains details of
            // why we were seeing the null ref.
            //
            // Sometimes, the SetWindowPos call below results in sending certain window messages
            // like (WM_SIZE) and their handling leads to setting some property on the Window leading
            // to a call to HwndStyleManager.StartManaging.  Thus, we end up calling
            // dispose on the "new" usage of the Manager before we complete this run of Flush method.
            // This resulted in null ref in setting the Dirty bit below since we were not using
            // a local copy and the window member copy was already set to null by the "new" usage
            // of Manager.  To fix this bug, we do the following two things:
            //
            // 1) Keep a local copy of HwndStyleManager in this method to make it re-entrant.
            // 2) null out _window.Manager in HwndStyleMangager.Dispose only if _window.Manager is
            //    this instance of the Manager.
            //
            HwndStyleManager manager = Manager;
            if (manager.Dirty && CriticalHandle != IntPtr.Zero)
            {
                UnsafeNativeMethods.CriticalSetWindowLong(new HandleRef(this,CriticalHandle), NativeMethods.GWL_STYLE, (IntPtr)_styleDoNotUse.Value);
                UnsafeNativeMethods.CriticalSetWindowLong(new HandleRef(this,CriticalHandle), NativeMethods.GWL_EXSTYLE, (IntPtr)_styleExDoNotUse.Value);

                UnsafeNativeMethods.SetWindowPos(new HandleRef(this, CriticalHandle), NativeMethods.NullHandleRef, 0, 0, 0, 0,
                                               NativeMethods.SWP_NOMOVE |
                                               NativeMethods.SWP_NOSIZE |
                                               NativeMethods.SWP_NOZORDER |
                                               NativeMethods.SWP_FRAMECHANGED |
                                               NativeMethods.SWP_DRAWFRAME |
                                               NativeMethods.SWP_NOACTIVATE);
                manager.Dirty = false;
            }
        }
        private void ClearRootVisual()
        {
            if (_swh != null)
            {
                _swh.ClearRootVisual();
            }
        }


        private NativeMethods.POINT GetPointRelativeToWindow( int x, int y )
        {
            return _swh.GetPointRelativeToWindow( x, y, this.FlowDirection);
        }

        //     If you're in the middle of changing the window's _style or _styleEx and call this function,
        //     you may get inconsistent results.
        private Size GetHwndNonClientAreaSizeInMeasureUnits()
        {
            // HwndSource expands the client area to cover the entire window when it is in UsesPerPixelOpacity mode,
            // So non client area is (0,0)
            return AllowsTransparency ? new Size(0, 0) : _swh.GetHwndNonClientAreaSizeInMeasureUnits();
        }

        private void ClearSourceWindow()
        {
            if (_swh != null)
            {
                try
                {
                    _swh.RemoveDisposedHandler(OnSourceWindowDisposed);
                }
                finally
                {
                    HwndSource source = _swh.HwndSourceWindow;
                    _swh = null;

                    if (source != null)
                    {
                        source.SizeToContentChanged -= new EventHandler(OnSourceSizeToContentChanged);
                    }
                }
            }
        }

        private void ClearHiddenWindowIfAny()
        {
            // If there is a hiddenWindow and it's the owner of the current one as the result of setting ShowInTaskbar,
            // we need to unparent it. Because when we dipose the hiddenWindow, if it's still the parent
            // of the current window, the current Window will get a second WM_DESTORY because its owner being distoried.
            // See detail in bug 953988.
            // Unparent it in WM_CLOSE because when we get to WM_DESTROY, _sourceWindow.Handle could be IntPtr.Zero;
            // InternalDispose() is where _hiddenWindow is disposed. It could be called from two places: 1. WmDestroy 2. OnSourceWindowDisposed.
            // When it's called from OnSourceWindowDisposed, _sourceWindow.Handle could have been set to IntPtr.Zero.
            if ((_hiddenWindow != null) && (_hiddenWindow.Handle == _ownerHandle))
            {
                SetOwnerHandle(IntPtr.Zero);
            }
        }

        private void VerifyConsistencyWithAllowsTransparency()
        {
            if (AllowsTransparency)
            {
                VerifyConsistencyWithAllowsTransparency(WindowStyle);
            }
        }

        private void VerifyConsistencyWithAllowsTransparency(WindowStyle style)
        {
            if (AllowsTransparency && style != WindowStyle.None)
            {
                throw new InvalidOperationException(SR.Get(SRID.MustUseWindowStyleNone));
            }
        }

        private void VerifyConsistencyWithShowActivated()
        {
            //
            // We don't support to show a maximized non-activated window.
            // Don't check this consistency in a RBW (would break because Visibility is set when launching the RBW).
            //
            if (!_inTrustedSubWindow && WindowState == WindowState.Maximized && !ShowActivated)
                throw new InvalidOperationException(SR.Get(SRID.ShowNonActivatedAndMaximized));
        }

        private static bool IsValidSizeToContent(SizeToContent value)
        {
            return value == SizeToContent.Manual ||
                   value == SizeToContent.Width ||
                   value == SizeToContent.Height ||
                   value == SizeToContent.WidthAndHeight;
        }

        private static bool IsValidResizeMode(ResizeMode value)
        {
            return value == ResizeMode.NoResize
                || value == ResizeMode.CanMinimize
                || value == ResizeMode.CanResize
                || value == ResizeMode.CanResizeWithGrip;
        }

        private static bool IsValidWindowStartupLocation(WindowStartupLocation value)
        {
            return value == WindowStartupLocation.CenterOwner
                || value == WindowStartupLocation.CenterScreen
                || value == WindowStartupLocation.Manual;
        }

        private static bool IsValidWindowState(WindowState value)
        {
            return value == WindowState.Maximized
                || value == WindowState.Minimized
                || value == WindowState.Normal;
        }

        private static bool IsValidWindowStyle(WindowStyle value)
        {
            return value == WindowStyle.None
                || value == WindowStyle.SingleBorderWindow
                || value == WindowStyle.ThreeDBorderWindow
                || value == WindowStyle.ToolWindow;
        }

        #endregion Private Methods

        #region Manipulation Boundary Feedback

        /// <summary>
        ///     Provides feedback when a manipulation has encountered a boundary by nudging the window.
        /// </summary>
        /// <remarks>
        ///     PanningFeedback is reported on Handle of the Window using Begin/Update/EndPanningFeedback
        ///     APIs. When the Handle reported to these APIs is not of a top-level window, these APIs
        ///     do a GetAncestor call to get the top-level window and apply the effect of it. For eg.
        ///     in the case of XBAPs we report the feedback to the Handle of RootBrowserWindow which is
        ///     a child window, but still the panning feedback effect gets applied to the browser window
        ///     itself. Security wise this also assumes that PanningFeedback will not move the Window
        ///     by an arbitrary distance.
        /// </remarks>
        protected override void OnManipulationBoundaryFeedback(ManipulationBoundaryFeedbackEventArgs e)
        {
            base.OnManipulationBoundaryFeedback(e);

            // If the original source is not from the same PresentationSource as of the Window,
            // then do not act on the PanningFeedback.
            if (!PresentationSource.UnderSamePresentationSource(e.OriginalSource as DependencyObject, this))
            {
                return;
            }

            if (!e.Handled)
            {
                if (_currentPanningTarget == null ||
                    !_currentPanningTarget.IsAlive ||
                    _currentPanningTarget.Target != e.OriginalSource)
                {
                    if (_swh != null)
                    {
                        // Cache location if starting the panning feedback.
                        // Using SourceWindowHelper.WindowBounds instead of Window.Left
                        // and Window.Top so that the implementation works with
                        // RootBrowserWindow.
                        NativeMethods.RECT rc = WindowBounds;
                        _prePanningLocation = DeviceToLogicalUnits(new Point(rc.left, rc.top));
                    }
                }
                ManipulationDelta manipulation = e.BoundaryFeedback;
                UpdatePanningFeedback(manipulation.Translation, e.OriginalSource);
                e.CompensateForBoundaryFeedback = CompensateForPanningFeedback;
            }
        }

        private static void OnStaticManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            Window window = sender as Window;
            if (window != null)
            {
                // Transitioning from direct manipulation to inertia, animate the window
                // back to its original position.
                window.EndPanningFeedback(true);
            }
        }

        private static void OnStaticManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            Window window = sender as Window;
            if (window != null)
            {
                // A complete was encountered. If this was a forced complete, snap the window
                // back to its original position.
                window.EndPanningFeedback(false);
            }
        }

        /// <summary>
        ///     Provides feedback by nudging the window.
        /// </summary>
        private void UpdatePanningFeedback(Vector totalOverpanOffset, object originalSource)
        {
            if ((_currentPanningTarget != null) && !_currentPanningTarget.IsAlive)
            {
                // The old source is gone, end any pending feedback
                _currentPanningTarget = null;
                EndPanningFeedback(false);
            }

            if (_swh != null)
            {
                if (_currentPanningTarget == null)
                {
                    // Provide feedback for this source's events
                    _currentPanningTarget = new WeakReference(originalSource);
                }

                // Only provide feedback for one source at a time
                if (originalSource == _currentPanningTarget.Target)
                {
                    // Update the window position
                    _swh.UpdatePanningFeedback(totalOverpanOffset, false);
                }
            }
        }

        /// <summary>
        ///     Returns the window to its original position.
        /// </summary>
        private void EndPanningFeedback(bool animateBack)
        {
            if (_swh != null)
            {
                // Restore the window to its original position
                _swh.EndPanningFeedback(animateBack);
            }
            _currentPanningTarget = null;
            _prePanningLocation = new Point(double.NaN, double.NaN);
        }

        /// <summary>
        ///     Method to compensate a point for PanningFeedback.
        /// </summary>
        Point CompensateForPanningFeedback(Point point)
        {
            if (!double.IsNaN(_prePanningLocation.X) && !double.IsNaN(_prePanningLocation.Y) && (_swh != null))
            {
                // transform the point to pre pan coordinate system
                NativeMethods.RECT rc = WindowBounds;
                Point windowLocation = DeviceToLogicalUnits(new Point(rc.left, rc.top));
                return new Point(point.X - (_prePanningLocation.X - windowLocation.X), point.Y - (_prePanningLocation.Y - windowLocation.Y));
            }
            return point;
        }

        #endregion

        //----------------------------------------------
        //
        // Private Properties
        //
        //----------------------------------------------
        #region Private Properties

        private SizeToContent HwndSourceSizeToContent
        {
            get
            {
                return _swh.HwndSourceSizeToContent;
            }
            set
            {
                _swh.HwndSourceSizeToContent = value;
            }
        }

        private NativeMethods.RECT WindowBounds
        {
            get
            {
                Debug.Assert( _swh != null );
                return _swh.WindowBounds;
            }
        }

        private int StyleFromHwnd
        {
            get
            {
                return _swh != null ? _swh.StyleFromHwnd : 0;
            }
        }

        private int StyleExFromHwnd
        {
            get
            {
                return _swh != null ? _swh.StyleExFromHwnd : 0;
            }
        }

        /// <summary>
        ///     Private helper for OwnedWindows property.
        ///     The public version returns a copy of the WindowCollection.
        ///     For internal Window usage, we use OwnedWindowInternal
        ///     so that we can modify the underlying collection.
        /// </summary>
        private WindowCollection OwnedWindowsInternal
        {
            get
            {
                if (_ownedWindows == null)
                {
                    _ownedWindows = new WindowCollection();
                }
                return _ownedWindows;
            }
        }

        /// <summary>
        ///     Application Instance
        /// </summary>
        private System.Windows.Application App
        {
            get {return System.Windows.Application.Current;}
        }

        /// <summary>
        ///     Tells whether the Application object exists or not
        /// </summary>
        private bool IsInsideApp
        {
            get
            {
                return (Application.Current != null);
            }
        }

        /// <summary>
        ///     List of events on this Window
        /// </summary>
        private EventHandlerList Events
        {
            get
            {
                if (_events == null)
                {
                    _events = new EventHandlerList();
                }
                return _events;
            }
        }

        #endregion Private Properties


        //----------------------------------------------
        //
        // Private Fields
        //
        //----------------------------------------------
        #region Private Fields

        private SourceWindowHelper  _swh;                               // object that will hold the window
        private Window              _ownerWindow;                       // owner window

        // keeps track of the owner hwnd
        // we need this one b/c a owner/parent
        // can be set through the WindowInteropHandler
        // which is different than the owner Window object
        private IntPtr              _ownerHandle = IntPtr.Zero;   // no need to dispose this
        private WindowCollection    _ownedWindows;
        private ArrayList           _threadWindowHandles;

        private bool                _updateHwndSize     = true;
        private bool                _updateHwndLocation = true;
        private bool                _updateStartupLocation;
        private bool                _isVisible;
        private bool                _isVisibilitySet;           // use this to tell whether Visibility is set or not.
        private bool                _resetKeyboardCuesProperty; // true if we set ShowKeyboradCuesProperty in ShowDialog
        private bool                _previousKeyboardCuesProperty;

        private static bool         _dialogCommandAdded;
        private bool                _postContentRenderedFromLoadedHandler;

        // WCP Window:  Define the dispose behavior for Window
        private bool                _disposed;

        private bool                _appShuttingDown;
        private bool                _ignoreCancel;
        private bool                _showingAsDialog;
        private bool                _isClosing;
        private bool                _visibilitySetInternally;

        // The hwnd can be created before window is shown via WindowInteropHelper.EnsureHandle.
        private bool                _hwndCreatedButNotShown;

        private double              _trackMinWidthDeviceUnits = 0;
        private double              _trackMinHeightDeviceUnits = 0;
        private double              _trackMaxWidthDeviceUnits = Double.PositiveInfinity;
        private double              _trackMaxHeightDeviceUnits = Double.PositiveInfinity;
        private double              _windowMaxWidthDeviceUnits = Double.PositiveInfinity;
        private double              _windowMaxHeightDeviceUnits = Double.PositiveInfinity;

        private double              _actualTop = Double.NaN;

        private double              _actualLeft = Double.NaN;

        //Never expose this at any cost
        private bool                        _inTrustedSubWindow;

        private ImageSource _icon;

        private NativeMethods.IconHandle    _defaultLargeIconHandle;
        private NativeMethods.IconHandle    _defaultSmallIconHandle;
        private NativeMethods.IconHandle    _currentLargeIconHandle;
        private NativeMethods.IconHandle    _currentSmallIconHandle;

        private bool?                       _dialogResult = null;
        private IntPtr                      _dialogOwnerHandle = IntPtr.Zero;
        private IntPtr                      _dialogPreviousActiveHandle;
        private DispatcherFrame             _dispatcherFrame;

        private WindowStartupLocation       _windowStartupLocation = WindowStartupLocation.Manual;

        // The previous WindowState value before WindowState changes
        private WindowState                 _previousWindowState = WindowState.Normal;
        private HwndWrapper         _hiddenWindow;
        private EventHandlerList    _events;

        // These should never be used directly, access only through property accessors

        private SecurityCriticalDataForSet<int>                 _styleDoNotUse;
        private SecurityCriticalDataForSet<int>                 _styleExDoNotUse;
        private HwndStyleManager    _manager;

        // reference to Resize Grip control; this is used to find out whether
        // the mouse of over the resizegrip control
        private Control                 _resizeGripControl;

        Point _prePanningLocation = new Point(double.NaN, double.NaN);

        // static objects for Events
        private static readonly object EVENT_SOURCEINITIALIZED = new object();
        private static readonly object EVENT_CLOSING = new object();
        private static readonly object EVENT_CLOSED = new object();
        private static readonly object EVENT_ACTIVATED = new object();
        private static readonly object EVENT_DEACTIVATED = new object();
        private static readonly object EVENT_STATECHANGED = new object();
        private static readonly object EVENT_LOCATIONCHANGED = new object();
        private static readonly object EVENT_CONTENTRENDERED = new object();
        private static readonly object EVENT_VISUALCHILDRENCHANGED = new object();

        #region Windows 7 Taskbar related fields

        // Register Window Message used by Shell to notify that the corresponding taskbar button has been added to the taskbar.
        private static readonly WindowMessage WM_TASKBARBUTTONCREATED;

        // Register Window Message used by Window to signal that we need to apply the taskbar button information again.
        private static readonly WindowMessage WM_APPLYTASKBARITEMINFO;

        // Magic constant determined by Shell.
        private const int c_MaximumThumbButtons = 7;

        private ITaskbarList3 _taskbarList;

        // When a taskbarList update fails because Explorer is non-responsive, defer further changes for a little while
        // to avoid causing the app to be non-responsive as well.
        private DispatcherTimer _taskbarRetryTimer;

        private Size _overlaySize;

        #endregion

        internal static readonly DependencyProperty IWindowServiceProperty
            = DependencyProperty.RegisterAttached("IWindowService", typeof(IWindowService), typeof(Window),
                                          new FrameworkPropertyMetadata((IWindowService)null,
                                          FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.OverridesInheritanceBehavior));

        DispatcherOperation         _contentRenderedCallback;

        private WeakReference _currentPanningTarget;

        #endregion Private Fields

        #region Private Class

        internal class SourceWindowHelper
        {
                internal SourceWindowHelper( HwndSource sourceWindow )
                {
                    Debug.Assert( sourceWindow != null );
                    _sourceWindow = sourceWindow;
                }

                internal bool IsSourceWindowNull
                {
                    get
                    {
                        return ( _sourceWindow == null );
                    }
                }

                internal bool IsCompositionTargetInvalid
                {
                    get
                    {
                        return (CompositionTarget == null);
                    }
                }

                internal IntPtr CriticalHandle
                {
                    get
                    {
                        if (_sourceWindow != null)
                        {
                            return _sourceWindow.CriticalHandle;
                        }
                        else
                        {
                            return IntPtr.Zero;
                        }
                    }
                }

                ///<summary>
                /// Get the work area bounds for this window - taking multi-mon into account.
                ///</summary>
                internal NativeMethods.RECT WorkAreaBoundsForNearestMonitor
                {
                    get
                    {
                        IntPtr monitor;
                        NativeMethods.MONITORINFOEX monitorInfo = new NativeMethods.MONITORINFOEX();
                        monitorInfo.cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFOEX));

                        monitor = SafeNativeMethods.MonitorFromWindow( new HandleRef( this, CriticalHandle), NativeMethods.MONITOR_DEFAULTTONEAREST  );
                        if ( monitor != IntPtr.Zero )
                        {
                            SafeNativeMethods.GetMonitorInfo( new HandleRef ( this, monitor ) , monitorInfo);
                        }

                        return monitorInfo.rcWork;
}
                }

                private NativeMethods.RECT ClientBounds
                {
                    get
                    {
                        NativeMethods.RECT rc = new NativeMethods.RECT(0,0,0,0);
                        SafeNativeMethods.GetClientRect(new HandleRef(this, CriticalHandle), ref rc);

                        return rc;
                    }
                }

                internal NativeMethods.RECT WindowBounds
                {
                    get
                    {
                        NativeMethods.RECT rc = new NativeMethods.RECT(0,0,0,0);
                        SafeNativeMethods.GetWindowRect(new HandleRef(this, CriticalHandle), ref rc);

                        return rc;
                    }
                }


                private NativeMethods.POINT GetWindowScreenLocation(FlowDirection flowDirection)
                {
                    Debug.Assert(IsSourceWindowNull != true, "IsSourceWindowNull cannot be true here");
                    NativeMethods.POINT pt = new NativeMethods.POINT(0, 0);
                    if (flowDirection == FlowDirection.RightToLeft)
                    {
                        NativeMethods.RECT rc = new NativeMethods.RECT(0, 0, 0, 0);

                        // with RTL window, GetClientRect returns reversed coordinates
                        SafeNativeMethods.GetClientRect(new HandleRef(this, CriticalHandle), ref rc);

                        // note that we use rc.right here for the RTL case and client to screen that point
                        pt = new NativeMethods.POINT(rc.right, rc.top);
                    }
                    UnsafeNativeMethods.ClientToScreen(new HandleRef(this, _sourceWindow.CriticalHandle), pt);

                    return pt;
                }

                internal SizeToContent HwndSourceSizeToContent
                {
                    get
                    {
                        return _sourceWindow.SizeToContent;
                    }

                    set
                    {
                        _sourceWindow.SizeToContent = value;
                    }
                }

                internal Visual RootVisual
                {
                    set
                    {
                        _sourceWindow.RootVisual = value;
                    }
                }

                internal bool IsActiveWindow
                {
                    get
                    {
                        return (_sourceWindow.CriticalHandle == UnsafeNativeMethods.GetActiveWindow());
                    }
                }

                internal HwndSource HwndSourceWindow
                {
                    get
                    {
                        return _sourceWindow;
                    }
                }

                internal HwndTarget CompositionTarget
                {
                    get
                    {
                        if (_sourceWindow != null)
                        {
                            HwndTarget compositionTarget = _sourceWindow.CompositionTarget;
                            if (compositionTarget != null && compositionTarget.IsDisposed == false)
                            {
                                return compositionTarget;
                            }
                        }
                        return null;
                    }
                }

                ///<summary>
                /// Return the relative window width and height.
                ///</summary>
                internal Size WindowSize
                {
                    get
                    {
                        // Get the size of the avalon window and pass it to
                        // the base implementation.

                        NativeMethods.RECT rc = WindowBounds;

                        return new Size(rc.right - rc.left, rc.bottom - rc.top);
                    }
                }

                internal int StyleExFromHwnd
                {
                    get
                    {
                        // Should never be called when Handle is non-null
                        Debug.Assert( IsSourceWindowNull == false , "Should only be invoked when we know Handle is non-null" );
                        return UnsafeNativeMethods.GetWindowLong(new HandleRef(this,CriticalHandle), NativeMethods.GWL_EXSTYLE);
                    }
                }

                internal int StyleFromHwnd
                {
                    get
                    {
                        // Should never be called when Handle is non-null
                        Debug.Assert( IsSourceWindowNull == false , "Should only be invoked when we know Handle is non-null" );
                        return UnsafeNativeMethods.GetWindowLong(new HandleRef(this,CriticalHandle), NativeMethods.GWL_STYLE);
                    }
                }


                ///<summary>
                ///     Transform global coords of window location
                ///     to coords relative to top/left of the window.
                ///</summary>
                internal NativeMethods.POINT GetPointRelativeToWindow( int x, int y, FlowDirection flowDirection )
                {
                    NativeMethods.POINT ptWindow = GetWindowScreenLocation(flowDirection);

                    // At this point ptWindow contains the location of the client area's top/left wrt
                    // the screen

                    return new NativeMethods.POINT( x - ptWindow.x, y - ptWindow.y );
                }

                /// <summary>
                ///     Gets the size from the hwnd
                /// </summary>
                internal Size GetSizeFromHwndInMeasureUnits()
                {
                    Debug.Assert( IsSourceWindowNull == false , "IsSourceWindowNull can't be true here");

                    Point pt = new Point(0,0);
                    NativeMethods.RECT rect = WindowBounds;
                    pt.X = rect.right - rect.left;
                    pt.Y = rect.bottom - rect.top;
                    pt = _sourceWindow.CompositionTarget.TransformFromDevice.Transform(pt);
                    return new Size(pt.X,pt.Y);
                }


                /// <summary>
                ///     Gets the frame size of the hwnd.
                ///     Note that we use the current Hwnd's style information.
                ///     If you're in the middle of changing the window's _style or _styleEx and call this function,
                ///     you may get inconsistent results.
                /// </summary>
                internal Size GetHwndNonClientAreaSizeInMeasureUnits()
                {
                    Debug.Assert( IsSourceWindowNull == false , "IsSourceWindowNull can't be true here");

                    // Diff the Client and Window sizes to get the dimensions of the frame.
                    NativeMethods.RECT clientRect = ClientBounds;
                    NativeMethods.RECT windowRect = WindowBounds;

                    Point pt = new Point(
                        (windowRect.right - windowRect.left) - (clientRect.right - clientRect.left),
                        (windowRect.bottom - windowRect.top) - (clientRect.bottom - clientRect.top));

                    pt = _sourceWindow.CompositionTarget.TransformFromDevice.Transform(pt);

                    // it's possible that the client rect is actually larger than the window rect
                    // Presumably this happens because the rects are changing
                    // out from under us, and it's just a transient state.  The Max calls
                    // avoid a crash when this happens.
                    return new Size(Math.Max(0.0, pt.X), Math.Max(0.0, pt.Y));
                }

                internal void ClearRootVisual()
                {
                    if ( _sourceWindow.RootVisual != null )
                    {
                        _sourceWindow.RootVisual = null;
                    }
                }

                internal void AddDisposedHandler( EventHandler theHandler )
                {
                    if (_sourceWindow != null)
                    {
                        _sourceWindow.Disposed += theHandler;
                    }
                }

                internal void RemoveDisposedHandler( EventHandler theHandler )
                {
                    if (_sourceWindow != null)
                    {
                        _sourceWindow.Disposed -= theHandler;
                    }
                }

                /// <summary>
                ///     Updates panning feedback for this window based on the offset.
                /// </summary>
                /// <param name="totalOverpanOffset">The amount of over-panning being reported.</param>
                /// <param name="animate">Whether to animate to the new feedback position.</param>
                internal void UpdatePanningFeedback(Vector totalOverpanOffset, bool animate)
                {
                    if ((_panningFeedback == null) && (_sourceWindow != null))
                    {
                        _panningFeedback = new HwndPanningFeedback(_sourceWindow);
                    }

                    if (_panningFeedback != null)
                    {
                        // Update the window position
                        _panningFeedback.UpdatePanningFeedback(totalOverpanOffset, animate);
                    }
                }

                /// <summary>
                ///     Return the window back to its original position.
                /// </summary>
                /// <param name="animateBack">Whether to animate to the original position.</param>
                internal void EndPanningFeedback(bool animateBack)
                {
                    if (_panningFeedback != null)
                    {
                        // Restore the window to its original position
                        _panningFeedback.EndPanningFeedback(animateBack);
                        _panningFeedback = null;
                    }
                }

                private HwndSource _sourceWindow;

                private HwndPanningFeedback _panningFeedback;
        }

        internal class HwndStyleManager : IDisposable
        {
            static internal HwndStyleManager StartManaging(Window w, int Style, int StyleEx )
            {
                if (w.Manager == null)
                {
                    return new HwndStyleManager(w, Style, StyleEx);
                }
                else
                {
                    w.Manager._refCount++;
                    return w.Manager;
                }
            }

            private HwndStyleManager(Window w, int Style, int StyleEx  )
            {
                _window = w;
                _window.Manager = this;

                if ( w.IsSourceWindowNull == false )
                {
                    _window._Style    =  Style;
                    _window._StyleEx  = StyleEx;

                    // Dirty ==> _style and hwnd are out of sync. Since we just got
                    // the style from hwnd, it obviously is not Dirty.
                    Dirty = false;
                }
                _refCount = 1;
            }

            void IDisposable.Dispose()
            {
                _refCount--;

                // (A NullReferenceException occurs when animating
                // the WindowStyle enum via a custom animation).  This bug contains details of
                // why we were seeing the null ref.
                //
                // Sometimes, the Flush call below results in sending certain window messages
                // and their handling leads to setting some property on the Window leading
                // to a call to HwndStyleManager.StartManaging.  Thus, we end up calling
                // dispose on that before we complete this run of the Dispose method.  This
                // resulted in null ref in Flush.  To fix this bug, we do the following two things:
                //
                // 1) Keep a local copy of HwndStyleManager in Flush to make it re-entrant
                // 2) null out _window.Manager below only if _window.Manager is this instance
                //    of the Manager.

                if (_refCount == 0)
                {
                    _window.Flush();

                    if (_window.Manager == this)
                    {
                        _window.Manager = null;
                    }
                }
            }

            internal bool Dirty
            {
                get { return _fDirty; }
                set { _fDirty = value; }
            }

            private Window          _window;
            private int             _refCount;
            private bool            _fDirty;
        }


        /// <summary>
        /// A helper class to convert (Left, Top) <see cref="Point"/> expressed
        /// in WPF-space (1/96" units) to Screen units.
        /// </summary>
        /// <remarks>
        /// Initially, an HWND created in <see cref="Window"/> in a Per-Monitor Aware process may not
        /// have a relaible DPI. Instead, it would be associated with the monitor where the screen point (0,0)
        /// happens to be, but the screen point corresponding to a WPF Window's (requestedLeft, requestedTop) may not map
        /// to that monitor. Our initial DeviceToLogicalUnits and LogicalToDeviceUnits transforms
        /// would normally have to rely on the DPI obtained from this HWND, which may lead us to
        /// an incorrect result when calculating screen points. To avoid this, we use a special heuristic here
        /// to identify the (screenLeft, screenTop) initially without relying on the HWND, and then
        /// supply it to HwndSourceParameters at the time of HWND creation. This would in turn set up the HWND
        /// with the correct DPI from the start, and all our transformations would also follow suit.
        /// </remarks>
        private class WindowStartupTopLeftPointHelper
        {
            /// <summary>
            /// The (Left, Top) logical point, in WPF coordinate space,
            /// that must be translated into Screen space.
            /// </summary>
            internal Point LogicalTopLeft { get; }

            /// <summary>
            /// The Screen-space translation of <see cref="LogicalTopLeft"/>
            /// </summary>
            internal Point? ScreenTopLeft { get; private set; } = null;

            /// <summary>
            /// Creates a new instance of <see cref="WindowStartupTopLeftPointHelper"/>
            /// </summary>
            /// <param name="topLeft">(left, top) point in WPF-space to be converted to a Screen-space point</param>
            internal WindowStartupTopLeftPointHelper(Point topLeft)
            {
                LogicalTopLeft = topLeft;

                if (IsHelperNeeded)
                {
                    IdentifyScreenTopLeft();
                }
            }

            /// <summary>
            /// Decides whether this helper should be used.
            /// This helper is used when -
            ///     a. WPF supports DPI scaling (HwndTarget.IsPerMonitorDpiScalingEnabled), and
            ///     b. The process is PMA (HwndTarget.IsProcessPerMonitorDpiAware)
            /// </summary>
            /// <remarks>
            /// <see cref="CoreAppContextSwitches.DoNotUsePresentationDpiCapabilityTier2OrGreater"/>
            /// is tested to determine whether accurate window placement behavior should be enabled, or not. This is a
            /// compatibility measure put in place to ensure that our improvement/fix does not clash with workarounds
            /// that developers might have built into their applications to correct this problem.
            /// </remarks>
            private bool IsHelperNeeded
            {
                get
                {
                    if (CoreAppContextSwitches.DoNotUsePresentationDpiCapabilityTier2OrGreater)
                    {
                        return false;
                    }

                    if (!HwndTarget.IsPerMonitorDpiScalingEnabled)
                    {
                        return false;
                    }

                    if (HwndTarget.IsProcessPerMonitorDpiAware.HasValue)
                    {
                        return HwndTarget.IsProcessPerMonitorDpiAware.Value;
                    }

                    // WPF supports Per-Monitor scaling, but HwndTarget has not
                    // yet been initialized with the first HWND, and therefore
                    // HwndTarget.IsProcessPerMonitorDpiAware is not queryable.
                    // Let's use the current process' DPI awareness as a proxy.
                    return DpiUtil.GetProcessDpiAwareness(IntPtr.Zero) == NativeMethods.PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE;
                }
            }

            /// <summary>
            /// Uses EnumDisplayDevices to iterate through each monitor, and looks to see if
            /// <see cref="LogicalTopLeft"/> exists within that monitor's rectangle. To do this,
            /// it scales that monitors coordinate space by its corresponding DPI scale factor (this
            /// happens in <see cref="MonitorEnumProc(IntPtr, IntPtr, ref Runtime.InteropServices.NativeMethods.RECT, IntPtr)"/>)
            /// and normalizes it to a 96 DPI space - which is the space in which WPF itself operates - and then sees whether
            /// <see cref="LogicalTopLeft"/> lies within that rectangle. If it does, then it updates <see cref="ScreenTopLeft"/>
            /// with the corresponding unscaled monitor-space point.
            /// </summary>
            private void IdentifyScreenTopLeft()
            {
                var nullHandle = new HandleRef(null, IntPtr.Zero);
                var hdc = UnsafeNativeMethods.GetDC(nullHandle);
                UnsafeNativeMethods.EnumDisplayMonitors(
                    hdc,
                    IntPtr.Zero,
                    MonitorEnumProc,
                    IntPtr.Zero);
                UnsafeNativeMethods.ReleaseDC(nullHandle, new HandleRef(null, hdc));
            }

            /// <summary>
            /// Helper for <see cref="IdentifyScreenTopLeft"/>
            /// </summary>
            /// <remarks>See summary and remarks for <see cref="IdentifyScreenTopLeft"/></remarks>
            /// <param name="hMonitor">A handle to the display monitor. This value will always be non-NULL.</param>
            /// <param name="hdcMonitor">A handle to the device context</param>
            /// <param name="lprcMonitor">
            /// A pointer to a RECT structure.
            ///
            /// If hdcMonitor is non-NULL, this rectangle is the intersection of the clipping area of the device
            /// context identified by hdcMonitor and the display monitor rectangle.The rectangle coordinates are
            /// device-context coordinates.
            ///
            /// If hdcMonitor is NULL, this rectangle is the display monitor rectangle. The rectangle coordinates
            /// are virtual-screen coordinates.
            /// </param>
            /// <param name="dwData">
            /// Application-defined data that EnumDisplayMonitors passes directly to
            /// the enumeration function.
            /// </param>
            /// <returns>
            /// To continue the enumeration, return true.
            /// To stop the enumeration, return false.
            /// </returns>
            private bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.RECT lprcMonitor, IntPtr dwData)
            {
                // The call the EnumDisplayMonitors set hdc to null
                // This means that hdcMonitor will be null, and
                // lprcMonitor will represent the RECT of the whole monitor
                // skip the checks and trust the API contract that this will
                // be so

                bool continueMonitorEnumeration = true;

                uint dpiX, dpiY;
                var hr =
                    UnsafeNativeMethods.GetDpiForMonitor(
                        new HandleRef(null, hMonitor),
                        NativeMethods.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                        out dpiX, out dpiY);

                if (hr == NativeMethods.S_OK)
                {
                    double dpiScaleX = dpiX * 1.0d / 96.0d;
                    double dpiScaleY = dpiY * 1.0d / 96.0d;

                    Rect monitorRectInWpfScale = new Rect
                    {
                        X = lprcMonitor.left / dpiScaleX,
                        Y = lprcMonitor.top / dpiScaleY,
                        Width = (lprcMonitor.right - lprcMonitor.left) / dpiScaleX,
                        Height = (lprcMonitor.bottom - lprcMonitor.top) / dpiScaleY
                    };

                    if (monitorRectInWpfScale.Contains(LogicalTopLeft))
                    {
                        ScreenTopLeft = new Point
                        {
                            X = LogicalTopLeft.X * dpiScaleX,
                            Y = LogicalTopLeft.Y * dpiScaleY
                        };

                        continueMonitorEnumeration = false;
                    }
                }

                return continueMonitorEnumeration;
            }
        }

        #endregion PrivateClass

        #region Private Enums
        private enum TransformType
        {
            WorkAreaToScreenArea = 0,
            ScreenAreaToWorkArea = 1
        }

        private enum BoundsSpecified
        {
            Height = 0,
            Width = 1,
            Top = 2,
            Left = 3
        }
        #endregion Private Enums

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



    #region Enums

    /// <summary>
    /// WindowStyle
    /// </summary>
    public enum WindowStyle
    {
        /// <summary>
        /// no border at all  also implies no caption
        /// </summary>
        None = 0,                                               // no border at all  also implies no caption

        /// <summary>
        /// SingleBorderWindow
        /// </summary>
        SingleBorderWindow = 1,                                    // WS_BORDER

        /// <summary>
        /// 3DBorderWindow
        /// </summary>
        ThreeDBorderWindow = 2,                                    // WS_BORDER | WS_EX_CLIENTEDGE

        /// <summary>
        /// FixedToolWindow
        /// </summary>
        ToolWindow = 3,                                           // WS_BORDER | WS_EX_TOOLWINDOW

        // NOTE: if you add or remove any values in this enum, be sure to update Window.IsValidWindowStyle()
    }


    /// <summary>
    /// WindowState
    /// </summary>
    public enum WindowState
    {
        /// <summary>
        /// Default size
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Minimized
        /// </summary>
        Minimized = 1,   // WS_MINIMIZE

        /// <summary>
        /// Maximized
        /// </summary>
        Maximized = 2   // WS_MAXIMIZE

        // NOTE: if you add or remove any values in this enum, be sure to update Window.IsValidWindowState()

#if THEATRE_FULLSCREEN
        // The following Two are not Implement yet
        /// <summary>
        /// Theatre
        /// </summary>
        Theatre = 3,

        /// <summary>
        /// FullScreen
        /// </summary>
        FullScreen = 4
#endif //THEATRE_FULLSCREEN
    }

    /// <summary>
    ///
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public enum WindowStartupLocation
    {
        /// <summary>
        /// Uses the values specified by Left and Top properties to position the Window
        /// </summary>
        Manual = 0,

        /// <summary>
        /// Centers the Window on the screen.  If there are more than one monitors, then
        /// the Window is centered on the monitor that has the mouse on it
        /// </summary>
        CenterScreen = 1,

        /// <summary>
        /// Centers the Window on its owner.  If there is no owner window defined or if
        /// it is not possible to center it on the owner, then defaults to Manual
        /// </summary>
        CenterOwner = 2,

        // NOTE: if you add or remove any values in this enum, be sure to update Window.IsValidWindowStartupLocation()
    }

    /// <summary>
    ///     ResizeMode
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public enum ResizeMode
    {
        /// <summary>
        ///     User cannot resize the Window. Maximize and Minimize boxes
        ///     do not show in the caption bar.
        /// </summary>
        NoResize = 0,

        /// <summary>
        ///     User can only minimize the Window.  Minimize box is shown and enabled
        ///     in the caption bar while the Maximize box is disabled.
        /// </summary>
        CanMinimize = 1,

        /// <summary>
        ///     User can fully resize the Window including minimize and maximize.
        ///     Both Maximize and Minimize boxes are shown and enabled in the caption
        ///     bar.
        /// </summary>
        CanResize = 2,

        /// <summary>
        ///     Same as CanResize and ResizeGrip will show
        /// </summary>
        CanResizeWithGrip = 3

        // NOTE: if you add or remove any values in this enum, be sure to update Window.IsValidResizeMode()
    }
    #endregion Enums

    internal class SingleChildEnumerator : IEnumerator
    {
        internal SingleChildEnumerator(object Child)
        {
            _child = Child;
            _count = Child == null ? 0 : 1;
        }

        object IEnumerator.Current
        {
            get { return (_index == 0) ? _child : null; }
        }

        bool IEnumerator.MoveNext()
        {
            _index++;
            return _index < _count;
        }

        void IEnumerator.Reset()
        {
            _index = -1;
        }

        private int _index = -1;
        private int _count = 0;
        private object _child;
    }
}



