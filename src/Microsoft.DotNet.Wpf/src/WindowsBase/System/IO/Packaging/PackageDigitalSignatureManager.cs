// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This class provides api's to add/remove/verify signatures on an MMCF container. 
//
//
//
//

// Allow use of presharp warning numbers [6506] unknown to the compiler
#pragma warning disable 1634, 1691

using System;
using System.Collections.Generic;
using System.Windows;                                   // For Exception strings - SRID
using System.Text;                                      // for StringBuilder
using System.Diagnostics;                               // for Assert
using System.Security;                                  // for SecurityCritical tag
using System.Security.Cryptography.Xml;                 // for SignedXml
using System.Security.Cryptography.X509Certificates;    // for X509Certificate
using MS.Internal.IO.Packaging;                         // for internal helpers
using System.Collections.ObjectModel;                   // for ReadOnlyCollection<>
using MS.Internal;                                      // for ContentType
using MS.Internal.WindowsBase;

using Package = System.IO.Packaging.Package;
using MS.Internal.IO.Packaging.Extensions;

namespace System.IO.Packaging
{
    /// <summary>
    /// Options for storing the signing Certificate
    /// </summary>
    public enum CertificateEmbeddingOption : int
    {
        /// <summary>
        /// Embed certificate in its own PackagePart (or share if same cert already exists)
        /// </summary>
        InCertificatePart = 0,      // embed the certificate in its own, possibly-shared part
        /// <summary>
        /// Embed certificate within the signature PackagePart
        /// </summary>
        InSignaturePart = 1,        // embed the certificate within the signature
        /// <summary>
        /// Do not embed
        /// </summary>
        NotEmbedded = 2,            // do not embed the certificate at all
    }

    /// <summary>
    /// Type of the handler that is invoked if signature validation is non-success.
    /// </summary>
    /// <param name="sender">signature</param>
    /// <param name="e">event arguments - containing the result</param>
    /// <returns>true to continue verifying other signatures, false to abandon effort</returns>
    public delegate void InvalidSignatureEventHandler(object sender, SignatureVerificationEventArgs e);

    /// <summary>
    /// Signature Verification Event Args - information about a verification event
    /// </summary>
    public class SignatureVerificationEventArgs : EventArgs
    {
        //------------------------------------------------------
        //
        //  Public Members
        //
        //------------------------------------------------------
        /// <summary>
        /// Signature being processed
        /// </summary>
        public PackageDigitalSignature Signature 
        {
            get
            {
                return _signature;
            }
        }
        
        /// <summary>
        /// Result of Verification
        /// </summary>
        public VerifyResult VerifyResult
        {
            get
            {
                return _result;
            }
        }
        
        //------------------------------------------------------
        //
        //  Internal Members
        //
        //------------------------------------------------------
        internal SignatureVerificationEventArgs(PackageDigitalSignature signature,
            VerifyResult result)
        {
            // verify arguments
            if (signature == null)
                throw new ArgumentNullException("signature");

            if (result < VerifyResult.Success || result > VerifyResult.NotSigned)
                throw new System.ArgumentOutOfRangeException("result");

            _signature = signature;
            _result = result;
        }

        //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------
        private PackageDigitalSignature                 _signature;
        private VerifyResult                            _result;
    }
    
    /// <summary>
    /// PackageDigitalSignatureManager
    /// </summary>
    public sealed class PackageDigitalSignatureManager
    {
        #region Public Members
        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        /// <summary>
        /// Event to subscribe to for signature validation activities
        /// </summary>
        public event InvalidSignatureEventHandler InvalidSignatureEvent;

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        /// <summary>
        /// Does this container hold digital signatures?
        /// </summary>
        /// <value>true if signatures exist</value>
        /// <remarks>this does not evaluate the signatures - they may be invalid even if this returns true</remarks>
        public bool IsSigned
        {
            get
            {
                EnsureSignatures();
                return (_signatures.Count > 0);
            }
        }

        /// <summary>
        /// Signatures in container
        /// </summary>
        /// <value>read only list of immutable signatures found in the container</value>
        public ReadOnlyCollection<PackageDigitalSignature> Signatures
        {
            get
            {
                // ensure signatures are loaded from origin
                EnsureSignatures();

                // Return a read-only collection referring to them.
                // This list will be automatically updated when the underlying collection is changed.
                if (_signatureList == null)
                    _signatureList = new ReadOnlyCollection<PackageDigitalSignature>(_signatures);

                return _signatureList;
            }
        }

        /// <summary>
        /// ContentType - Transform mapping dictionary
        /// </summary>
        /// <remarks>Dictionary of transform Uri's indexed by ContentType.  
        /// Contains a single transform to be applied 
        /// before hashing any Part encountered with that ContentType</remarks>
        public Dictionary<String, String> TransformMapping 
        {
            get
            {
                return _transformDictionary;
            }
        }

        /// <summary>
        /// Handle of parent window to use when displaying certificate selection dialog
        /// </summary>
        /// <value></value>
        /// <remarks>not necessary if certificates are provided in calls to sign</remarks>
        public IntPtr ParentWindow
        {
            get
            {
                return _parentWindow;
            }
            set
            {
                _parentWindow = value;
            }
        }

        /// <summary>
        /// Hashalgorithm to use when creating/verifying signatures
        /// </summary>
        /// <value></value>
        /// <remarks>defaults to SHA1</remarks>
        public String HashAlgorithm
        {
            get
            {
                return _hashAlgorithmString;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (value.Length == 0)
                    throw new ArgumentException(SR.Get(SRID.UnsupportedHashAlgorithm), "value");

                _hashAlgorithmString = value;
            }
        }

        /// <summary>
        /// How to embed certificates when Signing
        /// </summary>
        /// <value></value>
        public CertificateEmbeddingOption CertificateOption
        {
            get
            {
                return _certificateEmbeddingOption;
            }
            set
            {
                if ((value < CertificateEmbeddingOption.InCertificatePart) || (value > CertificateEmbeddingOption.NotEmbedded))
                    throw new ArgumentOutOfRangeException("value");

                _certificateEmbeddingOption = value;
            }
        }

        /// <summary>
        /// How to format the SignatureTime in new signatures
        /// </summary>
        /// <remarks>Legal formats specified in Opc book and reproduced here:
        /// YYYY-MM-DDThh:mm:ss.sTZD
        /// YYYY-MM-DDThh:mm:ssTZD
        /// YYYY-MM-DDThh:mmTZD
        /// YYYY-MM-DD         
        /// YYYY-MM                  
        /// YYYY
        /// 
        /// where: 
        /// Y = year, M = month integer (leading zero), D = day integer (leading zero), 
        /// hh = 24hr clock hour
        /// mm = minutes (leading zero)
        /// ss = seconds (leading zero)
        /// .s = tenths of a second
        /// </remarks>
        public String TimeFormat
        {
            get
            {
                return _signatureTimeFormat;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (XmlSignatureProperties.LegalFormat(value))
                    _signatureTimeFormat = value;
                else
                    throw new FormatException(SR.Get(SRID.BadSignatureTimeFormatString));
            }
        }

        //------------------------------------------------------
        //
        //  Public Fields
        //
        //------------------------------------------------------
        /// <summary>
        /// Name of signature origin part
        /// </summary>
        /// <value></value>
        /// <remarks>This value may vary by Package because the name is not formally defined. While this 
        /// implementation will generally use the same default value, the value returned by this property will reflect 
        /// whatever origin is already present in the current Package (if any) which may vary between implementations.
        /// </remarks>
        public Uri SignatureOrigin
        {
            get
            {
                OriginPartExists();           // force search for OriginPart in case it is different from default
                return _originPartName;
            }
        }

        /// <summary>
        /// Type of default signature origin relationship
        /// </summary>
        /// <value></value>
        static public String SignatureOriginRelationshipType
        {
            get
            {
                return _originRelationshipType;
            }
        }

        /// <summary>
        /// Default hash algorithm
        /// </summary>
        /// <value></value>
        static public String DefaultHashAlgorithm
        {
            get
            {
                // If we set the compatibility flag, return the legacy default (SHA1).
                return (BaseAppContextSwitches.UseSha1AsDefaultHashAlgorithmForDigitalSignatures) ? SignedXml.XmlDsigSHA1Url : _defaultHashAlgorithm;
            }
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Create a new PackageDigitalSignature manager
        /// </summary>
        /// <param name="package">container to work with</param>
        /// <remarks>based on the default origin</remarks>
        /// <exception cref="ArgumentNullException">package is null</exception>
        public PackageDigitalSignatureManager(Package package)
        {
            if (package == null)
                throw new ArgumentNullException("package");

            _parentWindow = IntPtr.Zero;
            _container = package;

            // initialize the transform dictionary with defaults
            _transformDictionary = new Dictionary<String, String>(4);
            _transformDictionary[PackagingUtilities.RelationshipPartContentType.ToString()] = SignedXml.XmlDsigC14NTransformUrl;    // relationship parts
            _transformDictionary[XmlDigitalSignatureProcessor.ContentType.ToString()] = SignedXml.XmlDsigC14NTransformUrl;          // xml signature
        }

        #region Sign
        /// <summary>
        /// Sign - prompts for certificate and embeds it
        /// </summary>
        /// <param name="parts">list of parts to sign</param>
        /// <remarks>Set ParentWindow before this call if you want to make the certificate
        /// selection dialog modal to a particular window.  Does not prompt for certificates if none could be located in the default certificate store.</remarks>
        /// <returns>null if no certificate could be located, or if the user cancels from the certificate selection dialog.</returns>
        public PackageDigitalSignature Sign(IEnumerable<Uri> parts)
        {
            X509Certificate certificate = PromptForSigningCertificate(ParentWindow);
            if (certificate == null)
                return null;
            else
                return Sign(parts, certificate);
        }

        /// <summary>
        /// Sign - certificate provided by caller
        /// </summary>
        /// <param name="parts">list of parts to sign</param>
        /// <param name="certificate">signer's certificate</param>
        public PackageDigitalSignature Sign(IEnumerable<Uri> parts, X509Certificate certificate)
        {
            // create unique signature name
            return Sign(parts, certificate, null);
        }
        
        /// <summary>
        /// Sign - certificate provided by caller
        /// </summary>
        /// <param name="parts">list of parts to sign - may be empty or null</param>
        /// <param name="certificate">signer's certificate</param>
        /// <param name="relationshipSelectors">relationshipSelectors that hold information about 
        /// the relationships to be signed - may be empty or null</param>
        /// <remarks>one of parts or relationships must be non-null and contain at least a single entry</remarks>
        public PackageDigitalSignature Sign(IEnumerable<Uri> parts, X509Certificate certificate, IEnumerable<PackageRelationshipSelector> relationshipSelectors)
        {
            // use default signature Id
            return Sign(parts, certificate, relationshipSelectors, XTable.Get(XTable.ID.OpcSignatureAttrValue));
        }
        
                /// <summary>
        /// Sign - certificate provided by caller
        /// </summary>
        /// <param name="parts">list of parts to sign - may be empty or null</param>
        /// <param name="certificate">signer's certificate</param>
        /// <param name="relationshipSelectors">relationshipSelectors that hold information about 
        /// the relationships to be signed - may be empty or null</param>
        /// <param name="signatureId">id for the new Signature - may be empty or null</param>  
        /// <remarks>one of parts or relationships must be non-null and contain at least a single entry</remarks>
        public PackageDigitalSignature Sign(
            IEnumerable<Uri> parts,
            X509Certificate certificate,
            IEnumerable<PackageRelationshipSelector> relationshipSelectors,
            String signatureId)
        {
            // Cannot both be null - need to check here because the similar check in the super-overload cannot
            // distinguish to this level.
            if (parts == null && relationshipSelectors == null)
            {
                throw new ArgumentException(SR.Get(SRID.NothingToSign));
            }

            return Sign(parts, certificate, relationshipSelectors, signatureId, null, null);
        }

        /// <summary>
        /// Sign - caller specifies custom "Object" and/or SignedInfo "Reference" tags
        /// </summary>
        /// <param name="parts">list of parts to sign - may be empty or null</param>
        /// <param name="certificate">signer's certificate</param>
        /// <param name="relationshipSelectors">relationshipSelectors that hold information about 
        /// the relationships to be signed - may be empty or null</param>
        /// <param name="signatureId">id for the new Signature - may be empty or null</param>  
        /// <param name="objectReferences">references to custom object tags.  The DigestMethod on each
        /// Reference will be ignored.  The signature will use the globally defined HashAlgorithm
        /// obtained from the current value of the HashAlgorithm property.</param>
        /// <param name="signatureObjects">objects (signed or not)</param>
        /// <exception cref="InvalidOperationException">Thrown if any TransformMapping
        /// defines an empty or null transform for the ContentType of any Part being signed or if an unknown
        /// transform is encountered.</exception>
        /// <exception cref="System.Xml.XmlException">Thrown if signatureId is non-null and violates the
        /// Xml Id schema (essentially - no leading digit is allowed).</exception>
        /// <remarks>One of parts, relationships, signatureObjects and objectReferences must be 
        /// non-null and contain at least a single entry.
        /// This and every other Sign overload makes use of the current state of the TransformMapping
        /// dictionary which defines a Transform to apply based on ContentType.  The Opc specification
        /// only currently allows for two legal Transform algorithms: C14 and C14N.
        /// Note that the w3c Xml Signature standard does not allow for empty Manifest tags.
        /// Because the Opc specification requires the existence of a Package-specific Object
        /// tag and further specifies that this Object tag contain a Manifest and SignatureProperties
        /// tags, it follows that this Manifest tag must include at least one Reference tag.
        /// This means that every signature include at least one of a Part to sign (non-empty parts tag)
        /// or a Relationship to sign (non-empty relationshipSelectors) even if such a signature
        /// is only destined to sign signatureObjects and/or objectReferences.
        /// This overload provides support for generation of Xml signatures that require custom
        /// Object tags.  For any provided Object tag to be signed, a corresponding Reference
        /// tag must be provided with a Uri that targets the Object tag using local fragment 
        /// syntax.  If the object had an ID of "myObject" the Uri on the Reference would
        /// be "#myObject".  For unsigned objects, no reference is required.</remarks>
        public PackageDigitalSignature Sign(
            IEnumerable<Uri> parts, 
            X509Certificate certificate,
            IEnumerable<PackageRelationshipSelector> relationshipSelectors,
            String signatureId,
            IEnumerable<System.Security.Cryptography.Xml.DataObject> signatureObjects,
            IEnumerable<System.Security.Cryptography.Xml.Reference> objectReferences)
        {
            if (ReadOnly)
                throw new InvalidOperationException(SR.Get(SRID.CannotSignReadOnlyFile));

            VerifySignArguments(parts, certificate, relationshipSelectors, signatureId, signatureObjects, objectReferences);

            // substitute default id if none given
            if (String.IsNullOrEmpty(signatureId))
            {
                signatureId = "packageSignature";   // default
            }

            // Make sure the list reflects what's in the package.
            // Do this before adding the new signature part because we don't want it included until it
            // is fully formed (and delaying the add saves us having to remove it in case there is an 
            // error during the Sign call).
            EnsureSignatures();

            Uri newSignaturePartName = GenerateSignaturePartName();
            if (_container.PartExists(newSignaturePartName))
                throw new ArgumentException(SR.Get(SRID.DuplicateSignature));

            // Pre-create origin part if it does not already exist.
            // Do this before signing to allow for signing the package relationship part (because a Relationship
            // is added from the Package to the Origin part by this call) and the Origin Relationship part in case this is
            // a Publishing signature and the caller wants the addition of more signatures to break this signature.
            PackageRelationship relationshipToNewSignature = OriginPart.CreateRelationship(newSignaturePartName, TargetMode.Internal,
                    _originToSignatureRelationshipType);
            _container.Flush();     // ensure the origin relationship part is persisted so that any signature will include this newest relationship

            VerifyPartsExist(parts);

            // sign the data and optionally embed the certificate
            bool embedCertificateInSignaturePart = (_certificateEmbeddingOption == CertificateEmbeddingOption.InSignaturePart);

            // convert cert to version2 - more functionality
            X509Certificate2 exSigner = certificate as X509Certificate2;
            if (exSigner == null)
                exSigner = new X509Certificate2(certificate.Handle);

            //PRESHARP: Parameter to this public method must be validated:  A null-dereference can occur here.
            //      Parameter 'exSigner' to this public method must be validated:  A null-dereference can occur here. 
            //This is a false positive as the checks above can gurantee no null dereference will occur  
#pragma warning disable 6506

            PackageDigitalSignature signature = null;
            PackagePart newSignaturePart = null;
            try
            {
                // create the new part
                newSignaturePart = _container.CreatePart(newSignaturePartName, XmlDigitalSignatureProcessor.ContentType.ToString());

                // do the actual signing - only Xml signatures currently supported
                signature = XmlDigitalSignatureProcessor.Sign(this, newSignaturePart, parts, relationshipSelectors, exSigner, signatureId, embedCertificateInSignaturePart,
                    signatureObjects, objectReferences);
            }
            catch (InvalidOperationException)
            {
                // bad hash algorithm - revert changes
                // guarantees proper cleanup including removal of Origin if appropriate
                // Note: _signatures.Count reflects the number of signatures that were 
                // existing before this sign method was called. So we want to leave those 
                // untouched and clean up what we added in this method prior to the 
                // exception. If the count is zero, we will also delete the origin part.
                InternalRemoveSignature(newSignaturePartName, _signatures.Count);      
                _container.Flush();    // actually persist the revert
                throw;
            }
            catch (System.IO.IOException)
            {
                // failure to open part - revert changes
                // guarantees proper cleanup including removal of Origin if appropriate
                // Note: _signatures.Count reflects the number of signatures that were 
                // existing before this sign method was called. So we want to leave those 
                // untouched and clean up what we added in this method prior to the 
                // exception. If the count is zero, we will also delete the origin part.
                InternalRemoveSignature(newSignaturePartName, _signatures.Count);
                _container.Flush();    // actually persist the revert
                throw;
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                // failure to sign - revert changes
                // guarantees proper cleanup including removal of Origin if appropriate
                // Note: _signatures.Count reflects the number of signatures that were 
                // existing before this sign method was called. So we want to leave those 
                // untouched and clean up what we added in this method prior to the 
                // exception. If the count is zero, we will also delete the origin part.
                InternalRemoveSignature(newSignaturePartName, _signatures.Count);
                _container.Flush();    // actually persist the revert
                throw;
            }

            // add to the list
            _signatures.Add(signature);

            // embed certificate if called for
            if (_certificateEmbeddingOption == CertificateEmbeddingOption.InCertificatePart)
            {
                // create the cert part
                // auto-generate a certificate name - will be the same for the same certificate
                Uri certificatePartName = PackUriHelper.CreatePartUri(new Uri(
                    CertificatePart.PartNamePrefix + exSigner.SerialNumber + CertificatePart.PartNameExtension, UriKind.Relative));

                // create the serialization helper class (side-effect of creating or opening the part)
                CertificatePart certPart = new CertificatePart(_container, certificatePartName);
                certPart.SetCertificate(exSigner);

                // establish a relationship
                newSignaturePart.CreateRelationship(certificatePartName, TargetMode.Internal, CertificatePart.RelationshipType);
                signature.SetCertificatePart(certPart);
            }
#pragma warning restore 6506

            _container.Flush();

            // return to caller in case they need it
            return signature;
        }
        #endregion

        #region CounterSign
        /// <summary>
        /// CounterSign - prompts for certificate and embeds it based on current CertificateEmbeddingOption
        /// </summary>
        /// <remarks>Set ParentWindow before this call if you want to make the certificate
        /// selection dialog modal to a particular window.  Does not present the dialog if no suitable certificate 
        /// could be found in the default certificate store.
        /// Signs all existing signature parts so that any change to these part(s) will invalidate the
        /// returned signature.</remarks>
        /// <exception cref="InvalidOperationException">Cannot CounterSign an unsigned package.</exception>
        /// <returns>null if no certificate could be located, or if the user cancels from the certificate selection dialog.</returns>
        public PackageDigitalSignature Countersign()
        {
            // Counter-sign makes no sense if we are not already signed
            // Check before asking for certificate
            if (!IsSigned)
                throw new InvalidOperationException(SR.Get(SRID.NoCounterSignUnsignedContainer));

            // prompt for certificate
            X509Certificate certificate = PromptForSigningCertificate(ParentWindow);
            if (certificate == null)
                return null;
            else
                return Countersign(certificate);
        }

        /// <summary>
        /// CounterSign - certificate provided
        /// </summary>
        /// <param name="certificate">signer's certificate</param>
        /// <exception cref="InvalidOperationException">Cannot CounterSign an unsigned package.</exception>
        /// <exception cref="ArgumentNullException">certificate must be non-null.</exception>
        /// <remarks>Signs all existing signature parts so that any change to these part(s) will invalidate the
        /// returned signature.</remarks>
        public PackageDigitalSignature Countersign(X509Certificate certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException("certificate");

            // Counter-sign makes no sense if we are not already signed
            // Check before asking for certificate
            if (!IsSigned)
                throw new InvalidOperationException(SR.Get(SRID.NoCounterSignUnsignedContainer));

            // sign all existing signatures
            List<Uri> signatures = new List<Uri>(_signatures.Count);
            for (int i = 0; i < _signatures.Count; i++)
            {
                signatures.Add(_signatures[i].SignaturePart.Uri);
            }

            // sign
            return Sign(signatures, certificate);
        }

        /// <summary>
        /// CounterSign - signature part name(s) specified by caller
        /// </summary>
        /// <param name="certificate">signer's certificate</param>
        /// <param name="signatures">signature parts to sign</param>
        /// <remarks>Signs the given signature parts so that any change to these part(s) will invalidate the
        /// returned signature.</remarks>
        /// <exception cref="InvalidOperationException">Cannot CounterSign an unsigned package.</exception>
        /// <exception cref="ArgumentException">signatures must be non-empty and cannot refer to parts other than signature parts.</exception>
        /// <exception cref="ArgumentNullException">Both arguments must be non-null.</exception>
        public PackageDigitalSignature Countersign(X509Certificate certificate, IEnumerable<Uri> signatures)
        {
            if (certificate == null)
                throw new ArgumentNullException("certificate");

            if (signatures == null)
                throw new ArgumentNullException("signatures");

            // Counter-sign makes no sense if we are not already signed
            if (!IsSigned)
                throw new InvalidOperationException(SR.Get(SRID.NoCounterSignUnsignedContainer));

            // Restrict signatures to be actual signature part references
            foreach (Uri uri in signatures)
            {
                PackagePart part = _container.GetPart(uri);
                if (!part.ValidatedContentType().AreTypeAndSubTypeEqual(XmlDigitalSignatureProcessor.ContentType))
                    throw new ArgumentException(SR.Get(SRID.CanOnlyCounterSignSignatureParts, signatures));
            }

            return Sign(signatures, certificate);
        }
        #endregion

        /// <summary>
        /// verify all signatures - calls verify on each signature
        /// </summary>
        /// <param name="exitOnFailure">true to exit on first failure - false to continue</param>
        /// <remarks>register for invalid signature events</remarks>
        public VerifyResult VerifySignatures(bool exitOnFailure)
        {
            VerifyResult result;
            EnsureSignatures();

            // signed?
            if (_signatures.Count == 0)
                result = VerifyResult.NotSigned;
            else
            {
                // contract is to return a failure value, even if there are subsequent successes
                // defaulting to success here simplifies the logic for this
                result = VerifyResult.Success;     // default
                for (int i = 0; i < _signatures.Count; i++)
                {
                    VerifyResult temp = _signatures[i].Verify();
                    if (temp != VerifyResult.Success)
                    {
                        result = temp;  // note failure
                        
                        if (InvalidSignatureEvent != null)
                            InvalidSignatureEvent(this, new SignatureVerificationEventArgs(_signatures[i], temp));

                        if (exitOnFailure)
                            break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Remove a signature
        /// </summary>
        /// <param name="signatureUri">signature to remove</param>
        /// <remarks>Caller should call Package.Flush() in order to persist changes.</remarks>
        public void RemoveSignature(Uri signatureUri)
        {
            if (ReadOnly)
                throw new InvalidOperationException(SR.Get(SRID.CannotRemoveSignatureFromReadOnlyFile));

            if (signatureUri == null)
                throw new ArgumentNullException("signatureUri");

            // empty?
            if (!IsSigned)      // calls EnsureSignatures for us
                return;

            // find the signature
            int index = GetSignatureIndex(signatureUri);
            if (index < 0)
                return;

            try
            {
                Debug.Assert(index < _signatures.Count);

                //After this signature is removed the total number of signatures remaining will
                //be _signatures.Count - 1. If this count is zero, then additional clean up needs
                //to be done, like removing the Origin part.
                InternalRemoveSignature(signatureUri, _signatures.Count - 1 /*since we are deleting one*/);

                // invalidate the signature itself
                _signatures[index].Invalidate();
            }
            finally
            {
                _signatures.RemoveAt(index);    // ensure it is actually removed from the list
            }
        }

        /// <summary>
        /// Remove all signatures based on this origin
        /// </summary>
        /// <remarks>also removes all certificate parts and the signature origin.  Caller must call Flush() to persist changes.</remarks>
        public void RemoveAllSignatures()
        {
            if (ReadOnly)
                throw new InvalidOperationException(SR.Get(SRID.CannotRemoveSignatureFromReadOnlyFile));

            EnsureSignatures();

            try
            {
                // Remove via known traversal - required to find all signatures (we may not know all signature content-types).
                for (int i = 0; i < _signatures.Count; i++)
                {
                    PackagePart p = _signatures[i].SignaturePart;

                    // Delete any Certificate part(s) targeted by this signature.  We know that all of the
                    // reference counts will reach zero because we are removing all signatures.
                    foreach (PackageRelationship r in p.GetRelationshipsByType(CertificatePart.RelationshipType))
                    {
                        // don't resolve if external
                        if (r.TargetMode != TargetMode.Internal)
                            continue;   // fail silently

                        _container.DeletePart(PackUriHelper.ResolvePartUri(r.SourceUri, r.TargetUri));                 // will not throw if part not found
                    }

                    // delete signature part
                    _container.DeletePart(p.Uri);

                    // invalidate the signature itself
                    _signatures[i].Invalidate();
                }

                DeleteOriginPart();
            }
            finally
            {
                // update internal variables
                _signatures.Clear();
            }
        }

        /// <summary>
        /// Obtain the PackageDigitalSignature referred to by the given Uri
        /// </summary>
        /// <param name="signatureUri">ID obtained from a PackageDigitalSignature object</param>
        /// <returns>null if signature not found</returns>       
        public PackageDigitalSignature GetSignature(Uri signatureUri)
        {
            if (signatureUri == null)
                throw new ArgumentNullException("signatureUri");

            int index = GetSignatureIndex(signatureUri);
            if (index < 0)
                return null;
            else
            {
                Debug.Assert(index < _signatures.Count);
                return _signatures[index];
            }
        }

        /// <summary>
        /// Verify Certificate
        /// </summary>
        /// <param name="certificate">certificate to inspect</param>
        /// <exception cref="System.Security.Cryptography.CryptographicException">certificate is invalid but the error code is not recognized</exception>
        /// <returns>the first error encountered when inspecting the certificate chain or NoError if the certificate is valid</returns>
        public static X509ChainStatusFlags VerifyCertificate(X509Certificate certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException("certificate");

            X509ChainStatusFlags status = X509ChainStatusFlags.NoError;

            // build the certificate chain
            X509Chain chain = new X509Chain();
            bool valid = chain.Build(new X509Certificate2(certificate.Handle));

            // inspect the results
            if (!valid)
            {
                X509ChainStatus[] chainStatus = chain.ChainStatus;
                for (int i = 0; i < chainStatus.Length; i++)
                {
                    status |= chainStatus[i].Status;
                }
            }

            return status;
        }
        #endregion

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Get package - used by DigitalSignatureProcessors
        /// </summary>
        internal Package Package
        {
            get
            {
                return _container;
            }
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// PromptForSigningCertificate - invoked from Sign overloads if certificate is not provided by caller
        /// </summary>
        /// <param name="hwndParent"></param>
        /// <returns>null if user cancels or no certificate could be located</returns>
        static internal X509Certificate PromptForSigningCertificate(IntPtr hwndParent)
        {
            X509Certificate2 X509cert = null;

            // look for appropriate certificates
            X509Store store = new X509Store(StoreLocation.CurrentUser);
            store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
            X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;

            // narrow down the choices
            // timevalid
            collection = collection.Find(X509FindType.FindByTimeValid, DateTime.Now, true);

            // intended for signing (or no intent specified)
            collection = collection.Find(X509FindType.FindByKeyUsage, X509KeyUsageFlags.DigitalSignature, false);

            // remove certs that don't have private key
            // work backward so we don't disturb the enumeration
            for (int i = collection.Count - 1; i >= 0; i--)
            {
                if (!collection[i].HasPrivateKey)
                {
                    collection.RemoveAt(i);
                }
            }

            // any suitable certificates available?
            if (collection.Count > 0)
            {
                // ask user to select
                collection = X509Certificate2UI.SelectFromCollection(collection, SR.Get(SRID.CertSelectionDialogTitle), SR.Get(SRID.CertSelectionDialogMessage), X509SelectionFlag.SingleSelection, hwndParent);
                if (collection.Count > 0)
                {
                    X509cert = collection[0];   // return the first one
                }
            }

            return X509cert;
        }

        #region Private Members
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Predicate for use with List.Exists()
        /// </summary>
        private class StringMatchPredicate
        {
            public StringMatchPredicate(String id)
            {
                _id = id;
            }

            public bool Match(String id)
            {
                return (String.CompareOrdinal(_id, id) == 0);
            }

            private string _id;
        }

        /// <summary>
        /// Verify Parts Exist before signing
        /// </summary>
        /// <param name="parts"></param>
        /// <remarks>This call must be done after the signature Origin has been created to allow for 
        /// callers to sign an Origin (or it's relationship part) for the first signature in the package.</remarks>
        private void VerifyPartsExist(IEnumerable<Uri> parts)
        {
            // check for missing parts
            if (parts != null)
            {
                foreach (Uri partUri in parts)
                {
                    if (!_container.PartExists(partUri))
                    {
                        // delete origin part if it was created and this is the first signature
                        if (_signatures.Count == 0)
                            DeleteOriginPart();

                        throw new ArgumentException(SR.Get(SRID.PartToSignMissing), "parts");
                    }
                }
            }
}

        /// <summary>
        /// Verifies arguments to Sign() method - sub-function to reduce complexity in Sign() logic
        /// </summary>
        /// <param name="parts"></param>
        /// <param name="certificate"></param>
        /// <param name="relationshipSelectors"></param>
        /// <param name="signatureId"></param>
        /// <param name="signatureObjects"></param>
        /// <param name="objectReferences"></param>
        private void VerifySignArguments(IEnumerable<Uri> parts,
            X509Certificate certificate,
            IEnumerable<PackageRelationshipSelector> relationshipSelectors,
            String signatureId,
            IEnumerable<System.Security.Cryptography.Xml.DataObject> signatureObjects,
            IEnumerable<System.Security.Cryptography.Xml.Reference> objectReferences)
        {
            if (certificate == null)
                throw new ArgumentNullException("certificate");

            // Check for empty collections in order to provide negative feedback as soon as possible.
            if (EnumeratorEmptyCheck(parts) && EnumeratorEmptyCheck(relationshipSelectors)
                && EnumeratorEmptyCheck(signatureObjects) && EnumeratorEmptyCheck(objectReferences))
                throw new ArgumentException(SR.Get(SRID.NothingToSign));

            // check for illegal and/or duplicate id's in signatureObjects
            if (signatureObjects != null)
            {
                List<String> ids = new List<String>();
                foreach (DataObject obj in signatureObjects)
                {
                    // ensure they don't duplicate the reserved one
                    if (String.CompareOrdinal(obj.Id, XTable.Get(XTable.ID.OpcAttrValue)) == 0)
                        throw new ArgumentException(SR.Get(SRID.SignaturePackageObjectTagMustBeUnique), "signatureObjects");

                    // check for duplicates
                    //if (ids.Contains(obj.Id))
                    if (ids.Exists(new StringMatchPredicate(obj.Id).Match))
                        throw new ArgumentException(SR.Get(SRID.SignatureObjectIdMustBeUnique), "signatureObjects");
                    else
                        ids.Add(obj.Id);
                }
            }

            // ensure id is legal Xml id
            if (!String.IsNullOrEmpty(signatureId))
            {
                try
                {
                    // An XSD ID is an NCName that is unique.
                    System.Xml.XmlConvert.VerifyNCName(signatureId);
                }
                catch (System.Xml.XmlException xmlException)
                {
                    throw new ArgumentException(SR.Get(SRID.NotAValidXmlIdString, signatureId), "signatureId", xmlException);
                }
            }
        }

        /// <summary>
        /// Returns true if the given enumerator is null or empty
        /// </summary>
        /// <param name="enumerable">may be null</param>
        /// <returns>true if enumerator is empty or null</returns>
        private bool EnumeratorEmptyCheck(System.Collections.IEnumerable enumerable)
        {
            if (enumerable == null)
                return true;            // null means empty

            // see if it's really a collection as this is more efficient than enumerating
            System.Collections.ICollection collection = enumerable as System.Collections.ICollection;
            if (collection != null)
            {
                return (collection.Count == 0);
            }
            else
            {
                // not a collection - do things the hard way
                foreach (Object o in enumerable)
                {
                    return false;   // if we get here - we're not empty
                }

                return true;        // empty
            }
        }

        /// <summary>
        /// Remove a signature - helper method
        /// </summary>
        /// <param name="signatureUri">signature to remove</param>
        /// <param name="countOfSignaturesRemaining">number of signatures that will remain 
        /// after the remove operation. If this count becomes zero, then we can remove the
        /// origin part also from the package as there will be no remaining signatures 
        /// in the package.</param>
        /// <remarks>Caller should call Package.Flush() in order to persist changes.</remarks>
        private void InternalRemoveSignature(Uri signatureUri, int countOfSignaturesRemaining)
        {
            Debug.Assert(signatureUri != null);
            Debug.Assert(countOfSignaturesRemaining >= 0);

            // Remove origin if this operation will have removed the last signature in order to conform with Metro specification.
            // This will remove all relationships too so the code in the "else" clause becomes redundant and we can skip it.
            if (countOfSignaturesRemaining == 0)
            {
                DeleteOriginPart();
            }
            else    // there will be at least a single signature left after this remove, so we need to be more delicate in our surgery
            {
                SafeVisitRelationships(
                    OriginPart.GetRelationshipsByType(_originToSignatureRelationshipType),
                    DeleteRelationshipToSignature, signatureUri);
            }

            // delete the cert (if any) if it's reference count will become zero
            SafeVisitRelationships(_container.GetPart(signatureUri).GetRelationshipsByType(CertificatePart.RelationshipType),
                DeleteCertificateIfReferenceCountBecomesZeroVisitor);

            // delete the signature part
            _container.DeletePart(signatureUri);
        }

        // return true to continue
        private delegate bool RelationshipOperation(PackageRelationship r, Object context);

        /// <summary>
        /// Visit relationships without disturbing the PackageRelationshipCollection iterator
        /// </summary>
        /// <param name="relationships">collection of relationships to visit</param>
        /// <param name="visit">function to call with each relationship in the list</param>
        private void SafeVisitRelationships(PackageRelationshipCollection relationships, RelationshipOperation visit)
        {
            SafeVisitRelationships(relationships, visit, null);
        }

        /// <summary>
        /// Visit relationships without disturbing the PackageRelationshipCollection iterator
        /// </summary>
        /// <param name="relationships">collection of relationships to visit</param>
        /// <param name="visit">function to call with each relationship in the list</param>
        /// <param name="context">context object - may be null</param>
        private void SafeVisitRelationships(PackageRelationshipCollection relationships, RelationshipOperation visit, Object context)
        {
            // make a local copy that will not be invalidated by any activity of the visitor function
            List<PackageRelationship> relationshipsToVisit = new List<PackageRelationship>(relationships);

            // now invoke the delegate for each member
            for (int i = 0; i < relationshipsToVisit.Count; i++)
            {
                // exit if visitor wants us to
                if (!visit(relationshipsToVisit[i], context))
                    break;
            }
        }

        /// <summary>
        /// Removes the certificate associated with the given signature if removing the signature would leave the
        /// certificate part orphaned.
        /// </summary>
        private bool DeleteCertificateIfReferenceCountBecomesZeroVisitor(PackageRelationship r, Object context)
        {
            // don't resolve if external
            if (r.TargetMode != TargetMode.Internal)
                throw new FileFormatException(SR.Get(SRID.PackageSignatureCorruption));
            
            Uri certificatePartName = PackUriHelper.ResolvePartUri(r.SourceUri, r.TargetUri);
            if (CertificatePartReferenceCount(certificatePartName) == 1)    // we are part of the calculation so one is the magic number
                _container.DeletePart(certificatePartName);                 // will not throw if part not found

            return true;
        }

        /// <summary>
        /// Deletes any relationship that is of the type that relates a Package to the Digital Signature Origin
        /// </summary>
        /// <param name="r"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private bool DeleteRelationshipOfTypePackageToOriginVisitor(PackageRelationship r, Object context)
        {
            Debug.Assert(Uri.Compare(r.SourceUri, 
                                     MS.Internal.IO.Packaging.PackUriHelper.PackageRootUri, 
                                     UriComponents.SerializationInfoString, 
                                     UriFormat.UriEscaped, 
                                     StringComparison.Ordinal) == 0, 
                "Logic Error: This visitor should only be called with relationships from the Package itself");

            // don't resolve if external
            if (r.TargetMode != TargetMode.Internal)
                throw new FileFormatException(SR.Get(SRID.PackageSignatureCorruption));

            Uri targetUri = PackUriHelper.ResolvePartUri(r.SourceUri, r.TargetUri);
            if (PackUriHelper.ComparePartUri(targetUri, _originPartName) == 0)
                _container.DeleteRelationship(r.Id);

            return true;
        }

        /// <summary>
        /// Deletes any relationship to the given signature from the signature origin
        /// </summary>
        /// <param name="r">relationship from origin</param>
        /// <param name="signatureUri">signatureUri</param>
        /// <returns>true</returns>
        private bool DeleteRelationshipToSignature(PackageRelationship r, Object signatureUri)
        {
            Uri uri = signatureUri as Uri;
            Debug.Assert(uri != null, "Improper use of delegate - context must be Uri");

            // don't resolve if external
            if (r.TargetMode != TargetMode.Internal)
                throw new FileFormatException(SR.Get(SRID.PackageSignatureCorruption));

            if (PackUriHelper.ComparePartUri(PackUriHelper.ResolvePartUri(r.SourceUri, r.TargetUri), uri) == 0)
            {
                OriginPart.DeleteRelationship(r.Id);    // don't break early in case there are redundant relationships
            }

            return true;
        }

        private void DeleteOriginPart()
        {
            try
            {
                // remove all relationships of the type "package-to-signature-origin"
                SafeVisitRelationships(_container.GetRelationshipsByType(_originRelationshipType), 
                    DeleteRelationshipOfTypePackageToOriginVisitor);

                _container.DeletePart(_originPartName);
            }
            finally
            {
                // reset state variables
                _originPartExists = false;
                _originSearchConducted = true;
                _originPart = null;
            }
        }

        /// <summary>
        /// Lookup the index of the signature object in the _signatures array by the name of the part
        /// </summary>
        /// <param name="uri">name of the signature part</param>
        /// <returns>zero-based index or -1 if not found</returns>
        private int GetSignatureIndex(Uri uri)
        {
            EnsureSignatures();
            for (int i = 0; i < _signatures.Count; i++)
            {
                if (PackUriHelper.ComparePartUri(uri, _signatures[i].SignaturePart.Uri) == 0)
                    return i;
            }
            return -1;      // not found
        }


        /// <summary>
        /// Counts the number of signatures using the given certificate
        /// </summary>
        /// <param name="certificatePartUri">certificate to inspect</param>
        private int CertificatePartReferenceCount(Uri certificatePartUri)
        {
            // count the number of signatures that reference this certificate part
            int count = 0;
            for (int i = 0; i < _signatures.Count; i++)
            {
                // for each signature, follow it's certificate link (if there) and compare the Uri
                if (_signatures[i].GetCertificatePart() != null)
                {
                    // same Uri?
                    if (PackUriHelper.ComparePartUri(certificatePartUri, _signatures[i].GetCertificatePart().Uri) == 0)
                        ++count;
                }
            }

            return count;
        }

        /// <summary>
        /// Generate guid-based signature name to reduce chances of conflict in merging scenarios
        /// </summary>
        /// <returns></returns>
        private Uri GenerateSignaturePartName()
        {
            return PackUriHelper.CreatePartUri(new Uri(_defaultSignaturePartNamePrefix +
                Guid.NewGuid().ToString(_guidStorageFormatString, (IFormatProvider)null) + _defaultSignaturePartNameExtension, UriKind.Relative));
        }

        // load signatures from container
        private void EnsureSignatures()
        {
            if (_signatures == null)
            {
                _signatures = new List<PackageDigitalSignature>();

                // no signatures if origin not found
                if (OriginPartExists())
                {
                    // find all signatures from this origin (if any)
                    PackageRelationshipCollection relationships = _originPart.GetRelationshipsByType(
                        _originToSignatureRelationshipType);

                    foreach (PackageRelationship r in relationships)
                    {
                        // don't resolve if external
                        if (r.TargetMode != TargetMode.Internal)
                            throw new FileFormatException(SR.Get(SRID.PackageSignatureCorruption));

                        Uri signaturePartName = PackUriHelper.ResolvePartUri(_originPart.Uri, r.TargetUri);

                        // throw if part does not exist
                        if (!_container.PartExists(signaturePartName))
                            throw new FileFormatException(SR.Get(SRID.PackageSignatureCorruption));

                        PackagePart signaturePart = _container.GetPart(signaturePartName);

                        // ignore future signature types that we do not recognize
                        if (signaturePart.ValidatedContentType().AreTypeAndSubTypeEqual
                            (XmlDigitalSignatureProcessor.ContentType))
                        {
                            // parse it
                            PackageDigitalSignature signature = new PackageDigitalSignature(this, signaturePart);

                            // add to the list
                            _signatures.Add(signature);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Looks for part name of Origin by searching from the container root and following the metro origin part relationship
        /// </summary>
        /// <remarks>side effect of assigning the _originPartName and _originPart if found</remarks>
        /// <returns>true if found</returns>
        private bool OriginPartExists()
        {
            // only search once
            if (!_originSearchConducted)
            {
                try
                {
                    Debug.Assert(!_originPartExists, "Logic Error: If OriginPartExists, OriginSearchConducted should be true.");
                    PackageRelationshipCollection containerRelationships = _container.GetRelationshipsByType(_originRelationshipType);
                    foreach (PackageRelationship r in containerRelationships)
                    {
                        // don't resolve if external
                        if (r.TargetMode != TargetMode.Internal)
                            throw new FileFormatException(SR.Get(SRID.PackageSignatureCorruption));

                        // resolve target (may be relative)
                        Uri targetUri = PackUriHelper.ResolvePartUri(r.SourceUri, r.TargetUri);

                        // if part does not exist - we throw
                        if (!_container.PartExists(targetUri))
                            throw new FileFormatException(SR.Get(SRID.SignatureOriginNotFound));

                        PackagePart p = _container.GetPart(targetUri);

                        // inspect content type - ignore things we don't understand
                        if (p.ValidatedContentType().AreTypeAndSubTypeEqual(_originPartContentType))
                        {
                            // throw if more than one relationship to an origin part that we recognize
                            if (_originPartExists)
                                throw new FileFormatException(SR.Get(SRID.MultipleSignatureOrigins));

                            // overwrite default if some container is using some other name
                            _originPartName = targetUri;
                            _originPart = p;
                            _originPartExists = true;
                        }
                    }
                }
                finally
                {
                    _originSearchConducted = true;
                }
            }
            return _originPartExists;
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------
        private bool ReadOnly
        {
            get
            {
                return (_container.FileOpenAccess == FileAccess.Read);
            }
        }

        private PackagePart OriginPart
        {
            get
            {
                if (_originPart == null)
                {
                    if (!OriginPartExists())
                    {
                        // add if not found
                        _originPart = _container.CreatePart(_originPartName, _originPartContentType.ToString());
                        _container.CreateRelationship(_originPartName, TargetMode.Internal, _originRelationshipType);
                    }
                }

                return _originPart;
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        private CertificateEmbeddingOption      _certificateEmbeddingOption;
        private Package                         _container;
        private IntPtr                          _parentWindow;
        private static Uri _defaultOriginPartName = PackUriHelper.CreatePartUri(new Uri("/package/services/digital-signature/origin.psdsor", UriKind.Relative));
        private Uri                             _originPartName = _defaultOriginPartName;
        private PackagePart                     _originPart;
        private String                          _hashAlgorithmString = DefaultHashAlgorithm;
        private String                          _signatureTimeFormat = XmlSignatureProperties.DefaultDateTimeFormat;
        private List<PackageDigitalSignature>   _signatures;
        private Dictionary<String, String>      _transformDictionary;
        private bool                            _originSearchConducted;             // don't look more than once for Origin part
        private bool                            _originPartExists;                  // was the part found?
        private ReadOnlyCollection<PackageDigitalSignature> _signatureList;         // lazy-init cached return value for Signatures property

        private static readonly ContentType _originPartContentType = new ContentType("application/vnd.openxmlformats-package.digital-signature-origin");

        private static readonly String _guidStorageFormatString = @"N";     // N - simple format without adornments
        private static readonly String _defaultHashAlgorithm =  "http://www.w3.org/2001/04/xmlenc#sha256";
        private static readonly String _originRelationshipType = "http://schemas.openxmlformats.org/package/2006/relationships/digital-signature/origin";
        private static readonly String _originToSignatureRelationshipType = "http://schemas.openxmlformats.org/package/2006/relationships/digital-signature/signature";
        private static readonly String _defaultSignaturePartNamePrefix = "/package/services/digital-signature/xml-signature/";
        private static readonly String _defaultSignaturePartNameExtension = ".psdsxs";
        #endregion Private Members
    }
}

