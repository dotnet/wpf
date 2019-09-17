// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++


Abstract:

    Definition and implementation of the private XmlPrintCapReader class.



--*/

using System;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Globalization;

using System.Printing;
using MS.Internal.Printing.Configuration;

#pragma warning disable 1634, 1691 // Allows suppression of certain PreSharp messages

namespace MS.Internal.Printing.Configuration
{
    /// <summary>
    /// Reader class of XML PrintCapabilities
    /// </summary>
    internal class XmlPrintCapReader
    {
        #region Constructors

        /// <summary>
        /// Instantiates a reader object for the given XML PrintCapabilities
        /// </summary>
        /// <remarks>Constructor verifies the root element is valid</remarks>
        /// <exception cref="FormatException">thrown if XML PrintCapabilities is not well-formed</exception>
        public XmlPrintCapReader(Stream xmlStream)
        {
            // Internally the XML PrintCapabilities reader uses XmlTextReader
            _xmlReader = new XmlTextReader(xmlStream);

            // We need namespace support from the reader.
            _xmlReader.Namespaces = true;

            // Don't resolve external resources.
            _xmlReader.XmlResolver = null;

            // Verify root element is <PrintCapabilities> in our standard namespace
            if ((_xmlReader.MoveToContent() != XmlNodeType.Element) ||
                (_xmlReader.LocalName != PrintSchemaTags.Framework.PrintCapRoot) ||
                (_xmlReader.NamespaceURI != PrintSchemaNamespaces.Framework))
            {
                throw NewPrintCapFormatException(String.Format(CultureInfo.CurrentCulture,
                                                 PTUtility.GetTextFromResource("FormatException.InvalidRootElement"),
                                                 _xmlReader.NamespaceURI,
                                                 _xmlReader.LocalName));
            }

            // Verify the XML PrintCapabilities version is supported

            // For XML attribute without a prefix (e.g. <... name="prn:PageMediaSize">),
            // even though the XML document has default namespace defined as our standard
            // Print Schema framework namespace, the XML atribute still has NULL namespaceURI.
            // It will only have the correct namespaceURI when a prefix is used. This doesn't
            // apply to XML element, whose namespaceURI works fine with default namespace.

            // GetAttribute doesn't move the reader cursor away from the current element
            string version = _xmlReader.GetAttribute(PrintSchemaTags.Framework.RootVersionAttr,
                                                     PrintSchemaNamespaces.FrameworkAttrForXmlReader);

            if (version == null)
            {
                throw NewPrintCapFormatException(String.Format(CultureInfo.CurrentCulture,
                                                 PTUtility.GetTextFromResource("FormatException.RootMissingAttribute"),
                                                 PrintSchemaTags.Framework.RootVersionAttr));
            }

            // Convert string to number to verify
            decimal versionNum;

            try
            {
                versionNum = XmlConvertHelper.ConvertStringToDecimal(version);
            }
            catch (FormatException e)
            {
                throw NewPrintCapFormatException(String.Format(CultureInfo.CurrentCulture,
                                                 PTUtility.GetTextFromResource("FormatException.RootInvalidAttribute"),
                                                 PrintSchemaTags.Framework.RootVersionAttr,
                                                 version),
                                                 e);
            }

            if (versionNum != PrintSchemaTags.Framework.SchemaVersion)
            {
                throw NewPrintCapFormatException(String.Format(CultureInfo.CurrentCulture,
                                                 PTUtility.GetTextFromResource("FormatException.VersionNotSupported"),
                                                 versionNum));
            }

            // Reset internal states to be ready for client's reading of the PrintCapabilities XML
            ResetCurrentElementState();
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Moves the reader cursor to the next Print Schema Framework element at the given depth.
        /// (The element could be Feature, ParameterDefinition, Option, ScoredProperty or Property)
        /// </summary>
        /// <param name="depth">client-requested traversing depth</param>
        /// <param name="typeFilterFlags">flags to indicate client interested node types</param>
        /// <returns>True if next Framework element is ready to read.
        /// False if no more Framework element at the given depth.</returns>
        /// <exception cref="XmlException">XML is not well-formed.</exception>
        public bool MoveToNextSchemaElement(int depth, PrintSchemaNodeTypes typeFilterFlags)
        {
            bool foundElement = false;

            while (!foundElement && _xmlReader.Read())
            {
                // Read() throws XmlException if error occurred while parsing the XML.

                // If we hit an end-element tag at higher depth, we know there are no more
                // Framework elements at the client-requested depth.
                if ((_xmlReader.NodeType == XmlNodeType.EndElement) &&
                    (_xmlReader.Depth < depth))
                {
                    break;
                }

                // Stop at the next XML start element at the client-requested depth
                // and in the standard Framework element namespace.
                if ((_xmlReader.NodeType != XmlNodeType.Element) ||
                    (_xmlReader.Depth != depth) ||
                    (_xmlReader.NamespaceURI != PrintSchemaNamespaces.Framework))
                {
                    continue;
                }

                // Find a candidate, so reset internal states to be ready for its parsing.
                ResetCurrentElementState();

                foundElement = true;

                _currentElementDepth = depth;
                _currentElementIsEmpty = _xmlReader.IsEmptyElement;

                // Map element name to Schema node type
                int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithMap(
                                                  PrintSchemaTags.Framework.NodeTypeMapTable,
                                                  _xmlReader.LocalName);

                if (enumValue > 0)
                {
                    _currentElementNodeType = (PrintSchemaNodeTypes)enumValue;
                }
                else
                {
                    #if _DEBUG
                    Trace.WriteLine("-Warning- skip unknown element '" + _xmlReader.LocalName +
                                    "' at line " + _xmlReader.LineNumber + ", position " +
                                    _xmlReader.LinePosition);
                    #endif

                    foundElement = false;
                }

                if (foundElement)
                {
                    // Check whether or not the found element type is what client is interested in.
                    // If not, we will skip this element.
                    if ((CurrentElementNodeType & typeFilterFlags) == 0)
                    {
                        #if _DEBUG
                        Trace.WriteLine("-Warning- skip not-wanted element '" + _xmlReader.LocalName +
                                        "' at line " + _xmlReader.LineNumber + ", position " +
                                        _xmlReader.LinePosition);
                        #endif

                        foundElement = false;
                    }
                }

                if (foundElement)
                {
                    // The element is what the client wants.
                    if (CurrentElementNodeType != PrintSchemaNodeTypes.Value)
                    {
                        // Element other than <Value> should have the "name" XML attribute.
                        // Reader will verify the "name" XML attribute has a QName value that
                        // is in our standard Keyword namespace.
                        string QName = _xmlReader.GetAttribute(PrintSchemaTags.Framework.NameAttr,
                                                               PrintSchemaNamespaces.FrameworkAttrForXmlReader);

                        // Only <Option> element is allowed not to have the "name" XML attribute
                        if (QName == null)
                        {
                            if (CurrentElementNodeType != PrintSchemaNodeTypes.Option)
                            {
                                #if _DEBUG
                                Trace.WriteLine("-Warning- skip element " + CurrentElementNodeType +
                                                " at line " + _xmlReader.LineNumber + ", position " +
                                                _xmlReader.LinePosition + " due to missing 'name' XML attribute");
                                #endif

                                foundElement = false;
                            }
                        }
                        else
                        {
                            string URI = XmlReaderQName.GetURI(_xmlReader, QName);
                            string localName = XmlReaderQName.GetLocalName(QName);

                            if (URI == PrintSchemaNamespaces.Framework)
                            {
                                _currentElementPSFNameAttrValue = localName;
                            }
                            else if (URI == PrintSchemaNamespaces.StandardKeywordSet)
                            {
                                _currentElementNameAttrValue = localName;
                            }
                            else
                            {
                                // If QName value is not in standard PSF or PSK namespace,
                                // then skip this Schema element.
                                #if _DEBUG
                                Trace.WriteLine("-Warning- skip element " + CurrentElementNodeType +
                                                " at line " + _xmlReader.LineNumber + ", position " +
                                                _xmlReader.LinePosition +
                                                " due to non-PSF/PSK 'name' XML attribute value: " + QName);
                                #endif

                                foundElement = false;
                            }
                        }
                    }
                    else
                    {
                        // For <Value> element, we need to get its element text value.
                        // If this function tells client the <Value> element is found, it guarantees
                        // that the <Value> element text value is non-empty.

                        // Needs to handle xsi:type verification
                        // ReadElementString() returns empty string if the element is empty
                        // (<item></item> or <item/>), and it could throws XmlException.
                        _currentElementTextValue = _xmlReader.ReadElementString();

                        // Our schema requires that <Value> element should always have non-empty value.
                        if ((_currentElementTextValue == null) || (_currentElementTextValue.Length == 0))
                        {
                            #if _DEBUG
                            Trace.WriteLine("-Warning- skip element " + CurrentElementNodeType +
                                            " at line " + _xmlReader.LineNumber + ", position " +
                                            _xmlReader.LinePosition + " since it has empty element text");
                            #endif

                            _currentElementTextValue = null;
                            foundElement = false;
                        }
                    }
                }
            }

            return foundElement;
        }

        /// <summary>
        /// Generic processing of option-element XML attributes
        /// </summary>
        /// <exception>none</exception>
        public void OptionAttributeGenericHandler(PrintCapabilityOption option)
        {
            // Currently we support these option-element attributes:
            // "name"

            // Handle the "name" XML attribute
            option._optionName = this.CurrentElementNameAttrValue;
        }

        /// <summary>
        /// Gets current Property/ScoredProperty's full text value from its "Value" child-element.
        /// </summary>
        /// <exception cref="FormatException">can't find the value</exception>
        /// <exception cref="XmlException">XML is not well-formed.</exception>
        public string GetCurrentPropertyFullValueWithException()
        {
            // No need to loop here. We just need to look for the first <Value> child-element
            if (!MoveToNextSchemaElement(CurrentElementDepth + 1, PrintSchemaNodeTypes.Value))
            {
                throw NewPrintCapFormatException(String.Format(CultureInfo.CurrentCulture,
                                                 PTUtility.GetTextFromResource("FormatException.MissingRequiredChildElement"),
                                                 PrintSchemaTags.Framework.Value,
                                                 _xmlReader.LineNumber,
                                                 _xmlReader.LinePosition));
            }

            return CurrentElementTextValue;
        }

        /// <summary>
        /// Gets current Property/ScoredProperty's integer value from its "Value" child-element
        /// </summary>
        /// <exception cref="FormatException">either can't find the value or find invalid value number</exception>
        /// <exception cref="XmlException">XML is not well-formed.</exception>
        public int GetCurrentPropertyIntValueWithException()
        {
            // Do the assignment outside of try-catch so the FormatException of value-not-found could be thrown properly.
            string textValue = GetCurrentPropertyFullValueWithException();

            int intValue = 0;

            try
            {
                intValue = XmlConvertHelper.ConvertStringToInt32(textValue);
            }
            catch (FormatException e)
            {
                throw NewPrintCapFormatException(String.Format(CultureInfo.CurrentCulture,
                                                 PTUtility.GetTextFromResource("FormatException.InvalidXMLIntValue"),
                                                 CurrentElementTextValue,
                                                 _xmlReader.LineNumber,
                                                 _xmlReader.LinePosition),
                                                 e);
            }

            return intValue;
        }

        /// <summary>
        /// Gets current Property/ScoredProperty's QName LocalName value from its "Value" child-element.
        /// The returned property QName value is guaranteed to be in the standard keyword set namespace.
        /// </summary>
        /// <exception cref="FormatException">either can't find the value or value is private</exception>
        /// <exception cref="XmlException">XML is not well-formed.</exception>
        public string GetCurrentPropertyQNameValueWithException()
        {
            string QName = GetCurrentPropertyFullValueWithException();

            if (XmlReaderQName.GetURI(_xmlReader, QName) == PrintSchemaNamespaces.StandardKeywordSet)
            {
                return XmlReaderQName.GetLocalName(QName);
            }
            else
            {
                // Needs to handle private XML text value
                throw NewPrintCapFormatException(String.Format(CultureInfo.CurrentCulture,
                                                 PTUtility.GetTextFromResource("FormatException.PrivateXMLTextValue"),
                                                 QName,
                                                 _xmlReader.LineNumber,
                                                 _xmlReader.LinePosition));
            }
        }

        /// <summary>
        /// Gets current Property/ScoredProperty's ParameterRef name from its "ParameterRef" child-element.
        /// The returned ParameterRef name is guaranteed to be in the standard keyword set namespace.
        /// </summary>
        /// <exception cref="FormatException">either can't find the child-element or its name is private</exception>
        /// <exception cref="XmlException">XML is not well-formed.</exception>
        public string GetCurrentPropertyParamRefNameWithException()
        {
            if (!MoveToNextSchemaElement(CurrentElementDepth + 1, PrintSchemaNodeTypes.ParameterRef))
            {
                throw NewPrintCapFormatException(String.Format(CultureInfo.CurrentCulture,
                                                 PTUtility.GetTextFromResource("FormatException.MissingRequiredChildElement"),
                                                 PrintSchemaTags.Framework.ParameterRef,
                                                 _xmlReader.LineNumber,
                                                 _xmlReader.LinePosition));
            }

            return CurrentElementNameAttrValue;
        }

        #endregion Public Methods

        #region Public Properties

        /// <summary>
        /// Current element's Print Schema node type
        /// </summary>
        public PrintSchemaNodeTypes CurrentElementNodeType
        {
            get
            {
                return _currentElementNodeType;
            }
        }

        /// <summary>
        /// Current element's depth
        /// </summary>
        public int CurrentElementDepth
        {
            get
            {
                return _currentElementDepth;
            }
        }

        /// <summary>
        /// Checks if current element is an empty XML element
        /// </summary>
        public bool CurrentElementIsEmpty
        {
            get
            {
                return _currentElementIsEmpty;
            }
        }

        /// <summary>
        /// Current element's "name" XML attribute value in Print Schema Keyword namespace.
        /// </summary>
        /// <remarks>
        /// The value is the localname part of a QName whose namespace is the standard
        /// Print Schema Keyword namespace.
        /// </remarks>
        public string CurrentElementNameAttrValue
        {
            get
            {
                return _currentElementNameAttrValue;
            }
        }

        /// <summary>
        /// Current 'Value' element's text value
        /// </summary>
        public string CurrentElementTextValue
        {
            get
            {
                return _currentElementTextValue;
            }
        }

        /// <summary>
        /// Current element's "name" XML attribute value in Print Schema Framework namespace.
        /// </summary>
        /// <remarks>
        /// The value is the localname part of a QName whose namespace is the standard
        /// Print Schema Framework namespace.
        /// </remarks>
        public string CurrentElementPSFNameAttrValue
        {
            get
            {
                return _currentElementPSFNameAttrValue;
            }
        }

        #endregion Public Properties

        #region Internal Fields

        // The XML text reader instance
        internal XmlTextReader _xmlReader;

        #endregion Internal Fields

        #region Private Methods

        /// <summary>
        /// Resets reader's internal schema-element state so we are ready for next element
        /// </summary>
        private void ResetCurrentElementState()
        {
            _currentElementNodeType = PrintSchemaNodeTypes.None;
            _currentElementDepth = 0;
            _currentElementIsEmpty = false;
            _currentElementNameAttrValue = null;
            _currentElementTextValue = null;
            _currentElementPSFNameAttrValue = null;
        }

        /// <summary>
        /// Returns a new FormatException instance for not-well-formed PrintCapabilities XML.
        /// </summary>
        /// <param name="detailMsg">detailed message about the violation of well-formness</param>
        /// <returns>the new FormatException instance</returns>
        private static FormatException NewPrintCapFormatException(string detailMsg)
        {
            return InternalPrintCapabilities.NewPrintCapFormatException(detailMsg);
        }

        /// <summary>
        /// Returns a new FormatException instance for not-well-formed PrintCapabilities XML.
        /// </summary>
        /// <param name="detailMsg">detailed message about the violation of well-formness</param>
        /// <param name="innerException">the exception that causes the violation of well-formness</param>
        /// <returns>the new FormatException instance</returns>
        private static FormatException NewPrintCapFormatException(string detailMsg, Exception innerException)
        {
            return InternalPrintCapabilities.NewPrintCapFormatException(detailMsg, innerException);
        }

        #endregion Private Methods

        #region Private Fields

        private PrintSchemaNodeTypes    _currentElementNodeType;
        private int                     _currentElementDepth;
        private bool                    _currentElementIsEmpty;
        private string                  _currentElementNameAttrValue;
        private string                  _currentElementTextValue;
        private string                  _currentElementPSFNameAttrValue;

        #endregion Private Fields
    }

    /// <summary>
    /// QName helper class for XMLTextReader
    /// </summary>
    internal class XmlReaderQName
    {
        #region Constructors

        // Never need to use an instance. All static methods.
        private XmlReaderQName() {}

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Gets URI of the QName
        /// </summary>
        /// <param name="xmlReader">the XmlTextReader object</param>
        /// <param name="QName">the qualified name</param>
        /// <returns>URI of the QName (null if no matching namespace is found)</returns>
        /// <exception>none</exception>
        public static string GetURI(XmlTextReader xmlReader, string QName)
        {
            int colonIndex = QName.IndexOf(":", StringComparison.Ordinal);

            string prefix = (colonIndex == (-1)) ? "" : QName.Substring(0, colonIndex);

            // Prefix is non-null (could be empty), so LookupNamespace won't throw exception.
            // It will return null if no matching prefix is found.
            return xmlReader.LookupNamespace(prefix);
        }

        /// <summary>
        /// Gets local name of the QName
        /// </summary>
        /// <param name="QName">the qualified name</param>
        /// <returns>local name of the QName</returns>
        /// <exception>none</exception>
        public static string GetLocalName(string QName)
        {
            int colonIndex = QName.IndexOf(":", StringComparison.Ordinal);

            return QName.Substring(colonIndex + 1);
        }

        #endregion Public Methods
    }
}