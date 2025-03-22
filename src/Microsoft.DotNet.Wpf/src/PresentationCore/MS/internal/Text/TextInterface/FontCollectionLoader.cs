// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MS.Internal.Text.TextInterface.Interfaces;

namespace MS.Internal.Text.TextInterface
{
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    internal unsafe class FontCollectionLoader : IDWriteFontCollectionLoaderMirror
    {
        private const int S_OK = unchecked((int)0L);
        private const int E_INVALIDARG = unchecked((int)0x80070057L);

        private readonly IFontSourceCollectionFactory _fontSourceCollectionFactory;
        private readonly FontFileLoader _fontFileLoader;

        public FontCollectionLoader()
        {
            Debug.Fail("Assertion failed");
        }

        public FontCollectionLoader(IFontSourceCollectionFactory fontSourceCollectionFactory, FontFileLoader fontFileLoader)
        {
            _fontSourceCollectionFactory = fontSourceCollectionFactory;
            _fontFileLoader = fontFileLoader;
        }

        /// <summary>
        /// Creates a font file enumerator object that encapsulates a collection of font files.
        /// The font system calls back to this interface to create a font collection.
        /// </summary>
        /// <param name="collectionKey">Font collection key that uniquely identifies the collection of font files within
        /// the scope of the font collection loader being used.</param>
        /// <param name="collectionKeySize">Size of the font collection key in bytes.</param>
        /// <param name="fontFileEnumerator">Pointer to the newly created font file enumerator.</param>
        /// <returns>
        /// Standard HRESULT error code.
        /// </returns>
        [ComVisible(true)]
        public int CreateEnumeratorFromKey(IntPtr factory, [In] void* collectionKey, [In, MarshalAs(UnmanagedType.U4)] uint collectionKeySize, IntPtr* fontFileEnumerator)
        {
            uint numberOfCharacters = collectionKeySize / sizeof(char);
            if ((fontFileEnumerator == null)
                || (collectionKeySize % sizeof(char) != 0)                        // The collectionKeySize must be divisible by sizeof(WCHAR)
                || (numberOfCharacters <= 1)                                      // The collectionKey cannot be less than or equal 1 character as it has to contain the NULL character.
                || (((char*)collectionKey)[numberOfCharacters - 1] != '\0'))  // The collectionKey must end with the NULL character
            {
                return E_INVALIDARG;
            }

            *fontFileEnumerator = IntPtr.Zero;

            string uriString = new string((char*)collectionKey);
            int hr = S_OK;

            try
            {
                IFontSourceCollection fontSourceCollection = _fontSourceCollectionFactory.Create(uriString);
                FontFileEnumerator fontFileEnum = new FontFileEnumerator(
                                                          fontSourceCollection,
                                                          _fontFileLoader,
                                                          (Native.IDWriteFactory*)factory.ToPointer()
                                                          );
                IntPtr pIDWriteFontFileEnumeratorMirror = Marshal.GetComInterfaceForObject(
                                                        fontFileEnum,
                                                        typeof(IDWriteFontFileEnumeratorMirror));

                *fontFileEnumerator = pIDWriteFontFileEnumeratorMirror;
            }
            catch (Exception exception)
            {
                hr = Marshal.GetHRForException(exception);
            }

            return hr;
        }
    }
}
