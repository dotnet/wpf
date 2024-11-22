// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace MS.Internal.Interop.DWrite
{
    internal unsafe struct IDWriteFactory : IUnknown
    {
        public void** lpVtbl;

        public int QueryInterface(Guid* riid, void** ppvObject)
        {
            return ((delegate* unmanaged<IDWriteFactory*, Guid*, void**, int>)(lpVtbl[0]))((IDWriteFactory*)Unsafe.AsPointer(ref this), riid, ppvObject);
        }

        public uint AddRef()
        {
            return ((delegate* unmanaged<IDWriteFactory*, uint>)(lpVtbl[1]))((IDWriteFactory*)Unsafe.AsPointer(ref this));
        }

        public uint Release()
        {
            return ((delegate* unmanaged<IDWriteFactory*, uint>)(lpVtbl[2]))((IDWriteFactory*)Unsafe.AsPointer(ref this));
        }

        public int GetSystemFontCollection(IDWriteFontCollection** fontCollection, int checkForUpdates = 0)
        {
            return ((delegate* unmanaged<IDWriteFactory*, IDWriteFontCollection**, int, int>)(lpVtbl[3]))((IDWriteFactory*)Unsafe.AsPointer(ref this), fontCollection, checkForUpdates);
        }

        public int CreateCustomFontCollection(IDWriteFontCollectionLoader* collectionLoader, void* collectionKey, uint collectionKeySize, IDWriteFontCollection** fontCollection)
        {
            return ((delegate* unmanaged<IDWriteFactory*, IDWriteFontCollectionLoader*, void*, uint, IDWriteFontCollection**, int>)(lpVtbl[4]))((IDWriteFactory*)Unsafe.AsPointer(ref this), collectionLoader, collectionKey, collectionKeySize, fontCollection);
        }

        public int RegisterFontCollectionLoader(IDWriteFontCollectionLoader* fontCollectionLoader)
        {
            return ((delegate* unmanaged<IDWriteFactory*, IDWriteFontCollectionLoader*, int>)(lpVtbl[5]))((IDWriteFactory*)Unsafe.AsPointer(ref this), fontCollectionLoader);
        }

        public int UnregisterFontCollectionLoader(IDWriteFontCollectionLoader* fontCollectionLoader)
        {
            return ((delegate* unmanaged<IDWriteFactory*, IDWriteFontCollectionLoader*, int>)(lpVtbl[6]))((IDWriteFactory*)Unsafe.AsPointer(ref this), fontCollectionLoader);
        }

        public int CreateFontFace(DWRITE_FONT_FACE_TYPE fontFaceType, uint numberOfFiles, IDWriteFontFile** fontFiles, uint faceIndex, DWRITE_FONT_SIMULATIONS fontFaceSimulationFlags, IDWriteFontFace** fontFace)
        {
            return ((delegate* unmanaged<IDWriteFactory*, DWRITE_FONT_FACE_TYPE, uint, IDWriteFontFile**, uint, DWRITE_FONT_SIMULATIONS, IDWriteFontFace**, int>)(lpVtbl[9]))((IDWriteFactory*)Unsafe.AsPointer(ref this), fontFaceType, numberOfFiles, fontFiles, faceIndex, fontFaceSimulationFlags, fontFace);
        }

        public int RegisterFontFileLoader(IDWriteFontFileLoader* fontFileLoader)
        {
            return ((delegate* unmanaged<IDWriteFactory*, IDWriteFontFileLoader*, int>)(lpVtbl[13]))((IDWriteFactory*)Unsafe.AsPointer(ref this), fontFileLoader);
        }

        public int UnregisterFontFileLoader(IDWriteFontFileLoader* fontFileLoader)
        {
            return ((delegate* unmanaged<IDWriteFactory*, IDWriteFontFileLoader*, int>)(lpVtbl[14]))((IDWriteFactory*)Unsafe.AsPointer(ref this), fontFileLoader);
        }

        public int CreateTextAnalyzer(IDWriteTextAnalyzer** textAnalyzer)
        {
            return ((delegate* unmanaged<IDWriteFactory*, IDWriteTextAnalyzer**, int>)(lpVtbl[21]))((IDWriteFactory*)Unsafe.AsPointer(ref this), textAnalyzer);
        }
    }
}
