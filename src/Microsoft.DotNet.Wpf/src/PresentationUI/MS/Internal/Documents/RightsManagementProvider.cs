// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Packaging;
using System.Security;
using System.Security.RightsManagement;
using System.Windows.TrustUI;

using MS.Internal.Documents.Application;
using MS.Internal.PresentationUI;

using Microsoft.Win32;

using SR=System.Windows.TrustUI.SR;
using SRID=System.Windows.TrustUI.SRID;

namespace MS.Internal.Documents
{
/// <summary>
/// RightsManagementProvider is used to connect DRP to RM APIs 
/// </summary>
/// <remarks>
/// This class is a facade for the RM APIs. It is the model between the Manager
/// and the EncryptedPackageEnvelope and System.Security.RightsManagement classes.
/// </remarks>
internal class RightsManagementProvider : IRightsManagementProvider, IDisposable
{
    #region Constructors
    //--------------------------------------------------------------------------
    //  Constructors
    //--------------------------------------------------------------------------

    /// <summary>
    /// A constructor that takes an encrypted package.  This
    /// can be null
    /// <<param name="package">The encrypted package</param>
    /// </summary>
    [MS.Internal.PresentationUI.FriendAccessAllowed]
    public RightsManagementProvider(EncryptedPackageEnvelope encryptedPackage)
    {
        _encryptedPackageEnvelope = encryptedPackage;
    }

    #endregion Constructors

    #region IRightsManagementProvider
    //--------------------------------------------------------------------------
    // IRightsManagementProvider
    //--------------------------------------------------------------------------

    /// <summary>
    /// Is the XPS document RM-protected?
    /// </summary>
    bool IRightsManagementProvider.IsProtected
    {
        get
        {
            InitializeMembers();

            return (CurrentPublishLicense != null);
        }
    }

    /// <summary>
    /// Gets the rights granted in the current use license.
    /// </summary>
    RightsManagementLicense IRightsManagementProvider.CurrentUseLicense
    {
        get { return _rmUseLicense.Value; }
    }

    /// <summary>
    /// Gets or sets the publish license associated with the current package.
    /// Setting the current publish license invalidates any saved use licenses.
    /// </summary>
    PublishLicense IRightsManagementProvider.CurrentPublishLicense
    {
        get
        {
            return _currentPublishLicense;
        }

        set
        {
            _currentPublishLicense = value;

            // Invalidate the saved use license and grants
            _useLicense.Value = null;
            _rmUseLicense.Value = null;
        }
    }

    /// <summary>
    /// Gets the currently active user.
    /// </summary>
    RightsManagementUser IRightsManagementProvider.CurrentUser
    {
        get { return _user.Value; }
    }

    /// <summary>
    /// Enrolls a new user and sets up the secure environment.
    /// </summary>
    /// <param name="accountType">The account type to use to initialize the
    /// secure environment</param>
    void IRightsManagementProvider.InitializeEnvironment(EnrollmentAccountType accountType)
    {        
        InitializeMembers();

        CleanUpSecureEnvironment();

        try
        {
            AuthenticationType authType;
            UserActivationMode userActMode;

            switch (accountType)
            {
                case (EnrollmentAccountType.Network):
                    {
                        authType = AuthenticationType.Windows;
                        userActMode = UserActivationMode.Permanent;
                        break;
                    }
                case (EnrollmentAccountType.Temporary):
                    {
                        authType = AuthenticationType.Windows;
                        userActMode = UserActivationMode.Temporary;
                        break;
                    }
                case (EnrollmentAccountType.NET):
                    {
                        authType = AuthenticationType.Passport;
                        userActMode = UserActivationMode.Permanent;
                        break;
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }

            _secureEnvironment.Value = SecureEnvironment.Create(
                                                                GetApplicationManifest(),
                                                                authType,
                                                                userActMode);                
        }
        // If the secure environment initialization fails, or the AccountType is
        // set incorrectly, we need to clean out the SecureEnvironment.
        catch
        {
            CleanUpSecureEnvironment();

            throw;
        }

        Trace.SafeWrite(
            Trace.Rights,
            "SecureEnvironment was initialized as part of enrollment.");

        SetUserFromSecureEnvironment();        
    }
    /// <summary>
    /// Sets up the secure environment for a particular user.
    /// </summary>
    /// <param name="user">The user for whom to set up the environment.</param>
    void IRightsManagementProvider.InitializeEnvironment(RightsManagementUser user)
    {
        InitializeMembers();
        CleanUpSecureEnvironment();

        _secureEnvironment.Value = SecureEnvironment.Create(
                                                            GetApplicationManifest(),
                                                            user);

        Trace.SafeWriteIf(
            (_secureEnvironment.Value != null),
            Trace.Rights,
            "SecureEnvironment was initialized for a specific user.");

        SetUserFromSecureEnvironment();        
    }

    /// <summary>
    /// Loads a use license for the user from the package.
    /// This requires InitializeEnvironment to have been called.
    /// </summary>
    /// <returns>Whether or not a use license could be loaded directly from the
    /// package</returns>
    bool IRightsManagementProvider.LoadUseLicense()
    {
        if (!IsProtected)
        {
            throw new InvalidOperationException(
                SR.Get(SRID.RMProviderExceptionNoPackageToDecrypt));
        }

        UseLicense useLicense;

        useLicense = _encryptedPackageEnvelope
                .RightsManagementInformation.LoadUseLicense(_user.Value);

        if (useLicense != null)
        {
            Trace.SafeWrite(Trace.Rights, "Existing use license was found.");
            _useLicense.Value = useLicense;
        }

        return (useLicense != null);
    }

    /// <summary>
    /// Acquires a use license for the package.
    /// This requires InitializeEnvironment to have been called.
    /// </summary>
    /// <returns>Whether or not a use license could be acquired</returns>
    bool IRightsManagementProvider.AcquireUseLicense()
    {
        if (!IsProtected)
        {
            throw new InvalidOperationException(
                SR.Get(SRID.RMProviderExceptionNoPackageToDecrypt));
        }

        UseLicense useLicense = null;
        RightsManagementException rmException = null;
    
        try
        {
            useLicense = CurrentPublishLicense.AcquireUseLicense(_secureEnvironment.Value);
        }
        catch(RightsManagementException e)
        {
            rmException = e;
        }

        if (useLicense != null)
        {
            Trace.SafeWrite(Trace.Rights, "A new use license was acquired.");

            _useLicense.Value = useLicense;
        }
        else
        {
            Trace.SafeWrite(
                Trace.Rights,
                "New use license acquisition failed: {0}",
                rmException.Message);
        }

        return (useLicense != null);
    }

    /// <summary>
    /// Saves the current use license and embeds it in the package.
    /// This requires a use license to have been acquired.
    /// </summary>
    void IRightsManagementProvider.SaveUseLicense(EncryptedPackageEnvelope package)
    {                
        if (!IsProtected)
        {
            throw new InvalidOperationException(
                SR.Get(SRID.RMProviderExceptionNoPackageToDecrypt));
        }

        if (_useLicense.Value == null)
        {
            throw new InvalidOperationException(
                SR.Get(SRID.RMProviderExceptionNoUseLicense));
        }

        if (AllowLicenseCaching)
        {
            Trace.SafeWrite(Trace.Rights, "Writing Use License to package.");

            // Attempt to write the acquired license back to the package
            if (package.FileOpenAccess != FileAccess.Read)
            {
                package.RightsManagementInformation.
                    SaveUseLicense(_user.Value, _useLicense.Value);
            }
        }
        else
        {            
            Trace.SafeWrite(Trace.Rights, "NOLICCACHE is set, not writing Use License to package.");
        }        
    }

    /// <summary>
    /// Binds the use license to the secure environment. This sets the
    /// CurrentUseLicense property to the appropriate value.
    /// This requires the use license to be set beforehand.
    /// </summary>
    void IRightsManagementProvider.BindUseLicense()
    {
        if (!IsProtected)
        {
            throw new InvalidOperationException(
                SR.Get(SRID.RMProviderExceptionNoPackageToDecrypt));
        }

        if (_useLicense.Value == null)
        {
            throw new InvalidOperationException(
                SR.Get(SRID.RMProviderExceptionNoUseLicense));
        }

        // If a CryptoProvider is already set in the EncryptedPackageEnvelope,
        // we can't set it again. As a result we should use the existing one if
        // possible.

        CryptoProvider cryptoProvider = null;
        ReadOnlyCollection<ContentGrant> grants = null;

        cryptoProvider =
            _encryptedPackageEnvelope.RightsManagementInformation.CryptoProvider;

        if (cryptoProvider != null)
        {
            grants = cryptoProvider.BoundGrants;
        }

        // If there is no CryptoProvider set in the envelope, we need to create
        // one and associate it with the envelope now

        if (cryptoProvider == null)
        {
            cryptoProvider = GenerateCryptoProvider();

            grants = cryptoProvider.BoundGrants;

            _encryptedPackageEnvelope.RightsManagementInformation.CryptoProvider =
                cryptoProvider;
        }

        // Create our RM license object using the grants retrieved from the
        // CryptoProvider

        Invariant.Assert(
            grants != null,
            "CryptoProvider had no bound grants.");

        _rmUseLicense.Value = ConvertGrantList(_user.Value, grants);

        // If possible use the CryptoProvider to decrypt the publish license

        if (HasPermission(
            _rmUseLicense.Value, RightsManagementPermissions.AllowOwner))
        {
            _unsignedPublishLicense.Value =
                CurrentPublishLicense.DecryptUnsignedPublishLicense(
                    cryptoProvider);

            Trace.SafeWrite(
                Trace.Rights, "Publish license was decrypted.");
        }
    }

    /// <summary>
    /// Gets a list of all credentials available to the current user.
    /// </summary>
    /// <returns>A list of all available credentials</returns>
    ReadOnlyCollection<RightsManagementUser> IRightsManagementProvider.GetAvailableCredentials()
    {
        ReadOnlyCollection<ContentUser> users;

        users = SecureEnvironment.GetActivatedUsers();

        List<RightsManagementUser> rmUsers =
            new List<RightsManagementUser>(users.Count);

        foreach (ContentUser user in users)
        {
            rmUsers.Add(RightsManagementUser.CreateUser(user));
        }

        return new ReadOnlyCollection<RightsManagementUser>(rmUsers);
    }

    /// <summary>
    /// Gets the default credentials.
    /// </summary>
    /// <returns>Default credentials</returns>
    RightsManagementUser IRightsManagementProvider.GetDefaultCredentials()
    {
        RightsManagementUser defaultUser = null;

        string defaultUserName = null;
        int defaultAccountType = -1;  //invalid account

        ReadOnlyCollection<RightsManagementUser> users =
            ((IRightsManagementProvider)this).GetAvailableCredentials();

        using (RegistryKey defaultRMKey = Registry.CurrentUser.OpenSubKey(
            _registryLocationForDefaultUser))
        {
            if (defaultRMKey != null)
            {
                // Get account name and validate it as a string.
                object keyValue = defaultRMKey.GetValue(_registryValueNameForAccountName);
                if ((keyValue != null) && 
                    (defaultRMKey.GetValueKind(_registryValueNameForAccountName) == RegistryValueKind.String))
                {
                    defaultUserName = (string)keyValue;
                }
                // Get account type and validate it as an int.
                keyValue = defaultRMKey.GetValue(_registryValueNameForAccountType);
                if ((keyValue != null) && 
                    (defaultRMKey.GetValueKind(_registryValueNameForAccountType) == RegistryValueKind.DWord))
                {
                    defaultAccountType = (int)keyValue;
                }
            }
        }

        //lets match up the user/type string with an actual avalable user
        if ((users.Count > 0) && (defaultUserName != null))
        {
            RightsManagementUser user = RightsManagementUser.CreateUser(
                defaultUserName,
                (AuthenticationType)defaultAccountType);

            int index = users.IndexOf(user);

            //Did we find the new default user in our Available users?
            if (index != -1)
            {
                defaultUser = users[index];
            }
        }

        if ((defaultUser == null) && (users.Count > 0))
        {
            //If we get here then default doesn't match any available users.
            //Select first user as default.
            defaultUser = users[0];
        }

        return defaultUser;
    }

    /// <summary>
    /// Sets the default credentials.
    /// </summary>
    void IRightsManagementProvider.SetDefaultCredentials(RightsManagementUser user)
    {
        if (user == null)
        {
            throw new ArgumentNullException("user");
        }

        //Get AvailableCreds list so we can match new default user.
        ReadOnlyCollection<RightsManagementUser> users =
            ((IRightsManagementProvider)this).GetAvailableCredentials();

        int index = users.IndexOf(user);

        //Did we find the new default user in our Available users?
        //If not, doesn't save to registry.
        if (index != -1)
        {
            string defaultUserName = users[index].Name;
            int defaultAccountType = (int)users[index].AuthenticationType;

            using (RegistryKey defaultRMKey = Registry.CurrentUser.CreateSubKey(
                _registryLocationForDefaultUser))
            {
                if (defaultRMKey != null)
                {
                    defaultRMKey.SetValue(
                        _registryValueNameForAccountName, defaultUserName);
                    defaultRMKey.SetValue(
                        _registryValueNameForAccountType, defaultAccountType);
                }
            }
        }
    }

    /// <summary>
    /// Removes user from available credentials.
    /// </summary>
    void IRightsManagementProvider.RemoveCredentials(RightsManagementUser user)
    {
        if (user == null)
        {
            throw new ArgumentNullException("user");
        }

        SecureEnvironment.RemoveActivatedUser(user);
    }

    /// <summary>
    /// Gets a dictionary of all users and grants stored in the package.
    /// </summary>
    /// <returns>All the users and licenses in the package, keyed by User.
    /// </returns>
    IDictionary<RightsManagementUser, RightsManagementLicense>
    IRightsManagementProvider.GetAllAccessRights()
    {
        if (IsProtected &&
            _rightsDictionary.Value == null &&
            _rmUseLicense.Value != null &&
            _rmUseLicense.Value.HasPermission(RightsManagementPermissions.AllowOwner) &&
            _unsignedPublishLicense.Value != null)
        {
            UnsignedPublishLicense unsignedLicense = _unsignedPublishLicense.Value;

            IDictionary<RightsManagementUser, IList<ContentGrant>> grantDictionary =
                new Dictionary<RightsManagementUser, IList<ContentGrant>>();

            // Add all the grants from the unsigned license to the dictionary
            foreach (ContentGrant grant in GetGrantsFromUnsignedLicense(unsignedLicense))
            {
                ContentUser contentUser = null;

                contentUser = grant.User;

                RightsManagementUser user =
                    RightsManagementUser.CreateUser(contentUser);

                if (!grantDictionary.ContainsKey(user))
                {
                    grantDictionary[user] = new List<ContentGrant>();
                }

                grantDictionary[user].Add(grant);
            }

            ContentUser contentUserOwner = null;

            contentUserOwner = unsignedLicense.Owner;

            RightsManagementUser owner =
                RightsManagementUser.CreateUser(contentUserOwner);

            // Add a grant for the owner of the document to the dictionary
            if (!grantDictionary.ContainsKey(owner))
            {
                grantDictionary[owner] = new List<ContentGrant>(1);
            }

            ContentGrant ownerGrant =
                CreateGrant(
                    owner,
                    ContentRight.Owner,
                    DateTime.MinValue,
                    DateTime.MaxValue);

            grantDictionary[owner].Add(ownerGrant);

            // Convert the grant dictionary to a dictionary of licenses
            IDictionary<RightsManagementUser, RightsManagementLicense> dictionary =
                new Dictionary<RightsManagementUser, RightsManagementLicense>(
                    grantDictionary.Count);

            foreach (RightsManagementUser user in grantDictionary.Keys)
            {
                RightsManagementLicense currentLicense =
                    ConvertGrantList(user, grantDictionary[user]);

                dictionary[user] = currentLicense;
            }

            _rightsDictionary.Value =
                (IDictionary<RightsManagementUser, RightsManagementLicense>)dictionary;
        }

        return _rightsDictionary.Value;
    }

    /// <summary>
    /// Decrypt the encrypted package into a metro stream.
    /// </summary>
    Stream IRightsManagementProvider.DecryptPackage()
    {
        if (_encryptedPackageEnvelope == null)
        {
            throw new InvalidOperationException(
                SR.Get(SRID.RMProviderExceptionNoPackageToDecrypt));
        }

        Stream result;

        Trace.SafeWrite(Trace.Rights, "Decrypting the document.");

        result = _encryptedPackageEnvelope.GetPackageStream();

        return result;
    }

    /// <summary>
    /// Create an encrypted package from a stream using the current publish license.
    /// </summary>
    /// <param name="ciphered">The stream to encrypt</param>
    /// <returns>An EncryptedPackageEnvelope around the stream.</returns>
    EncryptedPackageEnvelope IRightsManagementProvider.EncryptPackage(Stream ciphered)
    {
        // If the publish license is to not encrypt, return null
        if (CurrentPublishLicense == null)
        {
            return null;
        }

        // If the user doesn't have rights to encrypt data, we also should not
        // attempt to create an EncryptedPackageEnvelope, since we could never
        // write to it

        RightsManagementLicense currentUseLicense =
            ((IRightsManagementProvider)this).CurrentUseLicense;
        if (currentUseLicense != null &&
            !HasPermission(currentUseLicense, RightsManagementPermissions.AllowEdit))
        {
            return null;
        }

        CryptoProvider cp = GenerateCryptoProvider();
        EncryptedPackageEnvelope result = null;

        result = EncryptedPackageEnvelope.Create(
            ciphered,
            CurrentPublishLicense,
            cp);

        // Always save the use license back into any new RM protected document
        if (result != null)
        {
            ((IRightsManagementProvider)this).SaveUseLicense(result);
        }
        
        return result;
    }

    /// <summary>
    /// Generates an unsigned publish license for the package from a collection
    /// of licenses.
    /// </summary>
    /// <param name="licenses">
    /// The list of licenses from which to generate a publish license</param>
    /// <param name="validUntil">
    /// The optional date until when the publish license will be valid</param>
    /// <param name="referralUri">
    /// A URI to contact to request additional permissions</param>
    void IRightsManagementProvider.GenerateUnsignedPublishLicense(
        IList<RightsManagementLicense> licenses)
    {
        if (licenses == null)
        {
            throw new ArgumentNullException("licenses");
        }

        // If the document is already protected, only owners can republish it
        // with different permissions
        if (IsProtected && !HasPermission(
            _rmUseLicense.Value, RightsManagementPermissions.AllowOwner))
        {
            throw new InvalidOperationException(
                SR.Get(SRID.RMProviderExceptionNotOwnerOfDocument));
        }

        Trace.SafeWrite(
            Trace.Rights, "Creating a publish license for the document.");

        //
        // Create the unsigned publish license
        //

        UnsignedPublishLicense unsignedPublishLicense = null;

        unsignedPublishLicense = new UnsignedPublishLicense();

        ICollection<ContentGrant> licenseGrants =
            GetGrantsFromUnsignedLicense(unsignedPublishLicense);

        //
        // Retrieve the referral information from the first license
        //

        string referralInfoName = string.Empty;
        Uri referralInfoUri = null;

        if (licenses.Count > 0)
        {
            referralInfoName = licenses[0].ReferralInfoName;
            referralInfoUri = licenses[0].ReferralInfoUri;
        }

        //
        // Generate grants from the licenses passed in
        //

        Dictionary<RightsManagementUser, RightsManagementLicense> rights =
            new Dictionary<RightsManagementUser, RightsManagementLicense>();

        // Look through each license passed in and generate grants from it
        foreach (RightsManagementLicense rmLicense in licenses)
        {
            // If the user has not already been granted rights, add the grants
            // given to the user to the grants dictionary and the grant list
            if (!rights.ContainsKey(rmLicense.LicensedUser))
            {
                rights[rmLicense.LicensedUser] = rmLicense;

                IList<ContentGrant> grantList = GetGrantsFromLicense(rmLicense);

                foreach (ContentGrant grant in grantList)
                {
                    licenseGrants.Add(grant);
                }
            }
        }

        // If the current user was not specified as an owner, add the user to
        // the rights dictionary and grant the user admin privileges forever
        if (!rights.ContainsKey(_user.Value))
        {
            ContentGrant ownerGrant =
                CreateGrant(
                    _user.Value,
                    ContentRight.Owner,
                    DateTime.MinValue,
                    DateTime.MaxValue);

            // Add the grant to the unsigned publish license
            licenseGrants.Add(ownerGrant);

            // Add the grant to the rights dictionary
            IList<ContentGrant> grantList = new List<ContentGrant>();
            grantList.Add(ownerGrant);
            rights[_user.Value] = ConvertGrantList(_user.Value, grantList);
        }           

        //
        // Set up remaining properties of the unsigned publish license
        //

        unsignedPublishLicense.Owner = _user.Value;

        unsignedPublishLicense.ReferralInfoName = referralInfoName;
        unsignedPublishLicense.ReferralInfoUri = referralInfoUri;

        //
        // Save temporary unsigned license and rights dictionary for signing
        //

        _temporaryRightsDictionary.Value = rights;
        _temporaryUnsignedPublishLicense.Value = unsignedPublishLicense;
    }

    /// <summary>
    /// Generates an unsigned publish license for the package from 
    /// the template.
    /// </summary>
    /// <param name="template">
    /// The template from which to generate a publish license</param>
    void IRightsManagementProvider.GenerateUnsignedPublishLicense(string template)
    {
        if (string.IsNullOrEmpty(template))
        {
            throw new NullReferenceException("template");
        }

        UnsignedPublishLicense unsignedPublishLicense = null;
        // Create the license from the template.
        unsignedPublishLicense = new UnsignedPublishLicense(template);

        // Get who is currently listed as the owner of the document in the
        // publish license
        ContentUser currentOwnerFromLicense = null;
        currentOwnerFromLicense = unsignedPublishLicense.Owner;

        RightsManagementUser currentOwner = null;

        if (currentOwnerFromLicense != null)
        {
            currentOwner = RightsManagementUser.CreateUser(
                currentOwnerFromLicense);
        }

        // If the listed owner is not the current user, change the listed owner
        // and ensure that the old owner still maintains owner rights on the
        // document
        if (!_user.Value.Equals(currentOwner))
        {
            ContentGrant currentOwnerGrant = null;

            if (currentOwner != null)
            {
                currentOwnerGrant = CreateGrant(
                    currentOwner,
                    ContentRight.Owner,
                    DateTime.MinValue,
                    DateTime.MaxValue);
            }

            unsignedPublishLicense.Owner = _user.Value;

            if (currentOwnerGrant != null)
            {
                unsignedPublishLicense.Grants.Add(currentOwnerGrant);
            }
        }

        // Assign the new publish license.
        _temporaryRightsDictionary.Value = null;
        _temporaryUnsignedPublishLicense.Value = unsignedPublishLicense;
    }

    /// <summary>
    /// Signs the unsigned publish license and saves a corresponding updated use
    /// license.
    /// This requires GenerateUnsignedPublishLicense to have been called.
    /// </summary>
    void IRightsManagementProvider.SignPublishLicense()
    {
        // If the document is already protected, only owners can republish it
        if (IsProtected &&
            !HasPermission(_rmUseLicense.Value,
                           RightsManagementPermissions.AllowOwner))
        {
            throw new InvalidOperationException(
                SR.Get(SRID.RMProviderExceptionNotOwnerOfDocument));
        }

        UseLicense useLicense;

        Trace.SafeWrite(
            Trace.Rights, "Signing the publish license for the document.");

        CurrentPublishLicense =
            _temporaryUnsignedPublishLicense.Value.Sign(
                _secureEnvironment.Value, out useLicense);

        _useLicense.Value = useLicense;

        // Copy and clear temporary values
        _unsignedPublishLicense.Value = _temporaryUnsignedPublishLicense.Value;
        _rightsDictionary.Value = _temporaryRightsDictionary.Value;
        _temporaryUnsignedPublishLicense.Value = null;
        _temporaryRightsDictionary.Value = null;

        // If the RightsDictionary exists then set use license.
        if (_rightsDictionary.Value != null)
        {
            _rmUseLicense.Value = _rightsDictionary.Value[_user.Value];
        }
        else
        {
            // Since the RightsDictionary doesn't exist (most likely because we're using
            // a template), generate the owner data.
            List<ContentGrant> grantList = new List<ContentGrant>();
            grantList.Add(CreateGrant(_user.Value, ContentRight.Owner, DateTime.MinValue, DateTime.MaxValue));
            _rmUseLicense.Value = ConvertGrantList(_user.Value, grantList);
        }
    }

    /// <summary>
    /// Saves the current set of licenses.
    /// </summary>
    void IRightsManagementProvider.SaveCurrentLicenses()
    {
        // Save the current publish license and use licenses for rollback
        _lastSavedPublishLicense = _currentPublishLicense;
        _lastSavedRMUseLicense.Value = _rmUseLicense.Value;
        _lastSavedUseLicense.Value = _useLicense.Value;
        _lastSavedRightsDictionary.Value = _rightsDictionary.Value;
    }

    /// <summary>
    /// Reverts to the last saved set of licenses.
    /// </summary>
    void IRightsManagementProvider.RevertToSavedLicenses()
    {
        CurrentPublishLicense = _lastSavedPublishLicense;
        _useLicense.Value = _lastSavedUseLicense.Value;
        _rmUseLicense.Value = _lastSavedRMUseLicense.Value;
        _rightsDictionary.Value = _lastSavedRightsDictionary.Value;

        _lastSavedPublishLicense = null;
        _lastSavedUseLicense.Value = null;
        _lastSavedRMUseLicense.Value = null;
        _lastSavedRightsDictionary.Value = null;
    }

    /// <summary>
    /// Sets the encapsulated EncryptedPackageEnvelope to a new value and loads
    /// a new publish license from it.  If the new publish license is the same
    /// as the old one, the function restores the old use license.
    /// </summary>
    /// <param name="newPackage">The new encrypted package</param>
    /// <param name="publishLicenseChanged">Whether or not the new encrypted
    /// package has a different publish license</param>
    void IRightsManagementProvider.SetEncryptedPackage(EncryptedPackageEnvelope newPackage, out bool publishLicenseChanged)
    {
        PublishLicense savedPublishLicense = null;
        UseLicense savedUseLicense = null;
        RightsManagementLicense savedRMLicense = null;

        // Save the current set of licenses. In case the publish license in the
        // new encrypted package envelope is the same, these will be restored.
        if (_publishLicenseFromEnvelope != null)
        {
            savedPublishLicense = _publishLicenseFromEnvelope;
            savedUseLicense = _useLicense.Value;
            savedRMLicense = _rmUseLicense.Value;
        }

        _encryptedPackageEnvelope = newPackage;
        _publishLicenseFromEnvelope = null;
        CurrentPublishLicense = null;

        InitializeMembers();

        publishLicenseChanged = true;

        // If both publish licenses are non-null, compare them
        if (savedPublishLicense != null &&
            _publishLicenseFromEnvelope != null)
        {
            string serializedSavedPublishLicense = string.Empty;
            string serializedNewPublishLicense = string.Empty;

            serializedSavedPublishLicense = savedPublishLicense.ToString();
            serializedNewPublishLicense = _publishLicenseFromEnvelope.ToString();

            publishLicenseChanged = !string.Equals(
                serializedSavedPublishLicense,
                serializedNewPublishLicense,
                StringComparison.Ordinal);
        }
        // If both publish licenses are null, that means the document wasn't
        // protected before and still isn't protected, so the publish license
        // hasn't changed.
        else if (savedPublishLicense == null &&
                 _publishLicenseFromEnvelope == null)
        {
            publishLicenseChanged = false;
        }

        if (IsProtected && !publishLicenseChanged)
        {
            // If the publish license hasn't changed, restore the saved use
            // license and generate a new CryptoProvider from it.

            _useLicense.Value = savedUseLicense;
            _rmUseLicense.Value = savedRMLicense;

            CryptoProvider cryptoProvider = GenerateCryptoProvider();

            _encryptedPackageEnvelope.RightsManagementInformation.CryptoProvider =
                cryptoProvider;
        }

        // Since the encrypted package envelope has been changed, the last saved
        // licenses aren't applicable any more.
        _lastSavedPublishLicense = null;
        _lastSavedUseLicense.Value = null;
        _lastSavedRMUseLicense.Value = null;
        _lastSavedRightsDictionary.Value = null;

        Trace.SafeWrite(
            Trace.Rights,
            "SetEncryptedPackage called. publishLicenseChanged: {0}",
            publishLicenseChanged);
    }

    #endregion IRightsManagementProvider

    #region IDisposable Members
    //--------------------------------------------------------------------------
    // IDisposable Members
    //--------------------------------------------------------------------------

    /// <summary>
    /// Disposes everything that needs to be disposed.
    /// </summary>
    public void Dispose()
    {
        if (_cryptoProviders != null)
        {
            foreach (CryptoProvider cryptoProvider in _cryptoProviders)
            {
                cryptoProvider.Dispose();
            }

            _cryptoProviders = null;
        }

        CleanUpSecureEnvironment();

        GC.SuppressFinalize(this);
    }

    #endregion IDisposable Members

    #region Private Methods
    //--------------------------------------------------------------------------
    // Private Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// Destroys the RightsManagementProvider.
    /// </summary>
    ~RightsManagementProvider()
    {
        Dispose();
    }

    /// <summary>
    /// Binds the current use license to the current secure environment to get
    /// a new CryptoProvider.
    /// </summary>
    private CryptoProvider GenerateCryptoProvider()
    {
        CryptoProvider cryptoProvider = null;

        cryptoProvider = _useLicense.Value.Bind(_secureEnvironment.Value);

        Trace.SafeWrite(
            Trace.Rights, "The CryptoProvider was initialized.");

        // Add a reference to the CryptoProvider to the list of CryptoProviders
        // for disposal
        if (cryptoProvider != null)
        {
            if (_cryptoProviders == null)
            {
                IList<CryptoProvider> cryptoProviders = new List<CryptoProvider>();
                _cryptoProviders = cryptoProviders;
            }

            _cryptoProviders.Add(cryptoProvider);
        }

        return cryptoProvider;
    }

    /// <summary>
    /// Checks whether the specified license has the specified permission.
    /// </summary>
    /// <param name="license">The license to check</param>
    /// <param name="permission">The permission for which to check</param>
    /// <returns>Whether or not the license has the permission</returns>
    private static bool HasPermission(
        RightsManagementLicense license, RightsManagementPermissions permission)
    {
        return license.HasPermission(permission);
    }

    /// <summary>
    /// Adds referral information read from the current publish license to a
    /// use license.
    /// </summary>
    /// <param name="rmLicense">The use license (grant list) to which to add
    /// referral information</param>
    private void AddReferralInfo(RightsManagementLicense rmLicense)
    {
        // If there is no publish license yet there is no information to add
        if (CurrentPublishLicense == null)
        {
            return;
        }

        rmLicense.ReferralInfoName = CurrentPublishLicense.ReferralInfoName;
        rmLicense.ReferralInfoUri = CurrentPublishLicense.ReferralInfoUri;
    }

    /// <summary>
    /// Converts a collection of grants into a "license" that is usable
    /// internally by this component.
    /// </summary>
    /// <param name="useLicense">The UseLicense to convert</param>
    private RightsManagementLicense ConvertGrantList(
        RightsManagementUser user,
        IList<ContentGrant> grantList)
    {
        RightsManagementLicense rmLicense = new RightsManagementLicense();

        rmLicense.LicensedUser = user;
        rmLicense.LicensePermissions = RightsManagementPermissions.AllowNothing;
        rmLicense.ValidFrom = DateTime.MinValue;
        rmLicense.ValidUntil = DateTime.MaxValue;        

        AddReferralInfo(rmLicense);

        if (grantList != null)
        {
            bool canSign = false;

            DateTime validFrom = DateTime.MinValue;
            DateTime validUntil = DateTime.MaxValue;

            foreach (ContentGrant grant in grantList)
            {
                ContentRight right;

                right = grant.Right;

                if (grant.ValidFrom > validFrom)
                {
                    validFrom = grant.ValidFrom;
                }

                if (grant.ValidUntil < validUntil)
                {
                    validUntil = grant.ValidUntil;
                }

                switch (right)
                {
                    // Translate each grant into a Mongoose permission

                    case ContentRight.View:
                        rmLicense.AddPermission(
                            RightsManagementPermissions.AllowView);
                        break;

                    case ContentRight.Print:
                        rmLicense.AddPermission(
                            RightsManagementPermissions.AllowPrint);
                        break;

                    case ContentRight.Extract:
                        rmLicense.AddPermission(
                            RightsManagementPermissions.AllowCopy);
                        break;

                    case ContentRight.Owner:
                        rmLicense.AddPermission(
                            RightsManagementPermissions.AllowOwner);
                        break;

                    // The Edit grant can mean two things:
                    //
                    //  1) Without any other grants, it means basically nothing
                    //     since Mongoose doesn't support editing documents.
                    //  2) In conjunction with the Sign grant, it means that
                    //     the user can sign the document.
                    //
                    // As a result we have to keep track of the edit and sign
                    // grants separately and determine later whether both were
                    // granted before we can allow the user to sign.

                    case ContentRight.Edit:
                        rmLicense.AddPermission(
                           RightsManagementPermissions.AllowEdit);
                        break;

                    case ContentRight.Sign:
                        canSign = true;
                        break;

                    case ContentRight.DocumentEdit:
                        // DocumentEdit is a custom right, that when applied with 
                        // Edit, we want to treat as our custom right, Sign.
                        canSign = true;
                        break;
                }
            }

            if (rmLicense.HasPermission(RightsManagementPermissions.AllowEdit) &&
                canSign)
            {
                rmLicense.AddPermission(
                    RightsManagementPermissions.AllowSign);
            }

            rmLicense.ValidFrom = validFrom;
            rmLicense.ValidUntil = validUntil;
        }

        return rmLicense;
    }

    /// <summary>
    /// Generates a list of Grant objects corresponding to the data contained
    /// in a RightsManagementLicense object.
    /// </summary>
    /// <param name="rmLicense">The RightsManagementLicense to transform</param>
    /// <returns>A list of Grant objects</returns>
    private IList<ContentGrant> GetGrantsFromLicense(
        RightsManagementLicense rmLicense)
    {
        RightsManagementUser user = rmLicense.LicensedUser;

        List<ContentGrant> grants = new List<ContentGrant>();

        // Translate each Mongoose permission into a grant

        if (HasPermission(rmLicense, RightsManagementPermissions.AllowView))
        {
            grants.Add(
                CreateGrant(
                    user,
                    ContentRight.View,
                    rmLicense.ValidFrom,
                    rmLicense.ValidUntil));
        }

        if (HasPermission(rmLicense, RightsManagementPermissions.AllowPrint))
        {
            grants.Add(
                CreateGrant(
                    user,
                    ContentRight.Print,
                    rmLicense.ValidFrom,
                    rmLicense.ValidUntil));
        }

        if (HasPermission(rmLicense, RightsManagementPermissions.AllowCopy))
        {
            grants.Add(
                CreateGrant(
                    user,
                    ContentRight.Extract,
                    rmLicense.ValidFrom,
                    rmLicense.ValidUntil));
        }

        bool editRightGranted = false;

        if (HasPermission(rmLicense, RightsManagementPermissions.AllowEdit))
        {
            grants.Add(
                CreateGrant(
                    user,
                    ContentRight.Edit,
                    rmLicense.ValidFrom,
                    rmLicense.ValidUntil));

            editRightGranted = true;
        }

        if (HasPermission(rmLicense, RightsManagementPermissions.AllowSign))
        {
            // The sign permission translates to the combination of the Edit
            // and Sign grants.  If the user was already granted Edit, there is
            // no need to grant it again.

            if (!editRightGranted)
            {
                grants.Add(
                    CreateGrant(
                        user,
                        ContentRight.Edit,
                        rmLicense.ValidFrom,
                        rmLicense.ValidUntil));
            }

            grants.Add(
                CreateGrant(
                    user,
                    ContentRight.Sign,
                    rmLicense.ValidFrom,
                    rmLicense.ValidUntil));
        }

        if (HasPermission(rmLicense, RightsManagementPermissions.AllowOwner))
        {
            grants.Add(
                CreateGrant(
                    user,
                    ContentRight.Owner,
                    rmLicense.ValidFrom,
                    rmLicense.ValidUntil));
        }

        return grants;
    }

    /// <summary>
    /// Initializes the necessary member variables.  This can be called more
    /// than once without ill effect.
    /// </summary>
    private void InitializeMembers()
    {
        if (_encryptedPackageEnvelope != null &&
            _publishLicenseFromEnvelope == null)
        {
            RightsManagementInformation rmInfo =
                _encryptedPackageEnvelope.RightsManagementInformation;
            PublishLicense publishLicense = null;

            publishLicense = rmInfo.LoadPublishLicense();

            if (publishLicense == null)
            {
                throw new FileFormatException(
                    SR.Get(SRID.RMProviderExceptionNoPublishLicense));
            }

            _publishLicenseFromEnvelope = publishLicense;
            CurrentPublishLicense = publishLicense;
        }
    }

    /// <summary>
    /// Cleans up the SecureEnvironment member.
    /// </summary>
    private void CleanUpSecureEnvironment()
    {
        if (_secureEnvironment.Value != null)
        {
             _secureEnvironment.Value.Dispose();
            _secureEnvironment.Value = null;
        }
    }

    /// <summary>
    /// Sets the currently active user from the value stored in the saved
    /// secure environment.
    /// </summary>
    /// Critical
    ///  1) Asserts for RightsManagementPermission to get the value of the
    ///     _secureEnvironment.Value.User parameter
    ///  2) Sets SecurityCriticalDataForSet variable _user
    ///  3) Calls SecurityCritical function RightsManagementUser.CreateUser
    /// TreatAsSafe
    ///  1) _secureEnvironment is SecurityCriticalDataForSet, and the call is
    ///     reading a property value which requires asserts to access any data
    ///     from it.
    ///  2) The _user variable is set from SecurityCritical function
    ///     RightsManagementUser.CreateUser.
    ///  3) The argument to the CreateUser function is information that is
    ///     retrieved from the SecureEnvironment created by SecurityCritical
    ///     method Create.
    private void SetUserFromSecureEnvironment()
    {
        _user.Value =
            RightsManagementUser.CreateUser(_secureEnvironment.Value.User);
    }

    /// <summary>
    /// Creates a ContentGrant object with the given user and rights.
    /// </summary>
    /// <param name="user">The user for whom the grant applies</param>
    /// <param name="right">The right to grant</param>
    /// <returns>A new ContentGrant object</returns>
    private ContentGrant CreateGrant(RightsManagementUser user, ContentRight right, DateTime validFrom, DateTime validUntil)
    {
        return new ContentGrant(user, right, validFrom, validUntil);
    }

    /// <summary>
    /// Gets a collection of grants from an unsigned publish license.
    /// </summary>
    /// <param name="unsignedLicense">An unsigned publish license</param>
    /// <returns>A collection of grants</returns>
    private ICollection<ContentGrant> GetGrantsFromUnsignedLicense(
        UnsignedPublishLicense unsignedLicense)
    {
        ICollection<ContentGrant> grants = null;

        if (unsignedLicense != null)
        {
            grants = unsignedLicense.Grants;
        }

        return grants;
    }

    /// <summary>
    /// Cleans up the SecureEnvironment member.
    /// </summary>
    private string GetApplicationManifest()
    {
        //Get the current Process and MainModule.
        System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        //Content of the RM application Manifest
        System.Diagnostics.ProcessModule processModule = currentProcess.MainModule;

        Invariant.Assert(
            processModule != null,
            "Failed to get Process Module");
        
        string fileName = processModule.FileName;
        string applicationManifest = null;

        //Using exe module path, create path to application manifest file (XPSViewerManifest.xml)
        string applicationManifestFileLocation = Path.Combine(  Path.GetDirectoryName(fileName),
                                                                _applicationManifestFileName);

        // Create an instance of StreamReader to read from a file.
        // The using statement also closes the StreamReader.
        using (StreamReader sr = new StreamReader(applicationManifestFileLocation))
        {
            applicationManifest = sr.ReadToEnd();
        }

        return applicationManifest;
    }

    #endregion Private Methods

    #region Private Properties
    //--------------------------------------------------------------------------
    // Private Properties
    //--------------------------------------------------------------------------

    /// <summary>
    /// Gets whether the XPS document is RM protected. This is a convenient way
    /// to use the IsProtected property without having to cast to an instance
    /// of IRightsManagementProvider each time.
    /// </summary>
    private bool IsProtected
    {
        get
        {
            return ((IRightsManagementProvider)this).IsProtected;
        }
    }

    /// <summary>
    /// Gets or sets the current publish license that will be used for saving
    /// an encrypted document.  This is a convenient way to use the
    /// CurrentPublishLicense property without having to cast to an instance of
    /// IRightsManagementProvider each time.
    /// </summary>
    private PublishLicense CurrentPublishLicense
    {
        get
        {
            return ((IRightsManagementProvider)this).CurrentPublishLicense;
        }

        set
        {
            ((IRightsManagementProvider)this).CurrentPublishLicense = value;
        }
    }

    /// <summary>
    /// Indicates whether the current Use License allows caching of the license
    /// back to the container.    
    /// </summary>
    private bool AllowLicenseCaching
    {       
        get
        {
            bool result = false;

            // If the key pair "NOLICCACHE","1" (in _noLicCacheKeyValuePair) is present
            // in the Use License's ApplicationData, this signifies that license 
            // caching should be disabled (and thus we will return false here)
            result =
                !(_useLicense.Value != null &&
                _useLicense.Value.ApplicationData != null &&
                _useLicense.Value.ApplicationData.Contains(_noLicCacheKeyValuePair));
            
            return result;            
        }
    }

    #endregion Private Properties

    #region Private Fields
    //--------------------------------------------------------------------------
    // Private Fields
    //--------------------------------------------------------------------------

    /// <summary>
    /// The underlying EncryptedPackageEnvelope class from the RM APIs.
    /// </summary>
    EncryptedPackageEnvelope _encryptedPackageEnvelope;
    
    /// <summary>
    /// The currently active secure environment.
    /// </summary>
    SecurityCriticalDataForSet<SecureEnvironment> _secureEnvironment;

    /// <summary>
    /// The use license the user has for the currently open package.
    /// </summary>
    SecurityCriticalDataForSet<UseLicense> _useLicense;

    /// <summary>
    /// The last saved use license.
    /// </summary>
    SecurityCriticalDataForSet<UseLicense> _lastSavedUseLicense;

    /// <summary>
    /// A copy of the unsigned publish license.
    /// </summary>
    SecurityCriticalDataForSet<UnsignedPublishLicense> _unsignedPublishLicense;

    /// <summary>
    /// A generated unsigned publish license that has not yet been signed. Once
    /// it is signed, it will replace the _unsignedPublishLicense above.
    /// </summary>
    SecurityCriticalDataForSet<UnsignedPublishLicense> _temporaryUnsignedPublishLicense;

    /// <summary>
    /// The publish license saved in the current _encryptedPackage.
    /// </summary>
    PublishLicense _publishLicenseFromEnvelope;
    
    /// <summary>
    /// The current publish license, which may be different from
    /// _publishLicenseFromEnvelope above if the user has committed a publishing
    /// operation.
    /// </summary>
    PublishLicense _currentPublishLicense;

    /// <summary>
    /// The last saved publish license.
    /// </summary>
    PublishLicense _lastSavedPublishLicense;

    /// <summary>
    /// The specially formatted version of the use license describing what
    /// rights the current user has on the document.
    /// </summary>
    SecurityCriticalDataForSet<RightsManagementLicense> _rmUseLicense;

    /// <summary>
    /// The last saved RM use license.
    /// </summary>
    SecurityCriticalDataForSet<RightsManagementLicense> _lastSavedRMUseLicense;

    /// <summary>
    /// The user for whom this document has been opened.
    /// </summary>
    SecurityCriticalDataForSet<RightsManagementUser> _user;

    /// <summary>
    /// A dictionary of rights granted to users on this document.
    /// </summary>
    SecurityCriticalDataForSet<
        IDictionary<RightsManagementUser, RightsManagementLicense>> _rightsDictionary;

    /// <summary>
    /// The last saved version of the dictionary of rights granted to users.
    /// </summary>
    SecurityCriticalDataForSet<
        IDictionary<RightsManagementUser, RightsManagementLicense>> _lastSavedRightsDictionary;

    /// <summary>
    /// A dictionary of rights corresponding to the rights granted in a
    /// temporary unsigned publish license.
    /// </summary>
    SecurityCriticalDataForSet<
        IDictionary<RightsManagementUser, RightsManagementLicense>> _temporaryRightsDictionary;

    /// <summary>
    /// A list of all the CryptoProviders generated
    /// </summary>
    IList<CryptoProvider> _cryptoProviders;

    //Name of the RM application manifest.
    private const string _applicationManifestFileName = "XPSViewerManifest.xml";

    private const string _registryLocationForDefaultUser =
        @"Software\Microsoft\XPSViewer\";

    private const string _registryBaseForXpsViewer =
        @"HKEY_CURRENT_USER\Software\Microsoft\XPSViewer\";

    private const string _registryValueNameForAccountName = "AccountName";

    private const string _registryValueNameForAccountType = "AccountType";
    
    /// <summary>
    /// The Key/Value pair for the NOLICCACHE publishing option.  This disables
    /// caching of the Use License in the container if present.
    /// </summary>
    private readonly KeyValuePair<string, string> _noLicCacheKeyValuePair = 
        new KeyValuePair<string, string>("NOLICCACHE", "1");

    #endregion Private Fields
}
}
