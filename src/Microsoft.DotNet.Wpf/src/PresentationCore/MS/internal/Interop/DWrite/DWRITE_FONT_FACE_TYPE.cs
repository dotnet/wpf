// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Interop.DWrite
{
    internal enum DWRITE_FONT_FACE_TYPE
    {
        DWRITE_FONT_FACE_TYPE_CFF,
        DWRITE_FONT_FACE_TYPE_TRUETYPE,
        DWRITE_FONT_FACE_TYPE_OPENTYPE_COLLECTION,
        DWRITE_FONT_FACE_TYPE_TYPE1,
        DWRITE_FONT_FACE_TYPE_VECTOR,
        DWRITE_FONT_FACE_TYPE_BITMAP,
        DWRITE_FONT_FACE_TYPE_UNKNOWN,
        DWRITE_FONT_FACE_TYPE_RAW_CFF,
        DWRITE_FONT_FACE_TYPE_TRUETYPE_COLLECTION = DWRITE_FONT_FACE_TYPE_OPENTYPE_COLLECTION,
    }
}
