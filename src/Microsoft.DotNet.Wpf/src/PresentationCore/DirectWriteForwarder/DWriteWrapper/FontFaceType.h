// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FONTFACETYPE_H
#define __FONTFACETYPE_H

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <summary>
    /// The file format of a complete font face.
    /// Font formats that consist of multiple files, e.g. Type 1 .PFM and .PFB, have
    /// a single enum entry.
    /// </summary>
    private enum class FontFaceType
    {
        /// <summary>
        /// OpenType font face with CFF outlines.
        /// </summary>
        CFF,

        /// <summary>
        /// OpenType font face with TrueType outlines.
        /// </summary>
        TrueType,

        /// <summary>
        /// OpenType font face that is a part of a TrueType collection.
        /// </summary>
        TrueTypeCollection,

        /// <summary>
        /// A Type 1 font face.
        /// </summary>
        Type1,

        /// <summary>
        /// A vector .FON format font face.
        /// </summary>
        Vector,

        /// <summary>
        /// A bitmap .FON format font face.
        /// </summary>
        Bitmap,

        /// <summary>
        /// Font face type is not recognized by the DirectWrite font system.
        /// </summary>
        Unknown
    };

}}}}//MS::Internal::Text::TextInterface

#endif //__FONTFACETYPE_H