// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace MS.Internal.Interop.DWrite
{
    /// <summary>
    /// Managed interop for IDWriteColorGlyphRunEnumerator COM interface.
    /// Vtable layout: IUnknown (0-2), MoveNext (3), GetCurrentRun (4).
    /// </summary>
    internal unsafe struct IDWriteColorGlyphRunEnumerator
    {
        public void** lpVtbl;

        public uint Release()
        {
            return ((delegate* unmanaged<IDWriteColorGlyphRunEnumerator*, uint>)(lpVtbl[2]))((IDWriteColorGlyphRunEnumerator*)Unsafe.AsPointer(ref this));
        }

        public int MoveNext(int* hasRun)
        {
            return ((delegate* unmanaged<IDWriteColorGlyphRunEnumerator*, int*, int>)(lpVtbl[3]))((IDWriteColorGlyphRunEnumerator*)Unsafe.AsPointer(ref this), hasRun);
        }

        public int GetCurrentRun(DWRITE_COLOR_GLYPH_RUN** colorGlyphRun)
        {
            return ((delegate* unmanaged<IDWriteColorGlyphRunEnumerator*, DWRITE_COLOR_GLYPH_RUN**, int>)(lpVtbl[4]))((IDWriteColorGlyphRunEnumerator*)Unsafe.AsPointer(ref this), colorGlyphRun);
        }
    }
}
