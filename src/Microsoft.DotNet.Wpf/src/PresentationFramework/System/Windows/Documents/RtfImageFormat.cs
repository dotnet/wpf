// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: RtfImageFormat which indicates the image format type.
//

namespace System.Windows.Documents
{
    /// <summary>
    /// Rtf image format enumeration indicates whether an image is a bitmap,
    /// png, jpeg, gif, tif, dib or windows metafile etc.
    /// </summary>
    internal enum RtfImageFormat
    {
        Unknown,
        Bmp,
        Dib,
        Emf,
        Exif,
        Gif,
        Jpeg,
        Png,
        Tif,
        Wmf
    }
}
