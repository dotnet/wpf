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
    internal static class DevModeICMIntents
    {
        /// <summary>
        /// Color matching should optimize for color saturation. This value is the most appropriate choice for business graphs when dithering is not desired.
        /// </summary>
        public static readonly uint DMICM_SATURATE = 1;

        /// <summary>
        /// Color matching should optimize for color contrast. This value is the most appropriate choice for scanned or photographic images when dithering is desired.
        /// </summary>
        public static readonly uint DMICM_CONTRAST = 2;

        /// <summary>
        /// Color matching should optimize to match the exact color requested. This value is most appropriate for use with business logos or other images when an exact color match is desired.
        /// </summary>
        public static readonly uint DMICM_COLORIMETRIC = 3;

        /// <summary>
        /// Color matching should optimize to match the exact color requested without white point mapping. This value is most appropriate for use with proofing.
        /// </summary>
        public static readonly uint DMICM_ABS_COLORIMETRIC = 4;
    }
}