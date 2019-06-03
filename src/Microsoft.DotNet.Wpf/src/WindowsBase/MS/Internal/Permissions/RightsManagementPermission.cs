// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Rights Managment Permission. 
//                  It is a class for permission that will be asserted/demanded internally. 
//                  Only DocumentApplication (or Mongoose) code will assert these permissions.
//
//              Using it allows the following: 
//                  We can have very specific targeted asserts for enabling Rights Management.
//                  This is to provide a granular permissio for Rights Management to be used
//                  by DocumentApplication to enable Encrypted Documents scenarios in Partial Trust
//                  rather than asserting broader permission such as Unmanaged Code
//
// !!!! Warning !!!!: No code other than DocumentApplication (or Mongoose) should assert this
//                      permission without agreement from this code owners.

using System;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Windows; 
using MS.Internal.WindowsBase;

namespace MS.Internal.Permissions
{
    // !!!! Warning !!!!: No code other than DocumentApplication (or Mongoose) should assert this
    //  permission without agreement from this code owners.
    [Serializable]
    [FriendAccessAllowed]
    internal class RightsManagementPermission : InternalParameterlessPermissionBase
    {
        public RightsManagementPermission() : this(PermissionState.Unrestricted)
        {
        }

        public RightsManagementPermission(PermissionState state): base(state)
        {
        }

        public override IPermission Copy()
        {
            // There is no state: just return a new instance of RightsManagementPermission
            return new RightsManagementPermission(); 
        }        
    }
}

