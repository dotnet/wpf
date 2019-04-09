// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose: Class that interfaces with TokenReader and BamlWriter for
*          parsing Template
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

#if PBTCOMPILER
namespace MS.Internal.Markup
#else

using System.Windows;
using System.Windows.Threading;

namespace System.Windows.Markup
#endif
{
    /// <summary>
    /// Handles overrides for case when Template is being built to a tree
    /// instead of compiling to a file.
    /// </summary>
    internal class TemplateXamlParser : XamlParser
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
        internal TemplateXamlParser(
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
        internal TemplateXamlParser(
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
                // relevent to template parsing.
                case XamlNodeType.DocumentStart:
                case XamlNodeType.DocumentEnd:
                    break;

                case XamlNodeType.ElementEnd:
                    base.ProcessXamlNode(xamlNode, ref cleanup, ref done);
                    // If we're at the depth that we started out, then we must be done.  In that case quit
                    // and restore the XamlParser that the token reader was using before parsing templates.
                    if (_styleModeStack.Depth == 0)
                    {
                        done = true;      // Stop the template parse
                        cleanup = false;  // Don't close the stream
                        TokenReader.ControllingXamlParser = _previousXamlParser;
                    }
                    break;

                case XamlNodeType.PropertyArrayStart:
                case XamlNodeType.PropertyArrayEnd:
                case XamlNodeType.DefTag:
                    ThrowException(SRID.TemplateTagNotSupported, xamlNode.TokenType.ToString(),
                                   xamlNode.LineNumber, xamlNode.LinePosition);
                    break;

#if PBTCOMPILER
                case XamlNodeType.EndAttributes:
                    // if there's a DataType present but no x:Key, write out
                    // the key now based on the DataType
                    if (_dataTypePropertyNode != null && _dataTypePropertyNodeDepth == _styleModeStack.Depth)
                    {
                        if (!_defNameFound)
                        {
                            WriteDataTypeKey(_dataTypePropertyNode);
                        }
                        _dataTypePropertyNode = null;
                        _dataTypePropertyNodeDepth = -1;
                    }

                    base.ProcessXamlNode(xamlNode, ref cleanup, ref done);
                    break;
#endif

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
        /// For template parsing, the 'Set' tag is an unknown tag, but this will map to a
        /// Trigger set command.  Store this as an element start record here.
        /// Also 'Set.Value' will map to the a complex Value set portion of the Set command.
        /// </remarks>
        public override void WriteUnknownTagStart(XamlUnknownTagStartNode xamlUnknownTagStartNode)
        {
            StyleMode mode = _styleModeStack.Mode;

            // Keep mode and other state up-to-date.  This
            // must be called before updating stack.

             CommonElementStartProcessing(xamlUnknownTagStartNode, null, ref mode);
            _styleModeStack.Push(mode);


#if PBTCOMPILER
            string localElementFullName = string.Empty;
            int lastIndex = xamlUnknownTagStartNode.Value.LastIndexOf('.');

            // if local complex property bail out now and handle in 2nd pass when TypInfo is available
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
                // It can be a fairly common error for,
                // <ControlTemplate.Triggers>, <DataTemplate.Triggers>, or <TableTemplate.Triggers>
                // to be specified at the wrong nesting level.  Detect
                // these cases to give more meaningful error messages.
                if (xamlUnknownTagStartNode.Value == XamlTemplateSerializer.ControlTemplateTriggersFullPropertyName ||
                    xamlUnknownTagStartNode.Value == XamlTemplateSerializer.DataTemplateTriggersFullPropertyName ||
                    xamlUnknownTagStartNode.Value == XamlTemplateSerializer.HierarchicalDataTemplateTriggersFullPropertyName ||
                    xamlUnknownTagStartNode.Value == XamlTemplateSerializer.HierarchicalDataTemplateItemsSourceFullPropertyName ||
                    xamlUnknownTagStartNode.Value == XamlTemplateSerializer.HierarchicalDataTemplateItemTemplateFullPropertyName ||
                    xamlUnknownTagStartNode.Value == XamlTemplateSerializer.HierarchicalDataTemplateItemTemplateSelectorFullPropertyName ||
                    xamlUnknownTagStartNode.Value == XamlTemplateSerializer.HierarchicalDataTemplateItemContainerStyleFullPropertyName ||
                    xamlUnknownTagStartNode.Value == XamlTemplateSerializer.HierarchicalDataTemplateItemContainerStyleSelectorFullPropertyName ||
                    xamlUnknownTagStartNode.Value == XamlTemplateSerializer.HierarchicalDataTemplateItemStringFormatFullPropertyName ||
                    xamlUnknownTagStartNode.Value == XamlTemplateSerializer.HierarchicalDataTemplateItemBindingGroupFullPropertyName ||
                    xamlUnknownTagStartNode.Value == XamlTemplateSerializer.HierarchicalDataTemplateAlternationCountFullPropertyName
                    )
                {
                    ThrowException(SRID.TemplateKnownTagWrongLocation,
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
        /// For template parsing, the 'Set' tag is an unknown tag, but this will map to a
        /// Trigger set command.  Store this as an element end record here.
        /// </remarks>
        public override void WriteUnknownTagEnd(XamlUnknownTagEndNode xamlUnknownTagEndNode)
        {
            if (_inSetterDepth == xamlUnknownTagEndNode.Depth)
            {
                XamlElementEndNode elementEnd = new XamlElementEndNode(
                                                        xamlUnknownTagEndNode.LineNumber,
                                                        xamlUnknownTagEndNode.LinePosition,
                                                        xamlUnknownTagEndNode.Depth);
                base.WriteElementEnd(elementEnd);
                _inSetterDepth = -1;
            }
            else
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

            _styleModeStack.Pop();
        }

        /// <summary>
        /// Write unknown attribute
        /// </summary>
        /// <remarks>
        /// For template parsing, the 'Set' tag is an unknown tag and contains properties that
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
#if PBTCOMPILER
            // reset the event scope so that a new tag may start a new scope for its own events
            if (_styleModeStack.Mode == StyleMode.VisualTree && !xamlEndAttributesNode.IsCompact)
            {
                _isSameScope = false;
            }
#endif
            if (_styleModeStack.Mode == StyleMode.TriggerBase && !xamlEndAttributesNode.IsCompact)
            {
                MemberInfo dpInfo = null;
                if (_setterOrTriggerPropertyNode != null)
                {
                    dpInfo = GetDependencyPropertyInfo(_setterOrTriggerPropertyNode);
                    base.WriteProperty(_setterOrTriggerPropertyNode);
                    _setterOrTriggerPropertyNode = null;
                    _setterOrTriggerPropertyMemberInfo = dpInfo;
                }

                _setterTargetNameOrConditionSourceName = null;

                if (_setterOrTriggerValueNode != null)
                {
                    ProcessPropertyValueNode();
                }
            }

            base.WriteEndAttributes(xamlEndAttributesNode);

        }

        private MemberInfo GetDependencyPropertyInfo(XamlPropertyNode xamlPropertyNode)
        {
            string member = xamlPropertyNode.Value;
            MemberInfo dpInfo = GetCLRPropertyInfo(xamlPropertyNode, ref member);
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

        private MemberInfo GetCLRPropertyInfo(XamlPropertyNode xamlPropertyNode, ref string member)
        {
            // Strip off namespace prefix from the event or property name and
            // map this to an xmlnamespace.  Also extract the class name, if present
            string prefix = string.Empty;
            string target = member;
            string propertyName = member;
            int dotIndex = member.LastIndexOf('.');
            if (-1 != dotIndex)
            {
                target = propertyName.Substring(0, dotIndex);
                member = propertyName.Substring(dotIndex+1);
            }
            int colonIndex = target.IndexOf(':');
            if (-1 != colonIndex)
            {
                // If using .net then match against the class.
                prefix = target.Substring(0, colonIndex);
                if (-1 == dotIndex)
                {
                    member = target.Substring(colonIndex+1);
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
            else if (_setterTargetNameOrConditionSourceName != null)
            {
                targetType = _IDTypes[_setterTargetNameOrConditionSourceName] as Type;
                if (targetType == null
#if PBTCOMPILER
                    && !IsLocalPass1
#endif
                   )
                {
                    ThrowException(SRID.TemplateNoTriggerTarget,
                                   _setterTargetNameOrConditionSourceName,
                                   xamlPropertyNode.LineNumber,
                                   xamlPropertyNode.LinePosition);

                }
            }
            else
            {
                targetType = TargetType;
            }

            MemberInfo memberInfo = null;
            if (targetType != null)
            {
                string objectName = propertyName;
                memberInfo = XamlTypeMapper.GetClrInfo(
                                        false,
                                        targetType,
                                        xmlNamespace,
                                        member,
                                    ref objectName) as MemberInfo;
            }

            if (memberInfo != null)
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
                                           xamlPropertyNode.LineNumber,
                                           xamlPropertyNode.LinePosition);
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
                                           xamlPropertyNode.LineNumber,
                                           xamlPropertyNode.LinePosition);
                        }
                    }
                }
            }
            // local properties will be added to the baml in pass2 of the compilation.
            // so don't throw now.
            else
#if PBTCOMPILER
                if (!IsLocalPass1)
#endif
            {
                if (targetType != null)
                {
                    ThrowException(SRID.TemplateNoProp,
                                   member,
                                   targetType.FullName,
                                   xamlPropertyNode.LineNumber,
                                   xamlPropertyNode.LinePosition);
                }
                else
                {
                    ThrowException(SRID.TemplateNoTarget,
                                   member,
                                   xamlPropertyNode.LineNumber,
                                   xamlPropertyNode.LinePosition);
                }
            }

            return memberInfo;
        }

        /// <summary>
        /// The Value="foo" property node for a Setter and a Trigger has been saved
        /// and is resolved some time afterwards using the associated Property="Bar"
        /// attribute.  This is done so that the property record can be written using
        /// the type converter associated with "Bar"
        /// </summary>
        private void ProcessPropertyValueNode()
        {
            Debug.Assert(_setterOrTriggerValueNode != null);

            if (_setterOrTriggerPropertyMemberInfo != null)
            {
                // Now we have PropertyInfo or a MethodInfo for the property setter.
                // Get the type of the property from this which will be used
                // by BamlRecordWriter.WriteProperty to find an associated
                // TypeConverter to use at runtime.
                // To allow for per-property type converters we need to extract
                // information from the member info about the property
                Type propertyType = XamlTypeMapper.GetPropertyType(_setterOrTriggerPropertyMemberInfo);
                _setterOrTriggerValueNode.ValuePropertyType = propertyType;
                _setterOrTriggerValueNode.ValuePropertyMember = _setterOrTriggerPropertyMemberInfo;
                _setterOrTriggerValueNode.ValuePropertyName = XamlTypeMapper.GetPropertyName(_setterOrTriggerPropertyMemberInfo);
                _setterOrTriggerValueNode.ValueDeclaringType = _setterOrTriggerPropertyMemberInfo.DeclaringType;

                base.WriteProperty(_setterOrTriggerValueNode);
            }
            else
            {
                base.WriteBaseProperty(_setterOrTriggerValueNode);
            }

            _setterOrTriggerValueNode = null;
            _setterOrTriggerPropertyNode = null;
            _setterTargetNameOrConditionSourceName = null;
            _setterOrTriggerPropertyMemberInfo = null;
        }

        /// <summary>
        /// Write Def Attribute
        /// </summary>
        /// <remarks>
        /// Template parsing supports x:ID, so check for this here
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
                // the DataType="{x:Type SomeType}" key that may also be present.
                if (xamlDefAttributeNode.Name == XamlReaderHelper.DefinitionName &&
                    _styleModeStack.Mode == StyleMode.Base)
                {
                    _defNameFound = true;
                }
#endif
                base.WriteDefAttribute(xamlDefAttributeNode);
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
        /// For template parsing, determine when it is withing a Trigger or
        /// MultiTrigger section.  This is done for validity checking of
        /// unknown tags and attributes.
        /// </remarks>
        public override void WriteElementStart(XamlElementStartNode xamlElementStartNode)
        {
            StyleMode mode = _styleModeStack.Mode;
            bool tagWritten = false;

            if (mode == StyleMode.Base && _styleModeStack.Depth == 0)
            {
                // The default TargetType of the Template is needed for resolving names when
                // TargetType is not set. Remember it now appropriately for each kind of Template.
                // Ideally this should come from an attribute or other means instead of hard-coding here.
                if (KnownTypes.Types[(int)KnownElements.ControlTemplate].IsAssignableFrom(xamlElementStartNode.ElementType))
                {
                    _defaultTargetType = KnownTypes.Types[(int)KnownElements.Control];
                }
                else if (KnownTypes.Types[(int)KnownElements.DataTemplate].IsAssignableFrom(xamlElementStartNode.ElementType))
                {
                    _defaultTargetType = KnownTypes.Types[(int)KnownElements.ContentPresenter];
#if PBTCOMPILER
                    // The type to use for the dictionary key depends on what kind of
                    // template this is. Remember it now.
                    if (xamlElementStartNode.ElementType == ItemContainerTemplateType)
                    {
                        _templateKeyType = ItemContainerTemplateKeyType;
                    }
                    else
                    {
                        _templateKeyType = KnownTypes.Types[(int)KnownElements.DataTemplateKey];
                    }
#endif
                }
                else if (KnownTypes.Types[(int)KnownElements.ItemsPanelTemplate].IsAssignableFrom(xamlElementStartNode.ElementType))
                {
                    _defaultTargetType = KnownTypes.Types[(int)KnownElements.ItemsPresenter];
                }
            }

            _setterOrTriggerPropertyMemberInfo = null;

            // Track elements during compile so that we can resolve id names to types.  This
            // is useful when resolving Setter Property / Value attributes.
            _elementTypeStack.Push(xamlElementStartNode.ElementType);

            // Keep style mode and other state up-to-date.
            CommonElementStartProcessing(xamlElementStartNode, xamlElementStartNode.ElementType, ref mode);

            // The very first element encountered within a template block
            // should a template tree node.
            if (mode == StyleMode.Base && _styleModeStack.Depth > 0)
            {
                ; // Nothing special to do
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
                // Just entered the <Setter .../> section of a Trigger
                _inSetterDepth = xamlElementStartNode.Depth;
            }
#if PBTCOMPILER
            else if (_styleModeStack.Mode == StyleMode.DataTypeProperty &&
                     InDeferLoadedSection &&
                     _styleModeStack.Depth >= 2 &&
                     !_defNameFound)
            {
                // We have to treat DataType="{x:Type SomeType}" as a key in a
                // resource dictionary, if one is present.  This means generating
                // a series of baml records to use as the key for the defer loaded
                // body of the Style.
                if (_styleModeStack.Depth == 2)
                {
                    base.WriteKeyElementStart(new XamlElementStartNode(
                        xamlElementStartNode.LineNumber,
                        xamlElementStartNode.LinePosition,
                        xamlElementStartNode.Depth,
                        _templateKeyType.Assembly.FullName,
                        _templateKeyType.FullName,
                        _templateKeyType,
                        null));
                    base.WriteConstructorParametersStart(new XamlConstructorParametersStartNode(
                        xamlElementStartNode.LineNumber,
                        xamlElementStartNode.LinePosition,
                        xamlElementStartNode.Depth));
                }
                base.WriteElementStart(xamlElementStartNode);
                tagWritten = true;
            }
#endif

            // Handle custom serializers within the template section by creating an instance
            // of that serializer and handing off control.

            if (xamlElementStartNode.SerializerType != null && _styleModeStack.Depth > 0)
            {
                XamlSerializer serializer = XamlTypeMapper.CreateInstance(xamlElementStartNode.SerializerType)
                                                          as XamlSerializer;
                 if (serializer == null)
                 {
                     ThrowException(SRID.ParserNoSerializer,
                                   xamlElementStartNode.TypeFullName,
                                   xamlElementStartNode.LineNumber,
                                   xamlElementStartNode.LinePosition);
                 }
                 else
                 {
                     // Depending on whether this is the compile case or the parse xaml
                     // case, we want to convert the xaml into baml or objects.

                     #if PBTCOMPILER
                         serializer.ConvertXamlToBaml(TokenReader,
                                       BamlRecordWriter == null ? ParserContext : BamlRecordWriter.ParserContext,
                                       xamlElementStartNode, BamlRecordWriter);
                     #else

                         // If we're in the content of the template, we'll convert to baml.  Then TemplateBamlRecordReader
                         // gets the option of instantiating it or keeping it in baml.  For example, if this is a nested
                         // <Button.Style>, it can be instantiated, but if it's a part of the .Resources of an element
                         // in the template, it needs to be left in baml.

                         // Notice that TreeBuilder null check is for the case when the current template is within the
                         // content section of a parent template. This means we need to be writing to Baml.
                         // <DataTemplate>
                         //   <RadioButton>
                         //   <RadioButton.Template>
                         //     <ControlTemplate>
                         //     <ControlTemplate.Triggers>
                         //         <Trigger Property="..." Value="...">
                         //             <Setter Property="Style">
                         //                 <Style ... />
                         //             </Setter>
                         //         </Trigger>
                         //     </ControlTemplate.Triggers>
                         //     </ControlTemplate>
                         //   </RadioButton.Template>
                         //   </RadioButton>
                         // </DataTemplate>

                         if ( _styleModeStack.Mode == StyleMode.VisualTree ||
                              TreeBuilder == null )
                         {
                             serializer.ConvertXamlToBaml(TokenReader,
                                                          BamlRecordWriter.ParserContext,
                                                          xamlElementStartNode,
                                                          BamlRecordWriter);
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

                if (!tagWritten)
                {
                    base.WriteElementStart(xamlElementStartNode);
                }
            }
        }


        //
        //  CommonElementStartProcessing
        //
        //  This is used by WriteElementStart and WriteUnknownTagStart.  It is used
        //  to keep style mode up-to-date.
        //
        private void CommonElementStartProcessing (XamlNode xamlNode, Type elementType, ref StyleMode mode)
        {

            if (mode == StyleMode.Base && _styleModeStack.Depth > 0 )
            {
                if (_templateRootCount++ > 0)
                {
                    ThrowException(SRID.TemplateNoMultipleRoots,
                                   (elementType == null ? "Unknown tag" : elementType.Name),
                                   xamlNode.LineNumber,
                                   xamlNode.LinePosition);
                }

                // Validate that the root is an FE or FCE.  If the type is unknown (and internal type), we'll
                // catch this during template instantiation.

                if (elementType != null
                    &&
                    !KnownTypes.Types[(int)KnownElements.FrameworkElement].IsAssignableFrom(elementType)
                    &&
                    !KnownTypes.Types[(int)KnownElements.FrameworkContentElement].IsAssignableFrom(elementType))
                {
                    ThrowException(SRID.TemplateInvalidRootElementTag,
                                   elementType.ToString(),
                                   xamlNode.LineNumber,
                                   xamlNode.LinePosition);
                }

                mode = StyleMode.VisualTree;
            }

        }



        /// <summary>
        /// Write End Element
        /// </summary>
        /// <remarks>
        /// For template parsing, determine when it is withing a Trigger or
        /// MultiTrigger section.  This is done for validity checking of
        /// unknown tags and attributes.
        /// </remarks>
        public override void WriteElementEnd(XamlElementEndNode xamlElementEndNode)
        {
            bool tagWritten = false;

            if (_styleModeStack.Mode == StyleMode.TriggerBase &&
                xamlElementEndNode.Depth == _inSetterDepth)
            {
                // Just exited the <Setter .../> section of a Trigger
                _inSetterDepth = -1;
            }

            if (xamlElementEndNode.Depth == _inPropertyTriggerDepth)
            {
                _inPropertyTriggerDepth = -1;
            }

#if PBTCOMPILER
            if (_styleModeStack.Mode == StyleMode.DataTypeProperty &&
                     InDeferLoadedSection &&
                     !_defNameFound)
            {
                // We have to treat DataType="{x:Type SomeType}" as a key in a
                // resource dictionary, if one is present.  This means generating
                // a series of baml records to use as the key for the defer loaded
                // body of the Style in addition to generating the records to set
                // the TargetType value.
                if (_styleModeStack.Depth == 2)
                {
                    base.WriteElementEnd(xamlElementEndNode);
                    base.WriteConstructorParametersEnd(new XamlConstructorParametersEndNode(
                        xamlElementEndNode.LineNumber,
                        xamlElementEndNode.LinePosition,
                        xamlElementEndNode.Depth));
                    base.WriteKeyElementEnd(xamlElementEndNode);
                    tagWritten = true;
                }
            }
#endif

            // Track elements during compile so that we can resolve names to types.  This
            // is useful when resolving Setter Property / Value.
            _elementTypeStack.Pop();

            _styleModeStack.Pop();

            if (!tagWritten)
            {
                base.WriteElementEnd(xamlElementEndNode);
            }
        }

        private Type TargetType
        {
            get
            {
                return (_templateTargetTypeType != null ? _templateTargetTypeType
#if PBTCOMPILER
                                                        : IsLocalPass1 ? null
#endif
                                                        : _defaultTargetType);
            }
        }

#if PBTCOMPILER
        /// <summary>
        /// Write the start of a constructor parameter section
        /// </summary>
        public override void WriteConstructorParameterType(
            XamlConstructorParameterTypeNode xamlConstructorParameterTypeNode)
        {
            if (_styleModeStack.Mode == StyleMode.DataTypeProperty &&
                InDeferLoadedSection &&
                !_defNameFound)
            {
                // Generate a series of baml records to use as the key for the defer loaded
                // body of the Style in addition to generating the records to set
                // the normal constructor.
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
            if (_styleModeStack.Mode == StyleMode.DataTypeProperty &&
                InDeferLoadedSection &&
                !_defNameFound)
            {
                // We have to treat DataType="{x:Type SomeType}" as a key in a
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
            if (_styleModeStack.Mode == StyleMode.DataTypeProperty &&
                InDeferLoadedSection &&
                !_defNameFound &&
                _styleModeStack.Depth > 2)
            {
                // We have to treat DataType="{x:Type SomeType}" as a key in a
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
        /// For template parsing, treat complex property tags as
        /// xml element tags for the purpose of validity checking
        /// </remarks>
        public override void WritePropertyComplexStart(XamlPropertyComplexStartNode xamlNode)
        {
            StyleMode mode = _styleModeStack.Mode;

            if (_styleModeStack.Depth == 1)
            {
                if (xamlNode.PropName == XamlTemplateSerializer.TargetTypePropertyName)
                {
                    mode = StyleMode.TargetTypeProperty;
                }
                else if (xamlNode.PropName == XamlTemplateSerializer.DataTypePropertyName)
                {
                    mode = StyleMode.DataTypeProperty;
                }
                else if (xamlNode.PropName == XamlTemplateSerializer.ItemsSourcePropertyName ||
                        xamlNode.PropName == XamlTemplateSerializer.ItemTemplatePropertyName ||
                        xamlNode.PropName == XamlTemplateSerializer.ItemTemplateSelectorPropertyName ||
                        xamlNode.PropName == XamlTemplateSerializer.ItemContainerStylePropertyName ||
                        xamlNode.PropName == XamlTemplateSerializer.ItemContainerStyleSelectorPropertyName ||
                        xamlNode.PropName == XamlTemplateSerializer.ItemStringFormatPropertyName ||
                        xamlNode.PropName == XamlTemplateSerializer.ItemBindingGroupPropertyName ||
                        xamlNode.PropName == XamlTemplateSerializer.AlternationCountPropertyName
                        )
                {
                    mode = StyleMode.ComplexProperty;
                }
                else
                {
                    ThrowException(SRID.TemplateUnknownProp, xamlNode.PropName,
                                   xamlNode.LineNumber, xamlNode.LinePosition);
                }
            }
            else if (mode == StyleMode.VisualTree)
            {
                _visualTreeComplexPropertyDepth++;
            }
            else if (mode == StyleMode.TriggerBase)
            {
                _triggerComplexPropertyDepth++;
            }
#if PBTCOMPILER
            else if (mode == StyleMode.DataTypeProperty &&
                     InDeferLoadedSection &&
                     !_defNameFound)
            {
                // We have to treat DataType="{x:Type SomeType}" as a key in a
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
        /// For template parsing, treat complex property tags as
        /// xml element tags for the purpose of validity checking
        /// </remarks>
        public override void WritePropertyComplexEnd(XamlPropertyComplexEndNode xamlNode)
        {
            StyleMode mode = _styleModeStack.Mode;

            if (mode == StyleMode.VisualTree)
            {
                _visualTreeComplexPropertyDepth--;
            }
            if (mode == StyleMode.TriggerBase)
            {
                _triggerComplexPropertyDepth--;
            }
#if PBTCOMPILER
            else if (mode == StyleMode.DataTypeProperty &&
                     InDeferLoadedSection &&
                     !_defNameFound &&
                     _styleModeStack.Depth > 2)
            {
                // We have to treat DataType="{x:Type SomeType}" as a key in a
                // resource dictionary, if one is present.  This means generating
                // a series of baml records to use as the key for the defer loaded
                // body of the Style in addition to generating the records to set
                // the TargetType value.
                base.WritePropertyComplexEnd(xamlNode);
            }
#endif
            if (_styleModeStack.Depth <= 2)
            {
                mode = StyleMode.Base;
            }

            _styleModeStack.Pop();
            base.WritePropertyComplexEnd(xamlNode);
        }

        /// <summary>
        /// Write start of a list complex property
        /// </summary>
        /// <remarks>
        /// For template parsing, treat complex property tags as
        /// xml element tags for the purpose of validity checking
        /// </remarks>
        public override void WritePropertyIListStart(XamlPropertyIListStartNode xamlNode)
        {
            StyleMode mode = _styleModeStack.Mode;

            if (_styleModeStack.Depth == 1)
            {
                if (xamlNode.PropName == XamlTemplateSerializer.TriggersPropertyName)
                {
                    mode = StyleMode.TriggerBase;
                }
                else
                {
                    ThrowException(SRID.TemplateUnknownProp, xamlNode.PropName,
                                   xamlNode.LineNumber, xamlNode.LinePosition);
                }
            }
            else if (mode == StyleMode.VisualTree)
            {
                _visualTreeComplexPropertyDepth++;
            }
            else if (mode == StyleMode.TriggerBase &&
                     _styleModeStack.Depth == 2)
            {
                mode = StyleMode.Base;
            }
            else if (mode == StyleMode.TriggerBase &&
                     _styleModeStack.Depth == 3 &&
                     xamlNode.PropName == XamlTemplateSerializer.EventTriggerActions)
            {
                mode = StyleMode.TriggerActions;
            }
            else if (mode == StyleMode.TriggerBase)
            {
                _triggerComplexPropertyDepth++;
            }

            _styleModeStack.Push(mode);
            base.WritePropertyIListStart(xamlNode);
        }

        /// <summary>
        /// Write end of a list complex property
        /// </summary>
        /// <remarks>
        /// For template parsing, treat complex property tags as
        /// xml element tags for the purpose of validity checking when we're counting
        /// element tags.
        /// </remarks>
        public override void WritePropertyIListEnd(XamlPropertyIListEndNode xamlNode)
        {
            if (_styleModeStack.Mode == StyleMode.VisualTree)
            {
                _visualTreeComplexPropertyDepth--;
            }
            else if (_styleModeStack.Mode == StyleMode.TriggerBase)
            {
                _triggerComplexPropertyDepth--;
            }

            base.WritePropertyIListEnd(xamlNode);
            _styleModeStack.Pop();
        }

        /// <summary>
        /// Write Property Array Start
        /// </summary>
        public override void WritePropertyArrayStart(XamlPropertyArrayStartNode xamlPropertyArrayStartNode)
        {
            if (_styleModeStack.Mode == StyleMode.VisualTree)
            {
                _visualTreeComplexPropertyDepth++;
            }
            else if (_styleModeStack.Mode == StyleMode.TriggerBase)
            {
                _triggerComplexPropertyDepth++;
            }

            base.WritePropertyArrayStart(xamlPropertyArrayStartNode);
            _styleModeStack.Push();
        }


        /// <summary>
        /// Write Property Array End
        /// </summary>
        public override void WritePropertyArrayEnd(XamlPropertyArrayEndNode xamlPropertyArrayEndNode)
        {
            if (_styleModeStack.Mode == StyleMode.VisualTree)
            {
                _visualTreeComplexPropertyDepth--;
            }
            else if (_styleModeStack.Mode == StyleMode.TriggerBase)
            {
                _triggerComplexPropertyDepth--;
            }
            base.WritePropertyArrayEnd(xamlPropertyArrayEndNode);
            _styleModeStack.Pop();
        }

        /// <summary>
        /// Write Property IDictionary Start
        /// </summary>
        public override void WritePropertyIDictionaryStart(XamlPropertyIDictionaryStartNode xamlPropertyIDictionaryStartNode)
        {
            StyleMode mode = _styleModeStack.Mode;

            if (mode == StyleMode.VisualTree)
            {
                _visualTreeComplexPropertyDepth++;
            }
            else if (mode == StyleMode.TriggerBase)
            {
                _triggerComplexPropertyDepth++;
            }
            else if (_styleModeStack.Depth == 1 && mode == StyleMode.Base)
            {
                if( xamlPropertyIDictionaryStartNode.PropName == XamlTemplateSerializer.ResourcesPropertyName)
                {
                    mode = StyleMode.Resources;
                }
                else
                {
                    ThrowException(SRID.TemplateUnknownProp, xamlPropertyIDictionaryStartNode.PropName,
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
            StyleMode mode = _styleModeStack.Mode;

            if (mode == StyleMode.VisualTree)
            {
                _visualTreeComplexPropertyDepth--;
            }
            else if (mode == StyleMode.TriggerBase)
            {
                _triggerComplexPropertyDepth--;
            }

            base.WritePropertyIDictionaryEnd(xamlPropertyIDictionaryEndNode);
            _styleModeStack.Pop();
        }

        /// <summary>
        /// Write Text node and do template related error checking
        /// </summary>
        public override void WriteText(XamlTextNode xamlTextNode)
        {
            StyleMode mode = _styleModeStack.Mode;

            // Text is only valid within certain locations in the Template section.
            // Check for all the valid locations and write out the text record.  For
            // all other locations, see if the text is non-blank and throw an error
            // if it is.  Ignore any whitespace, since this is not considered
            // significant in Style cases.
            if (mode == StyleMode.TargetTypeProperty)
            {
                // Remember the TargetType so that the setter parsing could use it
                // to resolve non-qualified property names
                if (xamlTextNode.Text != null)
                {
                    _templateTargetTypeType = XamlTypeMapper.GetTypeFromBaseString(xamlTextNode.Text,
                                                                                   ParserContext,
                                                                                   true);
                }
            }

#if PBTCOMPILER
            if (mode == StyleMode.DataTypeProperty &&
                InDeferLoadedSection &&
                !_defNameFound)
            {
                // We have to treat DataType="{x:Type SomeType}" as a key in a
                // resource dictionary, if one is present.  This means generating
                // a series of baml records to use as the key for the defer loaded
                // body of the Style in addition to generating the records to set
                // the TargetType value.
                base.WriteText(xamlTextNode);
            }
#endif
            // Text is only valid within certain locations in the <Template> section.
            // Check for all the valid locations and write out the text record.  For
            // all other locations, see if the text is non-blank and throw an error
            // if it is.  Ignore any whitespace, since this is not considered
            // significant in Template cases.
            if (mode != StyleMode.TriggerBase ||
                _inSetterDepth >= 0 ||
                _triggerComplexPropertyDepth >= 0)
            {
                base.WriteText(xamlTextNode);
            }
            else
            {
                for (int i = 0; i< xamlTextNode.Text.Length; i++)
                {
                    if (!XamlReaderHelper.IsWhiteSpace(xamlTextNode.Text[i]))
                    {
                        ThrowException(SRID.TemplateTextNotSupported, xamlTextNode.Text,
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
                Debug.Assert(_styleModeStack.Mode == StyleMode.VisualTree);

                // if this event token is not owned by this TemplateXamlParser, then just chain
                // the WriteClrEvent call to the controlling parser. Ulimately, this will
                // reach the markup compiler that will deal with this info as appropriate.

                bool isOriginatingEvent = xamlClrEventNode.IsOriginatingEvent;
                if (isOriginatingEvent)
                {
                    // set up additional state on event node for the markup compiler to use.
                    Debug.Assert(!xamlClrEventNode.IsStyleSetterEvent);
                    xamlClrEventNode.IsTemplateEvent = true;
                    xamlClrEventNode.IsSameScope = _isSameScope;

                    // any intermediary controlling parsers need to get out of the way so that
                    // the markup compiler can ultimately do its thing.
                    xamlClrEventNode.IsOriginatingEvent = false;

                    // Store away the type of the element we're currently in.  E.g.
                    // in <Button Click="OnClicked"/>, this will be typeof(Button).
                    // In the case of regular CLR events, this type is the same as that
                    // which can be found in xamlClrEventNode.EventMember.  But in the
                    // case of attached events, EventMember is the class that holds
                    // the attached event, and the compiler needs to know the type of
                    // the listener.

                    xamlClrEventNode.ListenerType = (Type) _elementTypeStack.Peek();
                }

                _previousXamlParser.WriteClrEvent(xamlClrEventNode);

                if (isOriginatingEvent)
                {
                    if (!String.IsNullOrEmpty(xamlClrEventNode.LocalAssemblyName))
                    {
                        // if this event is a local event need to generate baml for it now

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
        /// <remarks>
        /// When in a VisualTree, only DependencyProperties can be set directly on
        /// a FrameworkElementFactory.  This method checks the state of parsing and
        /// with throw an exception if a clr property is set on a FrameworkElementFactory.
        /// </remarks>
        public override void WriteProperty(XamlPropertyNode xamlPropertyNode)
        {
            StyleMode mode = _styleModeStack.Mode;

            if (mode == StyleMode.TriggerBase &&
                WritePropertyForTriggers(xamlPropertyNode))
            {
                return;
            }

            // If we are on the DataTemplate tag itself, and we encounter a DataType property,
            // this can be used as the key when placing this template in a ResourceDictionary.
            // In that case, remember what the property value is so that we can
            // later update the defer key held by the baml writer.
            if (mode == StyleMode.Base &&
                xamlPropertyNode.PropName == XamlTemplateSerializer.DataTypePropertyName)
            {
#if PBTCOMPILER
                // Treat DataType="some string" as a key in a resource dictionary.
                // Generate a sequence of baml records to describe the key.
                if (InDeferLoadedSection && !_defNameFound)
                {
                    base.WriteKeyElementStart(new XamlElementStartNode(
                        xamlPropertyNode.LineNumber,
                        xamlPropertyNode.LinePosition,
                        xamlPropertyNode.Depth,
                        _templateKeyType.Assembly.FullName,
                        _templateKeyType.FullName,
                        _templateKeyType,
                        null));
                    base.WriteConstructorParametersStart(new XamlConstructorParametersStartNode(
                        xamlPropertyNode.LineNumber,
                        xamlPropertyNode.LinePosition,
                        xamlPropertyNode.Depth));
                    base.WriteText(new XamlTextNode(
                        xamlPropertyNode.LineNumber,
                        xamlPropertyNode.LinePosition,
                        xamlPropertyNode.Depth,
                        xamlPropertyNode.Value,
                        null));
                    base.WriteConstructorParametersEnd(new XamlConstructorParametersEndNode(
                        xamlPropertyNode.LineNumber,
                        xamlPropertyNode.LinePosition,
                        xamlPropertyNode.Depth));
                    base.WriteKeyElementEnd(new XamlElementEndNode(
                        xamlPropertyNode.LineNumber,
                        xamlPropertyNode.LinePosition,
                        xamlPropertyNode.Depth));
                }
#endif
            }
#if PBTCOMPILER
            else if (mode == StyleMode.DataTypeProperty &&
                     InDeferLoadedSection &&
                     !_defNameFound)
            {
                // We have to treat DataType="{x:Type SomeType}" as a key in a
                // resource dictionary, if one is present.  This means generating
                // a series of baml records to use as the key for the defer loaded
                // body of the Style.
                base.WriteProperty(xamlPropertyNode);
            }
#endif
            xamlPropertyNode.DefaultTargetType = TargetType;
            base.WriteProperty(xamlPropertyNode);

            // If the property being written identifies a Runtime name, then remember
            // the name and the type associated with it.  This may be needed for
            // Setter value and property resolutions later on.
            if (xamlPropertyNode.AttributeUsage == BamlAttributeUsage.RuntimeName)
            {
                if (_IDTypes.ContainsKey(xamlPropertyNode.Value))
                {
                    ThrowException(SRID.TemplateDupName, xamlPropertyNode.Value,
                                  xamlPropertyNode.LineNumber, xamlPropertyNode.LinePosition);
                }
                else
                {
                    _IDTypes[xamlPropertyNode.Value] = _elementTypeStack.Peek() as Type;
                }
            }
        }

        /// <summary>
        /// Handle property node when within a Triggers section.  Return true if this
        /// node is fully handled and needs no further processing.
        /// </summary>
        private bool WritePropertyForTriggers(XamlPropertyNode xamlPropertyNode)
        {
            if (_inSetterDepth >= 0)
            {
                if (xamlPropertyNode.PropName == XamlTemplateSerializer.SetterValueAttributeName)
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
                    return true;
                }
                else if (xamlPropertyNode.PropName == XamlTemplateSerializer.SetterPropertyAttributeName)
                {
                    // Property names should be trimmed since whitespace is not significant
                    // and can affect name resolution 
                    xamlPropertyNode.SetValue(xamlPropertyNode.Value.Trim());
                    _setterOrTriggerPropertyNode = xamlPropertyNode;

                    // return now as Setter.TargetName might not have been set yet and we need that for resolving
                    // the property name. So this is done in WriteEndattributes.
                    return true;
                }
                else if (xamlPropertyNode.PropName == XamlTemplateSerializer.SetterTargetAttributeName)
                {
                    _setterTargetNameOrConditionSourceName = xamlPropertyNode.Value;
                }
            }
            else
            {
                if (xamlPropertyNode.PropName == XamlTemplateSerializer.PropertyTriggerValuePropertyName)
                {
                    // DataTrigger doesn't have a "Property" property so value has to be written directly.
                    // This check filters out if "Property" was actually set vs. not present at all for
                    // other Triggers.
                    Type t = (Type)_elementTypeStack.Peek();
                    if (!KnownTypes.Types[(int)KnownElements.DataTrigger].IsAssignableFrom(t))
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
                        return true;
                    }
                }
                else if (xamlPropertyNode.PropName == XamlTemplateSerializer.PropertyTriggerPropertyName)
                {
                    // Property names should be trimmed since whitespace is not significant
                    // and can affect name resolution 
                    xamlPropertyNode.SetValue(xamlPropertyNode.Value.Trim());
                    _setterOrTriggerPropertyNode = xamlPropertyNode;

                    // return now as Trigger.SourceName might not have been set yet and we need that for resolving
                    // the property name. So this is done in WriteEndattributes.
                    return true;
                }
                else if (xamlPropertyNode.PropName == XamlTemplateSerializer.PropertyTriggerSourceName)
                {
                    _setterTargetNameOrConditionSourceName = xamlPropertyNode.Value;
                }
            }

            return false;
        }

        public override void WritePropertyWithExtension(XamlPropertyWithExtensionNode xamlPropertyWithExtensionNode)
        {
            xamlPropertyWithExtensionNode.DefaultTargetType = TargetType;
            base.WritePropertyWithExtension(xamlPropertyWithExtensionNode);
        }

        /// <summary>
        /// Write a Property, which has the form in markup of property="value" where
        /// value has been resolved to a Type reference.
        /// </summary>
        /// <remarks>
        /// When in a VisualTree, only DependencyProperties can be set directly on
        /// a FrameworkElementFactory.  This method checks the state of parsing and
        /// with throw an exception if a clr property is set on a FrameworkElementFactory.
        /// </remarks>
        public override void WritePropertyWithType(XamlPropertyWithTypeNode xamlPropertyNode)
        {
            StyleMode mode = _styleModeStack.Mode;

            if (mode == StyleMode.Base &&
                xamlPropertyNode.PropName == XamlTemplateSerializer.TargetTypePropertyName)
            {
                _templateTargetTypeType = xamlPropertyNode.ValueElementType;
            }
            // If we are on the DataTemplate tag itself, and we encounter a DataType property,
            // this can be used as the key when placing this template in a ResourceDictionary.
            // In that case, remember what the property value is so that we can
            // later update the defer key held by the baml writer.
            else if (mode == StyleMode.Base &&
                xamlPropertyNode.PropName == XamlTemplateSerializer.DataTypePropertyName)
            {
#if PBTCOMPILER
                // Treat DataType="some type" as a key in a resource dictionary.
                // Generate a sequence of baml records to describe the key.
                if (InDeferLoadedSection && !_defNameFound)
                {
                    _dataTypePropertyNode = xamlPropertyNode;
                    _dataTypePropertyNodeDepth = _styleModeStack.Depth;
                }
#endif
            }
#if PBTCOMPILER
            else if (mode == StyleMode.DataTypeProperty &&
                     InDeferLoadedSection &&
                     !_defNameFound)
            {
                // We have to treat DataType="{x:Type SomeType}" as a key in a
                // resource dictionary, if one is present.  This means generating
                // a series of baml records to use as the key for the defer loaded
                // body of the Style.
                base.WritePropertyWithType(xamlPropertyNode);
            }
#endif

            base.WritePropertyWithType(xamlPropertyNode);
        }

#if PBTCOMPILER
        // This method writes out BAML to produce a resource key based on the
        // DataType property.
        private void WriteDataTypeKey(XamlPropertyWithTypeNode xamlPropertyNode)
        {
            base.WriteKeyElementStart(new XamlElementStartNode(
                xamlPropertyNode.LineNumber,
                xamlPropertyNode.LinePosition,
                xamlPropertyNode.Depth,
                _templateKeyType.Assembly.FullName,
                _templateKeyType.FullName,
                _templateKeyType,
                null));
            base.WriteConstructorParametersStart(new XamlConstructorParametersStartNode(
                xamlPropertyNode.LineNumber,
                xamlPropertyNode.LinePosition,
                xamlPropertyNode.Depth));
            base.WriteConstructorParameterType(new XamlConstructorParameterTypeNode(
                xamlPropertyNode.LineNumber,
                xamlPropertyNode.LinePosition,
                xamlPropertyNode.Depth,
                xamlPropertyNode.ValueTypeFullName,
                xamlPropertyNode.ValueTypeAssemblyName,
                xamlPropertyNode.ValueElementType));
            base.WriteConstructorParametersEnd(new XamlConstructorParametersEndNode(
                xamlPropertyNode.LineNumber,
                xamlPropertyNode.LinePosition,
                xamlPropertyNode.Depth));
            base.WriteKeyElementEnd(new XamlElementEndNode(
                xamlPropertyNode.LineNumber,
                xamlPropertyNode.LinePosition,
                xamlPropertyNode.Depth));
        }
#endif

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
        // does some validations that are not valid in the case of parsing Templates, so do a
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
        /// defer load section of markup.  Note that if this is nested within a
        /// Style, then don't consider it to be in a defer loaded section since
        /// this is used a cue to determine when to generate dictionary keys.
        /// </summary>
        bool InDeferLoadedSection
        {
            get { return BamlRecordWriter != null &&
                         BamlRecordWriter.InDeferLoadedSection &&
                         _previousXamlParser.GetType() != typeof(StyleXamlParser); }
        }

        /// <summary>
        /// Return true if this is pass one of a compile process.
        /// </summary>
        bool IsLocalPass1
        {
            get { return BamlRecordWriter == null; }
        }
        
        private Type ItemContainerTemplateType
        {
            get
            {
                if (_itemContainerTemplateType == null)
                {
                    _itemContainerTemplateType = XamlTypeMapper.AssemblyPF.GetType(_itemContainerTemplateTypeName);
                }
                return _itemContainerTemplateType;
            }
        }

        private Type ItemContainerTemplateKeyType
        {
            get
            {
                if (_itemContainerTemplateKeyType == null)
                {
                    _itemContainerTemplateKeyType = XamlTypeMapper.AssemblyPF.GetType(_itemContainerTemplateKeyTypeName);
                }
                return _itemContainerTemplateKeyType;
            }
        }

#endif

#endregion Properties

#region Data

#if !PBTCOMPILER
        // TreeBuilder that created this parser
        XamlTreeBuilder _treeBuilder;
#endif
        // The XamlParser that the TokenReader was using when this instance of
        // the TemplateXamlParser was created.  This must be restored on exit
        XamlParser      _previousXamlParser;

        // Depth in the Xaml file when parsing of this template block started.
        // This is used to determine when to stop parsing
        int             _startingDepth;

        // Number of template root nodes encountered immediately under a Template.  Only 1
        // is allowed.
        int             _templateRootCount;

        StyleModeStack  _styleModeStack = new StyleModeStack();

        // Depth in the element tree where a <Setter .../> has begun.
        int             _inSetterDepth = -1;

        // The actual Type of the TargetType property on template.  This may be null.
        Type            _templateTargetTypeType;

        // The default TargetType of the template to use when TargetType is not set.
        Type            _defaultTargetType;

        // The XamlPropertyNode for the "Foo" Property attribute in <Setter Property="Foo" ... />
        // or <Trigger Property="Foo" .../>
        XamlPropertyNode _setterOrTriggerPropertyNode;

        // The Property node for Value attribute in <Setter Value="Bar" ... /> or
        // <Trigger Value="Bar" .../>
        XamlPropertyNode _setterOrTriggerValueNode;

        // Depth in the element tree where a Trigger or MultiTrigger
        // section has begun.  Set to -1 to indicate it is not within such a section.
        int             _inPropertyTriggerDepth = -1;

        // Depth of nested complex properties within a VisualTree.  This is used to
        // track when it is valid to have a clr property vs a dependency property
        // specified in the VisualTree;
        int             _visualTreeComplexPropertyDepth = -1;

        // Depth of nested complex properties within the Triggers section.  This is used to
        // track when it is valid to have text.
        int             _triggerComplexPropertyDepth = -1;
#if PBTCOMPILER
        // True if x:Key property was found on the template tag
        bool            _defNameFound;

        // During second pass, remember the info for the DataType property, so
        // that we can write out a key if no x:Key is present
        XamlPropertyWithTypeNode _dataTypePropertyNode;
        int                      _dataTypePropertyNodeDepth;

        // True if event is in the same VisualTree FEF.
        bool            _isSameScope;

        // Type to use for the implicit key in the dictionary
        Type            _templateKeyType;

        // Cached Type for ItemContainerTemplate
        private static Type _itemContainerTemplateType;

        // Cached Type for ItemContainerTemplateKey
        private static Type _itemContainerTemplateKeyType;

        private const string _itemContainerTemplateTypeName = "System.Windows.Controls.ItemContainerTemplate";
        private const string _itemContainerTemplateKeyTypeName = "System.Windows.Controls.ItemContainerTemplateKey";

#endif
        // Stack to keep track of element types during compile.
        Stack           _elementTypeStack = new Stack(5);

        // Dictionary of names, where the key is the name string and the value
        // is the element type that is using that name.
        Hashtable       _IDTypes = new Hashtable();

        // The value of <Setter TargetName="foo" ... /> that identifies the target
        // of the set operation or the value of <Condition SourceName="bar" ... />
        // that identifies the source of a trigger condition.
        string          _setterTargetNameOrConditionSourceName;

        // MemberInfo fo the DP Property value for the case where a Value is read as ComplexAsSimple.
        MemberInfo      _setterOrTriggerPropertyMemberInfo;

#endregion Data
    }
}

