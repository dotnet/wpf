// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FONT_H
#define __FONT_H

#include "Common.h"
#include "FontWeight.h"
#include "FontStyle.h"
#include "FontMetrics.h"
#include "FontStretch.h"
#include "FontSimulation.h"
#include "LocalizedStrings.h"
#include "InformationalStringID.h"
#include "FontFace.h"
#include "NativePointerWrapper.h"

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /*******************************************************************************************************************************/
    //Forward declaration of FontFamily since there was a circular reference between "Font" & "FontFamily"
    ref class FontFamily;
    /*******************************************************************************************************************************/  

    /// <summary>
    /// Represents a physical font in a font collection.
    /// </summary>
    private ref class Font sealed
    {
        private:

            /// <summary>
            /// An entry in the _fontFaceCache array, maps Font to FontFace.
            /// </summary>
            value struct FontFaceCacheEntry
            {
                Font^ font;
                FontFace^ fontFace;
            };

            /// <summary>
            /// The DWrite font object that this class wraps.
            /// </summary>
            NativeIUnknownWrapper<IDWriteFont>^ _font;

            /// <summary>
            /// The font's version number.
            /// </summary>
            double _version;

            /// <summary>
            /// FontMetrics for this font. Lazily allocated.
            /// </summary>
            FontMetrics^ _fontMetrics;
            /// <summary>
            /// Flags reflecting the state of the object.
            /// </summary>
            int _flags;

            /// <summary>
            /// Mutex used to control access to _fontFaceCache, which is locked when
            /// _mutex > 0.
            /// </summary>
            static int _mutex;

            /// <summary>
            /// Size of the _fontFaceCache, maximum number of FontFace instances cached.
            /// </summary>
            /// <remarks>
            /// Cache size could be based upon measurements of the TextFormatter micro benchmarks.
            /// English test cases allocate 1 - 3 FontFace instances, at the opposite extreme
            /// the Korean test maxes out at 13.  16 looks like a reasonable cache size.
            ///
            /// However, dwrite circa win7 has an issue aggressively consuming address space and
            /// therefore we need to be conservative holding on to font references.
            /// 

            static const int _fontFaceCacheSize = 4;

            /// <summary>
            /// Cached FontFace instances.
            /// </summary>
            static array<FontFaceCacheEntry>^ _fontFaceCache;

            /// <summary>
            /// Most recently used element in the FontFace cache.
            /// </summary>
            static int _fontFaceCacheMRU;

            /// <summary>
            /// Adds a new FontFace to the cache, discarding an older entry if necessary.
            /// </summary>
            FontFace^ AddFontFaceToCache();

            /// <summary>
            /// Does a linear search through the FontFace cache, looking for a match for the current Font.
            /// </summary>
            FontFace^ LookupFontFaceSlow();

            /// <summary>
            /// Creates a font face object for the font.
            /// </summary>
            /// <returns>The newly created font face object.</returns>
            FontFace^ CreateFontFace();

        internal:

            /// <summary>
            /// Contructs a Font object.
            /// </summary>
            /// <param name="font">The DWrite font object that this class wraps.</param>
            Font(
                IDWriteFont* font
                );

            /// <summary>
            /// Gets the pointer to the DWrite Font object.
            /// </summary>
            property System::IntPtr DWriteFontAddRef
            {
                System::IntPtr get();
            }

            /// <summary>
            /// Gets the FontFamily that this Font belongs to.
            /// </summary>
            property FontFamily^ Family
            {                
                FontFamily^ get();
            }

            /// <summary>
            /// Gets the weight of the font.
            /// </summary>
            property FontWeight Weight
            {
                FontWeight get();
            }

            /// <summary>
            /// Gets the stretch of the font.
            /// </summary>
            property FontStretch Stretch
            {
                FontStretch get();
            }

            /// <summary>
            /// Gets the style of the font.
            /// </summary>
            property FontStyle Style
            {
                FontStyle get();
            }

            /// <summary>
            /// Returns whether this is a symbol font.
            /// </summary>
            property bool IsSymbolFont
            {
                bool get();
            }

            /// <summary>
            /// Gets a localized strings collection containing the face names for the font (e.g., Regular or Bold), indexed by locale name.
            /// </summary>
            property LocalizedStrings^ FaceNames
            {
                LocalizedStrings^ get();
            }

            /// <summary>
            /// Gets the simulation flags.
            /// </summary>
            property FontSimulations SimulationFlags
            {
                FontSimulations get();
            }

            /// <summary>
            /// Gets the font metrics.
            /// </summary>
            property FontMetrics^ Metrics
            {
                FontMetrics^ get();
            }

            /// <summary>
            /// Gets the version of the font.
            /// </summary>
            property double Version
            {
                double get();
            }

            /// <summary>
            /// Gets the font metrics for display device.
            /// </summary>
            FontMetrics^ DisplayMetrics(FLOAT emSize, FLOAT pixelsPerDip);

            /// <summary>
            /// Clears the FontFace cache, releasing all native resources.
            /// </summary>
            /// <remarks>
            /// This method does not guarantee that the cache will be cleared.
            /// If the cache is busy, nothing happens.
            /// </remarks>
            static void ResetFontFaceCache();

            /// <summary>
            /// Returns a FontFace matching this Font.
            /// </summary>
            /// <remarks>
            /// Caller must use FontFace::Release to free native resources allocated by the FontFace.
            /// While FontFace does have a finalizer, it is not hard to exhaust available address space
            /// by enumerating all installed FontFaces synchronously, before the gc has a chance to
            /// kick off finalization.
            /// </remarks>
            FontFace^ GetFontFace();

            /// <summary>
            /// Gets a localized strings collection containing the specified informational strings, indexed by locale name.
            /// </summary>
            /// <param name="informationalStringID">Identifies the string to get.</param>
            /// <param name="informationalStrings">Receives the newly created localized strings object.</param>
            /// <returns>Whether the requested string was found or not.</returns>
            bool GetInformationalStrings(
                                        InformationalStringID                                      informationalStringID,
                                        [System::Runtime::InteropServices::Out] LocalizedStrings^% informationalStrings
                                        );

            /// <summary>
            /// Determines whether the font supports the specified character.
            /// </summary>
            /// <param name="unicodeValue">Unicode (UCS-4) character value.</param>
            /// <returns>TRUE if the font supports the specified character or FALSE if not.</returns>
            bool HasCharacter(
                             UINT32 unicodeValue
                             );
    };
}}}}//MS::Internal::Text::TextInterface

#endif //__FONT_H
