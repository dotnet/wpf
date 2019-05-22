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
    internal enum DevModeICMMethod : uint
    {
        /// <summary>
        /// Specifies that ICM is disabled.
        /// </summary>
        DMICMMETHOD_NONE = 1,

        /// <summary>
        /// Specifies that ICM is handled by Windows.
        /// </summary>
        DMICMMETHOD_SYSTEM = 2,

        ///Specifies that ICM is handled by the device driver.
        DMICMMETHOD_DRIVER = 3,

        /// <summary>
        /// Specifies that ICM is handled by the destination device.
        /// </summary>
        DMICMMETHOD_DEVICE = 4
    }
}