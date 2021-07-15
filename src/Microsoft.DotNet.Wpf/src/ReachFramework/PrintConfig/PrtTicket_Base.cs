// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++


Abstract:

    Definition and implementation of PrintTicket base types.



--*/

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using System.Printing;
using MS.Internal.Printing.Configuration;

#pragma warning disable 1634, 1691 // Allows suppression of certain PreSharp messages

namespace MS.Internal.Printing.Configuration
{
    /// <internalonly/>
    /// <summary>
    /// Do not use.
    /// Abstract base class of <see cref="InternalPrintTicket"/> feature.
    /// </summary>
    abstract internal class PrintTicketFeature
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of the PrintTicketFeature class.
        /// </summary>
        /// <param name="ownerPrintTicket">The <see cref="InternalPrintTicket"/> object this feature belongs to.</param>
        protected PrintTicketFeature(InternalPrintTicket ownerPrintTicket)
        {
            this._ownerPrintTicket = ownerPrintTicket;

            // Base class sets the defaults

            // _featureName = null;
            // _parentFeature = null;
            // _propertyMaps = null;
            // The above fields need to be set by each derived classes in their constructors.
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Clears this feature's setting(s) in the Print Ticket.
        /// </summary>
        /// <remarks>
        /// After calling this method, the feature's setting(s) will become unspecified, and the Print Ticket XML
        /// will no longer contain the XML element for the feature.
        /// </remarks>
        public void ClearSetting()
        {
            XmlElement parentElement = null;

            // Find the feature XML element's parent element.
            if (_parentFeature != null)
            {
                // If this feature has a parent feature, we need to find the XML element of the parent feature.
                if (_parentFeature.FeatureNode != null)
                {
                    parentElement = _parentFeature.FeatureNode.FeatureElement;
                }
            }
            else
            {
                // It's a root feature.
                parentElement = _ownerPrintTicket.XmlDoc.DocumentElement;
            }

            if (parentElement != null)
            {
                // Delete current feature's XML element (and the whole subtree under that XML element).
                PrintTicketEditor.RemoveAllSchemaElementsWithNameAttr(_ownerPrintTicket,
                                                                      parentElement,
                                                                      PrintSchemaTags.Framework.Feature,
                                                                      _featureName);
            }

            // Feature could have parameter-ref scored properties, so we also need to take care of deleting
            // the feature's parameter-init XML element.
            for (int i=0; i<_propertyMaps.Length; i++)
            {
                if (_propertyMaps[i].PropType == PTPropValueTypes.IntParamRefValue)
                {
                    PrintTicketEditor.RemoveAllSchemaElementsWithNameAttr(this._ownerPrintTicket,
                                                            this._ownerPrintTicket.XmlDoc.DocumentElement,
                                                            PrintSchemaTags.Framework.ParameterInit,
                                                            _propertyMaps[i].ParamRefName);
                }
            }
        }

        #endregion Public Methods

        #region Internal Properties

        internal PTFeatureNode FeatureNode
        {
            get
            {
                PTFeatureNode featureNode = null;

                if (_parentFeature != null)
                {
                    // If this is a sub-feature, we need to get the feature node relative
                    // to the parent feature node.
                    if (_parentFeature.FeatureNode != null)
                    {
                        featureNode = PTFeatureNode.GetFeatureNode(this,
                                                    _parentFeature.FeatureNode.FeatureElement);
                    }
                }
                else
                {
                    // If this is a root-feature, we can get the feature node from the root.
                    featureNode = PTFeatureNode.GetFeatureNode(this,
                                                    this._ownerPrintTicket.XmlDoc.DocumentElement);
                }

                return featureNode;
            }
        }

        /// <summary>
        /// Indexer to get/set scored property value, including the OptionName
        /// </summary>
        /// <remarks>
        /// This indexer supports any property whose value is one of following types:
        /// 1) int-based enum value
        /// 2) integer number
        /// 3) integer parameter initializer reference
        /// </remarks>
        internal int this[string propertyName]
        {
            get
            {
                int propValue;

                PTPropertyMapEntry map = LookupPropertyMap(propertyName);

                #if _DEBUG
                Trace.WriteLine("-Trace- reading " + this._featureName + " " + propertyName);
                #endif

                // Set the default value first.
                if (map.PropType == PTPropValueTypes.EnumStringValue)
                {
                    // Initialize the setting state to be "Unspecified".
                    propValue = PrintSchema.EnumUnspecifiedValue;
                }
                else
                {
                    propValue = PrintSchema.UnspecifiedIntValue;
                }

                // We have property value to read only when the feature's XML element is in the Print Ticket XML.
                if (FeatureNode != null)
                {
                    if (map.PropType == PTPropValueTypes.PositiveIntValue)
                    {
                        int intValue;

                        if (FeatureNode.GetOptionPropertyIntValue(propertyName, out intValue))
                        {
                            // Even though only positive integer values to valid to Print Schema,
                            // we want the object to report whatever integer value is specified.
                            propValue = intValue;
                        }
                    }
                    else if (map.PropType == PTPropValueTypes.EnumStringValue)
                    {
                        string stringValue;
                        bool bInPrivateNamespace;

                        if (propertyName == PrintSchemaTags.Framework.OptionNameProperty)
                        {
                            // Special handling of "OptionName"
                            stringValue = FeatureNode.GetOptionName(out bInPrivateNamespace);
                        }
                        else
                        {
                            // For other properties, retrieves the localname of the QName value
                            stringValue = FeatureNode.GetOptionPropertyStdStringValue(
                                              propertyName,
                                              out bInPrivateNamespace);
                        }

                        if (stringValue != null)
                        {
                            // non-Null stringValue must be in standard namespace already.

                            // Try to map the OptionName or ScoredProperty value to standard enum.
                            // The match function excludes the first special "Unknown" or "Unspecified" enum value.
                            int enumValue = PrintSchemaMapper.SchemaNameToEnumValueWithArray(
                                                                  map.PropEnumStrings,
                                                                  map.PropEnumValues,
                                                                  stringValue);

                            if (enumValue > 0)
                            {
                                propValue = enumValue;
                            }
                            else
                            {
                                // Can't find a matching enum value to the public stringValue.
                                // It could be a new public value defined in new version of schema.
                                propValue = PrintSchema.EnumUnknownValue;
                            }
                        }
                        else if (bInPrivateNamespace)
                        {
                            // We were able to find an option name string or property value string
                            // but it's in a private namespace.
                            propValue = PrintSchema.EnumUnknownValue;
                        }
                    }
                    else if (map.PropType == PTPropValueTypes.IntParamRefValue)
                    {
                        string paramName = FeatureNode.GetOptionPropertyParamRefName(map.ParamPropName);

                        // Verify the PrintTicket's ParamRef name matches to reigstered Print Schema name
                        if (paramName == map.ParamRefName)
                        {
                            propValue = map.Parameter.IntValue;
                        }
                    }
                    else
                    {
                        #if _DEBUG
                        throw new InvalidOperationException("_DEBUG: unknown property value type");
                        #endif
                    }
                }

                return propValue;
            }
            set
            {
                PTPropertyMapEntry map = LookupPropertyMap(propertyName);

                // "value" must be verified to be in range before derived feature class
                // calls this base class property setter.

                // Use the indexer to get the getter behavior. This is necessary for cases that
                // client is setting the property value without ever reading it.
                if (this[propertyName] == value)
                    return;

                if (FeatureNode == null)
                {
                    // The PrintTicket doesn't have the feature element, so we need to create one.
                    XmlElement parentElement = null;

                    if (_parentFeature != null)
                    {
                        // This is a sub-feature, so if the parent feature element is NOT in XML,
                        // we need to create the parent element in XML PrintTicket first.
                        if (_parentFeature.FeatureNode == null)
                        {
                            PTFeatureNode.CreateFeatureNode(_parentFeature,
                                                            this._ownerPrintTicket.XmlDoc.DocumentElement);
                        }

                        PTFeatureNode parentFeatureNode = _parentFeature.FeatureNode;

                        if (parentFeatureNode != null)
                        {
                            parentElement = parentFeatureNode.FeatureElement;
                        }
                    }
                    else
                    {
                        // This is a root-feature, so the feature element will be created at root level.
                        parentElement = this._ownerPrintTicket.XmlDoc.DocumentElement;
                    }

                    if (parentElement != null)
                    {
                        PTFeatureNode.CreateFeatureNode(this, parentElement);
                    }
                }

                if (map.PropType == PTPropValueTypes.PositiveIntValue)
                {
                    FeatureNode.SetOptionPropertyIntValue(propertyName, value);
                }
                else if (map.PropType == PTPropValueTypes.EnumStringValue)
                {
                    // Map the client specified enum-value into a standard string
                    string stringValue = PrintSchemaMapper.EnumValueToSchemaNameWithArray(
                                                               map.PropEnumStrings,
                                                               map.PropEnumValues,
                                                               value);

                    #if _DEBUG
                    // stringValue should never be null since derived class must verify "value"
                    if (stringValue == null)
                    {
                        throw new InvalidOperationException("_DEBUG: stringValue should never be null here");
                    }
                    #endif

                    if (propertyName == PrintSchemaTags.Framework.OptionNameProperty)
                    {
                        // Special handling of "OptionName"
                        FeatureNode.SetOptionName(stringValue);
                    }
                    else
                    {
                        // Use the localname to set the QName value of the ScoredProperty
                        FeatureNode.SetOptionPropertyStdStringValue(propertyName, stringValue);
                    }
                }
                else if (map.PropType == PTPropValueTypes.IntParamRefValue)
                {
                    // First create/set the ParameterRef element under option's ScoredProperty element
                    FeatureNode.SetOptionPropertyParamRefName(map.ParamPropName, map.ParamRefName);

                    // Then create/set the matching ParameterInit element under root
                    map.Parameter.IntValue = value;
                }
                else
                {
                    #if _DEBUG
                    throw new InvalidOperationException("_DEBUG: unknown property value type");
                    #endif
                }
            }
        }

        #endregion Internal Properties

        #region Internal Fields

        internal InternalPrintTicket _ownerPrintTicket;
        internal PrintTicketFeature   _parentFeature;

        // Following internal fields should be initialized by subclass constructor
        internal string               _featureName;
        internal PTPropertyMapEntry[] _propertyMaps;

        #endregion Internal Fields

        #region Private Methods

        private PTPropertyMapEntry LookupPropertyMap(string propertyName)
        {
            PTPropertyMapEntry map = null;

            for (int i=0; i<_propertyMaps.Length; i++)
            {
                if (_propertyMaps[i].PropName == propertyName)
                {
                    map = _propertyMaps[i];
                    break;
                }
            }

            #if _DEBUG
            if (map == null)
            {
                throw new InvalidOperationException("_DEBUG: LookupPropertyMap should never return null");
            }
            #endif
            return map;
        }

        #endregion Private Methods
    }

    internal class PTFeatureNode
    {
        #region Constructors

        private PTFeatureNode(PrintTicketFeature ownerFeature, XmlElement featureElement)
        {
            this._ownerFeature = ownerFeature;
            this._featureElement = featureElement;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Creates a new instance of PTFeatureNode if the PrintTicket contains a feature element
        /// node for the specified feature. If the feature element doesn't exist in the PrintTicket,
        /// no new instance will be created and null will be returned.
        /// </summary>
        public static PTFeatureNode GetFeatureNode(PrintTicketFeature ptFeature,
                                                   XmlElement parentElement)
        {
            InternalPrintTicket pt = ptFeature._ownerPrintTicket;
            PTFeatureNode featureNode = null;

            // Get the feature XML element
            XmlElement featureElement = PrintTicketEditor.GetSchemaElementWithNameAttr(pt,
                                                            parentElement,
                                                            PrintSchemaTags.Framework.Feature,
                                                            ptFeature._featureName);

            if (featureElement != null)
            {
                featureNode = new PTFeatureNode(ptFeature, featureElement);
            }

            return featureNode;
        }

        /// <summary>
        /// Adds a new feature element in the PrintTicket XML for the specified feature.
        /// </summary>
        public static void CreateFeatureNode(PrintTicketFeature ptFeature,
                                             XmlElement parentElement)
        {
            InternalPrintTicket pt = ptFeature._ownerPrintTicket;

            // Add the feature XML element
            PrintTicketEditor.AddSchemaElementWithNameAttr(pt,
                                                           parentElement,
                                                           PrintSchemaTags.Framework.Feature,
                                                           ptFeature._featureName);
        }

        /// <summary>
        /// Gets the feature's first option's name. Null is returned if the feature has no option child
        /// or option child has no name.
        /// </summary>
        public string GetOptionName(out bool bInPrivateNamespace)
        {
            InternalPrintTicket pt = this.OwnerFeature._ownerPrintTicket;
            string optionLocalName = null;

            bInPrivateNamespace = false;

            // Gets the feature's first option child element
            XmlElement optionNode = GetFirstOption();

            if (optionNode != null)
            {
                string optionName = optionNode.GetAttribute(PrintSchemaTags.Framework.NameAttr,
                                                            PrintSchemaNamespaces.FrameworkAttrForXmlDOM);

                // XmlElement.GetAttribute returns empty string when the attribute is not found.
                // The option name must be a QName in our standard keyword namespace.
                if (optionName != null)
                {
                    if (XmlDocQName.GetURI(pt.XmlDoc, optionName) == PrintSchemaNamespaces.StandardKeywordSet)
                    {
                        optionLocalName = XmlDocQName.GetLocalName(optionName);
                    }
                    else
                    {
                        // We could find an option name but it's not in public namespace.
                        bInPrivateNamespace = true;
                    }
                }
            }

            return optionLocalName;
        }

        /// <summary>
        /// Sets the feature's first option's name. If the feature has no option child, a new option
        /// child will be created with the specified name.
        /// </summary>
        /// <remarks>
        /// Even though OptionName has the short-form of being specified as the "name" XML attribute,
        /// we treat it same as other ScoredProperties so setting a new OptionName doesn't affect the
        /// option's other ScoredProperties.
        /// </remarks>
        public void SetOptionName(string optionName)
        {
            InternalPrintTicket pt = this.OwnerFeature._ownerPrintTicket;

            XmlElement optionNode = GetFirstOption();

            // If an option element is already present, we will change its "name" XML attribute.
            // Otherwise we need to add an option element first.
            if (optionNode == null)
            {
                optionNode = PrintTicketEditor.AddSchemaElementWithNameAttr(pt,
                                                 this.FeatureElement,
                                                 PrintSchemaTags.Framework.Option,
                                                 null);
            }

            optionNode.SetAttribute(PrintSchemaTags.Framework.NameAttr,
                                    PrintSchemaNamespaces.FrameworkAttrForXmlDOM,
                                    XmlDocQName.GetQName(pt.XmlDoc,
                                                         PrintSchemaNamespaces.StandardKeywordSet,
                                                         optionName));
        }

        /// <summary>
        /// Gets the feature's first option's ScoredProperty integer value. False is returned if
        /// the feature has no option child or option child doesn't have the specified ScoredProperty
        /// or the ScoredProperty doesn't have a valid integer value.
        /// </summary>
        public bool GetOptionPropertyIntValue(string propertyName, out int value)
        {
            bool found = false;
            value = 0;

            XmlElement optionNode = GetFirstOption();

            if (optionNode == null)
                return found;

            string valueText = GetOptionPropertyValueText(optionNode, propertyName);

            if (valueText == null)
                return found;

            try
            {
                value = XmlConvertHelper.ConvertStringToInt32(valueText);
                found = true;
            }
            // We want to catch internal FormatException to skip recoverable XML content syntax error
            #pragma warning suppress 56502
            #if _DEBUG
            catch (FormatException e)
            #else
            catch (FormatException)
            #endif
            {
                #if _DEBUG
                Trace.WriteLine("-Warning- ignore invalid property value '" + valueText +
                                "' for feature '" + this.OwnerFeature._featureName +
                                "' property '" + propertyName + "' : " + e.Message);
                #endif
            }

            return found;
        }

        /// <summary>
        /// Sets the feature's first option's ScoredProperty to the integer value. If the feature has
        /// no option child, a new option child will be created. If the option child already exists,
        /// the existing ScoredProperty will be removed and a new ScoredProperty will be re-added.
        /// </summary>
        public void SetOptionPropertyIntValue(string propertyName, int value)
        {
            InternalPrintTicket pt = this.OwnerFeature._ownerPrintTicket;

            XmlElement option = GetFirstOption();

            // If an option element is already present, we will add scored property under that.
            // Otherwise we need to add an option element first.
            if (option == null)
            {
                option = PrintTicketEditor.AddSchemaElementWithNameAttr(pt,
                                                      this.FeatureElement,
                                                      PrintSchemaTags.Framework.Option,
                                                      null);
            }

            // If the ScoredProperty already exists, we will remove it and add it back to make
            // sure the resulting ScoredProperty doesn't have unexpected content.
            PrintTicketEditor.RemoveAllSchemaElementsWithNameAttr(pt,
                                                option,
                                                PrintSchemaTags.Framework.ScoredProperty,
                                                propertyName);

            XmlElement property = PrintTicketEditor.AddSchemaElementWithNameAttr(pt,
                                                      option,
                                                      PrintSchemaTags.Framework.ScoredProperty,
                                                      propertyName);

            XmlElement valueNode = PrintTicketEditor.AddSchemaElementWithNameAttr(pt,
                                                                property,
                                                                PrintSchemaTags.Framework.Value,
                                                                null);

            // set xsi:type attribute
            PrintTicketEditor.SetXsiTypeAttr(pt, valueNode, PrintSchemaXsiTypes.Integer);

            valueNode.InnerText = value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the feature's first option's ScoredProperty QName value's localname part. Null
        /// is returned if the feature has no option child or option child doesn't have the specified
        /// ScoredProperty or the ScoredProperty doesn't have a QName value in our standard namespace.
        /// </summary>
        public string GetOptionPropertyStdStringValue(string propertyName, out bool bInPrivateNamespace)
        {
            InternalPrintTicket pt = this.OwnerFeature._ownerPrintTicket;
            string stdValue = null;

            bInPrivateNamespace = false;

            XmlElement optionNode = GetFirstOption();

            if (optionNode == null)
                return stdValue;

            string valueText = GetOptionPropertyValueText(optionNode, propertyName);

            // ScoredProperty's standard string value must be in our standard keyword namespace
            if (valueText != null)
            {
                if (XmlDocQName.GetURI(pt.XmlDoc, valueText) == PrintSchemaNamespaces.StandardKeywordSet)
                {
                    stdValue = XmlDocQName.GetLocalName(valueText);
                }
                else
                {
                    // We could find a property value string but it's not in public namespace.
                    bInPrivateNamespace = true;
                }
            }

            return stdValue;
        }

        /// Sets the feature's first option's ScoredProperty to the QName value with the given
        /// localname. If the feature has no option child, a new option child will be created.
        /// If the option child already exists, the existing ScoredProperty will be removed and
        /// a new ScoredProperty will be re-added.
        public void SetOptionPropertyStdStringValue(string propertyName, string stdValue)
        {
            InternalPrintTicket pt = this.OwnerFeature._ownerPrintTicket;

            XmlElement option = GetFirstOption();

            // If an option element is already present, we will add scored property under that.
            // Otherwise we need to add an option element first.
            if (option == null)
            {
                option = PrintTicketEditor.AddSchemaElementWithNameAttr(pt,
                                                      this.FeatureElement,
                                                      PrintSchemaTags.Framework.Option,
                                                      null);
            }

            // If the ScoredProperty already exists, we will remove it and add it back to make
            // sure the resulting ScoredProperty doesn't have unexpected content.
            PrintTicketEditor.RemoveAllSchemaElementsWithNameAttr(pt,
                                                option,
                                                PrintSchemaTags.Framework.ScoredProperty,
                                                propertyName);

            XmlElement property = PrintTicketEditor.AddSchemaElementWithNameAttr(pt,
                                                      option,
                                                      PrintSchemaTags.Framework.ScoredProperty,
                                                      propertyName);

            XmlElement valueNode = PrintTicketEditor.AddSchemaElementWithNameAttr(pt,
                                                                property,
                                                                PrintSchemaTags.Framework.Value,
                                                                null);

            // set xsi:type attribute
            PrintTicketEditor.SetXsiTypeAttr(pt, valueNode, PrintSchemaXsiTypes.QName);

            valueNode.InnerText = XmlDocQName.GetQName(pt.XmlDoc,
                                                       PrintSchemaNamespaces.StandardKeywordSet,
                                                       stdValue);
        }

        /// <summary>
        /// Gets the feature's first option's ScoredProperty value's ParameterRef name. Null
        /// is returned if the feature has no option child or option child doesn't have the specified
        /// ScoredProperty or the ScoredProperty doesn't have a ParameterRef child in our standard namespace.
        /// </summary>
        public string GetOptionPropertyParamRefName(string propertyName)
        {
            InternalPrintTicket pt = this.OwnerFeature._ownerPrintTicket;
            string refName = null;

            XmlElement optionNode = GetFirstOption();

            if (optionNode == null)
                return refName;

            // Gets the ScoredProperty element
            XmlElement propertyNode = PrintTicketEditor.GetSchemaElementWithNameAttr(pt,
                                                          optionNode,
                                                          PrintSchemaTags.Framework.ScoredProperty,
                                                          propertyName);

            if (propertyNode == null)
                return refName;

            // Gets the ScoredProperty element's ParameterRef child element
            XmlElement refNode = PrintTicketEditor.GetSchemaElementWithNameAttr(pt,
                                                       propertyNode,
                                                       PrintSchemaTags.Framework.ParameterRef,
                                                       null);

            if (refNode == null)
                return refName;

            string fullRefName = refNode.GetAttribute(PrintSchemaTags.Framework.NameAttr,
                                                      PrintSchemaNamespaces.FrameworkAttrForXmlDOM);

            // XmlElement.GetAttribute returns empty string when the attribute is not found.
            if ((fullRefName != null) &&
                (fullRefName.Length != 0) &&
                (XmlDocQName.GetURI(pt.XmlDoc, fullRefName) == PrintSchemaNamespaces.StandardKeywordSet))
            {
                refName = XmlDocQName.GetLocalName(fullRefName);
            }

            return refName;
        }

        /// <summary>
        /// Sets the feature's first option's ScoredProperty value's ParameterRef name.
        /// If the feature has no option child, a new option child will be created. If the option
        /// child already exists, the existing ScoredProperty will be removed and a new
        /// ScoredProperty will be re-added.
        /// </summary>
        /// <remarks>
        /// This function only sets the ParameterRef element's "name" XML attribute. It doesn't
        /// update the ParameterInit element.
        /// </remarks>
        public void SetOptionPropertyParamRefName(string propertyName, string paramRefName)
        {
            InternalPrintTicket pt = this.OwnerFeature._ownerPrintTicket;

            XmlElement option = GetFirstOption();

            // If an option element is already present, we will add scored property under that.
            // Otherwise we need to add an option element first.
            if (option == null)
            {
                option = PrintTicketEditor.AddSchemaElementWithNameAttr(pt,
                                                      this.FeatureElement,
                                                      PrintSchemaTags.Framework.Option,
                                                      null);
            }

            // If the ScoredProperty already exists, we will remove it and add it back to make
            // sure the resulting ScoredProperty doesn't have unexpected content.
            PrintTicketEditor.RemoveAllSchemaElementsWithNameAttr(pt,
                                                option,
                                                PrintSchemaTags.Framework.ScoredProperty,
                                                propertyName);

            XmlElement property = PrintTicketEditor.AddSchemaElementWithNameAttr(pt,
                                                      option,
                                                      PrintSchemaTags.Framework.ScoredProperty,
                                                      propertyName);

            PrintTicketEditor.AddSchemaElementWithNameAttr(pt,
                                                           property,
                                                           PrintSchemaTags.Framework.ParameterRef,
                                                           paramRefName);
        }

        /// <summary>
        /// Gets the first option child of this feature. Null is returned if no option child is found.
        /// </summary>
        public XmlElement GetFirstOption()
        {
            InternalPrintTicket pt = this.OwnerFeature._ownerPrintTicket;

            return PrintTicketEditor.GetSchemaElementWithNameAttr(pt,
                                                                this.FeatureElement,
                                                                PrintSchemaTags.Framework.Option,
                                                                null);
        }

        #endregion Public Methods

        #region Public Properties

        public PrintTicketFeature OwnerFeature
        {
            get
            {
                return _ownerFeature;
            }
        }

        public XmlElement FeatureElement
        {
            get
            {
                return _featureElement;
            }
        }

        #endregion Public Properties

        #region Private Methods

        /// <summary>
        /// Gets the text string of the parent option ScoredProperty's Value child element
        /// </summary>
        private string GetOptionPropertyValueText(XmlElement parentOption, string propertyName)
        {
            InternalPrintTicket pt = this.OwnerFeature._ownerPrintTicket;

            // Gets the ScoredProperty element
            XmlElement propertyNode = PrintTicketEditor.GetSchemaElementWithNameAttr(pt,
                                                          parentOption,
                                                          PrintSchemaTags.Framework.ScoredProperty,
                                                          propertyName);

            if (propertyNode == null)
                return null;

            // Gets the ScoredProperty element's Value child element
            XmlElement valueNode = PrintTicketEditor.GetSchemaElementWithNameAttr(pt,
                                                       propertyNode,
                                                       PrintSchemaTags.Framework.Value,
                                                       null);

            // Verifies the Value child element does exist and has a child Text element
            if ((valueNode == null) ||
                (valueNode.FirstChild == null) ||
                (valueNode.FirstChild.NodeType != XmlNodeType.Text))
            {
                #if _DEBUG
                Trace.WriteLine("-Warning- feature '" + this.OwnerFeature._featureName +
                                "' property '" + propertyName + "' is missing value element" +
                                " or value element is missing text child");
                #endif

                return null;
            }

            // Returns the Valuld element's child Text element's text string
            return valueNode.FirstChild.Value;
        }

        #endregion Private Methods

        #region Private Fields

        private PrintTicketFeature _ownerFeature;
        private XmlElement         _featureElement;

        #endregion Private Fields
    }

    /// <summary>
    /// ScoredProperty value data types
    /// </summary>
    internal enum PTPropValueTypes
    {
        EnumStringValue,
        IntParamRefValue,
        PositiveIntValue,
    }

    internal class PTPropertyMapEntry
    {
        #region Constructors

        /// <summary>
        /// Constructor for PositiveIntValue-type property
        /// </summary>
        public PTPropertyMapEntry(PrintTicketFeature ownerFeature,
                                  string propName,
                                  PTPropValueTypes propType)
        {
            this.OwnerFeature = ownerFeature;
            this.PropName = propName;
            this.PropType = propType;
        }

        /// <summary>
        /// Constructor for EnumStringValue-type property
        /// </summary>
        public PTPropertyMapEntry(PrintTicketFeature ownerFeature,
                                  string propName,
                                  PTPropValueTypes propType,
                                  string[] enumStrings,
                                  int[] enumValues)
        {
            this.OwnerFeature = ownerFeature;
            this.PropName = propName;
            this.PropType = propType;
            this.PropEnumStrings = enumStrings;
            this.PropEnumValues = enumValues;
        }

        /// <summary>
        /// Constructor for IntParamRefValue-type property
        /// </summary>
        public PTPropertyMapEntry(PrintTicketFeature ownerFeature,
                                  string propName,
                                  PTPropValueTypes propType,
                                  string paramPropName,
                                  string paramRefName)
        {
            this.OwnerFeature = ownerFeature;
            this.PropName = propName;
            this.PropType = propType;
            this.ParamPropName = paramPropName;
            this.ParamRefName = paramRefName;
        }

        #endregion Constructors

        #region Public Properties

        public PrintTicketParameter Parameter
        {
            get
            {
                #if _DEBUG
                if (PropType != PTPropValueTypes.IntParamRefValue)
                {
                    throw new InvalidOperationException("_DEBUG: Invalid property value type");
                }
                #endif
                // We don't do object caching here. Always return a new object.
                return new PrintTicketParameter(OwnerFeature._ownerPrintTicket,
                                                ParamRefName,
                                                PrintTicketParamTypes.Parameter,
                                                PrintTicketParamValueTypes.IntValue);
            }
        }

        #endregion Public Properties

        #region Public Fields

        public PrintTicketFeature  OwnerFeature;
        public string            PropName;
        public PTPropValueTypes  PropType;

        // These 2 arrays are used to map between Print Schema standard property string values
        // and their corresponding enum values
        public string[]          PropEnumStrings;
        public int[]             PropEnumValues;

        // These 2 are only needed for parameterized property. The first is the XML property name,
        // the second is the ParameterRef "name" attribute value.
        public string            ParamPropName;
        public string            ParamRefName;

        #endregion Public Fields
    }

    /// <summary>
    /// Parameter types
    /// </summary>
    internal enum PrintTicketParamTypes
    {
        Parameter,
        RootProperty,
    }

    /// <summary>
    /// Parameter value data types
    /// </summary>
    internal enum PrintTicketParamValueTypes
    {
        StringValue,
        IntValue,
    }

    /// <internalonly/>
    /// <summary>
    /// Do not use.
    /// Base class of a PrintTicket parameter or root property.
    /// </summary>
    internal class PrintTicketParameter
    {
        // Public Print Schema parameter initializers should create a derived class to expose its
        // own properties. Internal PrintTicket parameter initializers can just use the base class.
        #region Constructors

        internal PrintTicketParameter(InternalPrintTicket        ownerPrintTicket,
                                      string                     paramName,
                                      PrintTicketParamTypes      paramType,
                                      PrintTicketParamValueTypes paramValueType)
        {
            this._ownerPrintTicket = ownerPrintTicket;
            this._parameterName = paramName;
            this._parameterType = paramType;
            this._parameterValueType = paramValueType;

            if (_parameterType == PrintTicketParamTypes.Parameter)
            {
                _parameterNodeTagName = PrintSchemaTags.Framework.ParameterInit;
            }
            else if (_parameterType == PrintTicketParamTypes.RootProperty)
            {
                _parameterNodeTagName = PrintSchemaTags.Framework.Property;
            }
            else
            {
                #if _DEBUG
                throw new InvalidOperationException("_DEBUG: invalid paramType.");
                #endif
            }
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Clears this parameter's setting in the Print Ticket.
        /// </summary>
        /// <remarks>
        /// After calling this method, the parameter's setting will become unspecified, and the Print Ticket XML
        /// will no longer contain the XML element for the parameter.
        /// </remarks>
        public void ClearSetting()
        {
            // Both ParameterInit and root Property XML elements are at the Print Ticket XML root level.
            // The XML element tag name varies depending on the type of ParameterInit vs. root Property.
            PrintTicketEditor.RemoveAllSchemaElementsWithNameAttr(_ownerPrintTicket,
                                                                  _ownerPrintTicket.XmlDoc.DocumentElement,
                                                                  _parameterNodeTagName,
                                                                  _parameterName);

            SettingClearCallback();
        }

        #endregion Public Methods

        #region Internal Methods

        /// <summary>
        /// callback function for derived types to issue property change notification for data-binding.
        /// </summary>
        internal virtual void SettingClearCallback()
        {
            // nothing to do in the base class
        }

        #endregion Internal Methods

        #region Internal Properties

        internal PrintTicketParameterNode ParameterNode
        {
            get
            {
                PrintTicketParameterNode parameterNode = PrintTicketParameterNode.GetParameterNode(this);

                return parameterNode;
            }
        }

        /// <summary>
        /// Property to get/set parameter int value
        /// </summary>
        internal int IntValue
        {
            get
            {
                #if _DEBUG
                if (_parameterValueType != PrintTicketParamValueTypes.IntValue)
                {
                    throw new InvalidOperationException("_DEBUG: Parameter value type mismatch");
                }
                #endif

                #if _DEBUG
                Trace.WriteLine("-Trace- reading " + this._parameterName);
                #endif

                // Sets the default value first.
                int intValue = PrintSchema.UnspecifiedIntValue;

                // Must use the property here to invoke the code that locates the XML element
                if (ParameterNode != null)
                {
                    int paramValue;

                    if (ParameterNode.GetIntValue(out paramValue))
                    {
                        intValue = paramValue;
                    }
                }

                return intValue;
            }
            set
            {
                #if _DEBUG
                if (_parameterValueType != PrintTicketParamValueTypes.IntValue)
                {
                    throw new InvalidOperationException("_DEBUG: Parameter value type mismatch");
                }
                #endif

                // "value" must be verified to be in range before derived feature class
                // calls this base class property setter.

                // Use the property to get the getter behavior. This is necessary for cases that
                // client is setting the value without ever reading it.
                if (IntValue == value)
                    return;

                if (ParameterNode == null)
                {
                    // The PrintTicket doesn't have the parameter element, so we need to create one.
                    PrintTicketParameterNode.CreateParameterNode(this);
                }

                if (ParameterNode != null)
                {
                    ParameterNode.SetIntValue(value);
                }
            }
        }

        /// <summary>
        /// Property to get/set parameter string value
        /// </summary>
        internal string StringValue
        {
            get
            {
                #if _DEBUG
                if (_parameterValueType != PrintTicketParamValueTypes.StringValue)
                {
                    throw new InvalidOperationException("_DEBUG: Parameter value type mismatch");
                }
                #endif

                #if _DEBUG
                Trace.WriteLine("-Trace- reading " + this._parameterName);
                #endif

                // Sets the default value first.
                string stringValue = "";

                // Must use the property here to invoke the code that locates the XML element
                if (ParameterNode != null)
                {
                    string paramValue;

                    if (ParameterNode.GetStringValue(out paramValue))
                    {
                        stringValue = paramValue;
                    }
                }

                return stringValue;
            }
            set
            {
                #if _DEBUG
                if (_parameterValueType != PrintTicketParamValueTypes.StringValue)
                {
                    throw new InvalidOperationException("_DEBUG: Parameter value type mismatch");
                }
                #endif

                if (ParameterNode == null)
                {
                    // The PrintTicket doesn't have the parameter element, so we need to create one.
                    PrintTicketParameterNode.CreateParameterNode(this);
                }

                if (ParameterNode != null)
                {
                    ParameterNode.SetStringValue(value, PrintSchemaXsiTypes.String);
                }
            }
        }

        #endregion Internal Properties

        #region Internal Fields

        // Following internal fields should be initialized by subclass constructor
        internal InternalPrintTicket        _ownerPrintTicket;
        internal string                     _parameterName;
        internal PrintTicketParamTypes      _parameterType;
        internal PrintTicketParamValueTypes _parameterValueType;
        internal string                     _parameterNodeTagName;

        #endregion Internal Fields
    }

    internal class PrintTicketParameterNode
    {
        #region Constructors

        private PrintTicketParameterNode(PrintTicketParameter ownerParameter, XmlElement parameterElement)
        {
            this._ownerParameter = ownerParameter;
            this._parameterElement = parameterElement;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Creates a new instance of PrintTicketParameterNode if the PrintTicket contains a parameter element
        /// node for the specified parameter. If the parameter element doesn't exist in the PrintTicket,
        /// no new instance will be created and null will be returned.
        /// </summary>
        public static PrintTicketParameterNode GetParameterNode(PrintTicketParameter ptParameter)
        {
            InternalPrintTicket pt = ptParameter._ownerPrintTicket;
            PrintTicketParameterNode parameterNode = null;

            // Get the parameter XML element at root level
            XmlElement parameterElement = PrintTicketEditor.GetSchemaElementWithNameAttr(pt,
                                                            pt.XmlDoc.DocumentElement,
                                                            ptParameter._parameterNodeTagName,
                                                            ptParameter._parameterName);

            if (parameterElement != null)
            {
                parameterNode = new PrintTicketParameterNode(ptParameter, parameterElement);
            }

            return parameterNode;
        }

        /// <summary>
        /// Adds a new parameter element in the PrintTicket XML for the specified parameter.
        /// </summary>
        public static void CreateParameterNode(PrintTicketParameter ptParameter)
        {
            InternalPrintTicket pt = ptParameter._ownerPrintTicket;

            // Add the parameter XML element at root level
            PrintTicketEditor.AddSchemaElementWithNameAttr(pt,
                                                           pt.XmlDoc.DocumentElement,
                                                           ptParameter._parameterNodeTagName,
                                                           ptParameter._parameterName);
        }

        /// <summary>
        /// Gets the parameter's integer value. False is returned if the parameter element doesn't
        /// have a valid integer value.
        /// </summary>
        public bool GetIntValue(out int value)
        {
            bool found = false;
            string valueText;

            value = 0;

            if (!GetStringValue(out valueText))
                return found;

            try
            {
                value = XmlConvertHelper.ConvertStringToInt32(valueText);
                found = true;
            }
            // We want to catch internal FormatException to skip recoverable XML content syntax error
            #pragma warning suppress 56502
            #if _DEBUG
            catch (FormatException e)
            #else
            catch (FormatException)
            #endif
            {
                #if _DEBUG
                Trace.WriteLine("-Warning- ignore invalid parameter value '" + valueText +
                                "' for parameter '" + this.OwnerParameter._parameterName +
                                "' : " + e.Message);
                #endif
            }

            return found;
        }

        /// <summary>
        /// Gets the parameter's string value. False is returned if the parameter element doesn't
        /// have a string value.
        /// </summary>
        public bool GetStringValue(out string value)
        {
            InternalPrintTicket pt = this.OwnerParameter._ownerPrintTicket;

            value = "";

            // Gets the parameter element's Value child element
            XmlElement valueNode = PrintTicketEditor.GetSchemaElementWithNameAttr(pt,
                                                       this.ParameterElement,
                                                       PrintSchemaTags.Framework.Value,
                                                       null);

            // Verifies the Value child element does exist and has a child Text element
            if ((valueNode == null) ||
                (valueNode.FirstChild == null) ||
                (valueNode.FirstChild.NodeType != XmlNodeType.Text))
            {
                #if _DEBUG
                Trace.WriteLine("-Warning- parameter '" + this.OwnerParameter._parameterName +
                                "' is missing value element or value element is missing text child");
                #endif

                return false;
            }

            value = valueNode.FirstChild.Value;
            return true;
        }

        /// <summary>
        /// Sets the parameter's integer value.
        /// </summary>
        public XmlElement SetIntValue(int value)
        {
            XmlElement valueNode = SetStringValue(value.ToString(CultureInfo.InvariantCulture),
                                                  PrintSchemaXsiTypes.Integer);

            return valueNode;
        }

        /// <summary>
        /// Sets the parameter's string value.
        /// </summary>
        public XmlElement SetStringValue(string value, string xsiType)
        {
            InternalPrintTicket pt = this.OwnerParameter._ownerPrintTicket;

            // We remove the Value child (if it exists) and add it back to make
            // sure the resulting Value child doesn't have unexpected content.
            PrintTicketEditor.RemoveAllSchemaElementsWithNameAttr(pt,
                                                this.ParameterElement,
                                                PrintSchemaTags.Framework.Value,
                                                null);

            XmlElement valueNode = PrintTicketEditor.AddSchemaElementWithNameAttr(pt,
                                                                this.ParameterElement,
                                                                PrintSchemaTags.Framework.Value,
                                                                null);

            // set xsi:type attribute
            PrintTicketEditor.SetXsiTypeAttr(pt, valueNode, xsiType);

            valueNode.InnerText = value;

            return valueNode;
        }

        #endregion Public Methods

        #region Public Properties

        public PrintTicketParameter OwnerParameter
        {
            get
            {
                return _ownerParameter;
            }
        }

        public XmlElement ParameterElement
        {
            get
            {
                return _parameterElement;
            }
        }

        #endregion Public Properties

        #region Private Fields

        private PrintTicketParameter _ownerParameter;
        private XmlElement           _parameterElement;

        #endregion Private Fields
    }
}