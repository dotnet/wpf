// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FONTFILETYPE_H
#define __FONTFILETYPE_H

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <summary>
    /// The type of a font represented by a single font file.
    /// Font formats that consist of multiple files, e.g. Type 1 .PFM and .PFB, have
    /// separate enum values for each of the file type.
    /// </summary>
    private enum class FontFileType
    {
        /// <summary>
        /// Font type is not recognized by the DirectWrite font system.
        /// </summary>
        Unknown,

        /// <summary>
        /// OpenType font with CFF outlines.
        /// </summary>
        CFF,

        /// <summary>
        /// OpenType font with TrueType outlines.
        /// </summary>
        TrueType,

        /// <summary>
        /// OpenType font that contains a TrueType collection.
        /// </summary>
        TrueTypeCollection,

        /// <summary>
        /// Type 1 PFM font.
        /// </summary>
        Type1PFM,

        /// <summary>
        /// Type 1 PFB font.
        /// </summary>
        Type1PFB,

        /// <summary>
        /// Vector .FON font.
        /// </summary>
        Vector,

        /// <summary>
        /// Bitmap .FON font.
        /// </summary>
        Bitmap
    };

}}}}//MS::Internal::Text::TextInterface

#endif //__FONTFILETYPE_H