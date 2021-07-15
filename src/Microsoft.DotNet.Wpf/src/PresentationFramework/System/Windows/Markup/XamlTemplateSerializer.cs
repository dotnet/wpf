// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   Class that serializes and deserializes Templates.
//

using System;
using System.ComponentModel;

using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using MS.Utility;

#if !PBTCOMPILER
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Documents;
#endif

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    /// <summary>
    ///     Class that knows how to serialize and deserialize Template objects
    /// </summary>
    internal class XamlTemplateSerializer : XamlSerializer
    {
#if PBTCOMPILER
        #region Construction

        /// <summary>
        ///     Constructor for XamlTemplateSerializer
        /// </summary>
        public XamlTemplateSerializer() : base()
        {
        }


        internal XamlTemplateSerializer(ParserHooks parserHooks) : base()
        {
            _parserHooks = parserHooks;
        }

        private ParserHooks _parserHooks = null;

        #endregion Construction

        /// <summary>
        ///   Convert from Xaml read by a token reader into baml being written
        ///   out by a record writer.  The context gives mapping information.
        /// </summary>
        internal override void ConvertXamlToBaml (
            XamlReaderHelper             tokenReader,
            ParserContext          context,
            XamlNode               xamlNode,
            BamlRecordWriter       bamlWriter)
        {

            TemplateXamlParser templateParser = new TemplateXamlParser(tokenReader, context);
            templateParser.ParserHooks = _parserHooks;
            templateParser.BamlRecordWriter = bamlWriter;

            // Process the xamlNode that is passed in so that the <Template> element is written to baml
            templateParser.WriteElementStart((XamlElementStartNode)xamlNode);

            // Parse the entire Template section now, writing everything out directly to BAML.
            templateParser.Parse();
       }
#else
        /// <summary>
        ///   If the Template represented by a group of baml records is stored in a dictionary, this
        ///   method will extract the key used for this dictionary from the passed
        ///   collection of baml records.  For ControlTemplate, this is the styleTargetType.
        ///   For DataTemplate, this is the DataTemplateKey containing the DataType.
        /// </summary>
        internal override object GetDictionaryKey(BamlRecord startRecord,  ParserContext parserContext)
        {
            object     key = null;
            int        numberOfElements = 0;
            BamlRecord record = startRecord;
            short      ownerTypeId = 0;

            while (record != null)
            {
                if (record.RecordType == BamlRecordType.ElementStart)
                {
                    BamlElementStartRecord elementStart = record as BamlElementStartRecord;
                    if (++numberOfElements == 1)
                    {
                        // save the type ID of the first element (i.e. <ControlTemplate>)
                        ownerTypeId = elementStart.TypeId;
                    }
                    else
                    {
                        // We didn't find the key before a reading the
                        // VisualTree nodes of the template
                        break;
                    }
                }
                else if (record.RecordType == BamlRecordType.Property && numberOfElements == 1)
                {
                    // look for the TargetType property on the <ControlTemplate> element
                    // or the DataType property on the <DataTemplate> element
                    BamlPropertyRecord propertyRecord = record as BamlPropertyRecord;
                    short attributeOwnerTypeId;
                    string attributeName;
                    BamlAttributeUsage attributeUsage;
                    parserContext.MapTable.GetAttributeInfoFromId(propertyRecord.AttributeId, out attributeOwnerTypeId, out attributeName, out attributeUsage);
                    if (attributeOwnerTypeId == ownerTypeId)
                    {
                        if (attributeName == TargetTypePropertyName)
                        {
                            key = parserContext.XamlTypeMapper.GetDictionaryKey(propertyRecord.Value, parserContext);
                        }
                        else if (attributeName == DataTypePropertyName)
                        {
                            object dataType = parserContext.XamlTypeMapper.GetDictionaryKey(propertyRecord.Value, parserContext);
                            Exception ex = TemplateKey.ValidateDataType(dataType, null);
                            if (ex != null)
                            {
                                ThrowException(SRID.TemplateBadDictionaryKey,
                                               parserContext.LineNumber,
                                               parserContext.LinePosition,
                                               ex);
                            }
                            key = new DataTemplateKey(dataType);
                        }
                    }
                }
                else if (record.RecordType == BamlRecordType.PropertyComplexStart ||
                         record.RecordType == BamlRecordType.PropertyIListStart ||
                         record.RecordType == BamlRecordType.ElementEnd)
                {
                    // We didn't find the targetType before a complex property like
                    // FrameworkTemplate.VisualTree or the </ControlTemplate> tag or
                    // TableTemplate.Tree or the </TableTemplate> tag
                    break;
                }
                record = record.Next;
            }

            if (key == null)
            {
                ThrowException(SRID.StyleNoDictionaryKey,
                               parserContext.LineNumber,
                               parserContext.LinePosition,
                               null);
            }

            return key;
        }

        // Helper to insert line and position numbers into message, if they are present
        void ThrowException(
             string id,
             int  lineNumber,
             int  linePosition,
             Exception innerException)
        {
            string message = SR.Get(id);
            XamlParseException parseException;

            // Throw the appropriate execption.  If we have line numbers, then we are
            // parsing a xaml file, so throw a xaml exception.  Otherwise were are
            // parsing a baml file.
            if (lineNumber > 0)
            {
                message += " ";
                message += SR.Get(SRID.ParserLineAndOffset,
                                  lineNumber.ToString(CultureInfo.CurrentUICulture),
                                  linePosition.ToString(CultureInfo.CurrentUICulture));
                parseException = new XamlParseException(message, lineNumber, linePosition);
            }
            else
            {
                parseException = new XamlParseException(message);
            }

            throw parseException;
        }


#endif // !PBTCOMPILER


        #region Data

        // Constants used for emitting specific properties and attributes for a Style
        internal const string ControlTemplateTagName                        = "ControlTemplate";
        internal const string DataTemplateTagName                           = "DataTemplate";
        internal const string HierarchicalDataTemplateTagName               = "HierarchicalDataTemplate";
        internal const string ItemsPanelTemplateTagName                     = "ItemsPanelTemplate";
        internal const string TargetTypePropertyName                        = "TargetType";
        internal const string DataTypePropertyName                          = "DataType";
        internal const string TriggersPropertyName                          = "Triggers";
        internal const string ResourcesPropertyName                         = "Resources";
        internal const string SettersPropertyName                           = "Setters";
        internal const string ItemsSourcePropertyName                       = "ItemsSource";
        internal const string ItemTemplatePropertyName                      = "ItemTemplate";
        internal const string ItemTemplateSelectorPropertyName              = "ItemTemplateSelector";
        internal const string ItemContainerStylePropertyName                = "ItemContainerStyle";
        internal const string ItemContainerStyleSelectorPropertyName        = "ItemContainerStyleSelector";
        internal const string ItemStringFormatPropertyName                  = "ItemStringFormat";
        internal const string ItemBindingGroupPropertyName                  = "ItemBindingGroup";
        internal const string AlternationCountPropertyName                  = "AlternationCount";
        internal const string ControlTemplateTriggersFullPropertyName       = ControlTemplateTagName + "." + TriggersPropertyName;
        internal const string ControlTemplateResourcesFullPropertyName      = ControlTemplateTagName + "." + ResourcesPropertyName;
        internal const string DataTemplateTriggersFullPropertyName          = DataTemplateTagName + "." + TriggersPropertyName;
        internal const string DataTemplateResourcesFullPropertyName         = DataTemplateTagName + "." + ResourcesPropertyName;
        internal const string HierarchicalDataTemplateTriggersFullPropertyName = HierarchicalDataTemplateTagName + "." + TriggersPropertyName;
        internal const string HierarchicalDataTemplateItemsSourceFullPropertyName = HierarchicalDataTemplateTagName + "." + ItemsSourcePropertyName;
        internal const string HierarchicalDataTemplateItemTemplateFullPropertyName = HierarchicalDataTemplateTagName + "." + ItemTemplatePropertyName;
        internal const string HierarchicalDataTemplateItemTemplateSelectorFullPropertyName = HierarchicalDataTemplateTagName + "." + ItemTemplateSelectorPropertyName;
        internal const string HierarchicalDataTemplateItemContainerStyleFullPropertyName = HierarchicalDataTemplateTagName + "." + ItemContainerStylePropertyName;
        internal const string HierarchicalDataTemplateItemContainerStyleSelectorFullPropertyName = HierarchicalDataTemplateTagName + "." + ItemContainerStyleSelectorPropertyName;
        internal const string HierarchicalDataTemplateItemStringFormatFullPropertyName = HierarchicalDataTemplateTagName + "." + ItemStringFormatPropertyName;
        internal const string HierarchicalDataTemplateItemBindingGroupFullPropertyName = HierarchicalDataTemplateTagName + "." + ItemBindingGroupPropertyName;
        internal const string HierarchicalDataTemplateAlternationCountFullPropertyName = HierarchicalDataTemplateTagName + "." + AlternationCountPropertyName;
        internal const string PropertyTriggerPropertyName                   = "Property";
        internal const string PropertyTriggerValuePropertyName              = "Value";
        internal const string PropertyTriggerSourceName                     = "SourceName";
        internal const string PropertyTriggerEnterActions                   = "EnterActions";
        internal const string PropertyTriggerExitActions                    = "ExitActions";
        internal const string DataTriggerBindingPropertyName                = "Binding";
        internal const string EventTriggerEventName                         = "RoutedEvent";
        internal const string EventTriggerSourceName                          = "SourceName";
        internal const string EventTriggerActions                           = "Actions";
        internal const string MultiPropertyTriggerConditionsPropertyName    = "Conditions";
        internal const string SetterTagName                                 = "Setter";
        internal const string SetterPropertyAttributeName                   = "Property";
        internal const string SetterValueAttributeName                      = "Value";
        internal const string SetterTargetAttributeName                     = "TargetName";
        internal const string SetterEventAttributeName                      = "Event";
        internal const string SetterHandlerAttributeName                    = "Handler";
#if HANDLEDEVENTSTOO
        internal const string SetterHandledEventsTooAttributeName           = "HandledEventsToo";
#endif
        #endregion Data

    }
}


