// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Helper object implementing IEnumUnknown for enumerating controls            
//
//              Source copied from AxContainer.cs
//

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Security;

using MS.Win32;

namespace MS.Internal.Controls
{
    #region class EnumUnknown

    internal class EnumUnknown : UnsafeNativeMethods.IEnumUnknown
    {
        private Object[] arr;
        private int loc;
        private int size;

        internal EnumUnknown(Object[] arr)
        {
            this.arr = arr;
            this.loc = 0;
            this.size = (arr == null) ? 0 : arr.Length;
        }

        private EnumUnknown(Object[] arr, int loc)
            : this(arr)
        {
            this.loc = loc;
        }

        unsafe int UnsafeNativeMethods.IEnumUnknown.Next(int celt, IntPtr rgelt, IntPtr pceltFetched)
        {
            if (pceltFetched != IntPtr.Zero)
                Marshal.WriteInt32(pceltFetched, 0, 0);

            if (celt < 0)
            {
                return NativeMethods.E_INVALIDARG;
            }

            int fetched = 0;
            if (this.loc >= this.size)
            {
                fetched = 0;
            }
            else
            {
                for (; this.loc < this.size && fetched < celt; ++(this.loc))
                {
                    if (this.arr[this.loc] != null)
                    {
                        Marshal.WriteIntPtr(rgelt, Marshal.GetIUnknownForObject(this.arr[this.loc]));
                        rgelt = (IntPtr)((long)rgelt + (long)sizeof(IntPtr));
                        ++fetched;
                    }
                }
            }

            if (pceltFetched != IntPtr.Zero)
                Marshal.WriteInt32(pceltFetched, 0, fetched);

            if (fetched != celt)
            {
                return (NativeMethods.S_FALSE);
            }
            return NativeMethods.S_OK;
        }

        int UnsafeNativeMethods.IEnumUnknown.Skip(int celt)
        {
            this.loc += celt;
            if (this.loc >= this.size)
            {
                return (NativeMethods.S_FALSE);
            }
            return NativeMethods.S_OK;
        }

        void UnsafeNativeMethods.IEnumUnknown.Reset()
        {
            this.loc = 0;
        }

        void UnsafeNativeMethods.IEnumUnknown.Clone(out UnsafeNativeMethods.IEnumUnknown ppenum)
        {
            ppenum = new EnumUnknown(this.arr, this.loc);
        }
    }
    #endregion class EnumUnknown
}
