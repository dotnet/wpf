// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  These are the internal helpers required to call into unmanaged 
//  Promethium Rights Management SDK APIs 
//
//
//
//

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Security.RightsManagement;
using SecurityHelper = MS.Internal.WindowsBase.SecurityHelper;
using System.Text;
using System.Globalization;                 // For CultureInfo
// for Invariant
using System.Windows;                       // for SR and SRID

using MS.Internal;
using System.Runtime.InteropServices;

using Microsoft.Win32; // for Registry and RegistryKey classes 

using MS.Internal.WindowsBase;

namespace MS.Internal.Security.RightsManagement
{
    internal class ClientSession : IDisposable
    {
        internal ClientSession(ContentUser user)
            :
                    this(user, UserActivationMode.Permanent)
        {
        }

        internal ClientSession(
            ContentUser user,
            UserActivationMode userActivationMode)
        {
            Invariant.Assert(user != null);
            Invariant.Assert((userActivationMode == UserActivationMode.Permanent) ||
                                    (userActivationMode == UserActivationMode.Temporary));

            _user = user;
            _userActivationMode = userActivationMode;

            // prepare callback handler 
            _callbackHandler = new CallbackHandler();

            int hr = SafeNativeMethods.DRMCreateClientSession(
                _callbackHandler.CallbackDelegate,
                NativeConstants.DrmCallbackVersion,
                _user.AuthenticationProviderType,
                _user.Name,
                out _hSession);

            Errors.ThrowOnErrorCode(hr);
            Invariant.Assert((_hSession != null) && (!_hSession.IsInvalid));
        }

        ~ClientSession()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // close all the CryptoProviders that are bound to the Session
                // This way if user attempt to close SecureEnvironment while using the CryptoProvider 
                // the meaningful object Disposed exception will be thrown instead of misleading 
                // Interop exception indicating that an invalid handle was used.
                try
                {
                    if (_cryptoProviderList != null)
                    {
                        foreach (CryptoProvider cryptoProvider in _cryptoProviderList)
                        {
                            cryptoProvider.Dispose();
                        }
                    }
                }
                finally
                {
                    _cryptoProviderList = null;

                    // close session handle which is a DRMHSESSION 
                    try
                    {
                        if ((_hSession != null) &&
                            (!_hSession.IsInvalid))
                        {
                            // if we deal with temporatry activation we should clean up 
                            // Group Identity Cerytificates and the Client Licensor Certificates
                            if (_userActivationMode == UserActivationMode.Temporary)
                            {
                                RemoveUsersCertificates(EnumerateLicenseFlags.SpecifiedClientLicensor);
                                RemoveUsersCertificates(EnumerateLicenseFlags.SpecifiedGroupIdentity);
                            }

                            _hSession.Dispose();
                        }
                    }
                    finally
                    {
                        _hSession = null;

                        // close default library handle which is a DRMHANDLE  
                        try
                        {
                            if ((_defaultLibraryHandle != null) &&
                                (!_defaultLibraryHandle.IsInvalid))
                            {
                                _defaultLibraryHandle.Dispose();
                            }
                        }
                        finally
                        {
                            _defaultLibraryHandle = null;

                            // close secure environment handle which is a DRMENVHANDLE  
                            try
                            {
                                if ((_envHandle != null) &&
                                     (!_envHandle.IsInvalid))
                                {
                                    _envHandle.Dispose();
                                }
                            }
                            finally
                            {
                                _envHandle = null;

                                // Dispose call back handler 
                                try
                                {
                                    if (_callbackHandler != null)
                                    {
                                        _callbackHandler.Dispose();
                                    }
                                }
                                finally
                                {
                                    _callbackHandler = null;
                                }
                            }
                        }
                    }
                }
            }
        }

        internal static ClientSession DefaultUserClientSession(AuthenticationType authentication)
        {
            return new ClientSession(new ContentUser(_defaultUserName, authentication));
        }

        internal bool IsMachineActivated()
        {
            CheckDisposed();

            return IsActivated(ActivationFlags.Machine) && (GetMachineCert() != null);
        }

        internal void ActivateMachine(AuthenticationType authentication)
        {
            CheckDisposed();

            // machine activation is always silent 
            ActivationFlags actFlags = ActivationFlags.Machine | ActivationFlags.Silent;

            Activate(actFlags, null); // GetActivationUrl(authentication));  // for local activation we do not need URL 
            // as we have Hard dependency on SP1 lock box - which is the only one that                
            // is managed code friendly, we should never try to activate machine  
            // using URL - always local SP1 activation          
        }

        internal bool IsUserActivated()
        {
            CheckDisposed();

            // check that group identity is activated 
            if (!IsActivated(ActivationFlags.GroupIdentity))
            {
                return false;
            }

            // enumerate user certificates andg get the current one 
            string userCertificateChain = GetGroupIdentityCert();

            if (userCertificateChain == null)
            {
                return false;
            }

            // extract identity from the certitficate and match it with the user 
            ContentUser userFromCert = ExtractUserFromCertificateChain(userCertificateChain);

            if (userFromCert == null)
            {
                return false;
            }

            // make sure the user matches completely (including authentication type). 
            // All the checks above will pass if the requested user identity and 
            // activated user identity only differ by authentication type.
            return _user.GenericEquals(userFromCert);
        }

        internal ContentUser ActivateUser(
            AuthenticationType authentication, UserActivationMode userActivationMode)
        {
            CheckDisposed();

            ActivationFlags actFlags = ActivationFlags.GroupIdentity;

            // for windows Silen Flag must be set For Passport it must not be set 
            if (_user.AuthenticationType == AuthenticationType.Windows)
            {
                actFlags |= ActivationFlags.Silent;
            }

            if (userActivationMode == UserActivationMode.Temporary)
            {
                actFlags |= ActivationFlags.Temporary;
            }

            string userCertificate =
                Activate(actFlags, GetCertificationUrl(authentication));

            return ExtractUserFromCertificate(userCertificate);
        }

        internal bool IsClientLicensorCertificatePresent()
        {
            CheckDisposed();

            return (GetClientLicensorCert() != null);
        }

        /// <summary>
        /// This function is used to acquire a license either an End Use license
        /// or a Client Licensor Certificate 
        /// </summary>
        internal void AcquireClientLicensorCertificate()
        {
            CheckDisposed();

            // get the URL for Client Licensor Cert Acquisition 
            Uri url = GetClientLicensorUrl(_user.AuthenticationType);

            string license = GetGroupIdentityCert();

            int hr = SafeNativeMethods.DRMAcquireLicense(
                _hSession,
                0,  // flags default to 0  for CLC acquisition 
                license,
                null,  // requested data is reserved and not used
                null, // custom data 
                url.AbsoluteUri,  // We are using Uri class as a basic validation mechanism. These URIs come from unmanaged 
                // code libraries and go back as parameters into the unmanaged code libraries. 
                // We use AbsoluteUri property as means of verifying that it is actually an absolute and 
                // well formed Uri. If by any chance it happened to be a relative URI, an exception will 
                // be thrown here. This will perform the necessary escaping.

                IntPtr.Zero);    // context

            Errors.ThrowOnErrorCode(hr);

            _callbackHandler.WaitForCompletion();     // it will throw a proper exception in a failure case        
        }

        internal void BuildSecureEnvironment(string applicationManifest)
        {
            CheckDisposed();

            Invariant.Assert(_envHandle == null);

            string providerPath = GetSecurityProviderPath();
            string machineCertificate = GetMachineCert();

            _defaultLibraryHandle = null;
            _envHandle = null;

            int hr = SafeNativeMethods.DRMInitEnvironment(
                (uint)SecurityProviderType.SoftwareSecRep,
                (uint)SpecType.FileName,
                providerPath,
                applicationManifest,
                machineCertificate,
                out _envHandle,
                out _defaultLibraryHandle);

            Errors.ThrowOnErrorCode(hr);
            Debug.Assert((_envHandle != null) && (!_envHandle.IsInvalid));
        }

        private bool IsActivated(ActivationFlags activateFlags)
        {
            CheckDisposed();

            return (0 == SafeNativeMethods.DRMIsActivated(_hSession, (uint)activateFlags, null));
        }

        internal string GetMachineCert()
        {
            CheckDisposed();

            // We always assume that the first machine certificate is the correct one
            return EnumerateLicense(EnumerateLicenseFlags.Machine, 0);
        }

        /// <summary>
        ///  This function is used to build a List of certificates of a given type (Licensor or Identity) 
        ///  From all of the certificates based on the matching of the User Id 
        /// </summary>
        internal List<string> EnumerateUsersCertificateIds(
                                                                                ContentUser user,
                                                                                EnumerateLicenseFlags certificateType)
        {
            CheckDisposed();

            if ((certificateType != EnumerateLicenseFlags.Machine) &&
                                    (certificateType != EnumerateLicenseFlags.GroupIdentity) &&
                                    (certificateType != EnumerateLicenseFlags.GroupIdentityName) &&
                                    (certificateType != EnumerateLicenseFlags.GroupIdentityLid) &&
                                    (certificateType != EnumerateLicenseFlags.SpecifiedGroupIdentity) &&
                                    (certificateType != EnumerateLicenseFlags.Eul) &&
                                    (certificateType != EnumerateLicenseFlags.EulLid) &&
                                    (certificateType != EnumerateLicenseFlags.ClientLicensor) &&
                                    (certificateType != EnumerateLicenseFlags.ClientLicensorLid) &&
                                    (certificateType != EnumerateLicenseFlags.SpecifiedClientLicensor) &&
                                    (certificateType != EnumerateLicenseFlags.RevocationList) &&
                                    (certificateType != EnumerateLicenseFlags.RevocationListLid) &&
                                    (certificateType != EnumerateLicenseFlags.Expired))
            {
                throw new ArgumentOutOfRangeException("certificateType");
            }

            List<string> certificateIdList = new List<string>();

            int index = 0;
            // first enumerate certificates and find the ones that match given user 
            while (true)
            {
                // we get a string which can be parsed to get the ID and type 
                string currentUserCertificate = EnumerateLicense(certificateType, index);

                if (currentUserCertificate == null)
                    break;

                // we need to parse the information out of the string 
                ContentUser currentUser = ExtractUserFromCertificateChain(currentUserCertificate);

                // let's see if we have a match on the User Id, if we do we need to add it to the list 
                if (user.GenericEquals(currentUser))
                {
                    // we got a match let's preserve the certificate in the list 
                    certificateIdList.Add(ClientSession.ExtractCertificateIdFromCertificateChain(currentUserCertificate));
                }

                index++;
            }

            return certificateIdList;
        }

        internal void DeleteLicense(string licenseId)
        {
            CheckDisposed();

            int hr = SafeNativeMethods.DRMDeleteLicense(_hSession, licenseId);
            Errors.ThrowOnErrorCode(hr);
        }

        internal void RemoveUsersCertificates(EnumerateLicenseFlags certificateType)
        {
            CheckDisposed();

            // We only expect to be called for removal of the specific Group Identity Certs
            // and the specific Client Licensor Certs  
            Invariant.Assert((certificateType == EnumerateLicenseFlags.SpecifiedClientLicensor) ||
                                  (certificateType == EnumerateLicenseFlags.SpecifiedGroupIdentity));

            // We actually need to enumerate all the specified identity certs and then parse
            // them to get the license Id , and only then remove them 
            ArrayList certList = EnumerateAllValuesOnSession(_hSession, certificateType);

            foreach (string cert in certList)
            {
                DeleteLicense(ExtractCertificateIdFromCertificateChain(cert));
            }
        }

        private string GetClientLicensorCert()
        {
            return GetLatestCertificate(EnumerateLicenseFlags.SpecifiedClientLicensor);
        }

        private string GetGroupIdentityCert()
        {
            return GetLatestCertificate(EnumerateLicenseFlags.SpecifiedGroupIdentity);
        }

        private string GetLatestCertificate(EnumerateLicenseFlags enumerateLicenseFlags)
        {
            int index = 0;

            string currentCert = EnumerateLicense(enumerateLicenseFlags, index);
            if (currentCert == null)
            {
                return null;
            }

            DateTime currentTimeStamp = ExtractIssuedTimeFromCertificateChain(currentCert, DateTime.MinValue);

            while (currentCert != null)
            {
                index++;

                string newCert = EnumerateLicense(enumerateLicenseFlags, index);

                // if we have completed the enumeration we can stop right here
                if (newCert == null)
                {
                    break;
                }

                DateTime newTimeStamp = ExtractIssuedTimeFromCertificateChain(newCert, DateTime.MinValue);

                if (DateTime.Compare(currentTimeStamp, newTimeStamp) < 0)
                {
                    currentCert = newCert;
                    currentTimeStamp = newTimeStamp;
                }
            }

            return currentCert;
        }

        private static ArrayList EnumerateAllValuesOnSession(SafeRightsManagementSessionHandle sessionHandle, EnumerateLicenseFlags enumerateLicenseFlags)
        {
            ArrayList result = new ArrayList(5);
            int index = 0;

            while (true)
            {
                string currentRes = GetLicenseOnSession(sessionHandle, enumerateLicenseFlags, index);

                if (currentRes == null)
                {
                    break;
                }
                result.Add(currentRes);
                index++;
            }
            return result;
        }

        internal static string GetLicenseOnSession(SafeRightsManagementSessionHandle sessionHandle, EnumerateLicenseFlags enumerateLicenseFlags, int index)
        {
            Invariant.Assert(index >= 0);

            if ((enumerateLicenseFlags != EnumerateLicenseFlags.Machine) &&
                (enumerateLicenseFlags != EnumerateLicenseFlags.GroupIdentity) &&
                (enumerateLicenseFlags != EnumerateLicenseFlags.GroupIdentityName) &&
                (enumerateLicenseFlags != EnumerateLicenseFlags.GroupIdentityLid) &&
                (enumerateLicenseFlags != EnumerateLicenseFlags.SpecifiedGroupIdentity) &&
                (enumerateLicenseFlags != EnumerateLicenseFlags.Eul) &&
                (enumerateLicenseFlags != EnumerateLicenseFlags.EulLid) &&
                (enumerateLicenseFlags != EnumerateLicenseFlags.ClientLicensor) &&
                (enumerateLicenseFlags != EnumerateLicenseFlags.ClientLicensorLid) &&
                (enumerateLicenseFlags != EnumerateLicenseFlags.SpecifiedClientLicensor) &&
                (enumerateLicenseFlags != EnumerateLicenseFlags.RevocationList) &&
                (enumerateLicenseFlags != EnumerateLicenseFlags.RevocationListLid) &&
                (enumerateLicenseFlags != EnumerateLicenseFlags.Expired))
            {
                throw new ArgumentOutOfRangeException("enumerateLicenseFlags");
            }

            int hr = 0;
            bool sharedFlag = false;
            uint dataLen = 0;
            StringBuilder license = null;

            hr = SafeNativeMethods.DRMEnumerateLicense(
                sessionHandle, (uint)enumerateLicenseFlags, (uint)index, ref sharedFlag, ref dataLen, null);

            if (hr == (int)RightsManagementFailureCode.NoMoreData)
                return null;

            Errors.ThrowOnErrorCode(hr);

            if (dataLen > System.Int32.MaxValue)
                return null;

            //returned size accounts for null termination; we do not need to add 1 
            checked
            {
                license = new StringBuilder((int)dataLen);
            }

            hr = SafeNativeMethods.DRMEnumerateLicense(
                sessionHandle, (uint)enumerateLicenseFlags, (uint)index, ref sharedFlag, ref dataLen, license);

            Errors.ThrowOnErrorCode(hr);

            return license.ToString();
        }

        internal string EnumerateLicense(EnumerateLicenseFlags enumerateLicenseFlags, int index)
        {
            CheckDisposed();

            return GetLicenseOnSession(_hSession, enumerateLicenseFlags, index);
        }

        internal PublishLicense SignIssuanceLicense(IssuanceLicense issuanceLicense, out UseLicense authorUseLicense)
        {
            CheckDisposed();

            Invariant.Assert(issuanceLicense != null);
            Invariant.Assert(!_envHandle.IsInvalid);

            using (CallbackHandler signIssuanceLicenseCallbackHandler = new CallbackHandler())
            {
                string clientLicensorCertificate = GetClientLicensorCert();

                if (clientLicensorCertificate == null)
                    throw new RightsManagementException(SR.Get(SRID.UserHasNoClientLicensorCert));

                // Trim all the leading and trailing white space characters
                // of the clientLicensorCertificate.
                clientLicensorCertificate = clientLicensorCertificate.Trim();

                // Make sure the clientLicensorCertificate is valid. By trimming white spaces
                // above, if the certificate string is empty or contains only white spaces, it
                // is empty now.
                if (clientLicensorCertificate.Length == 0)
                    throw new RightsManagementException(SR.Get(SRID.UserHasNoClientLicensorCert));

                // Offline publishing supported no Online publishing support 
                int hr = SafeNativeMethods.DRMGetSignedIssuanceLicense(
                    _envHandle,
                    issuanceLicense.Handle,
                    (uint)(SignIssuanceLicenseFlags.Offline |
                                SignIssuanceLicenseFlags.AutoGenerateKey |
                                SignIssuanceLicenseFlags.OwnerLicenseNoPersist),
                    null,
                    0,
                    NativeConstants.ALGORITHMID_AES,     // currently AES is the only supported key type 
                    clientLicensorCertificate,
                    signIssuanceLicenseCallbackHandler.CallbackDelegate,
                    null, // we are only supporting offline publishing no url needed
                    0); // no context required

                Errors.ThrowOnErrorCode(hr);

                signIssuanceLicenseCallbackHandler.WaitForCompletion();     // it will throw a proper exception in a failure case                

                // build publish License from th result 
                PublishLicense publishLicense = new PublishLicense(
                    signIssuanceLicenseCallbackHandler.CallbackData);

                // After Issuance license is signed we should build the Author's Use License 
                authorUseLicense = new UseLicense(GetOwnerLicense(issuanceLicense.Handle));

                return publishLicense;
            }
        }

        internal UseLicense AcquireUseLicense(string publishLicense, bool noUI)
        {
            CheckDisposed();

            Invariant.Assert(!_envHandle.IsInvalid);

            SafeRightsManagementSessionHandle licenseStorageSessionHandle = null;

            //first let's build the license storage session
            int hr = SafeNativeMethods.DRMCreateLicenseStorageSession(
                _envHandle,
                _defaultLibraryHandle,
                _hSession,
                0,
                publishLicense,
                out licenseStorageSessionHandle);

            Errors.ThrowOnErrorCode(hr);
            Debug.Assert((licenseStorageSessionHandle != null) && (!licenseStorageSessionHandle.IsInvalid));

            using (licenseStorageSessionHandle)
            {
                uint flags = 0;
                if (noUI)
                {
                    flags |= (uint)AcquireLicenseFlags.NoUI;
                }

                string license = GetGroupIdentityCert();

                // the newly acquired use license will be added to the License Storage Session. 
                // We need to enumerate all the entries before and after in order to properly find the new one 
                ArrayList oldLicenseIds = EnumerateAllValuesOnSession
                                        (licenseStorageSessionHandle, EnumerateLicenseFlags.EulLid);

                hr = SafeNativeMethods.DRMAcquireLicense(
                    licenseStorageSessionHandle,
                    flags,
                    license,
                    null,  // requested data is reserved and not used
                    null, // custom data 
                    null, //no url required it will be taken from publish license   
                    IntPtr.Zero);    // context 

                Errors.ThrowOnErrorCode(hr);

                _callbackHandler.WaitForCompletion();         // it will throw a proper exception in a failure case            

                // now we can enumerate the EUL Ids  again and try to find the new one 
                ArrayList newLicenseIds = EnumerateAllValuesOnSession
                                        (licenseStorageSessionHandle, EnumerateLicenseFlags.EulLid);

                int indexOfTheAcquiredLicense = FindNewEntryIndex(oldLicenseIds, newLicenseIds);

                if (indexOfTheAcquiredLicense < 0)
                {
                    // we have failed to find the new license 
                    throw new RightsManagementException(RightsManagementFailureCode.LicenseAcquisitionFailed);
                }

                return new UseLicense(GetLicenseOnSession(
                                                            licenseStorageSessionHandle,
                                                            EnumerateLicenseFlags.Eul,
                                                            indexOfTheAcquiredLicense));
            }
        }

        private static int FindNewEntryIndex(ArrayList oldList, ArrayList newList)
        {
            Invariant.Assert((oldList != null) && (newList != null));

            for (int i = 0; i < newList.Count; i++)
            {
                string newElement = (string)newList[i];
                bool matchFound = false;

                foreach (string oldElement in oldList)
                {
                    if (String.CompareOrdinal(newElement, oldElement) == 0)
                    {
                        matchFound = true;
                        break;
                    }
                }

                // we have found an entry in the newList without a match in the old list 
                // we can return the index 
                if (!matchFound)
                {
                    return i;
                }
            }

            // No new entry were found 
            return -1;
        }

        // This function attempts to bind License to a given Identity 
        // It will try to bind all rights One-by-one in order to eliminate
        // grants that may have been expired, so it will only bind The ones that are still valid 
        private CryptoProvider BindUseLicense(string serializedUseLicense,
                                                                List<RightNameExpirationInfoPair> unboundRightsList,
                                                                BoundLicenseParams boundLicenseParams,
                                                                out int theFirstHrFailureCode)
        {
            Debug.Assert(serializedUseLicense != null);
            Debug.Assert(unboundRightsList != null);
            Debug.Assert(boundLicenseParams != null);


            List<SafeRightsManagementHandle> successfullyBoundLicenseHandleList =
                        new List<SafeRightsManagementHandle>(unboundRightsList.Count);

            List<RightNameExpirationInfoPair> successfullyBoundRightsList =
                        new List<RightNameExpirationInfoPair>(unboundRightsList.Count);
            try
            {
                uint errorLogHandle;

                // we neeed to return the first failure code, that is the one that will communicate to the user 
                int hr;
                theFirstHrFailureCode = 0;

                SafeRightsManagementHandle boundLicenseHandle;

                // first we are enumerating all rights one-by-one and preserving the ones that can be bound 
                // we are going through the list of "recognised rights"
                foreach (RightNameExpirationInfoPair rightInfo in unboundRightsList)
                {
                    boundLicenseParams.wszRightsRequested = rightInfo.RightName;
                    boundLicenseHandle = null;
                    errorLogHandle = 0;

                    hr = SafeNativeMethods.DRMCreateBoundLicense(
                                                _envHandle,
                                                boundLicenseParams,
                                                serializedUseLicense,
                                                out boundLicenseHandle,
                                                out errorLogHandle);

                    if (boundLicenseHandle != null && (hr == 0))
                    {
                        // we got a successful bound let's copy the whole grant 
                        // the only thing that we need to substitute in the grant is the User identity 
                        // along with the original right name (prior to binding), as unmanaged SDK 
                        // messes up right names and their expiration 
                        // in case of multiple expiration dates and additional rights granted to an owner
                        successfullyBoundLicenseHandleList.Add(boundLicenseHandle);
                        successfullyBoundRightsList.Add(rightInfo);
                    }

                    // preserve the first encountered error code 
                    if ((theFirstHrFailureCode == 0) && (hr != 0))
                    {
                        theFirstHrFailureCode = hr;
                    }
                }

                // At this point we have a list of potential "Right" -candidates 
                // if it is empty we can get out 
                if (successfullyBoundLicenseHandleList.Count > 0)
                {
                    ContentUser user = ExtractUserFromCertificateChain(boundLicenseParams.wszDefaultEnablingPrincipalCredentials);

                    CryptoProvider cryptoProvider = new CryptoProvider(successfullyBoundLicenseHandleList,
                                                                    successfullyBoundRightsList,
                                                                    user);
                    CryptoProviderList.Add(cryptoProvider);
                    return cryptoProvider;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                // In case of a failure we should clean up handle that have been accumulated
                // otherwise the list of handles is either empty or given to the CryptoProvider 
                // to be taken care of
                foreach (SafeRightsManagementHandle handle in successfullyBoundLicenseHandleList)
                {
                    handle.Dispose();
                }
                throw;
            }
        }


        internal CryptoProvider TryBindUseLicenseToAllIdentites(string serializedUseLicense)
        {
            CheckDisposed();

            Invariant.Assert(serializedUseLicense != null);
            
            int hr = 0;
            int theFirstHrFailureCode = 0;

            ///////////////////////////
            // prepare bound license param structure 
            ///////////////////////////
            //prepare for binding (enumerate unbound rights)
            string rightsGroupName;

            List<RightNameExpirationInfoPair> unboundRightsList =
                        GetRightsInfoFromUseLicense(serializedUseLicense, out rightsGroupName);

            BoundLicenseParams boundLicenseParams = new BoundLicenseParams();

            boundLicenseParams.uVersion = 0;
            boundLicenseParams.hEnablingPrincipal = 0;
            boundLicenseParams.hSecureStore = 0;
            boundLicenseParams.wszRightsGroup = rightsGroupName;

            string contentId;
            string contentIdType;
            GetContentIdFromLicense(serializedUseLicense, out contentId, out contentIdType);

            boundLicenseParams.DRMIDuVersion = 0;
            boundLicenseParams.DRMIDIdType = contentIdType;
            boundLicenseParams.DRMIDId = contentId;

            boundLicenseParams.cAuthenticatorCount = 0;//reserved.should be 0.
            boundLicenseParams.rghAuthenticators = IntPtr.Zero;

            string userCertificate = GetGroupIdentityCert();

            boundLicenseParams.wszDefaultEnablingPrincipalCredentials = userCertificate;

            boundLicenseParams.dwFlags = 0;

            // let's try to bind this using currently provided user first 
            CryptoProvider cryptoProvider = BindUseLicense(serializedUseLicense,
                                                                unboundRightsList,
                                                                boundLicenseParams,
                                                                out hr);

            if (cryptoProvider != null)
            {
                return cryptoProvider;
            }

            // preserve the first encountered error code 
            if ((theFirstHrFailureCode == 0) && (hr != 0))
            {
                theFirstHrFailureCode = hr;
            }

            // now if the current user failed we can try to enumerate all the userr certificates 
            // and go through them one-by-one 
            int userCertIndex = 0;
            while (true)
            {
                userCertificate = EnumerateLicense(EnumerateLicenseFlags.GroupIdentity, userCertIndex);
                if (userCertificate == null)
                {
                    // we have to enumerate all of the user certs . . . 
                    break;
                }
                userCertIndex++;
                boundLicenseParams.wszDefaultEnablingPrincipalCredentials = userCertificate;

                cryptoProvider = BindUseLicense(serializedUseLicense,
                                                                unboundRightsList,
                                                                boundLicenseParams,
                                                                out hr);

                if (cryptoProvider != null)
                {
                    return cryptoProvider;
                }

                // preserve the first encountered error code 
                if ((theFirstHrFailureCode == 0) && (hr != 0))
                {
                    theFirstHrFailureCode = hr;
                }
            }

            // at this point we can only translate failure into some meaningfull exception 
            Invariant.Assert(theFirstHrFailureCode != 0); // it must contain an error as a succesfull return above should take of non-failure cases
            Errors.ThrowOnErrorCode(theFirstHrFailureCode);
            return null;
        }

        // The newer SDK automatically does machine activation. This function would be necessary if we had
        // to support the older SDK.
#if needToActivateExplicitly
        private Uri GetActivationUrl(AuthenticationType authentication)
        {
            if (authentication == AuthenticationType.Windows)
            {
                return GetServiceLocation(ServiceType.Activation, ServiceLocation.Enterprise, null);
            }
            else if (authentication == AuthenticationType.Passport)
            {
                return GetServiceLocation(ServiceType.Activation, ServiceLocation.Internet, null);
            }
            else
            {
                Debug.Assert(false,"Invalid Authentication type");
                return null;            // retail build might be ale to recover from SDK defaults                  
            }
        }
#endif

        private Uri GetCertificationUrl(AuthenticationType authentication)
        {
            Debug.Assert((authentication == AuthenticationType.Windows) ||
                               (authentication == AuthenticationType.Passport));

            // for Passport scenario we just use default null value everywhere and expect promethium 
            // SDK do proper server discovery 
            Uri server = null;

            if (authentication == AuthenticationType.Windows)
            {
                // first try to get Corporate Domain server, if it fails try the internet 
                // regardless of the user type 
                server = GetServiceLocation(ServiceType.Certification, ServiceLocation.Enterprise, null);

                if (server == null)
                {
                    server = GetServiceLocation(ServiceType.Certification, ServiceLocation.Internet, null);
                }
            }
            else // it must be passport 
            {
                // 1st we need to check regiostry for override, and then if it missing we can use Discovery Service
                server = GetRegistryPassportCertificationUrl();
                if (server == null)
                {  // let's use server discovery                 
                    server = GetServiceLocation(ServiceType.Certification, ServiceLocation.Internet, null);
                }
            }

            return server;
        }



        private static Uri GetRegistryPassportCertificationUrl()
        {
            // This Function Will return null, if the registry entry is missing
            RegistryKey key = Registry.LocalMachine.OpenSubKey(_passportActivationRegistryKeyName);
            if (key == null)
            {
                return null;
            }
            else
            {
                object keyValue = key.GetValue(null); // this should get the default value
                string stringValue = keyValue as string;
                if (stringValue != null)
                {
                    return new Uri(stringValue);
                }
                else
                {
                    return null;
                }
            }
        }

        private Uri GetClientLicensorUrl(AuthenticationType authentication)
        {
            Debug.Assert((authentication == AuthenticationType.Windows) ||
                               (authentication == AuthenticationType.Passport));

            // Both, for Passport and Windows authenticvation types scenarios we do the same server discovery: 
            // first try to get Corporate Domain server
            Uri server = GetServiceLocation(ServiceType.ClientLicensor, ServiceLocation.Enterprise, null);

            if (server == null)
            {
                // if it fails try the internet discovery services 
                server = GetServiceLocation(ServiceType.ClientLicensor, ServiceLocation.Internet, null);
            }

            return server;
        }

#if false           // re-enable when needed, This is only needed for on-line publishing, which currently isn't supported 
        private Uri GetClientPublishingUrl()
        {
            // first try to get Corporate Domain server, if it fails try the internet 
            // regardless of the user type 
            Uri server = GetServiceLocation(ServiceType.Publishing, ServiceLocation.Enterprise, null);

            if (server != null)
            {
                return server;
            }

            return GetServiceLocation(ServiceType.Publishing, ServiceLocation.Internet, null);            
        }        
#endif

        /// <summary>
        /// The Activate function obtains a lockbox and machine certificate for a machine or a rights 
        /// account certificate for a user (depend on activationFlags). 
        /// </summary>
        private string Activate(
            ActivationFlags activationFlags, Uri url)
        {
            // Optional server information. To query UDDI for an activation URL, pass in NULL
            ActivationServerInfo activationServer = null;

            if (url != null)
            {
                activationServer = new ActivationServerInfo();
                activationServer.PubKey = null;
                activationServer.Url = url.AbsoluteUri;  // We are using Uri class as a basic validation mechanism. These URIs come from unmanaged 
                // code libraries and go back as parameters into the unmanaged code libraries. 
                // We use AbsoluteUri property as means of verifying that it is actually an absolute and 
                // well formed Uri. If by any chance it happened to be a relative URI, an exception will 
                // be thrown here. This will perform the necessary escaping.

                activationServer.Version = NativeConstants.DrmCallbackVersion;
            }

            int hr = SafeNativeMethods.DRMActivate(
                _hSession,
                (uint)activationFlags,
                0,                          //language Id 
                activationServer,
                IntPtr.Zero,    // context 
                IntPtr.Zero);  // parent Window handle 

            Errors.ThrowOnErrorCode(hr);

            _callbackHandler.WaitForCompletion();        // it will throw a proper exception in a failure case             

            return _callbackHandler.CallbackData;
        }

        private Uri GetServiceLocation(
                ServiceType serviceType, ServiceLocation serviceLocation, string issuanceLicense)
        {
            uint serviceUrlLength = 0;
            StringBuilder serviceUrl = null;

            int hr = SafeNativeMethods.DRMGetServiceLocation(
                _hSession, (uint)serviceType, (uint)serviceLocation, issuanceLicense, ref serviceUrlLength, null);

            if (hr == (int)RightsManagementFailureCode.UseDefault)
            {
                // there is a special case in which this error code means that application supposed to use the default nul URL 
                return null;
            }

            Errors.ThrowOnErrorCode(hr);

            checked
            {
                serviceUrl = new StringBuilder((int)serviceUrlLength);
            }

            hr = SafeNativeMethods.DRMGetServiceLocation(
                _hSession, (uint)serviceType, (uint)serviceLocation, issuanceLicense, ref serviceUrlLength, serviceUrl);

            Errors.ThrowOnErrorCode(hr);

            return new Uri(serviceUrl.ToString());
        }

        internal static string GetOwnerLicense(SafeRightsManagementPubHandle issuanceLicenseHandle)
        {
            Invariant.Assert(!issuanceLicenseHandle.IsInvalid);

            uint ownerLicenseLength = 0;
            StringBuilder ownerLicense = null;

            int hr = SafeNativeMethods.DRMGetOwnerLicense(
                issuanceLicenseHandle, ref ownerLicenseLength, null);
            Errors.ThrowOnErrorCode(hr);

            checked
            {
                ownerLicense = new StringBuilder((int)ownerLicenseLength);
            }

            hr = SafeNativeMethods.DRMGetOwnerLicense(
                issuanceLicenseHandle, ref ownerLicenseLength, ownerLicense);
            Errors.ThrowOnErrorCode(hr);

            return ownerLicense.ToString();
        }

        static private string GetElementFromCertificateChain(
            string certificateChain, int index)
        {
            Invariant.Assert(index >= 0);
            Invariant.Assert(certificateChain != null);
            uint chainElementSize = 0;
            StringBuilder chainElement = null;

            int hr = SafeNativeMethods.DRMDeconstructCertificateChain(
                certificateChain,
                (uint)index,
                ref chainElementSize,
                null);

            Errors.ThrowOnErrorCode(hr);

            checked
            {
                chainElement = new StringBuilder((int)chainElementSize);
            }

            hr = SafeNativeMethods.DRMDeconstructCertificateChain(
                certificateChain,
                (uint)index,
                ref chainElementSize,
                chainElement);

            Errors.ThrowOnErrorCode(hr);

            return chainElement.ToString();
        }

        private static string GetUnboundLicenseStringAttribute(
            SafeRightsManagementQueryHandle queryHandle, string attributeType, uint attributeIndex)
        {
            uint attributeSize = 0;
            byte[] dataBuffer = null;

            uint encodingType;

            // get the attribute information (memory size to be allocated)
            int hr = SafeNativeMethods.DRMGetUnboundLicenseAttribute(
                queryHandle, attributeType, attributeIndex, out encodingType, ref attributeSize, null);

            if (hr == (int)RightsManagementFailureCode.QueryReportsNoResults)
            {
                return null;
            }
            Errors.ThrowOnErrorCode(hr);

            // this is the size of the null terminator so essentially this is an empty string
            if (attributeSize < 2)
                return null;

            checked
            {
                dataBuffer = new byte[(int)attributeSize];
            }

            hr = SafeNativeMethods.DRMGetUnboundLicenseAttribute(
                queryHandle, attributeType, attributeIndex, out encodingType, ref attributeSize, dataBuffer);
            Errors.ThrowOnErrorCode(hr);

            // we need to truncate the last 2 bytes that have unicode 0 termination
            return Encoding.Unicode.GetString(dataBuffer, 0, dataBuffer.Length - 2);
        }

        // This method has only one caller GetGrantsFromBoundUseLicense(), which is
        // in the CHK build only. So we should also preserve this method only in the CHK build.
        // Otherwise FxCop will complain: AvoidUncalledPrivateCode in the FREE built DLL.
#if DEBUG
        static private string GetBoundLicenseStringAttribute(
            SafeRightsManagementHandle queryHandle,
            string attributeType, 
            uint attributeIndex)
        {
            uint attributeSize = 0;
            byte[] dataBuffer = null;

            uint encodingType;

            int hr = SafeNativeMethods.DRMGetBoundLicenseAttribute(
                queryHandle, attributeType, attributeIndex, out encodingType, ref attributeSize, null);
            Errors.ThrowOnErrorCode(hr);

            if (encodingType != (uint)LicenseAttributeEncoding.String)
            {
                throw new RightsManagementException(RightsManagementFailureCode.InvalidLicense);
            }

            // this is the size of the null terminator so essentially this is an empty string
            if (attributeSize < 2)
                return null;

            checked
            {
                dataBuffer = new byte[(int)attributeSize];
            }

            hr = SafeNativeMethods.DRMGetBoundLicenseAttribute(
                queryHandle, attributeType, attributeIndex, out encodingType, ref attributeSize, dataBuffer);
            Errors.ThrowOnErrorCode(hr);

            // we need to truncate the last 2 bytes that have unicode 0 termination
            return Encoding.Unicode.GetString(dataBuffer, 0, dataBuffer.Length - 2);
        }
#endif

        static private DateTime GetUnboundLicenseDateTimeAttribute(
            SafeRightsManagementQueryHandle queryHandle,
            string attributeType,
            uint attributeIndex,
            DateTime defaultValue)
        {
            uint attributeSize = SystemTime.Size;
            byte[] dataBuffer = new byte[attributeSize];
            uint encodingType;

            int hr = SafeNativeMethods.DRMGetUnboundLicenseAttribute(
                queryHandle, attributeType, attributeIndex, out encodingType,
                ref attributeSize, dataBuffer);

            if (encodingType != (uint)LicenseAttributeEncoding.Time)
            {
                throw new RightsManagementException(RightsManagementFailureCode.InvalidLicense);
            }

            if ((hr == (int)RightsManagementFailureCode.NoMoreData) ||
                 (hr == (int)RightsManagementFailureCode.QueryReportsNoResults))
            {
                return defaultValue;
            }
            Errors.ThrowOnErrorCode(hr);

            Debug.Assert(attributeSize == SystemTime.Size); // if isn't true it is an indication of a problem in the underlying libraries

            SystemTime sysTime = new SystemTime(dataBuffer);

            return sysTime.GetDateTime(defaultValue);
        }

        // This method has only one caller GetGrantsFromBoundUseLicense(), which is
        // in the CHK build only. So we should also preserve this method only in the CHK build.
        // Otherwise FxCop will complain: AvoidUncalledPrivateCode in the FREE built DLL.
#if DEBUG
        static private DateTime GetBoundLicenseDateTimeAttribute(
            SafeRightsManagementHandle queryHandle,
            string attributeType,
            uint attributeIndex,
            DateTime defaultValue)
        {
            uint attributeSize = SystemTime.Size;
            byte[] dataBuffer = new byte[attributeSize];
            uint encodingType;

            int hr = SafeNativeMethods.DRMGetBoundLicenseAttribute(
                queryHandle, attributeType, attributeIndex, out encodingType,
                ref attributeSize, dataBuffer);

            if (encodingType != (uint)LicenseAttributeEncoding.Time)
            {
                throw new RightsManagementException(RightsManagementFailureCode.InvalidLicense);
            }

            if ((hr == (int)RightsManagementFailureCode.NoMoreData) ||
                 (hr == (int)RightsManagementFailureCode.QueryReportsNoResults))
            {
                return defaultValue;
            }
            Errors.ThrowOnErrorCode(hr);

            Debug.Assert(attributeSize == SystemTime.Size); // if isn't true it is an indication of a problem in the underlying libraries

            SystemTime sysTime = new SystemTime(dataBuffer);

            return sysTime.GetDateTime(defaultValue);
        }
#endif

        internal static ContentUser ExtractUserFromCertificateChain(string certificateChain)
        {
            Invariant.Assert(certificateChain != null);

            return ExtractUserFromCertificate(GetElementFromCertificateChain(certificateChain, 0));
        }


        private static DateTime ExtractIssuedTimeFromCertificateChain(
                        string certificateChain,
                        DateTime defaultValue)
        {
            Invariant.Assert(certificateChain != null);

            return ExtractIssuedTimeFromCertificate(GetElementFromCertificateChain(certificateChain, 0), defaultValue);
        }

        private static DateTime ExtractIssuedTimeFromCertificate(
                        string certificate,
                        DateTime defaultValue)
        {
            SafeRightsManagementQueryHandle queryRootHandle = null;
            int hr;

            hr = SafeNativeMethods.DRMParseUnboundLicense(
                certificate,
                out queryRootHandle);
            Errors.ThrowOnErrorCode(hr);
            Debug.Assert((queryRootHandle != null) && (!queryRootHandle.IsInvalid));

            using (queryRootHandle)
            {
                return GetUnboundLicenseDateTimeAttribute(
                            queryRootHandle,
                            NativeConstants.QUERY_ISSUEDTIME,
                            0,
                            defaultValue);
            }
        }

        internal static ContentUser
            ExtractUserFromCertificate(string certificate)
        {
            SafeRightsManagementQueryHandle queryRootHandle = null;

            int hr;

            hr = SafeNativeMethods.DRMParseUnboundLicense(
                certificate,
                out queryRootHandle);
            Errors.ThrowOnErrorCode(hr);
            Debug.Assert((queryRootHandle != null) && (!queryRootHandle.IsInvalid));

            using (queryRootHandle)
            {
                SafeRightsManagementQueryHandle querySubHandle = null;

                hr = SafeNativeMethods.DRMGetUnboundLicenseObject(
                    queryRootHandle,
                    NativeConstants.QUERY_ISSUEDPRINCIPAL,
                    0,
                    out querySubHandle);
                Errors.ThrowOnErrorCode(hr);
                Debug.Assert((querySubHandle != null) && (!querySubHandle.IsInvalid));

                using (querySubHandle)
                {
                    string name = GetUnboundLicenseStringAttribute(
                        querySubHandle,
                        NativeConstants.QUERY_NAME,
                        0);

                    string authenticationType = GetUnboundLicenseStringAttribute(
                        querySubHandle,
                        NativeConstants.QUERY_IDTYPE,
                        0);

                    // We recognise authentication type Windows everything else is assumed to be Passport 
                    if (String.CompareOrdinal(
                        AuthenticationType.Windows.ToString().ToUpper(CultureInfo.InvariantCulture),
                        authenticationType.ToUpper(CultureInfo.InvariantCulture)) == 0)
                    {
                        return new ContentUser(name, AuthenticationType.Windows);
                    }
                    else
                    {
                        return new ContentUser(name, AuthenticationType.Passport);
                    }
                }
            }
        }

        internal static string ExtractCertificateIdFromCertificateChain(string certificateChain)
        {
            Invariant.Assert(certificateChain != null);

            return ExtractCertificateIdFromCertificate(GetElementFromCertificateChain(certificateChain, 0));
        }

        internal static string
            ExtractCertificateIdFromCertificate(string certificate)
        {
            SafeRightsManagementQueryHandle queryRootHandle = null;

            int hr = SafeNativeMethods.DRMParseUnboundLicense(
                certificate,
                out queryRootHandle);
            Errors.ThrowOnErrorCode(hr);
            Debug.Assert((queryRootHandle != null) && (!queryRootHandle.IsInvalid));

            using (queryRootHandle)
            {
                string certificateId = GetUnboundLicenseStringAttribute(
                    queryRootHandle,
                    NativeConstants.QUERY_IDVALUE,
                    0);

                return certificateId;
            }
        }

        internal static Dictionary<string, string> ExtractApplicationSpecificDataFromLicense(string useLicenseChain)
        {
            Invariant.Assert(useLicenseChain != null);
            Dictionary<string, string> _applicationSpecificDataDictionary =
                        new Dictionary<string, string>(3, StringComparer.Ordinal);

            string useLicense =
                GetElementFromCertificateChain(useLicenseChain, 0);
            Invariant.Assert(useLicense != null);

            SafeRightsManagementQueryHandle queryRootHandle = null;

            int hr = SafeNativeMethods.DRMParseUnboundLicense(useLicense, out queryRootHandle);
            Errors.ThrowOnErrorCode(hr);
            Debug.Assert((queryRootHandle != null) && (!queryRootHandle.IsInvalid));

            using (queryRootHandle)
            {
                uint index = 0;
                while (true)
                {
                    // extract Application Data Name 
                    string attributeName = GetUnboundLicenseStringAttribute(queryRootHandle,
                                                            NativeConstants.QUERY_APPDATANAME,
                                                            index);
                    if (attributeName == null) // null is used to indicate a missing value or an end of sequence 
                    {
                        break;
                    }
                    Errors.ThrowOnErrorCode(hr);

                    // extract Application Data Value 
                    string attributeValue = GetUnboundLicenseStringAttribute(queryRootHandle,
                                                            NativeConstants.QUERY_APPDATAVALUE,
                                                            index);
                    Errors.ThrowOnErrorCode(hr);

                    // we expect that dictionary will validate all necessary key/value data requirements 
                    _applicationSpecificDataDictionary.Add(attributeName, attributeValue);

                    index++;
                }
            }

            return _applicationSpecificDataDictionary;
        }

        internal static void GetContentIdFromLicense(
            string useLicenseChain,
            out string contentId,
            out string contentIdType)
        {
            Invariant.Assert(useLicenseChain != null);

            string useLicense =
                GetElementFromCertificateChain(useLicenseChain, 0);
            Invariant.Assert(useLicense != null);

            SafeRightsManagementQueryHandle queryRootHandle = null;

            // Parse the license and get the query handle 
            int hr = SafeNativeMethods.DRMParseUnboundLicense(
                useLicense,
                out queryRootHandle);
            Errors.ThrowOnErrorCode(hr);
            Debug.Assert((queryRootHandle != null) && (!queryRootHandle.IsInvalid));

            using (queryRootHandle)
            {
                SafeRightsManagementQueryHandle workItemQueryHandle = null;

                // extract object information from each Work Item 
                hr = SafeNativeMethods.DRMGetUnboundLicenseObject(
                                        queryRootHandle,
                                        NativeConstants.QUERY_WORK,
                                        0,
                                        out workItemQueryHandle);
                Errors.ThrowOnErrorCode(hr);
                Debug.Assert((workItemQueryHandle != null) && (!workItemQueryHandle.IsInvalid));

                using (workItemQueryHandle)
                {
                    // get the attributes we are after 
                    contentIdType = GetUnboundLicenseStringAttribute(
                        workItemQueryHandle,
                        NativeConstants.QUERY_IDTYPE,
                        0);

                    contentId = GetUnboundLicenseStringAttribute(
                        workItemQueryHandle,
                        NativeConstants.QUERY_IDVALUE,
                        0);
                }
            }
        }

        #region Debug
        // We currently don’t use these two methods, but they may be useful in the future. 
        // So we keep them in the debug build only, and changed them from internal methods 
        // to private methods to remove them from asmmeta files.
#if DEBUG
        private static List<ContentGrant> GetGrantsFromBoundUseLicense(
                                    SafeRightsManagementHandle boundUseLicenseHandle, ContentUser user)
        {
            Invariant.Assert(!boundUseLicenseHandle.IsInvalid);

            List<ContentGrant> resultList = new List<ContentGrant>(10);

            // Go through each ContentRight within group  item
            for (uint rightIndex = 0; ; rightIndex++)
            {
                // extract object information from each Work Item 
                SafeRightsManagementHandle rightQueryHandle = null;

                int hr = SafeNativeMethods.DRMGetBoundLicenseObject(
                    boundUseLicenseHandle,
                    NativeConstants.QUERY_RIGHT,
                    rightIndex,
                    out rightQueryHandle);

                if ((hr == (int)RightsManagementFailureCode.NoMoreData) ||
                     (hr == (int)RightsManagementFailureCode.QueryReportsNoResults))
                {
                    // we got to the end of the RIGHT's list  
                    break;
                }

                Errors.ThrowOnErrorCode(hr);
                Debug.Assert((rightQueryHandle != null) && (!rightQueryHandle.IsInvalid));

                using (rightQueryHandle)
                {
                    // We got to the "right" object, now we can ask for the name 
                    string rightName = GetBoundLicenseStringAttribute(rightQueryHandle, NativeConstants.QUERY_NAME, 0);

                    // if it is one of the erights that we "understand" we can proceed to query the time interval 
                    Nullable<ContentRight> right = GetRightFromString(rightName);

                    if (right != null)
                    {
                        DateTime timeFrom = DateTime.MinValue;
                        DateTime timeUntil = DateTime.MaxValue;

                        SafeRightsManagementHandle rangeTimeQueryHandle = null;

                        hr = SafeNativeMethods.DRMGetBoundLicenseObject(
                            rightQueryHandle,
                            NativeConstants.QUERY_RANGETIMECONDITION,
                            0,
                            out rangeTimeQueryHandle);


                        if ((hr != (int)RightsManagementFailureCode.NoMoreData) &&
                             (hr != (int)RightsManagementFailureCode.QueryReportsNoResults))
                        {
                            Errors.ThrowOnErrorCode(hr);
                            Debug.Assert((rangeTimeQueryHandle != null) && (!rangeTimeQueryHandle.IsInvalid));

                            using (rangeTimeQueryHandle)
                            {
                                timeFrom = GetBoundLicenseDateTimeAttribute(
                                                rangeTimeQueryHandle,
                                                NativeConstants.QUERY_FROMTIME,
                                                0,
                                                DateTime.MinValue);

                                timeUntil = GetBoundLicenseDateTimeAttribute(
                                                rangeTimeQueryHandle,
                                                NativeConstants.QUERY_UNTILTIME,
                                                0,
                                                DateTime.MaxValue);
                            }
                        }

                        resultList.Add(new ContentGrant(user, right.Value, timeFrom, timeUntil));
                    }
                }
            }
            return resultList;
        }

        private static List<ContentGrant> GetGrantsFromBoundUseLicenseList(
                                    List<SafeRightsManagementHandle> boundUseLicenseHandleList, ContentUser user)
        {
            Invariant.Assert(boundUseLicenseHandleList != null);

            List<ContentGrant> resultList = new List<ContentGrant>(boundUseLicenseHandleList.Count);

            // Go through each ContentRight within group  item
            foreach (SafeRightsManagementHandle boundUseLicenseHandle in boundUseLicenseHandleList)
            {
                Debug.Assert(!boundUseLicenseHandle.IsInvalid);

                List<ContentGrant> newList = GetGrantsFromBoundUseLicense(boundUseLicenseHandle, user);
                foreach (ContentGrant newGrant in newList)
                {
                    resultList.Add(newGrant);
                }
            }
            return resultList;
        }
#endif
        #endregion Debug

        private static List<RightNameExpirationInfoPair> GetRightsInfoFromUseLicense(
            string useLicenseChain,
            out string rightGroupName)
        {
            Invariant.Assert(useLicenseChain != null);

            string useLicense = GetElementFromCertificateChain(useLicenseChain, 0);
            Invariant.Assert(useLicense != null);

            List<RightNameExpirationInfoPair> resultRightsInfoList = new List<RightNameExpirationInfoPair>(10);

            SafeRightsManagementQueryHandle queryRootHandle = null;

            // Parse the license and get the query handle 
            int hr = SafeNativeMethods.DRMParseUnboundLicense(
                    useLicense,
                    out queryRootHandle);
            Errors.ThrowOnErrorCode(hr);
            Debug.Assert((queryRootHandle != null) && (!queryRootHandle.IsInvalid));

            using (queryRootHandle)
            {
                SafeRightsManagementQueryHandle workItemQueryHandle = null;

                // extract object information from the Work Item 
                hr = SafeNativeMethods.DRMGetUnboundLicenseObject(
                    queryRootHandle,
                    NativeConstants.QUERY_WORK,
                    0,
                    out workItemQueryHandle);
                Errors.ThrowOnErrorCode(hr);
                Debug.Assert((workItemQueryHandle != null) && (!workItemQueryHandle.IsInvalid));

                using (workItemQueryHandle)
                {
                    SafeRightsManagementQueryHandle rightGroupQueryHandle = null;

                    // extract object information from right group Item
                    hr = SafeNativeMethods.DRMGetUnboundLicenseObject(
                        workItemQueryHandle,
                        NativeConstants.QUERY_RIGHTSGROUP,
                        0,
                        out rightGroupQueryHandle);
                    Errors.ThrowOnErrorCode(hr);
                    Debug.Assert((rightGroupQueryHandle != null) && (!rightGroupQueryHandle.IsInvalid));

                    using (rightGroupQueryHandle)
                    {
                        rightGroupName = GetUnboundLicenseStringAttribute(
                            rightGroupQueryHandle,
                            NativeConstants.QUERY_NAME,
                            0);

                        // Go through each Right within group  item
                        for (uint rightIndex = 0; ; rightIndex++)
                        {
                            RightNameExpirationInfoPair rightInfo =
                                        GetRightInfoFromRightGroupQueryHandle(rightGroupQueryHandle, rightIndex);

                            if (rightInfo == null)
                            {
                                break;
                            }

                            resultRightsInfoList.Add(rightInfo);
                        }
                    }
                }
            }

            return resultRightsInfoList;
        }

        private static RightNameExpirationInfoPair GetRightInfoFromRightGroupQueryHandle
                            (SafeRightsManagementQueryHandle rightGroupQueryHandle, uint rightIndex)
        {
            SafeRightsManagementQueryHandle rightQueryHandle = null;

            int hr = SafeNativeMethods.DRMGetUnboundLicenseObject(
                    rightGroupQueryHandle,
                    NativeConstants.QUERY_RIGHT,
                    rightIndex,
                    out rightQueryHandle);

            if ((hr == (int)RightsManagementFailureCode.NoMoreData) ||
                     (hr == (int)RightsManagementFailureCode.QueryReportsNoResults))
            {
                // we got to the end of the RIGHT's list  
                return null;
            }
            Errors.ThrowOnErrorCode(hr);
            Debug.Assert((rightQueryHandle != null) && (!rightQueryHandle.IsInvalid));

            using (rightQueryHandle)
            {
                // We got to the "right" object, now we can ask for the name 
                string rightName = GetUnboundLicenseStringAttribute(
                                                                        rightQueryHandle,
                                                                        NativeConstants.QUERY_NAME,
                                                                        0);

                DateTime timeFrom = DateTime.MinValue;
                DateTime timeUntil = DateTime.MaxValue;

                SafeRightsManagementQueryHandle conditionListHandle = null;

                // we should also get the expiration infornmation out 
                hr = SafeNativeMethods.DRMGetUnboundLicenseObject(
                    rightQueryHandle,
                    NativeConstants.QUERY_CONDITIONLIST,
                    0,
                    out conditionListHandle);

                if (hr >= 0)
                {
                    Debug.Assert((conditionListHandle != null) && (!conditionListHandle.IsInvalid));

                    using (conditionListHandle)
                    {
                        SafeRightsManagementQueryHandle rangeTimeQueryHandle = null;

                        hr = SafeNativeMethods.DRMGetUnboundLicenseObject(
                            conditionListHandle,
                            NativeConstants.QUERY_RANGETIMECONDITION,
                            0,
                            out rangeTimeQueryHandle);

                        if ((hr != (int)RightsManagementFailureCode.NoMoreData) &&
                             (hr != (int)RightsManagementFailureCode.QueryReportsNoResults))
                        {
                            Errors.ThrowOnErrorCode(hr);
                            Debug.Assert((rangeTimeQueryHandle != null) && (!rangeTimeQueryHandle.IsInvalid));

                            using (rangeTimeQueryHandle)
                            {
                                timeFrom = GetUnboundLicenseDateTimeAttribute(
                                                rangeTimeQueryHandle,
                                                NativeConstants.QUERY_FROMTIME,
                                                0,
                                                DateTime.MinValue);

                                timeUntil = GetUnboundLicenseDateTimeAttribute(
                                                rangeTimeQueryHandle,
                                                NativeConstants.QUERY_UNTILTIME,
                                                0,
                                                DateTime.MaxValue);
                            }
                        }
                    }
                }

                return new RightNameExpirationInfoPair(rightName, timeFrom, timeUntil);
            }
        }

        internal static string GetContentIdFromPublishLicense(string publishLicense)
        {
            Invariant.Assert(publishLicense != null);

            SafeRightsManagementQueryHandle queryRootHandle = null;

            // Parse the license and get the query handle 
            int hr = SafeNativeMethods.DRMParseUnboundLicense(publishLicense, out queryRootHandle);
            Errors.ThrowOnErrorCode(hr);
            Debug.Assert((queryRootHandle != null) && (!queryRootHandle.IsInvalid));

            using (queryRootHandle)
            {
                SafeRightsManagementQueryHandle workQueryHandle = null;


                // extract object information from the Work Item 
                hr = SafeNativeMethods.DRMGetUnboundLicenseObject(
                                        queryRootHandle,
                                        NativeConstants.QUERY_WORK,
                                        0,
                                        out workQueryHandle);
                Errors.ThrowOnErrorCode(hr);
                Debug.Assert((workQueryHandle != null) && (!workQueryHandle.IsInvalid));

                // contentIdValue information from the root query object 
                using (workQueryHandle)
                {
                    return GetUnboundLicenseStringAttribute(
                                            workQueryHandle,
                                            NativeConstants.QUERY_IDVALUE,
                                            0);
                }
            }
        }

        internal static Uri GetUseLicenseAcquisitionUriFromPublishLicense(string publishLicense)
        {
            string nameAttributeValue;
            string addressAttributeValue;

            GetDistributionPointInfoFromPublishLicense
                        (publishLicense,
                        _distributionPointLicenseAcquisitionType,
                        out nameAttributeValue,
                        out addressAttributeValue);

            return new Uri(addressAttributeValue);
        }

        internal static void GetReferralInfoFromPublishLicense(
                            string publishLicense,
                            out string referralInfoName, 
                            out Uri referralInfoUri)
        {
            string nameAttributeValue;
            string addressAttributeValue;

            GetDistributionPointInfoFromPublishLicense
                        (publishLicense,
                        _distributionPointReferralInfoType,
                        out nameAttributeValue,
                        out addressAttributeValue);

            referralInfoName = nameAttributeValue;

            if (addressAttributeValue != null)
            {
                referralInfoUri = new Uri(addressAttributeValue);
            }
            else
            {
                referralInfoUri = null;
            }
        }

        private static void GetDistributionPointInfoFromPublishLicense(
                                                                            string publishLicense,
                                                                            string distributionPointType,
                                                                            out string nameAttributeValue,
                                                                            out string addressAttributeValue)
        {
            Invariant.Assert(publishLicense != null);

            // we are not making a distinction between truly missing values and NULL values 
            nameAttributeValue = null;
            addressAttributeValue = null;

            // Parse the license and get the query handle 
            SafeRightsManagementQueryHandle queryRootHandle = null;
            int hr = SafeNativeMethods.DRMParseUnboundLicense(publishLicense, out queryRootHandle);
            Errors.ThrowOnErrorCode(hr);
            Debug.Assert((queryRootHandle != null) && (!queryRootHandle.IsInvalid));

            using (queryRootHandle)
            {
                uint index = 0;

                while (true)
                {
                    SafeRightsManagementQueryHandle distributionPointQueryHandle = null;
                    // extract object information from the Root Item 
                    hr = SafeNativeMethods.DRMGetUnboundLicenseObject(
                                            queryRootHandle,
                                            NativeConstants.QUERY_DISTRIBUTIONPOINT,
                                            index,
                                            out distributionPointQueryHandle);
                    if (hr == (int)RightsManagementFailureCode.QueryReportsNoResults)
                    {
                        break;
                    }
                    Errors.ThrowOnErrorCode(hr);
                    Debug.Assert((distributionPointQueryHandle != null) && (!distributionPointQueryHandle.IsInvalid));

                    using (distributionPointQueryHandle)
                    {
                        string addressType = GetUnboundLicenseStringAttribute(
                                                distributionPointQueryHandle,
                                                NativeConstants.QUERY_OBJECTTYPE,
                                                0);
                        if (String.CompareOrdinal(addressType, distributionPointType) == 0)
                        {
                            nameAttributeValue = GetUnboundLicenseStringAttribute(
                                                distributionPointQueryHandle,
                                                NativeConstants.QUERY_NAME,
                                                0);

                            addressAttributeValue = GetUnboundLicenseStringAttribute(
                                                distributionPointQueryHandle,
                                                NativeConstants.QUERY_ADDRESSVALUE,
                                                0);
                            return;
                        }
                    }

                    index++;
                }
            }
        }


#if FALSE
        // not using this for now, as we are not binding successfully bound rights as a single list   
        internal static string BuildCommaSeparatedList(List<string> stringList)
        {
            StringBuilder concatenatedStringList  = new StringBuilder(stringList.Count * 10);//guess the average right name size 
    
            bool firstElementFlag = true;

            foreach (string right in stringList)
            {
                if (firstElementFlag)
                {
                    firstElementFlag = false; 
                }
                else
                {
                    concatenatedStringList .Append(',');                     
                }

                concatenatedStringList.Append(right); 
            }

            return concatenatedStringList.ToString();
        }
#endif

        internal static string GetSecurityProviderPath()
        {
            uint typeLength = 0;
            StringBuilder type = null;
            uint pathLength = 0;
            StringBuilder path = null;

            int hr = SafeNativeMethods.DRMGetSecurityProvider(0,
                                                        ref typeLength,
                                                        null,
                                                        ref pathLength,
                                                        null);
            Errors.ThrowOnErrorCode(hr);

            checked
            {
                type = new StringBuilder((int)typeLength);
                path = new StringBuilder((int)pathLength);
            }

            hr = SafeNativeMethods.DRMGetSecurityProvider(0,
                                                        ref typeLength,
                                                        type,
                                                        ref pathLength,
                                                        path);
            Errors.ThrowOnErrorCode(hr);

            return path.ToString();
        }

        internal static Nullable<ContentRight> GetRightFromString(string rightName)
        {
            rightName = rightName.ToString().ToUpper(CultureInfo.InvariantCulture);

            for (int i = 0; i < _rightEnums.Length; i++)
            {
                if (String.CompareOrdinal(_rightNames[i], rightName) == 0)
                {
                    return _rightEnums[i];
                }
            }

            return null;
        }


        internal static string GetStringFromRight(ContentRight right)
        {
            for (int i = 0; i < _rightEnums.Length; i++)
            {
                if (_rightEnums[i] == right)
                {
                    return _rightNames[i];
                }
            }

            throw new ArgumentOutOfRangeException("right");
        }

        private List<CryptoProvider> CryptoProviderList
        {
            get
            {
                if (_cryptoProviderList == null)
                {
                    _cryptoProviderList = new List<CryptoProvider>(5);
                }
                return _cryptoProviderList;
            }
        }

        /// <summary>
        /// Call this before accepting any API call 
        /// </summary>
        private void CheckDisposed()
        {
            if ((_hSession == null) ||
                (_hSession.IsInvalid))
                throw new ObjectDisposedException("SecureEnvironment");
        }

        private const string _defaultUserName = @"DefaultUser@DefaultDomain.DefaultCom";     // RM default user name

        private const string _distributionPointLicenseAcquisitionType = @"License-Acquisition-URL";
        private const string _distributionPointReferralInfoType = @"Referral-Info";

        private const string _passportActivationRegistryFullKeyName = @"HKEY_LOCAL_MACHINE\Software\Microsoft\MSDRM\ServiceLocation\PassportActivation";
        private const string _passportActivationRegistryKeyName = @"Software\Microsoft\MSDRM\ServiceLocation\PassportActivation";

        private ContentUser _user = null;
        private CallbackHandler _callbackHandler;

        private SafeRightsManagementSessionHandle _hSession = null; // if this is zero, we are disposed

        // we preserve this so ve can remove certificates in case of temp activation             
        UserActivationMode _userActivationMode = UserActivationMode.Permanent;

        private SafeRightsManagementEnvironmentHandle _envHandle = null;  // if this is null, we are disposed

        private SafeRightsManagementHandle _defaultLibraryHandle = null;

        private List<CryptoProvider> _cryptoProviderList;

        // the following 2 arrays are used for parsing and converting between String and Enum; 
        // therefore, the entries in the _rightEnums and the _rightNames must be in the same order. 
        static private ContentRight[] _rightEnums = {
                                        ContentRight.View,
                                        ContentRight.Edit,
                                        ContentRight.Print,
                                        ContentRight.Extract,
                                        ContentRight.ObjectModel,
                                        ContentRight.Owner,
                                        ContentRight.ViewRightsData, 
                                        ContentRight.Forward,
                                        ContentRight.Reply,
                                        ContentRight.ReplyAll,
                                        ContentRight.Sign,
                                        ContentRight.DocumentEdit,
                                        ContentRight.Export};

        // entries in this array must be in UPPERCASE, as we make such assumption during parsing                                         
        static private string[] _rightNames = {
                                        "VIEW",
                                        "EDIT",
                                        "PRINT",
                                        "EXTRACT",
                                        "OBJMODEL",
                                        "OWNER",
                                        "VIEWRIGHTSDATA", 
                                        "FORWARD",
                                        "REPLY",
                                        "REPLYALL",
                                        "SIGN",
                                        "DOCEDIT",
                                        "EXPORT"};
    }
}
