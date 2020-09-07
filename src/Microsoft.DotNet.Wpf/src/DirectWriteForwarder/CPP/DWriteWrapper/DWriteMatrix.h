// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __DWRITEMATRIX_H
#define __DWRITEMATRIX_H

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <summary>
    /// The DWRITE_MATRIX structure specifies the graphics transform to be applied
    /// to rendered glyphs.
    /// </summary>
    private value struct DWriteMatrix sealed
    {
        /// <summary>
        /// Horizontal scaling / cosine of rotation
        /// </summary>
        FLOAT M11;

        /// <summary>
        /// Horizontal shear / sine of rotation
        /// </summary>
        FLOAT M12;

        /// <summary>
        /// Vertical shear / negative sine of rotation
        /// </summary>
        FLOAT M21;

        /// <summary>
        /// Vertical scaling / cosine of rotation
        /// </summary>
        FLOAT M22;

        /// <summary>
        /// Horizontal shift (always orthogonal regardless of rotation)
        /// </summary>
        FLOAT Dx;

        /// <summary>
        /// Vertical shift (always orthogonal regardless of rotation)
        /// </summary>
        FLOAT Dy;
    };

}}}}//MS::Internal::Text::TextInterface

#endif //__DWRITEMATRIX_H