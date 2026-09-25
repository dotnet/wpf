// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using MS.Internal.Interop.DWrite;

namespace MS.Internal.Text.TextInterface
{
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    internal unsafe class FontFileEnumerator : IDWriteFontFileEnumeratorMirror
    {
        private const int S_OK = unchecked((int)0L);
        private const int E_INVALIDARG = unchecked((int)0x80070057L);

        private readonly IEnumerator<IFontSource> _fontSourceCollectionEnumerator;
        private readonly FontFileLoader _fontFileLoader;
        private readonly IDWriteFactory* _factory;

        public FontFileEnumerator()
        {
            Debug.Fail("Assertion failed");
        }

        public FontFileEnumerator(IEnumerable<IFontSource> fontSourceCollection, FontFileLoader fontFileLoader, IDWriteFactory* factory)
        {
            _fontSourceCollectionEnumerator = fontSourceCollection.GetEnumerator();
            _fontFileLoader = fontFileLoader;
            factory->AddRef();
            _factory = factory;
        }

        /// <summary>
        /// Advances to the next font file in the collection. When it is first created, the enumerator is positioned
        /// before the first element of the collection and the first call to MoveNext advances to the first file.
        /// </summary>
        /// <param name="hasCurrentFile">Receives the value TRUE if the enumerator advances to a file, or FALSE if
        /// the enumerator advanced past the last file in the collection.</param>
        /// <returns>
        /// Standard HRESULT error code.
        /// </returns>
        [ComVisible(true)]
        public int MoveNext([MarshalAs(UnmanagedType.Bool)] out bool hasCurrentFile)
        {
            try
            {
                hasCurrentFile = _fontSourceCollectionEnumerator.MoveNext();
                return S_OK;
            }
            catch (Exception exception)
            {
                hasCurrentFile = default;
                return Marshal.GetHRForException(exception);
            }
        }

        /// <summary>
        /// Gets a reference to the current font file.
        /// </summary>
        /// <param name="fontFile">Pointer to the newly created font file object.</param>
        /// <returns>
        /// Standard HRESULT error code.
        /// </returns>
        [ComVisible(true)]
        public unsafe int GetCurrentFontFile(IDWriteFontFile** fontFile)
        {
            if (fontFile is null)
                return E_INVALIDARG;

            return InternalFactory.CreateFontFile(
                                          (Native.IDWriteFactory*)_factory,
                                          _fontFileLoader,
                                          _fontSourceCollectionEnumerator.Current.Uri,
                                          (Native.IDWriteFontFile**)fontFile
                                          );
        }
    }
}
