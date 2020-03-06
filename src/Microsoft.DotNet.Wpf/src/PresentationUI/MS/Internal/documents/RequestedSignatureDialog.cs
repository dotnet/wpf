// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.TrustUI;
using MS.Internal.PresentationUI;

namespace MS.Internal.Documents
{
    internal sealed partial class RequestedSignatureDialog : DialogBaseForm
    {
        #region Constructors
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Constructor
        /// </summary>
        internal RequestedSignatureDialog(DocumentSignatureManager docSigManager)
        {
            if (docSigManager != null)
            {
                //Init private fields
                _documentSignatureManager = docSigManager;
            }
            else
            {
                throw new ArgumentNullException("docSigManager");
            }

            // Initialize the "Must Sign By:" field
            _dateTimePicker.MinDate = DateTime.Now;
            _dateTimePicker.Value = DateTime.Now.AddDays(10);
        }
        #endregion Constructors

        #region Private Methods
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------


        /// <summary>
        /// oKButton_Click
        /// </summary>
        private void _addButton_Click(object sender, EventArgs e)
        {
            //Check to see this the input is valid
            if (ValidateUserData())
            {
                //Create SignatureResource to pass back to DocumentSignatureManager
                SignatureResources sigResources = new SignatureResources();

                //Get the user data.
                sigResources._subjectName = _requestedSignerNameTextBox.Text;
                sigResources._reason = _intentComboBox.Text;
                sigResources._location = _requestedLocationTextBox.Text;
                    
                //Add the SignatureDefinition.
                _documentSignatureManager.OnAddRequestSignature(sigResources,_dateTimePicker.Value);

                //Close the Add Request dialog
                Close();
            }
            else
            {
                System.Windows.MessageBox.Show(
                                    SR.Get(SRID.DigitalSignatureWarnErrorReadOnlyInputError),
                                    SR.Get(SRID.DigitalSignatureWarnErrorSigningErrorTitle),
                                    System.Windows.MessageBoxButton.OK, 
                                    System.Windows.MessageBoxImage.Exclamation
                                    );
            }
        }

        /// <summary>
        /// ValidateUserData.  Check the user input is valid.
        /// </summary>
        private bool ValidateUserData()
        {
            bool rtnvalue = false;
            
            //Remove extra white space.
            string requestSignerName = _requestedSignerNameTextBox.Text.Trim();
            string intentComboBoxText = _intentComboBox.Text.Trim();

            //Do the text/combo contain any text?
            if (!String.IsNullOrEmpty(requestSignerName) &&
                !String.IsNullOrEmpty(intentComboBoxText))
            {
                rtnvalue = true;
            }

            return rtnvalue;
        }

        #endregion Private Methods

        #region Private Fields
        //------------------------------------------------------    
        //    
        //  Private Fields
        //    
        //------------------------------------------------------

        private DocumentSignatureManager _documentSignatureManager;               

        #endregion Private Fields

        #region Protected Methods
        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// ApplyResources
        /// </summary>
        protected override void ApplyResources()
        {
            base.ApplyResources();

            //Get localized strings.
            _addButton.Text = SR.Get(SRID.RequestSignatureDialogAdd);
            _cancelButton.Text = SR.Get(SRID.RequestSignatureDialogCancel);            
            _requestSignerNameLabel.Text = SR.Get(SRID.RequestSignatureDialogRequestSignerNameLabel);
            _intentLabel.Text = SR.Get(SRID.RequestSignatureDialogIntentLabel);
            _requestLocationLabel.Text = SR.Get(SRID.RequestSignatureDialogLocationLabel);
            _signatureAppliedByDateLabel.Text = SR.Get(SRID.RequestSignatureDialogSignatureAppliedByDateLabel);
            Text = SR.Get(SRID.RequestSignatureDialogTitle);

            //Load the Intent/Reason combo
            _intentComboBox.Items.Add(SR.Get(SRID.DigSigIntentString1));
            _intentComboBox.Items.Add(SR.Get(SRID.DigSigIntentString2));
            _intentComboBox.Items.Add(SR.Get(SRID.DigSigIntentString3));
            _intentComboBox.Items.Add(SR.Get(SRID.DigSigIntentString4));
            _intentComboBox.Items.Add(SR.Get(SRID.DigSigIntentString5));
            _intentComboBox.Items.Add(SR.Get(SRID.DigSigIntentString6));
        }

        #endregion Protected Methods
    }
}
