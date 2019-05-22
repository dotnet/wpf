// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   This is partial class declaration of the SafeNativeMethods
//   specifically this file contains the internal wrappers for the private pinvoke calls 
//   declared in UnsafeNativeMethods.cs
//
//
//
//

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Security;
using System.Security.Permissions;
using MS.Internal.Permissions;
using SecurityHelper = MS.Internal.WindowsBase.SecurityHelper;

namespace MS.Internal.Security.RightsManagement
{
    /// <SecurityNote>
    ///     Critical:  This class server as a wrapper on top of private class UnsafeNativeMethods.
    ///     UnsafeNativeMethods has suppress unamanged code attribute set. 
    ///     It is up to this class to ensure that the only calls that can go through must beeither done in Full Trust 
    ///     or with RightsManagementPermission. This class exposes DRMFoo functions that perform demand on the 
    ///     RightsManagementPermission and then call through to the matching member of the Private Static class 
    ///     UnsafeNativeMethods
    /// </SecurityNote>
    [SecurityCritical(SecurityCriticalScope.Everything)]
    internal static partial class SafeNativeMethods
    {
        internal static int DRMCreateClientSession(
                                CallbackDelegate pfnCallback,
                                 uint uCallbackVersion,
                                 string GroupIDProviderType,
                                 string GroupID,
                                 out SafeRightsManagementSessionHandle phSession)
        {
            SecurityHelper.DemandRightsManagementPermission();
            int res = UnsafeNativeMethods.DRMCreateClientSession(
                                pfnCallback,
                                uCallbackVersion,
                                GroupIDProviderType,
                                GroupID,
                                out phSession);

            // on some platforms in the failure cases the out parameter is being created with the value 0
            // in order to simplify error handling and Disposing of those handles we will just close them as 
            // soon as we detect such case  
            if ((phSession != null) && phSession.IsInvalid)
            {
                phSession.Dispose();
                phSession = null;
            }
            return res;
        }

        // we can not use safe handle in the DrmClose... function
        // as the SafeHandle implementation marks this instance as an invalid by the time 
        // ReleaseHandle is called. After that marshalling code doesn't let the current instance 
        // of the Safe*Handle sub-class to cross managed/unmanaged boundary.
        internal static int DRMCloseSession(
                                uint sessionHandle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMCloseSession(
                                sessionHandle);
        }

        // we can not use safe handle in the DrmClose... function
        // as the SafeHandle implementation marks this instance as an invalid by the time 
        // ReleaseHandle is called. After that marshalling code doesn't let the current instance 
        // of the Safe*Handle sub-class to cross managed/unmanaged boundary.
        internal static int DRMCloseHandle(
                                 uint handle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMCloseHandle(
                                handle);
        }

        // we can not use safe handle in the DrmClose... function
        // as the SafeHandle implementation marks this instance as an invalid by the time 
        // ReleaseHandle is called. After that marshalling code doesn't let the current instance 
        // of the Safe*Handle sub-class to cross managed/unmanaged boundary.
        internal static int DRMCloseQueryHandle(
                                 uint queryHandle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMCloseQueryHandle(
                                queryHandle);
        }

        // we can not use safe handle in the DrmClose... function
        // as the SafeHandle implementation marks this instance as an invalid by the time 
        // ReleaseHandle is called. After that marshalling code doesn't let the current instance 
        // of the Safe*Handle sub-class to cross managed/unmanaged boundary.
        internal static int DRMCloseEnvironmentHandle(
                                 uint envHandle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMCloseEnvironmentHandle(
                                envHandle);
        }

        internal static int DRMInitEnvironment(
                                 uint eSecurityProviderType,
                                 uint eSpecification,
                                 string securityProvider,
                                 string manifestCredentials,
                                 string machineCredentials,
                                 out SafeRightsManagementEnvironmentHandle environmentHandle,
                                 out SafeRightsManagementHandle defaultLibrary)
        {
            SecurityHelper.DemandRightsManagementPermission();
            int res = UnsafeNativeMethods.DRMInitEnvironment(
                                eSecurityProviderType,
                                eSpecification,
                                securityProvider,
                                manifestCredentials,
                                machineCredentials,
                                out environmentHandle,
                                out defaultLibrary);

            // on some platforms in the failure cases the out parameter is being created with the value 0
            // in order to simplify error handling and Disposing of those handles we will just close them as 
            // soon as we detect such case  
            if ((environmentHandle != null) && environmentHandle.IsInvalid)
            {
                environmentHandle.Dispose();
                environmentHandle = null;
            }
            if ((defaultLibrary != null) && defaultLibrary.IsInvalid)
            {
                defaultLibrary.Dispose();
                defaultLibrary = null;
            }

            return res;
        }

        internal static int DRMIsActivated(
                                 SafeRightsManagementSessionHandle hSession,
                                 uint uFlags,
                                 ActivationServerInfo activationServerInfo)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMIsActivated(
                                hSession,
                                uFlags,
                                activationServerInfo);
        }

        internal static int DRMActivate(
                                SafeRightsManagementSessionHandle hSession,
                                uint uFlags,
                                uint uLangID,
                                ActivationServerInfo activationServerInfo,
                                IntPtr context,
                                IntPtr parentWindowHandle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMActivate(
                                hSession,
                                uFlags,
                                uLangID,
                                activationServerInfo,
                                context,
                                parentWindowHandle);
        }

        internal static int DRMCreateLicenseStorageSession(
                                SafeRightsManagementEnvironmentHandle hEnv,
                                SafeRightsManagementHandle hDefLib,
                                SafeRightsManagementSessionHandle hClientSession,
                                uint uFlags,
                                string IssuanceLicense,
                                out SafeRightsManagementSessionHandle phLicenseStorageSession)
        {
            SecurityHelper.DemandRightsManagementPermission();
            int res = UnsafeNativeMethods.DRMCreateLicenseStorageSession(
                                hEnv,
                                hDefLib,
                                hClientSession,
                                uFlags,
                                IssuanceLicense,
                                out phLicenseStorageSession);

            // on some platforms in the failure cases the out parameter is being created with the value 0
            // in order to simplify error handling and Disposing of those handles we will just close them as 
            // soon as we detect such case  
            if ((phLicenseStorageSession != null) && phLicenseStorageSession.IsInvalid)
            {
                phLicenseStorageSession.Dispose();
                phLicenseStorageSession = null;
            }
            return res;
        }

        internal static int DRMAcquireLicense(
                                 SafeRightsManagementSessionHandle hSession,
                                 uint uFlags,
                                 string GroupIdentityCredential,
                                 string RequestedRights,
                                 string CustomData,
                                 string url,
                                IntPtr context)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMAcquireLicense(
                                hSession,
                                uFlags,
                                GroupIdentityCredential,
                                RequestedRights,
                                CustomData,
                                url,
                                context);
        }

        internal static int DRMEnumerateLicense(
                                 SafeRightsManagementSessionHandle hSession,
                                 uint uFlags,
                                 uint uIndex,
                                 ref bool pfSharedFlag,
                                 ref uint puCertDataLen,
                                 StringBuilder wszCertificateData)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMEnumerateLicense(
                                hSession,
                                uFlags,
                                uIndex,
                                ref pfSharedFlag,
                                ref puCertDataLen,
                                wszCertificateData);
        }

        internal static int DRMGetServiceLocation(
                                 SafeRightsManagementSessionHandle clientSessionHandle,
                                 uint serviceType,
                                 uint serviceLocation,
                                 string issuanceLicense,
                                 ref uint serviceUrlLength,
                                 StringBuilder serviceUrl)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMGetServiceLocation(
                                clientSessionHandle,
                                serviceType,
                                serviceLocation,
                                issuanceLicense,
                                ref serviceUrlLength,
                                serviceUrl);
        }

        internal static int DRMDeconstructCertificateChain(
                                 string chain,
                                 uint index,
                                 ref uint certificateLength,
                                 StringBuilder certificate)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMDeconstructCertificateChain(
                                chain,
                                index,
                                ref certificateLength,
                                certificate);
        }

        internal static int DRMParseUnboundLicense(
                                 string certificate,
                                 out SafeRightsManagementQueryHandle queryRootHandle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            int res = UnsafeNativeMethods.DRMParseUnboundLicense(
                                certificate,
                                out queryRootHandle);

            // on some platforms in the failure cases the out parameter is being created with the value 0
            // in order to simplify error handling and Disposing of those handles we will just close them as 
            // soon as we detect such case  
            if ((queryRootHandle != null) && queryRootHandle.IsInvalid)
            {
                queryRootHandle.Dispose();
                queryRootHandle = null;
            }
            return res;
        }

        internal static int DRMGetUnboundLicenseObjectCount(
                                 SafeRightsManagementQueryHandle queryRootHandle,
                                 string subObjectType,
                                 out uint objectCount)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMGetUnboundLicenseObjectCount(
                                queryRootHandle,
                                subObjectType,
                                out objectCount);
        }

        internal static int DRMGetBoundLicenseObject(
                                 SafeRightsManagementHandle queryRootHandle,
                                 string subObjectType,
                                 uint index,
                                 out SafeRightsManagementHandle subQueryHandle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            int res = UnsafeNativeMethods.DRMGetBoundLicenseObject(
                                queryRootHandle,
                                subObjectType,
                                index,
                                out subQueryHandle);

            // on some platforms in the failure cases the out parameter is being created with the value 0
            // in order to simplify error handling and Disposing of those handles we will just close them as 
            // soon as we detect such case  
            if ((subQueryHandle != null) && subQueryHandle.IsInvalid)
            {
                subQueryHandle.Dispose();
                subQueryHandle = null;
            }
            return res;
        }

        internal static int DRMGetUnboundLicenseObject(
                                 SafeRightsManagementQueryHandle queryRootHandle,
                                 string subObjectType,
                                 uint index,
                                 out SafeRightsManagementQueryHandle subQueryHandle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            int res = UnsafeNativeMethods.DRMGetUnboundLicenseObject(
                                queryRootHandle,
                                subObjectType,
                                index,
                                out subQueryHandle);

            // on some platforms in the failure cases the out parameter is being created with the value 0
            // in order to simplify error handling and Disposing of those handles we will just close them as 
            // soon as we detect such case  
            if ((subQueryHandle != null) && subQueryHandle.IsInvalid)
            {
                subQueryHandle.Dispose();
                subQueryHandle = null;
            }
            return res;
        }

        internal static int DRMGetUnboundLicenseAttribute(
                                 SafeRightsManagementQueryHandle queryRootHandle,
                                 string attributeType,
                                 uint index,
                                 out uint encodingType,
                                 ref uint bufferSize,
                                 byte[] buffer)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMGetUnboundLicenseAttribute(
                                queryRootHandle,
                                attributeType,
                                index,
                                out encodingType,
                                ref bufferSize,
                                buffer);
        }

        internal static int DRMGetBoundLicenseAttribute(
                                 SafeRightsManagementHandle queryRootHandle,
                                 string attributeType,
                                 uint index,
                                 out uint encodingType,
                                 ref uint bufferSize,
                                 byte[] buffer)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMGetBoundLicenseAttribute(
                                queryRootHandle,
                                attributeType,
                                index,
                                out encodingType,
                                ref bufferSize,
                                buffer);
        }

        internal static int DRMCreateIssuanceLicense(
                                 SystemTime timeFrom,
                                 SystemTime timeUntil,
                                 string referralInfoName,
                                 string referralInfoUrl,
                                 SafeRightsManagementPubHandle ownerUserHandle,
                                 string issuanceLicense,
                                 SafeRightsManagementHandle boundLicenseHandle,
                                 out SafeRightsManagementPubHandle issuanceLicenseHandle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            int res = UnsafeNativeMethods.DRMCreateIssuanceLicense(
                                timeFrom,
                                timeUntil,
                                referralInfoName,
                                referralInfoUrl,
                                ownerUserHandle,
                                issuanceLicense,
                                boundLicenseHandle,
                                out issuanceLicenseHandle);

            // on some platforms in the failure cases the out parameter is being created with the value 0
            // in order to simplify error handling and Disposing of those handles we will just close them as 
            // soon as we detect such case  
            if ((issuanceLicenseHandle != null) && issuanceLicenseHandle.IsInvalid)
            {
                issuanceLicenseHandle.Dispose();
                issuanceLicenseHandle = null;
            }
            return res;
        }

        internal static int DRMCreateUser(
                                 string userName,
                                 string userId,
                                 string userIdType,
                                 out SafeRightsManagementPubHandle userHandle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            int res = UnsafeNativeMethods.DRMCreateUser(
                                userName,
                                userId,
                                userIdType,
                                out userHandle);

            // on some platforms in the failure cases the out parameter is being created with the value 0
            // in order to simplify error handling and Disposing of those handles we will just close them as 
            // soon as we detect such case  
            if ((userHandle != null) && userHandle.IsInvalid)
            {
                userHandle.Dispose();
                userHandle = null;
            }
            return res;
        }

        internal static int DRMGetUsers(
                                 SafeRightsManagementPubHandle issuanceLicenseHandle,
                                 uint index,
                                 out SafeRightsManagementPubHandle userHandle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            int res = UnsafeNativeMethods.DRMGetUsers(
                                issuanceLicenseHandle,
                                index,
                                out userHandle);

            // on some platforms in the failure cases the out parameter is being created with the value 0
            // in order to simplify error handling and Disposing of those handles we will just close them as 
            // soon as we detect such case  
            if ((userHandle != null) && userHandle.IsInvalid)
            {
                userHandle.Dispose();
                userHandle = null;
            }
            return res;
        }

        internal static int DRMGetUserRights(
                                 SafeRightsManagementPubHandle issuanceLicenseHandle,
                                 SafeRightsManagementPubHandle userHandle,
                                 uint index,
                                 out SafeRightsManagementPubHandle rightHandle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            int res = UnsafeNativeMethods.DRMGetUserRights(
                                issuanceLicenseHandle,
                                userHandle,
                                index,
                                out rightHandle);

            // on some platforms in the failure cases the out parameter is being created with the value 0
            // in order to simplify error handling and Disposing of those handles we will just close them as 
            // soon as we detect such case  
            if ((rightHandle != null) && rightHandle.IsInvalid)
            {
                rightHandle.Dispose();
                rightHandle = null;
            }
            return res;
        }

        internal static int DRMGetUserInfo(
                                 SafeRightsManagementPubHandle userHandle,
                                 ref uint userNameLength,
                                 StringBuilder userName,
                                 ref uint userIdLength,
                                 StringBuilder userId,
                                 ref uint userIdTypeLength,
                                 StringBuilder userIdType)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMGetUserInfo(
                                userHandle,
                                ref userNameLength,
                                userName,
                                ref userIdLength,
                                userId,
                                ref userIdTypeLength,
                                userIdType);
        }

        internal static int DRMGetRightInfo(
                                 SafeRightsManagementPubHandle rightHandle,
                                 ref uint rightNameLength,
                                 StringBuilder rightName,
                                 SystemTime timeFrom,
                                 SystemTime timeUntil)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMGetRightInfo(
                                rightHandle,
                                ref rightNameLength,
                                rightName,
                                timeFrom,
                                timeUntil);
        }

        internal static int DRMCreateRight(
                                 string rightName,
                                 SystemTime timeFrom,
                                 SystemTime timeUntil,
                                 uint countExtendedInfo,
                                 string[] extendedInfoNames,
                                 string[] extendedInfoValues,
                                 out SafeRightsManagementPubHandle rightHandle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            int res = UnsafeNativeMethods.DRMCreateRight(
                                rightName,
                                timeFrom,
                                timeUntil,
                                countExtendedInfo,
                                extendedInfoNames,
                                extendedInfoValues,
                                out rightHandle);
            // on some platforms in the failure cases the out parameter is being created with the value 0
            // in order to simplify error handling and Disposing of those handles we will just close them as 
            // soon as we detect such case  
            if ((rightHandle != null) && rightHandle.IsInvalid)
            {
                rightHandle.Dispose();
                rightHandle = null;
            }
            return res;
        }

        internal static int DRMGetIssuanceLicenseTemplate(
                                 SafeRightsManagementPubHandle issuanceLicenseHandle,
                                 ref uint issuanceLicenseTemplateLength,
                                 StringBuilder issuanceLicenseTemplate)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMGetIssuanceLicenseTemplate(
                                issuanceLicenseHandle,
                                ref issuanceLicenseTemplateLength,
                                issuanceLicenseTemplate);
        }

        // we can not use safe handle in the DrmClose... function
        // as the SafeHandle implementation marks this instance as an invalid by the time 
        // ReleaseHandle is called. After that marshalling code doesn't let the current instance 
        // of the Safe*Handle sub-class to cross managed/unmanaged boundary.
        internal static int DRMClosePubHandle(
                                 uint pubHandle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMClosePubHandle(
                                pubHandle);
        }

        internal static int DRMAddRightWithUser(
                                 SafeRightsManagementPubHandle issuanceLicenseHandle,
                                 SafeRightsManagementPubHandle rightHandle,
                                 SafeRightsManagementPubHandle userHandle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMAddRightWithUser(
                                issuanceLicenseHandle,
                                rightHandle,
                                userHandle);
        }

        internal static int DRMSetMetaData(
                                 SafeRightsManagementPubHandle issuanceLicenseHandle,
                                 string contentId,
                                 string contentIdType,
                                 string SkuId,
                                 string SkuIdType,
                                 string contentType,
                                 string contentName)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMSetMetaData(
                                issuanceLicenseHandle,
                                contentId,
                                contentIdType,
                                SkuId,
                                SkuIdType,
                                contentType,
                                contentName);
        }

        internal static int DRMGetIssuanceLicenseInfo(
                                 SafeRightsManagementPubHandle issuanceLicenseHandle,
                                 SystemTime timeFrom,
                                 SystemTime timeUntil,
                                 uint flags,
                                 ref uint distributionPointNameLength,
                                 StringBuilder DistributionPointName,
                                 ref uint distributionPointUriLength,
                                 StringBuilder DistributionPointUri,
                                 out SafeRightsManagementPubHandle ownerHandle,
                                 out bool officialFlag)
        {
            SecurityHelper.DemandRightsManagementPermission();
            int res = UnsafeNativeMethods.DRMGetIssuanceLicenseInfo(
                                issuanceLicenseHandle,
                                timeFrom,
                                timeUntil,
                                flags,
                                ref distributionPointNameLength,
                                DistributionPointName,
                                ref distributionPointUriLength,
                                DistributionPointUri,
                                out ownerHandle,
                                out officialFlag);
            // on some platforms in the failure cases the out parameter is being created with the value 0
            // in order to simplify error handling and Disposing of those handles we will just close them as 
            // soon as we detect such case  
            if ((ownerHandle != null) && ownerHandle.IsInvalid)
            {
                ownerHandle.Dispose();
                ownerHandle = null;
            }
            return res;
        }

        internal static int DRMGetSecurityProvider(
                                 uint flags,  // currently not used by the DRM SDK      
                                 ref uint typeLength,
                                 StringBuilder type,
                                 ref uint pathLength,
                                 StringBuilder path)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMGetSecurityProvider(
                                flags,
                                ref typeLength,
                                type,
                                ref pathLength,
                                path);
        }

        internal static int DRMDeleteLicense(
                                 SafeRightsManagementSessionHandle hSession,
                                 string wszLicenseId)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMDeleteLicense(
                                hSession,
                                wszLicenseId);
        }

        internal static int DRMSetNameAndDescription(
                                    SafeRightsManagementPubHandle issuanceLicenseHandle,
                                    bool flagDelete,
                                    uint localeId,
                                    string name,
                                    string description)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMSetNameAndDescription(
                                 issuanceLicenseHandle,
                                 flagDelete,
                                 localeId,
                                 name,
                                 description);
        }


        internal static int DRMGetNameAndDescription(
                                    SafeRightsManagementPubHandle issuanceLicenseHandle,
                                    uint uIndex,
                                    out uint localeId,
                                    ref uint nameLength,
                                    StringBuilder name,
                                    ref uint descriptionLength,
                                    StringBuilder description)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMGetNameAndDescription(
                                    issuanceLicenseHandle,
                                    uIndex,
                                    out localeId,
                                    ref nameLength,
                                    name,
                                    ref descriptionLength,
                                    description);
        }

        internal static int DRMGetSignedIssuanceLicense(
                                 SafeRightsManagementEnvironmentHandle environmentHandle,
                                 SafeRightsManagementPubHandle issuanceLicenseHandle,
                                 uint flags,
                                 byte[] symmetricKey,
                                 uint symmetricKeyByteCount,
                                 string symmetricKeyType,
                                 string clientLicensorCertificate,
                                 CallbackDelegate pfnCallback,
                                 string Url,
                                 uint context)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMGetSignedIssuanceLicense(
                                environmentHandle,
                                issuanceLicenseHandle,
                                flags,
                                symmetricKey,
                                symmetricKeyByteCount,
                                symmetricKeyType,
                                clientLicensorCertificate,
                                pfnCallback,
                                Url,
                                context);
        }

        internal static int DRMGetOwnerLicense(
                                 SafeRightsManagementPubHandle issuanceLicenseHandle,
                                 ref uint ownerLicenseLength,
                                 StringBuilder ownerLicense)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMGetOwnerLicense(
                                issuanceLicenseHandle,
                                ref ownerLicenseLength,
                                ownerLicense);
        }

        internal static int DRMCreateBoundLicense(
                                 SafeRightsManagementEnvironmentHandle environmentHandle,
                                 BoundLicenseParams boundLicenseParams,
                                 string licenseChain,
                                 out SafeRightsManagementHandle boundLicenseHandle,
                                 out uint errorLogHandle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            int res = UnsafeNativeMethods.DRMCreateBoundLicense(
                                environmentHandle,
                                boundLicenseParams,
                                licenseChain,
                                out boundLicenseHandle,
                                out errorLogHandle);
            // on some platforms in the failure cases the out parameter is being created with the value 0
            // in order to simplify error handling and Disposing of those handles we will just close them as 
            // soon as we detect such case  
            if ((boundLicenseHandle != null) && boundLicenseHandle.IsInvalid)
            {
                boundLicenseHandle.Dispose();
                boundLicenseHandle = null;
            }
            return res;
}

        internal static int DRMCreateEnablingBitsDecryptor(
                                 SafeRightsManagementHandle boundLicenseHandle,
                                 string right,
                                 uint auxLibrary,
                                 string auxPlugin,
                                 out SafeRightsManagementHandle decryptorHandle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            int res = UnsafeNativeMethods.DRMCreateEnablingBitsDecryptor(
                                boundLicenseHandle,
                                right,
                                auxLibrary,
                                auxPlugin,
                                out decryptorHandle);
            // on some platforms in the failure cases the out parameter is being created with the value 0
            // in order to simplify error handling and Disposing of those handles we will just close them as 
            // soon as we detect such case  
            if ((decryptorHandle != null) && decryptorHandle.IsInvalid)
            {
                decryptorHandle.Dispose();
                decryptorHandle = null;
            }
            return res;
        }

        internal static int DRMCreateEnablingBitsEncryptor(
                                 SafeRightsManagementHandle boundLicenseHandle,
                                 string right,
                                 uint auxLibrary,
                                 string auxPlugin,
                                 out SafeRightsManagementHandle encryptorHandle)
        {
            SecurityHelper.DemandRightsManagementPermission();
            int res = UnsafeNativeMethods.DRMCreateEnablingBitsEncryptor(
                                boundLicenseHandle,
                                right,
                                auxLibrary,
                                auxPlugin,
                                out encryptorHandle);
            // on some platforms in the failure cases the out parameter is being created with the value 0
            // in order to simplify error handling and Disposing of those handles we will just close them as 
            // soon as we detect such case  
            if ((encryptorHandle != null) && encryptorHandle.IsInvalid)
            {
                encryptorHandle.Dispose();
                encryptorHandle = null;
            }
            return res;
        }

        internal static int DRMDecrypt(
                                 SafeRightsManagementHandle cryptoProvHandle,
                                 uint position,
                                 uint inputByteCount,
                                 byte[] inputBuffer,
                                 ref uint outputByteCount,
                                 byte[] outputBuffer)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMDecrypt(
                                cryptoProvHandle,
                                position,
                                inputByteCount,
                                inputBuffer,
                                ref outputByteCount,
                                outputBuffer);
        }

        internal static int DRMEncrypt(
                                 SafeRightsManagementHandle cryptoProvHandle,
                                 uint position,
                                 uint inputByteCount,
                                 byte[] inputBuffer,
                                 ref uint outputByteCount,
                                 byte[] outputBuffer)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMEncrypt(
                                cryptoProvHandle,
                                position,
                                inputByteCount,
                                inputBuffer,
                                ref outputByteCount,
                                outputBuffer);
        }

        internal static int DRMGetInfo(
                                 SafeRightsManagementHandle handle,
                                 string attributeType,
                                 out uint encodingType,
                                 ref uint outputByteCount,
                                 byte[] outputBuffer)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMGetInfo(
                                handle,
                                attributeType,
                                out encodingType,
                                ref outputByteCount,
                                outputBuffer);
        }

        internal static int DRMGetApplicationSpecificData(
                                SafeRightsManagementPubHandle issuanceLicenseHandle,
                                uint index,
                                ref uint nameLength,
                                StringBuilder name,
                                ref uint valueLength,
                                StringBuilder value)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMGetApplicationSpecificData(
                                issuanceLicenseHandle,
                                index,
                                ref nameLength,
                                name,
                                ref valueLength,
                                value);
        }

        internal static int DRMSetApplicationSpecificData(
                                SafeRightsManagementPubHandle issuanceLicenseHandle,
                                bool flagDelete,
                                string name,
                                string value)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMSetApplicationSpecificData(
                                issuanceLicenseHandle,
                                flagDelete,
                                name,
                                value);
        }

        internal static int DRMGetIntervalTime(
                                SafeRightsManagementPubHandle issuanceLicenseHandle,
                                ref uint days)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMGetIntervalTime(
                                issuanceLicenseHandle,
                                ref days);
        }

        internal static int DRMSetIntervalTime(
                                SafeRightsManagementPubHandle issuanceLicenseHandle,
                                uint days)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMSetIntervalTime(
                                issuanceLicenseHandle,
                                days);
        }

        internal static int DRMGetRevocationPoint(
                                SafeRightsManagementPubHandle issuanceLicenseHandle,
                                ref uint idLength,
                                StringBuilder id,
                                ref uint idTypeLength,
                                StringBuilder idType,
                                ref uint urlLength,
                                StringBuilder url,
                                SystemTime frequency,
                                ref uint nameLength,
                                StringBuilder name,
                                ref uint publicKeyLength,
                                StringBuilder publicKey)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMGetRevocationPoint(
                                issuanceLicenseHandle,
                                ref idLength,
                                id,
                                ref idTypeLength,
                                idType,
                                ref urlLength,
                                url,
                                frequency,
                                ref nameLength,
                                name,
                                ref publicKeyLength,
                                publicKey);
        }


        internal static int DRMSetRevocationPoint(
                                SafeRightsManagementPubHandle issuanceLicenseHandle,
                                bool flagDelete,
                                string id,
                                string idType,
                                string url,
                                SystemTime frequency,
                                string name,
                                string publicKey)
        {
            SecurityHelper.DemandRightsManagementPermission();
            return UnsafeNativeMethods.DRMSetRevocationPoint(
                                issuanceLicenseHandle,
                                flagDelete,
                                id,
                                idType,
                                url,
                                frequency,
                                name,
                                publicKey);
        }
    }
}
