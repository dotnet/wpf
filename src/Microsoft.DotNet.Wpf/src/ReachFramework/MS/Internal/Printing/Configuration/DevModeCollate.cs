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
    internal enum DevModeCollate : short
    {
        /// <summary>
        /// Do not collate when printing multiple copies.
        /// </summary>
        DMCOLLATE_FALSE = 0,

        /// <summary>
        /// Collate when printing multiple copies.
        /// </summary>
        DMCOLLATE_TRUE = 1
    }
}