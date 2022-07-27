// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Linq.Expressions;
using System.Collections.Concurrent;
using WinRT.Interop;

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value

namespace WinRT
{
    internal static class MarshalExtensions
    {
        public static void Dispose(this GCHandle handle)
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }

    // TODO: minimize heap allocations for marshalers by eliminating explicit try/finally
    // and adopting ref structs with non-IDisposable Dispose and 'using var ...' pattern,
    // as well as passing marshalers to FromAbi by ref so they can be conditionally disposed.
    internal class MarshalString
    {
        public unsafe struct HStringHeader // sizeof(HSTRING_HEADER)
        {
            public fixed byte Reserved[24];
        };
        public HStringHeader _header;
        public GCHandle _gchandle;
        public IntPtr _handle;

        public void Dispose()
        {
            _gchandle.Dispose();
        }

        public static unsafe MarshalString CreateMarshaler(string value)
        {
            if (value == null) return null;

            var m = new MarshalString();
            Func<bool> dispose = () => { m.Dispose(); return false; };
            try
            {
                m._gchandle = GCHandle.Alloc(value, GCHandleType.Pinned);
                fixed (void* chars = value, header = &m._header, handle = &m._handle)
                {
                    Marshal.ThrowExceptionForHR(Platform.WindowsCreateStringReference(
                        (char*)chars, value.Length, (IntPtr*)header, (IntPtr*)handle));
                };
                return m;
            }
            catch (Exception) when (dispose())
            {
                // Will never execute
                return default;
            }
        }

        public static IntPtr GetAbi(MarshalString m) => m is null ? IntPtr.Zero : m._handle;

        public static IntPtr GetAbi(object box) => box is null ? IntPtr.Zero : ((MarshalString)box)._handle;

        public static void DisposeMarshaler(MarshalString m) => m?.Dispose();

        public static void DisposeMarshaler(object box)
        {
            if (box != null)
                DisposeMarshaler(((MarshalString)box));
        }

        public static void DisposeAbi(IntPtr hstring)
        {
            if (hstring != IntPtr.Zero)
                Platform.WindowsDeleteString(hstring);
        }

        public static void DisposeAbi(object abi)
        {
            if (abi != null)
                DisposeAbi(((IntPtr)abi));
        }

        public static unsafe string FromAbi(IntPtr value)
        {
            if (value == IntPtr.Zero)
                return "";
            uint length;
            var buffer = Platform.WindowsGetStringRawBuffer(value, &length);
            return new string(buffer, 0, (int)length);
        }

        public static unsafe IntPtr FromManaged(string value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            IntPtr handle;
            Marshal.ThrowExceptionForHR(
                Platform.WindowsCreateString(value, value.Length, &handle));
            return handle;
        }

        internal struct MarshalerArray
        {
            public void Dispose()
            {
                if (_marshalers != null)
                {
                    foreach (var marshaler in _marshalers)
                    {
                        marshaler?.Dispose();
                    }
                }
                if (_array != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(_array);
                }
            }

            public IntPtr _array;
            public MarshalString[] _marshalers;
        }

        public static unsafe MarshalerArray CreateMarshalerArray(string[] array)
        {
            var m = new MarshalerArray();
            if (array is null)
            {
                return m;
            }
            Func<bool> dispose = () => { m.Dispose(); return false; };
            try
            {
                var length = array.Length;
                m._array = Marshal.AllocCoTaskMem(length * Marshal.SizeOf<IntPtr>());
                m._marshalers = new MarshalString[length];
                var elements = (IntPtr*)m._array.ToPointer();
                for (int i = 0; i < length; i++)
                {
                    m._marshalers[i] = MarshalString.CreateMarshaler(array[i]);
                    elements[i] = MarshalString.GetAbi(m._marshalers[i]);
                };
                return m;
            }
            catch (Exception) when (dispose())
            {
                // Will never execute
                return default;
            }
        }

        public static (int length, IntPtr data) GetAbiArray(object box)
        {
            var m = (MarshalerArray)box;
            return (m._marshalers?.Length ?? 0, m._array);
        }

        public static unsafe string[] FromAbiArray(object box)
        {
            if (box is null)
            {
                return null;
            }
            var abi = ((int length, IntPtr data))box;
            string[] array = new string[abi.length];
            var data = (IntPtr*)abi.data.ToPointer();
            for (int i = 0; i < abi.length; i++)
            {
                array[i] = MarshalString.FromAbi(data[i]);
            }
            return array;
        }

        public static unsafe void CopyAbiArray(string[] array, object box)
        {
            var abi = ((int length, IntPtr data))box;
            var data = (IntPtr*)abi.data.ToPointer();
            for (int i = 0; i < abi.length; i++)
            {
                array[i] = MarshalString.FromAbi(data[i]);
            }
        }

        public static unsafe (int length, IntPtr data) FromManagedArray(string[] array)
        {
            if (array is null)
            {
                return (0, IntPtr.Zero);
            }
            IntPtr data = IntPtr.Zero;
            int i = 0;
            Func<bool> dispose = () =>
            {
                DisposeAbiArray((i, data));
                i = 0;
                data = IntPtr.Zero;
                return false;
            };
            try
            {
                var length = array.Length;
                data = Marshal.AllocCoTaskMem(length * Marshal.SizeOf<IntPtr>());
                var elements = (IntPtr*)data;
                for (i = 0; i < length; i++)
                {
                    elements[i] = MarshalString.FromManaged(array[i]);
                };
            }
            catch (Exception) when (dispose())
            {
                // Will never execute
                return default;
            }
            return (i, data);
        }

        public static unsafe void CopyManagedArray(string[] array, IntPtr data)
        {
            if (array is null)
            {
                return;
            }
            DisposeAbiArrayElements((array.Length, data));
            int i = 0;
            Func<bool> dispose = () => { DisposeAbiArrayElements((i, data)); return false; };
            try
            {
                var length = array.Length;
                var elements = (IntPtr*)data;
                for (i = 0; i < length; i++)
                {
                    elements[i] = MarshalString.FromManaged(array[i]);
                };
            }
            catch (Exception) when (dispose())
            {
            }
        }

        public static void DisposeMarshalerArray(object box)
        {
            if (box != null)
                ((MarshalerArray)box).Dispose();
        }

        public static unsafe void DisposeAbiArrayElements((int length, IntPtr data) abi)
        {
            var elements = (IntPtr*)abi.data;
            for (int i = 0; i < abi.length; i++)
            {
                DisposeAbi(elements[i]);
            }
        }

        public static unsafe void DisposeAbiArray(object box)
        {
            if (box == null) return;
            var abi = ((int length, IntPtr data))box;
            DisposeAbiArrayElements(abi);
            Marshal.FreeCoTaskMem(abi.data);
        }
    }

    internal struct MarshalBlittable<T>
    {
        internal struct MarshalerArray
        {
            public MarshalerArray(Array array) => _gchandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            public void Dispose() => _gchandle.Dispose();

            public GCHandle _gchandle;
        };

        public static MarshalerArray CreateMarshalerArray(Array array) => new MarshalerArray(array);

        public static (int length, IntPtr data) GetAbiArray(object box)
        {
            var m = (MarshalerArray)box;
            return (((Array)m._gchandle.Target).Length, m._gchandle.AddrOfPinnedObject());
        }

        public static unsafe T[] FromAbiArray(object box)
        {
            if (box is null)
            {
                return null;
            }
            var abi = ((int length, IntPtr data))box;
            var abiSpan = new ReadOnlySpan<T>(abi.data.ToPointer(), abi.length);
            return abiSpan.ToArray();
        }

        public static unsafe (int length, IntPtr data) FromManagedArray(Array array)
        {
            if (array is null)
            {
                return (0, IntPtr.Zero);
            }
            var length = array.Length;
            var byte_length = length * Marshal.SizeOf<T>();
            var data = Marshal.AllocCoTaskMem(byte_length);
            CopyManagedArray(array, data);
            return (length, data);
        }

        public static unsafe void CopyManagedArray(Array array, IntPtr data)
        {
            if (array is null)
            {
                return;
            }
            var length = array.Length;
            var byte_length = length * Marshal.SizeOf<T>();
            var array_handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            var array_data = array_handle.AddrOfPinnedObject();
            Buffer.MemoryCopy(array_data.ToPointer(), data.ToPointer(), byte_length, byte_length);
            array_handle.Free();
        }

        public static void DisposeMarshalerArray(object box)
        {
            if (box != null)
                ((MarshalerArray)box).Dispose();
        }

        public static void DisposeAbiArray(object box)
        {
            if (box == null) return;
            var abi = ((int length, IntPtr data))box;
            Marshal.FreeCoTaskMem(abi.data);
        }
    }

    internal class MarshalGeneric<T>
    {
        protected static readonly Type HelperType = typeof(T).GetHelperType();
        protected static readonly Type AbiType = typeof(T).GetAbiType();
        protected static readonly Type MarshalerType = typeof(T).GetMarshalerType();

        static MarshalGeneric()
        {
            CreateMarshaler = BindCreateMarshaler();
            GetAbi = BindGetAbi();
            FromAbi = BindFromAbi();
            CopyAbi = BindCopyAbi();
            FromManaged = BindFromManaged();
            CopyManaged = BindCopyManaged();
            DisposeMarshaler = BindDisposeMarshaler();
        }

        public static readonly Func<T, object> CreateMarshaler;
        private static Func<T, object> BindCreateMarshaler()
        {
            var parms = new[] { Expression.Parameter(typeof(T), "arg") };
            return Expression.Lambda<Func<T, object>>(
                Expression.Convert(Expression.Call(HelperType.GetMethod("CreateMarshaler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static), parms),
                    typeof(object)), parms).Compile();
        }

        public static readonly Func<object, object> GetAbi;
        private static Func<object, object> BindGetAbi()
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(Expression.Call(HelperType.GetMethod("GetAbi", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                    new[] { Expression.Convert(parms[0], MarshalerType) }),
                        typeof(object)), parms).Compile();
        }

        public static readonly Action<object, IntPtr> CopyAbi;
        private static Action<object, IntPtr> BindCopyAbi()
        {
            var copyAbi = HelperType.GetMethod("CopyAbi", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (copyAbi == null) return null;
            var parms = new[] { Expression.Parameter(typeof(object), "arg"), Expression.Parameter(typeof(IntPtr), "dest") };
            return Expression.Lambda<Action<object, IntPtr>>(
                Expression.Call(copyAbi,
                    new Expression[] { Expression.Convert(parms[0], MarshalerType), parms[1] }), parms).Compile();
        }

        public static readonly Func<object, T> FromAbi;
        private static Func<object, T> BindFromAbi()
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Func<object, T>>(
                Expression.Call(HelperType.GetMethod("FromAbi", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                    new[] { Expression.Convert(parms[0], AbiType) }), parms).Compile();
        }

        public static readonly Func<T, object> FromManaged;
        private static Func<T, object> BindFromManaged()
        {
            var parms = new[] { Expression.Parameter(typeof(T), "arg") };
            return Expression.Lambda<Func<T, object>>(
                Expression.Convert(Expression.Call(HelperType.GetMethod("FromManaged", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static), parms),
                    typeof(object)), parms).Compile();
        }

        public static readonly Action<T, IntPtr> CopyManaged;
        private static Action<T, IntPtr> BindCopyManaged()
        {
            var copyManaged = HelperType.GetMethod("CopyManaged", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (copyManaged == null) return null;
            var parms = new[] { Expression.Parameter(typeof(T), "arg"), Expression.Parameter(typeof(IntPtr), "dest") };
            return Expression.Lambda<Action<T, IntPtr>>(
                Expression.Call(copyManaged, parms), parms).Compile();
        }

        public static readonly Action<object> DisposeMarshaler;
        private static Action<object> BindDisposeMarshaler()
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Action<object>>(
                Expression.Call(HelperType.GetMethod("DisposeMarshaler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                    new[] { Expression.Convert(parms[0], MarshalerType) }), parms).Compile();
        }
    }

    internal class MarshalNonBlittable<T> : MarshalGeneric<T>
    {
        internal struct MarshalerArray
        {
            public void Dispose()
            {
                if (_marshalers != null)
                {
                    foreach (var marshaler in _marshalers)
                    {
                        Marshaler<T>.DisposeMarshaler(marshaler);
                    }
                }
                if (_array != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(_array);
                }
            }

            public IntPtr _array;
            public object[] _marshalers;
        }

        public static unsafe MarshalerArray CreateMarshalerArray(T[] array)
        {
            MarshalerArray m = new MarshalerArray();
            if (array is null)
            {
                return m;
            }
            Func<bool> dispose = () => { m.Dispose(); return false; };
            try
            {
                int length = array.Length;
                var abi_element_size = Marshal.SizeOf(HelperType);
                var byte_length = length * abi_element_size;
                m._array = Marshal.AllocCoTaskMem(byte_length);
                m._marshalers = new object[length];
                var element = (byte*)m._array.ToPointer();
                for (int i = 0; i < length; i++)
                {
                    m._marshalers[i] = Marshaler<T>.CreateMarshaler(array[i]);
                    Marshaler<T>.CopyAbi(m._marshalers[i], (IntPtr)element);
                    element += abi_element_size;
                }
                return m;
            }
            catch (Exception) when (dispose())
            {
                // Will never execute
                return default;
            }
        }

        public static (int length, IntPtr data) GetAbiArray(object box)
        {
            var m = (MarshalerArray)box;
            return (m._marshalers?.Length ?? 0, m._array);
        }

        public static unsafe T[] FromAbiArray(object box)
        {
            if (box is null)
            {
                return null;
            }
            var abi = ((int length, IntPtr data))box;
            var array = new T[abi.length];
            var data = (byte*)abi.data.ToPointer();
            var abi_element_size = Marshal.SizeOf(HelperType);
            for (int i = 0; i < abi.length; i++)
            {
                var abi_element = Marshal.PtrToStructure((IntPtr)data, HelperType);
                array[i] = Marshaler<T>.FromAbi(abi_element);
                data += abi_element_size;
            }
            return array;
        }

        public static unsafe void CopyAbiArray(T[] array, object box)
        {
            var abi = ((int length, IntPtr data))box;
            if (abi.data == IntPtr.Zero)
            {
                return;
            }
            var data = (byte*)abi.data.ToPointer();
            var abi_element_size = Marshal.SizeOf(HelperType);
            for (int i = 0; i < abi.length; i++)
            {
                var abi_element = Marshal.PtrToStructure((IntPtr)data, HelperType);
                array[i] = Marshaler<T>.FromAbi(abi_element);
                data += abi_element_size;
            }
        }

        public static unsafe (int length, IntPtr data) FromManagedArray(T[] array)
        {
            if (array is null)
            {
                return (0, IntPtr.Zero);
            }
            IntPtr data = IntPtr.Zero;
            int i = 0;
            Func<bool> dispose = () =>
            {
                DisposeAbiArray((i, data));
                i = 0;
                data = IntPtr.Zero;
                return false;
            };
            try
            {
                int length = array.Length;
                var abi_element_size = Marshal.SizeOf(HelperType);
                var byte_length = length * abi_element_size;
                data = Marshal.AllocCoTaskMem(byte_length);
                var bytes = (byte*)data.ToPointer();
                for (i = 0; i < length; i++)
                {
                    Marshaler<T>.CopyManaged(array[i], (IntPtr)bytes);
                    bytes += abi_element_size;
                }
            }
            catch (Exception) when (dispose())
            {
                // Will never execute
                return default;
            }
            return (i, data);
        }

        public static unsafe void CopyManagedArray(T[] array, IntPtr data)
        {
            if (array is null)
            {
                return;
            }
            DisposeAbiArrayElements((array.Length, data));
            int i = 0;
            Func<bool> dispose = () => { DisposeAbiArrayElements((i, data)); return false; };
            try
            {
                int length = array.Length;
                var abi_element_size = Marshal.SizeOf(HelperType);
                var byte_length = length * abi_element_size;
                var bytes = (byte*)data.ToPointer();
                for (i = 0; i < length; i++)
                {
                    Marshaler<T>.CopyManaged(array[i], (IntPtr)bytes);
                    bytes += abi_element_size;
                }
            }
            catch (Exception) when (dispose())
            {
            }
        }

        public static void DisposeMarshalerArray(object box) => ((MarshalerArray)box).Dispose();

        public static unsafe void DisposeAbiArrayElements((int length, IntPtr data) abi)
        {
            var data = (byte*)abi.data.ToPointer();
            var abi_element_size = Marshal.SizeOf(HelperType);
            for (int i = 0; i < abi.length; i++)
            {
                var abi_element = Marshal.PtrToStructure((IntPtr)data, HelperType);
                Marshaler<T>.DisposeAbi(abi_element);
                data += abi_element_size;
            }
        }

        public static unsafe void DisposeAbiArray(object box)
        {
            if (box == null) return;
            var abi = ((int length, IntPtr data))box;
            if (abi.data == IntPtr.Zero) return;
            DisposeAbiArrayElements(abi);
            Marshal.FreeCoTaskMem(abi.data);
        }
    }

    internal class MarshalInterfaceHelper<T>
    {
        internal struct MarshalerArray
        {
            public void Dispose()
            {
                if (_marshalers != null)
                {
                    foreach (var marshaler in _marshalers)
                    {
                        DisposeMarshaler(marshaler);
                    }
                }
                if (_array != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(_array);
                }
            }

            public IntPtr _array;
            public IObjectReference[] _marshalers;
        }

        public static unsafe MarshalerArray CreateMarshalerArray(T[] array, Func<T, IObjectReference> createMarshaler)
        {
            MarshalerArray m = new MarshalerArray();
            if (array is null)
            {
                return m;
            }
            Func<bool> dispose = () => { m.Dispose(); return false; };
            try
            {
                int length = array.Length;
                var byte_length = length * IntPtr.Size;
                m._array = Marshal.AllocCoTaskMem(byte_length);
                m._marshalers = new IObjectReference[length];
                var element = (IntPtr*)m._array.ToPointer();
                for (int i = 0; i < length; i++)
                {
                    m._marshalers[i] = createMarshaler(array[i]);
                    element[i] = GetAbi(m._marshalers[i]);
                }
                return m;
            }
            catch (Exception) when (dispose())
            {
                // Will never execute
                return default;
            }
        }

        public static (int length, IntPtr data) GetAbiArray(object box)
        {
            var m = (MarshalerArray)box;
            return (m._marshalers?.Length ?? 0, m._array);
        }

        public static unsafe T[] FromAbiArray(object box, Func<IntPtr, T> fromAbi)
        {
            if (box is null)
            {
                return null;
            }
            var abi = ((int length, IntPtr data))box;
            var array = new T[abi.length];
            var data = (IntPtr*)abi.data.ToPointer();
            for (int i = 0; i < abi.length; i++)
            {
                array[i] = fromAbi(data[i]);
            }
            return array;
        }

        public static unsafe (int length, IntPtr data) FromManagedArray(T[] array, Func<T, IntPtr> fromManaged)
        {
            if (array is null)
            {
                return (0, IntPtr.Zero);
            }
            IntPtr data = IntPtr.Zero;
            int i = 0;
            Func<bool> dispose = () =>
            {
                DisposeAbiArray((i, data));
                i = 0;
                data = IntPtr.Zero;
                return false;
            };
            try
            {
                int length = array.Length;
                var byte_length = length * IntPtr.Size;
                data = Marshal.AllocCoTaskMem(byte_length);
                var native = (IntPtr*)data.ToPointer();
                for (i = 0; i < length; i++)
                {
                    native[i] = fromManaged(array[i]);
                }
            }
            catch (Exception) when (dispose())
            {
                // Will never execute
                return default;
            }
            return (i, data);
        }

        public static unsafe void CopyManagedArray(T[] array, IntPtr data, Action<T, IntPtr> copyManaged)
        {
            if (array is null)
            {
                return;
            }
            DisposeAbiArrayElements((array.Length, data));
            int i = 0;
            Func<bool> dispose = () => { DisposeAbiArrayElements((i, data)); return false; };
            try
            {
                int length = array.Length;
                var byte_length = length * IntPtr.Size;
                var bytes = (byte*)data.ToPointer();
                for (i = 0; i < length; i++)
                {
                    copyManaged(array[i], (IntPtr)bytes);
                    bytes += IntPtr.Size;
                }
            }
            catch (Exception) when (dispose())
            {
            }
        }

        public static void DisposeMarshalerArray(object box) => ((MarshalerArray)box).Dispose();

        public static unsafe void DisposeAbiArrayElements((int length, IntPtr data) abi)
        {
            var data = (IntPtr*)abi.data.ToPointer();
            for (int i = 0; i < abi.length; i++)
            {
                DisposeAbi(data[i]);
            }
        }

        public static unsafe void DisposeAbiArray(object box)
        {
            if (box == null) return;
            var abi = ((int length, IntPtr data))box;
            if (abi.data == IntPtr.Zero) return;
            DisposeAbiArrayElements(abi);
            Marshal.FreeCoTaskMem(abi.data);
        }

        public static IntPtr GetAbi(IObjectReference objRef)
        {
            return objRef?.ThisPtr ?? IntPtr.Zero;
        }

        public static void DisposeMarshaler(IObjectReference objRef)
        {
            objRef?.Dispose();
        }

        public static void DisposeAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return;
            // TODO: this should be a direct v-table call when function pointers are a thing
            ObjectReference<WinRT.Interop.IUnknownVftbl>.Attach(ref ptr).Dispose();
        }
    }

    internal struct MarshalInterface<T>
    {
        private static readonly Type HelperType = typeof(T).GetHelperType();
        private static Func<T, IObjectReference> _ToAbi;
        private static Func<IntPtr, T> _FromAbi;
        private static Func<IObjectReference, IObjectReference> _As;

        public static T FromAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return (T)(object)null;
            }

            object primaryManagedWrapper = MarshalInspectable.FromAbi(ptr);

            if (primaryManagedWrapper is T obj)
            {
                return obj;
            }
            // If the metadata type doesn't implement the interface, then create a tear-off RCW.
            // TODO: Uniqueness of tear-offs?
            if (_FromAbi == null)
            {
                _FromAbi = BindFromAbi();
            }
            return _FromAbi(ptr);
        }

        public static IObjectReference CreateMarshaler(T value)
        {
            if (value is null)
            {
                return null;
            }

            // If the value passed in is the native implementation of the interface
            // use the ToAbi delegate since it will be faster than reflection.
            if (value.GetType() == HelperType)
            {
                if (_ToAbi == null)
                {
                    _ToAbi = BindToAbi();
                }
                return _ToAbi(value);
            }

            if (_As is null)
            {
                _As = BindAs();
            }

            var inspectable = MarshalInspectable.CreateMarshaler(value, true);

            return _As(inspectable);
        }

        public static IntPtr GetAbi(IObjectReference value) => 
            value is null ? IntPtr.Zero : MarshalInterfaceHelper<T>.GetAbi(value);

        public static void DisposeAbi(IntPtr thisPtr) => MarshalInterfaceHelper<T>.DisposeAbi(thisPtr);

        public static void DisposeMarshaler(IObjectReference value) => MarshalInterfaceHelper<T>.DisposeMarshaler(value);

        public static IntPtr FromManaged(T value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler(value).GetRef();
        }

        public static unsafe void CopyManaged(T value, IntPtr dest)
        {
            *(IntPtr*)dest.ToPointer() =
                (value is null) ? IntPtr.Zero : CreateMarshaler(value).GetRef();
        }

        public static unsafe MarshalInterfaceHelper<T>.MarshalerArray CreateMarshalerArray(T[] array) => MarshalInterfaceHelper<T>.CreateMarshalerArray(array, (o) => CreateMarshaler(o));

        public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<T>.GetAbiArray(box);

        public static unsafe T[] FromAbiArray(object box) => MarshalInterfaceHelper<T>.FromAbiArray(box, FromAbi);

        public static unsafe (int length, IntPtr data) FromManagedArray(T[] array) => MarshalInterfaceHelper<T>.FromManagedArray(array, (o) => FromManaged(o));

        public static unsafe void CopyManagedArray(T[] array, IntPtr data) => MarshalInterfaceHelper<T>.CopyManagedArray(array, data, (o, dest) => CopyManaged(o, dest));

        public static void DisposeMarshalerArray(object box) => MarshalInterfaceHelper<T>.DisposeMarshalerArray(box);

        public static unsafe void DisposeAbiArray(object box) => MarshalInterfaceHelper<T>.DisposeAbiArray(box);

        private static Func<IntPtr, T> BindFromAbi()
        {
            var fromAbiMethod = HelperType.GetMethod("FromAbi", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            var objReferenceConstructor = HelperType.GetConstructor(BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, null, new[] { fromAbiMethod.ReturnType }, null);
            var parms = new[] { Expression.Parameter(typeof(IntPtr), "arg") };
            return Expression.Lambda<Func<IntPtr, T>>(
                    Expression.New(objReferenceConstructor,
                        Expression.Call(fromAbiMethod, parms[0])), parms).Compile();
        }

        private static Func<T, IObjectReference> BindToAbi()
        {
            var parms = new[] { Expression.Parameter(typeof(T), "arg") };
            return Expression.Lambda<Func<T, IObjectReference>>(
                Expression.MakeMemberAccess(
                    Expression.Convert(parms[0], HelperType),
                    HelperType.GetField("_obj", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)), parms).Compile();
        }

        private static Func<IObjectReference, IObjectReference> BindAs()
        {
            var helperType = typeof(T).GetHelperType();
            var parms = new[] { Expression.Parameter(typeof(IObjectReference), "arg") };
            return Expression.Lambda<Func<IObjectReference, IObjectReference>>(
                Expression.Call(
                    parms[0],
                    typeof(IObjectReference).GetMethod("As", Type.EmptyTypes).MakeGenericMethod(helperType.FindVftblType())
                    ), parms).Compile();
        }
    }

    static internal class MarshalInspectable
    {
        public static IObjectReference CreateMarshaler(object o, bool unwrapObject = true)
        {
            if (o is null)
            {
                return null;
            }

            if (unwrapObject && ComWrappersSupport.TryUnwrapObject(o, out var objRef))
            {
                return objRef.As<IInspectable.Vftbl>();
            }
            return ComWrappersSupport.CreateCCWForObject(o);
        }

        public static IntPtr GetAbi(IObjectReference objRef) => 
            objRef is null ? IntPtr.Zero : MarshalInterfaceHelper<object>.GetAbi(objRef);

        public static object FromAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return (object)null;
            }
            using var objRef = ObjectReference<IUnknownVftbl>.FromAbi(ptr);
            using var unknownObjRef = objRef.As<IUnknownVftbl>();
            if (unknownObjRef.IsReferenceToManagedObject)
            {
                return ComWrappersSupport.FindObject<object>(unknownObjRef.ThisPtr);
            }
            else if (Projections.TryGetMarshalerTypeForProjectedRuntimeClass(objRef, out Type type))
            {
                var fromAbiMethod = type.GetMethod("FromAbi", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (fromAbiMethod is null)
                {
                    throw new MissingMethodException();
                }
                return fromAbiMethod.Invoke(null, new object[] { ptr });
            }
            else
            {
                return ComWrappersSupport.CreateRcwForComObject(ptr);
            }
        }

        public static void DisposeMarshaler(IObjectReference objRef) => MarshalInterfaceHelper<object>.DisposeMarshaler(objRef);

        public static void DisposeAbi(IntPtr ptr) => MarshalInterfaceHelper<object>.DisposeAbi(ptr);
        public static IntPtr FromManaged(object o, bool unwrapObject = true)
        {
            var objRef = CreateMarshaler(o, unwrapObject);
            return objRef?.GetRef() ?? IntPtr.Zero;
        }

        public static unsafe void CopyManaged(object o, IntPtr dest, bool unwrapObject = true)
        {
            var objRef = CreateMarshaler(o, unwrapObject);
            *(IntPtr*)dest.ToPointer() = objRef?.GetRef() ?? IntPtr.Zero;
        }

        public static unsafe MarshalInterfaceHelper<object>.MarshalerArray CreateMarshalerArray(object[] array) => MarshalInterfaceHelper<object>.CreateMarshalerArray(array, (o) => CreateMarshaler(o));

        public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<object>.GetAbiArray(box);

        public static unsafe object[] FromAbiArray(object box) => MarshalInterfaceHelper<object>.FromAbiArray(box, FromAbi);

        public static unsafe (int length, IntPtr data) FromManagedArray(object[] array) => MarshalInterfaceHelper<object>.FromManagedArray(array, (o) => FromManaged(o));

        public static unsafe void CopyManagedArray(object[] array, IntPtr data) => MarshalInterfaceHelper<object>.CopyManagedArray(array, data, (o, dest) => CopyManaged(o, dest));

        public static void DisposeMarshalerArray(object box) => MarshalInterfaceHelper<object>.DisposeMarshalerArray(box);

        public static unsafe void DisposeAbiArray(object box) => MarshalInterfaceHelper<object>.DisposeAbiArray(box);
    }

    internal class Marshaler<T>
    {
        static Marshaler()
        {
            Type type = typeof(T);

            // structs cannot contain arrays, and arrays may only ever appear as parameters
            if (type.IsArray)
            {
                throw new InvalidOperationException("Arrays may not be marshaled generically.");
            }

            if (type == typeof(String))
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = (T value) => MarshalString.CreateMarshaler((string)(object)value);
                GetAbi = (object box) => MarshalString.GetAbi(box);
                FromAbi = (object value) => (T)(object)MarshalString.FromAbi((IntPtr)value);
                FromManaged = (T value) => MarshalString.FromManaged((string)(object)value);
                DisposeMarshaler = (object box) => MarshalString.DisposeMarshaler(box);
                DisposeAbi = (object box) => MarshalString.DisposeAbi(box);
                CreateMarshalerArray = (T[] array) => MarshalString.CreateMarshalerArray((string[])(object)array);
                GetAbiArray = (object box) => MarshalString.GetAbiArray(box);
                FromAbiArray = (object box) => (T[])(object)MarshalString.FromAbiArray(box);
                FromManagedArray = (T[] array) => MarshalString.FromManagedArray((string[])(object)array);
                CopyManagedArray = (T[] array, IntPtr data) => MarshalString.CopyManagedArray((string[])(object)array, data);
                DisposeMarshalerArray = (object box) => MarshalString.DisposeMarshalerArray(box);
                DisposeAbiArray = (object box) => MarshalString.DisposeAbiArray(box);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.KeyValuePair<,>))
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = MarshalGeneric<T>.CreateMarshaler;
                GetAbi = MarshalGeneric<T>.GetAbi;
                CopyAbi = MarshalGeneric<T>.CopyAbi;
                FromAbi = MarshalGeneric<T>.FromAbi;
                FromManaged = MarshalGeneric<T>.FromManaged;
                CopyManaged = MarshalGeneric<T>.CopyManaged;
                DisposeMarshaler = MarshalGeneric<T>.DisposeMarshaler;
                DisposeAbi = (object box) => { };
            }
            else if (type.IsValueType || type == typeof(Type))
            {
                AbiType = type.FindHelperType();
                if (AbiType != null)
                {
                    // Could still be blittable and the 'ABI.*' type exists for other reasons (e.g. it's a mapped type)
                    if (AbiType.GetMethod("FromAbi", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static) == null)
                    {
                        AbiType = null;
                    }
                }

                if (AbiType == null)
                {
                    AbiType = type;
                    CreateMarshaler = (T value) => value;
                    GetAbi = (object box) => box;
                    FromAbi = (object value) => (T)value;
                    FromManaged = (T value) => value;
                    DisposeMarshaler = (object box) => { };
                    DisposeAbi = (object box) => { };
                    CreateMarshalerArray = (T[] array) => MarshalBlittable<T>.CreateMarshalerArray(array);
                    GetAbiArray = (object box) => MarshalBlittable<T>.GetAbiArray(box);
                    FromAbiArray = (object box) => MarshalBlittable<T>.FromAbiArray(box);
                    FromManagedArray = (T[] array) => MarshalBlittable<T>.FromManagedArray(array);
                    CopyManagedArray = (T[] array, IntPtr data) => MarshalBlittable<T>.CopyManagedArray(array, data);
                    DisposeMarshalerArray = (object box) => MarshalBlittable<T>.DisposeMarshalerArray(box);
                    DisposeAbiArray = (object box) => MarshalBlittable<T>.DisposeAbiArray(box);
                }
                else
                {
                    CreateMarshaler = MarshalNonBlittable<T>.CreateMarshaler;
                    GetAbi = MarshalNonBlittable<T>.GetAbi;
                    CopyAbi = MarshalNonBlittable<T>.CopyAbi;
                    FromAbi = MarshalNonBlittable<T>.FromAbi;
                    FromManaged = MarshalNonBlittable<T>.FromManaged;
                    CopyManaged = MarshalNonBlittable<T>.CopyManaged;
                    DisposeMarshaler = MarshalNonBlittable<T>.DisposeMarshaler;
                    DisposeAbi = (object box) => { };
                    CreateMarshalerArray = (T[] array) => MarshalNonBlittable<T>.CreateMarshalerArray(array);
                    GetAbiArray = (object box) => MarshalNonBlittable<T>.GetAbiArray(box);
                    FromAbiArray = (object box) => MarshalNonBlittable<T>.FromAbiArray(box);
                    FromManagedArray = (T[] array) => MarshalNonBlittable<T>.FromManagedArray(array);
                    CopyManagedArray = (T[] array, IntPtr data) => MarshalNonBlittable<T>.CopyManagedArray(array, data);
                    DisposeMarshalerArray = (object box) => MarshalNonBlittable<T>.DisposeMarshalerArray(box);
                    DisposeAbiArray = (object box) => MarshalNonBlittable<T>.DisposeAbiArray(box);
                }
            }
            else if (type.IsInterface)
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = (T value) => MarshalInterface<T>.CreateMarshaler(value);
                GetAbi = (object objRef) => MarshalInterface<T>.GetAbi((IObjectReference)objRef);
                FromAbi = (object value) => (T)(object)MarshalInterface<T>.FromAbi((IntPtr)value);
                FromManaged = (T value) => ((IObjectReference)CreateMarshaler(value)).GetRef();
                DisposeMarshaler = (object objRef) => MarshalInterface<T>.DisposeMarshaler((IObjectReference)objRef);
                DisposeAbi = (object box) => MarshalInterface<T>.DisposeAbi((IntPtr)box);
            }
            else if (typeof(T) == typeof(object))
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = (T value) => MarshalInspectable.CreateMarshaler(value);
                GetAbi = (object objRef) => MarshalInspectable.GetAbi((IObjectReference)objRef);
                FromAbi = (object box) => (T)MarshalInspectable.FromAbi((IntPtr)box);
                FromManaged = (T value) => MarshalInspectable.FromManaged(value);
                CopyManaged = (T value, IntPtr dest) => MarshalInspectable.CopyManaged(value, dest);
                DisposeMarshaler = (object objRef) => MarshalInspectable.DisposeMarshaler((IObjectReference)objRef);
                DisposeAbi = (object box) => MarshalInspectable.DisposeAbi((IntPtr)box);
            }
            else // delegate, class 
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = MarshalGeneric<T>.CreateMarshaler;
                GetAbi = MarshalGeneric<T>.GetAbi;
                FromAbi = MarshalGeneric<T>.FromAbi;
                FromManaged = MarshalGeneric<T>.FromManaged;
                CopyManaged = MarshalGeneric<T>.CopyManaged;
                DisposeMarshaler = MarshalGeneric<T>.DisposeMarshaler;
                DisposeAbi = (object box) => { };
            }
            RefAbiType = AbiType.MakeByRefType();
        }

        public static readonly Type AbiType;
        public static readonly Type RefAbiType;
        public static readonly Func<T, object> CreateMarshaler;
        public static readonly Func<object, object> GetAbi;
        public static readonly Action<object, IntPtr> CopyAbi;
        public static readonly Func<object, T> FromAbi;
        public static readonly Func<T, object> FromManaged;
        public static readonly Action<T, IntPtr> CopyManaged;
        public static readonly Action<object> DisposeMarshaler;
        public static readonly Action<object> DisposeAbi;
        public static readonly Func<T[], object> CreateMarshalerArray;
        public static readonly Func<object, (int, IntPtr)> GetAbiArray;
        public static readonly Func<object, T[]> FromAbiArray;
        public static readonly Func<T[], (int, IntPtr)> FromManagedArray;
        public static readonly Action<T[], IntPtr> CopyManagedArray;
        public static readonly Action<object> DisposeMarshalerArray;
        public static readonly Action<object> DisposeAbiArray;
    }
}
