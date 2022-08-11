// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  Signature processor implementation that follows the Feb 12, 2002 W3C DigSig Recommendation
//
//  Generates and consumes Manifest portion of the
//  XmlDSig-compliant digital signatures based on the subset
//  specified by the OPC file format.
//
// Manifest appears in this context:
//
//      <Object ID="Package"> 
//          <Manifest>...</Manifest>
//          <SignatureProperties>...</SignatureProperties>
//      </Object>
//
// Manifest form is:
//
//      <Manifest>
//
//          # simple reference - no transforms
//          <Reference URI="/page1.xml?ContentType=xml">
//              <DigestMethod Algorithm="sha1" />
//              <DigestValue>... </DigestValue>
//          </Reference>
//
//          # simple reference with c14n canonicalization transform
//          <Reference URI="/page2.xml?ContentType=xml">
//              <Transforms>
//                  <Transform Algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315" />
//              </Transforms>
//              <DigestMethod Algorithm="sha1" />
//              <DigestValue>... </DigestValue>
//          </Reference>
//
//          # reference that signs multiple PackageRelationships
//          <Reference URI="/shared/_rels/image.jpg.rels?ContentType=image/jpg">
//              <Transforms>
//                  <Transform Algorithm="http://schemas.openxmlformats.org/package/2006/RelationshipTransform">
//                      <RelationshipReference SourceId="1" />
//                      <RelationshipReference SourceId="2" />
//                      <RelationshipReference SourceId="8" />
//                  </Transform>
//                  <Transform Algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315" />
//              </Transforms>
//              <DigestMethod Algorithm="sha1" />
//              <DigestValue>... </DigestValue>
//          </Reference>
//
//          # reference that signs PackageRelationships by Relationship Type and a single Relationship by ID
//          <Reference URI="/shared/_rels/image.jpg.rels?ContentType=image/jpg">
//              <Transforms>
//                  <Transform Algorithm="http://schemas.openxmlformats.org/package/2006/RelationshipTransform">
//                      <RelationshipGroupReference SourceType="http://schemas.openxmlformats.org/package/2006/relationships/digital-signature/certificate" />
//                      <RelationshipReference SourceId="abc123" />
//                  </Transform>
//                  <Transform Algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315" />
//              </Transforms>
//              <DigestMethod Algorithm="sha1" />
//              <DigestValue>... </DigestValue>
//          </Reference>
//          ...
//
//      </Manifest>

//
//
//

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
//using System.Security;                      // for SecurityCritical and SecurityTreatAsSafe
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.IO;
using System.Windows;
using System.IO.Packaging;
using MS.Internal;
using MS.Internal.WindowsBase;

using PackageRelationship = MS.Internal.IO.Packaging.Extensions.PackageRelationship;

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// Manifest generator/parser
    /// </summary>
    /// <remarks>See: http://www.w3.org/TR/2002/REC-xmldsig-core-20020212/ for details</remarks>
    internal static class XmlSignatureManifest
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Parse the Manifest tag
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="reader">XmlReader positioned to the Manifest tag</param>
        /// <param name="partManifest"></param>
        /// <param name="partEntryManifest"></param>
        /// <param name="relationshipManifest"></param>
        internal static void ParseManifest(
            PackageDigitalSignatureManager manager,
            XmlReader reader,
            out List<Uri> partManifest,
            out List<PartManifestEntry> partEntryManifest,
            out List<PackageRelationshipSelector> relationshipManifest)
        {
            Invariant.Assert(manager != null);
            Invariant.Assert(reader != null);

            // these are empty (non-null) when nothing is found
            partManifest = new List<Uri>();
            partEntryManifest = new List<PartManifestEntry>();
            relationshipManifest = new List<PackageRelationshipSelector>();

            // manually parse the Relationship tags because they are custom formed and the Reference class will not handle
            // them correctly
            string referenceTagName = XTable.Get(XTable.ID.ReferenceTagName);
            int referenceCount = 0;
            while (reader.Read() && (reader.MoveToContent() == XmlNodeType.Element))
            {
                // should be on a <Reference> tag
                if (String.CompareOrdinal(reader.NamespaceURI, SignedXml.XmlDsigNamespaceUrl) == 0
                    && (String.CompareOrdinal(reader.LocalName, referenceTagName) == 0)
                    && reader.Depth == 2)
                {
                    // Parse each reference - distinguish between Relationships and Parts
                    // because we don't store the Relationship-part itself - just it's Relationships.
                    PartManifestEntry partManifestEntry = ParseReference(reader);
                    if (partManifestEntry.IsRelationshipEntry)
                    {
                        foreach (PackageRelationshipSelector relationshipSelector in partManifestEntry.RelationshipSelectors)
                            relationshipManifest.Add(relationshipSelector);
                    }
                    else
                        partManifest.Add(partManifestEntry.Uri);

                    // return the manifest entry to be used for hashing
                    partEntryManifest.Add(partManifestEntry);

                    referenceCount++;
                }
                else
                    throw new XmlException(SR.Format(SR.UnexpectedXmlTag, reader.Name));
            }

            // XmlDSig xsd requires at least one <Reference> tag
            if (referenceCount == 0)
                throw new XmlException(SR.PackageSignatureCorruption);
        }

        /// <summary>
        /// Parse the DigestMethod tag
        /// </summary>
        /// <param name="reader"></param>
        private static string ParseDigestAlgorithmTag(XmlReader reader)
        {
            // verify namespace and lack of attributes
            if (PackagingUtilities.GetNonXmlnsAttributeCount(reader) > 1
                || String.CompareOrdinal(reader.NamespaceURI, SignedXml.XmlDsigNamespaceUrl) != 0
                || reader.Depth != 3)
                throw new XmlException(SR.XmlSignatureParseError);

            // get the Algorithm attribute
            string hashAlgorithm = null;
            if (reader.HasAttributes)
            {
                hashAlgorithm = reader.GetAttribute(XTable.Get(XTable.ID.AlgorithmAttrName));
            }

            if (hashAlgorithm == null || hashAlgorithm.Length == 0)
                throw new XmlException(SR.UnsupportedHashAlgorithm);

            return hashAlgorithm;
        }

        /// <summary>
        /// Parse the DigestValue tag
        /// </summary>
        /// <param name="reader"></param>
        private static string ParseDigestValueTag(XmlReader reader)
        {
            Debug.Assert(reader != null);

            // verify namespace and lack of attributes
            if (PackagingUtilities.GetNonXmlnsAttributeCount(reader) > 0
                || String.CompareOrdinal(reader.NamespaceURI, SignedXml.XmlDsigNamespaceUrl) != 0
                || reader.Depth != 3)
                throw new XmlException(SR.XmlSignatureParseError);

            // there are no legal attributes and the only content must be text
            if (reader.HasAttributes || (reader.Read() && reader.MoveToContent() != XmlNodeType.Text))
                throw new XmlException(SR.XmlSignatureParseError);

            // get the Value
            return reader.ReadString();
        }

        /// <summary>
        /// Get the part uri and it's content type from the current Reference tag
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="contentType">contentType extracted from the query portion of the Uri</param>
        /// <returns>PackagePart uri and contentType</returns>
        private static Uri ParsePartUri(XmlReader reader, out ContentType contentType)
        {
            // should be a relative Package uri with a query portion that contains the ContentType
            contentType = ContentType.Empty;
            Uri partUri = null;

            // must be one and only one attribute
            if (PackagingUtilities.GetNonXmlnsAttributeCount(reader) == 1)
            {
                string uriAttrValue = reader.GetAttribute(XTable.Get(XTable.ID.UriAttrName));
                if (uriAttrValue != null)
                {
                    partUri = ParsePartUriAttribute(uriAttrValue, out contentType);
                }
            }

            // will be null if we had no success
            if (partUri == null)
                throw new XmlException(SR.Format(SR.RequiredXmlAttributeMissing, XTable.Get(XTable.ID.UriAttrName)));

            return partUri;
        }

        /// <summary>
        /// Parses a Reference tag
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>partManifestEntry that represents the state of the tag</returns>
        private static PartManifestEntry ParseReference(XmlReader reader)
        {
            Debug.Assert(reader != null);

            // <Reference> found - get part Uri from the tag
            ContentType contentType = null;
            Uri partUri = ParsePartUri(reader, out contentType);

            // only allocate if this turns out to be a Relationship transform
            List<PackageRelationshipSelector> relationshipSelectors = null;

            // move through the sub-tags: <DigestMethod>, <DigestValue> and optional <Transforms>
            string hashAlgorithm = null;    // digest method
            string hashValue = null;        // digest value
            List<String> transforms = null; // optional transform algorithm names
            bool transformsParsed = false;  // since null is legal for transforms var we need a 
            // bool to detect multiples
            while (reader.Read() && (reader.MoveToContent() == XmlNodeType.Element))
            {
                // Correct Namespace?
                if (String.CompareOrdinal(reader.NamespaceURI, SignedXml.XmlDsigNamespaceUrl) != 0
                    || reader.Depth != 3)
                {
                    throw new XmlException(SR.PackageSignatureCorruption);
                }

                // DigestMethod?
                if (hashAlgorithm == null &&
                    String.CompareOrdinal(reader.LocalName, XTable.Get(XTable.ID.DigestMethodTagName)) == 0)
                {
                    hashAlgorithm = ParseDigestAlgorithmTag(reader);
                    continue;
                }

                // DigestValue?
                if (hashValue == null &&
                    String.CompareOrdinal(reader.LocalName, XTable.Get(XTable.ID.DigestValueTagName)) == 0)
                {
                    hashValue = ParseDigestValueTag(reader);
                    continue;
                }

                // TransformsTag?
                if (!transformsParsed &&
                    String.CompareOrdinal(reader.LocalName, XTable.Get(XTable.ID.TransformsTagName)) == 0)
                {
                    transforms = ParseTransformsTag(reader, partUri, ref relationshipSelectors);
                    transformsParsed = true;
                    continue;
                }

                // if we get to here, we didn't see what we expected
                throw new XmlException(SR.PackageSignatureCorruption);
            }

            // add to our list
            return new PartManifestEntry(partUri, contentType, hashAlgorithm, hashValue, transforms, relationshipSelectors);
        }

        /// <summary>
        /// Parses Transforms tag
        /// </summary>
        /// <param name="reader">node to parse</param>
        /// <param name="partUri">Part Uri for the part owning the relationships</param>
        /// <param name="relationshipSelectors">allocates and returns a list of 
        /// PackageRelationshipSelectors if Relationship transform</param>
        /// <returns>ordered list of Transform names</returns>
        private static List<String> ParseTransformsTag(XmlReader reader, Uri partUri, ref List<PackageRelationshipSelector> relationshipSelectors)
        {
            // # reference that signs multiple PackageRelationships
            // <Reference URI="/shared/_rels/image.jpg.rels?ContentType=image/jpg">
            //      <Transforms>
            //          <Transform Algorithm="http://schemas.openxmlformats.org/package/2006/RelationshipTransform">
            //              <RelationshipReference SourceId="1" />
            //              <RelationshipReference SourceId="2" />
            //              <RelationshipReference SourceId="8" />
            //          </Transform>
            //          <Transform Algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315" />
            //      </Transforms>
            //      <DigestMethod Algorithm="sha1" />
            //      <DigestValue>... </DigestValue>
            // </Reference>

            // verify lack of attributes
            if (PackagingUtilities.GetNonXmlnsAttributeCount(reader) != 0)
                throw new XmlException(SR.XmlSignatureParseError);

            List<String> transforms = null;
            bool relationshipTransformFound = false;
            int transformsCountWhenRelationshipTransformFound = 0;
                        
            // Look for transforms.
            // There are currently only 3 legal transforms which can be arranged in any
            // combination.
            while (reader.Read() && (reader.MoveToContent() == XmlNodeType.Element))
            {
                String transformName = null;               

                // at this level, all tags must be Transform tags
                if (reader.Depth != 4
                    || String.CompareOrdinal(reader.NamespaceURI, SignedXml.XmlDsigNamespaceUrl) != 0
                    || String.CompareOrdinal(reader.LocalName, XTable.Get(XTable.ID.TransformTagName)) != 0)
                {
                    throw new XmlException(SR.XmlSignatureParseError);
                }

                // inspect the Algorithm attribute to determine the type of transform
                if (PackagingUtilities.GetNonXmlnsAttributeCount(reader) == 1)
                {
                    transformName = reader.GetAttribute(XTable.Get(XTable.ID.AlgorithmAttrName));
                }

                // legal transform name?
                if ((transformName != null) && (transformName.Length > 0))
                {
                    // what type of transform?
                    if (String.CompareOrdinal(transformName, XTable.Get(XTable.ID.RelationshipsTransformName)) == 0)
                    {
                        if (!relationshipTransformFound)
                        {
                            // relationship transform
                            ParseRelationshipsTransform(reader, partUri, ref relationshipSelectors);

                            if (transforms == null)
                                transforms = new List<String>();

                            transforms.Add(transformName);

                            relationshipTransformFound = true;
                            transformsCountWhenRelationshipTransformFound = transforms.Count;
                            continue;   // success
                        }
                        else
                            throw new XmlException(SR.MultipleRelationshipTransformsFound);
                    }                    
                    else
                    {
                        // non-Relationship transform should have no children
                        if (reader.IsEmptyElement)
                        {
                            if (transforms == null)
                                transforms = new List<String>();

                            if (XmlDigitalSignatureProcessor.IsValidXmlCanonicalizationTransform(transformName))
                            {
                                transforms.Add(transformName);  // return it
                                continue;   // success
                            }
                            else
                                throw new InvalidOperationException(SR.UnsupportedTransformAlgorithm);
                        }
                    }
                }
                throw new XmlException(SR.XmlSignatureParseError);
            }

            if (transforms.Count == 0)
                throw new XmlException(SR.XmlSignatureParseError);
            
            //If we found another transform after the Relationship transform, it will be validated earlier
            //in this method to make sure that its a supported xml canonicalization algorithm and so we can 
            //simplify this test condition - As per the OPC spec - Relationship transform must be followed
            //by a canonicalization algorithm.
            if (relationshipTransformFound && (transforms.Count == transformsCountWhenRelationshipTransformFound))
                throw new XmlException(SR.RelationshipTransformNotFollowedByCanonicalizationTransform);

            return transforms;
        }

        /// <summary>
        /// Parse the Relationship-specific Transform
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="partUri"></param>
        /// <param name="relationshipSelectors">may be allocated but will never be empty</param>
        private static void ParseRelationshipsTransform(XmlReader reader, Uri partUri, ref List<PackageRelationshipSelector> relationshipSelectors)
        {
            Uri owningPartUri = System.IO.Packaging.PackUriHelper.GetSourcePartUriFromRelationshipPartUri(partUri);

            // find all of the Relationship tags of form:
            //      <RelationshipReference SourceId="abc123" />
            // or 
            //      <RelationshipsGroupReference SourceType="reference-type-of-the-week" />
            while (reader.Read() && (reader.MoveToContent() == XmlNodeType.Element)
                && reader.Depth == 5)
            {
                // both types have no children, a single required attribute and belong to the OPC namespace
                if (reader.IsEmptyElement
                    && PackagingUtilities.GetNonXmlnsAttributeCount(reader) == 1
                    && (String.CompareOrdinal(reader.NamespaceURI, XTable.Get(XTable.ID.OpcSignatureNamespace)) == 0))
                {
                    // <RelationshipReference>?
                    if (String.CompareOrdinal(reader.LocalName, XTable.Get(XTable.ID.RelationshipReferenceTagName)) == 0)
                    {
                        // RelationshipReference tags are legal and these must be empty with a single SourceId attribute
                        // get the SourceId attribute 
                        string id = reader.GetAttribute(XTable.Get(XTable.ID.SourceIdAttrName));
                        if (id != null && id.Length > 0)
                        {
                            if (relationshipSelectors == null)
                                relationshipSelectors = new List<PackageRelationshipSelector>();

                            // we found a legal SourceId so create a selector and continue searching
                            relationshipSelectors.Add(new PackageRelationshipSelector(owningPartUri, PackageRelationshipSelectorType.Id, id));
                            continue;
                        }
                    }   // <RelationshipsGroupReference>?
                    else if ((String.CompareOrdinal(reader.LocalName, XTable.Get(XTable.ID.RelationshipsGroupReferenceTagName)) == 0))
                    {
                        // RelationshipsGroupReference tags must be empty with a single SourceType attribute
                        string type = reader.GetAttribute(XTable.Get(XTable.ID.SourceTypeAttrName));
                        if (type != null && type.Length > 0)
                        {
                            // lazy init
                            if (relationshipSelectors == null)
                                relationshipSelectors = new List<PackageRelationshipSelector>();

                            // got a legal SourceType attribute
                            relationshipSelectors.Add(new PackageRelationshipSelector(owningPartUri, PackageRelationshipSelectorType.Type, type));
                            continue;
                        }
                    }
                }

                // if we get to here, we have not found a legal tag so we throw
                throw new XmlException(SR.Format(SR.UnexpectedXmlTag, reader.LocalName));
            }
        }

        /// <summary>
        /// Generate Manifest tag
        /// </summary>
        /// <param name="manager">manager</param>
        /// <param name="xDoc">current Xml doc</param>
        /// <param name="hashAlgorithm">hash algorithm to hash with</param>
        /// <param name="parts">parts to sign - possibly null</param>
        /// <param name="relationshipSelectors">relationshipSelectors that represent the
        /// relationships that have to be signed - possibly null</param>
        /// <returns></returns>
        internal static XmlNode GenerateManifest(
            PackageDigitalSignatureManager manager,
            XmlDocument xDoc,
            HashAlgorithm hashAlgorithm,
            IEnumerable<Uri> parts,
            IEnumerable<PackageRelationshipSelector> relationshipSelectors)
        {
            Debug.Assert(manager != null);
            Debug.Assert(xDoc != null);
            Debug.Assert(hashAlgorithm != null);

            // check args
            if (!hashAlgorithm.CanReuseTransform)
                throw new ArgumentException(SR.HashAlgorithmMustBeReusable);

            // <Manifest>
            XmlNode manifest = xDoc.CreateNode(XmlNodeType.Element,
                XTable.Get(XTable.ID.ManifestTagName),
                SignedXml.XmlDsigNamespaceUrl);

            // add part references
            if (parts != null)
            {
                // loop and write - may still be empty
                foreach (Uri partUri in parts)
                {
                    // generate a reference tag
                    manifest.AppendChild(GeneratePartSigningReference(manager, xDoc, hashAlgorithm, partUri));
                }
            }

            // any relationship references?
            int relationshipCount = 0;
            if (relationshipSelectors != null)
            {
                relationshipCount = GenerateRelationshipSigningReferences(manager, xDoc, hashAlgorithm, relationshipSelectors, manifest);
            }

            // did we sign anything? Manifest can NOT be empty
            if (parts == null && relationshipCount == 0)
                throw new ArgumentException(SR.NothingToSign);

            return manifest;
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// GenerateRelationshipSigningReferences
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="xDoc"></param>
        /// <param name="hashAlgorithm"></param>
        /// <param name="relationshipSelectors"></param>
        /// <param name="manifest"></param>
        /// <returns>number of references to be signed</returns>
        private static int GenerateRelationshipSigningReferences(
            PackageDigitalSignatureManager manager,
            XmlDocument xDoc, HashAlgorithm hashAlgorithm,
            IEnumerable<PackageRelationshipSelector> relationshipSelectors,
            XmlNode manifest)
        {
            // PartUri - and its list of PackageRelationshipSelectors
            Dictionary<Uri, List<PackageRelationshipSelector>> partAndSelectorDictionary 
                = new Dictionary<Uri, List<PackageRelationshipSelector>>();

            foreach (PackageRelationshipSelector relationshipSelector in relationshipSelectors)
            {
                //update the partAndSelectorDictionary for each relationshipSelector
                Uri relationshipPartUri = System.IO.Packaging.PackUriHelper.GetRelationshipPartUri(relationshipSelector.SourceUri);                

                List<PackageRelationshipSelector> selectors;
                if (partAndSelectorDictionary.ContainsKey(relationshipPartUri))
                    selectors = partAndSelectorDictionary[relationshipPartUri];
                else
                {
                    selectors = new List<PackageRelationshipSelector>();
                    partAndSelectorDictionary.Add(relationshipPartUri, selectors);
                }

                selectors.Add(relationshipSelector);                                  
            }
        
            // now that we have them grouped by Part name, emit the XML
            // Here is an optimization for saving space by declaring the OPC namespace and prefix
            // in the <Manifest> tag. It will become: 
            // <Manifest xmlns:opc="http://schemas.openxmlformats.org/package/2006/digital-signature">
            // Later when we generate the RelationshipSigningReference we can use the namespace prefix "opc"
            // instead of the long namespace itself, thus saving some space if the manifest has more than one
            // RelationshipSigningReference.
            // 
            XmlElement xmlE = (XmlElement)manifest;
            xmlE.SetAttribute(XTable.Get(XTable.ID.OpcSignatureNamespaceAttribute), 
                                XTable.Get(XTable.ID.OpcSignatureNamespace));

            int count = 0;
            foreach (Uri partName in partAndSelectorDictionary.Keys)
            {
                // emit xml and append
                manifest.AppendChild(
                    GenerateRelationshipSigningReference(manager, xDoc, hashAlgorithm, 
                    partName, /* we are guaranteed that this is a valid part Uri, so we do not use PackUriHelper.CreatePartUri */
                    partAndSelectorDictionary[partName]));

                count++;
            }

            return count;
        }

        private static Uri ParsePartUriAttribute(String attrValue, out ContentType contentType)
        {
            // extract the query portion - do not ask the Uri class for it because it will escape
            // characters and we want to do a simple text comparison later
            contentType = ContentType.Empty;             // out argument must always be set
            int index = attrValue.IndexOf('?');
            Uri uri = null;
            if (index > 0)
            {
                try
                {
                    // ensure it starts with the correct query prefix
                    String query = attrValue.Substring(index);
                    if ((query.Length > _contentTypeQueryStringPrefix.Length) && (query.StartsWith(_contentTypeQueryStringPrefix, StringComparison.Ordinal)))
                    {
                        // truncate the prefix and validate
                        contentType = new ContentType(query.Substring(_contentTypeQueryStringPrefix.Length));
}

                    // now construct the uri without the query
                    uri = PackUriHelper.ValidatePartUri(new Uri(attrValue.Substring(0, index), UriKind.Relative));
                }
                catch (ArgumentException ae)
                {
                    // Content type or part uri is malformed so we have a bad signature.
                    // Rethrow as XmlException so outer validation loop can catch it and return validation result.
                    throw new XmlException(SR.PartReferenceUriMalformed, ae);
                }
            }

            // throw if we failed
            if (contentType.ToString().Length <= 0)
                throw new XmlException(SR.PartReferenceUriMalformed);

            return uri;
        }

        /// <summary>
        /// Generates a Reference tag that contains a Relationship transform
        /// </summary>
        /// <param name="manager">manager</param>
        /// <param name="relationshipPartName">name of the relationship part</param>
        /// <param name="xDoc">current xml document</param>
        /// <param name="hashAlgorithm">hash algorithm = digest method</param>
        /// <param name="relationshipSelectors">relationshipSelectors that represent the relationships to sign </param>
        /// <remarks>ContentType is known and part name can be derived from the relationship collection</remarks>
        private static XmlNode GenerateRelationshipSigningReference(
            PackageDigitalSignatureManager manager,
            XmlDocument xDoc,
            HashAlgorithm hashAlgorithm,
            Uri relationshipPartName,
            IEnumerable<PackageRelationshipSelector> relationshipSelectors)
        {
            string relPartContentType = PackagingUtilities.RelationshipPartContentType.ToString();

            // <Reference>
            XmlElement reference = xDoc.CreateElement(XTable.Get(XTable.ID.ReferenceTagName), SignedXml.XmlDsigNamespaceUrl);

            // add Uri
            // persist the Uri of the associated Relationship part
            String relationshipPartString;
            if (System.IO.Packaging.PackUriHelper.ComparePartUri(relationshipPartName, PackageRelationship.ContainerRelationshipPartName) == 0)
                relationshipPartString = PackageRelationship.ContainerRelationshipPartName.ToString();
            else
                relationshipPartString = PackUriHelper.GetStringForPartUri(relationshipPartName);

            XmlAttribute uriAttr = xDoc.CreateAttribute(XTable.Get(XTable.ID.UriAttrName));
            uriAttr.Value = relationshipPartString + _contentTypeQueryStringPrefix + relPartContentType;
            reference.Attributes.Append(uriAttr);

            // add transforms tag (always necessary)

            // <Transforms>
            XmlElement transforms = xDoc.CreateElement(XTable.Get(XTable.ID.TransformsTagName), SignedXml.XmlDsigNamespaceUrl);

            // add Relationship transform
            String opcNamespace = XTable.Get(XTable.ID.OpcSignatureNamespace);
            String opcNamespacePrefix = XTable.Get(XTable.ID.OpcSignatureNamespacePrefix);

            XmlElement transform = xDoc.CreateElement(XTable.Get(XTable.ID.TransformTagName), SignedXml.XmlDsigNamespaceUrl);
            XmlAttribute algorithmAttr = xDoc.CreateAttribute(XTable.Get(XTable.ID.AlgorithmAttrName));
            algorithmAttr.Value = XTable.Get(XTable.ID.RelationshipsTransformName);
            transform.Attributes.Append(algorithmAttr);

            // <RelationshipReference SourceId="abc" /> or
            // <RelationshipGroupReference SourceType="xyz" />
            foreach (PackageRelationshipSelector relationshipSelector in relationshipSelectors)
            {
                switch (relationshipSelector.SelectorType)
                {
                    case PackageRelationshipSelectorType.Id:
                        {
                            XmlNode relationshipNode = xDoc.CreateElement(opcNamespacePrefix, XTable.Get(XTable.ID.RelationshipReferenceTagName), opcNamespace);
                            XmlAttribute idAttr = xDoc.CreateAttribute(XTable.Get(XTable.ID.SourceIdAttrName));
                            idAttr.Value = relationshipSelector.SelectionCriteria;
                            relationshipNode.Attributes.Append(idAttr);
                            transform.AppendChild(relationshipNode);
                        }
                        break;
                    case PackageRelationshipSelectorType.Type:
                        {
                            XmlNode relationshipNode = xDoc.CreateElement(opcNamespacePrefix, XTable.Get(XTable.ID.RelationshipsGroupReferenceTagName), opcNamespace);
                            XmlAttribute typeAttr = xDoc.CreateAttribute(XTable.Get(XTable.ID.SourceTypeAttrName));
                            typeAttr.Value = relationshipSelector.SelectionCriteria;
                            relationshipNode.Attributes.Append(typeAttr);
                            transform.AppendChild(relationshipNode);
                        }
                        break;
                    default:
                        Invariant.Assert(false, "This option should never be executed");
                        break;
                }
            }

            transforms.AppendChild(transform);

            // add non-Relationship transform (if any)
            String transformName = null;
            if (manager.TransformMapping.ContainsKey(relPartContentType))
            {
                transformName = manager.TransformMapping[relPartContentType];       // let them override

                //Currently we only support two transforms and so we validate whether its
                //one of those
                if (transformName == null || 
                    transformName.Length == 0 ||
                    !XmlDigitalSignatureProcessor.IsValidXmlCanonicalizationTransform(transformName))
                    throw new InvalidOperationException(SR.UnsupportedTransformAlgorithm);

                // <Transform>
                transform = xDoc.CreateElement(XTable.Get(XTable.ID.TransformTagName), SignedXml.XmlDsigNamespaceUrl);
                algorithmAttr = xDoc.CreateAttribute(XTable.Get(XTable.ID.AlgorithmAttrName));
                algorithmAttr.Value = transformName;
                transform.Attributes.Append(algorithmAttr);

                transforms.AppendChild(transform);
            }
            reference.AppendChild(transforms);

            // <DigestMethod>
            reference.AppendChild(GenerateDigestMethod(manager, xDoc));

            // <DigestValue> - digest the virtual node list made from these Relationship tags
            using (Stream s = XmlDigitalSignatureProcessor.GenerateRelationshipNodeStream(GetRelationships(manager, relationshipSelectors)))    // serialized node list
            {
                reference.AppendChild(GenerateDigestValueNode(xDoc, hashAlgorithm, s, transformName));
            }

            return reference;
        }

        private static XmlNode GeneratePartSigningReference(
            PackageDigitalSignatureManager manager,
            XmlDocument xDoc,
            HashAlgorithm hashAlgorithm,
            Uri partName)
        {
            PackagePart part = manager.Package.GetPart(partName);

            // <Reference>
            XmlElement reference = xDoc.CreateElement(XTable.Get(XTable.ID.ReferenceTagName), SignedXml.XmlDsigNamespaceUrl);

            // add Uri with content type as Query
            XmlAttribute uriAttr = xDoc.CreateAttribute(XTable.Get(XTable.ID.UriAttrName));
            uriAttr.Value = PackUriHelper.GetStringForPartUri(partName) + _contentTypeQueryStringPrefix + part.ContentType;
            reference.Attributes.Append(uriAttr);

            // add transforms tag if necessary
            String transformName = String.Empty;
            if (manager.TransformMapping.ContainsKey(part.ContentType))
            {
                transformName = manager.TransformMapping[part.ContentType];

                // <Transforms>
                XmlElement transforms = xDoc.CreateElement(XTable.Get(XTable.ID.TransformsTagName), SignedXml.XmlDsigNamespaceUrl);

                // <Transform>
                XmlElement transform = xDoc.CreateElement(XTable.Get(XTable.ID.TransformTagName), SignedXml.XmlDsigNamespaceUrl);
                XmlAttribute algorithmAttr = xDoc.CreateAttribute(XTable.Get(XTable.ID.AlgorithmAttrName));
                algorithmAttr.Value = transformName;
                transform.Attributes.Append(algorithmAttr);

                transforms.AppendChild(transform);
                reference.AppendChild(transforms);
            }

            // <DigestMethod>
            reference.AppendChild(GenerateDigestMethod(manager, xDoc));

            // <DigestValue>
            using (Stream s = part.GetSeekableStream(FileMode.Open, FileAccess.Read))
            {
                reference.AppendChild(GenerateDigestValueNode(xDoc, hashAlgorithm, s, transformName));
            }

            return reference;
        }

        private static XmlNode GenerateDigestMethod(
            PackageDigitalSignatureManager manager,
            XmlDocument xDoc)
        {
            // <DigestMethod>
            XmlElement digestMethod = xDoc.CreateElement(XTable.Get(XTable.ID.DigestMethodTagName), SignedXml.XmlDsigNamespaceUrl);
            XmlAttribute digestAlgorithmAttr = xDoc.CreateAttribute(XTable.Get(XTable.ID.AlgorithmAttrName));
            digestAlgorithmAttr.Value = manager.HashAlgorithm;
            digestMethod.Attributes.Append(digestAlgorithmAttr);
            return digestMethod;
        }

        private static XmlNode GenerateDigestValueNode(XmlDocument xDoc, HashAlgorithm hashAlgorithm, Stream s, String transformName)
        {
            // <DigestValue>
            XmlElement digestValue = xDoc.CreateElement(XTable.Get(XTable.ID.DigestValueTagName), SignedXml.XmlDsigNamespaceUrl);
            XmlText digestValueText = xDoc.CreateTextNode(XmlDigitalSignatureProcessor.GenerateDigestValue(s, transformName, hashAlgorithm));
            digestValue.AppendChild(digestValueText);
            return digestValue;
        }


        //Returns the sorted PackageRelationship collection from a given collection of PackageRelationshipSelectors
        //Note: All the selectors in the given selector collection are assumed to be for the same Part/PackageRoot
        //This method should be called for a part/packageroot
        private static IEnumerable<System.IO.Packaging.PackageRelationship> GetRelationships(
            PackageDigitalSignatureManager manager,
            IEnumerable<PackageRelationshipSelector> relationshipSelectorsWithSameSource)
        {
            SortedDictionary<String, System.IO.Packaging.PackageRelationship>
                relationshipsDictionarySortedById = new SortedDictionary<String, System.IO.Packaging.PackageRelationship>(StringComparer.Ordinal);

            foreach (PackageRelationshipSelector relationshipSelector in relationshipSelectorsWithSameSource)
            {
                // loop and accumulate and group them by owning Part
                foreach (System.IO.Packaging.PackageRelationship r in relationshipSelector.Select(manager.Package))
                {
                    // add relationship
                    if(!relationshipsDictionarySortedById.ContainsKey(r.Id))
                        relationshipsDictionarySortedById.Add(r.Id, r);
                }
            }
            return relationshipsDictionarySortedById.Values;
        }

        //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------
        const string _contentTypeQueryStringPrefix = "?ContentType=";
    }
}
