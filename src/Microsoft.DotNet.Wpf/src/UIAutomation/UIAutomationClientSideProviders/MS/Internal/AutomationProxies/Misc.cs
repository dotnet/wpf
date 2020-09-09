// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Miscellaneous helper routines


// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using Microsoft.Win32.SafeHandles;
using MS.Win32;
using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using NativeMethodsSetLastError = MS.Internal.UIAutomationClientSideProviders.NativeMethodsSetLastError;

namespace MS.Internal.AutomationProxies
{
    static class Misc
    {

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        //
        //  HrGetWindowShortcut()
        //
        internal static string AccessKey(string s)
        {
            // Get the index of the shortcut
            int iPosShortCut = s.IndexOf('&');

            // Did we found an & or is it at the end of the string
            if (iPosShortCut < 0 || iPosShortCut + 1 >= s.Length)
            {
                return null;
            }

            // Build the result string
            return SR.Get(SRID.KeyAlt) + "+" + s[iPosShortCut + 1];
        }

        // Extend an existing RunTimeID by one element
        internal static int[] AppendToRuntimeId(int[] baseID, int id)
        {
            // For the base case, where parent is a hwnd, baseID will be null,
            // so use AppendRuntimeId instead. UIA will then glue that to the ID
            // of the parent HWND.
            if(baseID == null)
                baseID = new int[] { AutomationInteropProvider.AppendRuntimeId };

            int len = baseID.Length;
            int[] newID = new int[len + 1];

            baseID.CopyTo(newID, 0);
            newID[len] = id;
            return newID;
        }

        internal static double[] RectArrayToDoubleArray(Rect[] rectArray)
        {
            if (rectArray == null)
                return null;
            double[] doubles = new double[rectArray.Length * 4];
            int scan = 0;
            for (int i = 0; i < rectArray.Length; i++)
            {
                doubles[scan++] = rectArray[i].X;
                doubles[scan++] = rectArray[i].Y;
                doubles[scan++] = rectArray[i].Width;
                doubles[scan++] = rectArray[i].Height;
            }
            return doubles;
        }

        // Ensure a window and all its parents are enabled.
        // If not, throw ElementNotEnabledException.
        internal static void CheckEnabled(IntPtr hwnd)
        {
            if (!IsEnabled(hwnd))
            {
                throw new ElementNotEnabledException();
            }
        }

        // Checks to see if the process owning the hwnd is currently in menu mode
        // and takes steps to exit menu mode if it is
        internal static void ClearMenuMode()
        {
            // Check if we're in menu mode with helper method.
            if (InMenuMode())
            {
                // If we are, send an alt keypress to escape
                Input.SendKeyboardInput(Key.LeftAlt, true);
                Input.SendKeyboardInput(Key.LeftAlt, false);

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

        internal static bool CloseHandle(IntPtr processHandle)
        {
            bool result = UnsafeNativeMethods.CloseHandle(processHandle);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        // Compares 2 raw elements and returns true if equal, false otherwise
        internal static bool Compare(ProxySimple el1, ProxySimple el2)
        {
            int[] a1 = el1.GetRuntimeId();
            int[] a2 = el2.GetRuntimeId();
            int l = a1.Length;

            if (l != a2.Length)
                return false;

            for (int i = 0; i < l; i++)
            {
                if (a1[i] != a2[i])
                {
                    return false;
                }
            }

            return true;
        }

        internal static IntPtr DispatchMessage(ref NativeMethods.MSG msg)
        {
            // From the Windows SDK documentation:
            // The return value specifies the value returned by the window procedure.
            // Although its meaning depends on the message being dispatched, the return
            // value generally is ignored.
#pragma warning suppress 6031, 6523
            return UnsafeNativeMethods.DispatchMessage(ref msg);
        }


        internal unsafe static bool EnumChildWindows(IntPtr hwnd, NativeMethods.EnumChildrenCallbackVoid lpEnumFunc, void* lParam)
        {
            bool result = UnsafeNativeMethods.EnumChildWindows(hwnd, lpEnumFunc, lParam);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string className, string wndName)
        {
            IntPtr result = NativeMethodsSetLastError.FindWindowEx(hwndParent, hwndChildAfter, className, wndName);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (result == IntPtr.Zero)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static string GetClassName(IntPtr hwnd)
        {
            StringBuilder sb = new StringBuilder(NativeMethods.MAX_PATH + 1);

            int result = UnsafeNativeMethods.GetClassName(hwnd, sb, NativeMethods.MAX_PATH);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (result == 0)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
                return "";
            }

            return sb.ToString();
        }

        // Get the name of a control and conditionally strip mnemonic.
        // label is the hwnd of the control that is funtioning as the label.  Use GetLabelhwnd to find this.
        // If stripMnemonic is true, amperstrands characters will be stripped out.
        internal static string GetControlName(IntPtr label, bool stripMnemonic)
        {
            if (label == IntPtr.Zero)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder(MaxLengthNameProperty);

            int result = NativeMethodsSetLastError.GetWindowText(label, sb, MaxLengthNameProperty);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (result == 0)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
                return null;
            }

            return stripMnemonic ? StripMnemonic(sb.ToString()) : sb.ToString();

        }

        internal static bool GetClientRectInScreenCoordinates(IntPtr hwnd, ref NativeMethods.Win32Rect rc)
        {
            rc = NativeMethods.Win32Rect.Empty;

            if (!GetClientRect(hwnd, ref rc))
            {
                return false;
            }

            NativeMethods.Win32Point leftTop = new NativeMethods.Win32Point(rc.left, rc.top);
            if (!MapWindowPoints(hwnd, IntPtr.Zero, ref leftTop, 1))
            {
                return false;
            }

            NativeMethods.Win32Point rightBottom = new NativeMethods.Win32Point(rc.right, rc.bottom);
            if (!MapWindowPoints(hwnd, IntPtr.Zero, ref rightBottom, 1))
            {
                return false;
            }

            rc = new NativeMethods.Win32Rect(leftTop.x, leftTop.y, rightBottom.x, rightBottom.y);
            return true;
        }

        internal static bool GetClientRect(IntPtr hwnd, ref NativeMethods.Win32Rect rc)
        {
            bool result = UnsafeNativeMethods.GetClientRect(hwnd, ref rc);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            // When the control is right to left GetClentRect() will return a rectangle with left > right.
            // Normalize thesee rectangle back to left to right.
            rc.Normalize(IsLayoutRTL(hwnd));

            return result;
        }

        internal static bool GetComboBoxInfo(IntPtr hwnd, ref NativeMethods.COMBOBOXINFO cbi)
        {
            bool result = UnsafeNativeMethods.GetComboBoxInfo(hwnd, ref cbi);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool GetCursorPos(ref NativeMethods.Win32Point pt)
        {
            // Vista and beyond use GetPhysicalCursorPos which handles DPI issues
            bool result = (System.Environment.OSVersion.Version.Major >= 6) ? UnsafeNativeMethods.GetPhysicalCursorPos(ref pt)
                                                                            : UnsafeNativeMethods.GetCursorPos(ref pt);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }


        internal static IntPtr GetDC(IntPtr hwnd)
        {
            IntPtr hdc = UnsafeNativeMethods.GetDC(hwnd);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (hdc == IntPtr.Zero)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return hdc;
        }

        internal static IntPtr GetFocusedWindow()
        {
            NativeMethods.GUITHREADINFO gui;

            return ProxyGetGUIThreadInfo(0, out gui) ? gui.hwndFocus : IntPtr.Zero;
        }

        internal static string GetItemToolTipText(IntPtr hwnd, IntPtr hwndToolTip, int item)
        {
            if (hwndToolTip != IntPtr.Zero)
            {
                // We've found the tooltip window, so we won't need to scan for it.

                // Got a tooltip window - use it.
                NativeMethods.TOOLINFO tool = new NativeMethods.TOOLINFO();
                tool.Init(Marshal.SizeOf(typeof(NativeMethods.TOOLINFO)));

                tool.hwnd = hwnd;
                tool.uId = item;

                return XSendMessage.GetItemText(hwndToolTip, tool);
            }
            else
            {
                // Control doesn't know its tooltip window - instead scan for one...

                // Enum the top-level windows owned by this thread...
                uint processId;
                uint threadId = GetWindowThreadProcessId(hwnd, out processId);

                UnsafeNativeMethods.ENUMTOOLTIPWINDOWINFO info = new UnsafeNativeMethods.ENUMTOOLTIPWINDOWINFO();
                info.hwnd = hwnd;
                info.id = item;
                info.name = "";

                UnsafeNativeMethods.EnumThreadWndProc enumToolTipWindows = new UnsafeNativeMethods.EnumThreadWndProc(EnumToolTipWindows);
                GCHandle gch = GCHandle.Alloc(enumToolTipWindows);
                UnsafeNativeMethods.EnumThreadWindows(threadId, enumToolTipWindows, ref info);
                gch.Free();

                return info.name;
            }
        }

        // --------------------------------------------------------------------------
        //
        //  GetLabelhwnd()
        //
        //  This walks backwards among peer windows to find a static field.  It stops
        //  if it gets to the front or hits a group/tabstop, just like the dialog
        //  manager does.
        //
        // Ported from OleAcc\Client.CPP
        // --------------------------------------------------------------------------

        internal static IntPtr GetLabelhwnd(IntPtr hwnd)
        {
            // Sanity check
            if (!UnsafeNativeMethods.IsWindow(hwnd))
            {
                return IntPtr.Zero;
            }

            // Only get labels for child windows - not top-level windows or desktop
            IntPtr hwndParent = Misc.GetParent(hwnd);
            if (hwndParent == IntPtr.Zero || hwndParent == UnsafeNativeMethods.GetDesktopWindow())
            {
                return IntPtr.Zero;
            }

            IntPtr peer = hwnd;

            // If GetWindow fails we're going to exit, no need to call Marshal.GetLastWin32Error
#pragma warning suppress 56523
            while ((peer = NativeMethodsSetLastError.GetWindow(peer, NativeMethods.GW_HWNDPREV)) != IntPtr.Zero)
            {
                //
                // Is this a static dude?
                //
                int code = Misc.ProxySendMessageInt(peer, NativeMethods.WM_GETDLGCODE, IntPtr.Zero, IntPtr.Zero);
                if ((code & NativeMethods.DLGC_STATIC) == NativeMethods.DLGC_STATIC)
                {
                    //
                    // Great, we've found our label.
                    //
                    return peer;
                }

                //
                // Skip invisible controls.
                // Note that we do this after checking if its a static, so that we give invisible statics a chance.
                // Using invisible statics is an easy workaround to add names to controls without changing the visual UI.
                //
                // If GetWindowLong fails we're going to exit, no need to call Marshal.GetLastWin32Error
#pragma warning suppress 56523
                int error = 0;
                int style = UnsafeNativeMethods.GetWindowLong(peer, NativeMethods.GWL_STYLE, out error);
                if ((style & NativeMethods.WS_VISIBLE) != 0)
                    continue;

                //
                // Is this a tabstop or group?  If so, bail out now.
                //
                if ((style & (NativeMethods.WS_GROUP | NativeMethods.WS_TABSTOP)) != 0)
                    break;
            }

            // Failed to find a suitable peer
            return IntPtr.Zero;
        }

        internal static bool GetMenuBarInfo(IntPtr hwnd, int idObject, uint item, ref NativeMethods.MENUBARINFO mbi)
        {
            bool result = NativeMethodsSetLastError.GetMenuBarInfo(hwnd, idObject, item, ref mbi);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static int GetMenuItemCount(IntPtr hmenu)
        {
            int count = UnsafeNativeMethods.GetMenuItemCount(hmenu);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (count == -1)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return count;
        }

        internal static bool GetMenuItemInfo(IntPtr hmenu, int item, bool byPosition, ref NativeMethods.MENUITEMINFO menuItemInfo)
        {
            bool result = UnsafeNativeMethods.GetMenuItemInfo(hmenu, item, byPosition, ref menuItemInfo);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool GetMenuItemRect(IntPtr hwnd, IntPtr hmenu, int item, out NativeMethods.Win32Rect rc)
        {
            bool result = UnsafeNativeMethods.GetMenuItemRect(hwnd, hmenu, item, out rc);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool GetMessage(ref NativeMethods.MSG msg, IntPtr hwnd, int msgFilterMin, int msgFilterMax)
        {
            int result = UnsafeNativeMethods.GetMessage(ref msg, hwnd, msgFilterMin, msgFilterMax);
            int lastWin32Error = Marshal.GetLastWin32Error();

            bool success = (result != 0 && result != -1);
            if (!success)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return success;
        }

        internal static int GetObjectW(IntPtr hObject, int size, ref NativeMethods.LOGFONT lf)
        {
            int result = UnsafeNativeMethods.GetObjectW(hObject, size, ref lf);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (result == 0)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static IntPtr GetParent(IntPtr hwnd)
        {
            IntPtr hwndParent = NativeMethodsSetLastError.GetAncestor(hwnd, NativeMethods.GA_PARENT);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (hwndParent == IntPtr.Zero)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return hwndParent;
        }

        internal static bool GetScrollBarInfo(IntPtr hwnd, int fnBar, ref NativeMethods.ScrollBarInfo sbi)
        {
            bool result = UnsafeNativeMethods.GetScrollBarInfo(hwnd, fnBar, ref sbi);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool GetScrollInfo(IntPtr hwnd, int fnBar, ref NativeMethods.ScrollInfo si)
        {
            bool result = UnsafeNativeMethods.GetScrollInfo(hwnd, fnBar, ref si);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                // 1447     ERROR_NO_SCROLLBARS     The window does not have scroll bars.
                // If GetScrollInfo() fails with ERROR_NO_SCROLLBARS then there is no scroll information
                // to get.  Just return false saying that GetScrollInfo() could not get the information
                if (lastWin32Error == 1447)
                {
                    return false;
                }
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static int GetTextExtentPoint32(IntPtr hdc, string text, int length, out NativeMethods.SIZE size)
        {
            int result = NativeMethodsSetLastError.GetTextExtentPoint32(hdc, text, length, out size);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (result == 0)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        // Calls a message that retrieves a string, but doesn't take a length argument, in a relatively safe manner.
        // Attempts to rapidly resize in hopes of crashing UI Automation should just crash whatever is rapidly resizing.
        // Param "hwnd" the Window Handle
        // Param "uMsg" the Windows Message
        // Param "wParam" the Windows wParam
        // Param "maxLength" the size of the string
        internal static unsafe string GetUnsafeText(IntPtr hwnd, int uMsg, IntPtr wParam, int maxLength)
        {
            uint pageSize = GetPageSize();
            IntPtr memAddr = IntPtr.Zero;     // Ptr to remote mem
            // calculate the size needed for the string
            uint cbSize = (uint)((maxLength + 1) * sizeof(char));
            // resize it to include enough pages for the string, and an extra guarding page, and one extra page to account for shifts.
            cbSize = ((cbSize / pageSize) + 3) * pageSize;

            try
            {
                // Allocate the space
                memAddr = VirtualAlloc(IntPtr.Zero, new UIntPtr(cbSize), UnsafeNativeMethods.MEM_COMMIT, UnsafeNativeMethods.PAGE_READWRITE);

                // Allocate the Final page as No Access, so any attempt to write to it will GPF
                VirtualAlloc(new IntPtr((byte *)memAddr.ToPointer() + cbSize - pageSize), new UIntPtr(pageSize), UnsafeNativeMethods.MEM_COMMIT, UnsafeNativeMethods.PAGE_NOACCESS);

                // Send the message...
                if (ProxySendMessage(hwnd, uMsg, wParam, memAddr) == IntPtr.Zero)
                {
                    return "";
                }

                String str = new string((char*)memAddr.ToPointer(), 0, maxLength);
                // Note: lots of "old world" strings are null terminated
                // Leaving the null termination in the System.String may lead
                // to some issues when used with the StringBuilder
                int nullTermination = str.IndexOf('\0');

                if (-1 != nullTermination)
                {
                    // We need to strip null terminated char and everything behind it from the str
                    str = str.Remove(nullTermination, maxLength - nullTermination);
                }
                return str;
            }
            finally
            {
                // Free the memory
                if (memAddr != IntPtr.Zero)
                {
                    VirtualFree(memAddr, UIntPtr.Zero, UnsafeNativeMethods.MEM_RELEASE);
                }
            }
        }

        internal static IntPtr GetWindow(IntPtr hwnd, int cmd)
        {
            IntPtr resultHwnd = NativeMethodsSetLastError.GetWindow(hwnd, cmd);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (resultHwnd == IntPtr.Zero)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return resultHwnd;
        }

        // Gets the extended style of the window
        internal static int GetWindowExStyle(IntPtr hwnd)
        {
            int lastWin32Error = 0;
            int exstyle = UnsafeNativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, out lastWin32Error);

            if (exstyle == 0)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return exstyle;
        }

        // Gets the id of the window
        internal static int GetWindowId(IntPtr hwnd)
        {
            int lastWin32Error = 0;
            int id = UnsafeNativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_ID, out lastWin32Error);

            if (id == 0)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return id;
        }

        // Gets the parent of the window
        internal static IntPtr GetWindowParent(IntPtr hwnd)
        {
            // NOTE: This may have issues in 64-bit.

            int lastWin32Error = 0;
            int result = UnsafeNativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_HWNDPARENT, out lastWin32Error);

            if (result == 0)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return (IntPtr)result;
        }

        internal static bool GetWindowRect(IntPtr hwnd, ref NativeMethods.Win32Rect rc)
        {
            bool result = UnsafeNativeMethods.GetWindowRect(hwnd, ref rc);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }
            return result;
        }

        // Gets the style of the window
        internal static int GetWindowStyle(IntPtr hwnd)
        {
            int lastWin32Error = 0;
            int style = UnsafeNativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE, out lastWin32Error);

            if (style == 0)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return style;
        }

        internal static uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId)
        {
            // GetWindowThreadProcessId does use SetLastError().  So a call to GetLastError() would be meanless.
            // Disabling the PreSharp warning.
#pragma warning suppress 6523
            uint threadId = UnsafeNativeMethods.GetWindowThreadProcessId(hwnd, out processId);

            if (threadId == 0)
            {
                throw new ElementNotAvailableException();
            }

            return threadId;
        }

        internal static short GlobalAddAtom(string atomName)
        {
            short atom = UnsafeNativeMethods.GlobalAddAtom(atomName);
            int lastWin32Error = Marshal.GetLastWin32Error();
            if (atom == 0)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }
            return atom;
        }

        internal static short GlobalDeleteAtom(short atom)
        {
            short result = NativeMethodsSetLastError.GlobalDeleteAtom(atom);
            ThrowWin32ExceptionsIfError(Marshal.GetLastWin32Error());
            return result;
        }

        // detect if we're in the menu mode
        internal static bool InMenuMode()
        {
            NativeMethods.GUITHREADINFO gui;
            return (ProxyGetGUIThreadInfo(0, out gui) && (IsBitSet(gui.dwFlags, NativeMethods.GUI_INMENUMODE)));
        }

        internal static bool IsBitSet(int flags, int bit)
        {
            return (flags & bit) == bit;
        }

        // Check if window is really enabled, taking parent state into account.
        internal static bool IsEnabled(IntPtr hwnd)
        {
            // Navigate up parent chain. If any ancestor window is
            // not enabled, then that has the effect of disabling this window.
            // All ancestor windows must be enabled for this window to be enabled.
            for (; ; )
            {
                if (!SafeNativeMethods.IsWindowEnabled(hwnd))
                {
                    return false;
                }

                hwnd = NativeMethodsSetLastError.GetAncestor(hwnd, NativeMethods.GA_PARENT);
                if (hwnd == IntPtr.Zero)
                {
                    return true;
                }
            }
        }

        internal static bool IsControlRTL(IntPtr hwnd)
        {
            int exStyle = GetWindowExStyle(hwnd);
            return IsBitSet(exStyle, NativeMethods.WS_EX_LAYOUTRTL) || IsBitSet(exStyle, NativeMethods.WS_EX_RTLREADING);
        }

        internal static bool IsLayoutRTL(IntPtr hwnd)
        {
            return IsBitSet(GetWindowExStyle(hwnd), NativeMethods.WS_EX_LAYOUTRTL);
        }

        internal static bool IsReadingRTL(IntPtr hwnd)
        {
            return IsBitSet(GetWindowExStyle(hwnd), NativeMethods.WS_EX_RTLREADING);
        }

        internal static bool IntersectRect(ref NativeMethods.Win32Rect rcDest, ref NativeMethods.Win32Rect rc1, ref NativeMethods.Win32Rect rc2)
        {
            bool result = SafeNativeMethods.IntersectRect(ref rcDest, ref rc1, ref rc2);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        // Call IsCriticalException w/in a catch-all-exception handler to allow critical exceptions
        // to be thrown (this is copied from exception handling code in WinForms but feel free to
        // add new critical exceptions).  Usage:
        //      try
        //      {
        //          Somecode();
        //      }
        //      catch (Exception e)
        //      {
        //          if (Misc.IsCriticalException(e))
        //              throw;
        //          // ignore non-critical errors from external code
        //      }
        internal static bool IsCriticalException(Exception e)
        {
            return e is NullReferenceException || e is StackOverflowException || e is OutOfMemoryException || e is System.Threading.ThreadAbortException;
        }

        // this is to determine is an item is visible.  The assumption is that the items here are not hwnds
        // and that they are clipped by there parent.  For example this is called by the WindowsListBox.
        // In that case the hwnd is the list box and the itemRect would be a list item this code checks to see
        // if the item is scrolled out of view.
        static internal bool IsItemVisible(IntPtr hwnd, ref NativeMethods.Win32Rect itemRect)
        {
            NativeMethods.Win32Rect clientRect = new NativeMethods.Win32Rect(0, 0, 0, 0);
            if (!GetClientRectInScreenCoordinates(hwnd, ref clientRect))
                return false;

            NativeMethods.Win32Rect intersection = new NativeMethods.Win32Rect(0, 0, 0, 0);

            // Returns true if the passed in itemRect overlaps with the client area of the hwnd this API
            // does not modify clientRect or itemRect
            return IntersectRect(ref intersection, ref clientRect, ref itemRect);
        }

        static internal bool IsItemVisible(ref NativeMethods.Win32Rect parentRect, ref NativeMethods.Win32Rect itemRect)
        {
            NativeMethods.Win32Rect intersection = new NativeMethods.Win32Rect(0, 0, 0, 0);

            // Returns true if the passed in itemRect overlaps with the client area of the hwnd this API
            // does not modify clientRect or itemRect
            return IntersectRect(ref intersection, ref parentRect, ref itemRect);
        }

        static internal bool IsItemVisible(ref NativeMethods.Win32Rect parentRect, ref Rect itemRect)
        {
            NativeMethods.Win32Rect itemRc = new NativeMethods.Win32Rect(itemRect);
            NativeMethods.Win32Rect intersection = new NativeMethods.Win32Rect(0, 0, 0, 0);

            // Returns true if the passed in itemRect overlaps with the client area of the hwnd this API
            // does not modify clientRect or itemRect
            return IntersectRect(ref intersection, ref parentRect, ref itemRc);
        }

        static internal bool IsItemVisible(ref Rect parentRect, ref NativeMethods.Win32Rect itemRect)
        {
            NativeMethods.Win32Rect parentRc = new NativeMethods.Win32Rect(parentRect);
            NativeMethods.Win32Rect intersection = new NativeMethods.Win32Rect(0, 0, 0, 0);

            // Returns true if the passed in itemRect overlaps with the client area of the hwnd this API
            // does not modify clientRect or itemRect
            return IntersectRect(ref intersection, ref parentRc, ref itemRect);
        }

        static internal bool IsItemVisible(ref Rect parentRect, ref Rect itemRect)
        {
            NativeMethods.Win32Rect itemRc = new NativeMethods.Win32Rect(itemRect);
            NativeMethods.Win32Rect parentRc = new NativeMethods.Win32Rect(parentRect);
            NativeMethods.Win32Rect intersection = new NativeMethods.Win32Rect(0, 0, 0, 0);

            // Returns true if the passed in itemRect overlaps with the client area of the hwnd this API
            // does not modify clientRect or itemRect
            return IntersectRect(ref intersection, ref parentRc, ref itemRc);
        }

        internal static bool IsProgmanWindow(IntPtr hwnd)
        {
            while (hwnd != IntPtr.Zero)
            {
                if (GetClassName(hwnd).CompareTo("Progman") == 0)
                {
                    return true;
                }
                hwnd = NativeMethodsSetLastError.GetAncestor(hwnd, NativeMethods.GA_PARENT);
            }
            return false;
        }

        internal static bool IsWow64Process(MS.Internal.AutomationProxies.SafeProcessHandle hProcess, out bool Wow64Process)
        {
            bool result = UnsafeNativeMethods.IsWow64Process(hProcess, out Wow64Process);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        // wrapper for MapWindowPoints
        internal static bool MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, ref NativeMethods.Win32Rect rect, int cPoints)
        {
            int mappingOffset = NativeMethodsSetLastError.MapWindowPoints(hWndFrom, hWndTo, ref rect, cPoints);
            int lastWin32Error = Marshal.GetLastWin32Error();
            if (mappingOffset == 0)
            {
                // When mapping points to/from Progman and its children MapWindowPoints may fail with error code 1400
                // Invalid Window Handle.  Since Progman is the desktop no mapping is need.
                if ((IsProgmanWindow(hWndFrom) && hWndTo == IntPtr.Zero) ||
                    (hWndFrom == IntPtr.Zero && IsProgmanWindow(hWndTo)))
                {
                    lastWin32Error = 0;
                }

                ThrowWin32ExceptionsIfError(lastWin32Error);

                // If the coordinates is at the origin a zero return is valid.
                // Use GetLastError() to check that. Error code 0 is "Operation completed successfull".
                return lastWin32Error == 0;
            }

            return true;
        }

        // wrapper for MapWindowPoints
        internal static bool MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, ref NativeMethods.Win32Point pt, int cPoints)
        {
            int mappingOffset = NativeMethodsSetLastError.MapWindowPoints(hWndFrom, hWndTo, ref pt, cPoints);
            int lastWin32Error = Marshal.GetLastWin32Error();
            if (mappingOffset == 0)
            {
                // When mapping points to/from Progman and its children MapWindowPoints may fail with error code 1400
                // Invalid Window Handle.  Since Progman is the desktop no mapping is need.
                if ((IsProgmanWindow(hWndFrom) && hWndTo == IntPtr.Zero) ||
                    (hWndFrom == IntPtr.Zero && IsProgmanWindow(hWndTo)))
                {
                    lastWin32Error = 0;
                }

                ThrowWin32ExceptionsIfError(lastWin32Error);

                // If the coordinates is at the origin a zero return is valid.
                // Use GetLastError() to check that. Error code 0 is "Operation completed successfull".
                return lastWin32Error == 0;
            }

            return true;
        }

        // Move the mouse to the x, y location and perfoms a mouse clik
        // The mouse is then brough back to the original location.
        internal static void MouseClick(int x, int y)
        {
            MouseClick(x, y, false);
        }

        // Move the mouse to the x, y location and perfoms either
        // a single of double clik depending on the fDoubleClick parameter
        // The mouse is then brough back to the original location.
        internal static void MouseClick(int x, int y, bool fDoubleClick)
        {
            NativeMethods.Win32Point ptPrevious = new NativeMethods.Win32Point();
            bool fSetOldCursorPos = GetCursorPos(ref ptPrevious);
            bool mouseSwapped = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_SWAPBUTTON) != 0;

            Input.SendMouseInput(x, y, 0, SendMouseInputFlags.Move | SendMouseInputFlags.Absolute);

            Input.SendMouseInput(0, 0, 0, mouseSwapped ? SendMouseInputFlags.RightDown : SendMouseInputFlags.LeftDown);
            Input.SendMouseInput(0, 0, 0, mouseSwapped ? SendMouseInputFlags.RightUp : SendMouseInputFlags.LeftUp);

            if (fDoubleClick)
            {
                Input.SendMouseInput(0, 0, 0, mouseSwapped ? SendMouseInputFlags.RightDown : SendMouseInputFlags.LeftDown);
                Input.SendMouseInput(0, 0, 0, mouseSwapped ? SendMouseInputFlags.RightUp : SendMouseInputFlags.LeftUp);
            }

            // toolbar items don't have time to proccess the mouse click if we move it back too soon
            // so wait a small amount of time to give them a chance.  A value of 10 made this work
            // on a 2gig dual proc machine so 50 should cover a slower machine.
            System.Threading.Thread.Sleep(50);

            // Set back the mouse position where it was
            if (fSetOldCursorPos)
            {
                Input.SendMouseInput(ptPrevious.x, ptPrevious.y, 0, SendMouseInputFlags.Move | SendMouseInputFlags.Absolute);
            }
        }

        internal static int MsgWaitForMultipleObjects(SafeWaitHandle handle, bool waitAll, int milliseconds, int wakeMask)
        {
            int terminationEvent, lastWin32Error;
            if (handle == null)
            {
                terminationEvent = UnsafeNativeMethods.MsgWaitForMultipleObjects(0, null, waitAll, milliseconds, wakeMask);
                lastWin32Error = Marshal.GetLastWin32Error();
            }
            else
            {
                #pragma warning disable SYSLIB0004 // The Constrained Execution Region (CER) feature is not supported.  
                RuntimeHelpers.PrepareConstrainedRegions();
                #pragma warning restore SYSLIB0004 // The Constrained Execution Region (CER) feature is not supported.  
                bool fRelease = false;
                try
                {
                    handle.DangerousAddRef(ref fRelease);
                    IntPtr[] handles = { handle.DangerousGetHandle() };
                    terminationEvent = UnsafeNativeMethods.MsgWaitForMultipleObjects(1, handles, waitAll, milliseconds, wakeMask);
                    lastWin32Error = Marshal.GetLastWin32Error();
                }
                finally
                {
                    if (fRelease)
                    {
                        handle.DangerousRelease();
                    }
                }
            }
            if (terminationEvent == NativeMethods.WAIT_FAILED)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }
            return terminationEvent;
        }

        internal static IntPtr OpenProcess(int flags, bool inherit, uint processId, IntPtr hwnd)
        {
            IntPtr processHandle = UnsafeNativeMethods.OpenProcess(flags, inherit, processId);
            int lastWin32Error = Marshal.GetLastWin32Error();

            // If we fail due to permission issues, if we're on vista, try the hooking technique
            // to access the process instead.
            if (processHandle == IntPtr.Zero
             && lastWin32Error == 5/*ERROR_ACCESS_DENIED*/
             && System.Environment.OSVersion.Version.Major >= 6)
            {
                try
                {
                    processHandle = UnsafeNativeMethods.GetProcessHandleFromHwnd(hwnd);
                    lastWin32Error = Marshal.GetLastWin32Error();
                }
                catch(EntryPointNotFoundException)
                {
                    // Ignore; until OLEACC propogates into Vista builds, the entry point may not be present.
                }

            }

            if (processHandle == IntPtr.Zero)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return processHandle;
        }

        // wrapper for PostMessage
        internal static void PostMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            bool result = UnsafeNativeMethods.PostMessage(hwnd, msg, wParam, lParam);
            int lastWin32Error = Marshal.GetLastWin32Error();
            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }
        }

        // Returns the Win32 Class Name for an hwnd
        internal static string ProxyGetClassName(IntPtr hwnd)
        {
            const int OBJID_QUERYCLASSNAMEIDX = unchecked(unchecked((int)0xFFFFFFF4));
            const int QUERYCLASSNAME_BASE = 65536;

            // Call ProxySendMessage ignoring the timeout
            // are there known bad hwnd that do not work with WM_GETOBJECT? We should investigate more with hwnd for
            // which this call time outs
            int index = ProxySendMessageInt(hwnd, NativeMethods.WM_GETOBJECT, IntPtr.Zero, (IntPtr)OBJID_QUERYCLASSNAMEIDX, true);

            if (index >= QUERYCLASSNAME_BASE && index - QUERYCLASSNAME_BASE < _asClassNames.Length)
            {
                return _asClassNames[index - QUERYCLASSNAME_BASE];
            }
            else
            {
                return  RealGetWindowClass(hwnd);
            }
        }

        // wrapper for GetGuiThreadInfo
        internal static bool ProxyGetGUIThreadInfo(uint idThread, out NativeMethods.GUITHREADINFO gui)
        {
            gui = new NativeMethods.GUITHREADINFO();
            gui.cbSize = Marshal.SizeOf(gui.GetType());

            bool result = UnsafeNativeMethods.GetGUIThreadInfo(idThread, ref gui);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                // If the focused thread is on another [secure] desktop, GetGUIThreadInfo
                // will fail with ERROR_ACCESS_DENIED - don't throw an exception for that case,
                // instead treat as a failure. Callers will treat this as though no window has
                // focus.
                if (lastWin32Error == 5 /*ERROR_ACCESS_DENIED*/)
                    return false;

                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

#if _NEED_DEBUG_OUTPUT
            bool fCaretBlink = (gui.dwFlags & NativeMethods.GUI_CARETBLINKING) != 0;
            bool fMoveSize = (gui.dwFlags & NativeMethods.GUI_INMOVESIZE) != 0;
            bool fMenuMode = (gui.dwFlags & NativeMethods.GUI_INMENUMODE) != 0;
            bool fSystemMenuMode = (gui.dwFlags & NativeMethods.GUI_SYSTEMMENUMODE) != 0;
            bool fPopupMenuMode = (gui.dwFlags & NativeMethods.GUI_POPUPMENUMODE) != 0;

            StringBuilder sbFlag = new StringBuilder(NativeMethods.MAX_PATH);
            if (fCaretBlink)
            {
                sbFlag.Append("GUI_CARETBLINKING");
            }
            if (fMoveSize)
            {
                sbFlag.Append(sbFlag.Length > 0 ? " | GUI_INMOVESIZE" : "GUI_INMOVESIZE");
            }
            if (fMenuMode)
            {
                sbFlag.Append(sbFlag.Length > 0 ? " | GUI_INMENUMODE" : "GUI_INMENUMODE");
            }
            if (fSystemMenuMode)
            {
                sbFlag.Append(sbFlag.Length > 0 ? " | GUI_SYSTEMMENUMODE" : "GUI_SYSTEMMENUMODE");
            }
            if (fPopupMenuMode)
            {
                sbFlag.Append(sbFlag.Length > 0 ? " | GUI_POPUPMENUMODE" : "GUI_POPUPMENUMODE");
            }

            StringBuilder sb = new StringBuilder(NativeMethods.MAX_PATH);
            sb.Append("GUITHREADINFO \n\r{");
            sb.AppendFormat("\n\r\tcbSize = {0}", gui.cbSize);
            sb.AppendFormat("\n\r\tdwFlags = {0}", gui.dwFlags);
            if (sbFlag.Length > 0)
            {
                sb.Append(" (");
                sb.Append(sbFlag);
                sb.Append(")");
            }
            sb.AppendFormat("\n\r\thwndActive = 0x{0:x8}", gui.hwndActive.ToInt32());
            sb.AppendFormat("\n\r\thwndFocus = 0x{0:x8}", gui.hwndFocus.ToInt32());
            sb.AppendFormat("\n\r\thwndCapture = 0x{0:x8}", gui.hwndCapture.ToInt32());
            sb.AppendFormat("\n\r\thwndMenuOwner = 0x{0:x8}", gui.hwndMenuOwner.ToInt32());
            sb.AppendFormat("\n\r\thwndMoveSize = 0x{0:x8}", gui.hwndMoveSize.ToInt32());
            sb.AppendFormat("\n\r\thwndCaret = 0x{0:x8}", gui.hwndCaret.ToInt32());
            sb.AppendFormat("\n\r\trc = ({0}, {1}, {2}, {3})", gui.rc.left, gui.rc.top, gui.rc.right, gui.rc.bottom);
            sb.Append("\n\r}");

            System.Diagnostics.Debug.WriteLine(sb.ToString());
#endif

            return result;
        }

        // The name text based on the WM_GETTEXT message. The text is truncated to a predefined character
        // length.
        internal static string ProxyGetText(IntPtr hwnd)
        {
            return ProxyGetText(hwnd, MaxLengthNameProperty);
        }

        internal static string ProxyGetText(IntPtr hwnd, int length)
        {
            // if the length is zero don't bother asking for the text.
            if (length == 0)
            {
                return "";
            }

            // Length passes to SendMessage includes terminating NUL
            StringBuilder str = new StringBuilder(length + 1);

            // Send the message...
            ProxySendMessage(hwnd, NativeMethods.WM_GETTEXT, (IntPtr)str.Capacity, str);

            // We don't try to decifer between a zero length string and an error
            return str.ToString();
        }

        // wrapper for GetTitleBarInfo
        internal static bool ProxyGetTitleBarInfo(IntPtr hwnd, out UnsafeNativeMethods.TITLEBARINFO ti)
        {
            ti = new UnsafeNativeMethods.TITLEBARINFO();
            ti.cbSize = Marshal.SizeOf(ti.GetType());

            bool result = UnsafeNativeMethods.GetTitleBarInfo(hwnd, ref ti);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
                return false;
            }
            return true;
        }

        internal static bool ProxyGetTitleBarInfoEx(IntPtr hwnd, out UnsafeNativeMethods.TITLEBARINFOEX ti)
        {
            ti = new UnsafeNativeMethods.TITLEBARINFOEX();
            ti.cbSize = Marshal.SizeOf(ti.GetType());
            IntPtr result;
            IntPtr resultSendMessage = UnsafeNativeMethods.SendMessageTimeout(hwnd, NativeMethods.WM_GETTITLEBARINFOEX, IntPtr.Zero, ref ti, _sendMessageFlags, _sendMessageTimeoutValue, out result);
            int lastWin32Error = Marshal.GetLastWin32Error();
            if (resultSendMessage == IntPtr.Zero)
            {
                //Window owner failed to process the message WM_GETTITLEBARINFOEX
                EvaluateSendMessageTimeoutError(lastWin32Error);
            }
            return true;
        }

        // Return the bounding rects for titlebar items or null if they are invisible or offscreen
        internal static Rect [] GetTitlebarRects(IntPtr hwnd)
        {
            // Vista and beyond
            if (System.Environment.OSVersion.Version.Major >= 6)
            {
                return GetTitlebarRectsEx(hwnd);
            }

            // Up through XP
            return GetTitlebarRectsXP(hwnd);
        }

        internal static Rect GetTitleBarRect(IntPtr hwnd)
        {
            UnsafeNativeMethods.TITLEBARINFO ti;
            if (!Misc.ProxyGetTitleBarInfo(hwnd, out ti) || ti.rcTitleBar.IsEmpty)
            {
                return Rect.Empty;
            }

            NativeMethods.MENUBARINFO mbi;
            bool retValue = WindowsMenu.GetMenuBarInfo(hwnd, NativeMethods.OBJID_SYSMENU, 0, out mbi);

            int left = 0;
            int right = 0;
            if(Misc.IsControlRTL(hwnd))
            {
                // Possible that there is no menu
                left  = ti.rcTitleBar.left;
                right = (!retValue || mbi.rcBar.IsEmpty) ? ti.rcTitleBar.right : mbi.rcBar.right;
            }
            else
            {
                // Possible that there is no menu
                left  = (!retValue || mbi.rcBar.IsEmpty) ? ti.rcTitleBar.left : mbi.rcBar.left;
                right = ti.rcTitleBar.right;
            }
            return new Rect(left, ti.rcTitleBar.top, right - left, ti.rcTitleBar.bottom - ti.rcTitleBar.top);
        }

        internal static IntPtr ProxySendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr result;

            IntPtr resultSendMessage = UnsafeNativeMethods.SendMessageTimeout(hwnd, msg, wParam, lParam, _sendMessageFlags, _sendMessageTimeoutValue, out result);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (resultSendMessage == IntPtr.Zero)
            {
                EvaluateSendMessageTimeoutError(lastWin32Error);
            }

            return result;
        }

        // On a 64-bit platform, the value of the IntPtr is too large to represent as a 32-bit signed integer.
        // An int is a System.Int32.  When an explicit cast of IntPtr to int is done on a 64-bit platform an
        // OverflowException will occur when the IntPtr value exceeds the range of int. In cases where using
        // SendMessage to get back int (e.g. an item index or an enum value), this version safely truncates
        // from IntPtr to int.
        internal static int ProxySendMessageInt(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr result = ProxySendMessage(hwnd, msg, wParam, lParam);
            return unchecked((int)(long)result);
        }

        // Same as above but does not throw on timeout
        // This maybe a temp solution for quick unblock, be careful when using this method
        internal static IntPtr ProxySendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, bool ignoreTimeout)
        {
            IntPtr result;

            IntPtr resultSendMessage = UnsafeNativeMethods.SendMessageTimeout(hwnd, msg, wParam, lParam, _sendMessageFlags, _sendMessageTimeoutValue, out result);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (resultSendMessage == IntPtr.Zero)
            {
                EvaluateSendMessageTimeoutError(lastWin32Error, ignoreTimeout);
            }

            return result;
        }

        // On a 64-bit platform, the value of the IntPtr is too large to represent as a 32-bit signed integer.
        // An int is a System.Int32.  When an explicit cast of IntPtr to int is done on a 64-bit platform an
        // OverflowException will occur when the IntPtr value exceeds the range of int. In cases where using
        // SendMessage to get back int (e.g. an item index or an enum value), this version safely truncates
        // from IntPtr to int.
        internal static int ProxySendMessageInt(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, bool ignoreTimeout)
        {
            IntPtr result = ProxySendMessage(hwnd, msg, wParam, lParam, ignoreTimeout);
            return unchecked((int)(long)result);
        }

        internal static IntPtr ProxySendMessage(IntPtr hwnd, int msg, IntPtr wParam, StringBuilder sb)
        {
            IntPtr result;

            IntPtr resultSendMessage = UnsafeNativeMethods.SendMessageTimeout(hwnd, msg, wParam, sb, _sendMessageFlags, _sendMessageTimeoutValue, out result);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (resultSendMessage == IntPtr.Zero)
            {
                EvaluateSendMessageTimeoutError(lastWin32Error);
            }

            return result;
        }

        // On a 64-bit platform, the value of the IntPtr is too large to represent as a 32-bit signed integer.
        // An int is a System.Int32.  When an explicit cast of IntPtr to int is done on a 64-bit platform an
        // OverflowException will occur when the IntPtr value exceeds the range of int. In cases where using
        // SendMessage to get back int (e.g. an item index or an enum value), this version safely truncates
        // from IntPtr to int.
        internal static int ProxySendMessageInt(IntPtr hwnd, int msg, IntPtr wParam, StringBuilder sb)
        {
            IntPtr result = ProxySendMessage(hwnd, msg, wParam, sb);
            return unchecked((int)(long)result);
        }

        internal static IntPtr ProxySendMessage(IntPtr hwnd, int msg, IntPtr wParam, ref NativeMethods.Win32Rect lParam)
        {
            IntPtr result;

            IntPtr resultSendMessage = UnsafeNativeMethods.SendMessageTimeout(hwnd, msg, wParam, ref lParam, _sendMessageFlags, _sendMessageTimeoutValue, out result);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (resultSendMessage == IntPtr.Zero)
            {
                EvaluateSendMessageTimeoutError(lastWin32Error);
            }

            return result;
        }

        internal static IntPtr ProxySendMessage(IntPtr hwnd, int msg, out int wParam, out int lParam)
        {
            IntPtr result;

            IntPtr resultSendMessage = UnsafeNativeMethods.SendMessageTimeout(hwnd, msg, out wParam, out lParam, _sendMessageFlags, _sendMessageTimeoutValue, out result);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (resultSendMessage == IntPtr.Zero)
            {
                EvaluateSendMessageTimeoutError(lastWin32Error);
            }

            return result;
        }

        // Check if a point is within the bounding Rect of a window
        internal static bool PtInRect(ref NativeMethods.Win32Rect rc, int x, int y)
        {
            return x >= rc.left && x < rc.right && y >= rc.top && y < rc.bottom;
        }

        internal static bool PtInRect(ref Rect rc, int x, int y)
        {
            return x >= rc.Left && x < rc.Right && y >= rc.Top && y < rc.Bottom;
        }

        // Check if a point is within the client Rect of a window
        internal static bool PtInWindowRect(IntPtr hwnd, int x, int y)
        {
            NativeMethods.Win32Rect rc = new NativeMethods.Win32Rect();

            if (!GetWindowRect(hwnd, ref rc))
            {
                return false;
            }
            return x >= rc.left && x < rc.right && y >= rc.top && y < rc.bottom;
        }

        internal static bool ReadProcessMemory(MS.Internal.AutomationProxies.SafeProcessHandle hProcess, IntPtr source, IntPtr dest, IntPtr size, out IntPtr bytesRead)
        {
            bool result = UnsafeNativeMethods.ReadProcessMemory(hProcess, source, dest, size, out bytesRead);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool ReadProcessMemory(MS.Internal.AutomationProxies.SafeProcessHandle hProcess, IntPtr source, MS.Internal.AutomationProxies.SafeCoTaskMem destAddress, IntPtr size, out IntPtr bytesRead)
        {
            bool result = UnsafeNativeMethods.ReadProcessMemory(hProcess, source, destAddress, size, out bytesRead);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        // Get the class name
        internal static string RealGetWindowClass(IntPtr hwnd)
        {
            System.Text.StringBuilder className = new System.Text.StringBuilder(NativeMethods.MAX_PATH + 1);

            uint result = UnsafeNativeMethods.RealGetWindowClass(hwnd, className, NativeMethods.MAX_PATH);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (result == 0)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
                return "";
            }

            return className.ToString();
        }

        internal static bool RegisterHotKey(IntPtr hwnd, short atom, int modifiers, int vk)
        {
            bool result = UnsafeNativeMethods.RegisterHotKey(hwnd, atom, modifiers, vk);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static int ReleaseDC(IntPtr hwnd, IntPtr hdc)
        {
            // If ReleaseDC fails we will not do anything with that information so just ignore the
            // PRESHARP warnings.
#pragma warning suppress 6031, 6523
            return UnsafeNativeMethods.ReleaseDC(hwnd, hdc);
        }

        internal static int RegisterWindowMessage(string msg)
        {
            int result = SafeNativeMethods.RegisterWindowMessage(msg);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (result == 0)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static IntPtr SelectObject(IntPtr hdc, IntPtr hObject)
        {
            // There is no indication in the Windows SDK documentation that SelectObject()
            // will set an error to be retrieved with GetLastError, so set the pragma to ignore
            // the PRESHARP warning.  Anyway if ReleaseDC() fails here, nothing more can be done
            // since the code is restoring the orginal object and discarding the temp object.
#pragma warning suppress 6031, 6523
            return UnsafeNativeMethods.SelectObject(hdc, hObject);
        }

        internal static int SendInput(int inputs, ref NativeMethods.INPUT ki, int size)
        {
            int eventCount = UnsafeNativeMethods.SendInput(inputs, ref ki, size);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (eventCount <= 0)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return eventCount;
        }

        // The Win32 call to SetFocus does not do it for hwnd that are
        // not in the same process as the caller.
        // This is implemented here to work around this behavior.
        // Fake keystroke are sent to a specific hwnd to force
        // Windows to give the focus to that hwnd.
        static internal bool SetFocus(IntPtr hwnd)
        {
            // First Check for ComboLBox
            // Because it uses Keystrokes it dismisses the ComboLBox
            string className = RealGetWindowClass(hwnd);

            if (className == "ComboLBox")
                return true;


            // If window is currently Disabled or Invisible no need
            // to continue
            if (!SafeNativeMethods.IsWindowVisible(hwnd) || !SafeNativeMethods.IsWindowEnabled(hwnd))
            {
                return false;
            }

            // If already focused, leave as-is. Calling SetForegroundWindow
            // on an already focused HWND will remove focus!
            if (GetFocusedWindow().Equals(hwnd))
            {
                return true;
            }

            // Try calling SetForegroundWindow directly first; it should succeed if we
            // already have the focus or have UIAccess
            if (UnsafeNativeMethods.SetForegroundWindow(hwnd))
            {
                return true;
            }

            // Use the hotkey technique:
            // Register a hotkey and send it to ourselves - this gives us the
            // input, and allows us to call SetForegroundWindow.
            short atom = GlobalAddAtom("FocusHotKey");
            if (atom == 0)
            {
                return false;
            }
            short vk = 0xB9;
            bool gotHotkey = false;

            for (int tries = 0; tries < 10; tries++)
            {
                if (RegisterHotKey(IntPtr.Zero, atom, 0, vk))
                {
                    gotHotkey = true;
                    break;
                }

                vk++; // try another key
            }

            if (gotHotkey)
            {
                // Get state of modifiers - and temporarilly release them...
                bool fShiftDown = (UnsafeNativeMethods.GetAsyncKeyState(UnsafeNativeMethods.VK_SHIFT) & unchecked((int)0x80000000)) != 0;
                bool fAltDown = (UnsafeNativeMethods.GetAsyncKeyState(UnsafeNativeMethods.VK_MENU) & unchecked((int)0x80000000)) != 0;
                bool fCtrlDown = (UnsafeNativeMethods.GetAsyncKeyState(UnsafeNativeMethods.VK_CONTROL) & unchecked((int)0x80000000)) != 0;

                if (fShiftDown)
                    Input.SendKeyboardInputVK(UnsafeNativeMethods.VK_SHIFT, false);

                if (fAltDown)
                    Input.SendKeyboardInputVK(UnsafeNativeMethods.VK_MENU, false);

                if (fCtrlDown)
                    Input.SendKeyboardInputVK(UnsafeNativeMethods.VK_CONTROL, false);

                Input.SendKeyboardInputVK(vk, true);
                Input.SendKeyboardInputVK(vk, false);

                // Restore release modifier keys...
                if (fShiftDown)
                    Input.SendKeyboardInputVK(UnsafeNativeMethods.VK_SHIFT, true);

                if (fAltDown)
                    Input.SendKeyboardInputVK(UnsafeNativeMethods.VK_MENU, true);

                if (fCtrlDown)
                    Input.SendKeyboardInputVK(UnsafeNativeMethods.VK_CONTROL, true);

                // Spin in this message loop until we get the hot key
                while (true)
                {
                    // If the hotkey input gets lost (eg due to desktop switch), GetMessage may not return -
                    // so use MsgWait first so we can timeout if there's no message present instead of blocking.
                    int result = MsgWaitForMultipleObjects(null, false, 2000, NativeMethods.QS_ALLINPUT);
                    if (result == NativeMethods.WAIT_FAILED || result == NativeMethods.WAIT_TIMEOUT)
                        break;

                    NativeMethods.MSG msg = new NativeMethods.MSG();
                    if (!GetMessage(ref msg, IntPtr.Zero, 0, 0))
                        break;

                    // TranslateMessage() will not set an error to be retrieved with GetLastError,
                    // so set the pragma to ignore the PERSHARP warning.
                    // the PERSHARP warning.
#pragma warning suppress 6031, 6523
                    UnsafeNativeMethods.TranslateMessage(ref msg);

                    // From the Windows SDK documentation:
                    // The return value specifies the value returned by the window procedure.
                    // Although its meaning depends on the message being dispatched, the return
                    // value generally is ignored.
#pragma warning suppress 6031, 6523
                    UnsafeNativeMethods.DispatchMessage(ref msg);

                    if (msg.message == NativeMethods.WM_HOTKEY && (short)msg.wParam == atom)
                    {
                        break;
                    }
                }

                UnregisterHotKey(IntPtr.Zero, atom);
            }

            GlobalDeleteAtom(atom);

            return UnsafeNativeMethods.SetForegroundWindow(hwnd);
        }

        internal static int SetScrollPos(IntPtr hwnd, int bar, int pos, bool redraw)
        {
            // NOTE: From Windows SDK Documentaion:
            // If the function succeeds, the return value is the previous position of the scroll
            // box.  If the desktop is themed and the parent window is a message-only window,
            // the function returns an incorrect value.

            int prevPos = NativeMethodsSetLastError.SetScrollPos(hwnd, bar, pos, redraw);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (prevPos == 0)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return prevPos;
        }

        internal static IntPtr SetWinEventHook(int eventMin, int eventMax, IntPtr hmodWinEventProc, NativeMethods.WinEventProcDef WinEventReentrancyFilter, uint idProcess, uint idThread, int dwFlags)
        {
            // There is no indication in the Windows SDK documentation that SetWinEventHook()
            // will set an error to be retrieved with GetLastError, so set the pragma to ignore
            // the PERSHARP warning.
#pragma warning suppress 6523
            return UnsafeNativeMethods.SetWinEventHook(eventMin, eventMax, hmodWinEventProc, WinEventReentrancyFilter, idProcess, idThread, dwFlags);
        }

        // this strips the mnemonic prefix for short cuts as well as leading spaces
        // If we find && leave one & there.
        internal static string StripMnemonic(string s)
        {
            // If there are no spaces or & then it's ok just return it
            if (string.IsNullOrEmpty(s) || s.IndexOfAny(new char[2] { ' ', '&' }) < 0)
            {
                return s;
            }

            char[] ach = s.ToCharArray();
            bool amper = false;
            bool leadingSpace = false;
            int dest = 0;

            for (int source = 0; source < ach.Length; source++)
            {
                // get rid of leading spaces
                if (ach[source] == ' ' && leadingSpace == false)
                {
                    continue;
                }
                else
                {
                    leadingSpace = true;
                }

                // get rid of &
                if (ach[source] == '&' && amper == false)
                {
                    amper = true;
                }
                else
                {
                    ach[dest++] = ach[source];
                }
            }

            return new string(ach, 0, dest);
        }

        internal static void ThrowWin32ExceptionsIfError(int errorCode)
        {
            switch (errorCode)
            {
                case 0:     //    0 ERROR_SUCCESS                   The operation completed successfully.
                    // The error code indicates that there is no error, so do not throw an exception.
                    break;

                case 6:     //    6 ERROR_INVALID_HANDLE            The handle is invalid.
                case 1400:  // 1400 ERROR_INVALID_WINDOW_HANDLE     Invalid window handle.
                case 1401:  // 1401 ERROR_INVALID_MENU_HANDLE       Invalid menu handle.
                case 1402:  // 1402 ERROR_INVALID_CURSOR_HANDLE     Invalid cursor handle.
                case 1403:  // 1403 ERROR_INVALID_ACCEL_HANDLE      Invalid accelerator table handle.
                case 1404:  // 1404 ERROR_INVALID_HOOK_HANDLE       Invalid hook handle.
                case 1405:  // 1405 ERROR_INVALID_DWP_HANDLE        Invalid handle to a multiple-window position structure.
                case 1406:  // 1406 ERROR_TLW_WITH_WSCHILD          Cannot create a top-level child window.
                case 1407:  // 1407 ERROR_CANNOT_FIND_WND_CLASS     Cannot find window class.
                case 1408:  // 1408 ERROR_WINDOW_OF_OTHER_THREAD    Invalid window; it belongs to other thread.
                    throw new ElementNotAvailableException();

                // We're getting this in AMD64 when calling RealGetWindowClass; adding this code
                // to allow the DRTs to pass while we continue investigation.
                case 87:    //   87 ERROR_INVALID_PARAMETER
                    throw new ElementNotAvailableException();

                case 8:     //    8 ERROR_NOT_ENOUGH_MEMORY         Not enough storage is available to process this command.
                case 14:    //   14 ERROR_OUTOFMEMORY               Not enough storage is available to complete this operation.
                    throw new OutOfMemoryException();

                case 998:   //  998 ERROR_NOACCESS                  Invalid access to memory location.
                case 5:     //    5 ERROR_ACCESS_DENIED
                    throw new InvalidOperationException();

                case 121:   //  121 ERROR_SEM_TIMEOUT               The semaphore timeout period has expired.
                case 258:   //  258 WAIT_TIMEOUT                    The wait operation timed out.
                case 1053:  // 1053 ERROR_SERVICE_REQUEST_TIMEOUT   The service did not respond to the start or control request in a timely fashion.
                case 1460:  // 1460 ERROR_TIMEOUT                   This operation returned because the timeout period expired.
                    throw new TimeoutException();

                default:
                    // Not sure how to map the reset of the error codes so throw generic Win32Exception.
                    throw new Win32Exception(errorCode);
            }
        }

        internal static bool UnhookWinEvent(IntPtr winEventHook)
        {
            // There is no indication in the Windows SDK documentation that UnhookWinEvent()
            // will set an error to be retrieved with GetLastError, so set the pragma to ignore
            // the PERSHARP warning.
#pragma warning suppress 6523
            return UnsafeNativeMethods.UnhookWinEvent(winEventHook);
        }

        internal static bool UnionRect(out NativeMethods.Win32Rect rcDst, ref NativeMethods.Win32Rect rc1, ref NativeMethods.Win32Rect rc2)
        {
            bool result = SafeNativeMethods.UnionRect(out rcDst, ref rc1, ref rc2);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool UnregisterHotKey(IntPtr hwnd, short atom)
        {
            bool result = UnsafeNativeMethods.UnregisterHotKey(hwnd, atom);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static IntPtr VirtualAlloc(IntPtr address, UIntPtr size, int allocationType, int protect)
        {
            IntPtr result = UnsafeNativeMethods.VirtualAlloc(address, size, allocationType, protect);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (result == IntPtr.Zero)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static IntPtr VirtualAllocEx(MS.Internal.AutomationProxies.SafeProcessHandle hProcess, IntPtr address, UIntPtr size, int allocationType, int protect)
        {
            IntPtr result = UnsafeNativeMethods.VirtualAllocEx(hProcess, address, size, allocationType, protect);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (result == IntPtr.Zero)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool VirtualFree(IntPtr address, UIntPtr size, int freeType)
        {
            bool result = UnsafeNativeMethods.VirtualFree(address, size, freeType);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool VirtualFreeEx(MS.Internal.AutomationProxies.SafeProcessHandle hProcess, IntPtr address, UIntPtr size, int freeType)
        {
            bool result = UnsafeNativeMethods.VirtualFreeEx(hProcess, address, size, freeType);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool WriteProcessMemory(MS.Internal.AutomationProxies.SafeProcessHandle hProcess, IntPtr dest, IntPtr sourceAddress, IntPtr size, out IntPtr bytesWritten)
        {
            bool result = UnsafeNativeMethods.WriteProcessMemory(hProcess, dest, sourceAddress, size, out bytesWritten);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool IsWindowInGivenProcess(IntPtr hwnd, string targetprocess)
        {
            uint processId;
            //GetWindowThreadProcessId throws ElementNotAvailableException if the hwnd is no longer valid.
            //But, this exception should be handled by the client accessing this proxy.
            uint threadId = GetWindowThreadProcessId(hwnd, out processId);
            try
            {
                string processName = System.Diagnostics.Process.GetProcessById((int)processId).ProcessName;
                if (processName == targetprocess)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (IsCriticalException(ex))
                    throw;

                if (ex is ArgumentException || ex is SystemException)
                {
                    //The process is no longer running which implies AutomationElement is no longer available.
                    //Let the client handle it.
                    throw new ElementNotAvailableException();
                }

                //All other exceptions are ignored as the purpose of this method is to find whether the hwnd belongs to
                //a particular process or not.
            }
            return false;
        }

        internal static bool InTheShellProcess(IntPtr hwnd)
        {
            IntPtr hwndShell = SafeNativeMethods.GetShellWindow();
            if (hwndShell == IntPtr.Zero)
                return false;
            uint idProcessUs;
            GetWindowThreadProcessId(hwnd, out idProcessUs);
            uint idProcessShell;
            GetWindowThreadProcessId(hwndShell, out idProcessShell);
            return idProcessUs == idProcessShell;
        }

        // the windows listview has feature (like group by) in v6 that only exist on
        // vista and beyond.  This lets us test for that.
        internal static bool IsComctrlV6OnOsVerV6orHigher(IntPtr hwnd)
        {
            int commonControlVersion  = Misc.ProxySendMessageInt(hwnd, NativeMethods.CCM_GETVERSION, IntPtr.Zero, IntPtr.Zero);
            if (Environment.OSVersion.Version.Major >= 6 && commonControlVersion >= 6)
            {
                return true;
            }

            return false;
        }

        #endregion

        // ------------------------------------------------------
        //
        // Internal Fields
        //
        // ------------------------------------------------------

        #region Internal Fields

        // Max length for the name
        internal const int MaxLengthNameProperty = 2000;

        // Timeout for clearing menus
        internal const long MenuTimeOut = 100;

        #endregion

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------

        #region Private Methods

        private static bool EnumToolTipWindows(IntPtr hwnd, ref UnsafeNativeMethods.ENUMTOOLTIPWINDOWINFO lParam)
        {
            // Use ProxyGetClassName here instead of GetClassName(),
            // since for a winforms tooltip the latter will return
            // "WindowsForms10.tooltips_class32.app.0.b7ab7b".
            // Instead, ProxyGetClassName uses WM_GETOBJECT with
            // OBJID_QUERYCLASSNAMEIDX, which will return the correct answer.
            if (!ProxyGetClassName(hwnd).Equals("tooltips_class32"))
            {
                return true;
            }

            NativeMethods.TOOLINFO tool = new NativeMethods.TOOLINFO();
            tool.Init(Marshal.SizeOf(typeof(NativeMethods.TOOLINFO)));
            // For tooltips with ids of 0, MFC will create the tooltip with the flag of TTF_IDISHWND.
            // TTF_IDISHWND indicates that the uId member is the window handle to the tool.
            if (lParam.id == 0)
            {
                tool.hwnd = Misc.GetParent(lParam.hwnd);
                tool.uId = unchecked((int)lParam.hwnd);
                tool.uFlags = NativeMethods.TTF_IDISHWND;
            }
            else
            {
                tool.hwnd = lParam.hwnd;
                tool.uId = lParam.id;
            }

            string name = XSendMessage.GetItemText(hwnd, tool);

            // Didn't get anything - continue looking...
            if (string.IsNullOrEmpty(name))
            {
                return true;
            }

            lParam.name = name;

            // Got it - can stop iterating now.
            return false;
        }

        // Get the size of a page in memory for VirtualAlloc
        private static uint GetPageSize()
        {
            NativeMethods.SYSTEM_INFO sysInfo;
            UnsafeNativeMethods.GetSystemInfo(out sysInfo);
            return sysInfo.dwPageSize;
        }

        //This function throws corresponding exception depending on the last error.
        private static void EvaluateSendMessageTimeoutError(int error)
        {
            EvaluateSendMessageTimeoutError(error, false);
        }

        private static void EvaluateSendMessageTimeoutError(int error, bool ignoreTimeout)
        {
            // SendMessageTimeout Function
            // If the function fails or times out, the return value is zero. To get extended error information,
            // call GetLastError. If GetLastError returns zero, then the function timed out.
            // NOTE: The GetLastError after a SendMessageTimeout my also be an ERROR_TIMEOUT depending on the
            // message.

            // 1460 This operation returned because the timeout period expired. ERROR_TIMEOUT
            if (error == 0 || error == 1460)
            {
                if (!ignoreTimeout)
                {
                    throw new TimeoutException();
                }
            }
            else
            {
                ThrowWin32ExceptionsIfError(error);
            }
        }

        private static Rect[] GetTitlebarRectsXP(IntPtr hwnd)
        {
            Debug.Assert(System.Environment.OSVersion.Version.Major < 6);

            UnsafeNativeMethods.TITLEBARINFO tiDL;
            if (!Misc.ProxyGetTitleBarInfo(hwnd, out tiDL))
            {
                return null;
            }

            // Titlebars that are invisible or may not exist should not have a rect
            if ((tiDL.rgstate[NativeMethods.INDEX_TITLEBAR_SELF] & (NativeMethods.STATE_SYSTEM_INVISIBLE | NativeMethods.STATE_SYSTEM_OFFSCREEN)) != 0)
            {
                return null;
            }

            // We really should be using the the theme APIs but they give incorrect results with the titlebar buttons so we are reverting back to system metrics
            // This is not perfect, its a looks like it may be a few pixels of here or there but clicking on the center of the button will be sucessfull.  The only place this
            // does not work is on console windows when the theme is Windows Clasic Style.

            // The system metric seems to be just bit off, subtracting 1 seems to get a better result on all themes
            int buttonWidth = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CXSIZE) - 1;
            int buttonHeight = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CYSIZE) - 1;

            Rect[] rects = new Rect[NativeMethods.CCHILDREN_TITLEBAR + 1];

            // Start at the end and work backwards to the system menu.  Subtract buttonSize when a child is present.
            int leftEdge;

            if (Misc.IsLayoutRTL(hwnd))
            {
                // Right to left mirroring style

                // This is to take in count for a bug in GetTitleBarInfo().  It does not calculate the
                // rcTitleBar correctly when the WS_EX_LAYOUTRTL extended style is set.  It assumes
                // SYSMENU is always on the left and removes its space from the wrong side of rcTitleBar.
                // Use the bounding rectangle of the whole title bar to get the true left boundary.
                leftEdge = (int)(Misc.GetTitleBarRect(hwnd).Left);
                for (int i = NativeMethods.INDEX_TITLEBAR_MAC; i > NativeMethods.INDEX_TITLEBAR_SELF; i--)
                {
                    if ((tiDL.rgstate[i] & NativeMethods.STATE_SYSTEM_INVISIBLE) == 0)
                    {
                        rects[i] = new Rect(leftEdge, tiDL.rcTitleBar.top, buttonWidth, buttonHeight);
                        leftEdge += buttonWidth;
                    }
                    else
                    {
                        rects[i] = Rect.Empty;
                    }
                }
            }
            else
            {
                leftEdge = tiDL.rcTitleBar.right - buttonWidth;
                for (int i = NativeMethods.INDEX_TITLEBAR_MAC; i > NativeMethods.INDEX_TITLEBAR_SELF; i--)
                {
                    if ((tiDL.rgstate[i] & NativeMethods.STATE_SYSTEM_INVISIBLE) == 0)
                    {
                        rects[i] = new Rect(leftEdge, tiDL.rcTitleBar.top, buttonWidth, buttonHeight);
                        leftEdge -= buttonWidth;
                    }
                    else
                    {
                        rects[i] = Rect.Empty;
                    }
                }
            }

            return rects;
        }

        private static Rect[] GetTitlebarRectsEx(IntPtr hwnd)
        {
            Debug.Assert(System.Environment.OSVersion.Version.Major >= 6);

            UnsafeNativeMethods.TITLEBARINFOEX ti;
            if (!Misc.ProxyGetTitleBarInfoEx(hwnd, out ti))
            {
                return null;
            }

            // Titlebars that are invisible or may not exist should not have a rect
            if ((ti.rgstate[NativeMethods.INDEX_TITLEBAR_SELF] & (NativeMethods.STATE_SYSTEM_INVISIBLE | NativeMethods.STATE_SYSTEM_OFFSCREEN)) != 0)
            {
                return null;
            }

            Rect[] rects = new Rect[NativeMethods.CCHILDREN_TITLEBAR + 1];
            for (int i = 0; i <= NativeMethods.CCHILDREN_TITLEBAR; i++)
            {
                // Buttons that are invisible or may not exist should not have a rect
                if ((ti.rgstate[i] & NativeMethods.STATE_SYSTEM_INVISIBLE) != 0)
                {
                    rects[i] = Rect.Empty;
                }
                else
                {
                    rects[i] = new Rect(ti.rgrect[i].left, ti.rgrect[i].top, ti.rgrect[i].right - ti.rgrect[i].left, ti.rgrect[i].bottom - ti.rgrect[i].top);
                }
            }

            return rects;
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------

        #region Private Fields

        // Generic flags for SendMessages
        private const int _sendMessageFlags = NativeMethods.SMTO_BLOCK;

        // Generic time out for SendMessages
        // Most messages won't need anything near this - but there are a few - eg. WM_COMMAND/CBN_DROPDOWN
        // when sent to IE's address combo for the first time causes it to populate iself, and that can
        // take a couple of seconds on a slow machine.
        private const int _sendMessageTimeoutValue = 10000;

        // Array of known class names
        private static string[] _asClassNames = {
            "ListBox",
            "#32768",
            "Button",
            "Static",
            "Edit",
            "ComboBox",
            "#32770",
            "#32771",
            "MDIClient",
            "#32769",
            "ScrollBar",
            "msctls_statusbar32",
            "ToolbarWindow32",
            "msctls_progress32",
            "SysAnimate32",
            "SysTabControl32",
            "msctls_hotkey32",
            "SysHeader32",
            "msctls_trackbar32",
            "SysListView32",
            "OpenListView",
            "msctls_updown",
            "msctls_updown32",
            "tooltips_class",
            "tooltips_class32",
            "SysTreeView32",
            "SysMonthCal32",
            "SysDateTimePick32",
            "RICHEDIT",
            "RichEdit20A",
            "RichEdit20W",
            "SysIPAddress32"
        };

        #endregion

    }
}
