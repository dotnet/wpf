// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WinRT.Interop
{
    [Guid("00000000-0000-0000-C000-000000000046")]
    internal struct IUnknownVftbl
    {
        public unsafe delegate int _QueryInterface(IntPtr pThis, ref Guid iid, out IntPtr vftbl);
        internal delegate uint _AddRef(IntPtr pThis);
        internal delegate uint _Release(IntPtr pThis);

        public _QueryInterface QueryInterface;
        public _AddRef AddRef;
        public _Release Release;

        public static readonly IUnknownVftbl AbiToProjectionVftbl;
        public static readonly IntPtr AbiToProjectionVftblPtr;

        static IUnknownVftbl()
        {
            AbiToProjectionVftbl = GetVftbl();
            AbiToProjectionVftblPtr = Marshal.AllocHGlobal(Marshal.SizeOf<IUnknownVftbl>());
            Marshal.StructureToPtr(AbiToProjectionVftbl, AbiToProjectionVftblPtr, false);
        }

        private static IUnknownVftbl GetVftbl()
        {
            return ComWrappersSupport.IUnknownVftbl;
        }
    }
}
