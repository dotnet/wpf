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
    internal static class DevModeMediaTypes
    {
        public static bool IsCustom(short mediaTypeCode)
        {
            return mediaTypeCode >= DMMEDIA_USER;
        }

        /// <summary>
        /// Plain paper.
        /// </summary>
        public const uint DMMEDIA_STANDARD = 0x0001;

        /// <summary>
        /// Transparent film.
        /// </summary>
        public const uint DMMEDIA_TRANSPARENCY = 0x0002;

        /// <summary>
        /// Glossy paper.
        /// </summary>
        public const uint DMMEDIA_GLOSSY = 0x0003;

        private static readonly uint DMMEDIA_USER = 0x100;
    }
}