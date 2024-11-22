// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This class is a helper to load a set of associations between a user and the use license
//  granted to that user.
//
//
//
//
// 
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Security.RightsManagement;

using MS.Internal;

namespace MS.Internal.IO.Packaging.CompoundFile
{
    internal class UserUseLicenseDictionaryLoader
    {
        //------------------------------------------------------
        //
        // Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        internal UserUseLicenseDictionaryLoader(RightsManagementEncryptionTransform rmet)
        {
            _dict = new Dictionary<ContentUser, UseLicense>(ContentUser._contentUserComparer);

            //
            // This constructor is only called from RightsManagementEncryptionTransform
            // .GetEmbeddedUseLicenses. That method passes "this" as the parameter.
            // So it can't possibly be null.
            //
            Invariant.Assert(rmet != null);

            Load(rmet);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        // Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties 
        internal Dictionary<ContentUser, UseLicense> LoadedDictionary 
        {
            get
            {
                return _dict;
            }
        }

        #endregion Internal Properties 

        //------------------------------------------------------
        //
        // Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Load the contents of the dictionary from the compound file.
        /// </summary>
        /// <param name="rmet">
        /// The object that knows how to load use license data from the compound file.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="rmet"/> is null.
        /// </exception>
        private void Load(RightsManagementEncryptionTransform rmet )
        {
            rmet.EnumUseLicenseStreams(
                    new RightsManagementEncryptionTransform.UseLicenseStreamCallback(
                            this.AddUseLicenseFromStreamToDictionary
                            ),
                    null
                    );
        }

        /// <summary>
        /// Callback function used by Load. Called once for each use license stream
        /// in the compound file. Extracts the user and use license from the specified
        /// stream.
        /// </summary>
        /// <param name="rmet">
        /// The object that knows how to extract license information from the compound file.
        /// </param>
        /// <param name="si">
        /// The stream containing the user/user license pair to be added to the dictionary.
        /// </param>
        /// <param name="param">
        /// Caller-supplied parameter to EnumUseLicenseStreams. Not used.
        /// </param>
        /// <param name="stop">
        /// Set to true if the callback function wants to stop the enumeration. This callback
        /// function never wants to stop the enumeration, so this parameter is not used.
        /// </param>
        private void
        AddUseLicenseFromStreamToDictionary(
            RightsManagementEncryptionTransform rmet,
            StreamInfo si,
            object param,
            ref bool stop
            )
        {
            ContentUser user;
            using (Stream stream = si.GetStream(FileMode.Open, FileAccess.Read))
            {
                using(BinaryReader utf8Reader = new BinaryReader(stream, _utf8Encoding))
                {
                    UseLicense useLicense = rmet.LoadUseLicenseAndUserFromStream(utf8Reader, out user);

                    _dict.Add(user, useLicense);
                }
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        // Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        //
        // The object that provides the implementation of the IDictionary methods.
        //
        private Dictionary<ContentUser, UseLicense> _dict;

        //
        // Text encoding object used to read or write publish licenses and use licenses.
        //
        private UTF8Encoding _utf8Encoding = new UTF8Encoding();

        #endregion Private Fields
    }
}
