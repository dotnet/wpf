// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FONTFACE_H
#define __FONTFACE_H

#include "Common.h"
#include "FontFaceType.h"
#include "FontSimulation.h"
#include "FontFile.h"
#include "FontWeight.h"
#include "FontStyle.h"
#include "FontStretch.h"
#include "FontMetrics.h"
#include "GlyphMetrics.h"
#include "DWriteMatrix.h"
#include "OpenTypeTableTag.h"
#include "NativePointerWrapper.h"

using namespace MS::Internal::Text::TextInterface::Generics;

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{   
    /// <summary>
    /// An absolute reference to a font face.
    /// It contains font face type, appropriate file references and face identification data.
    /// Various font data such as metrics, names and glyph outlines is obtained from FontFace.
    /// </summary>
    private ref class FontFace sealed
    {
        private:

            /// <summary>
            /// The DWrite font face object.
            /// </summary>
            NativeIUnknownWrapper<IDWriteFontFace>^ _fontFace;

            /// <summary>
            /// FontMetrics for this font. Lazily allocated.
            /// </summary>
            FontMetrics^ _fontMetrics;

            /// <summary>
            /// Reference count.
            /// </summary>
            /// <remarks>
            /// FontFace instances are ref counted to manage the lifetime of associated native
            /// resources.
            /// </remarks>
            int _refCount;

        internal:

            /// <summary>
            /// Constructs a font face object.
            /// </summary>
            /// <param name="fontFace">A pointer to the DWrite font face object.</param>
            FontFace(IDWriteFontFace* fontFace);

            /// <summary>
            /// Gets the pointer to the DWrite Font Face object.
            ///
            /// WARNING: AFTER GETTING THIS NATIVE POINTER YOU ARE RESPONSIBLE FOR MAKING SURE THAT THE WRAPPING MANAGED
            /// OBJECT IS KEPT ALIVE BY THE GC OR ELSE YOU ARE RISKING THE POINTER GETTING RELEASED BEFORE YOU'D 
            /// WANT TO
            /// </summary>
            property IDWriteFontFace* DWriteFontFaceNoAddRef
            {
                IDWriteFontFace* get();
            }

            /// <summary>
            /// Gets the pointer to the DWrite Font Face object.
            /// </summary>
            property System::IntPtr DWriteFontFaceAddRef
            {
                System::IntPtr get();
            }

            /// <summary>
            /// Gets the file format type of a font face.
            /// </summary>
            property FontFaceType Type
            {
                FontFaceType get();
            }

            /// <summary>
            /// Gets the index of a font face in the context of its font files.
            /// </summary>
            property unsigned int Index
            {
                unsigned int get();
            }

            /// <summary>
            /// Gets the algorithmic style simulation flags of a font face.
            /// </summary>
            property FontSimulations SimulationFlags
            {
                FontSimulations get();
            }

            /// <summary>
            /// Gets whether the font is a symbol font.
            /// </summary>
            property bool IsSymbolFont
            {
                bool get();
            }


            /// <summary>
            /// Gets design units and common metrics for the font face.
            /// These metrics are applicable to all the glyphs within a fontface and are used by applications for layout calculations.
            /// </summary>
            property FontMetrics^ Metrics
            {
                FontMetrics^ get();
            }

            /// <summary>
            /// Gets the number of glyphs in the font face.
            /// </summary>
            property UINT16 GlyphCount
            {
                UINT16 get();
            }

            /// <summary>
            /// Gets the first font file representing the font face.
            /// </summary>
            FontFile^ GetFileZero();

            /// <summary>
            /// Increments the reference count on this FontFace.
            /// </summary>
            void AddRef()
            {
                System::Threading::Interlocked::Increment(_refCount);
            }

            /// <summary>
            /// Decrements the reference count on this FontFace.
            /// </summary>
            void Release()
            {
                if (-1 == System::Threading::Interlocked::Decrement(_refCount))
                {
                    // At this point we know the FontFace is eligable for the finalizer queue.
                    // However, because native dwrite font faces consume enormous amounts of 
                    // address space, we need to be aggressive about disposing immediately.
                    // If we rely solely on the GC finalizer, we will exhaust available address
                    // space in reasonable scenarios like enumerating all installed fonts.
                    delete this;
                }
            }

            /// <summary>
            /// Obtains ideal glyph metrics in font design units. Design glyphs metrics are used for glyph positioning.
            /// </summary>
            /// <param name="glyphIndices">An array of glyph indices to compute the metrics for.</param>
            /// <param name="pGlyphMetrics">Unsafe pointer to flat array of GlyphMetrics structs for output. Passed as
            /// unsafe to allow optimization by the caller of stack or heap allocation.
            /// The metrics returned are in font design units</param>
            void GetDesignGlyphMetrics(
                __in_ecount(glyphCount) const UINT16 *pGlyphIndices,
                UINT32 glyphCount,
                __out_ecount(glyphCount) GlyphMetrics *pGlyphMetrics
                );

            void GetDisplayGlyphMetrics(
                __in_ecount(glyphCount) const UINT16 *pGlyphIndices,
                UINT32 glyphCount,
                __out_ecount(glyphCount) GlyphMetrics *pGlyphMetrics,
                FLOAT emSize,
                bool useDisplayNatural,
                bool isSideways,
                float pixelsPerDip
                );


            /// <summary>
            /// Returns the nominal mapping of UCS4 Unicode code points to glyph indices as defined by the font 'CMAP' table.
            /// Note that this mapping is primarily provided for line layout engines built on top of the physical font API.
            /// Because of OpenType glyph substitution and line layout character substitution, the nominal conversion does not always correspond
            /// to how a Unicode string will map to glyph indices when rendering using a particular font face.
            /// Also, note that Unicode Variant Selectors provide for alternate mappings for character to glyph.
            /// This call will always return the default variant.
            /// </summary>
            /// <param name="codePoints">An array of USC4 code points to obtain nominal glyph indices from.</param>
            /// <returns>Array of nominal glyph indices filled by this function.</returns>
            // "GetGlyphIndices" is defined in WinGDI.h to be "GetGlyphIndicesW" that why we chose
            // "GetArrayOfGlyphIndices"
            void GetArrayOfGlyphIndices(
                __in_ecount(glyphCount) const UINT32* pCodePoints,
                UINT32 glyphCount,
                __out_ecount(glyphCount) UINT16* pGlyphIndices
                );

            /// <summary>
            /// Finds the specified OpenType font table if it exists and returns a pointer to it.            
            /// </summary>
            /// <param name="openTypeTableTag">The tag of table to find.</param>
            /// <param name="tableData">The table.</param>
            /// <returns>True if table exists.</returns>
            bool TryGetFontTable(
                                                                        OpenTypeTableTag openTypeTableTag,         
                                [System::Runtime::InteropServices::Out] array<byte>^%    tableData
                                );

            /// <summary>
            /// Reads the FontEmbeddingRights
            /// </summary>
            /// <param name="tableData">The font embedding rights value.</param>
            /// <returns>True if os2 table exists and the FontEmbeddingRights was read successfully.</returns>
            bool ReadFontEmbeddingRights([System::Runtime::InteropServices::Out] unsigned short% fsType);

            /// <summary>
            /// dtor.
            /// </summary>
            ~FontFace();
    };

}}}}//MS::Internal::Text::TextInterface

#endif //__FONTFACE_H
