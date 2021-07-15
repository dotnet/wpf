// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Wraps some of IAccessible to support Focus and top-level window creates

using System.Windows.Automation;
using System;
using Accessibility;
using System.Text;
using System.Diagnostics;
using MS.Win32;

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

namespace MS.Internal.Automation
{
    internal class Accessible
    {
        internal Accessible(IAccessible acc, object child)
        {
            Debug.Assert(acc != null, "null IAccessible");
            _acc = acc;
            _child = child;
        }
        
        // Create an Accessible object; may return null
        internal static Accessible Create( IntPtr hwnd, int idObject, int idChild )
        {
            IAccessible acc = null;
            object child = null;
            if( UnsafeNativeMethods.AccessibleObjectFromEvent( hwnd, idObject, idChild, ref acc, ref child ) != 0 /*S_OK*/ || acc == null )
                return null;

            // Per SDK must use the ppacc and pvarChild from AccessibleObjectFromEvent
            // to access information about this UI element.
            return new Accessible( acc, child );
        }

        internal int State
        { 
            get 
            { 
                try
                {
                    return (int)_acc.get_accState(_child); 
                }
                catch (Exception e)
                {
                    if (IsCriticalMSAAException(e))
                        // PRESHARP will flag this as a warning 56503/6503: Property get methods should not throw exceptions
                        // Since this Property is internal, we CAN throw an exception
#pragma warning suppress 6503
                        throw;

                    return UnsafeNativeMethods.STATE_SYSTEM_UNAVAILABLE;
                }
            } 
        }

        internal IntPtr Window
        {
            get
            {
                if (_hwnd == IntPtr.Zero)
                {
                    try
                    {
                        if (UnsafeNativeMethods.WindowFromAccessibleObject(_acc, ref _hwnd) != 0/*S_OK*/)
                        {
                            _hwnd = IntPtr.Zero;
                        }
                    }
                    catch( Exception e )
                    {
                        if (IsCriticalMSAAException(e))
                            // PRESHARP will flag this as a warning 56503/6503: Property get methods should not throw exceptions
                            // Since this Property is internal, we CAN throw an exception
#pragma warning suppress 6503
                            throw;

                        _hwnd = IntPtr.Zero;
                    }
                }
                return _hwnd;
            }
        }

        // miscellaneous functions used with Accessible

        internal static bool CompareClass(IntPtr hwnd, string szClass)
        {
            return ProxyManager.GetClassName(NativeMethods.HWND.Cast(hwnd)) == szClass;
        }

        internal static bool IsComboDropdown(IntPtr hwnd)
        {
            return CompareClass(hwnd, "ComboLBox");
        }

        internal static bool IsStatic(IntPtr hwnd)
        {
            return CompareClass(hwnd, "Static");
        }

        private bool IsCriticalMSAAException(Exception e)
        {
            // Some OLEACC proxies produce out-of-memory for non-critical reasons:
            // notably, the treeview proxy will raise this if the target HWND no longer exists,
            // GetWindowThreadProcessID fails and it therefore won't be able to allocate shared
            // memory in the target process, so it incorrectly assumes OOM.
            // Some Native impls (media player) return E_POINTER, which COM Interop translates
            // into NullRefException; need to ignore those too.
            // should ignore those
            return !(e is OutOfMemoryException)
                && !(e is NullReferenceException)
                && Misc.IsCriticalException(e);
        }
        private IntPtr _hwnd;
        private IAccessible _acc;
        private Object _child;
    }
}
