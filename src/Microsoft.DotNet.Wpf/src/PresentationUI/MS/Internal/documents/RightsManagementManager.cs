// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//    DocumentRightsManagementManager is an internal API for Mongoose to deal
//    with Rights Management.
#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Security;
using System.Security.RightsManagement;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Windows.TrustUI;

using MS.Internal.Documents.Application;
using MS.Internal.PresentationUI;

namespace MS.Internal.Documents
{
    /// <summary>
    /// RightsManagementManager is a internal Avalon class used to expose the RM Document API .
    /// </summary>
    /// <remarks>
    /// This class serves as the controller that is between the UI and the facade for
    /// the RM APIs.
    /// </remarks>
    [FriendAccessAllowed]
    internal sealed class DocumentRightsManagementManager
    {
        #region Constructors
        //------------------------------------------------------
        // Constructors
        //------------------------------------------------------

        /// <summary>
        /// A constructor that takes the RM provider to use
        /// </summary>
        private DocumentRightsManagementManager(IRightsManagementProvider rmProvider)
        {
            if (rmProvider != null)
            {
                _rmProviderCache.Value = rmProvider;
            }
            else
            {
                throw new ArgumentNullException("rmProvider");
            }

            //Create dictionary for Credential Management
            //used to map between CredManResources and RM Users
            _userMap = new Dictionary<string, RightsManagementUser>();

            // notify the documentmanager when the license changes
            PublishLicenseChange += DocumentManager.OnModify;
        }

        #endregion Constructors

        #region Internal Methods
        //------------------------------------------------------
        // Internal Methods
        //------------------------------------------------------

        /// <summary>
        /// Initializes a new singleton instance of the DocumentRightsManagementManager
        /// </summary>
        /// <param name="rmProvider">The provider underlying the manager</param>
        internal static void Initialize(IRightsManagementProvider rmProvider)
        {
             Trace.SafeWrite(Trace.Rights, "Initializing RightsManagementManager");

             System.Diagnostics.Debug.Assert(
                 _currentManager.Value == null,
                 "RightsManagementManager initialized twice.");

             if (_currentManager.Value == null)
             {
                 _currentManager.Value = new DocumentRightsManagementManager(rmProvider);
             }
        }

        /// <summary>
        /// Decrypts the encrypted document using the appropriate credentials.
        /// Returns null if the document is not actually encrypted or the
        /// encrypted document could not be decrypted.
        /// </summary>
        /// <returns>A decrypted stream containing an XPS document.</returns>
        internal Stream DecryptPackage()
        {
            // If the RM client is not installed, prompt the user to install it.
            // If the user accepts, this will cause a navigation to the install site
            // in a separate window.
            if (!IsRMClientInstalled())
            {
                PromptToInstallRM();

                // Return null to abort document loading.
                return null;
            }

            Stream returnPackage = null;

            if (_rmProvider.IsProtected)
            {
                Trace.SafeWrite(Trace.Rights, "Document is protected.");

                // Try to get a set of credentials so we can try to decrypt
                // the document. If we can't choose any credentials at all,
                // we can't decrypt the document.
                if (ChooseCredentials(true))
                {
                    // See if the set of credentials chosen has permission to
                    // view the document.
                    RightsManagementLicense license = GetUseLicense();

                    if (license != null &&
                        license.HasPermission(RightsManagementPermissions.AllowView))
                    {
                        Trace.SafeWrite(
                            Trace.Rights,
                            "User has enough rights; decrypting package.");

                        try
                        {
                            returnPackage = _rmProvider.DecryptPackage();
                        }
                        catch (RightsManagementException exception)
                        {
                            RightsManagementErrorHandler.HandleOrRethrowException(
                                RightsManagementOperation.Decrypt,
                                exception);
                        }
                    }
                }
            }

            return returnPackage;
        }

        /// <summary>
        /// Forces an Evaluate which will result in RMPolicy event being fired.  This is
        /// only valid after a decrypted Package is available (i.e. either the protected
        /// package has been decrypted, or the package was never protected).
        /// </summary>
        internal void Evaluate()
        {
            RightsManagementStatus calcRMStatus = RightsManagementStatus.Unprotected;
            RightsManagementPolicy calcRMPolicy = RightsManagementPolicy.AllowView;

            //Check to see if document is protected.
            if (_rmProvider.IsProtected)
            {
                calcRMStatus = RightsManagementStatus.Protected;

                // Get the rights granted from the RM license
                RightsManagementLicense rmLicense = GetUseLicense();

                if (rmLicense == null)
                {
                    calcRMPolicy = RightsManagementPolicy.AllowNothing;
                }
                else
                {
                    calcRMPolicy = rmLicense.ConvertToPolicy();
                }
            }
            else
            {
                Trace.SafeWrite(
                    Trace.Rights,
                    "Evaluate: Document is not protected; using default policy.");

                //There is no RM policy applied to this document.
                calcRMPolicy =
                    RightsManagementPolicy.AllowAnnotate |
                    RightsManagementPolicy.AllowCopy |
                    RightsManagementPolicy.AllowSign |
                    RightsManagementPolicy.AllowPrint |
                    RightsManagementPolicy.AllowView;
            }

            //Fire the events.
            OnRMStatusChange(calcRMStatus);
            OnRMPolicyChange(calcRMPolicy);
        }

        /// <summary>
        /// Sets the EncryptedPackageEnvelope on the provider to a new value
        /// and invalidates the saved publish and use licenses if necessary.
        /// The package passed can be null to indicate that the new package is
        /// not protected.
        /// </summary>
        /// <param name="newPackage">The new encrypted package (possibly null)
        /// </param>
        internal void SetEncryptedPackage(EncryptedPackageEnvelope newPackage)
        {
            bool publishLicenseChanged = true;

            try
            {
                _rmProvider.SetEncryptedPackage(
                    newPackage,
                    out publishLicenseChanged);
            }
            catch (RightsManagementException exception)
            {
                RightsManagementErrorHandler.HandleOrRethrowException(
                    RightsManagementOperation.Decrypt,
                    exception);

                // On an exception here we can't continue; rethrow it in an
                // XPSViewer exception
                throw new XpsViewerException(
                    SR.Get(SRID.XpsViewerRightsManagementException),
                    exception);
            }

            if (_rmProvider.IsProtected)
            {
                // Get a use license for the new package
                RightsManagementLicense license = GetUseLicense();

                if (license == null ||
                    !license.HasPermission(RightsManagementPermissions.AllowView))
                {
                    throw new XpsViewerException(
                        SR.Get(SRID.RMProviderExceptionNoRightsToDocument));
                }
            }

            if (publishLicenseChanged)
            {
                // Fire the event to indicate a new publish license
                OnPublishLicenseChange();
            }

            // Evaluate the status of the new package (to fire events)
            Evaluate();
        }

        /// <summary>
        /// Displays the Credential Management UI.
        /// </summary>
        internal void ShowCredentialManagementUI()
        {
            ShowCredentialManagementUI(false);
        }

        /// <summary>
        /// Displays the Credential Management UI.
        /// </summary>
        /// <param name="decrypting">Whether the dialog is being shown in the
        /// process of decrypting the document for the first time</param>
        /// <returns>The result of the credential management dialog</returns>
        internal DialogResult ShowCredentialManagementUI(bool decrypting)
        {
            DialogResult result = DialogResult.Cancel;

            IList<string> accountList = GetCredentialManagementResourceList();
            string userAccount = GetDefaultCredentialManagementResource();
            RightsManagementUser previousDefaultUser = _rmProvider.GetDefaultCredentials();

            _credManagerDialog = new CredentialManagerDialog(accountList, userAccount, this);
            result = _credManagerDialog.ShowDialog();

            if (_credManagerDialog != null)
            {
                _credManagerDialog.Dispose();
            }

            RightsManagementUser newDefaultUser = _rmProvider.GetDefaultCredentials();

            // When a new rm account is added a new SecureEnvironment is created in order
            // to validate the new account.  If a document is currently protected this will
            // mean that the existing SecureEnvironment is destroyed, which puts the document
            // in an unreliable state.  The only way to correct this is to reload the document.
            // Check if the document is done decrypting, is protected, and if the
            // SecureEnvironment is in an unreliable state or the default user has been
            // changed.  If so reload the document.
            if (!decrypting && _rmProvider.IsProtected &&
                (!_isSecureEnvironmentReliable || !previousDefaultUser.Equals(newDefaultUser)))
            {
                DocumentManager.CreateDefault().Reload(null);
            }
            // Check if the default user has changed, and setup an environment for this user.
            // This case will only be hit for unprotected, or undecrypted documents (by the
            // previous if statement) where it is reliable to create a new SecureEnvironment.
            else if (!previousDefaultUser.Equals(newDefaultUser))
            {
                try
                {
                    _rmProvider.InitializeEnvironment(newDefaultUser);
                }
                catch (RightsManagementException exception)
                {
                    bool handled = RightsManagementErrorHandler.HandleOrRethrowException(
                        RightsManagementOperation.Other,
                        exception);
                }
            }


            return result;
        }


        /// <summary>
        /// ShowEnrollment:  Displays the RM enrollment.
        /// </summary>
        internal void ShowEnrollment()
        {
            DialogResult result = DialogResult.Cancel;
            EnrollmentAccountType enrollmentAccountType = EnrollmentAccountType.None;

            // skip first page if already enrolled
            if (_rmProvider.CurrentUser != null || _rmProvider.GetDefaultCredentials() != null)
            {
                result = DialogResult.OK;
            }
            else
            {
                //Show the First page
                using (RMEnrollmentPage1 page1 = new RMEnrollmentPage1())
                {
                    result = page1.ShowDialog();
                }
            }

            if (result == DialogResult.OK)
            {
                using (RMEnrollmentPage2 page2 = new RMEnrollmentPage2())
                {
                    //Show the second page
                    result = page2.ShowDialog();
                    enrollmentAccountType = page2.AccountTypeSelected;
                }
            }

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                //It is now time to enroll.
                Enroll(enrollmentAccountType);
            }
        }

        /// <summary>
        /// Enroll.
        /// </summary>
        /// <param name="accountType">The account type to enroll</param>
        /// <returns>Whether or not enrollment succeeded</returns>
        internal bool Enroll(EnrollmentAccountType accountType)
        {
            bool rtn = false;

            //User has selected to proceed.  Now we need to Enroll.  This will
            //Require two thing.  1) start the blocking enroll call 
            //(call QueueWorkItem) and 2) showing a status/progress Dialog while 
            //we are enrolling.
            RMEnrollmentPage3 rmEnrollmentPage3 = new RMEnrollmentPage3();

            RightsManagementEnrollThreadInfo rmEnrollThreadInfo = new RightsManagementEnrollThreadInfo();

            //Setup Fields
            rmEnrollThreadInfo.AccountType = accountType;
            rmEnrollThreadInfo.ProgressForm = rmEnrollmentPage3;

            // Pass work off so UI doesn't block.
            // We use WaitCallback here because that is the delegate that is
            // supposed to be passed to QueueUserWorkItem
            if (ThreadPool.QueueUserWorkItem(new WaitCallback(RMEnrollThreadProc), rmEnrollThreadInfo))
            {
                rmEnrollmentPage3.ShowDialog();

                rtn = (_rmProvider.CurrentUser != null);
            }

            // Since we've called the enrollment, the current SecureEnvironment is not reliable.
            _isSecureEnvironmentReliable = false;

            return rtn;
        }
        
        /// <summary>
        /// Launches the My Permissions UI.
        /// </summary>
        internal void ShowPermissions()
        {
            RightsManagementLicense rmLicense = GetUseLicense();

            // A null license means the user has no rights to the document, and
            // this should never have been called.
            Invariant.Assert(
                rmLicense != null,
                "Permissions dialog requested with no rights on the document.");

            // If this is the owner call ShowPublishing instead.
            if (rmLicense.HasPermission(RightsManagementPermissions.AllowOwner))
            {
                ShowPublishing();
            }
            else
            {
                // User wants to view the My Permissions dialog.
                RMPermissionsDialog rmPermissionsPage = new RMPermissionsDialog(rmLicense);
                rmPermissionsPage.ShowDialog();

                if (rmPermissionsPage != null)
                {
                    rmPermissionsPage.Dispose();
                }
            }
        }

        /// <summary>
        /// Displays the publishing UI.
        /// </summary>
        internal void ShowPublishing()
        {
            if (!ChooseCredentials(false))
            {
                // If there are no credentials, and we can't select any, we
                // should just bail out.  If there was an error somewhere
                // during choosing credentials, an explanatory message box
                // should already have been shown.
                return;
            }

            if (_rmProvider.IsProtected)
            {
                RightsManagementLicense license = GetUseLicense();
                if (license == null ||
                    !(license.HasPermission(RightsManagementPermissions.AllowOwner)))
                {
                    throw new InvalidOperationException(
                        SR.Get(SRID.RMProviderExceptionNotOwnerOfDocument));
                }
            }

            IDictionary<RightsManagementUser, RightsManagementLicense> allRights = null;
            if (_rmProvider.IsProtected)
            {
                allRights = _rmProvider.GetAllAccessRights();
            }

            // Create the publishing dialog outside of the while loop so that it is only
            // constructed once, rather than on each loop.
            RMPublishingDialog rmPublish = null;
            bool statusChanged = false;

            try
            {
                rmPublish =
                    new RMPublishingDialog(_rmProvider.CurrentUser, allRights);

                // Keep showing the dialog until it succeeds or the user cancels
                while (true)
                {
                    DialogResult result = DialogResult.Cancel;

                    // Display the dialog to the user.
                    result = rmPublish.ShowDialog();

                    // If the user canceled out of the dialog, break out of the loop and 
                    // do not apply changes.
                    if (result != DialogResult.OK)
                    {
                        break;
                    }

                    bool publishingSuccess = false;
                    bool publishLicenseChanged = false;
                    bool exitDialog = false;

                    // Save the current set of licenses for rollback
                    _rmProvider.SaveCurrentLicenses();

                    // If a template exists on the publishing dialog then user has selected
                    // a template rather than individual settings.
                    if (rmPublish.Template != null)
                    {
                        publishingSuccess = ProcessRMTemplate(rmPublish.Template, out exitDialog);
                        publishLicenseChanged = true;
                    }
                    // Template was not selected, so carry on with individual permissions.
                    else
                    {
                        publishingSuccess = ProcessRMLicenses(
                            rmPublish.Licenses,
                            out exitDialog,
                            out publishLicenseChanged);
                    }

                    // Check if a signed publish license was successfully created.
                    // If not take the appropriate action.
                    if (!publishingSuccess)
                    {
                        // If exitDialog is true, then an error has happened that is severe
                        // enough to stop running the Publish dialog.
                        if (exitDialog)
                        {
                            return;
                        }
                        // Otherwise continue with running the dialog for another attempt
                        // to get the settings correct.
                        else
                        {
                            continue;
                        }
                    }

                    // If the publish license actually changed during the
                    // publishing operation, fire the event to indicate this
                    if (publishLicenseChanged)
                    {
                        OnPublishLicenseChange();
                    }

                    // Save the protected document.
                    DocumentManager docManager = DocumentManager.CreateDefault();
                    bool saveSuccess = false;

                    if (docManager != null)
                    {
                        if (rmPublish.IsSaveAs)
                        {
                            saveSuccess = docManager.SaveAs(null);
                        }
                        else
                        {
                            saveSuccess = docManager.Save(null);
                        }
                    }

                    if (publishingSuccess && !saveSuccess)
                    {
                        _rmProvider.RevertToSavedLicenses();
                        continue;
                    }

                    statusChanged =
                        publishingSuccess && saveSuccess && publishLicenseChanged;

                    break;
                } // end while(true)
            }
            finally
            {
                if (rmPublish != null)
                {
                    rmPublish.Dispose();
                }
            }

            // If the status changed, call Evaluate to re-evaluate the RM
            // status of the document and fire the appropriate events
            if (statusChanged)
            {
                Evaluate();
            }
        }

        /// <summary>
        /// Get User account from credentialManagementResources and sets as default.
        /// </summary>
        internal void OnCredentialManagementSetDefault(string defaultAccount)
        {
            //Get user from user name
            RightsManagementUser user = _userMap[defaultAccount];

            //if found set default
            if (user != null)
            {
                _rmProvider.SetDefaultCredentials(user);
            }
        }
        
        /// <summary>
        /// Removes user from available user list.
        /// </summary>
        internal void OnCredentialManagementRemove(string accountName)
        {
            //Get user from hash
            RightsManagementUser user = _userMap[accountName];

            //if found remove (if this user isn't in use, if it is in use warn user)
            if (user != null && !user.Equals(_rmProvider.CurrentUser))
            {
                try
                {
                    _rmProvider.RemoveCredentials(user);

                    if (_credManagerDialog != null)
                    {
                        //Set the data source for the listbox
                        _credManagerDialog.SetCredentialManagementList(
                            GetCredentialManagementResourceList(),
                            GetDefaultCredentialManagementResource());
                    }
                }
                catch (RightsManagementException exception)
                {
                    RightsManagementErrorHandler.HandleOrRethrowException(
                        RightsManagementOperation.Other,
                        exception);

                    // If the remove failed just bail out
                    return;
                }
            }
            else
            {
                System.Windows.MessageBox.Show(
                    SR.Get(SRID.RightsManagementWarnErrorFailedToRemoveUser),
                    SR.Get(SRID.RightsManagementWarnErrorTitle),
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Exclamation
                    );
            }
        }
        
        /// <summary>
        /// Calls the Enrollment and refreshes the listbox.
        /// </summary>
        internal void OnCredentialManagementShowEnrollment()
        {
            ShowEnrollment();

            if (_credManagerDialog != null)
            {
                //Set the data source for the listbox
                _credManagerDialog.SetCredentialManagementList(
                    GetCredentialManagementResourceList(),
                    GetDefaultCredentialManagementResource());
            }
        }

        /// <summary>
        /// Prompts the user to see if he or she would like to install the RM Client.
        /// If the user so chooses, we will then start a top-level navigation to 
        /// the external install location for the RM Client.
        /// </summary>
        internal void PromptToInstallRM()
        {
            // Notify the user that RM is not installed, and ask
            // if they would like to install it now.
            System.Windows.MessageBoxResult dialogResult = System.Windows.MessageBox.Show(
                                SR.Get(SRID.RightsManagementWarnErrorRMNotInstalled),
                                SR.Get(SRID.RightsManagementWarnErrorTitle),
                                System.Windows.MessageBoxButton.YesNo,
                                System.Windows.MessageBoxImage.Warning);

            if (dialogResult == System.Windows.MessageBoxResult.Yes)
            {
                // Navigate to the install location
                const string rmClientInstallLocation = "http://go.microsoft.com/fwlink/?LinkID=18134";
                NavigationHelper.NavigateToExternalUri(new Uri(rmClientInstallLocation));
            }
        }

        /// <summary>
        /// Gets a List of CredentialManagmentResources.
        /// </summary>
        internal IList<string> GetCredentialManagementResourceList()
        {
            _userMap.Clear();

            //create list for CredentialManagementResources
            IList<string> accountList = new List<string>();

            //Loop through each user and create listbox string and add to return list
            foreach (RightsManagementUser user in _rmProvider.GetAvailableCredentials())
            {
                string accountName =
                    RightsManagementResourceHelper.GetCredentialManagementResources(user);

                if (!accountList.Contains(accountName))
                {
                    //Add to map and to list.
                    _userMap.Add(accountName, user);
                    accountList.Add(accountName);
                }
            }

            return accountList;
        }

        /// <summary>
        /// Gets default CredentialManagmentResources.
        /// </summary>
        internal string GetDefaultCredentialManagementResource()
        {
            string userAccount = null;

            RightsManagementUser user = _rmProvider.GetDefaultCredentials();

            if (user != null)
            {
                userAccount = RightsManagementResourceHelper.GetCredentialManagementResources(user);
            }

            return userAccount;
        }              

        #endregion Internal Methods

        #region Internal Properties
        //------------------------------------------------------
        // Internal Properties
        //------------------------------------------------------

        /// <summary>
        /// Gets the current singleton instance of the DocumentRightsManagementManager
        /// </summary>
        internal static DocumentRightsManagementManager Current
        {
            get
            {
                return _currentManager.Value;
            }
        }

        /// <summary>
        /// Gets the current publish license associated with the document.
        /// </summary>
        internal PublishLicense PublishLicense
        {
            // Commented out since not used, but left to show that a set only property
            // was implemented by design.
            // get { return _rmProvider.CurrentPublishLicense; }
            set
            {
                if (_rmProvider.CurrentPublishLicense != value)
                {
                    _rmProvider.CurrentPublishLicense = value;
                    OnPublishLicenseChange();
                }
            }
        }

        /// <summary>
        /// Gets whether or not the user has permission to save the document.
        /// </summary>
        internal bool HasPermissionToSave
        {
            get
            {
                return
                    !_rmProvider.IsProtected ||
                    GetUseLicense().HasPermission(RightsManagementPermissions.AllowCopy);
            }
        }

        /// <summary>
        /// Gets whether or not the user has permission to edit the document.
        /// </summary>
        internal bool HasPermissionToEdit
        {
            get
            {
                return
                    !_rmProvider.IsProtected ||
                    GetUseLicense().HasPermission(RightsManagementPermissions.AllowEdit);
            }
        }

        /// <summary>
        /// Returns true if we find the RM Client is installed on the machine.
        /// </summary>
        /// <remarks> 
        /// We only make the actual check if RM is installed
        /// once per application instance. Is this okay? Or should we keep
        /// checking to see if this gets installed over the course of the 
        /// application?
        /// </remarks>
        internal bool IsRMInstalled
        {
            get
            {
                if (! _determinedRMInstallState)
                {
                    // Find out if the RM Client is installed. 
                    _isRMInstalled = IsRMClientInstalled();
                    _determinedRMInstallState = true;
                }

                return _isRMInstalled;
            }
        }

        #endregion Internal Properties

        #region Internal Event
        //------------------------------------------------------
        // Internal Event
        //------------------------------------------------------

        internal event RMStatusChangeHandler RMStatusChange;

        internal event EventHandler PublishLicenseChange;

        internal event RMPolicyChangeHandler RMPolicyChange;

        #endregion Internal Event

        #region Internal Delegate
        //------------------------------------------------------
        // Internal Delegate
        //------------------------------------------------------

        internal delegate void RMStatusChangeHandler(object sender,
                                                     RightsManagementStatusEventArgs args
                                                     );

        internal delegate void RMPolicyChangeHandler(object sender,
                                                     RightsManagementPolicyEventArgs args
                                                     );

        #endregion Internal Delegate

        #region Private Methods
        //------------------------------------------------------
        // Private Methods
        //------------------------------------------------------

        /// <summary>
        /// Saves the use license back into the package.
        /// </summary>
        private void SaveUseLicense()
        {
            //This action should occur after other work has been completed.
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                (DispatcherOperationCallback)delegate(object arg)
                {
                    DocumentManager docManager = DocumentManager.CreateDefault();
                    if (docManager.CanSave)
                    {
                        Trace.SafeWrite(
                            Trace.Rights,
                            "Re-opening document for edit before saving Use License");
                        docManager.EnableEdit(null);
                    }

                    // Check if saving is still possible (i.e. the file could
                    // be reopened for edit).
                    //
                    // This breaks encapsulation -- we happen to know that
                    // docManager.CanSave indicates only that the file could
                    // be opened writeable. We also know that saving an RM
                    // protected document will save the use license back to
                    // the file.
                    if (docManager.CanSave)
                    {
                        Trace.SafeWrite(
                            Trace.Rights,
                            "Opened original file writeable; saving Use License");
                        docManager.Save(null);
                    }

                    return null;
                }, null /* DispatcherOperation argument, none specified */);
        } 

        /// <summary>
        /// Determines if the RM client is installed by looking for  
        /// MSDRM.DLL in the user's system folder.
        /// </summary>
        /// <returns>Whether RM Client DLL, msdrm.dll, was found</returns>
        private bool IsRMClientInstalled()
        {
            bool foundRMClient = false;

            // Build the path for the MSDRM.DLL
            string systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);

            // Append the DLL filename.
            const string msdrmDLLName = "MSDRM.DLL";
            string msdrmdllPath = Path.Combine(systemPath, msdrmDLLName);
            // Check if the DLL exists.
            foundRMClient = File.Exists(msdrmdllPath);

            return foundRMClient;
        }

        /// <summary>
        /// Attempts to automatically choose the credentials to use to access
        /// the RM-protected document or add RM protection to an existing
        /// document.
        /// </summary>
        /// <param name="decrypting">Whether or not we are choosing credentials
        /// to decrypt or encrypt a document</param>
        /// <returns>Whether or not credentials were successfully chosen</returns>
        private bool ChooseCredentials(bool decrypting)
        {
            bool foundCredentials = false;

            // If there already is a user selected there is no need to select
            // credentials again
            if (_rmProvider.CurrentUser != null)
            {
                return true;
            }

            //Get default user
            RightsManagementUser user = _rmProvider.GetDefaultCredentials();

            // Save whether or not RM has been initialized so that we don't do
            // it twice
            bool initialized = false;

            //Check to see if we have a user.  If we don't we need to enroll.
            if (user == null)
            {
                //Show enrollment UI
                ShowEnrollment();

                // Get the current user.  If we don't have one after enrollment
                // that means the user cancelled enrollment or something else
                // went wrong.
                user = _rmProvider.CurrentUser;

                // If the enrollment was successful
                if (user != null)
                {
                    // Set the enrolled user as the default user in the registry
                    _rmProvider.SetDefaultCredentials(user);
                    initialized = true;
                }
            }

            // Now we will see if this user account has rights for the document
            while (user != null)
            {
                if (!initialized)
                {
                    try
                    {
                        // Attempt to initialize the environment
                        _rmProvider.InitializeEnvironment(user);

                        initialized = true;
                    }
                    catch (RightsManagementException exception)
                    {
                        bool handled =
                            RightsManagementErrorHandler.HandleOrRethrowException(
                                decrypting ?
                                    RightsManagementOperation.Initialize :
                                    RightsManagementOperation.Other,
                                exception);

                        // If the error couldn't be handled cleanly, give up
                        if (!handled)
                        {
                            break;
                        }
                    }
                }

                // If the initialization succeeded and the document is
                // protected by RM, check if the current user can view the
                // document
                if (initialized && _rmProvider.IsProtected)
                {
                    RightsManagementLicense license = GetUseLicense();

                    // Check if the user has permissions to view the document
                    if (license != null &&
                        license.HasPermission(RightsManagementPermissions.AllowView))
                    {
                        foundCredentials = true;
                        break;
                    }

                    // The current user doesn't have permissions
                    else
                    {
                        // Ask the user whether we should try a different set of
                        // credentials or just give up.

                        System.Windows.MessageBoxResult result =
                            System.Windows.MessageBox.Show(
                                SR.Get(SRID.RightsManagementWarnErrorNoPermission),
                                SR.Get(SRID.RightsManagementWarnErrorTitle),
                                System.Windows.MessageBoxButton.YesNo,
                                System.Windows.MessageBoxImage.Exclamation
                                );

                        // If the user didn't want to continue, bail out
                        if (result != System.Windows.MessageBoxResult.Yes)
                        {
                            break;
                        }
                    }
                }
                // If the document is not protected, there is no need to check
                // whether the user can view the document as long as the
                // environment was properly initialized
                else if (initialized)
                {
                    foundCredentials = true;
                    break;
                }

                // The current default user doesn't have permission to view
                // this container, or there was an error when attempting to
                // initialize the environment with the current default user.
                // 
                // Show the CredMan to allow user to change default account
                if(ShowCredentialManagementUI(decrypting) != DialogResult.OK)
                {
                    break;
                }

                //Get new default user.
                user = _rmProvider.GetDefaultCredentials();

                // The provider's CurrentUser property is set only when an
                // environment has been initialized. As a result comparing the
                // credentials we will use to the provider's CurrentUser can
                // tell us whether or not the provider has been initialized for
                // that user.
                initialized = (user.Equals(_rmProvider.CurrentUser));
            }

            return foundCredentials;
        }

        /// <summary>
        /// Retrieves the use license for the document and returns the rights granted to
        /// the current user.  Returns null if the user does not have rights to the
        /// document.
        /// </summary>
        /// <returns>The rights granted to the user.</returns>
        private RightsManagementLicense GetUseLicense()
        {            
            if (_rmProvider.IsProtected &&
                _rmProvider.CurrentUseLicense == null)
            {
                Invariant.Assert(
                    _rmProvider.CurrentUser != null,
                    "No user for whom to get a license.");

                try
                {
                    if (_rmProvider.LoadUseLicense())
                    {
                        _rmProvider.BindUseLicense();
                    }
                    else if (_rmProvider.AcquireUseLicense())
                    {
                        // We have just acquired a new use license; this needs to be stored back
                        // into the package.
                        _rmProvider.BindUseLicense();
                        SaveUseLicense();
                    }
                    else
                    {
                        // No use license was found, which means that the
                        // current user has no rights to the document. We
                        // return null and let the caller decide what to do.
                    }
                }
                catch (RightsManagementException exception)
                {
                    // Whether or not the exception was handled is irrelevant,
                    // since if the use license wasn't bound, the function will
                    // return null
                    RightsManagementErrorHandler.HandleOrRethrowException(
                        RightsManagementOperation.Decrypt,
                        exception);
                }
            }

            return _rmProvider.CurrentUseLicense;
        }

        /// <summary>
        /// Checks if the grants listed in the licenses passed in as an argument are
        /// different from the ones already granted on the document.
        /// </summary>
        /// <param name="newGrants">A list of new grants to compare with the existing
        /// set</param>
        /// <exception cref="System.Security.RightsManagement.RightsManagementException">
        /// Can be thrown by the RM APIs when there is an error when getting
        /// all existing access rights on the document</exception>
        /// <returns>Whether or not the new grants are different</returns>
        private bool HasPublishLicenseChanged(IList<RightsManagementLicense> newGrants)
        {
            // If the desired state is that the document will not be protected (i.e. no
            // licenses are specified), we check if the document was already not
            // protected.
            if (newGrants == null || newGrants.Count == 0)
            {
                return _rmProvider.IsProtected;
            }

            // Check if we are adding protection to an unprotected document
            if (!_rmProvider.IsProtected)
            {
                return true;
            }

            // This might throw, but it will be caught in the caller
            IDictionary<RightsManagementUser, RightsManagementLicense> allRights =
                _rmProvider.GetAllAccessRights();

            // Check if the number of licenses is different
            if (newGrants.Count != allRights.Count)
            {
                return true;
            }

            // Look through each license to see if it is different from the old ones.
            // We already know that the number of licenses is the same in both lists of
            // licenses, so any other differences will be detected here.
            foreach (RightsManagementLicense newLicense in newGrants)
            {
                if (!allRights.ContainsKey(newLicense.LicensedUser))
                {
                    return true;
                }

                RightsManagementLicense existingLicense = allRights[newLicense.LicensedUser];

                // Check if both licenses grant the same permissions
                if (newLicense.LicensePermissions !=
                    existingLicense.LicensePermissions)
                {
                    return true;
                }

                if (DateTime.Compare(
                    newLicense.ValidUntil,
                    existingLicense.ValidUntil) != 0)
                {
                    return true;
                }

                if (!Uri.Equals(
                    newLicense.ReferralInfoUri,
                    existingLicense.ReferralInfoUri))
                {
                    return true;
                }

            }

            return false;
        }
        
        /// <summary>
        /// The thread operation that enrolls a new Rights Management user.
        /// </summary>
        private void RMEnrollThreadProc(Object stateInfo)
        {
            Trace.SafeWrite(Trace.Rights, "Enrollment thread started.");

            //Get Thread Info
            RightsManagementEnrollThreadInfo rmEnrollThreadInfo = (RightsManagementEnrollThreadInfo)stateInfo;

            //Enroll
            try
            {
                _rmProvider.InitializeEnvironment(rmEnrollThreadInfo.AccountType);
                //Enrollment is done.  Let's close the Progress dialog.
                rmEnrollThreadInfo.ProgressForm.Invoke(new MethodInvoker(rmEnrollThreadInfo.ProgressForm.Close));
            }
            catch (RightsManagementException exception)
            {
                rmEnrollThreadInfo.ProgressForm.Invoke(new MethodInvoker(rmEnrollThreadInfo.ProgressForm.Close));
                RightsManagementOperation operation =
                    RightsManagementOperation.Other;

                if (rmEnrollThreadInfo.AccountType == EnrollmentAccountType.NET)
                {
                    operation = RightsManagementOperation.PassportActivation;
                }

                RightsManagementErrorHandler.HandleOrRethrowException(
                    operation,
                    exception);
            }
            // Close the progress dialog in the event of any exception.  This prevents the
            // dialog from remaining open when the error page appears.  We cannot use a finally
            // block here because in the case of an uncaught exception the unhandled exception
            // handler will get the exception (and show the error page) before the block runs.
            catch
            {
                rmEnrollThreadInfo.ProgressForm.Invoke(new MethodInvoker(rmEnrollThreadInfo.ProgressForm.Close));
                throw;
            }

            Trace.SafeWrite(Trace.Rights, "Enrollment thread ended.");
        }

        /// <summary>
        /// Creates an appropriate EventArgs and sends the RMStatusChange event.
        /// </summary>
        /// <param name="newStatus">The RM status to put in the EventArgs
        /// </param>
        private void OnRMStatusChange(RightsManagementStatus newStatus)
        {
            Trace.SafeWrite(Trace.Rights, "RightsManagementStatus changed.");

            RightsManagementStatusEventArgs args = new RightsManagementStatusEventArgs(
                newStatus);

            RaiseRMStatusChange(args);
        }

        /// <summary>
        /// Raises an RMStatusChange event with the given arguments.
        /// </summary>
        /// <param name="args">The event arguments</param>
        private void RaiseRMStatusChange(RightsManagementStatusEventArgs args)
        {
            if (RMStatusChange != null)
            {
                RMStatusChange(this, args);
            }
        }

        /// <summary>
        /// Creates an appropriate EventArgs and sends the RMPolicyChange event.
        /// </summary>
        /// <param name="newPolicy">The RM policy to put in the EventArgs
        /// </param>
        private void OnRMPolicyChange(RightsManagementPolicy newPolicy)
        {
            Trace.SafeWrite(Trace.Rights, "RightsManagementPolicy changed.");
            RightsManagementPolicyEventArgs args = new RightsManagementPolicyEventArgs(newPolicy);

            RaiseRMPolicyChange(args);
        }

        /// <summary>
        /// Raises an RMPolicyChange event with the given arguments.
        /// </summary>
        /// <param name="args">The event arguments</param>
        private void RaiseRMPolicyChange(RightsManagementPolicyEventArgs args)
        {
            if (RMPolicyChange != null)
            {
                RMPolicyChange(this, args);
            }
        }

        /// <summary>
        /// Fires the event that indicates that the publish license has changed.
        /// </summary>
        private void OnPublishLicenseChange()
        {
            Trace.SafeWrite(Trace.Rights, "Publish License changed.");

            RaisePublishLicenseChange(null);
        }

        /// <summary>
        /// Raises the PublishLicenseChange with the given arguments.
        /// </summary>
        /// <param name="args">The event arguments</param>
        private void RaisePublishLicenseChange(EventArgs args)
        {
            if (PublishLicenseChange != null)
            {
                PublishLicenseChange(this, args);
            }
        }

        /// <summary>
        /// Using the provided list of licenses, attempt to setup a signed
        /// publish license for the document.
        /// </summary>
        /// <param name="licenses">The licenses to process.</param>
        /// <param name="exitDialog">True if an error has occurred that should
        /// exit the RMPublishing dialog</param>
        /// <param name="publishLicenseChanged">True if the grants passed in
        /// are different from the ones in the document</param>
        /// <returns>True on success, false on failure.</returns>
        private bool ProcessRMLicenses(
            IList<RightsManagementLicense> licenses,
            out bool exitDialog,
            out bool publishLicenseChanged)
        {
            // By default exitDialog is true so that on unhandled errors the publish
            // dialog will not reload.
            exitDialog = true;

            publishLicenseChanged = true;

            try
            {
                publishLicenseChanged = HasPublishLicenseChanged(licenses);
            }
            catch (RightsManagementException exception)
            {
                bool handled =
                    RightsManagementErrorHandler.HandleOrRethrowException(
                        RightsManagementOperation.Other,
                        exception);

                // If there was an exception (that could not be
                // handled) that occurred in detecting whether the
                // publish license changed, bail out
                if (!handled)
                {
                    return false;
                }
            }

            // If the new publish license is unchanged don't bother
            // doing anything else
            if (publishLicenseChanged)
            {
                if (licenses != null && licenses.Count > 0)
                {
                    try
                    {
                        _rmProvider.GenerateUnsignedPublishLicense(licenses);
                        _rmProvider.SignPublishLicense();
                    }
                    catch (RightsManagementException exception)
                    {
                        bool handled =
                            RightsManagementErrorHandler.HandleOrRethrowException(
                                RightsManagementOperation.Other,
                                exception);

                        // If the exception was handled, don't exit the dialog
                        if (handled)
                        {
                            exitDialog = false;
                        }
                        // Stop processing and post dialog.
                        return false;
                    }
                }
                else
                {
                    this.PublishLicense = null;
                }
            }

            // If we made it this far then it is considered success
            exitDialog = false;
            return true;
        }

        /// <summary>
        /// Using the provided template filename, attempt to load the template from
        /// disk and setup a signed publish license from it.
        /// </summary>
        /// <param name="templateFilename">The template file to load.</param>
        /// <param name="exitDialog">Used to determine if the RMPublishing
        /// dialog should exit.</param>
        /// <returns>True on success, false on failure.</returns>
        private bool ProcessRMTemplate(Uri templateFilename, out bool exitDialog)
        {
            string template = string.Empty;

            // With the current template system we should never bail on the dialog
            // as we will inform the user and allow them to select again.
            exitDialog = false;

            try
            {
                // Load the template from file.
                template = GetTemplateFromFile(templateFilename);
            }
            // This generic catch allows all failures of loading the file to be
            // directed to our central error handler which will show the
            // appropriate error message to the user.  Since this operation is
            // classified as a critical operation, the error handler will re-throw
            // the exception to allow it to be handled higher on the stack if it
            // is fatal (which includes all exceptions not specifically handled
            // by the error handler).
#pragma warning suppress 56500 // suppress PreSharp Warning 56500: Avoid `swallowing errors by catching non-specific exceptions..
            catch (Exception exception)
            {
                // This exception will be thrown if there is a problem
                // reading the template from the file.
                // We need to handle the method and determine the
                // appropriate action.
                RightsManagementErrorHandler.HandleOrRethrowException(
                    RightsManagementOperation.TemplateAccess,
                    exception);

                // Since any exception means the file was not loaded, bail out.
                return false;
            }
            // Ensure that template was actually loaded.
            if (!string.IsNullOrEmpty(template))
            {
                try
                {
                    // Generate the license from the template.
                    _rmProvider.GenerateUnsignedPublishLicense(template);
                    _rmProvider.SignPublishLicense();
                }
                catch (RightsManagementException exception)
                {
                    // This exception will be thrown if there is a problem
                    // either parsing/loading the template, or signing it.
                    // In either case a message should be displayed informing
                    // the user about the invalid template.
                    RightsManagementErrorHandler.HandleOrRethrowException(
                        RightsManagementOperation.TemplateAccess,
                        exception);

                    // Since any exception means the file was not loaded, bail out.
                    return false;
                }
            }
            else
            {
                // The template could not be loaded correctly from disk
                // (could be that it was removed, permissions are incorrect,
                // network down, etc), so display an error message.
                RightsManagementErrorHandler.HandleOrRethrowException(
                    RightsManagementOperation.TemplateAccess,
                    new FileFormatException());

                // Since any exception means the file was not loaded, bail out.
                return false;
            }

            // Set exitDialog to false to continue with the signing process.
            exitDialog = false;
            return true;
        }

        /// <summary>
        /// Open a template file and attempt to load it into _template.
        /// </summary>
        /// <param name="templateFilename">The filename (with path) to load.</param>
        /// <returns>The text string read from the file if successful, otherwise
        /// string.Empty</returns>
        private string GetTemplateFromFile(Uri templateFilename)
        {
            // Set the default value, this value will remain if any of the file
            // loading fails.
            string template = string.Empty;

            // If a template exists on the publishing dialog then user has selected
            // a template rather than individual settings.
            if (templateFilename != null)
            {
                // Save the path local to this method to guarantee it does not
                // change between the assert and the file access
                string templateLocalPath = templateFilename.LocalPath;

                // The RM client requires that templates are in UTF16
                // (Encoding.Unicode) format.
                template = File.ReadAllText(templateLocalPath, Encoding.Unicode);
            }
            return template;
        }

        #endregion Private Methods

        #region Private Properties
        //------------------------------------------------------
        // Private Properties
        //------------------------------------------------------

        /// <summary>
        /// Setup as a clr property to transparently use CriticalDataForSet.
        /// </summary>
        private IRightsManagementProvider _rmProvider
        {
            get
            {
                return _rmProviderCache.Value;
            }
        }

        #endregion Private Properties

        #region Private Fields
        //------------------------------------------------------
        // Private Fields
        //------------------------------------------------------

        private static SecurityCriticalDataForSet<DocumentRightsManagementManager> _currentManager;
        private SecurityCriticalDataForSet<IRightsManagementProvider> _rmProviderCache;

        /// <summary>
        /// A handle to a currently open instance of the credential manager dialog
        /// </summary>
        private CredentialManagerDialog _credManagerDialog;

        /// <summary>
        /// True if we have yet determined whether the RM client is installed.
        /// We cache this to keep us from checking if the RM Client is installed
        /// over and over.
        /// </summary>
        private bool _determinedRMInstallState = false;

        /// <summary>
        /// True if the RM client is installed.
        /// </summary>
        private bool _isRMInstalled = false;

        /// <summary>
        /// False if the SecureEnvironment is not in a reliable state (ie need to reload).
        /// </summary>
        private bool _isSecureEnvironmentReliable = true;

        /// <summary>
        /// A dictionary mapping user names to RightsManagementUser objects.
        /// </summary>
        private IDictionary<string, RightsManagementUser> _userMap;

        #endregion Private Fields 

        #region Nested Class
        //------------------------------------------------------
        // Nested Class
        //------------------------------------------------------

        /// <summary>
        /// RMStatusEventArgs, object used when firing RMStatus change.
        /// </summary>
        public class RightsManagementStatusEventArgs : EventArgs
        {
            #region Constructors
            //------------------------------------------------------
            // Constructors
            //------------------------------------------------------

            /// <summary>
            /// The constructor
            /// </summary>
            /// <param name="rmStatus">Rights Management status</param>
            /// <param name="statusResources">Resources containing other information
            /// relevant to the document RM status</param>
            public RightsManagementStatusEventArgs(
                RightsManagementStatus rmStatus)
            {
                _rmStatus = rmStatus;
                _statusResourcesLoaded = false;
            }

            #endregion Constructors

            #region Public Properties
            //------------------------------------------------------
            // Public Properties
            //------------------------------------------------------

            /// <summary>
            /// The Rights Management status
            /// </summary>
            public RightsManagementStatus RMStatus
            {
                get { return _rmStatus; }
            }

            /// <summary>
            /// Property to get the StatusResources.  To improve performance the resources are not loaded
            /// until the first time they are accessed.
            /// </summary>
            public DocumentStatusResources StatusResources
            {
                get 
                { 
                    if (!_statusResourcesLoaded) 
                    {
                        _statusResources = RightsManagementResourceHelper.GetDocumentLevelResources(_rmStatus);
                        _statusResourcesLoaded = true;
                    }
                    return _statusResources;
                }
            }

            #endregion Public Properties

            #region Private Fields
            //------------------------------------------------------
            // Private Fields
            //------------------------------------------------------

            private RightsManagementStatus _rmStatus;
            private DocumentStatusResources _statusResources;
            private bool _statusResourcesLoaded;

            #endregion Private Fields

        }
        
        /// <summary>
        /// RMPolicyEventArgs, object used when firing RMPolicy change.
        /// </summary>
        public class RightsManagementPolicyEventArgs : EventArgs
        {
            #region Constructors
            //------------------------------------------------------
            // Constructors
            //------------------------------------------------------

            /// <summary>
            /// The constructor
            /// </summary>
            internal RightsManagementPolicyEventArgs(RightsManagementPolicy rmPolicy)
            {
                _rmPolicy = rmPolicy;
            }

            #endregion Constructors

            #region Public Properties
            //------------------------------------------------------
            // Public Properties
            //------------------------------------------------------

            /// <summary>
            /// Property to get/set the RMPolicy
            /// </summary>
            public RightsManagementPolicy RMPolicy
            {
                get { return _rmPolicy; }
            }

            #endregion Public Properties

            #region Private Fields
            //------------------------------------------------------
            // Private Fields
            //------------------------------------------------------

            private RightsManagementPolicy _rmPolicy;

            #endregion Private Fields 
        }

        /// <summary>
        /// The data passed to the Rights Management enrollment thread.
        /// </summary>
        private struct RightsManagementEnrollThreadInfo
        {
            /// <summary>
            /// The account type to enroll
            /// </summary>
            public EnrollmentAccountType AccountType;

            /// <summary>
            /// A handle to the form that displays enrollment progress
            /// </summary>
            public Form ProgressForm;
        }

        #endregion Nested Class
    }
}
