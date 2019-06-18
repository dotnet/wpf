// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This is partial class declaration of the InternalSafeNativeMethods
//   specifically the private sub class PrivateUnsafeNativeMethods with the PInvoke declarations
//
//
//
//

#define PRESENTATION_HOST_DLL
// We use this #ifdef to control the usage of the intermediate unmanaged 
// DLL that is used to satisfy requirement of the DRM DK SP1 Lock box. 
// SP1 Lock box requires that certain functions (DRMGetBoundLicenseObject, 
// DRMInitEnvironment, ...) be called from the signed unmanaged DLL. We use 
// PresentationHostDll for that purpose. In future we expect that such 
// requirement might go away and we wouldn't need it. It is also a convenient 
// debugging tool to use MSDRM.dll directly with the DRM SDK shipped msdrm-lcp.dll 
// (This one intended to serve as a proxy to the real msdrm.dll in order to enable 
// debugging of the client applications)


using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Security;

using MS.Win32;

namespace MS.Internal.Security.RightsManagement
{
    internal static partial class SafeNativeMethods
    {
        private static class UnsafeNativeMethods
        {
            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMCreateClientSession(
                                    [In, MarshalAs(UnmanagedType.FunctionPtr)]CallbackDelegate pfnCallback,
                                    [In, MarshalAs(UnmanagedType.U4)] uint uCallbackVersion,
                                    [In, MarshalAs(UnmanagedType.LPWStr)] string GroupIDProviderType,
                                    [In, MarshalAs(UnmanagedType.LPWStr)] string GroupID,
                                    [Out] out SafeRightsManagementSessionHandle phSession);

            // we can not use safe handle in the DrmClose... function
            // as the SafeHandle implementation marks this instance as an invalid by the time 
            // ReleaseHandle is called. After that marshalling code doesn't let the current instance 
            // of the Safe*Handle sub-class to cross managed/unmanaged boundary.
            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMCloseSession(
                                    [In, MarshalAs(UnmanagedType.U4)] uint sessionHandle);


            // we can not use safe handle in the DrmClose... function
            // as the SafeHandle implementation marks this instance as an invalid by the time 
            // ReleaseHandle is called. After that marshalling code doesn't let the current instance 
            // of the Safe*Handle sub-class to cross managed/unmanaged boundary.
#if PRESENTATION_HOST_DLL
            [DllImport(ExternDll.PresentationHostDll,SetLastError=false,CharSet=CharSet.Unicode,CallingConvention=CallingConvention.StdCall)]
#else
            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
#endif
            internal static extern int DRMCloseHandle(
                                    [In, MarshalAs(UnmanagedType.U4)] uint handle);

            // we can not use safe handle in the DrmClose... function
            // as the SafeHandle implementation marks this instance as an invalid by the time 
            // ReleaseHandle is called. After that marshalling code doesn't let the current instance 
            // of the Safe*Handle sub-class to cross managed/unmanaged boundary.
            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMCloseQueryHandle(
                                    [In, MarshalAs(UnmanagedType.U4)] uint queryHandle);



            // we can not use safe handle in the DrmClose... function
            // as the SafeHandle implementation marks this instance as an invalid by the time 
            // ReleaseHandle is called. After that marshalling code doesn't let the current instance 
            // of the Safe*Handle sub-class to cross managed/unmanaged boundary.
#if PRESENTATION_HOST_DLL
            [DllImport(ExternDll.PresentationHostDll, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
#else
            [DllImport(ExternDll.MsDrm,SetLastError=false,CharSet=CharSet.Unicode,CallingConvention=CallingConvention.StdCall)]
#endif
            internal static extern int DRMCloseEnvironmentHandle(
                                    [In, MarshalAs(UnmanagedType.U4)] uint envHandle);


#if PRESENTATION_HOST_DLL
            [DllImport(ExternDll.PresentationHostDll, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
#else
            [DllImport(ExternDll.MsDrm,SetLastError=false,CharSet=CharSet.Unicode,CallingConvention=CallingConvention.StdCall)]
#endif
            internal static extern int DRMInitEnvironment(
                                    [In, MarshalAs(UnmanagedType.U4)] uint eSecurityProviderType,
                                    [In, MarshalAs(UnmanagedType.U4)] uint eSpecification,
                                    [In, MarshalAs(UnmanagedType.LPWStr)] string securityProvider,
                                    [In, MarshalAs(UnmanagedType.LPWStr)] string manifestCredentials,
                                    [In, MarshalAs(UnmanagedType.LPWStr)] string machineCredentials,
                                    [Out] out SafeRightsManagementEnvironmentHandle environmentHandle,
                                    [Out] out SafeRightsManagementHandle defaultLibrary);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMIsActivated(
                                    [In] SafeRightsManagementSessionHandle hSession,
                                    [In, MarshalAs(UnmanagedType.U4)] uint uFlags,
                                    [In, MarshalAs(UnmanagedType.LPStruct)] ActivationServerInfo activationServerInfo);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMActivate(
                                    [In] SafeRightsManagementSessionHandle hSession,
                                    [In, MarshalAs(UnmanagedType.U4)] uint uFlags,
                                    [In, MarshalAs(UnmanagedType.U4)]uint uLangID,
                                    [In, MarshalAs(UnmanagedType.LPStruct)] ActivationServerInfo activationServerInfo,
                                    IntPtr context,  // this is a void* in the unmanaged SDK so IntPtr is the right (platform dependent declaration)
                                    IntPtr parentWindowHandle); // this a HWND in the unmanaged SDK so IntPtr is the right (platform dependent declaration)

#if PRESENTATION_HOST_DLL
            [DllImport(ExternDll.PresentationHostDll, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
#else
            [DllImport(ExternDll.MsDrm,SetLastError=false,CharSet=CharSet.Unicode,CallingConvention=CallingConvention.StdCall)]
#endif
            internal static extern int DRMCreateLicenseStorageSession(
                                    [In] SafeRightsManagementEnvironmentHandle envHandle,
                                    [In] SafeRightsManagementHandle hDefLib,
                                    [In] SafeRightsManagementSessionHandle hClientSession,
                                    [In, MarshalAs(UnmanagedType.U4)] uint uFlags,
                                    [In, MarshalAs(UnmanagedType.LPWStr)] string IssuanceLicense,
                                    [Out] out SafeRightsManagementSessionHandle phLicenseStorageSession);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMAcquireLicense(
                                    [In] SafeRightsManagementSessionHandle hSession,
                                    [In, MarshalAs(UnmanagedType.U4)] uint uFlags,
                                    [In, MarshalAs(UnmanagedType.LPWStr)] string GroupIdentityCredential,
                                    [In, MarshalAs(UnmanagedType.LPWStr)] string RequestedRights,
                                    [In, MarshalAs(UnmanagedType.LPWStr)] string CustomData,
                                    [In, MarshalAs(UnmanagedType.LPWStr)] string url,
                                    IntPtr context); // this is a void* in the unmanaged SDK so IntPtr is the right (platform dependent declaration)

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMEnumerateLicense(
                                    [In] SafeRightsManagementSessionHandle hSession,
                                    [In, MarshalAs(UnmanagedType.U4)] uint uFlags,
                                    [In, MarshalAs(UnmanagedType.U4)] uint uIndex,
                                    [In, Out, MarshalAs(UnmanagedType.Bool)] ref bool pfSharedFlag,
                                    [In, Out, MarshalAs(UnmanagedType.U4)] ref uint puCertDataLen,
                                    [MarshalAs(UnmanagedType.LPWStr)] StringBuilder wszCertificateData);

#if PRESENTATION_HOST_DLL
            [DllImport(ExternDll.PresentationHostDll, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
#else
            [DllImport(ExternDll.MsDrm,SetLastError=false,CharSet=CharSet.Unicode,CallingConvention=CallingConvention.StdCall)]
#endif
            internal static extern int DRMGetServiceLocation(
                    [In] SafeRightsManagementSessionHandle clientSessionHandle,
                    [In, MarshalAs(UnmanagedType.U4)] uint serviceType,
                    [In, MarshalAs(UnmanagedType.U4)] uint serviceLocation,
                    [In, MarshalAs(UnmanagedType.LPWStr)] string issuanceLicense,
                    [In, Out, MarshalAs(UnmanagedType.U4)] ref uint serviceUrlLength,
                    [MarshalAs(UnmanagedType.LPWStr)] StringBuilder serviceUrl);


            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMDeconstructCertificateChain(
                [In, MarshalAs(UnmanagedType.LPWStr)] string chain,
                [In, MarshalAs(UnmanagedType.U4)] uint index,
                [In, Out, MarshalAs(UnmanagedType.U4)] ref uint certificateLength,
                [MarshalAs(UnmanagedType.LPWStr)] StringBuilder certificate);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMParseUnboundLicense(
                [In, MarshalAs(UnmanagedType.LPWStr)] string certificate,
                [Out] out SafeRightsManagementQueryHandle queryRootHandle);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMGetUnboundLicenseObjectCount(
                [In] SafeRightsManagementQueryHandle queryRootHandle,
                [In, MarshalAs(UnmanagedType.LPWStr)] string subObjectType,
                [Out, MarshalAs(UnmanagedType.U4)] out uint objectCount);

#if PRESENTATION_HOST_DLL
            [DllImport(ExternDll.PresentationHostDll, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
#else
            [DllImport(ExternDll.MsDrm,SetLastError=false,CharSet=CharSet.Unicode,CallingConvention=CallingConvention.StdCall)]
#endif
            internal static extern int DRMGetBoundLicenseObject(
                [In] SafeRightsManagementHandle queryRootHandle,
                [In, MarshalAs(UnmanagedType.LPWStr)] string subObjectType,
                [In, MarshalAs(UnmanagedType.U4)] uint index,
                [Out] out SafeRightsManagementHandle subQueryHandle);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMGetUnboundLicenseObject(
                [In] SafeRightsManagementQueryHandle queryRootHandle,
                [In, MarshalAs(UnmanagedType.LPWStr)] string subObjectType,
                [In, MarshalAs(UnmanagedType.U4)] uint index,
                [Out] out SafeRightsManagementQueryHandle subQueryHandle);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMGetUnboundLicenseAttribute(
                [In] SafeRightsManagementQueryHandle queryRootHandle,
                [In, MarshalAs(UnmanagedType.LPWStr)] string attributeType,
                [In, MarshalAs(UnmanagedType.U4)] uint index,
                [Out, MarshalAs(UnmanagedType.U4)] out uint encodingType,
                [In, Out, MarshalAs(UnmanagedType.U4)] ref uint bufferSize,
                byte[] buffer);

#if PRESENTATION_HOST_DLL
            [DllImport(ExternDll.PresentationHostDll, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
#else
            [DllImport(ExternDll.MsDrm,SetLastError=false,CharSet=CharSet.Unicode,CallingConvention=CallingConvention.StdCall)]
#endif
            internal static extern int DRMGetBoundLicenseAttribute(
                [In] SafeRightsManagementHandle queryRootHandle,
                [In, MarshalAs(UnmanagedType.LPWStr)] string attributeType,
                [In, MarshalAs(UnmanagedType.U4)] uint index,
                [Out, MarshalAs(UnmanagedType.U4)] out uint encodingType,
                [In, Out, MarshalAs(UnmanagedType.U4)] ref uint bufferSize,
                byte[] buffer);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMCreateIssuanceLicense(
                            [In, MarshalAs(UnmanagedType.LPStruct)] SystemTime timeFrom,
                            [In, MarshalAs(UnmanagedType.LPStruct)] SystemTime timeUntil,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string referralInfoName,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string referralInfoUrl,
                            [In] SafeRightsManagementPubHandle ownerUserHandle,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string issuanceLicense,
                            [In] SafeRightsManagementHandle boundLicenseHandle,
                            [Out] out SafeRightsManagementPubHandle issuanceLicenseHandle);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMCreateUser(
                            [In, MarshalAs(UnmanagedType.LPWStr)] string userName,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string userId,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string userIdType,
                            [Out] out SafeRightsManagementPubHandle userHandle);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMGetUsers(
                            [In] SafeRightsManagementPubHandle issuanceLicenseHandle,
                            [In, MarshalAs(UnmanagedType.U4)] uint index,
                            [Out] out SafeRightsManagementPubHandle userHandle);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMGetUserRights(
                            [In] SafeRightsManagementPubHandle issuanceLicenseHandle,
                            [In] SafeRightsManagementPubHandle userHandle,
                            [In, MarshalAs(UnmanagedType.U4)] uint index,
                            [Out] out SafeRightsManagementPubHandle rightHandle);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMGetUserInfo(
                            [In] SafeRightsManagementPubHandle userHandle,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint userNameLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder userName,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint userIdLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder userId,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint userIdTypeLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder userIdType);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMGetRightInfo(
                            [In] SafeRightsManagementPubHandle rightHandle,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint rightNameLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder rightName,
                            [MarshalAs(UnmanagedType.LPStruct)] SystemTime timeFrom,
                            [MarshalAs(UnmanagedType.LPStruct)] SystemTime timeUntil);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMCreateRight(
                            [In, MarshalAs(UnmanagedType.LPWStr)] string rightName,
                            [In, MarshalAs(UnmanagedType.LPStruct)] SystemTime timeFrom,
                            [In, MarshalAs(UnmanagedType.LPStruct)] SystemTime timeUntil,
                            [In, MarshalAs(UnmanagedType.U4)] uint countExtendedInfo,
                            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] string[] extendedInfoNames,
                            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] string[] extendedInfoValues,
                            [Out] out SafeRightsManagementPubHandle rightHandle);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMGetIssuanceLicenseTemplate(
                            [In] SafeRightsManagementPubHandle issuanceLicenseHandle,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint issuanceLicenseTemplateLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder issuanceLicenseTemplate);

            // we can not use safe handle in the DrmClose... function
            // as the SafeHandle implementation marks this instance as an invalid by the time 
            // ReleaseHandle is called. After that marshalling code doesn't let the current instance 
            // of the Safe*Handle sub-class to cross managed/unmanaged boundary.
            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMClosePubHandle(
                            [In, MarshalAs(UnmanagedType.U4)] uint pubHandle);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMAddRightWithUser(
                            [In] SafeRightsManagementPubHandle issuanceLicenseHandle,
                            [In] SafeRightsManagementPubHandle rightHandle,
                            [In] SafeRightsManagementPubHandle userHandle);


            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMSetMetaData(
                            [In] SafeRightsManagementPubHandle issuanceLicenseHandle,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string contentId,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string contentIdType,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string SkuId,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string SkuIdType,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string contentType,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string contentName);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMGetIssuanceLicenseInfo(
                            [In] SafeRightsManagementPubHandle issuanceLicenseHandle,
                            [MarshalAs(UnmanagedType.LPStruct)] SystemTime timeFrom,
                            [MarshalAs(UnmanagedType.LPStruct)] SystemTime timeUntil,
                            [In, MarshalAs(UnmanagedType.U4)] uint flags,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint distributionPointNameLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder DistributionPointName,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint distributionPointUriLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder DistributionPointUri,
                            [Out] out SafeRightsManagementPubHandle ownerHandle,
                            [Out, MarshalAs(UnmanagedType.Bool)] out bool officialFlag);


            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMGetSecurityProvider(
                            [In, MarshalAs(UnmanagedType.U4)] uint flags,  // currently not used by the DRM SDK      
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint typeLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder type,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint pathLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder path);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMDeleteLicense(
                            [In] SafeRightsManagementSessionHandle hSession,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string wszLicenseId);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMSetNameAndDescription(
                            [In] SafeRightsManagementPubHandle issuanceLicenseHandle,
                            [In, MarshalAs(UnmanagedType.Bool)] bool flagDelete,
                            [In, MarshalAs(UnmanagedType.U4)] uint localeId,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string name,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string description);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMGetNameAndDescription(
                            [In] SafeRightsManagementPubHandle issuanceLicenseHandle,
                            [In, MarshalAs(UnmanagedType.U4)] uint uIndex,
                            [Out, MarshalAs(UnmanagedType.U4)] out uint localeId,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint nameLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder name,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint descriptionLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder description);

#if PRESENTATION_HOST_DLL
            [DllImport(ExternDll.PresentationHostDll, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
#else
            [DllImport(ExternDll.MsDrm,SetLastError=false,CharSet=CharSet.Unicode,CallingConvention=CallingConvention.StdCall)]
#endif
            internal static extern int DRMGetSignedIssuanceLicense(
                            [In] SafeRightsManagementEnvironmentHandle environmentHandle,
                            [In] SafeRightsManagementPubHandle issuanceLicenseHandle,
                            [In, MarshalAs(UnmanagedType.U4)] uint flags,
                            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] symmetricKey,
                            [In, MarshalAs(UnmanagedType.U4)] uint symmetricKeyByteCount,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string symmetricKeyType,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string clientLicensorCertificate,
                            [In, MarshalAs(UnmanagedType.FunctionPtr)]CallbackDelegate pfnCallback,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string url,
                            [In, MarshalAs(UnmanagedType.U4)] uint context);


            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMGetOwnerLicense(
                            [In] SafeRightsManagementPubHandle issuanceLicenseHandle,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint ownerLicenseLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder ownerLicense);

#if PRESENTATION_HOST_DLL
            [DllImport(ExternDll.PresentationHostDll, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
#else
            [DllImport(ExternDll.MsDrm,SetLastError=false,CharSet=CharSet.Unicode,CallingConvention=CallingConvention.StdCall)]
#endif
            internal static extern int DRMCreateBoundLicense(
                            [In] SafeRightsManagementEnvironmentHandle environmentHandle,
                            [In, MarshalAs(UnmanagedType.LPStruct)] BoundLicenseParams boundLicenseParams,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string licenseChain,
                            [Out] out SafeRightsManagementHandle boundLicenseHandle,
                            [Out, MarshalAs(UnmanagedType.U4)] out uint errorLogHandle);


#if PRESENTATION_HOST_DLL
            [DllImport(ExternDll.PresentationHostDll, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
#else
            [DllImport(ExternDll.MsDrm,SetLastError=false,CharSet=CharSet.Unicode,CallingConvention=CallingConvention.StdCall)]
#endif
            internal static extern int DRMCreateEnablingBitsDecryptor(
                [In] SafeRightsManagementHandle boundLicenseHandle,
                [In, MarshalAs(UnmanagedType.LPWStr)] string right,
                [In, MarshalAs(UnmanagedType.U4)] uint auxLibrary,
                [In, MarshalAs(UnmanagedType.LPWStr)] string auxPlugin,
                [Out] out SafeRightsManagementHandle decryptorHandle);

#if PRESENTATION_HOST_DLL
            [DllImport(ExternDll.PresentationHostDll, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
#else
            [DllImport(ExternDll.MsDrm,SetLastError=false,CharSet=CharSet.Unicode,CallingConvention=CallingConvention.StdCall)]
#endif
            internal static extern int DRMCreateEnablingBitsEncryptor(
                [In] SafeRightsManagementHandle boundLicenseHandle,
                [In, MarshalAs(UnmanagedType.LPWStr)] string right,
                [In, MarshalAs(UnmanagedType.U4)] uint auxLibrary,
                [In, MarshalAs(UnmanagedType.LPWStr)] string auxPlugin,
                [Out] out SafeRightsManagementHandle encryptorHandle);

#if PRESENTATION_HOST_DLL
            [DllImport(ExternDll.PresentationHostDll, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
#else
            [DllImport(ExternDll.MsDrm,SetLastError=false,CharSet=CharSet.Unicode,CallingConvention=CallingConvention.StdCall)]
#endif
            internal static extern int DRMDecrypt(
                [In] SafeRightsManagementHandle cryptoProvHandle,
                [In, MarshalAs(UnmanagedType.U4)] uint position,
                [In, MarshalAs(UnmanagedType.U4)] uint inputByteCount,
                byte[] inputBuffer,
                [In, Out, MarshalAs(UnmanagedType.U4)] ref uint outputByteCount,
                byte[] outputBuffer);

#if PRESENTATION_HOST_DLL
            [DllImport(ExternDll.PresentationHostDll, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
#else
            [DllImport(ExternDll.MsDrm,SetLastError=false,CharSet=CharSet.Unicode,CallingConvention=CallingConvention.StdCall)]
#endif
            internal static extern int DRMEncrypt(
                [In] SafeRightsManagementHandle cryptoProvHandle,
                [In, MarshalAs(UnmanagedType.U4)] uint position,
                [In, MarshalAs(UnmanagedType.U4)] uint inputByteCount,
                byte[] inputBuffer,
                [In, Out, MarshalAs(UnmanagedType.U4)] ref uint outputByteCount,
                byte[] outputBuffer);

#if PRESENTATION_HOST_DLL
            [DllImport(ExternDll.PresentationHostDll, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
#else
            [DllImport(ExternDll.MsDrm,SetLastError=false,CharSet=CharSet.Unicode,CallingConvention=CallingConvention.StdCall)]
#endif
            internal static extern int DRMGetInfo(
                [In] SafeRightsManagementHandle handle,
                [In, MarshalAs(UnmanagedType.LPWStr)] string attributeType,
                [Out, MarshalAs(UnmanagedType.U4)] out uint encodingType,
                [In, Out, MarshalAs(UnmanagedType.U4)] ref uint outputByteCount,
                byte[] outputBuffer);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMGetApplicationSpecificData(
                            [In] SafeRightsManagementPubHandle issuanceLicenseHandle,
                            [In, MarshalAs(UnmanagedType.U4)] uint index,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint nameLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder name,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint valueLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder value);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMSetApplicationSpecificData(
                            [In] SafeRightsManagementPubHandle issuanceLicenseHandle,
                            [In, MarshalAs(UnmanagedType.Bool)] bool flagDelete,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string name,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string value);


            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMGetIntervalTime(
                            [In] SafeRightsManagementPubHandle issuanceLicenseHandle,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint days);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMSetIntervalTime(
                            [In] SafeRightsManagementPubHandle issuanceLicenseHandle,
                            [In, MarshalAs(UnmanagedType.U4)] uint days);


            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMGetRevocationPoint(
                            [In] SafeRightsManagementPubHandle issuanceLicenseHandle,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint idLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder id,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint idTypeLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder idType,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint urlLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder url,
                            [MarshalAs(UnmanagedType.LPStruct)] SystemTime frequency,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint nameLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder name,
                            [In, Out, MarshalAs(UnmanagedType.U4)] ref uint publicKeyLength,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder publicKey);

            [DllImport(ExternDll.MsDrm, SetLastError = false, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern int DRMSetRevocationPoint(
                            [In] SafeRightsManagementPubHandle issuanceLicenseHandle,
                            [In, MarshalAs(UnmanagedType.Bool)] bool flagDelete,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string id,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string idType,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string url,
                            [In, MarshalAs(UnmanagedType.LPStruct)] SystemTime frequency,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string name,
                            [In, MarshalAs(UnmanagedType.LPWStr)] string publicKey);
        }
    }
}
