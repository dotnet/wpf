// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//    IRightsManagementProvider is interface that defines the DRP's RM API adapter.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.RightsManagement;
using System.IO;
using System.IO.Packaging;
using MS.Internal.Security.RightsManagement;
using System.Security;

namespace MS.Internal.Documents
{
    /// <summary>
    /// IRightsManagementProvider is the interface that defines the DRP's RM API adapter. 
    /// </summary>
    internal interface IRightsManagementProvider
    {

        /// <summary>
        /// Is the XPS document RM-protected?
        /// </summary>
        bool IsProtected
        {
            get;
        }

        /// <summary>
        /// Gets the rights granted in the current use license.
        /// </summary>
        RightsManagementLicense CurrentUseLicense
        {
            get;
        }

        /// <summary>
        /// Gets or sets the publish license associated with the current
        /// package. Setting the current publish license invalidates any saved
        /// use licenses.
        /// </summary>
        PublishLicense CurrentPublishLicense
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the currently active user.
        /// </summary>
        RightsManagementUser CurrentUser
        {
            get;
        }

        /// <summary>
        /// Enrolls a new user and sets up the secure environment.
        /// </summary>
        void InitializeEnvironment(EnrollmentAccountType accountType);

        /// <summary>
        /// Sets up the secure environment for a particular user.
        /// </summary>
        void InitializeEnvironment(RightsManagementUser user);

        /// <summary>
        /// Loads a use license for the user from the package.
        /// This requires InitializeEnvironment to have been called.
        /// </summary>
        /// <returns>Whether or not a use license could be loaded directly from the
        /// package</returns>
        bool LoadUseLicense();

        /// <summary>
        /// Acquires a use license for the package.
        /// This requires InitializeEnvironment to have been called.
        /// </summary>
        /// <returns>Whether or not a use license could be acquired</returns>
        bool AcquireUseLicense();

        /// <summary>
        /// Saves the current use license and embeds it in the package.
        /// This requires a use license to have been acquired.
        /// </summary>
        void SaveUseLicense(EncryptedPackageEnvelope package);

        /// <summary>
        /// Binds the use license to the secure environment.
        /// This requires the use license to be set by the LoadUseLicense or
        /// AcquireUseLicense function.
        /// </summary>
        void BindUseLicense();

        /// <summary>
        /// Gets a list of all credentials available to the current user.
        /// </summary>
        /// <returns>A list of all available credentials</returns>
        ReadOnlyCollection<RightsManagementUser> GetAvailableCredentials();

        /// <summary>
        /// Removes user from available credentials.
        /// </summary>
        void RemoveCredentials(RightsManagementUser user);

        /// <summary>
        /// Gets a the default credentials.
        /// </summary>
        /// <returns>Default credentials</returns>
        RightsManagementUser GetDefaultCredentials();

        /// <summary>
        /// Sets the default credentials.
        /// </summary>
        void SetDefaultCredentials(RightsManagementUser user);

        /// <summary>
        /// Retrieves access rights for all users embedded in the package.
        /// </summary>
        /// <returns>A dictionary with all the users </returns>
        IDictionary<RightsManagementUser, RightsManagementLicense> GetAllAccessRights();

        /// <summary>
        /// Decrypt the encrypted package into a metro stream.
        /// </summary>
        /// <returns>The decrypted version of the package.</returns>
        Stream DecryptPackage();

        /// <summary>
        /// Creates an encrypted package on the provided stream.
        /// </summary>
        /// <param name="ciphered">The stream to store the chiphered data.</param>
        /// <returns>A new EncryptedPackageEnvelope</returns>
        EncryptedPackageEnvelope EncryptPackage(Stream ciphered);

        /// <summary>
        /// Generates an unsigned publish license for the package from a collection
        /// of licenses.
        /// </summary>
        void GenerateUnsignedPublishLicense(IList<RightsManagementLicense> licenses);

        /// <summary>
        /// Generates an unsigned publish license for the package from a template
        /// definition (string of XrML).
        /// </summary>
        void GenerateUnsignedPublishLicense(string template);

        /// <summary>
        /// Signs the unsigned publish license and saves a corresponding updated use
        /// license.
        /// This requires GenerateUnsignedPublishLicense to have been called.
        /// </summary>
        void SignPublishLicense();

        /// <summary>
        /// Saves the current set of licenses.
        /// </summary>
        void SaveCurrentLicenses();

        /// <summary>
        /// Reverts to the last saved set of licenses.
        /// </summary>
        void RevertToSavedLicenses();

        /// <summary>
        /// Sets the encapsulated EncryptedPackageEnvelope to a new value and invalidates the
        /// saved publish and use licenses.
        /// </summary>
        /// <param name="newPackage">The new encrypted package</param>
        /// <param name="publishLicenseChanged">Whether or not the new encrypted
        /// package has a different publish license</param>
        void SetEncryptedPackage(EncryptedPackageEnvelope newPackage, out bool publishLicenseChanged);
    }
}
