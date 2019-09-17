// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:  Secure Environment class is a starting point for Managed RM APIs 
//   It provides basic services of enumerating User Certificates, Initializing Environment 
//
//
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using MS.Internal.Security.RightsManagement;
using SecurityHelper=MS.Internal.WindowsBase.SecurityHelper; 
using MS.Internal;
using MS.Internal.WindowsBase;

namespace System.Security.RightsManagement 
{
    /// <summary>
    /// This class represent a client session, which used in activation, binding  and other function calls.
    /// </summary>
    public class SecureEnvironment : IDisposable
    {
        /// <summary>
        /// This static Method builds a new instance of a SecureEnvironment  for a given user that is already 
        /// activated. If this method called with a user that isn't activated, and exception will be thrown. 
        /// The user that is passed into the function must have a well defined authentication type 
        /// AuthenticationType.Windows or AuthenticationType.Passport, all other Authentication 
        /// types(AuthenticationType.WindowsPassport or AuthenticationType.Internal) are not allowed.
        /// </summary>
        public static SecureEnvironment Create(string applicationManifest,
                                               ContentUser user)
        {
    
            return CriticalCreate(applicationManifest, user);
        }


        /// <summary>
        /// This static method activates a user and creates a new instance of SecureEnvironment.
        /// The authentication type determines the type of user identity that will be activated. 
        /// If Permanent Windows activation is requested then the default currently logged on 
        /// Windows Account identity will be activated. If Temporary Windows activation requested 
        /// then user will be prompted for Windows Domain credentials through a dialog, and the 
        /// user identified through those credentials will be activated. 
        /// In case of Passport authentication, a Passport authentication dialog will always
        /// appear regardless of temporary or permanent activation mode. The user that authenticatd 
        /// through that Passport Authentication dialog will be activated.
        /// Regardless of Windows or Passport Authentication, all Temporary created activation will be 
        /// destroyed when SecureEnvironment instance is Disposed or Finalized.  
        /// </summary>   
        public static SecureEnvironment Create(string applicationManifest, 
                                                                                        AuthenticationType authentication, 
                                                                                        UserActivationMode userActivationMode)
        {

            return CriticalCreate(applicationManifest, 
                                            authentication,
                                            userActivationMode);
        }
        
        /// <summary>
        /// This property verifies whether the current machine was prepared for consuming and producing RM protected content. 
        /// If property returns true it could be used as an indication that Init function call will not result in a network transaction.
        /// </summary>
        public static bool IsUserActivated(ContentUser user)
        {
        
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            // we only let specifically identified users to be used here  
            if ((user.AuthenticationType != AuthenticationType.Windows) && 
                 (user.AuthenticationType != AuthenticationType.Passport))
            {
                throw new ArgumentOutOfRangeException("user", SR.Get(SRID.OnlyPassportOrWindowsAuthenticatedUsersAreAllowed));
            }
            
            using (ClientSession userClientSession = new ClientSession(user))
            {
                // if machine activation is not present we can return false right away             
                return (userClientSession.IsMachineActivated() && userClientSession.IsUserActivated());
            }
        }

        /// <summary>
        /// Removes activation for a given user. User must have Windows or Passport authnetication 
        /// </summary>
        public static void RemoveActivatedUser(ContentUser user)
        {
            
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            // we only let specifically identifyed users to be used here  
            if ((user.AuthenticationType != AuthenticationType.Windows) && 
                 (user.AuthenticationType != AuthenticationType.Passport))
            {
                throw new ArgumentOutOfRangeException("user", SR.Get(SRID.OnlyPassportOrWindowsAuthenticatedUsersAreAllowed));
            }

            // Generic client session to enumerate user certificates 
            using (ClientSession userClientSession = new ClientSession(user))
            {          
                // Remove Licensor certificastes first 
                List<string> userClientLicensorCertificateIds = 
                                userClientSession.EnumerateUsersCertificateIds(user, EnumerateLicenseFlags.ClientLicensor);

                // and now we can remove certificates that have been enumerated 
                foreach(string licenseId in userClientLicensorCertificateIds)
                {
                    userClientSession.DeleteLicense(licenseId); 
                }                
                        
                // Remove User's identity certificastes second 
                List<string> userGroupIdentityCertificateIds = 
                                userClientSession.EnumerateUsersCertificateIds(user, EnumerateLicenseFlags.GroupIdentity);

                // and now we can remove certificates that have been enumerated 
                foreach(string licenseId in userGroupIdentityCertificateIds)
                {
                    userClientSession.DeleteLicense(licenseId); 
                }                
            }
        }

        /// <summary>
        /// This function returns a read only collection of the activated users.
        /// </summary>
        static public  ReadOnlyCollection<ContentUser>  GetActivatedUsers()
        {
            
            //build user with the default authentication type and a default name 
            // neither name not authentication type is important in this case 
            //ContentUser tempUser = new ContentUser(_defaultUserName, AuthenticationType.Windows);
        
            // Generic client session to enumerate user certificates 
            using(ClientSession genericClientSession = 
                ClientSession.DefaultUserClientSession(AuthenticationType.Windows))
            {
                List<ContentUser> userList = new List<ContentUser>(); 

                // if machine activation is not present we can return empty list right away             
                if (genericClientSession.IsMachineActivated())
                {
                    int index =0; 
                    while(true)
                    {
                        // we get a string which can be parsed to get the ID and type 
                        string userCertificate = genericClientSession.EnumerateLicense(EnumerateLicenseFlags.GroupIdentity, index);

                        if (userCertificate == null)
                            break;

                        // we need to parse the information out of the string 
                        ContentUser user = ClientSession.ExtractUserFromCertificateChain(userCertificate);

                        // User specific client session to check it's status 
                        using(ClientSession userClientSession = new ClientSession(user))
                        {
                            if (userClientSession.IsUserActivated()) 
                            {
                                userList.Add(user);
                            }
                        }

                        index ++;
                    }
                }
                
                return new ReadOnlyCollection<ContentUser>(userList);
            }
        }

        /// <summary>
        /// This method is responsible for tearing down secure environment that was built as a result of Init call.
        /// </summary>
        public void Dispose()
        {              
            
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Read only property which returns the User provided in the constructor. 
        /// </summary>
        public ContentUser User
        {
            get
            {
            
                CheckDisposed();
                return _user;
            }
        }

        /// <summary>
        /// Read only property which returns the Application Manifest provided in the constructor. 
        /// </summary>
        public string ApplicationManifest        
        {
            get
            {
            
                CheckDisposed();
                return _applicationManifest;
            }
        }

        /// <summary>
        /// Dispose(bool)
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (disposing && (_clientSession != null))
                {
                    _clientSession.Dispose();
                }
            }
            finally
            {
                _clientSession = null;
            }
        }

        internal ClientSession ClientSession 
        {
            get
            {
                Invariant.Assert(_clientSession != null);
            
                return _clientSession;
            }
        }

        /// <summary>
        /// This static Method builds a new instance of a secure environment for a given user that is assumed to be already activated. 
        /// client Application can use GetActivatedUsers property to enumerate Activated users.
        /// </summary>
        private static SecureEnvironment CriticalCreate(string applicationManifest, ContentUser user)
        {
            if (applicationManifest == null)
            {
                throw new ArgumentNullException("applicationManifest");
            }

            if (user == null)
            {
                throw new  ArgumentNullException("user");
            }

            // we only let specifically identifyed users to be used here  
            if ((user.AuthenticationType != AuthenticationType.Windows) && 
                 (user.AuthenticationType != AuthenticationType.Passport))
            {
                throw new ArgumentOutOfRangeException("user");
            }

            if (!IsUserActivated(user))
            {
                throw new RightsManagementException(RightsManagementFailureCode.NeedsGroupIdentityActivation);
            }
            
            ClientSession clientSession = new ClientSession(user);

            try
            {
                clientSession.BuildSecureEnvironment(applicationManifest);

                return new SecureEnvironment(applicationManifest, user, clientSession);
            }
            catch
            {
                clientSession.Dispose();
                throw;
            }
        }

        private static SecureEnvironment CriticalCreate(
            string applicationManifest, 
            AuthenticationType authentication,
            UserActivationMode userActivationMode)
        {
            if (applicationManifest == null)
            {
                throw new ArgumentNullException("applicationManifest");
            }

            if ((authentication != AuthenticationType.Windows) && 
                 (authentication != AuthenticationType.Passport))
            {
                throw new ArgumentOutOfRangeException("authentication");
            }

            if ((userActivationMode != UserActivationMode.Permanent) &&
                 (userActivationMode != UserActivationMode.Temporary))
            {
                throw new ArgumentOutOfRangeException("userActivationMode");            
            }

            //build user with the given authnetication type and a default name 
            // only authentication type is critical in this case 
            ContentUser user; 
            
            using (ClientSession tempClientSession =
                ClientSession.DefaultUserClientSession(authentication))
            {
                //Activate Machine if neccessary
                if (!tempClientSession.IsMachineActivated())
                {
                    // activate Machine
                    tempClientSession.ActivateMachine(authentication);
                }

                //Activate User (we will force start activation at this point)
                // at this point we should have a real user name 
                user = tempClientSession.ActivateUser(authentication, userActivationMode);
            }

            Debug.Assert(IsUserActivated(user));

            ClientSession clientSession = new ClientSession(user, userActivationMode);

            try
            {
                try
                {
                    // make sure we have a Client Licensor Certificate 
                    clientSession.AcquireClientLicensorCertificate();
                }
                catch (RightsManagementException)
                {
                    // In case of the RightsMnaagement exception we are willing to proceed
                    // as ClientLicensorCertificate only required for publishing not for consumption 
                    // and therefore it is optional to have one.
                }
            
                clientSession.BuildSecureEnvironment(applicationManifest);

                return new SecureEnvironment(applicationManifest, user, clientSession);
            }
            catch
            {
                clientSession.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Private Constructor for the SecureEnvironment. 
        /// </summary>
        private SecureEnvironment(string applicationManifest,
                                                         ContentUser user, 
                                                         ClientSession clientSession) 
        {
            Invariant.Assert(applicationManifest != null);
            Invariant.Assert(user != null);
            Invariant.Assert(clientSession != null);

            _user = user;
            _applicationManifest = applicationManifest;
            _clientSession = clientSession;
        }

        /// <summary>
        /// Call this before accepting any API call 
        /// </summary>
        private void CheckDisposed()
        {
            if (_clientSession == null)
                throw new ObjectDisposedException("SecureEnvironment");
        }

        private ContentUser _user;
        private string _applicationManifest;
        private ClientSession _clientSession;       // if null we are disposed
    }
}
