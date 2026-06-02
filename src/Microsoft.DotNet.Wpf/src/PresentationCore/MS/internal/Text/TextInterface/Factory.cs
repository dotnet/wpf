// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using MS.Internal.Interop;
using MS.Internal.Interop.DWrite;
using MS.Internal.Text.TextInterface.Interfaces;

namespace MS.Internal.Text.TextInterface
{
    /// <summary>
    /// The root factory interface for all DWrite objects.
    /// </summary>
    internal unsafe class Factory
    {
        private const int DWRITE_E_FILEFORMAT = unchecked((int)0x88985000L);

        // b859ee5a-d838-4b5b-a2e8-1adc7d93db48
        private static readonly Guid IID_IDWriteFactory = new Guid(0xb859ee5a, 0xd838, 0x4b5b, 0xa2, 0xe8, 0x1a, 0xdc, 0x7d, 0x93, 0xdb, 0x48);

        /// <summary>
        /// A pointer to the wrapped DWrite factory object.
        /// </summary>
        private NativeFactoryWrapper _factory;

        /// <summary>
        /// The custom loader used by WPF to load font files.
        /// </summary>
        private FontFileLoader _wpfFontFileLoader;

        /// <summary>
        /// The custom loader used by WPF to load font collections.
        /// </summary>
        private FontCollectionLoader _wpfFontCollectionLoader;

        private readonly IFontSourceFactory _fontSourceFactory;

        /// <summary>
        /// Pointer to the IDWriteFactory2 interface, or null if not available.
        /// Used for COLR v0 color glyph support (TranslateColorGlyphRun).
        /// Available on Windows 8.1+.
        /// </summary>
        private void* _pFactory2;

        /// <summary>
        /// Constructs a factory object.
        /// </summary>
        /// <param name="factoryType">Identifies whether the factory object will be shared or isolated.</param>
        /// <param name="fontSourceCollectionFactory">A factory object that will create managed FontSourceCollection 
        /// objects that will be utilized to load embedded fonts.</param>
        /// <param name="fontSourceFactory">A factory object that will create managed FontSource
        /// objects that will be utilized to load embedded fonts.</param>
        /// <returns>
        /// The factory just created.
        /// </returns>
        private Factory(FactoryType factoryType, IFontSourceCollectionFactory fontSourceCollectionFactory, IFontSourceFactory fontSourceFactory)
        {
            Initialize(factoryType);

            // QueryInterface for IDWriteFactory2, which provides TranslateColorGlyphRun
            // for COLR v0 color glyph decomposition. Available on Windows 8.1+; on older
            // versions the QI returns E_NOINTERFACE and _pFactory2 stays null, which
            // disables color glyph rendering gracefully.
            Guid iidFactory2 = new Guid(0x0439fc60, 0xca44, 0x4994, 0x8d, 0xee, 0x3a, 0x9a, 0xf7, 0xb7, 0x32, 0xec);
            void* pFactory2 = null;
            int qiHr = _factory.Value->QueryInterface(&iidFactory2, &pFactory2);
            if (qiHr == 0 && pFactory2 != null)
                _pFactory2 = pFactory2;

            _wpfFontFileLoader = new FontFileLoader(fontSourceFactory);
            _wpfFontCollectionLoader = new FontCollectionLoader(
                                                               fontSourceCollectionFactory,
                                                               _wpfFontFileLoader
                                                               );

            _fontSourceFactory = fontSourceFactory;

            IntPtr pIDWriteFontFileLoaderMirror = Marshal.GetComInterfaceForObject(
                                                    _wpfFontFileLoader,
                                                    typeof(IDWriteFontFileLoaderMirror));

            // Future improvement note: 
            // This seems a bit hacky, but unclear at this time how to implement this any better. 
            // When we attempt to unregister these, do we need to keep around the same IntPtr
            // representing the result of GetComInterfaceForObject to free it ? Or will it 
            // be the same if we call it again?



            int hr = _factory.Value->RegisterFontFileLoader(
                                        (IDWriteFontFileLoader*)pIDWriteFontFileLoaderMirror.ToPointer()
                                        );

            Marshal.Release(pIDWriteFontFileLoaderMirror);

            DWriteUtil.ConvertHresultToException(hr);

            IntPtr pIDWriteFontCollectionLoaderMirror = Marshal.GetComInterfaceForObject(
                                                    _wpfFontCollectionLoader,
                                                    typeof(IDWriteFontCollectionLoaderMirror));
            hr = _factory.Value->RegisterFontCollectionLoader(
                                                        (IDWriteFontCollectionLoader*)pIDWriteFontCollectionLoaderMirror.ToPointer()
                                                        );

            Marshal.Release(pIDWriteFontCollectionLoaderMirror);

            DWriteUtil.ConvertHresultToException(hr);
        }

        internal IDWriteFactory* DWriteFactory
            => _factory.Value;

        /// <summary>
        /// Initializes a factory object.
        /// </summary>
        /// <param name="factoryType">Identifies whether the factory object will be shared or isolated.</param>
        private void Initialize(FactoryType factoryType)
        {
            Guid iid = IID_IDWriteFactory;
            IDWriteFactory* factory = null;

            delegate* unmanaged<int, void*, void*, int> pfnDWriteCreateFactory = DWriteLoader.GetDWriteCreateFactoryFunctionPointer();

            int hr = pfnDWriteCreateFactory((int)DWriteTypeConverter.Convert(factoryType), &iid, &factory);

            DWriteUtil.ConvertHresultToException(hr);

            _factory = new NativeFactoryWrapper(factory, this);
        }

        /// <summary>
        /// Creates a DirectWrite factory object that is used for subsequent creation of individual DirectWrite objects.
        /// </summary>
        /// <param name="factoryType">Identifies whether the factory object will be shared or isolated.</param>
        /// <param name="fontSourceCollectionFactory">A factory object that will create managed FontSourceCollection 
        /// objects that will be utilized to load embedded fonts.</param>
        /// <param name="fontSourceFactory">A factory object that will create managed FontSource
        /// objects that will be utilized to load embedded fonts.</param>
        /// <returns>
        /// The factory just created.
        /// </returns>
        internal static Factory Create(
            FactoryType factoryType,
            IFontSourceCollectionFactory fontSourceCollectionFactory,
            IFontSourceFactory fontSourceFactory)
        {
            return new Factory(factoryType, fontSourceCollectionFactory, fontSourceFactory);
        }

        /// <summary>
        /// Creates a font file object from a local font file.
        /// </summary>
        /// <param name="filePathUri">file path uri.</param>
        /// <returns>
        /// Newly created font file object, or NULL in case of failure.
        /// </returns>
        internal FontFile CreateFontFile(Uri filePathUri)
        {
            Native.IDWriteFontFile* dwriteFontFile = null;
            int hr = InternalFactory.CreateFontFile((Native.IDWriteFactory*)_factory.Value, _wpfFontFileLoader, filePathUri, &dwriteFontFile);

            // If DWrite's CreateFontFileReference fails then try opening the file using WPF's logic.
            // The failures that WPF returns are more granular than the HRESULTs that DWrite returns
            // thus we use WPF's logic to open the file to throw the same exceptions that
            // WPF would have thrown before.
            if (hr != 0)
            {
                IFontSource fontSource = _fontSourceFactory.Create(filePathUri.AbsoluteUri);
                fontSource.TestFileOpenable();

            }

            //This call is made to prevent this object from being collected and hence get its finalize method called 
            //While there are others referencing it.
            GC.KeepAlive(this);

            DWriteUtil.ConvertHresultToException(hr);

            return new FontFile(dwriteFontFile);
        }

        /// <summary>
        /// Creates a font face object.
        /// </summary>
        /// <param name="filePathUri">The file path of the font face.</param>
        /// <param name="faceIndex">The zero based index of a font face in cases when the font files contain a collection of font faces.
        /// If the font files contain a single face, this value should be zero.</param>
        /// <returns>
        /// Newly created font face object, or NULL in case of failure.
        /// </returns>
        internal FontFace CreateFontFace(Uri filePathUri, uint faceIndex)
        {
            return CreateFontFace(
                                 filePathUri,
                                 faceIndex,
                                 FontSimulations.None
                                 );
        }

        /// <summary>
        /// Creates a font face object.
        /// </summary>
        /// <param name="filePathUri">The file path of the font face.</param>
        /// <param name="faceIndex">The zero based index of a font face in cases when the font files contain a collection of font faces.
        /// If the font files contain a single face, this value should be zero.</param>
        /// <param name="fontSimulationFlags">Font face simulation flags for algorithmic emboldening and italicization.</param>
        /// <returns>
        /// Newly created font face object, or NULL in case of failure.
        /// </returns>
        internal FontFace CreateFontFace(Uri filePathUri, uint faceIndex, FontSimulations fontSimulationFlags)
        {
            FontFile fontFile = CreateFontFile(filePathUri);
            Native.DWRITE_FONT_FILE_TYPE dwriteFontFileType;
            Native.DWRITE_FONT_FACE_TYPE dwriteFontFaceType;
            uint numberOfFaces = 0;

            int hr;
            if (fontFile.Analyze(
                                 out dwriteFontFileType,
                                 out dwriteFontFaceType,
                                 out numberOfFaces,
                                 &hr
                                 ))
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(faceIndex, numberOfFaces);

                byte dwriteFontSimulationsFlags = DWriteTypeConverter.Convert(fontSimulationFlags);
                IDWriteFontFace* dwriteFontFace = null;
                IDWriteFontFile* dwriteFontFile = (IDWriteFontFile*)fontFile.DWriteFontFileNoAddRef;

                hr = _factory.Value->CreateFontFace(
                                                     (DWRITE_FONT_FACE_TYPE)dwriteFontFaceType,
                                                     1,
                                                     &dwriteFontFile,
                                                     faceIndex,
                                                     (DWRITE_FONT_SIMULATIONS)dwriteFontSimulationsFlags,
                                                     &dwriteFontFace
                                                     );
                GC.KeepAlive(fontFile);
                GC.KeepAlive(this);

                DWriteUtil.ConvertHresultToException(hr);

                return new FontFace((Native.IDWriteFontFace*)dwriteFontFace);
            }

            // This path is here because there is a behavior mismatch between DWrite and WPF.
            // If a directory was given instead of a font uri WPF previously throws 
            // System.UnauthorizedAccessException. We handle most of the exception behavior mismatch 
            // in FontFile^ Factory::CreateFontFile by opening the file using WPF's previous (prior to DWrite integration) logic if 
            // CreateFontFileReference fails (please see comments in FontFile^ Factory::CreateFontFile).
            // However in this special case DWrite's CreateFontFileReference will succeed if given
            // a directory instead of a font file and it is the Analyze() call will fail returning DWRITE_E_FILEFORMAT.
            // Thus, incase the hr returned from Analyze() was DWRITE_E_FILEFORMAT we do as we did in FontFile^ Factory::CreateFontFile
            // to try and open the file using WPF old logic and throw System.UnauthorizedAccessException as WPF used to do.
            // If a file format exception is expected then opening the file should succeed and ConvertHresultToException()
            // Should throw the correct exception.
            // A final note would be that this overhead is only incurred in error conditions and so the normal execution path should
            // not be affected.
            else
            {
                if (hr == DWRITE_E_FILEFORMAT)
                {
                    IFontSource fontSource = _fontSourceFactory.Create(filePathUri.AbsoluteUri);
                    fontSource.TestFileOpenable();
                }
                DWriteUtil.ConvertHresultToException(hr);
            }

            return null;
        }

        /// <summary>
        /// Gets a font collection representing the set of installed fonts.
        /// </summary>
        /// <returns>
        /// The system font collection.
        /// </returns>
        internal FontCollection GetSystemFontCollection()
        {
            return GetSystemFontCollection(false);
        }

        /// <summary>
        /// Gets a font collection representing the set of installed fonts.
        /// </summary>
        /// <param name="checkForUpdates">If this parameter is true, the function performs an immediate check for changes to the set of
        /// installed fonts. If this parameter is FALSE, the function will still detect changes if the font cache service is running, but
        /// there may be some latency. For example, an application might specify TRUE if it has itself just installed a font and wants to 
        /// be sure the font collection contains that font.</param>
        /// <returns>
        /// The system font collection.
        /// </returns>
        private FontCollection GetSystemFontCollection(bool checkForUpdates)
        {
            IDWriteFontCollection* fontCollection = null;
            int checkForUpdatesInt = checkForUpdates ? 1 : 0;
            int hr = _factory.Value->GetSystemFontCollection(&fontCollection, checkForUpdatesInt);

            DWriteUtil.ConvertHresultToException(hr);

            return new FontCollection((Native.IDWriteFontCollection*)fontCollection);
        }

        /// <summary>
        /// Gets a font collection in a custom location.
        /// </summary>
        /// <param name="uri">The uri of the font collection.</param>
        /// <returns>
        /// The font collection.
        /// </returns>
        internal FontCollection GetFontCollection(Uri uri)
        {
            string uriString = uri.AbsoluteUri;
            IDWriteFontCollection* dwriteFontCollection = null;

            IntPtr pIDWriteFontCollectionLoaderMirror = Marshal.GetComInterfaceForObject(
                                                    _wpfFontCollectionLoader,
                                                    typeof(IDWriteFontCollectionLoaderMirror));

            IDWriteFontCollectionLoader* pIDWriteFontCollectionLoader = 
                (IDWriteFontCollectionLoader*)pIDWriteFontCollectionLoaderMirror.ToPointer();

            int hr;

            fixed (char* uriStringPtr = uriString)
            {
                uint collectionKeySize = (uint)((uriString.Length + 1) * sizeof(char));
                hr = _factory.Value->CreateCustomFontCollection(pIDWriteFontCollectionLoader, uriStringPtr, collectionKeySize, &dwriteFontCollection);
            }

            Marshal.Release(pIDWriteFontCollectionLoaderMirror);

            GC.KeepAlive(this);

            DWriteUtil.ConvertHresultToException(hr);

            return new FontCollection((Native.IDWriteFontCollection*)dwriteFontCollection);
        }

        internal TextAnalyzer CreateTextAnalyzer()
        {
            IDWriteTextAnalyzer* textAnalyzer = null;

            _factory.Value->CreateTextAnalyzer(&textAnalyzer);

            return new TextAnalyzer((Native.IDWriteTextAnalyzer*)textAnalyzer);
        }

        /// <summary>
        /// Gets whether this factory supports color glyph rendering (IDWriteFactory2+).
        /// </summary>
        internal bool SupportsColorGlyphs => _pFactory2 != null;

        // DWRITE_E_NOCOLOR: font has no COLR table or glyphs have no color layers.
        private const int DWRITE_E_NOCOLOR = unchecked((int)0x8898500C);

        /// <summary>
        /// Translates a glyph run into color glyph layers using IDWriteFactory2.
        /// Returns null if the glyph run has no color layers or color glyphs are not supported.
        /// </summary>
        internal List<ColorGlyphLayer> TranslateColorGlyphRun(
            FontFace fontFace,
            float fontEmSize,
            ushort[] glyphIndices,
            float[] glyphAdvances,
            float[] glyphOffsets,
            uint glyphCount,
            bool isSideways,
            uint bidiLevel,
            float baselineOriginX,
            float baselineOriginY,
            uint measuringMode)
        {
            if (_pFactory2 == null || fontFace == null ||
                glyphIndices == null || glyphAdvances == null || glyphCount == 0 ||
                glyphCount > (uint)int.MaxValue ||
                glyphIndices.Length < (int)glyphCount ||
                glyphAdvances.Length < (int)glyphCount)
            {
                return null;
            }

            IDWriteColorGlyphRunEnumerator* pColorEnum = null;
            try
            {
                fixed (ushort* pGlyphIndices = glyphIndices)
                fixed (float* pGlyphAdvances = glyphAdvances)
                {
                    // Convert interleaved float pairs to DWRITE_GLYPH_OFFSET structs.
                    DWRITE_GLYPH_OFFSET* pOffsets = null;
                    DWRITE_GLYPH_OFFSET* allocatedOffsets = null;

                    try
                    {
                        if (glyphOffsets != null && glyphCount <= (uint)(glyphOffsets.Length / 2))
                        {
                            allocatedOffsets = (DWRITE_GLYPH_OFFSET*)Marshal.AllocHGlobal(
                                (int)(glyphCount * (uint)sizeof(DWRITE_GLYPH_OFFSET)));

                            fixed (float* pGlyphOffsets = glyphOffsets)
                            {
                                for (uint i = 0; i < glyphCount; i++)
                                {
                                    allocatedOffsets[i].advanceOffset = pGlyphOffsets[i * 2];
                                    allocatedOffsets[i].ascenderOffset = pGlyphOffsets[i * 2 + 1];
                                }
                            }
                            pOffsets = allocatedOffsets;
                        }

                        DWRITE_GLYPH_RUN glyphRun;
                        glyphRun.fontFace = (void*)fontFace.DWriteFontFaceAddRef;
                        glyphRun.fontEmSize = fontEmSize;
                        glyphRun.glyphCount = glyphCount;
                        glyphRun.glyphIndices = pGlyphIndices;
                        glyphRun.glyphAdvances = pGlyphAdvances;
                        glyphRun.glyphOffsets = pOffsets;
                        glyphRun.isSideways = isSideways ? 1 : 0;
                        glyphRun.bidiLevel = bidiLevel;

                        void* pEnum = null;

                        // IDWriteFactory2::TranslateColorGlyphRun is vtable slot 28.
                        void** factory2Vtbl = *(void***)_pFactory2;
                        int hr = ((delegate* unmanaged<void*, float, float, DWRITE_GLYPH_RUN*, void*, int, void*, uint, void**, int>)(factory2Vtbl[28]))(
                            _pFactory2,
                            baselineOriginX, baselineOriginY,
                            &glyphRun, null,
                            (int)measuringMode, null,
                            0, &pEnum);

                        // Release the AddRef'd font face now that the DWrite call is complete.
                        ((IDWriteFontFace*)glyphRun.fontFace)->Release();
                        glyphRun.fontFace = null;

                        GC.KeepAlive(fontFace);
                        GC.KeepAlive(this);

                        if (hr == DWRITE_E_NOCOLOR || hr < 0 || pEnum == null)
                        {
                            if (pEnum != null)
                                ((IDWriteColorGlyphRunEnumerator*)pEnum)->Release();
                            return null;
                        }

                        pColorEnum = (IDWriteColorGlyphRunEnumerator*)pEnum;
                    }
                    finally
                    {
                        if (allocatedOffsets != null)
                            Marshal.FreeHGlobal((IntPtr)allocatedOffsets);
                    }
                }

                // Enumerate color layers. DirectWrite returns them in back-to-front paint order.
                const int MaxColorLayers = 1024;
                var layers = new List<ColorGlyphLayer>();

                int hasRun = 0;
                while (pColorEnum->MoveNext(&hasRun) >= 0 && hasRun != 0 && layers.Count < MaxColorLayers)
                {
                    DWRITE_COLOR_GLYPH_RUN* pColorRun = null;
                    int runHr = pColorEnum->GetCurrentRun(&pColorRun);
                    if (runHr < 0 || pColorRun == null)
                        break;

                    ColorGlyphLayer layer;
                    layer.BaselineOriginX = pColorRun->baselineOriginX;
                    layer.BaselineOriginY = pColorRun->baselineOriginY;

                    // paletteIndex 0xFFFF means "use the text foreground color".
                    layer.UseForegroundColor = (pColorRun->paletteIndex == 0xFFFF);

                    // Clamp RGBA to [0,1].
                    layer.ColorR = Clamp01(pColorRun->runColor.r);
                    layer.ColorG = Clamp01(pColorRun->runColor.g);
                    layer.ColorB = Clamp01(pColorRun->runColor.b);
                    layer.ColorA = Clamp01(pColorRun->runColor.a);

                    uint layerGlyphCount = pColorRun->glyphRun.glyphCount;

                    // Deep-copy glyph data (pointer is only valid until next MoveNext).
                    layer.GlyphIndices = new ushort[layerGlyphCount];
                    for (uint i = 0; i < layerGlyphCount; i++)
                        layer.GlyphIndices[i] = pColorRun->glyphRun.glyphIndices[i];

                    layer.GlyphAdvances = new float[layerGlyphCount];
                    if (pColorRun->glyphRun.glyphAdvances != null)
                    {
                        for (uint i = 0; i < layerGlyphCount; i++)
                            layer.GlyphAdvances[i] = pColorRun->glyphRun.glyphAdvances[i];
                    }

                    if (pColorRun->glyphRun.glyphOffsets != null)
                    {
                        layer.GlyphOffsets = new float[layerGlyphCount * 2];
                        for (uint i = 0; i < layerGlyphCount; i++)
                        {
                            layer.GlyphOffsets[i * 2] = pColorRun->glyphRun.glyphOffsets[i].advanceOffset;
                            layer.GlyphOffsets[i * 2 + 1] = pColorRun->glyphRun.glyphOffsets[i].ascenderOffset;
                        }
                    }
                    else
                    {
                        layer.GlyphOffsets = null;
                    }

                    layers.Add(layer);
                }

                return layers.Count > 0 ? layers : null;
            }
            finally
            {
                if (pColorEnum != null)
                    pColorEnum->Release();
            }
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        internal static bool IsLocalUri(Uri uri)
        {
            return uri.IsFile && uri.IsLoopback && !uri.IsUnc;
        }

        private sealed unsafe class NativeFactoryWrapper : NativeIUnknownWrapper<IDWriteFactory>
        {
            private readonly Factory _managedFactory;

            public NativeFactoryWrapper(void* nativePointer, Factory managedFactory)
                : base(nativePointer)
            {
                _managedFactory = managedFactory;
            }

            protected override bool ReleaseHandle()
            {
                FontCollectionLoader wpfFontCollectionLoader = _managedFactory._wpfFontCollectionLoader;
                if (wpfFontCollectionLoader != null)
                {
                    IntPtr pIDWriteFontCollectionLoaderMirror = Marshal.GetComInterfaceForObject(
                                                            wpfFontCollectionLoader,
                                                            typeof(IDWriteFontCollectionLoaderMirror));

                    Value->UnregisterFontCollectionLoader((IDWriteFontCollectionLoader*)pIDWriteFontCollectionLoaderMirror.ToPointer());
                    Marshal.Release(pIDWriteFontCollectionLoaderMirror);
                    _managedFactory._wpfFontCollectionLoader = null;
                }

                FontFileLoader wpfFontFileLoader = _managedFactory._wpfFontFileLoader;
                if (wpfFontFileLoader != null)
                {
                    IntPtr pIDWriteFontFileLoaderMirror = Marshal.GetComInterfaceForObject(
                                                            wpfFontFileLoader,
                                                            typeof(IDWriteFontFileLoaderMirror));

                    Value->UnregisterFontFileLoader((IDWriteFontFileLoader*)pIDWriteFontFileLoaderMirror.ToPointer());
                    Marshal.Release(pIDWriteFontFileLoaderMirror);
                    _managedFactory._wpfFontFileLoader = null;
                }

                // Release the IDWriteFactory2 interface obtained via QueryInterface.
                if (_managedFactory._pFactory2 != null)
                {
                    ((IDWriteFactory*)_managedFactory._pFactory2)->Release();
                    _managedFactory._pFactory2 = null;
                }

                return base.ReleaseHandle();
            }
        }
    }
}
