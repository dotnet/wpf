// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using MS.Win32;
using Windows.Win32.Foundation;

namespace MS.Internal.Controls
{
    #region class EnumUnknown

    /// <summary>
    ///  Helper object implementing IEnumUnknown for enumerating controls.
    ///  Source copied from AxContainer.cs
    /// </summary>
    internal class EnumUnknown : UnsafeNativeMethods.IEnumUnknown
    {
        private Object[] arr;
        private int loc;
        private int size;

        internal EnumUnknown(Object[] arr)
        {
            this.arr = arr;
            loc = 0;
            size = (arr == null) ? 0 : arr.Length;
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
                return HRESULT.E_INVALIDARG;
            }

            int fetched = 0;
            if (loc >= size)
            {
                fetched = 0;
            }
            else
            {
                for (; loc < size && fetched < celt; ++(loc))
                {
                    if (arr[loc] != null)
                    {
                        Marshal.WriteIntPtr(rgelt, Marshal.GetIUnknownForObject(arr[loc]));
                        rgelt = (IntPtr)((long)rgelt + (long)sizeof(IntPtr));
                        ++fetched;
                    }
                }
            }

            if (pceltFetched != IntPtr.Zero)
                Marshal.WriteInt32(pceltFetched, 0, fetched);

            if (fetched != celt)
            {
                return (HRESULT.S_FALSE);
            }
            return HRESULT.S_OK;
        }

        int UnsafeNativeMethods.IEnumUnknown.Skip(int celt)
        {
            loc += celt;
            if (loc >= size)
            {
                return (HRESULT.S_FALSE);
            }
            return HRESULT.S_OK;
        }

        void UnsafeNativeMethods.IEnumUnknown.Reset()
        {
            loc = 0;
        }

        void UnsafeNativeMethods.IEnumUnknown.Clone(out UnsafeNativeMethods.IEnumUnknown ppenum)
        {
            ppenum = new EnumUnknown(arr, loc);
        }
    }
    #endregion class EnumUnknown
}
