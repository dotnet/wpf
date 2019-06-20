// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FONTFAMILY_H
#define __FONTFAMILY_H

#include "Common.h"
#include "LocalizedStrings.h"
#include "Font.h"
#include "FontList.h"
#include "FontWeight.h"
#include "FontStretch.h"
#include "FontStyle.h"

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <summary>
    /// Represents a set of fonts that share the same design but are differentiated
    /// by weight, stretch, and style.
    /// </summary>
    private ref class FontFamily sealed : public FontList
    {
        private:

            /// <summary>
            /// A reference to the regular font in this family. This is used by upper layers in WPF.
            /// </summary>
            Font^ _regularFont;

        internal:

            /// <summary>
            /// Contructs a FontFamily object.
            /// </summary>
            /// <param name="fontFamily">The DWrite font family object that this class wraps.</param>
            FontFamily(IDWriteFontFamily* fontFamily);

            /// <summary>
            /// Gets a localized strings object that contains the family names for the font family, indexed by locale name.
            /// </summary>
            property LocalizedStrings^ FamilyNames
            {
                LocalizedStrings^ get();
            }

            /// <summary>
            /// Gets whether this FontFamily is a physical one.
            /// </summary>
            property bool IsPhysical
            {
                bool get();
            }

            /// <summary>
            /// Gets whether this FontFamily is a composite one.
            /// </summary>
            property bool IsComposite
            {
                bool get();
            }

            /// <summary>
            /// Returns a name that uniquely identifies this family.
            /// The name culture doesn't matter, as the name is supposed to be used only
            /// for FontFamily construction. This is used by WPF only.
            /// </summary>
            property System::String^ OrdinalName
            {
                System::String^ get();
            }


            /// <summary>
            /// Gets the font metrics for the regular font in this family.
            /// </summary>
            property FontMetrics^ Metrics
            {
                FontMetrics^ get();
            }

            FontMetrics^ DisplayMetrics(float emSize, float pixelsPerDip);

            /// <summary>
            /// Gets the font that best matches the specified properties.
            /// </summary>
            /// <param name="weight">Requested font weight.</param>
            /// <param name="stretch">Requested font stretch.</param>
            /// <param name="style">Requested font style.</param>
            /// <returns>The newly created font object.</returns>
            Font^ GetFirstMatchingFont(
                                      FontWeight  weight,
                                      FontStretch stretch,
                                      FontStyle   style
                                      );

            /// <summary>
            /// Gets a list of fonts in the font family ranked in order of how well they match the specified properties.
            /// </summary>
            /// <param name="weight">Requested font weight.</param>
            /// <param name="stretch">Requested font stretch.</param>
            /// <param name="style">Requested font style.</param>
            /// <returns>The newly created font object.</returns>
            FontList^ GetMatchingFonts(
                                      FontWeight  weight,
                                      FontStretch stretch,
                                      FontStyle   style
                                      );        
    };

}}}}//MS::Internal::Text::TextInterface

#endif //__FONTFAMILY_H
