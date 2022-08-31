// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Windows.TrustUI;
using System.Security;
using System.Security.RightsManagement;

using MS.Internal.Documents.Application;

namespace MS.Internal.Documents
{
    /// <summary>
    /// Class representing the Rights Management Permissions dialog.
    /// </summary>
    internal sealed partial class RMPermissionsDialog : DialogBaseForm
    {
        //--------------------------------------------------------------------------
        // Constructors
        //--------------------------------------------------------------------------
        #region Constructors
        /// <summary>
        /// Constructor to use for non-owners
        /// </summary>
        /// <param name="userLicense">The use license of the current user</param>
        internal RMPermissionsDialog(RightsManagementLicense userLicense)
        {
            // Set up labels
            authenticatedAsLabel.Text = userLicense.LicensedUser.Name;
            expiresOnLabel.Text = GetUtcDateAsString(userLicense.ValidUntil);

            InitializeReferralInformation(userLicense);

            // Set up list of rights
            AddPermissions(GetRightsFromPermissions(userLicense));
        }
        #endregion Constructors

        //--------------------------------------------------------------------------
        // Private Methods
        //--------------------------------------------------------------------------
        #region Private Methods

        /// <summary>
        /// Initialize the referral information (the name and the URI) to
        /// display in the UI.
        /// </summary>
        /// <param name="userLicense">The license from which to retrieve the
        /// information to display</param>
        private void InitializeReferralInformation(RightsManagementLicense userLicense)
        {
            string referralName = userLicense.ReferralInfoName;
            Uri referralUri = userLicense.ReferralInfoUri;

            // The referral information displayed is:
            //  - the referral URI. If that is not available,
            //  - the referral name. If that is not available,
            //  - the default text "Unknown"
            if (referralUri != null)
            {
                requestFromLabel.Text = referralUri.ToString();
            }
            else if (!string.IsNullOrEmpty(referralName))
            {
                requestFromLabel.Text = referralName;
            }

            // If the referral URI is a mailto URI, make the LinkLabel clickable
            if (referralUri != null && AddressUtility.IsMailtoUri(referralUri))
            {
                // LinkLabels have one Link in the Links list by default
                requestFromLabel.Links[0].Description = referralName;

                // Set up the click handler; it uses _referralUri
                _referralUri = referralUri;
                requestFromLabel.LinkClicked +=
                    new LinkLabelLinkClickedEventHandler(requestFromLabel_LinkClicked);
            }
            else
            {
                // If there is no referral URI or it is not a mailto: link, the
                // label should not appear clickable.
                requestFromLabel.Links.Clear();
            }
        }

        /// <summary>
        /// Generates a string for each individual right granted to a user as represented
        /// in a RightsManagementLicense object.
        /// </summary>
        /// <param name="license">The license to convert</param>
        /// <returns>An array of strings representing all rights granted</returns>
        private static string[] GetRightsFromPermissions(RightsManagementLicense license)
        {
            IList<string> rightsStrings = new List<string>();

            if (license.HasPermission(RightsManagementPermissions.AllowOwner))
            {
                rightsStrings.Add(SR.Get(SRID.RMPermissionsOwnerPermission));
            }
            else
            {
                if (license.HasPermission(RightsManagementPermissions.AllowView))
                {
                    rightsStrings.Add(SR.Get(SRID.RMPermissionsViewPermission));
                }

                if (license.HasPermission(RightsManagementPermissions.AllowPrint))
                {
                    rightsStrings.Add(SR.Get(SRID.RMPermissionsPrintPermission));
                }

                if (license.HasPermission(RightsManagementPermissions.AllowCopy))
                {
                    rightsStrings.Add(SR.Get(SRID.RMPermissionsCopyPermission));
                }

                if (license.HasPermission(RightsManagementPermissions.AllowSign))
                {
                    rightsStrings.Add(SR.Get(SRID.RMPermissionsSignPermission));
                }

                if (license.HasPermission(RightsManagementPermissions.AllowAnnotate))
                {
                    rightsStrings.Add(SR.Get(SRID.RMPermissionsAnnotatePermission));
                }
            }

            string[] stringArray = new string[rightsStrings.Count];
            rightsStrings.CopyTo(stringArray, 0);

            return stringArray;
        }

        /// <summary>
        /// Returns a string that is the local representation (in the local
        /// time zone) of a UTC date
        /// </summary>
        /// <param name="date">The date to represent</param>
        /// <returns>A string representing the date</returns>
        private static string GetUtcDateAsString(DateTime? date)
        {
            if (!date.HasValue ||
                date.Value.Equals(DateTime.MaxValue))
            {
                return SR.Get(SRID.RMPermissionsNoExpiration);
            }
            else
            {
                DateTime localDate = date.Value.ToLocalTime();
                return localDate.ToShortDateString();
            }
        }

        /// <summary>
        /// The handler for when the "request permissions from" link is
        /// clicked.  This navigates to the saved referral URI.
        /// </summary>
        /// <param name="sender">Event sender (not used)</param>
        /// <param name="e">Event arguments (not used)</param>
        /// <remarks>We save and use the cached referral URI for security
        /// reasons.  This function is an event handler, so it cannot be marked
        /// critical and can be called by anything, so it is probably is not
        /// safe to read information from the argument.</remarks>
        private void requestFromLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Navigate to the cached referral URI
            NavigationHelper.Navigate(new SecurityCriticalData<Uri>(_referralUri));
        }

        /// <summary>
        /// Adds the UI permission strings to the UI.
        /// </summary>
        /// <param name="uiStrings"></param>
        private void AddPermissions(string[] uiStrings)
        {
            foreach (string permission in uiStrings)
            {
                if (!string.IsNullOrEmpty(permission))
                {
                    // Create a new label and add it to the permissionsFlowPanel
                    Label permissionLabel = new Label();
                    permissionLabel.AutoSize = true;
                    permissionLabel.Text = permission;
                    permissionLabel.Margin = new Padding(13, 0, 3, 0);
                    permissionsFlowPanel.Controls.Add(permissionLabel);
                }
            }
        }
        #endregion Private Methods

        //------------------------------------------------------
        // Private Fields
        //------------------------------------------------------
        #region Private Fields
        /// <summary>
        /// The URI to contact for permissions.
        /// </summary>
        private Uri _referralUri;

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

            Text = SR.Get(SRID.RMPermissionsTitle);

            authenticatedAsTextLabel.Text = SR.Get(SRID.RMPermissionsAuthenticatedAs);
            permissionsHeldLabel.Text = SR.Get(SRID.RMPermissionsHavePermissions);
            requestFromTextLabel.Text = SR.Get(SRID.RMPermissionsRequestFrom);
            requestFromLabel.Text = SR.Get(SRID.RMPermissionsUnknownOwner);
            expiresOnTextLabel.Text = SR.Get(SRID.RMPermissionsExpiresOn);
            closeButton.Text = SR.Get(SRID.RMPermissionsCloseButton);
        }

        #endregion Protected Methods
    }
}
