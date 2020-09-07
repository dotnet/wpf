// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Packaging;
using System.Net;
using System.Security;              // For elevations
using System.Security.Cryptography.X509Certificates;
using System.Windows.TrustUI;
using System.Windows.Xps.Packaging;

using MS.Internal.PresentationUI;   // For FriendAccessAllowed

namespace MS.Internal.Documents
{
    /// <summary>
    /// DigitalSignatureProvider is used to connect DRP to Xps dig sig 
    /// </summary>
    [FriendAccessAllowed]
    internal class DigitalSignatureProvider : IDigitalSignatureProvider
    {
        #region Constructors
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="package">The package whose signatures this provider
        /// will manipulate</param>
        public DigitalSignatureProvider(Package package)
        {
            if (package != null)
            {
                XpsDocument = new XpsDocument(package);
                FixedDocumentSequence = XpsDocument.FixedDocumentSequenceReader;
                if (FixedDocumentSequence == null)
                {
                    throw new ArgumentException(SR.Get(SRID.DigitalSignatureNoFixedDocumentSequence));
                }
                // We only want to save the first fixed document since all
                // XPS Viewer signature definitions will be added to the first
                // fixed document.
                FixedDocument =
                    FixedDocumentSequence.FixedDocuments[0];
            }
            else
            {
                throw new ArgumentNullException("package");
            }
        }
        #endregion Constructors

        #region IDigitalSignatureProvider
        //------------------------------------------------------
        //
        //  IDigitalSignatureProvider
        //
        //------------------------------------------------------

        /// <summary>
        /// See IDigitalSignatureProvider
        /// </summary>
        bool IDigitalSignatureProvider.IsSigned
        {
            get
            {
                return XpsDocument.Signatures.Count > 0;
            }
        }

        /// <summary>
        /// See IDigitalSignatureProvider
        /// </summary>        
        bool IDigitalSignatureProvider.IsSignable
        {
            get
            {
                if (!_isSignableCacheValid.Value)
                {
                    _isSignableCache.Value = XpsDocument.IsSignable;
                    _isSignableCacheValid.Value = true;
                }

                return _isSignableCache.Value;                        
            }
        }

        /// <summary>
        /// See IDigitalSignatureProvider
        /// </summary>
        bool IDigitalSignatureProvider.HasRequests
        {
            get
            {
                bool rtn = false;

                foreach (DigitalSignature digitalSignature in ((IDigitalSignatureProvider)this).Signatures)
                {
                    if (digitalSignature.SignatureState == SignatureStatus.NotSigned)
                    {
                        rtn = true;
                        break;
                    }
                }

                return rtn;
            }
        }

        /// <summary>
        /// See IDigitalSignatureProvider
        /// </summary>
        void IDigitalSignatureProvider.SignDocument(DigitalSignature digitalSignature)
        {
            AssertIsSignable();

            XpsDigSigPartAlteringRestrictions reachRestrictions = XpsDigSigPartAlteringRestrictions.None;


            if (digitalSignature.IsDocumentPropertiesRestricted)
            {
                reachRestrictions |= XpsDigSigPartAlteringRestrictions.CoreMetadata;
            }

            // If additional signatures should invalidate this signature, we
            // need to sign the signature origin part
            if (digitalSignature.IsAddingSignaturesRestricted)
            {
                reachRestrictions |= XpsDigSigPartAlteringRestrictions.SignatureOrigin;
            }

            // a null guid means there was no associated spot, so create a guid
            if (digitalSignature.GuidID == null)
            {
                digitalSignature.GuidID = Guid.NewGuid();
            }

            XpsDigitalSignature xpsDigitalSignature =
                XpsDocument.SignDigitally(
                    digitalSignature.Certificate,
                    true,
                    reachRestrictions,
                    (Guid)digitalSignature.GuidID,
                    false  /* don't re-verify IsSignable, we've already done it */
                    );


            if (xpsDigitalSignature != null)
            {
                // Fill in relevant fields from the XPS signature
                digitalSignature.XpsDigitalSignature = xpsDigitalSignature;
                digitalSignature.SignatureState = SignatureStatus.Valid;
                digitalSignature.SignedOn = xpsDigitalSignature.SigningTime;

                // Save the simple name from the certificate as the subject name
                // in the signature
                digitalSignature.SubjectName =
                    digitalSignature.Certificate.GetNameInfo(
                        X509NameType.SimpleName,
                        false /* don't include issuer name */);

                // Add the new signature to the list (if it isn't already there).
                // That is a possibility since the first signature in a document
                // is always added as a signature definition and a signature.
                if (!DigitalSignatureList.Contains(digitalSignature))
                {
                    DigitalSignatureList.Add(digitalSignature);
                }
            }
        }

        /// <summary>
        /// See IDigitalSignatureProvider
        /// </summary>
        Guid IDigitalSignatureProvider.AddRequestSignature(DigitalSignature digitalSignature)
        {
            AssertIsSignable();

            // Create guid used for signature ID
            Guid guidID = Guid.NewGuid();

            // Create a new SignatureDefinition
            XpsSignatureDefinition xpsSignatureDefinition = new XpsSignatureDefinition();

            // Use the digSig to setup the SignatureDefinition.
            xpsSignatureDefinition.RequestedSigner = digitalSignature.SubjectName;
            xpsSignatureDefinition.Intent = digitalSignature.Reason;
            xpsSignatureDefinition.SigningLocale = digitalSignature.Location;
            xpsSignatureDefinition.SignBy = digitalSignature.SignedOn;            

            // Use our new guid to setup the ID
            xpsSignatureDefinition.SpotId = guidID;

            // Add the signature definition to the document
            FixedDocument.AddSignatureDefinition(xpsSignatureDefinition);
            FixedDocument.CommitSignatureDefinition();

            // Set the signature's status to Not Signed before adding to our list
            digitalSignature.SignatureState = SignatureStatus.NotSigned;

            // Add the new signature to our list of signatures and definitions
            DigitalSignatureList.Add(digitalSignature);

            return guidID;
        }

        /// <summary>
        /// See IDigitalSignatureProvider
        /// </summary>
        void IDigitalSignatureProvider.RemoveRequestSignature(Guid spotId)
        {
            AssertIsSignable();

            XpsSignatureDefinition definition = FindSignatureDefinition(spotId);
            if (definition != null)
            {
                FixedDocument.RemoveSignatureDefinition(definition);
                FixedDocument.CommitSignatureDefinition();
            }

            // Loop through the signature list and remove the entry for the
            // requested signature
            foreach (DigitalSignature signature in DigitalSignatureList)
            {
                if (signature.GuidID == spotId)
                {
                    // We only want to remove unsigned signature definitions
                    // (requested signatures) and not actual signatures.
                    if (signature.SignatureState == SignatureStatus.NotSigned)
                    {
                        DigitalSignatureList.Remove(signature);
                    }

                    // It is safe to remove an element from the list that we're
                    // currently enumerating because we stop enumerating as soon
                    // as we find the element we're looking for.
                    break;
                }
            }
        }

        /// <summary>
        /// See IDigitalSignatureProvider
        /// </summary>
        void IDigitalSignatureProvider.UnsignDocument(Guid id)
        {
            AssertIsSignable();

            foreach (DigitalSignature signature in DigitalSignatureList)
            {
                if (signature.GuidID == id)
                {
                    // Remove the associated XpsDigitalSignature from the
                    // document
                    if (signature.XpsDigitalSignature != null)
                    {
                        XpsDocument.RemoveSignature(signature.XpsDigitalSignature);
                        signature.XpsDigitalSignature = null;
                    }

                    // Check if the document contains a signature definition
                    // corresponding to this signature
                    bool matchesDefinition = (FindSignatureDefinition(id) != null);

                    // If the signature matches a signature definition in the
                    // document, mark the signature as NotSigned (i.e. an
                    // unsigned request) but leave it in the list
                    if (matchesDefinition)
                    {
                        signature.SignatureState = SignatureStatus.NotSigned;
                    }
                    else
                    {
                        // Remove the signature from the list
                        DigitalSignatureList.Remove(signature);
                    }

                    // It is safe to remove an element from the list that we're
                    // currently enumerating because we stop enumerating as soon
                    // as we find the element we're looking for.
                    break;
                }
            }
        }

        /// <summary>
        /// See IDigitalSignatureProvider
        /// </summary>
        void IDigitalSignatureProvider.VerifySignatures()
        {
            // If we haven't yet retrieved signatures from the package, do so
            if (DigitalSignatureList == null)
            {
                DigitalSignatureList = GetSignaturesFromPackage();
            }

            // Verify each XPS digital signature from the map
            // This will update the status of the signatures in the list also
            foreach (DigitalSignature signature in DigitalSignatureList)
            {

                if (signature.XpsDigitalSignature != null)
                {
                    signature.SignatureState =
                        VerifyXpsDigitalSignature(signature.XpsDigitalSignature);
                }
            }
        }

        /// <summary>
        /// See IDigitalSignatureProvider
        /// </summary>
        IList<X509Certificate2> IDigitalSignatureProvider.GetAllCertificates()
        {
            List<X509Certificate2> certificateList = new List<X509Certificate2>();

            foreach (DigitalSignature signature in ((IDigitalSignatureProvider)this).Signatures)
            {
                X509Certificate2 certificate = signature.Certificate;

                if (certificate != null && !certificateList.Contains(certificate))
                {
                    certificateList.Add(certificate);
                }
            }

            return certificateList;
        }

        /// <summary>
        /// See IDigitalSignatureProvider
        /// </summary>
        IDictionary<X509Certificate2, CertificatePriorityStatus> IDigitalSignatureProvider.GetCertificateStatus(
            IList<X509Certificate2> certificates)
        {
            Dictionary<X509Certificate2, CertificatePriorityStatus> certificateStatusTable =
                new Dictionary<X509Certificate2, CertificatePriorityStatus>();

            foreach (X509Certificate2 certificate in certificates)
            {
                certificateStatusTable.Add(
                    certificate,
                    GetCertificateStatus(certificate));
            }

            return certificateStatusTable;
        }
        
        /// <summary>
        /// Returns a Read Only collection of our Digital Signatures.
        /// </summary>
        ReadOnlyCollection<DigitalSignature> IDigitalSignatureProvider.Signatures
        {
            get
            {
                // If we have not yet read signatures from the package, load them
                if (DigitalSignatureList == null)
                {
                    DigitalSignatureList = GetSignaturesFromPackage();
                }

                if (_readOnlySignatureList.Value == null)
                {
                    _readOnlySignatureList.Value =
                        new ReadOnlyCollection<DigitalSignature>(DigitalSignatureList);
                }

                return _readOnlySignatureList.Value;
            }
        }

        #endregion IDigitalSignatureProvider

        #region Private Methods
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Returns a list of all the DigitalSignature objects from the package.
        /// </summary>
        /// <returns>A list of DigitalSignature objects</returns>
        private IList<DigitalSignature> GetSignaturesFromPackage()
        {
            IList<DigitalSignature> signatureList = new List<DigitalSignature>();

            // This will contain a mapping of GUIDs to signature definitions so
            // that we can easily look up the signature definition (if any) that
            // corresponds with a signature
            IDictionary<Guid, XpsSignatureDefinition> signatureDefinitionMap =
                new Dictionary<Guid, XpsSignatureDefinition>();

            // This will contain a list of all the signature definitions that do
            // not have an associated GUID, which means that they have to be
            // requested signatures
            IList<XpsSignatureDefinition> requestedSignatureList =
                new List<XpsSignatureDefinition>();

            // Enumerate all the signature definitions in all of the fixed
            // documents in the XPS document to generate the map of GUIDs to
            // signature definitions

            foreach (IXpsFixedDocumentReader fixedDocument in FixedDocumentSequence.FixedDocuments)
            {
                ICollection<XpsSignatureDefinition> documentSignatureDefinitionList =
                    fixedDocument.SignatureDefinitions;

                if (documentSignatureDefinitionList != null)
                {
                    // Add each signature definition to either the GUID map or
                    // the list of requested signatures
                    foreach (XpsSignatureDefinition signatureDefinition in documentSignatureDefinitionList)
                    {
                        // If the signature definition has a GUID, add it to the map
                        if (signatureDefinition.SpotId != null)
                        {
                            signatureDefinitionMap.Add(signatureDefinition.SpotId.Value, signatureDefinition);
                        }
                        // If it does not have a GUID it cannot match a signature yet,
                        // so add it to the list of requested signatures
                        else
                        {
                            requestedSignatureList.Add(signatureDefinition);
                        }
                    }
                }
            }

            // Now loop through all the XpsDigitalSignatures, matching them with
            // signature definitions by GUID to get the signature fields that
            // are only found in signature definitions.
            foreach (XpsDigitalSignature xpsDigitalSignature in XpsDocument.Signatures)
            {
                // Convert the XPS signature into our format
                DigitalSignature digitalSignature =
                    ConvertXpsDigitalSignature(xpsDigitalSignature);

                // Check if the signature corresponds to a definition by seeing
                // if the GUID is in the signature definition map
                bool definitionFound =
                    xpsDigitalSignature.Id.HasValue &&
                    signatureDefinitionMap.ContainsKey(xpsDigitalSignature.Id.Value);

                // If the signature corresponds to a signature definition, copy
                // fields from the corresponding definition
                if (definitionFound)
                {
                    XpsSignatureDefinition signatureDefinition =
                        signatureDefinitionMap[xpsDigitalSignature.Id.Value];

                    // Copy SignatureDefinition fields
                    digitalSignature.Reason = signatureDefinition.Intent;
                    digitalSignature.Location = signatureDefinition.SigningLocale;

                    // Now that we have found a signature that matches this
                    // signature definition, it can no longer match any other
                    // signatures by GUID and we can remove it from the map.
                    signatureDefinitionMap.Remove(xpsDigitalSignature.Id.Value);
                }
                
                signatureList.Add(digitalSignature);
            }

            // What is left over in the signatureDefinitionMap are definitions
            // that don't have matching XpsDigSigs.  Add these as requested
            // signatures.
            foreach (XpsSignatureDefinition signatureDefinition in signatureDefinitionMap.Values)
            {
                //Add this request signature to our list.
                signatureList.Add(ConvertXpsSignatureDefinition(signatureDefinition));
            }

            // Add all the definitions we already knew were requested signatures
            foreach (XpsSignatureDefinition definition in requestedSignatureList)
            {
                //Add this request signature to our list.
                signatureList.Add(ConvertXpsSignatureDefinition(definition));
            }

            return signatureList;
        }

        /// <summary>
        /// Maps an XpsDigitalSignature to our DigitalSignature.
        /// </summary>
        /// <param name="xpsDigitalSignature">The signature to convert</param>
        /// <returns>A DigitalSignature that corresponds to the signature
        /// passed in as a parameter</returns>
        private static DigitalSignature ConvertXpsDigitalSignature(XpsDigitalSignature xpsDigitalSignature)
        {
            DigitalSignature digitalSignature = new DigitalSignature();

            digitalSignature.XpsDigitalSignature = xpsDigitalSignature;

            X509Certificate2 x509Certificate2 =
                xpsDigitalSignature.SignerCertificate as X509Certificate2;

            digitalSignature.SignatureState = SignatureStatus.Unknown;

            // Copy simple fields if cert isn't null.  If it is null then the
            // cert wasn't embedded into container so don't copy cert related
            // fields.
            if (x509Certificate2 != null)
            {
                digitalSignature.Certificate = x509Certificate2;
                digitalSignature.SignedOn = xpsDigitalSignature.SigningTime;

                // save the simple name from the certificate as the subject name
                // in the signature
                digitalSignature.SubjectName =
                    x509Certificate2.GetNameInfo(
                        X509NameType.SimpleName,
                        false  /* don't include issuer name */);
            }

            digitalSignature.IsDocumentPropertiesRestricted =
                xpsDigitalSignature.DocumentPropertiesRestricted;

            // If the signature origin part is signed, adding new signatures
            // will invalidate this signature
            digitalSignature.IsAddingSignaturesRestricted =
                xpsDigitalSignature.SignatureOriginRestricted;

            //These fields come from a Signature Definition.
            digitalSignature.Reason = string.Empty;
            digitalSignature.Location = string.Empty;

            return digitalSignature;
        }

        /// <summary>
        /// Maps an XpsSignatureDefinition to our DigitalSignature.
        /// </summary>
        /// <param name="signatureDefinition">The signature definition to
        /// convert</param>
        /// <returns>A DigitalSignature representing a requested signature with
        /// signature status NotSigned</returns>
        private static DigitalSignature ConvertXpsSignatureDefinition(XpsSignatureDefinition signatureDefinition)
        {
            //Create new DigSig.  This is a request and will have the status NotSigned.
            DigitalSignature digitalSignature = new DigitalSignature();
            digitalSignature.SignatureState = SignatureStatus.NotSigned;

            //set fields using the definition.
            digitalSignature.SubjectName = signatureDefinition.RequestedSigner;
            digitalSignature.Reason = signatureDefinition.Intent;
            digitalSignature.SignedOn = signatureDefinition.SignBy;
            digitalSignature.Location = signatureDefinition.SigningLocale;
            digitalSignature.GuidID = signatureDefinition.SpotId;

            return digitalSignature;
        }

        /// <summary>
        /// Verifies a given XPS digital signature.
        /// </summary>
        /// <remarks>This computes and checks the hashes of all the package
        /// parts, so it may take a long time to complete.</remarks>
        /// <param name="xpsSignature">The XPS signature to verify</param>
        /// <returns>The status of the signature</returns>
        private static SignatureStatus VerifyXpsDigitalSignature(XpsDigitalSignature xpsDigitalSignature)
        {
            SignatureStatus status = SignatureStatus.Unknown;

            //Verify signature and map to DRPs SignatureStatus enum.
            switch (xpsDigitalSignature.Verify())
            {
                case VerifyResult.Success:
                    {
                        status = SignatureStatus.Valid;
                        break;
                    }

                case VerifyResult.NotSigned:
                    {
                        status = SignatureStatus.NotSigned;
                        break;
                    }

                default:
                    {
                        status = SignatureStatus.Invalid;
                        break;
                    }
            }

            return status;
        }

        /// <summary>
        /// Gets the CertificateStatus for a specific certificate.
        /// </summary>
        private static CertificatePriorityStatus GetCertificateStatus(X509Certificate2 certificate)
        {
            //Default status is the most severe error:  Corrupted
            CertificatePriorityStatus certificatePriorityStatus = CertificatePriorityStatus.Corrupted;
            X509ChainStatusFlags x509ChainStatusFlags;

            // Use the static VerifyCertificate method on XpsDigitalSignature
            // to verify the certificate
            x509ChainStatusFlags = XpsDigitalSignature.VerifyCertificate(certificate);

            //Strip out all known flags (minus Cyclic and NotSignatureValid).  What is left are any unknown flags
            //and flags that convert to Corrupted.
            X509ChainStatusFlags x509RemainingFlags = (x509ChainStatusFlags ^ _x509NonCorruptedFlags) &
                                                     ~(_x509NonCorruptedFlags);  

            //x509ChainStatusFlags is a flag we want to convert to a CertificatePriorityStatus.
            //First we need to make sure there are no unknown flags.  If there is an unknown
            //flag we assume that it is the worst possible error and leave CertificateStatus
            //set to Corrupted.  Leaving out Cyclic and NotSignatureValid since they also convert
            //to Corrupted.
            if (x509RemainingFlags == X509ChainStatusFlags.NoError)
            {
                //The following flags convert to CannotBeVerified
                if ((x509ChainStatusFlags & _x509CannotBeVerifiedFlags) != X509ChainStatusFlags.NoError)
                {
                    certificatePriorityStatus = CertificatePriorityStatus.CannotBeVerified;
                }
                //The following flags convert to IssuerNotTrusted
                else if ((x509ChainStatusFlags & _x509IssuerNotTrustedFlags) != X509ChainStatusFlags.NoError)
                {
                    certificatePriorityStatus = CertificatePriorityStatus.IssuerNotTrusted;
                }
                //The following flags convert to Revoked
                else if ((x509ChainStatusFlags & _x509RevokedFlags) != X509ChainStatusFlags.NoError)
                {
                    certificatePriorityStatus = CertificatePriorityStatus.Revoked;
                }
                //The following flags convert to Expired
                else if ((x509ChainStatusFlags & _x509ExpiredFlags) != X509ChainStatusFlags.NoError)
                {
                    certificatePriorityStatus = CertificatePriorityStatus.Expired;
                }
                //The following flags are all considered Igorable
                else
                {
                    certificatePriorityStatus = CertificatePriorityStatus.Ok;
                }
            }

            return certificatePriorityStatus;
        }

        /// <summary>
        /// Returns a signature definition in the document that matches a GUID.
        /// </summary>
        /// <param name="id">The GUID to match</param>
        /// <returns>The corresponding signature definition, if one exists.
        /// </returns>
        private XpsSignatureDefinition FindSignatureDefinition(Guid id)
        {
            XpsSignatureDefinition definition = null;

            // Loop through our collection of definitions and find the matching
            // GUID. We only need to look at the fixed document we have saved
            // (and not the other documents in the sequence) since we always
            // save signature definitions to that fixed document.
            foreach (XpsSignatureDefinition signatureDefinition in FixedDocument.SignatureDefinitions)
            {
                if (signatureDefinition.SpotId == id)
                {
                    definition = signatureDefinition;
                    break;
                }
            }

            return definition;
        }

        /// <summary>
        /// Sanity check that the content is signable, and that the check for this
        /// has been done prior to calling Signing-related methods.
        /// </summary>
        private void AssertIsSignable()
        {
            // We assert that _isSignableCacheValid is true here --
            // we don't want to block on calling XpsDocument.IsSignable so we
            // require calling code do that work prior to invoking SignDocument.
            Invariant.Assert(_isSignableCacheValid.Value);

            // Assert that the document is actually signable.  We should never
            // get here if it's not.
            Invariant.Assert(_isSignableCache.Value);
        }

        #endregion Private Methods

        #region Private Properties
        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Gets or sets the XPS document from which to read signatures.
        /// </summary>
        private XpsDocument XpsDocument
        {
            get
            {
                return _xpsDocument.Value;
            }

            set
            {
                _xpsDocument.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the fixed document to which to write signature
        /// definitions.
        /// </summary>
        private IXpsFixedDocumentReader FixedDocument
        {
            get
            {
                return _fixedDocument.Value;
            }

            set
            {
                _fixedDocument.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the fixed document sequence 
        /// </summary>
        private IXpsFixedDocumentSequenceReader FixedDocumentSequence
        {
            get
            {
                return _fixedDocumentSequence.Value;
            }

            set
            {
                _fixedDocumentSequence.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the list of all the signatures in the package.
        /// </summary>
        private IList<DigitalSignature> DigitalSignatureList
        {
            get
            {
                return _digitalSignatureList;
            }

            set
            {
                _digitalSignatureList = value;
            }
        }
        #endregion Private Properties

        #region Private Fields
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        /// <summary>
        /// The XPS document from which to read signatures.
        /// </summary>
        SecurityCriticalDataForSet<XpsDocument> _xpsDocument;

        /// <summary>
        /// The fixed document sequence to which to write signature definitions.
        /// </summary>
        SecurityCriticalDataForSet<IXpsFixedDocumentSequenceReader> _fixedDocumentSequence;

        /// <summary>
        /// The fixed document to which to write signature definitions.
        /// </summary>
        SecurityCriticalDataForSet<IXpsFixedDocumentReader> _fixedDocument;

        /// <summary>
        /// A list of all the signatures in the package.
        /// </summary>
        IList<DigitalSignature> _digitalSignatureList;

        /// <summary>
        /// A cached read-only version of the signature list. This is a wrapper
        /// around _digitalSignatureList that is intended to be passed out by
        /// the Signatures property.
        /// </summary>
        SecurityCriticalDataForSet<ReadOnlyCollection<DigitalSignature>> _readOnlySignatureList;

        //Contains all known flags that don't convert to Corrupted.
        //(All flags except Cyclic and NotSignatureValid).  We will be looking for unknown flags using this
        //and if any exist then status will be set to Corrupted.
        private const X509ChainStatusFlags _x509NonCorruptedFlags = 
                                                    X509ChainStatusFlags.HasExcludedNameConstraint |
                                                    X509ChainStatusFlags.HasNotDefinedNameConstraint |
                                                    X509ChainStatusFlags.HasNotPermittedNameConstraint |
                                                    X509ChainStatusFlags.HasNotSupportedNameConstraint |
                                                    X509ChainStatusFlags.InvalidBasicConstraints |
                                                    X509ChainStatusFlags.InvalidExtension |
                                                    X509ChainStatusFlags.InvalidNameConstraints |
                                                    X509ChainStatusFlags.InvalidPolicyConstraints |
                                                    X509ChainStatusFlags.NoIssuanceChainPolicy |
                                                    X509ChainStatusFlags.PartialChain |
                                                    X509ChainStatusFlags.UntrustedRoot |
                                                    X509ChainStatusFlags.Revoked |
                                                    X509ChainStatusFlags.NotTimeValid |
                                                    X509ChainStatusFlags.NoError |
                                                    X509ChainStatusFlags.CtlNotSignatureValid |
                                                    X509ChainStatusFlags.CtlNotTimeValid |
                                                    X509ChainStatusFlags.CtlNotValidForUsage |
                                                    X509ChainStatusFlags.NotTimeNested |
                                                    X509ChainStatusFlags.NotValidForUsage |
                                                    X509ChainStatusFlags.OfflineRevocation |
                                                    X509ChainStatusFlags.RevocationStatusUnknown;

        //Create variable that contains all known flags that convert to CannotBeVerified.
        private const X509ChainStatusFlags _x509CannotBeVerifiedFlags = 
                                                    X509ChainStatusFlags.HasExcludedNameConstraint |
                                                    X509ChainStatusFlags.HasNotDefinedNameConstraint |
                                                    X509ChainStatusFlags.HasNotPermittedNameConstraint |
                                                    X509ChainStatusFlags.HasNotSupportedNameConstraint |
                                                    X509ChainStatusFlags.InvalidBasicConstraints |
                                                    X509ChainStatusFlags.InvalidExtension |
                                                    X509ChainStatusFlags.InvalidNameConstraints |
                                                    X509ChainStatusFlags.InvalidPolicyConstraints |
                                                    X509ChainStatusFlags.NoIssuanceChainPolicy;
        
        //Create variable that contains all known flags that convert to IssuerNotTrusted.
        private const X509ChainStatusFlags _x509IssuerNotTrustedFlags = 
                                                    X509ChainStatusFlags.PartialChain |
                                                    X509ChainStatusFlags.UntrustedRoot;

        //Create variable that contains all known flags that convert to Revoked.
        private const X509ChainStatusFlags _x509RevokedFlags =
                                                    X509ChainStatusFlags.Revoked;

        //Create variable that contains all known flags that convert to Expired.
        private const X509ChainStatusFlags _x509ExpiredFlags =
                                                    X509ChainStatusFlags.NotTimeValid;

        /// <summary>
        /// Cached value for the IsSignable property
        /// </summary>
        private SecurityCriticalDataForSet<bool> _isSignableCache;
        private SecurityCriticalDataForSet<bool> _isSignableCacheValid;

        #endregion Private Fields
    }
}
