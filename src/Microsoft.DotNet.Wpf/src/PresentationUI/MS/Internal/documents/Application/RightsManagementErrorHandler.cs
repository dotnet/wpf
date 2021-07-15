// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//  This file contains a class that handles all Rights Management errors.

using System;
using System.Security.RightsManagement;
using System.Windows.TrustUI;

namespace MS.Internal.Documents.Application
{
    /// <summary>
    /// Enumeration that represents the possible RM operations in which an
    /// error could need to be handled. This is useful to display the proper UI
    /// for each situation.
    /// </summary>
    internal enum RightsManagementOperation : int
    {
        /// <summary>
        /// Initializing the RM system
        /// </summary>
        Initialize,

        /// <summary>
        /// Opening (decrypting) an RM-protected document
        /// </summary>
        Decrypt,

        /// <summary>
        /// Activating a passport account for RM
        /// </summary>
        PassportActivation,

        /// <summary>
        /// Loading or signing using a template
        /// </summary>
        TemplateAccess,
        
        /// <summary>
        /// Other (non-critical) operation
        /// </summary>
        Other,
    }

    /// <summary>
    /// Handles all Rights Management subsystem errors and displays correct
    /// error messages as necessary.
    /// </summary>
    internal static class RightsManagementErrorHandler
    {
        #region Internal Methods
        //------------------------------------------------------
        // Internal Methods
        //------------------------------------------------------

        /// <summary>
        /// Handles the given exception if possible, showing the correct UI if
        /// necessary.  If the operation is a critical one (i.e. the
        /// application cannot continue without performing the operation) and
        /// the exception cannot be handled cleanly, the method rethrows an
        /// appropriate exception.
        /// </summary>
        /// <param name="operation">The operation being performed</param>
        /// <param name="exception">The exception to try to handle</param>
        /// <returns>Whether or not the exception was handled</returns>
        internal static bool HandleOrRethrowException(
            RightsManagementOperation operation,
            Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            Trace.SafeWrite(
                Trace.Rights,
                "ErrorHandler: Hit exception:\n{0}",
                exception);

            bool askUser = false;
            bool fatal = true;

            string message = ParseException(
                operation,
                exception,
                out askUser,
                out fatal);

            // If no message for a message box is provided
            if (string.IsNullOrEmpty(message))
            {
                // If the exception won't be rethrown, show a generic error
                // message; otherwise it is something we don't know how to
                // handle, and we will let it kill the application
                if (!fatal || !IsCriticalOperation(operation))
                {
                    System.Windows.MessageBox.Show(
                        SR.Get(SRID.RightsManagementWarnErrorGenericFailure),
                        SR.Get(SRID.RightsManagementWarnErrorTitle),
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
            }

            // If there is a message provided and the user gets to decide the
            // correct course of action
            else if (askUser)
            {
                // Show a message box to ask the user
                System.Windows.MessageBoxResult result =
                    System.Windows.MessageBox.Show(
                        message,
                        SR.Get(SRID.RightsManagementWarnErrorTitle),
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning);

                // Consider the error fatal if the user declined the mitigation
                // offered in the message box
                fatal = fatal && (result != System.Windows.MessageBoxResult.Yes);
            }

            // If there is a message provided, but the user has no choice
            else
            {
                // Display the error message box
                System.Windows.MessageBox.Show(
                    message,
                    SR.Get(SRID.RightsManagementWarnErrorTitle),
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }

            // If the exception is fatal and the operation is critical, we
            // should throw an XPS Viewer exception to be handled higher up
            // the stack
            if (fatal && IsCriticalOperation(operation))
            {
                Trace.SafeWrite(
                    Trace.Rights,
                    "ErrorHandler: Could not handle exception. Rethrowing.");

                throw new XpsViewerException(
                    SR.Get(SRID.XpsViewerRightsManagementException),
                    exception);
            }

            Trace.SafeWrite(
                Trace.Rights,
                "ErrorHandler: Handled exception.");

            return !fatal;
        }

        #endregion Internal Methods

        #region Private Methods
        //------------------------------------------------------
        // Private Methods
        //------------------------------------------------------

        /// <summary>
        /// Checks whether or not the given operation is a critical one. A
        /// critical operation is defined as an operation which should stop the
        /// application if it does not complete cleanly.
        /// </summary>
        /// <param name="operation">The operation to check</param>
        /// <returns></returns>
        private static bool IsCriticalOperation(
            RightsManagementOperation operation)
        {
            // The only non-critical operation is the generic "other" operation
            return operation != RightsManagementOperation.Other;
        }

        /// <summary>
        /// Gets the appropriate error message corresponding to an exception
        /// and detects whether the failure is fatal to the current operation
        /// (i.e. the user cannot take any action to circumvent the failure).
        /// If the user should be given the option to take action to circumvent
        /// the failure, true will be returned as the askUser parameter. If the
        /// user declines to take action, the exception is fatal if the fatal
        /// parameter is true. This function basically dispatches exceptions to
        /// a more specific ParseException function.
        /// </summary>
        /// <param name="operation">The current operation</param>
        /// <param name="exception">The exception to parse</param>
        /// <param name="askUser">Whether or not the user should be given a
        /// choice to take action to prevent the failure</param>
        /// <param name="fatal">Whether or not the failure represented is fatal
        /// </param>
        /// <returns>A user-friendly message corresponding to the exception
        /// </returns>
        private static string ParseException(
            RightsManagementOperation operation,
            Exception exception,
            out bool askUser,
            out bool fatal)
        {
            askUser = false;
            fatal = true;

            // Filter out Template related errors, as they all should receive an
            // "Invalid Template" type of message.
            if (operation == RightsManagementOperation.TemplateAccess)
            {
                return ParseTemplateExceptions(
                    exception,
                    out askUser,
                    out fatal);
            }

            // Each handled type of exception has a different handler function
            else if (exception is RightsManagementException)
            {
                return ParseRightsManagementException(
                    operation,
                    (RightsManagementException)exception,
                    out askUser,
                    out fatal);
            }

            // Any other exception is unknown, so there is no message string
            // and it is assumed to be fatal
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Determines the error that has happened, and the appropriate
        /// message to display.
        /// </summary>
        /// <param name="operation">The current operation</param>
        /// <param name="exception">The exception to parse</param>
        /// <param name="askUser">Whether or not the user should be given a
        /// choice to take action to prevent the failure</param>
        /// <param name="fatal">Whether or not the failure represented is fatal
        /// </param>
        /// <returns>A user-friendly message corresponding to the exception
        /// </returns>
        private static string ParseTemplateExceptions(
            Exception exception,
            out bool askUser,
            out bool fatal)
        {
            string result = String.Empty;

            // Most errors with applying templates should be user prompts, so
            // set the default askUser and fatal this way.
            askUser = false;
            fatal = false;

            // Check the exception type to determine the appropriate message and action
            if ((exception is RightsManagementException) ||
                // FileFormatException added to handle the case when the template
                // file loaded fine, but was empty.  In this case we'd rather show
                // an invalid template message, since it's not file related, but
                // content related.
                (exception is System.IO.FileFormatException))
            {
                result = SRID.RightsManagementWarnErrorInvalidTemplate;
            }
            // These remaining exception types are related to File I/O, all of which
            // should be handled with an error message and fail the signing process.
            // They should not crash the application.
            else if ((exception is ArgumentException) ||
                     (exception is ArgumentNullException) ||
                     (exception is NotSupportedException) ||
                     (exception is System.IO.PathTooLongException) ||
                     (exception is System.IO.IOException) ||
                     (exception is UnauthorizedAccessException) ||
                     (exception is System.IO.FileNotFoundException) ||
                     (exception is System.IO.DirectoryNotFoundException))
            {
                result = SRID.RightsManagementWarnErrorFailedToLoadTemplate;
            }
            else
            {
                fatal = true;
            }

            if (result == null)
            {
                return null;
            }
            else
            {
                return SR.Get(result);
            }
        }

        /// <summary>
        /// Gets the error message corresponding to a Rights Management
        /// exception and detects whether it is fatal and whether the user
        /// should be offered a mitigation. This is a specialized version of
        /// the ParseException function.
        /// </summary>
        /// <param name="operation">The current operation</param>
        /// <param name="exception">The exception to parse</param>
        /// <param name="askUser">Whether or not the user should be given a
        /// choice to take action to prevent the failure</param>
        /// <param name="fatal">Whether or not the failure represented is fatal
        /// </param>
        /// <returns>A user-friendly message corresponding to the exception
        /// </returns>
        private static string ParseRightsManagementException(
            RightsManagementOperation operation,
            RightsManagementException rmException,
            out bool askUser,
            out bool fatal)
        {
            askUser = false;
            fatal = true;

            RightsManagementFailureCode failureCode = rmException.FailureCode;

            string result = null;
            switch (failureCode)
            {
                case RightsManagementFailureCode.InvalidLicense:
                    if (operation == RightsManagementOperation.TemplateAccess)
                    {
                        result = SRID.RightsManagementWarnErrorInvalidTemplate;
                    }
                    else
                    {
                        result = SRID.RightsManagementWarnErrorConfigurationError;
                    }
                    break;
                case RightsManagementFailureCode.InvalidLicenseSignature:
                    if (operation == RightsManagementOperation.Initialize)
                    {
                        result = SRID.RightsManagementWarnErrorConfigurationError;
                    }
                    else
                    {
                        result = SRID.RightsManagementWarnErrorInvalidContent;
                    }

                    break;
                case RightsManagementFailureCode.RightNotGranted:
                    result = SRID.RightsManagementWarnErrorNoPermission;
                    askUser = true;
                    break;
                case RightsManagementFailureCode.InvalidVersion:
                    result = SRID.RightsManagementWarnErrorConfigurationError;
                    break;
                case RightsManagementFailureCode.ClockRollbackDetected:
                    result = SRID.RightsManagementWarnErrorClockModified;
                    break;
                case RightsManagementFailureCode.BindValidityTimeViolated:
                    result = SRID.RightsManagementWarnErrorExpiredPermission;
                    break;
                case RightsManagementFailureCode.BrokenCertChain:
                    result = SRID.RightsManagementWarnErrorConfigurationError;
                    break;
                case RightsManagementFailureCode.BindPolicyViolation:
                    result = SRID.RightsManagementWarnErrorNoPermission;
                    askUser = true;
                    break;
                case RightsManagementFailureCode.ManifestPolicyViolation:
                    result = SRID.RightsManagementWarnErrorConfigurationError;
                    break;
                case RightsManagementFailureCode.BindRevokedLicense:
                    result = SRID.RightsManagementWarnErrorNoPermission;
                    askUser = true;
                    break;
                case RightsManagementFailureCode.BindRevokedIssuer:
                    result = SRID.RightsManagementWarnErrorNoPermission;
                    askUser = true;
                    break;
                case RightsManagementFailureCode.BindRevokedPrincipal:
                    result = SRID.RightsManagementWarnErrorNoPermission;
                    askUser = true;
                    break;
                case RightsManagementFailureCode.BindRevokedResource:
                    result = SRID.RightsManagementWarnErrorNoPermission;
                    askUser = true;
                    break;
                case RightsManagementFailureCode.BindRevokedModule:
                    result = SRID.RightsManagementWarnErrorConfigurationError;
                    break;
                case RightsManagementFailureCode.BindAccessUnsatisfied:
                    result = SRID.RightsManagementWarnErrorNoPermission;
                    askUser = true;
                    break;
                case RightsManagementFailureCode.BindMachineNotFoundInGroupIdentity:
                    result = SRID.RightsManagementWarnErrorConfigurationError;
                    break;
                case RightsManagementFailureCode.BindRevocationListStale:
                    result = SRID.RightsManagementWarnErrorNoPermission;
                    askUser = true;
                    break;
                case RightsManagementFailureCode.BindNoApplicableRevocationList:
                    result = SRID.RightsManagementWarnErrorNoPermission;
                    askUser = true;
                    break;
                case RightsManagementFailureCode.LicenseAcquisitionFailed:
                    result = SRID.RightsManagementWarnErrorConfigurationError;
                    break;
                case RightsManagementFailureCode.NoDistributionPointUrlFound:
                    result = SRID.RightsManagementWarnErrorInvalidContent;
                    break;
                case RightsManagementFailureCode.NoConnect:
                    result = SRID.RightsManagementWarnErrorServerError;
                    break;
                case RightsManagementFailureCode.ActivationFailed:
                    result = SRID.RightsManagementWarnErrorConfigurationError;
                    break;
                case RightsManagementFailureCode.Aborted:
                    fatal = (operation != RightsManagementOperation.PassportActivation);
                    break;
                case RightsManagementFailureCode.OutOfQuota:
                    result = SRID.RightsManagementWarnErrorNoPermission;
                    askUser = true;
                    break;
                case RightsManagementFailureCode.AuthenticationFailed:
                    fatal = false;
                    break;
                case RightsManagementFailureCode.ServerError:
                    result = SRID.RightsManagementWarnErrorServerError;
                    break;
                case RightsManagementFailureCode.HidCorrupted:
                    result = SRID.RightsManagementWarnErrorConfigurationError;
                    break;
                case RightsManagementFailureCode.InvalidServerResponse:
                    result = SRID.RightsManagementWarnErrorServerError;
                    break;
                case RightsManagementFailureCode.ServiceNotFound:
                    result = SRID.RightsManagementWarnErrorServerError;
                    break;
                case RightsManagementFailureCode.UseDefault:
                    result = SRID.RightsManagementWarnErrorConfigurationError;
                    break;
                case RightsManagementFailureCode.ServerNotFound:
                    result = SRID.RightsManagementWarnErrorServerError;
                    break;
                case RightsManagementFailureCode.InvalidEmail:
                    fatal = false;
                    break;
                case RightsManagementFailureCode.ValidityTimeViolation:
                    result = SRID.RightsManagementWarnErrorExpiredPermission;
                    break;
                case RightsManagementFailureCode.OutdatedModule:
                    result = SRID.RightsManagementWarnErrorConfigurationError;
                    break;
                case RightsManagementFailureCode.ServiceMoved:
                    result = SRID.RightsManagementWarnErrorServerError;
                    break;
                case RightsManagementFailureCode.ServiceGone:
                    result = SRID.RightsManagementWarnErrorServerError;
                    break;
                case RightsManagementFailureCode.AdEntryNotFound:
                    fatal = false;
                    break;
                case RightsManagementFailureCode.NotAChain:
                    result = SRID.RightsManagementWarnErrorConfigurationError;
                    break;
                case RightsManagementFailureCode.RequestDenied:
                    result = SRID.RightsManagementWarnErrorTemporaryActivationNotSupported;
                    fatal = false;
                    break;
                case RightsManagementFailureCode.LicenseBindingToWindowsIdentityFailed:
                    result = SRID.RightsManagementWarnErrorNoPermission;
                    askUser = true;
                    break;
                case RightsManagementFailureCode.InvalidIssuanceLicenseTemplate:
                    result = SRID.RightsManagementWarnErrorInvalidTemplate;
                    fatal = false;
                    break;
                case RightsManagementFailureCode.ExpiredOfficialIssuanceLicenseTemplate:
                    result = SRID.RightsManagementWarnErrorInvalidTemplate;
                    fatal = false;
                    break;
                case RightsManagementFailureCode.InvalidClientLicensorCertificate:
                    result = SRID.RightsManagementWarnErrorConfigurationError;
                    break;
                case RightsManagementFailureCode.HidInvalid:
                    result = SRID.RightsManagementWarnErrorConfigurationError;
                    break;
                case RightsManagementFailureCode.EmailNotVerified:
                    fatal = false;
                    break;
                case RightsManagementFailureCode.DebuggerDetected:
                    result = SRID.RightsManagementWarnErrorDebuggerDetected;
                    break;
                case RightsManagementFailureCode.InvalidLockboxType:
                    result = SRID.RightsManagementWarnErrorConfigurationError;
                    break;
                case RightsManagementFailureCode.InvalidLockboxPath:
                    result = SRID.RightsManagementWarnErrorConfigurationError;
                    break;
                case RightsManagementFailureCode.NoAesCryptoProvider:
                    result = SRID.RightsManagementWarnErrorConfigurationError;
                    break;
                default:
                    return null;
            }

            if (result == null)
            {
                return null;
            }
            else
            {
                return SR.Get(result);
            }
        }

        #endregion Private Methods
    }
}



