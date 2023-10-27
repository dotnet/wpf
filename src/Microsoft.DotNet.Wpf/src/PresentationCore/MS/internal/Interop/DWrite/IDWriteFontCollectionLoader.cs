// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace MS.Internal.Interop.DWrite
{
    internal unsafe struct IDWriteFontCollectionLoader : IUnknown
    {
        public void** lpVtbl;

        public int QueryInterface(Guid* riid, void** ppvObject)
        {
            return ((delegate* unmanaged<IDWriteFontCollectionLoader*, Guid*, void**, int>)(lpVtbl[0]))((IDWriteFontCollectionLoader*)Unsafe.AsPointer(ref this), riid, ppvObject);
        }

        public uint AddRef()
        {
            return ((delegate* unmanaged<IDWriteFontCollectionLoader*, uint>)(lpVtbl[1]))((IDWriteFontCollectionLoader*)Unsafe.AsPointer(ref this));
        }

        public uint Release()
        {
            return ((delegate* unmanaged<IDWriteFontCollectionLoader*, uint>)(lpVtbl[2]))((IDWriteFontCollectionLoader*)Unsafe.AsPointer(ref this));
        }
    }
}
