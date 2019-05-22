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
    internal static class DevModeResolutions
    {
        /// <summary>
        /// Returns true if the resolution represents DPI
        /// </summary>
        /// <param name="xResolution"></param>
        /// <returns></returns>
        public static bool IsCustom(short xResolution)
        {
            return xResolution > 0;
        }

        /// <summary>
        /// High-resolution printouts
        /// </summary>
        public const short DMRES_HIGH = unchecked((short)0xFFFC);

        /// <summary>
        /// Medium-resolution printouts
        /// </summary>
        public const short DMRES_MEDIUM = unchecked((short)0xFFFD);

        /// <summary>
        /// Low-resolution printouts
        /// </summary>
        public const short DMRES_LOW = unchecked((short)0xFFFE);

        /// <summary>
        /// Draft-resolution printouts
        /// </summary>
        public const short DMRES_DRAFT = unchecked((short)0xFFFF);
    }
}