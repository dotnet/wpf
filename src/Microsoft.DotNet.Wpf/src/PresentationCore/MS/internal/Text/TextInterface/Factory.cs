// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
                if (faceIndex >= numberOfFaces)
                {
                    throw new ArgumentOutOfRangeException("faceIndex");
                }

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

                return base.ReleaseHandle();
            }
        }
    }
}
