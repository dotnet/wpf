using System;
using System.Runtime.InteropServices;
using MS.Internal.Interop;
using MS.Internal.Interop.DWrite;
using MS.Internal.Text.TextInterface.Interfaces;

namespace MS.Internal.Text.TextInterface
{
    internal unsafe class Factory
    {
        private const int DWRITE_E_FILEFORMAT = unchecked((int)0x88985000L);
        private NativeIUnknownWrapper<IDWriteFactory> _factory;

        private IFontSourceFactory _fontSourceFactory;
        private FontFileLoader _wpfFontFileLoader;
        private FontCollectionLoader _wpfFontCollectionLoader;

        // b859ee5a-d838-4b5b-a2e8-1adc7d93db48
        private static readonly Guid IID_IDWriteFactory = new Guid(0xb859ee5a, 0xd838, 0x4b5b, 0xa2, 0xe8, 0x1a, 0xdc, 0x7d, 0x93, 0xdb, 0x48);

        private Factory(IDWriteFactory* nativePointer)
        {
            _factory = new NativeIUnknownWrapper<IDWriteFactory>(nativePointer);
        }

        internal IDWriteFactory* DWriteFactoryAddRef
        {
            get
            {
                _factory.Value->AddReference();

                return _factory.Value;
            }
        }

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

        private void Initialize(FactoryType factoryType)
        {
            Guid iid = IID_IDWriteFactory;
            IDWriteFactory* factory = null;

            delegate* unmanaged<int, void*, void*, int> pfnDWriteCreateFactory = DWriteLoader.GetDWriteCreateFactoryFunctionPointer();

            int hr = pfnDWriteCreateFactory((int)DWriteTypeConverter.Convert(factoryType), &iid, &factory);

            DWriteUtil.ConvertHresultToException(hr);

            _factory = new NativeIUnknownWrapper<IDWriteFactory>(factory);
        }

        internal static Factory Create(
            FactoryType factoryType,
            IFontSourceCollectionFactory fontSourceCollectionFactory,
            IFontSourceFactory fontSourceFactory)
        {
            return new Factory(factoryType, fontSourceCollectionFactory, fontSourceFactory);
        }

        internal TextAnalyzer CreateTextAnalyzer()
        {
            IDWriteTextAnalyzer* textAnalyzer = null;

            _factory.Value->CreateTextAnalyzer(&textAnalyzer);

            return new TextAnalyzer((Native.IDWriteTextAnalyzer*)textAnalyzer);
        }

        internal FontFile CreateFontFile(Uri filePathUri)
        {
            Native.IDWriteFontFile* dwriteFontFile = null;
            int hr = InternalFactory.CreateFontFile((Native.IDWriteFactory*)_factory.Value, null, filePathUri, &dwriteFontFile);

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

        internal FontFace CreateFontFace(Uri filePathUri, uint faceIndex)
        {
            return CreateFontFace(
                                 filePathUri,
                                 faceIndex,
                                 FontSimulations.None
                                 );
        }

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

        internal FontCollection GetSystemFontCollection()
        {
            return GetSystemFontCollection(false);
        }

        private FontCollection GetSystemFontCollection(bool checkForUpdate)
        {
            IDWriteFontCollection* fontCollection = null;
            int checkForUpdateInt = checkForUpdate ? 1 : 0;
            int hr = _factory.Value->GetSystemFontCollection(&fontCollection, checkForUpdateInt);

            DWriteUtil.ConvertHresultToException(hr);

            return new FontCollection((Native.IDWriteFontCollection*)fontCollection);
        }

        internal FontCollection GetFontCollection(Uri uri)
        {
            IDWriteFontCollection* fontCollection = null;

            string uriString = uri.AbsoluteUri;

            fixed (char* uriStringPtr = uriString)
            {
                uint collectionKeySize = (uint)((uriString.Length + 1) * sizeof(char));
                _factory.Value->CreateCustomFontCollection(null, uriStringPtr, collectionKeySize, &fontCollection);
            }

            return new FontCollection((Native.IDWriteFontCollection*)fontCollection);
        }

        internal static bool IsLocalUri(Uri uri)
        {
            return uri.IsFile && uri.IsLoopback && !uri.IsUnc;
        }
    }
}
