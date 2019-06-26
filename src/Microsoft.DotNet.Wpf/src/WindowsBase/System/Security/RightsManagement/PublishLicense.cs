// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This class implements the UnsignedPublishLicense class 
//   this class is the first step in the RightsManagement publishing process
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
using MS.Internal.Security.RightsManagement;
using SecurityHelper=MS.Internal.WindowsBase.SecurityHelper; 

// Disable message about unknown message numbers so as to allow the suppression
// of PreSharp warnings (whose numbers are unknown to the compiler).
#pragma warning disable 1634, 1691

namespace System.Security.RightsManagement 
{
    /// <summary>
    /// A Publish License is a list of rights, users, metadata, and other information that specifies how a specific user on 
    /// a specific computer is able to use the specified content. This Publish License must be signed by using the 
    /// UnisignedPublishLicense.Sign function. The resulting signed Publish License is given to a potential end user 
    /// who must then request a Use License by calling the PublishLicense.AcquireUseLicense function. It is only the 
    /// Use License that allows an application to exercise the rights that have been granted.
    /// </summary>
    public class PublishLicense
    {
        /// <summary>
        /// This constructor accepts a string representation of a Publish License, which is supposed to be proided by the 
        /// publisher of a document to tyhe consumer of a document. 
        /// </summary>
        public PublishLicense(string signedPublishLicense)
        {

            if (signedPublishLicense == null)
            {
                throw new ArgumentNullException("signedPublishLicense");
            }
            
            _serializedPublishLicense = signedPublishLicense;

            /////////////////
            // parse out the Use License acquisition Url 
            /////////////////
            _useLicenseAcquisitionUriFromPublishLicense =
                    ClientSession.GetUseLicenseAcquisitionUriFromPublishLicense(_serializedPublishLicense);

            if (_useLicenseAcquisitionUriFromPublishLicense == null)
            {
                throw new RightsManagementException(RightsManagementFailureCode.InvalidLicense);
            }


            /////////////////
            // parse out the Content Id GUID 
            /////////////////
            String contentIdStr = ClientSession.GetContentIdFromPublishLicense(_serializedPublishLicense);

            if (contentIdStr == null)
            {
                throw new RightsManagementException(RightsManagementFailureCode.InvalidLicense);                
            }
            else
            {
                _contentId = new Guid(contentIdStr);
            }

            /////////////////
            // parse out the Referral Info 
            /////////////////
            ClientSession.GetReferralInfoFromPublishLicense(
                            _serializedPublishLicense,
                            out _referralInfoName, 
                            out _referralInfoUri);
        }

        /// <summary>
        /// This function allows the Owner (or a person granted ViewRightsData right)
        /// to extract the original publishing information that was encrypted during publishing process.
        /// </summary>
        public UnsignedPublishLicense DecryptUnsignedPublishLicense(CryptoProvider cryptoProvider )
        {
            
            if (cryptoProvider == null)
            {
                throw new ArgumentNullException("cryptoProvider");
            }

            return cryptoProvider.DecryptPublishLicense(_serializedPublishLicense);
        }


        /// <summary>
        /// The referral Information provided by the author of the protected content to the consumer.
        /// This property usually exposes a contact information to ask for additional rights for the 
        /// the protected content.
        /// </summary>
        public string ReferralInfoName
        {
            get 
            { 
            
                return _referralInfoName; 
            }
        }

        /// <summary>
        /// The referral Information provided by the author of the protected content to the consumer.
        /// This property usually exposes a contact information to ask for additional rights for the 
        /// the protected content. Commonly mailto: URIs are used to expose a way to contact the author
        /// of the content. 
        /// </summary>
        public Uri ReferralInfoUri
        {
            get 
            { 
            
                return _referralInfoUri; 
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
        /// The Uri that will be used by the AcquireUseLicense call to get the UseLicense. 
        /// </summary>
        public Uri UseLicenseAcquisitionUrl 
        {
            get 
            {
            
                return _useLicenseAcquisitionUriFromPublishLicense;
            }
        }
        
        /// <summary>
        /// Returns the original XrML string that was used to deserialize the Pubish License 
        /// </summary>
        public override string ToString()
        {
            
            return _serializedPublishLicense;
        }        

        /// <summary>
        ///  This function attempts to acquire a Use License.
        /// </summary>
        public UseLicense AcquireUseLicense(SecureEnvironment secureEnvironment)
        {
            
            if (secureEnvironment == null)
            {
                throw new ArgumentNullException("secureEnvironment");
            }

            // The SecureEnvironment constructor makes sure ClientSession cannot be null.
            // Accordingly suppressing preSharp warning about having to validate ClientSession.
#pragma warning suppress 6506
            return secureEnvironment.ClientSession.AcquireUseLicense(_serializedPublishLicense, false);
        }

        /// <summary>
        ///  This function attempts to acquire a Use License. 
        ///  This function suppresses the Windows network authentication dialog box. If the license request is denied
        ///  because the user does not have permission. This function will prevent the network authentication dialog 
        ///  box from being displayed. This is useful when attempting to handle license acquisition on a background 
        ///  or other non-user interface thread because you can avoid potentially confusing dialog boxes. If authentication 
        ///  does fail, the function will throw an appropriate RightsManagementException
        /// </summary>
        public UseLicense AcquireUseLicenseNoUI(SecureEnvironment secureEnvironment)
        {
                    
            if (secureEnvironment == null)
            {
                throw new ArgumentNullException("secureEnvironment");
            }

            // The SecureEnvironment constructor makes sure ClientSession cannot be null.
            // Accordingly suppressing preSharp warning about having to validate ClientSession.
#pragma warning suppress 6506
            return secureEnvironment.ClientSession.AcquireUseLicense(_serializedPublishLicense, true);
        }        

        private string _serializedPublishLicense;
        private string _referralInfoName;
        private Uri _referralInfoUri;
        private Guid _contentId;
        private Uri _useLicenseAcquisitionUriFromPublishLicense = null;
    }
}
