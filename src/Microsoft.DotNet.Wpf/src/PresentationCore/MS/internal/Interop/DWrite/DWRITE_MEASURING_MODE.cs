// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Interop.DWrite
{
    /// <summary>
    /// The measuring method used for text layout.
    /// </summary>
    internal enum DWRITE_MEASURING_MODE
    {
        /// <summary>
        /// Text is measured using glyph ideal metrics whose values are independent to the current display resolution.
        /// </summary>
        DWRITE_MEASURING_MODE_NATURAL,

        /// <summary>
        /// Text is measured using glyph display compatible metrics whose values tuned for the current display resolution.
        /// </summary>
        DWRITE_MEASURING_MODE_GDI_CLASSIC,

        /// <summary>
        // Text is measured using the same glyph display metrics as text measured by GDI using a font
        // created with CLEARTYPE_NATURAL_QUALITY.
        /// </summary>
        DWRITE_MEASURING_MODE_GDI_NATURAL
    }
}
