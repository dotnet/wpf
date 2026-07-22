// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace MS.Internal.Interop.DWrite
{
    internal unsafe struct IDWriteFactory2 : IUnknown
    {
        public void** lpVtbl;

        public int QueryInterface(Guid* riid, void** ppvObject)
        {
            return ((delegate* unmanaged<IDWriteFactory2*, Guid*, void**, int>)(lpVtbl[0]))((IDWriteFactory2*)Unsafe.AsPointer(ref this), riid, ppvObject);
        }

        public uint AddRef()
        {
            return ((delegate* unmanaged<IDWriteFactory2*, uint>)(lpVtbl[1]))((IDWriteFactory2*)Unsafe.AsPointer(ref this));
        }

        public uint Release()
        {
            return ((delegate* unmanaged<IDWriteFactory2*, uint>)(lpVtbl[2]))((IDWriteFactory2*)Unsafe.AsPointer(ref this));
        }

        public int TranslateColorGlyphRun(
            float baselineOriginX,
            float baselineOriginY,
            DWRITE_GLYPH_RUN* glyphRun,
            void* glyphRunDescription,
            int measuringMode,
            void* worldAndDpiTransform,
            uint colorPaletteIndex,
            IDWriteColorGlyphRunEnumerator** colorLayers)
        {
            return ((delegate* unmanaged<IDWriteFactory2*, float, float, DWRITE_GLYPH_RUN*, void*, int, void*, uint, IDWriteColorGlyphRunEnumerator**, int>)(lpVtbl[28]))(
                (IDWriteFactory2*)Unsafe.AsPointer(ref this),
                baselineOriginX,
                baselineOriginY,
                glyphRun,
                glyphRunDescription,
                measuringMode,
                worldAndDpiTransform,
                colorPaletteIndex,
                colorLayers);
        }
    }
}
