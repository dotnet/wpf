// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  Implementation of the W3C Digital Signature Handler.
//  Generates and consumes XmlDSig-compliant digital signatures based on the subset
//  specified by the Opc file format.
//


using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;                      // for SecurityCritical and SecurityTreatAsSafe
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.IO;
using System.Windows;
using System.IO.Packaging;
using Microsoft.Win32;
using MS.Internal;
using MS.Internal.WindowsBase;

using MS.Internal.IO.Packaging.Extensions;

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// Signature Handler implementation that follows the Feb 12, 2002 W3C DigSig Recommendation
    /// </summary>
    /// <remarks>See: http://www.w3.org/TR/2002/REC-xmldsig-core-20020212/ for details</remarks>
    internal class XmlDigitalSignatureProcessor
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Constructor - called from PackageDigitalSignatureManager when opening an existing signature
        /// </summary>
        /// <param name="manager">current DigitalSignatureManager</param>
        /// <param name="packageSignature">public signature object</param>
        /// <param name="signaturePart">the part that will/does house the associated XML signature</param>
        internal XmlDigitalSignatureProcessor(PackageDigitalSignatureManager manager,
            PackagePart signaturePart, PackageDigitalSignature packageSignature) : this(manager, signaturePart)
        {
            _signature = packageSignature;
        }

        /// <summary>
        /// Factory method that creates a new PackageDigitalSignature
        /// </summary>
        internal static PackageDigitalSignature Sign(
            PackageDigitalSignatureManager              manager,
            PackagePart                                 signaturePart,
            IEnumerable<Uri>                            parts,
            IEnumerable<System.IO.Packaging.PackageRelationshipSelector>    relationshipSelectors,
            X509Certificate2                            signer,
            String                                      signatureId,
            bool                                        embedCertificate,
            IEnumerable<System.Security.Cryptography.Xml.DataObject> signatureObjects,
            IEnumerable<System.Security.Cryptography.Xml.Reference> objectReferences)
        {
            // create
            XmlDigitalSignatureProcessor p = new XmlDigitalSignatureProcessor(manager, signaturePart);

            // and sign
            return p.Sign(parts, relationshipSelectors, signer, signatureId,
                embedCertificate, signatureObjects, objectReferences);
        }

        /// <summary>
        /// Verify the given signature
        /// </summary>
        /// <exception cref="InvalidOperationException">throws if no certificate found in the signature</exception>
        /// <returns>true if the data stream has not been altered since it was signed</returns>
        internal bool Verify()
        {
            return Verify(Signer);
        }

        /// <summary>
        /// Verify the given signature
        /// </summary>
        /// <param name="signer">certificate to use (ignores any embedded cert)</param>
        /// <returns>true if the data stream has not been altered since it was signed</returns>
        internal bool Verify(X509Certificate2 signer)
        {
            Invariant.Assert(signer != null);

            // Create a SignedXml to do the dirty work
            SignedXml xmlSig = EnsureXmlSignatureParsed();

            bool result = false;

            // Validate the Reference tags in the SignedInfo as per the 
            // restrictions imposed by the OPC spec
            ValidateReferences(xmlSig.SignedInfo.References, true /*allowPackageSpecificReference*/);

            // verify "standard" XmlSignature portions
            result = xmlSig.CheckSignature(signer, true);

            if (result)
            {
                HashAlgorithm hashAlgorithm = null;                 // optimize - generally only need to create and re-use one of these
                String currentHashAlgorithmName = String.Empty;     // guaranteed not to match

                try
                {
                    try
                    {
                        // if that succeeds, verify the Manifest including Part/Relationship content and ContentTypes
                        ParsePackageDataObject();
                    }
                    catch (XmlException)
                    {
                        // parsing exception - means this is a bad signature
                        return false;
                    }

                    foreach (PartManifestEntry partEntry in _partEntryManifest)
                    {
                        // compare the content
                        Stream s = null;

                        // Relationship requires special handling
                        if (partEntry.IsRelationshipEntry)
                        {
                            // This behaves correctely even if the Relationship Part is missing entirely
                            s = GetRelationshipStream(partEntry);
                        }
                        else    // Part entry - inspect raw stream
                        {
                            // part is guaranteed to exist at this point because we fail early in PackageDigitalSignature.Verify()
                            Debug.Assert(_manager.Package.PartExists(partEntry.Uri));

                            // Compare the content type first so we can fail early if it doesn't match 
                            // (faster than hashing the content itself).
                            // Compare ordinal case-sensitive which is more strict than normal ContentType
                            // comparision because this is manadated by the OPC specification.
                            PackagePart part = _manager.Package.GetPart(partEntry.Uri);
                            if (String.CompareOrdinal(
                                partEntry.ContentType.OriginalString,
                                part.ValidatedContentType().OriginalString) != 0)
                            {
                                result = false;     // content type mismatch
                                break;
                            }
                            s = part.GetSeekableStream(FileMode.Open, FileAccess.Read);
                        }

                        using (s)
                        {
                            // ensure hash algorithm object is available - re-use if possible
                            if (((hashAlgorithm != null) && (!hashAlgorithm.CanReuseTransform)) ||
                                String.CompareOrdinal(partEntry.HashAlgorithm, currentHashAlgorithmName) != 0)
                            {
                                if (hashAlgorithm != null)
                                    ((IDisposable)hashAlgorithm).Dispose();

                                currentHashAlgorithmName = partEntry.HashAlgorithm;
                                hashAlgorithm = GetHashAlgorithm(currentHashAlgorithmName);

                                // not a supported or recognized algorithm?
                                if (hashAlgorithm == null)
                                {
                                    // return invalid result instead of throwing exception
                                    result = false;
                                    break;
                                }
                            }

                            // calculate hash
                            String base64EncodedHashValue = GenerateDigestValue(s, partEntry.Transforms, hashAlgorithm);

                            // now compare the hash - must be identical
                            if (String.CompareOrdinal(base64EncodedHashValue, partEntry.HashValue) != 0)
                            {
                                result = false;     // hash mismatch
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    if (hashAlgorithm != null)
                        ((IDisposable)hashAlgorithm).Dispose();
                }
            }

            return result;
        }

        /// <summary>
        /// Get the list of transforms applied to the given part (works for Relationship parts too)
        /// </summary>
        /// <param name="partName">part to get transforms for</param>
        /// <returns>possibly empty, ordered list of transforms applied to the given part (never null)</returns>
        /// <remarks>This method only returns transform names.  Transform-specific properties can be obtained by parsing the actual
        /// signature contents which conform to the W3C XmlDSig spec.</remarks>
        internal List<String> GetPartTransformList(Uri partName)
        {
            // query the parsed manifest
            ParsePackageDataObject();

            List<String> transformList = null;

            // look through the manifest for the requested part
            foreach (PartManifestEntry entry in _partEntryManifest)
            {
                if (System.IO.Packaging.PackUriHelper.ComparePartUri(entry.Uri, partName) == 0)
                {
                    transformList = entry.Transforms;
                    break;
                }
            }

            // never return null - an empty list is better form
            if (transformList == null)
                transformList = new List<String>();

            return transformList;
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Content type of signature parts created by this processor
        /// </summary>
        internal static ContentType ContentType
        {
            get
            {
                return _xmlSignaturePartType;
            }
        }


        /// <summary>
        /// Associated signature part
        /// </summary>
        internal PackagePart SignaturePart
        {
            get
            {
                return _signaturePart;
            }
        }


        /// <summary>
        /// Obtain the list of signed parts
        /// </summary>
        /// <returns>Authors identity in handler-proprietary format</returns>
        /// <exception cref="XmlException">if signature xml is malformed</exception>
        internal List<Uri> PartManifest
        {
            get
            {
                ParsePackageDataObject();
                return _partManifest;
            }
        }

        /// <summary>
        /// Obtain the author's identity as a byte stream
        /// </summary>
        /// <returns>Authors identity in handler-proprietary format</returns>
        /// <exception cref="XmlException">if signature xml is malformed</exception>
        internal List<System.IO.Packaging.PackageRelationshipSelector> RelationshipManifest
        {
            get
            {
                ParsePackageDataObject();
                return _relationshipManifest;
            }
        }

        /// <summary>
        /// Obtain the author's identity in X509 Certificate form
        /// </summary>
        /// <returns>Authors identity as a certificate in X509 form, or null if none found</returns>
        internal X509Certificate2 Signer
        {
            get
            {
                // lazy init when loading existing cert - Sign may have assigned this for us
                if (_certificate == null)
                {
                    // first look for cert part
                    if (PackageSignature.GetCertificatePart() != null)
                        _certificate = PackageSignature.GetCertificatePart().GetCertificate();
                    else
                    {
                        // look in signature
                        if (_lookForEmbeddedCert)
                        {
                            IEnumerator keyInfoClauseEnum = EnsureXmlSignatureParsed().KeyInfo.GetEnumerator(typeof(KeyInfoX509Data));
                            while (keyInfoClauseEnum.MoveNext())
                            {
                                KeyInfoX509Data x509Data = (KeyInfoX509Data)keyInfoClauseEnum.Current;
                                foreach (X509Certificate2 cert in x509Data.Certificates)
                                {
                                    // just take the first one for now
                                    _certificate = cert;
                                    break;
                                }

                                // just need one for now
                                if (_certificate != null)
                                    break;
                            }

                            // only need to do this once 
                            _lookForEmbeddedCert = false;
                        }
                    }
                }

                return _certificate;    // may be null
            }
        }

        /// <summary>
        /// encrypted hash value
        /// </summary>
        /// <value></value>
        internal byte[] SignatureValue
        {
            get
            {
                return EnsureXmlSignatureParsed().SignatureValue;
            }
        }

        /// <summary>
        /// The object that actually creates the signature
        /// Note: This API is exposed to the public API surface through the 
        /// PackageDigitalSignature.Signature property. Through this API it is 
        /// possible to create Signatures that are not OPC compliant. However, 
        /// at verify time, we will throw exceptions for non-conforming signatures. 
        /// </summary>
        /// <value>object of type System.Security.Cryptography.Xml.Signature</value>
        internal Signature Signature
        {
            get
            {
                return EnsureXmlSignatureParsed().Signature;
            }
            set
            {
                UpdatePartFromSignature(value);
            }
        }

        /// <summary>
        /// Get the given signature
        /// </summary>
        internal PackageDigitalSignature PackageSignature
        {
            get
            {
                return _signature;
            }
        }

        /// <summary>
        /// Time that the signature was created
        /// </summary>
        /// <returns>Time of signing if available, or DateTime.MinValue if signature was not time-stamped</returns>
        internal DateTime SigningTime
        {
            get
            {
                // lazy init
                ParsePackageDataObject();
                return _signingTime;
            }
        }

        internal String TimeFormat
        {
            get
            {
                // lazy init
                ParsePackageDataObject();
                return _signingTimeFormat;
            }
        }

        //------------------------------------------------------
        //
        //  Digest Helper Functions
        //
        //------------------------------------------------------
        /// <summary>
        /// Generate digest value tag
        /// </summary>
        /// <param name="s"></param>
        /// <param name="transformName">name of the single transform to use - may be null</param>
        /// <param name="hashAlgorithm">hash algorithm to use</param>
        /// <returns></returns>
        internal static String GenerateDigestValue(
            Stream s,
            String transformName,
            HashAlgorithm hashAlgorithm)
        {
            List<String> transforms = null;
            if (transformName != null)
            {
                transforms = new List<String>(1);
                transforms.Add(transformName);
            }
            return GenerateDigestValue(s, transforms, hashAlgorithm);
        }

        /// <summary>
        /// Generate digest value tag
        /// </summary>
        /// <param name="transforms">transforms to apply - may be null and list may contain empty strings</param>
        /// <param name="s">stream to hash</param>
        /// <param name="hashAlgorithm">algorithm to use</param>
        internal static String GenerateDigestValue(
            Stream s,
            List<String> transforms,
            HashAlgorithm hashAlgorithm)
        {
            s.Seek(0, SeekOrigin.Begin);

            // We need to be able to dispose streams generated by the
            // Transform object but we don't want to dispose the stream
            // passed to us so we insert this to block any propagated Dispose() calls.
            Stream transformStream = new IgnoreFlushAndCloseStream(s);

            List<Stream> transformStreams = null;

            // canonicalize the part content if asked
            if (transforms != null)
            {
                transformStreams = new List<Stream>(transforms.Count);
                transformStreams.Add(transformStream);
                foreach (String transformName in transforms)
                {
                    // ignore empty strings at this point (as well as Relationship Transforms) - these are legal
                    if ((transformName.Length == 0)
                        || (String.CompareOrdinal(transformName, XTable.Get(XTable.ID.RelationshipsTransformName)) == 0))
                    {
                        continue;
                    }

                    // convert the transform names into objects (if defined)
                    Transform transform = StringToTransform(transformName);

                    if (transform == null)
                    {
                        // throw XmlException so the outer loop knows the signature is invalid
                        throw new XmlException(SR.UnsupportedTransformAlgorithm);
                    }

                    transformStream = TransformXml(transform, transformStream);
                    transformStreams.Add(transformStream);
                }
            }

            // hash it and encode to Base64
            String hashValueString = System.Convert.ToBase64String(HashStream(hashAlgorithm, transformStream));

            // dispose of any generated streams
            if (transformStreams != null)
            {
                foreach (Stream stream in transformStreams)
                    stream.Close();
            }

            return hashValueString;
        }

        /// <summary>
        /// Synthesizes a NodeList of Reference tags to hash
        /// </summary>
        /// <param name="relationships"></param>
        /// <returns></returns>
        internal static Stream GenerateRelationshipNodeStream(IEnumerable<System.IO.Packaging.PackageRelationship> relationships)
        {
            // create a NodeList containing valid Relationship XML and serialize it to the stream
            Stream s = new MemoryStream();

            // Wrap in a stream that ignores Flush and Close so the XmlTextWriter
            // will not close it.
            // use UTF-8 encoding by default
            using (XmlTextWriter writer = new XmlTextWriter(new IgnoreFlushAndCloseStream(s),
                System.Text.Encoding.UTF8))
            {
                // start outer Relationships tag
                writer.WriteStartElement(XTable.Get(XTable.ID.RelationshipsTagName), PackagingUtilities.RelationshipNamespaceUri);

                // generate a valid Relationship tag according to the Opc schema
                InternalRelationshipCollection.WriteRelationshipsAsXml(writer, relationships,
                        true,  /* systematically write target mode */
                        false  /* not in streaming production */
                        );

                // end of Relationships tag
                writer.WriteEndElement();
            }

            return s;
        }

        /// <summary>
        /// Convert algorithm name to object
        /// </summary>
        /// <param name="hashAlgorithmName">fully specified name</param>
        /// <returns>HashAlgorithm object or null if it does not map to a supported hash type</returns>
        /// <remarks>Caller is responsible for calling Dispose() on returned object</remarks>
        internal static HashAlgorithm GetHashAlgorithm(String hashAlgorithmName)
        {
            Object o = CryptoConfig.CreateFromName(hashAlgorithmName);
            HashAlgorithm algorithm = o as HashAlgorithm;

            // In the case that we get a valid crypto object that is not a hash algorithm
            // we should attempt to dispose it if it offers IDisposable.
            if (algorithm == null && o != null)
            {
                IDisposable disposable = o as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }

            return algorithm;
        }

        /// <remarks> 
        /// IMPORTANT NOTE:
        /// 1. In the XmlDigitalSignatureProcessor.IsValidXmlCanonicalizationTransform method, 
        /// we have similar logic regarding these two transforms.So both these methods must be updated
        /// in sync.
        /// </remarks>
        private static Transform StringToTransform(String transformName)
        {
            Invariant.Assert(transformName != null);

            if (String.CompareOrdinal(transformName, SignedXml.XmlDsigC14NTransformUrl) == 0)
            {
                return new XmlDsigC14NTransform();
            }
            else if (String.CompareOrdinal(transformName, SignedXml.XmlDsigC14NWithCommentsTransformUrl) == 0)
            {
                return new XmlDsigC14NWithCommentsTransform();
            }
            else
                return null;
}

        // As per the OPC spec, only two tranforms are valid. Also, both of these happen to be
        // XML canonicalization transforms.
        // In the XmlSignatureManifest.ParseTransformsTag method we make use this method to 
        // validate the transforms to make sure that they are supported by the OPC spec and
        // we also take advantage of the fact that both of them are XML canonicalization transforms
        // IMPORTANT NOTE:
        // 1. In the XmlDigitalSignatureProcessor.StringToTransform method, we have similar logic
        // regarding these two transforms.So both these methods must be updated in sync.
        // 2. If ever this method is updated to add other transforms, careful review must be done to 
        // make sure that methods calling this method are updated as required.
        internal static bool IsValidXmlCanonicalizationTransform(String transformName)
        {
            Invariant.Assert(transformName != null);

            if (String.CompareOrdinal(transformName, SignedXml.XmlDsigC14NTransformUrl) == 0 ||
                String.CompareOrdinal(transformName, SignedXml.XmlDsigC14NWithCommentsTransformUrl) == 0)
            {
                return true;
            }
            else
                return false;
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------
        /// <summary>
        /// Helper class
        /// </summary>
        private SignedXml EnsureXmlSignatureParsed()
        {
            // lazy init
            if (_signedXml == null)
            {
                _signedXml = new CustomSignedXml();

                // Load the XML
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.PreserveWhitespace = true;
                using (Stream s = SignaturePart.GetSeekableStream())
                {
                    using (XmlTextReader xmlReader = new XmlTextReader(s))
                    {
                        //Prohibit DTD from the markup as per the OPC spec
                        xmlReader.ProhibitDtd = true;

                        //This method expects the reader to be in ReadState.Initial.
                        //It will make the first read call.
                        PackagingUtilities.PerformInitailReadAndVerifyEncoding(xmlReader);

                        //If the reader.ReadState is ReadState.Initial, then XmlDocument with perform the
                        //first xmlReader.Read() and start loading from that node/tag. 
                        //If the reader.ReadState is ReadState.Intermediate, then XmlDocument, will start
                        //loading from that location itself.
                        //Note: Since in the above method we perform only the first read and will have not 
                        //moved the reader state further down in the markup, we should be okay, and 
                        //xmlDocument.Load will load from the very begining as intended.
                        xmlDocument.Load(xmlReader);

                        // W3C spec allows for Signature tag to appear as an island and inherently allows
                        // for multiple Signature tags within the same XML document.
                        // OPC restricts this to a single, root-level Signature tag.  However, Signature
                        // tags are allowed to exist within the non-OPC Object tags within an OPC signature.
                        // This is common for XAdES signatures and must be explicitly allowed.
                        XmlNodeList nodeList = xmlDocument.ChildNodes;
                        if (nodeList == null || nodeList.Count == 0 || nodeList.Count > 2)
                            throw new XmlException(SR.PackageSignatureCorruption);

                        XmlNode node = nodeList[0];
                        if (nodeList.Count == 2)
                        {
                            // First node must be the XmlDeclaration <?xml...>
                            if (nodeList[0].NodeType != XmlNodeType.XmlDeclaration)
                                throw new XmlException(SR.PackageSignatureCorruption);

                            // Second node must be in the w3c namespace, and must be the <Signature> tag
                            node = nodeList[1];
                        }

                        if ((node.NodeType != XmlNodeType.Element) ||
                           (String.CompareOrdinal(node.NamespaceURI, SignedXml.XmlDsigNamespaceUrl) != 0) ||
                           (String.CompareOrdinal(node.LocalName, XTable.Get(XTable.ID.SignatureTagName)) != 0))
                        {
                            throw new XmlException(SR.PackageSignatureCorruption);
                        }

                        // instantiate the SignedXml from the xmlDoc
                        _signedXml.LoadXml((XmlElement)node);
                    }
                }
            }

            // As per the OPC spec, only two Canonicalization methods can be specified            
            if (!IsValidXmlCanonicalizationTransform(_signedXml.SignedInfo.CanonicalizationMethod))
                throw new XmlException(SR.UnsupportedCanonicalizationMethod);

            // As per OPC spec, signature ID must be NCName
            if (_signedXml.Signature.Id != null)
            {
                try
                {
                    System.Xml.XmlConvert.VerifyNCName(_signedXml.Signature.Id);
                }
                catch (System.Xml.XmlException)
                {
                    throw new XmlException(SR.PackageSignatureCorruption);
                }
            }

            return _signedXml;
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Constructor - called from public constructor as well as static Sign() method
        /// </summary>
        /// <param name="manager">current DigitalSignatureManager</param>
        /// <param name="signaturePart">the part that will/does house the associated XML signature</param>
        private XmlDigitalSignatureProcessor(PackageDigitalSignatureManager manager,
            PackagePart signaturePart)
        {
            Invariant.Assert(manager != null);
            Invariant.Assert(signaturePart != null);

            _signaturePart = signaturePart;
            _manager = manager;
            _lookForEmbeddedCert = true;
        }

        /// <summary>
        /// Create a new PackageDigitalSignature
        /// </summary>
        /// <param name="parts">the data being protected by this signature</param>
        /// <param name="relationshipSelectors">possibly null collection of relationshipSelectors that represent the
        /// relationships that are to be signed</param>
        /// <param name="signer">Identity of the author</param>
        /// <param name="signatureId">Id attribute of the new Xml Signature</param>  
        /// <param name="embedCertificate">true if caller wants certificate embedded in the signature itself</param>
        /// <param name="objectReferences">references</param>
        /// <param name="signatureObjects">objects to sign</param>
        private PackageDigitalSignature Sign(
            IEnumerable<Uri>                            parts,
            IEnumerable<System.IO.Packaging.PackageRelationshipSelector>    relationshipSelectors,
            X509Certificate2                            signer,
            String                                      signatureId,
            bool                                        embedCertificate,
            IEnumerable<System.Security.Cryptography.Xml.DataObject> signatureObjects,
            IEnumerable<System.Security.Cryptography.Xml.Reference> objectReferences)
        {
            // don't overwrite
#if DEBUG
            using (Stream stream = SignaturePart.GetStream())
            {
                Debug.Assert(stream.Length == 0, "Logic Error: Can't sign when signature already exists");
            }
#endif

            // grab hash algorithm as this may change in the future
            _hashAlgorithmName = _manager.HashAlgorithm;

            // keep the signer if indicated
            if (_manager.CertificateOption == CertificateEmbeddingOption.NotEmbedded)
                _lookForEmbeddedCert = false;       // don't bother parsing
            else
                _certificate = signer;              // save some parsing

            // we only release this key if we obtain it
            AsymmetricAlgorithm key = null;
            if (signer.HasPrivateKey)
            {
                key = GetPrivateKey(signer);
            }
            else
            {
                key = GetPrivateKeyForSigning(signer);
            }

            try
            {
                _signedXml = new CustomSignedXml();
                _signedXml.SigningKey = key;
                _signedXml.Signature.Id = signatureId;

                if (BaseCompatibilityPreferences.MatchPackageSignatureMethodToPackagePartDigestMethod)
                {
                    // Defaulting to SHA1 for key signing is counter-productive if the hash algorithm
                    // for the rest of the package is more secure.  So if we have a selected hash
                    // algorithm in the PackageDigitalSignatureManager that is more secure, then 
                    // select an equivalent level algorithm for key signing.
                    _signedXml.SignedInfo.SignatureMethod = SelectSignatureMethod(key);
                }

                // Track if we are matching the signature method in order to retry on failure
                bool usingMatchingSignatureMethod = _signedXml.SignedInfo.SignatureMethod != null;

                // put it in the XML
                if (embedCertificate)
                {
                    _signedXml.KeyInfo = GenerateKeyInfo(key, signer);
                }

                // Package object tag
                // convert from string to class and ensure we dispose
                using (HashAlgorithm hashAlgorithm = GetHashAlgorithm(_hashAlgorithmName))
                {
                    // inform caller if hash algorithm is unknown
                    if (hashAlgorithm == null)
                        throw new InvalidOperationException(SR.UnsupportedHashAlgorithm);

                    _signedXml.AddObject(GenerateObjectTag(hashAlgorithm, parts, relationshipSelectors, signatureId));
                }

                // add reference from SignedInfo to Package object tag
                Reference objectReference = new Reference(XTable.Get(XTable.ID.OpcLinkAttrValue));
                objectReference.Type = XTable.Get(XTable.ID.W3CSignatureNamespaceRoot) + "Object";
                objectReference.DigestMethod = _hashAlgorithmName;
                _signedXml.AddReference(objectReference);

                // add any custom object tags
                AddCustomObjectTags(signatureObjects, objectReferences);

                // compute the signature
                SignedXml xmlSig = _signedXml;

                try
                {
                    xmlSig.ComputeSignature();
                }
                catch (CryptographicException) when (usingMatchingSignatureMethod)
                {
                    // We've hit a state where System.Security is possibly missing the required updates to process the matched signature.
                    // Disable our matching and attempt to sign again with the default SignatureMethod
                    BaseCompatibilityPreferences.MatchPackageSignatureMethodToPackagePartDigestMethod = false;
                    xmlSig.SignedInfo.SignatureMethod = null;
                    xmlSig.ComputeSignature();
                }

                // persist
                UpdatePartFromSignature(_signedXml.Signature);
            }
            finally
            {
                if (key != null)
                    ((IDisposable)key).Dispose();
            }

            // create the PackageDigitalSignature object
            _signature = new PackageDigitalSignature(_manager, this);
            return _signature;
        }

        /// <summary>
        /// Attempt to select a key signing method that matches the strength of the selected
        /// hash algorithm in the PackageDigitalSignatureManager.
        /// 
        /// If no algorithm was selected, defaults to SHA1.
        /// </summary>
        /// <remarks>
        /// DSA keys have no supported stronger signing solution and are always SHA1 signed.
        /// </remarks>
        /// <param name="key">The key being used</param>
        /// <returns>The URI of the appropriate signing method</returns>
        private string SelectSignatureMethod(AsymmetricAlgorithm key)
        {
            string signatureMethod = null;

            if (key is RSA)
            {
                _rsaSigMethodLookup.TryGetValue(_manager.HashAlgorithm, out signatureMethod);
            }

            return signatureMethod;
        }

        /// <summary>
        /// Extracts the private key from the X509Certificate2 certificate
        /// </summary>
        /// <param name="cert">certificate for which we are looking for the private key</param>
        /// <returns>returns the private key</returns>
        private static AsymmetricAlgorithm GetPrivateKey(X509Certificate2 cert)
        {
            // Adding support for CNG certificates. The default certificate template in Windows Server 2008+ is CNG
            // Get[Algorithm]PrivateKey methods returns unique object while PrivateKey property returns shared one
            // Make sure to dispose the key if it is from Get[Algorithm]PrivateKey methods
            AsymmetricAlgorithm key = cert.GetRSAPrivateKey();
            if (key != null)
                return key;

            key = cert.GetDSAPrivateKey();
            if (key != null)
                return key;

            key = cert.GetECDsaPrivateKey();
            if (key != null)
                return key;

            // Get[Algorithm]PrivateKey methods would always have returned the private key if the PrivateKey property would
            // But Get[Algorithm]PrivateKey methods never throw but returns null in case of error during cryptographic operations
            // But we want exception to be thrown when an error occurs during a cryptographic operation so that we can revert the changes
            #pragma warning disable SYSLIB0028
            return cert.PrivateKey;
            #pragma warning restore SYSLIB0028
        }

        /// <summary>
        /// Assembles the sorted list of relationships for this part entry and
        /// generates a stream with the Xml-equivalent as defined in the Opc spec
        /// </summary>
        /// <param name="partEntry">relationship-type part entry</param>
        /// <returns></returns>
        private Stream GetRelationshipStream(PartManifestEntry partEntry)
        {
            Debug.Assert(partEntry.IsRelationshipEntry);

            //Get the list of relationships from the RelationshipSelectors for this part
            SortedDictionary<String, System.IO.Packaging.PackageRelationship> partRelationships =
                new SortedDictionary<String, System.IO.Packaging.PackageRelationship>(StringComparer.Ordinal);
            foreach (System.IO.Packaging.PackageRelationshipSelector relationshipSelector in partEntry.RelationshipSelectors)
            {
                foreach (System.IO.Packaging.PackageRelationship r in relationshipSelector.Select(_manager.Package))
                {
                    if(!partRelationships.ContainsKey(r.Id))
                        partRelationships.Add(r.Id, r);
                }
            }

            return GenerateRelationshipNodeStream(partRelationships.Values);
        }

        private void AddCustomObjectTags(IEnumerable<System.Security.Cryptography.Xml.DataObject> signatureObjects,
            IEnumerable<System.Security.Cryptography.Xml.Reference> objectReferences)
        {
            Invariant.Assert(_signedXml != null);


            // add any references
            if (objectReferences != null)
            {
                // Validate the Reference tags in the SignedInfo as per the 
                // restrictions imposed by the OPC spec
                ValidateReferences(objectReferences, false /*allowPackageSpecificReference*/);

                foreach (Reference reference in objectReferences)
                {
                    // consistent hash algorithm for entire signature
                    reference.DigestMethod = _hashAlgorithmName;
                    _signedXml.AddReference(reference);
                }
            }

            // any object tags
            if (signatureObjects != null)
            {
                // thes have been pre-screened for matches against reserved OpcAttrValue and duplicates
                foreach (DataObject obj in signatureObjects)
                {
                    _signedXml.AddObject(obj);
                }
            }
        }

        private void UpdatePartFromSignature(Signature sig)
        {
            // write to stream
            using (Stream s = SignaturePart.GetSeekableStream(FileMode.Create, FileAccess.Write))
            {
                using (XmlTextWriter xWriter = new XmlTextWriter(s, System.Text.Encoding.UTF8))
                {
                    xWriter.WriteStartDocument(true);
                    sig.GetXml().WriteTo(xWriter);
                    xWriter.WriteEndDocument();
                }
            }
            _signedXml = null;    // force a re-parse
        }

        private static byte[] HashStream(HashAlgorithm hashAlgorithm, Stream s)
        {
            s.Seek(0, SeekOrigin.Begin);

            // reset algorithm
            hashAlgorithm.Initialize();
            return hashAlgorithm.ComputeHash(s);
        }

        private static Stream TransformXml(Transform xForm, Object source)
        {
            // transform
            xForm.LoadInput(source);

            return (Stream)xForm.GetOutput();
        }

        /// <summary>
        /// Full parse of the Package-specific Object tag
        /// </summary>
        /// <remarks>Side effect of updating _signingTime, _signingTimeFormat, 
        /// _partManifest, _partEntryManifest and _relationshipManifest</remarks>
        /// <exception cref="XmlException">throws if markup does not match OPC spec</exception>
        private void ParsePackageDataObject()
        {
            if (!_dataObjectParsed)
            {
                EnsureXmlSignatureParsed();

                // find the package-specific Object tag
                XmlNodeList nodeList = GetPackageDataObject().Data;

                // The legal parent is a "Package" Object tag with 2 children
                // <Manifest> and <SignatureProperties>
                if (nodeList.Count != 2)
                    throw new XmlException(SR.XmlSignatureParseError);

                // get a NodeReader that allows us to easily and correctly skip comments
                XmlReader reader = new XmlNodeReader(nodeList[0].ParentNode);

                // parse the <Object> tag - ensure that it is in the correct namespace
                reader.Read();  // enter the Object tag
                if (String.CompareOrdinal(reader.NamespaceURI, SignedXml.XmlDsigNamespaceUrl) != 0)
                    throw new XmlException(SR.XmlSignatureParseError);

                string signaturePropertiesTagName = XTable.Get(XTable.ID.SignaturePropertiesTagName);
                string manifestTagName = XTable.Get(XTable.ID.ManifestTagName);
                bool signaturePropertiesTagFound = false;
                bool manifestTagFound = false;
                while (reader.Read() && (reader.NodeType == XmlNodeType.Element))
                {
                    if (reader.MoveToContent() == XmlNodeType.Element
                        && (String.CompareOrdinal(reader.NamespaceURI, SignedXml.XmlDsigNamespaceUrl) == 0)
                        && reader.Depth == 1)
                    {
                        if (!signaturePropertiesTagFound && String.CompareOrdinal(reader.LocalName, signaturePropertiesTagName) == 0)
                        {
                            signaturePropertiesTagFound = true;

                            // parse the <SignatureProperties> tag
                            _signingTime = XmlSignatureProperties.ParseSigningTime(
                                reader, _signedXml.Signature.Id, out _signingTimeFormat);

                            continue;
                        }
                        else if (!manifestTagFound && String.CompareOrdinal(reader.LocalName, manifestTagName) == 0)
                        {
                            manifestTagFound = true;

                            // parse the <Manifest> tag
                            XmlSignatureManifest.ParseManifest(_manager, reader,
                                out _partManifest, out _partEntryManifest, out _relationshipManifest);

                            continue;
                        }
                    }

                    throw new XmlException(SR.XmlSignatureParseError);
                }

                // these must both exist on exit
                if (!(signaturePropertiesTagFound && manifestTagFound))
                    throw new XmlException(SR.XmlSignatureParseError);

                _dataObjectParsed = true;
            }
        }

        /// <summary>
        /// Finds and return the package-specific Object tag
        /// </summary>
        /// <returns></returns>
        private DataObject GetPackageDataObject()
        {
            EnsureXmlSignatureParsed();

            // look for the Package-specific object tag
            String opcId = XTable.Get(XTable.ID.OpcAttrValue);
            DataObject returnValue = null;
            foreach (DataObject dataObject in _signedXml.Signature.ObjectList)
            {
                if (String.CompareOrdinal(dataObject.Id, opcId) == 0)
                {
                    // duplicates not allowed
                    if (returnValue != null)
                        throw new XmlException(SR.SignatureObjectIdMustBeUnique);

                    returnValue = dataObject;
                }
            }

            // Package object tag required
            if (returnValue != null)
                return returnValue;
            else
                throw new XmlException(SR.PackageSignatureObjectTagRequired);
        }

        private KeyInfo GenerateKeyInfo(AsymmetricAlgorithm key, X509Certificate2 signer)
        {
            // KeyInfo section
            KeyInfo keyInfo = new KeyInfo();
            KeyInfoName keyInfoName = new KeyInfoName();
            keyInfoName.Value = signer.Subject;
            keyInfo.AddClause(keyInfoName);               // human readable Principal name

            // Include the public key information (if we are familiar with the algorithm type)
            if (key is RSA)
                keyInfo.AddClause(new RSAKeyValue((RSA)key));    // RSA key parameters
            else
            {
                if (key is DSA)
                    keyInfo.AddClause(new DSAKeyValue((DSA)key));    // DSA
                else
                    throw new ArgumentException(SR.CertificateKeyTypeNotSupported, "signer");
            }

            // the actual X509 cert
            keyInfo.AddClause(new KeyInfoX509Data(signer));

            return keyInfo;
        }

        private DataObject GenerateObjectTag(
                HashAlgorithm hashAlgorithm,
                IEnumerable<Uri> parts, IEnumerable<System.IO.Packaging.PackageRelationshipSelector> relationshipSelectors,
                String signatureId)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.AppendChild(xDoc.CreateNode(XmlNodeType.Element, "root", "namespace")); // dummy root
            xDoc.DocumentElement.AppendChild(XmlSignatureManifest.GenerateManifest(_manager, xDoc, hashAlgorithm, parts, relationshipSelectors));
            xDoc.DocumentElement.AppendChild(XmlSignatureProperties.AssembleSignatureProperties(xDoc, DateTime.Now, _manager.TimeFormat, signatureId));

            DataObject dataObject = new DataObject();
            dataObject.Data = xDoc.DocumentElement.ChildNodes;
            dataObject.Id = XTable.Get(XTable.ID.OpcAttrValue);

            return dataObject;
        }

        /// <summary>
        /// lookup the private key using the given identity
        /// </summary>
        /// <param name="signer">X509Cert</param>
        /// <returns>IDisposable asymmetric algorithm that serves as a proxy to the private key.  Caller must dispose
        /// of properly.</returns>
        private static AsymmetricAlgorithm GetPrivateKeyForSigning(X509Certificate2 signer)
        {
            // if the certificate does not actually contain the key, we need to look it up via ThumbPrint
            Invariant.Assert(!signer.HasPrivateKey);

            // look for appropriate certificates
            X509Store store = new X509Store(StoreLocation.CurrentUser);

            try
            {
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

                X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;

                collection = collection.Find(X509FindType.FindByThumbprint, signer.Thumbprint, true);
                if (collection.Count > 0)
                {
                    if (collection.Count > 1)
                        throw new CryptographicException(SR.DigSigDuplicateCertificate);

                    signer = collection[0];
                }
                else
                    throw new CryptographicException(SR.DigSigCannotLocateCertificate);
            }
            finally
            {
                store.Close();
            }

            // get the corresponding AsymmetricAlgorithm
            return GetPrivateKey(signer);
        }


        /// <summary>
        /// This method validated the Reference tags as per the restrictions imposed
        /// by the OPC spec.
        /// NOTE: The same method is called from Verify and Sign methods. At verify time we need to make sure
        /// that there is exactly one Package-specific reference. At Sign time we need to make sure that
        /// there are no package-specific references in the list of references passed to Sign APIs as a 
        /// input parameter, since we will be generating Package-specific object.
        /// </summary>
        /// <param name="references">list of references to be validated</param>
        /// <param name="allowPackageSpecificReferences">When "true", we check to make sure that there is
        /// exactly one package-specific reference and when "false", we do not allow any package-specific
        /// references</param>
        private void ValidateReferences(IEnumerable references, bool allowPackageSpecificReferences)
        {
            Debug.Assert(references != null);

            bool packageReferenceFound = false;
            TransformChain currentTransformChain;

            foreach (Reference currentReference in references)
            {
                //As per the OPC spec, Uri attribute in Reference elements MUST refer using fragment identifiers
                //This implies that Uri cannot be absolute.
                if (currentReference.Uri.StartsWith("#", StringComparison.Ordinal))
                {
                    //As per the OPC spec, there MUST be exactly one package specific reference to the 
                    //package specific <Object> element 
                    if (String.CompareOrdinal(currentReference.Uri, XTable.Get(XTable.ID.OpcLinkAttrValue)) == 0)
                    {
                        if (!allowPackageSpecificReferences)
                            throw new ArgumentException(SR.PackageSpecificReferenceTagMustBeUnique);

                        //If there are more than one package specific tags
                        if (packageReferenceFound == true)
                            throw new XmlException(SR.MoreThanOnePackageSpecificReference);
                        else
                            packageReferenceFound = true;
                    }

                    currentTransformChain = currentReference.TransformChain;

                    for(int j=0; j<currentTransformChain.Count; j++)
                    {
                        //As per the OPC spec, only two transforms are supported for the reference tags
                        if (!IsValidXmlCanonicalizationTransform(currentTransformChain[j].Algorithm))
                            throw new XmlException(SR.UnsupportedTransformAlgorithm);
                    }
                }
                else
                    throw new XmlException(SR.InvalidUriAttribute);
            }

            // If there are zero reference tags or if there wasn't any package specific reference tag            
            if (allowPackageSpecificReferences && !packageReferenceFound)
                throw new XmlException(SR.PackageSignatureReferenceTagRequired);
        }


        //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------
        private PackagePart _signaturePart;
        private X509Certificate2                    _certificate;       // non-null if it's embedded
        private bool                                _lookForEmbeddedCert;
        private PackageDigitalSignatureManager      _manager;
        private PackageDigitalSignature             _signature;         // parsed from part or newly created
        private SignedXml                           _signedXml;         // our format friend
        private String                              _hashAlgorithmName;     // first hash algorithm obtained - considered to be the setting for the entire signature

        // OPC Object tag parsing - once parsed, all fields in this section are considered viable
        private bool                            _dataObjectParsed;          // true if package-specific data Object tag has been parsed
        private DateTime                        _signingTime;               // cached value
        private String                          _signingTimeFormat;         // format string
        private List<Uri>                       _partManifest;              // signed parts (suitable for return to public API)
        private List<PartManifestEntry>         _partEntryManifest;         // signed parts (with extra info)
        private List<System.IO.Packaging.PackageRelationshipSelector> _relationshipManifest;    // signed relationship selectors

        private static readonly ContentType _xmlSignaturePartType
            = new ContentType("application/vnd.openxmlformats-package.digital-signature-xmlsignature+xml");

        /// <summary>
        /// Mapping from hash algorithms in the PackageDigitalSignatureManager to RSA key signing methods.
        /// </summary>
        private static readonly Dictionary<string, string> _rsaSigMethodLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "http://www.w3.org/2001/04/xmlenc#sha256", "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256" },
            { "http://www.w3.org/2001/04/xmldsig-more#sha384", "http://www.w3.org/2001/04/xmldsig-more#rsa-sha384" },
            { "http://www.w3.org/2001/04/xmlenc#sha512", "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512" },
        };
    }
}

