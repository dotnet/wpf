// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Contents:  The XML Composite font parsing
//
//

using System;
using System.IO;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Markup;
using System.Xml;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using MS.Internal.TextFormatting;

using System.Reflection;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.FontFace
{
    internal class CompositeFontParser
    {
        internal static void VerifyMultiplierOfEm(string propertyName, ref double value)
        {
            if (DoubleUtil.IsNaN(value))
            {
                throw new ArgumentException(SR.Get(SRID.PropertyValueCannotBeNaN, propertyName));
            }
            else if (value > Constants.GreatestMutiplierOfEm)
            {
                value = Constants.GreatestMutiplierOfEm;
            }
            else if (value < -Constants.GreatestMutiplierOfEm)
            {
                value = -Constants.GreatestMutiplierOfEm;
            }
        }

        internal static void VerifyPositiveMultiplierOfEm(string propertyName, ref double value)
        {
            if (DoubleUtil.IsNaN(value))
            {
                throw new ArgumentException(SR.Get(SRID.PropertyValueCannotBeNaN, propertyName));
            }
            else if (value > Constants.GreatestMutiplierOfEm)
            {
                value = Constants.GreatestMutiplierOfEm;
            }
            else if (value <= 0)
            {
                throw new ArgumentException(SR.Get(SRID.PropertyMustBeGreaterThanZero, propertyName));
            }
        }

        internal static void VerifyNonNegativeMultiplierOfEm(string propertyName, ref double value)
        {
            if (DoubleUtil.IsNaN(value))
            {
                throw new ArgumentException(SR.Get(SRID.PropertyValueCannotBeNaN, propertyName));
            }
            else if (value > Constants.GreatestMutiplierOfEm)
            {
                value = Constants.GreatestMutiplierOfEm;
            }
            else if (value < 0)
            {
                throw new ArgumentException(SR.Get(SRID.PropertyCannotBeNegative, propertyName));
            }
        }

        private double GetAttributeAsDouble()
        {
            object value = null;

            try
            {
                value = _doubleTypeConverter.ConvertFromString(
                    null, // type converter context
                    System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS,
                    GetAttributeValue()
                    );
            }
            catch (NotSupportedException)
            {
                FailAttributeValue();
            }

            if (value == null)
                FailAttributeValue();

            return (double)value;
        }

        private XmlLanguage GetAttributeAsXmlLanguage()
        {
            object value = null;

            try
            {
                value = _xmlLanguageTypeConverter.ConvertFromString(
                    null, // type converter context
                    System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS,
                    GetAttributeValue()
                    );
            }
            catch (NotSupportedException)
            {
                FailAttributeValue();
            }

            if (value == null)
                FailAttributeValue();

            return (XmlLanguage)value;
        }

        /// <summary>
        /// Gets the value of the value of the current node which is assumed to be an
        /// attribute. Checks for markup expressions or escaped braces.
        /// </summary>
        private string GetAttributeValue()
        {
            string s = _reader.Value;
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            if (s[0] == '{')
            {
                if (s.Length > 1 && s[1] == '}')
                {
                    s = s.Substring(2);
                }
                else
                {
                    FailAttributeValue();
                }
            }

            return s;
        }

        private const NumberStyles UnsignedDecimalPointStyle =
            NumberStyles.AllowTrailingWhite | 
            NumberStyles.AllowLeadingWhite  | 
            NumberStyles.AllowDecimalPoint;
 
        private const NumberStyles SignedDecimalPointStyle = UnsignedDecimalPointStyle | NumberStyles.AllowLeadingSign;

        /// <summary>
        /// Reads the specified composite font file.
        /// </summary>
        internal static CompositeFontInfo LoadXml(Stream fileStream)
        {
            CompositeFontParser parser = new CompositeFontParser(fileStream);
            return parser._compositeFontInfo;
        }

        /// <summary>
        /// Constructs the composite font parser and parses the file.
        /// </summary>
        /// <param name="fileStream">File stream to parse.</param>
        private CompositeFontParser(Stream fileStream)
        {
            _compositeFontInfo = new CompositeFontInfo();

            _namespaceMap = new Hashtable();
            _doubleTypeConverter = TypeDescriptor.GetConverter(typeof(double));
            _xmlLanguageTypeConverter = new System.Windows.Markup.XmlLanguageConverter();

            _reader = CreateXmlReader(fileStream);

            try
            {
                if (IsStartElement(FontFamilyElement, CompositeFontNamespace))
                {
                    ParseFontFamilyElement();
                }
                // DevDiv:1158540
                // CompositeFont files have been modified to allow a family collection in order to select
                // a family based on OS versions.
                else if (IsStartElement(FontFamilyCollectionElement, CompositeFontNamespace))
                {
                    ParseFontFamilyCollectionElement();
                }
                else
                {
                    FailUnknownElement();
                }
            }
            catch (XmlException x)
            {
                FailNotWellFormed(x);
            }
            catch (Exception x) when(string.Equals(x.GetType().FullName, "System.Security.XmlSyntaxException", StringComparison.OrdinalIgnoreCase))
            {
                FailNotWellFormed(x);
            }
            catch (FormatException x)
            {
                if (_reader.NodeType == XmlNodeType.Attribute)
                    FailAttributeValue(x);
                else
                    Fail(x.Message, x);
            }
            catch (ArgumentException x)
            {
                if (_reader.NodeType == XmlNodeType.Attribute)
                    FailAttributeValue(x);
                else
                    Fail(x.Message, x);
            }
            finally
            {
                _reader.Close();
                _reader = null;
            }
        }


        /// <summary>
        /// Creates the XML reader for the specified file.
        /// </summary>
        private XmlReader CreateXmlReader(Stream fileStream)
        {
            XmlReaderSettings settings = new XmlReaderSettings();

            settings.CloseInput = true;
            settings.IgnoreComments = true;
            settings.IgnoreWhitespace = false;
            settings.ProhibitDtd = true;

            XmlReader baseReader = XmlReader.Create(fileStream, settings);

            return new XmlCompatibilityReader(baseReader, new IsXmlNamespaceSupportedCallback(IsXmlNamespaceSupported));
        }

        
        /// <summary>
        /// Determines whether a given XML namespace is "known" (i.e., should always be processed)
        /// or "unknown" (i.e., should be skipped if declared ignorable).
        /// </summary>
        /// <param name="xmlNamespace">XML namespace to look up.</param>
        /// <param name="newXmlNamespace">Other namespace to map to, if any. Used for versioning.
        /// This implementation always returns null in this parameter.</param>
        /// <returns>
        /// Returns true ("known") for the XAML namespace or the composite font namespace.
        /// for which a Mapping PI exists.
        /// </returns>
        /// <remarks>
        /// System.String is the only object in a mapped namespace that we can instantiate. However, 
        /// we don't want to ignore any mapped namespaces for compatibility reasons. In general, it's 
        /// better for us to reject valid XAML than to accept invalid XAML. We therefore don't want
        /// to ignore an element which the XAML parser would not ignore -- the ignored element might
        /// be invalid. If mapped elements other than System.String are needed in future versions, a 
        /// composite font author can achieve backwards compatibility by conditionalizing mapped 
        /// elements using c:AlternateContent markup.
        /// </remarks>
        private bool IsXmlNamespaceSupported(string xmlNamespace, out string newXmlNamespace)
        {
            newXmlNamespace = null;
            return xmlNamespace == CompositeFontNamespace || 
                xmlNamespace == XamlNamespace || 
                IsMappedNamespace(xmlNamespace);
        }

        /// <summary>
        /// Calls MoveToContent and checks whether the reader is positioned on the specified element.
        /// </summary>
        /// <remarks>
        /// We should always call this method instead of calling _reader.IsStartElement directly because
        /// the latter calls MoveToContent on the underlying reader which means we could fail to parse
        /// Mapping processing instructions.
        /// </remarks>
        private bool IsStartElement(string localName, string namespaceURI)
        {
            MoveToContent();
            return _reader.IsStartElement(localName, namespaceURI);
        }

        /// <summary>
        /// Same semantics as XmlReader.MoveToContent, but this method processes Mapping processing
        /// instructions as it advances past them.
        /// </summary>
        private XmlNodeType MoveToContent()
        {
            bool contentNode = false;

            do
            {
                switch (_reader.NodeType)
                {
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Element:
                    case XmlNodeType.EndElement:
                    case XmlNodeType.EntityReference:
                    case XmlNodeType.EndEntity:
                        contentNode = true;
                        break;
                }
            } while (!contentNode && _reader.Read());

            return _reader.NodeType;
        }

        #region ProcessingInstructions

       private bool IsMappedNamespace(string xmlNamespace)
        {
            return _namespaceMap.ContainsKey(xmlNamespace);
        }

        private bool IsSystemNamespace(string xmlNamespace)
        {
            return (xmlNamespace == "clr-namespace:System;assembly=mscorlib");
        }

        #endregion ProcessingInstructions

        /// <summary>
        /// Find the OS specific font family from a collection in the CompositeFont
        /// 
        /// We must compare the OS in the font family against the current version.  The CompositeFont file
        /// is in descending OS order.
        /// </summary>
        private void ParseFontFamilyCollectionElement()
        {
            bool foundOsSection = false;

            OperatingSystemVersion fontFamilyOsVersion;

            while (_reader.Read())
            {
                // Once we find a FontFamilyElement with the proper OS attribute, parse it
                if (Enum.TryParse(_reader.GetAttribute("OS"), out fontFamilyOsVersion)
                    && OSVersionHelper.IsOsVersionOrGreater(fontFamilyOsVersion))
                {
                    foundOsSection = true;
                    ParseFontFamilyElement();
                    return;
                }
            }

            if (!foundOsSection)
            {
                Fail(string.Format("No FontFamily element found in FontFamilyCollection that matches current OS or greater: {0}", OSVersionHelper.GetOsVersion().ToString()));
            }
        }

        /// <summary>
        /// Parses the FontFamily element, including its attributes and children,
        /// and advances to the next sibling element.
        /// </summary>
        private void ParseFontFamilyElement()
        {
            // Iterate over the attributes.
            if (_reader.MoveToFirstAttribute())
            {
                do
                {
                    // Process attributes in the composite font namespace
                    if (IsCompositeFontAttribute())
                    {
                        string name = _reader.LocalName;

                        if (name == BaselineAttribute)
                        {
                            _compositeFontInfo.Baseline = GetAttributeAsDouble();
                        }
                        else if (name == LineSpacingAttribute)
                        {
                            _compositeFontInfo.LineSpacing = GetAttributeAsDouble();
                        }
                        // DevDiv:1158540
                        // We have to ignore the newly added OS attribute since it is just
                        // used in FontFamilyElement selection, not for anything else.
                        else if (name != OsAttribute)
                        {
                            FailUnknownAttribute();
                        }
                    }
                    else if (!IsIgnorableAttribute())
                    {
                        FailUnknownAttribute();
                    }
} while (_reader.MoveToNextAttribute());

                _reader.MoveToElement();
            }

            // Empty element?
            if (_reader.IsEmptyElement)
            {
                VerifyCompositeFontInfo();
                _reader.Read();
                return;
            }

            // Advance past the start tag.
            _reader.Read();

            // Iterate over children.
            while (MoveToContent() != XmlNodeType.EndElement)
            {
                if (_reader.NodeType == XmlNodeType.Element && _reader.NamespaceURI == CompositeFontNamespace)
                {
                    bool isEmpty = _reader.IsEmptyElement;

                    // It's an element in the composite font namespace; branch depending on the name.
                    switch (_reader.LocalName)
                    {
                        case FamilyNamesPropertyElement:
                            VerifyNoAttributes();
                            _reader.Read();
                            if (!isEmpty)
                            {
                                // Process all child elements.
                                while (MoveToContent() == XmlNodeType.Element)
                                {
                                    if (_reader.LocalName == StringElement && IsSystemNamespace(_reader.NamespaceURI))
                                    {
                                        // It's a System.String.
                                        ParseFamilyNameElement();
                                    }
                                    else
                                    {
                                        // Only System.String is valid in this context.
                                        FailUnknownElement();
                                    }
                                }

                                // Advance past the </FontFamily.FamilyNames> end element, or throw
                                // an exception if we're not at an end element.
                                _reader.ReadEndElement();
                            }
                            break;

                        case FamilyTypefacesPropertyElement:
                            VerifyNoAttributes();
                            _reader.Read();
                            if (!isEmpty)
                            {
                                // Process child elements, of which the only one we recognize in this
                                // context is FamilyTypeface.
                                while (IsStartElement(FamilyTypefaceElement, CompositeFontNamespace))
                                {
                                    ParseFamilyTypefaceElement();
                                }

                                // Advance past the </FontFamily.FamilyTypefaces> end element, or throw
                                // an exception if we're not at an end element.
                                _reader.ReadEndElement();
                            }
                            break;

                        case FamilyMapsPropertyElement:
                            VerifyNoAttributes();
                            _reader.Read();
                            if (!isEmpty)
                            {
                                // Process child elements, of which the only one we recognize in this
                                // context is FontFamilyMap.
                                while (IsStartElement(FamilyMapElement, CompositeFontNamespace))
                                {
                                    ParseFamilyMapElement();
                                }

                                // Advance past the </FontFamily.FamilyTypefaces> end element, or throw
                                // an exception if we're not at an end element.
                                _reader.ReadEndElement();
                            }
                            break;

                        default:
                            // It's some other element.
                            FailUnknownElement();
                            break;
                    }
                }
                else
                {
                    // It's some other content besides an element in the composite font namespace; skip it.
                    _reader.Skip();
                }
            }

            // We should now have read right up to the </FontFamily> end tag.
            VerifyCompositeFontInfo();
            _reader.ReadEndElement();
        }

        /// <summary>
        /// Makes sure the current element has no attributes (except ignorable ones).
        /// </summary>
        private void VerifyNoAttributes()
        {
            if (_reader.MoveToFirstAttribute())
            {
                do
                {
                    if (!IsIgnorableAttribute())
                        FailUnknownAttribute();
} while (_reader.MoveToNextAttribute());

                _reader.MoveToElement();
            }
        }

        /// <summary>
        /// Parses the FamilyName element (actually String), including its attributes 
        /// and children, and advances to the next sibling element.
        /// </summary>
        private void ParseFamilyNameElement()
        {
            XmlLanguage language = null;

            // Iterate over the attributes.
            if (_reader.MoveToFirstAttribute())
            {
                do
                {
                    if (_reader.NamespaceURI == XamlNamespace && _reader.LocalName == KeyAttribute)
                    {
                        language = GetAttributeAsXmlLanguage();
                    }
                    else if (!IsIgnorableAttribute())
                    {
                        FailUnknownAttribute();
                    }
} while (_reader.MoveToNextAttribute());

                _reader.MoveToElement();
            }

            // XAML requires x:Key so we should, too.
            if (language == null)
            {
                FailMissingAttribute(LanguageAttribute);
            }

            // The family name is the element content.
            string familyName = _reader.ReadElementString();
            if (string.IsNullOrEmpty(familyName))
            {
                FailMissingAttribute(NameAttribute);
            }

            _compositeFontInfo.FamilyNames.Add(language, familyName);
        }

        /// <summary>
        /// Parses the FamilyTypeface element, including its attributes and children,
        /// and advances to the next sibling element.
        /// </summary>
        private void ParseFamilyTypefaceElement()
        {
            FamilyTypeface face = new FamilyTypeface();

            ParseFamilyTypefaceAttributes(face);

            if (_reader.IsEmptyElement)
            {
                _reader.Read();
            }
            else
            {
                _reader.Read();

                while (MoveToContent() != XmlNodeType.EndElement)
                {
                    if (_reader.NodeType == XmlNodeType.Element && _reader.NamespaceURI == CompositeFontNamespace)
                    {
                        if (_reader.LocalName == DeviceFontCharacterMetricsPropertyElement)
                        {
                            VerifyNoAttributes();

                            if (_reader.IsEmptyElement)
                            {
                                _reader.Read();
                            }
                            else
                            {
                                _reader.Read();

                                // Process all child elements.
                                while (MoveToContent() == XmlNodeType.Element)
                                {
                                    if (_reader.LocalName == CharacterMetricsElement)
                                    {
                                        ParseCharacterMetricsElement(face);
                                    }
                                    else
                                    {
                                        // Only CharacterMetricsElement is valid in this context.
                                        FailUnknownElement();
                                    }
                                }
                                // Process the end element for the collection.
                                _reader.ReadEndElement();
                            }
                        }
                        else
                        {
                            FailUnknownElement();
                        }
                    }
                    else
                    {
                        _reader.Skip();
                    }
                }

                _reader.ReadEndElement();
            }

            // Add the typeface.
            _compositeFontInfo.GetFamilyTypefaceList().Add(face);
        }

        /// <summary>
        /// Parses the attributes of the FamilyTypeface element and sets the corresponding
        /// properties on the specified FamilyTypeface object. On return, the reader remains
        /// positioned on the element.
        /// </summary>
        private void ParseFamilyTypefaceAttributes(FamilyTypeface face)
        {
            // Iterate over the attributes.
            if (_reader.MoveToFirstAttribute())
            {
                do
                {
                    // Process attributes in the composite font namespace; ignore any others.
                    if (IsCompositeFontAttribute())
                    {
                        string name = _reader.LocalName;

                        if (name == StyleAttribute)
                        {
                            FontStyle fontStyle = new FontStyle();
                            if (!FontStyles.FontStyleStringToKnownStyle(GetAttributeValue(), CultureInfo.InvariantCulture, ref fontStyle))
                                FailAttributeValue();

                            face.Style = fontStyle;
                        }
                        else if (name == WeightAttribute)
                        {
                            FontWeight fontWeight = new FontWeight();
                            if (!FontWeights.FontWeightStringToKnownWeight(GetAttributeValue(), CultureInfo.InvariantCulture, ref fontWeight))
                                FailAttributeValue();

                            face.Weight = fontWeight;
                        }
                        else if (name == StretchAttribute)
                        {
                            FontStretch fontStretch = new FontStretch();
                            if (!FontStretches.FontStretchStringToKnownStretch(GetAttributeValue(), CultureInfo.InvariantCulture, ref fontStretch))
                                FailAttributeValue();

                            face.Stretch = fontStretch;
                        }
                        else if (name == UnderlinePositionAttribute)
                        {
                            face.UnderlinePosition = GetAttributeAsDouble();
                        }
                        else if (name == UnderlineThicknessAttribute)
                        {
                            face.UnderlineThickness = GetAttributeAsDouble();
                        }
                        else if (name == StrikethroughPositionAttribute)
                        {
                            face.StrikethroughPosition = GetAttributeAsDouble();
                        }
                        else if (name == StrikethroughThicknessAttribute)
                        {
                            face.StrikethroughThickness = GetAttributeAsDouble();
                        }
                        else if (name == CapsHeightAttribute)
                        {
                            face.CapsHeight = GetAttributeAsDouble();
                        }
                        else if (name == XHeightAttribute)
                        {
                            face.XHeight = GetAttributeAsDouble();
                        }
                        else if (name == DeviceFontNameAttribute)
                        {
                            face.DeviceFontName = GetAttributeValue();
                        }
                        else
                        {
                            FailUnknownAttribute();
                        }
                    }
                    else if (!IsIgnorableAttribute())
                    {
                        FailUnknownAttribute();
                    }
} while (_reader.MoveToNextAttribute());

                _reader.MoveToElement();
            }
        }

        /// <summary>
        /// Parses a CharacterMetrics element, and advances the current position beyond the
        /// element. Adds a CharacterMetrics object to the given FamilyTypface.
        /// </summary>
        private void ParseCharacterMetricsElement(FamilyTypeface face)
        {
            string key = null;
            string metrics = null;

            if (_reader.MoveToFirstAttribute())
            {
                do
                {
                    if (_reader.NamespaceURI == XamlNamespace && _reader.LocalName == KeyAttribute)
                    {
                        key = GetAttributeValue();
                    }
                    else if (IsCompositeFontAttribute() && _reader.LocalName == MetricsAttribute)
                    {
                        metrics = GetAttributeValue();
                    }
                    else if (!IsIgnorableAttribute())
                    {
                        FailUnknownAttribute();
                    }
                } while (_reader.MoveToNextAttribute());

                _reader.MoveToElement();
            }

            if (key == null)
                FailMissingAttribute(KeyAttribute);

            if (metrics == null)
                FailMissingAttribute(MetricsAttribute);

            face.DeviceFontCharacterMetrics.Add(
                CharacterMetricsDictionary.ConvertKey(key),
                new CharacterMetrics(metrics)
                );

            // There should be no child elements.
            ParseEmptyElement();
        }

        /// <summary>
        /// Parses the FontFamilyMap element, including its attributes and children,
        /// and advances to the next sibling element.
        /// </summary>
        private void ParseFamilyMapElement()
        {
            FontFamilyMap fmap = new FontFamilyMap();

            // Parse the family map attributes.
            if (_reader.MoveToFirstAttribute())
            {
                do
                {
                    // Process attributes in the composite font namespace; ignore any others.
                    if (IsCompositeFontAttribute())
                    {
                        string name = _reader.LocalName;

                        if (name == UnicodeAttribute)
                        {
                            fmap.Unicode = GetAttributeValue();
                        }
                        else if (name == TargetAttribute)
                        {
                            fmap.Target = GetAttributeValue();
                        }
                        else if (name == ScaleAttribute)
                        {
                            fmap.Scale = GetAttributeAsDouble();
                        }
                        else if (name == LanguageAttribute)
                        {
                            fmap.Language = GetAttributeAsXmlLanguage();
                        }
                        else
                        {
                            FailUnknownAttribute();
                        }
                    }
                    else if (!IsIgnorableAttribute())
                    {
                        FailUnknownAttribute();
                    }
} while (_reader.MoveToNextAttribute());

                _reader.MoveToElement();
            }

            _compositeFontInfo.FamilyMaps.Add(fmap);

            // There should be no child elements.
            ParseEmptyElement();
        }

        /// <summary>
        /// Advances past the current element and its children, throwing and exception 
        /// if there are any child elements in the composite font namespace.
        /// </summary>
        private void ParseEmptyElement()
        {
            if (_reader.IsEmptyElement)
            {
                _reader.Read();
                return;
            }

            _reader.Read();

            while (MoveToContent() != XmlNodeType.EndElement)
            {
                if (_reader.NodeType == XmlNodeType.Element && _reader.NamespaceURI == CompositeFontNamespace)
                {
                    FailUnknownElement();
                }
                else
                {
                    _reader.Skip();
                }
            }

            _reader.ReadEndElement();
        }

        /// <summary>
        /// Determines whether the reader is positioned on an composite font attribute, 
        /// which we define to me either (a) it has no namespace at all, or (b) it's in 
        /// the composite font namespace.
        /// </summary>
        private bool IsCompositeFontAttribute()
        {
            string ns = _reader.NamespaceURI;
            return string.IsNullOrEmpty(ns) || ns == CompositeFontNamespace;
        }

        /// <summary>
        /// Determines whether the attribute can be safely ignored, even if it
        /// has not been explicitly declared as ignorable via compatibility markup.
        /// Currently, we ignore attributes defined by the XML and XML namespaces
        /// standards.
        /// </summary>
        private bool IsIgnorableAttribute()
        {
            string ns = _reader.NamespaceURI;
            return ns == XmlNamespace || ns == XmlnsNamespace;
        }

        #region error reporting

        /// <summary>
        /// Make sure the minimum required information is specified.
        /// </summary>
        private void VerifyCompositeFontInfo()
        {
            if (_compositeFontInfo.FamilyMaps.Count == 0)
                Fail(SR.Get(SRID.CompositeFontMissingElement, FamilyMapElement));

            if (_compositeFontInfo.FamilyNames.Count == 0)
                Fail(SR.Get(SRID.CompositeFontMissingElement, StringElement));
        }

        /// <summary>
        /// Fail because of an XML exception.
        /// </summary>
        private void FailNotWellFormed(Exception x)
        {
            throw new FileFormatException(new Uri(_reader.BaseURI, UriKind.RelativeOrAbsolute), x);
        }

        /// <summary>
        /// Fail because of an incorrect attribute value.
        /// </summary>
        private void FailAttributeValue()
        {
            Fail(SR.Get(
                SRID.CompositeFontAttributeValue1,
                _reader.LocalName));
        }

        /// <summary>
        /// Fail because of an incorrect attribute value with an inner exception.
        /// </summary>
        private void FailAttributeValue(Exception x)
        {
            Fail(SR.Get(
                SRID.CompositeFontAttributeValue2,
                _reader.LocalName,
                x.Message),
                x);
        }

        /// <summary>
        /// Fail because of an unknown element.
        /// </summary>
        private void FailUnknownElement()
        {
            Fail(SR.Get(
                SRID.CompositeFontUnknownElement,
                _reader.LocalName,
                _reader.NamespaceURI));
        }

        /// <summary>
        /// Fail because of an unknown attribute.
        /// </summary>
        private void FailUnknownAttribute()
        {
            Fail(SR.Get(
                SRID.CompositeFontUnknownAttribute,
                _reader.LocalName,
                _reader.NamespaceURI));
        }
        
        /// <summary>
        /// Fail because a required attribute is not present.
        /// </summary>
        /// <param name="name"></param>
        private void FailMissingAttribute(string name)
        {
            Fail(SR.Get(SRID.CompositeFontMissingAttribute, name));
        }

        /// <summary>
        ///  Fail with a specified error message.
        /// </summary>
        private void Fail(string message)
        {
            Fail(message, null);
        }

        /// <summary>
        /// Fail with a specified error message and inner exception.
        /// </summary>
        private void Fail(string message, Exception innerException)
        {
            string fileName = _reader.BaseURI;
            throw new FileFormatException(new Uri(fileName, UriKind.RelativeOrAbsolute), message, innerException);
        }
        #endregion

        private CompositeFontInfo _compositeFontInfo;

        private XmlReader _reader;

        // XML namespaces for which Mapping processing instructions have been read. For each entry,
        // the key is the XML namespace, and the value is either SystemClrNamespace (if the the 
        // Mapping PI specifies "System" and "MSCORLIB") or String.Empty (any other Mapping).
        private Hashtable _namespaceMap;

        // Type converters for double and XmlLanguage types.
        private TypeConverter _doubleTypeConverter;
        private TypeConverter _xmlLanguageTypeConverter;

        private const string SystemClrNamespace = "System";

        private const string CompositeFontNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/composite-font";
        private const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
        private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
        private const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

        // DevDiv:1158540
        // Adding new collection element to hold multiple FontFamilyElements
        private const string FontFamilyCollectionElement = "FontFamilyCollection";
        private const string FontFamilyElement = "FontFamily";
        private const string BaselineAttribute = "Baseline";
        private const string LineSpacingAttribute = "LineSpacing";
        private const string FamilyNamesPropertyElement = "FontFamily.FamilyNames";
        private const string StringElement = "String";
        private const string FamilyTypefacesPropertyElement = "FontFamily.FamilyTypefaces";
        private const string FamilyTypefaceElement = "FamilyTypeface";
        private const string FamilyMapsPropertyElement = "FontFamily.FamilyMaps";
        private const string FamilyMapElement = "FontFamilyMap";
        private const string KeyAttribute = "Key";
        private const string LanguageAttribute = "Language";
        private const string NameAttribute = "Name";
        private const string StyleAttribute = "Style";
        private const string WeightAttribute = "Weight";
        private const string StretchAttribute = "Stretch";
        private const string UnderlinePositionAttribute = "UnderlinePosition";
        private const string UnderlineThicknessAttribute = "UnderlineThickness";
        private const string StrikethroughPositionAttribute = "StrikethroughPosition";
        private const string StrikethroughThicknessAttribute = "StrikethroughThickness";
        private const string CapsHeightAttribute = "CapsHeight";
        private const string XHeightAttribute = "XHeight";
        private const string UnicodeAttribute = "Unicode";
        private const string TargetAttribute = "Target";
        private const string ScaleAttribute = "Scale";
        private const string DeviceFontNameAttribute = "DeviceFontName";
        private const string DeviceFontCharacterMetricsPropertyElement = "FamilyTypeface.DeviceFontCharacterMetrics";
        private const string CharacterMetricsElement = "CharacterMetrics";
        private const string MetricsAttribute = "Metrics";

        // DevDiv:1158540
        // FontFamilyElements can now have OS specific attributes
        private const string OsAttribute = "OS";
    }
}


namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Partial class splitted from the original one in LineServices.cs.
    /// We do this to avoid bringing in TextFormatting namespace when 
    /// building FontCacheServices.exe
    /// </summary>
    internal static partial class Constants
    {
        /// <summary>
        /// Greatest multiple of em allowed in composite font file
        /// </summary>
        public const double GreatestMutiplierOfEm = 100;
    }
}
