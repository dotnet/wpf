// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FACTORY_H
#define __FACTORY_H

#include "Common.h"
#include "FactoryType.h"
#include "FontFile.h"
#include "FontFace.h"
#include "FontCollection.h"
#include "DWriteTypeConverter.h"
#include "IFontSourceCollection.h"
#include "FontCollectionLoader.h"
#include "FontFileLoader.h"
#include "TextAnalyzer.h"
#include "NativePointerWrapper.h"

using namespace MS::Internal::Text::TextInterface::Generics;
using namespace System::Windows::Threading;

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <summary>
    /// The root factory interface for all DWrite objects.
    /// </summary>
    private ref class Factory sealed : public CriticalHandle
    {
        private:

            /// <summary>
            /// A pointer to the wrapped DWrite factory object.
            /// </summary>
            IDWriteFactory* _pFactory;
                      
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
            Factory(
                   FactoryType                   factoryType,
                   IFontSourceCollectionFactory^ fontSourceCollectionFactory,
                   IFontSourceFactory^           fontSourceFactory
                   );

            /// <summary>
            /// Initializes a factory object.
            /// </summary>
            /// <param name="factoryType">Identifies whether the factory object will be shared or isolated.</param>
            void Initialize(FactoryType factoryType);

            /// <summary>
            /// The custom loader used by WPF to load font collections.
            /// </summary>
            FontCollectionLoader^ _wpfFontCollectionLoader;

            /// <summary>
            /// The custom loader used by WPF to load font files.
            /// </summary>
            FontFileLoader^       _wpfFontFileLoader;

            IFontSourceFactory^   _fontSourceFactory;

            [ThreadStatic]
            static Dictionary<System::Uri^,System::Runtime::InteropServices::ComTypes::FILETIME>^ _timeStampCache;
            
            [ThreadStatic]
            static DispatcherOperation^ _timeStampCacheCleanupOp;
            
            static void CleanupTimeStampCache();

        protected:

            #pragma warning (disable : 4950) // The Constrained Execution Region (CER) feature is not supported.  
            [ReliabilityContract(Consistency::WillNotCorruptState, Cer::Success)]
            #pragma warning (default : 4950) // The Constrained Execution Region (CER) feature is not supported.  
            virtual bool ReleaseHandle() override;

        internal:

            property IDWriteFactory* DWriteFactoryAddRef
            {
                IDWriteFactory* get()
                {
                    _pFactory->AddRef();
                    return _pFactory;
                }
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
            static Factory^ Create(
                                  FactoryType                   factoryType,
                                  IFontSourceCollectionFactory^ fontSourceCollectionFactory,
                                  IFontSourceFactory^           fontSourceFactory
                                  );


            /// <summary>
            /// Creates a font file object from a local font file.
            /// </summary>
            /// <param name="filePathUri">file path uri.</param>
            /// <returns>
            /// Newly created font file object, or NULL in case of failure.
            /// </returns>
            FontFile^ CreateFontFile(System::Uri^ filePathUri);

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
            FontFace^ CreateFontFace(
                                    System::Uri^    filePathUri,
                                    unsigned int    faceIndex,
                                    FontSimulations fontSimulationFlags
                                    );

            /// <summary>
            /// Creates a font face object.
            /// </summary>
            /// <param name="filePathUri">The file path of the font face.</param>
            /// <param name="faceIndex">The zero based index of a font face in cases when the font files contain a collection of font faces.
            /// If the font files contain a single face, this value should be zero.</param>
            /// <returns>
            /// Newly created font face object, or NULL in case of failure.
            /// </returns>
            FontFace^ CreateFontFace(
                                    System::Uri^ filePathUri,
                                    unsigned int faceIndex
                                    );

            /// <summary>
            /// Gets a font collection representing the set of installed fonts.
            /// </summary>
            /// <returns>
            /// The system font collection.
            /// </returns>
            FontCollection^ GetSystemFontCollection();

            /// <summary>
            /// Gets a font collection in a custom location.
            /// </summary>
            /// <param name="uri">The uri of the font collection.</param>
            /// <returns>
            /// The font collection.
            /// </returns>
            FontCollection^ GetFontCollection(System::Uri^ uri);

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
            FontCollection^ GetSystemFontCollection(bool checkForUpdates);

            static bool IsLocalUri(System::Uri^ uri)
            {
                return (uri->IsFile && uri->IsLoopback && !uri->IsUnc);
            }

            /// <summary>
            /// Creates an IDWriteFontFile* from a URI, using either the DWrite built in local font file loader or our custom font 
            /// file loader implementation
            /// </summary>
            /// <param name="factory">The IDWriteFactory object<param>
            /// <param name="fontFileLoader">Reference to our previously created and registered custom font file loader</param>
            /// <param name="filePathUri">The URI</param>
            /// <param name="dwriteFontFile">The newly created IDWRiteFontFile representation</param>
            /// <returns>
            /// Standard HRESULT error code
            /// </returns>
            static HRESULT CreateFontFile(
                                         IDWriteFactory*         factory,
                                         FontFileLoader^         fontFileLoader,
                                         System::Uri^            filePathUri,
                                         __out IDWriteFontFile** dwriteFontFile
                                         );

            __declspec(noinline) static DWRITE_MATRIX GetIdentityTransform()
            {
                DWRITE_MATRIX transform;
                transform.m11=1;
                transform.m12=0;
                transform.m22=1;
                transform.m21=0;
                transform.dx =0;
                transform.dy =0;

                return transform;
            }

            TextAnalyzer^ CreateTextAnalyzer();

        public:

            virtual property bool IsInvalid
            {
                #pragma warning (disable : 4950) // The Constrained Execution Region (CER) feature is not supported.  
                [ReliabilityContract(Consistency::WillNotCorruptState, Cer::Success)]
                #pragma warning (default : 4950) // The Constrained Execution Region (CER) feature is not supported.  
                bool get() override;
            }
    };

}}}}//MS::Internal::Text::TextInterface

#endif //__FACTORY_H
