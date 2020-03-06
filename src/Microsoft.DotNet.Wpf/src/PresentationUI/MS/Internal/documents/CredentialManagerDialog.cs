// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//    CredentialManagerDialog is the Forms dialog that allows users to select RM Creds.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.TrustUI;
using System.Security;

using System.Security.RightsManagement;


namespace MS.Internal.Documents
{
    /// <summary>
    /// CredentialManagerDialog is used for choose RM credentials. 
    /// </summary>
    internal sealed partial class CredentialManagerDialog : DialogBaseForm
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
        internal CredentialManagerDialog(   IList<string> accountList,
                                            string defaultAccount,
                                            DocumentRightsManagementManager docRightsManagementManager
                                        )
        {
            Invariant.Assert(docRightsManagementManager != null);
            _docRightsManagementManager = docRightsManagementManager;

            //Set the data source for the listbox
            SetCredentialManagementList(accountList, defaultAccount);

            //Enable or disable remove button depending on whether there is anything to remove
            _credListBox_SelectedIndexChanged(this, EventArgs.Empty);
        }
        #endregion Constructors

        #region Public Methods
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        
        /// <summary>
        /// SetCredentialManagementList.
        /// </summary>
        internal void SetCredentialManagementList(
            IList<string> accountList,
            string defaultAccount
            )
        {
            //now we need to refresh everything.
            //Set the data source
            _credListBox.DataSource = accountList;

            if (defaultAccount != null)
            {
                //Now we need to get and select the default.
                _credListBox.SelectedIndex = _credListBox.Items.IndexOf(defaultAccount);
            }
        }
        #endregion Public Methods

        #region Private Methods
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Called when the selected item changes on the listbox.
        /// </summary>
        private void _credListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Check to see if something is selected.
            if (_credListBox.SelectedIndex >= 0)
            {
                _removeButton.Enabled = true;
            }
            else
            {
                _removeButton.Enabled = false;
            }
        }

        /// <summary>
        /// OK button handler.
        /// </summary>
        private void _okButton_Click(object sender, EventArgs e)
        {
            //Check to see if something is selected.
            if (_credListBox.SelectedIndex >= 0)
            {   
                //Call Manager to set the default.
                _docRightsManagementManager.OnCredentialManagementSetDefault(
                    (string) _credListBox.Items[_credListBox.SelectedIndex]);
            }
        }


        /// <summary>
        /// Remove button handler.
        /// </summary>
        private void _removeButton_Click(object sender, EventArgs e)
        {
            //Check to see if something is selected.
            if (_credListBox.SelectedIndex >= 0)
            {
                //Call Manager to remove selected user.
                _docRightsManagementManager.OnCredentialManagementRemove(
                    (string)_credListBox.Items[_credListBox.SelectedIndex]);
                //Enable or disable remove button depending on whether there is anything to remove
                _credListBox_SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Add button handler.
        /// </summary>
        private void _addButton_Click(object sender, EventArgs e)
        {
            //to add new user just call enrollment
            _docRightsManagementManager.OnCredentialManagementShowEnrollment();
            //Enable or disable remove button depending on whether there is anything to remove
            _credListBox_SelectedIndexChanged(this, EventArgs.Empty);
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

            _cancelButton.Text = SR.Get(SRID.RMCredManagementCancel);
            _okButton.Text = SR.Get(SRID.RMCredManagementOk);
            _addButton.Text = SR.Get(SRID.RMCredManagementAdd);
            _removeButton.Text = SR.Get(SRID.RMCredManagementRemove);
            _instructionLabel.Text = SR.Get(SRID.RMCredManagementInstruction);
            Text = SR.Get(SRID.RMCredManagementDialog);

            // Setup matching Add/Remove button widths
            int maxWidth = Math.Max(_addButton.Width, _removeButton.Width);
            _addButton.Width = maxWidth;
            _removeButton.Width = maxWidth;
        }

        #endregion Protected Methods

        #region Private Fields
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private DocumentRightsManagementManager _docRightsManagementManager;

        #endregion Private Fields
    }
}
