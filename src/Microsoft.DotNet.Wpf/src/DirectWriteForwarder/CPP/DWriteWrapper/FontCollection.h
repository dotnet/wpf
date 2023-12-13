// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FONTCOLLECTION_H
#define __FONTCOLLECTION_H

#include "Common.h"
#include "FontFamily.h"
#include "Font.h"
#include "FontFace.h"
#include "NativePointerWrapper.h"

using namespace MS::Internal::Text::TextInterface::Generics;

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <summary>
    /// The FontCollection encapsulates a collection of fonts.
    /// </summary>
    private ref class FontCollection sealed
    {
        private:

            /// <summary>
            /// The DWrite font collection.
            /// </summary>
            NativeIUnknownWrapper<IDWriteFontCollection>^ _fontCollection;

        internal:

            /// <summary>
            /// Contructs a FontCollection object.
            /// </summary>
            /// <param name="fontCollection">The DWrite font collection object that this class wraps.</param>
            FontCollection(IDWriteFontCollection* fontCollection); 

            /// <summary>
            /// Gets the number of families in this font collection.
            /// </summary>
            property unsigned int FamilyCount
            {
                unsigned int get();
            }            

            /// <summary>
            /// Gets a font family by index.
            /// </summary>
            property FontFamily^ default[unsigned int]
            {
                FontFamily^ get(unsigned int familyIndex);
            }

            /// <summary>
            /// Gets a font family by name.
            /// </summary>
            property FontFamily^ default[System::String^]
            {
                FontFamily^ get(System::String^ familyName);
            }

            /// <summary>
            /// Finds the font family with the specified family name.
            /// </summary>
            /// <param name="familyName">Name of the font family. The name is not case-sensitive but must otherwise exactly match a family name in the collection.</param>
            /// <param name="index">Receives the zero-based index of the matching font family if the family name was found or UINT_MAX otherwise.</param>
            /// <returns>TRUE if the family name exists or FALSE otherwise.</returns>
            bool FindFamilyName(
                                                                       System::String^ familyName,
                               [System::Runtime::InteropServices::Out] unsigned int%   index
                               );

            /// <summary>
            /// Gets the font object that corresponds to the same physical font as the specified font face object. The specified physical font must belong 
            /// to the font collection.
            /// </summary>
            /// <param name="fontFace">Font face object that specifies the physical font.</param>
            /// <returns>The newly created font object if successful or NULL otherwise.</returns>
            Font^ GetFontFromFontFace(FontFace^ fontFace);
    };

}}}}//MS::Internal::Text::TextInterface

#endif //__FONTCOLLECTION_H
