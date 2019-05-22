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
    internal enum DevModeNUp : uint
    {
        /// <summary>
        /// The print spooler does the NUP.
        /// </summary>
        DMNUP_SYSTEM = 1,

        /// <summary>
        /// The application does the NUP.
        /// </summary>
        DMNUP_ONEUP = 2
    }
}