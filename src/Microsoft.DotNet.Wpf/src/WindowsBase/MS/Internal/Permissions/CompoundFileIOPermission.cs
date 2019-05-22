// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: CompoundFile IO Permission. 
//                  It is a class for permission that will be asserted/demanded internally. 
//                  Only XPSViewer (or Mongoose) code will assert this permission.
//
//              Using it allows the following: 
//                  We can have very specific targeted asserts for enabling EncryptedPackageEnevelope
//                      and CompoundFile IO APIs.
//                  This is to provide a granular permission for CompoundFile IO operations to be used
//                  by XPSViewer to enable Encrypted Documents scenarios in Partial Trust
//                  rather than asserting broader permission such as Unmanaged Code
//
// !!!! Warning !!!!: No code other than XPSViewer should assert this
//                      permission without agreement from this code owners.


using System;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Windows; 
using MS.Internal.WindowsBase;

namespace MS.Internal.Permissions
{
    // !!!! Warning !!!!: No code other than XPSViewer (or Mongoose) should assert this
    //  permission without agreement from this code owners.
    [Serializable]
    [FriendAccessAllowed]
    internal class CompoundFileIOPermission : InternalParameterlessPermissionBase
    {
        public CompoundFileIOPermission() : this(PermissionState.Unrestricted)
        {
        }

        public CompoundFileIOPermission(PermissionState state): base(state)
        {
        }

        public override IPermission Copy()
        {
            // There is no state: just return a new instance of CompoudFileIOPermission
            return new CompoundFileIOPermission(); 
        }        
    }
}

