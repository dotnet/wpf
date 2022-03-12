// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace MS.Internal.Interop.DWrite
{
    internal unsafe struct IDWriteTextAnalyzer : IUnknown
    {
        public void** lpVtbl;

        public int QueryInterface(Guid* riid, void** ppvObject)
        {
            return ((delegate* unmanaged[Stdcall]<IDWriteTextAnalyzer*, Guid*, void**, int>)(lpVtbl[0]))((IDWriteTextAnalyzer*)Unsafe.AsPointer(ref this), riid, ppvObject);
        }

        public uint AddReference()
        {
            return ((delegate* unmanaged[Stdcall]<IDWriteTextAnalyzer*, uint>)(lpVtbl[1]))((IDWriteTextAnalyzer*)Unsafe.AsPointer(ref this));
        }

        public uint Release()
        {
            return ((delegate* unmanaged[Stdcall]<IDWriteTextAnalyzer*, uint>)(lpVtbl[2]))((IDWriteTextAnalyzer*)Unsafe.AsPointer(ref this));
        }
    }
}
