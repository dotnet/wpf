// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Security;
using System.Security.RightsManagement;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.TrustUI;

using MS.Internal.Documents.Application;

namespace MS.Internal.Documents
{
    /// <summary>
    /// The publishing dialog.
    /// </summary>
    internal sealed partial class RMPublishingDialog : DialogBaseForm
    {
        //------------------------------------------------------
        // Constructors
        //------------------------------------------------------
        #region Constructors
        /// <summary>
        /// Creates a new Publishing dialog for the given user with the given list of
        /// already granted rights.
        /// </summary>
        /// <param name="grantDictionary">A dictionary of rights already granted on the
        /// document.  This should be null if the document is not protected, and an
        /// empty dictionary if it is but no grants exist.</param>
        internal RMPublishingDialog(
            RightsManagementUser user,
            IDictionary<RightsManagementUser, RightsManagementLicense> grantDictionary)
        {
            checkBoxValidUntil.Checked = false;
            datePickerValidUntil.Enabled = false;

            // The DateTimePicker displays validity dates in the local time
            // zone, so the date displayed is not necessarily the value that is
            // returned by the ValidUntil property.
            datePickerValidUntil.MinDate = DateTime.Today.AddDays(1);
            datePickerValidUntil.Value = DateTime.Today.AddDays(1);

            textBoxPermissionsContact.Enabled = true;

            checkBoxPermissionsContact.Checked = true;
            textBoxPermissionsContact.Text = user.Name;            

            // Check if referral info is already specified in a license
            if (grantDictionary != null && grantDictionary.Count != 0)
            {
                IEnumerator<RightsManagementLicense> licenseEnumerator =
                    grantDictionary.Values.GetEnumerator();

                licenseEnumerator.MoveNext();

                Uri referralUri = licenseEnumerator.Current.ReferralInfoUri;

                string address =
                    AddressUtility.GetEmailAddressFromMailtoUri(referralUri);

                // If there was an address previously specified, use it
                if (!string.IsNullOrEmpty(address))
                {
                    textBoxPermissionsContact.Text = address;
                }
                // Otherwise disable the checkbox
                else
                {
                    checkBoxPermissionsContact.Checked = false;
                }
            }

            if (grantDictionary != null)
            {
                _isAlreadyProtected = true;
                radioButtonPermissions.Checked = true;

                _rmLicenses = new List<RightsManagementLicense>(grantDictionary.Values);

                DateTime validUntil = DateTime.MaxValue;

                foreach (RightsManagementLicense license in grantDictionary.Values)
                {
                    if (license.ValidUntil > DateTime.UtcNow &&
                        license.ValidUntil < validUntil)
                    {
                        validUntil = license.ValidUntil;
                    }
                }

                DateTime localValidUntil = DateTimePicker.MaximumDateTime;

                // Convert the UTC time to local time if necessary
                if (validUntil < DateTime.MaxValue)
                {
                    localValidUntil = validUntil.ToLocalTime();
                }

                if (localValidUntil < DateTimePicker.MaximumDateTime)
                {
                    checkBoxValidUntil.Checked = true;
                    datePickerValidUntil.Enabled = true;
                    datePickerValidUntil.Value = localValidUntil;
                }
            }


            rightsTable.InitializeRightsTable(user.Name, grantDictionary);
            UpdateAnyoneEnabled();

            // Determine the list of available templates.
            InitializeTemplates();
            // Add templates to the UI (ComboBox).
            PopulateTemplateUI();

            // Attach our event handlers for the Save/SaveAs buttons here.
            // We do this here so that the event handlers can be Critical (and not TAS) to prevent
            // them from being invoked in other ways...
            buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            buttonSaveAs.Click += new System.EventHandler(this.buttonSaveAs_Click);

            // Update the radiobuttons to their initial state, which will also toggle the
            // Save buttons appropriately.
            UpdateRadioButtonState();

            // check if we can save the document with the current filename
            _canSave = false;

            DocumentManager documentManager = DocumentManager.CreateDefault();
            if (documentManager != null)
            {
                _canSave = documentManager.CanSave;
            }

            buttonSave.Enabled = _canSave;
        }
        #endregion Constructors

        //------------------------------------------------------
        // Internal Properties
        //------------------------------------------------------
        #region Internal Properties
        /// <summary>
        /// A list of all the RM licenses to grant for this document.
        /// </summary>
        internal IList<RightsManagementLicense> Licenses
        {
            get
            {
                return (radioButtonPermissions.Checked) ? _rmLicenses : null;
            }
        }

        /// <summary>
        /// The time until when the protection on the document is valid, in the
        /// UTC time zone.
        /// </summary>
        internal DateTime? ValidUntil
        {
            get
            {
                if (checkBoxValidUntil.Checked)
                {
                    return datePickerValidUntil.Value.ToUniversalTime();
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The address to contact for more permissions on the document. This
        /// may return null if none was specified.
        /// </summary>
        internal Uri ReferralUri
        {
            get
            {
                return _referralUri;
            }
        }

        /// <summary>
        /// Indicates the type of Save operation chosen.
        /// </summary>
        internal bool IsSaveAs
        {
            get
            {
                return _isSaveAs;
            }
        }

        /// <summary>
        /// The uri of the currently selected template.
        /// Will be null if no template is currently selected.
        /// </summary>
        internal Uri Template
        {
            get
            {
                // Check if a valid template was selected (we assume that the first template
                // is a placeholder for "<none>"
                if (radioButtonTemplate.Checked && (comboBoxTemplates.SelectedIndex < _templates.Count))
                {
                    return _templates[comboBoxTemplates.SelectedIndex].Template;
                }
                else
                {
                    // Since template was not found return the empty string.
                    return null;
                }
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        // Private Methods
        //------------------------------------------------------
        #region Private Methods

        /// <summary>
        /// Generates RightsManagementLicense objects for each grant specified by
        /// the user.
        /// </summary>
        /// <returns>A list of RightsManagementLicense objects</returns>
        private IList<RightsManagementLicense> CreateRightsManagementLicenses()
        {
            List<RightsManagementLicense> licenseList =
                new List<RightsManagementLicense>();

            // Enumerate through all the rows that correspond with users
            foreach (DataGridViewRow row in rightsTable.Rows)
            {
                if (row != null)
                {
                    RightsManagementLicense rmLicense = new RightsManagementLicense();

                    string userName = RightsTable.GetUsernameFromRow(row);

                    rmLicense.LicensedUser = GetUserFromUserName(userName);
                    rmLicense.LicensePermissions = RightsTable.GetPermissionsFromRow(row);
                    rmLicense.ValidFrom = DateTime.MinValue;
                    rmLicense.ValidUntil = DateTime.MaxValue;

                    if (ReferralUri != null)
                    {
                        rmLicense.ReferralInfoName = textBoxPermissionsContact.Text;
                        rmLicense.ReferralInfoUri = ReferralUri;
                    }

                    // If the user for the current license has not been given
                    // owner rights, and the dialog contains an expiration
                    // date, set the ValidFrom and ValidUntil dates in the
                    // license from the dialog

                    if (!rmLicense.HasPermission(RightsManagementPermissions.AllowOwner) &&
                        ValidUntil.HasValue)
                    {
                        rmLicense.ValidFrom = DateTime.UtcNow;
                        rmLicense.ValidUntil = ValidUntil.Value;
                    }

                    licenseList.Add(rmLicense);
                }
            }

            return licenseList;
        }

        /// <summary>
        /// Gets the template path from the registry and builds a list of
        /// ServerSideTemplates.
        /// </summary>
        private void InitializeTemplates()
        {
            List<ServerSideTemplate> templates = null;
            // Get the template path setting from the registry.
            _templatePath = GetTemplatePath();

            // Get the template file paths
            // This method will return an empty list if the path is invalid.
            templates = GetXmlTemplates();

            // Insert the default case, <none>
            templates.Insert(0, new ServerSideTemplate(null));

            // Assign ReadOnlyCollection
            _templates = new ReadOnlyCollection<ServerSideTemplate>(templates);
        }

        /// <summary>
        /// Sets up the ComboBox with data and handlers.
        /// </summary>
        private void PopulateTemplateUI()
        {
            // Set the template list as the ComboBox source.
            comboBoxTemplates.DataSource = _templates;
            // Add a handler to the SelectedValueChanged event to ensure that
            // the correct controls are enabled/disabled depending on whether
            // a template has been selected.
            comboBoxTemplates.SelectedValueChanged += new EventHandler(comboBox1_SelectedValueChanged);
            // Setup initial index value.
            comboBox1_SelectedValueChanged(null, null);
        }

        /// <summary>
        /// Used to get the value of HKCU\Software\Microsoft\XPSViewer\Common\DRM\AdminTemplatePath
        /// </summary>
        /// <returns>The contents of the key, otherwise string.Empty.</returns>
        private Uri GetTemplatePath()
        {
            string path = string.Empty;

            using (RegistryKey defaultRMKey = Registry.CurrentUser.OpenSubKey(
                _registryLocationForRMTemplates))
            {
                // Ensure key exists
                if (defaultRMKey != null)
                {
                    // If the requested key is available then attempt to get the value.
                    object keyValue = defaultRMKey.GetValue(_registryValueNameForRMTemplatePath);
                    if ((keyValue != null) &&
                        (defaultRMKey.GetValueKind(_registryValueNameForRMTemplatePath) == RegistryValueKind.String))
                    {
                        path = (string)keyValue;
                    }
                }
            }

            Uri newUri = null;
            // Attempt to build a new Uri of the found path value.
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    newUri = new Uri(path);
                }
                catch (UriFormatException e)
                {
                    // The two exceptions we could have are UriFormatException and
                    // ArgumentNullException.  Since we've validated against ArgumentNullException
                    // we only have to catch UriFormatException here.  This will silently catch
                    // the exception and fail to create a Uri (or fill in UI template values).
                    Trace.SafeWrite(
                        Trace.Rights,
                        "Exception Hit: {0}",
                        e);
                }
            }

            return newUri;
        }

        /// <summary>
        /// Builds a list of ServerSideTemplates based on any xml files found in the
        /// _templatePath directory.
        /// </summary>
        /// <returns>Collection of ServerSideTemplates, each one representing a
        /// template found in the _templatePath directory.</returns>
        private List<ServerSideTemplate> GetXmlTemplates()
        {
            List<Uri> templateList = new List<Uri>();

            // Only look for files if a path was found in the registry.
            if (_templatePath != null)
            {
                // Save the path local to this method to guarantee it does not
                // change between the assert and the file access
                string templateLocalPath = _templatePath.LocalPath;
                try
                {
                    DirectoryInfo dir = new DirectoryInfo(templateLocalPath);
                    // Ensure that the required path exists.
                    if (dir.Exists)
                    {
                        // Add each .xml file into the list
                        foreach (FileInfo file in dir.GetFiles("*.xml"))
                        {
                            templateList.Add(new Uri(file.FullName));
                        }
                    }
                }
                // Any exception above represents a failure to load templates.
                // Failing to load them is not a fatal error, so we can safely
                // continue with an empty list.
#pragma warning suppress 56500 // suppress PreSharp Warning 56500: Avoid `swallowing errors by catching non-specific exceptions..
                catch (Exception e)
                {
                    // Nothing should be done here, as we purposely catch all exceptions.
                    // If a directory fails to open, either from a system problem, or
                    // the registry value being set to garbage we should just fail
                    // to find a value (or populate the list) silently.
                    Trace.SafeWrite(
                        Trace.Rights,
                        "Exception Hit: {0}",
                        e);
                }
            }
            return ServerSideTemplate.BuildTemplateList(templateList);
        }

        /// <summary>
        /// Gets a User object from a user's name.
        /// </summary>
        /// <param name="userName">The name of the user.</param>
        /// <returns>A User object corresponding to the name</returns>
        private static RightsManagementUser GetUserFromUserName(string userName)
        {
            if (RightsTable.IsEveryone(userName))
            {
                return RightsManagementUser.AnyoneRightsManagementUser;
            }
            else
            {
                // XPS Viewer doesn't allow document authors to explicitly define the authentication type of the consumers.
                // So we are using WindowsPassport Authentication type as means of enabling both Passport and Windows 
                // consumers 
                return
                    RightsManagementUser.CreateUser(
                        userName,
                        AuthenticationType.WindowsPassport);
            }
        }

        /// <summary>
        /// Validates the Referral address entered into the dialog and prompts
        /// the user if the address is invalid.  If the address is valid,
        /// _referralUri is updated.
        /// </summary>
        /// <param name="isSaveAs"></param>
        /// <returns>A bool reflecting the validity of the address.</returns>      
        private bool ValidateReferralAddress()
        {
            bool ok = true;

            _referralUri = null;

            // If the permissions contact check box is checked, parse the
            // referral information to make a mailto URI.
            if (checkBoxPermissionsContact.Checked)
            {
                string permissionsAddress =
                    textBoxPermissionsContact.Text.Trim();

                if (!string.IsNullOrEmpty(permissionsAddress))
                {
                    _referralUri =
                        AddressUtility.GenerateMailtoUri(permissionsAddress);
                    ok = true;
                }
                // If no e-mail address was entered, we should show an
                // error dialog box and not let the form be closed
                else
                {
                    System.Windows.MessageBox.Show(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            SR.Get(SRID.RightsManagementWarnErrorNoReferralAddress),
                            textBoxPermissionsContact.Text),
                        SR.Get(SRID.RightsManagementWarnErrorTitle),
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);

                    textBoxPermissionsContact.Focus();
                    textBoxPermissionsContact.SelectAll();

                    ok = false;
                }
            }

            return ok;
        }

        /// <summary>
        /// Sets up a button on which an icon will be displayed, including all
        /// styling to apply to the button.
        /// </summary>
        /// <param name="button">The button</param>
        /// <param name="icon">The icon to put on the button</param>
        /// <param name="text">The text associated with the button</param>
        private void SetupIconButton(Button button, Icon icon, string text, string tooltip)
        {
            ImageList images = new ImageList();

            // The button is square, so the padding will be the same both
            // horizontally and vertically.
            int totalPadding = button.Padding.Horizontal + button.Margin.Horizontal;

            // Add one pixel of padding to ensure the image is not cut off by
            // the Windows button bevel
            totalPadding++;

            // Calculate the image size by subtracting the padding from the
            // size of the whole button
            images.ImageSize = System.Drawing.Size.Subtract(
                button.Size,
                new System.Drawing.Size(totalPadding, totalPadding));

            // Ensure the highest color icon is selected
            images.ColorDepth = ColorDepth.Depth32Bit;

            // Set the button to use the icon
            images.Images.Add(icon);
            button.ImageList = images;
            button.ImageIndex = 0;

            // Buttons with icons should look just like the icon image (with no
            // border or beveling). When the user focuses on it using Tab or
            // mouses over it, the button's beveling and border should return
            // until the user leaves the button again.

            // Set the button to appear flat with no border
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;

            // Set up event handlers to change the button's style when it is
            // focused or the mouse hovers over it
            button.MouseEnter += new EventHandler(OnIconButtonEnter);
            button.GotFocus += new EventHandler(OnIconButtonEnter);
            button.MouseLeave += new EventHandler(OnIconButtonLeave);
            button.LostFocus += new EventHandler(OnIconButtonLeave);

            // Since the button has no text, we need to set the AccessibleName
            button.AccessibleName = text;

            // Set the button's tooltip text.
            _toolTip.SetToolTip(button, tooltip);
        }

        /// <summary>
        /// Invoked when a setting is changed; this will enable/disable the Save/SaveAs buttons
        /// so that the settings may be committed to disk.
        /// </summary>
        /// <param name="isSaveAllowed">A bool value used to determine the state of save</param>
        private void ToggleSave(bool isSaveAllowed)
        {
            buttonSave.Enabled = isSaveAllowed && _canSave;
            buttonSaveAs.Enabled = isSaveAllowed;
        }

        /// <summary>
        /// Handler for grid selection change -- enables/disables the Remove button
        /// as appropriate.
        /// </summary>
        /// <param name="sender">Event sender (not used)</param>
        /// <param name="e">Event arguments (not used)</param>
        private void rightsTable_SelectionChanged(object sender, System.EventArgs e)
        {
            bool allowRemove = false;
            // make sure that an item is selected
            if (rightsTable.SelectedRows.Count == 1)
            {
                // retrieve the selected row
                DataGridViewRow row = rightsTable.SelectedRows[0];
                // if the row's index is greater than 0, then (and only then) allow remove
                // (the owner, who can't be removed, is at index 0)
                if (row.Index > 0)
                {
                    allowRemove = true;
                }
            }
            buttonRemoveUser.Enabled = allowRemove;
        }

        /// <summary>
        /// Handler for the "restrict permissions" radio buttons change event.
        /// </summary>
        /// <param name="sender">Event sender (not used)</param>
        /// <param name="e">Event arguments (not used)</param>
        private void radioButton_CheckedChanged(object sender, System.EventArgs e)
        {
            UpdateRadioButtonState();
        }

        /// <summary>
        /// Rotate the visible pane as appropriate.  Toggle the Save/SaveAs buttons depending
        /// on the current radio button selection.
        /// </summary>
        private void UpdateRadioButtonState()
        {
            if (this.radioButtonUnrestricted.Checked)
            {
                flowLayoutPanelUnrestricted.Visible = true;
                flowLayoutPanelPermissions.Visible = false;
                flowLayoutPanelTemplate.Visible = false;
                // If the document was already protected allow the owner unrestrict
                // the permissions.
                ToggleSave(_isAlreadyProtected);
            }
            else if (this.radioButtonPermissions.Checked)
            {
                flowLayoutPanelUnrestricted.Visible = false;
                flowLayoutPanelPermissions.Visible = true;
                flowLayoutPanelTemplate.Visible = false;
                ToggleSave(true);
            }
            else if (this.radioButtonTemplate.Checked)
            {
                flowLayoutPanelUnrestricted.Visible = false;
                flowLayoutPanelPermissions.Visible = false;
                flowLayoutPanelTemplate.Visible = true;
                // Enable save buttons if a template is selected.
                ToggleSave(comboBoxTemplates.SelectedIndex > 0);
            }
        }

        /// <summary>
        /// Handler for the "valid until" checkbox change event.
        /// </summary>
        /// <param name="sender">Event sender (not used)</param>
        /// <param name="e">Event arguments (not used)</param>
        private void checkBoxValidUntil_CheckedChanged(object sender, EventArgs e)
        {
            datePickerValidUntil.Enabled = checkBoxValidUntil.Checked;
        }

        /// <summary>
        /// Handler for the "request permissions from" checkbox change event.
        /// </summary>
        /// <param name="sender">Event sender (not used)</param>
        /// <param name="e">Event arguments (not used)</param>
        private void checkBoxPermissionsContact_CheckedChanged(object sender, EventArgs e)
        {
            textBoxPermissionsContact.Enabled = checkBoxPermissionsContact.Checked;
        }

        /// <summary>
        /// Handler for when the Add User button is clicked.
        /// </summary>
        /// <param name="sender">Event sender (not used)</param>
        /// <param name="e">Event arguments (not used)</param>
        private void buttonAddUser_Click(object sender, EventArgs e)
        {
            string userNames = textBoxUserName.Text;
            string[] userNameArray = null;

            if (string.IsNullOrEmpty(userNames))
            {
                System.Windows.MessageBox.Show(
                    SR.Get(SRID.RightsManagementWarnErrorNoAddress),
                    SR.Get(SRID.RightsManagementWarnErrorTitle),
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
            }

            userNameArray =
                userNames.Split(
                    SR.Get(SRID.RMPublishingUserSeparator).ToCharArray(),
                    StringSplitOptions.RemoveEmptyEntries);            

            foreach (string userName in userNameArray)
            {
                string trimmedName = userName.Trim();
                if (!string.IsNullOrEmpty(trimmedName))
                {
                    rightsTable.AddUser(new RightsTableUser(trimmedName));
                }
            }
            
            // Clear the text box so the user can enter a new username.
            textBoxUserName.Clear();

            textBoxUserName.Focus();

            UpdateAnyoneEnabled();
        }

        /// <summary>
        /// Handler for when the People Picker button is clicked.
        /// </summary>
        /// <param name="sender">Event sender (not used)</param>
        /// <param name="e">Event arguments (not used)</param>
        private void buttonPeoplePicker_Click(object sender, EventArgs e)
        {
            //Invoke the People Picker, which should return a user-selected
            //e-mail address or an empty array if nothing was selected.
            PeoplePickerWrapper picker = new PeoplePickerWrapper();
            String[] userNames = picker.Show(this.Handle);

            if (userNames != null && userNames.Length > 0)
            {
                textBoxUserName.Text =
                    string.Join(
                        SR.Get(SRID.RMPublishingUserSeparator),
                        userNames);
            }
        }

        /// <summary>
        /// Handler for when the Anyone button is clicked.
        /// </summary>
        /// <param name="sender">Event sender (not used)</param>
        /// <param name="e">Event arguments (not used)</param>
        private void buttonEveryone_Click(object sender, EventArgs e)
        {
            rightsTable.AddUser(new RightsTableUser(SR.Get(SRID.RMPublishingAnyoneUserDisplay)));
            UpdateAnyoneEnabled();
        }

        /// <summary>
        /// Handler for when the remove button is clicked. It removes the row of the
        /// sender from the table
        /// </summary>
        /// <param name="sender">Event sender (button clicked)</param>
        /// <param name="e">Event arguments (not used)</param>
        private void buttonRemoveUser_Click(object sender, EventArgs e)
        {
            rightsTable.DeleteUser();
            UpdateAnyoneEnabled();
        }

        /// <summary>
        /// Handler for when the Save button is clicked.
        /// </summary>
        /// <param name="sender">Event sender (not used)</param>
        /// <param name="e">Event arguments (not used)</param>    
        private void buttonSave_Click(object sender, EventArgs e)
        {
            // Check the Referral Address, and if it's OK we can continue to 
            // set the IsSaveAs flag and close the dialog.
            if (ValidateReferralAddress())
            {
                _rmLicenses = CreateRightsManagementLicenses();
                DialogResult = DialogResult.OK;
                _isSaveAs = false;

                this.Close();
            }
        }

        /// <summary>
        /// Handler for when the Save As button is clicked.
        /// </summary>
        /// <param name="sender">Event sender (not used)</param>
        /// <param name="e">Event arguments (not used)</param>     
        private void buttonSaveAs_Click(object sender, EventArgs e)
        {
            // Check the Referral Address, and if it's OK we can continue to 
            // set the IsSaveAs flag and close the dialog.
            if (ValidateReferralAddress())
            {
                _rmLicenses = CreateRightsManagementLicenses();
                DialogResult = DialogResult.OK;
                _isSaveAs = true;

                this.Close();
            }
        }
        
        /// <summary>
        /// Handler for when the user name text box gains focus.  This sets the dialog's
        /// accept button to the Add User button.
        /// </summary>
        /// <param name="sender">Event sender (not used)</param>
        /// <param name="e">Event arguments (not used)</param>
        private void textBoxUserName_GotFocus(object sender, System.EventArgs e)
        {
            this.AcceptButton = buttonAddUser;
        }

        /// <summary>
        /// Handler for when the user name text box loses focus.  This sets the dialog's
        /// accept button to the Save button.
        /// </summary>
        /// <param name="sender">Event sender (not used)</param>
        /// <param name="e">Event arguments (not used)</param>
        private void textBoxUserName_LostFocus(object sender, System.EventArgs e)
        {
            this.AcceptButton = buttonSaveAs;
        }

        /// <summary>
        /// Used to enable/disable controls based on the selected template.
        /// </summary>
        /// <param name="sender">Event sender (not used)</param>
        /// <param name="e">Event arguments (not used)</param>
        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            ToggleSave(comboBoxTemplates.SelectedIndex > 0);
        }

        /// <summary>
        /// Sets the sending button's style to Standard when it is focused or
        /// moused over.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">Event args (not used)</param>
        private static void OnIconButtonEnter(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button != null)
            {
                button.FlatStyle = FlatStyle.Standard;
            }
        }

        /// <summary>
        /// Sets the sending button's style to the Flat style when tab or mouse
        /// focus leaves.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">Event args (not used)</param>
        private static void OnIconButtonLeave(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button != null)
            {
                button.FlatStyle = FlatStyle.Flat;
            }
        }

        /// <summary>
        /// Enables or disables the Everyone button depending on whether Everyone
        /// is already in the table.
        /// </summary>
        private void UpdateAnyoneEnabled()
        {
            buttonEveryone.Enabled = !rightsTable.AnyoneUserPresent;
        }

        #endregion Private Methods

        //------------------------------------------------------
        // Private Properties
        //------------------------------------------------------
        #region Private Properties
        #endregion Private Properties

        //------------------------------------------------------
        // Private Fields
        //------------------------------------------------------
        #region Private Fields
        /// <summary>
        /// The list of licenses represented in this dialog
        /// </summary>
        private IList<RightsManagementLicense> _rmLicenses;

        /// <summary>
        /// The URI to contact for permissions.
        /// </summary>
        private Uri _referralUri;       

        /// <summary>
        /// A bool indicating the type of Save operation chosen.
        /// </summary>
        private bool _isSaveAs;

        /// <summary>
        /// A bool indicating if the document was protected before it was loaded this time.
        /// </summary>
        private bool _isAlreadyProtected;

        /// <summary>
        /// The path of the templates, as defined in the registry.
        /// </summary>
        private Uri _templatePath;

        /// <summary>
        /// List of files which are to be considered valid templates.
        /// </summary>
        private ReadOnlyCollection<ServerSideTemplate> _templates;

        private const string _registryBaseForRMTemplates =
            @"HKEY_CURRENT_USER\Software\Microsoft\XPSViewer\Common\DRM\";

        private const string _registryLocationForRMTemplates =
            @"Software\Microsoft\XPSViewer\Common\DRM";

        private const string _registryValueNameForRMTemplatePath = "AdminTemplatePath";

        /// <summary>
        /// Indicates whether you can can save replacing the current file.
        /// This is false for web based documents.
        /// </summary>
        private bool _canSave = false;

        #endregion Private Fields

        //------------------------------------------------------
        // Protected Methods
        //------------------------------------------------------
        #region Protected Methods
        /// <summary>
        /// ApplyResources override.  Called to apply dialog resources.
        /// </summary>
        protected override void ApplyResources()
        {
            base.ApplyResources();

            Text = SR.Get(SRID.RMPublishingTitle);

            radioButtonUnrestricted.Text = SR.Get(SRID.RMPublishingUnrestrictedRadio);
            radioButtonPermissions.Text = SR.Get(SRID.RMPublishingPermissionsRadio);
            radioButtonTemplate.Text = SR.Get(SRID.RMPublishingTemplateRadio);

            groupBoxMainContent.Text = SR.Get(SRID.RMPublishingMainContentGroupLabel);

            textBoxUnrestrictedText.Text = SR.Get(SRID.RMPublishingUnrestrictedText);

            labelSelectTemplate.Text = SR.Get(SRID.RMPublishingSelectTemplate);

            // Set the text for the Add User button first so we can use it to
            // calculate the size of the image buttons
            buttonAddUser.Text = SR.Get(SRID.RMPublishingAddUserButton);

            // Each of the image buttons is a square, and the height will be
            // the same as the text button
            System.Drawing.Size imageButtonSize = new System.Drawing.Size(
                buttonAddUser.Size.Height,
                buttonAddUser.Size.Height);

            // Set the Add User button's tooltip
            _toolTip.SetToolTip(buttonAddUser, SR.Get(SRID.RMPublishingAddUserButtonToolTip));

            buttonPeoplePicker.Size = imageButtonSize;
            buttonEveryone.Size = imageButtonSize;
            buttonRemoveUser.Size = imageButtonSize;

            // Set the button icons and text
            SetupIconButton(
                buttonPeoplePicker,
                (System.Drawing.Icon)Resources.RMPublishingPeoplePicker,
                SR.Get(SRID.RMPublishingPeoplePickerButton),
                SR.Get(SRID.RMPublishingPeoplePickerButtonToolTip));
            SetupIconButton(
                buttonEveryone,
                (System.Drawing.Icon)Resources.RMPublishingEveryone,
                SR.Get(SRID.RMPublishingAnyoneButton),
                SR.Get(SRID.RMPublishingAnyoneButtonToolTip));
            SetupIconButton(
                buttonRemoveUser,
                (System.Drawing.Icon)Resources.RMPublishingRemove,
                SR.Get(SRID.RMPublishingRemoveButton),
                SR.Get(SRID.RMPublishingRemoveButtonToolTip));

            checkBoxValidUntil.Text = SR.Get(SRID.RMPublishingExpiresOn);
            checkBoxPermissionsContact.Text =
                SR.Get(SRID.RMPublishingRequestPermissionsFrom);
            buttonSave.Text = SR.Get(SRID.RMPublishingSaveButton);
            _toolTip.SetToolTip(buttonSave, SR.Get(SRID.RMPublishingSaveButtonToolTip));
            buttonSaveAs.Text = SR.Get(SRID.RMPublishingSaveAsButton);
            _toolTip.SetToolTip(buttonSaveAs, SR.Get(SRID.RMPublishingSaveAsButtonToolTip));
            buttonCancel.Text = SR.Get(SRID.RMPublishingCancelButton);
        }
        #endregion Protected Methods

        //------------------------------------------------------
        //  Nested Classes
        //------------------------------------------------------
        #region Nested Classes
        //------------------------------------------------------
        //
        //  ServerSideTemplate
        //
        //------------------------------------------------------
        /// <summary>
        /// Represents a template to be displayed in the UI.
        /// </summary>
        private class ServerSideTemplate
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="filename">Reference to the template file. If null the <none> case
            /// will be used.</param>
            public ServerSideTemplate(Uri filename)
            {
                _template = filename;
            }

            /// <summary>
            /// Gets the actual full filename to the template.
            /// </summary>
            public Uri Template
            {
                get
                {
                    return _template;
                }
            }

            /// <summary>
            /// Used to determine what to display in the UI.
            /// </summary>
            /// <returns>Returns the name of the template (without path or extension).</returns>
            public override string ToString()
            {
                if (_template == null)
                {
                    return SR.Get(SRID.ServerSideTemplateDisplayNone);
                }
                // This will remove the path and extension information to make
                // a screen readable string.  Here we use LocalPath instead of
                // AbsolutePath since its result is unescaped (only the filename
                // is used so the rest of the path doesn't matter).
                return Path.GetFileNameWithoutExtension(_template.LocalPath);
            }

            /// <summary>
            /// Builds a List of ServerSideTemplate's from a list of Uri file references.
            /// </summary>
            /// <param name="files">List of files to match.</param>
            /// <returns>The list of ServerSideTemplates</returns>
            public static List<ServerSideTemplate> BuildTemplateList(List<Uri> files)
            {
                List<ServerSideTemplate> templates = new List<ServerSideTemplate>();

                if (files != null)
                {
                    foreach (Uri file in files)
                    {
                        templates.Add(new ServerSideTemplate(file));
                    }
                }

                return templates;
            }

            #region Private Fields
            private Uri _template;
            #endregion Private Fields
        }

        #endregion Nested Classes
    }
}
