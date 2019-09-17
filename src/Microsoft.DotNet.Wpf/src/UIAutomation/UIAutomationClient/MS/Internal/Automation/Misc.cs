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
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using NativeMethodsSetLastError = MS.Internal.UIAutomationClient.NativeMethodsSetLastError;

namespace MS.Internal.Automation
{
    // Helper class that contains methods/utilities that don't really belong anywhere else,
    // or don't justify a class of their own.
    internal static class Misc
    {
        // Static class, so no ctor

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        #region Element Comparisons

        // compare two arrays
        internal static bool Compare(int[] a1, int[] a2)
        {
            CheckNonNull(a1, a2);

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

        // compare two AutomationElements
        internal static bool Compare(AutomationElement el1, AutomationElement el2)
        {
            CheckNonNull(el1, el2);
            return Compare(el1.GetRuntimeId(), el2.GetRuntimeId());
        }
        #endregion Element Comparisons

        #region Array combination

        // Concantenate arrays from a collection of arrays
        internal static Array CombineArrays(IEnumerable arrays, Type t)
        {
            int totalLength = 0;

            foreach (Array a in arrays)
            {
                totalLength += a.Length;
            }

            Array combined = Array.CreateInstance(t, totalLength);
            int pos = 0;

            foreach (Array a in arrays)
            {
                int l = a.Length;

                Array.Copy(a, 0, combined, pos, l);
                pos += l;
            }

            return combined;
        }

        // return an array with duplicate elements removed. Has side-effect of sorting array;
        // therefore elements must be sortable (IComparable)
        internal static Array RemoveDuplicates(Array a, Type t)
        {
            if (a.Length == 0)
                return a;

            Array.Sort(a);

            // Remove duplicate elements by selective copy-to-self...
            int dest = 0;

            for (int src = 1; src < a.Length; src++)
            {
                if (!a.GetValue(src).Equals(a.GetValue(dest)))
                {
                    dest++;
                    a.SetValue(a.GetValue(src), dest);
                }
            }

            // dest was the index of the last item, now use it as the length by adding one.
            int newLength = dest + 1;

            if (newLength == a.Length)
            {
                // No duplicates found - just return as-is
                return a;
            }
            else
            {
                // Return shorter array
                Array a2 = Array.CreateInstance(t, newLength);

                Array.Copy(a, 0, a2, 0, newLength);
                return a2;
            }
        }
        #endregion Array combination

        #region Interface & Property Wrapping

        // Wrap pattern interface on client side, before handing to client - ie. create a client-side pattern wrapper
        internal static object WrapInterfaceOnClientSide(AutomationElement el, SafePatternHandle hPattern, AutomationPattern pattern)
        {
            if (hPattern.IsInvalid)
                return null;

            AutomationPatternInfo pi;

            if (!Schema.GetPatternInfo(pattern, out pi))
            {
                throw new ArgumentException(SR.Get(SRID.UnsupportedPattern));
            }

            if (pi.ClientSideWrapper == null)
            {
                Debug.Assert(false, "missing client-side pattern wrapper");
                return null;
            }
            else
            {
                // false -> not cached. (Cached goes via AutomationElement.GetCachedPattern, not here)
                return pi.ClientSideWrapper(el, hPattern, false);
            }
        }

        #endregion Interface & Property Wrapping

        #region Param validation & Error related

        // Check that specified argument is non-null, if so, throw exception
        internal static void ValidateArgumentNonNull(object obj, string argName)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(argName);
            }
        }

        // Throw an argument Exception with a generic error
        internal static void ThrowInvalidArgument(string argName)
        {
            throw new ArgumentException(SR.Get(SRID.GenericInvalidArgument, argName));
        }

        // Check that specified condition is true; if not, throw exception
        internal static void ValidateArgument(bool cond, string reason)
        {
            if (!cond)
            {
                throw new ArgumentException(SR.Get(reason));
            }
        }

        // Check that specified condition is true; if not, throw exception
        internal static void ValidateArgumentInRange(bool cond, string argName)
        {
            if (!cond)
            {
                throw new ArgumentOutOfRangeException(argName);
            }
        }

        // Called by the patterns before accessing .Cache
        internal static void ValidateCached(bool cached)
        {
            if (!cached)
            {
                throw new InvalidOperationException(SR.Get(SRID.CacheRequestNeedCache));
            }
        }

        // Called by the patterns before accessing .Current
        internal static void ValidateCurrent(SafePatternHandle hPattern)
        {
            if (hPattern.IsInvalid)
            {
                throw new InvalidOperationException(SR.Get(SRID.CacheRequestNeedLiveForProperties));
            }
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
        internal static bool IsCriticalException( Exception e )
        {
            return e is NullReferenceException || e is StackOverflowException || e is OutOfMemoryException || e is System.Threading.ThreadAbortException;
        }

        internal static bool IsWindowsFormsControl(string className)
        {
            return className.IndexOf("windowsforms", StringComparison.OrdinalIgnoreCase) > -1;
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
                    throw new InvalidOperationException();

                default:
                    // Not sure how to map the reset of the error codes so throw generic Win32Exception.
                    throw new Win32Exception(errorCode);
            }
        }

        // We have this wrapper because casting IntPtr to int may
        // generate OverflowException when one of high 32 bits is set.
        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        #endregion Param validation & Error related

        #region Misc

        internal static IntPtr CreateRectRgn(int left, int top, int right, int bottom)
        {
            IntPtr result = SafeNativeMethods.CreateRectRgn(left, top, right, bottom);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (result == IntPtr.Zero)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool CloseHandle(IntPtr handle)
        {
            bool result = UnsafeNativeMethods.CloseHandle(handle);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool DeleteObject(IntPtr hrgn)
        {
            bool result = UnsafeNativeMethods.DeleteObject(hrgn);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool GetClientRect(NativeMethods.HWND hwnd, out NativeMethods.RECT rc)
        {
            bool result = SafeNativeMethods.GetClientRect(hwnd, out rc);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool GetGUIThreadInfo(int idThread, ref SafeNativeMethods.GUITHREADINFO guiThreadInfo)
        {
            guiThreadInfo.cbSize = Marshal.SizeOf(guiThreadInfo);

            bool result = SafeNativeMethods.GetGUIThreadInfo(0, ref guiThreadInfo);
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

            return result;
        }

        internal static bool GetMenuBarInfo(NativeMethods.HWND hwnd, int idObject, uint item, ref UnsafeNativeMethods.MENUBARINFO mbi)
        {
            bool result = NativeMethodsSetLastError.GetMenuBarInfo(hwnd, idObject, item, ref mbi);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static int GetModuleFileNameEx(MS.Internal.Automation.SafeProcessHandle hProcess, IntPtr hModule, StringBuilder buffer, int length)
        {
            int result = SafeNativeMethods.GetModuleFileNameEx(hProcess, hModule, buffer, length);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (result == 0)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static int GetMessage(ref UnsafeNativeMethods.MSG msg, NativeMethods.HWND hwnd, int nMsgFilterMin, int nMsgFilterMax)
        {
            int result = UnsafeNativeMethods.GetMessage(ref msg, hwnd, nMsgFilterMin, nMsgFilterMax);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (result == -1)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static NativeMethods.HWND GetWindow(NativeMethods.HWND hwnd, int uCmd)
        {
            NativeMethods.HWND result = NativeMethodsSetLastError.GetWindow(hwnd, uCmd);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (result == NativeMethods.HWND.NULL)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static int GetWindowLong(NativeMethods.HWND hWnd, int nIndex)
        {
            int iResult = 0;
            IntPtr result = IntPtr.Zero;
            int error = 0;

            if (IntPtr.Size == 4)
            {
                // use GetWindowLong
                iResult = NativeMethodsSetLastError.GetWindowLong(hWnd, nIndex);
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(iResult);
            }
            else
            {
                // use GetWindowLongPtr
                result = NativeMethodsSetLastError.GetWindowLongPtr(hWnd, nIndex);
                error = Marshal.GetLastWin32Error();
                iResult = IntPtrToInt32(result);
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                ThrowWin32ExceptionsIfError(error);
            }

            return iResult;
        }

        internal static bool GetWindowPlacement(NativeMethods.HWND hwnd, ref UnsafeNativeMethods.WINDOWPLACEMENT wp)
        {
            bool result = UnsafeNativeMethods.GetWindowPlacement(hwnd, ref wp);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool GetWindowRect(NativeMethods.HWND hwnd, out NativeMethods.RECT rc)
        {
            bool result = SafeNativeMethods.GetWindowRect(hwnd, out rc);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static short GlobalAddAtom(string lpString)
        {
            short result = SafeNativeMethods.GlobalAddAtom(lpString);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (result == 0)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static short GlobalDeleteAtom(short atom)
        {
            short result = NativeMethodsSetLastError.GlobalDeleteAtom(atom);
            ThrowWin32ExceptionsIfError(Marshal.GetLastWin32Error());
            return result;
        }

        internal static int TryMsgWaitForMultipleObjects(SafeWaitHandle handle, bool waitAll, int milliseconds, int wakeMask, ref int lastWin32Error)
        {
            int terminationEvent;
            if (handle == null)
            {
                terminationEvent = UnsafeNativeMethods.MsgWaitForMultipleObjects(0, null, waitAll, milliseconds, wakeMask);
                lastWin32Error = Marshal.GetLastWin32Error();
            }
            else
            {
                RuntimeHelpers.PrepareConstrainedRegions();
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
            return terminationEvent;
        }

        internal static int MsgWaitForMultipleObjects(SafeWaitHandle handle, bool waitAll, int milliseconds, int wakeMask)
        {
            int lastWin32Error = 0;
            int terminationEvent = TryMsgWaitForMultipleObjects(handle, waitAll, milliseconds, wakeMask, ref lastWin32Error);
            if (terminationEvent == UnsafeNativeMethods.WAIT_FAILED)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }
            return terminationEvent;
        }

        internal static IntPtr OpenProcess(int dwDesiredAccess, bool fInherit, int dwProcessId, NativeMethods.HWND hwnd)
        {
            IntPtr processHandle = UnsafeNativeMethods.OpenProcess(dwDesiredAccess, fInherit, dwProcessId);
            int lastWin32Error = Marshal.GetLastWin32Error();

            // If we fail due to permission issues, if we're on vista, try the hooking technique
            // to access the process instead.
            if (processHandle == IntPtr.Zero
             && lastWin32Error == 5/*ERROR_ACCESS_DENIED*/
             && System.Environment.OSVersion.Version.Major >= 6)
            {
                try
                {
                    processHandle = UnsafeNativeMethods.GetProcessHandleFromHwnd(hwnd.h);
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

        internal static bool PostMessage(NativeMethods.HWND hWnd, int nMsg, IntPtr wParam, IntPtr lParam)
        {
            bool result = UnsafeNativeMethods.PostMessage(hWnd, nMsg, wParam, lParam);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool RegisterHotKey(NativeMethods.HWND hWnd, int id, int fsModifiers, int vk)
        {
            bool result = UnsafeNativeMethods.RegisterHotKey(hWnd, id, fsModifiers, vk);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static int SendInput(int nInputs, ref UnsafeNativeMethods.INPUT mi, int cbSize)
        {
            int result = UnsafeNativeMethods.SendInput(nInputs, ref mi, cbSize);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (result == 0)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static IntPtr SendMessageTimeout(NativeMethods.HWND hwnd, int Msg, IntPtr wParam, IntPtr lParam)
        {
            // Use a timeout of 10 seconds.
            // Don't use the SMTO_ABORTIFHUNG flag - windows if often too quick to think that an app is hung - eg only a couple of
            // seconds or less - many apps recover from this.
            IntPtr lresult;
            IntPtr smtoRetVal = UnsafeNativeMethods.SendMessageTimeout(hwnd, Msg, wParam, lParam, 0, 10000, out lresult);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (smtoRetVal == IntPtr.Zero)
            {
                EvaluateSendMessageTimeoutError(lastWin32Error);
            }

            return lresult;
        }

        internal static IntPtr SendMessageTimeout(NativeMethods.HWND hwnd, int Msg, IntPtr wParam, ref UnsafeNativeMethods.MINMAXINFO lParam)
        {
            // Use a timeout of 10 seconds.
            // Don't use the SMTO_ABORTIFHUNG flag - windows if often too quick to think that an app is hung - eg only a couple of
            // seconds or less - many apps recover from this.
            IntPtr lresult;
            IntPtr smtoRetVal = UnsafeNativeMethods.SendMessageTimeout(hwnd, Msg, wParam, ref lParam, 0, 10000, out lresult);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (smtoRetVal == IntPtr.Zero)
            {
                EvaluateSendMessageTimeoutError(lastWin32Error);
            }

            return lresult;
        }

        internal static IntPtr SendMessageTimeout(NativeMethods.HWND hwnd, int Msg, IntPtr wParam, StringBuilder lParam)
        {
            // Use a timeout of 10 seconds.
            // Don't use the SMTO_ABORTIFHUNG flag - windows if often too quick to think that an app is hung - eg only a couple of
            // seconds or less - many apps recover from this.
            IntPtr lresult;
            IntPtr smtoRetVal = UnsafeNativeMethods.SendMessageTimeout(hwnd, Msg, wParam, lParam, 0, 10000, out lresult);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (smtoRetVal == IntPtr.Zero)
            {
                EvaluateSendMessageTimeoutError(lastWin32Error);
            }

            return lresult;
        }


        internal static bool SetWindowPlacement(NativeMethods.HWND hwnd, ref UnsafeNativeMethods.WINDOWPLACEMENT wp)
        {
            bool result = UnsafeNativeMethods.SetWindowPlacement(hwnd, ref wp);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool SetWindowPos(NativeMethods.HWND hWnd, NativeMethods.HWND hWndInsertAfter, int x, int y, int cx, int cy, int flags)
        {
            bool result = UnsafeNativeMethods.SetWindowPos(hWnd, hWndInsertAfter, x, y, cx, cy, flags);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
        }

        internal static bool UnregisterHotKey(NativeMethods.HWND hWnd, int id)
        {
            bool result = UnsafeNativeMethods.UnregisterHotKey(hWnd, id);
            int lastWin32Error = Marshal.GetLastWin32Error();

            if (!result)
            {
                ThrowWin32ExceptionsIfError(lastWin32Error);
            }

            return result;
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


        #endregion Misc

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Helper used by the various comparison functions above
        private static void CheckNonNull(object el1, object el2)
        {
            if (el1 == null)
                throw new ArgumentNullException("el1");

            if (el2 == null)
                throw new ArgumentNullException("el2");
        }

        //This function throws corresponding exception depending on the last error.
        private static void EvaluateSendMessageTimeoutError(int error)
        {
            // SendMessageTimeout Function
            // If the function fails or times out, the return value is zero. To get extended error information,
            // call GetLastError. If GetLastError returns zero, then the function timed out.
            // NOTE: The GetLastError after a SendMessageTimeout my also be an ERROR_TIMEOUT depending on the
            // message.

            // 1460 This operation returned because the timeout period expired. ERROR_TIMEOUT
            if (error == 0 || error == 1460)
            {
                throw new TimeoutException();
            }
            else
            {
                ThrowWin32ExceptionsIfError(error);
            }
        }

        #endregion Private Methods
    }
}
