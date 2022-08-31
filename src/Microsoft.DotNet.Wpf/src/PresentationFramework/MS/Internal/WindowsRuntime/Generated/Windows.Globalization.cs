// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;


#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to

namespace MS.Internal.WindowsRuntime
{
namespace Windows.Globalization
{
    [global::WinRT.WindowsRuntimeType]
    [Guid("EA79A752-F7C2-4265-B1BD-C4DEC4E4F080")]
    
    internal interface ILanguage
    {
        string DisplayName { get; }
        string LanguageTag { get; }
        string NativeName { get; }
        string Script { get; }
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("6A47E5B5-D94D-4886-A404-A5A5B9D5B494")]
    
    internal interface ILanguage2
    {
        LanguageLayoutDirection LayoutDirection { get; }
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("7D7DAF45-368D-4364-852B-DEC927037B85")]
    
    internal interface ILanguageExtensionSubtags
    {
        global::System.Collections.Generic.IReadOnlyList<string> GetExtensionSubtags(string singleton);
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("9B0252AC-0C27-44F8-B792-9793FB66C63E")]
    internal interface ILanguageFactory
    {
        Language CreateLanguage(string languageTag);
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("B23CD557-0865-46D4-89B8-D59BE8990F0D")]
    
    internal interface ILanguageStatics
    {
        bool IsWellFormed(string languageTag);
        string CurrentInputMethodLanguageTag { get; }
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("30199F6E-914B-4B2A-9D6E-E3B0E27DBE4F")]
    
    internal interface ILanguageStatics2
    {
        bool TrySetInputMethodLanguageTag(string languageTag);
    }
    [global::WinRT.WindowsRuntimeType]
    [global::WinRT.ProjectedRuntimeClass(nameof(_default))]
    
    internal sealed class Language : global::System.Runtime.InteropServices.ICustomQueryInterface, IEquatable<Language>
    {
        public IntPtr ThisPtr => _default.ThisPtr;

        private IObjectReference _inner = null;
        private readonly Lazy<global::MS.Internal.WindowsRuntime.ABI.Windows.Globalization.ILanguage> _defaultLazy;

        private global::MS.Internal.WindowsRuntime.ABI.Windows.Globalization.ILanguage _default => _defaultLazy.Value;

        internal class _ILanguageFactory : ABI.Windows.Globalization.ILanguageFactory
        {
            public _ILanguageFactory() : base(ActivationFactory<Language>.As<ABI.Windows.Globalization.ILanguageFactory.Vftbl>()) { }
            private static WeakLazy<_ILanguageFactory> _instance = new WeakLazy<_ILanguageFactory>();
            public static _ILanguageFactory Instance => _instance.Value;

            public unsafe new IntPtr CreateLanguage(string languageTag)
            {
                MarshalString __languageTag = default;
                IntPtr __retval = default;
                try
                {
                    __languageTag = MarshalString.CreateMarshaler(languageTag);
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CreateLanguage_0(ThisPtr, MarshalString.GetAbi(__languageTag), out __retval));
                    return __retval;
                }
                finally
                {
                    MarshalString.DisposeMarshaler(__languageTag);
                }
            }

        }

        public Language(string languageTag) : this(((Func<global::MS.Internal.WindowsRuntime.ABI.Windows.Globalization.ILanguage>)(() => {
            IntPtr ptr = (_ILanguageFactory.Instance.CreateLanguage(languageTag));
            try
            {
                return new global::MS.Internal.WindowsRuntime.ABI.Windows.Globalization.ILanguage(ComWrappersSupport.GetObjectReferenceForInterface(ptr));
            }
            finally
            {
                MarshalInspectable.DisposeAbi(ptr);
            }
        }))())
        {
            ComWrappersSupport.RegisterObjectForInterface(this, ThisPtr);
        }

        internal class _ILanguageStatics : ABI.Windows.Globalization.ILanguageStatics
        {
            public _ILanguageStatics() : base((new BaseActivationFactory("Windows.Globalization", "Windows.Globalization.Language"))._As<ABI.Windows.Globalization.ILanguageStatics.Vftbl>()) { }
            private static WeakLazy<_ILanguageStatics> _instance = new WeakLazy<_ILanguageStatics>();
            public static ILanguageStatics Instance => _instance.Value;
        }

        public static bool IsWellFormed(string languageTag) => _ILanguageStatics.Instance.IsWellFormed(languageTag);

        public static string CurrentInputMethodLanguageTag => _ILanguageStatics.Instance.CurrentInputMethodLanguageTag;

        internal class _ILanguageStatics2 : ABI.Windows.Globalization.ILanguageStatics2
        {
            public _ILanguageStatics2() : base((new BaseActivationFactory("Windows.Globalization", "Windows.Globalization.Language"))._As<ABI.Windows.Globalization.ILanguageStatics2.Vftbl>()) { }
            private static WeakLazy<_ILanguageStatics2> _instance = new WeakLazy<_ILanguageStatics2>();
            public static ILanguageStatics2 Instance => _instance.Value;
        }

        public static bool TrySetInputMethodLanguageTag(string languageTag) => _ILanguageStatics2.Instance.TrySetInputMethodLanguageTag(languageTag);

        public static Language FromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero) return null;
            var obj = MarshalInspectable.FromAbi(thisPtr);
            return obj is Language ? (Language)obj : new Language((global::MS.Internal.WindowsRuntime.ABI.Windows.Globalization.ILanguage)obj);
        }

        public Language(global::MS.Internal.WindowsRuntime.ABI.Windows.Globalization.ILanguage ifc)
        {
            _defaultLazy = new Lazy<global::MS.Internal.WindowsRuntime.ABI.Windows.Globalization.ILanguage>(() => ifc);
        }

        public static bool operator ==(Language x, Language y) => (x?.ThisPtr ?? IntPtr.Zero) == (y?.ThisPtr ?? IntPtr.Zero);
        public static bool operator !=(Language x, Language y) => !(x == y);
        public bool Equals(Language other) => this == other;
        public override bool Equals(object obj) => obj is Language that && this == that;
        public override int GetHashCode() => ThisPtr.GetHashCode();

        private  IObjectReference GetDefaultReference<T>() => _default.AsInterface<T>();
        private  IObjectReference GetReferenceForQI() => _inner ?? _default.ObjRef;

        private struct InterfaceTag<I>{};

        private ILanguage AsInternal(InterfaceTag<ILanguage> _) => _default;

        private ILanguageExtensionSubtags AsInternal(InterfaceTag<ILanguageExtensionSubtags> _) => new global::MS.Internal.WindowsRuntime.ABI.Windows.Globalization.ILanguageExtensionSubtags(GetReferenceForQI());

        public global::System.Collections.Generic.IReadOnlyList<string> GetExtensionSubtags(string singleton) => AsInternal(new InterfaceTag<ILanguageExtensionSubtags>()).GetExtensionSubtags(singleton);

        private ILanguage2 AsInternal(InterfaceTag<ILanguage2> _) => new global::MS.Internal.WindowsRuntime.ABI.Windows.Globalization.ILanguage2(GetReferenceForQI());

        public string DisplayName => _default.DisplayName;

        public string LanguageTag => _default.LanguageTag;

        public LanguageLayoutDirection LayoutDirection => AsInternal(new InterfaceTag<ILanguage2>()).LayoutDirection;

        public string NativeName => _default.NativeName;

        public string Script => _default.Script;

        private bool IsOverridableInterface(Guid iid) => false;

        global::System.Runtime.InteropServices.CustomQueryInterfaceResult global::System.Runtime.InteropServices.ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;
            if (IsOverridableInterface(iid) || typeof(global::WinRT.IInspectable).GUID == iid)
            {
                return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.NotHandled;
            }

            if (GetReferenceForQI().TryAs<IUnknownVftbl>(iid, out ObjectReference<IUnknownVftbl> objRef) >= 0)
            {
                using (objRef)
                {
                    ppv = objRef.GetRef();
                    return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.Handled;
                }
            }

            return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.NotHandled;
        }
    }
    [global::WinRT.WindowsRuntimeType]
    
    internal enum LanguageLayoutDirection : int
    {
        Ltr = unchecked((int)0),
        Rtl = unchecked((int)0x1),
        TtbLtr = unchecked((int)0x2),
        TtbRtl = unchecked((int)0x3),
    }
}

namespace ABI.Windows.Globalization
{
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("EA79A752-F7C2-4265-B1BD-C4DEC4E4F080")]
    internal class ILanguage : global::MS.Internal.WindowsRuntime.Windows.Globalization.ILanguage
    {
        [Guid("EA79A752-F7C2-4265-B1BD-C4DEC4E4F080")]
        internal struct Vftbl
        {
            public IInspectable.Vftbl IInspectableVftbl;
            public _get_PropertyAsString get_LanguageTag_0;
            public _get_PropertyAsString get_DisplayName_1;
            public _get_PropertyAsString get_NativeName_2;
            public _get_PropertyAsString get_Script_3;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable, 
                    get_LanguageTag_0 = Do_Abi_get_LanguageTag_0,
                    get_DisplayName_1 = Do_Abi_get_DisplayName_1,
                    get_NativeName_2 = Do_Abi_get_NativeName_2,
                    get_Script_3 = Do_Abi_get_Script_3
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 4);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_get_DisplayName_1(IntPtr thisPtr, out IntPtr value)
            {
                string __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Globalization.ILanguage>(thisPtr).DisplayName;
                    value = MarshalString.FromManaged(__value);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_LanguageTag_0(IntPtr thisPtr, out IntPtr value)
            {
                string __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Globalization.ILanguage>(thisPtr).LanguageTag;
                    value = MarshalString.FromManaged(__value);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_NativeName_2(IntPtr thisPtr, out IntPtr value)
            {
                string __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Globalization.ILanguage>(thisPtr).NativeName;
                    value = MarshalString.FromManaged(__value);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_Script_3(IntPtr thisPtr, out IntPtr value)
            {
                string __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Globalization.ILanguage>(thisPtr).Script;
                    value = MarshalString.FromManaged(__value);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator ILanguage(IObjectReference obj) => (obj != null) ? new ILanguage(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public ILanguage(IObjectReference obj) : this(obj.As<Vftbl>()) {}
        public ILanguage(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe string DisplayName
        {
            get
            {
                IntPtr __retval = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_DisplayName_1(ThisPtr, out __retval));
                    return MarshalString.FromAbi(__retval);
                }
                finally
                {
                    MarshalString.DisposeAbi(__retval);
                }
            }
        }

        public unsafe string LanguageTag
        {
            get
            {
                IntPtr __retval = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_LanguageTag_0(ThisPtr, out __retval));
                    return MarshalString.FromAbi(__retval);
                }
                finally
                {
                    MarshalString.DisposeAbi(__retval);
                }
            }
        }

        public unsafe string NativeName
        {
            get
            {
                IntPtr __retval = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_NativeName_2(ThisPtr, out __retval));
                    return MarshalString.FromAbi(__retval);
                }
                finally
                {
                    MarshalString.DisposeAbi(__retval);
                }
            }
        }

        public unsafe string Script
        {
            get
            {
                IntPtr __retval = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Script_3(ThisPtr, out __retval));
                    return MarshalString.FromAbi(__retval);
                }
                finally
                {
                    MarshalString.DisposeAbi(__retval);
                }
            }
        }
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("6A47E5B5-D94D-4886-A404-A5A5B9D5B494")]
    internal class ILanguage2 : global::MS.Internal.WindowsRuntime.Windows.Globalization.ILanguage2
    {
        [Guid("6A47E5B5-D94D-4886-A404-A5A5B9D5B494")]
        internal struct Vftbl
        {
            public IInspectable.Vftbl IInspectableVftbl;
            public ILanguage2_Delegates.get_LayoutDirection_0 get_LayoutDirection_0;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable, 
                    get_LayoutDirection_0 = Do_Abi_get_LayoutDirection_0
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_get_LayoutDirection_0(IntPtr thisPtr, out global::MS.Internal.WindowsRuntime.Windows.Globalization.LanguageLayoutDirection value)
            {
                global::MS.Internal.WindowsRuntime.Windows.Globalization.LanguageLayoutDirection __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Globalization.ILanguage2>(thisPtr).LayoutDirection;
                    value = __value;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator ILanguage2(IObjectReference obj) => (obj != null) ? new ILanguage2(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public ILanguage2(IObjectReference obj) : this(obj.As<Vftbl>()) {}
        public ILanguage2(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe global::MS.Internal.WindowsRuntime.Windows.Globalization.LanguageLayoutDirection LayoutDirection
        {
            get
            {
                global::MS.Internal.WindowsRuntime.Windows.Globalization.LanguageLayoutDirection __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_LayoutDirection_0(ThisPtr, out __retval));
                return __retval;
            }
        }
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static class ILanguage2_Delegates
    {
        public unsafe delegate int get_LayoutDirection_0(IntPtr thisPtr, out global::MS.Internal.WindowsRuntime.Windows.Globalization.LanguageLayoutDirection value);
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("7D7DAF45-368D-4364-852B-DEC927037B85")]
    internal class ILanguageExtensionSubtags : global::MS.Internal.WindowsRuntime.Windows.Globalization.ILanguageExtensionSubtags
    {
        [Guid("7D7DAF45-368D-4364-852B-DEC927037B85")]
        internal struct Vftbl
        {
            public IInspectable.Vftbl IInspectableVftbl;
            public ILanguageExtensionSubtags_Delegates.GetExtensionSubtags_0 GetExtensionSubtags_0;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable, 
                    GetExtensionSubtags_0 = Do_Abi_GetExtensionSubtags_0
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_GetExtensionSubtags_0(IntPtr thisPtr, IntPtr singleton, out IntPtr value)
            {
                global::System.Collections.Generic.IReadOnlyList<string> __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Globalization.ILanguageExtensionSubtags>(thisPtr).GetExtensionSubtags(MarshalString.FromAbi(singleton));
                    value = global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IReadOnlyList<string>.FromManaged(__value);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator ILanguageExtensionSubtags(IObjectReference obj) => (obj != null) ? new ILanguageExtensionSubtags(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public ILanguageExtensionSubtags(IObjectReference obj) : this(obj.As<Vftbl>()) {}
        public ILanguageExtensionSubtags(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe global::System.Collections.Generic.IReadOnlyList<string> GetExtensionSubtags(string singleton)
        {
            MarshalString __singleton = default;
            IntPtr __retval = default;
            try
            {
                __singleton = MarshalString.CreateMarshaler(singleton);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetExtensionSubtags_0(ThisPtr, MarshalString.GetAbi(__singleton), out __retval));
                return global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IReadOnlyList<string>.FromAbi(__retval);
            }
            finally
            {
                MarshalString.DisposeMarshaler(__singleton);
                global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IReadOnlyList<string>.DisposeAbi(__retval);
            }
        }
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static class ILanguageExtensionSubtags_Delegates
    {
        public unsafe delegate int GetExtensionSubtags_0(IntPtr thisPtr, IntPtr singleton, out IntPtr value);
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("9B0252AC-0C27-44F8-B792-9793FB66C63E")]
    internal class ILanguageFactory : global::MS.Internal.WindowsRuntime.Windows.Globalization.ILanguageFactory
    {
        [Guid("9B0252AC-0C27-44F8-B792-9793FB66C63E")]
        internal struct Vftbl
        {
            public IInspectable.Vftbl IInspectableVftbl;
            public ILanguageFactory_Delegates.CreateLanguage_0 CreateLanguage_0;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable, 
                    CreateLanguage_0 = Do_Abi_CreateLanguage_0
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_CreateLanguage_0(IntPtr thisPtr, IntPtr languageTag, out IntPtr result)
            {
                global::MS.Internal.WindowsRuntime.Windows.Globalization.Language __result = default;

                result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Globalization.ILanguageFactory>(thisPtr).CreateLanguage(MarshalString.FromAbi(languageTag));
                    result = global::MS.Internal.WindowsRuntime.ABI.Windows.Globalization.Language.FromManaged(__result);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator ILanguageFactory(IObjectReference obj) => (obj != null) ? new ILanguageFactory(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public ILanguageFactory(IObjectReference obj) : this(obj.As<Vftbl>()) {}
        public ILanguageFactory(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe global::MS.Internal.WindowsRuntime.Windows.Globalization.Language CreateLanguage(string languageTag)
        {
            MarshalString __languageTag = default;
            IntPtr __retval = default;
            try
            {
                __languageTag = MarshalString.CreateMarshaler(languageTag);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CreateLanguage_0(ThisPtr, MarshalString.GetAbi(__languageTag), out __retval));
                return global::MS.Internal.WindowsRuntime.ABI.Windows.Globalization.Language.FromAbi(__retval);
            }
            finally
            {
                MarshalString.DisposeMarshaler(__languageTag);
                global::MS.Internal.WindowsRuntime.ABI.Windows.Globalization.Language.DisposeAbi(__retval);
            }
        }
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static class ILanguageFactory_Delegates
    {
        public unsafe delegate int CreateLanguage_0(IntPtr thisPtr, IntPtr languageTag, out IntPtr result);
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("B23CD557-0865-46D4-89B8-D59BE8990F0D")]
    internal class ILanguageStatics : global::MS.Internal.WindowsRuntime.Windows.Globalization.ILanguageStatics
    {
        [Guid("B23CD557-0865-46D4-89B8-D59BE8990F0D")]
        internal struct Vftbl
        {
            public IInspectable.Vftbl IInspectableVftbl;
            public ILanguageStatics_Delegates.IsWellFormed_0 IsWellFormed_0;
            public _get_PropertyAsString get_CurrentInputMethodLanguageTag_1;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable, 
                    IsWellFormed_0 = Do_Abi_IsWellFormed_0,
                    get_CurrentInputMethodLanguageTag_1 = Do_Abi_get_CurrentInputMethodLanguageTag_1
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 2);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_IsWellFormed_0(IntPtr thisPtr, IntPtr languageTag, out byte result)
            {
                bool __result = default;

                result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Globalization.ILanguageStatics>(thisPtr).IsWellFormed(MarshalString.FromAbi(languageTag));
                    result = (byte)(__result ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_CurrentInputMethodLanguageTag_1(IntPtr thisPtr, out IntPtr value)
            {
                string __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Globalization.ILanguageStatics>(thisPtr).CurrentInputMethodLanguageTag;
                    value = MarshalString.FromManaged(__value);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator ILanguageStatics(IObjectReference obj) => (obj != null) ? new ILanguageStatics(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public ILanguageStatics(IObjectReference obj) : this(obj.As<Vftbl>()) {}
        public ILanguageStatics(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe bool IsWellFormed(string languageTag)
        {
            MarshalString __languageTag = default;
            byte __retval = default;
            try
            {
                __languageTag = MarshalString.CreateMarshaler(languageTag);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IsWellFormed_0(ThisPtr, MarshalString.GetAbi(__languageTag), out __retval));
                return __retval != 0;
            }
            finally
            {
                MarshalString.DisposeMarshaler(__languageTag);
            }
        }

        public unsafe string CurrentInputMethodLanguageTag
        {
            get
            {
                IntPtr __retval = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_CurrentInputMethodLanguageTag_1(ThisPtr, out __retval));
                    return MarshalString.FromAbi(__retval);
                }
                finally
                {
                    MarshalString.DisposeAbi(__retval);
                }
            }
        }
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static class ILanguageStatics_Delegates
    {
        public unsafe delegate int IsWellFormed_0(IntPtr thisPtr, IntPtr languageTag, out byte result);
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("30199F6E-914B-4B2A-9D6E-E3B0E27DBE4F")]
    internal class ILanguageStatics2 : global::MS.Internal.WindowsRuntime.Windows.Globalization.ILanguageStatics2
    {
        [Guid("30199F6E-914B-4B2A-9D6E-E3B0E27DBE4F")]
        internal struct Vftbl
        {
            public IInspectable.Vftbl IInspectableVftbl;
            public ILanguageStatics2_Delegates.TrySetInputMethodLanguageTag_0 TrySetInputMethodLanguageTag_0;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable, 
                    TrySetInputMethodLanguageTag_0 = Do_Abi_TrySetInputMethodLanguageTag_0
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_TrySetInputMethodLanguageTag_0(IntPtr thisPtr, IntPtr languageTag, out byte result)
            {
                bool __result = default;

                result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::MS.Internal.WindowsRuntime.Windows.Globalization.ILanguageStatics2>(thisPtr).TrySetInputMethodLanguageTag(MarshalString.FromAbi(languageTag));
                    result = (byte)(__result ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator ILanguageStatics2(IObjectReference obj) => (obj != null) ? new ILanguageStatics2(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public ILanguageStatics2(IObjectReference obj) : this(obj.As<Vftbl>()) {}
        public ILanguageStatics2(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe bool TrySetInputMethodLanguageTag(string languageTag)
        {
            MarshalString __languageTag = default;
            byte __retval = default;
            try
            {
                __languageTag = MarshalString.CreateMarshaler(languageTag);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.TrySetInputMethodLanguageTag_0(ThisPtr, MarshalString.GetAbi(__languageTag), out __retval));
                return __retval != 0;
            }
            finally
            {
                MarshalString.DisposeMarshaler(__languageTag);
            }
        }
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static class ILanguageStatics2_Delegates
    {
        public unsafe delegate int TrySetInputMethodLanguageTag_0(IntPtr thisPtr, IntPtr languageTag, out byte result);
    }

    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal struct Language
    {
        public static IObjectReference CreateMarshaler(global::MS.Internal.WindowsRuntime.Windows.Globalization.Language obj) => obj is null ? null : MarshalInspectable.CreateMarshaler(obj).As<ILanguage.Vftbl>();
        public static IntPtr GetAbi(IObjectReference value) => value is null ? IntPtr.Zero : MarshalInterfaceHelper<object>.GetAbi(value);
        public static global::MS.Internal.WindowsRuntime.Windows.Globalization.Language FromAbi(IntPtr thisPtr) => global::MS.Internal.WindowsRuntime.Windows.Globalization.Language.FromAbi(thisPtr);
        public static IntPtr FromManaged(global::MS.Internal.WindowsRuntime.Windows.Globalization.Language obj) => obj is null ? IntPtr.Zero : CreateMarshaler(obj).GetRef();
        public static unsafe MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Globalization.Language>.MarshalerArray CreateMarshalerArray(global::MS.Internal.WindowsRuntime.Windows.Globalization.Language[] array) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Globalization.Language>.CreateMarshalerArray(array, (o) => CreateMarshaler(o));
        public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Globalization.Language>.GetAbiArray(box);
        public static unsafe global::MS.Internal.WindowsRuntime.Windows.Globalization.Language[] FromAbiArray(object box) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Globalization.Language>.FromAbiArray(box, FromAbi);
        public static (int length, IntPtr data) FromManagedArray(global::MS.Internal.WindowsRuntime.Windows.Globalization.Language[] array) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Globalization.Language>.FromManagedArray(array, (o) => FromManaged(o));
        public static void DisposeMarshaler(IObjectReference value) => MarshalInspectable.DisposeMarshaler(value);
        public static void DisposeMarshalerArray(MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Globalization.Language>.MarshalerArray array) => MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Globalization.Language>.DisposeMarshalerArray(array);
        public static void DisposeAbi(IntPtr abi) => MarshalInspectable.DisposeAbi(abi);
        public static unsafe void DisposeAbiArray(object box) => MarshalInspectable.DisposeAbiArray(box);
    }
}
}
