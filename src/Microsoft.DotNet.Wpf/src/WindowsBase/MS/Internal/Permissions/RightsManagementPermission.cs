// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Windows; 
using MS.Internal.WindowsBase;

namespace MS.Internal.Permissions
{
    [FriendAccessAllowed]
    internal class RightsManagementPermission : InternalParameterlessPermissionBase
    {
        public RightsManagementPermission() : this(PermissionState.Unrestricted)
        {
        }
        public RightsManagementPermission(PermissionState state): base()
        {
        }
        public IPermission Copy() { return default(IPermission); } 
    }
}

