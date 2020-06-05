// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using WinRT;
using WinRT.Interop;

#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to

namespace WinRT.Interop
{
    struct ComCallData
    {
        public int dwDispid;
        public int dwReserved;
        public IntPtr pUserDefined;
    }

    unsafe delegate int PFNCONTEXTCALL(ComCallData* data);

    [Guid("000001da-0000-0000-C000-000000000046")]
    unsafe interface IContextCallback
    {
        // The pUnk parameter is intentionally excluded here
        // since it is required to always be null.
        void ContextCallback(
            PFNCONTEXTCALL pfnCallback,
            ComCallData* pParam,
            Guid riid,
            int iMethod);
    }
}


namespace ABI.WinRT.Interop
{
    [Guid("000001da-0000-0000-C000-000000000046")]
    class IContextCallback : global::WinRT.Interop.IContextCallback
    {
        [Guid("000001da-0000-0000-C000-000000000046")]
        internal struct Vftbl
        {
            public unsafe delegate int _ContextCallback(
                IntPtr pThis,
                PFNCONTEXTCALL pfnCallback,
                ComCallData* pData,
                ref Guid riid,
                int iMethod,
                IntPtr pUnk);
            global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public _ContextCallback ContextCallback_4;
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IContextCallback(IObjectReference obj) => (obj != null) ? new IContextCallback(obj) : null;
        public static implicit operator IContextCallback(ObjectReference<Vftbl> obj) => (obj != null) ? new IContextCallback(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IContextCallback(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IContextCallback(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        private unsafe struct ContextCallData
        {
            public IntPtr delegateHandle;
            public ComCallData* userData;
        }

        public unsafe void ContextCallback(global::WinRT.Interop.PFNCONTEXTCALL pfnCallback, ComCallData* pParam, Guid riid, int iMethod)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.ContextCallback_4(ThisPtr, pfnCallback, pParam, ref riid, iMethod, IntPtr.Zero));
        }
    }
}
