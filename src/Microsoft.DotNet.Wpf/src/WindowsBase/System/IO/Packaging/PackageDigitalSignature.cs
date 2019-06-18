// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This class represents a PackageDigitalSignature.  It is immutable. 
//
//
//
//
//

using System;
using System.Collections.Generic;
using System.Windows;           // For Exception strings - SRID
using System.Text;              // for StringBuilder
using System.Diagnostics;        // for Assert
using System.Security;          // for SecurityCritical
using System.Security.Cryptography.Xml;     // for Xml Signature classes
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using MS.Internal.IO.Packaging;            // helper classes Certificate, HashStream
using System.Collections.ObjectModel;       // for ReadOnlyCollection<>
using MS.Internal.WindowsBase;

namespace System.IO.Packaging
{
    /// <summary>
    /// VerifyResult
    /// </summary>
    public enum VerifyResult : int
    {
        /// <summary>
        /// Verification succeeded
        /// </summary>
        Success,               // signature valid
    
        /// <summary>
        /// Signature was invalid (tampering detected)
        /// </summary>
        InvalidSignature,      // hash incorrect
    
        /// <summary>
        /// Certificate was not embedded in container and caller did not supply one
        /// </summary>
        CertificateRequired,   // no certificate is embedded in container - caller must provide one
    
        /// <summary>
        /// Certificate was invalid (perhaps expired?)
        /// </summary>
        InvalidCertificate,    // certificate problem - verify does not fully verify cert
    
        /// <summary>
        /// PackagePart was missing - signature invalid
        /// </summary>
        ReferenceNotFound,     // signature failed because a part is missing
    
        /// <summary>
        /// Package not signed
        /// </summary>
        NotSigned               // no signatures were found
    }

    /// <summary>
    /// PackageDigitalSignature
    /// </summary>
    public class PackageDigitalSignature
    {
        #region Public Members
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        /// <summary>
        /// Parts that are covered by this signature
        /// </summary>
        /// <value>read only list</value>
        /// <exception cref="InvalidOperationException">Thrown if associated digital signature has been deleted.</exception>
        public ReadOnlyCollection<Uri> SignedParts
        {
            get 
            {
                ThrowIfInvalidated();

                // wrap in read-only collection to protect against alteration
                if (_signedParts == null)
                    _signedParts = new ReadOnlyCollection<Uri>(_processor.PartManifest);

                return _signedParts;
            }
        }

        /// <summary>
        /// Relationships that are covered by this signature
        /// </summary>
        /// <value>read only list</value>
        /// <exception cref="InvalidOperationException">Thrown if associated digital signature has been deleted.</exception>
        public ReadOnlyCollection<PackageRelationshipSelector> SignedRelationshipSelectors
        {
            get
            {
                ThrowIfInvalidated();

                // wrap in read-only collection to protect against alteration
                if (_signedRelationshipSelectors == null)
                    _signedRelationshipSelectors = new ReadOnlyCollection<PackageRelationshipSelector>(_processor.RelationshipManifest);

                return _signedRelationshipSelectors;
            }
        }

        /// <summary>
        /// The part that contains the actual signature - useful for counter-signing scenarios
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if associated digital signature has been deleted.</exception>
        public PackagePart SignaturePart
        {
            get
            {
                ThrowIfInvalidated();

                return _processor.SignaturePart;
            }
        }

        /// <summary>
        /// Certificate of signer embedded in container
        /// </summary>
        /// <value>null if certificate was not embedded</value>
        /// <exception cref="InvalidOperationException">Thrown if associated digital signature has been deleted.</exception>
        public X509Certificate Signer 
        {
            get
            {
                ThrowIfInvalidated();

                return _processor.Signer;
            }
        }
 
        /// <summary>
        /// Time signature was created - not a trusted TimeStamp
        /// </summary>
        /// <value></value>
        /// <exception cref="InvalidOperationException">Thrown if associated digital signature has been deleted.</exception>
        public DateTime SigningTime
        {
            get
            {
                ThrowIfInvalidated();

                return _processor.SigningTime;
            }
        }

        /// <summary>
        /// Format of time returned by SigningTime (see PackageDigitalSignatureManager.TimeFormat for details)
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if associated digital signature has been deleted.</exception>
        public String TimeFormat
        {
            get
            {
                ThrowIfInvalidated();

                return _processor.TimeFormat;
            }
        }

        /// <summary>
        /// encrypted hash value
        /// </summary>
        /// <value></value>
        /// <exception cref="InvalidOperationException">Thrown if associated digital signature has been deleted.</exception>
        public byte[] SignatureValue
        {
            get
            {
                ThrowIfInvalidated();

                return _processor.SignatureValue;
            }
        }

        /// <summary>
        /// Content Type of signature
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if associated digital signature has been deleted.</exception>
        public String SignatureType
        {
            get
            {
                ThrowIfInvalidated();

                return XmlDigitalSignatureProcessor.ContentType.ToString();
            }
        }

        /// <summary>
        /// Type-specific signature object
        /// </summary>
        /// <remarks>
        /// Provides access to the underlying class that performs the signature type-specific cryptographic
        /// functions and serialization to/from the package part that houses the signature.
        /// </remarks>
        /// <returns>
        /// Returns an object of type System.Security.Cryptography.Xml.Signature.
        /// Future signature types will return objects of different classes.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if associated digital signature has been deleted.</exception>
        public Signature Signature
        {
            get
            {
                ThrowIfInvalidated();
                return _processor.Signature;
            }
            set
            {
                ThrowIfInvalidated();
                if (value == null)
                    throw new ArgumentNullException("value");

                _processor.Signature = value;
            }
        }

        /// <summary>
        /// Where is the certificate?
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if associated digital signature has been deleted.</exception>
        public CertificateEmbeddingOption CertificateEmbeddingOption
        {
            get
            {
                ThrowIfInvalidated();

                if (GetCertificatePart() == null)
                {
                    if (Signer == null)
                    {
                        return CertificateEmbeddingOption.NotEmbedded;
                    }
                    else
                    {
                        return CertificateEmbeddingOption.InSignaturePart;
                    }
                }
                else
                {
                    return CertificateEmbeddingOption.InCertificatePart;
                }
            }
        }
        
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Returns ordered list of transforms applied to the given part
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if associated digital signature has been deleted.</exception>
        public List<String> GetPartTransformList(Uri partName)
        {
            ThrowIfInvalidated();

            // no need to clone this for return as it's already a single-use collection
            return _processor.GetPartTransformList(partName);
        }
        
        /// <summary>
        /// Verify
        /// </summary>
        /// <remarks>cannot use this overload with signatures created without embedding their certs</remarks>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if associated digital signature has been deleted.</exception>
        public VerifyResult Verify()
        {
            ThrowIfInvalidated();

            if (Signer == null)
                return VerifyResult.CertificateRequired;

            return Verify(Signer);
        }

        /// <summary>
        /// Verify
        /// </summary>
        /// <param name="signingCertificate">certificate used to create the signature</param>
        /// <returns></returns>
        /// <remarks>Use this overload when the certificate is not embedded in the container at signing time</remarks>
        /// <exception cref="InvalidOperationException">Thrown if associated digital signature has been deleted.</exception>
        public VerifyResult Verify(X509Certificate signingCertificate)
        {
            ThrowIfInvalidated();

            VerifyResult result = VerifyResult.NotSigned;

            if (signingCertificate == null)
                throw new ArgumentNullException("signingCertificate");

            // Check for part existence
            foreach (Uri partUri in SignedParts)
            {
                // check if they exist
                if (!_manager.Package.PartExists(partUri))
                {
                    return VerifyResult.ReferenceNotFound;
                }
            }

            // convert to Ex variant that has more functionality
            X509Certificate2 certificate = signingCertificate as X509Certificate2;
            if (certificate == null)
                certificate = new X509Certificate2(signingCertificate.Handle);

            // verify
            if (_processor.Verify(certificate))
                result = VerifyResult.Success;
            else
                result = VerifyResult.InvalidSignature;

            return result;
        }

        #endregion

        #region Internal Members
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Constructor for creating a new signature
        /// </summary>
        /// <param name="manager">digital signature manager - to consult for hash, embedding and other options</param>
        /// <param name="processor">digital signature manager - to consult for hash, embedding and other options</param>
        internal PackageDigitalSignature(
            PackageDigitalSignatureManager manager, 
            XmlDigitalSignatureProcessor processor)
        {
            Debug.Assert(processor.PackageSignature == null, "Logic Error: one processor per-signature");
            _manager = manager;
            _processor = processor;
//            _processor.PackageSignature = this;
        }

        /// <summary>
        /// Constructor for use when opening an existing signature
        /// </summary>
        /// <param name="manager">digital signature manager - to consult for hash, embedding and other options</param>
        /// <param name="signaturePart">part that houses the signature</param>
        internal PackageDigitalSignature(
            PackageDigitalSignatureManager manager,
            PackagePart signaturePart)
        {
            _manager = manager;
            _processor = new XmlDigitalSignatureProcessor(manager, signaturePart, this);
        }

        /// <summary>
        /// This is called when the underlying signature is deleted - it prevents usage of the object
        /// </summary>
        internal void Invalidate()
        {
            _invalid = true;
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        /// <summary>
        /// Get certificate part - null if none
        /// </summary>
        internal CertificatePart GetCertificatePart()
        {
            // lazy init
            if (_certificatePart == null && !_alreadyLookedForCertPart)
            {
                PackageRelationshipCollection relationships = SignaturePart.GetRelationshipsByType(
                    CertificatePart.RelationshipType);
                foreach (PackageRelationship relationship in relationships)
                {
                    // don't resolve if external
                    if (relationship.TargetMode != TargetMode.Internal)
                        throw new FileFormatException(SR.Get(SRID.PackageSignatureCorruption));

                    Uri resolvedUri = PackUriHelper.ResolvePartUri(SignaturePart.Uri, relationship.TargetUri);

                    // don't create if it doesn't exist
                    if (!_manager.Package.PartExists(resolvedUri))
                    {
                        continue;
                    }

                    // find the cert
                    _certificatePart = new CertificatePart(_manager.Package, resolvedUri);
                    break;
                }
                _alreadyLookedForCertPart = true;
            }
            
            return _certificatePart;
        }
        
        internal void SetCertificatePart(CertificatePart certificatePart)
        {
            Debug.Assert(certificatePart != null, "Logic Error: Not expecting setting certificate part to null on digital signature");
            _certificatePart = certificatePart;
        }

        #endregion

        #region Private Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// ThrowIfInvalidated - check on each access
        /// </summary>
        private void ThrowIfInvalidated()
        {
            if (_invalid)
                throw new InvalidOperationException(SR.Get(SRID.SignatureDeleted));
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        private PackageDigitalSignatureManager                     _manager;
        private XmlDigitalSignatureProcessor                       _processor;
        private CertificatePart                                    _certificatePart;
        private ReadOnlyCollection<Uri>                            _signedParts;
        private ReadOnlyCollection<PackageRelationshipSelector>    _signedRelationshipSelectors;
        private bool                                               _alreadyLookedForCertPart;
        private bool                                               _invalid;   // have we been invalidated?
    }
}

