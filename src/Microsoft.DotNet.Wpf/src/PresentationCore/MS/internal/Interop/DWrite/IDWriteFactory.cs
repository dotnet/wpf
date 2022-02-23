using System;
using System.Runtime.CompilerServices;

namespace MS.Internal.Interop.DWrite
{
    internal unsafe struct IDWriteFactory : IUnknown
    {
        private readonly void** Vtbl;

        public int QueryInterface(Guid* guid, void** comObject)
        {
            var function = (delegate* unmanaged<IDWriteFactory*, Guid*, void**, int>)Vtbl[0];

            fixed (IDWriteFactory* handle = &this)
            {
                return function(handle, guid, comObject);
            }
        }

        public uint AddReference()
        {
            var function = (delegate* unmanaged<IDWriteFactory*, uint>)Vtbl[1];

            fixed (IDWriteFactory* handle = &this)
            {
                return function(handle);
            }
        }

        public uint Release()
        {
            var function = (delegate* unmanaged<IDWriteFactory*, uint>)Vtbl[2];

            fixed (IDWriteFactory* handle = &this)
            {
                return function(handle);
            }
        }

        internal int GetSystemFontCollection(void* fontCollection, int checkForUpdate)
        {
            var function = (delegate* unmanaged<IDWriteFactory*, void*, int, int>)Vtbl[3];

            fixed (IDWriteFactory* handle = &this)
            {
                return function(handle, fontCollection, checkForUpdate);
            }
        }

        internal int CreateCustomFontCollection(IDWriteFontCollectionLoader* collectionLoader, void* collectionKey, uint collectionKeySize, IDWriteFontCollection** fontCollection)
        {
            var function = (delegate* unmanaged<IDWriteFactory*, IDWriteFontCollectionLoader*, void*, uint, IDWriteFontCollection**, int>)Vtbl[4];

            fixed (IDWriteFactory* handle = &this)
            {
                return function(handle, collectionLoader, collectionKey, collectionKeySize, fontCollection);
            }
        }

        public int RegisterFontCollectionLoader(IDWriteFontCollectionLoader* fontCollectionLoader)
        {
            var function = (delegate* unmanaged<IDWriteFactory*, IDWriteFontCollectionLoader*, int>)Vtbl[5];

            fixed (IDWriteFactory* handle = &this)
            {
                return function(handle, fontCollectionLoader);
            }
        }

        internal int CreateFontFace(DWRITE_FONT_FACE_TYPE fontFaceType, uint numberOfFiles, IDWriteFontFile** fontFiles, uint faceIndex, DWRITE_FONT_SIMULATIONS fontFaceSimulationFlags, IDWriteFontFace** fontFace)
        {
            var function = (delegate* unmanaged<IDWriteFactory*, DWRITE_FONT_FACE_TYPE, uint, IDWriteFontFile**, uint, DWRITE_FONT_SIMULATIONS, IDWriteFontFace**, int>)Vtbl[9];

            fixed (IDWriteFactory* handle = &this)
            {
                return function(handle, fontFaceType, numberOfFiles, fontFiles, faceIndex, fontFaceSimulationFlags, fontFace);
            }
        }

        internal int RegisterFontFileLoader(IDWriteFontFileLoader* fontFileLoader)
        {
            var function = (delegate* unmanaged<IDWriteFactory*, IDWriteFontFileLoader*, int>)Vtbl[13];

            fixed (IDWriteFactory* handle = &this)
            {
                return function(handle, fontFileLoader);
            }
        }

        internal int CreateTextAnalyzer(IDWriteTextAnalyzer** textAnalyzer)
        {
            var function = (delegate* unmanaged<IDWriteFactory*, IDWriteTextAnalyzer**, int>)Vtbl[21];

            fixed (IDWriteFactory* handle = &this)
            {
                int hr = function(handle, textAnalyzer);

                return hr;
            }
        }
    }
}
