// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace MS.Internal.Interop.DWrite
{
    internal unsafe struct IDWriteFontCollection : IUnknown
    {
        public void** lpVtbl;

        public int QueryInterface(Guid* riid, void** ppvObject)
        {
            return ((delegate* unmanaged<IDWriteFontCollection*, Guid*, void**, int>)(lpVtbl[0]))((IDWriteFontCollection*)Unsafe.AsPointer(ref this), riid, ppvObject);
        }

        public uint AddRef()
        {
            return ((delegate* unmanaged<IDWriteFontCollection*, uint>)(lpVtbl[1]))((IDWriteFontCollection*)Unsafe.AsPointer(ref this));
        }

        public uint Release()
        {
            return ((delegate* unmanaged<IDWriteFontCollection*, uint>)(lpVtbl[2]))((IDWriteFontCollection*)Unsafe.AsPointer(ref this));
        }
    }
}
