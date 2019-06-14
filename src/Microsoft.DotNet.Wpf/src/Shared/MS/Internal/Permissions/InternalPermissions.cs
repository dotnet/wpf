// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Text;
using System.Security;
using System.Windows; 

#if WINDOWS_BASE
using MS.Internal.WindowsBase;
#endif

namespace MS.Internal.Permissions
{
    [FriendAccessAllowed]
    internal abstract class InternalParameterlessPermissionBase
    {
        protected InternalParameterlessPermissionBase() { }
        public bool IsUnrestricted() { return true; }
        public virtual SecurityElement ToXml() { return default(SecurityElement); }
        public virtual void FromXml( SecurityElement elem) {  }
        public virtual IPermission Intersect(IPermission target) { return default(IPermission); }
        public virtual bool IsSubsetOf(IPermission target) { return true; }
        public virtual IPermission Union(IPermission target) { return default(IPermission); }
        // Added hollow methods below that were originally part of 'CodeAccessPermission' which this class used to extend
        public void Demand() { }
        public void Assert() { }
        public static void RevertAssert() { }
    }
}

