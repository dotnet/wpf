// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//
// This creates and registers a custom ICredentialPolicy that will be used 
// whenever a server sends a 401 UNAUTHORIZED in response to a WebRequest. The 
// .NET framework will call ShouldSendCredential to determine if credentials 
// should be sent. 
//
// Our policy is to go ahead and send whatever credentials there might be, 
// EXCEPT if we are using default credentials and the request is not going to 
// an Intranet, Local Machine or Trusted domain.
//
//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// IMPORTANT: We are creating an instance of IInternetSecurityManager here. This
// is currently also done in the AppSecurityManager at the Framework level. Any
// modification to either of these classes--especially concerning MapUrlToZone--
// should be considered for both classes.
//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
//
// An IInternetSecurityManagerSite is not currently needed here, because the
// only method of IInternetSecurityManager that we are calling is MapUrlToZone,
// and that does not prompt the user.
// 

using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

using MS.Internal.PresentationCore;
using MS.Win32;

namespace MS.Internal.AppModel
{
    [FriendAccessAllowed]
    internal class CustomCredentialPolicy : ICredentialPolicy
    {
        /// <SecurityNote>
        /// Critical - Accesses critical members
        /// PublicOK - Nothing is exposed, and nothing depends on user input. We're just creating objects for use later.
        /// </SecurityNote>
        static CustomCredentialPolicy()
        {
            _lockObj = new object();
            _initialized = false;
        }

        /// <SecurityNote>
        /// Critical - Access critical member _environmentPermissionSet
        /// PublicOK - Nothing is exposed, and nothing depends on user input. We're just creating objects for use later.
        /// </SecurityNote>
        public CustomCredentialPolicy()
        {
            _environmentPermissionSet = new PermissionSet(null);
            _environmentPermissionSet.AddPermission(new EnvironmentPermission(EnvironmentPermissionAccess.Read, "USERDOMAIN"));
            _environmentPermissionSet.AddPermission(new EnvironmentPermission(EnvironmentPermissionAccess.Read, "USERNAME"));
        }

        /// <SecurityNote>
        /// Critical    - Asserts for permission to set the credential policy.
        /// TreatAsSafe - Nothing is exposed. This method is safe to call at any time.
        /// </SecurityNote>
        static internal void EnsureCustomCredentialPolicy()
        {
            if (!_initialized)
            {
                lock (_lockObj)
                {
                    if (!_initialized)
                    {
                        new SecurityPermission(SecurityPermissionFlag.ControlPolicy).Assert();  // BlessedAssert: 
                        try
                        {
                            // We should allow an application to set its own credential policy, if it has permssion to.
                            // We do not want to overwrite the application's setting. 
                            // Check whether it is already set before setting it. 
                            // The default of this property is null. It demands ControlPolicy permission to be set.
                            if (AuthenticationManager.CredentialPolicy == null)
                            {
                                AuthenticationManager.CredentialPolicy = new CustomCredentialPolicy();
                            }
                            _initialized = true;
                        }
                        finally
                        {
                            CodeAccessPermission.RevertAssert();
                        }
                    }
                }
            }
        }

        #region ICredentialPolicy Members

        /// <SecurityNote>
        /// Critical    - Calls SecurityCritical method IsDefaultCredentials.
        /// TreatAsSafe - This is called by the framework to determine if credentials should be sent
        ///               in response to the server sending a 401. The challengUri and request are
        ///               coming from the app; the credential and authenticationModule are coming
        ///               from the framework. The only ouput is whether or not credentials should
        ///               be sent, which is not critical.
        /// </SecurityNote>
        public bool ShouldSendCredential(Uri challengeUri, WebRequest request, NetworkCredential credential, IAuthenticationModule authenticationModule)
        {
            switch (MapUrlToZone(challengeUri))
            {
                // Always send credentials (including default credentials) to these zones
                case SecurityZone.Intranet:
                case SecurityZone.Trusted:
                case SecurityZone.MyComputer:
                    return true;
                // Don't send default credentials to any of these zones
                case SecurityZone.Internet:
                case SecurityZone.Untrusted:
                default:
                    return !IsDefaultCredentials(credential);
            }
        }

        #endregion

        /// <SecurityNote>
        /// Critical - Asserts for permission to examine the user name and password. They are 
        ///            only checked to see if they are non-null, and not exposed, but the 
        ///            fact that default credential are being used and are available might
        ///            something we would not want revealed, which is why it is not TAS.
        /// </SecurityNote>
        private bool IsDefaultCredentials(NetworkCredential credential)
        {
            _environmentPermissionSet.Assert();  // BlessedAssert: 
            try
            {
                return credential == CredentialCache.DefaultCredentials;
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }
        }

        /// <SecurityNote>
        /// Critical    - Call critical method MapUrlToZone.
        /// TreatAsSafe - Returns the zone of the uri, which is not critical.
        /// </SecurityNote>
        internal static SecurityZone MapUrlToZone(Uri uri)
        {
            EnsureSecurityManager();

            int targetZone;
            _securityManager.MapUrlToZone(BindUriHelper.UriToString(uri), out targetZone, 0);

            // The enum is directly mappable, but taking no chances...
            switch (targetZone)
            {
                case NativeMethods.URLZONE_LOCAL_MACHINE:
                    return SecurityZone.MyComputer;
                case NativeMethods.URLZONE_INTERNET:
                    return SecurityZone.Internet;
                case NativeMethods.URLZONE_INTRANET:
                    return SecurityZone.Intranet;
                case NativeMethods.URLZONE_TRUSTED:
                    return SecurityZone.Trusted;
                case NativeMethods.URLZONE_UNTRUSTED:
                    return SecurityZone.Untrusted;
            }

            return SecurityZone.NoZone;
        }

        /// <SecurityNote>
        /// Critical    - Accesses critical member _securityManager and calls the SUC'd MapUrlToZone.
        /// TreatAsSafe - Doesn't expose anything, doesn't take user input, safe to call at any time.
        /// </SecurityNote>
        private static void EnsureSecurityManager()
        {
            // IMPORTANT: See comments in header r.e. IInternetSecurityManager

            if (_securityManager == null)
            {
                lock (_lockObj)
                {
                    if (_securityManager == null)
                    {
                        _securityManager = (UnsafeNativeMethods.IInternetSecurityManager)new InternetSecurityManager();                        
                    }
                }
            }
        }

        [ComImport, ComVisible(false), Guid("7b8a2d94-0ac9-11d1-896c-00c04Fb6bfc4")]
        private class InternetSecurityManager
        {
        }

        ///<SecurityNote> 
        /// Critical - requires an elevation to create. 
        ///</SecurityNote> 
        private static UnsafeNativeMethods.IInternetSecurityManager _securityManager;

        private static object _lockObj;

        private static bool _initialized;

        PermissionSet _environmentPermissionSet;
    }
}
