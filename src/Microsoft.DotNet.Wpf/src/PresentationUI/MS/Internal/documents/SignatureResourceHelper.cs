// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//    SignatureResourceHelper is a helper class used to get resources.
using System;
using System.Collections;
using Drawing = System.Drawing;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.TrustUI;
using System.Security;

using MS.Internal.Documents.Application;

namespace MS.Internal.Documents
{
    internal static class SignatureResourceHelper
    {
        #region Internal Methods
        //--------------------------------------------------------------------------------
        //  Internal Methods
        //--------------------------------------------------------------------------------

        /// <summary>
        /// This will return the resources based on the overall status of the document.
        /// </summary>
        /// <param name="status">The overall status of the document.</param>
        /// <returns>The document level resources.</returns>
        internal static DocumentStatusResources GetDocumentLevelResources(
            SignatureStatus status)
        {
            DocumentStatusResources docSigStatusResources = new DocumentStatusResources();

            // Set the text representation for the status.
            switch (status)
            {
                case SignatureStatus.Valid:
                    docSigStatusResources.Text = SR.Get(SRID.DocumentSignatureManagerValid);
                    docSigStatusResources.ToolTip = SR.Get(SRID.DocumentSignatureManagerAppliedToolTip);
                    break;
                case SignatureStatus.Invalid:
                    docSigStatusResources.Text = SR.Get(SRID.DocumentSignatureManagerInvalid);
                    docSigStatusResources.ToolTip = SR.Get(SRID.DocumentSignatureManagerAppliedToolTip);
                    break;
                case SignatureStatus.NotSigned:
                    docSigStatusResources.Text = String.Empty;
                    docSigStatusResources.ToolTip = SR.Get(SRID.DocumentSignatureManagerDefaultToolTip);
                    break;                
                default: // SignatureStatus.Unknown or SignatureStatus.Undetermined
                         // In this case signatures have been applied to the document, but
                         // the validity of the signatures is not yet known.
                    docSigStatusResources.Text = SR.Get(SRID.DocumentSignatureManagerUndetermined);
                    docSigStatusResources.ToolTip = SR.Get(SRID.DocumentSignatureManagerAppliedToolTip);
                    break;
            }

            // Set the image representation for the status.
            docSigStatusResources.Image = GetDrawingBrushFromStatus(status);
            return docSigStatusResources;
        }

        /// <summary>
        /// Get the Image icon for the status.
        /// </summary>
        /// <param name="sigStatus">Requested signature status</param>
        /// <param name="certStatus">Requested certificate status</param>
        /// <returns>A Image on success (valid status, DrawingBrush found), null 
        /// otherwise.</returns>
        internal static Drawing.Image GetImageFromStatus(
            int height,
            int width,
            SignatureStatus sigStatus,
            CertificatePriorityStatus certStatus)
        {
            // TODO: #1239315 - Mongoose: Restore Dynamic Rendering of Icons from DrawingBrush

            // If the signature is okay, but the certificate cannot be trusted then display
            // invalid signature image.
            if ((sigStatus == SignatureStatus.Valid) && (certStatus != CertificatePriorityStatus.Ok))
            {
                sigStatus = SignatureStatus.Invalid;
            }

            string resourceName = string.Format(
                CultureInfo.InvariantCulture,
                @"{0}_{1}x{2}",
                sigStatus.ToString(),
                height,
                width);
            return (Drawing.Image)Resources.ResourceManager.GetObject(resourceName);
        }

        /// <summary>
        /// Will return the resources for the provided digital signature.
        /// </summary>
        /// <param name="signature">The signature to get resources for.</param>
        /// <returns>The signature resources.</returns>
        internal static SignatureResources GetResources(DigitalSignature signature, CertificatePriorityStatus certStatus)
        {
            const int defaultHeight = 35;
            const int defaultWidth = 35;

            SignatureResources resources = new SignatureResources();
            string none = SR.Get(SRID.SignatureResourceHelperNone);

            resources._displayImage = GetImageFromStatus(
                defaultHeight, defaultWidth, signature.SignatureState, certStatus);
            resources._location = 
                string.IsNullOrEmpty(signature.Location) ? none : signature.Location;
            resources._reason = 
                string.IsNullOrEmpty(signature.Reason) ? none : signature.Reason;
            resources._signBy = GetFormattedDate(signature.SignedOn);
            resources._subjectName = signature.SubjectName;
            resources._summaryMessage = GetSummaryMessage(signature, certStatus);

            Trace.SafeWrite(
                Trace.Rights,
                "Resources generated for {0} summary: {1}",
                resources._subjectName,
                resources._summaryMessage);

            return resources;
        }
        #endregion Internal Methods

        #region Private Methods
        /// <summary>
        /// Get the DrawingBrush icon for the status.
        /// </summary>
        /// <param name="status">Requested status</param>
        /// <returns>A DrawingBrush on success (valid status, DrawingBrush found), null 
        /// otherwise.</returns>
        private static DrawingBrush GetDrawingBrushFromStatus(SignatureStatus sigStatus)
        {
            if (_brushResources == null)
            {
                // Get the entire list of SignatureStatus values.
                Array statusList = Enum.GetValues(typeof(SignatureStatus));

                // Construct the array to hold brush references.
                _brushResources = new DrawingBrush[statusList.Length];

                // To find the DrawingBrushes in the theme resources we need a
                // FrameworkElement. TextBlock was used as it appears to have a very small
                // footprint, and won't take long to construct.  The actual
                // FrameworkElement doesn't matter as long as we have an instance to one
                _frameworkElement = new TextBlock();
            }

            if ((_brushResources != null) && (_frameworkElement != null))
            {
                int index = (int)sigStatus;

                // If there is no cached value of the requested DrawingBrush, then find
                // it in the Resources.
                if (_brushResources[index] == null)
                {
                    // Determine resource name.
                    string resourceName = "PUISignatureStatus"
                        + Enum.GetName(typeof(SignatureStatus), sigStatus)
                        + "BrushKey";

                    // Acquire reference to the brush.
                    object resource = _frameworkElement.FindResource(
                        new ComponentResourceKey(
                            typeof(PresentationUIStyleResources), resourceName));

                    // Set cache value for the brush.
                    _brushResources[index] = resource as DrawingBrush;
                }
                return _brushResources[index];
            }

            return null;
        }
        /// <summary>
        /// Builds the summary message.
        /// </summary>
        /// <param name="signature">A DigitalSignature</param>
        /// <returns>A summary message.</returns>
        private static string GetSummaryMessage(DigitalSignature signature, CertificatePriorityStatus certStatus)
        {
            if (signature == null)
            {
                return string.Empty;
            }

            // Setup the location text.  If not currently set, replace with the
            // string "<none>" to denote that no value was set.
            string location = (String.IsNullOrEmpty(signature.Location)) ?
                SR.Get(SRID.SignatureResourceHelperNone) : signature.Location;

            string result = String.Empty;

            switch (signature.SignatureState)
            {
                case SignatureStatus.Valid:
                case SignatureStatus.Invalid:
                case SignatureStatus.Unverifiable:
                    // Verify that if the signature is valid, it has a certificate
                    Invariant.Assert(
                        !(signature.SignatureState == SignatureStatus.Valid && signature.Certificate == null),
                        SR.Get(SRID.SignatureResourceHelperMissingCertificate));

                    // Create the signature status message
                    string sigSummary = string.Format(CultureInfo.CurrentCulture,
                        SR.Get(SRID.SignatureResourceHelperSummaryBreakLine),
                        GetSignatureSummaryMessage(signature.SignatureState, certStatus));

                    // Create the certificate status message (if required)
                    string certSummary = String.Empty;
                    if (certStatus != CertificatePriorityStatus.Ok)
                    {
                        certSummary = string.Format(CultureInfo.CurrentCulture,
                            SR.Get(SRID.SignatureResourceHelperSummaryBreakLine),
                            GetCertificateSummaryMessage(certStatus));
                    }

                    // Create the summary message using the signature and certificate messages
                    // along with details from the current signature.
                    result = string.Format(CultureInfo.CurrentCulture,
                        SR.Get(SRID.SignatureResourceHelperSummaryFormat),
                        sigSummary,
                        certSummary,
                        signature.SubjectName,
                        signature.SignedOn,
                        location);

                    break;

                case SignatureStatus.NotSigned:
                    // Create the summary message using signature information
                    result = string.Format(CultureInfo.CurrentCulture,
                        SR.Get(SRID.SignatureResourceHelperValidSigSummaryPending),
                        signature.SubjectName,
                        GetFormattedDate(signature.SignedOn),
                        location);
                    break;                            
            }

            return result;
        }

        /// <summary>
        /// Acquire the UI message associated with the certificate status
        /// </summary>
        /// <param name="certStatus">The status to represent</param>
        /// <returns>The string associated with the status, otherwise String.Empty.</returns>
        private static string GetCertificateSummaryMessage(CertificatePriorityStatus certStatus)
        {
            string message = String.Empty;
            switch (certStatus)
            {
                case CertificatePriorityStatus.Ok :
                    message = SR.Get(SRID.SignatureResourceHelperCertificateStatusOk);
                    break;
                case CertificatePriorityStatus.Corrupted :
                    message = SR.Get(SRID.SignatureResourceHelperCertificateStatusCorrupted);
                    break;
                case CertificatePriorityStatus.CannotBeVerified :
                    message = SR.Get(SRID.SignatureResourceHelperCertificateStatusCannotBeVerified);
                    break;
                case CertificatePriorityStatus.IssuerNotTrusted :
                    message = SR.Get(SRID.SignatureResourceHelperCertificateStatusIssuerNotTrusted);
                    break;
                case CertificatePriorityStatus.Revoked :
                    message = SR.Get(SRID.SignatureResourceHelperCertificateStatusRevoked);
                    break;
                case CertificatePriorityStatus.Expired :
                    message = SR.Get(SRID.SignatureResourceHelperCertificateStatusExpired);
                    break;
                case CertificatePriorityStatus.NoCertificate :
                    message = SR.Get(SRID.SignatureResourceHelperCertificateStatusNoCertificate);
                    break;
                case CertificatePriorityStatus.Verifying :
                    message = SR.Get(SRID.SignatureResourceHelperCertificateStatusVerifying);
                    break;
            }

            return message;
        }

        /// <summary>
        /// Acquire the UI message associated with the signature status
        /// </summary>
        /// <param name="sigStatus">The signature status to represent</param>
        /// <param name="certStatus">The certificate status for the signature</param>
        /// <returns>The string associated with the status, otherwise String.Empty.</returns>
        private static string GetSignatureSummaryMessage(SignatureStatus sigStatus, CertificatePriorityStatus certStatus)
        {
            string message = String.Empty;

            if (sigStatus == SignatureStatus.Valid)
            {
                message = (certStatus == CertificatePriorityStatus.Ok) ?
                    SR.Get(SRID.SignatureResourceHelperSignatureStatusValid) : // Cert valid
                    SR.Get(SRID.SignatureResourceHelperSignatureStatusValidCertInvalid); // Cert invalid
            }
            else if (sigStatus == SignatureStatus.Unverifiable)
            {
                message = SR.Get(SRID.SignatureResourceHelperSignatureStatusUnverifiable);
            }   
            else
            {
                message = SR.Get(SRID.SignatureResourceHelperSignatureStatusInvalid);
            }

            return message;
        }

        /// <summary>
        /// Will format the date for display.
        /// </summary>
        /// <param name="date">Either null or a valid date.</param>
        /// <returns>The short date or 'none' if the value was null.</returns>
        private static string GetFormattedDate(Nullable<DateTime> date)
        {
            string none = SR.Get(SRID.SignatureResourceHelperNone);

            return date == null ? 
                none : 
                String.Format(
                    CultureInfo.CurrentCulture,
                    ((DateTime)date).ToShortDateString());
        }
        #endregion Private Methods

        #region Private Fields
        /// <summary>
        /// Caches DrawingBrushes loaded from styles.
        /// </summary>
        private static DrawingBrush[]       _brushResources;
        /// <summary>
        /// Used to search resources.
        /// </summary>
        private static FrameworkElement _frameworkElement;
        #endregion
    }

    /// <summary>
    /// Internal representation of the document's signature status
    /// </summary>
    internal struct SignatureResources
    {
        public System.Drawing.Image _displayImage;
        public string _subjectName;
        public string _summaryMessage;

        public string _reason;
        public string _location;
        public string _signBy;

        /// <summary>
        /// Provide a ToString implementation so that the ListItems in the
        /// SignatureSummary dialog provide text information for UIAutomation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format(
                CultureInfo.CurrentCulture,
                SR.Get(SRID.SignatureResourcesFormatForAccessibility), 
                _summaryMessage, 
                _subjectName, 
                _reason, 
                _location, 
                _signBy);
        }
    }
}
