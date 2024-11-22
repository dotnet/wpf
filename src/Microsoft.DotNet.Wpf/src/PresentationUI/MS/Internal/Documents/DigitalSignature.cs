// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:    
//   The DigitalSignature class represents a digital signature or signature
//   request.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.IO.Packaging;
using System.Security; // for SecurityCritical attributes
using System.Windows.Xps.Packaging; // for XpsDigitalSignature

namespace MS.Internal.Documents
{
    /// <summary>
    /// The DigitalSignature class represents a digital signature or signature
    /// request.
    /// </summary>
    internal sealed class DigitalSignature
    {
        #region Constructor

        internal DigitalSignature()
        {
        }

        #endregion Constructor

        #region Internal properties

        /// <summary>
        /// Gets or sets the status of signature (Valid, Invalid, NotSigned ...)
        /// </summary>
        internal SignatureStatus SignatureState
        {
            get
            {
                return _signatureState;
            }

            set
            {
                _signatureState = value;
            }
        }

        /// <summary>
        /// Gets or sets the friendly name of the signer (obtained from the cert.
        /// </summary>
        internal string SubjectName
        {
            get
            {
                return _subjectName;
            }

            set
            {
                _subjectName = value;
            }
        }

        /// <summary>
        /// Gets or sets the intent (from the Signature Definition)
        /// </summary>
        internal string Reason
        {
            get
            {
                return _reason;
            }

            set
            {
                _reason = value;
            }
        }

        /// <summary>
        /// Gets or sets when this signature was applied (not a trusted time)
        /// </summary>
        internal DateTime? SignedOn
        {
            get
            {
                return _signedOn;
            }
            set
            {
                _signedOn = value;
            }
        }

        /// <security>
        /// Gets or sets the location field (what signer type into signature definition)
        /// </security>
        internal string Location
        {
            get
            {
                return _location;
            }

            set
            {
                _location = value;
            }
        }

        /// <summary>
        /// Gets or sets the restriction on document properties
        /// </summary>
        internal bool IsDocumentPropertiesRestricted
        {
            get
            {
                return _isDocumentPropertiesRestricted;
            }

            set
            {
                _isDocumentPropertiesRestricted = value;
            }
        }

        /// <summary>
        /// Gets or sets whether adding signatures will invalidate this signature
        /// </summary>
        internal bool IsAddingSignaturesRestricted
        {
            get
            {
                return _isAddingSignaturesRestricted;
            }

            set
            {
                _isAddingSignaturesRestricted = value;
            }
        }

        /// <summary>
        /// Gets or sets the Signature ID
        /// </summary>
        internal Guid? GuidID
        {
            get
            {
                return _guidID;
            }

            set
            {
                _guidID = value;
            }
        }

        /// <summary>
        /// Gets or sets the X.509 Certificate
        /// </summary>
        internal X509Certificate2 Certificate
        {
            get
            {
                return _x509Certificate2;
            }

            set
            {
                _x509Certificate2 = value;
            }
        }

        /// <summary>
        /// Gets or sets the XpsDigitalSignature associated with this signature.
        /// This value is expected to be left null for signature requests.
        /// </summary>
        internal XpsDigitalSignature XpsDigitalSignature
        {
            get
            {
                return _xpsDigitalSignature;
            }

            set
            {
                _xpsDigitalSignature = value;
            }
        }

        #endregion Internal properties

        #region Private data

        /// <summary>
        /// Status of signature (Valid, Invalid, NotSigned ...)
        /// </summary>
        private SignatureStatus _signatureState;

        /// <summary>
        /// Friendly name of the signer (obtained from the certificate)
        /// </summary>
        private string _subjectName;

        /// <summary>
        /// Intent:  From the Signature Definition
        /// </summary>
        private string _reason;

        /// <summary>
        /// When this signature was applied (not a trusted time)
        /// </summary>
        private DateTime? _signedOn;

        /// <summary>
        /// Location field (what signer type into signature definition)
        /// </summary>
        private string _location;

        /// <summary>
        /// Whether or not document properties changes are restricted by this signature
        /// </summary>
        private bool _isDocumentPropertiesRestricted;

        /// <summary>
        /// Whether or not adding signatures will invalidate this signature
        /// </summary>
        private bool _isAddingSignaturesRestricted;

        /// <summary>
        /// SignatureID
        /// </summary>
        private Guid? _guidID;

        private X509Certificate2 _x509Certificate2;

        /// <summary>
        /// The XpsDigitalSignature associated with this signature
        /// </summary>
        private XpsDigitalSignature _xpsDigitalSignature;

        #endregion Private data
    }
}
