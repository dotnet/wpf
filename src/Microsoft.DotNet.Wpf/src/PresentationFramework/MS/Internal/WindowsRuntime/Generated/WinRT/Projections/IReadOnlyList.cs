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
    [Guid("BBE1FA4C-B0E3-4583-BAEF-1F1B2E483E56")]
    interface IVectorView<T> : IIterable<T>
    {
        T GetAt(uint index);
        bool IndexOf(T value, out uint index);
        uint GetMany(uint startIndex, ref T[] items);
        uint Size { get; }
    }
}

namespace MS.Internal.WindowsRuntime.ABI.System.Collections.Generic
{
    using global::System;
    using global::System.Runtime.CompilerServices;

    [Guid("BBE1FA4C-B0E3-4583-BAEF-1F1B2E483E56")]
    internal class IReadOnlyList<T> : global::System.Collections.Generic.IReadOnlyList<T>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IReadOnlyList<T> obj) =>
            obj is null ? null : ComWrappersSupport.CreateCCWForObject(obj).As<Vftbl>(GuidGenerator.GetIID(typeof(IReadOnlyList<T>)));

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static global::System.Collections.Generic.IReadOnlyList<T> FromAbi(IntPtr thisPtr) =>
            thisPtr == IntPtr.Zero ? null : new IReadOnlyList<T>(ObjRefFromAbi(thisPtr));

        public static IntPtr FromManaged(global::System.Collections.Generic.IReadOnlyList<T> value) =>
            (value is null) ? IntPtr.Zero : CreateMarshaler(value).GetRef();

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::MS.Internal.WindowsRuntime.Windows.Foundation.Collections.IVectorView<T>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IReadOnlyList<T>));

        public class FromAbiHelper : global::System.Collections.Generic.IReadOnlyList<T>
        {
            private readonly global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IReadOnlyList<T> _vectorView;
            private readonly global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IEnumerable<T> _enumerable;

            public FromAbiHelper(IObjectReference obj) :
                this(new global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IReadOnlyList<T>(obj))
            {
            }

            public FromAbiHelper(global::MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IReadOnlyList<T> vectorView)
            {
                _vectorView = vectorView;
                _enumerable = new ABI.System.Collections.Generic.IEnumerable<T>(vectorView.ObjRef);
            }

            public int Count
            {
                get
                {
                    uint size = _vectorView.Size;
                    if (((uint)int.MaxValue) < size)
                    {
                        throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
                    }

                    return (int)size;
                }
            }

            public T this[int index] { get => Indexer_Get(index); }

            private T Indexer_Get(int index)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                try
                {
                    return _vectorView.GetAt((uint)index);

                    // We delegate bounds checking to the underlying collection and if it detected a fault,
                    // we translate it to the right exception:
                }
                catch (Exception ex)
                {
                    if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                        throw new ArgumentOutOfRangeException(nameof(index));

                    throw;
                }
            }

            public global::System.Collections.Generic.IEnumerator<T> GetEnumerator() => _enumerable.GetEnumerator();

            global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public sealed class ToAbiHelper : global::MS.Internal.WindowsRuntime.Windows.Foundation.Collections.IVectorView<T>
        {
            private readonly global::System.Collections.Generic.IReadOnlyList<T> _list;

            internal ToAbiHelper(global::System.Collections.Generic.IReadOnlyList<T> list) => _list = list;

            global::MS.Internal.WindowsRuntime.Windows.Foundation.Collections.IIterator<T> global::MS.Internal.WindowsRuntime.Windows.Foundation.Collections.IIterable<T>.First() =>
                new IEnumerator<T>.ToAbiHelper(_list.GetEnumerator());

            private static void EnsureIndexInt32(uint index, int limit = int.MaxValue)
            {
                // We use '<=' and not '<' because int.MaxValue == index would imply
                // that Size > int.MaxValue:
                if (((uint)int.MaxValue) <= index || index >= (uint)limit)
                {
                    Exception e = new ArgumentOutOfRangeException(nameof(index), ErrorStrings.ArgumentOutOfRange_IndexLargerThanMaxValue);
                    e.SetHResult(ExceptionHelpers.E_BOUNDS);
                    throw e;
                }
            }

            public T GetAt(uint index)
            {
                EnsureIndexInt32(index, _list.Count);

                try
                {
                    return _list[(int)index];
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    ex.SetHResult(ExceptionHelpers.E_BOUNDS);
                    throw;
                }
            }

            public uint Size => (uint)_list.Count;

            public bool IndexOf(T value, out uint index)
            {
                int ind = -1;
                int max = _list.Count;
                for (int i = 0; i < max; i++)
                {
                    if (EqualityComparer<T>.Default.Equals(value, _list[i]))
                    {
                        ind = i;
                        break;
                    }
                }

                if (-1 == ind)
                {
                    index = 0;
                    return false;
                }

                index = (uint)ind;
                return true;
            }

            public uint GetMany(uint startIndex, ref T[] items)
            {
                // Spec says "calling GetMany with startIndex equal to the length of the vector
                // (last valid index + 1) and any specified capacity will succeed and return zero actual
                // elements".
                if (startIndex == _list.Count)
                    return 0;

                EnsureIndexInt32(startIndex, _list.Count);

                if (items == null)
                {
                    return 0;
                }

                uint itemCount = Math.Min((uint)items.Length, (uint)_list.Count - startIndex);

                for (uint i = 0; i < itemCount; ++i)
                {
                    items[i] = _list[(int)(i + startIndex)];
                }

                if (typeof(T) == typeof(string))
                {
                    string[] stringItems = (items as string[])!;

                    // Fill in the rest of the array with string.Empty to avoid marshaling failure
                    for (uint i = itemCount; i < items.Length; ++i)
                        stringItems[i] = string.Empty;
                }

                return itemCount;
            }
        }

        [Guid("BBE1FA4C-B0E3-4583-BAEF-1F1B2E483E56")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public global::System.Delegate GetAt_0;
            internal _get_PropertyAsUInt32 get_Size_1;
            public global::System.Delegate IndexOf_2;
            public IReadOnlyList_Delegates.GetMany_3 GetMany_3;
            public static Guid PIID = GuidGenerator.CreateIID(typeof(IReadOnlyList<T>));
            private static readonly Type GetAt_0_Type = Expression.GetDelegateType(new Type[] { typeof(void*), typeof(uint), Marshaler<T>.AbiType.MakeByRefType(), typeof(int) });
            private static readonly Type IndexOf_2_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<T>.AbiType, typeof(uint).MakeByRefType(), typeof(byte).MakeByRefType(), typeof(int) });

            internal unsafe Vftbl(IntPtr thisPtr)
            {
                var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
                var vftbl = (IntPtr*)vftblPtr.Vftbl;
                IInspectableVftbl = Marshal.PtrToStructure<IInspectable.Vftbl>(vftblPtr.Vftbl);
                GetAt_0 = Marshal.GetDelegateForFunctionPointer(vftbl[6], GetAt_0_Type);
                get_Size_1 = Marshal.GetDelegateForFunctionPointer<_get_PropertyAsUInt32>(vftbl[7]);
                IndexOf_2 = Marshal.GetDelegateForFunctionPointer(vftbl[8], IndexOf_2_Type);
                GetMany_3 = Marshal.GetDelegateForFunctionPointer<IReadOnlyList_Delegates.GetMany_3>(vftbl[9]);
            }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    GetAt_0 = global::System.Delegate.CreateDelegate(GetAt_0_Type, typeof(Vftbl).GetMethod("Do_Abi_GetAt_0", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    get_Size_1 = Do_Abi_get_Size_1,
                    IndexOf_2 = global::System.Delegate.CreateDelegate(IndexOf_2_Type, typeof(Vftbl).GetMethod("Do_Abi_IndexOf_2", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    GetMany_3 = Do_Abi_GetMany_3
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 4);
                Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);
                nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.GetAt_0);
                nativeVftbl[7] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.get_Size_1);
                nativeVftbl[8] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.IndexOf_2);
                nativeVftbl[9] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.GetMany_3);

                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static ConditionalWeakTable<global::System.Collections.Generic.IReadOnlyList<T>, ToAbiHelper> _adapterTable =
                new ConditionalWeakTable<global::System.Collections.Generic.IReadOnlyList<T>, ToAbiHelper>();

            private static ToAbiHelper FindAdapter(IntPtr thisPtr)
            {
                var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Generic.IReadOnlyList<T>>(thisPtr);
                return _adapterTable.GetValue(__this, (list) => new ToAbiHelper(list));
            }

            private static unsafe int Do_Abi_GetAt_0<TAbi>(void* thisPtr, uint index, out TAbi __return_value__)
            {
                T ____return_value__ = default;
                __return_value__ = default;
                try
                {
                    ____return_value__ = FindAdapter(new IntPtr(thisPtr)).GetAt(index);
                    __return_value__ = (TAbi)Marshaler<T>.FromManaged(____return_value__);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IndexOf_2<TAbi>(void* thisPtr, TAbi value, out uint index, out byte __return_value__)
            {
                bool ____return_value__ = default;

                index = default;
                __return_value__ = default;
                uint __index = default;

                try
                {
                    ____return_value__ = FindAdapter(new IntPtr(thisPtr)).IndexOf(Marshaler<T>.FromAbi(value), out __index);
                    index = __index;
                    __return_value__ = (byte)(____return_value__ ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_GetMany_3(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, out uint __return_value__)
            {
                uint ____return_value__ = default;

                __return_value__ = default;
                T[] __items = Marshaler<T>.FromAbiArray((__itemsSize, items));

                try
                {
                    ____return_value__ = FindAdapter(thisPtr).GetMany(startIndex, ref __items);
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
            private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, out uint __return_value__)
            {
                uint ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = FindAdapter(thisPtr).Size;
                    __return_value__ = ____return_value__;

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

        public static implicit operator IReadOnlyList<T>(IObjectReference obj) => (obj != null) ? new IReadOnlyList<T>(obj) : null;
        public static implicit operator IReadOnlyList<T>(ObjectReference<Vftbl> obj) => (obj != null) ? new IReadOnlyList<T>(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }

        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IReadOnlyList(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IReadOnlyList(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
            _FromVectorView = new FromAbiHelper(this);
        }
        FromAbiHelper _FromVectorView;

        public unsafe T GetAt(uint index)
        {
            var __params = new object[] { ThisPtr, index, null };
            try
            {
                _obj.Vftbl.GetAt_0.DynamicInvokeAbi(__params);
                return Marshaler<T>.FromAbi(__params[2]);
            }
            finally
            {
                Marshaler<T>.DisposeAbi(__params[2]);
            }
        }

        public unsafe bool IndexOf(T value, out uint index)
        {
            object __value = default;
            var __params = new object[] { ThisPtr, null, null, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler(value);
                __params[1] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.IndexOf_2.DynamicInvokeAbi(__params);
                index = (uint)__params[2];
                return (byte)__params[3] != 0;
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        public unsafe uint GetMany(uint startIndex, ref T[] items)
        {
            object __items = default;
            int __items_length = default;
            IntPtr __items_data = default;
            uint __retval = default;
            try
            {
                __items = Marshaler<T>.CreateMarshalerArray(items);
                (__items_length, __items_data) = Marshaler<T>.GetAbiArray(__items);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetMany_3(ThisPtr, startIndex, __items_length, __items_data, out __retval));
                items = Marshaler<T>.FromAbiArray((__items_length, __items_data));
                return __retval;
            }
            finally
            {
                Marshaler<T>.DisposeMarshalerArray(__items);
            }
        }

        public unsafe uint Size
        {
            get
            {
                uint __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Size_1(ThisPtr, out __retval));
                return __retval;
            }
        }

        public int Count => _FromVectorView.Count;

        public T this[int index] => _FromVectorView[index];

        public global::System.Collections.Generic.IEnumerator<T> GetEnumerator() => _FromVectorView.GetEnumerator();

        IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
    internal static class IReadOnlyList_Delegates
    {
        public unsafe delegate int GetMany_3(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, out uint __return_value__);
    }
}
