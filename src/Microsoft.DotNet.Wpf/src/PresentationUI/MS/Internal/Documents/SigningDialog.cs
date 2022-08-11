// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//    SigningDialog is the Forms dialog that allows users to select signing parameters.
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.TrustUI;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;                 // For localization of string conversion

using MS.Internal.Documents.Application;

namespace MS.Internal.Documents
{
    /// <summary>
    /// SigningDialog is used for signing a XPS document 
    /// </summary>
    internal sealed partial class SigningDialog : DialogBaseForm
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
        internal SigningDialog(X509Certificate2 x509Certificate2, DigitalSignature digitalSignatureRequest, DocumentSignatureManager docSigManager)
        {
            if (x509Certificate2 == null)
            {
                throw new ArgumentNullException("x509Certificate2");
            } 
            if (docSigManager == null)
            {
                throw new ArgumentNullException("docSigManager");
            }
            
            _docSigManager = docSigManager;
            _x509Certificate2 = x509Certificate2;   // setting critical data.

            //This can be null and if so means this is a regular first signing 
            //(not signing a request or 2nd/3rd.. regular request which can't have
            //signatureDefinitions.)
            _digitalSignatureRequest = digitalSignatureRequest;

            //Now we need to set all DigSig Specific Text
            ApplySignatureSpecificResources();

            _signButton.Enabled = false;

            // Check DocumentManager to see if we can save package
            DocumentManager documentManager = DocumentManager.CreateDefault();
            if (documentManager != null)
            {
                _signButton.Enabled = documentManager.CanSave;
            }

            if (DocumentRightsManagementManager.Current != null)
            {
                _signSaveAsButton.Enabled = DocumentRightsManagementManager.Current.HasPermissionToSave;
            }
        }

        #endregion Constructors

        #region Private Methods
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Method for Signing and Save/SaveAs.
        /// </summary>
        private void SignAndSave(bool isSaveAs)
        {
            //Create new DigSig
            DigitalSignature digSig = null;

            //If this is a request, re-use the requested signature, so we update the request
            //to the signed version in place.
            if (_digitalSignatureRequest != null && 
                _digitalSignatureRequest.GuidID != null)
            {
                digSig = _digitalSignatureRequest;
            }
            else
            {
                digSig = new DigitalSignature();
            }

            //Get this user input and set to DigSig.
            digSig.Certificate = this.Certificate;
            // If this is a second signature that was not requested, then set the
            // default values for Reason and Location.
            if (_isSecondSignatureNotRequested)
            {
                string none = SR.Get(SRID.SignatureResourceHelperNone);
                digSig.Reason = none;
                digSig.Location = none;
            }
            else
            {
                digSig.Reason = Intent;
                digSig.Location = LocationText;
            }
            digSig.IsDocumentPropertiesRestricted = IsDocPropsRestricted;
            digSig.IsAddingSignaturesRestricted = IsDigSigRestricted;
           
            // if signing worked close because we are done
            // else do nothing to leave the dialog open
            // signing may have failed either because the user cancled or there
            // was a non-fatal error like the destination file was in use
            if (_docSigManager.SignDocument(digSig, this, isSaveAs))
            {
                Close();
            }
        }

        /// <summary>
        /// Handler for sign and save button.
        /// </summary>
        private void _signSaveButton_Click(object sender, EventArgs e)
        {
            // To prevent launching multiple dispatcher operations that try to sign the
            // document simulatenously, disable both signing buttons during the operation.

            _signButton.Enabled = false;
            bool saveAsEnabled = _signSaveAsButton.Enabled;
            _signSaveAsButton.Enabled = false;

            SignAndSave(false);

            _signButton.Enabled = true;
            _signSaveAsButton.Enabled = saveAsEnabled;
        }

        /// <summary>
        /// Handler for sign and save as button.
        /// </summary>
        private void _signSaveAsButton_Click(object sender, EventArgs e)
        {
            // To prevent launching multiple dispatcher operations that try to sign the
            // document simulatenously, disable both signing buttons during the operation.

            _signSaveAsButton.Enabled = false;
            bool saveEnabled = _signButton.Enabled;
            _signButton.Enabled = false;

            SignAndSave(true);

            _signSaveAsButton.Enabled = true;
            _signButton.Enabled = saveEnabled;
        }

        /// <summary>
        /// Handler for cancel button.
        /// </summary>
        private void _cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// ApplySignatureSpecificResources - sets the text based on Cert and Signature.
        /// </summary>
        private void ApplySignatureSpecificResources()
        {
            _signerlabel.Text = String.Format(CultureInfo.CurrentCulture,
                                    SR.Get(SRID.SigningDialogSignerlabel),
                                    Certificate.GetNameInfo(X509NameType.SimpleName,
                                    false /*Issuer*/)
                                  );

            //Check to see if this signing is a request(or second signing without request).
            if (_digitalSignatureRequest != null)
            {
                // For both Reason and Location, determine if this is a second signing
                // without a request.  If this is without a request, then display
                // "N/A" in the textboxes.
                if (String.IsNullOrEmpty(_digitalSignatureRequest.Reason) &&
                    String.IsNullOrEmpty(_digitalSignatureRequest.Location))
                {
                    _isSecondSignatureNotRequested = true;
                    string na = SR.Get(SRID.SigningDialogNA);
                    _reasonComboBox.Text = na;
                    _locationTextBox.Text = na;
                }
                else
                {
                    // Since this is a requested signature replace any empty strings 
                    // with "<none>"
                    string none = SR.Get(SRID.SignatureResourceHelperNone);
                    _reasonComboBox.Text = String.IsNullOrEmpty(_digitalSignatureRequest.Reason) ?
                        none : _digitalSignatureRequest.Reason;
                    _locationTextBox.Text = String.IsNullOrEmpty(_digitalSignatureRequest.Location) ?
                        none : _digitalSignatureRequest.Location;
                }

                //This signature can't create new signatureDefinition so Enable=false to all
                //related UI.
                _reasonComboBox.Enabled = false;
                _reasonLabel.Enabled = false;
                _locationTextBox.Enabled = false;
                _locationLabel.Enabled = false;

                _addDigSigCheckBox.Enabled = false;
                _addDocPropCheckBox.Enabled = false;                
            }

            // Regardless of the settings above, if there are any Signature Requests, 
            // then we should disable the checkbox option that says additional signatures 
            // will break this signature.   This covers ad hoc signing when signatures requests
            // also exist.
            _addDigSigCheckBox.Enabled = _addDigSigCheckBox.Enabled & !_docSigManager.HasRequests;
        }

        #endregion Private Methods

        #region Protected Methods
        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// ApplyResources override.  Called to apply dialog resources.
        /// </summary>
        protected override void ApplyResources()
        {
            base.ApplyResources();

            //Get the localized text strings.            
            _reasonLabel.Text = SR.Get(SRID.SigningDialogReasonLabel);
            _locationLabel.Text = SR.Get(SRID.SigningDialogLocationLabel);
            _actionlabel.Text = SR.Get(SRID.SigningDialogActionlabel);            
            _addDocPropCheckBox.Text = SR.Get(SRID.SigningDialogAddDocPropCheckBox);
            _addDigSigCheckBox.Text = SR.Get(SRID.SigningDialogAddDigSigCheckBox);
            _cancelButton.Text = SR.Get(SRID.SigningDialogCancelButton);
            _signButton.Text = SR.Get(SRID.SigningDialogSignButton);            
            _signSaveAsButton.Text = SR.Get(SRID.SigningDialogSignSaveAsButton);            
            Text = SR.Get(SRID.SigningDialogTitle);

            //Load the Intent/Reason combo
            _reasonComboBox.Items.Add(SR.Get(SRID.DigSigIntentString1));
            _reasonComboBox.Items.Add(SR.Get(SRID.DigSigIntentString2));
            _reasonComboBox.Items.Add(SR.Get(SRID.DigSigIntentString3));
            _reasonComboBox.Items.Add(SR.Get(SRID.DigSigIntentString4));
            _reasonComboBox.Items.Add(SR.Get(SRID.DigSigIntentString5));
            _reasonComboBox.Items.Add(SR.Get(SRID.DigSigIntentString6));
        }

        /// <summary>
        /// ApplyStyle
        /// </summary>
        protected override void ApplyStyle()
        {
            base.ApplyStyle();

            //Set the signer label to bold.
            _signerlabel.Font = new Font(System.Drawing.SystemFonts.DialogFont, System.Drawing.SystemFonts.DialogFont.Style | FontStyle.Bold);
        }

        #endregion Protected Methods

        #region Internal properties

        /// <summary>
        /// String field Intent.
        /// </summary>
        internal string Intent
        {
            get
            {
                return _reasonComboBox.Text;
            }
        }

        /// <summary>
        /// String field Location.
        /// </summary>
        internal string LocationText
        {
            get
            {
                return _locationTextBox.Text;
            }
        }        

        /// <summary>
        /// Is Doc Props Restricted.
        /// </summary>
        internal bool IsDocPropsRestricted
        {
            get
            {
                return _addDocPropCheckBox.Checked;
            }
        }

        /// <summary>
        /// Is Dig Sigs Restricted.
        /// </summary>
        internal bool IsDigSigRestricted
        {
            get
            {
                return _addDigSigCheckBox.Checked;
            }
        }

        internal X509Certificate2 Certificate
        {
            get
            {
                return _x509Certificate2;
            }
        }

        #endregion Internal properties

        #region Private Fields
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private DocumentSignatureManager _docSigManager;

        private X509Certificate2 _x509Certificate2;

        private DigitalSignature _digitalSignatureRequest;

        /// <summary>
        /// Used to determine if this signing is for an additional signature,
        /// without a signature requested.
        /// </summary>
        private bool _isSecondSignatureNotRequested;

        #endregion Private Fields

    }
}

