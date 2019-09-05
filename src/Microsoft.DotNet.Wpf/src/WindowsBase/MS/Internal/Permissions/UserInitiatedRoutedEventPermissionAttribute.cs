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
    // This permission attribute was defined in WindowsBase since it must be defined in
    // a seperate assembly from where it is used (PresentationCore). The reason for this is explained
    // in the following connect article.  The MSDN documentation has yet to be updated:
    // https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=297627
    [Serializable]
    [AttributeUsage(AttributeTargets.Method)]
    sealed internal class UserInitiatedRoutedEventPermissionAttribute : CodeAccessSecurityAttribute
    {
        private static UserInitiatedRoutedEventPermission _perm;

        public UserInitiatedRoutedEventPermissionAttribute(SecurityAction action): base(action)
        {
        }

        public override IPermission CreatePermission()
        {
            if (_perm == null)
            {
                _perm = new UserInitiatedRoutedEventPermission();
            }

            return _perm;
        }
    }
}
