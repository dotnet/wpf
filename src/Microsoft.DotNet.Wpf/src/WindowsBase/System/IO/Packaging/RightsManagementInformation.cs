// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This class represents the rights-management information stored in an
//  EncryptedPackageEnvelope. Specifically, it represents the PublishLicense and the
//  UseLicenses stored in the compound file that embodies the RM protected Package.
//
//
//
//

using System.Collections.Generic;
using System.Security.RightsManagement;

using MS.Internal.IO.Packaging.CompoundFile;

namespace System.IO.Packaging
{
    /// <summary>
    /// This class represents the rights-management information stored in an
    /// EncryptedPackageEnvelope. Specifically, it represents the PublishLicense and the
    /// UseLicenses stored in the compound file that embodies the RM protected Package.
    /// </summary>
    public class RightsManagementInformation
    {
        /// <summary>
        /// Internal constructor, called by EncryptedPackageEnvelope.Create or EncryptedPackageEnvelope.Open.
        /// </summary>
        internal
        RightsManagementInformation(
            RightsManagementEncryptionTransform rmet
            )
        {
            _rmet = rmet;
        }

        /// <value>
        /// This property represents the object that determines what operations the current
        /// user is allowed to perform on the encrypted content.
        /// </value>
        public CryptoProvider CryptoProvider
        {
            get
            {
                return _rmet.CryptoProvider;
            }

            set
            {
                _rmet.CryptoProvider = value;
            }
        }

        /// <summary>
        /// Read the publish license from the RM transform's primary instance data stream.
        /// </summary>
        /// <returns>
        /// The publish license, or null if the compound file does not contain a publish
        /// license (as it will not, for example, when the compound file is first created).
        /// </returns>
        /// <exception cref="FileFormatException">
        /// If the stream is corrupt, or if the RM instance data in this file cannot be
        /// read by the current version of this class.
        /// </exception>
        public PublishLicense LoadPublishLicense()
        {
            return _rmet.LoadPublishLicense();
        }

        /// <summary>
        /// Save the publish license to the RM transform's instance data stream.
        /// </summary>
        /// <param name="publishLicense">
        /// The publish licence to be saved. The RM server returns a publish license as a string.
        /// </param>
        /// <remarks>
        /// The stream is rewritten from the beginning, so any existing publish license is
        /// overwritten.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="publishLicense"/> is null.
        /// </exception>
        /// <exception cref="FileFormatException">
        /// If the existing RM instance data in this file cannot be updated by the current version
        /// of this class.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the transform settings are fixed.
        /// </exception>
        public void SavePublishLicense(PublishLicense publishLicense)
        {
            _rmet.SavePublishLicense(publishLicense);
        }

        /// <summary>
        /// Load a use license for the specified user from the RM transform's instance data
        /// storage in the compound file.
        /// </summary>
        /// <param name="userKey">
        /// The user whose use license is desired.
        /// </param>
        /// <returns>
        /// The use license for the specified user, or null if the compound file does not
        /// contain a use license for the specified user.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="userKey"/> is null.
        /// </exception>
        /// <exception cref="FileFormatException">
        /// If the RM information in this file cannot be read by the current version of
        /// this class.
        /// </exception>
        public UseLicense LoadUseLicense(ContentUser userKey) 
        {
            return _rmet.LoadUseLicense(userKey);
        }

        /// <summary>
        /// Save a use license for the specified user into the RM transform's instance data
        /// storage in the compound file.
        /// </summary>
        /// <param name="userKey">
        /// The user to whom the use license was issued.
        /// </param>
        /// <param name="useLicense">
        /// The use license issued to that user.
        /// </param>
        /// <remarks>
        /// Any existing use license for the specified user is removed from the compound
        /// file before the new use license is saved.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="userKey"/> or <paramref name="useLicense"/> is null.
        /// </exception>
        /// <exception cref="FileFormatException">
        /// If the RM information in this file cannot be written by the current version of
        /// this class.
        /// </exception>
        public void SaveUseLicense(ContentUser userKey, UseLicense useLicense)
        {
            _rmet.SaveUseLicense(userKey, useLicense);
        }

        /// <summary>
        /// Delete the use license for the specified user from the RM transform's instance
        /// data storage in the compound file.
        /// </summary>
        /// <param name="userKey">
        /// The user whose use license is to be deleted.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="userKey"/> is null.
        /// </exception>
        /// <exception cref="FileFormatException">
        /// If the RM information in this file cannot be updated by the current version of
        /// this class.
        /// </exception>
        public void DeleteUseLicense(ContentUser userKey)
        {
            _rmet.DeleteUseLicense(userKey);
        }

        /// <summary>
        /// This method retrieves a reference to a dictionary with keys of type User and values
        /// of type UseLicense, containing one entry for each use license embedded in the compound
        /// file for this particular transform instance. The collection is a snapshot of the use
        /// licenses in the compound file at the time of the call. The term "Embedded" in the method
        /// name emphasizes that the dictionary returned by this method only includes those use
        /// licenses that are embedded in the compound file. It does not include any other use
        /// licenses that the application might have acquired from an RM server but not yet embedded
        /// into the  compound file. 
        /// </summary>
        public IDictionary<ContentUser, UseLicense> GetEmbeddedUseLicenses()
        {
            return _rmet.GetEmbeddedUseLicenses();
        }

        private RightsManagementEncryptionTransform _rmet;
    }
}
