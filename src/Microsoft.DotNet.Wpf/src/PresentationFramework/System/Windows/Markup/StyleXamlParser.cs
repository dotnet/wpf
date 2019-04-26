// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose: Class that interfaces with TokenReader and BamlWriter for
*          parsing Style
*
\***************************************************************************/

using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Collections;
using System.ComponentModel;

using System.Diagnostics;
using System.Reflection;

using MS.Utility;

#if !PBTCOMPILER

using System.Windows;
using System.Windows.Threading;

#endif

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    /***************************************************************************\
    *
    * StyleMode
    *
    * The style parser works in several modes, and tags are interpreted differently
    * depending on which mode or section of the Style markup that the parser is
    * currently interpreting.
    *
    \***************************************************************************/

    /// <summary>
    /// Handles overrides for case when Style is being built to a tree
    /// instead of compiling to a file.
    /// </summary>
    internal class StyleXamlParser : XamlParser
    {

#region Constructors

#if !PBTCOMPILER
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// Note that we are re-using the token reader, so we'll swap out the XamlParser that
        /// the token reader uses with ourself.  Then restore it when we're done parsing.
        /// </remarks>
        internal StyleXamlParser(
            XamlTreeBuilder  treeBuilder,
            XamlReaderHelper       tokenReader,
            ParserContext    parserContext) : this(tokenReader, parserContext)
        {
            _treeBuilder      = treeBuilder;
        }

#endif

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// Note that we are re-using the token reader, so we'll swap out the XamlParser that
        /// the token reader uses with ourself.  Then restore it when we're done parsing.
        /// </remarks>
        internal StyleXamlParser(
            XamlReaderHelper      tokenReader,
            ParserContext   parserContext)
        {
            TokenReader       = tokenReader;
            ParserContext     = parserContext;

            _previousXamlParser = TokenReader.ControllingXamlParser;
            TokenReader.ControllingXamlParser = this;
            _startingDepth = TokenReader.XmlReader.Depth;
        }

#endregion Constructors

#region Overrides


        /// <summary>
        /// Override of the main switch statement that processes the xaml nodes.
        /// </summary>
        /// <remarks>
        ///  We need to control when cleanup is done and when the calling parse loop
        ///  is exited, so do this here.
        /// </remarks>
        internal override void ProcessXamlNode(
               XamlNode xamlNode,
           ref bool     cleanup,
           ref bool     done)
        {
            switch(xamlNode.TokenType)
            {
                // Ignore some types of xaml nodes, since they are not
                // relevent to style parsing.
                case XamlNodeType.DocumentStart:
                case XamlNodeType.DocumentEnd:
                    break;

                case XamlNodeType.ElementEnd:
                    base.ProcessXamlNode(xamlNode, ref cleanup, ref done);
                    // If we're at the depth that we started out, then we must be done.  In that case quit
                    // and restore the XamlParser that the token reader was using before parsing styles.
                    if (_styleModeStack.Depth == 0)
                    {
                        done = true;      // Stop the style parse
                        cleanup = false;  // Don't close the stream
                        TokenReader.ControllingXamlParser = _previousXamlParser;
                    }
                    break;

                case XamlNodeType.PropertyArrayStart:
                case XamlNodeType.PropertyArrayEnd:
                case XamlNodeType.DefTag:
                    ThrowException(SRID.StyleTagNotSupported, xamlNode.TokenType.ToString(),
                                   xamlNode.LineNumber, xamlNode.LinePosition);
                    break;

                // Most nodes are handled by the base XamlParser by creating a
                // normal BamlRecord.
                default:
                    base.ProcessXamlNode(xamlNode, ref cleanup, ref done);
                    break;
            }

        }

        /// <summary>
        /// Write start of an unknown tag
        /// </summary>
        /// <remarks>
        /// For style parsing, the 'Set' tag is an unknown tag, but this will map to a
        /// Trigger set command.  Store this as an element start record here.
        /// Also 'Set.Value' will map to the a complex Value set portion of the Set command.
        /// </remarks>
        public override void WriteUnknownTagStart(XamlUnknownTagStartNode xamlUnknownTagStartNode)
        {
#if PBTCOMPILER
            string localElementFullName = string.Empty;
            int lastIndex = xamlUnknownTagStartNode.Value.LastIndexOf('.');

            // if local complex property bail out now and handle in 2nd pass when TypeInfo is available
            if (-1 == lastIndex)
            {
                NamespaceMapEntry[] namespaceMaps = XamlTypeMapper.GetNamespaceMapEntries(xamlUnknownTagStartNode.XmlNamespace);

                if (namespaceMaps != null && namespaceMaps.Length == 1 && namespaceMaps[0].LocalAssembly)
                {
                    localElementFullName = namespaceMaps[0].ClrNamespace + "." + xamlUnknownTagStartNode.Value;
                }
            }
            else if (IsLocalPass1)
            {
                return;
            }

            if (localElementFullName.Length == 0 || !IsLocalPass1)
            {
#endif
                // It can be a fairly common error for <Style.Setters>,
                // or <Style.Triggers> to be specified
                // at the wrong nesting level.  Detect
                // these cases to give more meaningful error messages.
                if (xamlUnknownTagStartNode.Value == XamlStyleSerializer.VisualTriggersFullPropertyName ||
                    xamlUnknownTagStartNode.Value == XamlStyleSerializer.SettersFullPropertyName)
                {
                    ThrowException(SRID.StyleKnownTagWrongLocation,
                                   xamlUnknownTagStartNode.Value,
                                   xamlUnknownTagStartNode.LineNumber,
                                   xamlUnknownTagStartNode.LinePosition);
                }
                else
                {
                    base.WriteUnknownTagStart(xamlUnknownTagStartNode);
                }
#if PBTCOMPILER
            }
#endif
        }

        /// <summary>
        /// Write Start element for a dictionary key section.
        /// </summary>
        public override void WriteKeyElementStart(
            XamlElementStartNode xamlKeyElementStartNode)
        {
            _styleModeStack.Push(StyleMode.Key);
#if PBTCOMPILER
            _defNameFound = true;
#endif
            base.WriteKeyElementStart(xamlKeyElementStartNode);
        }

        /// <summary>
        /// Write End element for a dictionary key section
        /// </summary>
        public override void WriteKeyElementEnd(
            XamlElementEndNode xamlKeyElementEndNode)
        {
            _styleModeStack.Pop();
            base.WriteKeyElementEnd(xamlKeyElementEndNode);
        }

        /// <summary>
        /// Write end of an unknown tag
        /// </summary>
        /// <remarks>
        /// For style parsing, the 'Set' tag is an unknown tag, but this will map to a
        /// Trigger set command.  Store this as an element end record here.
        /// </remarks>
        public override void WriteUnknownTagEnd(XamlUnknownTagEndNode xamlUnknownTagEndNode)
        {
#if PBTCOMPILER
                NamespaceMapEntry[] namespaceMaps = XamlTypeMapper.GetNamespaceMapEntries(xamlUnknownTagEndNode.XmlNamespace);
                bool localTag = namespaceMaps != null && namespaceMaps.Length == 1 && namespaceMaps[0].LocalAssembly;

                if (!localTag || !IsLocalPass1)
                {
#endif
                   base.WriteUnknownTagEnd(xamlUnknownTagEndNode);
#if PBTCOMPILER
                }
#endif
        }

        /// <summary>
        /// Write unknown attribute
        /// </summary>
        /// <remarks>
        /// For style parsing, the 'Set' tag is an unknown tag and contains properties that
        /// are passed as UnknownAttributes.  Translate these into Property records.
        /// </remarks>
        public override void WriteUnknownAttribute(XamlUnknownAttributeNode xamlUnknownAttributeNode)
        {
#if PBTCOMPILER
            bool localAttrib = false;
            string localTagFullName = string.Empty;
            string localAttribName = xamlUnknownAttributeNode.Name;
            NamespaceMapEntry[] namespaceMaps = null;

            if (xamlUnknownAttributeNode.OwnerTypeFullName.Length > 0)
            {
                // These are attributes on a local tag ...
                localTagFullName = xamlUnknownAttributeNode.OwnerTypeFullName;
                localAttrib = true;
            }
            else
            {
                //  These are attributes on a non-local tag ...
                namespaceMaps = XamlTypeMapper.GetNamespaceMapEntries(xamlUnknownAttributeNode.XmlNamespace);
                localAttrib = namespaceMaps != null && namespaceMaps.Length == 1 && namespaceMaps[0].LocalAssembly;
            }

            if (localAttrib)
            {
                // ... and if there are any periods in the attribute name, then ...
                int lastIndex = localAttribName.LastIndexOf('.');

                if (-1 != lastIndex)
                {
                    // ... these might be attached props or events defined by a locally defined component,
                    // but being set on this non-local tag.

                    string ownerTagName = localAttribName.Substring(0, lastIndex);

                    if (namespaceMaps != null)
                    {
                        if (namespaceMaps.Length == 1 && namespaceMaps[0].LocalAssembly)
                        {
                            localTagFullName = namespaceMaps[0].ClrNamespace + "." + ownerTagName;
                        }
                    }
                    else
                    {
                        TypeAndSerializer typeAndSerializer = XamlTypeMapper.GetTypeOnly(xamlUnknownAttributeNode.XmlNamespace,
                                                                                 ownerTagName);

                        if (typeAndSerializer != null)
                        {
                            Type ownerTagType = typeAndSerializer.ObjectType;
                            localTagFullName = ownerTagType.FullName;
                        }
                        else
                        {
                            namespaceMaps = XamlTypeMapper.GetNamespaceMapEntries(xamlUnknownAttributeNode.XmlNamespace);
                            if (namespaceMaps != null && namespaceMaps.Length == 1 && namespaceMaps[0].LocalAssembly)
                            {
                                localTagFullName = namespaceMaps[0].ClrNamespace + "." + ownerTagName;
                            }
                            else
                            {
                                localTagFullName = string.Empty;
                            }
                        }
                    }

                    localAttribName = localAttribName.Substring(lastIndex + 1);
                }
            }

            if (localTagFullName.Length == 0 || !IsLocalPass1)
            {
#endif
                base.WriteUnknownAttribute(xamlUnknownAttributeNode);
#if PBTCOMPILER
            }
#endif
        }

        /// <summary>
        /// WriteEndAttributes occurs after the last attribute (property, complex property or
        /// def record) is written.  Note that if there are none of the above, then WriteEndAttributes
        /// is not called for a normal start tag.
        /// </summary>
        public override void WriteEndAttributes(XamlEndAttributesNode xamlEndAttributesNode)
        {
            if ((_styleModeStack.Mode == StyleMode.Setters || _styleModeStack.Mode == StyleMode.TriggerBase) &&
                !xamlEndAttributesNode.IsCompact)
            {
                if (_setterOrTriggerValueNode != null)
                {
                    ProcessPropertyValueNode();
                }
#if PBTCOMPILER
                else if (_inEventSetter)
                {
                    ProcessEventSetterNode(xamlEndAttributesNode);
                }
#endif
            }

            base.WriteEndAttributes(xamlEndAttributesNode);
        }

        private MemberInfo GetDependencyPropertyInfo(XamlPropertyNode xamlPropertyNode)
        {
            string member = xamlPropertyNode.Value;
            MemberInfo dpInfo = GetPropertyOrEventInfo(xamlPropertyNode, ref member);
            if (dpInfo != null)
            {
                // Note: Should we enforce that all DP fields should end with a 
                // "Property" or "PropertyKey" postfix here?

                if (BamlRecordWriter != null)
                {
                    short typeId;
                    short propertyId = MapTable.GetAttributeOrTypeId(BamlRecordWriter.BinaryWriter,
                                                                     dpInfo.DeclaringType,
                                                                     member,
                                                                     out typeId);

                    if (propertyId < 0)
                    {
                        xamlPropertyNode.ValueId = propertyId;
                        xamlPropertyNode.MemberName = null;
                    }
                    else
                    {
                        xamlPropertyNode.ValueId = typeId;
                        xamlPropertyNode.MemberName = member;
                    }
                }
            }

            return dpInfo;
        }

        private MemberInfo GetPropertyOrEventInfo(XamlNode xamlNode, ref string member)
        {
            // Strip off namespace prefix from the event or property name and
            // map this to an xmlnamespace.  Also extract the class name, if present
            string prefix = string.Empty;
            string target = member;
            string propOrEvent = member;
            int dotIndex = member.LastIndexOf('.');
            if (-1 != dotIndex)
            {
                target = propOrEvent.Substring(0, dotIndex);
                member = propOrEvent.Substring(dotIndex + 1);
            }
            int colonIndex = target.IndexOf(':');
            if (-1 != colonIndex)
            {
                // If using .net then match against the class.
                prefix = target.Substring(0, colonIndex);
                if (-1 == dotIndex)
                {
                    member = target.Substring(colonIndex + 1);
                }
            }

            string xmlNamespace = TokenReader.XmlReader.LookupNamespace(prefix);
            Type targetType = null;

            // Get the type associated with the property or event from the XamlTypeMapper and
            // use this to resolve the property or event into an EventInfo, PropertyInfo
            // or MethodInfo
            if (-1 != dotIndex)
            {
                targetType = XamlTypeMapper.GetTypeFromBaseString(target, ParserContext, false);
            }
            else if (_styleTargetTypeString != null)
            {
                targetType = XamlTypeMapper.GetTypeFromBaseString(_styleTargetTypeString, ParserContext, false);
                target = _styleTargetTypeString;
            }
            else if (_styleTargetTypeType != null)
            {
                targetType = _styleTargetTypeType;
                target = targetType.Name;
            }

            MemberInfo memberInfo = null;
            if (targetType != null)
            {
                string objectName = propOrEvent;
                memberInfo = XamlTypeMapper.GetClrInfo(
                                        _inEventSetter,
                                        targetType,
                                        xmlNamespace,
                                        member,
                                    ref objectName) as MemberInfo;
            }

            if (memberInfo != null)
            {
                if (!_inEventSetter)
                {
                    PropertyInfo pi = memberInfo as PropertyInfo;
                    if (pi != null)
                    {
                        // For trigger condition only allow if public or internal getter
                        if (_inSetterDepth < 0 && _styleModeStack.Mode == StyleMode.TriggerBase)
                        {
                            if (!XamlTypeMapper.IsAllowedPropertyGet(pi))
                            {
                                ThrowException(SRID.ParserCantSetTriggerCondition,
                                               pi.Name,
                                               xamlNode.LineNumber,
                                               xamlNode.LinePosition);
                            }
                        }
                        else // for general Setters check prop setters
                        {
                            if (!XamlTypeMapper.IsAllowedPropertySet(pi))
                            {
                                ThrowException(SRID.ParserCantSetAttribute,
                                               "Property Setter",
                                               pi.Name,
                                               "set",
                                               xamlNode.LineNumber,
                                               xamlNode.LinePosition);
                            }
                        }
                    }
                }
            }
            // local properties and events will be added to the baml in pass2 of the compilation.
            // so don't throw now.
            else
#if PBTCOMPILER
                if (!IsLocalPass1)
#endif
            {
                if (targetType != null)
                {
                    ThrowException(SRID.StyleNoPropOrEvent,
                                   (_inEventSetter ? "Event" : "Property"),
                                   member,
                                   targetType.FullName,
                                   xamlNode.LineNumber,
                                   xamlNode.LinePosition);
                }
                else
                {
                    ThrowException(SRID.StyleNoTarget,
                                   (_inEventSetter ? "Event" : "Property"),
                                   member,
                                   xamlNode.LineNumber,
                                   xamlNode.LinePosition);
                }
            }

            return memberInfo;
        }

#if PBTCOMPILER
        private void ProcessEventSetterNode(XamlEndAttributesNode xamlEndAttributesNode)
        {
            // Check for EventSetter properties. These aren't really stored as properties but
            // resolved at the EventSetter end tag as events by the compiler
            Debug.Assert(_inEventSetter);

            string member = _event;
            MemberInfo memberInfo = GetPropertyOrEventInfo(xamlEndAttributesNode, ref member);
            // If we have an event setter on a locally defined component, write it out
            // as a property instead of an event so that it will be resolved at runtime.
            if (null != memberInfo)
            {
                XamlClrEventNode eventNode = new XamlClrEventNode(
                    xamlEndAttributesNode.LineNumber,
                    xamlEndAttributesNode.LinePosition,
                    xamlEndAttributesNode.Depth,
                    member,
                    memberInfo,
                    _handler);
#if HANDLEDEVENTSTOO
                eventNode.HandledEventsToo = _handledEventsToo;
#endif
                WriteClrEvent(eventNode);
            }

            _event = null;
            _handler = null;
#if HANDLEDEVENTSTOO
            _handledEventsToo = false;
#endif
        }
#endif

        /// <summary>
        /// The Value="foo" property node for a Setter and a Trigger has been saved
        /// and is resolved some time afterwards using the associated Property="Bar"
        /// attribute.  This is done so that the property record can be written using
        /// the type converter associated with "Bar"
        /// </summary>
        private void ProcessPropertyValueNode()
        {
            if (_setterOrTriggerPropertyInfo != null)
            {
                // Now we have PropertyInfo or a MethodInfo for the property setter.
                // Get the type of the property from this which will be used
                // by BamlRecordWriter.WriteProperty to find an associated
                // TypeConverter to use at runtime.
                // To allow for per-property type converters we need to extract
                // information from the member info about the property
                Type propertyType = XamlTypeMapper.GetPropertyType(_setterOrTriggerPropertyInfo);
                _setterOrTriggerValueNode.ValuePropertyType = propertyType;
                _setterOrTriggerValueNode.ValuePropertyMember = _setterOrTriggerPropertyInfo;
                _setterOrTriggerValueNode.ValuePropertyName = XamlTypeMapper.GetPropertyName(_setterOrTriggerPropertyInfo);
                _setterOrTriggerValueNode.ValueDeclaringType = _setterOrTriggerPropertyInfo.DeclaringType;

                base.WriteProperty(_setterOrTriggerValueNode);
            }
            else
            {
                base.WriteBaseProperty(_setterOrTriggerValueNode);
            }

            _setterOrTriggerValueNode = null;
            _setterOrTriggerPropertyInfo = null;
        }

        /// <summary>
        /// Write Def Attribute
        /// </summary>
        /// <remarks>
        /// Style parsing supports x:ID, so check for this here
        /// </remarks>
        public override void WriteDefAttribute(XamlDefAttributeNode xamlDefAttributeNode)
        {
            if (xamlDefAttributeNode.Name == BamlMapTable.NameString)
            {
                if (BamlRecordWriter != null)
                {
                    BamlRecordWriter.WriteDefAttribute(xamlDefAttributeNode);
                }
            }
            else
            {
#if PBTCOMPILER
                // Remember that x:Key was read in, since this key has precedence over
                // the TargetType="{x:Type SomeType}" key that may also be present.
                if (xamlDefAttributeNode.Name == XamlReaderHelper.DefinitionName &&
                    _styleModeStack.Mode == StyleMode.Base)
                {
                    _defNameFound = true;
                }
#endif

                // Skip Uids for EventSetter, since they are not localizable.
                if (!_inEventSetter ||
                    xamlDefAttributeNode.Name != XamlReaderHelper.DefinitionUid)
                {
                    base.WriteDefAttribute(xamlDefAttributeNode);
                }
            }
        }

#if PBTCOMPILER
        /// <summary>
        /// Write out a key to a dictionary that has been resolved at compile or parse
        /// time to a Type object.
        /// </summary>
        public override void WriteDefAttributeKeyType(XamlDefAttributeKeyTypeNode xamlDefNode)
        {
            // Remember that x:Key was read in, since this key has precedence over
            // the TargetType="{x:Type SomeType}" key that may also be present.
            if (_styleModeStack.Mode == StyleMode.Base)
            {
                _defNameFound = true;
            }
            base.WriteDefAttributeKeyType(xamlDefNode);
        }
#endif

        /// <summary>
        /// Write Start of an Element, which is a tag of the form /<Classname />
        /// </summary>
        /// <remarks>
        /// For style parsing, determine when it is withing a Trigger or
        /// MultiTrigger section.  This is done for validity checking of
        /// unknown tags and attributes.
        /// </remarks>
        public override void WriteElementStart(XamlElementStartNode xamlElementStartNode)
        {
            StyleMode mode = _styleModeStack.Mode;
            int depth = _styleModeStack.Depth;
            _setterOrTriggerPropertyInfo = null;
            bool tagWritten = false;

            // The very first element encountered within a style block should be the
            // target type tag, or a Setter.
            if (mode == StyleMode.Base && depth > 0)
            {
                if (KnownTypes.Types[(int)KnownElements.SetterBase].IsAssignableFrom(xamlElementStartNode.ElementType))
                {
                    if (_setterPropertyEncountered)
                    {
                        ThrowException(SRID.StyleImpliedAndComplexChildren,
                                   xamlElementStartNode.ElementType.Name,
                                   XamlStyleSerializer.SettersPropertyName,
                                   xamlElementStartNode.LineNumber,
                                   xamlElementStartNode.LinePosition);
                    }
                    mode = StyleMode.Setters;
                    _setterElementEncountered = true;
                }
                else
                {
                    ThrowException(SRID.StyleNoTopLevelElement,
                                   xamlElementStartNode.ElementType.Name,
                                   xamlElementStartNode.LineNumber,
                                   xamlElementStartNode.LinePosition);
                }
            }
            else if (mode == StyleMode.TriggerBase &&
                     (xamlElementStartNode.ElementType == KnownTypes.Types[(int)KnownElements.Trigger] ||
                      xamlElementStartNode.ElementType == KnownTypes.Types[(int)KnownElements.MultiTrigger] ||
                      xamlElementStartNode.ElementType == KnownTypes.Types[(int)KnownElements.DataTrigger] ||
                      xamlElementStartNode.ElementType == KnownTypes.Types[(int)KnownElements.MultiDataTrigger] ||
                      xamlElementStartNode.ElementType == KnownTypes.Types[(int)KnownElements.EventTrigger]))
            {
                _inPropertyTriggerDepth = xamlElementStartNode.Depth;
            }
            else if (mode == StyleMode.TriggerBase &&
                     (KnownTypes.Types[(int)KnownElements.SetterBase].IsAssignableFrom(xamlElementStartNode.ElementType)))
            {
                // Just entered the <Setter> section of a Trigger
                _inSetterDepth = xamlElementStartNode.Depth;
            }
#if PBTCOMPILER
            else if (mode == StyleMode.TargetTypeProperty &&
                     InDeferLoadedSection &&
                     depth >= 2 &&
                     !_defNameFound)
            {
                // We have to treat TargetType="{x:Type SomeType}" as a key in a
                // resource dictionary, if one is present.  This means generating
                // a series of baml records to use as the key for the defer loaded
                // body of the Style.
                if (depth == 2)
                {
                    base.WriteKeyElementStart(xamlElementStartNode);
                }
                else
                {
                    base.WriteElementStart(xamlElementStartNode);
                }
                
                tagWritten = true;
            }
#endif

            if (mode == StyleMode.Setters)
            {
                if (xamlElementStartNode.ElementType == KnownTypes.Types[(int)KnownElements.EventSetter])
                {
#if !PBTCOMPILER
                    ThrowException(SRID.StyleNoEventSetters,
                        xamlElementStartNode.LineNumber,
                        xamlElementStartNode.LinePosition);
#else
                    _inEventSetter = true;
#endif
                }
                else if ((depth == 2 && _setterElementEncountered) ||
                         (depth == 3 && _setterPropertyEncountered))
                {
                    ThrowException(SRID.ParserNoSetterChild,
                                   xamlElementStartNode.TypeFullName,
                                   xamlElementStartNode.LineNumber,
                                   xamlElementStartNode.LinePosition);
                }
            }

            // Handle custom serializers within the style section by creating an instance
            // of that serializer and handing off control.
            if (xamlElementStartNode.SerializerType != null && depth > 0)
            {
                 XamlSerializer serializer;
                 if (xamlElementStartNode.SerializerType == typeof(XamlStyleSerializer))
                 {
#if PBTCOMPILER
                    // reset the event scope so that any other event setters encountered in this
                    // style after the nested Style is done parsing will be added to a new scope
                     _isSameScope = false;
#endif
                     serializer = new XamlStyleSerializer(ParserHooks);
                 }
                 else if (xamlElementStartNode.SerializerType == typeof(XamlTemplateSerializer))
                 {
#if PBTCOMPILER
                    // reset the event scope so that any other event setters encountered in this
                    // style after the nested Template is done parsing will be added to a new scope
                     _isSameScope = false;
#endif
                     serializer = new XamlTemplateSerializer(ParserHooks);
                 }
                 else
                 {
                     serializer = XamlTypeMapper.CreateInstance(xamlElementStartNode.SerializerType) as XamlSerializer;
                 }
                 if (serializer == null)
                 {
                     ThrowException(SRID.ParserNoSerializer,
                                   xamlElementStartNode.TypeFullName,
                                   xamlElementStartNode.LineNumber,
                                   xamlElementStartNode.LinePosition);
                 }
                 else
                 {

                     // If we're compiling (or otherwise producing baml), convert to baml.
                     // When we don't have a TreeBuilder, we're producing baml.

#if !PBTCOMPILER
                     
                     if( TreeBuilder == null )
                     {
#endif
                         serializer.ConvertXamlToBaml(TokenReader,
                                           BamlRecordWriter == null ? ParserContext : BamlRecordWriter.ParserContext, 
                                           xamlElementStartNode, BamlRecordWriter);
#if !PBTCOMPILER
                     }
                     else
                     {
                         serializer.ConvertXamlToObject(TokenReader, StreamManager,
                                           BamlRecordWriter.ParserContext, xamlElementStartNode,
                                           TreeBuilder.RecordReader);
                     }
#endif

                 }
            }
            else
            {
                _styleModeStack.Push(mode);
                
                if (!_inEventSetter)
                {
#if PBTCOMPILER
                    // If we DO NOT need a dictionary key, then set the flag that says
                    // a key was already found so that one is not manufactured from
                    // the TargetType property.
                    if (mode == StyleMode.Base && depth == 0)
                    {
                        _defNameFound = !xamlElementStartNode.NeedsDictionaryKey;
                    }
#endif
                    if (!tagWritten)
                    {
                        base.WriteElementStart(xamlElementStartNode);
                    }
                }
            }
        }

        /// <summary>
        /// Write End Element
        /// </summary>
        /// <remarks>
        /// For style parsing, determine when it is withing a Trigger or
        /// MultiTrigger section.  This is done for validity checking of
        /// unknown tags and attributes.
        /// </remarks>
        public override void WriteElementEnd(XamlElementEndNode xamlElementEndNode)
        {
            StyleMode mode = _styleModeStack.Mode;
            bool tagWritten = false;
            
            if (mode == StyleMode.TriggerBase &&
                xamlElementEndNode.Depth == _inSetterDepth)
            {
                // Just exited the <Setter .. /> section of a Trigger
                _inSetterDepth = -1;
            }

            if (xamlElementEndNode.Depth == _inPropertyTriggerDepth)
            {
                _inPropertyTriggerDepth = -1;
            }

#if PBTCOMPILER
           if (_styleModeStack.Depth != 1 &&
               mode == StyleMode.TargetTypeProperty &&
               InDeferLoadedSection &&
               !_defNameFound)
            {
                // We have to treat TargetType="{x:Type SomeType}" as a key in a
                // resource dictionary, if one is present.  This means generating
                // a series of baml records to use as the key for the defer loaded
                // body of the Style in addition to generating the records to set
                // the TargetType value.
                if (_styleModeStack.Depth == 3)
                {
                    base.WriteKeyElementEnd(xamlElementEndNode);
                }
                else
                {
                    base.WriteElementEnd(xamlElementEndNode);
                }
                
                tagWritten = true;
            }
#endif

            _styleModeStack.Pop();
            
            if (!_inEventSetter)
            {
                if (!tagWritten)
                {
                    base.WriteElementEnd(xamlElementEndNode);
                }
            }
            else if (mode == StyleMode.Setters)
            {
                _inEventSetter = false;
            }
        }

#if PBTCOMPILER
        /// <summary>
        /// Write the start of a constructor parameter section
        /// </summary>
        public override void WriteConstructorParameterType(
            XamlConstructorParameterTypeNode xamlConstructorParameterTypeNode)
        {
            if (_styleModeStack.Mode == StyleMode.TargetTypeProperty &&
                InDeferLoadedSection &&
                !_defNameFound)
            {
                // Generate a series of baml records to use as the key for the defer loaded
                // body of the Style in addition to generating the records to set
                // the normal constructor for an element.
                base.WriteConstructorParameterType(xamlConstructorParameterTypeNode);
            }
            base.WriteConstructorParameterType(xamlConstructorParameterTypeNode);
        }
#endif

        /// <summary>
        /// Write the start of a constructor parameter section
        /// </summary>
        public override void WriteConstructorParametersStart(XamlConstructorParametersStartNode xamlConstructorParametersStartNode)
        {
#if PBTCOMPILER
            if (_styleModeStack.Mode == StyleMode.TargetTypeProperty &&
                InDeferLoadedSection &&
                !_defNameFound)
            {
                // We have to treat TargetType="{x:Type SomeType}" as a key in a
                // resource dictionary, if one is present.  This means generating
                // a series of baml records to use as the key for the defer loaded
                // body of the Style in addition to generating the records to set
                // the TargetType value.
                base.WriteConstructorParametersStart(xamlConstructorParametersStartNode);
            }
#endif

            _styleModeStack.Push();
            base.WriteConstructorParametersStart(xamlConstructorParametersStartNode);
        }

        /// <summary>
        /// Write the end of a constructor parameter section
        /// </summary>
        public override void WriteConstructorParametersEnd(XamlConstructorParametersEndNode xamlConstructorParametersEndNode)
        {
#if PBTCOMPILER
            if (_styleModeStack.Mode == StyleMode.TargetTypeProperty &&
                InDeferLoadedSection &&
                !_defNameFound &&
                _styleModeStack.Depth > 2)
            {
                // We have to treat TargetType="{x:Type SomeType}" as a key in a
                // resource dictionary, if one is present.  This means generating
                // a series of baml records to use as the key for the defer loaded
                // body of the Style in addition to generating the records to set
                // the TargetType value.
                base.WriteConstructorParametersEnd(xamlConstructorParametersEndNode);
            }
#endif

            base.WriteConstructorParametersEnd(xamlConstructorParametersEndNode);
            _styleModeStack.Pop();
        }

        /// <summary>
        /// Write start of a complex property
        /// </summary>
        /// <remarks>
        /// For style parsing, treat complex property tags as
        /// xml element tags for the purpose of validity checking
        /// </remarks>
        public override void WritePropertyComplexStart(XamlPropertyComplexStartNode xamlNode)
        {
            StyleMode mode = _styleModeStack.Mode;
            
            if (_styleModeStack.Depth == 1)
            {
                if (xamlNode.PropName == XamlStyleSerializer.TargetTypePropertyName)
                {
                    mode = StyleMode.TargetTypeProperty;
                }
                else if (xamlNode.PropName == XamlStyleSerializer.BasedOnPropertyName)
                {
                    mode = StyleMode.BasedOnProperty;
                }
                else
                {
                    ThrowException(SRID.StyleUnknownProp, xamlNode.PropName,
                                   xamlNode.LineNumber, xamlNode.LinePosition);
                }
            }
            else if (mode == StyleMode.TriggerBase)
            {
                _visualTriggerComplexPropertyDepth++;
            }
#if PBTCOMPILER
            else if (mode == StyleMode.TargetTypeProperty &&
                     InDeferLoadedSection &&
                     !_defNameFound)
            {
                // We have to treat TargetType="{x:Type SomeType}" as a key in a
                // resource dictionary, if one is present.  This means generating
                // a series of baml records to use as the key for the defer loaded
                // body of the Style in addition to generating the records to set
                // the TargetType value.
                base.WritePropertyComplexStart(xamlNode);
            }
#endif

            _styleModeStack.Push(mode);
            base.WritePropertyComplexStart(xamlNode);
        }

        /// <summary>
        /// Write end of a complex property
        /// </summary>
        /// <remarks>
        /// For style parsing, treat complex property tags as
        /// xml element tags for the purpose of validity checking
        /// </remarks>
        public override void WritePropertyComplexEnd(XamlPropertyComplexEndNode xamlNode)
        {
            if (_styleModeStack.Mode == StyleMode.TriggerBase)
            {
                _visualTriggerComplexPropertyDepth--;
            }
#if PBTCOMPILER
            else if (_styleModeStack.Mode == StyleMode.TargetTypeProperty &&
                     InDeferLoadedSection &&
                     !_defNameFound &&
                     _styleModeStack.Depth > 2)
            {
                // We have to treat TargetType="{x:Type SomeType}" as a key in a
                // resource dictionary, if one is present.  This means generating
                // a series of baml records to use as the key for the defer loaded
                // body of the Style in addition to generating the records to set
                // the TargetType value.
                base.WritePropertyComplexEnd(xamlNode);
            }
#endif

            base.WritePropertyComplexEnd(xamlNode);
            _styleModeStack.Pop();
        }

        /// <summary>
        /// Write start of a list complex property
        /// </summary>
        /// <remarks>
        /// For style parsing, treat complex property tags as
        /// xml element tags for the purpose of validity checking
        /// </remarks>
        public override void WritePropertyIListStart(XamlPropertyIListStartNode xamlNode)
        {
            StyleMode mode = _styleModeStack.Mode;
            int depth = _styleModeStack.Depth;
            
            if (depth == 1)
            {
                if (xamlNode.PropName == XamlStyleSerializer.VisualTriggersPropertyName)
                {
                    mode = StyleMode.TriggerBase;
                }
                else if (xamlNode.PropName == XamlStyleSerializer.SettersPropertyName)
                {
                    if (_setterElementEncountered)
                    {
                        ThrowException(SRID.StyleImpliedAndComplexChildren,
                                   XamlStyleSerializer.SetterTagName,
                                   xamlNode.PropName,
                                   xamlNode.LineNumber, xamlNode.LinePosition);
                    }
                    mode = StyleMode.Setters;
                    _setterPropertyEncountered = true;
                }
                else
                {
                    ThrowException(SRID.StyleUnknownProp, xamlNode.PropName,
                                   xamlNode.LineNumber, xamlNode.LinePosition);
                }
            }
            else if ((mode == StyleMode.TriggerBase ||
                      mode == StyleMode.Setters) &&
                     depth == 2)
            {
                mode = StyleMode.Base;
            }
            else if (mode == StyleMode.TriggerBase &&
                     depth == 3)
            {
                if (xamlNode.PropName == XamlStyleSerializer.EventTriggerActions)
                {
                    mode = StyleMode.TriggerActions;
                }
            }

            _styleModeStack.Push(mode);
            base.WritePropertyIListStart(xamlNode);
        }

        /// <summary>
        /// Write end of a list complex property
        /// </summary>
        /// <remarks>
        /// For style parsing, treat complex property tags as
        /// xml element tags for the purpose of validity checking when we're counting
        /// element tags.
        /// </remarks>
        public override void WritePropertyIListEnd(XamlPropertyIListEndNode xamlNode)
        {
#if PBTCOMPILER
            if (_styleModeStack.Mode == StyleMode.Setters)
            {
                _isSameScope = false;
            }
#endif

            base.WritePropertyIListEnd(xamlNode);
            _styleModeStack.Pop();
        }

        /// <summary>
        /// Write Property Array Start
        /// </summary>
        public override void WritePropertyArrayStart(XamlPropertyArrayStartNode xamlPropertyArrayStartNode)
        {
            base.WritePropertyArrayStart(xamlPropertyArrayStartNode);
            _styleModeStack.Push();
        }


        /// <summary>
        /// Write Property Array End
        /// </summary>
        public override void WritePropertyArrayEnd(XamlPropertyArrayEndNode xamlPropertyArrayEndNode)
        {
            base.WritePropertyArrayEnd(xamlPropertyArrayEndNode);
            _styleModeStack.Pop();
        }

        /// <summary>
        /// Write Property IDictionary Start
        /// </summary>
        public override void WritePropertyIDictionaryStart(XamlPropertyIDictionaryStartNode xamlPropertyIDictionaryStartNode)
        {
            StyleMode mode = _styleModeStack.Mode;
            if (_styleModeStack.Depth == 1 && mode == StyleMode.Base)
            {
                if (xamlPropertyIDictionaryStartNode.PropName == XamlStyleSerializer.ResourcesPropertyName)
                {
                    mode = StyleMode.Resources;
                }
                else
                {
                    ThrowException(SRID.StyleUnknownProp, xamlPropertyIDictionaryStartNode.PropName,
                                   xamlPropertyIDictionaryStartNode.LineNumber, xamlPropertyIDictionaryStartNode.LinePosition);
                }
            }

            base.WritePropertyIDictionaryStart(xamlPropertyIDictionaryStartNode);
            _styleModeStack.Push(mode);
        }


        /// <summary>
        /// Write Property IDictionary End
        /// </summary>
        public override void WritePropertyIDictionaryEnd(XamlPropertyIDictionaryEndNode xamlPropertyIDictionaryEndNode)
        {
            base.WritePropertyIDictionaryEnd(xamlPropertyIDictionaryEndNode);
            _styleModeStack.Pop();
        }

        /// <summary>
        /// Write Text node and do style related error checking
        /// </summary>
        public override void WriteText(XamlTextNode xamlTextNode)
        {
            StyleMode mode = _styleModeStack.Mode;
            // Text is only valid within certain locations in the <Style> section.
            // Check for all the valid locations and write out the text record.  For
            // all other locations, see if the text is non-blank and throw an error
            // if it is.  Ignore any whitespace, since this is not considered
            // significant in Style cases.
            if (mode == StyleMode.TargetTypeProperty)
            {
                // Remember the TargetType name so that the event setter parsing could use it
                // to resolve non-qualified event names
                _styleTargetTypeString = xamlTextNode.Text;
            }

#if PBTCOMPILER
            if (mode == StyleMode.TargetTypeProperty &&
                InDeferLoadedSection &&
                !_defNameFound)
            {
                // We have to treat TargetType="{x:Type SomeType}" as a key in a
                // resource dictionary, if one is present.  This means generating
                // a series of baml records to use as the key for the defer loaded
                // body of the Style in addition to generating the records to set
                // the TargetType value.
                base.WriteText(xamlTextNode);
            }
#endif
            if ((mode != StyleMode.TriggerBase &&
                 mode != StyleMode.Base) ||
                (mode == StyleMode.TriggerBase && _inSetterDepth >= 0) ||
                (mode == StyleMode.TriggerActions) ||
                (mode == StyleMode.TriggerBase &&
                 _visualTriggerComplexPropertyDepth >= 0))
            {
                base.WriteText(xamlTextNode);
            }
            else
            {
                for (int i = 0; i< xamlTextNode.Text.Length; i++)
                {
                    if (!XamlReaderHelper.IsWhiteSpace(xamlTextNode.Text[i]))
                    {
                        ThrowException(SRID.StyleTextNotSupported, xamlTextNode.Text,
                               xamlTextNode.LineNumber, xamlTextNode.LinePosition);
                    }
                }
            }
        }

#if PBTCOMPILER
        /// <summary>
        /// Write an Event Connector and call the controlling compiler parser to generate event hookup code.
        /// </summary>
        public override void WriteClrEvent(XamlClrEventNode xamlClrEventNode)
        {
            if (_previousXamlParser != null)
            {
                if (_styleModeStack.Mode != StyleMode.Setters)
                {
                    ThrowException(SRID.StyleTargetNoEvents,
                        xamlClrEventNode.EventName,
                        xamlClrEventNode.LineNumber,
                        xamlClrEventNode.LinePosition);
                }

                // if this event token is not owned by this StyleXamlParser, then just chain
                // the WriteClrEvent call to the controlling parser. Ulimately, this will
                // reach the markup compiler that will deal with this info as appropriate.

                bool isOriginatingEvent = xamlClrEventNode.IsOriginatingEvent;
                if (isOriginatingEvent)
                {
                    // set up additional state on event node for the markup compiler to use.
                    Debug.Assert(!xamlClrEventNode.IsTemplateEvent && _inEventSetter);
                    xamlClrEventNode.IsStyleSetterEvent = _inEventSetter;
                    xamlClrEventNode.IsSameScope = _isSameScope;

                    // any intermediary controlling parsers need to get out of the way so that
                    // the markup compiler can ultimately do its thing.
                    xamlClrEventNode.IsOriginatingEvent = false;
                }

                _previousXamlParser.WriteClrEvent(xamlClrEventNode);

                if (isOriginatingEvent)
                {
                    if (!String.IsNullOrEmpty(xamlClrEventNode.LocalAssemblyName))
                    {
                        // if this event is a local event need to generate baml for it now

                        string assemblyName = KnownTypes.Types[(int)KnownElements.EventSetter].Assembly.FullName;

                        base.WriteElementStart(new XamlElementStartNode(XamlNodeType.ElementStart,
                                                                        xamlClrEventNode.LineNumber,
                                                                        xamlClrEventNode.LinePosition,
                                                                        xamlClrEventNode.Depth,
                                                                        assemblyName,
                                                                        KnownTypes.Types[(int)KnownElements.EventSetter].FullName,
                                                                        KnownTypes.Types[(int)KnownElements.EventSetter],
                                                                        null /*serializerType*/,
                                                                        false /*isEmptyElement*/,
                                                                        false /*needsDictionaryKey*/,
                                                                        false /*isInjected*/));

                        XamlPropertyNode xamlPropertyNode = new XamlPropertyNode(xamlClrEventNode.LineNumber,
                                                                                 xamlClrEventNode.LinePosition,
                                                                                 xamlClrEventNode.Depth,
                                                                                 null,
                                                                                 xamlClrEventNode.LocalAssemblyName,
                                                                                 xamlClrEventNode.EventMember.ReflectedType.FullName,
                                                                                 xamlClrEventNode.EventName,
                                                                                 xamlClrEventNode.Value,
                                                                                 BamlAttributeUsage.Default,
                                                                                 false);
                        base.WriteProperty(xamlPropertyNode);

                        base.WriteElementEnd(new XamlElementEndNode(xamlClrEventNode.LineNumber,
                                                                    xamlClrEventNode.LinePosition,
                                                                    xamlClrEventNode.Depth));
                    }
                    else
                    {
                        // write out a connectionId to the baml stream only if a
                        // new event scope was encountered
                        if (!_isSameScope)
                        {
                            base.WriteConnectionId(xamlClrEventNode.ConnectionId);
                        }

                        // We have just finished processing the start of a new event scope.
                        // So specifiy start of this new scope.
                        _isSameScope = true;
                    }
                }
            }
        }
#endif


        /// <summary>
        /// Write a Property, which has the form in markup of property="value".
        /// </summary>
        public override void WriteProperty(XamlPropertyNode xamlPropertyNode)
        {
            StyleMode mode = _styleModeStack.Mode;

            Debug.Assert (mode != StyleMode.Base || xamlPropertyNode.PropName != XamlStyleSerializer.TargetTypePropertyName,
                          "TargetType should be handled by WritePropertyWithType");

            if (mode == StyleMode.TargetTypeProperty &&
                xamlPropertyNode.PropName == "TypeName")
            {
                // Remember the TargetType name so that the event setter parsing could use it
                // to resolve non-qualified event names
                _styleTargetTypeString = xamlPropertyNode.Value;
            }

            // When compiling, native DependencyProperties may not have a static setter,
            // so all we can resolve is the associated PropertyInfo.  In this case, we
            // will check to see if there is a static field named PropName+Property and
            // assume that this will be a DependencyProperty that can be resolved at
            // runtime.  If there is no property with this naming pattern, then it can't
            // resolve at runtime, so complain now.  Note that this is only done in the
            // compile case, since a cheaper check is done in the StyleBamlRecordReader
            // for the xaml-to-tree case.
            if (mode == StyleMode.Setters ||
                mode == StyleMode.TriggerBase)
            {
                if (_inSetterDepth >= 0 && !_inEventSetter)
                {
                    // Trigger Setters processed here.
                    if (xamlPropertyNode.PropName == XamlStyleSerializer.SetterValueAttributeName)
                    {
                        _setterOrTriggerValueNode = xamlPropertyNode;
                        // Delay writing out the Value attribute until WriteEndAttributes if this is a
                        // normal property node.  If this is a property node that was created from complex
                        // syntax, then the WriteEndAttributes has already occurred, so process the
                        // node now.
                        if (xamlPropertyNode.ComplexAsSimple)
                        {
                            ProcessPropertyValueNode();
                        }

                        return;
                    }
                    else if (xamlPropertyNode.PropName == XamlStyleSerializer.SetterPropertyAttributeName)
                    {
                        // Property names should be trimmed since whitespace is not significant
                        // and can affect name resolution 
                        xamlPropertyNode.SetValue(xamlPropertyNode.Value.Trim());
                        _setterOrTriggerPropertyInfo = GetDependencyPropertyInfo(xamlPropertyNode);
                    }
                    else if (xamlPropertyNode.PropName == XamlTemplateSerializer.SetterTargetAttributeName)
                    {
                        ThrowException(SRID.TargetNameNotSupportedForStyleSetters,
                                       xamlPropertyNode.LineNumber,
                                       xamlPropertyNode.LinePosition);
                    }
                }
                else if (_inSetterDepth < 0 && !_inEventSetter)
                {
                    // Setters & Trigger Conditions processed here.
                    if (xamlPropertyNode.PropName == XamlStyleSerializer.PropertyTriggerValuePropertyName)
                    {
                        _setterOrTriggerValueNode = xamlPropertyNode;
                        // Delay writing out the Value attribute until WriteEndAttributes if this is a
                        // normal property node.  If this is a property node that was created from complex
                        // syntax, then the WriteEndAttributes has already occurred, so process the
                        // node now.
                        if (xamlPropertyNode.ComplexAsSimple)
                        {
                            ProcessPropertyValueNode();
                        }

                        return;
                    }
                    else if (xamlPropertyNode.PropName == XamlStyleSerializer.PropertyTriggerPropertyName)
                    {
                        // Property names should be trimmed since whitespace is not significant
                        // and can affect name resolution.
                        xamlPropertyNode.SetValue(xamlPropertyNode.Value.Trim());
                        _setterOrTriggerPropertyInfo = GetDependencyPropertyInfo(xamlPropertyNode);
                    }
                    else if (xamlPropertyNode.PropName == XamlStyleSerializer.PropertyTriggerSourceName)
                    {
                        ThrowException(SRID.SourceNameNotSupportedForStyleTriggers,
                                        xamlPropertyNode.LineNumber,
                                        xamlPropertyNode.LinePosition);
                    }
                    else if (xamlPropertyNode.PropName == XamlTemplateSerializer.SetterTargetAttributeName)
                    {
                        ThrowException(SRID.TargetNameNotSupportedForStyleSetters,
                                       xamlPropertyNode.LineNumber,
                                       xamlPropertyNode.LinePosition);
                    }
                }

#if PBTCOMPILER
                else if (_inEventSetter)
                {
                    // Check for EventSetter properties.  These aren't really stored as properties but
                    // resolved at the EventSetter end tag as events by the compiler
                    if (xamlPropertyNode.PropName == XamlStyleSerializer.SetterEventAttributeName)
                    {
                        _event = xamlPropertyNode.Value;
                    }
                    else if (xamlPropertyNode.PropName == XamlStyleSerializer.SetterHandlerAttributeName)
                    {
                        _handler = xamlPropertyNode.Value;
                    }
#if HANDLEDEVENTSTOO
                    else if (xamlPropertyNode.PropName == XamlStyleSerializer.SetterHandledEventsTooAttributeName)
                    {
                        _handledEventsToo = Boolean.Parse(xamlPropertyNode.Value);
                    }
#endif
                    return;
                }
#endif
            }

            base.WriteProperty(xamlPropertyNode);
        }

        /// <summary>
        /// Write a Property, which has the form in markup of property="value" where
        /// "value" has been resolved into a Type object.
        /// </summary>
        public override void WritePropertyWithType(XamlPropertyWithTypeNode xamlPropertyNode)
        {
            // If we are on the Style tag itself, and we encounter a TargetType property,
            // this can be used as the key when placing this style in a ResourceDictionary.
            if (_styleModeStack.Mode == StyleMode.Base &&
                xamlPropertyNode.PropName == XamlStyleSerializer.TargetTypePropertyName)
            {
                _styleTargetTypeType = xamlPropertyNode.ValueElementType;
#if PBTCOMPILER
                if (InDeferLoadedSection &&
                    !_defNameFound)
                {
                    if (BamlRecordWriter != null && xamlPropertyNode.ValueElementType == null)
                    {
                        ThrowException(SRID.ParserNoType, 
                                       xamlPropertyNode.ValueTypeFullName,
                                       xamlPropertyNode.LineNumber,
                                       xamlPropertyNode.LinePosition);
                    }

                    // We have to treat TargetType="{x:Type SomeType}" as a key in a
                    // resource dictionary, if one is present.  This means generating
                    // a  baml record to use as the key for the defer loaded
                    // body of the Style.
                    base.WriteDefAttributeKeyType(
                        new XamlDefAttributeKeyTypeNode(xamlPropertyNode.LineNumber,
                                 xamlPropertyNode.LinePosition,xamlPropertyNode.Depth,
                                 xamlPropertyNode.ValueTypeFullName,
                                 xamlPropertyNode.ValueTypeAssemblyName,
                                 xamlPropertyNode.ValueElementType));
                }
#endif
            }
            base.WritePropertyWithType(xamlPropertyNode);
        }

        /// <summary>
        /// Used when an exception is thrown -- does shutdown on the parser and throws the exception.
        /// </summary>
        /// <param name="e">Exception</param>
        internal override void ParseError(XamlParseException e)
        {
            CloseWriterStream();
#if !PBTCOMPILER
            // If there is an associated treebuilder, tell it about the error.  There may not
            // be a treebuilder, if this parser was built from a serializer for the purpose of
            // converting directly to baml, rather than converting to an object tree.
            if (TreeBuilder != null)
            {
                TreeBuilder.XamlTreeBuildError(e);
            }
#endif
            throw e;
        }

        /// <summary>
        /// Called when the parse was cancelled by the designer or the user.
        /// </summary>
        internal override void  ParseCancelled()
        {
            // Override so we don't close the writer stream, since we're a sub-parser
        }

        /// <summary>
        ///  Called when the parse has been completed successfully.
        /// </summary>
        internal override void ParseCompleted()
        {
            // Override so we don't close the writer stream, since we're a sub-parser
        }

        // Used to determine if strict or loose parsing rules should be enforced.  The TokenReader
        // does some validations that are not valid in the case of parsing Styles, so do a
        // looser parsing validation.
        internal override bool StrictParsing
        {
            get { return false; }
        }

#endregion Overrides

#region Methods

        /// <summary>
        /// Helper function if we are going to a Reader/Writer stream closes the writer
        /// side.
        /// </summary>
        internal void CloseWriterStream()
        {
#if !PBTCOMPILER
            // only close the BamlRecordWriter.  (Rename to Root??)
            if (null != BamlRecordWriter)
            {
                if (BamlRecordWriter.BamlStream is WriterStream)
                {
                    WriterStream writeStream = (WriterStream) BamlRecordWriter.BamlStream;
                    writeStream.Close();
                }
            }
#endif
        }

#endregion Methods

#region Properties

#if !PBTCOMPILER
        /// <summary>
        /// TreeBuilder associated with this class
        /// </summary>
        XamlTreeBuilder TreeBuilder
        {
            get { return _treeBuilder; }
        }
#else
        /// <summary>
        /// Return true if we are not in pass one of a compile and we are parsing a
        /// defer load section of markup.
        /// </summary>
        bool InDeferLoadedSection
        {
            get { return BamlRecordWriter != null && BamlRecordWriter.InDeferLoadedSection; }
        }

        /// <summary>
        /// Return true if this is pass one of a compile process.
        /// </summary>
        bool IsLocalPass1
        {
            get { return BamlRecordWriter == null; }
        }
#endif

#endregion Properties

        #region Data

#if !PBTCOMPILER
        // TreeBuilder that created this parser
        XamlTreeBuilder _treeBuilder;
#endif
        // The XamlParser that the TokenReader was using when this instance of
        // the StyleXamlParser was created.  This must be restored on exit
        XamlParser      _previousXamlParser;

        // Depth in the Xaml file when parsing of this style block started.
        // This is used to determine when to stop parsing
        int             _startingDepth;

        StyleModeStack  _styleModeStack = new StyleModeStack();

        // Depth in the element tree where a <Setter .../> element has begun.
        int             _inSetterDepth = -1;

        // Depth in the element tree where a Trigger or MultiTrigger
        // section has begun.  Set to -1 to indicate it is not within such a section.
        int             _inPropertyTriggerDepth = -1;

        // Depth of nested complex property ina TriggerBase section.  This is
        // used to determine when text content is valid inside a visual trigger.
        int             _visualTriggerComplexPropertyDepth = -1;

        // The string name for TypeExtension within the TargetType property on style.  This may be null.
        string          _styleTargetTypeString;

        // The actual Type of the TargetType property on style.  This may be null.
        Type            _styleTargetTypeType;

        // True if we are parsing the properties of an <EventSetter ... > tag
        bool            _inEventSetter;

        // True if a <Setter .../> or <EventSetter .../> was encountered
        bool            _setterElementEncountered;

        // True if a <Style.Setter> complex property tag was encountered
        bool            _setterPropertyEncountered;

        // The PropertyInfo for the "Foo" Property attribute in <Setter Property="Foo" ... />
        // or <Trigger Property="Foo" .../>
        MemberInfo      _setterOrTriggerPropertyInfo;

        // The Property node for Value attribute in <Setter Value="Bar" ... /> or
        // <Trigger Value="Bar" .../>
        XamlPropertyNode _setterOrTriggerValueNode;

#if PBTCOMPILER
        // The event in the EventSetter tag
        string          _event;

        // True if x:Key property was found on the style tag
        bool            _defNameFound;

        // The handler name in the EventSetter tag
        string          _handler;

#if HANDLEDEVENTSTOO
        // The handledEventsToo flag in the EventSetter tag
        bool            _handledEventsToo;
#endif
        // True if event is in the same setters collecton.
        bool            _isSameScope;
#endif

        #endregion Data
    }


}
