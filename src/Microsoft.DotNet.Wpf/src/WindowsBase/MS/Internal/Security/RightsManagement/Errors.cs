// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This file contains Error Constants defined by the promethium SDK and an exception mapping mechanism
//
//
//
//
//

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.RightsManagement;
using System.Windows;
using MS.Internal.WindowsBase;

// Enable presharp pragma warning suppress directives.
#pragma warning disable 1634, 1691

namespace MS.Internal.Security.RightsManagement
{
    internal static class Errors
    {
        internal static string GetLocalizedFailureCodeMessageWithDefault(RightsManagementFailureCode failureCode)
        {
            string errorMessage = GetLocalizedFailureCodeMessage(failureCode);
            if (errorMessage != null)
            {
                return errorMessage;
            }
            else
            {
                return SR.RmExceptionGenericMessage;
            }
        }

        /// <summary>
        /// This function throws the exception if hr doesn't indicate a success. In case of recognized
        /// Rights Management Failure code it will throw a RightsManagementException with an appropriate
        /// message. Otherwise, if code isn't recognized as Rights Management specific,  it will throw
        /// RightsManagementException which has a COMException as an inner exception with the
        /// appropriate hr code.
        /// </summary>
        internal static void ThrowOnErrorCode(int hr)
        {
            // we can return if it is not a failure right away
            // there is no reason to attempt to look-up codes and
            // messages which is somewhat expensive in the case of success
            if (hr >=0)
            {
                return;
            }

            string errorMessage = GetLocalizedFailureCodeMessage((RightsManagementFailureCode)hr);
            if (errorMessage != null)
            {
                throw new RightsManagementException((RightsManagementFailureCode)hr, errorMessage);
            }
            else
            {
                try
                {
                    // It seems that ThrowExceptionForHR is the most consistent way
                    // to get a platform representation of the unmanaged HR code
                    Marshal.ThrowExceptionForHR(hr);
                }
// disabling PreSharp false positive. In this case we are actually re-throwing the same exception
// wrapped in a more specific message
#pragma warning disable 56500
                catch (Exception e)
                {
                    // rethrow the exception as an inner exception of the RmExceptionGenericMessage
                    throw new RightsManagementException(SR.RmExceptionGenericMessage, e);
                }
#pragma warning restore 56500
            }
        }

        private static string GetLocalizedFailureCodeMessage(RightsManagementFailureCode failureCode)
        {
            string result;
            switch (failureCode)
            {
                case RightsManagementFailureCode.InvalidLicense:
                    result=nameof(SR.RmExceptionInvalidLicense); break;
                case RightsManagementFailureCode.InfoNotInLicense:
                    result=nameof(SR.RmExceptionInfoNotInLicense); break;
                case RightsManagementFailureCode.InvalidLicenseSignature:
                    result=nameof(SR.RmExceptionInvalidLicenseSignature); break;
                case RightsManagementFailureCode.EncryptionNotPermitted:
                    result=nameof(SR.RmExceptionEncryptionNotPermitted); break;
                case RightsManagementFailureCode.RightNotGranted:
                    result=nameof(SR.RmExceptionRightNotGranted); break;
                case RightsManagementFailureCode.InvalidVersion:
                    result=nameof(SR.RmExceptionInvalidVersion); break;
                case RightsManagementFailureCode.InvalidEncodingType:
                    result=nameof(SR.RmExceptionInvalidEncodingType); break;
                case RightsManagementFailureCode.InvalidNumericalValue:
                    result=nameof(SR.RmExceptionInvalidNumericalValue); break;
                case RightsManagementFailureCode.InvalidAlgorithmType:
                    result=nameof(SR.RmExceptionInvalidAlgorithmType); break;
                case RightsManagementFailureCode.EnvironmentNotLoaded:
                    result=nameof(SR.RmExceptionEnvironmentNotLoaded); break;
                case RightsManagementFailureCode.EnvironmentCannotLoad:
                    result=nameof(SR.RmExceptionEnvironmentCannotLoad); break;
                case RightsManagementFailureCode.TooManyLoadedEnvironments:
                    result=nameof(SR.RmExceptionTooManyLoadedEnvironments); break;
                case RightsManagementFailureCode.IncompatibleObjects:
                    result=nameof(SR.RmExceptionIncompatibleObjects); break;
                case RightsManagementFailureCode.LibraryFail:
                    result=nameof(SR.RmExceptionLibraryFail); break;
                case RightsManagementFailureCode.EnablingPrincipalFailure:
                    result=nameof(SR.RmExceptionEnablingPrincipalFailure); break;
                case RightsManagementFailureCode.InfoNotPresent:
                    result=nameof(SR.RmExceptionInfoNotPresent); break;
                case RightsManagementFailureCode.BadGetInfoQuery:
                    result=nameof(SR.RmExceptionBadGetInfoQuery); break;
                case RightsManagementFailureCode.KeyTypeUnsupported:
                    result=nameof(SR.RmExceptionKeyTypeUnsupported); break;
                case RightsManagementFailureCode.CryptoOperationUnsupported:
                    result=nameof(SR.RmExceptionCryptoOperationUnsupported); break;
                case RightsManagementFailureCode.ClockRollbackDetected:
                    result=nameof(SR.RmExceptionClockRollbackDetected); break;
                case RightsManagementFailureCode.QueryReportsNoResults:
                    result=nameof(SR.RmExceptionQueryReportsNoResults); break;
                case RightsManagementFailureCode.UnexpectedException:
                    result=nameof(SR.RmExceptionUnexpectedException); break;
                case RightsManagementFailureCode.BindValidityTimeViolated:
                    result=nameof(SR.RmExceptionBindValidityTimeViolated); break;
                case RightsManagementFailureCode.BrokenCertChain:
                    result=nameof(SR.RmExceptionBrokenCertChain); break;
                case RightsManagementFailureCode.BindPolicyViolation:
                    result=nameof(SR.RmExceptionBindPolicyViolation); break;
                case RightsManagementFailureCode.ManifestPolicyViolation:
                    result=nameof(SR.RmExceptionManifestPolicyViolation); break;
                case RightsManagementFailureCode.BindRevokedLicense:
                    result=nameof(SR.RmExceptionBindRevokedLicense); break;
                case RightsManagementFailureCode.BindRevokedIssuer:
                    result=nameof(SR.RmExceptionBindRevokedIssuer); break;
                case RightsManagementFailureCode.BindRevokedPrincipal:
                    result=nameof(SR.RmExceptionBindRevokedPrincipal); break;
                case RightsManagementFailureCode.BindRevokedResource:
                    result=nameof(SR.RmExceptionBindRevokedResource); break;
                case RightsManagementFailureCode.BindRevokedModule:
                    result=nameof(SR.RmExceptionBindRevokedModule); break;
                case RightsManagementFailureCode.BindContentNotInEndUseLicense:
                    result=nameof(SR.RmExceptionBindContentNotInEndUseLicense); break;
                case RightsManagementFailureCode.BindAccessPrincipalNotEnabling:
                    result=nameof(SR.RmExceptionBindAccessPrincipalNotEnabling); break;
                case RightsManagementFailureCode.BindAccessUnsatisfied:
                    result=nameof(SR.RmExceptionBindAccessUnsatisfied); break;
                case RightsManagementFailureCode.BindIndicatedPrincipalMissing:
                    result=nameof(SR.RmExceptionBindIndicatedPrincipalMissing); break;
                case RightsManagementFailureCode.BindMachineNotFoundInGroupIdentity:
                    result=nameof(SR.RmExceptionBindMachineNotFoundInGroupIdentity); break;
                case RightsManagementFailureCode.LibraryUnsupportedPlugIn:
                    result=nameof(SR.RmExceptionLibraryUnsupportedPlugIn); break;
                case RightsManagementFailureCode.BindRevocationListStale:
                    result=nameof(SR.RmExceptionBindRevocationListStale); break;
                case RightsManagementFailureCode.BindNoApplicableRevocationList:
                    result=nameof(SR.RmExceptionBindNoApplicableRevocationList); break;
                case RightsManagementFailureCode.InvalidHandle:
                    result=nameof(SR.RmExceptionInvalidHandle); break;
                case RightsManagementFailureCode.BindIntervalTimeViolated:
                    result=nameof(SR.RmExceptionBindIntervalTimeViolated); break;
                case RightsManagementFailureCode.BindNoSatisfiedRightsGroup:
                    result=nameof(SR.RmExceptionBindNoSatisfiedRightsGroup); break;
                case RightsManagementFailureCode.BindSpecifiedWorkMissing:
                    result=nameof(SR.RmExceptionBindSpecifiedWorkMissing); break;
                case RightsManagementFailureCode.NoMoreData:
                    result=nameof(SR.RmExceptionNoMoreData); break;
                case RightsManagementFailureCode.LicenseAcquisitionFailed:
                    result=nameof(SR.RmExceptionLicenseAcquisitionFailed); break;
                case RightsManagementFailureCode.IdMismatch:
                    result=nameof(SR.RmExceptionIdMismatch); break;
                case RightsManagementFailureCode.TooManyCertificates:
                    result=nameof(SR.RmExceptionTooManyCertificates); break;
                case RightsManagementFailureCode.NoDistributionPointUrlFound:
                    result=nameof(SR.RmExceptionNoDistributionPointUrlFound); break;
                case RightsManagementFailureCode.AlreadyInProgress:
                    result=nameof(SR.RmExceptionAlreadyInProgress); break;
                case RightsManagementFailureCode.GroupIdentityNotSet:
                    result=nameof(SR.RmExceptionGroupIdentityNotSet); break;
                case RightsManagementFailureCode.RecordNotFound:
                    result=nameof(SR.RmExceptionRecordNotFound); break;
                case RightsManagementFailureCode.NoConnect:
                    result=nameof(SR.RmExceptionNoConnect); break;
                case RightsManagementFailureCode.NoLicense:
                    result=nameof(SR.RmExceptionNoLicense); break;
                case RightsManagementFailureCode.NeedsMachineActivation:
                    result=nameof(SR.RmExceptionNeedsMachineActivation); break;
                case RightsManagementFailureCode.NeedsGroupIdentityActivation:
                    result=nameof(SR.RmExceptionNeedsGroupIdentityActivation); break;
                case RightsManagementFailureCode.ActivationFailed:
                    result=nameof(SR.RmExceptionActivationFailed); break;
                case RightsManagementFailureCode.Aborted:
                    result=nameof(SR.RmExceptionAborted); break;
                case RightsManagementFailureCode.OutOfQuota:
                    result=nameof(SR.RmExceptionOutOfQuota); break;
                case RightsManagementFailureCode.AuthenticationFailed:
                    result=nameof(SR.RmExceptionAuthenticationFailed); break;
                case RightsManagementFailureCode.ServerError:
                    result=nameof(SR.RmExceptionServerError); break;
                case RightsManagementFailureCode.InstallationFailed:
                    result=nameof(SR.RmExceptionInstallationFailed); break;
                case RightsManagementFailureCode.HidCorrupted:
                    result=nameof(SR.RmExceptionHidCorrupted); break;
                case RightsManagementFailureCode.InvalidServerResponse:
                    result=nameof(SR.RmExceptionInvalidServerResponse); break;
                case RightsManagementFailureCode.ServiceNotFound:
                    result=nameof(SR.RmExceptionServiceNotFound); break;
                case RightsManagementFailureCode.UseDefault:
                    result=nameof(SR.RmExceptionUseDefault); break;
                case RightsManagementFailureCode.ServerNotFound:
                    result=nameof(SR.RmExceptionServerNotFound); break;
                case RightsManagementFailureCode.InvalidEmail:
                    result=nameof(SR.RmExceptionInvalidEmail); break;
                case RightsManagementFailureCode.ValidityTimeViolation:
                    result=nameof(SR.RmExceptionValidityTimeViolation); break;
                case RightsManagementFailureCode.OutdatedModule:
                    result=nameof(SR.RmExceptionOutdatedModule); break;
                case RightsManagementFailureCode.ServiceMoved:
                    result=nameof(SR.RmExceptionServiceMoved); break;
                case RightsManagementFailureCode.ServiceGone:
                    result=nameof(SR.RmExceptionServiceGone); break;
                case RightsManagementFailureCode.AdEntryNotFound:
                    result=nameof(SR.RmExceptionAdEntryNotFound); break;
                case RightsManagementFailureCode.NotAChain:
                    result=nameof(SR.RmExceptionNotAChain); break;
                case RightsManagementFailureCode.RequestDenied:
                    result=nameof(SR.RmExceptionRequestDenied); break;
                case RightsManagementFailureCode.NotSet:
                    result=nameof(SR.RmExceptionNotSet); break;
                case RightsManagementFailureCode.MetadataNotSet:
                    result=nameof(SR.RmExceptionMetadataNotSet); break;
                case RightsManagementFailureCode.RevocationInfoNotSet:
                    result=nameof(SR.RmExceptionRevocationInfoNotSet); break;
                case RightsManagementFailureCode.InvalidTimeInfo:
                    result=nameof(SR.RmExceptionInvalidTimeInfo); break;
                case RightsManagementFailureCode.RightNotSet:
                    result=nameof(SR.RmExceptionRightNotSet); break;
                case RightsManagementFailureCode.LicenseBindingToWindowsIdentityFailed:
                    result=nameof(SR.RmExceptionLicenseBindingToWindowsIdentityFailed); break;
                case RightsManagementFailureCode.InvalidIssuanceLicenseTemplate:
                    result=nameof(SR.RmExceptionInvalidIssuanceLicenseTemplate); break;
                case RightsManagementFailureCode.InvalidKeyLength:
                    result=nameof(SR.RmExceptionInvalidKeyLength); break;
                case RightsManagementFailureCode.ExpiredOfficialIssuanceLicenseTemplate:
                    result=nameof(SR.RmExceptionExpiredOfficialIssuanceLicenseTemplate); break;
                case RightsManagementFailureCode.InvalidClientLicensorCertificate:
                    result=nameof(SR.RmExceptionInvalidClientLicensorCertificate); break;
                case RightsManagementFailureCode.HidInvalid:
                    result=nameof(SR.RmExceptionHidInvalid); break;
                case RightsManagementFailureCode.EmailNotVerified:
                    result=nameof(SR.RmExceptionEmailNotVerified); break;
                case RightsManagementFailureCode.DebuggerDetected:
                    result=nameof(SR.RmExceptionDebuggerDetected); break;
                case RightsManagementFailureCode.InvalidLockboxType:
                    result=nameof(SR.RmExceptionInvalidLockboxType); break;
                case RightsManagementFailureCode.InvalidLockboxPath:
                    result=nameof(SR.RmExceptionInvalidLockboxPath); break;
                case RightsManagementFailureCode.InvalidRegistryPath:
                    result=nameof(SR.RmExceptionInvalidRegistryPath); break;
                case RightsManagementFailureCode.NoAesCryptoProvider:
                    result=nameof(SR.RmExceptionNoAesCryptoProvider); break;
                case RightsManagementFailureCode.GlobalOptionAlreadySet:
                    result=nameof(SR.RmExceptionGlobalOptionAlreadySet); break;
                case RightsManagementFailureCode.OwnerLicenseNotFound:
                    result=nameof(SR.RmExceptionOwnerLicenseNotFound); break;
                default:
                    return null;
            }
            return SR.Get(result);
        }
    }
}



