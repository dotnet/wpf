// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   This class wraps the issuance license publishing serveces 
//
//
//
//


using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security;
using System.Security.RightsManagement;
using SecurityHelper = MS.Internal.WindowsBase.SecurityHelper;
using System.Globalization;             // For CultureInfo
using MS.Internal;                      // for Invariant
using System.Windows;

namespace MS.Internal.Security.RightsManagement
{
    internal class IssuanceLicense : IDisposable
    {
        internal IssuanceLicense(
                                        DateTime validFrom,
                                        DateTime validUntil,
                                        string referralInfoName,
                                        Uri referralInfoUri,
                                        ContentUser owner,
                                        string issuanceLicense,
                                        SafeRightsManagementHandle boundLicenseHandle,
                                        Guid contentId,
                                        ICollection<ContentGrant> grantCollection,
                                        IDictionary<int, LocalizedNameDescriptionPair> localizedNameDescriptionDictionary,
                                        IDictionary<string, string> applicationSpecificDataDictionary,
                                        int rightValidityIntervalDays,
                                        RevocationPoint revocationPoint)
        {
            Initialize(
                validFrom,
                validUntil,
                referralInfoName,
                referralInfoUri,
                owner,
                issuanceLicense,
                boundLicenseHandle,
                contentId,
                grantCollection,
                localizedNameDescriptionDictionary,
                applicationSpecificDataDictionary,
                rightValidityIntervalDays,
                revocationPoint);
        }

        /// <summary>
        /// constructor that buils an issuance license from scratch
        /// </summary>
        private void Initialize(
                                        DateTime validFrom,
                                        DateTime validUntil,
                                        string referralInfoName,
                                        Uri referralInfoUri,
                                        ContentUser owner,
                                        string issuanceLicense,
                                        SafeRightsManagementHandle boundLicenseHandle,
                                        Guid contentId,
                                        ICollection<ContentGrant> grantCollection,
                                        IDictionary<int, LocalizedNameDescriptionPair> localizedNameDescriptionDictionary,
                                        IDictionary<string, string> applicationSpecificDataDictionary,
                                        int rightValidityIntervalDays,
                                        RevocationPoint revocationPoint)
        {
            // according to the unmanaged RM SDK spec only the following scenarios are supported:
            // 1. This can be called to create an issuance license from a template. 
            //       issuanceLicense         An unsigned issuance license from 
            //                                   a file or by passing an issuance license 
            //                                   handle into DRMGetIssuanceLicenseTemplate 
            //       boundLicenseHandle   NULL
            //
            // 2. This allows you to reuse rights information (the list follows this table).
            //       issuance license        A signed issuance license
            //       boundLicenseHandle   Handle to license bound by OWNER or VIEWRIGHTSDATA right
            //
            // 3. This creates an issuance license from scratch. It includes no users, rights, metadata, or policies.
            //       issuance license         NULL
            //       boundLicenseHandle   NULL

            Debug.Assert(!boundLicenseHandle.IsClosed); // it must be either present or not
            // closed handle is an indication of some internal error

            Invariant.Assert((boundLicenseHandle.IsInvalid) || (issuanceLicense != null));

            SystemTime validFromSysTime = null;
            SystemTime validUntilSysTime = null;

            if ((validFrom != DateTime.MinValue) || (validUntil != DateTime.MaxValue))
            {
                // we need to use non null values if at least one of the time boundaries isn't default
                // DRM SDK will not enforce date time unless both timeFrom and timeUnti are set 
                validFromSysTime = new SystemTime((DateTime)validFrom);
                validUntilSysTime = new SystemTime((DateTime)validUntil);
            }

            string referralInfoUriStr = null;
            if (referralInfoUri != null)
            {
                referralInfoUriStr = referralInfoUri.ToString();
            }

            // input parameter must be initialized to the invalid handle 
            // attempt to pass in a null throws an exception from the Safe 
            // Handle Marshalling code  
            SafeRightsManagementPubHandle ownerHandle;

            if (owner != null)
            {
                ownerHandle = GetHandleFromUser(owner);
            }
            else
            {
                ownerHandle = SafeRightsManagementPubHandle.InvalidHandle;
            }

            int hr;

            _issuanceLicenseHandle = null;

            hr = SafeNativeMethods.DRMCreateIssuanceLicense(
                validFromSysTime,
                validUntilSysTime,
                referralInfoName,
                referralInfoUriStr,
                ownerHandle,
                issuanceLicense,
                boundLicenseHandle,
                out _issuanceLicenseHandle);

            Errors.ThrowOnErrorCode(hr);
            Invariant.Assert((_issuanceLicenseHandle != null) &&
                                       (!_issuanceLicenseHandle.IsInvalid));

            Debug.Assert(rightValidityIntervalDays >= 0); // our internal code makes the guarantee that is is not negative
            if (rightValidityIntervalDays > 0)
            {
                // If it is 0 we shouldn't override the value as it might be coming from a template 
                SafeNativeMethods.DRMSetIntervalTime(_issuanceLicenseHandle, (uint)rightValidityIntervalDays);
            }

            if (grantCollection != null)
            {
                foreach (ContentGrant grant in grantCollection)
                {
                    AddGrant(grant);
                }
            }

            // Set localized name description info 
            if (localizedNameDescriptionDictionary != null)
            {
                foreach (KeyValuePair<int, LocalizedNameDescriptionPair> nameDescriptionEntry in localizedNameDescriptionDictionary)
                {
                    AddNameDescription(nameDescriptionEntry.Key, nameDescriptionEntry.Value);
                }
            }

            // Set application specific data 
            if (applicationSpecificDataDictionary != null)
            {
                foreach (KeyValuePair<string, string> applicationSpecificDataEntry in applicationSpecificDataDictionary)
                {
                    AddApplicationSpecificData(applicationSpecificDataEntry.Key, applicationSpecificDataEntry.Value);
                }
            }

            // set metadata as required 
            if (contentId != Guid.Empty)
            {
                hr = SafeNativeMethods.DRMSetMetaData(
                    _issuanceLicenseHandle,
                    contentId.ToString("B"),
                    DefaultContentType,
                    null,
                    null,
                    null,
                    null);

                Errors.ThrowOnErrorCode(hr);
            }

            // set revocation point if required 
            if (revocationPoint != null)
            {
                SetRevocationPoint(revocationPoint);
            }
        }

        internal SafeRightsManagementPubHandle Handle
        {
            get
            {
                CheckDisposed();
                return _issuanceLicenseHandle;
            }
        }

        override public string ToString()
        {
            uint issuanceLicenseTemplateLength = 0;
            StringBuilder issuanceLicenseTemplate = null;

            int hr = SafeNativeMethods.DRMGetIssuanceLicenseTemplate(
                _issuanceLicenseHandle,
                ref issuanceLicenseTemplateLength,
                null);

            Errors.ThrowOnErrorCode(hr);

            issuanceLicenseTemplate = new StringBuilder(checked((int)issuanceLicenseTemplateLength));

            hr = SafeNativeMethods.DRMGetIssuanceLicenseTemplate(
                _issuanceLicenseHandle,
                ref issuanceLicenseTemplateLength,
                issuanceLicenseTemplate);

            Errors.ThrowOnErrorCode(hr);

            return issuanceLicenseTemplate.ToString();
        }

        internal void UpdateUnsignedPublishLicense(UnsignedPublishLicense unsignedPublishLicense)
        {
            Invariant.Assert(unsignedPublishLicense != null);

            DateTime timeFrom;
            DateTime timeUntil;
            DistributionPointInfo distributionPointInfo = DistributionPointInfo.ReferralInfo;
            string distributionPointName;
            string distributionPointUri;
            ContentUser owner;
            bool officialFlag;

            GetIssuanceLicenseInfo(out timeFrom,
                                                out timeUntil,
                                                distributionPointInfo,
                                                out distributionPointName,
                                                out distributionPointUri,
                                                out owner,
                                                out officialFlag);

            unsignedPublishLicense.ReferralInfoName = distributionPointName;

            if (distributionPointUri != null)
            {
                unsignedPublishLicense.ReferralInfoUri = new Uri(distributionPointUri);
            }
            else
            {
                unsignedPublishLicense.ReferralInfoUri = null;
            }

            unsignedPublishLicense.Owner = owner;

            // Let's get the validity Iterval information (days) and save it in the license 
            uint validityDays = 0;
            int hr = SafeNativeMethods.DRMGetIntervalTime(_issuanceLicenseHandle, ref validityDays);
            Errors.ThrowOnErrorCode(hr);
            checked { unsignedPublishLicense.RightValidityIntervalDays = (int)validityDays; }

            // let's get the rights information 
            int userIndex = 0;
            while (true) // in this loop we are enumerating users mentioned in the license 
            {
                SafeRightsManagementPubHandle userHandle = null;

                // extract the user based on the index 
                ContentUser user = GetIssuanceLicenseUser(userIndex, out userHandle);

                if ((user == null) || (userHandle == null))
                {
                    break;
                }

                int rightIndex = 0;
                while (true) // now we can enumerate rights granted to the given user  
                {
                    SafeRightsManagementPubHandle rightHandle = null;
                    DateTime validFrom;
                    DateTime validUntil;

                    // extract the right based on the index and the user 
                    Nullable<ContentRight> right = GetIssuanceLicenseUserRight
                                (userHandle, rightIndex, out rightHandle, out validFrom, out validUntil);

                    // 0 right handle is an indication of the end of the list 
                    if (rightHandle == null)
                    {
                        break;
                    }

                    // right == null is an indication of a right that we didn't recognize 
                    // we should still continue the enumeration 
                    if (right != null)
                    {
                        // Add the grant for the User Right pair here 
                        unsignedPublishLicense.Grants.Add(
                                new ContentGrant(user, right.Value, validFrom, validUntil));
                    }

                    rightIndex++;
                }
                userIndex++;
            }

            // let's get the localized name description pairs 
            int nameIndex = 0;
            while (true) // in this loop we are enumerating nameDescription pairs mentioned in the license 
            {
                int localeId;

                // extract the user based on the index 
                LocalizedNameDescriptionPair nameDescription = GetLocalizedNameDescriptionPair(nameIndex,
                                    out localeId);
                if (nameDescription == null)
                {
                    break;
                }

                // Add the name description info to the license 
                unsignedPublishLicense.LocalizedNameDescriptionDictionary.Add(localeId, nameDescription);
                nameIndex++;
            }

            // let's get the application specific data 
            int appDataIndex = 0;
            while (true) // in this loop we are enumerating nameDescription pairs mentioned in the license 
            {
                // extract the user based on the index 

                Nullable<KeyValuePair<string, string>> appSpecificDataEntry = GetApplicationSpecificData(appDataIndex);

                if (appSpecificDataEntry == null)
                {
                    break;
                }

                // Add the name description info to the license 
                unsignedPublishLicense.ApplicationSpecificDataDictionary.Add(appSpecificDataEntry.Value.Key, appSpecificDataEntry.Value.Value);
                appDataIndex++;
            }

            // Get the revocation Point information, it is optional and can be null 
            unsignedPublishLicense.RevocationPoint = GetRevocationPoint();
        }

        ~IssuanceLicense()
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
                try
                {
                    if (_issuanceLicenseHandle != null)
                    {
                        _issuanceLicenseHandle.Dispose();

                        // After some investigation it was determined that ArrayList 
                        // is a safe member ,and could be used in Finalizers 
                        // so we do not check for disposing flag.  
                        // It seems that Finalizers are called in nondeterministic order 
                        // but if we have a combination of classes with Finalizers and without Finalizers 
                        // referencing each other, the classes with Finalizers wil be Disposed first.
                        foreach (SafeRightsManagementPubHandle handler in _pubHandlesList)
                        {
                            handler.Dispose();
                        }
                    }
                }
                finally
                {
                    _issuanceLicenseHandle = null;
                    _pubHandlesList.Clear();
                }
            }
        }

        private void AddGrant(ContentGrant grant)
        {
            Invariant.Assert(grant != null);
            Invariant.Assert(grant.User != null);

            SafeRightsManagementPubHandle right = GetRightHandle(grant);

            SafeRightsManagementPubHandle user = GetHandleFromUser(grant.User);

            int hr = SafeNativeMethods.DRMAddRightWithUser(
                _issuanceLicenseHandle,
                right,
                user);

            Errors.ThrowOnErrorCode(hr);
        }

        private void AddNameDescription(int localeId, LocalizedNameDescriptionPair nameDescription)
        {
            // the managed APIs treat Locale Id as int and the unmanaged ones as uint 
            // we need to convert it back and force
            uint locId;
            checked { locId = (uint)localeId; }

            int hr = SafeNativeMethods.DRMSetNameAndDescription(
                                    _issuanceLicenseHandle,
                                    false, // true - means delete ; false - add
                                    locId,
                                    nameDescription.Name,
                                    nameDescription.Description);
            Errors.ThrowOnErrorCode(hr);
        }

        private void AddApplicationSpecificData(string name, string value)
        {
            int hr = SafeNativeMethods.DRMSetApplicationSpecificData(
                                    _issuanceLicenseHandle,
                                    false, // true - means delete ; false - add
                                    name,
                                    value);
            Errors.ThrowOnErrorCode(hr);
        }

        private SafeRightsManagementPubHandle GetRightHandle(ContentGrant grant)
        {
            SafeRightsManagementPubHandle rightHandle = null;

            // we only need to use date time inteval if at least one of the values isn't default to Min max 
            // If both of the Min Max we can leave tyhem as nulls otherwise (if at least one not default)
            // we need to set both 
            SystemTime systemTimeValidFrom = null;
            SystemTime systemTimeValidUntil = null;

            if ((grant.ValidFrom != DateTime.MinValue) || (grant.ValidUntil != DateTime.MaxValue))
            {
                // we need to use non null values if at least one of the time boundaries isn't default
                // DRM SDK will not enforce date time unless both timeFrom and timeUnti are set 
                systemTimeValidFrom = new SystemTime(grant.ValidFrom);
                systemTimeValidUntil = new SystemTime(grant.ValidUntil);
            }

            int hr = SafeNativeMethods.DRMCreateRight(
                ClientSession.GetStringFromRight(grant.Right),
                systemTimeValidFrom,    // SystemTime timeFrom, 
                systemTimeValidUntil,    // SystemTime timeUntil,
                0,       // countExtendedInfo,    
                null,    // string [] extendedInfoNames,
                null,    // string [] extendedInfoValues,
                out rightHandle);

            Errors.ThrowOnErrorCode(hr);
            Debug.Assert((rightHandle != null) && (!rightHandle.IsInvalid));

            _pubHandlesList.Add(rightHandle);

            return rightHandle;
        }

        private SafeRightsManagementPubHandle GetHandleFromUser(ContentUser user)
        {
            SafeRightsManagementPubHandle userHandle = null;
            int hr;

            // We need to create Internal Authnetication type Users differently 
            if (user.GenericEquals(ContentUser.AnyoneUser) || user.GenericEquals(ContentUser.OwnerUser))
            {
                // Anyone user 
                hr = SafeNativeMethods.DRMCreateUser(
                    user.Name, // This is an optional UI Name (some applications use this and do not work well when it is missing)
                    user.Name, // that would be string Anyone or Owner 
                    ConvertAuthenticationTypeToString(user),   // this would be internal  
                    out userHandle);
            }
            else
            {
                // Windws Passport or WindowsPassport authentication type user 
                hr = SafeNativeMethods.DRMCreateUser(
                    user.Name,
                    null,
                    ConvertAuthenticationTypeToString(user),
                    out userHandle);
            }

            Errors.ThrowOnErrorCode(hr);
            Debug.Assert((userHandle != null) && (!userHandle.IsInvalid));

            _pubHandlesList.Add(userHandle);
            return userHandle;
        }

        static private Nullable<ContentRight> GetRightFromHandle(SafeRightsManagementPubHandle rightHandle,
                                                        out DateTime validFrom,
                                                        out DateTime validUntil)
        {
            uint rightNameLength = 0;
            StringBuilder rightName;

            int hr = SafeNativeMethods.DRMGetRightInfo(rightHandle,
                                                                                ref rightNameLength,
                                                                                null,
                                                                                null,
                                                                                null);
            Errors.ThrowOnErrorCode(hr);

            rightName = new StringBuilder(checked((int)rightNameLength));
            SystemTime validFromSysTime = new SystemTime(DateTime.Now);
            SystemTime validUntilSysTime = new SystemTime(DateTime.Now);

            hr = SafeNativeMethods.DRMGetRightInfo(rightHandle,
                                                                                ref rightNameLength,
                                                                                rightName,
                                                                                validFromSysTime,
                                                                                validUntilSysTime);
            Errors.ThrowOnErrorCode(hr);

            validFrom = validFromSysTime.GetDateTime(DateTime.MinValue);
            validUntil = validUntilSysTime.GetDateTime(DateTime.MaxValue);

            return ClientSession.GetRightFromString(rightName.ToString());
        }

        static private ContentUser GetUserFromHandle(SafeRightsManagementPubHandle userHandle)
        {
            uint userNameLength = 0;
            StringBuilder userName = null;
            uint userIdLength = 0;
            StringBuilder userId = null;
            uint userIdTypeLength = 0;
            StringBuilder userIdType = null;

            int hr = SafeNativeMethods.DRMGetUserInfo(userHandle,
                                                                                ref userNameLength,
                                                                                null,
                                                                                ref userIdLength,
                                                                                null,
                                                                                ref userIdTypeLength,
                                                                                null);
            Errors.ThrowOnErrorCode(hr);

            if (userNameLength > 0)
            {   // only allocate memory if we got a non-zero size back
                userName = new StringBuilder(checked((int)userNameLength));
            }

            if (userIdLength > 0)
            {   // only allocate memory if we got a non-zero size back
                userId = new StringBuilder(checked((int)userIdLength));
            }

            if (userIdTypeLength > 0)
            {   // only allocate memory if we got a non-zero size back
                userIdType = new StringBuilder(checked((int)userIdTypeLength));
            }

            hr = SafeNativeMethods.DRMGetUserInfo(userHandle,
                                                                                ref userNameLength,
                                                                                userName,
                                                                                ref userIdLength,
                                                                                userId,
                                                                                ref userIdTypeLength,
                                                                                userIdType);
            Errors.ThrowOnErrorCode(hr);

            // Convert String Builder values to string values 
            string userNameStr = null;
            if (userName != null)
            {
                userNameStr = userName.ToString();
            }

            string userIdTypeStr = null;
            if (userIdType != null)
            {
                userIdTypeStr = userIdType.ToString().ToUpperInvariant();
            }

            string userIdStr = null;
            if (userId != null)
            {
                userIdStr = userId.ToString().ToUpperInvariant();
            }

            // based on the UserTypeId build appropriate instance of User class 
            if (String.CompareOrdinal(userIdTypeStr, AuthenticationType.Windows.ToString().ToUpperInvariant()) == 0)
            {
                return new ContentUser(userNameStr, AuthenticationType.Windows);
            }
            else if (String.CompareOrdinal(userIdTypeStr, AuthenticationType.Passport.ToString().ToUpperInvariant()) == 0)
            {
                return new ContentUser(userNameStr, AuthenticationType.Passport);
            }
            else if (String.CompareOrdinal(userIdTypeStr, AuthenticationType.Internal.ToString().ToUpperInvariant()) == 0)
            {
                // internal anyone user 
                if (ContentUser.CompareToAnyone(userIdStr))
                {
                    return ContentUser.AnyoneUser;
                }
                else if (ContentUser.CompareToOwner(userIdStr))
                {
                    return ContentUser.OwnerUser;
                }
            }
            else if (String.CompareOrdinal(userIdTypeStr, UnspecifiedAuthenticationType.ToUpperInvariant()) == 0)
            {
                return new ContentUser(userNameStr, AuthenticationType.WindowsPassport);
            }

            throw new RightsManagementException(RightsManagementFailureCode.InvalidLicense);
        }

        private ContentUser GetIssuanceLicenseUser(int index, out SafeRightsManagementPubHandle userHandle)
        {
            Invariant.Assert(index >= 0);

            int hr = SafeNativeMethods.DRMGetUsers(
                _issuanceLicenseHandle, (uint)index, out userHandle);

            // there is a special code indication end of the enumeration 
            if (hr == (int)RightsManagementFailureCode.NoMoreData)
            {
                userHandle = null;
                return null;
            }

            // check for errors
            Errors.ThrowOnErrorCode(hr);
            Debug.Assert((userHandle != null) && (!userHandle.IsInvalid));

            // preserve handle so we can destruct it later 
            _pubHandlesList.Add(userHandle);

            // now we can build a ContentUser
            return GetUserFromHandle(userHandle);
        }

        private LocalizedNameDescriptionPair GetLocalizedNameDescriptionPair
                                                    (int index, out int localeId)
        {
            Invariant.Assert(index >= 0);
            uint locId = 0;
            uint nameLength = 0;
            StringBuilder name = null;
            uint descriptionLength = 0;
            StringBuilder description = null;

            int hr = SafeNativeMethods.DRMGetNameAndDescription(
                    _issuanceLicenseHandle,
                    (uint)index,
                    out locId,
                    ref nameLength,
                    name,
                    ref descriptionLength,
                    description);

            // there is a special code indication end of the enumeration 
            if (hr == (int)RightsManagementFailureCode.NoMoreData)
            {
                localeId = 0;
                return null;
            }

            // check for errors
            Errors.ThrowOnErrorCode(hr);

            if (nameLength > 0)
            {   // only allocate memory if we got a non-zero size back
                name = new StringBuilder(checked((int)nameLength));
            }

            if (descriptionLength > 0)
            {   // only allocate memory if we got a non-zero size back
                description = new StringBuilder(checked((int)descriptionLength));
            }

            hr = SafeNativeMethods.DRMGetNameAndDescription(
                    _issuanceLicenseHandle,
                    (uint)index,
                    out locId,
                    ref nameLength,
                    name,
                    ref descriptionLength,
                    description);
            Errors.ThrowOnErrorCode(hr);

            // the managed APIs treat Locale Id as int and the unmanaged ones as uint 
            // we need to convert it back and force
            checked { localeId = (int)locId; }

            // now we can build a ContentUser
            return new LocalizedNameDescriptionPair(name == null ? null : name.ToString(),
                                                                          description == null ? null : description.ToString());
        }

        private Nullable<KeyValuePair<string, string>> GetApplicationSpecificData(int index)
        {
            Invariant.Assert(index >= 0);
            uint nameLength = 0;
            uint valueLength = 0;

            // check whether element with such index is present, 
            // and if it is get the sizes of the name value strings 
            int hr = SafeNativeMethods.DRMGetApplicationSpecificData(
                                                _issuanceLicenseHandle,
                                                (uint)index,    // safe cast as the caller responsible for keeping this postive 
                                                ref nameLength,
                                                null,
                                                ref valueLength,
                                                null);

            // there is a special code indicating end of the enumeration 
            if (hr == (int)RightsManagementFailureCode.NoMoreData)
            {
                return null;
            }

            // check for errors
            Errors.ThrowOnErrorCode(hr);

            StringBuilder tempName = null;
            // allocate memory as necessary, it seems that Unmanaged libraries really do not like
            // getting a non null buffer of size 0 
            if (nameLength > 0)
            {
                tempName = new StringBuilder(checked((int)nameLength));
            }

            StringBuilder tempValue = null;
            // allocate memory as necessary, it seems that Unmanaged libraries really do not like
            // getting a non null buffer of size 0 
            if (valueLength > 0)
            {
                tempValue = new StringBuilder(checked((int)valueLength));
            }

            // The second call supposed to return the actual string values fcheck whether element with such index is present, 
            // and if it is get the sizes of the name value strings 
            hr = SafeNativeMethods.DRMGetApplicationSpecificData(
                                                _issuanceLicenseHandle,
                                                (uint)index,    // safe cast as the caller responsible for keeping this postive 
                                                ref nameLength,
                                                tempName,
                                                ref valueLength,
                                                tempValue);
            // check for errors
            Errors.ThrowOnErrorCode(hr);

            // build strings from the StringBuilder  instances 
            string name = (tempName == null) ? null : tempName.ToString();
            string value = (tempValue == null) ? null : tempValue.ToString();

            KeyValuePair<string, string> result = new KeyValuePair<string, string>(name, value);

            return result;
        }

        private Nullable<ContentRight> GetIssuanceLicenseUserRight
                                                    (SafeRightsManagementPubHandle userHandle,
                                                    int index,
                                                    out SafeRightsManagementPubHandle rightHandle,
                                                    out DateTime validFrom,
                                                    out DateTime validUntil)
        {
            Invariant.Assert(index >= 0);

            int hr = SafeNativeMethods.DRMGetUserRights(_issuanceLicenseHandle, userHandle, (uint)index, out rightHandle);

            // there is a special code indicating end of the enumeration 
            if (hr == (int)RightsManagementFailureCode.NoMoreData)
            {
                rightHandle = null;
                validFrom = DateTime.MinValue;
                validUntil = DateTime.MaxValue;

                return null;       // intentionally we return an invalid value, to make use client 
                // properly verify right handle 
            }

            // check for errors
            Errors.ThrowOnErrorCode(hr);
            Debug.Assert((rightHandle != null) && (!rightHandle.IsInvalid));

            // preserve handle so we can destruct it later 
            _pubHandlesList.Add(rightHandle);

            // now we can build a User
            return GetRightFromHandle(rightHandle, out validFrom, out validUntil);
        }

        private void GetIssuanceLicenseInfo(
                                                    out DateTime timeFrom,
                                                    out DateTime timeUntil,
                                                    DistributionPointInfo distributionPointInfo,
                                                    out string distributionPointName,
                                                    out string distributionPointUri,
                                                    out ContentUser owner,
                                                    out bool officialFlag)
        {
            uint distributionPointNameLength = 0;
            uint distributionPointUriLength = 0;
            bool officialFlagTemp = false;
            SafeRightsManagementPubHandle ownerHandleTemp = null;

            int hr = SafeNativeMethods.DRMGetIssuanceLicenseInfo(
                                                                        _issuanceLicenseHandle,
                                                                        null,
                                                                        null,
                                                                        (uint)distributionPointInfo,
                                                                        ref distributionPointNameLength,
                                                                        null,
                                                                        ref distributionPointUriLength,
                                                                        null,
                                                                        out ownerHandleTemp,
                                                                        out officialFlagTemp);
            Errors.ThrowOnErrorCode(hr);

            if (ownerHandleTemp != null)
            {
                // As a result of calling DRMGetIssuanceLicenseInfo twice,
                // we are getting 2 handles. We are going to dispose the first one 
                // and preserve the second one.
                ownerHandleTemp.Dispose();
                ownerHandleTemp = null;
            }

            StringBuilder distributionPointNameTemp = null;
            // allocate memory as necessary, it seems that Unmanaged libraries really do not like
            // getting a non null buffer of size 0 
            if (distributionPointNameLength > 0)
            {
                distributionPointNameTemp = new StringBuilder(checked((int)distributionPointNameLength));
            }

            StringBuilder distributionPointUriTemp = null;
            // allocate memory as necessary, it seems that Unmanaged libraries really do not like
            // getting a non null buffer of size 0 
            if (distributionPointUriLength > 0)
            {
                distributionPointUriTemp = new StringBuilder(checked((int)distributionPointUriLength));
            }

            SystemTime timeFromTemp = new SystemTime(DateTime.Now);
            SystemTime timeUntilTemp = new SystemTime(DateTime.Now);

            hr = SafeNativeMethods.DRMGetIssuanceLicenseInfo(
                                                                        _issuanceLicenseHandle,
                                                                        timeFromTemp,
                                                                        timeUntilTemp,
                                                                        (uint)distributionPointInfo,
                                                                        ref distributionPointNameLength,
                                                                        distributionPointNameTemp,
                                                                        ref distributionPointUriLength,
                                                                        distributionPointUriTemp,
                                                                        out ownerHandleTemp,
                                                                        out officialFlagTemp);
            Errors.ThrowOnErrorCode(hr);

            timeFrom = timeFromTemp.GetDateTime(DateTime.MinValue);
            timeUntil = timeUntilTemp.GetDateTime(DateTime.MaxValue);

            // only if we got some data back we shall try to process it 
            if (distributionPointNameTemp != null)
            {
                distributionPointName = distributionPointNameTemp.ToString();
            }
            else
            {
                distributionPointName = null;
            }

            // only if we got some data back we shall try to process it 
            if (distributionPointUriTemp != null)
            {
                distributionPointUri = distributionPointUriTemp.ToString();
            }
            else
            {
                distributionPointUri = null;
            }

            // if we have owner let's convert it to a user and preserve 
            // handler for further destruction
            owner = null;
            if (ownerHandleTemp != null)
            {
                _pubHandlesList.Add(ownerHandleTemp);

                if (!ownerHandleTemp.IsInvalid)
                {
                    owner = GetUserFromHandle(ownerHandleTemp);
                }
            }

            officialFlag = officialFlagTemp;
        }

        private void SetRevocationPoint(RevocationPoint revocationPoint)
        {
            int hr = SafeNativeMethods.DRMSetRevocationPoint(
                                _issuanceLicenseHandle,
                                false, // flagDelete,
                                revocationPoint.Id,
                                revocationPoint.IdType,
                                revocationPoint.Url.AbsoluteUri, // We are using Uri class as a basic validation mechanism. These URIs come from unmanaged 
                // code libraries and go back as parameters into the unmanaged code libraries. 
                // We use AbsoluteUri property as means of verifying that it is actually an absolute and 
                // well formed Uri. If by any chance it happened to be a relative URI, an exception will 
                // be thrown here. This will perform the necessary escaping.
                                revocationPoint.Frequency,
                                revocationPoint.Name,
                                revocationPoint.PublicKey);

            Errors.ThrowOnErrorCode(hr);
        }

        private RevocationPoint GetRevocationPoint()
        {
            uint idLength = 0;
            uint idTypeLength = 0;
            uint urlLength = 0;
            uint nameLength = 0;
            uint publicKeyLength = 0;
            SystemTime frequency = new SystemTime(DateTime.Now);

            int hr = SafeNativeMethods.DRMGetRevocationPoint(
                                _issuanceLicenseHandle,
                                ref idLength,
                                null,
                                ref idTypeLength,
                                null,
                                ref urlLength,
                                null,
                                frequency,
                                ref nameLength,
                                null,
                                ref publicKeyLength,
                                null);
            if (hr == (int)RightsManagementFailureCode.RevocationInfoNotSet)
            {
                return null;
            }

            Errors.ThrowOnErrorCode(hr);

            // allocate memory as necessary, it seems that Unmanaged libraries really do not like
            // getting a non null buffer of size 0 
            StringBuilder idTemp = null;
            if (idLength > 0)
            {
                idTemp = new StringBuilder(checked((int)idLength));
            }

            StringBuilder idTypeTemp = null;
            if (idTypeLength > 0)
            {
                idTypeTemp = new StringBuilder(checked((int)idTypeLength));
            }

            StringBuilder urlTemp = null;
            if (urlLength > 0)
            {
                urlTemp = new StringBuilder(checked((int)urlLength));
            }

            StringBuilder nameTemp = null;
            if (nameLength > 0)
            {
                nameTemp = new StringBuilder(checked((int)nameLength));
            }

            StringBuilder publicKeyTemp = null;
            if (publicKeyLength > 0)
            {
                publicKeyTemp = new StringBuilder(checked((int)publicKeyLength));
            }

            hr = SafeNativeMethods.DRMGetRevocationPoint(
                                _issuanceLicenseHandle,
                                ref idLength,
                                idTemp,
                                ref idTypeLength,
                                idTypeTemp,
                                ref urlLength,
                                urlTemp,
                                frequency,
                                ref nameLength,
                                nameTemp,
                                ref publicKeyLength,
                                publicKeyTemp);
            Errors.ThrowOnErrorCode(hr);

            RevocationPoint resultRevocationPoint = new RevocationPoint();

            resultRevocationPoint.Id = (idTemp == null) ? null : idTemp.ToString();
            resultRevocationPoint.IdType = (idTypeTemp == null) ? null : idTypeTemp.ToString();
            resultRevocationPoint.Url = (urlTemp == null) ? null : new Uri(urlTemp.ToString());
            resultRevocationPoint.Name = (nameTemp == null) ? null : nameTemp.ToString();
            resultRevocationPoint.PublicKey = (publicKeyTemp == null) ? null : publicKeyTemp.ToString();
            resultRevocationPoint.Frequency = frequency;

            return resultRevocationPoint;
        }

        /// <summary>
        /// Call this before accepting any API call 
        /// </summary>
        private void CheckDisposed()
        {
            //As this class is not public, and the corresponding public class (Unsigned Publish License 
            //that uses this class is not disposable, it means, that the only probable reason for using 
            //Disposed instance of this class is a 


            Invariant.Assert((_issuanceLicenseHandle != null) &&
                                       (!_issuanceLicenseHandle.IsInvalid));
        }

        /// <summary>
        /// Converts Authentication type enumeration into a string that can be accepted by the unmanaged code
        /// </summary>
        private string ConvertAuthenticationTypeToString(ContentUser user)
        {
            if (user.AuthenticationType == AuthenticationType.WindowsPassport)
            {
                return UnspecifiedAuthenticationType;
            }
            else
            {
                return user.AuthenticationType.ToString();
            }
        }


        private List<SafeRightsManagementPubHandle> _pubHandlesList = new List<SafeRightsManagementPubHandle>(50); // initial capacity 
        private SafeRightsManagementPubHandle _issuanceLicenseHandle = null; // if this is null, we are disposed

        private const string DefaultContentType = "MS-GUID";

        private const string UnspecifiedAuthenticationType = "Unspecified";
    }
}
