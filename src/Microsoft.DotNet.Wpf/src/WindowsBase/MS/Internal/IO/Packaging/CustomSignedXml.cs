// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  Wrapper class for existing SignedXml class that works around


using System;
using System.Xml;
using System.Windows;                          // for SR
using System.Security.Cryptography.Xml;
using MS.Internal.WindowsBase;
using Microsoft.Win32;                          // for Registry and RegistryKey classes
using System.Globalization;                     // for CultureInfo

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// SignedXml wrapper that supports reference targeting of internal ID's
    /// </summary>
    /// <remarks>See: http://www.w3.org/TR/2002/REC-xmldsig-core-20020212/ for details</remarks>
    internal class CustomSignedXml : SignedXml
    {
        /// <summary>
        /// Returns the XmlElement that matches the given id
        /// </summary>
        /// <param name="document"></param>
        /// <param name="idValue"></param>
        /// <returns>element if found, otherwise return null</returns>
        public override XmlElement GetIdElement(XmlDocument document, string idValue)
        {
            // Always let the base class have a first try at finding the element
            XmlElement elem = base.GetIdElement(document, idValue);

            // If not found then we will try to find it ourselves
            if (elem == null)
            {
                // Require the id to be an NCName (to avoid side-effects when it's
                // used in an XPath query).  The base class does this check, but
                // doesn't expose the answer.  Hence, we have to do it again.
                // [Code copied from SignedXml.DefaultGetIdElement, in
                // NDP/clr/src/ManagedLibraries/Security/System/Security/Cryptography/Xml/SignedXml.cs]
                if (RequireNCNameIdentifier())
                {
                    try
                    {
                        XmlConvert.VerifyNCName(idValue);
                    }
                    catch (XmlException)
                    {
                        // Identifiers are required to be an NCName
                        //   (xml:id version 1.0, part 4, paragraph 2, bullet 1)
                        //
                        // If it isn't an NCName, it isn't allowed to match.
                        return null;
                    }
                }

                elem = SelectNodeByIdFromObjects(m_signature, idValue);
            }

            return elem;
        }

        /// <summary>
        /// Locate and return the node identified by idValue
        /// </summary>
        /// <param name="signature"></param>
        /// <param name="idValue"></param>
        /// <returns>node if found - else null</returns>
        /// <remarks>Tries to match each object in the Object list.</remarks>
        private static XmlElement SelectNodeByIdFromObjects(Signature signature, string idValue)
        {
            XmlElement node = null;

            // enumerate the objects
            foreach (DataObject dataObject in signature.ObjectList)
            {
                // direct reference to Object id - supported for all reference typs
                if (String.CompareOrdinal(idValue, dataObject.Id) == 0)
                {
                    // anticipate duplicate ID's and throw if any found
                    if (node != null)
                        throw new XmlException(SR.Get(SRID.DuplicateObjectId));

                    node = dataObject.GetXml();
                }
            }

            // now search for XAdES specific references
            if (node == null)
            {
                // For XAdES we implement special case where the reference may
                // be to an internal tag with matching "Id" attribute.
                node = SelectSubObjectNodeForXAdES(signature, idValue);
            }

            return node;
        }

        /// <summary>
        /// Locate any signed Object tag that matches the XAdES "target type"
        /// </summary>
        /// <param name="signature"></param>
        /// <param name="idValue"></param>
        /// <returns>element if found; null if not found</returns>
        /// <remarks>Special purpose code to support Sub-Object signing required by XAdES signatures</remarks>
        private static XmlElement SelectSubObjectNodeForXAdES(Signature signature, string idValue)
        {
            XmlElement node = null;

            // enumerate the References to determine if any are of type XAdES
            foreach (Reference reference in signature.SignedInfo.References)
            {
                // if we get a match by Type?
                if (String.CompareOrdinal(reference.Type, _XAdESTargetType) == 0)
                {
                    // now try to match by Uri
                    // strip off any preceding # mark to facilitate matching
                    string uri;
                    if ((reference.Uri.Length > 0) && (reference.Uri[0] == '#'))
                        uri = reference.Uri.Substring(1);
                    else
                        continue;   // ignore non-local references

                    // if we have a XAdES type reference and the ID matches the requested one
                    // search all object tags for the XML with this ID
                    if (String.CompareOrdinal(uri, idValue) == 0)
                    {
                        node = SelectSubObjectNodeForXAdESInDataObjects(signature, idValue);
                        break;
                    }
                }
            }

            return node;
        }

        /// <summary>
        /// Locates and selects the target XmlElement from all available Object tags
        /// </summary>
        /// <param name="signature"></param>
        /// <param name="idValue"></param>
        /// <returns>element if found; null if not found</returns>
        /// <remarks>relies on XPath query to search the Xml in each Object tag</remarks>
        private static XmlElement SelectSubObjectNodeForXAdESInDataObjects(Signature signature, string idValue)
        {
            XmlElement node = null;
            bool foundMatch = false;    // true if id has matched, even with the wrong namespace

            // now find an object tag that includes an element that matches
            foreach (DataObject dataObject in signature.ObjectList)
            {
                // skip the package object
                if (String.CompareOrdinal(dataObject.Id, XTable.Get(XTable.ID.OpcAttrValue)) != 0)
                {
                    XmlElement element = dataObject.GetXml();

                    // NOTE: this is executing an XPath query
                    // idValue has already been tested as an NCName (unless overridden for compatibility), so there's no
                    // escaping that needs to be done here.
                    XmlNodeList nodeList = element.SelectNodes(".//*[@Id='" + idValue + "']");

                    if (nodeList.Count > 0)
                    {
                        if (!AllowAmbiguousReferenceTargets() &&
                            (nodeList.Count > 1 || foundMatch))
                        {
                            throw new XmlException(SR.Get(SRID.DuplicateObjectId));
                        }

                        foundMatch = true;
                        XmlNode local = nodeList[0] as XmlElement;

                        if (local != null)
                        {
                            XmlNode temp = local;

                            // climb the tree towards the root until we find our namespace
                            while ((temp != null) && (temp.NamespaceURI.Length == 0))
                                temp = temp.ParentNode;

                            // only match if the target is in the XAdES namespace
                            if ((temp != null) && (String.CompareOrdinal(temp.NamespaceURI, _XAdESNameSpace) == 0))
                            {
                                node = local as XmlElement;
                                // continue searching, to find duplicates from different objects
                            }
                        }
                    }
                }
            }

            return node;
        }

        #region Registry access

        // The code in this region is copied (with cosmetic changes) from
        // NDP/clr/src/ManagedLibraries/Security/System/Security/Cryptography/Xml/Utils.cs

        private const string _NetFxSecurityFullKeyName = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework\Security";
        private const string _NetFxSecurityKey = @"SOFTWARE\Microsoft\.NETFramework\Security";

        private static long GetNetFxSecurityRegistryValue(string regValueName, long defaultValue)
        {

            using (RegistryKey securityRegKey = Registry.LocalMachine.OpenSubKey(_NetFxSecurityKey, false))
            {
                if (securityRegKey != null)
                {
                    object regValue = securityRegKey.GetValue(regValueName);
                    if (regValue != null)
                    {
                        RegistryValueKind valueKind = securityRegKey.GetValueKind(regValueName);
                        if (valueKind == RegistryValueKind.DWord || valueKind == RegistryValueKind.QWord)
                        {
                            return Convert.ToInt64(regValue, CultureInfo.InvariantCulture);
                        }
                    }
                }
            }

            return defaultValue;
        }


        private static bool s_readRequireNCNameIdentifier = false;
        private static bool s_requireNCNameIdentifier = true;

        private static bool RequireNCNameIdentifier()
        {
            if (s_readRequireNCNameIdentifier)
            {
                return s_requireNCNameIdentifier;
            }

            long numericValue = GetNetFxSecurityRegistryValue("SignedXmlRequireNCNameIdentifier", 1);
            bool requireNCName = numericValue != 0;

            s_requireNCNameIdentifier = requireNCName;
            System.Threading.Thread.MemoryBarrier();
            s_readRequireNCNameIdentifier = true;

            return s_requireNCNameIdentifier;
        }


        private static bool? s_allowAmbiguousReferenceTarget = null;

        private static bool AllowAmbiguousReferenceTargets()
        {
            // Allow machine administrators to specify that the legacy behavior of matching the first element
            // in an ambiguous reference situation should be persisted. The default behavior is to throw in that
            // situation, but a REG_DWORD or REG_QWORD value of 1 will revert.
            if (s_allowAmbiguousReferenceTarget.HasValue)
            {
                return s_allowAmbiguousReferenceTarget.Value;
            }

            long numericValue = GetNetFxSecurityRegistryValue("SignedXmlAllowAmbiguousReferenceTargets", 0);
            bool allowAmbiguousReferenceTarget = numericValue != 0;

            s_allowAmbiguousReferenceTarget = allowAmbiguousReferenceTarget;
            return s_allowAmbiguousReferenceTarget.Value;
        }

        #endregion Registry access

        private const string _XAdESNameSpace = @"http://uri.etsi.org/01903/v1.2.2#";
        private const string _XAdESTargetType = _XAdESNameSpace + @"SignedProperties";
    }
}

