// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++


Abstract:

    Definition and implementation of the internal PrintTicketEditor and XmlDocQName classes.



--*/

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;

using System.Printing;
using MS.Internal.Printing.Configuration;

namespace MS.Internal.Printing.Configuration
{
    internal class PrintTicketEditor
    {
        #region Constructors

        private PrintTicketEditor() {}

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Verifies if the PrintTicket is well-formed
        /// </summary>
        /// <exception cref="FormatException">
        /// The PrintTicket is not well-formed.
        /// </exception>
        public static void CheckIsWellFormedPrintTicket(InternalPrintTicket pt)
        {
            XmlElement root = pt.XmlDoc.DocumentElement;

            // Root element should be in our standard namespace and should be <PrintTicket>.
            if ((root.NamespaceURI != PrintSchemaNamespaces.Framework) ||
                (root.LocalName != PrintSchemaTags.Framework.PrintTicketRoot))
            {
                throw NewPTFormatException(String.Format(CultureInfo.CurrentCulture,
                                                         PTUtility.GetTextFromResource("FormatException.InvalidRootElement"),
                                                         root.NamespaceURI,
                                                         root.LocalName));
            }

            string version = root.GetAttribute(PrintSchemaTags.Framework.RootVersionAttr,
                                               PrintSchemaNamespaces.FrameworkAttrForXmlDOM);

            // Root element should have the "version" attribute
            // (XmlElement.GetAttribute returns empty string when the attribute is not found, but
            // (XmlTextReader.GetAttribute returns null when the attribute is not found)
            if ((version == null) || (version.Length == 0))
            {
                throw NewPTFormatException(String.Format(CultureInfo.CurrentCulture,
                                                         PTUtility.GetTextFromResource("FormatException.RootMissingAttribute"),
                                                         PrintSchemaTags.Framework.RootVersionAttr));
            }

            decimal versionNum;

            try
            {
                versionNum = XmlConvertHelper.ConvertStringToDecimal(version);
            }
            catch (FormatException e)
            {
                throw NewPTFormatException(String.Format(CultureInfo.CurrentCulture,
                                                         PTUtility.GetTextFromResource("FormatException.RootInvalidAttribute"),
                                                         PrintSchemaTags.Framework.RootVersionAttr,
                                                         version),
                                           e);
            }

            // and the "version" attribute value should be what we support
            if (versionNum != PrintSchemaTags.Framework.SchemaVersion)
            {
                throw NewPTFormatException(String.Format(CultureInfo.CurrentCulture,
                                                         PTUtility.GetTextFromResource("FormatException.VersionNotSupported"),
                                                         versionNum));
            }

            // Now go through each root child element and verify they are valid children
            XmlNode rootChild = root.FirstChild;

            // It's recommended that traversing the node in forward-only movement by using NextSibling
            // is best for XmlDocument performance. This is because the list is not double-linked.
            while (rootChild != null)
            {
                // If the root child doesn't live in our standard namespace, we should ignore it
                // rather than rejecting it since it's acceptable to have private elements under
                // the root.
                if (rootChild.NamespaceURI == PrintSchemaNamespaces.Framework)
                {
                    // For <PrintTicket> root, our Framework schema only allow these children elements:
                    // <Feature> <AttributeSet> <Property> <ParameterInit>
                    if ((rootChild.NodeType != XmlNodeType.Element) ||
                        ((rootChild.LocalName != PrintSchemaTags.Framework.Feature) &&
                         (rootChild.LocalName != PrintSchemaTags.Framework.ParameterInit) &&
                         (rootChild.LocalName != PrintSchemaTags.Framework.Property)))
                    {
                        throw NewPTFormatException(String.Format(CultureInfo.CurrentCulture,
                                                                 PTUtility.GetTextFromResource("FormatException.RootInvalidChildElement"),
                                                                 rootChild.Name));
                    }

                    string childName = ((XmlElement)rootChild).GetAttribute(PrintSchemaTags.Framework.NameAttr,
                                                                            PrintSchemaNamespaces.FrameworkAttrForXmlDOM);

                    // All the recognized root child element should have an XML attribut "name"
                    if ((childName == null) || (childName.Length == 0))
                    {
                        throw NewPTFormatException(String.Format(CultureInfo.CurrentCulture,
                                                                 PTUtility.GetTextFromResource("FormatException.RootChildMissingAttribute"),
                                                                 rootChild.Name,
                                                                 PrintSchemaTags.Framework.NameAttr));
                    }
                }

                rootChild = rootChild.NextSibling;
            }

            // We will end the verification at the root child level here. Instead of traversing the whole tree
            // to find violations in this construtor, we will delay detecting violations under root child level
            // until information of an individual feature/property subtree is requested.
        }

        /// <summary>
        /// For PrintTicket XML supplied by client, it may not have all the standard namespace declarations we will need
        /// in future PrintTicket operations. This function will check and add any missing standard namespace declarations
        /// to the PrintTicket XML root element.
        /// </summary>
        /// <remarks>
        /// This function should be called after the PrintTicket XML has passed CheckIsWellFormedPrintTicket() validation,
        /// so it's operating on a well-formed PrintTicket XML.
        /// </remarks>
        /// <exception cref="FormatException">
        /// Unable to add standard namespace declaration at root element.
        /// </exception>
        public static void CheckAndAddMissingStdNamespaces(InternalPrintTicket pt)
        {
            XmlElement root = pt.XmlDoc.DocumentElement;
            bool hasPSK = false, hasXSI = false, hasXSD = false;

            // Print Schema framework namespace must already exist in the well-formed PrintTicket XML.

            // first check if any standard namespace declaration is missing in the root element
            foreach (XmlAttribute attr in root.Attributes)
            {
                if (attr.Name.StartsWith("xmlns:", StringComparison.Ordinal) ||
                    (attr.Name == "xmlns"))
                {
                    if (attr.Value == PrintSchemaNamespaces.StandardKeywordSet)
                    {
                        hasPSK = true;
                    }
                    else if (attr.Value == PrintSchemaNamespaces.xsi)
                    {
                        hasXSI = true;
                    }
                    else if (attr.Value == PrintSchemaNamespaces.xsd)
                    {
                        hasXSD = true;
                    }
                }
            }

            // then add the declarations for any missing standard namespaces
            if (!hasPSK)
            {
                AddStdNamespaceDeclaration(root,
                                           PrintSchemaPrefixes.StandardKeywordSet,
                                           PrintSchemaNamespaces.StandardKeywordSet);
            }

            if (!hasXSI)
            {
                AddStdNamespaceDeclaration(root,
                                           PrintSchemaPrefixes.xsi,
                                           PrintSchemaNamespaces.xsi);
            }

            if (!hasXSD)
            {
                AddStdNamespaceDeclaration(root,
                                           PrintSchemaPrefixes.xsd,
                                           PrintSchemaNamespaces.xsd);
            }
        }

        /// <summary>
        /// Add namespace declaration for the specified namespace URI.
        /// </summary>
        /// <exception cref="FormatException">
        /// Unable to add standard namespace declaration at root element.
        /// </exception>
        public static string AddStdNamespaceDeclaration(XmlElement root, string prefix_header, string nsURI)
        {
            int limit = 1000; // hard-coded upper limit for namespace prefix string look up
            int index;
            string prefix = null;

            // We need to find a prefix string that doesn't conflict with any prefixes already used.
            for (index = 0; index < limit; index++)
            {
                // the new prefix is in the format like: "psk0000" "xsi0001" "xsd0100"
                prefix = prefix_header + index.ToString(CultureInfo.InvariantCulture).PadLeft(4, '0');

                if (root.Attributes["xmlns:" + prefix] == null)
                {
                    root.SetAttribute("xmlns:" + prefix, nsURI);
                    break;
                }
            }

            if (index >= limit)
            {
                throw NewPTFormatException(String.Format(CultureInfo.CurrentCulture,
                                                         PTUtility.GetTextFromResource("FormatException.AddXmlnsFailAtRootElement")));
            }

            return prefix;
        }

        /// <summary>
        /// Gets the feature XML element in the PrintTicket for the specified feature name.
        /// Null is returned if the feature XML element can't be found.
        /// </summary>
        public static XmlElement GetSchemaElementWithNameAttr(InternalPrintTicket pt,
                                                              XmlElement parent,
                                                              string schemaTag,
                                                              string nameAttrWanted)
        {
            XmlElement elementMatched = null;
            bool foundMatch = false;

            // Now go through each child element to find the matching schema element
            XmlNode child = parent.FirstChild;

            // It's recommended that traversing the node in forward-only movement by using NextSibling
            // is best for XmlDocument performance. This is because the list is not double-linked.
            while (child != null)
            {
                // We are looking for a standard namespace schema element node
                if ((child.NodeType != XmlNodeType.Element) ||
                    (child.LocalName != schemaTag) ||
                    (child.NamespaceURI != PrintSchemaNamespaces.Framework))
                {
                    child = child.NextSibling;
                    continue;
                }

                if (nameAttrWanted == null)
                {
                    // Caller is not looking for a "name" attribute value match, so we already
                    // found the wanted schema element.
                    foundMatch = true;
                }
                else
                {
                    // We need to match the "name" attribute value.
                    string childName = ((XmlElement)child).GetAttribute(
                                                      PrintSchemaTags.Framework.NameAttr,
                                                      PrintSchemaNamespaces.FrameworkAttrForXmlDOM);

                    if ((childName != null) &&
                        (childName.Length != 0) &&
                        (XmlDocQName.GetURI(pt.XmlDoc, childName) == PrintSchemaNamespaces.StandardKeywordSet) &&
                        (XmlDocQName.GetLocalName(childName) == nameAttrWanted))
                    {
                        foundMatch = true;
                    }
                }

                if (foundMatch)
                {
                    elementMatched = (XmlElement)child;
                    break;
                }

                child = child.NextSibling;
            }

            return elementMatched;
        }

        /// <summary>
        /// Removes all child schema element nodes that match the schemaTag name and the optional
        /// name attribute value of the specified parent node. This should only be used before
        /// writing a new schema element setting.
        /// </summary>
        public static void RemoveAllSchemaElementsWithNameAttr(InternalPrintTicket pt,
                                                               XmlElement parent,
                                                               string schemaTag,
                                                               string nameAttrToDelete)
        {
            XmlElement childMatched;

            // Now go through each child element to find the matching schema element
            XmlNode child = parent.FirstChild;

            // It's recommended that traversing the node in forward-only movement by using NextSibling
            // is best for XmlDocument performance. This is because the list is not double-linked.
            while (child != null)
            {
                childMatched = null;

                // We are looking for a standard namespace schema element node
                if ((child.NodeType != XmlNodeType.Element) ||
                    (child.LocalName != schemaTag) ||
                    (child.NamespaceURI != PrintSchemaNamespaces.Framework))
                {
                    child = child.NextSibling;
                    continue;
                }

                if (nameAttrToDelete == null)
                {
                    // Caller is not looking for a "name" attribute value match, so we already
                    // found the wanted schema element.
                    childMatched = (XmlElement)child;
                }
                else
                {
                    // We need to match the "name" attribute value.
                    string childName = ((XmlElement)child).GetAttribute(
                                                      PrintSchemaTags.Framework.NameAttr,
                                                      PrintSchemaNamespaces.FrameworkAttrForXmlDOM);

                    if ((childName != null) &&
                        (childName.Length != 0) &&
                        (XmlDocQName.GetURI(pt.XmlDoc, childName) == PrintSchemaNamespaces.StandardKeywordSet) &&
                        (XmlDocQName.GetLocalName(childName) == nameAttrToDelete))
                    {
                        childMatched = (XmlElement)child;
                    }
                }

                child = child.NextSibling;

                if (childMatched != null)
                {
                    parent.RemoveChild(childMatched);
                }
            }
        }

        public static XmlElement AddSchemaElementWithNameAttr(InternalPrintTicket pt,
                                                              XmlElement parent,
                                                              string schemaTag,
                                                              string nameAttr)
        {
            string prefix = pt.XmlDoc.DocumentElement.GetPrefixOfNamespace(PrintSchemaNamespaces.Framework);

            XmlElement newNode = pt.XmlDoc.CreateElement(prefix, schemaTag, PrintSchemaNamespaces.Framework);

            if (nameAttr != null)
            {
                newNode.SetAttribute(PrintSchemaTags.Framework.NameAttr,
                                     PrintSchemaNamespaces.FrameworkAttrForXmlDOM,
                                     XmlDocQName.GetQName(pt.XmlDoc, PrintSchemaNamespaces.StandardKeywordSet, nameAttr));
            }

            return (XmlElement)parent.AppendChild(newNode);
        }

        public static void SetXsiTypeAttr(InternalPrintTicket pt,
                                          XmlElement valueElement,
                                          string xsiType)
        {
            // the attribute is in the format like: xsi:type="xsd:integer"
            valueElement.SetAttribute("type",
                                      PrintSchemaNamespaces.xsi,
                                      XmlDocQName.GetQName(pt.XmlDoc, PrintSchemaNamespaces.xsd, xsiType));
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Returns a new FormatException instance for not-well-formed PrintTicket XML.
        /// </summary>
        /// <param name="detailMsg">detailed message about the violation of well-formness</param>
        /// <returns>the new FormatException instance</returns>
        private static FormatException NewPTFormatException(string detailMsg)
        {
            return InternalPrintTicket.NewPTFormatException(detailMsg);
        }

        /// <summary>
        /// Returns a new FormatException instance for not-well-formed PrintTicket XML.
        /// </summary>
        /// <param name="detailMsg">detailed message about the violation of well-formness</param>
        /// <param name="innerException">the exception that causes the violation of well-formness</param>
        /// <returns>the new FormatException instance</returns>
        private static FormatException NewPTFormatException(string detailMsg, Exception innerException)
        {
            return InternalPrintTicket.NewPTFormatException(detailMsg, innerException);
        }

        #endregion Private Methods
    }

    internal class XmlDocQName
    {
        #region Constructors

        private XmlDocQName() {}

        #endregion Constructors

        #region Public Methods

        public static string GetURI(XmlDocument xmlDoc, string QName)
        {
            int colonIndex = QName.IndexOf(":", StringComparison.Ordinal);

            string prefix = (colonIndex == (-1)) ? "" : QName.Substring(0, colonIndex);

            string uri = xmlDoc.DocumentElement.GetNamespaceOfPrefix(prefix);

            // Needs fixing: if prefix is "", GetNamespaceOfPrefix will return "" even if default namespace
            // is declared.
            return uri;
        }

        public static string GetLocalName(string QName)
        {
            int colonIndex = QName.IndexOf(":", StringComparison.Ordinal);

            string localName =QName.Substring(colonIndex + 1);

            return localName;
        }

        public static string GetQName(XmlDocument xmlDoc, string URI, string localName)
        {
            string QName;
            string prefix = xmlDoc.DocumentElement.GetPrefixOfNamespace(URI);

            if (prefix == null)
            {
                QName = localName;
            }
            else
            {
                QName = prefix + ":" + localName;
            }

            // Console.WriteLine("URI:{0}, localName:{1} ===> QName:{2}", URI, localName, QName);

            return QName;
        }

        #endregion Public Methods
    }
}
