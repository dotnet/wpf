// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using MS.Win32;
using MS.Utility;
using MS.Internal;
using MS.Internal.Interop;
using MS.Internal.PresentationCore;                        // SecurityHelper
using Microsoft.Win32;
using System.Diagnostics;
using System.ComponentModel;
using System;
using System.Security;
using System.IO;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Interop
{
    /// <summary>
    ///     The HwndSource class presents content within a Win32 HWND.
    /// </summary>
    public class HwndSource : PresentationSource, IDisposable, IWin32Window, IKeyboardInputSink
    {
        static HwndSource()
        {
            _threadSlot = Thread.AllocateDataSlot();
        }

        /// <summary>
        ///    Constructs an instance of the HwndSource class that will always resize to its content size.
        /// </summary>
        /// <param name="classStyle">
        ///     The Win32 class styles for this window.
        /// </param>
        /// <param name="style">
        ///     The Win32 styles for this window.
        /// </param>
        /// <param name="exStyle">
        ///     The extended Win32 styles for this window.
        /// </param>
        /// <param name="x">
        ///     The position of the left edge of this window.
        /// </param>
        /// <param name="y">
        ///     The position of the upper edge of this window.
        /// </param>
        /// <param name="name">
        ///     The name of this window.
        /// </param>
        /// <param name="parent">
        ///     The Win32 window that should be the parent of this window.
        /// </param>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public HwndSource(
            int classStyle,
            int style,
            int exStyle,
            int x,
            int y,
            string name,
            IntPtr parent)
        {

            HwndSourceParameters param = new HwndSourceParameters(name);
            param.WindowClassStyle = classStyle;
            param.WindowStyle = style;
            param.ExtendedWindowStyle = exStyle;
            param.SetPosition(x, y);
            param.ParentWindow = parent;
            Initialize(param);
        }

        /// <summary>
        ///    Constructs an instance of the HwndSource class. This version requires an
        ///    explicit width and height be sepecified.
        /// </summary>
        /// <param name="classStyle">
        ///     The Win32 class styles for this window.
        /// </param>
        /// <param name="style">
        ///     The Win32 styles for this window.
        /// </param>
        /// <param name="exStyle">
        ///     The extended Win32 styles for this window.
        /// </param>
        /// <param name="x">
        ///     The position of the left edge of this window.
        /// </param>
        /// <param name="y">
        ///     The position of the upper edge of this window.
        /// </param>
        /// <param name="width">
        ///     The width of this window.
        /// </param>
        /// <param name="height">
        ///     The height of this window.
        /// </param>
        /// <param name="name">
        ///     The name of this window.
        /// </param>
        /// <param name="parent">
        ///     The Win32 window that should be the parent of this window.
        /// </param>
        /// <param name="adjustSizingForNonClientArea">
        ///     Indicates that HwndSource should include the non-client area
        ///     of the hwnd when it calls the Layout Manager
        /// </param>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public HwndSource(int classStyle,
                          int style,
                          int exStyle,
                          int x,
                          int y,
                          int width,
                          int height,
                          string name,
                          IntPtr parent,
                          bool adjustSizingForNonClientArea)
        {

            HwndSourceParameters parameters = new HwndSourceParameters(name, width, height);
            parameters.WindowClassStyle = classStyle;
            parameters.WindowStyle = style;
            parameters.ExtendedWindowStyle = exStyle;
            parameters.SetPosition(x, y);
            parameters.ParentWindow = parent;
            parameters.AdjustSizingForNonClientArea = adjustSizingForNonClientArea;
            Initialize(parameters);
        }

        /// <summary>
        ///    Constructs an instance of the HwndSource class. This version requires an
        ///    explicit width and height be sepecified.
        /// </summary>
        /// <param name="classStyle">
        ///     The Win32 class styles for this window.
        /// </param>
        /// <param name="style">
        ///     The Win32 styles for this window.
        /// </param>
        /// <param name="exStyle">
        ///     The extended Win32 styles for this window.
        /// </param>
        /// <param name="x">
        ///     The position of the left edge of this window.
        /// </param>
        /// <param name="y">
        ///     The position of the upper edge of this window.
        /// </param>
        /// <param name="width">
        ///     The width of this window.
        /// </param>
        /// <param name="height">
        ///     The height of this window.
        /// </param>
        /// <param name="name">
        ///     The name of this window.
        /// </param>
        /// <param name="parent">
        ///     The Win32 window that should be the parent of this window.
        /// </param>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public HwndSource(
            int classStyle,
            int style,
            int exStyle,
            int x,
            int y,
            int width,
            int height,
            string name,
            IntPtr parent)
        {

            HwndSourceParameters parameters = new HwndSourceParameters(name, width, height);
            parameters.WindowClassStyle = classStyle;
            parameters.WindowStyle = style;
            parameters.ExtendedWindowStyle = exStyle;
            parameters.SetPosition(x, y);
            parameters.ParentWindow = parent;
            Initialize(parameters);
        }

        /// <summary>
        ///    HwndSource Ctor
        /// </summary>
        /// <param name="parameters"> parameter block </param>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public HwndSource(HwndSourceParameters parameters)
        {
            Initialize(parameters);
        }

        /// <summary>
        ///    HwndSource Ctor
        /// </summary>
        /// <param name="parameters"> parameter block </param>
        private void Initialize(HwndSourceParameters parameters)
        {
            _mouse = new SecurityCriticalDataClass<HwndMouseInputProvider>(new HwndMouseInputProvider(this));
            _keyboard = new SecurityCriticalDataClass<HwndKeyboardInputProvider>(new HwndKeyboardInputProvider(this));

            _layoutHook = new HwndWrapperHook(LayoutFilterMessage);
            _inputHook = new HwndWrapperHook(InputFilterMessage);
            _hwndTargetHook = new HwndWrapperHook(HwndTargetFilterMessage);

            _publicHook = new HwndWrapperHook(PublicHooksFilterMessage);

            // When processing WM_SIZE, LayoutFilterMessage must be invoked before
            // HwndTargetFilterMessage. This way layout will be updated before resizing
            // HwndTarget, resulting in single render per resize. This means that
            // layout hook should appear before HwndTarget hook in the wrapper hooks
            // list. If this is done the other way around, first HwndTarget resize will
            // force re-render, then layout will be updated according to the new size,
            // scheduling another render.
            HwndWrapperHook[] wrapperHooks = { _hwndTargetHook, _layoutHook, _inputHook, null };

            if (null != parameters.HwndSourceHook)
            {
                // In case there's more than one delegate, add these to the event storage backwards
                // so they'll get invoked in the expected order.
                Delegate[] handlers = parameters.HwndSourceHook.GetInvocationList();
                for (int i = handlers.Length -1; i >= 0; --i)
                {
                    _hooks += (HwndSourceHook)handlers[i];
                }
                wrapperHooks[3] = _publicHook;
            }

            _restoreFocusMode = parameters.RestoreFocusMode;
            _acquireHwndFocusInMenuMode = parameters.AcquireHwndFocusInMenuMode;

            if (parameters.EffectivePerPixelOpacity)
            {
                parameters.ExtendedWindowStyle |= NativeMethods.WS_EX_LAYERED;
            }
            else
            {
                parameters.ExtendedWindowStyle &= (~NativeMethods.WS_EX_LAYERED);
            }


            _constructionParameters = parameters;
            _hwndWrapper = new HwndWrapper(parameters.WindowClassStyle,
                                       parameters.WindowStyle,
                                       parameters.ExtendedWindowStyle,
                                       parameters.PositionX,
                                       parameters.PositionY,
                                       parameters.Width,
                                       parameters.Height,
                                       parameters.WindowName,
                                       parameters.ParentWindow,
                                       wrapperHooks);

            _hwndTarget = new HwndTarget(_hwndWrapper.Handle);
            _hwndTarget.UsesPerPixelOpacity = parameters.EffectivePerPixelOpacity;
            if(_hwndTarget.UsesPerPixelOpacity)
            {
                _hwndTarget.BackgroundColor = Colors.Transparent;

                // Prevent this window from being themed.
                UnsafeNativeMethods.CriticalSetWindowTheme(new HandleRef(this, _hwndWrapper.Handle), "", "");
            }
            _constructionParameters = null;

            if (!parameters.HasAssignedSize)
                _sizeToContent = SizeToContent.WidthAndHeight;

            _adjustSizingForNonClientArea = parameters.AdjustSizingForNonClientArea;
            _treatAncestorsAsNonClientArea = parameters.TreatAncestorsAsNonClientArea;

            // Listen to the UIContext.Disposed event so we can clean up.
            // The HwndTarget cannot work without a MediaContext which
            // is disposed when the UIContext is disposed.  So we need to
            // dispose the HwndTarget and also never use it again (to
            // paint or process input).  The easiest way to do this is to just
            // dispose the HwndSource at the same time.
            _weakShutdownHandler = new WeakEventDispatcherShutdown(this, this.Dispatcher);

            // Listen to the HwndWrapper.Disposed event so we can clean up.
            // The HwndTarget cannot work without a live HWND, and since
            // the HwndSource represents an HWND, we make sure we dispose
            // ourselves if the HWND is destroyed out from underneath us.
            _hwndWrapper.Disposed += new EventHandler(OnHwndDisposed);

            
            // HwndStylusInputProvider must be initialized after _hwndWrapper as the wrapper
            // is used in setting up PenContexts for the Wisp based Stylus stack.
            // If stylus and touch are disabled, simply do not instantiate the provider.
            // This will prevent any HWND from being registered in StylusLogic and therefore
            // the stack will never receive input.
            if (StylusLogic.IsStylusAndTouchSupportEnabled)
            {
                // Choose between Wisp and Pointer stacks
                if (StylusLogic.IsPointerStackEnabled)
                {
                    _stylus = new SecurityCriticalDataClass<IStylusInputProvider>(new HwndPointerInputProvider(this));
                }
                else
                {
                    _stylus = new SecurityCriticalDataClass<IStylusInputProvider>(new HwndStylusInputProvider(this));
                }
            }

            // WM_APPCOMMAND events are handled thru this.
            _appCommand = new SecurityCriticalDataClass<HwndAppCommandInputProvider>(new HwndAppCommandInputProvider(this));

            // Register the top level source with the ComponentDispatcher.
            if (parameters.TreatAsInputRoot)
            {
                _weakPreprocessMessageHandler = new WeakEventPreprocessMessage(this, false);
            }
            AddSource();

            // Register dropable window.
            if (_hwndWrapper.Handle != IntPtr.Zero)
            {
                // This call is safe since DragDrop.RegisterDropTarget is checking the unmanged
                // code permission.
                DragDrop.RegisterDropTarget(_hwndWrapper.Handle);
                _registeredDropTargetCount++;
            }
        }

        /// <summary>
        ///     Disposes the object
        /// </summary>
        ///<remarks>
        /// This API is not available in Internet Zone.
        ///</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Adds a hook that gets called for every window message.
        /// </summary>
        /// <param name="hook">
        ///     The hook to add.
        /// </param>
        ///<remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        ///</remarks>
        public void AddHook(HwndSourceHook hook)
        {
            Verify.IsNotNull(hook, "hook");

            CheckDisposed(true);

            if(_hooks == null)
            {
                _hwndWrapper.AddHook(_publicHook);
            }
            _hooks += hook;
        }

        /// <summary>
        ///     Removes a hook that was previously added.
        /// </summary>
        /// <param name="hook">
        ///     The hook to remove.
        /// </param>
        ///<remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        ///</remarks>
        public void RemoveHook(HwndSourceHook hook)
        {

            //this.VerifyAccess();

            _hooks -= hook;
            if(_hooks == null)
            {
                _hwndWrapper.RemoveHook(_publicHook);
            }
        }

        /// <summary>
        /// GetInputProvider - Given a InputDevice, returns corresponding Provider
        /// </summary>
        /// <param name="inputDevice">InputDevice for which we need InputProvider</param>
        /// <returns>InputProvider, if known</returns>
        ///<remarks>
        /// This API is not available in Internet Zone.
        ///</remarks>
        internal override IInputProvider GetInputProvider(Type inputDevice)
        {
            if (inputDevice == typeof(MouseDevice))
                return (_mouse    != null ?    _mouse.Value : null);

            if (inputDevice == typeof(KeyboardDevice))
                return (_keyboard != null ? _keyboard.Value : null);

            if (inputDevice == typeof(StylusDevice))
                return (_stylus   != null ?   _stylus.Value : null);

            return null;
        }

        /// <summary>
        /// Changes DPI as per the event args
        /// </summary>
        internal void ChangeDpi(HwndDpiChangedEventArgs e)
        {
            OnDpiChanged(e);
        }

        /// <summary>
        /// Change DPI per info in <paramref name="e"/>
        /// </summary>
        internal void ChangeDpi(HwndDpiChangedAfterParentEventArgs e)
        {
            OnDpiChangedAfterParent(e);
        }

        /// <summary>
        ///     Announces when the DPI is going to change for the window. If the user handles this event,
        ///     WPF does not scale any visual.
        /// </summary>
        protected virtual void OnDpiChanged(HwndDpiChangedEventArgs e)
        {
            DpiChanged?.Invoke(this, e);

            if (!e.Handled)
            {
                _hwndTarget?.OnDpiChanged(e);

                // New world transform has been set-up by HwndTarget
                // set the layouts size again to account for this
                if (IsLayoutActive() == true)
                {
                    // Call the helper method SetLayoutSize to set Layout's size
                    // We may already be inside a call to SetLayoutSize - schedule another call
                    // asynchronously
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(SetLayoutSize));

                    // Post the firing of ContentRendered as Input priority work item so that ContentRendered will be
                    // fired after render query empties.
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(FireContentRendered), this);
                }
                else
                {
                    // Even though layout won't run (the root visual is either null or not
                    // a UIElement), the hit-test results will certainly have changed.
                    InputManager.SafeCurrentNotifyHitTestInvalidated();
                }
            }
        }

        /// <summary>
        /// Announces that the DPI for a window is going to change.
        /// </summary>
        /// <remarks>
        /// This method only applies when the root-visual HWND has WS_CHILD, i.e.,
        /// when a WPF window/control is parented under another HWND (native window, or a WinForms control),
        /// it will only receive WM_DPICHANGED_AFTERPARENT (and WM_DPICHANTED_BEFOREPARENT), and
        /// it will NOT receive WM_DPICHANGED.
        /// This method is called in response to WM_DPICHANGED_AFTERPARENT, and it does not
        /// receive any data as part of the Window Message (unlike WM_DPICHANGED, which receives
        /// the suggested rectangle for the window).
        /// We calculate the current client rect size (by asking <see cref="_hwndTarget"/>),
        /// and then use that rect as a proxy for the "suggested rectangle" when notifying listeners
        /// of DPI change via the <see cref="DpiChanged"/> event.
        /// </remarks>
        private void OnDpiChangedAfterParent(HwndDpiChangedAfterParentEventArgs e)
        {
            if (_hwndTarget != null)
            {
                var dpiChangedEventArgs = (HwndDpiChangedEventArgs)e;
                DpiChanged?.Invoke(this, dpiChangedEventArgs);
                if (!dpiChangedEventArgs.Handled)
                {
                    _hwndTarget?.OnDpiChangedAfterParent(e);
                }

                // New world transform has been set-up by HwndTarget
                // Set/update the layout size again to account for this.
                //
                // This should be called regardless of whether it was WPF
                // or user code that handled the DpiChanged event. Presumably,
                // whichever code handles the DPI changes set up updated
                // window sizes etc. in screen/client coordinates, and now
                // we must adapt and update the corresponding layout sizes as well.
                if (IsLayoutActive() == true)
                {
                    // Call the helper method SetLayoutSize to set Layout's size
                    // We may already be inside a call to SetLayoutSize - schedule another call
                    // asynchronously
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(SetLayoutSize));

                    // Post the firing of ContentRendered as Input priority work item so that ContentRendered will be
                    // fired after render query empties.
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(FireContentRendered), this);
                }
                else
                {
                    // Even though layout won't run (the root visual is either null or not
                    // a UIElement), the hit-test results will certainly have changed.
                    InputManager.SafeCurrentNotifyHitTestInvalidated();
                }
            }
        }

        /// <summary>
        ///     Announces when this source is disposed.
        /// </summary>
        public event EventHandler Disposed;

        /// <summary>
        ///     Announces when the SizeToContent property changes on this source.
        /// </summary>
        public event EventHandler SizeToContentChanged;

        /// <summary>
        ///     Announces when the DPI of the monitor of this Hwnd has changed or the HWND is moved to a monitor with different DPI.
        /// </summary>
        public event HwndDpiChangedEventHandler DpiChanged;

        /// <summary>
        ///     Whether or not the object is disposed.
        /// </summary>
        public override bool IsDisposed {get {return _isDisposed;}}

        /// <summary>
        /// The Root Visual for this window. If it is a UIElement
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public override Visual RootVisual
        {
            get
            {
                if (_isDisposed)
                    return null;
                return (_rootVisual.Value);
            }
            set
            {
                CheckDisposed(true);

                RootVisualInternal = value;
            }
        }

        private Visual RootVisualInternal
        {
            set
            {
                if (_rootVisual.Value != value)
                {
                    Visual oldRoot = _rootVisual.Value;

                    if(value != null)
                    {
                        _rootVisual.Value = value;

                        if(_rootVisual.Value is UIElement)
                        {
                            ((UIElement)(_rootVisual.Value)).LayoutUpdated += new EventHandler(OnLayoutUpdated);
                        }

                        if (_hwndTarget != null && _hwndTarget.IsDisposed == false)
                        {
                            _hwndTarget.RootVisual = _rootVisual.Value;
                        }

                        UIElement.PropagateResumeLayout(null, value);
                    }
                    else
                    {
                        _rootVisual.Value = null;
                        if (_hwndTarget != null && !_hwndTarget.IsDisposed)
                        {
                            _hwndTarget.RootVisual = null;
                        }
                    }

                    if(oldRoot != null)
                    {
                        if(oldRoot is UIElement)
                        {
                            ((UIElement)oldRoot).LayoutUpdated -= new EventHandler(OnLayoutUpdated);
                        }

                        UIElement.PropagateSuspendLayout(oldRoot);
                    }

                    RootChanged(oldRoot, _rootVisual.Value);

                    if (IsLayoutActive() == true)
                    {
                        // Call the helper method SetLayoutSize to set Layout's size
                        SetLayoutSize();

                        // Post the firing of ContentRendered as Input priority work item so that ContentRendered will be
                        // fired after render query empties.
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(FireContentRendered), this);
                    }
                    else
                    {
                        // Even though layout won't run (the root visual is either null or not
                        // a UIElement), the hit-test results will certainly have changed.
                        InputManager.SafeCurrentNotifyHitTestInvalidated();
                    }

                    // It is possible that someone would have closed the window in one of the
                    // previous callouts - such as during RootChanged or during the layout
                    // we syncronously invoke.  In such cases, the state of this object would
                    // have been torn down.  We just need to protect against that.
                    if(_keyboard != null)
                    {
                        _keyboard.Value.OnRootChanged(oldRoot, _rootVisual.Value);
                    }
                }

                // when automation listeners are present, ensure that the top-level
                // peer is on the layout manager's AutomationEvents list.
                // [See <see cref="EventMap.NotifySources"/> for full discussion.  This is part (a)]
                if (value != null && _hwndTarget != null && !_hwndTarget.IsDisposed &&
                    MS.Internal.Automation.EventMap.HasListeners)
                {
                    _hwndTarget.EnsureAutomationPeer(value);
                }
            }
        }

        /// <summary>
        ///     Returns a sequence of registered input sinks.
        /// </summary>
        public IEnumerable<IKeyboardInputSink> ChildKeyboardInputSinks
        {
            get
            {
                if (_keyboardInputSinkChildren != null)
                {
                    foreach (IKeyboardInputSite site in _keyboardInputSinkChildren)
                        yield return site.Sink;
                }
            }
        }

        /// <summary>
        ///     Returns the HwndSource that corresponds to the specified window.
        /// </summary>
        /// <param name="hwnd">The window.</param>
        /// <returns>The source that corresponds to the specified window.</returns>
        ///<remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        ///</remarks>
        public static HwndSource FromHwnd(IntPtr hwnd)
        {
            return CriticalFromHwnd(hwnd);
        }

        internal static HwndSource CriticalFromHwnd(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                throw new ArgumentException(SR.Get(SRID.NullHwnd));
            }
            HwndSource hwndSource = null;
            foreach (PresentationSource source in PresentationSource.CriticalCurrentSources)
            {
                HwndSource test = source as HwndSource;
                if (test != null && test.CriticalHandle == hwnd)
                {
                    // Don't hand out a disposed source.
                    if (!test.IsDisposed)
                        hwndSource = test;
                    break;
                }
            }
            return hwndSource;
        }


        /// <summary>
        ///     The visual manager for the visuals being presented in the source.
        ///     Type-specific version of the CompositionTarget property for this source.
        /// </summary>
        public new HwndTarget CompositionTarget
        {
            get
            {
                if (_isDisposed)
                    return null;

                // Even though we created the HwndTarget, it can get disposed out from
                // underneath us.
                if (_hwndTarget!= null && _hwndTarget.IsDisposed == true)
                {
                    return null;
                }

                return _hwndTarget;
            }
        }

        /// <summary>
        ///     Returns visual target for this source.
        /// </summary>
        protected override CompositionTarget GetCompositionTargetCore()
        {
            return CompositionTarget;
        }

        /// <summary>
        ///     When an HwndSource enters menu mode, it subscribes to the
        ///     ComponentDispatcher.ThreadPreprocessMessage event to get
        ///     privileged access to the window messages.
        /// </summary>
        /// <remarks>
        ///     The ThreadPreprocessMessage handler for menu mode is
        ///     independent of the handler for the same event used for
        ///     keyboard processing by top-level windows.
        /// </remarks>
        internal override void OnEnterMenuMode()
        {
            // We opt-in this HwndSource to the new behavior for "exclusive"
            // menu mode only if AcquireHwndFocusInMenuMode is false.
            IsInExclusiveMenuMode = !_acquireHwndFocusInMenuMode;
            if(IsInExclusiveMenuMode)
            {
                Debug.Assert(_weakMenuModeMessageHandler == null);

                // Re-subscribe to the ComponentDispatcher.ThreadPreprocessMessage so we go first.
                _weakMenuModeMessageHandler = new WeakEventPreprocessMessage(this, true);

                // Hide the Win32 caret
                UnsafeNativeMethods.HideCaret(new HandleRef(this, IntPtr.Zero));
            }
        }

        /// <summary>
        ///     When an HwndSource leaves menu mode, it unsubscribes from the
        ///     ComponentDispatcher.ThreadPreprocessMessage event because it no
        ///     longer needs privileged access to the window messages.
        /// </summary>
        /// <remarks>
        ///     The ThreadPreprocessMessage handler for menu mode is
        ///     independent of the handler for the same event used for
        ///     keyboard processing by top-level windows.
        /// </remarks>
        internal override void OnLeaveMenuMode()
        {
            if(IsInExclusiveMenuMode)
            {
                Debug.Assert(_weakMenuModeMessageHandler != null);

                // Unsubscribe the special menu-mode handler since we don't need to go first anymore.
                _weakMenuModeMessageHandler.Dispose();
                _weakMenuModeMessageHandler = null;

                // Restore the Win32 caret.  This does not necessarily show the caret, it
                // just undoes the HideCaret call in OnEnterMenuMode.
                UnsafeNativeMethods.ShowCaret(new HandleRef(this, IntPtr.Zero));
            }
            IsInExclusiveMenuMode = false;
        }

        internal bool IsInExclusiveMenuMode{get; private set;}

        /// <summary>
        ///     Event invoked when the layout causes the HwndSource to resize automatically.
        /// </summary>
        public event AutoResizedEventHandler AutoResized;

        /// <summary>
        /// Handler for LayoutUpdated event of a rootVisual.
        /// </summary>
        private void OnLayoutUpdated(object obj, EventArgs args)
        {
            UIElement root = _rootVisual.Value as UIElement;

            if(root != null)
            {
                Size newSize = root.RenderSize;
                if (   _previousSize == null
                    || !DoubleUtil.AreClose(_previousSize.Value.Width, newSize.Width)
                    || !DoubleUtil.AreClose(_previousSize.Value.Height, newSize.Height))
                {
                    // We should update _previousSize, even if the hwnd is not
                    // sizing to content.  This fixes the scenario where:
                    //
                    // 1) hwnd is sized to content to say a, b
                    // 2) hwnd is resize to a bigger size
                    // 3) hwnd is sized to content to a, b again
                    _previousSize = newSize;

                    //
                    // Don't resize while the Window is in Minimized mode.
                    //
                    if (_sizeToContent != SizeToContent.Manual && !_isWindowInMinimizeState )
                    {
                        Resize(newSize);
                    }
                }
            }
        }

        /// <summary>
        /// This is called when LayoutManager was updated and its size (the layout size of top element) changed.
        /// Ask LayoutManager.Size to see what the new value is.
        /// </summary>
        private void Resize(Size newSize)
        {
            try
            {
                _myOwnUpdate = true;

                if (IsUsable)
                {
                    NativeMethods.RECT rect = AdjustWindowSize(newSize);

                    int newWidth = rect.right - rect.left;
                    int newHeight = rect.bottom - rect.top;

                    // Set the new window size
                    UnsafeNativeMethods.SetWindowPos(new HandleRef(this,_hwndWrapper.Handle), new HandleRef(null,IntPtr.Zero),
                                                   0, 0, newWidth, newHeight,
                                                   NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);

                    if (AutoResized != null)
                    {
                        AutoResized(this, new AutoResizedEventArgs(newSize));
                    }
                }
            }
            finally
            {
                _myOwnUpdate = false;
            }
        }

        /// <summary>
        /// This shows the system menu for the top level window that this HwndSource is in.
        /// </summary>
        internal void ShowSystemMenu()
        {
            // Find the topmost window.  This will handle the case where the HwndSource
            // is a child window.
            IntPtr hwndRoot = UnsafeNativeMethods.GetAncestor(new HandleRef(this, CriticalHandle), NativeMethods.GA_ROOT);

            // Open the system menu.
            UnsafeNativeMethods.PostMessage(new HandleRef(this, hwndRoot), MS.Internal.Interop.WindowMessage.WM_SYSCOMMAND, new IntPtr(NativeMethods.SC_KEYMENU), new IntPtr(NativeMethods.VK_SPACE));
        }

        internal Point TransformToDevice(Point pt)
        {
            // Any instances where this is done in Core and Framework should be updated to use this method
            return _hwndTarget.TransformToDevice.Transform(pt);
        }

        internal Point TransformFromDevice(Point pt)
        {
            return _hwndTarget.TransformFromDevice.Transform(pt);
        }

        private NativeMethods.RECT AdjustWindowSize(Size newSize)
        {
            // Gather the new client dimensions
            // The dimension WPF uses is logical unit. We need to convert to device unit first.
            Point pt = TransformToDevice(new Point(newSize.Width, newSize.Height));
            RoundDeviceSize(ref pt);
            NativeMethods.RECT rect = new NativeMethods.RECT(0, 0, (int)pt.X, (int)pt.Y);

            // If we're here, and it is the Window case (_adjustSizingForNonClientArea == true)
            // we get the size which includes the outside size of the window.  For browser case,
            // we don't support SizeToContent, so we don't take care of this condition.
            // For non-Window cases, we need to calculate the outside size of the window
            //
            // For windows with UsesPerPixelOpacity, we force the client to
            // fill the window, so we don't need to add in any frame space.
            //
            if (_adjustSizingForNonClientArea == false && !UsesPerPixelOpacity)
            {
                int style = NativeMethods.IntPtrToInt32((IntPtr)SafeNativeMethods.GetWindowStyle(new HandleRef(this, _hwndWrapper.Handle), false));
                int styleEx = NativeMethods.IntPtrToInt32((IntPtr)SafeNativeMethods.GetWindowStyle(new HandleRef(this, _hwndWrapper.Handle), true));

                SafeNativeMethods.AdjustWindowRectEx(ref rect, style, false, styleEx);
            }
            return rect;
        }

        // If the root element has Pixel snapping enabled, round the window size to the
        // nearest int.  Otherwise round the size up to the next int.
        private void RoundDeviceSize(ref Point size)
        {
            UIElement root = _rootVisual.Value as UIElement;
            if (root != null && root.SnapsToDevicePixels)
            {
                size = new Point(DoubleUtil.DoubleToInt(size.X), DoubleUtil.DoubleToInt(size.Y));
            }
            else
            {
                size = new Point(Math.Ceiling(size.X), Math.Ceiling(size.Y));
            }
        }

        /// <summary>
        /// Returns the hwnd handle to the window.
        /// </summary>
        ///<remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        ///</remarks>
        public IntPtr Handle
        {
            get
            {
                return CriticalHandle;
            }
        }

        internal IntPtr CriticalHandle
        {
            [FriendAccessAllowed]
            get
            {
                if (null != _hwndWrapper)
                    return _hwndWrapper.Handle;
                return IntPtr.Zero;
            }
        }

        internal HwndWrapper HwndWrapper
        {
            get { return _hwndWrapper; }
        }

        // Return whether this presentation source has capture.
        internal bool HasCapture
        {
            get
            {
                IntPtr capture = SafeNativeMethods.GetCapture();

                return ( capture == CriticalHandle );
            }
        }

        internal bool IsHandleNull
        {
            get
            {
                return _hwndWrapper.Handle == IntPtr.Zero ;
            }
        }

        /// <summary>
        /// Returns the hwnd handle to the window.
        /// </summary>
        public HandleRef CreateHandleRef()
        {
            return new HandleRef(this,Handle);
        }


        /// <summary>
        /// SizeToContent on HwndSource
        /// </summary>
        /// <value>
        /// The default value is SizeToContent.Manual
        /// </value>
        public SizeToContent SizeToContent
        {
            get
            {
                CheckDisposed(true);
                return _sizeToContent;
            }

            set
            {
                CheckDisposed(true);

                if (IsValidSizeToContent(value) != true)
                {
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(SizeToContent));
                }

                if (_sizeToContent == value)
                {
                    return;
                }

                _sizeToContent = value;

                // we only raise SizeToContentChanged when user interaction caused the change;
                // if a developer goes directly to HwndSource and sets SizeToContent, we will
                // not notify the wrapping Window

                if (IsLayoutActive() == true)
                {
                    // Call the helper method SetLayoutSize to set Layout's size
                    SetLayoutSize();
                }
            }
        }

        private bool IsLayoutActive()
        {
            if ((_rootVisual.Value is UIElement) && _hwndTarget!= null && _hwndTarget.IsDisposed == false)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// This is the helper method that sets Layout's size basing it on
        /// the current value of SizeToContent.
        /// </summary>
        private void SetLayoutSize()
        {
            Debug.Assert(_hwndTarget!= null, "HwndTarget is null");
            Debug.Assert(_hwndTarget.IsDisposed == false, "HwndTarget is disposed");

            UIElement rootUIElement = null;
            rootUIElement = _rootVisual.Value as UIElement;
            if (rootUIElement == null) return;

            // InvalidateMeasure() call is necessary in the following scenario
            //
            // Window w = new Window();
            // w.Measure(new Size(x,y));
            // w.Width = x;
            // w.Height = y;
            // w.Show()
            //
            // In the above scenario, the Measure call from SetLayoutSize will be opt out
            // and window will not receive the MeasureOverride call.  As such, the hwnd min/max
            // restrictions will not be applied since MeasureOverride did not happen after hwnd
            // creation.  Thus, we call InvalidatMeasure() to ensure MeasureOverride call on
            // Window after hwnd creation.

            rootUIElement.InvalidateMeasure();

            const EventTrace.Keyword etwKeywords = EventTrace.Keyword.KeywordLayout | EventTrace.Keyword.KeywordPerf;
            bool etwEnabled = EventTrace.IsEnabled(etwKeywords, EventTrace.Level.Info);
            long  ctxHashCode = 0;

            if (_sizeToContent == SizeToContent.WidthAndHeight)
            {
                //setup constraints for measure-to-content
                Size sz = new Size(double.PositiveInfinity, double.PositiveInfinity);

                if (etwEnabled)
                {
                    ctxHashCode = _hwndWrapper.Handle.ToInt64();
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientLayoutBegin, etwKeywords, EventTrace.Level.Info, ctxHashCode, EventTrace.LayoutSource.HwndSource_SetLayoutSize);
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientMeasureBegin, etwKeywords, EventTrace.Level.Info, ctxHashCode);
                }

                rootUIElement.Measure(sz);

                if (etwEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientMeasureEnd, etwKeywords, EventTrace.Level.Info, 1);
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientArrangeBegin, etwKeywords, EventTrace.Level.Info, ctxHashCode);
                }

                rootUIElement.Arrange(new Rect(new Point(), rootUIElement.DesiredSize));

                if (etwEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientArrangeEnd, etwKeywords, EventTrace.Level.Info, 1);
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientLayoutEnd, etwKeywords, EventTrace.Level.Info);
                }
            }
            else
            {
                // GetSizeFromHwnd sets either the outside size or the client size of the hwnd based on
                // _adjustSizeingForNonClientArea flag in logical units.
                Size sizeFromHwndLogicalUnits = GetSizeFromHwnd();
                Size sz = new Size(
                        (_sizeToContent == SizeToContent.Width ? double.PositiveInfinity : sizeFromHwndLogicalUnits.Width),
                        (_sizeToContent == SizeToContent.Height ? double.PositiveInfinity : sizeFromHwndLogicalUnits.Height));

                if (etwEnabled)
                {
                    ctxHashCode = _hwndWrapper.Handle.ToInt64();
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientLayoutBegin, etwKeywords, EventTrace.Level.Info, ctxHashCode, EventTrace.LayoutSource.HwndSource_SetLayoutSize);
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientMeasureBegin, etwKeywords, EventTrace.Level.Info, ctxHashCode);
                }

                rootUIElement.Measure(sz);

                if (etwEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientMeasureEnd, etwKeywords, EventTrace.Level.Info, 1);
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientArrangeBegin, etwKeywords, EventTrace.Level.Info, ctxHashCode);
                }

                if (_sizeToContent == SizeToContent.Width) sz = new Size(rootUIElement.DesiredSize.Width, sizeFromHwndLogicalUnits.Height);
                else if(_sizeToContent == SizeToContent.Height) sz = new Size(sizeFromHwndLogicalUnits.Width, rootUIElement.DesiredSize.Height);
                else sz = sizeFromHwndLogicalUnits;

                rootUIElement.Arrange(new Rect(new Point(), sz));

                if (etwEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientArrangeEnd, etwKeywords, EventTrace.Level.Info, 1);
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientLayoutEnd, etwKeywords, EventTrace.Level.Info);
                }
            }
            rootUIElement.UpdateLayout();
        }

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
                CheckDisposed(true);

                HwndTarget hwndTarget = CompositionTarget; // checks for disposed
                if(_hwndTarget != null)
                {
                    return _hwndTarget.UsesPerPixelOpacity;
                }
                else
                {
                    return false;
                }
            }
        }

        private Size GetSizeFromHwnd()
        {
            // Compute View's size and set
            NativeMethods.RECT rc = new NativeMethods.RECT(0, 0, 0, 0);

            if (_adjustSizingForNonClientArea == true)
            {
                GetNonClientRect(ref rc);
            }
            else
            {
                SafeNativeMethods.GetClientRect(new HandleRef(this,_hwndWrapper.Handle), ref rc);
            }

            Point convertedPt = TransformFromDevice(new Point(rc.right - rc.left, rc.bottom - rc.top));
            return new Size(convertedPt.X, convertedPt.Y);
        }

        private IntPtr HwndTargetFilterMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            IntPtr result = IntPtr.Zero ;

            if (IsUsable)
            {
                HwndTarget hwndTarget = _hwndTarget;
                if (hwndTarget != null)
                {
                    result = hwndTarget.HandleMessage((WindowMessage)msg, wParam, lParam);
                    if (result != IntPtr.Zero)
                    {
                        handled = true;
                    }
                }
}

            return result;
        }

        private IntPtr LayoutFilterMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            IntPtr result = IntPtr.Zero ;
            WindowMessage message = (WindowMessage)msg;

            // We have to revalidate everything because the Access() call
            // could have caused the CLR to enter a nested message pump,
            // during which almost anything could have happened that might
            // invalidate our checks.
            UIElement rootUIElement=null;
            rootUIElement = _rootVisual.Value as UIElement;
            if (IsUsable && rootUIElement != null)
            {
                switch (message)
                {
                    // A window receives this message when the user chooses a command from
                    // the Window menu or when the user chooses the maximize button, minimize
                    // button, restore button, or close button.
                    case WindowMessage.WM_SYSCOMMAND:
                        {
                            // The four low-order bits of the wParam parameter are used
                            // internally by the system.
                            Int32 sysCommand = NativeMethods.IntPtrToInt32(wParam) & 0xFFF0;

                            // Turn off SizeToContent if user chooses to maximize or resize.
                            if ((sysCommand == NativeMethods.SC_MAXIMIZE) ||
                                (sysCommand == NativeMethods.SC_SIZE))
                            {
                                DisableSizeToContent(rootUIElement, hwnd);
                            }
                        }
                        break;

                    // We get WM_SIZING. It means that user starts resizing the window.
                    // It is the first notification sent when user resizes (before WM_WINDOWPOSCHANGING)
                    // and it's not sent if window is resized programmatically.
                    // SizeToContent is turned off after user resizes.
                    case WindowMessage.WM_SIZING:
                        DisableSizeToContent(rootUIElement, hwnd);
                        break;

                    // The WM_WINDOWPOSCHANGING message is sent
                    // 1. when the size, position, or place in the Z order is about to change as a result of a call to
                    //    the SetWindowPos function or other window-management functions. SizeToContent orverrides all in this case.
                    // 2. when user resizes window. If it's user resize, we have turned SizeToContent off when we get WM_SIZING.
                    // It is sent before WM_SIZE.
                    // We can't use WM_GETMINMAXINFO, because if there is no window size change (we still need to make sure
                    // the client size not change), that notification wouldnt be sent.
                    case WindowMessage.WM_WINDOWPOSCHANGING:
                        Process_WM_WINDOWPOSCHANGING(rootUIElement, hwnd, message, wParam, lParam);
                        break;

                    // WM_SIZE message is sent after the size has changed.
                    // lParam has the new width and height of client area.
                    // root element's size should be adjust based on the new width and height and SizeToContent's value.
                    case WindowMessage.WM_SIZE:
                        Process_WM_SIZE(rootUIElement, hwnd, message, wParam, lParam);
                        break;
                }
            }

            // Certain messages need to be processed while we are in the middle
            // of construction - and thus an HwndTarget is not available.
            if(!handled && (_constructionParameters != null || IsUsable))
            {
                // Get the usesPerPixelOpacity from either the constructor parameters or the HwndTarget.
                bool usesPerPixelOpacity = _constructionParameters != null ? ((HwndSourceParameters)_constructionParameters).EffectivePerPixelOpacity : _hwndTarget.UsesPerPixelOpacity;

                switch(message)
                {
                    case WindowMessage.WM_NCCALCSIZE:
                        {
                            // Windows that use per-pixel opacity don't get
                            // their frames drawn by the system.  Generally
                            // this is OK, as users of per-pixel alpha tend
                            // to be doing customized UI anyways.  But we
                            // don't render correctly if we leave a non-client
                            // area, so here we expand the client area to
                            // cover any non-client area.
                            //
                            if(usesPerPixelOpacity)
                            {
                                if(wParam == IntPtr.Zero)
                                {
                                    // If wParam is FALSE, lParam points to a RECT
                                    // structure. On entry, the structure contains
                                    // the proposed window rectangle for the
                                    // window. On exit, the structure should
                                    // contain the screen coordinates of the
                                    // corresponding window client area.
                                    //
                                    // Since all we want to do is make the client
                                    // rect the same as the window rect, we don't
                                    // have to do anything.
                                    //
                                    result = IntPtr.Zero;
                                    handled = true;
                                }
                                else
                                {
                                    // If wParam is TRUE, lParam points to an
                                    // NCCALCSIZE_PARAMS structure that contains
                                    // information an application can use to
                                    // calculate the new size and position of
                                    // the client rectangle.
                                    //
                                    // When Windows sends the WM_NCCALCSIZE
                                    // message, the NCCALCSIZE_PARAMS structure
                                    // is filled out like this:
                                    //
                                    // rgrc[0] = new window rectangle (in parent coordinates)
                                    // rgrc[1] = old window rectangle (in parent coordinates)
                                    // rgrc[2] = old client rectangle (in parent coordinates)
                                    //
                                    // Notice that the client rectangle is given
                                    // in parent coordinates, not in client
                                    // coordinates.
                                    //
                                    // When your window procedure returns, Windows
                                    // expects the NCCALCSIZE_PARAMS structure to
                                    // be filled out like this:
                                    //
                                    // rgrc[0] = new client rectangle (in parent coordinates)
                                    //
                                    // Furthermore, if you return anything other
                                    // than 0, Windows expects the remaining two
                                    // rectangles to be filled out like this:
                                    //
                                    // rgrc[1] = destination rectangle (in parent coordinates)
                                    // rgrc[2] = source rectangle (in parent coordinates)
                                    //
                                    // (If you return 0, then Windows assumes that
                                    // the destination rectangle equals the new
                                    // client rectangle and the source rectangle
                                    // equals the old client rectangle.)
                                    //
                                    // Since all we want to do is make the client
                                    // rect the same as the window rect, we don't
                                    // have to do anything.
                                    //
                                    result = IntPtr.Zero;
                                    handled = true;
                                }
                            }
                        }
                        break;
                }
            }

            return result;
        }

        private void Process_WM_WINDOWPOSCHANGING(UIElement rootUIElement, IntPtr hwnd, WindowMessage msg, IntPtr wParam, IntPtr lParam)
        {
            // Only if SizeToContent overrides Win32 sizing change calls.
            // If it's coming from OnResize (_myOwnUpdate != true), it means we are adjusting
            // to the right size; don't need to do anything here.
            if ((_myOwnUpdate != true) && (SizeToContent != SizeToContent.Manual))
            {
                // Get the current style and calculate the size to be with the new style.
                // If WM_WINDOWPOSCHANGING is sent because of style changes, WM_STYLECHANGED is sent
                // before this. The style bits we get here are updated ones, but haven't been applied
                // to Window yet. For example, when the window is changing to borderless, without updating the window size,
                // the window size will remain the same but the client area will be bigger. So when SizeToContent is on, we calculate
                // the window size to be with the new style using AdustWindowRectEx and adjust it to make sure client area is not affected.
                NativeMethods.RECT rect = AdjustWindowSize(rootUIElement.RenderSize);

                int newCX = rect.right - rect.left;
                int newCY = rect.bottom - rect.top;

                // Get WINDOWPOS structure data from lParam; it contains information about the window's
                // new size and position.
                NativeMethods.WINDOWPOS windowPos = (NativeMethods.WINDOWPOS)UnsafeNativeMethods.PtrToStructure(lParam, typeof(NativeMethods.WINDOWPOS));

                bool sizeChanged = false;

                // If SWP_NOSIZE is set to ignore cx, cy. It could be a style or position change.
                if ((windowPos.flags & NativeMethods.SWP_NOSIZE) == NativeMethods.SWP_NOSIZE)
                {
                    NativeMethods.RECT windowRect = new NativeMethods.RECT(0, 0, 0, 0);

                    // Get the current Window rect
                    SafeNativeMethods.GetWindowRect(new HandleRef(this, _hwndWrapper.Handle), ref windowRect);

                    // If there is no size change with the new style we don't need to do anything.
                    if ((newCX != (windowRect.right - windowRect.left)) ||
                        (newCY != (windowRect.bottom - windowRect.top)))
                    {
                        // Otherwise unmark the flag to make our changes effective.
                        windowPos.flags &= ~NativeMethods.SWP_NOSIZE;

                        // When SWP_NOSIZE is on, the size info in cx and cy is bogus. They are ignored.
                        // When we turn it off, we need to provide valid value for both of them.
                        windowPos.cx = newCX;
                        windowPos.cy = newCY;
                        sizeChanged = true;
                    }
                }
                else
                {
                    // We have excluded SizeToContent == SizeToContent.Manual before entering this.
                    bool sizeToWidth = (SizeToContent == SizeToContent.Height) ? false : true;
                    bool sizeToHeight = (SizeToContent == SizeToContent.Width) ? false : true;

                    // Update WindowPos with the size we want.
                    if ((sizeToWidth) && (windowPos.cx != newCX))
                    {
                        windowPos.cx = newCX;
                        sizeChanged = true;
                    }

                    if ((sizeToHeight) && (windowPos.cy != newCY))
                    {
                        windowPos.cy = newCY;
                        sizeChanged = true;
                    }
                }

                // Marshal the structure back only when changed
                if (sizeChanged)
                {
                    Marshal.StructureToPtr(windowPos, lParam, true);
                }
            }
        }

        private void Process_WM_SIZE(UIElement rootUIElement, IntPtr hwnd, WindowMessage msg, IntPtr wParam, IntPtr lParam)
        {
            int x = NativeMethods.SignedLOWORD(lParam);
            int y = NativeMethods.SignedHIWORD(lParam);
            Point pt = new Point(x, y);
            const EventTrace.Keyword etwKeywords = EventTrace.Keyword.KeywordLayout | EventTrace.Keyword.KeywordPerf;
            bool etwEnabled = EventTrace.IsEnabled(etwKeywords, EventTrace.Level.Info);
            long ctxHashCode = 0;

            // 1. If it's coming from Layout (_myOwnUpdate), it means we are adjusting
            // to the right size; don't need to do anything here.
            // 2. If SizeToContent is set to WidthAndHeight, then we maintain the current hwnd size
            // in WM_WINDOWPOSCHANGING, so we don't need to re-layout here.  If SizeToContent
            // is set to Width or Height and developer calls SetWindowPos to set a new
            // Width/Height, we need to do a layout.
            // 3. We also don't need to do anything if it's minimized.

            // Keeps the status of whether the Window is in Minimized state or not.
            _isWindowInMinimizeState = (NativeMethods.IntPtrToInt32(wParam) == NativeMethods.SIZE_MINIMIZED) ? true : false;

            if ((!_myOwnUpdate) && (_sizeToContent != SizeToContent.WidthAndHeight) && !_isWindowInMinimizeState)
            {
                Point relevantPt = new Point(pt.X, pt.Y);

                // WM_SIZE has the client size of the window.
                // for appmodel window case, get the outside size of the hwnd.
                if (_adjustSizingForNonClientArea == true)
                {
                    NativeMethods.RECT rect = new NativeMethods.RECT(0, 0, (int)pt.X, (int)pt.Y);
                    GetNonClientRect(ref rect);
                    relevantPt.X = rect.Width;
                    relevantPt.Y = rect.Height;
                }

                // The lParam/wParam size and the GetNonClientRect size are
                // both in device coordinates, thus we convert to Measure
                // coordinates here.
                relevantPt = TransformFromDevice(relevantPt);

                Size sz = new Size(
                    (_sizeToContent == SizeToContent.Width ? double.PositiveInfinity : relevantPt.X),
                    (_sizeToContent == SizeToContent.Height ? double.PositiveInfinity : relevantPt.Y));

                // WPF content does not resize when the favorites
                // (or other side pane) is closed
                //
                // The issue is that when the browser shows favorites window, avalon
                // window is resized and we get WM_SIZE.  Here, we pass the IE window's
                // size to Measure so that Computed[Width/Height] gets the correct
                // IE window dimensions.  Since, IE window's size may not change, the
                // call to Measure is optimized out and no layout happens.  Invalidating
                // layout here ensures that we do layout.
                // This can happen only in the Window case
                if (_adjustSizingForNonClientArea == true)
                {
                    rootUIElement.InvalidateMeasure();
                }

                if (etwEnabled)
                {
                    ctxHashCode = _hwndWrapper.Handle.ToInt64();
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientLayoutBegin, etwKeywords, EventTrace.Level.Info, ctxHashCode, EventTrace.LayoutSource.HwndSource_WMSIZE);
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientMeasureBegin, etwKeywords, EventTrace.Level.Info, ctxHashCode);;
                }

                rootUIElement.Measure(sz);

                if (_sizeToContent == SizeToContent.Width) sz = new Size(rootUIElement.DesiredSize.Width, relevantPt.Y);
                else if (_sizeToContent == SizeToContent.Height) sz = new Size(relevantPt.X, rootUIElement.DesiredSize.Height);
                else sz = new Size(relevantPt.X, relevantPt.Y);

                if (etwEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientMeasureEnd, etwKeywords, EventTrace.Level.Info, 1);
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientArrangeBegin, etwKeywords, EventTrace.Level.Info, ctxHashCode);
                }

                rootUIElement.Arrange(new Rect(new Point(), sz));

                if (etwEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientArrangeEnd, etwKeywords, EventTrace.Level.Info, 1);
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientLayoutEnd, etwKeywords, EventTrace.Level.Info);
                }
                rootUIElement.UpdateLayout(); //finalizes layout
}
        }

        private void DisableSizeToContent(UIElement rootUIElement, IntPtr hwnd)
        {
            if (_sizeToContent != SizeToContent.Manual)
            {
                _sizeToContent = SizeToContent.Manual;

                // Window expereience layout issue when SizeToContent is being turned
                // off by user interaction
                // This bug was caused b/c we were giving rootUIElement.DesiredSize as input
                // to Measure/Arrange below.  That is incorrect since rootUIElement.DesiredSize may not
                // cover the entire hwnd client area.

                // GetSizeFromHwnd returns either the outside size or the client size of the hwnd based on
                // _adjustSizeingForNonClientArea flag in logical units.
                Size sizeLogicalUnits = GetSizeFromHwnd();
                rootUIElement.Measure(sizeLogicalUnits);
                rootUIElement.Arrange(new Rect(new Point(), sizeLogicalUnits));

                rootUIElement.UpdateLayout(); //finalizes layout


                if (SizeToContentChanged != null)
                {
                    SizeToContentChanged(this, EventArgs.Empty);
                }
            }
        }

        // Fills in the "non-client" rect of this HwndSource.  This is
        // either the window rect of this window, or of our ancestor root
        // window, depending on the value of
        // HwndSourceParameters.TreatAncestorsAsNonClientArea setting.
        private void GetNonClientRect(ref NativeMethods.RECT rc)
        {
            Debug.Assert(_adjustSizingForNonClientArea == true);

            IntPtr hwndRoot = IntPtr.Zero;

            if(_treatAncestorsAsNonClientArea)
            {
                hwndRoot = UnsafeNativeMethods.GetAncestor(new HandleRef(this, CriticalHandle), NativeMethods.GA_ROOT);
            }
            else
            {
                hwndRoot = CriticalHandle;
            }

            SafeNativeMethods.GetWindowRect(new HandleRef(this, hwndRoot), ref rc);
        }

        private IntPtr InputFilterMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            IntPtr result = IntPtr.Zero ;
            WindowMessage message = (WindowMessage)msg;

            // NOTE (alexz): invoke _stylus.FilterMessage before _mouse.FilterMessage
            // to give _stylus a chance to eat mouse message generated by stylus
            if (!_isDisposed && _stylus != null && !handled)
            {
                result = _stylus.Value.FilterMessage(hwnd, message, wParam, lParam, ref handled);
            }

            if (!_isDisposed && _mouse != null && !handled)
            {
                result = _mouse.Value.FilterMessage(hwnd, message, wParam, lParam, ref handled);
            }

            if (!_isDisposed && _keyboard != null && !handled)
            {
                // Sometimes WPF receives keyboard messages through both
                // IKeyboardInputSink and the WndProc.  Note that the WndProc
                // always comes after the IKIS methods.  We update
                // _lastKeyboardMessage in the IKIS methods to avoid responding
                // to the same message from the WndProc.
                // This is checked inside of HwndKeyboardInputProvider.FilterMessage.
                result = _keyboard.Value.FilterMessage(hwnd, message, wParam, lParam, ref handled);

                // When WPF is hosted within a "foreign" HWND, the parent
                // window may not talk to us through IKeyboardInputSink at all.
                // Instead, we will just receive window messages through the
                // WndProc.
                //
                // However, if IKIS was ever used in the past, then the last keyboard
                // message could be really old.  We need to flush out the last keybaord
                // message here now that we know we have received it in the WndProc.
                switch(message)
                {
                    case WindowMessage.WM_SYSKEYDOWN:
                    case WindowMessage.WM_KEYDOWN:
                    case WindowMessage.WM_SYSKEYUP:
                    case WindowMessage.WM_KEYUP:
                    case WindowMessage.WM_CHAR:
                    case WindowMessage.WM_DEADCHAR:
                    case WindowMessage.WM_SYSCHAR:
                    case WindowMessage.WM_SYSDEADCHAR:
                    {
                        _lastKeyboardMessage = new MSG();
                    }
                    break;
                }
            }

            if (!_isDisposed && _appCommand != null && !handled)
            {
                result = _appCommand.Value.FilterMessage(hwnd, message, wParam, lParam, ref handled);
            }

            return result;
        }

        /// <summary>
        ///    Called from HwndWrapper on all window messages.
        ///    Assumes Context.Access() is held.
        /// </summary>
        private IntPtr PublicHooksFilterMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // The default result for messages we handle is 0.
            IntPtr result = IntPtr.Zero ;
            WindowMessage message = (WindowMessage)msg;

            // Call all of the public hooks
            // We do this even if we are disposed because otherwise the hooks
            // would never see the WM_DESTROY etc. message.
            if (_hooks != null)
            {
                Delegate[] handlers = _hooks.GetInvocationList();
                for (int i = handlers.Length -1; i >= 0; --i)
                {
                    var hook = (HwndSourceHook)handlers[i];
                    result = hook(hwnd, msg, wParam, lParam, ref handled);
                    if(handled)
                    {
                        break;
                    }
                }
            }

            switch (message)
            {
                case WindowMessage.WM_NCDESTROY:
                    {
                        // We delivered the message to the hooks and the message
                        // is WM_NCDESTROY, so our commitments should be finished
                        // we can do final teardown. (like disposing the _hooks)
                        OnNoMoreWindowMessages();
                    }
                    break;
                case WindowMessage.WM_DESTROY:
                    {
                        
                        // This used to be called under Dispose triggered from WM_NCDESTROY.
                        // The problem there is that this is the wrong time for WISP to undelegate
                        // input.  Thus shutting down the stack would lead to an assert since the
                        // HWND in no longer in a good state.  Move this to WM_DESTROY, the start
                        // of the HWND destruction, allows us to shut down the stylus stack safely.
                        // Previously input was never properly undelegated as the COM references were
                        // not properly clearing from WISP.  Fixes to those issues in WISP and WPF have
                        // exposed this issue.
                        DisposeStylusInputProvider();
                    }
                    break;
            }

            return result;
        }

        /// <summary>
        /// Disposes the HwndStylusInputProvider to shutdown stylus/touch input.
        /// </summary>
        private void DisposeStylusInputProvider()
        {
            // Dispose the HwndStylusInputProvider BEFORE we destroy the HWND.
            // This is because the stylus provider has an async channel and
            // they don't want to process data after the HWND is destroyed.
            if (_stylus != null)
            {
                SecurityCriticalDataClass<IStylusInputProvider> stylus = _stylus;
                _stylus = null;
                stylus.Value.Dispose();
            }
        }

#region IKeyboardInputSink

        /// General security note on the implementation pattern of this interface. In Dev10 it was chosen
        /// to expose the interface implementation for overriding to customers. We did so by keeping the
        /// explicit interface implementations (that do have the property of being hidden from the public
        /// contract, which limits IntelliSense on derived types like WebBrowser) while sticking protected
        /// virtuals next to them. Those virtuals contain our base implementation, while the explicit
        /// interface implementation methods do call trivially into the virtuals.
        ///
        /// This comment outlines the security rationale applied to those methods.
        ///
        /// <SecurityNote Name="IKeyboardInputSink_Implementation">
        ///     The security attributes on the virtual methods within this region mirror the corresponding
        ///     IKeyboardInputSink methods; customers can override those methods, so we insert a LinkDemand
        ///     to encourage them to have a LinkDemand too (via FxCop).
        ///
        ///     While the methods have LinkDemands on them, the bodies of the methods typically contain
        ///     full demands through a SecurityHelper.DemandUnmanagedCode call. This might seem redundant.
        ///     The point here is we do a full demand for stronger protection of our built-in implementation
        ///     compared to the LinkDemand on the public interface. We really want full demands here but
        ///     declarative Demand doesn't work on interface methods. In addition, we try to take advantage
        ///     of the fact LinkDemands are consistently enforced between base and overridden virtual methods,
        ///     something full Demands do not give us, even when applied declaratively.

        private class MSGDATA
        {
            public MSG msg;
            public bool handled;
        }

        /// <summary>
        /// HwndSource keyboard input is sent through this delegate to check for
        /// Child Hwnd interop requirments.  If there are no child hwnds or focus
        /// is on this non-child hwnd then normal Avalon processing is done.
        /// </summary>
        private void OnPreprocessMessageThunk(ref MSG msg, ref bool handled)
        {
//             VerifyAccess();

            if (handled)
            {
                return;
            }

            // We only do these message.
            switch ((WindowMessage)msg.message)
            {
            case WindowMessage.WM_KEYUP:
            case WindowMessage.WM_KEYDOWN:
            case WindowMessage.WM_SYSKEYUP:
            case WindowMessage.WM_SYSKEYDOWN:
            case WindowMessage.WM_CHAR:
            case WindowMessage.WM_SYSCHAR:
            case WindowMessage.WM_DEADCHAR:
            case WindowMessage.WM_SYSDEADCHAR:
                MSGDATA msgdata = new MSGDATA();
                msgdata.msg = msg;
                msgdata.handled = handled;

                // Do this under the exception filter/handlers of the
                // dispatcher for this thread.
                //
                // NOTE: we lose the "perf optimization" of passing everything
                // around by-ref since we have to call through a delegate.
                object result = Dispatcher.CurrentDispatcher.Invoke(
                    DispatcherPriority.Send,
                    new DispatcherOperationCallback(OnPreprocessMessage),
                    msgdata);

                if (result != null)
                {
                    handled = (bool)result;
                }

                // the semantics dictate that the callers could change this data.
                msg = msgdata.msg;
                break;
            }
        }


        private object OnPreprocessMessage(object param)
        {
            MSGDATA msgdata = (MSGDATA) param;

            // Always process messages if this window is in menu mode.
            //
            // Otherwise, only process messages if someone below us has Focus.
            //
            // Mnemonics are broadcast to all branches of the window tree; even
            // those that don't have focus.  BUT! at least someone under this
            // top-level window must have focus.
            if (!((IKeyboardInputSink)this).HasFocusWithin() && !IsInExclusiveMenuMode)
            {
                return msgdata.handled;
            }

            ModifierKeys modifierKeys = HwndKeyboardInputProvider.GetSystemModifierKeys();

            // Interop with the Interop layer
            //
            switch ((WindowMessage)msgdata.msg.message)
            {
            case WindowMessage.WM_SYSKEYDOWN:
            case WindowMessage.WM_KEYDOWN:
                // MITIGATION: HANDLED_KEYDOWN_STILL_GENERATES_CHARS
                // In case a nested message pump is used before we return
                // from processing this message, we disable processing the
                // next WM_CHAR message because if the code pumps messages
                // it should really mark the message as handled.
                _eatCharMessages = true;
                DispatcherOperation restoreCharMessages = Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(RestoreCharMessages), null);

                // Force the Dispatcher to post a new message to service any
                // pending operations, so that the operation we just posted
                // is guaranteed to get dispatched after any pending WM_CHAR
                // messages are dispatched.
                Dispatcher.CriticalRequestProcessing(true);

                msgdata.handled = CriticalTranslateAccelerator(ref msgdata.msg, modifierKeys);
                if(!msgdata.handled)
                {
                    // MITIGATION: HANDLED_KEYDOWN_STILL_GENERATES_CHARS
                    // We did not handle the WM_KEYDOWN, so it is OK to process WM_CHAR messages.
                    // We can also abort the pending restore operation since we don't need it.
                    _eatCharMessages = false;
                    restoreCharMessages.Abort();
                }

                // Menu mode handles all keyboard messages so that they don't
                // get dispatched to some random window with focus.
                if(IsInExclusiveMenuMode)
                {
                    // However, if the WM_KEYDOWN message was not explicitly
                    // handled, then we need to generate WM_CHAR messages.  WPF
                    // expects this, but when we return handled, the outer
                    // message pump will skip the TranslateMessage and
                    // DispatchMessage calls.  We mitigate this by calling
                    // TranslateMessage directly.  This is the same trick that
                    // Win32 does in its menu loop.
                    if(!msgdata.handled)
                    {
                        UnsafeNativeMethods.TranslateMessage(ref msgdata.msg);
                    }

                    msgdata.handled = true;
                }

                break;

            case WindowMessage.WM_SYSKEYUP:
            case WindowMessage.WM_KEYUP:
                msgdata.handled = CriticalTranslateAccelerator(ref msgdata.msg, modifierKeys);

                // Menu mode handles all keyboard messages so that they don't
                // get dispatched to some random window with focus.
                if(IsInExclusiveMenuMode)
                {
                    msgdata.handled = true;
                }

                break;

            case WindowMessage.WM_CHAR:
            case WindowMessage.WM_SYSCHAR:
            case WindowMessage.WM_DEADCHAR:
            case WindowMessage.WM_SYSDEADCHAR:
                // MITIGATION: HANDLED_KEYDOWN_STILL_GENERATES_CHARS
                if(!_eatCharMessages)
                {
                    msgdata.handled = ((IKeyboardInputSink)this).TranslateChar(ref msgdata.msg, modifierKeys);

                    if (!msgdata.handled)
                    {
                        msgdata.handled = ((IKeyboardInputSink)this).OnMnemonic(ref msgdata.msg, modifierKeys);
                    }

                    if (!msgdata.handled)
                    {
                        _keyboard.Value.ProcessTextInputAction(msgdata.msg.hwnd, (WindowMessage)msgdata.msg.message,
                                                               msgdata.msg.wParam, msgdata.msg.lParam, ref msgdata.handled);
                    }
                }

                // Menu mode handles all keyboard messages so that they don't
                // get dispatched to some random window with focus.
                if(IsInExclusiveMenuMode)
                {
                    // If the WM_CHAR message is not explicitly handled, the
                    // standard behavior is to beep.
                    if(!msgdata.handled)
                    {
                        SafeNativeMethods.MessageBeep(0);
                    }

                    msgdata.handled = true;
                }

                break;
            }
            return msgdata.handled;
        }

        /// <summary>
        ///     Registers a child KeyboardInputSink with this sink.  A site
        ///     is returned.
        /// </summary>
        /// <remarks>
        ///     This API requires unrestricted UI Window permission.
        ///     We explicitly don't make this method overridable as we want to keep the
        ///     precise implementation fixed and make sure the _keyboardInputSinkChildren
        ///     state is kep consistent. By making the method protected, implementors can
        ///     still call into it when required. Notice as calls are made through the
        ///     IKIS interface, there's still a way for advanced developers to override
        ///     the behavior by re-implementing the interface.
        /// </remarks>
        protected IKeyboardInputSite RegisterKeyboardInputSinkCore(IKeyboardInputSink sink)
        {
            CheckDisposed(true);

            if (sink == null)
            {
                throw new ArgumentNullException("sink");
            }

            if (sink.KeyboardInputSite != null)
            {
                throw new ArgumentException(SR.Get(SRID.KeyboardSinkAlreadyOwned));
            }

            HwndSourceKeyboardInputSite site = new HwndSourceKeyboardInputSite(this, sink);

            if (_keyboardInputSinkChildren == null)
                _keyboardInputSinkChildren = new List<HwndSourceKeyboardInputSite>();
            _keyboardInputSinkChildren.Add(site);

            return site;
        }

        IKeyboardInputSite IKeyboardInputSink.RegisterKeyboardInputSink(IKeyboardInputSink sink)
        {
            return RegisterKeyboardInputSinkCore(sink);
        }

        /// <summary>
        ///     Gives the component a chance to process keyboard input.
        ///     Return value is true if handled, false if not.  Components
        ///     will generally call a child component's TranslateAccelerator
        ///     if they can't handle the input themselves.  The message must
        ///     either be WM_KEYDOWN or WM_SYSKEYDOWN.  It is illegal to
        ///     modify the MSG structure, it's passed by reference only as
        ///     a performance optimization.
        /// </summary>
        ///<remarks>
        /// This API is not available in Internet Zone.
        ///</remarks>
        protected virtual bool TranslateAcceleratorCore(ref MSG msg, ModifierKeys modifiers)
        {
//             VerifyAccess();

            return CriticalTranslateAccelerator(ref msg, modifiers);
        }

        bool IKeyboardInputSink.TranslateAccelerator(ref MSG msg, ModifierKeys modifiers)
        {
            return TranslateAcceleratorCore(ref msg, modifiers);
        }

        /// <summary>
        ///     Set focus to the first or last tab stop (according to the
        ///     TraversalRequest).  If it can't, because it has no tab stops,
        ///     the return value is false.
        /// </summary>
        protected virtual bool TabIntoCore(TraversalRequest request)
        {
            bool traversed = false;

            if(request == null)
            {
                throw new ArgumentNullException("request");
            }

            UIElement root =_rootVisual.Value as UIElement;
            if(root != null)
            {
                // atanask:
                // request.Mode == FocusNavigationDirection.First will navigate to the fist tabstop including root
                // request.Mode == FocusNavigationDirection.Last will navigate to the last tabstop including root
                traversed = root.MoveFocus(request);
            }

            return traversed;
        }

        bool IKeyboardInputSink.TabInto(TraversalRequest request)
        {
            if(request == null)
            {
                throw new ArgumentNullException("request");
            }

            return TabIntoCore(request);
        }

        /// <summary>
        ///     The property should start with a null value.  The component's
        ///     container will set this property to a non-null value before
        ///     any other methods are called.  It may be set multiple times,
        ///     and should be set to null before disposal.
        /// </summary>
        /// <remarks>
        ///     Setting KeyboardInputSite is not available in Internet Zone.
        ///     We explicitly don't make this property overridable as we want to keep the
        ///     precise implementation as a smart field for _keyboardInputSite fixed.
        ///     By making the property protected, implementors can still call into it
        ///     when required. Notice as calls are made through the IKIS interface,
        ///     there's still a way for advanced developers to override the behavior by
        ///     re-implementing the interface.
        /// </remarks>
        protected IKeyboardInputSite KeyboardInputSiteCore
        {
            get
            {
                return _keyboardInputSite;
            }

            set
            {

                _keyboardInputSite = value;
            }
        }

        IKeyboardInputSite IKeyboardInputSink.KeyboardInputSite
        {
            get
            {
                return KeyboardInputSiteCore;
            }

            set
            {
                KeyboardInputSiteCore = value;
            }
        }

        /// <summary>
        ///     This method is called whenever one of the component's
        ///     mnemonics is invoked.  The message must either be WM_KEYDOWN
        ///     or WM_SYSKEYDOWN.  It's illegal to modify the MSG structrure,
        ///     it's passed by reference only as a performance optimization.
        ///     If this component contains child components, the container
        ///     OnMnemonic will need to call the child's OnMnemonic method.
        /// </summary>
        protected virtual bool OnMnemonicCore(ref MSG msg, ModifierKeys modifiers)
        {
//             VerifyAccess();
            switch((WindowMessage)msg.message)
            {
                case WindowMessage.WM_SYSCHAR:
                case WindowMessage.WM_SYSDEADCHAR:
                    string text = new string((char)msg.wParam, 1);
                    if ((text != null) && (text.Length > 0))
                    {
                        // We have to work around an ordering issue with mnemonic processing.
                        //
                        // Imagine you have a top level menu with _File & _Project and under _File you have _Print.
                        // If the user pressses Alt+F,P you would expect _Print to be triggered
                        // however if the top level window processes the mnemonic first
                        // it will trigger _Project instead.
                        //
                        // One way to work around this would be for the top level window to notice that
                        // keyboard focus is in another root window & not handle the mnemonics.  The popup
                        // window would then get a chance to process the mnemonic and _Print would be triggered.
                        //
                        // This doesn't work out becasue the popup window is no-activate, so it doesn't have Win32 focus
                        // and it will bail out of OnPreprocessMessage before it handles the mnemonic.
                        //
                        // Instead the top level window should delegate OnMnemonic directly to the mnemonic scope window
                        // instead of processing it here.  This will let the Popup handle mnemonics instead of the top-
                        // level window.
                        //
                        // The mnemonic scope window is defined as the window with WPF keyboard focus.
                        //
                        // This is a behavioral breaking change, so we've decided to only do it when IsInExclusiveMenuMode
                        // is true to force the user to opt-in.
                        DependencyObject focusObject = Keyboard.FocusedElement as DependencyObject;
                        HwndSource mnemonicScope = (focusObject == null ? null : PresentationSource.CriticalFromVisual(focusObject) as HwndSource);
                        if (mnemonicScope != null &&
                            mnemonicScope != this &&
                            IsInExclusiveMenuMode)
                        {
                            return ((IKeyboardInputSink)mnemonicScope).OnMnemonic(ref msg, modifiers);
                        }

                        if (AccessKeyManager.IsKeyRegistered(this, text))
                        {
                            AccessKeyManager.ProcessKey(this, text, false);

                            // is it ok not to update _lastKeyboardMessage?
                            return true;
                        }
                    }
                    // these are OK
                    break;

                case WindowMessage.WM_CHAR:
                case WindowMessage.WM_DEADCHAR:
                    // these are OK
                    break;

                default:
                    throw new ArgumentException(SR.Get(SRID.OnlyAcceptsKeyMessages));
            }

            // We record the last message that was processed by us.
            // This is also checked in WndProc processing to prevent double processing.
            _lastKeyboardMessage = msg;

            // The bubble will take care of access key processing for this HWND.  Call
            // the IKIS children unless we are in menu mode.
            if (_keyboardInputSinkChildren != null && !IsInExclusiveMenuMode)
            {
                foreach ( HwndSourceKeyboardInputSite childSite in _keyboardInputSinkChildren )
                {
                    if (((IKeyboardInputSite)childSite).Sink.OnMnemonic(ref msg, modifiers))
                        return true;
                }
            }
            return false;
        }

        bool IKeyboardInputSink.OnMnemonic(ref MSG msg, ModifierKeys modifiers)
        {
            return OnMnemonicCore(ref msg, modifiers);
        }

        /// <summary>
        ///     Gives the component a chance to process keyboard input messages
        ///     WM_CHAR, WM_SYSCHAR, WM_DEADCHAR or WM_SYSDEADCHAR before calling OnMnemonic.
        ///     Will return true if "handled" meaning don't pass it to OnMnemonic.
        ///     The message must be WM_CHAR, WM_SYSCHAR, WM_DEADCHAR or WM_SYSDEADCHAR.
        ///     It is illegal to modify the MSG structure, it's passed by reference
        ///     only as a performance optimization.
        /// </summary>
        protected virtual bool TranslateCharCore(ref MSG msg, ModifierKeys modifiers)
        {
            if(HasFocus || IsInExclusiveMenuMode)
                return false;

            IKeyboardInputSink focusSink = this.ChildSinkWithFocus;
            if(null != focusSink)
            {
                return focusSink.TranslateChar(ref msg, modifiers);
            }
            return false;
        }

        bool IKeyboardInputSink.TranslateChar(ref MSG msg, ModifierKeys modifiers)
        {
            return TranslateCharCore(ref msg, modifiers);
        }

        /// <summary>
        ///
        /// </summary>
        protected virtual bool HasFocusWithinCore()
        {
            if(HasFocus)
            {
                return true;
            }
            else
            {
                if (null == _keyboardInputSinkChildren)
                    return false;

                foreach (HwndSourceKeyboardInputSite site in _keyboardInputSinkChildren)
                {
                    if (((IKeyboardInputSite)site).Sink.HasFocusWithin())
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        bool IKeyboardInputSink.HasFocusWithin()
        {
            return HasFocusWithinCore();
        }

        /// <summary>
        ///     The RestoreFocusMode for the window.
        /// </summary>
        /// <remarks>
        ///     This property can only be set at construction time via the
        ///     HwndSourceParameters.RestoreFocusMode property.
        /// </remarks>
        public RestoreFocusMode RestoreFocusMode
        {
            get
            {
                return _restoreFocusMode;
            }
        }

        /// <summary>
        ///     The default value for the AcquireHwndFocusInMenuMode setting.
        /// <summary>
        public static bool DefaultAcquireHwndFocusInMenuMode
        {
            get
            {
                if(!_defaultAcquireHwndFocusInMenuMode.HasValue)
                {
                    // The default value is true, for compat.
                    _defaultAcquireHwndFocusInMenuMode = true;
                }

                return _defaultAcquireHwndFocusInMenuMode.Value;
            }

            set
            {
                _defaultAcquireHwndFocusInMenuMode = value;
            }
        }

        /// <summary>
        ///     The AcquireHwndFocusInMenuMode setting for the window.
        /// </summary>
        /// <remarks>
        ///     This property can only be set at construction time via the
        ///     HwndSourceParameters.AcquireHwndFocusInMenuMode property.
        /// </remarks>
        public bool AcquireHwndFocusInMenuMode
        {
            get
            {
                return _acquireHwndFocusInMenuMode;
            }
        }

        /// <summary>
        ///   The method is not part of the interface (IKeyboardInputSink).
        /// </summary>
        /// <param name="site">The Site that containes the sink to unregister</param>
        internal void CriticalUnregisterKeyboardInputSink(HwndSourceKeyboardInputSite site)
        {
            if(_isDisposed)
                return;

            if (null != _keyboardInputSinkChildren)
            {
                if (!_keyboardInputSinkChildren.Remove(site))
                {
                    throw new InvalidOperationException(SR.Get(SRID.KeyboardSinkNotAChild));
                }
            }
        }

        IKeyboardInputSink ChildSinkWithFocus
        {
            get
            {
                IKeyboardInputSink ikis=null;

                if(null == _keyboardInputSinkChildren)
                    return null;

                foreach (HwndSourceKeyboardInputSite site in _keyboardInputSinkChildren)
                {
                    IKeyboardInputSite isite = (IKeyboardInputSite)site;

                    if (isite.Sink.HasFocusWithin())
                    {
                        ikis = isite.Sink;
                        break;
                    }
                }
                // This private property should only be called correctly.
                Debug.Assert(null!=ikis, "ChildSinkWithFocus called when none had focus");
                return ikis;
            }
        }

        internal bool CriticalTranslateAccelerator(ref MSG msg, ModifierKeys modifiers)
        {
            switch ((WindowMessage)msg.message)
            {
                case WindowMessage.WM_KEYUP:
                case WindowMessage.WM_KEYDOWN:
                case WindowMessage.WM_SYSKEYUP:
                case WindowMessage.WM_SYSKEYDOWN:
                    // these are OK
                    break;

                default:
                    throw new ArgumentException(SR.Get(SRID.OnlyAcceptsKeyMessages));
            }

            if (_keyboard == null)
                return false;

            bool handled = false;

            // TranslateAccelerator is called recursively on child Hwnds (Source & Host)
            // If this is the first Avalon TranslateAccelerator processing then we send the
            // key to be processed to the standard Avalon Input Filters and stuff.
            if (PerThreadData.TranslateAcceleratorCallDepth == 0)
            {
                // We record the last message that was processed by us.
                // This is also checked in WndProc processing to prevent double processing.
                // TranslateAcclerator is called from the pump before DispatchMessage
                // and the WndProc is called from DispatchMessage.   We have processing
                // in both places.  If we run the pump we process keyboard message here.
                // If we don't own the pump we process them in HwndKeyboardInputProvider.WndProc.
                _lastKeyboardMessage = msg;

                //  NORMAL AVALON KEYBOARD INPUT CASE
                // If this is the top most Avalon window (it might be a child Hwnd
                // but no Avalon windows above it).  And focus is on this window then
                // do the Normal Avalon Keyboard input Processing.
                if (HasFocus || IsInExclusiveMenuMode)
                {
                    _keyboard.Value.ProcessKeyAction(ref msg, ref handled);
                }
                // ELSE the focus is probably in but not on this HwndSource.
                // Beware: It is possible that someone calls IKIS.TranslateAccelerator() while the focus is
                //   somewhere entirely outside.
                // Do the once only message input filters etc and Tunnel/Bubble down
                // to the element that contains the child window with focus.
                // The Child HwndHost object will hook OnPreviewKeyDown() etc
                // to make the transition to its TranslateAccelerator() between the
                // tunnel and the bubble.
                else
                {
                    IKeyboardInputSink focusSink = ChildSinkWithFocus; // can be null!
                    IInputElement focusElement = (IInputElement)focusSink;

                    try {
                        PerThreadData.TranslateAcceleratorCallDepth += 1;
                        Keyboard.PrimaryDevice.ForceTarget = focusElement;
                       _keyboard.Value.ProcessKeyAction(ref msg, ref handled);
                    }
                    finally
                    {
                        Keyboard.PrimaryDevice.ForceTarget = null;
                        PerThreadData.TranslateAcceleratorCallDepth -= 1;
                    }
                }
}
            // ELSE we have seen this MSG before, we are HwndSource decendant of an
            // HwndSource (that ancestor presumably did the processing above).
            // Here we raise the tunnel/bubble events without the once only keyboard
            // input filtering.
            else
            {
                int virtualKey = HwndKeyboardInputProvider.GetVirtualKey(msg.wParam, msg.lParam);
                int scanCode = HwndKeyboardInputProvider.GetScanCode(msg.wParam, msg.lParam);
                bool isExtendedKey = HwndKeyboardInputProvider.IsExtendedKey(msg.lParam);
                Key key = KeyInterop.KeyFromVirtualKey(virtualKey);

                RoutedEvent keyPreviewEvent=null;
                RoutedEvent keyEvent=null;
                switch ((WindowMessage)msg.message)
                {
                case WindowMessage.WM_KEYUP:
                case WindowMessage.WM_SYSKEYUP:
                    keyPreviewEvent = Keyboard.PreviewKeyUpEvent;
                    keyEvent = Keyboard.KeyUpEvent;
                    break;
                case WindowMessage.WM_KEYDOWN:
                case WindowMessage.WM_SYSKEYDOWN:
                    keyPreviewEvent = Keyboard.PreviewKeyDownEvent;
                    keyEvent = Keyboard.KeyDownEvent;
                    break;
                }

                bool hasFocus = HasFocus;
                IKeyboardInputSink focusSink = (hasFocus || IsInExclusiveMenuMode) ? null : ChildSinkWithFocus;
                IInputElement focusElement = focusSink as IInputElement;
                // focusElement may be null, in which case Target is just "focus", but we use it only if it's an
                // element within this HwndSource. It is possible that someone calls IKIS.TranslateAccelerator()
                // on a nested HwndSource while the focus is somewhere entirely outside.
                // HasFocus implies the focused element should be within this HwndSource, but unfortunately
                // we allow 'split focus', at least for popup windows; that's why check explicitly.
                // Note that KeyboardDevice.Target will likely be the ForceTarget corresponding to the
                // container of this HwndSource. That's why we look at the real FocusedElement.
                if (focusElement == null && hasFocus)
                {
                    focusElement = Keyboard.PrimaryDevice.FocusedElement;
                    if (focusElement != null &&
                        PresentationSource.CriticalFromVisual((DependencyObject)focusElement) != this)
                    {
                        focusElement = null;
                    }
                }

                try {
                    Keyboard.PrimaryDevice.ForceTarget = focusSink as IInputElement;

                    if (focusElement != null)
                    {
                        KeyEventArgs tunnelArgs = new KeyEventArgs(Keyboard.PrimaryDevice, this, msg.time, key);
                        tunnelArgs.ScanCode = scanCode;
                        tunnelArgs.IsExtendedKey = isExtendedKey;
                        tunnelArgs.RoutedEvent = keyPreviewEvent;
                        focusElement.RaiseEvent(tunnelArgs);

                        handled = tunnelArgs.Handled;
                    }
                    if (!handled)
                    {
                        KeyEventArgs bubbleArgs = new KeyEventArgs(Keyboard.PrimaryDevice, this, msg.time, key);
                        bubbleArgs.ScanCode = scanCode;
                        bubbleArgs.IsExtendedKey = isExtendedKey;
                        bubbleArgs.RoutedEvent=keyEvent;
                        if(focusElement != null)
                        {
                            focusElement.RaiseEvent(bubbleArgs);
                            handled = bubbleArgs.Handled;
                        }

                        if (!handled)
                        {
                            // Raise the TranslateAccelerator event on the
                            // InputManager to allow keyboard navigation to
                            // happen on a descendent HwndSource
                            InputManager.UnsecureCurrent.RaiseTranslateAccelerator(bubbleArgs);
                            handled = bubbleArgs.Handled;
                        }
                    }
                }
                finally
                {
                    Keyboard.PrimaryDevice.ForceTarget = null;
                }
            }

            return handled;
        }

        // MITIGATION: HANDLED_KEYDOWN_STILL_GENERATES_CHARS
        // Go back to accepting character messages.  This method is posted
        // to the dispatcher when char messages are disable.
        internal static object RestoreCharMessages(object unused)
        {
            _eatCharMessages = false;
            return null;
        }

#endregion IKeyboardInputSink


        internal bool IsRepeatedKeyboardMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg != _lastKeyboardMessage.message)
                return false;
            if (hwnd != _lastKeyboardMessage.hwnd)
                return false;
            if (wParam != _lastKeyboardMessage.wParam)
                return false;
            if (lParam != _lastKeyboardMessage.lParam)
                return false;
            return true;
        }

        /// <summary>
        ///    This event handler is called from HwndWrapper when it is Disposing.
        /// </summary>
        // This could happen if someone calls his Dispose before (or instead
        // of) our dispose.  Or, more likely, the real window was killed by
        // something like the user clicking the close box.
        private void OnHwndDisposed(object sender, EventArgs args)
        {
            // This method is called from the HwndWrapper.Dispose().
            // So make sure we don't call HwndWrapper.Dispose().
            _inRealHwndDispose = true;
            Dispose();
        }

        /// <summary>
        ///    Called after the last window message is processed.
        /// </summary>

        // HwndSource is required to continue certain operations while,
        // and even after, Dispose runs.  HwndSource is resposible for
        // calling WndProcHooks with every message the window sees.
        // Including: WM_CLOSE, WM_DESTROY, WM_NCDESTROY.  The doc says
        // WM_NCDESTROY is the very last message, so we can release the
        // Hooks after that.
        // This assumes the Context.Access() is held.
        private void OnNoMoreWindowMessages()
        {
            _hooks = null;
        }

        private void OnShutdownFinished(object sender, EventArgs args)
        {
            // Note: We are already in the context being disposed.
            Dispose();
        }

        //
        // NOTE: shutdown order is very important.  Review any changes
        // carefully.
        //
        private void Dispose(bool disposing)
        {
            if(disposing)
            {
                // Make sure all access is synchronized.
//                 this.VerifyAccess();

                if (!_isDisposing)
                {
                    // _isDisposing is a guard against re-entery into this
                    // routine.  We fire Dispose and SourceChanged (RootVisual
                    // change) events which could cause re-entery.
                    _isDisposing = true;

                    // Notify listeners that we are being disposed.  We do this
                    // before we dispose our internal stuff, in case the event
                    // listener needs to access something.
                    if(Disposed != null)
                    {
                        try
                        {
                            Disposed(this, EventArgs.Empty);
                        }
#pragma warning disable 56500
                        // We can't tolerate an exception thrown by third-party code to
                        // abort our Dispose half-way through.  So we just eat it.
                        catch
                        {
                        }
#pragma warning restore 56500
                        Disposed = null;
                    }

                    // Remove any listeners of the ContentRendered event
                    ClearContentRenderedListeners();

                    // Clear the root visual.  This will raise a SourceChanged
                    // event to registered listeners.
                    RootVisualInternal = null;
                    RemoveSource();

                    // Unregister ourselves if we are a registered KeyboardInputSink.
                    // Use the property instead of the backing field in case a subclass has overridden it.
                    IKeyboardInputSite keyboardInputSite = ((IKeyboardInputSink)this).KeyboardInputSite;
                    if (keyboardInputSite != null)
                    {
                        keyboardInputSite.Unregister();
                        ((IKeyboardInputSink)this).KeyboardInputSite = null;
                    }
                    _keyboardInputSinkChildren = null;

                    if (!_inRealHwndDispose)
                    {
                        
                        // Disposing the stylus provider can only be done here when
                        // we're not actually in an Hwnd WM_NCDESTROY scenario as
                        // we're then guaranteed that the HWND is still alive.
                        
                        // In situations where the PenThread is busy, this dispose can
                        // cause re-entrancy.  If this re-entrancy triggers a layout
                        // WPF can throw if the HwndTarget has been disposed since there
                        // will no longer be a CompositionTarget for this HwndSource.
                        // Dispose here so any re-entrancy still has a valid
                        // HwndTarget.
                        DisposeStylusInputProvider();
                    }

                    // Our general shut-down principle is to destroy the window
                    // and let the individual HwndXXX components respons to WM_DESTROY.
                    //
                    // (see comment above about disposing the HwndStylusInputProvider)
                    //
                    {
                        if (_hwndTarget != null)
                        {
                            _hwndTarget.Dispose();
                            _hwndTarget = null;
                        }

                        if (_hwndWrapper != null)
                        {
                            // Revoke the drop target.
                            if (_hwndWrapper.Handle != IntPtr.Zero && _registeredDropTargetCount > 0)
                            {
                                // This call is safe since DragDrop.RevokeDropTarget is checking the unmanged
                                // code permission.
                                DragDrop.RevokeDropTarget(_hwndWrapper.Handle);
                                _registeredDropTargetCount--;
                            }

                            // Remove our HwndWrapper.Dispose() hander.
                            _hwndWrapper.Disposed -= new EventHandler(OnHwndDisposed);

                            if (!_inRealHwndDispose)
                            {
                                _hwndWrapper.Dispose();
                            }

                            // Don't null out _hwndWrapper after the Dispose().
                            // Dispose() will start destroying the Window but we
                            // still need to talk to it during that process while
                            // the WM_ msgs arrive.
                        }
                    }

                    if(_mouse != null)
                    {
                        _mouse.Value.Dispose();
                        _mouse = null;
                    }

                    if(_keyboard != null)
                    {
                        _keyboard.Value.Dispose();
                        _keyboard = null;
                    }

                    if (_appCommand != null)
                    {
                        _appCommand.Value.Dispose();
                        _appCommand = null;
                    }

                    if(null != _weakShutdownHandler)
                    {
                        _weakShutdownHandler.Dispose();
                        _weakShutdownHandler = null;
                    }

                    if(null != _weakPreprocessMessageHandler)
                    {
                        _weakPreprocessMessageHandler.Dispose();
                        _weakPreprocessMessageHandler = null;
                    }

                    // We wait to set the "_isDisposed" flag until after the
                    // Disposed, SourceChange (RootVisual=null), etc. events
                    // have fired.  We want to remain functional should their
                    // handlers call methods on us.
                    //
                    // Note: as the HwndWrapper shuts down, the final few messages
                    // will continue to pass through our WndProc hook.
                    _isDisposed = true;
                }
            }
        }

        private void CheckDisposed(bool verifyAccess)
        {
            if(verifyAccess)
            {
//                 this.VerifyAccess();
            }

            if(_isDisposed)
            {
                throw new ObjectDisposedException(null, SR.Get(SRID.HwndSourceDisposed));
            }
        }

        private bool IsUsable
        {
            get
            {
                return _isDisposed == false &&
                       _hwndTarget != null &&
                       _hwndTarget.IsDisposed == false;
            }
        }

        private bool HasFocus
        {
            get
            {
                return UnsafeNativeMethods.GetFocus() == CriticalHandle;
            }
        }

        private static bool IsValidSizeToContent(SizeToContent value)
        {
            return value == SizeToContent.Manual ||
                   value == SizeToContent.Width  ||
                   value == SizeToContent.Height ||
                   value == SizeToContent.WidthAndHeight;
        }

        class ThreadDataBlob
        {
            public int TranslateAcceleratorCallDepth;
        }

        private static ThreadDataBlob PerThreadData
        {
            get
            {
                ThreadDataBlob data;
                object obj = Thread.GetData(_threadSlot);
                if(null == obj)
                {
                    data = new ThreadDataBlob();
                    Thread.SetData(_threadSlot, data);
                }
                else
                {
                    data = (ThreadDataBlob) obj;
                }
                return data;
            }
        }

#region WeakEventHandlers

        private class WeakEventDispatcherShutdown: WeakReference
        {
            public WeakEventDispatcherShutdown(HwndSource source, Dispatcher that): base(source)
            {
                _that = that;
                _that.ShutdownFinished += new EventHandler(this.OnShutdownFinished);
            }

            public void OnShutdownFinished(object sender, EventArgs e)
            {
                HwndSource source = this.Target as HwndSource;
                if(null != source)
                {
                    source.OnShutdownFinished(sender, e);
                }
                else
                {
                    Dispose();
                }
            }

            public void Dispose()
            {
                if(null != _that)
                {
                    _that.ShutdownFinished-= new EventHandler(this.OnShutdownFinished);
                }
            }

            private Dispatcher _that;
        }

        private class WeakEventPreprocessMessage: WeakReference
        {
            public WeakEventPreprocessMessage(HwndSource source, bool addToFront): base(source)
            {
                _addToFront = addToFront;
                _handler = new ThreadMessageEventHandler(this.OnPreprocessMessage);

                if(addToFront)
                {
                    ComponentDispatcher.CriticalAddThreadPreprocessMessageHandlerFirst(_handler);
                }
                else
                {
                    ComponentDispatcher.ThreadPreprocessMessage += _handler;
                }
            }

            public void OnPreprocessMessage(ref MSG msg, ref bool handled)
            {
                HwndSource source = this.Target as HwndSource;
                if(null != source)
                {
                    source.OnPreprocessMessageThunk(ref msg, ref handled);
                }
                else
                {
                    Dispose();
                }
            }


            public void Dispose()
            {
                if(_addToFront)
                {
                    ComponentDispatcher.CriticalRemoveThreadPreprocessMessageHandlerFirst(_handler);
                }
                else
                {
                    ComponentDispatcher.ThreadPreprocessMessage -= _handler;
                }

                _handler = null;
            }

            private bool _addToFront;
            private ThreadMessageEventHandler _handler;
        }

#endregion WeakEventHandlers

        private object                      _constructionParameters; // boxed HwndSourceParameters

        private bool                        _isDisposed = false;
        private bool                        _isDisposing = false;
        private bool                        _inRealHwndDispose = false;

        private bool                        _adjustSizingForNonClientArea;
        private bool                        _treatAncestorsAsNonClientArea;

        private bool                        _myOwnUpdate;
        private bool                        _isWindowInMinimizeState = false;

        private int                         _registeredDropTargetCount;

        private SizeToContent               _sizeToContent = SizeToContent.Manual;
        private Size?                       _previousSize;

        private HwndWrapper                 _hwndWrapper;

        private HwndTarget                  _hwndTarget;

        private SecurityCriticalDataForSet<Visual>                      _rootVisual;

        private event HwndSourceHook _hooks;

        private SecurityCriticalDataClass<HwndMouseInputProvider>      _mouse;

        private SecurityCriticalDataClass<HwndKeyboardInputProvider>   _keyboard;

        private SecurityCriticalDataClass<IStylusInputProvider>        _stylus;

        private SecurityCriticalDataClass<HwndAppCommandInputProvider> _appCommand;

        WeakEventDispatcherShutdown _weakShutdownHandler;
        WeakEventPreprocessMessage _weakPreprocessMessageHandler;
        WeakEventPreprocessMessage _weakMenuModeMessageHandler;

        private static System.LocalDataStoreSlot _threadSlot;

        private RestoreFocusMode _restoreFocusMode;

        [ThreadStatic]
        private static bool? _defaultAcquireHwndFocusInMenuMode;
        private bool _acquireHwndFocusInMenuMode;

        private MSG                         _lastKeyboardMessage;
        private List<HwndSourceKeyboardInputSite> _keyboardInputSinkChildren;

        // Be careful about accessing this field directly.
        // It's bound to IKeyboardInputSink.KeyboardInputSite, so if a derived class overrides
        // that property then this field will be incorrect.
        private IKeyboardInputSite          _keyboardInputSite = null;

        private HwndWrapperHook             _layoutHook;

        private HwndWrapperHook             _inputHook;

        private HwndWrapperHook             _hwndTargetHook;

        private HwndWrapperHook             _publicHook;

        // MITIGATION: HANDLED_KEYDOWN_STILL_GENERATES_CHARS
        //
        // Avalon relies on the policy that if you handle the KeyDown
        // event, you will not get the TextInput events caused by
        // pressing the key.  This is generally implemented because the
        // message pump calls ComponentDispatcher.RaiseThreadMessage and
        // we return whether or not the WM_KEYDOWN message was handled,
        // and the message pump will only call TranslateMessage() if the
        // WM_KEYDOWN was not handled.  However, naive message pumps don't
        // call ComponentDispatcher.RaiseThreadMessage, and always call
        // TranslateMessage, so the WM_CHAR is generated no matter what.
        // The best work around we could think of was to eat the WM_CHAR
        // messages and not report them to Avalon.
        //
        [ThreadStatic]
        internal static bool _eatCharMessages; // used from HwndKeyboardInputProvider
}
}
