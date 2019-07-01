// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Internal.OSVersionHelper
{
    /// <summary>
    /// Utility functions needed in calls into OS version API's.
    /// </summary>
    internal static class Util
    {
        /// <summary>
        /// Retrieves the high-order byte from the specified value.
        /// </summary>
        /// <param name="w">A 16 bit unsigned integer whose high-order byte is desired</param>
        /// <returns>The high-order byte of <paramref name="w"/></returns>
        /// <remarks> We return an uint (rather than a byte) to be compatible 
        /// with <see cref="NativeTypes.OSVERSIONINFOEX.dwMajorVersion"/></remarks>
        internal static uint HIBYTE(ushort w)
        {
            return (uint)((w >> 8) & 0xFF);
        }

        /// <summary>
        /// Retrieves the low-order byte from the given 16-bit value.
        /// </summary>
        /// <param name="w">A 16-bit unsigned integer whose low-order byte is desired</param>
        /// <returns>The low-order byte of <paramref name="w"/></returns>
        /// <remarks>We return an uint (rather than a byte) to be compatible
        /// with <see cref="NativeTypes.OSVERSIONINFOEX.dwMinorVersion"/></remarks>
        internal static uint LOBYTE(ushort w)
        {
            return (uint)(w & 0xFF);
        }
    }
}
