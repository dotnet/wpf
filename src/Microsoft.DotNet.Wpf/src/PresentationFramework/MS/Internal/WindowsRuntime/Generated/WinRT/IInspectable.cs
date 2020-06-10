// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using WinRT.Interop;

namespace WinRT
{
    internal enum TrustLevel
    {
        BaseTrust = 0,
        PartialTrust = BaseTrust + 1,
        FullTrust = PartialTrust + 1
    }

    // IInspectable
    [ObjectReferenceWrapper(nameof(_obj))]
    [Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")]
    internal class IInspectable
    {
        [Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")]
        internal struct Vftbl
        {
            internal delegate int _GetIids(IntPtr pThis, out uint iidCount, out Guid[] iids);
            internal delegate int _GetRuntimeClassName(IntPtr pThis, out IntPtr className);
            internal delegate int _GetTrustLevel(IntPtr pThis, out TrustLevel trustLevel);

            public IUnknownVftbl IUnknownVftbl;
            public _GetIids GetIids;
            public _GetRuntimeClassName GetRuntimeClassName;
            public _GetTrustLevel GetTrustLevel;

            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = IUnknownVftbl.AbiToProjectionVftbl,
                    GetIids = Do_Abi_GetIids,
                    GetRuntimeClassName = Do_Abi_GetRuntimeClassName,
                    GetTrustLevel = Do_Abi_GetTrustLevel
                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
            }

            private static int Do_Abi_GetIids(IntPtr pThis, out uint iidCount, out Guid[] iids)
            {
                iidCount = 0u;
                iids = null;
                try
                {
                    iids = ComWrappersSupport.GetInspectableInfo(pThis).IIDs;
                    iidCount = (uint)iids.Length;
                }
                catch (Exception ex)
                {
                    return ex.HResult;
                }
                return 0;
            }

            private unsafe static int Do_Abi_GetRuntimeClassName(IntPtr pThis, out IntPtr className)
            {
                className = default;
                try
                {
                    string runtimeClassName = ComWrappersSupport.GetInspectableInfo(pThis).RuntimeClassName;
                    className = MarshalString.FromManaged(runtimeClassName);
                }
                catch (Exception ex)
                {
                    return ex.HResult;
                }
                return 0;
            }

            private static int Do_Abi_GetTrustLevel(IntPtr pThis, out TrustLevel trustLevel)
            {
                trustLevel = TrustLevel.BaseTrust;
                return 0;
            }
        }

        public static IInspectable FromAbi(IntPtr thisPtr) =>
            new IInspectable(ObjectReference<Vftbl>.FromAbi(thisPtr));

        private readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public static implicit operator IInspectable(IObjectReference obj) => obj.As<Vftbl>();
        public static implicit operator IInspectable(ObjectReference<Vftbl> obj) => new IInspectable(obj);
        public ObjectReference<I> As<I>() => _obj.As<I>();
        public IObjectReference ObjRef { get => _obj; }
        public IInspectable(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IInspectable(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe string GetRuntimeClassName(bool noThrow = false)
        {
            IntPtr __retval = default;
            try
            {
                var hr = _obj.Vftbl.GetRuntimeClassName(ThisPtr, out __retval);
                if (hr != 0)
                {
                    if (noThrow)
                        return null;
                    Marshal.ThrowExceptionForHR(hr);
                }
                uint length;
                char* buffer = Platform.WindowsGetStringRawBuffer(__retval, &length);
                return new string(buffer, 0, (int)length);
            }
            finally
            {
                Platform.WindowsDeleteString(__retval);
            }
        }
    }

}
