// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security;
using MS.Internal;
using MS.Internal.Interop;
using MS.Internal.PresentationCore;                        // SecurityHelper
using MS.Win32;
using MS.Utility;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Interop
{
    internal sealed class HwndMouseInputProvider : DispatcherObject, IMouseInputProvider, IDisposable
    {
        internal HwndMouseInputProvider(HwndSource source)
        {
            _site = new SecurityCriticalDataClass<InputProviderSite>(InputManager.Current.RegisterInputProvider(this));

            _source = new SecurityCriticalDataClass<HwndSource>(source);

            // MITIGATION_SETCURSOR
            _setCursorState = SetCursorState.SetCursorNotReceived;
            _haveCapture = false;
            _queryCursorOperation = null;
        }

        public void Dispose()
        {
            if(_site != null)
            {
                //Console.WriteLine("Disposing");

                // Cleanup the mouse tracking.
                StopTracking(_source.Value.CriticalHandle);

                // If we have capture, release it.
                try
                {
                    Debug.Assert(null != _source && null != _source.Value);

                    if(_source.Value.HasCapture )
                    {
                        SafeNativeMethods.ReleaseCapture();
                    }
                }
                catch(System.ComponentModel.Win32Exception)
                {
                    System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: Dispose: GetCapture failed!");
                }

                // Possibly deactivate the mouse input stream since our window is going away.
                try
                {
                    IntPtr hwndCapture = SafeNativeMethods.GetCapture();
                    PossiblyDeactivate(hwndCapture, false);
                }
                catch(System.ComponentModel.Win32Exception)
                {
                    System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: Dispose: GetCapture failed!");
                }

                _site.Value.Dispose();
                _site = null;
            }
            _source = null;
        }

        bool IInputProvider.ProvidesInputForRootVisual(Visual v)
        {
            Debug.Assert(null != _source && null != _source.Value);

            return _source.Value.RootVisual == v;
        }

        void IInputProvider.NotifyDeactivate()
        {
            if(_active)
            {
                StopTracking(_source.Value.CriticalHandle);

                _active = false;
            }
        }

        // Set the real cursor
        bool IMouseInputProvider.SetCursor(Cursor cursor)
        {
            bool success = false;

            // MITIGATION_SETCURSOR
            // If the _setCursortState flag is set to disabled, disallow the operation
            // - this is set if we have received a WM_MOUSEMOVE without a prior WM_SETCURSOR, a scenario
            //   that occurs during "Help Mode" where Win32 has set the cursor to an arrow with a question mark
            //   and does not want the underlying window changing it.
            if(_setCursorState != SetCursorState.SetCursorDisabled)
            {
                try
                {
                    SafeNativeMethods.SetCursor( cursor.Handle );
                    success = true;
                }
                catch(System.ComponentModel.Win32Exception)
                {
                    System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: SetCursor failed!");
                }
}

            return success;
        }

        bool IMouseInputProvider.CaptureMouse()
        {
            if(_isDwmProcess)
            {
                return true;
            }

            bool success = true;

            Debug.Assert(null != _source && null != _source.Value);

            try
            {
                SafeNativeMethods.SetCapture(new HandleRef(this,_source.Value.CriticalHandle));
                IntPtr capture = SafeNativeMethods.GetCapture();
                if (capture != _source.Value.CriticalHandle)
                {
                    success = false;
                }
            }
            catch(System.ComponentModel.Win32Exception)
            {
                System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: SetCapture or GetCapture failed!");

                success = false;
            }

            if(success)
            {
                _haveCapture = true;
            }

            // WORKAROUND for the fact that WM_MOUSELEAVE not sent when capture changes
            if (success && !_active)
            {
                NativeMethods.POINT ptCursor = new NativeMethods.POINT();

                success = UnsafeNativeMethods.TryGetCursorPos(ptCursor);

                if(success)
                {
                    try
                    {
                        SafeNativeMethods.ScreenToClient(new HandleRef(this, _source.Value.CriticalHandle), ptCursor);
                    }
                    catch(System.ComponentModel.Win32Exception)
                    {
                        System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: ScreenToClient failed!");

                        success = false;
                    }

                    if(success)
                    {
                        ReportInput(_source.Value.CriticalHandle,
                                    InputMode.Foreground,
                                    _msgTime,
                                    RawMouseActions.AbsoluteMove,
                                    ptCursor.x,
                                    ptCursor.y,
                                    0);
                    }
                }
            }

            return success;
        }

        void IMouseInputProvider.ReleaseMouseCapture()
        {
            // MITIGATION_SETCURSOR
            _haveCapture = false;

            if(_isDwmProcess)
            {
                return;
            }

            try
            {
                SafeNativeMethods.ReleaseCapture();
            }
            catch(System.ComponentModel.Win32Exception)
            {
                System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: ReleaseCapture failed!");
            }
        }

        /// <summary>
        /// GetIntermediatePoints
        /// </summary>
        /// <param name="relativeTo">points will be returned relative to this element</param>
        /// <param name="points">relative points prior to the current mouse point (including the current one)</param>
        /// <returns>Count of points if succeeded , -1 if error</returns>
        int IMouseInputProvider.GetIntermediatePoints(IInputElement relativeTo, Point[] points)
        {
            int cpt = -1;

            try
            {
                if (points != null && relativeTo != null)
                {
                    DependencyObject containingVisual = InputElement.GetContainingVisual(relativeTo as DependencyObject);
                    HwndSource inputSource = PresentationSource.FromDependencyObject(containingVisual) as HwndSource;

                    if (inputSource != null)
                    {
                        int nVirtualWidth  = UnsafeNativeMethods.GetSystemMetrics(SM.CXVIRTUALSCREEN);
                        int nVirtualHeight = UnsafeNativeMethods.GetSystemMetrics(SM.CYVIRTUALSCREEN);
                        int nVirtualLeft   = UnsafeNativeMethods.GetSystemMetrics(SM.XVIRTUALSCREEN);
                        int nVirtualTop    = UnsafeNativeMethods.GetSystemMetrics(SM.YVIRTUALSCREEN);
                        uint mode           = NativeMethods.GMMP_USE_DISPLAY_POINTS;

                        NativeMethods.MOUSEMOVEPOINT mp_in  = new NativeMethods.MOUSEMOVEPOINT();
                        NativeMethods.MOUSEMOVEPOINT[] mp_out = new NativeMethods.MOUSEMOVEPOINT[64];

                        mp_in.x = _latestMovePoint.x;
                        mp_in.y = _latestMovePoint.y;
                        mp_in.time = 0;     // don't use a timestamp here, none of the timestamps we have actually work

                        // get all points in the system buffer
                        int n = UnsafeNativeMethods.GetMouseMovePointsEx((uint)(Marshal.SizeOf(mp_in)), ref mp_in, mp_out, 64, mode);
                        if (n == -1)
                        {
                            throw new System.ComponentModel.Win32Exception();
                        }

                        // decide which points to return
                        cpt = 0;
                        bool ignore = true;
                        for (int i = 0; i < n && cpt < points.Length; i++)
                        {
                            // ignore points that happened after the latest MouseMove
                            if (ignore)
                            {
                                if (mp_out[i].time < _latestMovePoint.time ||
                                    (mp_out[i].time == _latestMovePoint.time &&
                                     mp_out[i].x == _latestMovePoint.x &&
                                     mp_out[i].y == _latestMovePoint.y))
                                {
                                    ignore = false;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            // stop when we reach the previous MouseMove point,
                            // or a point that happened earlier
                            if (mp_out[i].time < _previousMovePoint.time ||
                                (mp_out[i].time == _previousMovePoint.time &&
                                 mp_out[i].x == _previousMovePoint.x &&
                                 mp_out[i].y == _previousMovePoint.y))
                            {
                                break;
                            }

                            Point currentPosition = new Point(mp_out[i].x, mp_out[i].y);

                            switch (mode)
                            {
                                case NativeMethods.GMMP_USE_DISPLAY_POINTS:
                                    {
                                        if (currentPosition.X > 32767)
                                            currentPosition.X -= 65536;

                                        if (currentPosition.Y > 32767)
                                            currentPosition.Y -= 65536;
                                    }
                                    break;

                                case NativeMethods.GMMP_USE_HIGH_RESOLUTION_POINTS:
                                    {
                                        currentPosition.X = ((currentPosition.X * (nVirtualWidth - 1)) - (nVirtualLeft * 65536)) / nVirtualWidth;
                                        currentPosition.Y = ((currentPosition.Y * (nVirtualHeight - 1)) - (nVirtualTop * 65536)) / nVirtualHeight;
                                    }
                                    break;
                            }

                            currentPosition = PointUtil.ScreenToClient(currentPosition, inputSource);
                            currentPosition = PointUtil.ClientToRoot(currentPosition, inputSource);

                            // Translate the point from the root to the visual.
                            GeneralTransform gDown = inputSource.RootVisual.TransformToDescendant(VisualTreeHelper.GetContainingVisual2D(containingVisual));
                            if (gDown != null)
                            {
                                //  should we throw if the point could not be transformed?
                                gDown.TryTransform(currentPosition, out currentPosition);
                            }

                            points[cpt++] = currentPosition;
                        }
                    }
                }
            }
            catch(System.ComponentModel.Win32Exception)
            {
                System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: GetIntermediatePoints failed!");

                cpt = -1;
            }

            return cpt;
        }


        internal IntPtr FilterMessage(IntPtr hwnd, WindowMessage msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            IntPtr result = IntPtr.Zero ;

            // It is possible to be re-entered during disposal.  Just return.
            if(null == _source || null == _source.Value)
            {
                return result;
            }
            /*
            NativeMethods.POINT ptCursor = new NativeMethods.POINT();
            int messagePos = 0;
            try
            {
                messagePos = SafeNativeMethods.GetMessagePos();
            }
            catch(System.ComponentModel.Win32Exception)
            {
                System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: GetMessagePos failed!");
            }

            ptCursor.x = NativeMethods.SignedLOWORD(messagePos);
            ptCursor.y = NativeMethods.SignedHIWORD(messagePos);
            Console.WriteLine("HwndMouseInputProvider.FilterMessage: hwnd: {0} msg: {1} wParam: {2} lParam: {3} MessagePos: ({4},{5})", hwnd, msg, wParam, lParam, ptCursor.x, ptCursor.y);
            */
            _msgTime = 0;
            try
            {
                _msgTime = SafeNativeMethods.GetMessageTime();
            }
            catch(System.ComponentModel.Win32Exception)
            {
                System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: GetMessageTime failed!");
            }

            // Remove focus hacks once DWM DesktopSource is implemented.
            if (msg == WindowMessage.WM_MOUSEQUERY)
            {
                if(!_isDwmProcess)
                {
                    _isDwmProcess = true;
                }

                unsafe
                {
                    // Currently only sending WM_MOUSEMOVE through until we rip out the prototype bits.
                    UnsafeNativeMethods.MOUSEQUERY* pmq = (UnsafeNativeMethods.MOUSEQUERY*)lParam;
                    if((WindowMessage)pmq->uMsg == WindowMessage.WM_MOUSEMOVE)
                    {
                        msg = (WindowMessage)pmq->uMsg;
                        wParam = pmq->wParam;
                        lParam = MakeLPARAM(pmq->ptX, pmq->ptY);
                    }
                }
            }

            switch(msg)
            {
                // Compatibility Note:
                //
                // WM_POINTERUP, WM_POINTERDOWN
                //
                // These messages were introduced in Win8 to unify the various
                // mechanisms for processing events from pointing devices,
                // including stylus, touch, mouse, etc.  WPF does not
                // currently support these messages; we still rely on the
                // classic WM_MOUSE messages.  For classic applications, this
                // is supported by default in Windows.  However, for immersive
                // applications, the default is to report mouse input through
                // these new WM_POINTER messages.  Blend - the only immersive
                // WPF application - must explicitly request the mouse input
                // be delivered through the traditional WM_MOUSE messages by
                // calling EnableMouseInPointer(false).  If WPF ever supports
                // the WM_POINTER messages, we need to be careful not to break
                // Blend.
                
                case WindowMessage.WM_NCDESTROY:
                {
                    //Console.WriteLine("WM_NCDESTROY");

                    // This is the normal clean-up path.  HwndSource destroys the
                    // HWND first, which should trigger this code, before it
                    // explicitly disposes us.  This allows us to call
                    // PossiblyDeactivate since our window is no longer on the
                    // screen.
                    Dispose();
                }
                break;

                case WindowMessage.WM_MOUSEMOVE:
                {
                    int x = NativeMethods.SignedLOWORD(lParam);
                    int y = NativeMethods.SignedHIWORD(lParam);

                    //Console.WriteLine("WM_MOUSEMOVE: " + x + "," + y);

                    // Abort the pending operation waiting to update the cursor, because we
                    // are going to update it as part of this mouse move processing.
                    if (_queryCursorOperation != null)
                    {
                        _queryCursorOperation.Abort();
                        _queryCursorOperation = null;
                    }

                    // MITIGATION_SETCURSOR
                    if (_haveCapture)
                    {
                        // When we have capture we don't receive WM_SETCURSOR
                        // prior to a mouse move.  So that we don't erroneously think
                        // we're in "Help Mode" we'll pretend we've received a set
                        // cursor message.
                        _setCursorState = SetCursorState.SetCursorReceived;
                    }
                    else
                    {
                        if (_setCursorState == SetCursorState.SetCursorNotReceived)
                        {
                            _setCursorState = SetCursorState.SetCursorDisabled;
                        }
                        else if(_setCursorState == SetCursorState.SetCursorReceived)
                        {
                            _setCursorState = SetCursorState.SetCursorNotReceived;
                        }
                    }

                        // Should we report the various modifier keys?  I think these are async states.

                        handled = ReportInput(hwnd,
                                          InputMode.Foreground,
                                          _msgTime,
                                          RawMouseActions.AbsoluteMove,
                                          x,
                                          y,
                                          0);
                }
                break;

                case WindowMessage.WM_MOUSEWHEEL:
                {
                    int wheel = NativeMethods.SignedHIWORD(wParam);
                    int x = NativeMethods.SignedLOWORD(lParam);
                    int y = NativeMethods.SignedHIWORD(lParam);

                    // The WM_MOUSEWHEEL gives the coordinates relative to the desktop.
                    NativeMethods.POINT pt = new NativeMethods.POINT(x,y);
                    try
                    {
                        SafeNativeMethods.ScreenToClient(new HandleRef(this,hwnd), pt);

                        x = pt.x;
                        y = pt.y;

                            //Console.WriteLine("WM_MOUSEWHEEL: " + x + "," + y + "," + wheel);

                            // Should we report the various modifier keys?  I think these are async states.

                            handled = ReportInput(hwnd,
                                              InputMode.Foreground,
                                              _msgTime,
                                              RawMouseActions.VerticalWheelRotate,
                                              x,
                                              y,
                                              wheel);
                    }
                    catch(System.ComponentModel.Win32Exception)
                    {
                        System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: ScreenToClient failed!");
                    }
                }
                break;

                case WindowMessage.WM_LBUTTONDBLCLK:
                case WindowMessage.WM_LBUTTONDOWN:
                {
                    int x = NativeMethods.SignedLOWORD(lParam);
                    int y = NativeMethods.SignedHIWORD(lParam);

                        //Console.WriteLine("WM_LBUTTONDOWN: " + x + "," + y);

                        // Should we report the various modifier keys?  I think these are async states.

                        handled = ReportInput(hwnd,
                                          InputMode.Foreground,
                                          _msgTime,
                                          RawMouseActions.Button1Press,
                                          x,
                                          y,
                                          0);
                }
                break;

                case WindowMessage.WM_LBUTTONUP:
                {
                    int x = NativeMethods.SignedLOWORD(lParam);
                    int y = NativeMethods.SignedHIWORD(lParam);

                        //Console.WriteLine("WM_LBUTTONUP: " + x + "," + y);

                        // Should we report the various modifier keys?  I think these are async states.

                        handled = ReportInput(hwnd,
                                          InputMode.Foreground,
                                          _msgTime,
                                          RawMouseActions.Button1Release,
                                          x,
                                          y,
                                          0);
                }
                break;

                case WindowMessage.WM_RBUTTONDBLCLK:
                case WindowMessage.WM_RBUTTONDOWN:
                {
                    int x = NativeMethods.SignedLOWORD(lParam);
                    int y = NativeMethods.SignedHIWORD(lParam);

                        // Should we report the various modifier keys?  I think these are async states.

                        handled = ReportInput(hwnd,
                                          InputMode.Foreground,
                                          _msgTime,
                                          RawMouseActions.Button2Press,
                                          x,
                                          y,
                                          0);
                }
                break;

                case WindowMessage.WM_RBUTTONUP:
                {
                    int x = NativeMethods.SignedLOWORD(lParam);
                    int y = NativeMethods.SignedHIWORD(lParam);

                        // Should we report the various modifier keys?  I think these are async states.

                        handled = ReportInput(hwnd,
                                          InputMode.Foreground,
                                          _msgTime,
                                          RawMouseActions.Button2Release,
                                          x,
                                          y,
                                          0);
                }
                break;

                case WindowMessage.WM_MBUTTONDBLCLK:
                case WindowMessage.WM_MBUTTONDOWN:
                {
                    int x = NativeMethods.SignedLOWORD(lParam);
                    int y = NativeMethods.SignedHIWORD(lParam);

                        // Should we report the various modifier keys?  I think these are async states.

                        handled = ReportInput(hwnd,
                                          InputMode.Foreground,
                                          _msgTime,
                                          RawMouseActions.Button3Press,
                                          x,
                                          y,
                                          0);
                }
                break;

                case WindowMessage.WM_MBUTTONUP:
                {
                    int x = NativeMethods.SignedLOWORD(lParam);
                    int y = NativeMethods.SignedHIWORD(lParam);

                        // Should we report the various modifier keys?  I think these are async states.

                        handled = ReportInput(hwnd,
                                          InputMode.Foreground,
                                          _msgTime,
                                          RawMouseActions.Button3Release,
                                          x,
                                          y,
                                          0);
                }
                break;

                case WindowMessage.WM_XBUTTONDBLCLK:
                case WindowMessage.WM_XBUTTONDOWN:
                {
                    int button = NativeMethods.SignedHIWORD(wParam);
                    int x = NativeMethods.SignedLOWORD(lParam);
                    int y = NativeMethods.SignedHIWORD(lParam);

                    RawMouseActions actions = 0;
                    if(button == 1)
                    {
                        actions = RawMouseActions.Button4Press;
                    }
                    else if(button == 2)
                    {
                        actions = RawMouseActions.Button5Press;
                    }

                        // Should we report the various modifier keys?  I think these are async states.

                        handled = ReportInput(hwnd,
                                          InputMode.Foreground,
                                          _msgTime,
                                          actions,
                                          x,
                                          y,
                                          0);
                }
                break;

                case WindowMessage.WM_XBUTTONUP:
                {
                    int button = NativeMethods.SignedHIWORD(wParam);
                    int x = NativeMethods.SignedLOWORD(lParam);
                    int y = NativeMethods.SignedHIWORD(lParam);

                    RawMouseActions actions = 0;
                    if(button == 1)
                    {
                        actions = RawMouseActions.Button4Release;
                    }
                    else if(button == 2)
                    {
                        actions = RawMouseActions.Button5Release;
                    }

                        // Should we report the various modifier keys?  I think these are async states.

                        handled = ReportInput(hwnd,
                                          InputMode.Foreground,
                                          _msgTime,
                                          actions,
                                          x,
                                          y,
                                          0);
                }
                break;

                case WindowMessage.WM_MOUSELEAVE:
                {
                    //Console.WriteLine("WM_MOUSELEAVE");

                    // When the mouse moves off the window, we receive a
                    // WM_MOUSELEAVE.   We'll start tracking again when the
                    // mouse moves back over us.
                    StopTracking(hwnd);

                    // It is possible that we have capture but we still receive
                    // a mouse leave event.  This can happen in the case of
                    // "soft capture".  In such cases, we defer the actual
                    // deactivation until the capture is lost.
                    //
                    // See the note on WM_CAPTURECHANGED for more details.
                    try
                    {
                        IntPtr hwndCapture = SafeNativeMethods.GetCapture();
                        IntPtr hwndCurrent = _source.Value.CriticalHandle;
                        if (hwndCapture != hwndCurrent)
                        {
                            PossiblyDeactivate(hwndCapture, false);
                        }
                    }
                    catch(System.ComponentModel.Win32Exception)
                    {
                        System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: GetCapture failed!");
                    }
}
                break;

                case WindowMessage.WM_CAPTURECHANGED:
                {
                    //Console.WriteLine("WM_CAPTURECHANGED");

                    // Win32 has two concepts for capture:
                    //
                    // Hard Capture
                    // When a mouse button is pressed, Win32 finds the window
                    // underneath the mouse and assigns it as the MouseOwner.
                    // All mouse input is directed to this window until the
                    // mouse is button released.  The window does not even
                    // have to request capture.  Certain window types are
                    // excluded from this processing.
                    //
                    // Soft Capture
                    // This is accessed via the SetCapture API.  It assigns
                    // the window that should receive mouse input for the
                    // queue.  Win32 decides which queue the mouse input
                    // should go to without considering this type of capture.
                    // Once the input is in the queue, it is sent to the
                    // window with capture.  This means that the mouse
                    // messages will generally be sent to the specified window
                    // in the application, but other applications will work
                    // too.
                    //
                    // If another application calls SetCapture, the current
                    // application will receive a WM_CAPTURECHANGED.
                    //
                    // If the window took capture while Win32 was enforcing
                    // Hard Capture, and releases capture when the mouse
                    // button is released, then everything works as you
                    // probably expect.  But if the application retains
                    // capture after the mouse button is released, it is
                    // possible to receive a WM_MOUSELEAVE even though the
                    // window still has capture.

                    // Losing capture *after* a WM_MOUSELEAVE means we
                    // probably want to deactivate the mouse input stream.
                    // If someone else is taking capture, we may need
                    // to deactivate the mouse input stream too.

                    if(lParam != _source.Value.CriticalHandle) // Ignore odd messages that claim we are losing capture to ourselves.
                    {
                        // MITIGATION_SETCURSOR
                        _haveCapture = false;

                        if(_setCursorState == SetCursorState.SetCursorReceived)
                        {
                            _setCursorState = SetCursorState.SetCursorNotReceived;
                        }

                        if(!IsOurWindow(lParam) && _active)
                        {
                            ReportInput(hwnd,
                                        InputMode.Foreground,
                                        _msgTime,
                                        RawMouseActions.CancelCapture,
                                        0,
                                        0,
                                        0);
                        }

                        if(lParam != IntPtr.Zero || // someone else took capture
                           !_tracking)              // OR no one has capture and the mouse is not over us
                        {
                            PossiblyDeactivate(lParam, true);
                        }
                    }
                }
                break;

                case WindowMessage.WM_CANCELMODE:
                {
                    // MITIGATION: NESTED_MESSAGE_PUMPS_INTERFERE_WITH_INPUT
                    //
                    // When a nested message pump runs, it intercepts all messages
                    // before they are dispatched, and thus before they can be sent
                    // to the window with capture.
                    //
                    // This means that an element can take capture on MouseDown,
                    // expecting to receive either MouseUp or LostCapture.  But, in
                    // fact, neither event may be raised if a nested message pump
                    // runs.
                    //
                    // An example of this is displaying a dialog box in response to
                    // MouseDown.
                    //
                    // There isn't much we can do about the general case, but
                    // well-behaved message pumps (such as a dialog box) are
                    // supposed to send the WM_CANCELMODE message.  In response
                    // to this we release capture if we currently have it.
                    try
                    {
                        if(_source.Value.HasCapture )
                        {
                            SafeNativeMethods.ReleaseCapture();
                        }
                    }
                    catch(System.ComponentModel.Win32Exception)
                    {
                        System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: GetCapture failed!");
                    }
                }
                break;

                case WindowMessage.WM_SETCURSOR:
                {
                    if (_queryCursorOperation == null)
                    {
                        // It is possible that a WM_SETCURSOR is not followed by a WM_MOUSEMOVE, in which
                        // case we need a backup mechanism to query the cursor and update it. So we post to
                        // the queue to do this work. If a WM_MOUSEMOVE comes in earlier, then the operation
                        // is aborted, else it comes through and we update the cursor.
                        _queryCursorOperation = Dispatcher.BeginInvoke(DispatcherPriority.Input,
                            (DispatcherOperationCallback)delegate(object sender)
                            {
                                // Since this is an asynchronous operation and an arbitrary amount of time has elapsed
                                // since we received the WM_SETCURSOR, we need to be careful that the mouse hasn't
                                // been deactivated in the meanwhile. This is also another reason that we do not ReportInput,
                                // because the implicit assumption in doing that is to activate the MouseDevice. All we want
                                // to do is passively try to update the cursor.
                                if (_active)
                                {
                                    Mouse.UpdateCursor();
                                }

                                _queryCursorOperation = null;
                                return null;
                            },
                            null);
                    }

                    // MITIGATION_SETCURSOR
                    _setCursorState = SetCursorState.SetCursorReceived;

                    // Note: We get this message BEFORE we get WM_MOUSEMOVE.  This means that Avalon
                    //       still thinks the mouse is over the "old" element.  This is awkward, and we think
                    //       people will find it confusing to get a QueryCursor event before a MouseMove event.
                    //       Further, this means we would have to do a special hit-test, and route the
                    //       QueryCursor event differently than the other mouse events.
                    //
                    //       Another difference is that Win32 passes us a hit-test code, which was calculated
                    //       by an earlier WM_NCHITTEST message.  The problem with this is that it is a fixed
                    //       enum.  We don't have a similar concept in Avalon.
                    //
                    //       So instead, the MouseDevice will raise the QueryCursor event after every MouseMove
                    //       event.  We think this is a better ordering.  And the application can return whatever
                    //       cursor they want (not limited to a fixed enum of hit-test codes).
                    //
                    //       Of course, this is different than Win32.  One example of where this can cause a
                    //       problem is that sometimes Win32 will NOT send a WM_SETCURSOR message and just send
                    //       a WM_MOUSEMOVE.  This is for cases like when the mouse is captured, or when the
                    //       the "help mode" is active (clicking the little question mark in the title bar).
                    //       To accomodate this, we use the _setCursorState to prevent the user from changing
                    //       the cursor when we haven't received a WM_SETCURSOR message - which means that the
                    //       cursor is NOT supposed to change as it moves over new windows/elements/etc.  Note
                    //       that Avalon will raise the QueryCursor event, but the result is ignored.
                    //
                    // But:  We MUST mark this Win32 message as "handled" or windows will change the cursor to
                    //       the default cursor, which will cause annoying flicker if the app is trying to set
                    //       a custom one.  Of course, only do this for the client area.
                    //
                    int hittestCode = NativeMethods.SignedLOWORD((int) lParam);
                    if(hittestCode == NativeMethods.HTCLIENT)
                    {
                        handled = true;
                    }
                }
                break;
            }

            if (handled && EventTrace.IsEnabled(EventTrace.Keyword.KeywordInput | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
            {
                // Anything can (and does) happen in ReportInput.  We can be (and have been)
                // re-entered and Dispose()ed.  Then returning from ReportInput
                // needs to check for that.
                int dispatcherHashCode = 0;

                if( _source != null && !_source.Value.IsDisposed && _source.Value.CompositionTarget != null)
                    dispatcherHashCode = _source.Value.CompositionTarget.Dispatcher.GetHashCode();

                // The ETW manifest for this event declares the lParam and
                // wParam values to be integers.  This is not always true for
                // 64-bit systems, which sometimes pass pointers and handles
                // through this parameters.  However, we can't change the ETW
                // manifest in an in-place upgrade, so we are just going to
                // cast to an int.  Note that IntPtr defines the explicit int
                // cast operator to used a checked block, which will throw an
                // overflow exception if the IntPtr contains too big of a value.
                // So we do the cast ourselves and ignore the overflow.
                int wParamInt = (int) (long) wParam;;
                int lParamInt = (int) (long) lParam;
                
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientInputMessage, EventTrace.Keyword.KeywordInput | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, dispatcherHashCode, hwnd.ToInt64(), msg, wParamInt, lParamInt);
}

            return result;
        }

        private void PossiblyDeactivate(IntPtr hwndCapture, bool stillActiveIfOverSelf)
        {
            // we may have been disposed by a re-entrant call
            // If so, there's nothing more to do.
            if (null == _source || null == _source.Value )
            {
                return;
            }

            if(_isDwmProcess)
            {
                return;
            }

            //Console.WriteLine("PossiblyDeactivate(" + hwndCapture + ")");

            // We are now longer active ourselves, but it is possible that the
            // window the mouse is going to intereact with is in the same
            // Dispatcher as ourselves.  If so, we don't want to deactivate the
            // mouse input stream because the other window hasn't activated it
            // yet, and it may result in the input stream "flickering" between
            // active/inactive/active.  This is ugly, so we try to supress the
            // uneccesary transitions.
            //
            IntPtr hwndToCheck = hwndCapture;
            if(hwndToCheck == IntPtr.Zero)
            {
                NativeMethods.POINT ptCursor = new NativeMethods.POINT();
                int messagePos = 0;
                try
                {
                    messagePos = SafeNativeMethods.GetMessagePos();
                }
                catch(System.ComponentModel.Win32Exception)
                {
                    System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: GetMessagePos failed!");
                }

                ptCursor.x = NativeMethods.SignedLOWORD(messagePos);
                ptCursor.y = NativeMethods.SignedHIWORD(messagePos);
                //Console.WriteLine("  GetMessagePos: ({0},{1})", ptCursor.x, ptCursor.y);

                try
                {
                    hwndToCheck = UnsafeNativeMethods.WindowFromPoint(ptCursor.x, ptCursor.y);
                }
                catch(System.ComponentModel.Win32Exception)
                {
                    System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: WindowFromPoint failed!");
                }

                if (!stillActiveIfOverSelf && hwndToCheck == _source.Value.CriticalHandle)
                {
                    hwndToCheck = IntPtr.Zero;
                }

                if(hwndToCheck != IntPtr.Zero)
                {
                    // We need to check if the point is over the client or
                    // non-client area.  We only care about being over the
                    // non-client area.
                    try
                    {
                        NativeMethods.RECT rcClient = new NativeMethods.RECT();
                        SafeNativeMethods.GetClientRect(new HandleRef(this,hwndToCheck), ref rcClient);
                        SafeNativeMethods.ScreenToClient(new HandleRef(this,hwndToCheck), ptCursor);

                        if(ptCursor.x < rcClient.left || ptCursor.x >= rcClient.right ||
                           ptCursor.y < rcClient.top || ptCursor.y >= rcClient.bottom)
                        {
                            // We are not over the non-client area.  We can bail out.
                            //Console.WriteLine("  No capture, mouse outside of client area.");
                            //Console.WriteLine("  Client Area: ({0},{1})-({2},{3})", rcClient.left, rcClient.top, rcClient.right, rcClient.bottom);
                            //Console.WriteLine("  Mouse: ({0},{1})", ptCursor.x, ptCursor.y);
                            hwndToCheck = IntPtr.Zero;
                        }
                    }
                    catch(System.ComponentModel.Win32Exception)
                    {
                        System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: GetClientRect or ScreenToClient failed!");
                    }
                }
            }

            // If the window the mouse is over is ours, we'll just let it activate
            // without deactivating the mouse input stream for this window.  This prevents
            // the mouse input stream from flickering.
            bool deactivate = !IsOurWindow(hwndToCheck);

            //Console.WriteLine("  Deactivate=" + deactivate);

            // Only deactivate the mouse input stream if needed.
            if(deactivate)
            {
                ReportInput(_source.Value.CriticalHandle,
                            InputMode.Foreground,
                            _msgTime,
                            RawMouseActions.Deactivate,
                            0,
                            0,
                            0);
            }
            else
            {
                // We are not deactivating the mouse input stream because the
                // window that is going to provide mouse input next is one of
                // our Avalon windows.  This optimization keeps the mouse input
                // stream from flickering by transitioning to null.
                //
                // But this window itself should not be active anymore.
                _active = false;
            }
        }

        private void StartTracking(IntPtr hwnd)
        {
            if(!_tracking && !_isDwmProcess)
            {
                _tme.hwndTrack = hwnd;
                _tme.dwFlags = NativeMethods.TME_LEAVE;
                try
                {
                    SafeNativeMethods.TrackMouseEvent(_tme);
                    _tracking = true;
                }
                catch(System.ComponentModel.Win32Exception)
                {
                    System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: TrackMouseEvent failed!");
                }
            }
        }

        private void StopTracking(IntPtr hwnd)
        {
            if(_tracking && !_isDwmProcess)
            {
                _tme.hwndTrack = hwnd;
                _tme.dwFlags = NativeMethods.TME_CANCEL | NativeMethods.TME_LEAVE;
                try
                {
                    SafeNativeMethods.TrackMouseEvent(_tme);
                    _tracking = false;
                }
                catch(System.ComponentModel.Win32Exception)
                {
                    System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: TrackMouseEvent failed!");
                }
            }
        }

        private IntPtr MakeLPARAM(int high, int low)
        {
               return ((IntPtr)((high << 16) | (low & 0xffff)));
        }

        private bool IsOurWindow(IntPtr hwnd)
        {
            bool isOurWindow = false;

            Debug.Assert(null != _source && null != _source.Value);

            if(hwnd != IntPtr.Zero)
            {
                HwndSource hwndSource;
                hwndSource = HwndSource.CriticalFromHwnd(hwnd);

                if(hwndSource != null)
                {
                    if(hwndSource.Dispatcher == _source.Value.Dispatcher)
                    {
                        // The window has the same dispatcher, must be ours.
                        isOurWindow = true;
                    }
                    else
                    {
                        // The window has a different dispatcher, must not be ours.
                        isOurWindow = false;
                    }
                }
                else
                {
                    // The window is non-Avalon.
                    // Such windows are never ours.
                    isOurWindow = false;
                }
            }
            else
            {
                // This is not even a window.
                isOurWindow = false;
            }

            return isOurWindow;
        }

        private bool ReportInput(
            IntPtr hwnd,
            InputMode mode,
            int timestamp,
            RawMouseActions actions,
            int x,
            int y,
            int wheel)
        {
            // if there's no HwndSource, we shouldn't get here.  But just in case...
            Debug.Assert(null != _source && null != _source.Value);
            if (_source == null || _source.Value == null)
            {
                return false;
            }

            PresentationSource source = _source.Value;
            CompositionTarget ct = source.CompositionTarget;

            // Input reports should only be generated if the window is still valid.
            if(_site == null || source.IsDisposed || ct == null )
            {
                if(_active)
                {
                    // We are still active, but the window is dead.  Force a deactivate.
                    actions = RawMouseActions.Deactivate;
                }
                else
                {
                    return false;
                }
            }

            if((actions & RawMouseActions.Deactivate) == RawMouseActions.Deactivate)
            {
                // Stop tracking the mouse since we are deactivating.
                StopTracking(hwnd);

                _active = false;
            }
            else if((actions & RawMouseActions.CancelCapture) == RawMouseActions.CancelCapture)
            {
                // We have lost capture, but don't do anything else.
            }
            else if(!_active && (actions & RawMouseActions.VerticalWheelRotate) == RawMouseActions.VerticalWheelRotate)
            {
                // report mouse wheel events as if they came from the window that
                // is under the mouse (even though they are reported to the window
                // with keyboard focus)
                MouseDevice mouse = _site.Value.CriticalInputManager.PrimaryMouseDevice;
                if (mouse != null && mouse.CriticalActiveSource != null)
                {
                    source = mouse.CriticalActiveSource;
                }
            }
            else
            {
                // If we are not active, we need to activate first.
                if(!_active)
                {
                    // But first, check for "spurious" mouse events...
                    //
                    // Sometimes we get a mouse move for window "A" AFTER another
                    // window ("B") has become active.  This would cause "A" to think
                    // that it is active, and to tell Avalon. Now both "A" and "B" think
                    // they are active, and Avalon thinks "A" is, but REALLY, "B" is.
                    //
                    // Confused yet?
                    //
                    // To avoid this, if this window ("A") gets a mouse move,
                    // we verify that either "A" has capture, or the mouse is over "A"

                    IntPtr hwndToCheck = SafeNativeMethods.GetCapture();
                    if(hwnd != hwndToCheck)
                    {
                        // If we get this far, "A" does NOT have capture
                        // - now ensure mouse is over "A"
                        NativeMethods.POINT ptCursor = new NativeMethods.POINT();
                        try
                        {
                            UnsafeNativeMethods.GetCursorPos(ptCursor);
                        }
                        catch(System.ComponentModel.Win32Exception)
                        {
                            // Sometimes Win32 will fail this call, such as if you are
                            // not running in the interactive desktop.  For example,
                            // a secure screen saver may be running.
                            System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: GetCursorPos failed!");
                        }

                        try
                        {
                            hwndToCheck = UnsafeNativeMethods.WindowFromPoint(ptCursor.x, ptCursor.y);
                        }
                        catch(System.ComponentModel.Win32Exception)
                        {
                            System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: WindowFromPoint failed!");
                        }

                        if(hwnd != hwndToCheck)
                        {
                            // If we get this far:
                            // - the mouse is NOT over "A"
                            // - "A" does NOT have capture
                            // We consider this a "spurious" mouse move and ignore it.
                            System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: Spurious mouse event received!");
                            return false;
                        }
                    }


                    // We need to collect the current state of the mouse.
                    // Include the activation action.
                    actions |= RawMouseActions.Activate;

                    // Remember that we are active.
                    _active = true;
                    _lastX = x;
                    _lastY = y;

                    //Console.WriteLine("Activating the mouse.");
                }

                // Make sure we are tracking the mouse so we know when it
                // leaves the window.
                StartTracking(hwnd);

                // Even if a move isn't explicitly reported, we still may need to
                // report one if the coordinates are different.  This is to cover
                // some ugly edge cases with context menus and such.
                if((actions & RawMouseActions.AbsoluteMove) == 0)
                {
                    if(x != _lastX || y != _lastY)
                    {
                        actions |= RawMouseActions.AbsoluteMove;
                    }
                }
                else
                {
                    _lastX = x;
                    _lastY = y;
                }

                // record mouse motion so that GetIntermediatePoints has the
                // information it needs
                if ((actions & RawMouseActions.AbsoluteMove) != 0)
                {
                    RecordMouseMove(x, y, _msgTime);
                }

                // MITIGATION: WIN32_AND_AVALON_RTL
                //
                // When a window is marked with the WS_EX_LAYOUTRTL style, Win32
                // mirrors the coordinates received for mouse movement as well as
                // mirroring the output of drawing to a GDI DC.
                //
                // Avalon also sets up mirroring transforms so that we properly
                // mirror the output since we render to DirectX, not a GDI DC.
                //
                // Unfortunately, this means that our input is already mirrored
                // by Win32, and Avalon mirrors it again.  To work around this
                // problem, we un-mirror the input from Win32 before passing
                // it into Avalon.
                //
                if((actions & (RawMouseActions.AbsoluteMove | RawMouseActions.Activate)) != 0)
                {
                    try
                    {
                        //This has a SUC on it and accesses CriticalHandle
                        int windowStyle = SafeNativeMethods.GetWindowStyle(new HandleRef(this, _source.Value.CriticalHandle), true);

                        if((windowStyle & NativeMethods.WS_EX_LAYOUTRTL) == NativeMethods.WS_EX_LAYOUTRTL)
                        {
                            NativeMethods.RECT rcClient = new NativeMethods.RECT();
                            SafeNativeMethods.GetClientRect(new HandleRef(this,_source.Value.Handle), ref rcClient);
                            x = rcClient.right - x;
                        }
                    }
                    catch(System.ComponentModel.Win32Exception)
                    {
                        System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: GetWindowStyle or GetClientRect failed!");
                    }
                }
            }


            // Get the extra information sent along with the message.
            //There exists a SUC for this native method call
            IntPtr extraInformation = IntPtr.Zero;
            try
            {
                extraInformation = UnsafeNativeMethods.GetMessageExtraInfo();
            }
            catch(System.ComponentModel.Win32Exception)
            {
                System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: GetMessageExtraInfo failed!");
            }


            RawMouseInputReport report = new RawMouseInputReport(mode,
                                                                 timestamp,
                                                                 source,
                                                                 actions,
                                                                 x,
                                                                 y,
                                                                 wheel,
                                                                 extraInformation);


            bool handled = _site.Value.ReportInput(report);

            return handled;
        }


        // GetIntermediatePoints needs to know the time and position of the
        // last two MouseMove events, so that it can extract points from the
        // system buffer between these two.  This method is called at each
        // MouseMove event to record the required information.
        private void RecordMouseMove(int x, int y, int timestamp)
        {
            // (x,y) is in client coordinates, but the system buffer uses screen
            // coordinates.  Convert the new position into screen coordinates.
            Point currentPosition = new Point(x, y);
            currentPosition = PointUtil.ClientToScreen(currentPosition, _source.Value);

            // roll the MouseMove buffer forward
            _previousMovePoint = _latestMovePoint;

            _latestMovePoint.x = ((int)currentPosition.X) & 0x0000FFFF;  //Ensure that this number will pass through.
            _latestMovePoint.y = ((int)currentPosition.Y) & 0x0000FFFF;  //Ensure that this number will pass through.
            _latestMovePoint.time = timestamp;
        }


        private SecurityCriticalDataClass<HwndSource> _source;
        private  SecurityCriticalDataClass<InputProviderSite> _site;
        private int _msgTime;
        private NativeMethods.MOUSEMOVEPOINT _latestMovePoint;      // screen coordinates
        private NativeMethods.MOUSEMOVEPOINT _previousMovePoint;    // screen coordinates
        private int _lastX;     // client coordinates
        private int _lastY;

        private bool _tracking; // Whether or not we have called TrackMouse() to get a WM_MOUSELEAVE.  This essentaully means IsOver.
        private bool _active; // Whether or not the mouse is actively sending events to the input manager.

        // MITIGATION_SETCURSOR
        private SetCursorState _setCursorState; // Have we received a WM_SETCURSOR message lately?
        private bool _haveCapture; // Do we currently have capture?
        private DispatcherOperation _queryCursorOperation;

        private bool _isDwmProcess; // If we are the DWM, we need to always be _active, don't track focus.

        private NativeMethods.TRACKMOUSEEVENT _tme = new NativeMethods.TRACKMOUSEEVENT();

        // MITIGATION_SETCURSOR
        //
        // Windows can have a "context help" button in the title bar.  When the user
        // clicks it, the cursor is changed to a "help" cursor, and when the user
        // clicks somewhere in the window, a WM_HELP message is sent to the child
        // window to tell it to display a help tooltip.
        //
        // During this time, the application should not be setting the cursor.  Win32
        // accomplishes this by not sending the WM_SETCURSOR message while in this
        // help mode.  Thus applications do not typically set the cursor.  However,
        // Win32 does not prevent a programatic call to SetCursor() from changing the
        // cursor.
        //
        // Avalon programatically changes the cursor on every mouse move.  This is
        // because we simulate mouse moves in response to layout and animation.  The
        // cursor needs to reflect the control it is over, and that control needs to
        // know the mouse is over it.
        //
        // But we need to supress setting the cursor while in this "help" mode to
        // prevent the cursor from being reset to a standard arrow (or whatever the
        // app might want it to be).  There are a number of ways we could accomplish
        // this:
        //
        // 1) Poke User32 to simulate the mouse for us.
        //    Presumably User32 would supress sending the WM_SETCURSOR properly, and
        //    we could then only set the cursor in response to a WM_SETCURSOR message.
        //    Unfortunately we can't figure out a way of invoking xxxSetFMouseMoved()
        //    without encountering unwanted side effects.
        // 2) Detect the help mode and suppress the cursor only then.
        //    We could listen for WM_SYSCOMMAND(SC_CONTEXTHELP), but this won't be
        //    sent to us if we are a child window.  Instead we have a little state
        //    machine we drive of the WM_SETCURSOR and WM_MOUSEMOVE messages to
        //    try to detect this mode indirectly.
        private enum SetCursorState
        {
            SetCursorNotReceived,
            SetCursorReceived,
            SetCursorDisabled
        }
    }
}
