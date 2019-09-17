// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: This class represents the Use Lciense which enables end users to
//              consume protected content.
//
//
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using SecurityHelper=MS.Internal.WindowsBase.SecurityHelper;

using MS.Internal.Security.RightsManagement;
using MS.Internal.Utility;

// Disable message about unknown message numbers so as to allow the suppression
// of PreSharp warnings (whose numbers are unknown to the compiler).
#pragma warning disable 1634, 1691

namespace System.Security.RightsManagement
{
    /// <summary>
    /// This class represents the Use Lciense which enables end users to consume protected content.
    /// </summary>
    public class UseLicense
    {
        /// <summary>
        /// This constructor accepts the serialized form of a use license, and builds an instance of the classs based on that.
        /// </summary>
        public UseLicense(string useLicense)
        {

            if (useLicense == null)
            {
                throw new ArgumentNullException("useLicense");
            }
            _serializedUseLicense = useLicense;


            /////////////////
            // parse out the Content Id GUID
            /////////////////
            string contentId;
            string contentIdType;
            ClientSession.GetContentIdFromLicense(_serializedUseLicense, out contentId, out contentIdType);

            if (contentId == null)
            {
                throw new RightsManagementException(RightsManagementFailureCode.InvalidLicense);
            }
            else
            {
                _contentId = new Guid(contentId);
            }

            /////////////////
            // Get Owner information from the license
            /////////////////
            _owner = ClientSession.ExtractUserFromCertificateChain(_serializedUseLicense);

            /////////////////
            // Get ApplicationSpecific Data Dictionary
            /////////////////
            _applicationSpecificDataDictionary = new ReadOnlyDictionary <string, string>
                    (ClientSession.ExtractApplicationSpecificDataFromLicense(_serializedUseLicense));
        }

        /// <summary>
        /// This constructor accepts the serialized form of a use license, and builds an instance of the classs based on that.
        /// </summary>
        public ContentUser Owner
        {
            get
            {

                return _owner;
            }
        }

        /// <summary>
        /// The ContentId is created by the publisher and can be used to match content to UseLicense and PublishLicenses.
        /// </summary>
        public Guid ContentId
        {
            get
            {

                return _contentId;
            }
        }

        /// <summary>
        /// Returns the original XrML string that was used to deserialize the Use License
        /// </summary>
        public override string ToString()
        {

            return _serializedUseLicense;
        }


        /// <summary>
        /// This function allows an application to examine or exercise the rights on a locally stored license.
        /// </summary>
        public CryptoProvider Bind (SecureEnvironment secureEnvironment)
        {

            if (secureEnvironment == null)
            {
                throw new ArgumentNullException("secureEnvironment");
            }

            // The SecureEnvironment constructor makes sure ClientSession cannot be null.
            // Accordingly suppressing preSharp warning about having to validate ClientSession.
#pragma warning suppress 6506
            return secureEnvironment.ClientSession.TryBindUseLicenseToAllIdentites(_serializedUseLicense);
        }

        /// <summary>
        /// ApplicationData data dictionary contains values that are passed from publishing
        /// application to a consuming application. One data pair that is processed by a Rights
        /// Management Services (RMS) server is the string pair "Allow_Server_Editing"/"True".
        /// When an issuance license has this value pair, it will allow the service, or any trusted
        /// service, to reuse the content key. The pair "NOLICCACHE" / "1" is expected to control
        /// Use License embedding policy of the consuming applications. If it is set to 1, applications
        /// are expected not to embed the Use License into the document.
        /// </summary>
        public IDictionary<string,string> ApplicationData
        {
            get
            {

                return _applicationSpecificDataDictionary;
            }
        }

        /// <summary>
        /// Test for equality.
        /// </summary>
        public override bool Equals(object x)
        {

            if (x == null)
                return false;   // Standard behavior.

            if (x.GetType() != GetType())
                return false;   // Not the same type.

            // Note that because of the GetType() checking above, the casting must be valid.
            UseLicense obj = (UseLicense)x;
            return (String.CompareOrdinal(_serializedUseLicense, obj._serializedUseLicense) == 0);
}

        /// <summary>
        /// Compute hash code.
        /// </summary>
        public override int GetHashCode()
        {

            return _serializedUseLicense.GetHashCode();
        }


        private string _serializedUseLicense;
        private Guid _contentId;
        private ContentUser _owner = null;
        private IDictionary <string, string> _applicationSpecificDataDictionary = null;
    }
}
