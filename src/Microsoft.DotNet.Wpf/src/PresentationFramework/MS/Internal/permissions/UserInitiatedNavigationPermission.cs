// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using MS.Internal.Permissions;

namespace MS.Internal.Permissions
{
    internal class UserInitiatedNavigationPermission : InternalParameterlessPermissionBase
    {
        public UserInitiatedNavigationPermission() : this(PermissionState.Unrestricted)
        {
        }

        public UserInitiatedNavigationPermission(PermissionState state): base()
        {
        }

        public IPermission Copy() { return default(IPermission); }        
    }
}

