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

using MS.Internal.PresentationCore;
using MS.Win32;

namespace MS.Internal.AppModel
{
    [FriendAccessAllowed]
    internal class CustomCredentialPolicy : ICredentialPolicy
    {
        static CustomCredentialPolicy()
        {
            _lockObj = new object();
            _initialized = false;
        }

        static internal void EnsureCustomCredentialPolicy()
        {
            if (!_initialized)
            {
                lock (_lockObj)
                {
                    if (!_initialized)
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
                }
            }
        }

        #region ICredentialPolicy Members

        public bool ShouldSendCredential(Uri challengeUri, WebRequest request, NetworkCredential credential, IAuthenticationModule authenticationModule)
        {
            switch (MapUrlToZone(challengeUri))
            {
                // Always send credentials (including default credentials) to these zones
                case NativeMethods.URLZONE_INTRANET:
                case NativeMethods.URLZONE_TRUSTED:
                case NativeMethods.URLZONE_LOCAL_MACHINE:
                    return true;
                // Don't send default credentials to any of these zones
                case NativeMethods.URLZONE_INTERNET:
                case NativeMethods.URLZONE_UNTRUSTED:
                default:
                    return !IsDefaultCredentials(credential);
            }
        }

        #endregion

        private bool IsDefaultCredentials(NetworkCredential credential)
        {
            return credential == CredentialCache.DefaultCredentials;
        }

        internal static int MapUrlToZone(Uri uri)
        {
            EnsureSecurityManager();

            int targetZone;
            _securityManager.MapUrlToZone(BindUriHelper.UriToString(uri), out targetZone, 0);
            return targetZone;
        }

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

        private static UnsafeNativeMethods.IInternetSecurityManager _securityManager;

        private static object _lockObj;

        private static bool _initialized;
    }
}
