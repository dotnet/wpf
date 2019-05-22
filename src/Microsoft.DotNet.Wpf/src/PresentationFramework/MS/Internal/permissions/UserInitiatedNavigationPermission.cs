// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// 
//
// Description: Internal Permissions. 
//                  These are classes for permissions that will be asserted/demanded internally. 
//                  But will be granted in full-trust. 
//                  Only internal avalon code will assert these permissions. 
//
//              Using them allows the following: 
//                  We can have very specific targeted asserts. So for example instead of 
//                  a blanket assert for Unmanaged code instead we can have very granular permissiosn. 
//
//                  They are still available by default in full-trust. 
//                  
//                  Currently the only way to detect User-Initiated actions is for commands.
//                  So by associating a custom permisison with a command we can very tightly scope
//                  the set of operations allowed. 
// 


using System;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using MS.Internal.Permissions;


namespace MS.Internal.Permissions
{
    [Serializable]
    internal class UserInitiatedNavigationPermission : InternalParameterlessPermissionBase
    {
        public UserInitiatedNavigationPermission() : this(PermissionState.Unrestricted)
        {
        }

        public UserInitiatedNavigationPermission(PermissionState state): base(state)
        {
        }

        public override IPermission Copy()
        {
            // copy is easy there is no state ! 
            return new UserInitiatedNavigationPermission(); 
        }        
    }
}

