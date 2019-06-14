// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using MS.Internal.Permissions;

namespace MS.Internal.Permissions
{
    internal class UserInitiatedRoutedEventPermission : InternalParameterlessPermissionBase
    {
        public UserInitiatedRoutedEventPermission() : this(PermissionState.Unrestricted)
        {
        }
        public UserInitiatedRoutedEventPermission(PermissionState state): base()
        {
        }
        public IPermission Copy() { return default(IPermission); }
    }
}
