// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
All rights reserved.

--*/

namespace MS.Internal.Printing.Configuration
{
    /// <remarks>
    /// From http://msdn.microsoft.com/en-us/library/cc244659(PROT.13).aspx
    /// </remarks>
    internal static class DevModeDitherTypes
    {
        /// <summary>
        /// No dithering
        /// </summary>
        public static readonly uint DMDITHER_NONE = 0x00000001;

        /// <summary>
        /// Dithering with a coarse brush.
        /// </summary>
        public static readonly uint DMDITHER_COARSE = 0x00000002;

        /// <summary>
        /// Dithering with a fine brush.
        /// </summary>
        public static readonly uint DMDITHER_FINE = 0x00000003;

        /// <summary>
        /// Line art dithering.
        /// </summary>
        public static readonly uint DMDITHER_LINEART = 0x00000004;

        /// <summary>
        /// Windows 95/98/Me: Dithering in which an algorithm is used to spread, or diffuse, the error of approximating a specified color over adjacent pixels.
        /// In contrast, DMDITHER_COARSE, DMDITHER_FINE, and DMDITHER_LINEART use patterned halftoning to approximate a color.
        /// </summary>
        public static readonly uint DMDITHER_ERRORDIFFUSION = 0x00000005;

        /// <summary>
        /// Device does gray scaling.
        /// </summary>
        public static readonly uint DMDITHER_GRAYSCALE = 0x0000000A;
    }
}