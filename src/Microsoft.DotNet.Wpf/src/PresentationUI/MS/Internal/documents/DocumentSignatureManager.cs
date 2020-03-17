// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Description: 
//    DocumentSignatureManager is an internal API for Mongoose to deal with Digital Signatures.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Packaging;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows.TrustUI;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;

using MS.Internal.Documents.Application;
using MS.Internal.PresentationUI;

namespace MS.Internal.Documents
{
    /// <summary>
    /// DocumentSignatureManager is a internal Avalon class used to expose the DigSig Document API 
    /// </summary>
    [FriendAccessAllowed]
    internal sealed class DocumentSignatureManager
    {
        #region Constructors
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// The constructor
        /// </summary>
        private DocumentSignatureManager(IDigitalSignatureProvider digSigProvider)
        {
            if (digSigProvider != null)
            {
                DigitalSignatureProvider = digSigProvider;
            }
            else
            {
                throw new ArgumentNullException("digSigProvider");
            }

            _changeLog = new List<ChangeLogEntity>();

            _digSigSigResources = new Dictionary<SignatureResources, DigitalSignature>();

            DocumentRightsManagementManager rightsManagementManager =
                DocumentRightsManagementManager.Current;

            if(rightsManagementManager != null)
            {
                rightsManagementManager.RMPolicyChange += 
                    new DocumentRightsManagementManager.RMPolicyChangeHandler(OnRMPolicyChanged);
                rightsManagementManager.Evaluate(); 
            }

            // notify the documentmanager when the signatures change
            SignaturesChanged += DocumentManager.OnModify;
        }

        #endregion Constructors  
        
        #region Public Event
        //------------------------------------------------------
        //
        //  Public Event
        //
        //------------------------------------------------------

        public event EventHandler SignaturesChanged;
        public event SignatureStatusChangeHandler SignatureStatusChange;

        #endregion Public Event

        #region Internal Methods
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Forces an Evaluate which will result in SignatureStatus and
        /// SignaturePolicy event being fired.
        /// </summary>
        internal void Evaluate()
        {
            Trace.SafeWrite(
                Trace.Signatures,
                "Evaluate called.");

            SignatureStatus calcSigStatus = SignatureStatus.Unknown;
            SignaturePolicy calcSigPolicy = SignaturePolicy.AllowSigning |
                                            SignaturePolicy.ModifyDocumentProperties;
                          
            // If the document is not signed, we already know the signature
            // status of the document
            if (!IsSigned)
            {
                //There are no signatures applied to this document.
                calcSigStatus = SignatureStatus.NotSigned;
            }

            // Check to see if the certificates have been validated
            else if (!AreAllSignaturesVerified)
            {
                calcSigStatus = SignatureStatus.Undetermined;
                calcSigPolicy = SignaturePolicy.AllowNothing;
            }

            // Otherwise we should look at all the signatures in the document
            else
            {
                bool areAllSignaturesValid = true;

                // Verify that the document is signable before we continue.                        
                if (!VerifySignability())
                { 
                    // If the document does not meet the signing criteria, the signatures are 
                    // not considered valid.
                    areAllSignaturesValid = false;

                    // Walk the list of signatures applied to the package and set their state
                    // to "Unverifiable."
                    foreach (DigitalSignature digitalSignature in DigitalSignatureProvider.Signatures)
                    {
                        digitalSignature.SignatureState = SignatureStatus.Unverifiable;
                    }
                }
                else
                {
                    // Check all the signatures applied to the package to see
                    // if they are all valid. At this point the signatures should
                    // all have been verified, and the certificates associated with
                    // them should all have been validated.
                    foreach (DigitalSignature digitalSignature in DigitalSignatureProvider.Signatures)
                    {
                        bool valid =
                            (digitalSignature.SignatureState == SignatureStatus.Valid) &&
                            (GetCertificateStatusFromTable(digitalSignature) == CertificatePriorityStatus.Ok);

                        if (valid)
                        {
                            // Add the restrictions on the policy imposed by the
                            // new signature only if it is valid.
                            calcSigPolicy =
                                AddRestrictionsFromSignature(calcSigPolicy, digitalSignature);
                        }

                        areAllSignaturesValid &=
                            (valid ||
                            (digitalSignature.SignatureState == SignatureStatus.NotSigned));
                    }

                    // If the policy does not allow modifying document properties and the properties have been
                    // changed, then the signatures have been invalidated.
                    if (!IsAllowedByPolicy(calcSigPolicy, SignaturePolicy.ModifyDocumentProperties) &&
                        !DocumentProperties.Current.VerifyPropertiesUnchanged())
                    {
                        areAllSignaturesValid = false;

                        // Walk the list of signatures applied to the package and set their state
                        // to "Invalid" if necessary.
                        foreach (DigitalSignature digitalSignature in DigitalSignatureProvider.Signatures)
                        {
                            if (digitalSignature.IsDocumentPropertiesRestricted)
                            {
                                digitalSignature.SignatureState = SignatureStatus.Invalid;
                            }
                        }
                    }
                }

                if (areAllSignaturesValid)
                {
                    calcSigStatus = SignatureStatus.Valid;
                }
                else
                {
                    calcSigStatus = SignatureStatus.Invalid;
                }
            }

            Invariant.Assert(
                calcSigStatus != SignatureStatus.Unknown,
                "We should have determined a signature status by now.");

            //Fire the events.
            OnSignatureStatusChange(calcSigStatus);
            _signaturePolicy.Value = calcSigPolicy;
        }

        /// <summary>
        /// If the document is signed, this function verifies the signatures and
        /// validates all the associated certificates.
        /// </summary>
        /// <remarks>
        /// This function performs all the signature verification that must
        /// happen on the main thread. This includes loading the signatures and
        /// verifying the hashes. Certificate validation can and does happen on
        /// a background thread.
        /// </remarks>
        internal void VerifySignatures()
        {
            if (AreAllSignaturesVerified)
            {
                // If the certificates have already been verified, exit
                return;
            }
            else if (!IsSigned)
            {
                // If the document isn't signed there is no certificate
                // validation to do, and we can initialize the (empty)
                // certificate status table for later use
                _certificateStatusTable =
                    new Dictionary<X509Certificate2, CertificatePriorityStatus>();
            }
            else
            {
                Trace.SafeWrite(
                    Trace.Signatures,
                    "Document loading complete; verifying signatures.");

                // Once the document has finished loading, we can safely verify all
                // the signatures in the package (i.e. compare hashes)
                DigitalSignatureProvider.VerifySignatures();

                // Retrieve and save all the certificates used
                IList<X509Certificate2> certificateList =
                    DigitalSignatureProvider.GetAllCertificates();

                StartCertificateStatusCheck(certificateList);
            }
        }

        /// <summary>
        /// ShowSignatureSummary:  Displays the DigSig Summary dialog.
        /// </summary>
        internal void ShowSignatureSummaryDialog()
        {
            IList<SignatureResources> sigResList = GetSignatureResourceList(false /*requestsOnly*/);

            System.Windows.Forms.IWin32Window parentWindow =
                DocumentApplicationDocumentViewer.Instance.RootBrowserWindow;

            SignatureSummaryDialog dialog = new SignatureSummaryDialog(
                sigResList,
                this,
                false /*Sig Request Dialog*/);
                dialog.ShowDialog(parentWindow);

            if (dialog != null)
            {
                dialog.Dispose();
            }
        }

        /// <summary>
        /// RequestSigners:  Displays the DigSig Request Signature dialog.
        /// </summary>
        internal void ShowSignatureRequestSummaryDialog()
        {
            //Check to see if package is Read Only or signed (we can't added request to a signed
            //document.
            //If the document does not meet the signing criteria, we alert the user and return.
            if (!VerifySignability())
            {
                System.Windows.MessageBox.Show(
                    SR.Get(SRID.DigitalSignatureMessageDocumentNotSignable),
                    SR.Get(SRID.DigitalSignatureMessageDocumentNotSignableTitle),
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Exclamation);
            }
            else if (IsSigningAllowed && !IsSigned)
            {
                IList<SignatureResources> sigResList = GetSignatureResourceList(true /*requestsOnly*/);

                System.Windows.Forms.IWin32Window parentWindow =
                    DocumentApplicationDocumentViewer.Instance.RootBrowserWindow;

                //Create and show the Summary Dialog in the Request signature mode.
                SignatureSummaryDialog dialog = new SignatureSummaryDialog(
                    sigResList,
                    this,
                    true /*Sig Request Dialog*/);
                    dialog.ShowDialog(parentWindow);

                if (dialog != null)
                {
                    dialog.Dispose();
                }
            }
            else
            {
                if (!IsSigningAllowed)
                {
                    System.Windows.MessageBox.Show(
                        SR.Get(SRID.DigitalSignatureWarnErrorRMSigningMessage),
                        SR.Get(SRID.DigitalSignatureWarnErrorSigningErrorTitle),
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Exclamation
                        );
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        SR.Get(SRID.DigitalSignatureWarnErrorReadOnlyNoMoreRequest),
                        SR.Get(SRID.DigitalSignatureWarnErrorSigningErrorTitle),
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Exclamation
                        );
                }
            }
        }

        /// <summary>
        /// Display the signing dialog parented from the root browser window
        /// without an accompanying signature request.
        /// </summary>
        internal void ShowSigningDialog()
        {
            ShowSigningDialog(
                DocumentApplicationDocumentViewer.Instance.RootBrowserWindow.Handle);
        }

        /// <summary>
        /// Display the signing dialog without an accompanying signature request.
        /// </summary>
        /// <param name="parentWindow">A handle to the parent window for the
        /// signing dialog</param>
        internal void ShowSigningDialog(IntPtr parentWindow)
        {
            ShowSigningDialog(
                parentWindow,
                null /*DigitalSignature digitalSignatureRequest*/);
        }

        /// <summary>
        /// Display the signing dialog.  This method optionally takes a SignatureRequest.
        /// The actual signing operation will take place in a callback from the dialog.
        /// </summary>
        /// <param name="digitalSignatureRequest">The request to be associated
        /// with the signature (can be null)</param>
        /// <param name="parentWindow">A handle to the window that will be the
        /// parent of the signing dialog</param>
        internal void ShowSigningDialog(
            IntPtr parentWindow,
            DigitalSignature digitalSignatureRequest)
        {
            // First check if signing is allowed by RM.  If not, show an error
            // dialog and exit.
            if (!IsSigningAllowed)
            {
                System.Windows.MessageBox.Show(
                    SR.Get(SRID.DigitalSignatureWarnErrorRMSigningMessage),
                    SR.Get(SRID.DigitalSignatureWarnErrorSigningErrorTitle),
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Exclamation);

                return;
            }

            // If the document does not meet the signing criteria, we alert the user and return.
            if (!VerifySignability())
            {
                System.Windows.MessageBox.Show(
                    SR.Get(SRID.DigitalSignatureMessageDocumentNotSignable),
                    SR.Get(SRID.DigitalSignatureMessageDocumentNotSignableTitle),
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Exclamation);

                return;
            }

            // Check the signature policy to see if a new signature will invalidate an
            // existing signature.
            if (IsSigned && !IsSigningAllowedByPolicy)
            {
                System.Windows.MessageBoxResult dialogResult = System.Windows.MessageBox.Show(
                    SR.Get(SRID.DigitalSignatureMessageActionInvalidatesSignature),
                    SR.Get(SRID.DigitalSignatureMessageSignNowTitle),
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Exclamation);

                if (dialogResult == System.Windows.MessageBoxResult.No)
                {
                    return;
                }
            }

            // Check to see if user is trying to add another signature when no
            // request spot are available.  If there are no request spots warn user.
            if (IsSigned && !HasRequests)
            {
                System.Windows.MessageBoxResult dialogResult = System.Windows.MessageBox.Show(
                    SR.Get(SRID.DigitalSignatureMessageSignNoPending),
                    SR.Get(SRID.DigitalSignatureMessageSignNowTitle),
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Exclamation);

                if (dialogResult == System.Windows.MessageBoxResult.No)
                {
                    return;
                }
            }
            // Check to see if user is trying to add another signature when
            // request spot are available but they aren't using it.  If there are 
            // no request spots warn user.
            else if (IsSigned && HasRequests && digitalSignatureRequest == null)
            {
                System.Windows.MessageBoxResult dialogResult = System.Windows.MessageBox.Show(
                    SR.Get(SRID.DigitalSignatureMessageSignPending),
                    SR.Get(SRID.DigitalSignatureMessageSignNowTitle),
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Exclamation);

                if (dialogResult == System.Windows.MessageBoxResult.No)
                {
                    return;
                }
            }

            // Check to see if we are signing a request.
            if (digitalSignatureRequest == null)
            {
                //This is not a signing request.  Now Check
                //to see if document has already been signed.  If it has been signed
                //then we don't want to allow user to define intent/location (definition)
                //on the signing dialog.  To prevent this we will create an empty
                //DigitalSignatureRequest.
                if (IsSigned)
                {
                    digitalSignatureRequest = new DigitalSignature();
                }
                else
                {
                    // The document hasn't yet been signed -- warn the user that he/she
                    // should add signature requests before signing and give the user the option
                    // to cancel
                    System.Windows.MessageBoxResult dialogResult = System.Windows.MessageBox.Show(
                        SR.Get(SRID.DigitalSignatureMessageAddRequestsBeforeSigning),
                        SR.Get(SRID.DigitalSignatureMessageAddRequestsBeforeSigningTitle),
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Exclamation);

                    // If the user canceled, stop the signing process.
                    if (dialogResult == System.Windows.MessageBoxResult.No)
                    {
                        return;
                    }
                }
            }

            // Get certificate selection from the user.  The requestAgain flag is set to
            // true if we need to prompt the user for another selection.  This happens only
            // if the user selects a smart card certificate but doesn't insert the card.
            bool requestAgain;
            X509Certificate2 x509Certificate2;

            do
            {
                requestAgain = false;

                // Show certificate picker dialog
                x509Certificate2 = ShowCertificatePickerDialog(parentWindow);

                // If the certificate is null, the user cancelled the selection, so exit.
                if (x509Certificate2 == null)
                {
                    return;
                }

                // Check if we can retrieve the private key.  This will alert us of the case
                // in which the user has selected a smart card certificate but selected
                // cancel when asked to insert the card by throwing a CryptoGraphicException.
                // If the user selects a non-smart card certificate, or selects a smart card
                // certificate and inserts the smart card, no exception will be thrown.
                try
                {
                    // We access the PrivateKey only to test if it can be retrieved -- the
                    // actual contents are never used.
                    // DDVSO: 194333 Adding support for CNG certificates. The default certificate template in Windows Server 2008+ is CNG
                    using (RSA rsa = x509Certificate2.GetRSAPrivateKey())
                    {
                        if(rsa == null)
                        {
                            using (DSA dsa = x509Certificate2.GetDSAPrivateKey())
                            {
                                if(dsa == null)
                                {
                                    using (ECDsa ecdsa = x509Certificate2.GetECDsaPrivateKey())
                                    {
                                        if(ecdsa == null)
                                        {
                                            // Get[Algorithm]PrivateKey methods would always have returned the private key if the PrivateKey property would
                                            // But Get[Algorithm]PrivateKey methods never throw but returns null in case of error during cryptographic operations
                                            // But we want exception to be thrown when an error occurs during a cryptographic operation so that we can redisplay the certificate picker
                                            AsymmetricAlgorithm testKey = x509Certificate2.PrivateKey;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (CryptographicException)
                {
                    // In this case the user selected a smart card key but did not insert
                    // the smart card.  We should redisplay the certificate picker to allow
                    // the user to select a different key.
                    requestAgain = true;
                }
            } while (requestAgain);

            SigningDialog dialog = new SigningDialog(
                x509Certificate2,
                digitalSignatureRequest,
                this);
            dialog.ShowDialog(NativeWindow.FromHandle(parentWindow));

            if (dialog != null)
            {
                dialog.Dispose();
            }
        }

        /// <summary>
        /// Gets a List of SignatureResources.
        /// </summary>
        internal IList<SignatureResources> GetSignatureResourceList(bool requestsOnly)
        {
            //Clear the map.
            _digSigSigResources.Clear();

            IList<SignatureResources> signResourcesList = new List<SignatureResources>();

            //Loop through all DigSigs
            foreach (DigitalSignature digSig in DigitalSignatureProvider.Signatures)
            {
                SignatureResources sigResources;

                //Is this signature a request signature?
                bool notSigned = digSig.SignatureState == SignatureStatus.NotSigned;

                //Filter out signed DigitalSignature if requester wants request signatures only
                if (!requestsOnly || notSigned)
                {
                    //Use the SignatureResourceHelper to get the strings to display 
                    //in the ListBox 
                    sigResources = SignatureResourceHelper.GetResources(
                        digSig,
                        GetCertificateStatusFromTable(digSig));

                    //Add to map and to list.
                    _digSigSigResources.Add(sigResources, digSig);
                    signResourcesList.Add(sigResources);
                }
            }

            return signResourcesList;
        }

        /// <summary>
        /// Signs the document with the given digital signature. This function        
        /// blocks the UI until the signing operation is complete.
        /// </summary>
        internal bool SignDocument(DigitalSignature digSig, Form parentDialog, bool isSaveAs)
        {
            bool rtn = false;

            //First place this new DigSig's Cert in the CertStatus Table as CertificatePriorityStatus.Ok
            //since user has selected this cert to use for signing.  We will not be checking the status of
            //this cert since the user selected it for signing.
            if (_certificateStatusTable != null && !_certificateStatusTable.ContainsKey(digSig.Certificate))
            {
                _certificateStatusTable.Add(digSig.Certificate, CertificatePriorityStatus.Ok);
            }

            //Show the progress dialog
            ProgressDialog dialog =
                ProgressDialog.CreateThreaded(
                    SR.Get(SRID.SigningProgressTitle),
                    SR.Get(SRID.SigningProgressLabel)).Form;

            try
            {
                //Need to check if this is the first Signature applied to this document.  If so then
                //we need to create a SignatureDefinition before we can sign.
                if (!IsSigned && digSig.GuidID == null)
                {
                    digSig.GuidID = DigitalSignatureProvider.AddRequestSignature(digSig);
                    _changeLog.Add(new ChangeLogEntity((Guid)digSig.GuidID, true));
                }

                //Sign XPS document
                try
                {
                    DigitalSignatureProvider.SignDocument(digSig);
                }
                catch (CryptographicException e)
                {
                    // Close the progress dialog
                    ProgressDialog.CloseThreaded(dialog);

                    // If the exception thrown was a result of the user cancelling
                    // the operation (i.e. cancelling a smartcard PIN prompt)
                    // then we will return silently; otherwise we'll display the exception
                    // message to the user since it typically indicates a smartcard failure
                    // of some sort that the user can take action on, rather than something
                    // that is catastrophic.
                    if (GetErrorCode(e) != SCARD_W_CANCELLED_BY_USER)
                    {
                        System.Windows.MessageBoxResult dialogResult = System.Windows.MessageBox.Show(
                            String.Format(
                                CultureInfo.CurrentCulture,
                                SR.Get(SRID.DigitalSignatureWarnErrorGeneric),
                                e.Message),
                            SR.Get(SRID.DigitalSignatureWarnErrorSigningErrorTitle),
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Exclamation);
                    }

                    UndoChanges();
                    return false;
                }
            }
            finally
            {
                // Close the progress dialog
                ProgressDialog.CloseThreaded(dialog);
            }

            _changeLog.Add(new ChangeLogEntity((Guid)digSig.GuidID, false));

            //Since we're signing the document, signatures have changed; raise the
            //SignaturesChanged event before saving takes place.
            RaiseSignaturesChanged();

            //The signing is complete, now lets save the package.
            //Save XPS document to file
            DocumentManager docManager = DocumentManager.CreateDefault();
            if (docManager != null)
            {
                if (isSaveAs)
                {
                    rtn = docManager.SaveAs(null);
                }
                else
                {
                    rtn = docManager.Save(null);
                }

                if (!rtn)
                {
                    UndoChanges();
                }
                else
                {
                    // if we succeeded in signing and saving 
                    // the changes are 'committed' now, so 
                    // clear the changelog.
                    _changeLog.Clear();
                }
            }

            // If the operation succeeded, re-evaluate signature status
            if (rtn)
            {
                // If there are any signatures that will be invalidated by a
                // new signature, mark those signatures as invalid
                if (!IsSigningAllowedByPolicy)
                {
                    foreach (DigitalSignature signature in DigitalSignatureProvider.Signatures)
                    {
                        if (!signature.Equals(digSig) &&
                            signature.IsAddingSignaturesRestricted)
                        {
                            signature.SignatureState = SignatureStatus.Invalid;
                        }
                    }
                }

                Evaluate();
            }

            return rtn;
        }

        /// <summary>
        /// Displays a signing dialog off the given parent window, optionally
        /// using the fields from the signature request corresponding to the
        /// SignatureResources argument. This is called by the Signature Summary
        /// dialog.
        /// </summary>
        /// <param name="signatureResources">An optional SignatureResources
        /// object that corresponds to a signature request.</param>
        /// <param name="parentWindow">The parent window for the signing dialog
        /// </param>
        internal void OnSign(SignatureResources? signatureResources, IntPtr parentWindow)
        {
            //Nothing was highlighted in the dialog. So this isn't a request signature.
            if (signatureResources == null)
            {
                ShowSigningDialog(parentWindow);
            }
            else
            {
                //Something was highlighted.  Need to see if it was a request or regular Sig.

                DigitalSignature digSig = _digSigSigResources[signatureResources.Value];

                if (digSig.SignatureState == SignatureStatus.NotSigned)
                {
                    //We are signing a request.
                    ShowSigningDialog(parentWindow, digSig);
                }
                else
                {
                    //A regular signature was highlighted so just sign document.
                    ShowSigningDialog(parentWindow);
                }
            }
        }

        /// <summary>
        /// Uses the CLR's X509CertificateUI class to display the Windows
        /// certificate view dialog for the certificate associated with a given
        /// signature.
        /// </summary>
        /// <param name="signatureResources">The digital signature associated
        /// with the certificate</param>
        /// <param name="parentWindow">The window off which to parent the dialog
        /// </param>
        internal void OnCertificateView(
            SignatureResources signatureResources,
            IntPtr parentWindow)
        {
            DigitalSignature digSig = _digSigSigResources[signatureResources];
            X509Certificate2 certificate = null;

            if (digSig != null)
            {
                certificate = digSig.Certificate;
            }

            if (certificate != null)
            {
                X509Certificate2UI.DisplayCertificate(certificate, parentWindow);
            }
        }

        /// <summary>
        /// OnSummaryAdd.
        /// </summary>
        internal void OnSummaryAdd()
        {
            RequestedSignatureDialog requestSignatureDialog = new RequestedSignatureDialog(this);
            requestSignatureDialog.ShowDialog(DocumentApplicationDocumentViewer.Instance.RootBrowserWindow);
            requestSignatureDialog.Dispose();
        }

        /// <summary>
        /// OnSummaryDelete. This function is called from the signature summary
        /// dialog when the Delete button is clicked to remove a signature
        /// request.
        /// </summary>
        /// <param name="signatureResources">The resources of the request to
        /// delete</param>
        internal void OnSummaryDelete(SignatureResources signatureResources)
        {
            //Get the corresponding DigSig
            DigitalSignature digSig = _digSigSigResources[signatureResources];

            //Now lets delete the DigSig (SigDefinition).
            DigitalSignatureProvider.RemoveRequestSignature((Guid)digSig.GuidID);

            //Fire SignatureChanged event to reflect the removed request.
            RaiseSignaturesChanged();
        }

        /// <summary>
        /// OnAddRequestSignature.  Called from Request dialog.
        /// </summary>
        internal void OnAddRequestSignature(SignatureResources sigResources, DateTime dateTime)
        {
            //Use the SignatureResource to create Request Digitalsignature.
            DigitalSignature digSigRequest = new DigitalSignature();

            //Assign fields.
            digSigRequest.SignatureState = SignatureStatus.NotSigned;
            digSigRequest.SubjectName = sigResources._subjectName;
            digSigRequest.Reason = sigResources._reason;
            digSigRequest.Location = sigResources._location;
            digSigRequest.SignedOn = dateTime;

            Guid spotId = DigitalSignatureProvider.AddRequestSignature(digSigRequest);

            digSigRequest.GuidID = spotId;

            // A request has been added, so the signatures have changed.
            RaiseSignaturesChanged();
        }

        /// <summary>
        /// HasCertificate.  Check to see if this signatureResources has a corresponding cert.
        /// </summary>
        internal bool HasCertificate(SignatureResources signatureResources)
        {
            bool rtn = false;

            //Find corresponding digSig
            DigitalSignature digSig = _digSigSigResources[signatureResources];

            //If this is a request signature it won't have a cert. This can also
            //happen if the signature is invalid because it is missing a cert.
            if (digSig.Certificate != null)
            {
                rtn = true;
            }

            return rtn;
        }

        /// <summary>
        /// Initialize the singleton DocumentSignatureManager.
        /// </summary>
        /// <param name="provider">The provider with which to initialize the
        /// signature manager</param>
        internal static void Initialize(IDigitalSignatureProvider provider)
        {
            System.Diagnostics.Debug.Assert(
                _singleton == null,
                "DocumentSignatureManager initialized twice.");

            if (_singleton == null)
            {
                _singleton = new DocumentSignatureManager(provider);
            }
        }

        #endregion Internal Methods

        #region Internal Properties
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        internal static DocumentSignatureManager Current
        {
            get { return _singleton; }
        }

        /// <summary>
        /// Whether or not the document contains any signatures.
        /// </summary>
        internal bool IsSigned
        {
            get { return DigitalSignatureProvider.IsSigned; }
        }

        /// <summary>
        /// Whether or not the document meets the signing criteria.
        /// The first call to this may take a significant amount of time;
        /// Subsequent calls will return a cached value.
        /// </summary>
        internal bool IsSignable
        {
            get
            {
                return DigitalSignatureProvider.IsSignable;
            }
        }

        /// <summary>
        /// Whether or not the document contains any signature requests.
        /// </summary>
        internal bool HasRequests
        {
            get { return DigitalSignatureProvider.HasRequests; }
        }

        #endregion Internal Properties

        #region Internal Delegate
        //------------------------------------------------------
        //
        //  Internal Delegate
        //
        //------------------------------------------------------

        internal delegate void SignatureStatusChangeHandler(object sender,
                                                            SignatureStatusEventArgs args
                                                         );

        #endregion Internal Delegate
 
        #region Private Methods
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Displays the cert picker dialog.
        /// </summary>
        private static X509Certificate2 ShowCertificatePickerDialog(IntPtr parentWindow)
        {
            X509Certificate2 x509cert = null;

            // look for appropriate certificates
            X509Store store = new X509Store(StoreLocation.CurrentUser);
            store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
            X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;

            // narrow down the choices
            // timevalid
            collection = collection.Find(X509FindType.FindByTimeValid, DateTime.Now, true /*validOnly*/);

            // intended for signing (or no intent specified)
            collection = collection.Find(X509FindType.FindByKeyUsage, X509KeyUsageFlags.DigitalSignature, false /*validOnly*/);

            // remove certs that don't have private key
            // work backward so we don't disturb the enumeration
            for (int i = collection.Count - 1; i >= 0; i--)
            {
                if (!collection[i].HasPrivateKey)
                {
                    collection.RemoveAt(i);
                }
            }

            // any suitable certificates available?
            if (collection.Count > 0)
            {
                collection = X509Certificate2UI.SelectFromCollection(
                    collection,
                    SR.Get(SRID.CertSelectionDialogTitle),
                    SR.Get(SRID.CertSelectionDialogMessage),
                    X509SelectionFlag.SingleSelection,
                    parentWindow);

                if (collection.Count > 0)
                {
                    x509cert = collection[0];   // return the first one
                }
            }
            else
            {
                System.Windows.MessageBox.Show(
                    SR.Get(SRID.DigitalSignatureWarnErrorRMSigningMessageNoCerts),
                    SR.Get(SRID.DigitalSignatureWarnErrorSigningErrorTitle),
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Exclamation);
            }

            return x509cert;
        }

        /// <summary>
        /// Add restrictions to a signature policy based on the additional
        /// restrictions imposed by a given signature
        /// </summary>
        /// <param name="calcSigPolicy">The original signature policy</param>
        /// <param name="digSig">The signature from which to add restrictions</param>
        /// <returns>The new signature policy</returns>
        private static SignaturePolicy AddRestrictionsFromSignature(SignaturePolicy calcSigPolicy, DigitalSignature digSig)
        {
            if (digSig.IsDocumentPropertiesRestricted &&
                IsAllowedByPolicy(calcSigPolicy, SignaturePolicy.ModifyDocumentProperties))
            {
                calcSigPolicy ^= SignaturePolicy.ModifyDocumentProperties;
            }

            if (digSig.IsAddingSignaturesRestricted &&
                IsAllowedByPolicy(calcSigPolicy, SignaturePolicy.AllowSigning))
            {
                calcSigPolicy ^= SignaturePolicy.AllowSigning;
            }

            return calcSigPolicy;
        }

        /// <summary>
        /// Checks if an action (represented by a SignaturePolicy) is allowed
        /// by the given signature policy.
        /// </summary>
        /// <param name="policy">The signature policy</param>
        /// <param name="action">The action to check</param>
        /// <returns>True if the action is allowed by the policy</returns>
        private static bool IsAllowedByPolicy(SignaturePolicy policy, SignaturePolicy action)
        {
            return ((policy & action) == action);
        }

        /// <summary>
        /// StartCertificateStatusCheck starts thread to get Certificate status.
        /// Only call once at loadtime.
        /// </summary>
        /// <param name="certificateList">The list of certificates to validate</param>
        private void StartCertificateStatusCheck(IList<X509Certificate2> certificateList)
        {
            // First get a callback setup to call the Evaluate method using
            // this thread.  After the worker thread gets the certificate
            // status and sets the private field it will set the dispatcher
            // operation's priority to Background so the Evaluate method gets
            // called when the time comes.
            DispatcherOperation dispatcherOperation = Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Inactive,
                new DispatcherOperationCallback(delegate(object notused)
                {
                    Trace.SafeWrite(
                        Trace.Signatures,
                        "Dispatcher callback. Setting document loaded flag to true.");

                    // Now that the certs have been verified evaluate the DigSig
                    // state of the document.
                    Evaluate();
                    return null;
                }),
                null);

            CertificateValidationThreadInfo threadInfo =
                new CertificateValidationThreadInfo();

            threadInfo.CertificateList = certificateList;
            threadInfo.Operation = dispatcherOperation;

            Trace.SafeWrite(
                Trace.Signatures,
                "Kicking off certificate validation thread");

            // Pass certificate validation work off so UI doesn't block.

            // There are no concurrency issues here because the only work being
            // done by the work item is the validation of a list of
            // certificates. Accessing the signatures on the document could
            // cause a concurrency problem. This is why we read the signatures
            // and certificates in this thread before passing only the
            // certificates to the worker thread.

            System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(CertificateStatusCheckWorkItem), threadInfo);
        }

        /// <summary>
        /// CertificateStatusCheckThreadProc.  This is a worker thread used to call into the DigitalSignatureProvider 
        /// to verify the all certificates used to sign the document.  This method takes an unknown amount of time 
        /// (default of 15 second timeout per cert).  Once the work has been done then we set the waiting 
        /// dispatcherOperation to status background so it will call the Evaluate method.
        /// </summary>
        private void CertificateStatusCheckWorkItem(object stateInfo)
        {
            CertificateValidationThreadInfo threadInfo = (CertificateValidationThreadInfo)stateInfo;

            Trace.SafeWrite(
                Trace.Signatures,
                "Starting certificate validation in thread.");

            // XPSDocumentCertificateStatus can take an undetermined amount of
            // time. If timeouts occur then each Cert could take 15 secs each
            // by default and more if admin has changed defaults.
            _certificateStatusTable =
                DigitalSignatureProvider.GetCertificateStatus(threadInfo.CertificateList);

            Trace.SafeWrite(
                Trace.Signatures,
                "Done with certificate validation. Starting dispatcher operation.");

            // The DispatcherOperation was passed to this thread so the
            // DispatcherPriority could be set to Background, and the operation
            // that will run can call the Evaluate method.
            threadInfo.Operation.Priority = DispatcherPriority.Background;
        }

        /// <summary>
        /// Gets the CertificateStatus for a specific x509Certificate from the
        /// cached dictionary of certificates and their status.
        /// </summary>
        private CertificatePriorityStatus GetCertificateStatusFromTable(DigitalSignature digitalSignature)
        {
            CertificatePriorityStatus certificatePriorityStatus = CertificatePriorityStatus.Corrupted;

            if (digitalSignature == null)
            {
                throw new ArgumentNullException("digitalSignature");
            }

            // Signature requests and invalid signatures with missing certificates
            // both get the certificate status NoCertificate
            if (digitalSignature.SignatureState == SignatureStatus.NotSigned ||
                digitalSignature.Certificate == null)
            {
                certificatePriorityStatus = CertificatePriorityStatus.NoCertificate;
            }
            // If the certificate status table is null, the certificates are
            // still being verified
            else if (_certificateStatusTable == null)
            {
                certificatePriorityStatus = CertificatePriorityStatus.Verifying;
            }
            else
            {
                Invariant.Assert(
                    _certificateStatusTable.ContainsKey(digitalSignature.Certificate),
                    "The certificate was not found in the certificate status table.");

                certificatePriorityStatus = _certificateStatusTable[digitalSignature.Certificate];
            }

            return certificatePriorityStatus;
        }

        /// <summary>
        /// OnSignatureStatusChange.
        /// </summary>
        private void OnSignatureStatusChange(SignatureStatus newStatus)
        {
            SignatureStatusEventArgs args = new SignatureStatusEventArgs(   
                newStatus,
                SignatureResourceHelper.GetDocumentLevelResources(newStatus));

            RaiseSignatureStatusChange(args);
        }

        /// <summary>
        /// RaiseSignatureStatusChange.
        /// </summary>
        private void RaiseSignatureStatusChange(SignatureStatusEventArgs args)
        {
            if (SignatureStatusChange != null)
            {
                SignatureStatusChange(this, args);
            }
        }

        /// <summary>
        /// Raises the SignaturesChanged event
        /// </summary>
        private void RaiseSignaturesChanged()
        {
            if (SignaturesChanged != null)
            {
                SignaturesChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Handles the RMPolicyChanged event fired by the RMManager.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnRMPolicyChanged(object sender, DocumentRightsManagementManager.RightsManagementPolicyEventArgs args)
        {
            if (args != null)
            {
                if ((args.RMPolicy & RightsManagementPolicy.AllowSign) == RightsManagementPolicy.AllowSign)
                {
                    _allowSign.Value = true;
                }
                else
                {
                    _allowSign.Value = false;
                }
            }
            else
            {
                throw new ArgumentNullException("args");
            }
        }

        /// <summary>
        /// VerifySignability checks the XpsDocument.IsSignable property, which
        /// may take a significant amount of time to complete.  Because of this
        /// it will display a progress dialog to alert the user while the operation
        /// is pending.  
        /// </summary>        
        /// <returns></returns>        
        private bool VerifySignability()
        {
            bool result;

            //Show the Progress dialog
            ProgressDialog dialog = 
                ProgressDialog.CreateThreaded(
                    SR.Get(SRID.ValidationProgressTitle),
                    SR.Get(SRID.ValidationProgressLabel)).Form;        

            try
            {
                //Check the IsSignable property, which can take a significant portion of time...
                result = IsSignable;                 
            }
            // Close the progress dialog in the event of any exception.  This prevents the
            // dialog from remaining open when the error page appears.  We cannot use a finally
            // block here because in the case of an uncaught exception the unhandled exception
            // handler will get the exception (and show the error page) before the block runs.
            catch
            {
                ProgressDialog.CloseThreaded(dialog);
                throw;
            }

            // Close the progress dialog
            ProgressDialog.CloseThreaded(dialog);

            return result;
        }

        /// <summary>
        /// Uses reflection to get the protected HResult out of a CryptographicException,
        /// since it's not polite enough to make it publicly accessible to begin with.
        /// </summary>
        /// <returns></returns>
        private int GetErrorCode(CryptographicException ce)
        {
            int error = 0;

            // Get the protected "HResult" field from the CryptographicException
            Type t = typeof(CryptographicException);
            PropertyInfo p = t.GetProperty("HResult", BindingFlags.NonPublic | BindingFlags.Instance);
            error = (int)p.GetValue(ce, null);

            return error;
        }

        /// <summary>
        /// Un-does the changes in the current changelog and clears the changelog.
        /// Called when a signing or saving operation fails.  This method is critical
        /// because clearing the changelog at inopportune times could put us in an
        /// inconsistent state.
        /// </summary>
        private void UndoChanges()
        {            
            foreach (ChangeLogEntity entry in _changeLog)
            {
                if (entry.IsSignatureRequest)
                {
                    DigitalSignatureProvider.RemoveRequestSignature(entry.Id);
                }
                else
                {
                    DigitalSignatureProvider.UnsignDocument(entry.Id);
                }
            }

            _changeLog.Clear();
        }

        #endregion Private Methods

        #region Private Properties
        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Gets whether or not all signatures on the document have had their
        /// certificates validated.
        /// </summary>
        /// <remarks>
        /// This simply goes through all the signatures on the document and
        /// ensures that they are all represented in the status table. This is
        /// a safe way to check whether all signatures are verified because the
        /// status table is critical.
        /// </remarks>
        private bool AreAllSignaturesVerified
        {
            get
            {
                if (_certificateStatusTable == null)
                {
                    return false;
                }

                foreach (DigitalSignature signature in DigitalSignatureProvider.Signatures)
                {
                    if (signature.Certificate != null &&
                        !_certificateStatusTable.ContainsKey(signature.Certificate))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// The IDigitalSignatureProvider associated with this instance of the
        /// signature manager.
        /// </summary>
        private IDigitalSignatureProvider DigitalSignatureProvider
        {
            get
            {
                return _digitalSignatureProvider.Value;
            }

            set
            {
                _digitalSignatureProvider.Value = value;
            }
        }
        
        /// <summary>
        /// Whether or not the current Rights Management policy allows signing.
        /// This is a convenience wrapper around critical for set data
        /// _allowSign.
        /// </summary>
        private bool IsSigningAllowed
        {
            get
            {
                return _allowSign.Value;
            }
        }

        /// <summary>
        /// Returns true if (as indicated by the current signature policy) any
        /// additional signatures will not invalidate existing ones.
        /// </summary>
        private bool IsSigningAllowedByPolicy
        {
            get
            {
                return IsAllowedByPolicy(_signaturePolicy.Value, SignaturePolicy.AllowSigning);
            }
        }

        #endregion Private Properties

        #region Private Fields
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private static DocumentSignatureManager _singleton;

        /// <summary>
        /// The IDigitalSignatureProvider associated with this instance of the
        /// digital signature manager.
        /// </summary>
        private SecurityCriticalDataForSet<IDigitalSignatureProvider> _digitalSignatureProvider;

        private IDictionary<SignatureResources, DigitalSignature> _digSigSigResources;
        private SecurityCriticalDataForSet<bool> _allowSign;

        private SecurityCriticalDataForSet<SignaturePolicy> _signaturePolicy;

        /// <summary>
        /// The change log that is used to roll back signatures on failure.
        /// </summary>
        private List<ChangeLogEntity> _changeLog;

        /// <summary>Dictionary that stores the status of each certificate used
        /// in signatures in the document</summary>
        private IDictionary<X509Certificate2, CertificatePriorityStatus> _certificateStatusTable;

        /// <summary>
        /// Constant for CryptographicException HResult corresponding to user-cancellation
        /// of smart-card signing.
        /// </summary>
        private const int SCARD_W_CANCELLED_BY_USER = -2146434962;

        #endregion Private Fields

        #region Nested Class
        //------------------------------------------------------
        //
        //  Nested Class
        //
        //------------------------------------------------------   
                
        /// <summary>
        /// A struct representing data passed to the thread that validates a
        /// list of certificates
        /// </summary>
        private struct CertificateValidationThreadInfo
        {
            /// <summary>
            /// The list of certificates to validate
            /// </summary>
            internal IList<X509Certificate2> CertificateList;

            /// <summary>
            /// A handle to a dispatcher operation that will evaluate the
            /// document's signature status after the certificates have all been
            /// validated
            /// </summary>
            internal DispatcherOperation Operation;
        }

        /// <summary>
        /// SignatureStatusEventArgs, object used when firing SigStatus change.
        /// </summary>
        public class SignatureStatusEventArgs : EventArgs
        {
            #region Constructors
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            /// <summary>
            /// The constructor
            /// </summary>
            /// <param name="signatureStatus">signature status</param>
            /// <param name="statusResources">Resources containing other information
            /// relevant to the document signature status</param>
            public SignatureStatusEventArgs(
                SignatureStatus signatureStatus,
                DocumentStatusResources statusResources)
            {
                _signatureStatus = signatureStatus;
                _statusResources = statusResources;
            }

            #endregion Constructors

            #region Public Properties
            //------------------------------------------------------
            //
            //  Public Properties
            //
            //------------------------------------------------------

            /// <summary>
            /// Property to get the SigStatus
            /// </summary>
            public SignatureStatus SignatureStatus
            {
                get { return _signatureStatus; }
            }

            /// <summary>
            /// Property to get the StatusResources
            /// </summary>
            public DocumentStatusResources StatusResources
            {
                get { return _statusResources; }
            }

            #endregion Public Properties

            #region Private Fields
            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            private SignatureStatus _signatureStatus;
            private DocumentStatusResources _statusResources;

            #endregion Private Fields
        }

        /// <summary>
        /// Each instance represents the addition of either a request or a 
        /// signature (determined by IsSignatureRequest) along with the unique
        /// id for the item (Id).
        /// </summary>
        private struct ChangeLogEntity
        {
            #region Constructors
            //------------------------------------------------------------------
            // Constructors
            //------------------------------------------------------------------

            internal ChangeLogEntity(Guid id, bool isSignatureRequest)
            {
                _id = id;
                _isSignatureRequest = isSignatureRequest;
            }
            #endregion Constructors

            #region Internal Properties
            //------------------------------------------------------------------
            // Internal Properties
            //------------------------------------------------------------------

            public Guid Id
            {
                get { return _id; }
            }

            public bool IsSignatureRequest
            {
                get { return _isSignatureRequest; }
            }
            #endregion Internal Properties

            #region Private Fields
            //------------------------------------------------------------------
            // Private Fields
            //------------------------------------------------------------------
            private Guid _id;
            private bool _isSignatureRequest;
            #endregion Private Fields
        }

        #endregion Nested Class
    }
}
