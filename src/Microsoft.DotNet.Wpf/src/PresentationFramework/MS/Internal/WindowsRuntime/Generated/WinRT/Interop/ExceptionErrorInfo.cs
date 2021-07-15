// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Linq.Expressions;

namespace WinRT.Interop
{
    [Guid("1CF2B120-547D-101B-8E65-08002B2BD119")]
    internal interface IErrorInfo
    {
        Guid GetGuid();
        string GetSource();
        string GetDescription();
        string GetHelpFile();
        string GetHelpFileContent();
    }

    [Guid("DF0B3D60-548F-101B-8E65-08002B2BD119")]
    internal interface ISupportErrorInfo
    {
        bool InterfaceSupportsErrorInfo(Guid riid);
    }

    [Guid("04a2dbf3-df83-116c-0946-0812abf6e07d")]
    internal interface ILanguageExceptionErrorInfo
    {
        IObjectReference GetLanguageException();
    }

    [Guid("82BA7092-4C88-427D-A7BC-16DD93FEB67E")]
    internal interface IRestrictedErrorInfo
    {
        void GetErrorDetails(
            out string description,
            out int error,
            out string restrictedDescription,
            out string capabilitySid);

        string GetReference();
    }

    internal class ManagedExceptionErrorInfo : IErrorInfo, ISupportErrorInfo
    {
        private readonly Exception _exception;

        public ManagedExceptionErrorInfo(Exception ex)
        {
            _exception = ex;
        }

        public bool InterfaceSupportsErrorInfo(Guid riid) => true;

        public Guid GetGuid() => default;

        public string GetSource() => _exception.Source;

        public string GetDescription()
        {
            string desc = _exception.Message;
            if (string.IsNullOrEmpty(desc))
            {
                desc = _exception.GetType().FullName;
            }
            return desc;
        }

        public string GetHelpFile() => _exception.HelpLink;

        public string GetHelpFileContent() => string.Empty;
    }
}

#pragma warning disable CS0649

namespace ABI.WinRT.Interop
{
    using global::WinRT;

    [Guid("1CF2B120-547D-101B-8E65-08002B2BD119")]
    internal class IErrorInfo : global::WinRT.Interop.IErrorInfo
    {
        [Guid("1CF2B120-547D-101B-8E65-08002B2BD119")]
        internal struct Vftbl
        {
            internal delegate int _GetGuid(IntPtr thisPtr, out Guid guid);
            internal delegate int _GetBstr(IntPtr thisPtr, out IntPtr bstr);
            public global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public _GetGuid GetGuid_0;
            public _GetBstr GetSource_1;
            public _GetBstr GetDescription_2;
            public _GetBstr GetHelpFile_3;
            public _GetBstr GetHelpFileContent_4;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                    GetGuid_0 = Do_Abi_GetGuid_0,
                    GetSource_1 = Do_Abi_GetSource_1,
                    GetDescription_2 = Do_Abi_GetDescription_2,
                    GetHelpFile_3 = Do_Abi_GetHelpFile_3,
                    GetHelpFileContent_4 = Do_Abi_GetHelpFileContent_4
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static int Do_Abi_GetGuid_0(IntPtr thisPtr, out Guid guid)
            {
                guid = default;
                try
                {
                    guid = ComWrappersSupport.FindObject<global::WinRT.Interop.IErrorInfo>(thisPtr).GetGuid();
                }
                catch (Exception ex)
                {
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
                return 0;
            }

            private static int Do_Abi_GetSource_1(IntPtr thisPtr, out IntPtr source)
            {
                source = IntPtr.Zero;
                string _source;
                try
                {
                    _source = ComWrappersSupport.FindObject<global::WinRT.Interop.IErrorInfo>(thisPtr).GetSource();
                    source = Marshal.StringToBSTR(_source);
                }
                catch (Exception ex)
                {
                    Marshal.FreeBSTR(source);
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
                return 0;
            }

            private static int Do_Abi_GetDescription_2(IntPtr thisPtr, out IntPtr description)
            {
                description = IntPtr.Zero;
                string _description;
                try
                {
                    _description = ComWrappersSupport.FindObject<global::WinRT.Interop.IErrorInfo>(thisPtr).GetDescription();
                    description = Marshal.StringToBSTR(_description);
                }
                catch (Exception ex)
                {
                    Marshal.FreeBSTR(description);
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
                return 0;
            }

            private static int Do_Abi_GetHelpFile_3(IntPtr thisPtr, out IntPtr helpFile)
            {
                helpFile = IntPtr.Zero;
                string _helpFile;
                try
                {
                    _helpFile = ComWrappersSupport.FindObject<global::WinRT.Interop.IErrorInfo>(thisPtr).GetHelpFile();
                    helpFile = Marshal.StringToBSTR(_helpFile);
                }
                catch (Exception ex)
                {
                    Marshal.FreeBSTR(helpFile);
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
                return 0;
            }

            private static int Do_Abi_GetHelpFileContent_4(IntPtr thisPtr, out IntPtr helpFileContent)
            {
                helpFileContent = IntPtr.Zero;
                string _helpFileContent;
                try
                {
                    _helpFileContent = ComWrappersSupport.FindObject<global::WinRT.Interop.IErrorInfo>(thisPtr).GetHelpFileContent();
                    helpFileContent = Marshal.StringToBSTR(_helpFileContent);
                }
                catch (Exception ex)
                {
                    Marshal.FreeBSTR(helpFileContent);
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
                return 0;
            }
        }

        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IErrorInfo(IObjectReference obj) => (obj != null) ? new IErrorInfo(obj) : null;
        public static implicit operator IErrorInfo(ObjectReference<Vftbl> obj) => (obj != null) ? new IErrorInfo(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IErrorInfo(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IErrorInfo(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public Guid GetGuid()
        {
            Guid __return_value__;
            Marshal.ThrowExceptionForHR(_obj.Vftbl.GetGuid_0(ThisPtr, out __return_value__));
            return __return_value__;
        }

        public string GetSource()
        {
            IntPtr __retval = default;
            try
            {
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetSource_1(ThisPtr, out __retval));
                return __retval != IntPtr.Zero ? Marshal.PtrToStringBSTR(__retval) : string.Empty;
            }
            finally
            {
                Marshal.FreeBSTR(__retval);
            }
        }

        public string GetDescription()
        {
            IntPtr __retval = default;
            try
            {
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetDescription_2(ThisPtr, out __retval));
                return __retval != IntPtr.Zero ? Marshal.PtrToStringBSTR(__retval) : string.Empty;
            }
            finally
            {
                Marshal.FreeBSTR(__retval);
            }
        }

        public string GetHelpFile()
        {
            IntPtr __retval = default;
            try
            {
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetHelpFile_3(ThisPtr, out __retval));
                return __retval != IntPtr.Zero ? Marshal.PtrToStringBSTR(__retval) : string.Empty;
            }
            finally
            {
                Marshal.FreeBSTR(__retval);
            }
        }

        public string GetHelpFileContent()
        {
            IntPtr __retval = default;
            try
            {
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetHelpFileContent_4(ThisPtr, out __retval));
                return __retval != IntPtr.Zero ? Marshal.PtrToStringBSTR(__retval) : string.Empty;
            }
            finally
            {
                Marshal.FreeBSTR(__retval);
            }
        }
    }

    [Guid("04a2dbf3-df83-116c-0946-0812abf6e07d")]
    internal class ILanguageExceptionErrorInfo : global::WinRT.Interop.ILanguageExceptionErrorInfo
    {
        [Guid("04a2dbf3-df83-116c-0946-0812abf6e07d")]
        internal struct Vftbl
        {
            internal delegate int _GetLanguageException(IntPtr thisPtr, out IntPtr languageException);
            public global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public _GetLanguageException GetLanguageException_0;
        }

        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator ILanguageExceptionErrorInfo(IObjectReference obj) => (obj != null) ? new ILanguageExceptionErrorInfo(obj) : null;
        public static implicit operator ILanguageExceptionErrorInfo(ObjectReference<Vftbl> obj) => (obj != null) ? new ILanguageExceptionErrorInfo(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public ILanguageExceptionErrorInfo(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public ILanguageExceptionErrorInfo(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public IObjectReference GetLanguageException()
        {
            IntPtr __return_value__ = IntPtr.Zero;

            try
            {
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetLanguageException_0(ThisPtr, out __return_value__));
                return ObjectReference<global::WinRT.Interop.IUnknownVftbl>.Attach(ref __return_value__);
            }
            finally
            {
                // Attach with a using to do a release.
                // This can be simplified in the future when we have function pointers.
                using var obj = ObjectReference<global::WinRT.Interop.IUnknownVftbl>.Attach(ref __return_value__);
            }
        }
    }

    [Guid("DF0B3D60-548F-101B-8E65-08002B2BD119")]
    internal class ISupportErrorInfo : global::WinRT.Interop.ISupportErrorInfo
    {
        [Guid("DF0B3D60-548F-101B-8E65-08002B2BD119")]
        internal struct Vftbl
        {
            internal delegate int _InterfaceSupportsErrorInfo(IntPtr thisPtr, ref Guid riid);
            public global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public _InterfaceSupportsErrorInfo InterfaceSupportsErrorInfo_0;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                    InterfaceSupportsErrorInfo_0 = Do_Abi_InterfaceSupportsErrorInfo_0
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static int Do_Abi_InterfaceSupportsErrorInfo_0(IntPtr thisPtr, ref Guid guid)
            {
                try
                {
                    return global::WinRT.ComWrappersSupport.FindObject<global::WinRT.Interop.ISupportErrorInfo>(thisPtr).InterfaceSupportsErrorInfo(guid) ? 0 : 1;
                }
                catch (Exception ex)
                {
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
            }
        }

        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator ISupportErrorInfo(IObjectReference obj) => (obj != null) ? new ISupportErrorInfo(obj) : null;
        public static implicit operator ISupportErrorInfo(ObjectReference<Vftbl> obj) => (obj != null) ? new ISupportErrorInfo(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public ISupportErrorInfo(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public ISupportErrorInfo(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public bool InterfaceSupportsErrorInfo(Guid riid)
        {
            return _obj.Vftbl.InterfaceSupportsErrorInfo_0(ThisPtr, ref riid) == 0;
        }
    }

    [Guid("82BA7092-4C88-427D-A7BC-16DD93FEB67E")]
    internal class IRestrictedErrorInfo : global::WinRT.Interop.IRestrictedErrorInfo
    {
        [Guid("82BA7092-4C88-427D-A7BC-16DD93FEB67E")]
        internal struct Vftbl
        {
            internal delegate int _GetErrorDetails(IntPtr thisPtr, out IntPtr description, out int error, out IntPtr restrictedDescription, out IntPtr capabilitySid);
            internal delegate int _GetReference(IntPtr thisPtr, out IntPtr reference);

            public global::WinRT.Interop.IUnknownVftbl unknownVftbl;
            public _GetErrorDetails GetErrorDetails_0;
            public _GetReference GetReference_1;
        }

        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IRestrictedErrorInfo(IObjectReference obj) => (obj != null) ? new IRestrictedErrorInfo(obj) : null;
        public static implicit operator IRestrictedErrorInfo(ObjectReference<Vftbl> obj) => (obj != null) ? new IRestrictedErrorInfo(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IRestrictedErrorInfo(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IRestrictedErrorInfo(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public void GetErrorDetails(
            out string description,
            out int error,
            out string restrictedDescription,
            out string capabilitySid)
        {
            IntPtr _description = IntPtr.Zero;
            IntPtr _restrictedDescription = IntPtr.Zero;
            IntPtr _capabilitySid = IntPtr.Zero;
            try
            {
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetErrorDetails_0(ThisPtr, out _description, out error, out _restrictedDescription, out _capabilitySid));
                description = _description != IntPtr.Zero ? Marshal.PtrToStringBSTR(_description) : string.Empty;
                restrictedDescription = _restrictedDescription != IntPtr.Zero ? Marshal.PtrToStringBSTR(_restrictedDescription) : string.Empty;
                capabilitySid = _capabilitySid != IntPtr.Zero ? Marshal.PtrToStringBSTR(_capabilitySid) : string.Empty;
            }
            finally
            {
                Marshal.FreeBSTR(_description);
                Marshal.FreeBSTR(_restrictedDescription);
                Marshal.FreeBSTR(_capabilitySid);
            }
        }

        public string GetReference()
        {
            IntPtr __retval = default;
            try
            {
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetReference_1(ThisPtr, out __retval));
                return __retval != IntPtr.Zero ? Marshal.PtrToStringBSTR(__retval) : string.Empty;
            }
            finally
            {
                Marshal.FreeBSTR(__retval);
            }
        }
    }
}
