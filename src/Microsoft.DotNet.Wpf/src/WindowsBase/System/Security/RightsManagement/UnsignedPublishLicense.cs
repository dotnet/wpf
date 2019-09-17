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
using MS.Internal;
using SecurityHelper=MS.Internal.WindowsBase.SecurityHelper; 

// Disable message about unknown message numbers so as to allow the suppression
// of PreSharp warnings (whose numbers are unknown to the compiler).
#pragma warning disable 1634, 1691

namespace System.Security.RightsManagement 
{
    /// <summary>
    /// UnsignedPublishLicense class is used to represent publish license information before it was signed. 
    /// It can be used to build and sign Publish License, and it also can be used to build and serialize Publish License Template.   
    /// </summary>
    public class UnsignedPublishLicense
    {
        /// <summary>
        /// This constructor builds an empty Publish License. 
        /// </summary>
        public UnsignedPublishLicense()
        {
        
            _grantCollection = new Collection<ContentGrant>();
            _contentId = Guid.NewGuid();
        }
        
        /// <summary>
        /// This constructor accepts XrML Publish License template as a parameter. It parses the XrRML document 
        /// and initializes class based on that.
        /// </summary>
        public UnsignedPublishLicense(string publishLicenseTemplate) :this ()
        {

            if (publishLicenseTemplate == null)
            {   
                throw new ArgumentNullException("publishLicenseTemplate");
            }
            
            using(IssuanceLicense issuanceLicense = new IssuanceLicense(
                                        DateTime.MinValue,  // validFrom, - default 
                                        DateTime.MaxValue,  // validUntil, - default 
                                        null,  // referralInfoName,
                                        null,  //  referralInfoUrl,
                                        null,  // owner,
                                        publishLicenseTemplate, 
                                        SafeRightsManagementHandle.InvalidHandle,     // boundLicenseHandle,
                                        _contentId,  //  contentId,
                                        null,    //  grantCollection
                                        null,   //  Localized Name Description pairs collection 
                                        null,   //  Application Specific Data Dictionary
                                        0,      // validity interval days 
                                        null))     // revocation point info 
            {
                // update our instance data based on the parsed information 
                issuanceLicense.UpdateUnsignedPublishLicense(this);
            }
        }

        /// <summary>
        /// This functions signs the Publish License offline, and as a result produces 2 objects. It makes an instance of the  PublishLicense 
        /// and it also builds an instance of the UseLicense, which represeents the authors UseLicense 
        /// </summary>
        public PublishLicense Sign(SecureEnvironment secureEnvironment, out UseLicense authorUseLicense)
        {

            if (secureEnvironment == null)
            {
                throw new ArgumentNullException("secureEnvironment");
            }

            // in case owner wasn't specified we can just assume default owner 
            // based on the user identity that was used to build the secure environment
            ContentUser contentOwner;

            if (_owner != null)
            {
                contentOwner = _owner; 
            }
            else
            {
                contentOwner = secureEnvironment.User;
            }

            using(IssuanceLicense issuanceLicense = new IssuanceLicense(
                                        DateTime.MinValue,  // validFrom, - default 
                                        DateTime.MaxValue,  // validUntil, - default 
                                        _referralInfoName,
                                        _referralInfoUri,
                                        contentOwner,
                                        null,
                                        SafeRightsManagementHandle.InvalidHandle,     // boundLicenseHandle,
                                        _contentId,
                                        Grants,
                                        LocalizedNameDescriptionDictionary,
                                        ApplicationSpecificDataDictionary,
                                        _rightValidityIntervalDays,
                                        _revocationPoint))
            {
                // The SecureEnvironment constructor makes sure ClientSession cannot be null.
                // Accordingly suppressing preSharp warning about having to validate ClientSession.
#pragma warning suppress 6506
                return secureEnvironment.ClientSession.SignIssuanceLicense(issuanceLicense, out authorUseLicense);
            }
        }

        /// <summary>
        /// This property represent the user that will be the owner of the Pubish lciense.  
        /// This owner is also associated to the Owner node in the issuance license XrML. 
        /// By default if Owner isn't specified it will be assigned to the identity of the user 
        /// signing the UnsignedPublishLicense
        /// </summary>
        public ContentUser Owner
        {
            get 
            { 
            
                return _owner; 
            }
            set 
            { 
            
                _owner = value; 
            }
        }

        /// <summary>
        /// This property in conbimation with ReferralInfoUri is commonly used to enable 
        /// consumers of the protected content to contact the author/publisher of the content.
        /// </summary>
        public string ReferralInfoName
        {
            get 
            { 
            
                return _referralInfoName; 
            }
            set 
            { 
                
                _referralInfoName = value; 
            }
        }

        /// <summary>
        /// This property in conbimation with ReferralInfoName is commonly used to enable 
        /// consumers of the protected content to contact the author/publisher of the content.
        /// </summary>
        public Uri ReferralInfoUri
        {
            get 
            { 
            
                return _referralInfoUri; 
            }
            set 
            { 
            
                _referralInfoUri = value; 
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
            set 
            { 

                // Guid is a value type, so it can never be null; therefore, there is no nreed to check this
                _contentId = value;
            }
        }        

        /// <summary>
        /// This collection is used to assign rights to users in an Unsigned Publish License.
        /// </summary>
        public ICollection<ContentGrant> Grants
        {
            get 
            { 
            
                return _grantCollection; 
            }
        }

        /// <summary>
        /// This collection is used to assign Name Description pairs of strings to the 
        /// unsigned publish License templates based on the Local Id as a Key of the dictionary.
        /// </summary>
        public IDictionary <int, LocalizedNameDescriptionPair> LocalizedNameDescriptionDictionary
        {
            get 
            { 

                if (_localizedNameDescriptionDictionary == null)
                {
                    _localizedNameDescriptionDictionary  = new Dictionary <int, LocalizedNameDescriptionPair>(10);
                }
                
                return _localizedNameDescriptionDictionary; 
            }
        }
    

        /// <summary>
        /// This method produces serialized Publish License XRML template.
        /// </summary>
        override public string ToString()
        {
        
            using(IssuanceLicense issuanceLicense = new IssuanceLicense(
                                        DateTime.MinValue, 
                                        DateTime.MaxValue, 
                                        _referralInfoName,
                                        _referralInfoUri,
                                        _owner,
                                        null,
                                        SafeRightsManagementHandle.InvalidHandle,     // boundLicenseHandle,
                                        _contentId,
                                        Grants,
                                        LocalizedNameDescriptionDictionary,
                                        ApplicationSpecificDataDictionary,
                                        _rightValidityIntervalDays,
                                        _revocationPoint))
            {
                return issuanceLicense.ToString();
            }
        }

        /// <summary>
        /// This constructor accepts Signed XrML Publish License as a parameter. 
        /// It decrypts and parses parses the XrRML document and initializes class based on that.
        /// </summary>
        internal UnsignedPublishLicense(SafeRightsManagementHandle boundLicenseHandle, string publishLicenseTemplate)
                                                                                                                                         :this ()
        {
            Invariant.Assert(!boundLicenseHandle.IsInvalid);
            Invariant.Assert(publishLicenseTemplate != null);
            
            using(IssuanceLicense issuanceLicense = new IssuanceLicense(
                                        DateTime.MinValue,  // validFrom, - default 
                                        DateTime.MaxValue,  // validUntil, - default 
                                        null,  // referralInfoName,
                                        null,  //  referralInfoUrl,
                                        null,  // owner,
                                        publishLicenseTemplate, 
                                        boundLicenseHandle,     // boundLicenseHandle,
                                        _contentId,  //  contentId,
                                        null,    //  grantCollection
                                        null,   //  Localized Name Description pairs collection 
                                        null,   //  Application Specific Data Dictionary                                        
                                        0,       // validity interval days 
                                        null))     // revocation point info 
            {
                // update our instance data based on the parsed information 
                issuanceLicense.UpdateUnsignedPublishLicense(this);
            }
        }

        /// <summary>
        ///  This property sets/gets the number of days for a time condition of an issuance license.
        /// Unmanged SDK treats 0 as a missing(not set)  value  
        /// </summary>
        internal int RightValidityIntervalDays
        {
            get 
            {
                return _rightValidityIntervalDays; 
            }
            set 
            {
                // Invariant.Assert(value>=0);
                _rightValidityIntervalDays = value;
            }
        }

        /// <summary>
        /// This collection is used to assign Name Description pairs of strings to the 
        /// unsigned publish License templates based on the Local Id as a Key of the dictionary.
        /// </summary>
        internal IDictionary <string, string> ApplicationSpecificDataDictionary
        {
            get 
            { 

                if (_applicationSpecificDataDictionary == null)
                {
                    _applicationSpecificDataDictionary = new Dictionary <string , string>(5);
                }
                
                return _applicationSpecificDataDictionary; 
            }
        }

        /// <summary>
        /// This property enables us to implemen a revocation list pass through for template based publishing 
        /// takes from DRM SDK:
        ///         Revocation list can revoke end-user licenses, server licensor certificates, or 
        ///         almost anything else with an identifying GUID. See Revocation for a list of the 
        ///         items that can be revoked. The URL provided should refer to the list file itself. 
        ///         The rights management system handles checking for a valid revocation list. 
        ///         This function should only be called once, since subsequent calls will overwrite 
        ///         the previous revocation point in the issuance license.
        ///         The public key must be a base-64 encoded string.
        ///         Note that if there is no revocation point set in the license, the license can 
        ///         still be revoked by a revocation list signed by the issuer of the license.
        /// </summary>
        internal RevocationPoint RevocationPoint
        {
            get
            {
                return _revocationPoint;
            }
            set
            {
                _revocationPoint  = value;
            }
        }
        
        private Guid _contentId;
        private ContentUser _owner;
        private ICollection<ContentGrant> _grantCollection;
        private string _referralInfoName;
        private Uri _referralInfoUri;
        private IDictionary <int, LocalizedNameDescriptionPair> _localizedNameDescriptionDictionary = null;
        private IDictionary <string, string> _applicationSpecificDataDictionary = null;
        private int _rightValidityIntervalDays; // default 0 value is treated by the RM SDK as a non-defined missing value
        private RevocationPoint _revocationPoint;
    }
}
