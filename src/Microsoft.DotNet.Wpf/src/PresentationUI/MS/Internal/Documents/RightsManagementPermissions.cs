// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: 
//    RightsManagementPermissions enum for determining what permissions are
//    granted to the user by RM.
using System;

namespace MS.Internal.Documents
{
    /// <summary>
    /// This is the internal DRP representation of the permissions
    /// granted by RM.
    /// </summary>
    [Flags]
    internal enum RightsManagementPermissions : int
    {
        AllowNothing = RightsManagementPolicy.AllowNothing,
        AllowView = RightsManagementPolicy.AllowView,
        AllowPrint = RightsManagementPolicy.AllowPrint,
        AllowCopy = RightsManagementPolicy.AllowCopy,
        AllowSign = RightsManagementPolicy.AllowSign,
        AllowAnnotate = RightsManagementPolicy.AllowAnnotate,

        AllowEdit = 512,
        AllowChangePermissions = 1024,

        AllowOwner = (int)0xfffffff
    }
}
