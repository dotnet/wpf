// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FONTFILE_H
#define __FONTFILE_H

#include "Common.h"
#include "FontFaceType.h"
#include "FontFileType.h"
#include "NativePointerWrapper.h"

using namespace MS::Internal::Text::TextInterface::Generics;

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <summary>
    /// Represents a Font File.
    /// </summary> 
    private ref class FontFile sealed
    {            
        private:

            /// <summary>
            /// A pointer to the DWrite font file object.
            /// </summary>
            NativeIUnknownWrapper<IDWriteFontFile>^ _fontFile;

            /// <summary>
            /// This variable stores the GUID of the IDWriteLocalFontFileLoader interface.
            /// The reason we are not using __uuidof(IDWriteLocalFontFileLoader) is because the complier generates a global
            /// variable and a static method to initialize it which is not annotated properly with security tags.
            /// This makes the static method fail NGENing and causes Jitting which affects perf.
            /// If the complier gets fixed then we can remove this scheme and use __uuidof(IDWriteLocalFontFileLoader).
            /// </summary>
            static NativePointerWrapper<_GUID>^ _guidForIDWriteLocalFontFileLoader;

            /// <summary>
            /// static ctor to initialize the GUID of IDWriteLocalFontFileLoader interface.
            /// </summary>
            static FontFile();

            /// <summary>
            /// This method is used to release an IDWriteLocalFontFileLoader. This method
            /// is created to be marked with proper security attributes because when
            /// the call to Release() was made inside GetUriPath() it was causing Jitting.
            /// </summary>
            static void ReleaseInterface(IDWriteLocalFontFileLoader** ppInterface);
            
        internal:

            /// <summary>
            /// Constructs a Font File object.
            /// </summary>
            /// <param name="fontFile">A pointer to the DWrite fontFile object.</param>
            FontFile(IDWriteFontFile* fontFile);

            /// <summary>
            /// Gets a pointer to the DWrite font file object. This only used by the factory to construct a font face.
            /// By DWrite's design, the fontFace maintains it own references to the font file objects. Thus, after
            /// passing this pointer to font face, its OK to release it in FontFile's finalizer.
            ///
            /// WARNING: AFTER GETTING THIS NATIVE POINTER YOU ARE RESPONSIBLE FOR MAKING SURE THAT THE WRAPPING MANAGED
            /// OBJECT IS KEPT ALIVE BY THE GC OR ELSE YOU ARE RISKING THE POINTER GETTING RELEASED BEFORE YOU'D 
            /// WANT TO
            /// </summary>
            property IDWriteFontFile* DWriteFontFileNoAddRef
            {
                IDWriteFontFile* get();
            }

            /// <summary>
            /// Analyzes a file and returns whether it represents a font, and whether the font type is supported by the font system.
            /// </summary>
            /// <param name="fontFileType">The type of the font file. Note that even if isSupportedFontType is FALSE,
            /// the fontFileType value may be different from Unknown.</param>
            /// <param name="fontFaceType">The type of the font face that can be constructed from the font file
            /// if fontFileType is not equal to Unknown.</param>
            /// <param name="numberOfFaces">Number of font faces contained in the font file.</param>
            /// <param name="hr">The HRESULT resulting from DWrite.</param>
            /// <returns>TRUE if the font type is supported by the font system, FALSE otherwise.</returns>
            /// <remarks>
            /// IMPORTANT: certain font file types are recognized, but not supported by the font system.
            /// For example, the font system will recognize a file as a Type 1 font file,
            /// but will not be able to construct a font face object from it. In such situations, Analyze will set
            /// isSupportedFontType output parameter to FALSE.
            /// </remarks>
            bool Analyze(
                        [System::Runtime::InteropServices::Out] DWRITE_FONT_FILE_TYPE%  dwriteFontFileType,
                        [System::Runtime::InteropServices::Out] DWRITE_FONT_FACE_TYPE%  dwriteFontFaceType,
                        [System::Runtime::InteropServices::Out] unsigned int%  numberOfFaces,
                                                                HRESULT&       hr
                        );
            
            /// <summary>
            /// Gets the path of this font file.
            /// </summary>
            /// <returns>The path of this font file.</returns>
            System::String^ GetUriPath();

            //// <summary>
            /// dtor.
            /// </summary>
            ~FontFile();
    };

}}}}//MS::Internal::Text::TextInterface

#endif //__FONTFILE_H
