using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WinRT.Interop
{
    [WindowsRuntimeType]
    [Guid("00000038-0000-0000-C000-000000000046")]
    internal interface IWeakReferenceSource
    {
        IWeakReference GetWeakReference();
    }

    [WindowsRuntimeType]
    [Guid("00000037-0000-0000-C000-000000000046")]
    internal interface IWeakReference
    {
        IObjectReference Resolve(Guid riid);
    }

    internal class ManagedWeakReference : IWeakReference
    {
        private WeakReference<object> _ref;
        public ManagedWeakReference(object obj)
        {
            _ref = new WeakReference<object>(obj);
        }

        public IObjectReference Resolve(Guid riid)
        {
            if (!_ref.TryGetTarget(out object target))
            {
                return null;
            }

            using (IObjectReference objReference = ComWrappersSupport.CreateCCWForObject(target))
            {
                return objReference.As(riid);
            }
        }
    }
}


namespace ABI.WinRT.Interop
{
    using global::WinRT;
    using WinRT.Interop;

    [Guid("00000038-0000-0000-C000-000000000046")]
    internal class IWeakReferenceSource : global::WinRT.Interop.IWeakReferenceSource
    {
        [Guid("00000038-0000-0000-C000-000000000046")]
        internal struct Vftbl
        {
            internal delegate int _GetWeakReference(IntPtr thisPtr, out IntPtr weakReference);

            public global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public _GetWeakReference GetWeakReference;

            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                    GetWeakReference = Do_Abi_GetWeakReference
                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
            }

            private static int Do_Abi_GetWeakReference(IntPtr thisPtr, out IntPtr weakReference)
            {
                weakReference = default;

                try
                {
                    weakReference = ComWrappersSupport.CreateCCWForObject(new global::WinRT.Interop.ManagedWeakReference(ComWrappersSupport.FindObject<object>(thisPtr))).As<ABI.WinRT.Interop.IWeakReference.Vftbl>().GetRef();
                }
                catch (Exception __exception__)
                {
                    return __exception__.HResult;
                }
                return 0;
            }
        }

        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IWeakReferenceSource(IObjectReference obj) => (obj != null) ? new IWeakReferenceSource(obj) : null;
        public static implicit operator IWeakReferenceSource(ObjectReference<Vftbl> obj) => (obj != null) ? new IWeakReferenceSource(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IWeakReferenceSource(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IWeakReferenceSource(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public global::WinRT.Interop.IWeakReference GetWeakReference()
        {
            IntPtr objRef = IntPtr.Zero;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetWeakReference(ThisPtr, out objRef));
                return MarshalInterface<WinRT.Interop.IWeakReference>.FromAbi(objRef);
            }
            finally
            {
                MarshalInspectable.DisposeAbi(objRef);
            }
        }
    }

    [Guid("00000037-0000-0000-C000-000000000046")]
    internal class IWeakReference : global::WinRT.Interop.IWeakReference
    {
        [Guid("00000037-0000-0000-C000-000000000046")]
        internal struct Vftbl
        {
            internal delegate int _Resolve(IntPtr thisPtr, ref Guid riid, out IntPtr objectReference);

            public global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public _Resolve Resolve;

            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                    Resolve = Do_Abi_Resolve
                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
            }

            private static int Do_Abi_Resolve(IntPtr thisPtr, ref Guid riid, out IntPtr objectReference)
            {
                IObjectReference _objectReference = default;

                objectReference = default;

                try
                {
                    _objectReference = global::WinRT.ComWrappersSupport.FindObject<global::WinRT.Interop.IWeakReference>(thisPtr).Resolve(riid);
                    objectReference = _objectReference?.GetRef() ?? IntPtr.Zero;
                }
                catch (Exception __exception__)
                {
                    return __exception__.HResult;
                }
                return 0;
            }
        }

        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IWeakReference(IObjectReference obj) => (obj != null) ? new IWeakReference(obj) : null;
        public static implicit operator IWeakReference(ObjectReference<Vftbl> obj) => (obj != null) ? new IWeakReference(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();

        public IWeakReference(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IWeakReference(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public IObjectReference Resolve(Guid riid)
        {
            ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Resolve(ThisPtr, ref riid, out IntPtr objRef));
            return ComWrappersSupport.GetObjectReferenceForInterface(objRef);
        }
    }
}
