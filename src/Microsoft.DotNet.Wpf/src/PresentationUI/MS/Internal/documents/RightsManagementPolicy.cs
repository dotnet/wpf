// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//    RightsManagementPolicy enum for determining what RM will allow.
using System;

namespace MS.Internal.Documents
{
    /// <summary>
    /// This is the external representation of what actions should be allowed
    /// by Rights Management.
    /// </summary>
    [Flags]
    internal enum RightsManagementPolicy : int
    {
        AllowNothing = 0,
        AllowView = 1,
        AllowPrint = 2,
        AllowCopy = 4,
        AllowSign = 8,
        AllowAnnotate = 16
    }
}
