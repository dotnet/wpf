// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;
using System.Diagnostics;

#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to

namespace MS.Internal.WindowsRuntime.Windows.Foundation.Collections
{
    [Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3")]
    internal interface IIterable<T>
    {
        IIterator<T> First();
    }
    [Guid("6A79E863-4300-459A-9966-CBB660963EE1")]
    internal interface IIterator<T>
    {
        bool _MoveNext();
        uint GetMany(ref T[] items);
        T _Current { get; }
        bool HasCurrent { get; }
    }
}

namespace MS.Internal.WindowsRuntime.ABI.System.Collections.Generic
{
    using global::System;
    using global::System.Runtime.CompilerServices;

    [Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3")]
    internal class IEnumerable<T> : global::System.Collections.Generic.IEnumerable<T>, global::MS.Internal.WindowsRuntime.Windows.Foundation.Collections.IIterable<T>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IEnumerable<T> obj) =>
            obj is null ? null : ComWrappersSupport.CreateCCWForObject(obj).As<Vftbl>(GuidGenerator.GetIID(typeof(IEnumerable<T>)));

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static global::System.Collections.Generic.IEnumerable<T> FromAbi(IntPtr thisPtr) =>
            thisPtr == IntPtr.Zero ? null : new IEnumerable<T>(ObjRefFromAbi(thisPtr));

        public static IntPtr FromManaged(global::System.Collections.Generic.IEnumerable<T> value) =>
            (value is null) ? IntPtr.Zero : CreateMarshaler(value).GetRef();

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Foundation.Collections.IIterable<T>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IEnumerable<T>));

        public class FromAbiHelper : global::System.Collections.Generic.IEnumerable<T>
        {
            private readonly global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IEnumerable<T> _iterable;

            public FromAbiHelper(IObjectReference obj) :
                this(new global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IEnumerable<T>(obj))
            {
            }

            public FromAbiHelper(global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IEnumerable<T> iterable)
            {
                _iterable = iterable;
            }

            public global::System.Collections.Generic.IEnumerator<T> GetEnumerator()
            {
                var first = ((global::MS.Internal.WindowsRuntime.Windows.Foundation.Collections.IIterable<T>)_iterable).First();
                if (first is global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IEnumerator<T> iterator)
                {
                    return iterator;
                }
                throw new InvalidOperationException("Unexpected type for enumerator");
            }

            global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }

        internal sealed class ToAbiHelper : global::MS.Internal.WindowsRuntime.Windows.Foundation.Collections.IIterable<T>
        {
            private readonly IEnumerable<T> m_enumerable;

            internal ToAbiHelper(IEnumerable<T> enumerable) => m_enumerable = enumerable;

            public global::MS.Internal.WindowsRuntime.Windows.Foundation.Collections.IIterator<T> First() =>
                new IEnumerator<T>.ToAbiHelper(m_enumerable.GetEnumerator());
        }

        [Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public IEnumerable_Delegates.First_0 First_0;
            public static Guid PIID = GuidGenerator.CreateIID(typeof(IEnumerable<T>));

            internal unsafe Vftbl(IntPtr thisPtr)
            {
                var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
                var vftbl = (IntPtr*)vftblPtr.Vftbl;
                IInspectableVftbl = Marshal.PtrToStructure<IInspectable.Vftbl>(vftblPtr.Vftbl);
                First_0 = Marshal.GetDelegateForFunctionPointer<IEnumerable_Delegates.First_0>(vftbl[6]);
            }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    First_0 = Do_Abi_First_0
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);
                nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.First_0);

                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_First_0(IntPtr thisPtr, out IntPtr __return_value__)
            {
                __return_value__ = default;
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Generic.IEnumerable<T>>(thisPtr);
                    __return_value__ = MarshalInterface<global::System.Collections.Generic.IEnumerator<T>>.FromManaged(__this.GetEnumerator());
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        public static ObjectReference<Vftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var vftblT = new Vftbl(thisPtr);
            return ObjectReference<Vftbl>.FromAbi(thisPtr, vftblT.IInspectableVftbl.IUnknownVftbl, vftblT);
        }
        public static Guid PIID = Vftbl.PIID;

        public static implicit operator IEnumerable<T>(IObjectReference obj) => (obj != null) ? new IEnumerable<T>(obj) : null;
        public static implicit operator IEnumerable<T>(ObjectReference<Vftbl> obj) => (obj != null) ? new IEnumerable<T>(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }

        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IEnumerable(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IEnumerable(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
            _FromIterable = new FromAbiHelper(this);
        }
        FromAbiHelper _FromIterable;

        unsafe global::MS.Internal.WindowsRuntime.Windows.Foundation.Collections.IIterator<T> global::MS.Internal.WindowsRuntime.Windows.Foundation.Collections.IIterable<T>.First()
        {
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.First_0(ThisPtr, out __retval));
                return ABI.System.Collections.Generic.IEnumerator<T>.FromAbiInternal(__retval);
            }
            finally
            {
                ABI.System.Collections.Generic.IEnumerator<T>.DisposeAbi(__retval);
            }
        }

        public global::System.Collections.Generic.IEnumerator<T> GetEnumerator() => _FromIterable.GetEnumerator();
        IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
    internal static class IEnumerable_Delegates
    {
        public unsafe delegate int First_0(IntPtr thisPtr, out IntPtr __return_value__);
    }

    [Guid("6A79E863-4300-459A-9966-CBB660963EE1")]
    internal class IEnumerator<T> : global::System.Collections.Generic.IEnumerator<T>, global::MS.Internal.WindowsRuntime.Windows.Foundation.Collections.IIterator<T>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IEnumerator<T> obj) =>
            obj is null ? null : ComWrappersSupport.CreateCCWForObject(obj).As<Vftbl>(GuidGenerator.GetIID(typeof(IEnumerator<T>)));

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static global::System.Collections.Generic.IEnumerator<T> FromAbi(IntPtr thisPtr) =>
            thisPtr == IntPtr.Zero ? null : new IEnumerator<T>(ObjRefFromAbi(thisPtr));

        internal static global::MS.Internal.WindowsRuntime.Windows.Foundation.Collections.IIterator<T> FromAbiInternal(IntPtr thisPtr) =>
            new IEnumerator<T>(ObjRefFromAbi(thisPtr));

        public static IntPtr FromManaged(global::System.Collections.Generic.IEnumerator<T> value) =>
            (value is null) ? IntPtr.Zero : CreateMarshaler(value).GetRef();

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Foundation.Collections.IIterator<T>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IEnumerator<T>));

        public class FromAbiHelper : global::System.Collections.Generic.IEnumerator<T>
        {
            private readonly global::MS.Internal.WindowsRuntime.Windows.Foundation.Collections.IIterator<T> _iterator;

            public FromAbiHelper(IObjectReference obj) :
                this(new global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IEnumerator<T>(obj))
            {
            }

            internal FromAbiHelper(global::MS.Internal.WindowsRuntime.Windows.Foundation.Collections.IIterator<T> iterator)
            {
                _iterator = iterator;
            }

            private bool m_hadCurrent = true;
            private T m_current = default!;
            private bool m_isInitialized = false;

            public T Current
            {
                get
                {
                    // The enumerator has not been advanced to the first element yet.
                    if (!m_isInitialized)
                        throw new InvalidOperationException(ErrorStrings.InvalidOperation_EnumNotStarted);
                    // The enumerator has reached the end of the collection
                    if (!m_hadCurrent)
                        throw new InvalidOperationException(ErrorStrings.InvalidOperation_EnumEnded);
                    return m_current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    // The enumerator has not been advanced to the first element yet.
                    if (!m_isInitialized)
                        throw new InvalidOperationException(ErrorStrings.InvalidOperation_EnumNotStarted);
                    // The enumerator has reached the end of the collection
                    if (!m_hadCurrent)
                        throw new InvalidOperationException(ErrorStrings.InvalidOperation_EnumEnded);
                    return m_current;
                }
            }

            public bool MoveNext()
            {
                // If we've passed the end of the iteration, IEnumerable<T> should return false, while
                // IIterable will fail the interface call
                if (!m_hadCurrent)
                {
                    return false;
                }

                // IIterators start at index 0, rather than -1.  If this is the first call, we need to just
                // check HasCurrent rather than actually moving to the next element
                try
                {
                    if (!m_isInitialized)
                    {
                        m_hadCurrent = _iterator.HasCurrent;
                        m_isInitialized = true;
                    }
                    else
                    {
                        m_hadCurrent = _iterator._MoveNext();
                    }

                    // We want to save away the current value for two reasons:
                    //  1. Accessing .Current is cheap on other iterators, so having it be a property which is a
                    //     simple field access preserves the expected performance characteristics (as opposed to
                    //     triggering a COM call every time the property is accessed)
                    //
                    //  2. This allows us to preserve the same semantics as generic collection iteration when iterating
                    //     beyond the end of the collection - namely that Current continues to return the last value
                    //     of the collection
                    if (m_hadCurrent)
                    {
                        m_current = _iterator._Current;
                    }
                }
                catch (Exception e)
                {
                    // Translate E_CHANGED_STATE into an InvalidOperationException for an updated enumeration
                    if (Marshal.GetHRForException(e) == ExceptionHelpers.E_CHANGED_STATE)
                    {
                        throw new InvalidOperationException(ErrorStrings.InvalidOperation_EnumFailedVersion);
                    }
                    else
                    {
                        throw;
                    }
                }

                return m_hadCurrent;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
            }
        }

        public sealed class ToAbiHelper : global::MS.Internal.WindowsRuntime.Windows.Foundation.Collections.IIterator<T>
        {
            private readonly global::System.Collections.Generic.IEnumerator<T> m_enumerator;
            private bool m_firstItem = true;
            private bool m_hasCurrent;

            internal ToAbiHelper(global::System.Collections.Generic.IEnumerator<T> enumerator) => m_enumerator = enumerator;

            public T _Current
            {
                get
                {
                    // IEnumerator starts at item -1, while IIterators start at item 0.  Therefore, if this is the
                    // first access to the iterator we need to advance to the first item.
                    if (m_firstItem)
                    {
                        m_firstItem = false;
                        _MoveNext();
                    }

                    if (!m_hasCurrent)
                    {
                        ExceptionHelpers.ThrowExceptionForHR(ExceptionHelpers.E_BOUNDS);
                    }

                    return m_enumerator.Current;
                }
            }

            public bool HasCurrent
            {
                get
                {
                    // IEnumerator starts at item -1, while IIterators start at item 0.  Therefore, if this is the
                    // first access to the iterator we need to advance to the first item.
                    if (m_firstItem)
                    {
                        m_firstItem = false;
                        _MoveNext();
                    }

                    return m_hasCurrent;
                }
            }

            public bool _MoveNext()
            {
                try
                {
                    m_hasCurrent = m_enumerator.MoveNext();
                }
                catch (InvalidOperationException)
                {
                    ExceptionHelpers.ThrowExceptionForHR(ExceptionHelpers.E_CHANGED_STATE);
                }

                return m_hasCurrent;
            }

            public uint GetMany(ref T[] items)
            {
                if (items == null)
                {
                    return 0;
                }

                int index = 0;
                while (index < items.Length && HasCurrent)
                {
                    items[index] = _Current;
                    _MoveNext();
                    ++index;
                }

                if (typeof(T) == typeof(string))
                {
                    string[] stringItems = (items as string[])!;

                    // Fill the rest of the array with string.Empty to avoid marshaling failure
                    for (int i = index; i < items.Length; ++i)
                        stringItems[i] = string.Empty;
                }

                return (uint)index;
            }

            public object Current => _Current;

            public bool MoveNext() => _MoveNext();
        }

        [Guid("6A79E863-4300-459A-9966-CBB660963EE1")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public global::System.Delegate get_Current_0;
            internal _get_PropertyAsBoolean get_HasCurrent_1;
            public IEnumerator_Delegates.MoveNext_2 MoveNext_2;
            public IEnumerator_Delegates.GetMany_3 GetMany_3;
            public static Guid PIID = GuidGenerator.CreateIID(typeof(IEnumerator<T>));
            private static readonly Type get_Current_0_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<T>.AbiType.MakeByRefType(), typeof(int) });

            internal unsafe Vftbl(IntPtr thisPtr)
            {
                var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
                var vftbl = (IntPtr*)vftblPtr.Vftbl;
                IInspectableVftbl = Marshal.PtrToStructure<IInspectable.Vftbl>(vftblPtr.Vftbl);
                get_Current_0 = Marshal.GetDelegateForFunctionPointer(vftbl[6], get_Current_0_Type);
                get_HasCurrent_1 = Marshal.GetDelegateForFunctionPointer<_get_PropertyAsBoolean>(vftbl[7]);
                MoveNext_2 = Marshal.GetDelegateForFunctionPointer<IEnumerator_Delegates.MoveNext_2>(vftbl[8]);
                GetMany_3 = Marshal.GetDelegateForFunctionPointer<IEnumerator_Delegates.GetMany_3>(vftbl[9]);
            }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    get_Current_0 = global::System.Delegate.CreateDelegate(get_Current_0_Type, typeof(Vftbl).GetMethod("Do_Abi_get_Current_0", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    get_HasCurrent_1 = Do_Abi_get_HasCurrent_1,
                    MoveNext_2 = Do_Abi_MoveNext_2,
                    GetMany_3 = Do_Abi_GetMany_3
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 4);
                Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);
                nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.get_Current_0);
                nativeVftbl[7] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.get_HasCurrent_1);
                nativeVftbl[8] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.MoveNext_2);
                nativeVftbl[9] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.GetMany_3);

                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static ConditionalWeakTable<global::System.Collections.Generic.IEnumerator<T>, ToAbiHelper> _adapterTable =
                new ConditionalWeakTable<global::System.Collections.Generic.IEnumerator<T>, ToAbiHelper>();

            private static ToAbiHelper FindAdapter(IntPtr thisPtr)
            {
                var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Generic.IEnumerator<T>>(thisPtr);
                return _adapterTable.GetValue(__this, (enumerator) => new ToAbiHelper(enumerator));
            }

            private static unsafe int Do_Abi_MoveNext_2(IntPtr thisPtr, out byte __return_value__)
            {
                bool ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = FindAdapter(thisPtr)._MoveNext();
                    __return_value__ = (byte)(____return_value__ ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_GetMany_3(IntPtr thisPtr, int __itemsSize, IntPtr items, out uint __return_value__)
            {
                uint ____return_value__ = default;

                __return_value__ = default;
                T[] __items = Marshaler<T>.FromAbiArray((__itemsSize, items));

                try
                {
                    ____return_value__ = FindAdapter(thisPtr).GetMany(ref __items);
                    Marshaler<T>.CopyManagedArray(__items, items);
                    __return_value__ = ____return_value__;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_Current_0<TAbi>(void* thisPtr, out TAbi __return_value__)
            {
                T ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = FindAdapter(new IntPtr(thisPtr))._Current;
                    __return_value__ = (TAbi)Marshaler<T>.FromManaged(____return_value__);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            private static unsafe int Do_Abi_get_HasCurrent_1(IntPtr thisPtr, out byte __return_value__)
            {
                bool ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = FindAdapter(thisPtr).HasCurrent;
                    __return_value__ = (byte)(____return_value__ ? 1 : 0);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var vftblT = new Vftbl(thisPtr);
            return ObjectReference<Vftbl>.FromAbi(thisPtr, vftblT.IInspectableVftbl.IUnknownVftbl, vftblT);
        }
        public static Guid PIID = Vftbl.PIID;

        public static implicit operator IEnumerator<T>(IObjectReference obj) => (obj != null) ? new IEnumerator<T>(obj) : null;
        public static implicit operator IEnumerator<T>(ObjectReference<Vftbl> obj) => (obj != null) ? new IEnumerator<T>(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }

        public IntPtr ThisPtr => _obj.ThisPtr;

        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();

        public IEnumerator(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IEnumerator(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
            _FromIterator = new FromAbiHelper(this);
        }
        FromAbiHelper _FromIterator;

        public unsafe bool _MoveNext()
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.MoveNext_2(ThisPtr, out __retval));
            return __retval != 0;
        }

        public unsafe uint GetMany(ref T[] items)
        {
            object __items = default;
            int __items_length = default;
            IntPtr __items_data = default;
            uint __retval = default;
            try
            {
                __items = Marshaler<T>.CreateMarshalerArray(items);
                (__items_length, __items_data) = Marshaler<T>.GetAbiArray(__items);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetMany_3(ThisPtr, __items_length, __items_data, out __retval));
                items = Marshaler<T>.FromAbiArray((__items_length, __items_data));
                return __retval;
            }
            finally
            {
                Marshaler<T>.DisposeMarshalerArray(__items);
            }
        }

        public unsafe T _Current
        {
            get
            {
                var __params = new object[] { ThisPtr, null };
                try
                {
                    _obj.Vftbl.get_Current_0.DynamicInvokeAbi(__params);
                    return Marshaler<T>.FromAbi(__params[1]);
                }
                finally
                {
                    Marshaler<T>.DisposeAbi(__params[1]);
                }
            }
        }

        public unsafe bool HasCurrent
        {
            get
            {
                byte __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_HasCurrent_1(ThisPtr, out __retval));
                return __retval != 0;
            }
        }

        public bool MoveNext() => _FromIterator.MoveNext();
        public void Reset() => _FromIterator.Reset();
        public void Dispose() => _FromIterator.Dispose();
        public T Current => _FromIterator.Current;
        object IEnumerator.Current => Current;
    }
    internal static class IEnumerator_Delegates
    {
        public unsafe delegate int MoveNext_2(IntPtr thisPtr, out byte __return_value__);
        public unsafe delegate int GetMany_3(IntPtr thisPtr, int __itemsSize, IntPtr items, out uint __return_value__);
    }
}
