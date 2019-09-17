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
                return SR.Get(SRID.RmExceptionGenericMessage);
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
                    throw new RightsManagementException(SR.Get(SRID.RmExceptionGenericMessage), e);
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
                    result=SRID.RmExceptionInvalidLicense; break;
                case RightsManagementFailureCode.InfoNotInLicense:
                    result=SRID.RmExceptionInfoNotInLicense; break;
                case RightsManagementFailureCode.InvalidLicenseSignature:
                    result=SRID.RmExceptionInvalidLicenseSignature; break;
                case RightsManagementFailureCode.EncryptionNotPermitted:
                    result=SRID.RmExceptionEncryptionNotPermitted; break;
                case RightsManagementFailureCode.RightNotGranted:
                    result=SRID.RmExceptionRightNotGranted; break;
                case RightsManagementFailureCode.InvalidVersion:
                    result=SRID.RmExceptionInvalidVersion; break;
                case RightsManagementFailureCode.InvalidEncodingType:
                    result=SRID.RmExceptionInvalidEncodingType; break;
                case RightsManagementFailureCode.InvalidNumericalValue:
                    result=SRID.RmExceptionInvalidNumericalValue; break;
                case RightsManagementFailureCode.InvalidAlgorithmType:
                    result=SRID.RmExceptionInvalidAlgorithmType; break;
                case RightsManagementFailureCode.EnvironmentNotLoaded:
                    result=SRID.RmExceptionEnvironmentNotLoaded; break;
                case RightsManagementFailureCode.EnvironmentCannotLoad:
                    result=SRID.RmExceptionEnvironmentCannotLoad; break;
                case RightsManagementFailureCode.TooManyLoadedEnvironments:
                    result=SRID.RmExceptionTooManyLoadedEnvironments; break;
                case RightsManagementFailureCode.IncompatibleObjects:
                    result=SRID.RmExceptionIncompatibleObjects; break;
                case RightsManagementFailureCode.LibraryFail:
                    result=SRID.RmExceptionLibraryFail; break;
                case RightsManagementFailureCode.EnablingPrincipalFailure:
                    result=SRID.RmExceptionEnablingPrincipalFailure; break;
                case RightsManagementFailureCode.InfoNotPresent:
                    result=SRID.RmExceptionInfoNotPresent; break;
                case RightsManagementFailureCode.BadGetInfoQuery:
                    result=SRID.RmExceptionBadGetInfoQuery; break;
                case RightsManagementFailureCode.KeyTypeUnsupported:
                    result=SRID.RmExceptionKeyTypeUnsupported; break;
                case RightsManagementFailureCode.CryptoOperationUnsupported:
                    result=SRID.RmExceptionCryptoOperationUnsupported; break;
                case RightsManagementFailureCode.ClockRollbackDetected:
                    result=SRID.RmExceptionClockRollbackDetected; break;
                case RightsManagementFailureCode.QueryReportsNoResults:
                    result=SRID.RmExceptionQueryReportsNoResults; break;
                case RightsManagementFailureCode.UnexpectedException:
                    result=SRID.RmExceptionUnexpectedException; break;
                case RightsManagementFailureCode.BindValidityTimeViolated:
                    result=SRID.RmExceptionBindValidityTimeViolated; break;
                case RightsManagementFailureCode.BrokenCertChain:
                    result=SRID.RmExceptionBrokenCertChain; break;
                case RightsManagementFailureCode.BindPolicyViolation:
                    result=SRID.RmExceptionBindPolicyViolation; break;
                case RightsManagementFailureCode.ManifestPolicyViolation:
                    result=SRID.RmExceptionManifestPolicyViolation; break;
                case RightsManagementFailureCode.BindRevokedLicense:
                    result=SRID.RmExceptionBindRevokedLicense; break;
                case RightsManagementFailureCode.BindRevokedIssuer:
                    result=SRID.RmExceptionBindRevokedIssuer; break;
                case RightsManagementFailureCode.BindRevokedPrincipal:
                    result=SRID.RmExceptionBindRevokedPrincipal; break;
                case RightsManagementFailureCode.BindRevokedResource:
                    result=SRID.RmExceptionBindRevokedResource; break;
                case RightsManagementFailureCode.BindRevokedModule:
                    result=SRID.RmExceptionBindRevokedModule; break;
                case RightsManagementFailureCode.BindContentNotInEndUseLicense:
                    result=SRID.RmExceptionBindContentNotInEndUseLicense; break;
                case RightsManagementFailureCode.BindAccessPrincipalNotEnabling:
                    result=SRID.RmExceptionBindAccessPrincipalNotEnabling; break;
                case RightsManagementFailureCode.BindAccessUnsatisfied:
                    result=SRID.RmExceptionBindAccessUnsatisfied; break;
                case RightsManagementFailureCode.BindIndicatedPrincipalMissing:
                    result=SRID.RmExceptionBindIndicatedPrincipalMissing; break;
                case RightsManagementFailureCode.BindMachineNotFoundInGroupIdentity:
                    result=SRID.RmExceptionBindMachineNotFoundInGroupIdentity; break;
                case RightsManagementFailureCode.LibraryUnsupportedPlugIn:
                    result=SRID.RmExceptionLibraryUnsupportedPlugIn; break;
                case RightsManagementFailureCode.BindRevocationListStale:
                    result=SRID.RmExceptionBindRevocationListStale; break;
                case RightsManagementFailureCode.BindNoApplicableRevocationList:
                    result=SRID.RmExceptionBindNoApplicableRevocationList; break;
                case RightsManagementFailureCode.InvalidHandle:
                    result=SRID.RmExceptionInvalidHandle; break;
                case RightsManagementFailureCode.BindIntervalTimeViolated:
                    result=SRID.RmExceptionBindIntervalTimeViolated; break;
                case RightsManagementFailureCode.BindNoSatisfiedRightsGroup:
                    result=SRID.RmExceptionBindNoSatisfiedRightsGroup; break;
                case RightsManagementFailureCode.BindSpecifiedWorkMissing:
                    result=SRID.RmExceptionBindSpecifiedWorkMissing; break;
                case RightsManagementFailureCode.NoMoreData:
                    result=SRID.RmExceptionNoMoreData; break;
                case RightsManagementFailureCode.LicenseAcquisitionFailed:
                    result=SRID.RmExceptionLicenseAcquisitionFailed; break;
                case RightsManagementFailureCode.IdMismatch:
                    result=SRID.RmExceptionIdMismatch; break;
                case RightsManagementFailureCode.TooManyCertificates:
                    result=SRID.RmExceptionTooManyCertificates; break;
                case RightsManagementFailureCode.NoDistributionPointUrlFound:
                    result=SRID.RmExceptionNoDistributionPointUrlFound; break;
                case RightsManagementFailureCode.AlreadyInProgress:
                    result=SRID.RmExceptionAlreadyInProgress; break;
                case RightsManagementFailureCode.GroupIdentityNotSet:
                    result=SRID.RmExceptionGroupIdentityNotSet; break;
                case RightsManagementFailureCode.RecordNotFound:
                    result=SRID.RmExceptionRecordNotFound; break;
                case RightsManagementFailureCode.NoConnect:
                    result=SRID.RmExceptionNoConnect; break;
                case RightsManagementFailureCode.NoLicense:
                    result=SRID.RmExceptionNoLicense; break;
                case RightsManagementFailureCode.NeedsMachineActivation:
                    result=SRID.RmExceptionNeedsMachineActivation; break;
                case RightsManagementFailureCode.NeedsGroupIdentityActivation:
                    result=SRID.RmExceptionNeedsGroupIdentityActivation; break;
                case RightsManagementFailureCode.ActivationFailed:
                    result=SRID.RmExceptionActivationFailed; break;
                case RightsManagementFailureCode.Aborted:
                    result=SRID.RmExceptionAborted; break;
                case RightsManagementFailureCode.OutOfQuota:
                    result=SRID.RmExceptionOutOfQuota; break;
                case RightsManagementFailureCode.AuthenticationFailed:
                    result=SRID.RmExceptionAuthenticationFailed; break;
                case RightsManagementFailureCode.ServerError:
                    result=SRID.RmExceptionServerError; break;
                case RightsManagementFailureCode.InstallationFailed:
                    result=SRID.RmExceptionInstallationFailed; break;
                case RightsManagementFailureCode.HidCorrupted:
                    result=SRID.RmExceptionHidCorrupted; break;
                case RightsManagementFailureCode.InvalidServerResponse:
                    result=SRID.RmExceptionInvalidServerResponse; break;
                case RightsManagementFailureCode.ServiceNotFound:
                    result=SRID.RmExceptionServiceNotFound; break;
                case RightsManagementFailureCode.UseDefault:
                    result=SRID.RmExceptionUseDefault; break;
                case RightsManagementFailureCode.ServerNotFound:
                    result=SRID.RmExceptionServerNotFound; break;
                case RightsManagementFailureCode.InvalidEmail:
                    result=SRID.RmExceptionInvalidEmail; break;
                case RightsManagementFailureCode.ValidityTimeViolation:
                    result=SRID.RmExceptionValidityTimeViolation; break;
                case RightsManagementFailureCode.OutdatedModule:
                    result=SRID.RmExceptionOutdatedModule; break;
                case RightsManagementFailureCode.ServiceMoved:
                    result=SRID.RmExceptionServiceMoved; break;
                case RightsManagementFailureCode.ServiceGone:
                    result=SRID.RmExceptionServiceGone; break;
                case RightsManagementFailureCode.AdEntryNotFound:
                    result=SRID.RmExceptionAdEntryNotFound; break;
                case RightsManagementFailureCode.NotAChain:
                    result=SRID.RmExceptionNotAChain; break;
                case RightsManagementFailureCode.RequestDenied:
                    result=SRID.RmExceptionRequestDenied; break;
                case RightsManagementFailureCode.NotSet:
                    result=SRID.RmExceptionNotSet; break;
                case RightsManagementFailureCode.MetadataNotSet:
                    result=SRID.RmExceptionMetadataNotSet; break;
                case RightsManagementFailureCode.RevocationInfoNotSet:
                    result=SRID.RmExceptionRevocationInfoNotSet; break;
                case RightsManagementFailureCode.InvalidTimeInfo:
                    result=SRID.RmExceptionInvalidTimeInfo; break;
                case RightsManagementFailureCode.RightNotSet:
                    result=SRID.RmExceptionRightNotSet; break;
                case RightsManagementFailureCode.LicenseBindingToWindowsIdentityFailed:
                    result=SRID.RmExceptionLicenseBindingToWindowsIdentityFailed; break;
                case RightsManagementFailureCode.InvalidIssuanceLicenseTemplate:
                    result=SRID.RmExceptionInvalidIssuanceLicenseTemplate; break;
                case RightsManagementFailureCode.InvalidKeyLength:
                    result=SRID.RmExceptionInvalidKeyLength; break;
                case RightsManagementFailureCode.ExpiredOfficialIssuanceLicenseTemplate:
                    result=SRID.RmExceptionExpiredOfficialIssuanceLicenseTemplate; break;
                case RightsManagementFailureCode.InvalidClientLicensorCertificate:
                    result=SRID.RmExceptionInvalidClientLicensorCertificate; break;
                case RightsManagementFailureCode.HidInvalid:
                    result=SRID.RmExceptionHidInvalid; break;
                case RightsManagementFailureCode.EmailNotVerified:
                    result=SRID.RmExceptionEmailNotVerified; break;
                case RightsManagementFailureCode.DebuggerDetected:
                    result=SRID.RmExceptionDebuggerDetected; break;
                case RightsManagementFailureCode.InvalidLockboxType:
                    result=SRID.RmExceptionInvalidLockboxType; break;
                case RightsManagementFailureCode.InvalidLockboxPath:
                    result=SRID.RmExceptionInvalidLockboxPath; break;
                case RightsManagementFailureCode.InvalidRegistryPath:
                    result=SRID.RmExceptionInvalidRegistryPath; break;
                case RightsManagementFailureCode.NoAesCryptoProvider:
                    result=SRID.RmExceptionNoAesCryptoProvider; break;
                case RightsManagementFailureCode.GlobalOptionAlreadySet:
                    result=SRID.RmExceptionGlobalOptionAlreadySet; break;
                case RightsManagementFailureCode.OwnerLicenseNotFound:
                    result=SRID.RmExceptionOwnerLicenseNotFound; break;
                default:
                    return null;
            }
            return SR.Get(result);
        }
    }
}



