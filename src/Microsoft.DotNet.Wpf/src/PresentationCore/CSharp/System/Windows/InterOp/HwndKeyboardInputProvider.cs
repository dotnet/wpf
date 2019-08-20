// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using MS.Utility;
using MS.Internal;
using MS.Internal.Interop;
using MS.Win32;
using MS.Internal.PresentationCore;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Interop
{
    internal sealed class HwndKeyboardInputProvider : DispatcherObject, IKeyboardInputProvider, IDisposable
    {
        internal HwndKeyboardInputProvider(HwndSource source)
        {
            _site = new SecurityCriticalDataClass<InputProviderSite>(InputManager.Current.RegisterInputProvider(this));

            _source = new SecurityCriticalDataClass<HwndSource>(source);
        }



        public void Dispose()
        {
            if(_site != null)
            {
                _site.Value.Dispose();
                _site = null;
            }
            _source = null;
        }

        public void OnRootChanged(Visual oldRoot, Visual newRoot)
        {
            if(_active && newRoot != null)
            {
                Keyboard.Focus(null); // internally we will set the focus to the root.
            }
        }
        bool IInputProvider.ProvidesInputForRootVisual(Visual v)
        {
            Debug.Assert( null != _source );

            return _source.Value.RootVisual == v;
        }

        void IInputProvider.NotifyDeactivate()
        {
            _active        = false;
            _partialActive = false;
        }

        bool IKeyboardInputProvider.AcquireFocus(bool checkOnly)
        {
            bool result = false;

            Debug.Assert( null != _source );

            try
            {
                // Acquiring focus into this window should clear any pending focus restoration.
                if(!checkOnly)
                {
                    _acquiringFocusOurselves = true;
                    _restoreFocusWindow = IntPtr.Zero;
                    _restoreFocus = null;
                }

                HandleRef thisWindow = new HandleRef(this, _source.Value.CriticalHandle);
                IntPtr focus = UnsafeNativeMethods.GetFocus();

                int windowStyle = UnsafeNativeMethods.GetWindowLong(thisWindow, NativeMethods.GWL_EXSTYLE);
                if ((windowStyle & NativeMethods.WS_EX_NOACTIVATE) == NativeMethods.WS_EX_NOACTIVATE || _source.Value.IsInExclusiveMenuMode)
                {
                    // If this window has the WS_EX_NOACTIVATE style, then we
                    // do not set Win32 keyboard focus to this window because
                    // that would actually activate the window. This is
                    // typically for the menu Popup.
                    //
                    // If this window is in "menu mode", then we do not set
                    // Win32 focus to this window because we don't want to
                    // move Win32 focus from where it is.  This is typically
                    // for the main window.
                    //
                    // In either case, the window must be enabled.
                    if(SafeNativeMethods.IsWindowEnabled(thisWindow))
                    {

                        // In fully-trusted AppDomains, the only hard requirement
                        // is that Win32 keyboard focus be on some window owned
                        // by a thread that is attached to our Win32 queue.  This
                        // presumes that the thread's message pump will cooperate
                        // by calling ComponentDispatcher.RaiseThreadMessage.
                        // If so, WPF will be able to route the keyboard events to the
                        // element with WPF keyboard focus, regardless of which
                        // window has Win32 keyboard focus.
                        //
                        // Menus/ComboBoxes use this feature.
                        //
                        // Dev11 is moving more towards cross-process designer
                        // support.  They make sure to call AttachThreadInput so
                        // the the two threads share the same Win32 queue.  In
                        // addition, they repost the keyboard messages to the
                        // main UI process/thread for handling.
                        //
                        // We rely on the behavior of GetFocus to only return a
                        // window handle for windows attached to the calling
                        // thread's queue.
                        //
                        result = focus != IntPtr.Zero;
                    }
                }
                else
                {
                    // This is the normal case.  We want to keep WPF keyboard
                    // focus and Win32 keyboard focus in sync.
                    if(!checkOnly)
                    {
                        // Due to IsInExclusiveMenuMode, it is possible that an
                        // HWND keeps Win32 focus even though WPF has moved
                        // element focus somewhere else.  When the element focus
                        // moves somewhere else, this input provider will get
                        // deactivated.  If element focus is set back to an
                        // element within this provider, the HWND already has
                        // Win32 focus and so will not receive another
                        // WM_SETFOCUS, causing the provider to remain
                        // deactivated.  Now we detect that we already have
                        // Win32 focus but are not activated and treat it the
                        // same as getting focus.
                        if (!_active && focus == _source.Value.CriticalHandle)
                        {
                            OnSetFocus(focus);
                        }
                        else
                        {
                            UnsafeNativeMethods.TrySetFocus(thisWindow);

                            // Fetch the HWND with Win32 focus again, to double
                            // check we got it.
                            focus = UnsafeNativeMethods.GetFocus();
                        }
                    }

                    result = (focus == _source.Value.CriticalHandle);
                }
            }
            catch(System.ComponentModel.Win32Exception)
            {
                System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: AcquireFocus failed!");
            }
            finally
            {
                _acquiringFocusOurselves = false;
            }

            return result;
        }

        internal IntPtr FilterMessage(IntPtr hwnd, WindowMessage message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            IntPtr result = IntPtr.Zero ;

            // It is possible to be re-entered during disposal.  Just return.
            if(null == _source || null == _source.Value)
            {
                return result;
            }

            _msgTime = 0;
            try
            {
                _msgTime = SafeNativeMethods.GetMessageTime();
            }
            catch(System.ComponentModel.Win32Exception)
            {
                System.Diagnostics.Debug.WriteLine("HwndKeyboardInputProvider: GetMessageTime failed!");
            }

            switch(message)
            {
                // WM_KEYDOWN is sent when a nonsystem key is pressed.
                // A nonsystem key is a key that is pressed when the ALT key
                // is not pressed.
                // WM_SYSKEYDOWN is sent when a system key is pressed.
                case WindowMessage.WM_SYSKEYDOWN:
                case WindowMessage.WM_KEYDOWN:
                {
                    // If we have a IKeyboardInputSite, then we should have already
                    // called ProcessKeyDown (from TranslateAccelerator)
                    // But there are several paths (our message pump / app's message
                    // pump) where we do (or don't) call through IKeyboardInputSink.
                    // So the best way is to just check here if we already did it.
                    if(_source.Value.IsRepeatedKeyboardMessage(hwnd, (int)message, wParam, lParam))
                    {
                        break;
                    }

                    // We will use the current time before generating KeyDown events so we can filter
                    // the later posted WM_CHAR.
                    int currentTime = 0;
                    try
                    {
                        currentTime = SafeNativeMethods.GetTickCount();
                    }
                    catch(System.ComponentModel.Win32Exception)
                    {
                        System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: GetTickCount failed!");
                    }

                    // MITIGATION: HANDLED_KEYDOWN_STILL_GENERATES_CHARS
                    // In case a nested message pump is used before we return
                    // from processing this message, we disable processing the
                    // next WM_CHAR message because if the code pumps messages
                    // it should really mark the message as handled.
                    HwndSource._eatCharMessages = true;
                    DispatcherOperation restoreCharMessages = Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(HwndSource.RestoreCharMessages), null);

                    // Force the Dispatcher to post a new message to service any
                    // pending operations, so that the operation we just posted
                    // is guaranteed to get dispatched after any pending WM_CHAR
                    // messages are dispatched.
                    Dispatcher.CriticalRequestProcessing(true);

                    MSG msg = new MSG(hwnd, (int)message, wParam, lParam, _msgTime, 0, 0);
                    ProcessKeyAction(ref msg, ref handled);

                    if(!handled)
                    {
                        // MITIGATION: HANDLED_KEYDOWN_STILL_GENERATES_CHARS
                        // We did not handle the WM_KEYDOWN, so it is OK to process WM_CHAR messages.
                        // We can also abort the pending restore operation since we don't need it.
                        HwndSource._eatCharMessages = false;
                        restoreCharMessages.Abort();
                    }

                    // System.Console.WriteLine("KEYDOWN(message={0}, wParam={1})={2}", message, wParam, handled);
                }
                break;

                // WM_KEYUP is sent when a nonsystem key is released.
                // A nonsystem key is a key that is pressed when the ALT key
                // is not pressed.
                // WM_SYSKEYUP is sent when a system key is released.
                case WindowMessage.WM_SYSKEYUP:
                case WindowMessage.WM_KEYUP:
                {
                    if(_source.Value.IsRepeatedKeyboardMessage(hwnd, (int)message, wParam, lParam))
                    {
                        break;
                    }

                    MSG msg = new MSG(hwnd, (int)message, wParam, lParam, _msgTime, 0, 0);
                    ProcessKeyAction(ref msg, ref handled);
                    // System.Console.WriteLine("KEYUP  (message={0}, wParam={1})={2}", message, wParam, handled);
                }
                break;

                // WM_UNICHAR (UTF-32) support needs to be implemented
                // case WindowMessage.WM_UNICHAR:
                case WindowMessage.WM_CHAR:
                case WindowMessage.WM_DEADCHAR:
                case WindowMessage.WM_SYSCHAR:
                case WindowMessage.WM_SYSDEADCHAR:
                {
                    if(_source.Value.IsRepeatedKeyboardMessage(hwnd, (int)message, wParam, lParam))
                    {
                        break;
                    }

                    // MITIGATION: HANDLED_KEYDOWN_STILL_GENERATES_CHARS
                    if(HwndSource._eatCharMessages)
                    {
                        break;
                    }

                    ProcessTextInputAction(hwnd, message, wParam, lParam, ref handled);
                    // System.Console.WriteLine("CHAR(message={0}, wParam={1})={2}", message, wParam, handled);
                }
                break;

                case WindowMessage.WM_EXITMENULOOP:
                case WindowMessage.WM_EXITSIZEMOVE:
                {
                    // MITIGATION: KEYBOARD_STATE_OUT_OF_SYNC
                    //
                    // Avalon relies on keeping it's copy of the keyboard
                    // state.  This is for a number of reasons, including that
                    // we need to be able to give this state to worker threads.
                    //
                    // There are a number of cases where Win32 eats the
                    // keyboard messages, and this can cause our keyboard
                    // state to become stale.  Obviously this can happen when
                    // another app is in the foreground, but we handle that
                    // by re-synching our keyboard state when we get focus.
                    //
                    // Other times are when Win32 enters a nested loop.  While
                    // any one could enter a nested loop at any time for any
                    // reason, Win32 is nice enough to let us know when it is
                    // finished with the two common loops: menus and sizing.
                    // We re-sync our keyboard device in response to these.
                    //
                    if(_active)
                    {
                        _partialActive = true;

                        ReportInput(hwnd,
                                    InputMode.Foreground,
                                    _msgTime,
                                    RawKeyboardActions.Activate,
                                    0,
                                    false,
                                    false,
                                    0);
                    }
                }
                break;

                // WM_SETFOCUS is sent immediately after focus is granted.
                // This is our clue that the keyboard is active.
                case WindowMessage.WM_SETFOCUS:
                {
                    OnSetFocus(hwnd);

                    handled = true;
                }
                break;

                // WM_KILLFOCUS is sent immediately before focus is removed.
                // This is our clue that the keyboard is inactive.
                case WindowMessage.WM_KILLFOCUS:
                {
                    if(_active  &&  wParam != _source.Value.CriticalHandle )
                    {
                        // Console.WriteLine("WM_KILLFOCUS");

                        if(_source.Value.RestoreFocusMode == RestoreFocusMode.Auto)
                        {
                            // when the window that's acquiring focus (wParam) is
                            // a descendant of our window, remember the immediate
                            // child so that we can restore focus to it.
                            _restoreFocusWindow = GetImmediateChildFor((IntPtr)wParam, _source.Value.CriticalHandle);

                            _restoreFocus = null;

                            // If we aren't restoring focus to a child window,
                            // then restore focus to the element that currently
                            // has WPF keyboard focus if it is directly in this
                            // HwndSource.
                            if (_restoreFocusWindow == IntPtr.Zero)
                            {
                                DependencyObject focusedDO = Keyboard.FocusedElement as DependencyObject;
                                if (focusedDO != null)
                                {
                                    HwndSource hwndSource = PresentationSource.CriticalFromVisual(focusedDO) as HwndSource;
                                    if (hwndSource == _source.Value)
                                    {
                                        _restoreFocus = focusedDO as IInputElement;
                                    }
                                }
}
                        }

                        PossiblyDeactivate((IntPtr)wParam);
}

                    handled = true;
                }
                break;

                // WM_UPDATEUISTATE is sent when the user presses ALT, expecting
                // the app to display accelerator keys.  We don't always hear the
                // keystroke - another message loop may handle it.  So report it
                // here.
                case WindowMessage.WM_UPDATEUISTATE:
                {
                    RawUIStateInputReport report =
                        new RawUIStateInputReport(_source.Value,
                                                   InputMode.Foreground,
                                                   _msgTime,
                                                   (RawUIStateActions)NativeMethods.SignedLOWORD((int)wParam),
                                                   (RawUIStateTargets)NativeMethods.SignedHIWORD((int)wParam));

                    _site.Value.ReportInput(report);

                    handled = true;
                }
                break;
            }

            if (handled && EventTrace.IsEnabled(EventTrace.Keyword.KeywordInput | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
            {
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientInputMessage,
                                                    EventTrace.Keyword.KeywordInput | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info,
                                                     Dispatcher.GetHashCode(),
                                                     hwnd.ToInt64(),
                                                     message,
                                                     (int)wParam,
                                                     (int)lParam);
            }

            return result;
        }

        private void OnSetFocus(IntPtr hwnd)
        {
            // Normally we get WM_SETFOCUS only when _active is false.
            //  We have observed FatalExecutionEngineError when running stress when _active is true:
            //  1. Window contains a WindowsFormsHost, which contains a WF.TextBox that has focus
            //  2. User types Alt-Tab to switch to another app
            //  3. User types Alt-Tab again to return to this window
            // The ALT key sets _active to true, as we are processing keyboard input,
            // even though focus is in another window (the WF.TextBox).  But Alt-Tab
            // sends focus to another app, and we don't get any messages (the WF.TextBox
            // gets WM_KILLFOCUS, but doesn't tell us about it).   Thus when focus
            // returns after the second Alt-Tab, _active is still true.
            //
            // We need to run the focus restoration logic in this case. To make that
            // happen, we set _active to false here.  This leaves _active in the
            // state we want, even if the code herein encounters errors/exceptions.
            // There may be other cases where _active is true here (we don't know of
            // any, but we cannot rule them out), but we believe that the code won't
            // do any harm.
            _active = false;

            if (!_active)
            {
                // There is a chance that external code called during the focus
                // changes below will dispose our window, causing _source to get
                // cleared.  We actually saw this in XDesProc (the Blend XAML
                // designer process) in 4.5 Beta, but never tracked down the culprit.
                // To be safe, we cache the member variable in a local variable
                // for use within this method.
                HwndSource thisSource = _source.Value;

                // Console.WriteLine("WM_SETFOCUS");

                ReportInput(hwnd,
                            InputMode.Foreground,
                            _msgTime,
                            RawKeyboardActions.Activate,
                            0,
                            false,
                            false,
                            0);

                // MITIGATION: KEYBOARD_STATE_OUT_OF_SYNC
                //
                // This is how we deal with the fact that Win32 sometimes sends
                // us a WM_SETFOCUS message BEFORE it has updated it's internal
                // internal keyboard state information.  When we get the
                // WM_SETFOCUS message, we activate the keyboard with the
                // keyboard state (even though it could be wrong).  Then when
                // we get the first "real" keyboard input event, we activate
                // the keyboard again, since Win32 will have updated the
                // keyboard state correctly by then.
                //
                _partialActive = true;

                if (!_acquiringFocusOurselves && thisSource.RestoreFocusMode == RestoreFocusMode.Auto)
                {
                    // Restore the keyboard focus to the child window or element that had
                    // the focus before we last lost Win32 focus.  If nothing
                    // had focus before, set it to null.
                    if (_restoreFocusWindow != IntPtr.Zero)
                    {
                        IntPtr hwndRestoreFocus = _restoreFocusWindow;
                        _restoreFocusWindow = IntPtr.Zero;

                        UnsafeNativeMethods.TrySetFocus(new HandleRef(this, hwndRestoreFocus), ref hwndRestoreFocus);
                    }
                    else
                    {
                        DependencyObject restoreFocusDO = _restoreFocus as DependencyObject;
                        _restoreFocus = null;

                        if (restoreFocusDO != null)
                        {
                            // Only restore focus to an element if that
                            // element still belongs to this HWND.
                            HwndSource hwndSource = PresentationSource.CriticalFromVisual(restoreFocusDO) as HwndSource;
                            if (hwndSource != thisSource)
                            {
                                restoreFocusDO = null;
                            }
                        }

                        // Try to restore focus to the last element that had focus.  Note
                        // that if restoreFocusDO is null, we will internally set focus
                        // to the root element.
                        Keyboard.Focus(restoreFocusDO as IInputElement);

                        // Lots of things can happen when setting focus to an element,
                        // including that element may set focus somewhere else, possibly
                        // even into another HWND.  However, if Win32 focus remains on
                        // this window, we do not allow the focused element to be in
                        // a different window.
                        IntPtr focus = UnsafeNativeMethods.GetFocus();
                        if (focus == thisSource.CriticalHandle)
                        {
                            restoreFocusDO = (DependencyObject)Keyboard.FocusedElement;
                            if (restoreFocusDO != null)
                            {
                                HwndSource hwndSource = PresentationSource.CriticalFromVisual(restoreFocusDO) as HwndSource;
                                if (hwndSource != thisSource)
                                {
                                    Keyboard.ClearFocus();
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void ProcessKeyAction(ref MSG msg, ref bool handled)
        {
            // Remember the last message
            MSG previousMSG = ComponentDispatcher.UnsecureCurrentKeyboardMessage;
            ComponentDispatcher.UnsecureCurrentKeyboardMessage = msg;

            try
            {
                int virtualKey = GetVirtualKey(msg.wParam, msg.lParam);
                int scanCode = GetScanCode(msg.wParam, msg.lParam);
                bool isExtendedKey = IsExtendedKey(msg.lParam);
                bool isSystemKey = (((WindowMessage)msg.message == WindowMessage.WM_SYSKEYDOWN) || ((WindowMessage)msg.message == WindowMessage.WM_SYSKEYUP));
                RawKeyboardActions action = GetKeyUpKeyDown((WindowMessage)msg.message);

                // Console.WriteLine("WM_KEYDOWN: " + virtualKey + "," + scanCode);
                handled = ReportInput(msg.hwnd,
                                      InputMode.Foreground,
                                      _msgTime,
                                      action,
                                      scanCode,
                                      isExtendedKey,
                                      isSystemKey,
                                      virtualKey);
            }
            finally
            {
                // Restore the last message
                ComponentDispatcher.UnsecureCurrentKeyboardMessage = previousMSG;
            }
        }

        internal void ProcessTextInputAction(IntPtr hwnd, WindowMessage msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            char charcode = (char)wParam;
            bool isDeadChar = ((msg == WindowMessage.WM_DEADCHAR) || (msg == WindowMessage.WM_SYSDEADCHAR));
            bool isSystemChar = ((msg == WindowMessage.WM_SYSCHAR) || (msg == WindowMessage.WM_SYSDEADCHAR));
            bool isControlChar = false;

            // If the control is pressed but Alt is not, the char is control char.
            try
            {
                if (((UnsafeNativeMethods.GetKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0) &&
                    ((UnsafeNativeMethods.GetKeyState(NativeMethods.VK_MENU) & 0x8000) == 0) &&
                    Char.IsControl(charcode))
                {
                    isControlChar = true;
                }
            }
            catch(System.ComponentModel.Win32Exception)
            {
                System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: GetKeyState failed!");
            }

            RawTextInputReport report = new RawTextInputReport(_source.Value ,
                                                               InputMode.Foreground,
                                                               _msgTime,
                                                               isDeadChar,
                                                               isSystemChar,
                                                               isControlChar,
                                                               charcode);

            handled = _site.Value.ReportInput(report);
        }

        internal static int GetVirtualKey(IntPtr wParam, IntPtr lParam)
        {
            int virtualKey = NativeMethods.IntPtrToInt32( wParam);
            int scanCode = 0;
            int keyData = NativeMethods.IntPtrToInt32(lParam);

            // Find the left/right instance SHIFT keys.
            if(virtualKey == NativeMethods.VK_SHIFT)
            {
                scanCode = (keyData & 0xFF0000) >> 16;
                try
                {
                    virtualKey = SafeNativeMethods.MapVirtualKey(scanCode, 3);
                    if(virtualKey == 0)
                    {
                        virtualKey = NativeMethods.VK_LSHIFT;
                    }
                }
                catch(System.ComponentModel.Win32Exception)
                {
                    System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: MapVirtualKey failed!");

                    virtualKey = NativeMethods.VK_LSHIFT;
                }
            }

            // Find the left/right instance ALT keys.
            if(virtualKey == NativeMethods.VK_MENU)
            {
                bool right = ((keyData & 0x1000000) >> 24) != 0;

                if(right)
                {
                    virtualKey = NativeMethods.VK_RMENU;
                }
                else
                {
                    virtualKey = NativeMethods.VK_LMENU;
                }
            }

            // Find the left/right instance CONTROL keys.
            if(virtualKey == NativeMethods.VK_CONTROL)
            {
                bool right = ((keyData & 0x1000000) >> 24) != 0;

                if(right)
                {
                    virtualKey = NativeMethods.VK_RCONTROL;
                }
                else
                {
                    virtualKey = NativeMethods.VK_LCONTROL;
                }
            }

            return virtualKey;
        }

        internal static int GetScanCode(IntPtr wParam, IntPtr lParam)
        {
            int keyData = NativeMethods.IntPtrToInt32(lParam);

            int scanCode = (keyData & 0xFF0000) >> 16;
            if(scanCode == 0)
            {
                try
                {
                    int virtualKey = GetVirtualKey(wParam, lParam);
                    scanCode = SafeNativeMethods.MapVirtualKey(virtualKey, 0);
                }
                catch(System.ComponentModel.Win32Exception)
                {
                    System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: MapVirtualKey failed!");
                }
            }

            return scanCode;
        }

        internal static bool IsExtendedKey(IntPtr lParam)
        {
            int keyData = NativeMethods.IntPtrToInt32(lParam);
            return ((keyData & 0x01000000) != 0) ? true : false;
        }

        ///<summary>
        ///     Returns the set of modifier keys currently pressed as determined by calling to Win32
        ///</summary>
        ///<remarks>
        ///     Marked as FriendAccessAllowed so HwndHost in PresentationFramework can call it
        ///</remarks>
        [FriendAccessAllowed]
        internal static ModifierKeys GetSystemModifierKeys()
        {
            ModifierKeys modifierKeys = ModifierKeys.None;

            short keyState = UnsafeNativeMethods.GetKeyState(NativeMethods.VK_SHIFT);
            if((keyState & 0x8000) == 0x8000)
            {
                modifierKeys |= ModifierKeys.Shift;
            }

            keyState = UnsafeNativeMethods.GetKeyState(NativeMethods.VK_CONTROL);
            if((keyState & 0x8000) == 0x8000)
            {
                modifierKeys |= ModifierKeys.Control;
            }

            keyState = UnsafeNativeMethods.GetKeyState(NativeMethods.VK_MENU);
            if((keyState & 0x8000) == 0x8000)
            {
                modifierKeys |= ModifierKeys.Alt;
            }

            return modifierKeys;
        }

        private RawKeyboardActions GetKeyUpKeyDown(WindowMessage msg)
        {
            if(  msg == WindowMessage.WM_KEYDOWN || msg == WindowMessage.WM_SYSKEYDOWN )
                return RawKeyboardActions.KeyDown;
            if(  msg == WindowMessage.WM_KEYUP || msg == WindowMessage.WM_SYSKEYUP )
                return RawKeyboardActions.KeyUp;
            throw new ArgumentException(SR.Get(SRID.OnlyAcceptsKeyMessages));
        }

        private void PossiblyDeactivate(IntPtr hwndFocus)
        {
            Debug.Assert( null != _source );

            // We are now longer active ourselves, but it is possible that the
            // window the keyboard is going to intereact with is in the same
            // Dispatcher as ourselves.  If so, we don't want to deactivate the
            // keyboard input stream because the other window hasn't activated
            // it yet, and it may result in the input stream "flickering" between
            // active/inactive/active.  This is ugly, so we try to supress the
            // uneccesary transitions.
            //
            bool deactivate = !IsOurWindow(hwndFocus);

            // This window itself should not be active anymore.
            _active = false;

            // Only deactivate the keyboard input stream if needed.
            if(deactivate)
            {
                ReportInput(_source.Value.CriticalHandle,
                            InputMode.Foreground,
                            _msgTime,
                            RawKeyboardActions.Deactivate,
                            0,
                            false,
                            false,
                            0);
            }
        }

        private bool IsOurWindow(IntPtr hwnd)
        {
            bool isOurWindow = false;

            Debug.Assert( null != _source );

            if(hwnd != IntPtr.Zero)
            {
                HwndSource hwndSource = HwndSource.CriticalFromHwnd(hwnd);
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

        // return the immediate child (if any) of hwndRoot that governs the
        // given hwnd.  If hwnd is not a descendant of hwndRoot, return 0.
        private IntPtr GetImmediateChildFor(IntPtr hwnd, IntPtr hwndRoot)
        {
            while (hwnd != IntPtr.Zero)
            {
                // We only care to restore focus to child windows. Notice that WS_POPUP
                // windows also have parents but we do not want to track those here.

                int windowStyle = UnsafeNativeMethods.GetWindowLong(new HandleRef(this,hwnd), NativeMethods.GWL_STYLE);
                if((windowStyle & NativeMethods.WS_CHILD) == 0)
                {
                    break;
                }

                IntPtr hwndParent = UnsafeNativeMethods.GetParent(new HandleRef(this, hwnd));

                if (hwndParent == hwndRoot)
                {
                    return hwnd;
                }

                hwnd = hwndParent;
            }

            return IntPtr.Zero;
        }

        private bool ReportInput(
            IntPtr hwnd,
            InputMode mode,
            int timestamp,
            RawKeyboardActions actions,
            int scanCode,
            bool isExtendedKey,
            bool isSystemKey,
            int virtualKey)
        {
            Debug.Assert( null != _source );

            // The first event should also activate the keyboard device.
            if((actions & RawKeyboardActions.Deactivate) == 0)
            {
                if(!_active || _partialActive)
                {
                    try
                    {
                        // Include the activation action.
                        actions |= RawKeyboardActions.Activate;

                        // Remember that we are active.
                        _active = true;
                        _partialActive = false;
                    }
                    catch(System.ComponentModel.Win32Exception)
                    {
                        System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: GetKeyboardState failed!");

                        // We'll go ahead and report the input, but we'll try to "activate" next time.
                    }
                }
            }

            // Get the extra information sent along with the message.
            IntPtr extraInformation = IntPtr.Zero;
            try
            {
                extraInformation = UnsafeNativeMethods.GetMessageExtraInfo();
            }
            catch(System.ComponentModel.Win32Exception)
            {
                System.Diagnostics.Debug.WriteLine("HwndMouseInputProvider: GetMessageExtraInfo failed!");
            }

            RawKeyboardInputReport report = new RawKeyboardInputReport(_source.Value,
                                                                       mode,
                                                                       timestamp,
                                                                       actions,
                                                                       scanCode,
                                                                       isExtendedKey,
                                                                       isSystemKey,
                                                                       virtualKey,
                                                                       extraInformation);


            bool handled = _site.Value.ReportInput(report);

            return handled;
        }

        private int  _msgTime;
        private SecurityCriticalDataClass<HwndSource> _source;
        private SecurityCriticalDataClass<InputProviderSite> _site;
        private IInputElement _restoreFocus;
        private IntPtr _restoreFocusWindow;
        private bool _active;
        private bool _partialActive;
        private bool _acquiringFocusOurselves;
    }
}

