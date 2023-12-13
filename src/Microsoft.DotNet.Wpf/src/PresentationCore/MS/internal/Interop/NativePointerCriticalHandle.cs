// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace MS.Internal.Interop
{
    internal abstract unsafe class NativePointerCriticalHandle<TClass> : CriticalHandle
        where TClass : unmanaged
    {
        public NativePointerCriticalHandle(IntPtr nativePointer)
            : base(IntPtr.Zero)
        {
            SetHandle(nativePointer);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        public TClass* Value => (TClass*)handle;
    }

    internal unsafe class NativeIUnknownWrapper<TClass> : NativePointerCriticalHandle<TClass>
        where TClass : unmanaged, IUnknown
    {
        public NativeIUnknownWrapper(void* nativePointer)
            : base((IntPtr)nativePointer)
        {
        }

        protected override bool ReleaseHandle()
        {
            Value->Release();

            handle = IntPtr.Zero;

            return true;
        }
    }
}
