// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   Class that serializes and deserializes Styles.
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
#endif

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    /// <summary>
    ///     Class that knows how to serialize and deserialize Style objects
    /// </summary>
    internal class XamlStyleSerializer : XamlSerializer
    {
#if PBTCOMPILER 

        #region Construction

        /// <summary>
        ///     Constructor for XamlStyleSerializer
        /// </summary>
        public XamlStyleSerializer() : base()
        {
        }

        internal XamlStyleSerializer(ParserHooks parserHooks) : base()
        {
            _parserHooks = parserHooks;
        }

        private ParserHooks   _parserHooks = null;

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
            StyleXamlParser styleParser = new StyleXamlParser(tokenReader, context);
            styleParser.BamlRecordWriter = bamlWriter;
            styleParser.ParserHooks = _parserHooks;


            // Process the xamlNode that is passed in so that the <Style> element is written to baml
            styleParser.WriteElementStart((XamlElementStartNode)xamlNode);

            // Parse the entire Style section now, writing everything out directly to BAML.
            styleParser.Parse();
       }
#else


        /// <summary>
        ///   If the Style represented by a group of baml records is stored in a dictionary, this
        ///   method will extract the key used for this dictionary from the passed
        ///   collection of baml records.  For Style, this is the type of the first element record
        ///   in the record collection, skipping over the Style element itself.
        /// </summary>
        internal override object GetDictionaryKey(BamlRecord startRecord,  ParserContext parserContext)
        {
            Type       styleTargetType = Style.DefaultTargetType;
            bool       styleTargetTypeSet = false;
            object     targetType = null;
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
                        // save the type ID of the first element (i.e. <Style>)
                        ownerTypeId = elementStart.TypeId;
                    }
                    else if (numberOfElements == 2)
                    {
                        styleTargetType = parserContext.MapTable.GetTypeFromId(elementStart.TypeId);
                        styleTargetTypeSet = true;
                        break;
                    }
                }
                else if (record.RecordType == BamlRecordType.Property && numberOfElements == 1)
                {
                    // look for the TargetType property on the <Style> element
                    BamlPropertyRecord propertyRecord = record as BamlPropertyRecord;
                    if (parserContext.MapTable.DoesAttributeMatch(propertyRecord.AttributeId, ownerTypeId, TargetTypePropertyName))
                    {
                        targetType = parserContext.XamlTypeMapper.GetDictionaryKey(propertyRecord.Value, parserContext);
                    }
                }
                else if (record.RecordType == BamlRecordType.PropertyComplexStart ||
                         record.RecordType == BamlRecordType.PropertyIListStart)
                {
                    // We didn't find the second element before a complex property like
                    // Style.Triggers, so return the default style target type:  FrameworkElement.
                    break;
                }
                record = record.Next;
            }

            if (targetType == null)
            {
                if (!styleTargetTypeSet)
                {
                    ThrowException(SRID.StyleNoDictionaryKey,
                                   parserContext.LineNumber,
                                   parserContext.LinePosition);
                }
                return styleTargetType;
            }
            else
                return targetType;
        }

                // Helper to insert line and position numbers into message, if they are present
        void ThrowException(
             string id,
             int  lineNumber,
             int  linePosition)
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
                                  lineNumber.ToString(CultureInfo.CurrentCulture),
                                  linePosition.ToString(CultureInfo.CurrentCulture));
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
        internal const string StyleTagName                                  = "Style";
        internal const string TargetTypePropertyName                        = "TargetType";
        internal const string BasedOnPropertyName                           = "BasedOn";
        internal const string VisualTriggersPropertyName                    = "Triggers";
        internal const string ResourcesPropertyName                         = "Resources";
        internal const string SettersPropertyName                           = "Setters";
        internal const string VisualTriggersFullPropertyName    = StyleTagName + "." + VisualTriggersPropertyName;
        internal const string SettersFullPropertyName           = StyleTagName + "." + SettersPropertyName;
        internal const string ResourcesFullPropertyName         = StyleTagName + "." + ResourcesPropertyName;
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

