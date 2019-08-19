// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
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
// From MSDN:
//
// When you inherit from CodeAccessPermission, you must also implement the IUnrestrictedPermission interface. 
// The following CodeAccessPermission members must be overridden: Copy, Intersect, IsSubsetOf, ToXml, FromXml, and Union. 
// You must also define a constructor that takes a PermissionState as its only parameter. 
// You must apply the SerializableAttribute attribute to a class that inherits from CodeAccessPermission. 
//
// InternalParameterlessPermissionBase is a base class that requires derived classes to only support one
// PermissionState (Unrestricted) and to have no parameters/properties/state.  As above, derived classes must also be
// [Serializable] and have a public constructor that takes PermissionState.
// 
//  
//

using System;
using System.Diagnostics;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Windows; 

#if WINDOWS_BASE
using MS.Internal.WindowsBase;
#endif

namespace MS.Internal.Permissions
{
    //
    // derive all InternalPermissions from this. 
    // Provides default implementations of several overridable methods on CodeAccessPermission
    // 
    [FriendAccessAllowed]
    [Serializable]
    internal abstract class InternalParameterlessPermissionBase : CodeAccessPermission, IUnrestrictedPermission
    {
       //------------------------------------------------------
       //
       //  Constructors
       //
       //------------------------------------------------------
        #region Constructor    

        protected InternalParameterlessPermissionBase(PermissionState state) 
        {
            Debug.Assert(GetType().IsSerializable);

            switch (state)
            {
            case PermissionState.Unrestricted:
                break;        
            case PermissionState.None:
            default:
                throw new ArgumentException(SR.Get(SRID.InvalidPermissionStateValue, state), "state");
            }
        }

        #endregion Constructor 

       //------------------------------------------------------
       //
       //  Interface Methods
       //
       //------------------------------------------------------
        #region Interface Methods   
        
        public bool IsUnrestricted()
        {
            return true; 
        }
        
        #endregion Interface Methods        

       //------------------------------------------------------
       //
       //  Public Methods
       //
       //------------------------------------------------------

        #region Public Methods   

        public override SecurityElement ToXml()
        {
           SecurityElement element = new SecurityElement("IPermission");
           Type type = this.GetType();
           StringBuilder AssemblyName = new StringBuilder(type.Assembly.ToString());
           AssemblyName.Replace('\"', '\'');
           element.AddAttribute("class", type.FullName + ", " + AssemblyName);
           element.AddAttribute("version", "1");
           return element;
        }

        public override void FromXml( SecurityElement elem)
        {
            // from XML is easy - there is no state. 
        }

        public override IPermission Intersect(IPermission target)
        {
            if(null == target)
            {
                return null;
            }
            
            if ( target.GetType() != this.GetType() ) 
            {
                throw new ArgumentException( SR.Get(SRID.InvalidPermissionType, this.GetType().FullName), "target");
            }

            // there is no state. The intersection of 2 permissions of the same type is the same permission.   
            return this.Copy();
        }

        public override bool IsSubsetOf(IPermission target)
        {  
            if(null == target)
            {
                return false; 
            }

            if ( target.GetType() != this.GetType() ) 
            {
                throw new ArgumentException( SR.Get(SRID.InvalidPermissionType, this.GetType().FullName), "target");
            }

            // there is no state. If you are the same type as me - you are a subset of me. 
            return true; 
        }

        public override IPermission Union(IPermission target)
        {
            if(null == target)
            {
                return null;
            }
            
            if ( target.GetType() != this.GetType() ) 
            {
                throw new ArgumentException( SR.Get(SRID.InvalidPermissionType, this.GetType().FullName), "target");
            }

            // there is no state. The union of 2 permissions of the same type is the same permission.   
            return this.Copy();   
        }

        #endregion Public Methods   
    }
}

