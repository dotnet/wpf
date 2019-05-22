// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Base proxy for HWNDs. Provides HWND-based children, HWND properties such as Enabled, Visible etc.

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Collections;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;
using MS.Win32;
using NativeMethodsSetLastError = MS.Internal.UIAutomationClient.NativeMethodsSetLastError;

namespace MS.Internal.Automation
{
    // Disable warning for obsolete types.  These are scheduled to be removed in M8.2 so
    // only need the warning to come out for components outside of APT.
    #pragma warning disable 0618

    // Base proxy for HWNDs. Provides HWND-based children, HWND properties such as Enabled, Visible etc.
    internal class HwndProxyElementProvider:
        IRawElementProviderSimple,
        IRawElementProviderFragmentRoot,
        IRawElementProviderFragment,
        IWindowProvider,
        ITransformProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal HwndProxyElementProvider( NativeMethods.HWND hwnd )
        {
            Debug.Assert( hwnd != NativeMethods.HWND.NULL );

            if( hwnd == NativeMethods.HWND.NULL )
            {
                throw new ArgumentNullException( "hwnd" );
            }

            _hwnd = hwnd;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Interface IRawElementProviderSimple/Fragment/Root
        //
        //------------------------------------------------------

        #region IRawElementProviderSimple

        ProviderOptions IRawElementProviderSimple.ProviderOptions
        {
            get
            {
                return ProviderOptions.ClientSideProvider;
            }
        }

        object IRawElementProviderSimple.GetPatternProvider(int patternId)
        {
            AutomationPattern pattern = AutomationPattern.LookupById(patternId);
            // expose the Window pattern for things we know are windows
            if ( pattern == WindowPattern.Pattern )
            {
                if ( SupportsWindowPattern )
                    return this;
            }
            else if ( pattern == TransformPattern.Pattern )
            {
                if ( SupportsTransformPattern )
                    return this;
            }

            return null;
        }

        object IRawElementProviderSimple.GetPropertyValue(int propertyId)
        {
            AutomationProperty idProp = AutomationProperty.LookupById(propertyId);
            if (idProp == AutomationElement.AutomationIdProperty)
            {
                // Only child windows have control ids - top-level windows have HMENUs instead
                // So first check that this is not top-level
                if(IsTopLevelWindow(_hwnd))
                {
                    return null;
                }

                int id = Misc.GetWindowLong(_hwnd, SafeNativeMethods.GWL_ID);
                // Ignore controls with no id, or generic static text (-1)
                if( id == 0 || id == -1 )
                {
                    return null;
                }

                // Return string representation of id...
                return id.ToString(CultureInfo.InvariantCulture);
            }
            else if (idProp == AutomationElement.ClassNameProperty)
            {
                return ProxyManager.GetClassName( _hwnd );
            }
            else if (idProp == AutomationElement.NameProperty)
            {
                // For now, assume window text is same as name.
                // Not true for edits, combos, and other controls that use labels,
                // but will deal with that later.
                IntPtr len = Misc.SendMessageTimeout( _hwnd, UnsafeNativeMethods.WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero );
                int ilen = len.ToInt32();

                if (ilen == 0)
                {
                    return "";
                }

                // Length passes to SendMessage includes terminating NUL
                StringBuilder str = new StringBuilder( ilen + 1 );
                if (Misc.SendMessageTimeout(_hwnd, UnsafeNativeMethods.WM_GETTEXT, new IntPtr(ilen + 1), str) == IntPtr.Zero)
                {
                    str[0] = '\0';
                }

                // get rid of any & used for shortcut keys
                return Misc.StripMnemonic(str.ToString());
            }
            else if (idProp == AutomationElement.IsEnabledProperty)
            {
                return IsWindowReallyEnabled( _hwnd );
            }
            else if (idProp == AutomationElement.ProcessIdProperty)
            {
                // Get the pid of the process that the HWND lives in, not the
                // pid that this proxy lives in
                int pid;
                // GetWindowThreadProcessId does use SetLastError().  So a call to GetLastError() would be meanless.
                // Disabling the PreSharp warning.
#pragma warning suppress 6523
                if (SafeNativeMethods.GetWindowThreadProcessId(_hwnd, out pid) == 0)
                {
                    throw new ElementNotAvailableException();
                }

                return pid;
            }
            else if( idProp == AutomationElement.NativeWindowHandleProperty )
            {
                // Need to downcast to Int32, since IntPtr's are not remotable.
                return ((IntPtr) _hwnd).ToInt32();
            }
            else if (idProp == AutomationElement.HasKeyboardFocusProperty)
            {
                SafeNativeMethods.GUITHREADINFO gti = new SafeNativeMethods.GUITHREADINFO ();

                if (!Misc.GetGUIThreadInfo(0, ref gti))
                {
                    return false;
                }

                return  (gti.hwndFocus == _hwnd) || (SafeNativeMethods.IsChild(_hwnd, gti.hwndFocus));
            }
            else if (idProp == AutomationElement.FrameworkIdProperty)
            {
                return Misc.IsWindowsFormsControl(ProxyManager.GetClassName(_hwnd)) ? "WinForm" : "Win32";
            }
            else if (idProp == AutomationElement.ControlTypeProperty)
            {
                if (IsWindowPatternWindow(_hwnd))
                {
                    return ControlType.Window.Id;
                }
                else
                {
                    return ControlType.Pane.Id;
                }
            }

            return null;
        }

        IRawElementProviderSimple IRawElementProviderSimple.HostRawElementProvider
        {
            get
            {
                // Not needed, since this *is* a host - HWNDs aren't hosted in anything else,
                // they are the base UI that everything else lives in.
                return null;
            }
        }

        #endregion IRawElementProviderSimple

        #region IRawElementProviderFragment

        IRawElementProviderFragment IRawElementProviderFragment.Navigate(NavigateDirection direction)
        {
            HwndProxyElementProvider dest = null;
            switch (direction)
            {
                case NavigateDirection.Parent:          dest = GetParent(); break;
                case NavigateDirection.FirstChild:      dest = GetFirstChild(); break;
                case NavigateDirection.LastChild:       dest = GetLastChild(); break;
                case NavigateDirection.NextSibling:     dest = GetNextSibling(); break;
                case NavigateDirection.PreviousSibling: dest = GetPreviousSibling(); break;
            }
            return dest;
        }

        int[] IRawElementProviderFragment.GetRuntimeId()
        {
            // Ideally we can declare this as a constant somewhere, but AvalonPAW also
            // needs to be in sync. We still want it to be internal and not part of
            // UIAccessModelc.s
            return HwndProxyElementProvider.MakeRuntimeId(_hwnd);
        }

        Rect IRawElementProviderFragment.BoundingRectangle
        {
            get
            {
                // Special case for minimized windows - top level minimized windows
                // are actually moved off to a strange location (eg. -32000, -32000)
                // well off the display - so that the user only sees the buttons on
                // the taskbar instead.
                if (IsTopLevelWindow(_hwnd) && SafeNativeMethods.IsIconic(_hwnd))
                {
                    return Rect.Empty;
                }

                NativeMethods.RECT rcW32 = new NativeMethods.RECT();
                if (!Misc.GetWindowRect(_hwnd, out rcW32))
                {
                    return Rect.Empty;
                }
                return new Rect(rcW32.left, rcW32.top, rcW32.right - rcW32.left, rcW32.bottom - rcW32.top);
            }
        }

        IRawElementProviderSimple[] IRawElementProviderFragment.GetEmbeddedFragmentRoots()
        {
            ArrayList embeddedRoots = new ArrayList(6);
            GetAllUIFragmentRoots(_hwnd, false, embeddedRoots);
            return (IRawElementProviderSimple[])embeddedRoots.ToArray(typeof(IRawElementProviderSimple));
        }

        void IRawElementProviderFragment.SetFocus()
        {
            SetFocus(_hwnd);
        }

        IRawElementProviderFragmentRoot IRawElementProviderFragment.FragmentRoot
        {
            get
            {
                return GetRootProvider();
            }
        }

        #endregion IRawElementProviderFragment

        #region IRawElementProviderFragmentRoot

        IRawElementProviderFragment IRawElementProviderFragmentRoot.ElementProviderFromPoint(double x, double y)
        {
            return ElementProviderFromPoint(_hwnd, x, y);
        }

        IRawElementProviderFragment IRawElementProviderFragmentRoot.GetFocus()
        {
            return GetFocusedProvider();
        }

        #endregion IRawElementProviderFragmentRoot

        //------------------------------------------------------
        //
        //  Interface IWindowProvider
        //
        //------------------------------------------------------

        #region Interface IWindowProvider

        void IWindowProvider.SetVisualState( WindowVisualState state )
        {
            if ( !SafeNativeMethods.IsWindow( _hwnd ) )
                throw new ElementNotAvailableException();

            switch ( state )
            {
                case WindowVisualState.Normal:
                {
                    // you can't really do anything to a disabled window
                    if ( IsBitSet(GetWindowStyle(), SafeNativeMethods.WS_DISABLED) )
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));

                    // If already in the normal state, do not need to do anything.
                    if (((IWindowProvider)this).VisualState == WindowVisualState.Normal)
                    {
                        return;
                    }

                    ClearMenuMode();
                    UnsafeNativeMethods.WINDOWPLACEMENT wp = new UnsafeNativeMethods.WINDOWPLACEMENT();

                    wp.length = Marshal.SizeOf(typeof(UnsafeNativeMethods.WINDOWPLACEMENT));

                    // get the WINDOWPLACEMENT information
                    if (!Misc.GetWindowPlacement(_hwnd, ref wp))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }

                    wp.showCmd = UnsafeNativeMethods.SW_RESTORE;

                    // Use SetWindowPlacement to set state to normal because if the window is maximized then minimized
                    // sending SC_RESTORE puts it back to maximized instead of normal.
                    if (!Misc.SetWindowPlacement(_hwnd, ref wp))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }

                    return;
                }

                case WindowVisualState.Minimized:
                {
                    if (!((IWindowProvider)this).Minimizable)
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));

                    // If already minimzed, do not need to do anything.
                    if (((IWindowProvider)this).VisualState == WindowVisualState.Minimized)
                    {
                        return;
                    }

                    ClearMenuMode();

                    if (!Misc.PostMessage(_hwnd, UnsafeNativeMethods.WM_SYSCOMMAND, (IntPtr)UnsafeNativeMethods.SC_MINIMIZE, IntPtr.Zero))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }

                    return;
                }

                case WindowVisualState.Maximized:
                {
                    if ( ! ((IWindowProvider)this).Maximizable )
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));

                    // If already maximized, do not need to do anything.
                    if (((IWindowProvider)this).VisualState == WindowVisualState.Maximized)
                    {
                        return;
                    }

                    ClearMenuMode();

                    if (!Misc.PostMessage(_hwnd, UnsafeNativeMethods.WM_SYSCOMMAND, (IntPtr)UnsafeNativeMethods.SC_MAXIMIZE, IntPtr.Zero))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }

                    return;
                }

                default:
                {
                    Debug.Assert(false,"unexpected switch() case:");
                    throw new ArgumentException(SR.Get(SRID.UnexpectedWindowState),"state");
                }

            }

        }

        void IWindowProvider.Close()
        {
            ClearMenuMode();

            if (!Misc.PostMessage(_hwnd, UnsafeNativeMethods.WM_SYSCOMMAND, (IntPtr)UnsafeNativeMethods.SC_CLOSE, IntPtr.Zero))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }
        }

        bool IWindowProvider.WaitForInputIdle( int milliseconds )
        {
           if( milliseconds < 0 )
               throw new ArgumentOutOfRangeException( "milliseconds" );

            // Implementation note:  This method is usually used in response to handling a WindowPattern.WindowOpenedEvent.
            // In this case it works for both legacy and WCP windows. This is because the WindowOpenedEvent uses a private
            // WinEvent to detect when WCP UI appears (and this event is only fired when the UI is ready to interact with).
            // However, if called at other times, it may not work for WCP windows.  This is because to detect whether the
            // window is idle requires checking if the UIContext is idle and from this point in the code we can't do that.

            // For WaitForInputIdle to work correctly in all cases there needs to be a way to detect WCP tree being idle.
            // It also doesn't work for Console windows, so those need to be treated specially.  Need to check
            // what ThreadState and WaitReason are for Console app when busy & idle - will that work?

            //
            // To detect if the GUI thread is idle, need to get a ProcessThread object for it.  First get the PID of _hwnd,
            // then turn it into a Process object.  Can then search for the gui thread in its thread collection.
            //
            int pid;
            // GetWindowThreadProcessId does use SetLastError().  So a call to GetLastError() would be meanless.
            // Disabling the PreSharp warning.
#pragma warning suppress 6523
            int guiThreadId = SafeNativeMethods.GetWindowThreadProcessId(_hwnd, out pid);
            if ( guiThreadId == 0 )
            {
                throw new ElementNotAvailableException();
            }

            //
            // Wait for when the thread is idle.  Note that getting the Process object and Threads collection
            // for a process returns a copy so need to loop and re-get the Process and collection each time.
            //
            int waitCycles = milliseconds / 100 + 1;
            for (int i = 0; i < waitCycles; i++)
            {
                //
                // Find a ProcessThread object that corresponds to the thread _hwnd belongs to
                //
                System.Diagnostics.Process targetProcess = null;
                try
                {
                    targetProcess = System.Diagnostics.Process.GetProcessById( pid );
                }
                catch ( SystemException /*err*/ )
                {
                    // Process.GetProcessById may throw if the process has exited
                    // Maybe pass err in as inner exception once that ctor is available?
                    throw new ElementNotAvailableException();
                }

                //
                // Find the gui thread in this Process object's threads
                //
                ProcessThread procThread = null;
                try
                {
                    foreach ( ProcessThread thread in targetProcess.Threads )
                    {
                        if ( thread.Id == guiThreadId )
                        {
                            procThread = thread;
                            break;
                        }
                    }
                }
                catch (InvalidOperationException /*err*/)
                {
                    // Process.Threads may throw if the process has exited
                    // Maybe pass in err as inner exception once that ctor is available?
                    throw new ElementNotAvailableException();
                }

                //
                // If the thread wasn't found then this element isn't valid anymore
                //
                if ( procThread == null )
                    throw new ElementNotAvailableException();

                if ( procThread.ThreadState == System.Diagnostics.ThreadState.Wait &&
                     procThread.WaitReason == ThreadWaitReason.UserRequest )
                {
                    return true;
                }
                //Console.WriteLine( "Waiting {0} ThreadState {1} WaitReason {2}...", i, procThread.ThreadState,
                //            procThread.ThreadState == System.Diagnostics.ThreadState.Wait?procThread.WaitReason.ToString():"N/A");
                Thread.Sleep( 100 );
            }
            return false;
        }

        bool IWindowProvider.Maximizable
        {
            get
            {
                int style = GetWindowStyle();
                if (style == 0)
                {
                    return false;
                }

                if (IsBitSet(style, SafeNativeMethods.WS_DISABLED))
                {
                    return false;
                }

                return IsBitSet(style, SafeNativeMethods.WS_MAXIMIZEBOX);
            }
        }

        bool IWindowProvider.Minimizable
        {
            get
            {
                int style = GetWindowStyle();
                if (style == 0)
                {
                    return false;
                }

                if (IsBitSet(style, SafeNativeMethods.WS_DISABLED))
                {
                    return false;
                }

                return IsBitSet(style, SafeNativeMethods.WS_MINIMIZEBOX);
            }
        }

        bool IWindowProvider.IsModal
        {
            get
            {
                if (!SafeNativeMethods.IsWindow(_hwnd))
                {
                    // PreFast will flag this as a warning, 56503/6503: Property get methods should not throw exceptions.
                    // Since we communicate with the underlying control to get the information
                    // it is correct to throw an exception if that control is no longer there.
#pragma warning suppress 6503
                    throw new ElementNotAvailableException();
                }

                NativeMethods.HWND hwndOwner = GetRealOwner( _hwnd );
                if ( hwndOwner != NativeMethods.HWND.NULL )
                {
                    return IsBitSet(GetWindowStyle(hwndOwner), SafeNativeMethods.WS_DISABLED);
                }

                return false;
            }
        }

        WindowVisualState IWindowProvider.VisualState
        {
            get
            {
                int style = GetWindowStyle();
                if ( IsBitSet(style, SafeNativeMethods.WS_MAXIMIZE) )
                {
                    return WindowVisualState.Maximized;
                }
                else if ( IsBitSet(style, SafeNativeMethods.WS_MINIMIZE) )
                {
                    return WindowVisualState.Minimized;
                }
                else
                {
                    return WindowVisualState.Normal;
                }
            }
        }

        WindowInteractionState IWindowProvider.InteractionState
        {
            // Note: we should consider Implementing InteractionState by finding the gui thread of the 
            // process and mapping ThreadState and WaitReason to a WindowInteractionState
            get
            {
                // a window is considered to be Not responing if it does not call
                // GetMessage, PeekMessage, WaitMessage, or SendMessage
                // within the past five seconds.   This call to SendMessageTimeout
                // will test if that window is responding and will timeout after 5 seconds
                // (Uses a timeout of 0 so that the check for non-responsive state returns immediately)
                IntPtr dwResult;
                // This is just a ping to the hwnd and no data is being returned so suppressing presharp:
                // Call 'Marshal.GetLastWin32Error' or 'Marshal.GetHRForLastWin32Error' before any other interop call.
#pragma warning suppress 56523
                IntPtr ret = UnsafeNativeMethods.SendMessageTimeout(_hwnd, UnsafeNativeMethods.WM_NULL, IntPtr.Zero, IntPtr.Zero, UnsafeNativeMethods.SMTO_ABORTIFHUNG, 0, out dwResult);
                if ( ret == IntPtr.Zero )
                {
                    // If this is a valid window that is not responding just return NotResponding.
                    // If the window is not valid than assume when the user asked it was valid but
                    // in the mean time has gone away or been closed.  So then we return closing.
                    if ( SafeNativeMethods.IsWindow( _hwnd ) )
                        return WindowInteractionState.NotResponding;
                    else
                        return WindowInteractionState.Closing;
                }

                int style = GetWindowStyle();
                if (style == 0)
                {
                    return WindowInteractionState.Closing;
                }

                // if the window is disabled it may be that way because it is blocked by a
                // modal dialog so check to see if that is the case if not make it Running.
                if (IsBitSet(style, SafeNativeMethods.WS_DISABLED))
                {
                    if ( FindModalWindow() )
                        return WindowInteractionState.BlockedByModalWindow;
                    else
                        return WindowInteractionState.Running;
                }

                return WindowInteractionState.ReadyForUserInteraction;
            }
        }

        bool IWindowProvider.IsTopmost
        {
            get
            {
                return IsBitSet(GetWindowExStyle(), SafeNativeMethods.WS_EX_TOPMOST);
            }
        }

        #endregion Interface IWindowProvider

        //------------------------------------------------------
        //
        //  Interface ITransformProvider
        //
        //------------------------------------------------------

        #region Interface ITransformProvider

        void ITransformProvider.Move( double x, double y )
        {
            if ( ! ((ITransformProvider)this).CanMove )
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));

            int extendedStyle = GetWindowExStyle();
            if ( IsBitSet(extendedStyle, SafeNativeMethods.WS_EX_MDICHILD) )
            {
                // we always deal in screen pixels.  But if its an MDI window it interprets these as
                // client pixels so convert them to get the expected results.
                NativeMethods.POINT point = new NativeMethods.POINT((int)x, (int)y);
                NativeMethods.HWND hwndParent = SafeNativeMethods.GetAncestor(NativeMethods.HWND.Cast(_hwnd), SafeNativeMethods.GA_PARENT);
                if (!MapWindowPoints(NativeMethods.HWND.NULL, hwndParent, ref point, 1))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }
                x = point.x;
                y = point.y;

                // Make sure the MDI child stays on the parents client area.
                NativeMethods.RECT currentRect = new NativeMethods.RECT();
                if (!Misc.GetWindowRect(_hwnd, out currentRect))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }
                int currentHeight = currentRect.bottom - currentRect.top;
                int currentWidth = currentRect.right - currentRect.left;

                int dx = SafeNativeMethods.GetSystemMetrics(SafeNativeMethods.SM_CXHSCROLL);
                int dy = SafeNativeMethods.GetSystemMetrics(SafeNativeMethods.SM_CYHSCROLL);

                // If to far to the left move right edge to be visible.
                // Move the left edge the absalute differance of the right to the origin plus a little more to be visible.
                if (x + currentWidth < 0)
                {
                    x += ((x + currentWidth) * -1 + dx);
                }
                // If to far off the top move bottom edge down to be visible.
                // Move the top edge the absalute differance of the bottom to the origin plus a little more to be visible.
                if (y + currentHeight < 0)
                {
                    y += ((y + currentHeight) * -1 + dy);
                }

                NativeMethods.RECT parentRect = new NativeMethods.RECT();
                if (!Misc.GetClientRect(hwndParent, out parentRect))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }
                // If to far to the right move left edge to be visible.
                // Move the left edge back the diffance of it and the parent's client area right side plus a little more to be visible.
                if (x > parentRect.right)
                {
                    x -= (x - parentRect.right + dx);
                }
                // If to far off the bottome move top edge down to be visible.
                // Move the top edge up the diffance of it and the parent's client area bottom side plus a little more to be visible.
                if (y > parentRect.bottom)
                {
                    y -= (y - parentRect.bottom + dy);
                }
            }

            // position the window keeping the zorder the same and not resizing.
            // We do this first so that the window is moved in terms of screen coordinates.
            // The WindowPlacement APIs take in to account the workarea which ends up
            // putting the window in the wrong place
            if (!Misc.SetWindowPos(_hwnd, NativeMethods.HWND.NULL, (int)x, (int)y, 0, 0, UnsafeNativeMethods.SWP_NOSIZE | UnsafeNativeMethods.SWP_NOZORDER | UnsafeNativeMethods.SWP_NOACTIVATE))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            UnsafeNativeMethods.WINDOWPLACEMENT wp = new UnsafeNativeMethods.WINDOWPLACEMENT();
            wp.length = Marshal.SizeOf(typeof(UnsafeNativeMethods.WINDOWPLACEMENT));

            // get the WINDOWPLACEMENT information.  This includes the coordinates in
            // terms of the workarea.
            if (!Misc.GetWindowPlacement(_hwnd, ref wp))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            int style = GetWindowStyle();
            if (style == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }
            if ( IsBitSet(style, SafeNativeMethods.WS_MINIMIZE) )
            {
                // If the window is minimized the parameters have to be setup differently
                wp.ptMinPosition.y = (int) y;
                wp.ptMinPosition.x = (int) x;
                wp.flags = UnsafeNativeMethods.WPF_SETMINPOSITION;

                // Use SetWindowPlacement to move the window because it handles the case where the
                // window is move completly off the screen even in the multi-mon case.  If this happens
                // it will place the window on the primary monitor at a location closest to the taget.
                if (!Misc.SetWindowPlacement(_hwnd, ref wp))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }
            }
            else
            {
                NativeMethods.RECT currentRect = new NativeMethods.RECT();

                if (!Misc.GetWindowRect(_hwnd, out currentRect))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }

                // Use SetWindowPlacement to move the window because it handles the case where the
                // window is move completly off the screen even in the multi-mon case.  If this happens
                // it will place the window on the primary monitor at a location closest to the taget.
                if (!Misc.SetWindowPlacement(_hwnd, ref wp))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }

                // check to make sure SetWindowPlacement has not changed the size of our window
                int currentHeight = currentRect.bottom - currentRect.top;
                int currentWidth = currentRect.right - currentRect.left;

                if (!Misc.GetWindowPlacement(_hwnd, ref wp))
                {
                    throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                }

                int newHeight = wp.rcNormalPosition.bottom - wp.rcNormalPosition.top;
                int newWidth = wp.rcNormalPosition.right -wp.rcNormalPosition.left;

                if ( currentHeight != newHeight || currentWidth != newWidth )
                {
                    wp.rcNormalPosition.bottom = wp.rcNormalPosition.top + currentHeight;
                    wp.rcNormalPosition.right = wp.rcNormalPosition.left + currentWidth;

                    if (!Misc.SetWindowPlacement(_hwnd, ref wp))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
                    }
                }
            }
        }

        void ITransformProvider.Resize( double width, double height )
        {
            if ( !SafeNativeMethods.IsWindow( _hwnd ) )
                throw new ElementNotAvailableException();

            int widthInt = (int) width;
            int heightInt = (int) height;

            if ( ! ((ITransformProvider)this).CanResize )
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));

            UnsafeNativeMethods.MINMAXINFO minMaxInfo = new UnsafeNativeMethods.MINMAXINFO();

            // get the largest window size that can be produced by using the borders to size the window
            int x = SafeNativeMethods.GetSystemMetrics(SafeNativeMethods.SM_CXMAXTRACK);
            int y = SafeNativeMethods.GetSystemMetrics(SafeNativeMethods.SM_CYMAXTRACK);

            minMaxInfo.ptMaxSize = new NativeMethods.POINT( x, y );
            minMaxInfo.ptMaxPosition = new NativeMethods.POINT(0, 0);
            minMaxInfo.ptMinTrackSize = new NativeMethods.POINT(1, 1);
            minMaxInfo.ptMaxTrackSize = new NativeMethods.POINT( x, y );

            // if the window stopped responding there is a chance that resizing will not work
            // Don't check the return from SendMessageTimeout and go ahead
            // and try to resize in case it works.  The minMaxInfo struct has resonable
            // values even if this fails.
            Misc.SendMessageTimeout(_hwnd, UnsafeNativeMethods.WM_GETMINMAXINFO, IntPtr.Zero, ref minMaxInfo);

            if ( widthInt < minMaxInfo.ptMinTrackSize.x )
                widthInt = minMaxInfo.ptMinTrackSize.x;

            if ( heightInt < minMaxInfo.ptMinTrackSize.y )
                heightInt = minMaxInfo.ptMinTrackSize.y;

            if ( widthInt > minMaxInfo.ptMaxTrackSize.x )
                widthInt = minMaxInfo.ptMaxTrackSize.x;

            if ( heightInt > minMaxInfo.ptMaxTrackSize.y )
                heightInt = minMaxInfo.ptMaxTrackSize.y;

            UnsafeNativeMethods.WINDOWPLACEMENT wp = new UnsafeNativeMethods.WINDOWPLACEMENT();
            wp.length = Marshal.SizeOf(typeof(UnsafeNativeMethods.WINDOWPLACEMENT));

            // get the WINDOWPLACEMENT information
            if (!Misc.GetWindowPlacement(_hwnd, ref wp))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }

            // Calculate the new right and bottom for how the user wants the window resized and update the struct
            wp.rcNormalPosition.right = (widthInt + wp.rcNormalPosition.left);
            wp.rcNormalPosition.bottom = (heightInt + wp.rcNormalPosition.top);

            // Use SetWindowPlacement to move the window because it handles the case where the
            // window is sized completly off the screen even in the multi-mon case.  If this happens
            // it will place the window on the primary monitor at a location closes to the taget.
            if (!Misc.SetWindowPlacement(_hwnd, ref wp))
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
            }
        }

        void ITransformProvider.Rotate( double degrees )
        {
            if (!SafeNativeMethods.IsWindow(_hwnd))
                throw new ElementNotAvailableException();

            throw new InvalidOperationException(SR.Get(SRID.OperationCannotBePerformed));
        }

        bool ITransformProvider.CanMove
        {
            get
            {
                // if the hwnd is not valid there is nothing we can do
                int style = GetWindowStyle();
                if (style == 0)
                {
                    return false;
                }

                // if something is disabled it can't be moved
                if (IsBitSet(style, SafeNativeMethods.WS_DISABLED))
                {
                    return false;
                }

                // if there is a system menu let the system menu move item determine the state.
                if (IsBitSet(style, SafeNativeMethods.WS_SYSMENU))
                {
                    IntPtr hmenu = GetSystemMenuHandle();
                    if (hmenu != IntPtr.Zero)
                    {
                        return IsMenuItemSelectable(hmenu, UnsafeNativeMethods.SC_MOVE);
                    }
                }

                // if something is maximized it can't be moved
                if (IsBitSet(style, SafeNativeMethods.WS_MAXIMIZE))
                {
                    return false;
                }

                int extendedStyle = GetWindowExStyle();

                // minimized windows can't be move with the exception of minimized MDI children
                if ( IsBitSet(style, SafeNativeMethods.WS_MINIMIZE) && !IsBitSet(extendedStyle, SafeNativeMethods.WS_EX_MDICHILD) )
                    return false;

                // WS_BORDER | WS_DLGFRAME is WS_CAPTION.  A moveable window has a caption.
                // I need to test both because using WS_CAPTION returns true if one or the other is true.
                return IsBitSet(style, SafeNativeMethods.WS_BORDER) && IsBitSet(style, SafeNativeMethods.WS_DLGFRAME);
            }
        }

        bool ITransformProvider.CanResize
        {
            get
            {
                // if the hwnd is not valid there is nothing we can do
                int style = GetWindowStyle();
                if (style == 0)
                {
                    return false;
                }

                // if something is disabled it can't be resized
                if (IsBitSet(style, SafeNativeMethods.WS_DISABLED))
                {
                    return false;
                }

                // if there is a system menu let the system menu size item determine the state.
                if (IsBitSet(style, SafeNativeMethods.WS_SYSMENU))
                {
                    IntPtr hmenu = GetSystemMenuHandle();
                    if (hmenu != IntPtr.Zero)
                    {
                        return IsMenuItemSelectable(hmenu, UnsafeNativeMethods.SC_SIZE);
                    }
                }

                // if something is mimimized or maximized it can't be resized
                if ( IsBitSet(style, SafeNativeMethods.WS_MAXIMIZE) || IsBitSet(style, SafeNativeMethods.WS_MINIMIZE) )
                {
                    return false;
                }

                return IsBitSet(style, SafeNativeMethods.WS_THICKFRAME);
            }
        }

        bool ITransformProvider.CanRotate
        {
            get
            {
                // if the hwnd is not valid there is nothing we can do
                if (!SafeNativeMethods.IsWindow(_hwnd))
                {
                    // PreFast will flag this as a warning, 56503/6503: Property get methods should not throw exceptions.
                    // Since we communicate with the underlying control to get the information
                    // it is correct to throw an exception if that control is no longer there.
#pragma warning suppress 6503
                    throw new ElementNotAvailableException();
                }

                return false;
            }
        }

        #endregion Interface ITransformProvider

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal static IRawElementProviderFragmentRoot GetRootProvider()
        {
            NativeMethods.HWND desktop = SafeNativeMethods.GetDesktopWindow();
            return Wrap(desktop);
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // wrapper for GetMenuBarInfo
        unsafe private static bool GetMenuBarInfo(NativeMethods.HWND hwnd, int idObject, uint idItem, out UnsafeNativeMethods.MENUBARINFO mbi)
        {
            mbi = new UnsafeNativeMethods.MENUBARINFO();
            mbi.cbSize = sizeof(UnsafeNativeMethods.MENUBARINFO);
            bool result = Misc.GetMenuBarInfo(hwnd, idObject, idItem, ref mbi);

#if _NEED_DEBUG_OUTPUT
            StringBuilder sb = new StringBuilder(1024);
            sb.Append("MENUBARINFO\n\r");
            sb.Append("{\n\r");
            sb.AppendFormat("\tcbSize = {0},\n\r", mbi.cbSize);
            sb.AppendFormat("\trcBar = ({0}, {1}, {2}, {3}),\n\r", mbi.rcBar.left, mbi.rcBar.top, mbi.rcBar.right, mbi.rcBar.bottom);
            sb.AppendFormat("\thMenu = 0x{0:x8},\n\r", (mbi.hMenu).ToInt32());
            sb.AppendFormat("\thwndMenu = 0x{0:x8},\n\r", (mbi.hwndMenu).ToInt32());
            sb.AppendFormat("\tfocusFlags = 0x{0:x8},\n\r", mbi.focusFlags);
            sb.Append("}\n\r");
            System.Diagnostics.Debug.WriteLine(sb.ToString());
#endif

            return result;
        }

        private IntPtr GetSystemMenuHandle()
        {
            UnsafeNativeMethods.MENUBARINFO mbi;

            if (GetMenuBarInfo(_hwnd, UnsafeNativeMethods.OBJID_SYSMENU, 0, out mbi) && mbi.hMenu != IntPtr.Zero)
            {
                return mbi.hMenu;
            }

            return IntPtr.Zero;
        }

        private static HwndProxyElementProvider Wrap(NativeMethods.HWND hwnd)
        {
            if( hwnd == NativeMethods.HWND.NULL )
            {
                return null;
            }

            return new HwndProxyElementProvider( hwnd );
        }

        // Called to test is the specified hwnd should implement WindowPattern
        // Also used by ClientEventManager
        internal static bool IsWindowPatternWindow( NativeMethods.HWND hwnd )
        {
            // Note: This method shouldn't test if the window is visible; events needs to call this
            // for hwnds that are being hidden.

            // WS_EX_APPWINDOW says treat as an app window - even if it's a WS_POPUP. We check this
            // flag first so it takes precedence over the other checks.
            int extendedStyle = GetWindowExStyle(hwnd);
            if (IsBitSet(extendedStyle, SafeNativeMethods.WS_EX_APPWINDOW))
            {
                return true;
            }

            int style = GetWindowStyle(hwnd);
            if (style == 0)
            {
                return false;
            }

            // WS_BORDER and WS_DLGFRAME together mean there is a caption or titlebar
            bool hasTitleBar = IsBitSet( style, SafeNativeMethods.WS_BORDER ) && IsBitSet( style, SafeNativeMethods.WS_DLGFRAME );

            // WS_EX_TOOLWINDOW is the style that keeps a window from appearing in the taskbar.
            // This test says if hwnd is not on the taskbar and doesn't have a title bar the
            // user is probably not going to consider this a window.  This stops us from putting
            // the WindowPattern on comboboxes, for instance.
            if (IsBitSet( extendedStyle, SafeNativeMethods.WS_EX_TOOLWINDOW ) && !hasTitleBar )
                return false;
            // Similarly for popups...
            if (IsBitSet(style, SafeNativeMethods.WS_POPUP) && !hasTitleBar)
                return false;

            // If it is not a child of the desktop nor an MDI child it can't support WindowPattern
            if (!IsTopLevelWindow(hwnd) && !IsBitSet( extendedStyle, SafeNativeMethods.WS_EX_MDICHILD ) )
                return false;

            return true;
        }

        // Called to test if the specified hwnd should implement TransformPattern.
        // The WS_THICKFRAME style is the stretchy border and the other two styles together indicate a caption.
        // Moving minimized windows is supported only for minimized (iconic) MDI children, CanMove filters out
        // regular minimized windows.
        // The CanMove and CanResize properties do other checks to make sure that the element really can move
        // or resize but those checks are not done here so the pattern will not come and go based on a transeint state.
        private static bool IsTransformPatternWindow( NativeMethods.HWND hwnd )
        {
            int style = GetWindowStyle(hwnd);

            if (style == 0)
            {
                return false;
            }

            // WS_THICKFRAME is the stretchy border so it's resizable
            if ( IsBitSet(style, SafeNativeMethods.WS_THICKFRAME) )
                return true;

            // These two styles together are WS_CAPTION, so this is affectively a test for WS_CAPTION.
            if ( IsBitSet(style, SafeNativeMethods.WS_BORDER) && IsBitSet(style, SafeNativeMethods.WS_DLGFRAME) )
                return true;

            return false;
        }

        // Also used by WindowHideOrCloseTracker.cs
        internal static int[] MakeRuntimeId( NativeMethods.HWND hwnd )
        {
            // Called from event code to guarantee getting a RuntimeId w/o exceptions
            int[] id = new int[2];

            // WCTL #32188 : We need a helper method in UIAutomationCore that takes an hwnd and returns a RuntimeId.
            // For now the first number in the runtime Id has to stay in sync with what is in Automation\UnmanagedCore\UiaNode.cpp.
            // Symptom of this being broken is that WindowPattern.WindowClosed events don't appear to work anymore (because
            // code that tracks Window closes needs to do so using RuntimeId cached from the WindowOpened event).
            id[0] = UiaCoreApi.UiaHwndRuntimeIdBase;
            id[1] = hwnd.h.ToInt32();
            return id;
        }

        #region Navigation helpers

        // Navigation & Owned Windows:
        //
        // In the regular HWND tree, ownership is not taken into account - owned dialogs and
        // popups appear as children of the desktop. GetWindow(GW_OWNER) will report the owning
        // window. GetParent() will also report the owner for owned windows; but report parent
        // for non-owned windows - for that reason, GetAncestor(GA_PARENT) is used in preference
        // to GetParent() since it avoids that inconsistency.
        //
        // As spec'd, we do additional work to expose owned windows as children of their owners -
        // these appear before the regular children. Eg. Notepad's File/Open dialog appears as
        // a child of the Notepad window, not as a child of the desktop.
        //
        // This means that code that scans for valid children - skipping over invisible and
        // zero-sized ones - also has to skip over ones that are owned and which will instead
        // appear elsewhere in the tree.
        //
        // Owned windows appear before regular child windows in the exposed tree:
        // FirstChild looks for owned windows first; NextSibling has to transition from
        // owned windows to child windows; PreviousSibling has to transition from child windows
        // to owned windows, and LastChild has to check for owned windows if there are no
        // child windows.
        //
        // Note that we only deal with ownership for top-level windows - it appears possible
        // that windows might allow child windows to be owned or to be owners, however, no apps
        // appear to use this; additionally, allowing for this would be very expensive - *every*
        // first child operation may involve scanning all desktop windows looking for potential owners.
        //
        // - Microsoft 8/14/03

        private const int ScanPrev = SafeNativeMethods.GW_HWNDPREV;
        private const int ScanNext = SafeNativeMethods.GW_HWNDNEXT;
        private const bool IncludeSelf = true;
        private const bool ExcludeSelf = false;

        // Helper method to find next or previous visible window in specified direction.
        // Passes through NULL
        // includeSelf indicates whether the start hwnd should be considered a candidate, or always skipped over
        private static NativeMethods.HWND ScanVisible( NativeMethods.HWND hwnd, int dir, bool includeSelf, NativeMethods.HWND hwndOwnedBy )
        {
            if( hwnd == NativeMethods.HWND.NULL )
                return hwnd;

            if( ! includeSelf )
            {
                hwnd = Misc.GetWindow( hwnd, dir );
            }

            for( ; hwnd != NativeMethods.HWND.NULL ; hwnd = Misc.GetWindow( hwnd, dir ) )
            {
                if( ! IsWindowReallyVisible( hwnd ) )
                {
                    continue;
                }

                NativeMethods.HWND hwndOwner = GetRealOwner( hwnd );
                if( hwndOwner != hwndOwnedBy )
                {
                    continue;
                }

                break;
            }

            return hwnd;
        }

        // For given parent window, return first or last owned window
        private NativeMethods.HWND GetFirstOrLastOwnedWindow( NativeMethods.HWND parent, bool wantFirst )
        {
            if( ! IsTopLevelWindow( parent ) )
            {
                return NativeMethods.HWND.NULL;
            }

            NativeMethods.HWND desktop = SafeNativeMethods.GetDesktopWindow();
            NativeMethods.HWND scan = Misc.GetWindow(desktop, SafeNativeMethods.GW_CHILD);
            if( ! wantFirst )
            {
                // Want last owned window, so jump to last sibling and work back from there...
                scan = Misc.GetWindow(scan, SafeNativeMethods.GW_HWNDLAST);
            }

            // Look in appropriate direction for a top-level window with same owner...
            return ScanVisible( scan, wantFirst? ScanNext : ScanPrev, IncludeSelf, parent );
        }

        private HwndProxyElementProvider GetParent()
        {
            // Do allow this to be called for invisible HWNDs - for now,
            // this avoids issue where an Avalon popup fires a focus or
            // top-level event but is not yet visible.
            //
            // //If its not really visible then it is not available in the tree.
            // if(!IsWindowReallyVisible(_hwnd))
            // {
            //    throw new ElementNotAvailableException();
            // }

            NativeMethods.HWND parent = SafeNativeMethods.GetAncestor(_hwnd, SafeNativeMethods.GA_PARENT);

            // If this is a top-level window, then check if there's an owner first...
            if (parent == SafeNativeMethods.GetDesktopWindow())
            {
                NativeMethods.HWND hwndOwner = GetRealOwner(_hwnd);
                if (hwndOwner != NativeMethods.HWND.NULL)
                {
                    return HwndProxyElementProvider.Wrap(hwndOwner);
                }
            }

            // No owner, so use regular parent
            return HwndProxyElementProvider.Wrap(parent);
        }

        private HwndProxyElementProvider GetNextSibling()
        {
            //If its not really visible then it is not available in the tree.
            if (!IsWindowReallyVisible(_hwnd))
            {
                throw new ElementNotAvailableException();
            }

            // if this is an owned top-level window, look for next window with same owner...
            if (IsTopLevelWindow(_hwnd))
            {
                NativeMethods.HWND hwndOwner = GetRealOwner(_hwnd);
                if (hwndOwner != NativeMethods.HWND.NULL)
                {
                    // Look for next top-level window with same owner...
                    NativeMethods.HWND hwnd = ScanVisible(_hwnd, ScanNext, ExcludeSelf, hwndOwner);
                    if (hwnd == NativeMethods.HWND.NULL)
                    {
                        // no more owned windows by this owner - so move on to actual child windows of the same owner
                        hwnd = Misc.GetWindow(hwndOwner, SafeNativeMethods.GW_CHILD);
                        hwnd = ScanVisible(hwnd, ScanNext, IncludeSelf, NativeMethods.HWND.NULL);
                    }

                    return HwndProxyElementProvider.Wrap(hwnd);
                }
                // Top-level but no owner, fall through...
            }

            // Not and owned-top-level window - just get regular next sibling
            NativeMethods.HWND next = ScanVisible(_hwnd, ScanNext, ExcludeSelf, NativeMethods.HWND.NULL);
            return HwndProxyElementProvider.Wrap(next);
        }

        private HwndProxyElementProvider GetPreviousSibling()
        {
            //If its not really visible then it is not available in the tree.
            if (!IsWindowReallyVisible(_hwnd))
            {
                throw new ElementNotAvailableException();
            }

            // If this is a toplevel owned window, look for prev window with same owner...
            if (IsTopLevelWindow(_hwnd))
            {
                NativeMethods.HWND hwndOwner = GetRealOwner(_hwnd);
                if (hwndOwner != NativeMethods.HWND.NULL)
                {
                    // Find previous top-level window with same owner...
                    // (If we're at the start, we get null here - Wrap will pass that through.)
                    NativeMethods.HWND hwnd = ScanVisible(_hwnd, ScanPrev, ExcludeSelf, hwndOwner);
                    return HwndProxyElementProvider.Wrap(hwnd);
                }
                // Top-level but no owner, fall through...
            }

            // Not owned - so look for regular prev sibling...
            NativeMethods.HWND prev = ScanVisible(_hwnd, ScanPrev, ExcludeSelf, NativeMethods.HWND.NULL);
            if (prev == NativeMethods.HWND.NULL)
            {
                // No regular child windows before this on - so see if the parent window has any owned
                // windows to navigate to...
                NativeMethods.HWND parent = SafeNativeMethods.GetAncestor(_hwnd, SafeNativeMethods.GA_PARENT);
                prev = GetFirstOrLastOwnedWindow(parent, false);
            }

            return HwndProxyElementProvider.Wrap(prev);
        }

        private HwndProxyElementProvider GetFirstChild()
        {
            // Do allow this to be called for invisible HWNDs - for now,
            // this avoids issue where ReBar defers to an invisible HWND
            // for a band. Real fix is to fix rebar proxy, but doing this
            // to get BVT unblocked.
            //
            // //If its not really visible then it is not available in the tree.
            // if(!IsWindowReallyVisible(_hwnd))
            // {
            //    throw new ElementNotAvailableException();
            // }

            // If this is a top-level window, check for owned windows first...
            NativeMethods.HWND hwnd = GetFirstOrLastOwnedWindow(_hwnd, true);
            if (hwnd == NativeMethods.HWND.NULL)
            {
                // No owned windows - look for first regular child window instead...
                hwnd = Misc.GetWindow(_hwnd, SafeNativeMethods.GW_CHILD);
                hwnd = ScanVisible(hwnd, ScanNext, IncludeSelf, NativeMethods.HWND.NULL);
            }

            return HwndProxyElementProvider.Wrap(hwnd);
        }

        private HwndProxyElementProvider GetLastChild()
        {
            // Do allow this to be called for invisible HWNDs - for now,
            // this avoids issue where ReBar defers to an invisible HWND
            // for a band. Real fix is to fix rebar proxy, but doing this
            // to get BVT unblocked.
            //
            // //If its not really visible then it is not available in the tree.
            // if(!IsWindowReallyVisible(_hwnd))
            // {
            //    throw new ElementNotAvailableException();
            // }

            // Win32 has no simple way to get to the last child, so
            // instead go to the first child, and then its last sibling.
            NativeMethods.HWND hwnd = Misc.GetWindow(_hwnd, SafeNativeMethods.GW_CHILD);
            if (hwnd != NativeMethods.HWND.NULL)
            {
                hwnd = Misc.GetWindow(hwnd, SafeNativeMethods.GW_HWNDLAST);
                hwnd = ScanVisible(hwnd, ScanPrev, IncludeSelf, NativeMethods.HWND.NULL);
            }

            if (hwnd == NativeMethods.HWND.NULL)
            {
                // No regular child windows - if this is a toplevel window, then
                // check if there are any owned windows instead...
                hwnd = GetFirstOrLastOwnedWindow(_hwnd, false);
            }

            return HwndProxyElementProvider.Wrap(hwnd);
        }

        #endregion Navigation helpers

        private static void GetAllUIFragmentRoots(NativeMethods.HWND hwnd, bool includeThis, ArrayList uiFragmentRoots)
        {
            // This is used to scan for other impls that need to be notified when
            // registering events, so only need to save impls that implement the events interface
            // also could optimize by only adding those providers that fire the events we're asking
            // for.

            // Look for native or proxy imlps on this HWND...
            if (includeThis)
            {
                // Note that we only add a single provider from any node we touch - when that provider
                // is converted to a full RESW, the others will be discovered.
                bool addedProvider = false;

                // Check for proxy or native provider - native first (otherwise we'll always get the MSAA proxy via the bridge)...
                if (UiaCoreApi.UiaHasServerSideProvider(hwnd))
                {
                    // Add placeholder provider for the HWND - Core will expand it and get the remote provider
                    uiFragmentRoots.Add(Wrap(hwnd));
                    addedProvider = true;
                }
                else
                {
                    IRawElementProviderSimple proxyProvider = ProxyManager.ProxyProviderFromHwnd(hwnd, 0, UnsafeNativeMethods.OBJID_CLIENT);
                    if (proxyProvider != null)
                    {
                        uiFragmentRoots.Add((IRawElementProviderSimple)proxyProvider);
                        addedProvider = true;
                    }
                }

                // Check for non-client area proxy (but no need to do this if we've already
                // added a provider above)
                if (!addedProvider)
                {
                    IRawElementProviderSimple nonClientProvider = ProxyManager.GetNonClientProvider(hwnd.h);
                    if (nonClientProvider != null)
                    {
                        uiFragmentRoots.Add(nonClientProvider);
                        addedProvider = true;
                    }
                }

                if (addedProvider)
                {
                    // If we've added a provider for this HWND - *don't* go into the children.
                    // When RESW hits the above nodes, it will expand them into full RESWs, including
                    // HWND providers, and will then call those to continue.
                    return;
                }
            }

            // Continue and check children resursively...
            // (Infinite looping is 'possible' (though unlikely) when using GetWindow(...NEXT), so we counter-limit this loop...)
            int SanityLoopCount = 1024;

            // Putting a try/catch around this handles the case where the hwnd or any of its hChild windows disappears during
            // processing.  If any of the subtree is being invalidated then there isn't much use trying to work with it.  This
            // can happen when a menu is collapsed, for instance; the pop-up hwnd disappearing causes this code to execute when
            // AdviseEventRemoved is called because the hwnd is not visible/valid anymore.
            try
            {
                for (NativeMethods.HWND hChild = Misc.GetWindow(hwnd, SafeNativeMethods.GW_CHILD);
                     hChild != NativeMethods.HWND.NULL && --SanityLoopCount > 0;
                     hChild = Misc.GetWindow(hChild, SafeNativeMethods.GW_HWNDNEXT))
                {
                    if (IsWindowReallyVisible(hChild))
                    {
                        // Find all this child's UI fragments
                        GetAllUIFragmentRoots(hChild, true, uiFragmentRoots);
                    }
                }
            }
// PRESHARP: Warning - Catch statements should not have empty bodies
#pragma warning disable 6502
            catch (ElementNotAvailableException)
            {
                // the subtree or its children are gone so quit trying to work with this UI
            }
#pragma warning restore 6502

            if (SanityLoopCount == 0)
            {
                // Should we come up with something better here?
                Debug.Assert(false, "too many children or inf loop?");
            }
        }

        // Check if window is really enabled, taking parent state into account.
        private static bool IsWindowReallyEnabled( NativeMethods.HWND hwnd )
        {

            // Navigate up parent chain. If any ancestor window is
            // not enabled, then that has the effect of disabling this window.
            // All ancestor windows must be enabled for this window to be enabled.
            for( ; ; )
            {
                if( ! SafeNativeMethods.IsWindowEnabled( hwnd ) )
                    return false;

                hwnd = SafeNativeMethods.GetAncestor( hwnd, SafeNativeMethods.GA_PARENT );
                if( hwnd == NativeMethods.HWND.NULL )
                    return true;
            }
        }

        // Check that a window is visible, and has a non-empty rect
        private static bool IsWindowReallyVisible( NativeMethods.HWND hwnd )
        {
            if(!SafeNativeMethods.IsWindowVisible(hwnd))
            {
                return false;
            }

            // get the rect so we can tell if this window has a width and height - if does not it's effectivly invisible
            NativeMethods.RECT rcW32;
            if (!Misc.GetWindowRect(hwnd, out rcW32))
            {
                return false;
            }
            if( (rcW32.right - rcW32.left) <= 0 || (rcW32.bottom - rcW32.top) <= 0)
            {
                return false;
            }

            if (IsWindowCloaked(hwnd))
            {
                return false;
            }

            return true;
        }

        // Check if the window is cloaked. Cloaking is a way to hide a window while it still is technically open so it
        // continues to render/update, so thumbnails and such stay alive, but the window is off screen. The concept
        // of a cloaked window is newly introduced in Win8. So on lower level OSes the pinvoke call to get window
        // attributes is expected to fail with E_INVALID_ARG.
        private static bool IsWindowCloaked(NativeMethods.HWND hwnd)
        {
            int dwCloaked = 0;

            if (SafeNativeMethods.DwmGetWindowAttribute(hwnd, SafeNativeMethods.DWMWA_CLOAKED, ref dwCloaked, sizeof(int)) == 0)
            {
                return dwCloaked != 0;
            }

            // If we're unable to determine whether it's cloaked, fail towards visibility.
            // It's much less broken to have hidden stuff accidentally showing up, rather
            // than stuff being accidentally hidden.
            return false;
        }

        private static bool IsTopLevelWindow( NativeMethods.HWND hwnd )
        {
            return SafeNativeMethods.GetAncestor( hwnd, SafeNativeMethods.GA_PARENT ) == SafeNativeMethods.GetDesktopWindow();
        }

        private static NativeMethods.HWND GetRealOwner( NativeMethods.HWND hwnd )
        {
            NativeMethods.HWND hwndOwner = Misc.GetWindow(hwnd, SafeNativeMethods.GW_OWNER);
            if( hwndOwner == NativeMethods.HWND.NULL )
            {
                return NativeMethods.HWND.NULL;
            }

            // Ignore owners that are invisible - having an invisible owner is a common trick for
            // staying off the running apps list. (eg. Start/Run dialog uses this)
            if( ! IsWindowReallyVisible( hwndOwner ) )
            {
                return NativeMethods.HWND.NULL;
            }

            return hwndOwner;
        }

        private bool SupportsWindowPattern
        {
            get
            {
                // if we already know this hwnd is a Window return what we figured out before
                if ( _windowPatternChecked )
                    return _windowPattern;

                _windowPatternChecked = true;
                try
                {
                    _windowPattern = IsWindowPatternWindow(_hwnd);
                }
                catch (ElementNotAvailableException)
                {
                    // If the element is not available it does not support window patterns.
                    _windowPattern = false;
                }
                return _windowPattern;
            }
        }

        private bool SupportsTransformPattern
        {
            get
            {
                // if we already know this hwnd is a Transform return what we figured out before
                if ( _transformPatternChecked )
                    return _transformPattern;

                _transformPatternChecked = true;
                _transformPattern = IsTransformPatternWindow( _hwnd );
                return _transformPattern;
            }
        }

        private static bool IsBitSet( int flags, int bit )
        {
            return (flags & bit) != 0;
        }

        private bool FindModalWindow()
        {
            int process;
            // GetWindowThreadProcessId does use SetLastError().  So a call to GetLastError() would be meanless.
            // Disabling the PreSharp warning.
#pragma warning suppress 6523
            int thread = SafeNativeMethods.GetWindowThreadProcessId(_hwnd, out process);
            if (thread == 0)
            {
                throw new ElementNotAvailableException();
            }

            SafeNativeMethods.EnumThreadWndProc enumWindows = new SafeNativeMethods.EnumThreadWndProc(EnumWindows);
            GCHandle gch = GCHandle.Alloc(enumWindows);

            // if this returns true it means it went through all the windows and did not find
            // one that was modal.
            bool noModalWindow = SafeNativeMethods.EnumThreadWindows(thread, enumWindows, _hwnd);

            gch.Free();

            return !noModalWindow;
        }

        private bool EnumWindows( NativeMethods.HWND hwnd, NativeMethods.HWND possibleOwner )
        {
            int extendedStyle = GetWindowExStyle(hwnd);
            if ( IsBitSet(extendedStyle, SafeNativeMethods.WS_EX_DLGMODALFRAME) )
            {
                NativeMethods.HWND owner = Misc.GetWindow(hwnd, SafeNativeMethods.GW_OWNER);
                if ( owner.h == possibleOwner.h )
                {
                    return false;
                }
            }

            return true;
        }

        // Checks to see if the process owning the hwnd is currently in menu mode
        // and takes steps to exit menu mode if it is
        static private void ClearMenuMode()
        {
            // Check if we're in menu mode with helper method.
            if (InMenuMode())
            {
                // If we are, send an alt keypress to escape
                Input.SendKeyboardInput(System.Windows.Input.Key.LeftAlt, true);
                Input.SendKeyboardInput(System.Windows.Input.Key.LeftAlt, false);

                // Wait for a few milliseconds for this operation to be completed
                long dwTicks = (long)Environment.TickCount;

                // Wait until the action has been completed
                while (InMenuMode() && ((long)Environment.TickCount - dwTicks) < MenuTimeOut)
                {
                    // Sleep the shortest amount of time possible while still guaranteeing that some sleep occurs
                    System.Threading.Thread.Sleep(1);
                }
            }
        }

        // detect if we're in the menu mode
        private static bool InMenuMode()
        {
            SafeNativeMethods.GUITHREADINFO gui = new SafeNativeMethods.GUITHREADINFO();

            if (!Misc.GetGUIThreadInfo(0, ref gui))
            {
                return false;
            }
            return (SafeNativeMethods.GUI_INMENUMODE == (gui.dwFlags & SafeNativeMethods.GUI_INMENUMODE));
        }

        // determine if the menu item is selectable.
        private bool IsMenuItemSelectable(IntPtr hmenu, int item)
        {
            int state = UnsafeNativeMethods.GetMenuState(hmenu, item, UnsafeNativeMethods.MF_BYCOMMAND);
            bool isDisabled = IsBitSet(state, UnsafeNativeMethods.MF_DISABLED);
            bool isGrayed = IsBitSet(state, UnsafeNativeMethods.MF_GRAYED);

            return !(isDisabled | isGrayed);
        }

        private static HwndProxyElementProvider ElementProviderFromPoint(NativeMethods.HWND current, double x, double y)
        {
            // Algorithm:
            // Start at the current hwnd, then keep going down one level at a time,
            // until we hit the bottom. Don't stop for proxies or HWNDs that has a native
            // implementation - just keep drilling to the lowest HWND. UIA will then drill
            // into that, if it is native/proxy.
            // (This also ensures that if a HWND is nested in another HWND that is a proxy,
            // we'll get the nested HWND first, so the proxy won't need to handle that case.)
            NativeMethods.HWND child = NativeMethods.HWND.NULL;

            for (; ; )
            {
                bool isClientArea;

                child = ChildWindowFromPoint(current, x, y, out isClientArea);
                if (child == NativeMethods.HWND.NULL)
                {
                    // error!
                    return null;
                }

                // If no child at the point, then use the current window...
                if (child == current)
                {
                    break;
                }

                // Windows only nest within the client area of other windows, so if this
                // point is on the non-client area, stop drilling...
                if (!isClientArea)
                {
                    break;
                }

                // continue drilling in...
                current = child;
            }

            return Wrap(child);
        }

        private static bool PtInRect( NativeMethods.RECT rc, double x, double y )
        {
            return x >= rc.left && x < rc.right
                && y >= rc.top && y < rc.bottom;
        }

        private static bool Rect1InRect2( NativeMethods.RECT rc1, NativeMethods.RECT rc2 )
        {
            return rc1.left >= rc2.left
                && rc1.top >= rc2.top
                && rc1.right <= rc2.right
                && rc1.bottom <= rc2.bottom;
        }

        private static IntPtr MAKELPARAM( int low, int high )
        {
            return (IntPtr)((high << 16) | (low & 0xffff));
        }

        // Get the child window at the specified point.
        // Returns IntPtr.NULL on error; returns original window if point is not
        // on any child window.
        // When the returned window is a child, the out param isClientArea indicates
        // whether the returned point is on the client area of the returned HWND.
        private static NativeMethods.HWND ChildWindowFromPoint( NativeMethods.HWND hwnd, double x, double y, out bool isClientArea )
        {
            NativeMethods.HWND hBestFitTransparent = NativeMethods.HWND.NULL;
            NativeMethods.RECT rcBest = new NativeMethods.RECT();
            isClientArea = true;

            IntPtr hrgn = Misc.CreateRectRgn(0, 0, 0, 0); // NOTE: Must be deleted before returning
            if (hrgn == IntPtr.Zero)
            {
                return NativeMethods.HWND.NULL;
            }

            // Infinite looping is 'possible' (though unlikely) when
            // using GetWindow(...NEXT), so we counter-limit this loop...
            int SanityLoopCount = 1024;
            for (NativeMethods.HWND hChild = Misc.GetWindow(hwnd, SafeNativeMethods.GW_CHILD);
                hChild != NativeMethods.HWND.NULL && --SanityLoopCount > 0 ;
                hChild = Misc.GetWindow(hChild, SafeNativeMethods.GW_HWNDNEXT))
            {
                // Skip invisible...
                if( ! IsWindowReallyVisible( hChild ) )
                    continue;

                // Check for rect...
                NativeMethods.RECT rc = new NativeMethods.RECT();
                if (!Misc.GetWindowRect(hChild, out rc))
                {
                    continue;
                }

                // If on Vista, convert the incoming physical screen coords to hwndChild-relative
                // logical coords before using them in [logical] rect comparisons...
                double xLogical = x;
                double yLogical = y;
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    NativeMethods.HWND hwndTopLevel = SafeNativeMethods.GetAncestor(hChild, SafeNativeMethods.GA_ROOT);
                    NativeMethods.POINT pt = new NativeMethods.POINT((int)x, (int)y);
                    try
                    {
                        SafeNativeMethods.PhysicalToLogicalPoint(hwndTopLevel, ref pt);
                        xLogical = pt.x;
                        yLogical = pt.y;
                    }
                    catch (EntryPointNotFoundException)
                    {
                        // Ignore.
                    }
                }

                if(!PtInRect(rc, xLogical, yLogical))
                {
                    continue;
                }

                // Skip disabled child windows with other controls beneath it.
                // (only a few places actually use this,
                // eg. the Date&Time properties window, which has a disabled edit window
                // over the edits/static for the time components - see WinClient#856699)
                int style = GetWindowStyle(hChild);
                if ((style & SafeNativeMethods.WS_CHILD) != 0
                 && (style & SafeNativeMethods.WS_DISABLED) != 0)
                {
                    int x1 = (rc.left + rc.right) / 2;
                    int y1 = (rc.top + rc.bottom) / 2;
                    IntPtr hwndCompare = UnsafeNativeMethods.WindowFromPhysicalPoint(x1, y1);
                    // The WindowFromPoint function does not retrieve a handle to a hidden or disabled window,
                    // even if the point is within the window.  So we should either get the parents hwnd or
                    // the controls hwnd underneath the disabled control, if one exsists.
                    // Note: Using WindowFromPoint within ChildWindowFromPoint has bad perf,
                    // and defeats the purpose of having our own ChildWindowFromPoint impl. Should instead determine
                    // what WPF does here, and adopt that, if needed.
                    if (hwndCompare != (IntPtr)hwnd)
                    {
                        // This means that there is another child under `the disabled child, so we want to exclude
                        // `the disabled child high in the z-order.
                        continue;
                    }
                }

                // Check for transparent layered windows (eg as used by menu and tooltip shadows)
                // (Note that WS_EX_TRANSPARENT has a specific meaning when used with WS_EX_LAYERED
                // that is different then when it is used alone, so we must check both flags
                // together.)
                int exStyle = GetWindowExStyle(hChild);
                if( ( exStyle & SafeNativeMethods.WS_EX_LAYERED ) != 0
                  && ( exStyle & SafeNativeMethods.WS_EX_TRANSPARENT ) != 0 )
                {
                    continue;
                }

                // If window is using a region (eg. Media Player), check whether
                // point is in it...
                if (SafeNativeMethods.GetWindowRgn(hChild.h, hrgn) == SafeNativeMethods.COMPLEXREGION)
                {
                    // hrgn is relative to window (not client or screen), so offset point appropriately...
                    if (!SafeNativeMethods.PtInRegion(hrgn, (int)xLogical - rc.left, (int)yLogical - rc.top))
                    {
                        continue;
                    }
                }

                // Try for transparency and/or non-client areas:
                IntPtr lr = Misc.SendMessageTimeout( hChild, UnsafeNativeMethods.WM_NCHITTEST, IntPtr.Zero, MAKELPARAM( (int)x, (int)y ) );
                if( lr == UnsafeNativeMethods.HTTRANSPARENT )
                {
                    // For reasons best known to the writers of USER, statics - used
                    // as labels - claim to be transparent. So that we do hit-test
                    // to these, we remember the hwnd here, so if nothing better
                    // comes along, we'll use this.

                    // If we come accross two or more of these, we remember the
                    // one that fits inside the other - if any. That way,
                    // we hit-test to siblings 'within' siblings - eg. statics in
                    // a groupbox.

                    if( hBestFitTransparent == NativeMethods.HWND.NULL )
                    {
                        hBestFitTransparent = hChild;
                        if (!Misc.GetWindowRect(hChild, out rcBest))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // Is this child within the last remembered transparent?
                        // If so, remember it instead.
                        NativeMethods.RECT rcChild = new NativeMethods.RECT();
                        if (!Misc.GetWindowRect(hChild, out rcChild))
                        {
                            continue;
                        }
                        if( Rect1InRect2( rcChild, rcBest ) )
                        {
                            hBestFitTransparent = hChild;
                            rcBest = rcChild;
                        }
                    }

                    continue;
                }

                // Got the window!

                // Using the hit-test result and compairing against HTCLIENT is not good enough.  The Shell_TrayWnd control,
                // i.e. the task bar, will never returns a value of HTCLIENT, so check to see if the point is in the client area.
                NativeMethods.RECT rcClient = new NativeMethods.RECT();
                if (!Misc.GetClientRect(hChild, out rcClient) ||
                    !MapWindowPoints(hChild, NativeMethods.HWND.NULL, ref rcClient, 2) ||
                    !PtInRect(rcClient, xLogical, yLogical))
                {
                    isClientArea = false;
                }

                Misc.DeleteObject(hrgn); // finished with region
                return hChild;
            }

            Misc.DeleteObject(hrgn); // finished with region

            if( SanityLoopCount == 0 )
            {
                Debug.Assert(false, "too many children or inf loop?");
                return NativeMethods.HWND.NULL;
            }

            // Did we find a transparent (eg. a static) on our travels? If so, since
            // we couldn't find anything better, may as well use it.
            if( hBestFitTransparent != NativeMethods.HWND.NULL )
            {
                return hBestFitTransparent;
            }

            // Otherwise return the original window (not NULL!) if no child found...
            return hwnd;
        }

        private static bool IsProgmanWindow(NativeMethods.HWND hwnd)
        {
            while (hwnd != NativeMethods.HWND.NULL)
            {
                if (ProxyManager.GetClassName(hwnd).CompareTo("Progman") == 0)
                {
                    return true;
                }
                hwnd = SafeNativeMethods.GetAncestor(hwnd, SafeNativeMethods.GA_PARENT);
            }
            return false;
        }

        // wrapper for MapWindowPoints
        private static bool MapWindowPoints(NativeMethods.HWND hWndFrom, NativeMethods.HWND hWndTo, ref NativeMethods.RECT rect, int cPoints)
        {
            int mappingOffset = NativeMethodsSetLastError.MapWindowPoints(hWndFrom, hWndTo, ref rect, cPoints);
            int lastWin32Error = Marshal.GetLastWin32Error();
            if (mappingOffset == 0)
            {
                // When mapping points to/from Progman and its children MapWindowPoints may fail with error code 1400
                // Invalid Window Handle.  Since Progman is the desktop no mapping is need.
                if ((IsProgmanWindow(hWndFrom) && hWndTo == NativeMethods.HWND.NULL) ||
                    (hWndFrom == NativeMethods.HWND.NULL && IsProgmanWindow(hWndTo)))
                {
                    lastWin32Error = 0;
                }

                Misc.ThrowWin32ExceptionsIfError(lastWin32Error);

                // If the coordinates is at the origin a zero return is valid.
                // Use GetLastError() to check that. Error code 0 is "Operation completed successfull".
                return lastWin32Error == 0;
            }

            return true;
        }

        // wrapper for MapWindowPoints
        private static bool MapWindowPoints(NativeMethods.HWND hWndFrom, NativeMethods.HWND hWndTo, ref NativeMethods.POINT pt, int cPoints)
        {
            int mappingOffset = NativeMethodsSetLastError.MapWindowPoints(hWndFrom, hWndTo, ref pt, cPoints);
            int lastWin32Error = Marshal.GetLastWin32Error();
            if (mappingOffset == 0)
            {
                // When mapping points to/from Progman and its children MapWindowPoints may fail with error code 1400
                // Invalid Window Handle.  Since Progman is the desktop no mapping is need.
                if ((IsProgmanWindow(hWndFrom) && hWndTo == NativeMethods.HWND.NULL) ||
                    (hWndFrom == NativeMethods.HWND.NULL && IsProgmanWindow(hWndTo)))
                {
                    lastWin32Error = 0;
                }

                Misc.ThrowWin32ExceptionsIfError(lastWin32Error);

                // If the coordinates is at the origin a zero return is valid.
                // Use GetLastError() to check that. Error code 0 is "Operation completed successfull".
                return lastWin32Error == 0;
            }

            return true;
        }

        // Gets the style of the window
        private int GetWindowStyle()
        {
            return GetWindowStyle(_hwnd);
        }
        private static int GetWindowStyle(NativeMethods.HWND hwnd)
        {
            int style = Misc.GetWindowLong(hwnd, SafeNativeMethods.GWL_STYLE);
            return style;
        }

        // Gets the extended style of the window
        private int GetWindowExStyle()
        {
            return GetWindowExStyle(_hwnd);
        }
        private static int GetWindowExStyle(NativeMethods.HWND hwnd)
        {
            int exstyle = Misc.GetWindowLong(hwnd, SafeNativeMethods.GWL_EXSTYLE);
            return exstyle;
        }

        #region Set/Get Focus

        // Set focus to specified HWND
        private static bool SetFocus( NativeMethods.HWND hwnd )
        {
            // If already focused, leave as-is. Calling SetForegroundWindow
            // on an already focused HWND will remove focus!
            if( GetFocusedWindow() == hwnd )
            {
                return true;
            }

            // If this is an MDI Child window, send WM_MDIACTIVATE message
            int exStyle = GetWindowExStyle(hwnd);
            if (IsBitSet(exStyle, SafeNativeMethods.WS_EX_MDICHILD))
            {
                NativeMethods.HWND parent = SafeNativeMethods.GetAncestor(hwnd, SafeNativeMethods.GA_PARENT);
                if (parent == IntPtr.Zero)
                {
                    return false;
                }
                IntPtr lresult = Misc.SendMessageTimeout(parent, UnsafeNativeMethods.WM_MDIACTIVATE, (IntPtr)hwnd, IntPtr.Zero);
                return lresult == IntPtr.Zero;
            }

            // Use the hotkey technique:
            // Register a hotkey and send it to ourselves - this gives us the
            // input, and allows us to call SetForegroundWindow.
            short atom = Misc.GlobalAddAtom("FocusHotKey");
            if (atom == 0)
            {
                return false;
            }

            byte vk = 0xB9;
            bool gotHotkey = false;
            for( int tries = 0 ; tries < 10 ; tries++ )
            {
                if( Misc.RegisterHotKey( NativeMethods.HWND.NULL, atom, 0, vk ) )
                {
                    gotHotkey = true;
                    break;
                }
                vk++; // try another key
            }

            if( gotHotkey )
            {
                // Get state of modifiers - and temporarilly release them...
                bool fShiftDown = ( UnsafeNativeMethods.GetAsyncKeyState( UnsafeNativeMethods.VK_SHIFT ) & unchecked((int)0x80000000) ) != 0;
                bool fAltDown = ( UnsafeNativeMethods.GetAsyncKeyState( UnsafeNativeMethods.VK_MENU ) & unchecked((int)0x80000000) ) != 0;
                bool fCtrlDown = ( UnsafeNativeMethods.GetAsyncKeyState( UnsafeNativeMethods.VK_CONTROL ) & unchecked((int)0x80000000) ) != 0;

                if( fShiftDown )
                    Input.SendKeyboardInputVK( UnsafeNativeMethods.VK_SHIFT, false );
                if( fAltDown )
                    Input.SendKeyboardInputVK( UnsafeNativeMethods.VK_MENU, false );
                if( fCtrlDown )
                    Input.SendKeyboardInputVK( UnsafeNativeMethods.VK_CONTROL, false );

                Input.SendKeyboardInputVK( vk, true );
                Input.SendKeyboardInputVK( vk, false );

                // Restore release modifier keys...
                if( fShiftDown )
                    Input.SendKeyboardInputVK( UnsafeNativeMethods.VK_SHIFT, true );
                if( fAltDown )
                    Input.SendKeyboardInputVK( UnsafeNativeMethods.VK_MENU, true );
                if( fCtrlDown )
                    Input.SendKeyboardInputVK( UnsafeNativeMethods.VK_CONTROL, true );

                // Spin in this message loop until we get the hot key
                while (true)
                {
                    // If the hotkey input gets lost (eg due to desktop switch), GetMessage may not return -
                    // so use MsgWait first so we can timeout if there's no message present instead of blocking.
                    int result = Misc.MsgWaitForMultipleObjects(null, false, 2000, UnsafeNativeMethods.QS_ALLINPUT);
                    if (result == UnsafeNativeMethods.WAIT_FAILED || result == UnsafeNativeMethods.WAIT_TIMEOUT)
                        break;

                    UnsafeNativeMethods.MSG msg = new UnsafeNativeMethods.MSG();
                    if (Misc.GetMessage(ref msg, NativeMethods.HWND.NULL, 0, 0) == 0)
                        break;

                    // TranslateMessage() will not set an error to be retrieved with GetLastError,
                    // so set the pragma to ignore the PERSHARP warning.
#pragma warning suppress 6031, 6523
                    UnsafeNativeMethods.TranslateMessage(ref msg);

                    // From the Windows SDK documentation:
                    // The return value specifies the value returned by the window procedure.
                    // Although its meaning depends on the message being dispatched, the return
                    // value generally is ignored.
#pragma warning suppress 6031, 6523
                    UnsafeNativeMethods.DispatchMessage(ref msg);

                    if (msg.message == UnsafeNativeMethods.WM_HOTKEY
                        && msg.wParam == (IntPtr) atom)
                    {
                        break;
                    }
                }

                Misc.UnregisterHotKey(NativeMethods.HWND.NULL, atom);
            }
            Misc.GlobalDeleteAtom(atom);

            // Using this method uses the actual codepath the Alt+Tab uses
            UnsafeNativeMethods.SwitchToThisWindow(hwnd, true);
            return true;
        }

        // Get current focused HWND
        private static NativeMethods.HWND GetFocusedWindow()
        {
            SafeNativeMethods.GUITHREADINFO gti = new SafeNativeMethods.GUITHREADINFO();

            if (!Misc.GetGUIThreadInfo(0, ref gti))
            {
                return NativeMethods.HWND.NULL;
            }

            return gti.hwndFocus;
        }

        // determine focus - taking menu mode into account
        private static IRawElementProviderFragment GetFocusedProvider()
        {
            // Menus are special as far as focus goes - they get a special type of keyboard
            // capture from USER32 that is outside of the regular focus mechanism.
            // Therefore they have to be dealt with specially - regular drilling won't work.
            SafeNativeMethods.GUITHREADINFO gti = new SafeNativeMethods.GUITHREADINFO();
            if (Misc.GetGUIThreadInfo(0, ref gti))
            {
                if ((gti.dwFlags & SafeNativeMethods.GUI_INMENUMODE) != 0
                    || (gti.dwFlags & SafeNativeMethods.GUI_SYSTEMMENUMODE) != 0
                    || (gti.dwFlags & SafeNativeMethods.GUI_POPUPMENUMODE) != 0)
                {
                    // We're in menu mode - see if there's a registered menu focus handler:
                    IRawElementProviderSimple provider = ProxyManager.GetUser32FocusedMenuProvider(gti.hwndMenuOwner);
                    if (provider != null)
                    {
                        Debug.Assert(provider is IRawElementProviderFragment, "Expecting a fragment provider");
                        return provider as IRawElementProviderFragment;
                    }
                }
                else
                {
                    return Wrap(gti.hwndFocus);
                }
            }

            return null;
        }

        #endregion Set/Get Focus

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private NativeMethods.HWND _hwnd;
        private bool _windowPattern = false;
        private bool _windowPatternChecked = false;
        private bool _transformPattern = false;
        private bool _transformPatternChecked = false;

        // Timeout for clearing menus in ms
        private const long MenuTimeOut = 100;

        #endregion Private Fields
    }
}
